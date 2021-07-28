using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.RealmAbilities;
using DOL.GS.ServerProperties;
using DOL.GS.SkillHandler;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.Language;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.GameLiving;
using static DOL.GS.GameObject;

namespace DOL.GS
{
    public class AttackComponent
    {
        public GameLiving owner;
        public WeaponAction weaponAction;
        public AttackAction attackAction;

        /// <summary>
		/// Holds the Style that this living should use next
		/// </summary>
		protected Style m_nextCombatStyle;
        /// <summary>
        /// Holds the backup style for the style that the living should use next
        /// </summary>
        protected Style m_nextCombatBackupStyle;

        /// <summary>
        /// Gets or Sets the next combat style to use
        /// </summary>
        public Style NextCombatStyle
        {
            get { return m_nextCombatStyle; }
            set { m_nextCombatStyle = value; }
        }
        /// <summary>
        /// Gets or Sets the next combat backup style to use
        /// </summary>
        public Style NextCombatBackupStyle
        {
            get { return m_nextCombatBackupStyle; }
            set { m_nextCombatBackupStyle = value; }
        }

        public AttackComponent(GameLiving owner)
        {
            this.owner = owner;
        }

        public void Tick(long time)
        {
            if (weaponAction != null)
            {
                if (weaponAction.AttackFinished)
                    weaponAction = null;
                else
                    weaponAction.Tick(time);
            }
            if (attackAction != null)
            {
                attackAction.Tick(time);
            }
        }

        public void ExecuteWeaponStyle(Style style)
        {
            StyleProcessor.TryToUseStyle(owner, style);
        }

        

        /// <summary>
		/// The chance for a critical hit
		/// </summary>
		/// <param name="weapon">attack weapon</param>
		public int AttackCriticalChance(InventoryItem weapon)
        {
            var p = owner as GamePlayer;

            if (weapon != null && weapon.Item_Type == Slot.RANGED && p.rangeAttackComponent?.RangedAttackType == RangeAttackComponent.eRangedAttackType.Critical)
                return 0; // no crit damage for crit shots

            // check for melee attack
            if (weapon != null && weapon.Item_Type != Slot.RANGED)
            {
                return p.GetModified(eProperty.CriticalMeleeHitChance);
            }

            // check for ranged attack
            if (weapon != null && weapon.Item_Type == Slot.RANGED)
            {
                return p.GetModified(eProperty.CriticalArcheryHitChance);
            }

            // base 10% chance of critical for all with melee weapons
            return 10;
        }

        /// <summary>
        /// Returns the damage type of the current attack
        /// </summary>
        /// <param name="weapon">attack weapon</param>
        public eDamageType AttackDamageType(InventoryItem weapon)
        {
            var p = owner as GamePlayer;

            if (weapon == null)
                return eDamageType.Natural;
            switch ((eObjectType)weapon.Object_Type)
            {
                case eObjectType.Crossbow:
                case eObjectType.Longbow:
                case eObjectType.CompositeBow:
                case eObjectType.RecurvedBow:
                case eObjectType.Fired:
                    InventoryItem ammo = p.rangeAttackComponent?.RangeAttackAmmo;
                    if (ammo == null)
                        return (eDamageType)weapon.Type_Damage;
                    return (eDamageType)ammo.Type_Damage;
                case eObjectType.Shield:
                    return eDamageType.Crush; // TODO: shields do crush damage (!) best is if Type_Damage is used properly
                default:
                    return (eDamageType)weapon.Type_Damage;
            }
        }

        /// <summary>
        /// Returns the AttackRange of this living
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
                var p = owner as GamePlayer;

                GameLiving livingTarget = p.TargetObject as GameLiving;

                //TODO change to real distance of bows!
                if (p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    InventoryItem weapon = p.AttackWeapon;
                    if (weapon == null)
                        return 0;

                    double range;
                    InventoryItem ammo = p.rangeAttackComponent?.RangeAttackAmmo;

                    switch ((eObjectType)weapon.Object_Type)
                    {
                        case eObjectType.Longbow: range = 1760; break;
                        case eObjectType.RecurvedBow: range = 1680; break;
                        case eObjectType.CompositeBow: range = 1600; break;
                        default: range = 1200; break; // shortbow, xbow, throwing
                    }

                    range = Math.Max(32, range * p.GetModified(eProperty.ArcheryRange) * 0.01);

                    if (ammo != null)
                        switch ((ammo.SPD_ABS >> 2) & 0x3)
                        {
                            case 0: range *= 0.85; break; //Clout -15%
                                                          //						case 1:                break; //(none) 0%
                            case 2: range *= 1.15; break; //doesn't exist on live
                            case 3: range *= 1.25; break; //Flight +25%
                        }
                    if (livingTarget != null) range += Math.Min((p.Z - livingTarget.Z) / 2.0, 500);
                    if (range < 32) range = 32;

                    return (int)(range);
                }

