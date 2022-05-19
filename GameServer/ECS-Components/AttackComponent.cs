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
using DOL.GS.PropertyCalc;
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
        /// The objects currently attacking this living
        /// To be more exact, the objects that are in combat
        /// and have this living as target.
        /// </summary>
        protected List<GameObject> m_attackers;


        /// <summary>
        /// Returns the list of attackers
        /// </summary>
        public List<GameObject> Attackers
        {
            get { return m_attackers; }
        }

        /// <summary>
        /// Adds an attacker to the attackerlist
        /// </summary>
        /// <param name="attacker">the attacker to add</param>
        public void AddAttacker(GameObject attacker)
        {
            lock (Attackers)
            {
                if (attacker == owner) return;
                if (m_attackers.Contains(attacker)) return;
                m_attackers.Add(attacker);

                if (m_attackers.Count() > 0 &&
                    !EntityManager.GetLivingByComponent(typeof(AttackComponent)).Contains(owner))
                    EntityManager.AddComponent(typeof(AttackComponent), owner);
            }
        }

        /// <summary>
        /// Removes an attacker from the list
        /// </summary>
        /// <param name="attacker">the attacker to remove</param>
        public void RemoveAttacker(GameObject attacker)
        {
            //			log.Warn(Name + ": RemoveAttacker "+attacker.Name);
            //			log.Error(Environment.StackTrace);
            lock (Attackers)
            {
                if (m_attackers.Contains(attacker)) m_attackers.Remove(attacker);

                //if (m_attackers.Count() == 0)
                //    EntityManager.RemoveComponent(typeof(AttackComponent), owner);
            }
        }

        public AttackComponent(GameLiving owner)
        {
            this.owner = owner;
            m_attackers = new List<GameObject>();
        }

        public void Tick(long time)
        {
            if (attackAction != null)
            {
                attackAction.Tick(time);
            }

            if (weaponAction != null)
            {
                if (weaponAction.AttackFinished)
                    weaponAction = null;
            }

            if (weaponAction is null && attackAction is null && !owner.InCombat)
            {
                if (EntityManager.GetLivingByComponent(typeof(AttackComponent)).ToArray().Contains(owner))
                    EntityManager.RemoveComponent(typeof(AttackComponent), owner);
            }
        }


        //      /// <summary>
        ///// The result of an attack
        ///// </summary>
        //public enum eAttackResult : int
        //      {
        //          /// <summary>
        //          /// No specific attack
        //          /// </summary>
        //          Any = 0,
        //          /// <summary>
        //          /// The attack was a hit
        //          /// </summary>
        //          HitUnstyled = 1,
        //          /// <summary>
        //          /// The attack was a hit
        //          /// </summary>
        //          HitStyle = 2,
        //          /// <summary>
        //          /// Attack was denied by server rules
        //          /// </summary>
        //          NotAllowed_ServerRules = 3,
        //          /// <summary>
        //          /// No target for the attack
        //          /// </summary>
        //          NoTarget = 5,
        //          /// <summary>
        //          /// Target is already dead
        //          /// </summary>
        //          TargetDead = 6,
        //          /// <summary>
        //          /// Target is out of range
        //          /// </summary>
        //          OutOfRange = 7,
        //          /// <summary>
        //          /// Attack missed
        //          /// </summary>
        //          Missed = 8,
        //          /// <summary>
        //          /// The attack was evaded
        //          /// </summary>
        //          Evaded = 9,
        //          /// <summary>
        //          /// The attack was blocked
        //          /// </summary>
        //          Blocked = 10,
        //          /// <summary>
        //          /// The attack was parried
        //          /// </summary>
        //          Parried = 11,
        //          /// <summary>
        //          /// The target is invalid
        //          /// </summary>
        //          NoValidTarget = 12,
        //          /// <summary>
        //          /// The target is not visible
        //          /// </summary>
        //          TargetNotVisible = 14,
        //          /// <summary>
        //          /// The attack was fumbled
        //          /// </summary>
        //          Fumbled = 15,
        //          /// <summary>
        //          /// The attack was Bodyguarded
        //          /// </summary>
        //          Bodyguarded = 16,
        //          /// <summary>
        //          /// The attack was Phaseshiftet
        //          /// </summary>
        //          Phaseshift = 17,
        //          /// <summary>
        //          /// The attack was Grappled
        //          /// </summary>
        //          Grappled = 18
        //      }

        /// <summary>
        /// The chance for a critical hit
        /// </summary>
        /// <param name="weapon">attack weapon</param>
        public int AttackCriticalChance(InventoryItem weapon)
        {
            if (owner is GamePlayer)
            {
                var p = owner as GamePlayer;

                if (weapon != null && weapon.Item_Type == Slot.RANGED &&
                    p.rangeAttackComponent?.RangedAttackType == eRangedAttackType.Critical)
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

            /// [Atlas - Takii] Wild Minion Implementation. We don't want any non-pet NPCs to crit.
            /// We cannot reliably check melee vs ranged here since archer pets don't necessarily have a proper weapon with the correct slot type assigned.
            /// Since Wild Minion is the only way for pets to crit and we (currently) want it to affect melee/ranged/spells, we can just rely on the Melee crit chance even for archery attacks
            /// and as a result we don't actually need to detect melee vs ranged to end up with the correct behavior since all attack types will have the same % chance to crit in the end.
            if (owner is GameNPC NPC)
            {
                // Player-Summoned pet
                if (NPC is GamePet summonedPet && summonedPet.Owner is GamePlayer)
                {
                    return NPC.GetModified(eProperty.CriticalMeleeHitChance);
                }

                // Charmed Pet
                if (NPC.Brain is IControlledBrain charmedPetBrain && charmedPetBrain.GetPlayerOwner() != null)
                {
                    return NPC.GetModified(eProperty.CriticalMeleeHitChance);
                }
            }

            return 0;
        }

        /// <summary>
        /// Returns the damage type of the current attack
        /// </summary>
        /// <param name="weapon">attack weapon</param>
        public eDamageType AttackDamageType(InventoryItem weapon)
        {
            if (owner is GamePlayer || owner is CommanderPet)
            {
                var p = owner as GamePlayer;

                if (weapon == null)
                    return eDamageType.Natural;
                switch ((eObjectType) weapon.Object_Type)
                {
                    case eObjectType.Crossbow:
                    case eObjectType.Longbow:
                    case eObjectType.CompositeBow:
                    case eObjectType.RecurvedBow:
                    case eObjectType.Fired:
                        InventoryItem ammo = p.rangeAttackComponent?.RangeAttackAmmo;
                        if (ammo == null)
                            return (eDamageType) weapon.Type_Damage;
                        return (eDamageType) ammo.Type_Damage;
                    case eObjectType.Shield:
                        return
                            eDamageType
                                .Crush; // TODO: shields do crush damage (!) best is if Type_Damage is used properly
                    default:
                        return (eDamageType) weapon.Type_Damage;
                }
            }
            else if (owner is GameNPC)
            {
                return (owner as GameNPC).MeleeDamageType;
            }
            else return eDamageType.Natural;
        }

        /// <summary>
        /// Gets the attack-state of this living
        /// </summary>
        public virtual bool AttackState { get; set; }

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
                if (owner is GamePlayer)
                {
                    var p = owner as GamePlayer;

                    GameLiving livingTarget = p.TargetObject as GameLiving;

                    //TODO change to real distance of bows!
                    if (p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                    {
                        InventoryItem weapon = AttackWeapon;
                        if (weapon == null)
                            return 0;

                        double range;
                        InventoryItem ammo = p.rangeAttackComponent?.RangeAttackAmmo;

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
                            default:
                                range = 1200;
                                break; // shortbow, xbow, throwing
                        }

                        range = Math.Max(32, range * p.GetModified(eProperty.ArcheryRange) * 0.01);

                        if (ammo != null)
                            switch ((ammo.SPD_ABS >> 2) & 0x3)
                            {
                                case 0:
                                    range *= 0.85;
                                    break; //Clout -15%
                                //						case 1:                break; //(none) 0%
                                case 2:
                                    range *= 1.15;
                                    break; //doesn't exist on live
                                case 3:
                                    range *= 1.25;
                                    break; //Flight +25%
                            }

                        if (livingTarget != null) range += Math.Min((p.Z - livingTarget.Z) / 2.0, 500);
                        if (range < 32) range = 32;

                        return (int) (range);
                    }


                    int meleerange = 128;
                    GameKeepComponent
                        keepcomponent =
                            livingTarget as GameKeepComponent; // TODO better component melee attack range check
                    if (keepcomponent != null)
                        meleerange += 150;
                    else
                    {
                        if (livingTarget != null && livingTarget.IsMoving)
                            meleerange += 32;
                        if (p.IsMoving)
                            meleerange += 32;
                    }

                    //Console.WriteLine($"melee range: {meleerange} moving? {p.IsMoving} targmoving {livingTarget.IsMoving}");
                    return meleerange;
                }
                else
                {
                    //Mobs have a good distance range with distance weapons
                    //automatically
                    if (owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                    {
                        return Math.Max(32, (int) (2000.0 * owner.GetModified(eProperty.ArcheryRange) * 0.01));
                    }

                    //Normal mob attacks have 200 ...
                    //TODO dragon, big mobs etc...
                    return 200;
                }
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
            if (owner is GamePlayer)
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
                            case (int) eObjectType.Fired:
                            case (int) eObjectType.Longbow:
                            case (int) eObjectType.Crossbow:
                            case (int) eObjectType.RecurvedBow:
                            case (int) eObjectType.CompositeBow:
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
                        if (p.rangeAttackComponent?.RangedAttackType == eRangedAttackType.Critical)
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

                return (int) (speed * 100);
            }
            else
            {
                double speed = 3000 * (1.0 - (owner.GetModified(eProperty.Quickness) - 60) / 500.0);

                if (owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    speed *= 1.5; // mob archer speed too fast

                    // Old archery uses archery speed, but new archery uses casting speed
                    if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == true)
                        speed *= 1.0 - owner.GetModified(eProperty.ArcherySpeed) * 0.01;
                    else
                        speed *= 1.0 - owner.GetModified(eProperty.CastingSpeed) * 0.01;
                }
                else
                {
                    speed *= owner.GetModified(eProperty.MeleeSpeed) * 0.01;
                }

                return (int) Math.Max(500.0, speed);
            }
        }

        /// <summary>
        /// Gets the attack damage
        /// </summary>
        /// <param name="weapon">the weapon used for attack</param>
        /// <returns>the weapon damage</returns>
        public double AttackDamage(InventoryItem weapon)
        {
            if (owner is GamePlayer p)
            {
                if (weapon == null)
                    return 0;

                double effectiveness = 1.00;
                double damage = p.WeaponDamage(weapon) * weapon.SPD_ABS * 0.1;
                
                //slow weapon bonus as found here: https://www2.uthgard.net/tracker/issue/2753/@/Bow_damage_variance_issue_(taking_item_/_spec_???)
                //EDPS * (your WS/target AF) * (1-absorb) * slow weap bonus * SPD * 2h weapon bonus * Arrow Bonus 
                damage *= 1 + ((weapon.SPD_ABS - 20) * 0.03) * .1;

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
                            case 0:
                                damage *= 0.85;
                                break; //Blunt       (light) -15%
                            //case 1: damage *= 1;	break; //Bodkin     (medium)   0%
                            case 2:
                                damage *= 1.15;
                                break; //doesn't exist on live
                            case 3:
                                damage *= 1.25;
                                break; //Broadhead (X-heavy) +25%
                        }
                    }

                    //Ranged damage buff,debuff,Relic,RA
                    effectiveness += p.GetModified(eProperty.RangedDamage) * 0.01;
                }
                else if (weapon.Item_Type == Slot.RIGHTHAND || weapon.Item_Type == Slot.LEFTHAND ||
                         weapon.Item_Type == Slot.TWOHAND)
                {
                    //Melee damage buff,debuff,Relic,RA
                    effectiveness += p.GetModified(eProperty.MeleeDamage) * 0.01;
                }

                damage *= effectiveness;
                return damage;
            }
            else
            {
                double effectiveness = 1.00;
                //double effectiveness = Effectiveness;
                double damage = (1.0 + owner.Level / 3.7 + owner.Level * owner.Level / 175.0) * AttackSpeed(weapon) *
                                0.001;
                if (weapon == null || weapon.Item_Type == Slot.RIGHTHAND || weapon.Item_Type == Slot.LEFTHAND ||
                    weapon.Item_Type == Slot.TWOHAND)
                {
                    //Melee damage buff,debuff,RA
                    effectiveness += owner.GetModified(eProperty.MeleeDamage) * 0.01;
                }
                else if (weapon.Item_Type == Slot.RANGED && (weapon.Object_Type == (int) eObjectType.Longbow ||
                                                             weapon.Object_Type == (int) eObjectType.RecurvedBow ||
                                                             weapon.Object_Type == (int) eObjectType.CompositeBow))
                {
                    // RDSandersJR: Check to see if we are using old archery if so, use RangedDamge
                    if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == true)
                    {
                        effectiveness += owner.GetModified(eProperty.RangedDamage) * 0.01;
                    }
                    // RDSandersJR: If we are NOT using old archery it should be SpellDamage
                    else if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == false)
                    {
                        effectiveness += owner.GetModified(eProperty.SpellDamage) * 0.01;
                    }
                }
                else if (weapon.Item_Type == Slot.RANGED)
                {
                    effectiveness += owner.GetModified(eProperty.RangedDamage) * 0.01;
                }

                damage *= effectiveness;
                return damage;
            }
        }


        /// <summary>
        /// Starts a melee attack with this player
        /// </summary>
        /// <param name="attackTarget">the target to attack</param>
        public void StartAttack(GameObject attackTarget)
        {
            if (!EntityManager.GetLivingByComponent(typeof(AttackComponent)).ToArray().Contains(owner))
                EntityManager.AddComponent(typeof(AttackComponent), owner);

            var p = owner as GamePlayer;

            if (p != null)
            {
                if (p.CharacterClass.StartAttack(attackTarget) == false)
                {
                    return;
                }

                if (!p.IsAlive)
                {
                    p.Out.SendMessage(
                        LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouCantCombat"),
                        eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                // Necromancer with summoned pet cannot attack
                if (p.ControlledBrain != null)
                    if (p.ControlledBrain.Body != null)
                        if (p.ControlledBrain.Body is NecromancerPet)
                        {
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.StartAttack.CantInShadeMode"), eChatType.CT_YouHit,
                                eChatLoc.CL_SystemWindow);
                            return;
                        }

                if (p.IsStunned)
                {
                    p.Out.SendMessage(
                        LanguageMgr.GetTranslation(p.Client.Account.Language,
                            "GamePlayer.StartAttack.CantAttackStunned"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (p.IsMezzed)
                {
                    p.Out.SendMessage(
                        LanguageMgr.GetTranslation(p.Client.Account.Language,
                            "GamePlayer.StartAttack.CantAttackmesmerized"), eChatType.CT_YouHit,
                        eChatLoc.CL_SystemWindow);
                    return;
                }

                long vanishTimeout = p.TempProperties.getProperty<long>(VanishEffect.VANISH_BLOCK_ATTACK_TIME_KEY);
                if (vanishTimeout > 0 && vanishTimeout > GameLoop.GameLoopTime)
                {
                    p.Out.SendMessage(
                        LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouMustWaitAgain",
                            (vanishTimeout - GameLoop.GameLoopTime + 1000) / 1000), eChatType.CT_YouHit,
                        eChatLoc.CL_SystemWindow);
                    return;
                }

                long VanishTick = p.TempProperties.getProperty<long>(VanishEffect.VANISH_BLOCK_ATTACK_TIME_KEY);
                long changeTime = GameLoop.GameLoopTime - VanishTick;
                if (changeTime < 30000 && VanishTick > 0)
                {
                    p.Out.SendMessage(
                        LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouMustWait",
                            ((30000 - changeTime) / 1000).ToString()), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (p.IsOnHorse)
                    p.IsOnHorse = false;

                if (p.Steed != null && p.Steed is GameSiegeRam)
				{
					p.Out.SendMessage("You can't enter combat mode while riding a siegeram!.", eChatType.CT_YouHit,eChatLoc.CL_SystemWindow);
					return;
				}

                if (p.IsDisarmed)
                {
                    p.Out.SendMessage(
                        LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CantDisarmed"),
                        eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (p.IsSitting)
                {
                    p.Sit(false);
                }

                if (AttackWeapon == null)
                {
                    p.Out.SendMessage(
                        LanguageMgr.GetTranslation(p.Client.Account.Language,
                            "GamePlayer.StartAttack.CannotWithoutWeapon"), eChatType.CT_YouHit,
                        eChatLoc.CL_SystemWindow);
                    return;
                }

                if (AttackWeapon.Object_Type == (int) eObjectType.Instrument)
                {
                    p.Out.SendMessage(
                        LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CannotMelee"),
                        eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    return;
                }

                if (p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == false)
                    {
                        if ((eCharacterClass) p.CharacterClass.ID == eCharacterClass.Scout ||
                            (eCharacterClass) p.CharacterClass.ID == eCharacterClass.Hunter ||
                            (eCharacterClass) p.CharacterClass.ID == eCharacterClass.Ranger)
                        {
                            // There is no feedback on live when attempting to fire a bow with arrows
                            return;
                        }
                    }

                    // Check arrows for ranged attack
                    if (p.rangeAttackComponent?.RangeAttackAmmo == null)
                    {
                        p.Out.SendMessage(
                            LanguageMgr.GetTranslation(p.Client.Account.Language,
                                "GamePlayer.StartAttack.SelectQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    // Check if selected ammo is compatible for ranged attack
                    if (!p.rangeAttackComponent.CheckRangedAmmoCompatibilityWithActiveWeapon())
                    {
                        p.Out.SendMessage(
                            LanguageMgr.GetTranslation(p.Client.Account.Language,
                                "GamePlayer.StartAttack.CantUseQuiver"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (EffectListService.GetAbilityEffectOnTarget(p, eEffect.SureShot) != null)
                        p.rangeAttackComponent.RangedAttackType = eRangedAttackType.SureShot;
                    if (EffectListService.GetAbilityEffectOnTarget(p, eEffect.RapidFire) != null)
                        p.rangeAttackComponent.RangedAttackType = eRangedAttackType.RapidFire;
                    if (EffectListService.GetAbilityEffectOnTarget(p, eEffect.TrueShot) != null)
                        p.rangeAttackComponent.RangedAttackType = eRangedAttackType.Long;


                    if (p.rangeAttackComponent?.RangedAttackType == eRangedAttackType.Critical &&
                        p.Endurance < RangeAttackComponent.CRITICAL_SHOT_ENDURANCE)
                    {
                        p.Out.SendMessage(
                            LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.TiredShot"),
                            eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (p.Endurance < RangeAttackComponent.RANGE_ATTACK_ENDURANCE)
                    {
                        p.Out.SendMessage(
                            LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.TiredUse",
                                AttackWeapon.Name), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (p.IsStealthed)
                    {
                        // -Chance to unstealth while nocking an arrow = stealth spec / level
                        // -Chance to unstealth nocking a crit = stealth / level  0.20
                        int stealthSpec = p.GetModifiedSpecLevel(Specs.Stealth);
                        int stayStealthed = stealthSpec * 100 / p.Level;
                        if (p.rangeAttackComponent?.RangedAttackType == eRangedAttackType.Critical)
                            stayStealthed -= 20;

                        if (!Util.Chance(stayStealthed))
                            p.Stealth(false);
                    }
                }
                else
                {
                    if (attackTarget == null)
                        p.Out.SendMessage(
                            LanguageMgr.GetTranslation(p.Client.Account.Language,
                                "GamePlayer.StartAttack.CombatNoTarget"), eChatType.CT_YouHit,
                            eChatLoc.CL_SystemWindow);
                    else if (attackTarget is GameNPC)
                    {
                        p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,
                                "GamePlayer.StartAttack.CombatTarget",
                                attackTarget.GetName(0, false, p.Client.Account.Language, (attackTarget as GameNPC))),
                            eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        p.Out.SendMessage(
                            LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.CombatTarget",
                                attackTarget.GetName(0, false)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
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
                LivingStartAttack(attackTarget);

                if (p.IsCasting && !p.castingComponent.spellHandler.Spell.Uninterruptible)
                {
                    p.StopCurrentSpellcast();
                    p.Out.SendMessage(
                        LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.SpellCancelled"),
                        eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                }

                //Clear styles
                owner.styleComponent.NextCombatStyle = null;
                owner.styleComponent.NextCombatBackupStyle = null;

                if (p.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                {
                    p.Out.SendAttackMode(AttackState);
                }
                else
                {
                    p.TempProperties.setProperty(RangeAttackComponent.RANGE_ATTACK_HOLD_START, GameLoop.GameLoopTime);

                    string typeMsg = "shot";
                    if (AttackWeapon.Object_Type == (int) eObjectType.Thrown)
                        typeMsg = "throw";

                    string targetMsg = "";
                    if (attackTarget != null)
                    {
                        if (p.IsWithinRadius(attackTarget, AttackRange))
                            targetMsg = LanguageMgr.GetTranslation(p.Client.Account.Language,
                                "GamePlayer.StartAttack.TargetInRange");
                        else
                            targetMsg = LanguageMgr.GetTranslation(p.Client.Account.Language,
                                "GamePlayer.StartAttack.TargetOutOfRange");
                    }

                    int speed = AttackSpeed(AttackWeapon) / 100;
                    if (p.rangeAttackComponent.RangedAttackType == eRangedAttackType.RapidFire)
                        speed = Math.Max(15, speed / 2);

                    if (p.effectListComponent.ContainsEffectForEffectType(eEffect.Volley))//volley check
                    { }
                    else
                        p.Out.SendMessage(
                        LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.StartAttack.YouPrepare",
                            typeMsg, speed / 10, speed % 10, targetMsg), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
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

            AttackState = true;

            int speed = AttackSpeed(AttackWeapon);

            if (speed > 0)
            {
                //m_attackAction = CreateAttackAction();
                //attackAction = new AttackAction(owner);
                attackAction = owner.CreateAttackAction();

                if (owner.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    // only start another attack action if we aren't already aiming to shoot
                    if (owner.rangeAttackComponent?.RangedAttackState != eRangedAttackState.Aim)
                    {
                        owner.rangeAttackComponent.RangedAttackState = eRangedAttackState.Aim;
                        if (owner is GamePlayer && owner.effectListComponent.ContainsEffectForEffectType(eEffect.Volley))//volley check
                        {
                        }
                        else
                        {
                            foreach (GamePlayer player in owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                                player.Out.SendCombatAnimation(owner, null,
                                    (ushort)(AttackWeapon == null ? 0 : AttackWeapon.Model),
                                    0x00, player.Out.BowPrepare, (byte)(speed / 100), 0x00, 0x00);
                        }
                        //m_attackAction.Start((RangedAttackType == eRangedAttackType.RapidFire) ? speed / 2 : speed);
                        attackAction.StartTime =
                            (owner.rangeAttackComponent?.RangedAttackType == eRangedAttackType.RapidFire)
                                ? Math.Max(1500, speed / 2)
                                : speed;
                        attackAction.RangeInterruptTime = 750;
                    }
                }
                else
                {
                    //if (m_attackAction.TimeUntilElapsed < 500)
                    //	m_attackAction.Start(500);
                    if (attackAction.TimeUntilStart < 100)
                        attackAction.StartTime = 100;
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

            var npc = owner as GameNPC;

            npc.TargetObject = target;

            long lastTick = npc.TempProperties.getProperty<long>(GameNPC.LAST_LOS_TICK_PROPERTY);

            if (ServerProperties.Properties.ALWAYS_CHECK_PET_LOS &&
                npc.Brain != null &&
                npc.Brain is IControlledBrain &&
                (target is GamePlayer || (target is GameNPC && (target as GameNPC).Brain != null &&
                                          (target as GameNPC).Brain is IControlledBrain)))
            {
                GameObject lastTarget =
                    (GameObject) npc.TempProperties.getProperty<object>(GameNPC.LAST_LOS_TARGET_PROPERTY, null);
                if (lastTarget != null && lastTarget == target)
                {
                    if (lastTick != 0 && GameLoop.GameLoopTime - lastTick <
                        ServerProperties.Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
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
                    foreach (GamePlayer player in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        losChecker = player;
                        break;
                    }
                }

                if (losChecker == null)
                {
                    return;
                }

                lock (npc.LOS_LOCK)
                {
                    int count = npc.TempProperties.getProperty<int>(GameNPC.NUM_LOS_CHECKS_INPROGRESS, 0);

                    if (count > 10)
                    {
                        GameNPC.log.DebugFormat("{0} LOS count check exceeds 10, aborting LOS check!", npc.Name);

                        // Now do a safety check.  If it's been a while since we sent any check we should clear count
                        if (lastTick == 0 || GameLoop.GameLoopTime - lastTick >
                            ServerProperties.Properties.LOS_PLAYER_CHECK_FREQUENCY * 1000)
                        {
                            GameNPC.log.Debug("LOS count reset!");
                            npc.TempProperties.setProperty(GameNPC.NUM_LOS_CHECKS_INPROGRESS, 0);
                        }

                        return;
                    }

                    count++;
                    npc.TempProperties.setProperty(GameNPC.NUM_LOS_CHECKS_INPROGRESS, count);

                    npc.TempProperties.setProperty(GameNPC.LAST_LOS_TARGET_PROPERTY, target);
                    npc.TempProperties.setProperty(GameNPC.LAST_LOS_TICK_PROPERTY, GameLoop.GameLoopTime);
                    npc.m_targetLOSObject = target;
                }

                losChecker.Out.SendCheckLOS(npc, target, new CheckLOSResponse(npc.NPCStartAttackCheckLOS));
                return;
            }

            ContinueStartAttack(target);
        }

        public virtual void ContinueStartAttack(GameObject target)
        {
            var p = owner as GameNPC;

            p.StopMovingOnPath();

            if (p.Brain != null && p.Brain is IControlledBrain)
            {
                if ((p.Brain as IControlledBrain).AggressionState == eAggressionState.Passive)
                    return;

                //GamePlayer owner = null;

                //if ((owner = ((IControlledBrain)p.Brain).GetPlayerOwner()) != null)
                //    owner.Stealth(false);
            }

            // TODO: need to look into these timers
            //p.SetLastMeleeAttackTick();
            //p.StartMeleeAttackTimer();

            LivingStartAttack(target);

            if (AttackState)
            {
                // if we're moving we need to lock down the current position
                if (p.IsMoving)
                    p.SaveCurrentPosition();

                if (p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                {
                    // Archer mobs sometimes bug and keep trying to fire at max range unsuccessfully so force them to get just a tad closer.
                    p.Follow(target, AttackRange - 30, GameNPC.STICKMAXIMUMRANGE);
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
                owner.styleComponent.NextCombatStyle = null;
                owner.styleComponent.NextCombatBackupStyle = null;
                LivingStopAttack(forced);
                if (p.IsAlive)
                {
                    p.Out.SendAttackMode(AttackState);
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
            AttackState = false;

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
            if (p.Inventory != null && p.Inventory.GetItem(eInventorySlot.DistanceWeapon) != null &&
                p.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
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
        public AttackData MakeAttack(GameObject target, InventoryItem weapon, Style style, double effectiveness,
            int interruptDuration, bool dualWield)
        {
            var p = owner as GamePlayer;

            if (p != null)
            {
                if (p.IsCrafting)
                {
                    p.Out.SendMessage(
                        LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.InterruptedCrafting"),
                        eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    // TODO: look into timer
                    p.CraftTimer.Stop();
                    p.CraftTimer = null;
                    p.Out.SendCloseTimerWindow();
                }

                AttackData ad = LivingMakeAttack(target, weapon, style, effectiveness * p.Effectiveness,
                    interruptDuration, dualWield);

                //Clear the styles for the next round!
                owner.styleComponent.NextCombatStyle = null;
                owner.styleComponent.NextCombatBackupStyle = null;

                switch (ad.AttackResult)
                {
                    case eAttackResult.HitStyle:
                    case eAttackResult.HitUnstyled:
                    {
                        //keep component
                        if ((ad.Target is GameKeepComponent || ad.Target is GameKeepDoor ||
                             ad.Target is GameSiegeWeapon) && ad.Attacker is GamePlayer &&
                            ad.Attacker.GetModified(eProperty.KeepDamage) > 0)
                        {
                            int keepdamage = (int) Math.Floor((double) ad.Damage *
                                                              ((double) ad.Attacker.GetModified(eProperty.KeepDamage) /
                                                               100));
                            int keepstyle = (int) Math.Floor((double) ad.StyleDamage *
                                                             ((double) ad.Attacker.GetModified(eProperty.KeepDamage) /
                                                              100));
                            ad.Damage += keepdamage;
                            ad.StyleDamage += keepstyle;
                        }

                        // vampiir
                        if (p.CharacterClass is PlayerClass.ClassVampiir
                            && target is GameKeepComponent == false
                            && target is GameKeepDoor == false
                            && target is GameSiegeWeapon == false)
                        {
                            int perc = Convert.ToInt32(
                                ((double) (ad.Damage + ad.CriticalDamage) / 100) * (55 - p.Level));
                            perc = (perc < 1) ? 1 : ((perc > 15) ? 15 : perc);
                            p.Mana += Convert.ToInt32(Math.Ceiling(((Decimal) (perc * p.MaxMana) / 100)));
                        }

                        //only miss when strafing when attacking a player
                        //30% chance to miss
                        if (p.IsStrafing && ad.Target is GamePlayer && Util.Chance(30))
                        {
                            ad.AttackResult = eAttackResult.Missed;
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.StrafMiss"),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
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
                        if (p.HasAbility(Abilities.Camouflage) && target is GamePlayer || (target is GameNPC &&
                                (target as GameNPC).Brain is IControlledBrain &&
                                ((target as GameNPC).Brain as IControlledBrain).GetPlayerOwner() != null))
                        {
                            CamouflageECSGameEffect camouflage =
                                (CamouflageECSGameEffect) EffectListService.GetAbilityEffectOnTarget(p,
                                    eEffect.Camouflage);

                            if (camouflage != null) // Check if Camo is active, if true, cancel ability.
                            {
                                EffectService.RequestImmediateCancelEffect(camouflage, false);
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
                            InventoryItem attackWeapon = AttackWeapon;
                            InventoryItem leftWeapon = (p.Inventory == null)
                                ? null
                                : p.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
                            switch (style.ID)
                            {
                                case 374:
                                    numTargetsCanHit = 1;
                                    break; //Tribal Assault:   Hits 2 targets
                                case 377:
                                    numTargetsCanHit = 1;
                                    break; //Clan's Might:      Hits 2 targets
                                case 379:
                                    numTargetsCanHit = 2;
                                    break; //Totemic Wrath:      Hits 3 targets
                                case 384:
                                    numTargetsCanHit = 3;
                                    break; //Totemic Sacrifice:   Hits 4 targets
                                case 600:
                                    numTargetsCanHit = 255;
                                    break; //Shield Swipe: No Cap on Targets
                                default:
                                    numTargetsCanHit = 0;
                                    break; //For others;
                            }

                            if (numTargetsCanHit > 0)
                            {
                                if (style.ID != 600) // Not Shield Swipe
                                {
                                    foreach (GamePlayer pl in p.GetPlayersInRadius(false, (ushort) AttackRange))
                                    {
                                        if (pl == null) continue;
                                        if (GameServer.ServerRules.IsAllowedToAttack(p, pl, true))
                                        {
                                            listAvailableTargets.Add(pl);
                                        }
                                    }

                                    foreach (GameNPC npc in p.GetNPCsInRadius(false, (ushort) AttackRange))
                                    {
                                        if (GameServer.ServerRules.IsAllowedToAttack(p, npc, true))
                                        {
                                            listAvailableTargets.Add(npc);
                                        }
                                    }

                                    // remove primary target
                                    listAvailableTargets.Remove(target);
                                    numTargetsCanHit = (byte) Math.Min(numTargetsCanHit, listAvailableTargets.Count);

                                    if (listAvailableTargets.Count > 0)
                                    {
                                        while (extraTargets.Count < numTargetsCanHit)
                                        {
                                            random = Util.Random(listAvailableTargets.Count - 1);
                                            if (!extraTargets.Contains(listAvailableTargets[random]))
                                                extraTargets.Add(listAvailableTargets[random] as GameObject);
                                        }

                                        foreach (GameObject obj in extraTargets)
                                        {
                                            if (obj is GamePlayer && ((GamePlayer) obj).IsSitting)
                                            {
                                                effectiveness *= 2;
                                            }

                                            //new WeaponOnTargetAction(this, obj as GameObject, attackWeapon, leftWeapon, effectiveness, AttackSpeed(attackWeapon), null).Start(1);  // really start the attack
                                            //if (GameServer.ServerRules.IsAllowedToAttack(this, target as GameLiving, false))
                                            weaponAction = new WeaponAction(p, obj as GameObject, attackWeapon,
                                                leftWeapon, effectiveness, AttackSpeed(attackWeapon), null);
                                        }
                                    }
                                }
                                else // shield swipe
                                {
                                    foreach (GameNPC npc in p.GetNPCsInRadius(false, (ushort) AttackRange))
                                    {
                                        if (GameServer.ServerRules.IsAllowedToAttack(p, npc, true))
                                        {
                                            listAvailableTargets.Add(npc);
                                        }
                                    }

                                    listAvailableTargets.Remove(target);
                                    numTargetsCanHit = (byte) Math.Min(numTargetsCanHit, listAvailableTargets.Count);

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
                                                LivingMakeAttack(obj, attackWeapon, null, 1,
                                                    ServerProperties.Properties.SPELL_INTERRUPT_DURATION, false, false);
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
            else if (owner is NecromancerPet)
            {
                return (owner as NecromancerPet).MakeAttack(target, weapon, style, effectiveness, interruptDuration,
                    dualWield, false);
            }
            else
            {
                effectiveness = 1;
                return LivingMakeAttack(target, weapon, style, effectiveness, interruptDuration, dualWield);
            }
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
        public AttackData LivingMakeAttack(GameObject target, InventoryItem weapon, Style style, double effectiveness,
            int interruptDuration, bool dualWield)
        {
            return LivingMakeAttack(target, weapon, style, effectiveness, interruptDuration, dualWield, false);
        }


        public AttackData LivingMakeAttack(GameObject target, InventoryItem weapon, Style style, double effectiveness,
            int interruptDuration, bool dualWield, bool ignoreLOS)
        {
            AttackData ad = new AttackData();
            ad.Attacker = owner;
            ad.Target = target as GameLiving;
            ad.Damage = 0;
            ad.CriticalDamage = 0;
            ad.Style = style;
            ad.WeaponSpeed = AttackSpeed(weapon) / 100;
            ad.DamageType = AttackDamageType(weapon);
            ad.ArmorHitLocation = eArmorSlot.NOTSET;
            ad.Weapon = weapon;
            ad.IsOffHand = weapon != null && weapon.SlotPosition == Slot.LEFTHAND;
            AttackState = true;

            // Asp style range add
            int addRange = style?.Procs?.FirstOrDefault()?.Item1.SpellType == (byte) eSpellType.StyleRange
                ? (int) style?.Procs?.FirstOrDefault()?.Item1.Value - AttackRange
                : 0;

            if (dualWield && (ad.Attacker is GamePlayer gPlayer) &&
                gPlayer.CharacterClass.ID != (int) eCharacterClass.Savage)
                ad.AttackType = AttackData.eAttackType.MeleeDualWield;
            else if (weapon == null)
                ad.AttackType = AttackData.eAttackType.MeleeOneHand;
            else
                switch (weapon.Item_Type)
                {
                    default:
                    case Slot.RIGHTHAND:
                    case Slot.LEFTHAND:
                        ad.AttackType = AttackData.eAttackType.MeleeOneHand;
                        break;
                    case Slot.TWOHAND:
                        ad.AttackType = AttackData.eAttackType.MeleeTwoHand;
                        break;
                    case Slot.RANGED:
                        ad.AttackType = AttackData.eAttackType.Ranged;
                        break;
                }

            //No target, stop the attack
            if (ad.Target == null)
            {
                ad.AttackResult = (target == null) ? eAttackResult.NoTarget : eAttackResult.NoValidTarget;
                SendAttackingCombatMessages(ad);
                return ad;
            }

            // check region
            if (ad.Target.CurrentRegionID != owner.CurrentRegionID || ad.Target.ObjectState != eObjectState.Active)
            {
                ad.AttackResult = eAttackResult.NoValidTarget;
                SendAttackingCombatMessages(ad);
                return ad;
            }

            //Check if the target is in front of attacker
            if (!ignoreLOS && ad.AttackType != AttackData.eAttackType.Ranged && owner is GamePlayer &&
                !(ad.Target is GameKeepComponent) &&
                !(owner.IsObjectInFront(ad.Target, 120, true) && owner.TargetInView))
            {
                ad.AttackResult = eAttackResult.TargetNotVisible;
                SendAttackingCombatMessages(ad);
                return ad;
            }

            //Target is dead already
            if (!ad.Target.IsAlive)
            {
                ad.AttackResult = eAttackResult.TargetDead;
                SendAttackingCombatMessages(ad);
                return ad;
            }

            //Console.WriteLine($"AttkRange {AttackRange} AddRange {addRange} targetAttkComp {ad.Target.attackComponent.AttackRange}");
            //We have no attacking distance!
            if (!owner.IsWithinRadius(ad.Target,
                    ad.Target.ActiveWeaponSlot == eActiveWeaponSlot.Standard
                        ? Math.Max(AttackRange + addRange, ad.Target.attackComponent.AttackRange + addRange)
                        : AttackRange + addRange))
            {
                ad.AttackResult = eAttackResult.OutOfRange;
                SendAttackingCombatMessages(ad);
                return ad;
            }

            if (owner.rangeAttackComponent?.RangedAttackType == eRangedAttackType.Long)
            {
                AttackRange = (int) Math.Round(AttackRange * 1.5);
                owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;
            }

            if (!GameServer.ServerRules.IsAllowedToAttack(ad.Attacker, ad.Target, false))
            {
                ad.AttackResult = eAttackResult.NotAllowed_ServerRules;
                SendAttackingCombatMessages(ad);
                return ad;
            }

            /*
            if (SpellHandler.FindEffectOnTarget(owner, "Phaseshift") != null)
            {
                ad.AttackResult = eAttackResult.Phaseshift;
                SendAttackingCombatMessages(ad);
                return ad;
            }*/

            if (ad.Target.IsSitting)
                effectiveness *= 2;

            // Apply Mentalist RA5L
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
                    SendAttackingCombatMessages(ad);
                    return ad;
                }
            }

            // DamageImmunity Ability
            if ((GameLiving) target != null && ((GameLiving) target).HasAbility(Abilities.DamageImmunity))
            {
                //if (ad.Attacker is GamePlayer) ((GamePlayer)ad.Attacker).Out.SendMessage(string.Format("{0} can't be attacked!", ad.Target.GetName(0, true)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                ad.AttackResult = eAttackResult.NoValidTarget;
                SendAttackingCombatMessages(ad);
                return ad;
            }


            //Calculate our attack result and attack damage
            ad.AttackResult = ad.Target.attackComponent.CalculateEnemyAttackResult(ad, weapon);

            // calculate damage only if we hit the target
            if (ad.AttackResult == eAttackResult.HitUnstyled
                || ad.AttackResult == eAttackResult.HitStyle)
            {
                double damage = AttackDamage(weapon) * effectiveness;
                //Console.WriteLine($"Base damage {damage}");

                if (owner.Level > ServerProperties.Properties.MOB_DAMAGE_INCREASE_STARTLEVEL &&
                    ServerProperties.Properties.MOB_DAMAGE_INCREASE_PERLEVEL > 0 &&
                    damage > 0 &&
                    owner is GameNPC && (owner as GameNPC).Brain is IControlledBrain == false)
                {
                    double modifiedDamage = ServerProperties.Properties.MOB_DAMAGE_INCREASE_PERLEVEL *
                                            (owner.Level - ServerProperties.Properties.MOB_DAMAGE_INCREASE_STARTLEVEL);
                    damage += (modifiedDamage * effectiveness);
                }

                InventoryItem armor = null;

                if (ad.Target.Inventory != null)
                    armor = ad.Target.Inventory.GetItem((eInventorySlot) ad.ArmorHitLocation);

                InventoryItem weaponTypeToUse = null;

                if (weapon != null)
                {
                    weaponTypeToUse = new InventoryItem();
                    weaponTypeToUse.Object_Type = weapon.Object_Type;
                    weaponTypeToUse.SlotPosition = weapon.SlotPosition;

                    if ((owner is GamePlayer) && owner.Realm == eRealm.Albion
                                              && (GameServer.ServerRules.IsObjectTypesEqual(
                                                      (eObjectType) weapon.Object_Type, eObjectType.TwoHandedWeapon)
                                                  || GameServer.ServerRules.IsObjectTypesEqual(
                                                      (eObjectType) weapon.Object_Type, eObjectType.PolearmWeapon))
                                              && ServerProperties.Properties.ENABLE_ALBION_ADVANCED_WEAPON_SPEC)
                    {
                        // Albion dual spec penalty, which sets minimum damage to the base damage spec
                        if (weapon.Type_Damage == (int) eDamageType.Crush)
                        {
                            weaponTypeToUse.Object_Type = (int) eObjectType.CrushingWeapon;
                        }
                        else if (weapon.Type_Damage == (int) eDamageType.Slash)
                        {
                            weaponTypeToUse.Object_Type = (int) eObjectType.SlashingWeapon;
                        }
                        else
                        {
                            weaponTypeToUse.Object_Type = (int) eObjectType.ThrustWeapon;
                        }
                    }
                }


                if (owner is GamePlayer)
                {
                    int spec = owner.WeaponSpecLevel(weaponTypeToUse);

                    if (owner.Level < 5 && spec < 2) spec = 2;

                    //double lowerLimit = spec < owner.Level * 2 / 3 ? 0.25 : 0.75;
                    double lowerLimit = Math.Min(0.75 * (spec - 1) / (ad.Target.EffectiveLevel + 1) + 0.25, 1.0);
                    if (lowerLimit < 0) lowerLimit = 0.01;

                    //good luck to all ye who must read this math
                    //trust in the rust, baby
                    double upperLimit =
                        Math.Min(Math.Max(1.25 + ((3d * (spec - 1) / (ad.Target.EffectiveLevel + 1)) - 2) * .25, 1.25),
                            1.50);
                    
                    int varianceRange = (int) (upperLimit * 100 - lowerLimit * 100);

                    double weaponskillCalc = 1 + 
                        owner.GetWeaponSkill(weapon); //this provide level * damagetable * stats part of equation
                    double strengthRelicCount =
                        0.9 + (0.1 * Math.Max(1.0, RelicMgr.GetRelicBonusModifier(owner.Realm, eRelicType.Strength)));
                    double specModifier = lowerLimit + Util.Random(varianceRange) * 0.01;

                    double armorMod = (1 + ad.Target.GetArmorAF(ad.ArmorHitLocation)) /
                                      (1 - ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
                    if (armorMod <= 0) armorMod = 0.1;
                    
                    //double absBuffReduction = 1 - ad.Target.GetModified(eProperty.ArmorAbsorption) * .01; //this is included in the GetArmorAF method already
                    //double resistReduction = 1 - ad.Target.GetResist(ad.DamageType) * .01;
                    double DamageMod = weaponskillCalc * strengthRelicCount * specModifier / armorMod;
                    if (DamageMod > 3.0) DamageMod = 3.0;
                    damage *= DamageMod;

                    if (ad.Attacker is GamePlayer weaponskiller && weaponskiller.UseDetailedCombatLog)
                    {
                        weaponskiller.Out.SendMessage(
                            $"Base WS: {weaponskillCalc.ToString("0.00")} | Calc WS: {(weaponskillCalc * specModifier * strengthRelicCount).ToString("0.00")} | SpecMod: {specModifier.ToString("0.00")}",
                            eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                        weaponskiller.Out.SendMessage(
                            $"Base AF: {(ad.Target.GetArmorAF(ad.ArmorHitLocation)).ToString("0.00")} | ABS: {(ad.Target.GetArmorAbsorb(ad.ArmorHitLocation)*100).ToString("0.00")} | AF/ABS: {armorMod.ToString("0.00")}",
                            eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                        weaponskiller.Out.SendMessage($"Damage Modifier: {(int) (DamageMod * 1000)}",
                            eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                    }

                    if (ad.Target is GamePlayer attackee && attackee.UseDetailedCombatLog)
                        attackee.Out.SendMessage($"Damage Modifier: {(int) (DamageMod * 1000)}", eChatType.CT_DamageAdd,
                            eChatLoc.CL_SystemWindow);
                    /*
                        // Badge Of Valor Calculation 1+ absorb or 1- absorb
                        if (ad.Attacker.EffectList.GetOfType<BadgeOfValorEffect>() != null)
                        {
                            damage *= 1.0 + Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
                        }
                        else
                        {
                            damage *= 1.0 - Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
                        }*/
                }
                else
                {
                    int spec = owner.WeaponSpecLevel(weaponTypeToUse);
                    int styleSpec = 0;
                    if (ad.Style != null)
                    {
                        styleSpec = owner.GetModifiedSpecLevel(ad.Style.Spec);
                    }

                    // Modified to change the lowest value being 75
                    int lowerboundary = 75;
                    int upperboundary = 125;
                    if (owner is GameEpicBoss)//Epic Boss 
                    {
                        lowerboundary = 95;//min dmg
                        upperboundary = 105;//max dmg
                    }
                    var boss = owner as GameEpicBoss;//Epic Boss

                    if (owner is GameEpicBoss)
                    {
                         lowerboundary = 95;  //min
                         upperboundary = 105; //max
                    }

                    double specModifier = styleSpec > 0 ? ((100 + styleSpec) / 100.0) : ((100 + spec) / 100.0);
                    //Console.WriteLine($"spec: {spec} stylespec: {styleSpec} specMod: {specModifier}");
                    int range = upperboundary - lowerboundary;
                    damage *= (lowerboundary + Util.Random(range)) * 0.01;
                    double weaponskillCalc = (owner.GetWeaponSkill(weapon) + ad.Attacker.Level * 45/50d);
                    double armorCalc = (ad.Target.GetArmorAF(ad.ArmorHitLocation) + ad.Target.Level * 45/50d) * (1 +
                        ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
                    double DamageMod = weaponskillCalc / armorCalc;
                    if (DamageMod > 3.0) DamageMod = 3.0;
                    if (owner is GameEpicBoss)
                        damage *= DamageMod + (boss.Strength / 200);//only if it's EpicBoss
                    else
                        damage *= DamageMod;//normal mobs

                    if (ad.Attacker is GamePlayer weaponskiller && weaponskiller.UseDetailedCombatLog)
                    {
                        weaponskiller.Out.SendMessage($"WS: {weaponskillCalc} AF: {armorCalc}", eChatType.CT_DamageAdd,
                            eChatLoc.CL_SystemWindow);
                        weaponskiller.Out.SendMessage($"Damage Modifier: {(int) (DamageMod * 1000)}",
                            eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                    }

                    if (ad.Target is GamePlayer attackee && attackee.UseDetailedCombatLog)
                    {
                        attackee.Out.SendMessage($"NPC Damage Modifier: {(int) (DamageMod * 1000)}",
                            eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                    }


                    /*
                    // Badge Of Valor Calculation 1+ absorb or 1- absorb
                    if (ad.Attacker.EffectList.GetOfType<BadgeOfValorEffect>() != null)
                    {
                        damage *= 1.0 + Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
                    }
                    else
                    {
                        damage *= 1.0 - Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
                    }*/
                }

                if (ad.IsOffHand)
                {
                    damage *= 1 + ((owner.GetModified(eProperty.OffhandDamage) +
                                    owner.GetModified(eProperty.OffhandDamageAndChance)) * .01);
                }


                ad.Modifier = (int) (damage *
                                     (ad.Target.GetResist(ad.DamageType) +
                                      SkillBase.GetArmorResist(armor, ad.DamageType)) * -0.01);
                //damage += ad.Modifier;
                // RA resist check
                int resist = (int) (damage * ad.Target.GetDamageResist(owner.GetResistTypeForDamage(ad.DamageType)) *
                                    -0.01);

                eProperty property = ad.Target.GetResistTypeForDamage(ad.DamageType);
                int secondaryResistModifier = ad.Target.SpecBuffBonusCategory[(int) property];
                int resistModifier = 0;
                resistModifier +=
                    (int) ((ad.Damage + (double) resistModifier) * (double) secondaryResistModifier * -0.01);

                damage += resist;
                damage += resistModifier;
                ad.Modifier += resist;
                damage += ad.Modifier;
                ad.Damage = (int) damage;

                // apply total damage cap
                ad.UncappedDamage = ad.Damage;
                // Console.WriteLine($"uncapped {ad.UncappedDamage} calcUncap {UnstyledDamageCap(weapon)}");
                if (owner.rangeAttackComponent?.RangedAttackType == eRangedAttackType.Critical)
                    ad.Damage = Math.Min(ad.Damage, (int) (UnstyledDamageCap(weapon) * 2));
                else
                    ad.Damage = Math.Min(ad.Damage, (int) (UnstyledDamageCap(weapon) /* * effectiveness*/));

                if ((owner is GamePlayer ||
                     (owner is GameNPC && (owner as GameNPC).Brain is IControlledBrain && owner.Realm != 0)) &&
                    target is GamePlayer)
                {
                    ad.Damage = (int) ((double) ad.Damage * ServerProperties.Properties.PVP_MELEE_DAMAGE);
                }
                else if ((owner is GamePlayer ||
                          (owner is GameNPC && (owner as GameNPC).Brain is IControlledBrain && owner.Realm != 0)) &&
                         target is GameNPC)
                {
                    ad.Damage = (int) ((double) ad.Damage * ServerProperties.Properties.PVE_MELEE_DAMAGE);
                }

                //ad.UncappedDamage = ad.Damage;

                //Eden - Conversion Bonus (Crocodile Ring)  - tolakram - critical damage is always 0 here, needs to be moved
                if (ad.Target is GamePlayer && ad.Target.GetModified(eProperty.Conversion) > 0)
                {
                    int manaconversion = (int) Math.Round(((double) ad.Damage + (double) ad.CriticalDamage) *
                        (double) ad.Target.GetModified(eProperty.Conversion) / 100);
                    //int enduconversion=(int)Math.Round((double)manaconversion*(double)ad.Target.MaxEndurance/(double)ad.Target.MaxMana);
                    int enduconversion = (int) Math.Round(((double) ad.Damage + (double) ad.CriticalDamage) *
                        (double) ad.Target.GetModified(eProperty.Conversion) / 100);
                    if (ad.Target.Mana + manaconversion > ad.Target.MaxMana)
                        manaconversion = ad.Target.MaxMana - ad.Target.Mana;
                    if (ad.Target.Endurance + enduconversion > ad.Target.MaxEndurance)
                        enduconversion = ad.Target.MaxEndurance - ad.Target.Endurance;
                    if (manaconversion < 1) manaconversion = 0;
                    if (enduconversion < 1) enduconversion = 0;
                    if (manaconversion >= 1)
                        (ad.Target as GamePlayer).Out.SendMessage(
                            string.Format(
                                LanguageMgr.GetTranslation((ad.Target as GamePlayer).Client.Account.Language,
                                    "GameLiving.AttackData.GainPowerPoints"), manaconversion), eChatType.CT_Spell,
                            eChatLoc.CL_SystemWindow);
                    if (enduconversion >= 1)
                        (ad.Target as GamePlayer).Out.SendMessage(
                            string.Format(
                                LanguageMgr.GetTranslation((ad.Target as GamePlayer).Client.Account.Language,
                                    "GameLiving.AttackData.GainEndurancePoints"), enduconversion), eChatType.CT_Spell,
                            eChatLoc.CL_SystemWindow);
                    ad.Target.Endurance += enduconversion;
                    if (ad.Target.Endurance > ad.Target.MaxEndurance) ad.Target.Endurance = ad.Target.MaxEndurance;
                    ad.Target.Mana += manaconversion;
                    if (ad.Target.Mana > ad.Target.MaxMana) ad.Target.Mana = ad.Target.MaxMana;
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
                ad.AttackResult = eAttackResult.HitStyle;
            }

            if ((ad.AttackResult == eAttackResult.HitUnstyled || ad.AttackResult == eAttackResult.HitStyle))
            {
                ad.CriticalDamage = GetMeleeCriticalDamage(ad, weapon);
            }

            // Attacked living may modify the attack data.  Primarily used for keep doors and components.
            ad.Target.ModifyAttack(ad);

            if (ad.AttackResult == eAttackResult.HitStyle)
            {
                if (owner is GamePlayer)
                {
                    GamePlayer player = owner as GamePlayer;

                    string damageAmount = (ad.StyleDamage > 0)
                        ? " (+" + ad.StyleDamage + ", GR: " + ad.Style.GrowthRate + ")"
                        : "";
                    player.Out.SendMessage(
                        LanguageMgr.GetTranslation(player.Client.Account.Language,
                            "StyleProcessor.ExecuteStyle.PerformPerfectly", ad.Style.Name, damageAmount),
                        eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                }
                else if (owner is GameNPC)
                {
                    ControlledNpcBrain brain = ((GameNPC) owner).Brain as ControlledNpcBrain;

                    if (brain != null)
                    {
                        GamePlayer owner = brain.GetPlayerOwner();
                        if (owner != null)
                        {
                            string damageAmount = (ad.StyleDamage > 0)
                                ? " (+" + ad.StyleDamage + ", GR: " + ad.Style.GrowthRate + ")"
                                : "";
                            owner.Out.SendMessage(
                                LanguageMgr.GetTranslation(owner.Client.Account.Language,
                                    "StyleProcessor.ExecuteStyle.PerformsPerfectly", owner.Name, ad.Style.Name,
                                    damageAmount), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
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
                case eAttackResult.Parried:
                    message = string.Format("{0} attacks {1} and is parried!", ad.Attacker.GetName(0, true),
                        ad.Target.GetName(0, false));
                    break;
                case eAttackResult.Evaded:
                    message = string.Format("{0} attacks {1} and is evaded!", ad.Attacker.GetName(0, true),
                        ad.Target.GetName(0, false));
                    break;
                case eAttackResult.Missed:
                    message = string.Format("{0} attacks {1} and misses!", ad.Attacker.GetName(0, true),
                        ad.Target.GetName(0, false));
                    break;

                case eAttackResult.Blocked:
                {
                    message = string.Format("{0} attacks {1} and is blocked!", ad.Attacker.GetName(0, true),
                        ad.Target.GetName(0, false));
                    // guard messages
                    if (target != null && target != ad.Target)
                    {
                        excludes.Add(target);

                        // another player blocked for real target
                        if (target is GamePlayer)
                            ((GamePlayer) target).Out.SendMessage(
                                string.Format(
                                    LanguageMgr.GetTranslation(((GamePlayer) target).Client.Account.Language,
                                        "GameLiving.AttackData.BlocksYou"), ad.Target.GetName(0, true),
                                    ad.Attacker.GetName(0, false)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);

                        // blocked for another player
                        if (ad.Target is GamePlayer)
                        {
                            ((GamePlayer) ad.Target).Out.SendMessage(
                                string.Format(
                                    LanguageMgr.GetTranslation(((GamePlayer) ad.Target).Client.Account.Language,
                                        "GameLiving.AttackData.YouBlock") +
                                    " (" + /*(ad.Target as GamePlayer).GetBlockChance()*/
                                    ad.BlockChance.ToString("0.0") + "%)", ad.Attacker.GetName(0, false),
                                    target.GetName(0, false)), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                            ((GamePlayer) ad.Target).Stealth(false);
                        }
                    }
                    else if (ad.Target is GamePlayer)
                    {
                        // Added for Nature's Shield
                        int percent = 0;
                        if (ad.Target.styleComponent != null && (ad.Target.styleComponent.NextCombatStyle != null &&
                                                                 ad.Target.styleComponent.NextCombatStyle.ID == 394) ||
                            (ad.Target.styleComponent.NextCombatBackupStyle != null &&
                             ad.Target.styleComponent.NextCombatBackupStyle.ID == 394))
                            percent = 60;

                        ((GamePlayer) ad.Target).Out.SendMessage(
                            string.Format(
                                LanguageMgr.GetTranslation(((GamePlayer) ad.Target).Client.Account.Language,
                                    "GameLiving.AttackData.AttacksYou") + " (" + (percent > 0
                                    ? percent.ToString("0.0") + "%)"
                                    : ad.BlockChance.ToString("0.0") + "%)"), ad.Attacker.GetName(0, true)),
                            eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                    }

                    break;
                }
                case eAttackResult.HitUnstyled:
                case eAttackResult.HitStyle:
                {
                    if (target != null && target != ad.Target)
                    {
                        message = string.Format("{0} attacks {1} but hits {2}!", ad.Attacker.GetName(0, true),
                            target.GetName(0, false), ad.Target.GetName(0, false));
                        excludes.Add(target);

                        // intercept for another player
                        if (target is GamePlayer)
                            ((GamePlayer) target).Out.SendMessage(
                                string.Format(
                                    LanguageMgr.GetTranslation(((GamePlayer) target).Client.Account.Language,
                                        "GameLiving.AttackData.StepsInFront"), ad.Target.GetName(0, true)),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);

                        // intercept by player
                        if (ad.Target is GamePlayer)
                            ((GamePlayer) ad.Target).Out.SendMessage(
                                string.Format(
                                    LanguageMgr.GetTranslation(((GamePlayer) ad.Target).Client.Account.Language,
                                        "GameLiving.AttackData.YouStepInFront"), target.GetName(0, false)),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                    }
                    else
                    {
                        if (ad.Attacker is GamePlayer)
                        {
                            string hitWeapon = "weapon";
                            if (weapon != null)
                                hitWeapon = GlobalConstants.NameToShortName(weapon.Name);
                            message = string.Format("{0} attacks {1} with {2} {3}!", ad.Attacker.GetName(0, true),
                                ad.Target.GetName(0, false), ad.Attacker.GetPronoun(1, false), hitWeapon);
                        }
                        else
                        {
                            message = string.Format("{0} attacks {1} and hits!", ad.Attacker.GetName(0, true),
                                ad.Target.GetName(0, false));
                        }
                    }

                    break;
                }
                default:
                    broadcast = false;
                    break;
            }

            SendAttackingCombatMessages(ad);
            /*if(owner is GamePlayer && target != null)
            {
                (owner as GamePlayer).Out.SendObjectUpdate(target);
            }*/

            #region Prevent Flight

            if (ad.Attacker is GamePlayer)
            {
                GamePlayer attacker = ad.Attacker as GamePlayer;
                if (attacker.HasAbilityType(typeof(AtlasOF_PreventFlight)) && Util.Chance(35))
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

            #region controlled messages

            if (ad.Attacker is GameNPC)
            {
                IControlledBrain brain = ((GameNPC) ad.Attacker).Brain as IControlledBrain;
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

                                owner.Out.SendMessage(
                                    string.Format(
                                        LanguageMgr.GetTranslation(owner.Client.Account.Language,
                                            "GameLiving.AttackData.YourHits"), ad.Attacker.Name, attackTypeMsg,
                                        ad.Target.GetName(0, false), ad.Damage, modmessage), eChatType.CT_YouHit,
                                    eChatLoc.CL_SystemWindow);
                                if (ad.CriticalDamage > 0)
                                {
                                    owner.Out.SendMessage(
                                        string.Format(
                                            LanguageMgr.GetTranslation(owner.Client.Account.Language,
                                                "GameLiving.AttackData.YourCriticallyHits"), ad.Attacker.Name,
                                            ad.Target.GetName(0, false), ad.CriticalDamage), eChatType.CT_YouHit,
                                        eChatLoc.CL_SystemWindow);
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
                IControlledBrain brain = ((GameNPC) ad.Target).Brain as IControlledBrain;
                if (brain != null)
                {
                    GameLiving owner_living = brain.GetLivingOwner();
                    excludes.Add(owner_living);
                    if (owner_living != null && owner_living is GamePlayer && owner_living.ControlledBrain != null &&
                        ad.Target == owner_living.ControlledBrain.Body)
                    {
                        GamePlayer owner = owner_living as GamePlayer;
                        switch (ad.AttackResult)
                        {
                            case eAttackResult.Blocked:
                                owner.Out.SendMessage(
                                    string.Format(
                                        LanguageMgr.GetTranslation(owner.Client.Account.Language,
                                            "GameLiving.AttackData.Blocked"), ad.Attacker.GetName(0, true),
                                        ad.Target.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                                break;
                            case eAttackResult.Parried:
                                owner.Out.SendMessage(
                                    string.Format(
                                        LanguageMgr.GetTranslation(owner.Client.Account.Language,
                                            "GameLiving.AttackData.Parried"), ad.Attacker.GetName(0, true),
                                        ad.Target.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                                break;
                            case eAttackResult.Evaded:
                                owner.Out.SendMessage(
                                    string.Format(
                                        LanguageMgr.GetTranslation(owner.Client.Account.Language,
                                            "GameLiving.AttackData.Evaded"), ad.Attacker.GetName(0, true),
                                        ad.Target.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                                break;
                            case eAttackResult.Fumbled:
                                owner.Out.SendMessage(
                                    string.Format(
                                        LanguageMgr.GetTranslation(owner.Client.Account.Language,
                                            "GameLiving.AttackData.Fumbled"), ad.Attacker.GetName(0, true)),
                                    eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                                break;
                            case eAttackResult.Missed:
                                if (ad.AttackType != AttackData.eAttackType.Spell)
                                    owner.Out.SendMessage(
                                        string.Format(
                                            LanguageMgr.GetTranslation(owner.Client.Account.Language,
                                                "GameLiving.AttackData.Misses"), ad.Attacker.GetName(0, true),
                                            ad.Target.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);
                                break;
                            case eAttackResult.HitStyle:
                            case eAttackResult.HitUnstyled:
                            {
                                string modmessage = "";
                                if (ad.Modifier > 0) modmessage = " (+" + ad.Modifier + ")";
                                if (ad.Modifier < 0) modmessage = " (" + ad.Modifier + ")";
                                owner.Out.SendMessage(
                                    string.Format(
                                        LanguageMgr.GetTranslation(owner.Client.Account.Language,
                                            "GameLiving.AttackData.HitsForDamage"), ad.Attacker.GetName(0, true),
                                        ad.Target.Name, ad.Damage, modmessage), eChatType.CT_Damaged,
                                    eChatLoc.CL_SystemWindow);
                                if (ad.CriticalDamage > 0)
                                {
                                    owner.Out.SendMessage(
                                        string.Format(
                                            LanguageMgr.GetTranslation(owner.Client.Account.Language,
                                                "GameLiving.AttackData.CriticallyHitsForDamage"),
                                            ad.Attacker.GetName(0, true), ad.Target.Name, ad.CriticalDamage),
                                        eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
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
                Message.SystemToArea(ad.Attacker, message, eChatType.CT_OthersCombat,
                    (GameObject[]) excludes.ToArray(typeof(GameObject)));
            }

            // Interrupt the target of the attack
            ad.Target.StartInterruptTimer(ad, interruptDuration);

            // If we're attacking via melee, start an interrupt timer on ourselves so we cannot swing + immediately cast.
            if (ad.AttackType != AttackData.eAttackType.Spell && ad.AttackType != AttackData.eAttackType.Ranged)
                ad.Attacker.StartInterruptTimer(ad, owner.SpellInterruptDuration);

            owner.OnAttackEnemy(ad);

            //Return the result
            return ad;
        }

        /// <summary>
        /// Returns the result of an enemy attack,
        /// yes this means WE decide if an enemy hits us or not :-)
        /// </summary>
        /// <param name="ad">AttackData</param>
        /// <param name="weapon">the weapon used for attack</param>
        /// <returns>the result of the attack</returns>
        public virtual eAttackResult CalculateEnemyAttackResult(AttackData ad, InventoryItem weapon)
        {
            if (!IsValidTarget)
                return eAttackResult.NoValidTarget;

            //1.To-Hit modifiers on styles do not any effect on whether your opponent successfully Evades, Blocks, or Parries.  Grab Bag 2/27/03
            //2.The correct Order of Resolution in combat is Intercept, Evade, Parry, Block (Shield), Guard, Hit/Miss, and then Bladeturn.  Grab Bag 2/27/03, Grab Bag 4/4/03
            //3.For every person attacking a monster, a small bonus is applied to each player's chance to hit the enemy. Allowances are made for those who don't technically hit things when they are participating in the raid  for example, a healer gets credit for attacking a monster when he heals someone who is attacking the monster, because that's what he does in a battle.  Grab Bag 6/6/03
            //4.Block, parry, and bolt attacks are affected by this code, as you know. We made a fix to how the code counts people as "in combat." Before this patch, everyone grouped and on the raid was counted as "in combat." The guy AFK getting Mountain Dew was in combat, the level five guy hovering in the back and hoovering up some exp was in combat  if they were grouped with SOMEONE fighting, they were in combat. This was a bad thing for block, parry, and bolt users, and so we fixed it.  Grab Bag 6/6/03
            //5.Positional degrees - Side Positional combat styles now will work an extra 15 degrees towards the rear of an opponent, and rear position styles work in a 60 degree arc rather than the original 90 degree standard. This change should even out the difficulty between side and rear positional combat styles, which have the same damage bonus. Please note that front positional styles are not affected by this change. 1.62
            //http://daoc.catacombs.com/forum.cfm?ThreadKey=511&DefMessage=681444&forum=DAOCMainForum#Defense

            //GuardEffect guard = null;
            GuardECSGameEffect guard = null;
            DashingDefenseEffect dashing = null;
            //DashingDefenseECSGameEffect dashing = null;
            //InterceptEffect intercept = null;
            InterceptECSGameEffect intercept = null;
            GameSpellEffect bladeturn = null;
            ECSGameSpellEffect ecsbladeturn = null;
            //EngageEffect engage = null;
            EngageECSGameEffect engage = null;
            // ML effects
            GameSpellEffect phaseshift = null;
            GameSpellEffect grapple = null;
            GameSpellEffect brittleguard = null;

            AttackData lastAD = owner.TempProperties.getProperty<AttackData>(LAST_ATTACK_DATA, null);
            bool defenseDisabled = ad.Target.IsMezzed | ad.Target.IsStunned | ad.Target.IsSitting;

            // If berserk is on, no defensive skills may be used: evade, parry, ...
            // unfortunately this as to be check for every action itself to kepp oder of actions the same.
            // Intercept and guard can still be used on berserked
            //			BerserkEffect berserk = null;

            // get all needed effects in one loop
            //owner.effectListComponent.Effects.TryGetValue(eEffect.Bladeturn, out List<ECSGameEffect> btlist);
            //ecsbladeturn = btlist?.FirstOrDefault();

            if (EffectListService.GetAbilityEffectOnTarget(owner, eEffect.Guard) is GuardECSGameEffect guardEffect)
            {
                if (guard == null && guardEffect.GuardTarget == owner)
                    guard = guardEffect;
            }

            if (EffectListService.GetAbilityEffectOnTarget(owner, eEffect.Berserk) != null)
                defenseDisabled = true;

            if (EffectListService.GetAbilityEffectOnTarget(owner, eEffect.Berserk) is EngageECSGameEffect engageEffect)
                if (engage == null)
                    engage = engageEffect;

            if (EffectListService.GetSpellEffectOnTarget(owner,
                    eEffect.Bladeturn) is ECSGameSpellEffect bladeturnEffect)
                if (ecsbladeturn == null)
                    ecsbladeturn = bladeturnEffect;

            // We check if interceptor can intercept

            // we can only intercept attacks on livings, and can only intercept when active
            // you cannot intercept while you are sitting
            // if you are stuned or mesmeried you cannot intercept...
            if (EffectListService.GetAbilityEffectOnTarget(owner, eEffect.Intercept) is InterceptECSGameEffect inter)
            {
                if (intercept == null && inter != null && inter.InterceptTarget == owner &&
                    !inter.InterceptSource.IsStunned && !inter.InterceptSource.IsMezzed
                    && !inter.InterceptSource.IsSitting && inter.InterceptSource.ObjectState == eObjectState.Active &&
                    inter.InterceptSource.IsAlive
                    && owner.IsWithinRadius(inter.InterceptSource, InterceptAbilityHandler.INTERCEPT_DISTANCE)) //&&
                    //Util.Chance(inter.InterceptChance))
                {
                    int chance = (owner is GamePlayer own) ? own.RandomNumberDeck.GetInt() : Util.Random(100);
                    if(chance < inter.InterceptChance)
                        intercept = inter;
                }
            }

            bool stealthStyle = false;
            if (ad.Style != null && ad.Style.StealthRequirement && ad.Attacker is GamePlayer &&
                StyleProcessor.CanUseStyle((GamePlayer) ad.Attacker, ad.Style, weapon))
            {
                stealthStyle = true;
                defenseDisabled = true;
                //Eden - brittle guard should not intercept PA
                intercept = null;
                brittleguard = null;
            }

            // Bodyguard - the Aredhel way. Alas, this is not perfect yet as clearly,
            // this code belongs in GamePlayer, but it's a start to end this clutter.
            // Temporarily saving the below information here.
            // Defensive chances (evade/parry) are reduced by 20%, but target of bodyguard
            // can't be attacked in melee until bodyguard is killed or moves out of range.

            if (owner is GamePlayer)
            {
                GamePlayer playerAttacker = GetPlayerAttacker(ad.Attacker);

                if (playerAttacker != null)
                {
                    GameLiving attacker = ad.Attacker;

                    if (attacker.ActiveWeaponSlot != eActiveWeaponSlot.Distance)
                    {
                        GamePlayer target = owner as GamePlayer;
                        GamePlayer bodyguard = target.Bodyguard;
                        if (bodyguard != null)
                        {
                            target.Out.SendMessage(
                                String.Format(
                                    LanguageMgr.GetTranslation(target.Client.Account.Language,
                                        "GameLiving.CalculateEnemyAttackResult.YouWereProtected"), bodyguard.Name,
                                    attacker.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);

                            bodyguard.Out.SendMessage(
                                String.Format(
                                    LanguageMgr.GetTranslation(bodyguard.Client.Account.Language,
                                        "GameLiving.CalculateEnemyAttackResult.YouHaveProtected"), target.Name,
                                    attacker.Name), eChatType.CT_Missed, eChatLoc.CL_SystemWindow);

                            if (attacker == playerAttacker)
                                playerAttacker.Out.SendMessage(
                                    string.Format(
                                        LanguageMgr.GetTranslation(playerAttacker.Client.Account.Language,
                                            "GameLiving.CalculateEnemyAttackResult.YouAttempt"), target.Name,
                                        target.Name, bodyguard.Name), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            else
                                playerAttacker.Out.SendMessage(
                                    string.Format(
                                        LanguageMgr.GetTranslation(playerAttacker.Client.Account.Language,
                                            "GameLiving.CalculateEnemyAttackResult.YourPetAttempts"), target.Name,
                                        target.Name, bodyguard.Name), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            return eAttackResult.Bodyguarded;
                        }
                    }
                }
            }

            if (phaseshift != null)
                return eAttackResult.Missed;

            if (grapple != null)
                return eAttackResult.Grappled;

            if (brittleguard != null)
            {
                if (owner is GamePlayer)
                    ((GamePlayer) owner).Out.SendMessage(
                        LanguageMgr.GetTranslation(((GamePlayer) owner).Client.Account.Language,
                            "GameLiving.CalculateEnemyAttackResult.BlowIntercepted"), eChatType.CT_SpellResisted,
                        eChatLoc.CL_SystemWindow);
                if (ad.Attacker is GamePlayer)
                    ((GamePlayer) ad.Attacker).Out.SendMessage(
                        LanguageMgr.GetTranslation(((GamePlayer) ad.Attacker).Client.Account.Language,
                            "GameLiving.CalculateEnemyAttackResult.StrikeIntercepted"), eChatType.CT_SpellResisted,
                        eChatLoc.CL_SystemWindow);
                brittleguard.Cancel(false);
                return eAttackResult.Missed;
            }

            if (intercept != null && !stealthStyle)
            {
                ad.Target = intercept.InterceptSource;
                if (intercept.InterceptSource is GamePlayer)
                    intercept.Cancel(false); // can be canceled only outside of the loop
                return eAttackResult.HitUnstyled;
            }

            // i am defender, what con is attacker to me?
            // orange+ should make it harder to block/evade/parry
            double attackerConLevel = -owner.GetConLevel(ad.Attacker);
            //			double levelModifier = -((ad.Attacker.Level - Level) / (Level / 10.0 + 1));

            int attackerCount = m_attackers.Count;

            if (!defenseDisabled)
            {
                if (lastAD != null && lastAD.AttackResult != eAttackResult.HitStyle)
                    lastAD = null;

                bool UseRNGOverride = ServerProperties.Properties.OVERRIDE_DECK_RNG;

                double defensePenetration =
                    Math.Round(ad.Attacker.GetAttackerDefensePenetration(ad.Attacker, ad.Weapon), 2);

                double evadeChance = owner.TryEvade(ad, lastAD, attackerConLevel, attackerCount);
                ad.EvadeChance = evadeChance;
                double randomEvadeNum = Util.CryptoNextDouble() * 10000;
                randomEvadeNum = Math.Floor(randomEvadeNum);
                randomEvadeNum /= 100;
                evadeChance *= 100;

                if (evadeChance > 0)
                {
                    double? evadeDouble = (owner as GamePlayer)?.RandomNumberDeck.GetPseudoDouble();
                    double? evadeOutput = (evadeDouble != null) ? evadeDouble * 100 : randomEvadeNum;
                    if (ad.Attacker is GamePlayer evadeAtk && evadeAtk.UseDetailedCombatLog)
                    {
                        evadeAtk.Out.SendMessage(
                            $"target evade%: {Math.Round(evadeChance, 2)} rand: {evadeOutput} defense pen: {defensePenetration}",
                            eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                    }

                    if (ad.Target is GamePlayer evadeTarg && evadeTarg.UseDetailedCombatLog)
                    {
                        evadeTarg.Out.SendMessage(
                            $"your evade%: {Math.Round(evadeChance, 2)} rand: {evadeOutput} \nattkr def pen reduced % by {defensePenetration}%",
                            eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                    }

                    if (evadeDouble == null || UseRNGOverride)
                    {
                        if (evadeChance > randomEvadeNum)
                            return eAttackResult.Evaded;
                    }
                    else
                    {
                        evadeDouble *= 100;
                        if (evadeChance > evadeDouble)
                            return eAttackResult.Evaded;
                    }
                }

                if (ad.IsMeleeAttack)
                {
                    double parryChance = owner.TryParry(ad, lastAD, attackerConLevel, attackerCount);
                    ad.ParryChance = parryChance;
                    double ranParryNum = Util.CryptoNextDouble() * 10000;
                    ranParryNum = Math.Floor(ranParryNum);
                    ranParryNum /= 100;
                    parryChance *= 100;

                    if (parryChance > 0)
                    {
                        double? parryDouble = (owner as GamePlayer)?.RandomNumberDeck.GetPseudoDouble();
                        double? parryOutput = (parryDouble != null) ? parryDouble * 100 : ranParryNum;
                        if (ad.Attacker is GamePlayer parryAtk && parryAtk.UseDetailedCombatLog)
                        {
                            parryAtk.Out.SendMessage(
                                $"target parry%: {Math.Round(parryChance, 2)} rand: {parryOutput} defense pen: {defensePenetration}",
                                eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                        }

                        if (ad.Target is GamePlayer parryTarg && parryTarg.UseDetailedCombatLog)
                        {
                            parryTarg.Out.SendMessage(
                                $"your parry%: {Math.Round(parryChance, 2)} rand: {parryOutput} \nattkr def pen reduced % by {defensePenetration}%",
                                eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                        }

                        if (parryDouble == null || UseRNGOverride)
                        {
                            if (parryChance > ranParryNum)
                                return eAttackResult.Parried;
                        }
                        else
                        {
                            parryDouble *= 100;
                            if (parryChance > parryDouble)
                                return eAttackResult.Parried;
                        }
                    }
                }

                double blockChance = owner.TryBlock(ad, lastAD, attackerConLevel, attackerCount);
                ad.BlockChance = blockChance;
                double ranBlockNum = Util.CryptoNextDouble() * 10000;
                ranBlockNum = Math.Floor(ranBlockNum);
                ranBlockNum /= 100;
                blockChance *= 100;

                if (blockChance > 0)
                {
                    double? blockDouble = (owner as GamePlayer)?.RandomNumberDeck.GetPseudoDouble();
                    double? blockOutput = (blockDouble != null) ? blockDouble * 100 : ranBlockNum;
                    if (ad.Attacker is GamePlayer blockAttk && blockAttk.UseDetailedCombatLog)
                    {
                        blockAttk.Out.SendMessage(
                            $"target block%: {Math.Round(blockChance, 2)} rand: {blockOutput} defense pen: {defensePenetration}",
                            eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                    }

                    if (ad.Target is GamePlayer blockTarg && blockTarg.UseDetailedCombatLog)
                    {
                        blockTarg.Out.SendMessage(
                            $"your block%: {Math.Round(blockChance, 2)} rand: {blockOutput} \nattkr def pen reduced % by {defensePenetration}%",
                            eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                    }

                    if (blockDouble == null || UseRNGOverride)
                    {
                        if (blockChance > ranBlockNum)
                            return eAttackResult.Blocked;
                    }
                    else
                    {
                        blockDouble *= 100;
                        if (blockChance > blockDouble)
                            return eAttackResult.Blocked;
                    }
                }
                // reactive effects on block moved to GamePlayer
            }

            if (ad.Attacker.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
            {
                // Nature's shield 60% block
                if (owner.IsObjectInFront(ad.Attacker, 180) && (owner.styleComponent.NextCombatStyle != null &&
                                                                owner.styleComponent.NextCombatStyle.ID == 394) ||
                    (owner.styleComponent.NextCombatBackupStyle != null &&
                     owner.styleComponent.NextCombatBackupStyle.ID == 394))
                {
                    if (Util.Chance(60))
                    {
                        return eAttackResult.Blocked;
                    }
                }
            }

            // Guard
            if (guard != null &&
                guard.GuardSource.ObjectState == eObjectState.Active &&
                guard.GuardSource.IsStunned == false &&
                guard.GuardSource.IsMezzed == false &&
                guard.GuardSource.ActiveWeaponSlot != eActiveWeaponSlot.Distance &&
                //				guard.GuardSource.AttackState &&
                guard.GuardSource.IsAlive &&
                !guard.GuardSource.IsSitting &&
                !stealthStyle)
            {
                // check distance
                if (guard.GuardSource.IsWithinRadius(guard.GuardTarget, GuardAbilityHandler.GUARD_DISTANCE))
                {
                    // check player is wearing shield and NO two handed weapon
                    InventoryItem leftHand = guard.GuardSource.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
                    InventoryItem rightHand = guard.GuardSource.attackComponent.AttackWeapon;
                    if (((rightHand == null || rightHand.Hand != 1) && leftHand != null &&
                         leftHand.Object_Type == (int) eObjectType.Shield) || guard.GuardSource is GameNPC)
                    {
                        // TODO
                        // insert actual formula for guarding here, this is just a guessed one based on block.
                        int guardLevel =
                            guard.GuardSource
                                .GetAbilityLevel(Abilities
                                    .Guard); // multiply by 3 to be a bit qorse than block (block woudl be 5 since you get guard I with shield 5, guard II with shield 10 and guard III with shield 15)
                        double guardchance = 0;
                        if (guard.GuardSource is GameNPC)
                            guardchance = guard.GuardSource.GetModified(eProperty.BlockChance) * 0.001;
                        else
                            guardchance = (guard.GuardSource.GetModified(eProperty.BlockChance) * .001) *
                                          (leftHand.Quality * .01);
                        guardchance += guardLevel * 5 * .01; //5% additional chance to guard with each level in Guard
                        guardchance += attackerConLevel * 0.05;
                        int shieldSize = 0;
                        if (leftHand != null)
                            shieldSize = leftHand.Type_Damage;
                        if (guard.GuardSource is GameNPC)
                            shieldSize = 1;

                        double levelMod = (double) (leftHand.Level - 1) / 50 * 0.15;
                        guardchance +=
                            levelMod; //up to 15% extra block chance based on shield level (hidden mythic calc?)

                        if (guardchance < 0.01)
                            guardchance = 0.01;
                        //else if (ad.Attacker is GamePlayer && guardchance > .6)
                        // guardchance = .6;
                        else if (shieldSize == 1 && guardchance > .8)
                            guardchance = .8;
                        else if (shieldSize == 2 && guardchance > .9)
                            guardchance = .9;
                        else if (shieldSize == 3 && guardchance > .99)
                            guardchance = .99;

                        if (ad.AttackType == AttackData.eAttackType.MeleeDualWield) guardchance /= 2;
                        double ranBlockNum = Util.CryptoNextDouble() * 10000;
                        ranBlockNum = Math.Floor(ranBlockNum);
                        ranBlockNum /= 100;
                        guardchance *= 100;

                        double? blockDouble = (owner as GamePlayer)?.RandomNumberDeck.GetPseudoDouble();
                        double? blockOutput = (blockDouble != null) ? blockDouble * 100 : ranBlockNum;
                        if (guard.GuardSource is GamePlayer blockAttk && blockAttk.UseDetailedCombatLog)
                        {
                            blockAttk.Out.SendMessage(
                                $"Chance to guard: {guardchance} rand: {blockOutput} GuardSuccess? {guardchance > blockOutput}",
                                eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                        }

                        if (guard.GuardTarget is GamePlayer blockTarg && blockTarg.UseDetailedCombatLog)
                        {
                            blockTarg.Out.SendMessage(
                                $"Chance to be guarded: {guardchance} rand: {blockOutput} GuardSuccess? {guardchance > blockOutput}",
                                eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                        }

                        bool UseRNGOverride = ServerProperties.Properties.OVERRIDE_DECK_RNG;
                        if (blockDouble == null || UseRNGOverride)
                        {
                            if (guardchance > ranBlockNum)
                            {
                                ad.Target = guard.GuardSource;
                                return eAttackResult.Blocked;
                            }
                        }
                        else
                        {
                            if (guardchance > blockOutput)
                            {
                                ad.Target = guard.GuardSource;
                                return eAttackResult.Blocked;
                            }
                        }
                    }
                }
            }

            //Dashing Defense
            if (dashing != null &&
                dashing.GuardSource.ObjectState == eObjectState.Active &&
                dashing.GuardSource.IsStunned == false &&
                dashing.GuardSource.IsMezzed == false &&
                dashing.GuardSource.ActiveWeaponSlot != eActiveWeaponSlot.Distance &&
                dashing.GuardSource.IsAlive &&
                !stealthStyle)
            {
                // check distance
                if (dashing.GuardSource.IsWithinRadius(dashing.GuardTarget, DashingDefenseEffect.GUARD_DISTANCE))
                {
                    // check player is wearing shield and NO two handed weapon
                    InventoryItem leftHand = dashing.GuardSource.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
                    InventoryItem rightHand = dashing.GuardSource.attackComponent.AttackWeapon;
                    InventoryItem twoHand = dashing.GuardSource.Inventory.GetItem(eInventorySlot.TwoHandWeapon);
                    if ((rightHand == null || rightHand.Hand != 1) && leftHand != null &&
                        leftHand.Object_Type == (int) eObjectType.Shield)
                    {
                        int guardLevel =
                            dashing.GuardSource
                                .GetAbilityLevel(Abilities
                                    .Guard); // multiply by 3 to be a bit qorse than block (block woudl be 5 since you get guard I with shield 5, guard II with shield 10 and guard III with shield 15)
                        double guardchance = dashing.GuardSource.GetModified(eProperty.BlockChance) * leftHand.Quality *
                                             0.00001;
                        guardchance *= guardLevel * 0.25 + 0.05;
                        guardchance += attackerConLevel * 0.05;

                        if (guardchance > 0.99) guardchance = 0.99;
                        if (guardchance < 0.01) guardchance = 0.01;

                        int shieldSize = 0;
                        if (leftHand != null)
                            shieldSize = leftHand.Type_Damage;
                        if (m_attackers.Count > shieldSize)
                            guardchance /= (m_attackers.Count - shieldSize + 1);
                        if (ad.AttackType == AttackData.eAttackType.MeleeDualWield) guardchance /= 2;

                        double parrychance = double.MinValue;
                        parrychance = dashing.GuardSource.GetModified(eProperty.ParryChance);
                        if (parrychance != double.MinValue)
                        {
                            parrychance *= 0.001;
                            parrychance += 0.05 * attackerConLevel;
                            if (parrychance > 0.99) parrychance = 0.99;
                            if (parrychance < 0.01) parrychance = 0.01;
                            if (m_attackers.Count > 1) parrychance /= m_attackers.Count / 2;
                        }

                        if (Util.ChanceDouble(guardchance))
                        {
                            ad.Target = dashing.GuardSource;
                            return eAttackResult.Blocked;
                        }
                        else if (Util.ChanceDouble(parrychance))
                        {
                            ad.Target = dashing.GuardSource;
                            return eAttackResult.Parried;
                        }
                    }
                    //Check if Player is wearing Twohanded Weapon or nothing in the lefthand slot
                    else
                    {
                        double parrychance = double.MinValue;
                        parrychance = dashing.GuardSource.GetModified(eProperty.ParryChance);
                        if (parrychance != double.MinValue)
                        {
                            parrychance *= 0.001;
                            parrychance += 0.05 * attackerConLevel;
                            if (parrychance > 0.99) parrychance = 0.99;
                            if (parrychance < 0.01) parrychance = 0.01;
                            if (m_attackers.Count > 1) parrychance /= m_attackers.Count / 2;
                        }

                        if (Util.ChanceDouble(parrychance))
                        {
                            ad.Target = dashing.GuardSource;
                            return eAttackResult.Parried;
                        }
                    }
                }
            }

            // Missrate
            int missrate = (ad.Attacker is GamePlayer) ? 18 : 25; //player vs player tests show 20% miss on any level
            missrate -= ad.Attacker.GetModified(eProperty.ToHitBonus);
            // PVE group missrate
            if (owner is GameNPC && ad.Attacker is GamePlayer &&
                ((GamePlayer) ad.Attacker).Group != null &&
                (int) (0.90 * ((GamePlayer) ad.Attacker).Group.Leader.Level) >= ad.Attacker.Level &&
                ad.Attacker.IsWithinRadius(((GamePlayer) ad.Attacker).Group.Leader, 3000))
            {
                missrate -= (int) (5 * ((GamePlayer) ad.Attacker).Group.Leader.GetConLevel(owner));
            }
            else if (owner is GameNPC || ad.Attacker is GameNPC) // if target is not player use level mod
            {
                missrate += (int) (5 * ad.Attacker.GetConLevel(owner));
            }

            // experimental missrate adjustment for number of attackers
            if ((owner is GamePlayer && ad.Attacker is GamePlayer) == false)
            {
                missrate -= (Math.Max(0, Attackers.Count - 1) *
                             ServerProperties.Properties.MISSRATE_REDUCTION_PER_ATTACKERS);
            }

            // weapon/armor bonus
            int armorBonus = 0;
            if (ad.Target is GamePlayer)
            {
                ad.ArmorHitLocation = ((GamePlayer) ad.Target).CalculateArmorHitLocation(ad);
                InventoryItem armor = null;
                if (ad.Target.Inventory != null)
                    armor = ad.Target.Inventory.GetItem((eInventorySlot) ad.ArmorHitLocation);
                if (armor != null)
                    armorBonus = armor.Bonus;
            }

            if (weapon != null)
            {
                armorBonus -= weapon.Bonus;
            }

            if (ad.Target is GamePlayer && ad.Attacker is GamePlayer)
            {
                missrate += armorBonus;
            }
            else
            {
                missrate += missrate * armorBonus / 100;
            }

            if (ad.Style != null)
            {
                missrate -= ad.Style.BonusToHit; // add style bonus
            }

            if (lastAD != null && lastAD.AttackResult == eAttackResult.HitStyle && lastAD.Style != null)
            {
                // add defence bonus from last executed style if any
                missrate += lastAD.Style.BonusToDefense;
            }

            if (owner is GamePlayer && ad.Attacker is GamePlayer && weapon != null)
            {
                missrate -= (int) ((ad.Attacker.WeaponSpecLevel(weapon) - 1) * 0.1);
            }

            if (ad.Attacker.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
            {
                InventoryItem ammo = ad.Attacker.rangeAttackComponent.RangeAttackAmmo;
                if (ammo != null)
                    switch ((ammo.SPD_ABS >> 4) & 0x3)
                    {
                        // http://rothwellhome.org/guides/archery.htm
                        case 0:
                            missrate += (int) Math.Round(missrate * .15);
                            break; // Rough
                        //						case 1: missrate -= 0; break;
                        case 2:
                            missrate -= (int) Math.Round(missrate * .15);
                            break; // doesn't exist (?)
                        case 3:
                            missrate -= (int) Math.Round(missrate * .25);
                            break; // Footed
                    }
            }

            if (owner is GamePlayer && ((GamePlayer) owner).IsSitting)
            {
                missrate >>= 1; //halved
            }
            
            //check for dirty trick fumbles before misses
            DirtyTricksDetrimentalECSGameEffect dt = (DirtyTricksDetrimentalECSGameEffect)EffectListService.GetAbilityEffectOnTarget(ad.Attacker, eEffect.DirtyTricksDetrimental);
            if (dt != null && ad.IsRandomFumble)
                return eAttackResult.Fumbled;

            ad.MissRate = missrate;
            double rando = 0;
            bool skipDeckUsage = ServerProperties.Properties.OVERRIDE_DECK_RNG;
            if (missrate > 0)
            {
                if (ad.Attacker is GamePlayer atkkr && !skipDeckUsage)
                {
                    rando = atkkr.RandomNumberDeck.GetPseudoDouble();
                }
                else
                {
                    rando = Util.CryptoNextDouble();
                }


                if (ad.Attacker is GamePlayer misser && misser.UseDetailedCombatLog)
                {
                    misser.Out.SendMessage($"miss rate on target: {missrate}% rand: {(rando * 100).ToString("0.##")}", eChatType.CT_DamageAdd,
                        eChatLoc.CL_SystemWindow);
                    misser.Out.SendMessage($"Your chance to fumble: {(100 * ad.Attacker.ChanceToFumble).ToString("0.##")}% rand: {(100 * rando).ToString("0.##")}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                }

                
                if (ad.Target is GamePlayer missee && missee.UseDetailedCombatLog)
                    missee.Out.SendMessage($"chance to be missed: {missrate}% rand: {(rando * 100).ToString("0.##")}", eChatType.CT_DamageAdd,
                        eChatLoc.CL_SystemWindow);

                //check for normal fumbles
                //NOTE: fumbles are a subset of misses, and a player can only fumble if the attack would have
                //been a miss anyways
                if (ad.Attacker.ChanceToFumble > rando)
                    return eAttackResult.Fumbled;

                if (missrate > rando * 100)
                {
                    return eAttackResult.Missed;
                }
            }

            /*
            if (ad.IsRandomFumble)
                return eAttackResult.Fumbled;

            if (ad.IsRandomMiss)
                return eAttackResult.Missed;*/


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
            // "The blow penetrated the magical barrier!"
            if (ecsbladeturn != null)
            {
                bool penetrate = false;


                if (stealthStyle)
                    return
                        eAttackResult
                            .HitUnstyled; //exit early for stealth to prevent breaking bubble but still register a hit

                if (ad.Attacker.rangeAttackComponent.RangedAttackType ==
                    eRangedAttackType.Long // stealth styles pierce bladeturn
                    || (ad.AttackType == AttackData.eAttackType.Ranged &&
                        ad.Target != ecsbladeturn.SpellHandler.Caster && ad.Attacker is GamePlayer &&
                        ((GamePlayer) ad.Attacker).HasAbility(Abilities
                            .PenetratingArrow))) // penetrating arrow attack pierce bladeturn
                    penetrate = true;


                if (ad.IsMeleeAttack &&
                    !Util.ChanceDouble((double) ecsbladeturn.SpellHandler.Caster.Level / (double) ad.Attacker.Level))
                    penetrate = true;
                if (penetrate)
                {
                    if (ad.Target is GamePlayer)
                        ((GamePlayer) ad.Target).Out.SendMessage(
                            LanguageMgr.GetTranslation(((GamePlayer) ad.Target).Client.Account.Language,
                                "GameLiving.CalculateEnemyAttackResult.BlowPenetrated"), eChatType.CT_SpellResisted,
                            eChatLoc.CL_SystemWindow);
                    {
                        EffectService.RequestImmediateCancelEffect(ecsbladeturn);
                    }
                }
                else
                {
                    if (owner is GamePlayer)
                        ((GamePlayer) owner).Out.SendMessage(
                            LanguageMgr.GetTranslation(((GamePlayer) owner).Client.Account.Language,
                                "GameLiving.CalculateEnemyAttackResult.BlowAbsorbed"), eChatType.CT_SpellResisted,
                            eChatLoc.CL_SystemWindow);
                    if (ad.Attacker is GamePlayer)
                        ((GamePlayer) ad.Attacker).Out.SendMessage(
                            LanguageMgr.GetTranslation(((GamePlayer) ad.Attacker).Client.Account.Language,
                                "GameLiving.CalculateEnemyAttackResult.StrikeAbsorbed"), eChatType.CT_SpellResisted,
                            eChatLoc.CL_SystemWindow);
                    EffectService.RequestImmediateCancelEffect(ecsbladeturn);
                    if (owner is GamePlayer)
                        ((GamePlayer) owner).Stealth(false);
                    return eAttackResult.Missed;
                }
            }

            if (owner is GamePlayer && ((GamePlayer) owner).IsOnHorse)
                ((GamePlayer) owner).IsOnHorse = false;

            return eAttackResult.HitUnstyled;
        }

        /// <summary>
        /// Called to display an attack animation of this living
        /// </summary>
        /// <param name="ad">Infos about the attack</param>
        /// <param name="weapon">The weapon used for attack</param>
        public virtual void ShowAttackAnimation(AttackData ad, InventoryItem weapon)
        {
            bool showAnim = false;
            switch (ad.AttackResult)
            {
                case eAttackResult.HitUnstyled:
                case eAttackResult.HitStyle:
                case eAttackResult.Evaded:
                case eAttackResult.Parried:
                case eAttackResult.Missed:
                case eAttackResult.Blocked:
                case eAttackResult.Fumbled:
                    showAnim = true;
                    break;
            }

            if (showAnim && ad.Target != null)
            {
                //http://dolserver.sourceforge.net/forum/showthread.php?s=&threadid=836
                byte resultByte = 0;
                int attackersWeapon = (weapon == null) ? 0 : weapon.Model;
                int defendersWeapon = 0;

                switch (ad.AttackResult)
                {
                    case eAttackResult.Missed:
                        resultByte = 0;
                        break;
                    case eAttackResult.Evaded:
                        resultByte = 3;
                        break;
                    case eAttackResult.Fumbled:
                        resultByte = 4;
                        break;
                    case eAttackResult.HitUnstyled:
                        resultByte = 10;
                        break;
                    case eAttackResult.HitStyle:
                        resultByte = 11;
                        break;

                    case eAttackResult.Parried:
                        resultByte = 1;
                        if (ad.Target != null && ad.Target.attackComponent.AttackWeapon != null)
                        {
                            defendersWeapon = ad.Target.attackComponent.AttackWeapon.Model;
                        }

                        break;

                    case eAttackResult.Blocked:
                        resultByte = 2;
                        if (ad.Target != null && ad.Target.Inventory != null)
                        {
                            InventoryItem lefthand = ad.Target.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
                            if (lefthand != null && lefthand.Object_Type == (int) eObjectType.Shield)
                            {
                                defendersWeapon = lefthand.Model;
                            }
                        }

                        break;
                }

                foreach (GamePlayer player in ad.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player == null) continue;
                    int animationId;
                    switch (ad.AnimationId)
                    {
                        case -1:
                            animationId = player.Out.OneDualWeaponHit;
                            break;
                        case -2:
                            animationId = player.Out.BothDualWeaponHit;
                            break;
                        default:
                            animationId = ad.AnimationId;
                            break;
                    }

                    player.Out.SendCombatAnimation(owner, ad.Target, (ushort) attackersWeapon, (ushort) defendersWeapon,
                        animationId, 0, resultByte, ad.Target.HealthPercent);
                }
            }
        }

        protected bool IsValidTarget
        {
            get { return owner.EffectList.CountOfType<NecromancerShadeEffect>() <= 0; }
        }

        public GamePlayer GetPlayerAttacker(GameLiving living)
        {
            if (living is GamePlayer)
                return living as GamePlayer;

            GameNPC npc = living as GameNPC;

            if (npc != null)
            {
                if (npc.Brain is IControlledBrain && (npc.Brain as IControlledBrain).Owner is GamePlayer)
                    return (npc.Brain as IControlledBrain).Owner as GamePlayer;
            }

            return null;
        }

        /// <summary>
        /// Send the messages to the GamePlayer
        /// </summary>
        /// <param name="ad"></param>
        public void SendAttackingCombatMessages(AttackData ad)
        {
            //base.SendAttackingCombatMessages(ad);
            if (owner is GamePlayer)
            {
                var p = owner as GamePlayer;

                GameObject target = ad.Target;
                InventoryItem weapon = ad.Weapon;
                if (ad.Target is GameNPC)
                {
                    switch (ad.AttackResult)
                    {
                        case eAttackResult.TargetNotVisible:
                            p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.Attack.NotInView",
                                    ad.Target.GetName(0, true, p.Client.Account.Language, (ad.Target as GameNPC))),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.OutOfRange:
                            p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.Attack.TooFarAway",
                                    ad.Target.GetName(0, true, p.Client.Account.Language, (ad.Target as GameNPC))),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.TargetDead:
                            p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.Attack.AlreadyDead",
                                    ad.Target.GetName(0, true, p.Client.Account.Language, (ad.Target as GameNPC))),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.Blocked:
                            p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.Attack.Blocked",
                                    ad.Target.GetName(0, true, p.Client.Account.Language, (ad.Target as GameNPC))),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.Parried:
                            p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.Attack.Parried",
                                    ad.Target.GetName(0, true, p.Client.Account.Language, (ad.Target as GameNPC))),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.Evaded:
                            p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.Attack.Evaded",
                                    ad.Target.GetName(0, true, p.Client.Account.Language, (ad.Target as GameNPC))),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.NoTarget:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.NeedTarget"),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.NoValidTarget:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.Attack.CantBeAttacked"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.Missed:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Miss") + " (" +
                                ad.MissRate + "%)", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.Fumbled:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Fumble"),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.HitStyle:
                        case eAttackResult.HitUnstyled:
                            string modmessage = "";
                            if (ad.Modifier > 0) modmessage = " (+" + ad.Modifier + ")";
                            if (ad.Modifier < 0) modmessage = " (" + ad.Modifier + ")";

                            string hitWeapon = "";

                            switch (p.Client.Account.Language)
                            {
                                case "DE":
                                    if (weapon != null)
                                        hitWeapon = weapon.Name;
                                    break;
                                default:
                                    if (weapon != null)
                                        hitWeapon = GlobalConstants.NameToShortName(weapon.Name);
                                    break;
                            }

                            if (hitWeapon.Length > 0)
                                hitWeapon = " " +
                                            LanguageMgr.GetTranslation(p.Client.Account.Language,
                                                "GamePlayer.Attack.WithYour") + " " + hitWeapon;

                            string attackTypeMsg = LanguageMgr.GetTranslation(p.Client.Account.Language,
                                "GamePlayer.Attack.YouAttack");
                            if (p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                                attackTypeMsg = LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.Attack.YouShot");

                            // intercept messages
                            if (target != null && target != ad.Target)
                            {
                                p.Out.SendMessage(
                                    LanguageMgr.GetTranslation(p.Client.Account.Language,
                                        "GamePlayer.Attack.Intercepted", ad.Target.GetName(0, true),
                                        target.GetName(0, false)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                                p.Out.SendMessage(
                                    LanguageMgr.GetTranslation(p.Client.Account.Language,
                                        "GamePlayer.Attack.InterceptedHit", attackTypeMsg, target.GetName(0, false),
                                        hitWeapon, ad.Target.GetName(0, false), ad.Damage, modmessage),
                                    eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            }
                            else
                                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.Attack.InterceptHit", attackTypeMsg,
                                    ad.Target.GetName(0, false, p.Client.Account.Language, (ad.Target as GameNPC)),
                                    hitWeapon, ad.Damage, modmessage), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);

                            // critical hit
                            if (ad.CriticalDamage > 0)
                                p.Out.SendMessage(LanguageMgr.GetTranslation(p.Client.Account.Language,
                                        "GamePlayer.Attack.Critical",
                                        ad.Target.GetName(0, false, p.Client.Account.Language, (ad.Target as GameNPC)),
                                        ad.CriticalDamage) + " (" + AttackCriticalChance(ad.Weapon) + "%)",
                                    eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                    }
                }
                else
                {
                    switch (ad.AttackResult)
                    {
                        case eAttackResult.TargetNotVisible:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.NotInView",
                                    ad.Target.GetName(0, true)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.OutOfRange:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.TooFarAway",
                                    ad.Target.GetName(0, true)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.TargetDead:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.AlreadyDead",
                                    ad.Target.GetName(0, true)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.Blocked:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Blocked",
                                    ad.Target.GetName(0, true)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.Parried:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Parried",
                                    ad.Target.GetName(0, true)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.Evaded:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Evaded",
                                    ad.Target.GetName(0, true)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.NoTarget:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.NeedTarget"),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.NoValidTarget:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.Attack.CantBeAttacked"), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.Missed:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Miss") + " (" +
                                ad.MissRate + "%)", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.Fumbled:
                            p.Out.SendMessage(
                                LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Fumble"),
                                eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            break;
                        case eAttackResult.HitStyle:
                        case eAttackResult.HitUnstyled:
                            string modmessage = "";
                            if (ad.Modifier > 0) modmessage = " (+" + ad.Modifier + ")";
                            if (ad.Modifier < 0) modmessage = " (" + ad.Modifier + ")";

                            string hitWeapon = "";

                            switch (p.Client.Account.Language)
                            {
                                case "DE":
                                    if (weapon != null)
                                        hitWeapon = weapon.Name;
                                    break;
                                default:
                                    if (weapon != null)
                                        hitWeapon = GlobalConstants.NameToShortName(weapon.Name);
                                    break;
                            }

                            if (hitWeapon.Length > 0)
                                hitWeapon = " " +
                                            LanguageMgr.GetTranslation(p.Client.Account.Language,
                                                "GamePlayer.Attack.WithYour") + " " + hitWeapon;

                            string attackTypeMsg = LanguageMgr.GetTranslation(p.Client.Account.Language,
                                "GamePlayer.Attack.YouAttack");
                            if (p.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
                                attackTypeMsg = LanguageMgr.GetTranslation(p.Client.Account.Language,
                                    "GamePlayer.Attack.YouShot");

                            // intercept messages
                            if (target != null && target != ad.Target)
                            {
                                p.Out.SendMessage(
                                    LanguageMgr.GetTranslation(p.Client.Account.Language,
                                        "GamePlayer.Attack.Intercepted", ad.Target.GetName(0, true),
                                        target.GetName(0, false)), eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                                p.Out.SendMessage(
                                    LanguageMgr.GetTranslation(p.Client.Account.Language,
                                        "GamePlayer.Attack.InterceptedHit", attackTypeMsg, target.GetName(0, false),
                                        hitWeapon, ad.Target.GetName(0, false), ad.Damage, modmessage),
                                    eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
                            }
                            else
                                p.Out.SendMessage(
                                    LanguageMgr.GetTranslation(p.Client.Account.Language,
                                        "GamePlayer.Attack.InterceptHit", attackTypeMsg, ad.Target.GetName(0, false),
                                        hitWeapon, ad.Damage, modmessage), eChatType.CT_YouHit,
                                    eChatLoc.CL_SystemWindow);

                            // critical hit
                            if (ad.CriticalDamage > 0)
                                p.Out.SendMessage(
                                    LanguageMgr.GetTranslation(p.Client.Account.Language, "GamePlayer.Attack.Critical",
                                        ad.Target.GetName(0, false), ad.CriticalDamage) + " (" +
                                    AttackCriticalChance(ad.Weapon) + "%)", eChatType.CT_YouHit,
                                    eChatLoc.CL_SystemWindow);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Calculates melee critical damage of this player
        /// </summary>
        /// <param name="ad">The attack data</param>
        /// <param name="weapon">The weapon used</param>
        /// <returns>The amount of critical damage</returns>
        public int GetMeleeCriticalDamage(AttackData ad, InventoryItem weapon)
        {
            if (owner is GamePlayer)
            {
                if (Util.Chance(AttackCriticalChance(weapon)))
                {
                    // triple wield prevents critical hits
                    if (EffectListService.GetAbilityEffectOnTarget(ad.Target, eEffect.TripleWield) != null) return 0;

                    int critMin;
                    int critMax;
                    ECSGameEffect berserk = EffectListService.GetEffectOnTarget(owner, eEffect.Berserk);

                    if (berserk != null)
                    {
                        int level = owner.GetAbilityLevel(Abilities.Berserk);
                        // According to : http://daoc.catacombs.com/forum.cfm?ThreadKey=10833&DefMessage=922046&forum=37
                        // Zerk 1 = 1-25%
                        // Zerk 2 = 1-50%
                        // Zerk 3 = 1-75%
                        // Zerk 4 = 1-99%
                        critMin = (int) (0.01 * ad.Damage);
                        critMax = (int) (Math.Min(0.99, (level * 0.25)) * ad.Damage);
                    }
                    else
                    {
                        //think min crit dmage is 10% of damage
                        critMin = ad.Damage / 10;
                        // Critical damage to players is 50%, low limit should be around 20% but not sure
                        // zerkers in Berserk do up to 99%
                        if (ad.Target is GamePlayer)
                            critMax = ad.Damage >> 1;
                        else
                            critMax = ad.Damage;
                    }

                    critMin = Math.Max(critMin, 0);
                    critMax = Math.Max(critMin, critMax);

                    return Util.Random(critMin, critMax);
                }

                return 0;
            }
            else
                return LivingGetMeleeCriticalDamage(ad, weapon);
        }

        /// <summary>
        /// Calculates melee critical damage of this living.
        /// </summary>
        /// <param name="ad">The attack data.</param>
        /// <param name="weapon">The weapon used.</param>
        /// <returns>The amount of critical damage.</returns>
        public int LivingGetMeleeCriticalDamage(AttackData attackData, InventoryItem weapon)
        {
            if (Util.Chance(AttackCriticalChance(weapon)))
            {
                int maxCriticalDamage = (attackData.Target is GamePlayer)
                    ? attackData.Damage / 2
                    : attackData.Damage;

                int minCriticalDamage = (int) (attackData.Damage * MinMeleeCriticalDamage);

                if (minCriticalDamage > maxCriticalDamage) minCriticalDamage = maxCriticalDamage;

                return Util.Random(minCriticalDamage, maxCriticalDamage);
            }

            return 0;
        }

        /// <summary>
        /// Minimum melee critical damage as a percentage of the
        /// raw damage.
        /// </summary>
        protected float MinMeleeCriticalDamage
        {
            get { return 0.1f; }
        }

        /// <summary>
        /// Max. Damage possible without style
        /// </summary>
        /// <param name="weapon">attack weapon</param>
        public double UnstyledDamageCap(InventoryItem weapon)
        {
            if (owner is GameEpicBoss) //damage cap for epic encounters if they use melee weapons,if errors appear remove from here
            {
                var p = owner as GameEpicBoss;
                return AttackDamage(weapon) * ((double) p.Empathy / 100) *
                       ServerProperties.Properties.SET_EPIC_ENCOUNTER_WEAPON_DAMAGE_CAP;
            } ///////////////////////////remove until here if errors appear
              
            if (owner is GameDragon) //damage cap for dragon encounter
            {
                var p = owner as GameDragon;
                return AttackDamage(weapon) * ((double) p.Empathy / 100) *
                       ServerProperties.Properties.SET_EPIC_ENCOUNTER_WEAPON_DAMAGE_CAP;
            } 

            if (owner is GamePlayer)
            {
                var p = owner as GamePlayer;

                if (weapon != null)
                {
                    int DPS = weapon.DPS_AF;
                    int cap = 12 + 3 * p.Level;
                    if (p.RealmLevel > 39)
                        cap += 3;
                    if (DPS > cap)
                        DPS = cap;

                    double result = DPS * weapon.SPD_ABS * 0.03 * (0.94 + 0.003 * weapon.SPD_ABS);

                    if (weapon.Hand == 1) //2h
                    {
                        result *= 1.1 + (owner.WeaponSpecLevel(weapon) - 1) * 0.005;
                        if (weapon.Item_Type == Slot.RANGED)
                        {
                            // http://home.comcast.net/~shadowspawn3/bowdmg.html
                            //ammo damage bonus
                            double ammoDamageBonus = 1;
                            if (p.rangeAttackComponent.RangeAttackAmmo != null)
                            {
                                switch ((p.rangeAttackComponent.RangeAttackAmmo.SPD_ABS) & 0x3)
                                {
                                    case 0:
                                        ammoDamageBonus = 0.85;
                                        break; //Blunt       (light) -15%
                                    case 1:
                                        ammoDamageBonus = 1;
                                        break; //Bodkin     (medium)   0%
                                    case 2:
                                        ammoDamageBonus = 1.15;
                                        break; //doesn't exist on live
                                    case 3:
                                        ammoDamageBonus = 1.25;
                                        break; //Broadhead (X-heavy) +25%
                                }
                            }

                            result *= ammoDamageBonus;
                        }
                    }

                    if (weapon.Item_Type == Slot.RANGED && (weapon.Object_Type == (int) eObjectType.Longbow ||
                                                            weapon.Object_Type == (int) eObjectType.RecurvedBow ||
                                                            weapon.Object_Type == (int) eObjectType.CompositeBow))
                    {
                        if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == true)
                        {
                            result += p.GetModified(eProperty.RangedDamage) * 0.01;
                        }
                        else if (ServerProperties.Properties.ALLOW_OLD_ARCHERY == false)
                        {
                            result += p.GetModified(eProperty.SpellDamage) * 0.01;
                            result += p.GetModified(eProperty.RangedDamage) * 0.01;
                        }
                    }
                    else if (weapon.Item_Type == Slot.RANGED)
                    {
                        //Ranged damage buff,debuff,Relic,RA
                        result += p.GetModified(eProperty.RangedDamage) * 0.01;
                    }
                    else if (weapon.Item_Type == Slot.RIGHTHAND || weapon.Item_Type == Slot.LEFTHAND ||
                             weapon.Item_Type == Slot.TWOHAND)
                    {
                        result += p.GetModified(eProperty.MeleeDamage) * 0.01;
                    }

                    if (result <= 0) //Checking if 0 or negative
                        result = 1;
                    return result;
                }
                else
                {
                    // TODO: whats the damage cap without weapon?
                    return AttackDamage(weapon) * 3 * (1 + (AttackSpeed(weapon) * 0.001 - 2) * .03);
                }
            }
            else
            {
                return AttackDamage(weapon) * (2.82 + 0.00009 * AttackSpeed(weapon));
            }
        }

        /// <summary>
        /// Returns the weapon used to attack, null=natural
        /// </summary>
        public virtual InventoryItem AttackWeapon
        {
            get
            {
                if (owner.Inventory != null)
                {
                    switch (owner.ActiveWeaponSlot)
                    {
                        case eActiveWeaponSlot.Standard: return owner.Inventory.GetItem(eInventorySlot.RightHandWeapon);
                        case eActiveWeaponSlot.TwoHanded: return owner.Inventory.GetItem(eInventorySlot.TwoHandWeapon);
                        case eActiveWeaponSlot.Distance: return owner.Inventory.GetItem(eInventorySlot.DistanceWeapon);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Whether the living is actually attacking something.
        /// </summary>
        public virtual bool IsAttacking
        {
            //get { return (AttackState && (m_attackAction != null) && m_attackAction.IsAlive); }
            get { return (AttackState && (attackAction != null)); }
        }

        /// <summary>
        /// Checks whether Living has ability to use lefthanded weapons
        /// </summary>
        public bool CanUseLefthandedWeapon
        {
            get
            {
                if (owner is GamePlayer)
                    return (owner as GamePlayer).CharacterClass.CanUseLefthandedWeapon;
                else
                    return false;
            }
        }

        /// <summary>
        /// Calculates how many times left hand swings
        /// </summary>
        public int CalculateLeftHandSwingCount()
        {
            if (owner is GamePlayer)
            {
                if (CanUseLefthandedWeapon == false)
                    return 0;

                if (owner.GetBaseSpecLevel(Specs.Left_Axe) > 0)
                {
                    if (owner is GamePlayer ptemp && ptemp.UseDetailedCombatLog)
                    {
                        int LASpec = owner.GetModifiedSpecLevel(Specs.Left_Axe);
                        double effectiveness = 0;
                        if (LASpec > 0)
                        {
                            effectiveness = 0.625 + 0.0034 * LASpec;
                        }

                        ptemp.Out.SendMessage(
                            $"{Math.Round(effectiveness * 100, 2)}% dmg (after LA penalty) \n",
                            eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                    }

                    return 1; // always use left axe
                }
                

                int specLevel = Math.Max(owner.GetModifiedSpecLevel(Specs.Celtic_Dual),
                    owner.GetModifiedSpecLevel(Specs.Dual_Wield));
                specLevel = Math.Max(specLevel, owner.GetModifiedSpecLevel(Specs.Fist_Wraps));

                decimal tmpOffhandChance = (25 + (specLevel - 1) * 68 / 100);
                tmpOffhandChance += owner.GetModified(eProperty.OffhandChance) +
                                    owner.GetModified(eProperty.OffhandDamageAndChance);
                
                if (owner is GamePlayer p && p.UseDetailedCombatLog && owner.GetModifiedSpecLevel(Specs.HandToHand) <= 0)
                {
                    p.Out.SendMessage(
                        $"OH swing%: {Math.Round(tmpOffhandChance, 2)} ({owner.GetModified(eProperty.OffhandChance) + owner.GetModified(eProperty.OffhandDamageAndChance)}% from RAs) \n",
                        eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                }
                
                if (specLevel > 0)
                {
                    return Util.Chance((int) tmpOffhandChance) ? 1 : 0;
                }

                // HtH chance
                specLevel = owner.GetModifiedSpecLevel(Specs.HandToHand);
                InventoryItem attackWeapon = AttackWeapon;
                InventoryItem leftWeapon = (owner.Inventory == null)
                    ? null
                    : owner.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
                if (specLevel > 0 && attackWeapon != null && //attackWeapon.Object_Type == (int) eObjectType.HandToHand &&
                    leftWeapon != null && leftWeapon.Object_Type == (int) eObjectType.HandToHand)
                {
                    specLevel--;
                    int randomChance = Util.Random(99);
                    int hitChance = specLevel >> 1;

                    
                    if (owner is GamePlayer pl && pl.UseDetailedCombatLog)
                    {
                        pl.Out.SendMessage(
                            $"Chance for 2 hits: {hitChance}% | 3 hits: {specLevel >> 2}% | 4 hits: {specLevel >> 4}% \n",
                            eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                    }
                    
                    if (randomChance < hitChance)
                        return 1; // 1 hit = spec/2
                    
                    hitChance += specLevel >> 2;
                    if (randomChance < hitChance)
                        return 2; // 2 hits = spec/4
                    
                    hitChance += specLevel >> 4;
                    if (randomChance < hitChance)
                        return 3; // 3 hits = spec/16

                    return 0;
                }
                
                
            }

            return 0;
        }

        /// <summary>
        /// Returns a multiplier to adjust left hand damage
        /// </summary>
        /// <returns></returns>
        public double CalculateLeftHandEffectiveness(InventoryItem mainWeapon, InventoryItem leftWeapon)
        {
            double effectiveness = 1.0;

            if (owner is GamePlayer)
            {
                if (CanUseLefthandedWeapon && leftWeapon != null &&
                    leftWeapon.Object_Type == (int) eObjectType.LeftAxe && mainWeapon != null &&
                    (mainWeapon.Item_Type == Slot.RIGHTHAND || mainWeapon.Item_Type == Slot.LEFTHAND))
                {
                    int LASpec = owner.GetModifiedSpecLevel(Specs.Left_Axe);
                    if (LASpec > 0)
                    {
                        effectiveness = 0.625 + 0.0034 * LASpec;
                    }
                }
            }

            return effectiveness;
        }

        /// <summary>
        /// Returns a multiplier to adjust right hand damage
        /// </summary>
        /// <param name="leftWeapon"></param>
        /// <returns></returns>
        public double CalculateMainHandEffectiveness(InventoryItem mainWeapon, InventoryItem leftWeapon)
        {
            double effectiveness = 1.0;

            if (owner is GamePlayer)
            {
                if (CanUseLefthandedWeapon && leftWeapon != null &&
                    leftWeapon.Object_Type == (int) eObjectType.LeftAxe && mainWeapon != null &&
                    (mainWeapon.Item_Type == Slot.RIGHTHAND || mainWeapon.Item_Type == Slot.LEFTHAND))
                {
                    int LASpec = owner.GetModifiedSpecLevel(Specs.Left_Axe);
                    if (LASpec > 0)
                    {
                        effectiveness = 0.625 + 0.0034 * LASpec;
                    }
                }
            }

            return effectiveness;
        }
    }
}