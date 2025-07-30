using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;
using DOL.GS.ServerProperties;
using DOL.GS.SkillHandler;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.Language;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class AttackComponent : IServiceObject
    {
        public const double INHERENT_WEAPON_SKILL = 15.0;
        public const double INHERENT_ARMOR_FACTOR = 12.5;

        public GameLiving owner;
        public WeaponAction weaponAction; // This represents the current weapon action, which may become outdated when resolving ranged attacks.
        public AttackAction attackAction;
        public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.AttackComponent);
        public AttackerTracker AttackerTracker { get; private set; }

        private BlockRoundHandler _blockRoundHandler;
        private GameObject _startAttackTarget;
        private int _startAttackRequested;
        private GameLiving[] _broadcastExcludes = new GameLiving[3];

        public bool StartAttackRequested
        {
            get => Interlocked.CompareExchange(ref _startAttackRequested, 0, 0) == 1;
            set => Interlocked.Exchange(ref _startAttackRequested, Convert.ToInt32(value));
        }

        public AttackComponent(GameLiving owner)
        {
            this.owner = owner;
            attackAction = AttackAction.Create(owner);
            AttackerTracker = new(owner);
            _blockRoundHandler = new(owner);
        }

        public void Tick()
        {
            if (owner.ObjectState is not eObjectState.Active)
            {
                attackAction.CleanUp();
                ServiceObjectStore.Remove(this);
                return;
            }

            if (StartAttackRequested)
            {
                StartAttackRequested = false;
                StartAttack();
            }

            if (!attackAction.Tick())
                ServiceObjectStore.Remove(this);
        }

        public void AddAttacker(AttackData attackData)
        {
            long expireTime = GameLoop.GameLoopTime + (attackData.Interval > 0 ? attackData.Interval : Properties.SPELL_INTERRUPT_DURATION);
            AttackerTracker.AddOrUpdate(attackData.Attacker, attackData.IsMeleeAttack, expireTime);
        }

        /// <summary>
        /// The chance for a critical hit
        /// </summary>
        public int CalculateCriticalChance(WeaponAction action)
        {
            switch (owner.ActiveWeaponSlot)
            {
                default:
                case eActiveWeaponSlot.Standard:
                case eActiveWeaponSlot.TwoHanded:
                    return owner.GetModified(eProperty.CriticalMeleeHitChance);
                case eActiveWeaponSlot.Distance:
                    return action?.RangedAttackType is eRangedAttackType.Critical ? 0 : owner.GetModified(eProperty.CriticalArcheryHitChance);
            }
        }

        public DbInventoryItem GetAttackAmmo(WeaponAction action)
        {
            // Returns the ammo used by the passed down `WeaponAction` if there's any.
            // The currently active ammo otherwise.
            DbInventoryItem ammo = action?.Ammo;
            ammo ??= owner.rangeAttackComponent.Ammo;
            return ammo;
        }

        /// <summary>
        /// Returns the damage type of the current attack
        /// </summary>
        /// <param name="weapon">attack weapon</param>
        public eDamageType AttackDamageType(DbInventoryItem weapon, WeaponAction action)
        {
            GamePlayer playerOwner = owner as GamePlayer;

            if (playerOwner != null || owner is CommanderPet)
            {
                if (weapon == null)
                    return eDamageType.Natural;

                switch ((eObjectType) weapon.Object_Type)
                {
                    case eObjectType.Crossbow:
                    case eObjectType.Longbow:
                    case eObjectType.CompositeBow:
                    case eObjectType.RecurvedBow:
                    case eObjectType.Fired:
                    {
                        DbInventoryItem ammo = GetAttackAmmo(action);
                        return (eDamageType) (ammo == null ? weapon.Type_Damage : ammo.Type_Damage);
                    }
                    case eObjectType.Shield:
                        return eDamageType.Crush; // TODO: shields do crush damage (!) best is if Type_Damage is used properly
                    default:
                        return (eDamageType) weapon.Type_Damage;
                }
            }
            else if (owner is GameNPC npcOwner)
                return npcOwner.MeleeDamageType;
            else
                return eDamageType.Natural;
        }

        private bool _attackState;

        public virtual bool AttackState
        {
            get => _attackState || StartAttackRequested;
            set => _attackState = value;
        }

        /// <summary>
        /// Gets which weapon was used for the last dual wield attack
        /// 0: right (or non dual wield user), 1: left, 2: both
        /// </summary>
        public int UsedHandOnLastDualWieldAttack { get; set; }

        /// <summary>
        /// Returns this attack's range
        /// </summary>
        public int AttackRange
        {
            /* tested with:
            staff					= 125-130
            sword			   		= 126-128.06
            shield (Numb style)		= 127-129
            polearm	(Impale style)	= 127-130
            mace (Daze style)		= 127.5-128.7
            Think it's safe to say that it never changes; different with mobs. */

            get
            {
                if (owner is GamePlayer player)
                {
                    DbInventoryItem weapon = owner.ActiveWeapon;

                    if (weapon == null)
                        return 0;

                    GameLiving target = player.TargetObject as GameLiving;

                    // TODO: Change to real distance of bows.
                    if (weapon.SlotPosition == (int)eInventorySlot.DistanceWeapon)
                    {
                        double range;

                        switch ((eObjectType) weapon.Object_Type)
                        {
                            case eObjectType.Longbow:
                                range = 1760;
                                break;
                            case eObjectType.RecurvedBow:
                                range = 1680;
                                break;
                            case eObjectType.CompositeBow:
                                range = 1600;
                                break;
                            case eObjectType.Thrown:
                                range = 1160;
                                if (weapon.Name.ToLower().Contains("weighted"))
                                    range = 1450;
                                break;
                            default:
                                range = 1200;
                                break; // Shortbow, crossbow, throwing.
                        }

                        range = Math.Max(32, range * player.GetModified(eProperty.ArcheryRange) * 0.01);
                        DbInventoryItem ammo = GetAttackAmmo(null);

                        if (ammo != null)
                            switch ((ammo.SPD_ABS >> 2) & 0x3)
                            {
                                case 0:
                                    range *= 0.85;
                                    break; // Clout -15%
                                //case 1:
                                //  break; // (none) 0%
                                case 2:
                                    range *= 1.15;
                                    break; // Doesn't exist on live
                                case 3:
                                    range *= 1.25;
                                    break; // Flight +25%
                            }

                        // 1.70m: The maximum range bonus given to archery attacks from elevation has been set to 500.
                        if (target != null)
                            range += (player.Z - target.Z) / 2.0;

                        if (range < 32)
                            range = 32;

                        return (int) range;
                    }

                    return owner.MeleeAttackRange;
                }
                else
                {
                    return owner.ActiveWeaponSlot is eActiveWeaponSlot.Distance
                        ? Math.Max(32, (int) (2000.0 * owner.GetModified(eProperty.ArcheryRange) * 0.01))
                        : owner.MeleeAttackRange;
                }
            }
        }

        /// <summary>
        /// Gets the current attackspeed of this living in milliseconds
        /// </summary>
        /// <returns>effective speed of the attack. average if more than one weapon.</returns>
        public int AttackSpeed(DbInventoryItem mainWeapon, DbInventoryItem leftWeapon = null)
        {
            int minimum;

            if (owner is GamePlayer player)
            {
                if (mainWeapon == null)
                    return 0;

                minimum = 1500;
                double speed = 0;
                bool bowWeapon = false;

                // If leftWeapon is null even on a dual wield attack, use the mainWeapon instead.
                switch (UsedHandOnLastDualWieldAttack)
                {
                    case 2:
                    {
                        speed = mainWeapon.SPD_ABS;

                        if (leftWeapon != null)
                        {
                            speed += leftWeapon.SPD_ABS;
                            speed /= 2;
                        }

                        break;
                    }
                    case 1:
                    {
                        speed = leftWeapon != null ? leftWeapon.SPD_ABS : mainWeapon.SPD_ABS;
                        break;
                    }
                    case 0:
                    {
                        speed = mainWeapon.SPD_ABS;
                        break;
                    }
                }

                if (speed == 0)
                    return 0;

                bowWeapon = (eObjectType) mainWeapon.Object_Type is
                    eObjectType.Fired or
                    eObjectType.Longbow or
                    eObjectType.Crossbow or
                    eObjectType.RecurvedBow or
                    eObjectType.CompositeBow;
                int quickness = Math.Min(250, player.Quickness); //250 soft cap on quickness

                if (bowWeapon)
                {
                    if (Properties.ALLOW_OLD_ARCHERY)
                    {
                        //Draw Time formulas, there are very many ...
                        //Formula 2: y = iBowDelay * ((100 - ((iQuickness - 50) / 5 + iMasteryofArcheryLevel * 3)) / 100)
                        //Formula 1: x = (1 - ((iQuickness - 60) / 500 + (iMasteryofArcheryLevel * 3) / 100)) * iBowDelay
                        //Table a: Formula used: drawspeed = bowspeed * (1-(quickness - 50)*0.002) * ((1-MoA*0.03) - (archeryspeedbonus/100))
                        //Table b: Formula used: drawspeed = bowspeed * (1-(quickness - 50)*0.002) * (1-MoA*0.03) - ((archeryspeedbonus/100 * basebowspeed))

                        speed *= 1.0 - (quickness - 60) * 0.002;
                        double percent;
                        percent = speed * 0.01 * player.GetModified(eProperty.ArcherySpeed);
                        speed -= percent;

                        if (owner.rangeAttackComponent.RangedAttackType is eRangedAttackType.Critical)
                            speed = speed * 2 - (player.GetAbilityLevel(Abilities.Critical_Shot) - 1) * speed / 10;
                        else if (owner.rangeAttackComponent.RangedAttackType is eRangedAttackType.RapidFire)
                        {
                            speed *= RangeAttackComponent.RAPID_FIRE_ATTACK_SPEED_MODIFIER;
                            minimum = 900;
                        }
                    }
                    else
                        speed *= 1.0 - (quickness - 60) * 0.002;
                }
                else
                    speed *= (1.0 - (quickness - 60) * 0.002) * 0.01 * player.GetModified(eProperty.MeleeSpeed);

                return (int) Math.Max(minimum, speed * 100);
            }
            else
            {
                minimum = 500;
                double speed = NpcWeaponSpeed(mainWeapon) * 100 * (1.0 - (owner.GetModified(eProperty.Quickness) - 60) / 500.0);

                if (owner is GameSummonedPet pet)
                {
                    if (pet != null)
                    {
                        double modifier;

                        // This is pretty terrible.
                        switch (pet.Name)
                        {
                            case "amber simulacrum":
                            case "emerald simulacrum":
                            {
                                modifier = 1.45;
                                break;
                            }
                            case "ruby simulacrum":
                            case "sapphire simulacrum":
                            case "jade simulacrum":
                            {
                                modifier = 0.95;
                                break;
                            }
                            default:
                            {
                                modifier = 1.0;
                                break;
                            }
                        }

                        speed *= owner.GetModified(eProperty.MeleeSpeed) * 0.01 * modifier;
                    }
                }
                else
                {
                    if (owner.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
                    {
                        // Old archery uses archery speed, but new archery uses casting speed.
                        if (Properties.ALLOW_OLD_ARCHERY)
                            speed *= 1.0 - owner.GetModified(eProperty.ArcherySpeed) * 0.01;
                        else
                            speed *= 1.0 - owner.GetModified(eProperty.CastingSpeed) * 0.01;
                    }
                    else
                        speed *= owner.GetModified(eProperty.MeleeSpeed) * 0.01;
                }

                return (int) Math.Max(minimum, speed);
            }
        }

        /// <summary>
        /// InventoryItem.SPD_ABS isn't set for NPCs, so this method must be used instead.
        /// </summary>
        public static int NpcWeaponSpeed(DbInventoryItem weapon)
        {
            return weapon?.SlotPosition switch
            {
                Slot.TWOHAND => 40,
                Slot.RANGED => 45,
                _ => 30,
            };
        }

        public double AttackDamage(DbInventoryItem weapon, WeaponAction action, out double damageCap)
        {
            damageCap = 0;

            if (owner is GamePlayer player)
            {
                if (weapon == null)
                    return 0;

                damageCap = player.WeaponDamageWithoutQualityAndCondition(weapon) * weapon.SPD_ABS * 0.1 * CalculateSlowWeaponDamageModifier(weapon);

                if (player.ActiveLeftWeapon != null)
                {
                    if (weapon.Item_Type is Slot.RIGHTHAND or Slot.LEFTHAND or Slot.TWOHAND)
                        damageCap *= CalculateLeftAxeModifier();
                }
                else if (weapon.Item_Type is Slot.RANGED)
                {
                    damageCap *= CalculateTwoHandedDamageModifier(weapon);
                    DbInventoryItem ammo = GetAttackAmmo(action);

                    if (ammo != null)
                    {
                        switch ((ammo.SPD_ABS) & 0x3)
                        {
                            case 0:
                                damageCap *= 0.85;
                                break; // Blunt (light) -15%.
                            case 1:
                                break; // Bodkin (medium) 0%.
                            case 2:
                                damageCap *= 1.15;
                                break; // Doesn't exist on live.
                            case 3:
                                damageCap *= 1.25;
                                break; // Broadhead (X-heavy) +25%.
                        }
                    }
                }
                else if (weapon.Item_Type is Slot.TWOHAND)
                    damageCap *= CalculateTwoHandedDamageModifier(weapon);

                double damage = GamePlayer.ApplyWeaponQualityAndConditionToDamage(weapon, damageCap);
                damageCap *= 3;
                return damage;
            }
            else
            {
                double damage = (1.0 + owner.Level / Properties.PVE_MOB_DAMAGE_F1 + owner.Level * owner.Level / Properties.PVE_MOB_DAMAGE_F2) * NpcWeaponSpeed(weapon) * 0.1;

                if (owner is GameNPC npc)
                    damage *= npc.DamageFactor;

                if (weapon?.SlotPosition is Slot.TWOHAND or Slot.RANGED)
                    damage *= CalculateTwoHandedDamageModifier(weapon);

                damageCap = damage * 3;

                if (owner is GameEpicBoss)
                    damageCap *= Properties.SET_EPIC_ENCOUNTER_WEAPON_DAMAGE_CAP;

                return damage;
            }
        }

        public void RequestStartAttack(GameObject attackTarget = null)
        {
            _startAttackTarget = attackTarget ?? owner.TargetObject;
            StartAttackRequested = true;
            ServiceObjectStore.Add(this);
        }

        private void StartAttack()
        {
            if (owner is GamePlayer player)
            {
                if (!player.CharacterClass.StartAttack(_startAttackTarget))
                    return;

                if (!player.IsAlive)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.YouCantCombat"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                // Necromancer with summoned pet cannot attack.
                if (player.ControlledBrain?.Body is NecromancerPet)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.CantInShadeMode"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (player.IsStunned)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.CantAttackStunned"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (player.IsMezzed)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.CantAttackmesmerized"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                long vanishTimeout = player.TempProperties.GetProperty<long>(VanishEffect.VANISH_BLOCK_ATTACK_TIME_KEY);

                if (vanishTimeout > 0)
                {
                    if (vanishTimeout > GameLoop.GameLoopTime)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.YouMustWaitAgain", (vanishTimeout - GameLoop.GameLoopTime + 1000) / 1000), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    long changeTime = GameLoop.GameLoopTime - vanishTimeout;

                    if (changeTime < 30000)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.YouMustWait", ((30000 - changeTime) / 1000).ToString()), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }
                }

                if (player.IsOnHorse)
                    player.IsOnHorse = false;

                if (player.Steed is GameSiegeRam)
                {
                    player.Out.SendMessage("You can't attack while using a ram!", eChatType.CT_YouHit,eChatLoc.CL_SystemWindow);
                    return;
                }

                if (player.IsDisarmed)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.CantDisarmed"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (player.IsSitting)
                    player.Sit(false);

                DbInventoryItem attackWeapon = owner.ActiveWeapon;

                if (attackWeapon == null)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.CannotWithoutWeapon"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if ((eObjectType) attackWeapon.Object_Type is eObjectType.Instrument)
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.CannotMelee"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (player.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
                {
                    if (!Properties.ALLOW_OLD_ARCHERY)
                    {
                        if ((eCharacterClass) player.CharacterClass.ID is eCharacterClass.Scout or eCharacterClass.Hunter or eCharacterClass.Ranger)
                        {
                            // There is no feedback on live when attempting to fire a bow with arrows.
                            return;
                        }
                    }

                    // Check arrows for ranged attack.
                    if (player.rangeAttackComponent.UpdateAmmo(attackWeapon) == null)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.SelectQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    // Check if selected ammo is compatible for ranged attack.
                    if (!player.rangeAttackComponent.IsAmmoCompatible)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.CantUseQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    // This shouldn't be done here. Is this for safety?
                    if (player.effectListComponent.ContainsEffectForEffectType(eEffect.SureShot))
                        player.rangeAttackComponent.RangedAttackType = eRangedAttackType.SureShot;
                    else if (player.effectListComponent.ContainsEffectForEffectType(eEffect.RapidFire))
                        player.rangeAttackComponent.RangedAttackType = eRangedAttackType.RapidFire;
                    else if (player.effectListComponent.ContainsEffectForEffectType(eEffect.TrueShot))
                        player.rangeAttackComponent.RangedAttackType = eRangedAttackType.Long;

                    if (player.rangeAttackComponent?.RangedAttackType is eRangedAttackType.Critical &&
                        player.Endurance < RangeAttackComponent.CRITICAL_SHOT_ENDURANCE_COST)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.TiredShot"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (player.Endurance < RangeAttackComponent.DEFAULT_ENDURANCE_COST)
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.TiredUse", attackWeapon.Name), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (player.IsStealthed)
                    {
                        // -Chance to unstealth while nocking an arrow = stealth spec / level
                        // -Chance to unstealth nocking a crit = stealth / level  0.20
                        int stealthSpec = player.GetModifiedSpecLevel(Specs.Stealth);
                        int stayStealthed = stealthSpec * 100 / player.Level;

                        if (player.rangeAttackComponent?.RangedAttackType is eRangedAttackType.Critical)
                            stayStealthed -= 20;

                        if (!Util.Chance(stayStealthed))
                            player.Stealth(false);
                    }
                }
                else
                {
                    if (_startAttackTarget == null)
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.CombatNoTarget"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    else
                    {
                        if (_startAttackTarget is GameNPC npcTarget)
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.CombatTarget", _startAttackTarget.GetName(0, false, player.Client.Account.Language, npcTarget)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        else
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.CombatTarget", _startAttackTarget.GetName(0, false)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);

                        // Unstealth right after entering combat mode if anything is targeted.
                        // A timer is used to allow any potential opener to be executed.
                        if (player.IsStealthed)
                            new ECSGameTimer(player, Unstealth, 1);

                        static int Unstealth(ECSGameTimer timer)
                        {
                            (timer.Owner as GamePlayer).Stealth(false);
                            return 0;
                        }
                    }
                }

                /*
                if (p.CharacterClass is PlayerClass.ClassVampiir)
                {
                    GameSpellEffect removeEffect = SpellHandler.FindEffectOnTarget(p, "VampiirSpeedEnhancement");
                    if (removeEffect != null)
                        removeEffect.Cancel(false);
                }
                else
                {
                    // Bard RR5 ability must drop when the player starts a melee attack
                    IGameEffect DreamweaverRR5 = p.EffectList.GetOfType<DreamweaverEffect>();
                    if (DreamweaverRR5 != null)
                        DreamweaverRR5.Cancel(false);
                }*/

                bool oldAttackState = AttackState;

                if (LivingStartAttack())
                {
                    if (player.castingComponent.SpellHandler?.Spell.Uninterruptible == false)
                    {
                        player.StopCurrentSpellcast();
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.SpellCancelled"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    }

                    if (player.ActiveWeaponSlot is not eActiveWeaponSlot.Distance)
                    {
                        if (oldAttackState != AttackState)
                            player.Out.SendAttackMode(AttackState);
                    }
                    else
                    {
                        string typeMsg = (eObjectType) attackWeapon.Object_Type is eObjectType.Thrown ? "throw" : "shot";
                        string targetMsg;

                        if (_startAttackTarget != null)
                        {
                            targetMsg = player.IsWithinRadius(_startAttackTarget, AttackRange)
                                ? LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.TargetInRange")
                                : LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.TargetOutOfRange");
                        }
                        else
                            targetMsg = string.Empty;

                        int speed = AttackSpeed(attackWeapon) / 100;

                        if (!player.effectListComponent.ContainsEffectForEffectType(eEffect.Volley))
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.StartAttack.YouPrepare", typeMsg, speed / 10, speed % 10, targetMsg), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    }
                }
            }
            else if (owner is GameNPC)
                NpcStartAttack();
            else
                LivingStartAttack();
        }

        private bool LivingStartAttack()
        {
            if (owner.IsIncapacitated)
                return false;

            if (owner.IsEngaging)
                owner.CancelEngageEffect();

            if (owner.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
            {
                if (owner.rangeAttackComponent.RangedAttackState is not eRangedAttackState.Aim && attackAction.CheckInterruptTimer())
                    return false;

                owner.rangeAttackComponent.AttackStartTime = GameLoop.GameLoopTime;
            }

            AttackState = true;
            return true;
        }

        private void NpcStartAttack()
        {
            GameNPC npc = owner as GameNPC;
            npc.FireAmbientSentence(GameNPC.eAmbientTrigger.fighting, _startAttackTarget);

            if (npc.Brain is IControlledBrain brain)
            {
                if (brain.AggressionState is eAggressionState.Passive)
                    return;
            }

            // NPCs aren't allowed to prepare their ranged attack while moving or out of range.
            // If we have a running `AttackAction`, let it decide what to do. Not every NPC should start following their target and this allows us to react faster.
            if (npc.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
            {
                if (!npc.IsWithinRadius(_startAttackTarget, AttackRange - 30))
                {
                    if (attackAction == null || !attackAction.OnOutOfRangeOrNoLosRangedAttack())
                    {
                        // Default behavior. If `AttackAction` doesn't handle it, tell the NPC to get closer to its target.
                        StopAttack();
                        npc.Follow(_startAttackTarget, npc.StickMinimumRange, npc.StickMaximumRange);
                    }

                    return;
                }

                if (npc.IsMoving)
                    npc.StopMoving();
            }

            if (LivingStartAttack())
            {
                npc.TargetObject = _startAttackTarget;

                if (_startAttackTarget != npc.FollowTarget)
                {
                    if (npc.IsMoving)
                        npc.StopMoving();

                    if (npc.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
                        npc.TurnTo(_startAttackTarget);

                    npc.Follow(_startAttackTarget, npc.StickMinimumRange, npc.StickMaximumRange);
                }
            }
            else if (npc.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
                npc.TurnTo(_startAttackTarget);
        }

        public void StopAttack()
        {
            StartAttackRequested = false;

            if (owner.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
            {
                if (AttackState)
                {
                    // Only cancel the animation if the ranged ammo isn't released already and we aren't preparing another shot.
                    // If `weaponAction` is null, no attack was performed yet.
                    // If `weaponAction.ActiveWeaponSlot` isn't `eActiveWeaponSlot.Distance`, the instance is outdated.
                    if (weaponAction == null || weaponAction.ActiveWeaponSlot is not eActiveWeaponSlot.Distance || weaponAction.HasAmmoReachedTarget)
                    {
                        foreach (GamePlayer player in owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                            player.Out.SendInterruptAnimation(owner);
                    }
                }

                if (owner.effectListComponent.ContainsEffectForEffectType(eEffect.TrueShot))
                    EffectListService.GetEffectOnTarget(owner, eEffect.TrueShot).Stop();
            }

            attackAction.OnStopAttack();
            bool oldAttackState = AttackState;
            AttackState = false;
            owner.CancelEngageEffect();
            owner.styleComponent.NextCombatStyle = null;
            owner.styleComponent.NextCombatBackupStyle = null;

            if (owner is GamePlayer playerOwner)
            {
                if (playerOwner.IsAlive && oldAttackState)
                    playerOwner.Out.SendAttackMode(AttackState);
            }
            else if (owner is GameNPC npcOwner)
            {
                // Force NPCs to switch back to their ranged weapon if they have any and their aggro list is empty.
                if (npcOwner.Inventory?.GetItem(eInventorySlot.DistanceWeapon) != null &&
                    npcOwner.ActiveWeaponSlot is not eActiveWeaponSlot.Distance &&
                    npcOwner.Brain is StandardMobBrain brain &&
                    !brain.HasAggro)
                {
                    npcOwner.SwitchWeapon(eActiveWeaponSlot.Distance);
                }
            }
        }

        /// <summary>
        /// Called whenever a single attack strike is made
        /// </summary>
        public AttackData MakeAttack(WeaponAction action, GameObject target, DbInventoryItem weapon, Style style, double effectiveness, int interval, bool dualWield)
        {
            if (owner is GamePlayer playerOwner)
            {
                if (playerOwner.IsCrafting)
                {
                    playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    playerOwner.craftComponent.StopCraft();
                    playerOwner.CraftTimer = null;
                    playerOwner.Out.SendCloseTimerWindow();
                }

                if (playerOwner.IsSalvagingOrRepairing)
                {
                    playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    playerOwner.CraftTimer.Stop();
                    playerOwner.CraftTimer = null;
                    playerOwner.Out.SendCloseTimerWindow();
                }

                AttackData ad = LivingMakeAttack(action, target, weapon, style, effectiveness * playerOwner.Effectiveness, interval, dualWield);

                switch (ad.AttackResult)
                {
                    case eAttackResult.HitStyle:
                    case eAttackResult.HitUnstyled:
                    {
                        // Keep component.
                        if ((ad.Target is GameKeepComponent || ad.Target is GameKeepDoor || ad.Target is GameSiegeWeapon) &&
                            ad.Attacker is GamePlayer && ad.Attacker.GetModified(eProperty.KeepDamage) > 0)
                        {
                            int keepdamage = (int) Math.Floor(ad.Damage * ((double) ad.Attacker.GetModified(eProperty.KeepDamage) / 100));
                            int keepstyle = (int) Math.Floor(ad.StyleDamage * ((double) ad.Attacker.GetModified(eProperty.KeepDamage) / 100));
                            ad.Damage += keepdamage;
                            ad.StyleDamage += keepstyle;
                        }

                        // Vampiir.
                        if (playerOwner.CharacterClass is PlayerClass.ClassVampiir &&
                            target is not GameKeepComponent and not GameKeepDoor and not GameSiegeWeapon)
                        {
                            int perc = Convert.ToInt32((double) (ad.Damage + ad.CriticalDamage) / 100 * (55 - playerOwner.Level));
                            perc = (perc < 1) ? 1 : ((perc > 15) ? 15 : perc);
                            playerOwner.Mana += Convert.ToInt32(Math.Ceiling((decimal) (perc * playerOwner.MaxMana) / 100));
                        }

                        break;
                    }
                }

                switch (ad.AttackResult)
                {
                    case eAttackResult.Blocked:
                    case eAttackResult.Fumbled:
                    case eAttackResult.HitStyle:
                    case eAttackResult.HitUnstyled:
                    case eAttackResult.Missed:
                    case eAttackResult.Parried:
                    {
                        // Condition percent can reach 70%.
                        // Durability percent can reach 0%.

                        if (weapon is GameInventoryItem weaponItem)
                            weaponItem.OnStrikeTarget(playerOwner, target);

                        // Camouflage will be disabled only when attacking a GamePlayer or ControlledNPC of a GamePlayer.
                        // It should also be disabled even if Camouflage isn't active.
                        if (playerOwner.HasAbility(Abilities.Camouflage))
                        {
                            if (target is GamePlayer || (target is GameNPC targetNpc && targetNpc.Brain is IControlledBrain targetNpcBrain && targetNpcBrain.GetPlayerOwner() != null))
                                playerOwner.DisableSkill(SkillBase.GetAbility(Abilities.Camouflage), CamouflageSpecHandler.DISABLE_DURATION);
                        }

                        // Multiple Hit check.
                        if (ad.AttackResult is eAttackResult.HitStyle)
                        {
                            List<GameObject> extraTargets = new();
                            List<GameObject> listAvailableTargets = new();
                            DbInventoryItem attackWeapon = owner.ActiveWeapon;
                            DbInventoryItem leftWeapon = owner.ActiveLeftWeapon;

                            bool IsShieldSwipe = style.ID == 600;
                            int numTargetsCanHit;

                            // Tribal Assault: Hits 1 extra target.
                            // Clan's Might: Hits 1 extra target.
                            // Totemic Wrath: Hits 2 extra target.
                            // Totemic Sacrifice: Hits 3 extra target.
                            // Shield Swipe: No cap?
                            if (IsShieldSwipe)
                                numTargetsCanHit = 255;
                            else
                            {
                                StyleProcInfo styleProcInfo = style.Procs.Where(x => x.Spell.SpellType is eSpellType.MultiTarget).FirstOrDefault();
                                numTargetsCanHit = styleProcInfo == null ? 0 : (int) styleProcInfo.Spell.Value;
                            }

                            if (numTargetsCanHit <= 0)
                                break;

                            // This implementation of Shield Swipe doesn't affect players.
                            if (!IsShieldSwipe)
                            {
                                foreach (GamePlayer playerInRange in owner.GetPlayersInRadius((ushort) AttackRange))
                                {
                                    if (GameServer.ServerRules.IsAllowedToAttack(owner, playerInRange, true))
                                        listAvailableTargets.Add(playerInRange);
                                }
                            }

                            foreach (GameNPC npcInRange in owner.GetNPCsInRadius((ushort) AttackRange))
                            {
                                if (GameServer.ServerRules.IsAllowedToAttack(owner, npcInRange, true))
                                    listAvailableTargets.Add(npcInRange);
                            }

                            // Remove primary target.
                            listAvailableTargets.Remove(target);

                            if (numTargetsCanHit >= listAvailableTargets.Count)
                                extraTargets = listAvailableTargets;
                            else
                            {
                                int index;
                                GameObject availableTarget;

                                for (int i = numTargetsCanHit; i > 0; i--)
                                {
                                    index = Util.Random(listAvailableTargets.Count - 1);
                                    availableTarget = listAvailableTargets[index];
                                    listAvailableTargets.SwapRemoveAt(index);
                                    extraTargets.Add(availableTarget);
                                }
                            }

                            foreach (GameObject extraTarget in extraTargets)
                            {
                                // Damage bonus against sitting targets is normally set by `AttackAction`.
                                if (extraTarget is GamePlayer player && player.IsSitting)
                                    effectiveness *= 2;

                                weaponAction = new WeaponAction(playerOwner, extraTarget, attackWeapon, leftWeapon, effectiveness, AttackSpeed(attackWeapon), null);
                                weaponAction.Execute();
                            }
                        }

                        break;
                    }
                }

                return ad;
            }
            else
            {
                if (owner is NecromancerPet necromancerPet)
                    ((NecromancerPetBrain)necromancerPet.Brain).CheckAttackSpellQueue();
                else
                    effectiveness = 1;

                return LivingMakeAttack(action, target, weapon, style, effectiveness, interval, dualWield);
            }
        }

        /// <summary>
        /// This method is called to make an attack, it is called from the
        /// attacktimer and should not be called manually
        /// </summary>
        /// <returns>the object where we collect and modifiy all parameters about the attack</returns>
        public AttackData LivingMakeAttack(WeaponAction action, GameObject target, DbInventoryItem weapon, Style style, double effectiveness, int interval, bool dualWield, bool ignoreLOS = false)
        {
            AttackData ad = new()
            {
                Attacker = owner,
                Target = target as GameLiving,
                Style = style,
                DamageType = AttackDamageType(weapon, action),
                Weapon = weapon,
                Interval = interval,
                IsOffHand = weapon != null && weapon.SlotPosition is Slot.LEFTHAND
            };

            int attackRange = AttackRange;

            if (style != null)
            {
                StyleProcInfo styleProcInfo = style?.Procs.Where(x => x.Spell.SpellType is eSpellType.StyleRange).FirstOrDefault();

                if (styleProcInfo != null)
                    attackRange = (int) styleProcInfo.Spell.Value; // Fixed range for some reason, don't add to attack range.
            }

            ad.AttackType = AttackData.GetAttackType(weapon, dualWield, ad.Attacker);

            // No target.
            if (ad.Target == null)
            {
                ad.AttackResult = (target == null) ? eAttackResult.NoTarget : eAttackResult.NoValidTarget;
                SendAttackingCombatMessages(action, ad);
                return ad;
            }

            // Region / state check.
            if (ad.Target.CurrentRegionID != owner.CurrentRegionID || ad.Target.ObjectState is not eObjectState.Active)
            {
                ad.AttackResult = eAttackResult.NoValidTarget;
                SendAttackingCombatMessages(action, ad);
                return ad;
            }

            // LoS / in front check.
            if (!ignoreLOS && ad.AttackType is not AttackData.eAttackType.Ranged && owner is GamePlayer &&
                ad.Target is not GameKeepComponent &&
                !(owner.IsObjectInFront(ad.Target, 120) && owner.TargetInView))
            {
                ad.AttackResult = eAttackResult.TargetNotVisible;
                SendAttackingCombatMessages(action, ad);
                return ad;
            }

            // Target is already dead.
            if (!ad.Target.IsAlive)
            {
                ad.AttackResult = eAttackResult.TargetDead;
                SendAttackingCombatMessages(action, ad);
                return ad;
            }

            // Melee range check (ranged is already done at this point).
            if (ad.AttackType is not AttackData.eAttackType.Ranged)
            {
                if (!owner.IsWithinRadius(ad.Target, attackRange))
                {
                    ad.AttackResult = eAttackResult.OutOfRange;
                    SendAttackingCombatMessages(action, ad);
                    return ad;
                }
            }

            if (!GameServer.ServerRules.IsAllowedToAttack(ad.Attacker, ad.Target, GameLoop.GameLoopTime - attackAction.RoundWithNoAttackTime <= 1500))
            {
                ad.AttackResult = eAttackResult.NotAllowed_ServerRules;
                SendAttackingCombatMessages(action, ad);
                return ad;
            }

            if (ad.Target.IsSitting)
                effectiveness *= 2;

            // Apply Mentalist RA5L.
            SelectiveBlindnessEffect SelectiveBlindness = owner.EffectList.GetOfType<SelectiveBlindnessEffect>();
            if (SelectiveBlindness != null)
            {
                GameLiving EffectOwner = SelectiveBlindness.EffectSource;
                if (EffectOwner == ad.Target)
                {
                    if (owner is GamePlayer)
                        ((GamePlayer) owner).Out.SendMessage(
                            string.Format(
                                LanguageMgr.GetTranslation(((GamePlayer) owner).Client.Account.Language,
                                    "GameLiving.AttackData.InvisibleToYou"), ad.Target.GetName(0, true)),
                            eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                    ad.AttackResult = eAttackResult.NoValidTarget;
                    SendAttackingCombatMessages(action, ad);
                    return ad;
                }
            }

            // DamageImmunity Ability.
            if ((GameLiving) target != null && ((GameLiving) target).HasAbility(Abilities.DamageImmunity))
            {
                //if (ad.Attacker is GamePlayer) ((GamePlayer)ad.Attacker).Out.SendMessage(string.Format("{0} can't be attacked!", ad.Target.GetName(0, true)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                ad.AttackResult = eAttackResult.NoValidTarget;
                SendAttackingCombatMessages(action, ad);
                return ad;
            }

            // Add ourselves to the target's attackers list before going further.
            ad.Target.attackComponent.AddAttacker(ad);

            // Calculate our attack result and attack damage.
            ad.AttackResult = ad.Target.attackComponent.CalculateEnemyAttackResult(action, ad, weapon, ref effectiveness);

            GamePlayer playerOwner = owner as GamePlayer;

            // Strafing miss.
            if (playerOwner != null && playerOwner.IsStrafing && ad.IsMeleeAttack && ad.Target is GamePlayer && Util.Chance(30))
            {
                // Used to tell the difference between a normal miss and a strafing miss.
                // Ugly, but we shouldn't add a new field to 'AttackData' just for that purpose.
                ad.MissChance = 0;
                ad.AttackResult = eAttackResult.Missed;
            }

            switch (ad.AttackResult)
            {
                // Calculate damage only if we hit the target.
                case eAttackResult.HitUnstyled:
                case eAttackResult.HitStyle:
                {
                    double damage = AttackDamage(weapon, action, out double baseDamageCap);
                    DbInventoryItem armor = null;

                    if (ad.Target.Inventory != null)
                        armor = ad.Target.Inventory.GetItem((eInventorySlot) ad.ArmorHitLocation);

                    double weaponSkill = CalculateWeaponSkill(weapon, ad.Target, out int spec, out (double, double) varianceRange, out double specModifier, out double baseWeaponSkill);
                    double armorMod = CalculateTargetArmor(ad.Target, ad.ArmorHitLocation, out double armorFactor, out double absorb);
                    double damageMod = weaponSkill / armorMod;

                    // Badge Of Valor Calculation 1+ absorb or 1- absorb
                    // if (ad.Attacker.EffectList.GetOfType<BadgeOfValorEffect>() != null)
                    //     damage *= 1.0 + Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
                    // else
                    //     damage *= 1.0 - Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));

                    if (ad.IsOffHand)
                        damage *= 1 + owner.GetModified(eProperty.OffhandDamage) * 0.01;

                    // If the target is another player's pet, shouldn't 'PVP_MELEE_DAMAGE' be used?
                    if (owner is GamePlayer || (owner is GameNPC npcOwner && npcOwner.Brain is IControlledBrain && owner.Realm != 0))
                    {
                        if (target is GamePlayer)
                            damage *= Properties.PVP_MELEE_DAMAGE;
                        else if (target is GameNPC)
                            damage *= Properties.PVE_MELEE_DAMAGE;
                    }

                    damage *= damageMod;

                    // Melee damage and style damage ToA bonuses are pretty weird.
                    // * They're both calculated from base damage.
                    // * They effectively add the same amount of damage when used independently.
                    // * Style damage bonus works even on styles that have no growth rate.
                    // * However, the first one will be added to base damage, and the second one to style damage. This is really just for display purposes.
                    // * They stack multiplicatively. Assuming a GR of 0, two 10% bonuses result in the attack doing 21% more damage.
                    // * The higher the GR, the lower their contribution to total damage is (since GR is actually ignored).

                    effectiveness *= CalculateEffectiveness(weapon); // Augment the passed effectiveness with the weapon's effectiveness (ToA bonuses, etc.).
                    double preEffectivenessDamage = damage; // Damage snapshot before applying effectiveness, to be used to calculate style damage.
                    double preEffectivenessBaseDamageCap = baseDamageCap; // Damage cap snapshot before applying effectiveness, to be used to calculate style damage.
                    damage *= effectiveness;
                    baseDamageCap *= effectiveness;

                    double conversionMod = CalculateTargetConversion(ad.Target);
                    double primarySecondaryResistMod = CalculateTargetResistance(ad.Target, ad.DamageType, armor);
                    double primarySecondaryResistConversionMod = primarySecondaryResistMod * conversionMod;

                    // This makes capped unstyled hits have weird modifiers, and no longer match the actual damage reduction from resistances; for example 150 (-1432) against a naked target.
                    // But inaccurate modifiers when the cap is hit appears to be live like.
                    double preResistDamage = damage; // Pre resist damage snapshot in case we need to add style damage bonus.
                    double modifier = Math.Min(baseDamageCap, preResistDamage * primarySecondaryResistConversionMod) - damage;
                    damage += modifier;

                    // Outside the style execution block because we need it for the detailed combat log.
                    double styleDamageCap = 0;

                    if (style != null)
                    {
                        if (StyleProcessor.ExecuteStyle(owner, ad.Target, ad.Style, weapon, preEffectivenessDamage, preEffectivenessBaseDamageCap, ad.ArmorHitLocation, ad.StyleEffects, out double styleDamage, out styleDamageCap, out int animationId))
                        {
                            double styleDamageBonus = preResistDamage * owner.GetModified(eProperty.StyleDamage) * 0.01;
                            styleDamage += styleDamageBonus;
                            styleDamageCap += styleDamageBonus;

                            double preResistStyleDamage = styleDamage;
                            ad.StyleDamage = (int) preResistStyleDamage; // We show uncapped and unmodified by resistances style damage. This should only be used by the combat log.
                            // We have to calculate damage reduction again because `ExecuteStyle` works with pre resist base damage. Static growth styles also don't use it.
                            styleDamage = preResistStyleDamage * primarySecondaryResistConversionMod;

                            if (styleDamageCap > 0)
                                styleDamage = Math.Min(styleDamageCap, styleDamage);

                            damage += styleDamage;
                            modifier += styleDamage - preResistStyleDamage;
                            ad.AttackResult = eAttackResult.HitStyle;
                        }

                        // Play the style animation even on imperfect execution.
                        ad.AnimationId = animationId;
                    }

                    ad.Damage = (int) damage;
                    ad.Modifier = (int) Math.Floor(modifier);
                    ad.CriticalChance = CalculateCriticalChance(action);
                    ad.CriticalDamage = CalculateCriticalDamage(ad);

                    if (conversionMod < 1)
                    {
                        double conversionAmount = conversionMod > 0 ? damage / conversionMod - damage : damage;
                        ApplyTargetConversionRegen(ad.Target, (int) conversionAmount);
                    }

                    if (playerOwner != null && playerOwner.UseDetailedCombatLog)
                        PrintDetailedCombatLog(playerOwner, armorFactor, absorb, armorMod, baseWeaponSkill, varianceRange, specModifier, weaponSkill, damageMod, baseDamageCap, styleDamageCap);

                    if (target is GamePlayer targetPlayer && targetPlayer.UseDetailedCombatLog)
                        PrintDetailedCombatLog(targetPlayer, armorFactor, absorb, armorMod, baseWeaponSkill, varianceRange, specModifier, weaponSkill, damageMod, baseDamageCap, styleDamageCap);

                    break;
                }
                case eAttackResult.Blocked:
                case eAttackResult.Evaded:
                case eAttackResult.Parried:
                case eAttackResult.Missed:
                {
                    // Reduce endurance by half the style's cost if we missed.
                    if (ad.Style != null && playerOwner != null && weapon != null)
                        playerOwner.Endurance -= StyleProcessor.CalculateEnduranceCost(playerOwner, ad.Style, weapon.SPD_ABS) / 2;

                    break;
                }

                static void PrintDetailedCombatLog(GamePlayer player, double armorFactor, double absorb, double armorMod, double baseWeaponSkill, (double lowerLimit, double upperLimit) varianceRange, double specModifier, double weaponSkill, double damageMod, double baseDamageCap, double styleDamageCap)
                {
                    StringBuilder stringBuilder = new();
                    stringBuilder.Append($"BaseWS: {baseWeaponSkill:0.##} | SpecMod: {specModifier:0.##} ({varianceRange.lowerLimit:0.00}~{varianceRange.upperLimit:0.00}) | WS: {weaponSkill:0.##}\n");
                    stringBuilder.Append($"AF: {armorFactor:0.##} | ABS: {absorb * 100:0.##}% | AF/ABS: {armorMod:0.##}\n");
                    stringBuilder.Append($"DamageMod: {damageMod:0.##} | BaseDamageCap: {baseDamageCap:0.##}");

                    if (styleDamageCap > 0)
                        stringBuilder.Append($" | StyleDamageCap: {styleDamageCap:0.##}");

                    player.Out.SendMessage(stringBuilder.ToString(), eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                }
            }

            // Attacked living may modify the attack data. Primarily used for keep doors and components.
            ad.Target.ModifyAttack(ad);

            SendAttackingCombatMessages(action, ad);
            SendDefendingCombatMessages(ad);
            BroadcastObserverMessage(ad);

            #region Prevent Flight

            if (ad.Attacker is GamePlayer)
            {
                GamePlayer attacker = ad.Attacker as GamePlayer;
                if (attacker.HasAbilityType(typeof(AtlasOF_PreventFlight)) && Util.Chance(35))
                {
                    if (owner.IsObjectInFront(ad.Target, 120) && ad.Target.IsMoving)
                    {
                        bool preCheck = false;
                        float angle = ad.Target.GetAngle(ad.Attacker);
                        if (angle >= 150 && angle < 210) preCheck = true;

                        if (preCheck)
                        {
                            Spell spell = SkillBase.GetSpellByID(7083);
                            if (spell != null)
                            {
                                ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(owner, spell,
                                    SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                                if (spellHandler != null)
                                {
                                    spellHandler.StartSpell(ad.Target);
                                }
                            }
                        }
                    }
                }
            }

            #endregion

            // Interrupt the target of the attack.
            ad.Target.StartInterruptTimer(interval, ad.AttackType, ad.Attacker);

            // If we're attacking via melee, start an interrupt timer on ourselves so we cannot swing + immediately cast.
            if (ad.IsMeleeAttack)
                owner.StartInterruptTimer(owner.SelfInterruptDurationOnMeleeAttack, ad.AttackType, ad.Attacker);

            // Handles CC breaks, ablatives...
            owner.OnAttackEnemy(ad);
            return ad;
        }

        public double CalculateEffectiveness(DbInventoryItem weapon)
        {
            double effectiveness = 100;

            if (weapon == null || weapon.Item_Type is Slot.RIGHTHAND or Slot.LEFTHAND or Slot.TWOHAND)
                effectiveness += owner.GetModified(eProperty.MeleeDamage);
            else if (weapon.Item_Type is Slot.RANGED)
            {
                effectiveness += owner.GetModified(eProperty.RangedDamage);

                if ((eObjectType) weapon.Object_Type is eObjectType.Longbow or eObjectType.RecurvedBow or eObjectType.CompositeBow)
                {
                    if (!Properties.ALLOW_OLD_ARCHERY)
                        effectiveness += owner.GetModified(eProperty.SpellDamage);
                }
            }

            return effectiveness * 0.01;
        }

        public double CalculateWeaponSkill(DbInventoryItem weapon, GameLiving target, out int spec, out (double, double) varianceRange, out double specModifier, out double baseWeaponSkill)
        {
            spec = CalculateSpec(weapon);
            specModifier = CalculateSpecModifier(target, spec, out varianceRange);
            return CalculateWeaponSkill(weapon, specModifier, out baseWeaponSkill);
        }

        public double CalculateWeaponSkill(DbInventoryItem weapon, double specModifier, out double baseWeaponSkill)
        {
            baseWeaponSkill = owner.GetWeaponSkill(weapon) + INHERENT_WEAPON_SKILL;
            double relicBonus = 1.0;

            if (owner is GamePlayer)
                relicBonus += RelicMgr.GetRelicBonusModifier(owner.Realm, eRelicType.Strength);

            return baseWeaponSkill * relicBonus * specModifier;
        }

        public double CalculateDefensePenetration(DbInventoryItem weapon, int targetLevel)
        {
            int levelDifference = (owner is GamePlayer ? owner.WeaponSpecLevel(weapon) : owner.Level) - targetLevel;
            double specModifier = 1 + levelDifference * 0.01;
            return CalculateWeaponSkill(weapon, specModifier, out _) * 0.08 / 100;
        }

        public int CalculateSpec(DbInventoryItem weapon)
        {
            if (weapon == null)
                return 0;

            eObjectType objectType = (eObjectType) weapon.Object_Type;
            int slotPosition = weapon.SlotPosition;

            if (owner is GamePlayer && owner.Realm is eRealm.Albion && Properties.ENABLE_ALBION_ADVANCED_WEAPON_SPEC &&
                (GameServer.ServerRules.IsObjectTypesEqual((eObjectType) weapon.Object_Type, eObjectType.TwoHandedWeapon) ||
                GameServer.ServerRules.IsObjectTypesEqual((eObjectType) weapon.Object_Type, eObjectType.PolearmWeapon)))
            {
                // Albion dual spec penalty, which sets minimum damage to the base damage spec.
                if ((eDamageType) weapon.Type_Damage is eDamageType.Crush)
                    objectType = eObjectType.CrushingWeapon;
                else if ((eDamageType) weapon.Type_Damage is eDamageType.Slash)
                    objectType = eObjectType.SlashingWeapon;
                else
                    objectType = eObjectType.ThrustWeapon;
            }

            return owner.WeaponSpecLevel(objectType, slotPosition);
        }

        public (double, double) CalculateVarianceRange(GameLiving target, int spec)
        {
            (double lowerLimit, double upperLimit) varianceRange;

            if (owner is GamePlayer playerOwner)
            {
                if (playerOwner.SpecLock > 0)
                    return (playerOwner.SpecLock, playerOwner.SpecLock);

                // Characters below level 5 get a bonus to their spec to help with the very wide variance at this level range.
                // Target level, lower bound at 2, lower bound at 1:
                // 0 | 1      | 0.25
                // 1 | 0.625  | 0.25
                // 2 | 0.5    | 0.25
                // 3 | 0.4375 | 0.25
                // 4 | 0.4    | 0.25
                // 5 | 0.375  | 0.25
                // Absolute minimum spec is set to 1 to prevent an issue where the lower bound (with staffs for example) would slightly rise with the target's level.
                // Also prevents negative values.
                spec = Math.Max(owner.Level < 5 ? 2 : 1, spec);
                double specVsTargetLevelMod = (spec - 1) / ((double) target.Level + 1);
                varianceRange = (Math.Min(0.75 * specVsTargetLevelMod + 0.25, 1.0), Math.Min(Math.Max(1.25 + (3.0 * specVsTargetLevelMod - 2) * 0.25, 1.25), 1.5));
            }
            else
                varianceRange = (0.9, 1.1);

            return varianceRange;
        }

        public double CalculateSpecModifier(GameLiving target, int spec, out (double lowerLimit, double upperLimit) varianceRange)
        {
            varianceRange = CalculateVarianceRange(target, spec);
            double difference = varianceRange.upperLimit - varianceRange.lowerLimit;
            return varianceRange.lowerLimit + Util.RandomDoubleIncl() * difference;
        }

        public static double CalculateTargetArmor(GameLiving target, eArmorSlot armorSlot, out double armorFactor, out double absorb)
        {
            armorFactor = target.GetArmorAF(armorSlot) + INHERENT_ARMOR_FACTOR;

            // Gives an extra 0.4~20 bonus AF to players. Ideally this should be done in `ArmorFactorCalculator`.
            if (target is GamePlayer or GameTrainingDummy)
                armorFactor += target.Level * 20 / 50.0;

            absorb = target.GetArmorAbsorb(armorSlot);
            return absorb >= 1 ? double.MaxValue : armorFactor / (1 - absorb);
        }

        public static double CalculateTargetResistance(GameLiving target, eDamageType damageType, DbInventoryItem armor)
        {
            double damageModifier = 1.0;
            damageModifier *= 1.0 - (target.GetResist(damageType) + SkillBase.GetArmorResist(armor, damageType)) * 0.01;
            return damageModifier;
        }

        public static double CalculateTargetConversion(GameLiving target)
        {
            if (target is not GamePlayer)
                return 1.0;

            double conversionMod = 1 - target.GetModified(eProperty.Conversion) / 100.0;
            return Math.Min(1.0, conversionMod);
        }

        public static void ApplyTargetConversionRegen(GameLiving target, int conversionAmount)
        {
            if (target is not GamePlayer playerTarget)
                return;

            int powerConversion = conversionAmount;
            int enduranceConversion = conversionAmount;

            if (target.Mana + conversionAmount > target.MaxMana)
                powerConversion = target.MaxMana - target.Mana;

            if (target.Endurance + conversionAmount > target.MaxEndurance)
                enduranceConversion = target.MaxEndurance - target.Endurance;

            if (powerConversion > 0)
                playerTarget.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(playerTarget.Client.Account.Language, "GameLiving.AttackData.GainPowerPoints"), powerConversion), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

            if (enduranceConversion > 0)
                playerTarget.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(playerTarget.Client.Account.Language, "GameLiving.AttackData.GainEndurancePoints"), enduranceConversion), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);

            target.Mana = Math.Min(target.MaxMana, target.Mana + powerConversion);
            target.Endurance = Math.Min(target.MaxEndurance, target.Endurance + enduranceConversion);
        }

        public virtual bool CheckBlock(AttackData ad)
        {
            double blockChance = owner.TryBlock(ad, out int shieldSize);
            ad.BlockChance = blockChance * 100;
            double blockRoll;

            if (blockChance > 0)
            {
                if (!Properties.OVERRIDE_DECK_RNG && owner is GamePlayer player)
                    blockRoll = player.RandomDeck.GetPseudoDouble();
                else
                    blockRoll = Util.RandomDouble();

                if (ad.Attacker is GamePlayer attacker && attacker.UseDetailedCombatLog)
                    attacker.Out.SendMessage($"target block%: {blockChance * 100:0.##} rand: {blockRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                if (ad.Target is GamePlayer defender && defender.UseDetailedCombatLog)
                    defender.Out.SendMessage($"your block%: {blockChance * 100:0.##} rand: {blockRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                // The order here matters a lot. Either we consume attempts (by calling `Consume` first`) or blocks (by checking the roll first; the current implementation).
                // If we consume attempts, the effective block rate changes according to this formula: "if (shieldSize < attackerCount) blockChance *= (shieldSize / attackerCount)".
                // If we consume blocks, then the reduction is lower the lower the base block chance, and identical with a theoretical 100% block chance.
                if (blockChance > blockRoll && _blockRoundHandler.Consume(shieldSize, ad))
                    return true;
            }

            if (ad.AttackType is AttackData.eAttackType.Ranged or AttackData.eAttackType.Spell)
            {
                // Nature's shield, 100% block chance, 120° frontal angle.
                StyleProcInfo styleProcInfo =
                    owner.styleComponent.NextCombatStyle?.Procs.Where(x => x.Spell.SpellType is eSpellType.NaturesShield).FirstOrDefault() ??
                    owner.styleComponent.NextCombatBackupStyle?.Procs.Where(x => x.Spell.SpellType is eSpellType.NaturesShield).FirstOrDefault();

                if (styleProcInfo != null && owner.IsObjectInFront(ad.Attacker, 120))
                {
                    ad.BlockChance = styleProcInfo.Spell.Value;
                    return true;
                }
            }

            return false;
        }

        public bool CheckGuard(AttackData ad, bool stealthStyle)
        {
            foreach (GuardECSGameEffect guard in owner.effectListComponent.GetAbilityEffects(eEffect.Guard))
            {
                if (guard.Target != owner)
                    continue;

                GameLiving source = guard.Source;

                if (source == null ||
                    source.IsCrowdControlled ||
                    source.IsSitting ||
                    source.IsCasting ||
                    source.IsIncapacitated ||
                    source.ActiveWeaponSlot is eActiveWeaponSlot.Distance ||
                    stealthStyle ||
                    !guard.Source.IsObjectInFront(ad.Attacker, 180) ||
                    !guard.Source.IsWithinRadius(guard.Target, GuardAbilityHandler.GUARD_DISTANCE))
                {
                    continue;
                }

                DbInventoryItem rightHand = source.ActiveWeapon;
                DbInventoryItem leftHand = source.ActiveLeftWeapon;

                if (((rightHand != null && rightHand.Hand == 1) || leftHand == null || (eObjectType) leftHand.Object_Type is not eObjectType.Shield) && source is not GameNPC)
                    continue;

                double guardChance;

                if (source is GameNPC)
                    guardChance = source.GetModified(eProperty.BlockChance);
                else
                    guardChance = source.GetModified(eProperty.BlockChance) * (leftHand.Quality * 0.01) * (leftHand.Condition / (double) leftHand.MaxCondition);

                guardChance *= 0.001;
                guardChance += source.GetAbilityLevel(Abilities.Guard) * 0.05; // 5% additional chance to guard with each Guard level.
                guardChance *= 1 - ad.DefensePenetration;

                if (guardChance > Properties.BLOCK_CAP && ad.Attacker is GamePlayer && ad.Target is GamePlayer)
                    guardChance = Properties.BLOCK_CAP;

                int shieldSize = 1; // Guard isn't affected by shield size or attacker count.

                if (leftHand != null)
                    shieldSize = Math.Max(leftHand.Type_Damage, 1);

                // Possibly intended to be applied in RvR only.
                if (shieldSize == 1 && guardChance > 0.8)
                    guardChance = 0.8;
                else if (shieldSize == 2 && guardChance > 0.9)
                    guardChance = 0.9;
                else if (shieldSize == 3 && guardChance > 0.99)
                    guardChance = 0.99;

                if (ad.AttackType is AttackData.eAttackType.MeleeDualWield)
                    guardChance *= ad.Attacker.DualWieldDefensePenetrationFactor;

                if (guardChance > 0)
                {
                    double guardRoll;

                    if (!Properties.OVERRIDE_DECK_RNG && owner is GamePlayer player)
                        guardRoll = player.RandomDeck.GetPseudoDouble();
                    else
                        guardRoll = Util.RandomDouble();

                    if (source is GamePlayer blockAttk && blockAttk.UseDetailedCombatLog)
                        blockAttk.Out.SendMessage($"chance to guard: {guardChance * 100:0.##} rand: {guardRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                    if (guard.Target is GamePlayer blockTarg && blockTarg.UseDetailedCombatLog)
                        blockTarg.Out.SendMessage($"chance to be guarded: {guardChance * 100:0.##} rand: {guardRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                    if (guardChance > guardRoll)
                    {
                        ad.OriginalTarget = ad.Target;
                        ad.Target = source;
                        ad.BlockChance = guardChance * 100;
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool CheckDashingDefense(AttackData ad, bool stealthStyle, out eAttackResult result)
        {
            // Not implemented.
            result = eAttackResult.Any;
            return false;
        }

        /// <summary>
        /// Returns the result of an enemy attack
        /// </summary>
        public virtual eAttackResult CalculateEnemyAttackResult(WeaponAction action, AttackData ad, DbInventoryItem attackerWeapon, ref double effectiveness)
        {
            if (owner.EffectList.CountOfType<NecromancerShadeEffect>() > 0)
                return eAttackResult.NoValidTarget;

            //1.To-Hit modifiers on styles do not any effect on whether your opponent successfully Evades, Blocks, or Parries.  Grab Bag 2/27/03
            //2.The correct Order of Resolution in combat is Intercept, Evade, Parry, Block (Shield), Guard, Hit/Miss, and then Bladeturn.  Grab Bag 2/27/03, Grab Bag 4/4/03
            //3.For every person attacking a monster, a small bonus is applied to each player's chance to hit the enemy. Allowances are made for those who don't technically hit things when they are participating in the raid  for example, a healer gets credit for attacking a monster when he heals someone who is attacking the monster, because that's what he does in a battle.  Grab Bag 6/6/03
            //4.Block, parry, and bolt attacks are affected by this code, as you know. We made a fix to how the code counts people as "in combat." Before this patch, everyone grouped and on the raid was counted as "in combat." The guy AFK getting Mountain Dew was in combat, the level five guy hovering in the back and hoovering up some exp was in combat  if they were grouped with SOMEONE fighting, they were in combat. This was a bad thing for block, parry, and bolt users, and so we fixed it.  Grab Bag 6/6/03
            //5.Positional degrees - Side Positional combat styles now will work an extra 15 degrees towards the rear of an opponent, and rear position styles work in a 60 degree arc rather than the original 90 degree standard. This change should even out the difficulty between side and rear positional combat styles, which have the same damage bonus. Please note that front positional styles are not affected by this change. 1.62
            //http://daoc.catacombs.com/forum.cfm?ThreadKey=511&DefMessage=681444&forum=DAOCMainForum#Defense

            InterceptECSGameEffect intercept = null;
            // ML effects
            GameSpellEffect phaseshift = null;
            GameSpellEffect grapple = null;
            GameSpellEffect brittleguard = null;

            AttackData lastAttackData = owner.attackComponent.attackAction.LastAttackData;
            bool defenseDisabled = false; // CCs and casting state are individually checked in GameLiving.

            GamePlayer playerOwner = owner as GamePlayer;
            GamePlayer playerAttacker = ad.Attacker as GamePlayer;

            if (EffectListService.GetAbilityEffectOnTarget(owner, eEffect.Berserk) != null)
                defenseDisabled = true;

            // We check if interceptor can intercept.
            foreach (InterceptECSGameEffect inter in owner.effectListComponent.GetAbilityEffects(eEffect.Intercept))
            {
                if (inter.Target == owner && !inter.Source.IsIncapacitated && !inter.Source.IsSitting && owner.IsWithinRadius(inter.Source, InterceptAbilityHandler.INTERCEPT_DISTANCE))
                {
                    double interceptRoll;

                    if (!Properties.OVERRIDE_DECK_RNG && playerOwner != null)
                        interceptRoll = playerOwner.RandomDeck.GetPseudoDouble();
                    else
                        interceptRoll = Util.RandomDouble();

                    interceptRoll *= 100;

                    if (inter.InterceptChance > interceptRoll)
                    {
                        intercept = inter;
                        break;
                    }
                }
            }

            bool stealthStyle = false;

            if (ad.Style != null && ad.Style.StealthRequirement && playerAttacker != null && StyleProcessor.CanUseStyle(lastAttackData, playerAttacker, ad.Style, attackerWeapon))
            {
                stealthStyle = true;
                defenseDisabled = true;
                intercept = null;
                brittleguard = null;
            }

            if (playerOwner != null)
            {
                GameLiving attacker = ad.Attacker;
                GamePlayer tempPlayerAttacker = playerAttacker ?? ((attacker as GameNPC)?.Brain as IControlledBrain)?.GetPlayerOwner();

                if (tempPlayerAttacker != null && action.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                {
                    GamePlayer bodyguard = playerOwner.Bodyguard;

                    if (bodyguard != null)
                    {
                        playerOwner.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GameLiving.CalculateEnemyAttackResult.YouWereProtected"), bodyguard.Name, attacker.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                        bodyguard.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(bodyguard.Client.Account.Language, "GameLiving.CalculateEnemyAttackResult.YouHaveProtected"), playerOwner.Name, attacker.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);

                        if (attacker == tempPlayerAttacker)
                            tempPlayerAttacker.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(tempPlayerAttacker.Client.Account.Language, "GameLiving.CalculateEnemyAttackResult.YouAttempt"), playerOwner.Name, playerOwner.Name, bodyguard.Name), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        else
                            tempPlayerAttacker.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(tempPlayerAttacker.Client.Account.Language, "GameLiving.CalculateEnemyAttackResult.YourPetAttempts"), playerOwner.Name, playerOwner.Name, bodyguard.Name), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);

                        return eAttackResult.Bodyguarded;
                    }
                }
            }

            if (phaseshift != null)
                return eAttackResult.Missed;

            if (grapple != null)
                return eAttackResult.Grappled;

            if (brittleguard != null)
            {
                playerOwner?.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GameLiving.CalculateEnemyAttackResult.BlowIntercepted"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                playerAttacker?.Out.SendMessage(LanguageMgr.GetTranslation(playerAttacker.Client.Account.Language, "GameLiving.CalculateEnemyAttackResult.StrikeIntercepted"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                brittleguard.Cancel(false);
                return eAttackResult.Missed;
            }

            if (intercept != null && !stealthStyle)
            {
                if (intercept.Source is not GamePlayer || intercept.Stop())
                {
                    ad.OriginalTarget = ad.Target;
                    ad.Target = intercept.Source;
                    return eAttackResult.HitUnstyled;
                }

                intercept = null;
            }

            ad.DefensePenetration = ad.Attacker.attackComponent.CalculateDefensePenetration(ad.Weapon, ad.Target.Level);

            if (!defenseDisabled)
            {
                if (lastAttackData != null && lastAttackData.AttackResult is not eAttackResult.HitStyle)
                    lastAttackData = null;

                double evadeChance = owner.TryEvade(ad, lastAttackData);
                ad.EvadeChance = evadeChance * 100;
                double evadeRoll;

                if (!Properties.OVERRIDE_DECK_RNG && playerOwner != null)
                    evadeRoll = playerOwner.RandomDeck.GetPseudoDouble();
                else
                    evadeRoll = Util.RandomDouble();

                if (evadeChance > 0)
                {
                    if (ad.Attacker is GamePlayer evadeAtk && evadeAtk.UseDetailedCombatLog)
                        evadeAtk.Out.SendMessage($"target evade%: {evadeChance * 100:0.##} rand: {evadeRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                    if (ad.Target is GamePlayer evadeTarg && evadeTarg.UseDetailedCombatLog)
                        evadeTarg.Out.SendMessage($"your evade%: {evadeChance * 100:0.##} rand: {evadeRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                    if (evadeChance > evadeRoll)
                        return eAttackResult.Evaded;
                }

                if (ad.IsMeleeAttack)
                {
                    double parryChance = owner.TryParry(ad, lastAttackData, AttackerTracker.MeleeCount);
                    ad.ParryChance = parryChance * 100;
                    double parryRoll;

                    if (!Properties.OVERRIDE_DECK_RNG && playerOwner != null)
                        parryRoll = playerOwner.RandomDeck.GetPseudoDouble();
                    else
                        parryRoll = Util.RandomDouble();

                    if (parryChance > 0)
                    {
                        if (ad.Attacker is GamePlayer parryAtk && parryAtk.UseDetailedCombatLog)
                            parryAtk.Out.SendMessage($"target parry%: {parryChance * 100:0.##} rand: {parryRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                        if (ad.Target is GamePlayer parryTarg && parryTarg.UseDetailedCombatLog)
                            parryTarg.Out.SendMessage($"your parry%: {parryChance * 100:0.##} rand: {parryRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                        if (parryChance > parryRoll)
                            return eAttackResult.Parried;
                    }
                }

                if (CheckBlock(ad))
                    return eAttackResult.Blocked;
            }

            if (CheckGuard(ad, stealthStyle))
                return eAttackResult.Blocked;

            // Not implemented.
            // if (CheckDashingDefense(ad, stealthStyle, out eAttackResult result)
            //     return result;

            GamePlayer playerTarget = ad.Target as GamePlayer;

            // Must be set before calling `GetMissChance`.
            if (playerTarget != null)
                ad.ArmorHitLocation = playerTarget.CalculateArmorHitLocation(ad);

            double missChance = Math.Min(1, GetMissChance(action, ad, lastAttackData, attackerWeapon) * 0.01);
            double fumbleChance = ad.IsMeleeAttack ? Math.Min(1, ad.Attacker.GetModified(eProperty.FumbleChance) * 0.001) : 0;

            // At some point during Atlas' development it was decided to make fumbles a subset of misses (can't fumble without a miss), since otherwise the miss + fumble rate at low level is way too high.
            // However, this prevented fumble debuffs from working properly when fumble chance became higher than the miss chance.
            // To solve this, an extra early fumble check was added when the attacker is affected by Dirty Tricks, but this effectively made fumble chance be checked twice and made Dirty Tricks way stronger than it should be.
            // But we want to keep fumbles as a subset of misses. The solution is then to ensure miss chance can't be lower than fumble chance.
            // This however means that when miss chance is equal to fumble chance, the attacker can no longer technically miss, and can only fumble.
            // It also means that a level 50 player will always have at least 0.1% chance to fumble even against a very low level target.
            if (missChance < fumbleChance)
                missChance = fumbleChance;

            ad.MissChance = missChance * 100;

            if (missChance > 0)
            {
                double missRoll;

                if (!Properties.OVERRIDE_DECK_RNG && playerAttacker != null)
                    missRoll = playerAttacker.RandomDeck.GetPseudoDouble();
                else
                    missRoll = Util.RandomDouble();

                if (playerAttacker != null && playerAttacker.UseDetailedCombatLog)
                {
                    playerAttacker.Out.SendMessage($"miss rate: {missChance * 100:0.##}% rand: {missRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                    if (fumbleChance > 0)
                        playerAttacker.Out.SendMessage($"chance to fumble: {fumbleChance * 100:0.##}% rand: {missRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                }

                if (playerTarget != null && playerTarget.UseDetailedCombatLog)
                    playerTarget.Out.SendMessage($"chance to be missed: {missChance * 100:0.##}% rand: {missRoll * 100:0.##}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                if (missChance > missRoll)
                    return fumbleChance > missRoll ? eAttackResult.Fumbled : eAttackResult.Missed;
            }

            // Bladeturn
            // TODO: high level mob attackers penetrate bt, players are tested and do not penetrate (lv30 vs lv20)
            /*
             * http://www.camelotherald.com/more/31.shtml
             * - Bladeturns can now be penetrated by attacks from higher level monsters and
             * players. The chance of the bladeturn deflecting a higher level attack is
             * approximately the caster's level / the attacker's level.
             * Please be aware that everything in the game is
             * level/chance based - nothing works 100% of the time in all cases.
             * It was a bug that caused it to work 100% of the time - now it takes the
             * levels of the players involved into account.
             */

            ECSGameEffect bladeturn = EffectListService.GetEffectOnTarget(owner, eEffect.Bladeturn);

            if (bladeturn != null)
            {
                bool penetrate = false;

                if (stealthStyle)
                    return eAttackResult.HitUnstyled; // Exit early for stealth to prevent breaking bubble but still register a hit.

                if (ad.Attacker.Level > bladeturn.SpellHandler.Caster.Level && !Util.ChanceDouble(bladeturn.SpellHandler.Caster.Level / (double) ad.Attacker.Level))
                    penetrate = true;
                else if (ad.AttackType is AttackData.eAttackType.Ranged)
                {
                    double effectivenessAgainstBladeturn = CheckEffectivenessAgainstBladeturn(bladeturn);

                    if (effectivenessAgainstBladeturn > 0)
                        penetrate = true;

                    effectiveness *= effectivenessAgainstBladeturn;
                }

                if (bladeturn.Stop())
                {
                    if (penetrate)
                        playerOwner?.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GameLiving.CalculateEnemyAttackResult.BlowPenetrated"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                    else
                    {
                        playerAttacker?.Out.SendMessage(LanguageMgr.GetTranslation(playerAttacker.Client.Account.Language, "GameLiving.CalculateEnemyAttackResult.StrikeAbsorbed"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);

                        if (playerOwner != null)
                        {
                            playerOwner.Out.SendMessage(LanguageMgr.GetTranslation(playerOwner.Client.Account.Language, "GameLiving.CalculateEnemyAttackResult.BlowAbsorbed"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                            playerOwner.Stealth(false);
                        }

                        return eAttackResult.Missed;
                    }
                }
            }

            if (playerOwner?.IsOnHorse == true)
                playerOwner.IsOnHorse = false;

            return eAttackResult.HitUnstyled;

            double CheckEffectivenessAgainstBladeturn(ECSGameEffect bladeturn)
            {
                // 1.62: Longshot and Volley always penetrate.
                if (action.RangedAttackType is eRangedAttackType.Long or eRangedAttackType.Volley)
                    return 1.0;

                // 1.62: Penetrating Arrow penetrates only if the caster != target.
                if (owner == bladeturn.SpellHandler.Caster)
                    return 0.0;

                return 0.25 + ad.Attacker.GetAbilityLevel(Abilities.PenetratingArrow) * 0.25;
            }
        }

        private static readonly IReadOnlyDictionary<eAttackResult, string> _simpleAttackMessageKeys = new Dictionary<eAttackResult, string>
        {
            [eAttackResult.TargetNotVisible] = "GamePlayer.Attack.NotInView",
            [eAttackResult.OutOfRange] = "GamePlayer.Attack.TooFarAway",
            [eAttackResult.TargetDead] = "GamePlayer.Attack.AlreadyDead",
            [eAttackResult.Blocked] = "GamePlayer.Attack.Blocked",
            [eAttackResult.Parried] = "GamePlayer.Attack.Parried",
            [eAttackResult.Evaded] = "GamePlayer.Attack.Evaded",
            [eAttackResult.NoTarget] = "GamePlayer.Attack.NeedTarget",
            [eAttackResult.NoValidTarget] = "GamePlayer.Attack.CantBeAttacked",
        };

        private void SendAttackingCombatMessages(WeaponAction action, AttackData ad)
        {
            if (owner is not GamePlayer player)
                return;

            if (ShouldSuppressSpamMessage(ad.AttackResult))
            {
                if (GameLoop.GameLoopTime - attackAction.RoundWithNoAttackTime <= 1500)
                    return;

                attackAction.RoundWithNoAttackTime = 0;
            }

            if (_simpleAttackMessageKeys.TryGetValue(ad.AttackResult, out var messageKey))
            {
                string targetName = ad.Target?.GetName(0, true, player.Client.Account.Language, ad.Target as GameNPC);
                SendLocalizedMessage(player, messageKey, targetName);
                return;
            }

            switch (ad.AttackResult)
            {
                case eAttackResult.Missed:
                {
                    SendMissMessage(player, ad);
                    break;
                }
                case eAttackResult.Fumbled:
                {
                    SendLocalizedMessage(player, "GamePlayer.Attack.Fumble");
                    break;
                }
                case eAttackResult.HitStyle:
                case eAttackResult.HitUnstyled:
                {
                    SendHitMessages(player, action, ad);
                    break;
                }
            }

            static bool ShouldSuppressSpamMessage(eAttackResult result)
            {
                return result is not eAttackResult.Missed
                    and not eAttackResult.HitUnstyled
                    and not eAttackResult.HitStyle
                    and not eAttackResult.Evaded
                    and not eAttackResult.Blocked
                    and not eAttackResult.Parried;
            }

            static void SendMissMessage(GamePlayer player, AttackData ad)
            {
                string baseKey = ad.MissChance > 0 ? "GamePlayer.Attack.Miss" : "GamePlayer.Attack.StrafMiss";
                string message = LanguageMgr.GetTranslation(player.Client.Account.Language, baseKey);

                if (ad.MissChance > 0)
                    message += $" ({ad.MissChance:0.##}%)";

                player.Out.SendMessage(message, eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            }

            static void SendHitMessages(GamePlayer player, WeaponAction action, AttackData ad)
            {
                if (ad.AttackResult is eAttackResult.HitStyle)
                {
                    string damageAmount = $" (+{ad.StyleDamage}, GR: {ad.Style.GrowthRate})";
                    SendLocalizedMessage(player, "StyleProcessor.ExecuteStyle.PerformPerfectly", ad.Style.Name, damageAmount);
                }

                bool wasIntercepted = ad.OriginalTarget != null && ad.OriginalTarget != ad.Target;

                // Message: "You attack <OriginalTarget>, but <FinalTarget> steps in the way!"
                if (wasIntercepted)
                    SendLocalizedMessage(player, "GamePlayer.Attack.Intercepted", ad.OriginalTarget.GetName(0, true), ad.Target.GetName(0, false));

                // Build and send the main hit message.
                string attackTypeMsg = GetAttackVerb(player, action);
                string hitWeapon = GetWeaponNameForMessage(player, ad.Weapon);
                string modMessage = ad.Modifier == 0 ? string.Empty : $" ({(ad.Modifier > 0 ? "+" : "")}{ad.Modifier})";

                // Use a different key for intercepted hits vs direct hits.
                string hitMessageKey = wasIntercepted ? "GamePlayer.Attack.InterceptedHit" : "GamePlayer.Attack.InterceptHit";

                SendLocalizedMessage(player, hitMessageKey,
                    attackTypeMsg,
                    ad.Target.GetName(0, false, player.Client.Account.Language, ad.Target as GameNPC),
                    hitWeapon,
                    ad.Damage,
                    modMessage);

                // Send critical hit message if applicable.
                if (ad.CriticalDamage > 0)
                {
                    string baseMessage = LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Critical", ad.Target.GetName(0, false, player.Client.Account.Language, ad.Target as GameNPC), ad.CriticalDamage);
                    string criticalMessage = $"{baseMessage} ({ad.CriticalChance}%)";
                    player.Out.SendMessage(criticalMessage, eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                }

                static string GetWeaponNameForMessage(GamePlayer player, DbInventoryItem weapon)
                {
                    if (weapon == null)
                        return string.Empty;

                    string name = player.Client.Account.Language == "DE" ? weapon.Name : GlobalConstants.NameToShortName(weapon.Name);
                    string withYour = LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.WithYour");
                    return $" {withYour} {name}";
                }

                static string GetAttackVerb(GamePlayer player, WeaponAction action)
                {
                    string key = action.ActiveWeaponSlot is eActiveWeaponSlot.Distance ? "GamePlayer.Attack.YouShot" : "GamePlayer.Attack.YouAttack";
                    return LanguageMgr.GetTranslation(player.Client.Account.Language, key);
                }
            }

            static void SendLocalizedMessage(GamePlayer player, string key, params object[] args)
            {
                string message = LanguageMgr.GetTranslation(player.Client.Account.Language, key, args);
                player.Out.SendMessage(message, eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            }
        }

        private static void SendDefendingCombatMessages(AttackData ad)
        {
            bool wasIntercepted = ad.OriginalTarget != null && ad.OriginalTarget != ad.Target;

            switch (ad.AttackResult)
            {
                case eAttackResult.Blocked:
                {
                    if (wasIntercepted)
                    {
                        if (ad.OriginalTarget is GamePlayer guardedPlayer)
                            SendLocalizedMessage(guardedPlayer, "GameLiving.AttackData.BlocksYou", ad.BlockChance, ad.Target.GetName(0, true), ad.Attacker.GetName(0, false));

                        if (ad.Target is GamePlayer blockerPlayer)
                        {
                            SendLocalizedMessage(blockerPlayer, "GameLiving.AttackData.YouBlock", ad.BlockChance, ad.Attacker.GetName(0, false), ad.OriginalTarget.GetName(0, false));
                            blockerPlayer.Stealth(false);
                        }
                    }
                    else
                    {
                        if (ad.Target is GamePlayer targetPlayer)
                            SendLocalizedMessage(targetPlayer, "GameLiving.Attack.Block", ad.BlockChance, ad.Attacker.GetName(0, true));
                    }

                    break;
                }
                case eAttackResult.HitUnstyled:
                case eAttackResult.HitStyle:
                {
                    if (wasIntercepted)
                    {
                        if (ad.OriginalTarget is GamePlayer originalTargetPlayer)
                            SendLocalizedMessage(originalTargetPlayer, "GameLiving.AttackData.StepsInFront", 0, ad.Target.GetName(0, true));

                        if (ad.Target is GamePlayer finalTargetPlayer)
                            SendLocalizedMessage(finalTargetPlayer, "GameLiving.AttackData.YouStepInFront", 0, ad.OriginalTarget.GetName(0, false));
                    }

                    break;
                }

                // Note: Most of the missing logic here is currently handled by `GameLiving.OnAttackedByEnemy`.
            }

            static void SendLocalizedMessage(GamePlayer player, string key, double chance, params object[] args)
            {
                string message = LanguageMgr.GetTranslation(player.Client.Account.Language, key, args);

                if (chance > 0)
                    message += $" ({chance:0.0}%)";

                player.Out.SendMessage(message, eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
            }
        }

        private void BroadcastObserverMessage(AttackData ad)
        {
            Array.Clear(_broadcastExcludes);
            AddParticipantToExcludes(ad.Attacker, _broadcastExcludes, 0);
            AddParticipantToExcludes(ad.Target, _broadcastExcludes, 1);
            AddParticipantToExcludes(ad.OriginalTarget, _broadcastExcludes, 2);

            string message = ad.AttackResult switch
            {
                eAttackResult.Parried  => $"{ad.Attacker.GetName(0, true)} attacks {ad.Target.GetName(0, false)} and is parried!",
                eAttackResult.Evaded   => $"{ad.Attacker.GetName(0, true)} attacks {ad.Target.GetName(0, false)} and is evaded!",
                eAttackResult.Fumbled  => $"{ad.Attacker.GetName(0, true)} fumbled!",
                eAttackResult.Missed   => $"{ad.Attacker.GetName(0, true)} attacks {ad.Target.GetName(0, false)} and misses!",
                eAttackResult.Blocked  => BuildBlockedBroadcastMessage(ad),
                eAttackResult.HitUnstyled or eAttackResult.HitStyle => BuildHitBroadcastMessage(ad),
                _ => string.Empty,
            };

            if (!string.IsNullOrEmpty(message))
                Message.SystemToArea(ad.Attacker, message, eChatType.CT_OthersCombat, _broadcastExcludes);

            ad.BroadcastMessage = message;

            static void AddParticipantToExcludes(GameLiving entity, GameLiving[] excludes, int index)
            {
                if (entity == null)
                    return;

                if (entity is GamePlayer)
                    excludes[index] = entity;

                if (entity is GameNPC npc && npc.Brain is IControlledBrain brain)
                {
                    GamePlayer owner = brain.GetPlayerOwner();

                    if (owner != null)
                        excludes[index] = owner;
                }
            }

            static string BuildBlockedBroadcastMessage(AttackData ad)
            {
                bool wasGuarded = ad.OriginalTarget != null && ad.Target != ad.OriginalTarget;

                if (wasGuarded)
                    return $"{ad.Attacker.GetName(0, true)} attacks {ad.OriginalTarget.GetName(0, false)} but is blocked by {ad.Target.GetName(0, false)}!";

                return $"{ad.Attacker.GetName(0, true)} attacks {ad.Target.GetName(0, false)} and is blocked!";
            }

            static string BuildHitBroadcastMessage(AttackData ad)
            {
                bool wasIntercepted = ad.OriginalTarget != null && ad.Target != ad.OriginalTarget;

                if (wasIntercepted)
                    return $"{ad.Attacker.GetName(0, true)} attacks {ad.OriginalTarget.GetName(0, false)} but hits {ad.Target.GetName(0, false)}!";

                if (ad.Attacker is GamePlayer)
                {
                    string weaponName = ad.Weapon != null ? GlobalConstants.NameToShortName(ad.Weapon.Name) : "their weapon";
                    return $"{ad.Attacker.GetName(0, true)} attacks {ad.Target.GetName(0, false)} with {ad.Attacker.GetPronoun(1, false)} {weaponName}!";
                }

                return $"{ad.Attacker.GetName(0, true)} attacks {ad.Target.GetName(0, false)} and hits!";
            }
        }

        public int CalculateCriticalDamage(AttackData ad)
        {
            if (!Util.Chance(ad.CriticalChance))
                return 0;

            if (owner is GamePlayer)
            {
                // Triple wield prevents critical hits (1.62).
                if (EffectListService.GetAbilityEffectOnTarget(ad.Target, eEffect.TripleWield) != null)
                    return 0;

                int critMin;
                int critMax;
                ECSGameEffect berserk = EffectListService.GetEffectOnTarget(owner, eEffect.Berserk);

                if (berserk != null)
                {
                    int level = owner.GetAbilityLevel(Abilities.Berserk);
                    // https://web.archive.org/web/20061017095337/http://daoc.catacombs.com/forum.cfm?ThreadKey=10833&DefMessage=922046&forum=37
                    // 1% min is weird. Raised to 10%.
                    // Berserk 1 = 10-25%
                    // Berserk 2 = 10-50%
                    // Berserk 3 = 10-75%
                    // Berserk 4 = 10-99%
                    critMin = (int) (ad.Damage * 0.1);
                    critMax = (int) (Math.Min(0.99, level * 0.25) * ad.Damage);
                }
                else
                {
                    // Min crit damage is 10%.
                    critMin = (int) (ad.Damage * 0.1);

                    // Max crit damage to players is 50%.
                    if (ad.Target is GamePlayer)
                        critMax = ad.Damage / 2;
                    else
                        critMax = ad.Damage;
                }

                critMin = Math.Max(critMin, 0);
                critMax = Math.Max(critMin, critMax);
                return Util.Random(critMin, critMax);
            }
            else
            {
                int maxCriticalDamage = ad.Target is GamePlayer ? ad.Damage / 2 : ad.Damage;
                int minCriticalDamage = (int) (ad.Damage * MinMeleeCriticalDamage);

                if (minCriticalDamage > maxCriticalDamage)
                    minCriticalDamage = maxCriticalDamage;

                return Util.Random(minCriticalDamage, maxCriticalDamage);
            }
        }

        public double GetMissChance(WeaponAction action, AttackData ad, AttackData lastAD, DbInventoryItem weapon)
        {
            // No miss if the target is sitting or for Volley attacks.
            if ((owner is GamePlayer player && player.IsSitting) || action.RangedAttackType is eRangedAttackType.Volley)
               return 0;

            // In 1.117C, every weapon was given the intrinsic 5% flat bonus special weapons (such as artifacts) had, lowering the base miss rate to 13%.
            double missChance = 18;
            missChance -= ad.Attacker.GetModified(eProperty.ToHitBonus);

            if (owner is not GamePlayer || ad.Attacker is not GamePlayer)
            {
                // 1.33 per level difference.
                missChance -= (ad.Attacker.EffectiveLevel - owner.EffectiveLevel) * (1 + 1 / 3.0);
                missChance -= Math.Max(0, AttackerTracker.Count - 1) * Properties.MISSRATE_REDUCTION_PER_ATTACKERS;
            }

            // Weapon and armor bonuses.
            int armorBonus = 0;

            if (ad.Target is GamePlayer playerTarget)
            {
                if (ad.Target.Inventory != null)
                {
                    DbInventoryItem armor = ad.Target.Inventory.GetItem((eInventorySlot) ad.ArmorHitLocation);

                    if (armor != null)
                        armorBonus = armor.Bonus;
                }

                int bonusCap = GetBonusCapForLevel(playerTarget.Level);

                if (armorBonus > bonusCap)
                    armorBonus = bonusCap;
            }

            if (weapon != null)
            {
                int bonusCap = GetBonusCapForLevel(ad.Attacker.Level);
                int weaponBonus = weapon.Bonus;

                if (weaponBonus > bonusCap)
                    weaponBonus = bonusCap;

                armorBonus -= weaponBonus;
            }

            if (ad.Target is GamePlayer && ad.Attacker is GamePlayer)
                missChance += armorBonus;
            else
                missChance += missChance * armorBonus / 100;

            // Style bonuses.
            if (ad.Style != null)
                missChance -= ad.Style.BonusToHit;

            if (lastAD != null && lastAD.AttackResult is eAttackResult.HitStyle && lastAD.Style != null)
                missChance += lastAD.Style.BonusToDefense;

            if (action.ActiveWeaponSlot is eActiveWeaponSlot.Distance)
            {
                DbInventoryItem ammo = GetAttackAmmo(action);

                if (ammo != null)
                {
                    switch ((ammo.SPD_ABS >> 4) & 0x3)
                    {
                        // http://rothwellhome.org/guides/archery.htm
                        case 0:
                            missChance += missChance * 0.15;
                            break; // Rough
                        //case 1:
                        //  break;
                        case 2:
                            missChance -= missChance * 0.15;
                            break; // Doesn't exist (?)
                        case 3:
                            missChance -= missChance * 0.25;
                            break; // Footed
                    }
                }
            }

            return missChance;

            static int GetBonusCapForLevel(int level)
            {
                return level switch
                {
                    < 15 => 0,
                    < 20 => 5,
                    < 25 => 10,
                    < 30 => 15,
                    < 35 => 20,
                    < 40 => 25,
                    < 45 => 30,
                    _ => 35
                };
            }
        }

        /// <summary>
        /// Minimum melee critical damage as a percentage of the
        /// raw damage.
        /// </summary>
        protected float MinMeleeCriticalDamage => 0.1f;

        public static double CalculateSlowWeaponDamageModifier(DbInventoryItem weapon)
        {
            // Slow weapon bonus as found here: https://www2.uthgard.net/tracker/issue/2753/@/Bow_damage_variance_issue_(taking_item_/_spec_???)
            return 1 + (weapon.SPD_ABS - 20) * 0.003;
        }

        public double CalculateTwoHandedDamageModifier(DbInventoryItem weapon)
        {
            return 1.1 + owner.WeaponSpecLevel(weapon) * 0.005;
        }

        /// <summary>
        /// Checks whether Living has ability to use lefthanded weapons
        /// </summary>
        public bool CanUseLefthandedWeapon
        {
            get
            {
                if (owner is GamePlayer playerOwner)
                    return playerOwner.CharacterClass.CanUseLefthandedWeapon;
                else if (owner is GameNPC)
                    return true;

                return false;
            }
        }

        public double CalculateDwCdLeftHandSwingChance()
        {
            int specLevel = owner.GetModifiedSpecLevel(Specs.Dual_Wield);
            specLevel = Math.Max(specLevel, owner.GetModifiedSpecLevel(Specs.Celtic_Dual));
            specLevel = Math.Max(specLevel, owner.GetModifiedSpecLevel(Specs.Fist_Wraps));

            if (specLevel > 0)
            {
                int bonus = owner.GetModified(eProperty.OffhandChance) + owner.GetModified(eProperty.OffhandDamageAndChance);
                return 25 + specLevel * 68 * 0.01 + bonus;
            }

            return 0;
        }

        public (double, double, double) CalculateHthSwingChances(DbInventoryItem leftWeapon)
        {
            if (leftWeapon == null)
                return (0, 0, 0);

            int specLevel = owner.GetModifiedSpecLevel(Specs.HandToHand);

            if (specLevel <= 0 || (eObjectType) leftWeapon.Object_Type is not eObjectType.HandToHand)
                return (0, 0, 0);

            double doubleSwingChance = specLevel * 0.5; // specLevel >> 1
            double tripleSwingChance = specLevel >= 25 ? doubleSwingChance * 0.5 : 0; // specLevel >> 2
            double quadSwingChance = specLevel >= 40 ? tripleSwingChance * 0.25 : 0; // specLevel >> 4
            int bonus = owner.GetModified(eProperty.OffhandChance) + owner.GetModified(eProperty.OffhandDamageAndChance);
            doubleSwingChance += bonus; // It's apparently supposed to only affect double swing chance around 1.65, which puts it more in line with DW / CD.
            return (doubleSwingChance, tripleSwingChance, quadSwingChance);
        }

        /// <summary>
        /// Calculates how many times left hand swings
        /// </summary>
        public int CalculateLeftHandSwingCount(DbInventoryItem mainWeapon, DbInventoryItem leftWeapon)
        {
            // Let's make NPCs require an actual weapon too. It looks silly otherwise.
            if (!CanUseLefthandedWeapon || leftWeapon == null || (eObjectType) leftWeapon.Object_Type is eObjectType.Shield)
                return 0;

            if (owner is GameNPC npcOwner)
            {
                if (mainWeapon == null || mainWeapon.SlotPosition is not Slot.RIGHTHAND)
                    return 0;

                double random = Util.RandomDouble() * 100;
                return random < npcOwner.LeftHandSwingChance ? 1 : 0;
            }

            if (owner is not GamePlayer playerOwner || (eObjectType) leftWeapon.Object_Type is eObjectType.Shield || mainWeapon == null)
                return 0;

            if (owner.GetBaseSpecLevel(Specs.Left_Axe) > 0)
            {
                if (playerOwner != null && playerOwner.UseDetailedCombatLog)
                {
                    // This shouldn't be done here.
                    double effectiveness = CalculateLeftAxeModifier();
                    playerOwner.Out.SendMessage($"{Math.Round(effectiveness * 100, 2)}% dmg (after LA penalty)\n", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                }

                return 1; // Always use left axe.
            }

            double leftHandSwingChance = CalculateDwCdLeftHandSwingChance();

            if (leftHandSwingChance > 0)
            {
                double random = Util.RandomDouble() * 100;

                if (playerOwner != null && playerOwner.UseDetailedCombatLog)
                    playerOwner.Out.SendMessage($"OH swing%: {leftHandSwingChance:0.##}\n", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                return random < leftHandSwingChance ? 1 : 0;
            }

            (double doubleSwingChance, double tripleSwingChance, double quadSwingChance) = CalculateHthSwingChances(leftWeapon);

            if (doubleSwingChance > 0)
            {
                double random = Util.RandomDouble() * 100;

                if (playerOwner != null && playerOwner.UseDetailedCombatLog)
                    playerOwner.Out.SendMessage( $"Chance for 2 swings: {doubleSwingChance:0.##}% | 3 swings: {tripleSwingChance:0.##}% | 4 swings: {quadSwingChance:0.##}% \n", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);

                if (random < doubleSwingChance)
                    return 1;

                tripleSwingChance += doubleSwingChance;

                if (random < tripleSwingChance)
                    return 2;

                quadSwingChance += tripleSwingChance;

                if (random < quadSwingChance)
                    return 3;
            }

            return 0;
        }

        public double CalculateLeftAxeModifier()
        {
            int LeftAxeSpec = owner.GetModifiedSpecLevel(Specs.Left_Axe);

            if (LeftAxeSpec == 0)
                return 1.0;

            double modifier = 0.625 + 0.0034 * LeftAxeSpec;

            if (owner.GetModified(eProperty.OffhandDamageAndChance) > 0)
                return modifier + owner.GetModified(eProperty.OffhandDamageAndChance) * 0.01;

            return modifier;
        }

        public class BlockRoundHandler
        {
            private GameObject _owner;
            private int _usedBlockRoundCount;

            public BlockRoundHandler(GameObject owner)
            {
                _owner = owner;
            }

            public bool Consume(int shieldSize, AttackData attackData)
            {
                // Block rounds work from the point of view of the attacker and use their attack speed, similar to how interrupts work.
                // However, according to grab bags, it's supposed to be based on the defender's swing speed. But this sounds very wrong, since it implies haste buffs should make blocking more effective.

                if (attackData.Target is not GamePlayer)
                    return true;

                // There is no need to make dual wield even more effective against shields.
                // Returning true allow the off-hand of dual wield attacks to be blocked without consuming a block.
                if (attackData.AttackType is AttackData.eAttackType.MeleeDualWield && attackData.IsOffHand)
                    return true;

                // Many threads can enter this block simultaneously, so we increment the count first, then decrement if we've overshot the shield size.
                if (Interlocked.Increment(ref _usedBlockRoundCount) > shieldSize)
                {
                    Relinquish();
                    return false;
                }

                // Decrement the count after a duration equal to the attack interval.
                // We need to make sure it ticks before the attacker's next attack. We can't use `AttackData.Interval` only because `AttackAction.NextTick` is adjusted by `ServiceUtil.ShouldTickAdjust`.
                new BlockRoundCountDecrementTimer(_owner, Relinquish).Start((int) (attackData.Attacker.attackComponent.attackAction.NextTick - GameLoop.GameLoopTime + attackData.Interval));
                return true;
            }

            private void Relinquish()
            {
                Interlocked.Decrement(ref _usedBlockRoundCount);
            }

            class BlockRoundCountDecrementTimer : ECSGameTimerWrapperBase
            {
                private Action _decrementBlockRoundCount;

                public BlockRoundCountDecrementTimer(GameObject owner, Action decrementBlockRoundCount) : base(owner)
                {
                    _decrementBlockRoundCount = decrementBlockRoundCount;
                }

                protected override int OnTick(ECSGameTimer timer)
                {
                    _decrementBlockRoundCount();
                    return 0;
                }
            }
        }
    }
}