                int meleerange = 128;
                GameKeepComponent keepcomponent = livingTarget as GameKeepComponent; // TODO better component melee attack range check
                if (keepcomponent != null)
                    meleerange += 150;
                else
                {
                    if (livingTarget != null && livingTarget.IsMoving)
                        meleerange += 32;
                    if (p.IsMoving)
                        meleerange += 32;
                }
                return meleerange;
            }
            set { }
        }

        /// <summary>
        /// Gets the current attackspeed of this living in milliseconds
        /// </summary>
        /// <param name="weapons">attack weapons</param>
        /// <returns>effective speed of the attack. average if more than one weapon.</returns>
        public int AttackSpeed(params InventoryItem[] weapons)
        {
            var p = owner as GamePlayer;

            if (weapons == null || weapons.Length < 1)
                return 0;

            int count = 0;
            double speed = 0;
            bool bowWeapon = true;

            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] != null)
                {
                    speed += weapons[i].SPD_ABS;
                    count++;

                    switch (weapons[i].Object_Type)
                    {
                        case (int)eObjectType.Fired:
                        case (int)eObjectType.Longbow:
                        case (int)eObjectType.Crossbow:
                        case (int)eObjectType.RecurvedBow:
                        case (int)eObjectType.CompositeBow:
                            break;
                        default:
                            bowWeapon = false;
                            break;
                    }
                }
            }

            if (count < 1)
                return 0;

            speed /= count;

            int qui = Math.Min(250, p.Quickness); //250 soft cap on quickness

            if (bowWeapon)
            {
                if (ServerProperties.Properties.ALLOW_OLD_ARCHERY)
                {
                    //Draw Time formulas, there are very many ...
                    //Formula 2: y = iBowDelay * ((100 - ((iQuickness - 50) / 5 + iMasteryofArcheryLevel * 3)) / 100)
                    //Formula 1: x = (1 - ((iQuickness - 60) / 500 + (iMasteryofArcheryLevel * 3) / 100)) * iBowDelay
                    //Table a: Formula used: drawspeed = bowspeed * (1-(quickness - 50)*0.002) * ((1-MoA*0.03) - (archeryspeedbonus/100))
                    //Table b: Formula used: drawspeed = bowspeed * (1-(quickness - 50)*0.002) * (1-MoA*0.03) - ((archeryspeedbonus/100 * basebowspeed))

                    //For now use the standard weapon formula, later add ranger haste etc.
                    speed *= (1.0 - (qui - 60) * 0.002);
                    double percent = 0;
                    // Calcul ArcherySpeed bonus to substract
                    percent = speed * 0.01 * p.GetModified(eProperty.ArcherySpeed);
                    // Apply RA difference
                    speed -= percent;
                    //log.Debug("speed = " + speed + " percent = " + percent + " eProperty.archeryspeed = " + GetModified(eProperty.ArcherySpeed));
                    if (p.rangeAttackComponent?.RangedAttackType == RangeAttackComponent.eRangedAttackType.Critical)
                        speed = speed * 2 - (p.GetAbilityLevel(Abilities.Critical_Shot) - 1) * speed / 10;
                }
                else
                {
                    // no archery bonus
                    speed *= (1.0 - (qui - 60) * 0.002);
                }
            }
            else
            {
                // TODO use haste
                //Weapon Speed*(1-(Quickness-60)/500]*(1-Haste)
                speed *= (1.0 - (qui - 60) * 0.002) * 0.01 * p.GetModified(eProperty.MeleeSpeed);
            }

            // apply speed cap
            if (speed < 15)
            {
                speed = 15;
            }
            return (int)(speed * 100);
        }

        /// <summary>
        /// Gets the attack damage
        /// </summary>
        /// <param name="weapon">the weapon used for attack</param>
        /// <returns>the weapon damage</returns>
        public double AttackDamage(InventoryItem weapon)
        {
            var p = owner as GamePlayer;

            if (weapon == null)
                return 0;

            double effectiveness = 1.00;
            double damage = p.WeaponDamage(weapon) * weapon.SPD_ABS * 0.1;

            if (weapon.Hand == 1) // two-hand
            {
                // twohanded used weapons get 2H-Bonus = 10% + (Skill / 2)%
                int spec = p.WeaponSpecLevel(weapon) - 1;
                damage *= 1.1 + spec * 0.005;
            }

            if (weapon.Item_Type == Slot.RANGED)
            {
                //ammo damage bonus
                if (p.rangeAttackComponent?.RangeAttackAmmo != null)
                {
                    switch ((p.rangeAttackComponent?.RangeAttackAmmo.SPD_ABS) & 0x3)
                    {
                        case 0: damage *= 0.85; break; //Blunt       (light) -15%
                                                       //case 1: damage *= 1;	break; //Bodkin     (medium)   0%
                        case 2: damage *= 1.15; break; //doesn't exist on live
                        case 3: damage *= 1.25; break; //Broadhead (X-heavy) +25%
                    }
                }
                //Ranged damage buff,debuff,Relic,RA
                effectiveness += p.GetModified(eProperty.RangedDamage) * 0.01;
            }
            else if (weapon.Item_Type == Slot.RIGHTHAND || weapon.Item_Type == Slot.LEFTHAND || weapon.Item_Type == Slot.TWOHAND)
            {
                //Melee damage buff,debuff,Relic,RA
                effectiveness += p.GetModified(eProperty.MeleeDamage) * 0.01;
            }
            damage *= effectiveness;
            return damage;
        }

        /// <summary>
        /// Decides which style living will use in this moment
        /// </summary>
        /// <returns>Style to use or null if none</returns>
        public Style GetStyleToUse()
        {
            InventoryItem weapon;
            if (NextCombatStyle == null) return null;
            if (NextCombatStyle.WeaponTypeRequirement == (int)eObjectType.Shield)
                weapon = owner.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
            else weapon = owner.AttackWeapon;

            if (StyleProcessor.CanUseStyle(owner, NextCombatStyle, weapon))
                return NextCombatStyle;

            if (NextCombatBackupStyle == null) return NextCombatStyle;

            return NextCombatBackupStyle;
        }

        /// <summary>
		/// Picks a style, prioritizing reactives an	d chains over positionals and anytimes
		/// </summary>
		/// <returns>Selected style</returns>
		public Style NPCGetStyleToUse()
        {
            var p = owner as GameNPC;
            if (p.Styles == null || p.Styles.Count < 1 || p.TargetObject == null)
                return null;

            // Chain and defensive styles skip the GAMENPC_CHANCES_TO_STYLE,
            //	or they almost never happen e.g. NPC blocks 10% of the time,
            //	default 20% style chance means the defensive style only happens
            //	2% of the time, and a chain from it only happens 0.4% of the time.
            if (p.StylesChain != null && p.StylesChain.Count > 0)
                foreach (Style s in p.StylesChain)
                    if (StyleProcessor.CanUseStyle(p, s, p.AttackWeapon))
                        return s;

            if (p.StylesDefensive != null && p.StylesDefensive.Count > 0)
                foreach (Style s in p.StylesDefensive)
                    if (StyleProcessor.CanUseStyle(p, s, p.AttackWeapon)
                        && p.CheckStyleStun(s)) // Make sure we don't spam stun styles like Brutalize
                        return s;

            if (Util.Chance(Properties.GAMENPC_CHANCES_TO_STYLE))
            {
                // Check positional styles
                // Picking random styles allows mobs to use multiple styles from the same position
                //	e.g. a mob with both Pincer and Ice Storm side styles will use both of them.
                if (p.StylesBack != null && p.StylesBack.Count > 0)
                {
                    Style s = p.StylesBack[Util.Random(0, p.StylesBack.Count - 1)];
                    if (StyleProcessor.CanUseStyle(p, s, p.AttackWeapon))
                        return s;
                }

                if (p.StylesSide != null && p.StylesSide.Count > 0)
                {
                    Style s = p.StylesSide[Util.Random(0, p.StylesSide.Count - 1)];
                    if (StyleProcessor.CanUseStyle(p, s, p.AttackWeapon))
                        return s;
                }

                if (p.StylesFront != null && p.StylesFront.Count > 0)
                {
                    Style s = p.StylesFront[Util.Random(0, p.StylesFront.Count - 1)];
                    if (StyleProcessor.CanUseStyle(p, s, p.AttackWeapon))
                        return s;
                }

                // Pick a random anytime style
                if (p.StylesAnytime != null && p.StylesAnytime.Count > 0)
                    return p.StylesAnytime[Util.Random(0, p.StylesAnytime.Count - 1)];
            }

            return null;
        }

        /// <summary>
		/// Starts a melee attack with this player
		/// </summary>
		/// <param name="attackTarget">the target to attack</param>
		public void StartAttack(GameObject attackTarget)
        {
            var p = owner as GamePlayer;

            if (p != null)
            {
                if (p.CharacterClass.StartAttack(attackTarget) == false)
                {
                    return;
                }

                if (!p.IsAlive)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouCantCombat"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                // Necromancer with summoned pet cannot attack
                if (p.ControlledBrain != null)
                    if (p.ControlledBrain.Body != null)
                        if (p.ControlledBrain.Body is NecromancerPet)
                        {
                            p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CantInShadeMode"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            return;
                        }

                if (p.IsStunned)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CantAttackStunned"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }
                if (p.IsMezzed)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CantAttackmesmerized"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                long vanishTimeout = p.TempProperties.getProperty<long>(VanishEffect.VANISH_BLOCK_ATTACK_TIME_KEY);
                if (vanishTimeout > 0 && vanishTimeout > GameLoop.GameLoopTime)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouMustWaitAgain", (vanishTimeout - GameLoop.GameLoopTime + 1000) / 1000), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                long VanishTick = p.TempProperties.getProperty<long>(VanishEffect.VANISH_BLOCK_ATTACK_TIME_KEY);
                long changeTime = GameLoop.GameLoopTime - VanishTick;
                if (changeTime < 30000 && VanishTick > 0)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouMustWait", ((30000 - changeTime) / 1000).ToString()), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (p.IsOnHorse)
                    p.IsOnHorse = false;

                if (p.IsDisarmed)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CantDisarmed"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (p.IsSitting)
                {
                    p.Sit(false);
                }
                if (p.AttackWeapon == null)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CannotWithoutWeapon"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }
                if (p.AttackWeapon.Object_Type == (int)eObjectType.Instrument)
                {
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CannotMelee"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == false)
                    {
                        if ((eCharacterClass)p.CharacterClass.ID == eCharacterClass.Scout || (eCharacterClass)p.CharacterClass.ID == eCharacterClass.Hunter || (eCharacterClass)p.CharacterClass.ID == eCharacterClass.Ranger)
                        {
                            // There is no feedback on live when attempting to fire a bow with arrows
                            return;
                        }
                    }

                    // Check arrows for ranged attack
                    if (p.rangeAttackComponent?.RangeAttackAmmo == null)
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.SelectQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }
                    // Check if selected ammo is compatible for ranged attack
                    if (!p.rangeAttackComponent.CheckRangedAmmoCompatibilityWithActiveWeapon())
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CantUseQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    lock (p.EffectList)
                    {
                        foreach (IGameEffect effect in p.EffectList) // switch to the correct range attack type
                        {
                            if (effect is SureShotEffect)
                            {
                                p.rangeAttackComponent.RangedAttackType = RangeAttackComponent.eRangedAttackType.SureShot;
                                break;
                            }

                            if (effect is RapidFireEffect)
                            {
                                p.rangeAttackComponent.RangedAttackType = RangeAttackComponent.eRangedAttackType.RapidFire;
                                break;
                            }

                            if (effect is TrueshotEffect)
                            {
                                p.rangeAttackComponent.RangedAttackType = RangeAttackComponent.eRangedAttackType.Long;
                                break;
                            }
                        }
                    }

                    if (p.rangeAttackComponent?.RangedAttackType == RangeAttackComponent.eRangedAttackType.Critical && p.Endurance < RangeAttackComponent.CRITICAL_SHOT_ENDURANCE)
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.TiredShot"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (p.Endurance < RangeAttackComponent.RANGE_ATTACK_ENDURANCE)
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.TiredUse", p.AttackWeapon.Name), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (p.IsStealthed)
                    {
                        // -Chance to unstealth while nocking an arrow = stealth spec / level
                        // -Chance to unstealth nocking a crit = stealth / level  0.20
                        int stealthSpec = p.GetModifiedSpecLevel(Specs.Stealth);
                        int stayStealthed = stealthSpec * 100 / p.Level;
                        if (p.rangeAttackComponent?.RangedAttackType == RangeAttackComponent.eRangedAttackType.Critical)
                            stayStealthed -= 20;

                        if (!Util.Chance(stayStealthed))
                            p.Stealth(false);
                    }
                }
                else
                {
                    if (attackTarget == null)
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CombatNoTarget"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    else
                        if (attackTarget is GameNPC)
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CombatTarget",
                            attackTarget.GetName(0, false, p.Client.Account.Language, (attackTarget as GameNPC))), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CombatTarget", attackTarget.GetName(0, false)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    }
                }

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
                }
                LivingStartAttack(attackTarget);

                if (p.IsCasting && !p.castingComponent.spellHandler.Spell.Uninterruptible)
                {
                    p.StopCurrentSpellcast();
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.SpellCancelled"), eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                }

                //Clear styles
                NextCombatStyle = null;
                NextCombatBackupStyle = null;

                if (p.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                {
                    p.Out.SendAttackMode(p.AttackState);
                }
                else
                {
                    p.TempProperties.setProperty(RangeAttackComponent.RANGE_ATTACK_HOLD_START, 0L);

                    string typeMsg = "shot";
                    if (p.AttackWeapon.Object_Type == (int)eObjectType.Thrown)
                        typeMsg = "throw";

                    string targetMsg = "";
                    if (attackTarget != null)
                    {
                        if (p.IsWithinRadius(attackTarget, p.AttackRange))
                            targetMsg = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.TargetInRange");
                        else
                            targetMsg = LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.TargetOutOfRange");
                    }

                    int speed = p.AttackSpeed(p.AttackWeapon) / 100;
                    p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouPrepare", typeMsg, speed / 10, speed % 10, targetMsg), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                }
            }
            else if (owner is GameNPC)
            {
                NPCStartAttack(attackTarget);
            }
            else
            {
                LivingStartAttack(attackTarget);
            }
        }

        /// <summary>
		/// Starts a melee or ranged attack on a given target.
		/// </summary>
		/// <param name="attackTarget">The object to attack.</param>
		private void LivingStartAttack(GameObject attackTarget)
        {
            // Aredhel: Let the brain handle this, no need to call StartAttack
            // if the body can't do anything anyway.
            if (owner.IsIncapacitated)
                return;

            if (owner.IsEngaging)
                owner.CancelEngageEffect();

            owner.AttackState = true;

            int speed = owner.AttackSpeed(owner.AttackWeapon);

            if (speed > 0)
            {
                //m_attackAction = CreateAttackAction();
                attackAction = new AttackAction(owner);

                if (owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    // only start another attack action if we aren't already aiming to shoot
                    if (owner.rangeAttackComponent?.RangedAttackState != RangeAttackComponent.eRangedAttackState.Aim)
                    {
                        owner.rangeAttackComponent.RangedAttackState = RangeAttackComponent.eRangedAttackState.Aim;

                        foreach (GamePlayer player in owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                            player.Out.SendCombatAnimation(owner, null, (ushort)(owner.AttackWeapon == null ? 0 : owner.AttackWeapon.Model),
                                                           0x00, player.Out.BowPrepare, (byte)(speed / 100), 0x00, 0x00);

                        //m_attackAction.Start((RangedAttackType == eRangedAttackType.RapidFire) ? speed / 2 : speed);
                        attackAction.StartTime = (owner.rangeAttackComponent?.RangedAttackType == RangeAttackComponent.eRangedAttackType.RapidFire) ? speed / 2 : speed;

                    }
                }
                else
                {
                    //if (m_attackAction.TimeUntilElapsed < 500)
                    //	m_attackAction.Start(500);
                    if (attackAction.TimeUntilStart < 500)
                        attackAction.StartTime = 500;
                }
            }
        }

        /// <summary>
		/// Starts a melee attack on a target
		/// </summary>
		/// <param name="target">The object to attack</param>
		private void NPCStartAttack(GameObject target)
        {
            if (target == null)
                return;

            var p = owner as GameNPC;

            p.TargetObject = target;

            long lastTick = p.TempProperties.getProperty<long>(GameNPC.LAST_LOS_TICK_PROPERTY);

            if (ServerProperties.Properties.ALWAYS_CHECK_PET_LOS &&
                p.Brain != null &&
                p.Brain is IControlledBrain &&
                (target is GamePlayer || (target is GameNPC && (target as GameNPC).Brain != null && (target as GameNPC).Brain is IControlledBrain)))
            {
                GameObject lastTarget = (GameObject)p.TempProperties.getProperty<object>(GameNPC.LAST_LOS_TARGET_PROPERTY, null);
                if (lastTarget != null && lastTarget == target)
                {
                    if (lastTick != 0 && GameLoop.GameLoopTime - lastTick < ServerProperties.Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
                        return;
                }

                GamePlayer losChecker = null;
                if (target is GamePlayer)
                {
                    losChecker = target as GamePlayer;
                }
                else if (target is GameNPC && (target as GameNPC).Brain is IControlledBrain)
                {
                    losChecker = ((target as GameNPC).Brain as IControlledBrain).GetPlayerOwner();
                }
                else
                {
                    // try to find another player to use for checking line of site
                    foreach (GamePlayer player in p.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        losChecker = player;
                        break;
                    }
                }

                if (losChecker == null)
                {
                    return;
                }

                lock (p.LOS_LOCK)
                {
                    int count = p.TempProperties.getProperty<int>(GameNPC.NUM_LOS_CHECKS_INPROGRESS, 0);

                    if (count > 10)
                    {
                        GameNPC.log.DebugFormat("{0} LOS count check exceeds 10, aborting LOS check!", p.Name);

                        // Now do a safety check.  If it's been a while since we sent any check we should clear count
                        if (lastTick == 0 || GameLoop.GameLoopTime - lastTick > ServerProperties.Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
                        {
                            GameNPC.log.Debug("LOS count reset!");
                            p.TempProperties.setProperty(GameNPC.NUM_LOS_CHECKS_INPROGRESS, 0);
                        }

                        return;
                    }

                    count++;
                    p.TempProperties.setProperty(GameNPC.NUM_LOS_CHECKS_INPROGRESS, count);

                    p.TempProperties.setProperty(GameNPC.LAST_LOS_TARGET_PROPERTY, target);
                    p.TempProperties.setProperty(GameNPC.LAST_LOS_TICK_PROPERTY, GameLoop.GameLoopTime);
                    p.m_targetLOSObject = target;

                }

                losChecker.Out.SendCheckLOS(p, target, new CheckLOSResponse(p.NPCStartAttackCheckLOS));
                return;
            }

            ContinueStartAttack(target);
        }

        public virtual void ContinueStartAttack(GameObject target)
        {
            var p = owner as GameNPC;

            p.StopMoving();
            p.StopMovingOnPath();

            if (p.Brain != null && p.Brain is IControlledBrain)
            {
                if ((p.Brain as IControlledBrain).AggressionState == eAggressionState.Passive)
                    return;

                GamePlayer owner = null;

                if ((owner = ((IControlledBrain)p.Brain).GetPlayerOwner()) != null)
                    owner.Stealth(false);
            }

            p.SetLastMeleeAttackTick();
            p.StartMeleeAttackTimer();

            LivingStartAttack(target);

            if (p.AttackState)
            {
                // if we're moving we need to lock down the current position
                if (p.IsMoving)
                    p.SaveCurrentPosition();

                if (p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    // Archer mobs sometimes bug and keep trying to fire at max range unsuccessfully so force them to get just a tad closer.
                    p.Follow(target, p.AttackRange - 30, GameNPC.STICKMAXIMUMRANGE);
                }
                else
                {
                    p.Follow(target, GameNPC.STICKMINIMUMRANGE, GameNPC.STICKMAXIMUMRANGE);
                }
            }

        }



        /// <summary>
        /// Stops all attacks this player is making
        /// </summary>
        /// <param name="forced">Is this a forced stop or is the client suggesting we stop?</param>
        public void StopAttack(bool forced)
        {
            var p = owner as GamePlayer;

            if (p != null)
            {
                NextCombatStyle = null;
                NextCombatBackupStyle = null;
                LivingStopAttack(forced);
                if (p.IsAlive)
                {
                    p.Out.SendAttackMode(p.AttackState);
                }
            }
        }

        /// <summary>
        /// Stops all attacks this GameLiving is currently making.
        /// </summary>
        public void LivingStopAttack()
        {
            if (owner is GamePlayer)
            {
                StopAttack(true);
            }
            else
            {
                LivingStopAttack(true);
            }
        }

        /// <summary>
        /// Stop all attackes this GameLiving is currently making
        /// </summary>
        /// <param name="forced">Is this a forced stop or is the client suggesting we stop?</param>
        public void LivingStopAttack(bool forced)
        {
            owner.CancelEngageEffect();
            owner.AttackState = false;

            if (owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                owner.InterruptRangedAttack();
        }

        /// <summary>
		/// Stops all attack actions, including following target
		/// </summary>
		public void NPCStopAttack()
        {
            var p = owner as GameNPC;

            LivingStopAttack();
            p.StopFollowing();

            // Tolakram: If npc has a distance weapon it needs to be made active after attack is stopped
            if (p.Inventory != null && p.Inventory.GetItem(eInventorySlot.DistanceWeapon) != null && p.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                p.SwitchWeapon(eActiveWeaponSlot.Distance);
        }

        /// <summary>
        /// Called whenever a single attack strike is made
        /// </summary>
        /// <param name="target"></param>
        /// <param name="weapon"></param>
        /// <param name="style"></param>
        /// <param name="effectiveness"></param>
        /// <param name="interruptDuration"></param>
        /// <param name="dualWield"></param>
        /// <returns></returns>
        public AttackData MakeAttack(GameObject target, InventoryItem weapon, Style style, double effectiveness, int interruptDuration, bool dualWield)
        {
            var p = owner as GamePlayer;

            if (p.IsCrafting)
            {
                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                p.CraftTimer.Stop();
                p.CraftTimer = null;
                p.Out.SendCloseTimerWindow();
            }

            AttackData ad = LivingMakeAttack(target, weapon, style, effectiveness * p.Effectiveness * (1 + p.CharacterClass.WeaponSkillBase / 20.0 / 100.0), interruptDuration, dualWield);

            //Clear the styles for the next round!
            NextCombatStyle = null;
            NextCombatBackupStyle = null;

            switch (ad.AttackResult)
            {
                case eAttackResult.HitStyle:
                case eAttackResult.HitUnstyled:
                    {
                        //keep component
                        if ((ad.Target is GameKeepComponent || ad.Target is GameKeepDoor || ad.Target is GameSiegeWeapon) && ad.Attacker is GamePlayer && ad.Attacker.GetModified(eProperty.KeepDamage) > 0)
                        {
                            int keepdamage = (int)Math.Floor((double)ad.Damage * ((double)ad.Attacker.GetModified(eProperty.KeepDamage) / 100));
                            int keepstyle = (int)Math.Floor((double)ad.StyleDamage * ((double)ad.Attacker.GetModified(eProperty.KeepDamage) / 100));
                            ad.Damage += keepdamage;
                            ad.StyleDamage += keepstyle;
                        }
                        // vampiir
                        if (p.CharacterClass is PlayerClass.ClassVampiir
                            && target is GameKeepComponent == false
                            && target is GameKeepDoor == false
                            && target is GameSiegeWeapon == false)
                        {
                            int perc = Convert.ToInt32(((double)(ad.Damage + ad.CriticalDamage) / 100) * (55 - p.Level));
                            perc = (perc < 1) ? 1 : ((perc > 15) ? 15 : perc);
                            p.Mana += Convert.ToInt32(Math.Ceiling(((Decimal)(perc * p.MaxMana) / 100)));
                        }

                        //only miss when strafing when attacking a player
                        //30% chance to miss
                        if (p.IsStrafing && ad.Target is GamePlayer && Util.Chance(30))
                        {
                            ad.AttackResult = eAttackResult.Missed;
                            p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.StrafMiss"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
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
                    //Condition percent can reach 70%
                    //durability percent can reach zero
                    // if item durability reachs 0, item is useless and become broken item

                    if (weapon != null && weapon is GameInventoryItem)
                    {
                        (weapon as GameInventoryItem).OnStrikeTarget(p, target);
                    }
                    //Camouflage - Camouflage will be disabled only when attacking a GamePlayer or ControlledNPC of a GamePlayer.
                    if (p.HasAbility(Abilities.Camouflage) && target is GamePlayer || (target is GameNPC && (target as GameNPC).Brain is IControlledBrain && ((target as GameNPC).Brain as IControlledBrain).GetPlayerOwner() != null))
                    {
                        CamouflageEffect camouflage = p.EffectList.GetOfType<CamouflageEffect>();

                        if (camouflage != null)// Check if Camo is active, if true, cancel ability.
                        {
                            camouflage.Cancel(false);
                        }
                        Skill camo = SkillBase.GetAbility(Abilities.Camouflage); // now we find the ability
                        p.DisableSkill(camo, CamouflageSpecHandler.DISABLE_DURATION); // and here we disable it.
                    }

                    // Multiple Hit check
                    if (ad.AttackResult == eAttackResult.HitStyle)
                    {
                        byte numTargetsCanHit = 0;
                        int random;
                        IList extraTargets = new ArrayList();
                        IList listAvailableTargets = new ArrayList();
                        InventoryItem attackWeapon = p.AttackWeapon;
                        InventoryItem leftWeapon = (p.Inventory == null) ? null : p.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
                        switch (style.ID)
                        {
                            case 374: numTargetsCanHit = 1; break; //Tribal Assault:   Hits 2 targets
                            case 377: numTargetsCanHit = 1; break; //Clan's Might:      Hits 2 targets
                            case 379: numTargetsCanHit = 2; break; //Totemic Wrath:      Hits 3 targets
                            case 384: numTargetsCanHit = 3; break; //Totemic Sacrifice:   Hits 4 targets
                            case 600: numTargetsCanHit = 255; break; //Shield Swipe: No Cap on Targets
                            default: numTargetsCanHit = 0; break; //For others;
                        }
                        if (numTargetsCanHit > 0)
                        {
                            if (style.ID != 600) // Not Shield Swipe
                            {
                                foreach (GamePlayer pl in p.GetPlayersInRadius(false, (ushort)p.AttackRange))
                                {
                                    if (pl == null) continue;
                                    if (GameServer.ServerRules.IsAllowedToAttack(p, pl, true))
                                    {
                                        listAvailableTargets.Add(pl);
                                    }
                                }
                                foreach (GameNPC npc in p.GetNPCsInRadius(false, (ushort)p.AttackRange))
                                {
                                    if (GameServer.ServerRules.IsAllowedToAttack(p, npc, true))
                                    {
                                        listAvailableTargets.Add(npc);
                                    }
                                }

                                // remove primary target
                                listAvailableTargets.Remove(target);
                                numTargetsCanHit = (byte)Math.Min(numTargetsCanHit, listAvailableTargets.Count);

                                if (listAvailableTargets.Count > 1)
                                {
                                    while (extraTargets.Count < numTargetsCanHit)
                                    {
                                        random = Util.Random(listAvailableTargets.Count - 1);
                                        if (!extraTargets.Contains(listAvailableTargets[random]))
                                            extraTargets.Add(listAvailableTargets[random] as GameObject);
                                    }
                                    foreach (GameObject obj in extraTargets)
                                    {
                                        if (obj is GamePlayer && ((GamePlayer)obj).IsSitting)
                                        {
                                            effectiveness *= 2;
                                        }
                                        //new WeaponOnTargetAction(this, obj as GameObject, attackWeapon, leftWeapon, effectiveness, AttackSpeed(attackWeapon), null).Start(1);  // really start the attack
                                        //if (GameServer.ServerRules.IsAllowedToAttack(this, target as GameLiving, false))
                                        weaponAction = new WeaponAction(p, obj as GameObject, attackWeapon, leftWeapon, effectiveness, p.AttackSpeed(attackWeapon), null, 1000);
                                    }
                                }
                            }
                            else // shield swipe
                            {
                                foreach (GameNPC npc in p.GetNPCsInRadius(false, (ushort)p.AttackRange))
                                {
                                    if (GameServer.ServerRules.IsAllowedToAttack(p, npc, true))
                                    {
                                        listAvailableTargets.Add(npc);
                                    }
                                }

                                listAvailableTargets.Remove(target);
                                numTargetsCanHit = (byte)Math.Min(numTargetsCanHit, listAvailableTargets.Count);

                                if (listAvailableTargets.Count > 1)
                                {
                                    while (extraTargets.Count < numTargetsCanHit)
                                    {
                                        random = Util.Random(listAvailableTargets.Count - 1);
                                        if (!extraTargets.Contains(listAvailableTargets[random]))
                                        {
                                            extraTargets.Add(listAvailableTargets[random] as GameObject);
                                        }
                                    }
                                    foreach (GameNPC obj in extraTargets)
                                    {
                                        if (obj != ad.Target)
                                        {
                                            LivingMakeAttack(obj, attackWeapon, null, 1, ServerProperties.Properties.SPELL_INTERRUPT_DURATION, false, false);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
            return ad;
        }

        /// <summary>
        /// This method is called to make an attack, it is called from the
        /// attacktimer and should not be called manually
        /// </summary>
        /// <param name="target">the target that is attacked</param>
        /// <param name="weapon">the weapon used for attack</param>
        /// <param name="style">the style used for attack</param>
        /// <param name="effectiveness">damage effectiveness (0..1)</param>
        /// <param name="interruptDuration">the interrupt duration</param>
        /// <param name="dualWield">indicates if both weapons are used for attack</param>
        /// <returns>the object where we collect and modifiy all parameters about the attack</returns>
        public AttackData LivingMakeAttack(GameObject target, InventoryItem weapon, Style style, double effectiveness, int interruptDuration, bool dualWield)
        {
            return LivingMakeAttack(target, weapon, style, effectiveness, interruptDuration, dualWield, false);
        }


        public AttackData LivingMakeAttack(GameObject target, InventoryItem weapon, Style style, double effectiveness, int interruptDuration, bool dualWield, bool ignoreLOS)
        {
            AttackData ad = new AttackData();
            ad.Attacker = owner;
            ad.Target = target as GameLiving;
            ad.Damage = 0;
            ad.CriticalDamage = 0;
            ad.Style = style;
            ad.WeaponSpeed = owner.AttackSpeed(weapon) / 100;
            ad.DamageType = owner.AttackDamageType(weapon);
            ad.ArmorHitLocation = eArmorSlot.NOTSET;
            ad.Weapon = weapon;
            ad.IsOffHand = weapon == null ? false : weapon.Hand == 2;


            if (dualWield)
                ad.AttackType = AttackData.eAttackType.MeleeDualWield;
            else if (weapon == null)
                ad.AttackType = AttackData.eAttackType.MeleeOneHand;
            else switch (weapon.Item_Type)
                {
                    default:
                    case Slot.RIGHTHAND:
                    case Slot.LEFTHAND: ad.AttackType = AttackData.eAttackType.MeleeOneHand; break;
                    case Slot.TWOHAND: ad.AttackType = AttackData.eAttackType.MeleeTwoHand; break;
                    case Slot.RANGED: ad.AttackType = AttackData.eAttackType.Ranged; break;
                }

            //No target, stop the attack
            if (ad.Target == null)
            {
                ad.AttackResult = (target == null) ? eAttackResult.NoTarget : eAttackResult.NoValidTarget;
                return ad;
            }

            // check region
            if (ad.Target.CurrentRegionID != owner.CurrentRegionID || ad.Target.ObjectState != eObjectState.Active)
            {
                ad.AttackResult = eAttackResult.NoValidTarget;
                return ad;
            }

            //Check if the target is in front of attacker
            if (!ignoreLOS && ad.AttackType != AttackData.eAttackType.Ranged && owner is GamePlayer &&
                !(ad.Target is GameKeepComponent) && !(owner.IsObjectInFront(ad.Target, 120, true) && owner.TargetInView))
            {
                ad.AttackResult = eAttackResult.TargetNotVisible;
                return ad;
            }

            //Target is dead already
            if (!ad.Target.IsAlive)
            {
                ad.AttackResult = eAttackResult.TargetDead;
                return ad;
            }
            //We have no attacking distance!
            if (!owner.IsWithinRadius(ad.Target, ad.Target.ActiveWeaponSlot == eActiveWeaponSlot.Standard ? Math.Max(owner.AttackRange, ad.Target.AttackRange) : owner.AttackRange))
            {
                ad.AttackResult = eAttackResult.OutOfRange;
                return ad;
            }

            if (owner.rangeAttackComponent?.RangedAttackType == RangeAttackComponent.eRangedAttackType.Long)
            {
                owner.rangeAttackComponent.RangedAttackType = RangeAttackComponent.eRangedAttackType.Normal;
            }

            if (!GameServer.ServerRules.IsAllowedToAttack(ad.Attacker, ad.Target, false))
            {
                ad.AttackResult = eAttackResult.NotAllowed_ServerRules;
                return ad;
            }

            if (SpellHandler.FindEffectOnTarget(owner, "Phaseshift") != null)
            {
                ad.AttackResult = eAttackResult.Phaseshift;
                return ad;
            }

            // Apply Mentalist RA5L
            SelectiveBlindnessEffect SelectiveBlindness = owner.EffectList.GetOfType<SelectiveBlindnessEffect>();
            if (SelectiveBlindness != null)
            {
                GameLiving EffectOwner = SelectiveBlindness.EffectSource;
                if (EffectOwner == ad.Target)
                {
                    if (owner is GamePlayer)
                        ((GamePlayer)owner).Out.SendMessage(string.Format(LanguageMgr.GetTranslation(((GamePlayer)owner).Client.Account.Language, "GameLiving.AttackData.InvisibleToYou"), ad.Target.GetName(0, true)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                    ad.AttackResult = eAttackResult.NoValidTarget;
                    return ad;
                }
            }

            // DamageImmunity Ability
            if ((GameLiving)target != null && ((GameLiving)target).HasAbility(Abilities.DamageImmunity))
            {
                //if (ad.Attacker is GamePlayer) ((GamePlayer)ad.Attacker).Out.SendMessage(string.Format("{0} can't be attacked!", ad.Target.GetName(0, true)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                ad.AttackResult = eAttackResult.NoValidTarget;
                return ad;
            }


            //Calculate our attack result and attack damage
            ad.AttackResult = ad.Target.CalculateEnemyAttackResult(ad, weapon);

            // calculate damage only if we hit the target
            if (ad.AttackResult == eAttackResult.HitUnstyled
                || ad.AttackResult == eAttackResult.HitStyle)
            {
                double damage = owner.AttackDamage(weapon) * effectiveness;

                if (owner.Level > ServerProperties.Properties.MOB_DAMAGE_INCREASE_STARTLEVEL &&
                    ServerProperties.Properties.MOB_DAMAGE_INCREASE_PERLEVEL > 0 &&
                    damage > 0 &&
                    owner is GameNPC && (owner as GameNPC).Brain is IControlledBrain == false)
                {
                    double modifiedDamage = ServerProperties.Properties.MOB_DAMAGE_INCREASE_PERLEVEL * (owner.Level - ServerProperties.Properties.MOB_DAMAGE_INCREASE_STARTLEVEL);
                    damage += (modifiedDamage * effectiveness);
                }

                InventoryItem armor = null;

                if (ad.Target.Inventory != null)
                    armor = ad.Target.Inventory.GetItem((eInventorySlot)ad.ArmorHitLocation);

                InventoryItem weaponTypeToUse = null;

                if (weapon != null)
                {
                    weaponTypeToUse = new InventoryItem();
                    weaponTypeToUse.Object_Type = weapon.Object_Type;
                    weaponTypeToUse.SlotPosition = weapon.SlotPosition;

                    if ((owner is GamePlayer) && owner.Realm == eRealm.Albion
                        && (GameServer.ServerRules.IsObjectTypesEqual((eObjectType)weapon.Object_Type, eObjectType.TwoHandedWeapon)
                        || GameServer.ServerRules.IsObjectTypesEqual((eObjectType)weapon.Object_Type, eObjectType.PolearmWeapon))
                        && ServerProperties.Properties.ENABLE_ALBION_ADVANCED_WEAPON_SPEC)
                    {
                        // Albion dual spec penalty, which sets minimum damage to the base damage spec
                        if (weapon.Type_Damage == (int)eDamageType.Crush)
                        {
                            weaponTypeToUse.Object_Type = (int)eObjectType.CrushingWeapon;
                        }
                        else if (weapon.Type_Damage == (int)eDamageType.Slash)
                        {
                            weaponTypeToUse.Object_Type = (int)eObjectType.SlashingWeapon;
                        }
                        else
                        {
                            weaponTypeToUse.Object_Type = (int)eObjectType.ThrustWeapon;
                        }
                    }
                }

                int lowerboundary = (owner.WeaponSpecLevel(weaponTypeToUse) - 1) * 50 / (ad.Target.EffectiveLevel + 1) + 75;
                lowerboundary = Math.Max(lowerboundary, 75);
                lowerboundary = Math.Min(lowerboundary, 125);
                damage *= (owner.GetWeaponSkill(weapon) + 90.68) / (ad.Target.GetArmorAF(ad.ArmorHitLocation) + 20 * 4.67);

                // Badge Of Valor Calculation 1+ absorb or 1- absorb
                if (ad.Attacker.EffectList.GetOfType<BadgeOfValorEffect>() != null)
                {
                    damage *= 1.0 + Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
                }
                else
                {
                    damage *= 1.0 - Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
                }
                damage *= (lowerboundary + Util.Random(50)) * 0.01;
                ad.Modifier = (int)(damage * (ad.Target.GetResist(ad.DamageType) + SkillBase.GetArmorResist(armor, ad.DamageType)) * -0.01);
                //damage += ad.Modifier;
                // RA resist check
                int resist = (int)(damage * ad.Target.GetDamageResist(owner.GetResistTypeForDamage(ad.DamageType)) * -0.01);

                eProperty property = ad.Target.GetResistTypeForDamage(ad.DamageType);
                int secondaryResistModifier = ad.Target.SpecBuffBonusCategory[(int)property];
                int resistModifier = 0;
                resistModifier += (int)((ad.Damage + (double)resistModifier) * (double)secondaryResistModifier * -0.01);

                damage += resist;
                damage += resistModifier;
                ad.Modifier += resist;
                damage += ad.Modifier;
                ad.Damage = (int)damage;

                // apply total damage cap
                ad.UncappedDamage = ad.Damage;
                ad.Damage = Math.Min(ad.Damage, (int)(owner.UnstyledDamageCap(weapon)/* * effectiveness*/));

                if ((owner is GamePlayer || (owner is GameNPC && (owner as GameNPC).Brain is IControlledBrain && owner.Realm != 0)) && target is GamePlayer)
                {
                    ad.Damage = (int)((double)ad.Damage * ServerProperties.Properties.PVP_MELEE_DAMAGE);
                }
                else if ((owner is GamePlayer || (owner is GameNPC && (owner as GameNPC).Brain is IControlledBrain && owner.Realm != 0)) && target is GameNPC)
                {
                    ad.Damage = (int)((double)ad.Damage * ServerProperties.Properties.PVE_MELEE_DAMAGE);
                }

                ad.UncappedDamage = ad.Damage;

                //Eden - Conversion Bonus (Crocodile Ring)  - tolakram - critical damage is always 0 here, needs to be moved
                if (ad.Target is GamePlayer && ad.Target.GetModified(eProperty.Conversion) > 0)
                {
                    int manaconversion = (int)Math.Round(((double)ad.Damage + (double)ad.CriticalDamage) * (double)ad.Target.GetModified(eProperty.Conversion) / 100);
                    //int enduconversion=(int)Math.Round((double)manaconversion*(double)ad.Target.MaxEndurance/(double)ad.Target.MaxMana);
                    int enduconversion = (int)Math.Round(((double)ad.Damage + (double)ad.CriticalDamage) * (double)ad.Target.GetModified(eProperty.Conversion) / 100);
                    if (ad.Target.Mana + manaconversion > ad.Target.MaxMana) manaconversion = ad.Target.MaxMana - ad.Target.Mana;
                    if (ad.Target.Endurance + enduconversion > ad.Target.MaxEndurance) enduconversion = ad.Target.MaxEndurance - ad.Target.Endurance;
                    if (manaconversion < 1) manaconversion = 0;
                    if (enduconversion < 1) enduconversion = 0;
                    if (manaconversion >= 1) (ad.Target as GamePlayer).Out.SendMessage(string.Format(LanguageMgr.GetTranslation((ad.Target as GamePlayer).Client.Account.Language, "GameLiving.AttackData.GainPowerPoints"), manaconversion), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    if (enduconversion >= 1) (ad.Target as GamePlayer).Out.SendMessage(string.Format(LanguageMgr.GetTranslation((ad.Target as GamePlayer).Client.Account.Language, "GameLiving.AttackData.GainEndurancePoints"), enduconversion), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
                    ad.Target.Endurance += enduconversion; if (ad.Target.Endurance > ad.Target.MaxEndurance) ad.Target.Endurance = ad.Target.MaxEndurance;
                    ad.Target.Mana += manaconversion; if (ad.Target.Mana > ad.Target.MaxMana) ad.Target.Mana = ad.Target.MaxMana;
                }

                // Tolakram - let's go ahead and make it 1 damage rather than spamming a possible error
                if (ad.Damage == 0)
                {
                    ad.Damage = 1;

                    // log this as a possible error if we should do some damage to target
                    //if (ad.Target.Level <= Level + 5 && weapon != null)
                    //{
                    //    log.ErrorFormat("Possible Damage Error: {0} Damage = 0 -> miss vs {1}.  AttackDamage {2}, weapon name {3}", Name, (ad.Target == null ? "null" : ad.Target.Name), AttackDamage(weapon), (weapon == null ? "None" : weapon.Name));
                    //}

                    //ad.AttackResult = eAttackResult.Missed;
                }
            }

            //Add styled damage if style hits and remove endurance if missed
            if (StyleProcessor.ExecuteStyle(owner, ad, weapon))
            {
                ad.AttackResult = GameLiving.eAttackResult.HitStyle;
            }

            if ((ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
            {
                ad.CriticalDamage = owner.GetMeleeCriticalDamage(ad, weapon);
            }

            // Attacked living may modify the attack data.  Primarily used for keep doors and components.
            ad.Target.ModifyAttack(ad);

            if (ad.AttackResult == eAttackResult.HitStyle)
            {
                if (owner is GamePlayer)
                {
                    GamePlayer player = owner as GamePlayer;

                    string damageAmount = (ad.StyleDamage > 0) ? " (+" + ad.StyleDamage + ")" : "";
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "StyleProcessor.ExecuteStyle.PerformPerfectly", ad.Style.Name, damageAmount), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                }
                else if (owner is GameNPC)
                {
                    ControlledNpcBrain brain = ((GameNPC)owner).Brain as ControlledNpcBrain;

                    if (brain != null)
                    {
                        GamePlayer owner = brain.GetPlayerOwner();
                        if (owner != null)
                        {
                            string damageAmount = (ad.StyleDamage > 0) ? " (+" + ad.StyleDamage + ")" : "";
                            owner.Out.SendMessage(LanguageMgr.GetTranslation(owner.Client.Account.Language, "StyleProcessor.ExecuteStyle.PerformsPerfectly", owner.Name, ad.Style.Name, damageAmount), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        }
                    }
                }
            }

            string message = "";
            bool broadcast = true;
            ArrayList excludes = new ArrayList();
            excludes.Add(ad.Attacker);
            excludes.Add(ad.Target);

            switch (ad.AttackResult)
            {
                case eAttackResult.Parried: message = string.Format("{0} attacks {1} and is parried!", ad.Attacker.GetName(0, true), ad.Target.GetName(0, false)); break;
                case eAttackResult.Evaded: message = string.Format("{0} attacks {1} and is evaded!", ad.Attacker.GetName(0, true), ad.Target.GetName(0, false)); break;
                case eAttackResult.Missed: message = string.Format("{0} attacks {1} and misses!", ad.Attacker.GetName(0, true), ad.Target.GetName(0, false)); break;

                case eAttackResult.Blocked:
                    {
                        message = string.Format("{0} attacks {1} and is blocked!", ad.Attacker.GetName(0, true), ad.Target.GetName(0, false));
                        // guard messages
                        if (target != null && target != ad.Target)
                        {
                            excludes.Add(target);

                            // another player blocked for real target
                            if (target is GamePlayer)
                                ((GamePlayer)target).Out.SendMessage(string.Format(LanguageMgr.GetTranslation(((GamePlayer)target).Client.Account.Language, "GameLiving.AttackData.BlocksYou"), ad.Target.GetName(0, true), ad.Attacker.GetName(0, false)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);

                            // blocked for another player
                            if (ad.Target is GamePlayer)
                            {
                                ((GamePlayer)ad.Target).Out.SendMessage(string.Format(LanguageMgr.GetTranslation(((GamePlayer)ad.Target).Client.Account.Language, "GameLiving.AttackData.YouBlock"), ad.Attacker.GetName(0, false), target.GetName(0, false)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                                ((GamePlayer)ad.Target).Stealth(false);
                            }
                        }
                        else if (ad.Target is GamePlayer)
                        {
                            ((GamePlayer)ad.Target).Out.SendMessage(string.Format(LanguageMgr.GetTranslation(((GamePlayer)ad.Target).Client.Account.Language, "GameLiving.AttackData.AttacksYou"), ad.Attacker.GetName(0, true)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                        }
                        break;
                    }
                case eAttackResult.HitUnstyled:
                case eAttackResult.HitStyle:
                    {
                        if (target != null && target != ad.Target)
                        {
                            message = string.Format("{0} attacks {1} but hits {2}!", ad.Attacker.GetName(0, true), target.GetName(0, false), ad.Target.GetName(0, false));
                            excludes.Add(target);

                            // intercept for another player
                            if (target is GamePlayer)
                                ((GamePlayer)target).Out.SendMessage(string.Format(LanguageMgr.GetTranslation(((GamePlayer)target).Client.Account.Language, "GameLiving.AttackData.StepsInFront"), ad.Target.GetName(0, true)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);

                            // intercept by player
                            if (ad.Target is GamePlayer)
                                ((GamePlayer)ad.Target).Out.SendMessage(string.Format(LanguageMgr.GetTranslation(((GamePlayer)ad.Target).Client.Account.Language, "GameLiving.AttackData.YouStepInFront"), target.GetName(0, false)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        }
                        else
                        {
                            if (ad.Attacker is GamePlayer)
                            {
                                string hitWeapon = "weapon";
                                if (weapon != null)
                                    hitWeapon = GlobalConstants.NameToShortName(weapon.Name);
                                message = string.Format("{0} attacks {1} with {2} {3}!", ad.Attacker.GetName(0, true), ad.Target.GetName(0, false), ad.Attacker.GetPronoun(1, false), hitWeapon);
                            }
                            else
                            {
                                message = string.Format("{0} attacks {1} and hits!", ad.Attacker.GetName(0, true), ad.Target.GetName(0, false));
                            }
                        }
                        break;
                    }
                default: broadcast = false; break;
            }

            #region Prevent Flight
            if (ad.Attacker is GamePlayer)
            {
                GamePlayer attacker = ad.Attacker as GamePlayer;
                if (attacker.HasAbility(Abilities.PreventFlight) && Util.Chance(10))
                {
                    if (owner.IsObjectInFront(ad.Target, 120) && ad.Target.IsMoving)
                    {
                        bool preCheck = false;
                        if (ad.Target is GamePlayer) //only start if we are behind the player
                        {
                            float angle = ad.Target.GetAngle(ad.Attacker);
                            if (angle >= 150 && angle < 210) preCheck = true;
                        }
                        else preCheck = true;

                        if (preCheck)
                        {
                            Spell spell = SkillBase.GetSpellByID(7083);
                            if (spell != null)
                            {
                                ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(owner, spell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
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

            #region controlled messages

            if (ad.Attacker is GameNPC)
            {
                IControlledBrain brain = ((GameNPC)ad.Attacker).Brain as IControlledBrain;
                if (brain != null)
                {
                    GamePlayer owner = brain.GetPlayerOwner();
                    if (owner != null)
                    {
                        excludes.Add(owner);
                        switch (ad.AttackResult)
                        {
                            case eAttackResult.HitStyle:
                            case eAttackResult.HitUnstyled:
                                {
                                    string modmessage = "";
                                    if (ad.Modifier > 0) modmessage = " (+" + ad.Modifier + ")";
                                    if (ad.Modifier < 0) modmessage = " (" + ad.Modifier + ")";
                                    string attackTypeMsg = "attacks";
                                    if (ad.Attacker.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                                    {
                                        attackTypeMsg = "shoots";
                                    }
                                    owner.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(owner.Client.Account.Language, "GameLiving.AttackData.YourHits"), ad.Attacker.Name, attackTypeMsg, ad.Target.GetName(0, false), ad.Damage, modmessage), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                                    if (ad.CriticalDamage > 0)
                                    {
                                        owner.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(owner.Client.Account.Language, "GameLiving.AttackData.YourCriticallyHits"), ad.Attacker.Name, ad.Target.GetName(0, false), ad.CriticalDamage), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                                    }

                                    break;
                                }
                            default:
                                owner.Out.SendMessage(message, eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                                break;
                        }
                    }
                }
            }

            if (ad.Target is GameNPC)
            {
                IControlledBrain brain = ((GameNPC)ad.Target).Brain as IControlledBrain;
                if (brain != null)
                {
                    GameLiving owner_living = brain.GetLivingOwner();
                    excludes.Add(owner_living);
                    if (owner_living != null && owner_living is GamePlayer && owner_living.ControlledBrain != null && ad.Target == owner_living.ControlledBrain.Body)
                    {
                        GamePlayer owner = owner_living as GamePlayer;
                        switch (ad.AttackResult)
                        {
                            case eAttackResult.Blocked:
                                owner.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(owner.Client.Account.Language, "GameLiving.AttackData.Blocked"), ad.Attacker.GetName(0, true), ad.Target.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                                break;
                            case eAttackResult.Parried:
                                owner.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(owner.Client.Account.Language, "GameLiving.AttackData.Parried"), ad.Attacker.GetName(0, true), ad.Target.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                                break;
                            case eAttackResult.Evaded:
                                owner.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(owner.Client.Account.Language, "GameLiving.AttackData.Evaded"), ad.Attacker.GetName(0, true), ad.Target.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                                break;
                            case eAttackResult.Fumbled:
                                owner.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(owner.Client.Account.Language, "GameLiving.AttackData.Fumbled"), ad.Attacker.GetName(0, true)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                                break;
                            case eAttackResult.Missed:
                                if (ad.AttackType != AttackData.eAttackType.Spell)
                                    owner.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(owner.Client.Account.Language, "GameLiving.AttackData.Misses"), ad.Attacker.GetName(0, true), ad.Target.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                                break;
                            case eAttackResult.HitStyle:
                            case eAttackResult.HitUnstyled:
                                {
                                    string modmessage = "";
                                    if (ad.Modifier > 0) modmessage = " (+" + ad.Modifier + ")";
                                    if (ad.Modifier < 0) modmessage = " (" + ad.Modifier + ")";
                                    owner.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(owner.Client.Account.Language, "GameLiving.AttackData.HitsForDamage"), ad.Attacker.GetName(0, true), ad.Target.Name, ad.Damage, modmessage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                                    if (ad.CriticalDamage > 0)
                                    {
                                        owner.Out.SendMessage(string.Format(LanguageMgr.GetTranslation(owner.Client.Account.Language, "GameLiving.AttackData.CriticallyHitsForDamage"), ad.Attacker.GetName(0, true), ad.Target.Name, ad.CriticalDamage), eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                                    }
                                    break;
                                }
                            default: break;
                        }
                    }
                }
            }

            #endregion

            // broadcast messages
            if (broadcast)
            {
                Message.SystemToArea(ad.Attacker, message, eChatType.CT_OthersCombat, (GameObject[])excludes.ToArray(typeof(GameObject)));
            }

            ad.Target.StartInterruptTimer(ad, interruptDuration);
            //Return the result
            return ad;
        }
    }
}
