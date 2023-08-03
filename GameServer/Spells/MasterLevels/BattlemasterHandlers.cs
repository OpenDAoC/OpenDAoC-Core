using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Events;

namespace DOL.GS.Spells
{
    //http://www.camelotherald.com/masterlevels/ma.php?ml=Battlemaster
    #region Battlemaster-1
    [SpellHandlerAttribute("MLEndudrain")]
    public class MLEnduDrainHandler : MasterLevelHandling
    {
        public MLEnduDrainHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }


        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }


        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;
            //spell damage should 25;
            int end = (int)(Spell.Damage);
            target.ChangeEndurance(target, EEnduranceChangeType.Spell, (-end));

            if (target is GamePlayer)
                ((GamePlayer)target).Out.SendMessage(" You lose " + end + " endurance!", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);
            (m_caster as GamePlayer).Out.SendMessage("" + target.Name + " loses " + end + " endurance!", EChatType.CT_YouWereHit, EChatLoc.CL_SystemWindow);

            target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.EAttackType.Spell, Caster);
        }
    }
    #endregion

    #region Battlemaster-2
    [SpellHandlerAttribute("KeepDamageBuff")]
    public class KeepDamageBuffHandler : MasterLevelBuffHandling
    {
        public override EProperty Property1 { get { return EProperty.KeepDamage; } }

        public KeepDamageBuffHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    #region Battlemaster-3
    [SpellHandlerAttribute("MLManadrain")]
    public class MLManaDrainHandler : MasterLevelHandling
    {
        public MLManaDrainHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            //spell damage shood be 50-100 (thats the amount power tapped on use) i recommend 90 i think thats it but cood be wrong
            int mana = (int)(Spell.Damage);
            target.ChangeMana(target, EPowerChangeType.Spell, (-mana));

            target.StartInterruptTimer(target.SpellInterruptDuration, AttackData.EAttackType.Spell, Caster);
        }
    }
    #endregion

    #region Battlemaster-4
    [SpellHandlerAttribute("Grapple")]
    public class GrappleHandler : MasterLevelHandling
    {
        private int check = 0;
        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (selectedTarget is GameNpc == true)
            {
                MessageToCaster("This spell works only on realm enemys.", EChatType.CT_SpellResisted);
                return false;
            }
            return base.CheckBeginCast(selectedTarget);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            if (effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;
				if (player.EffectList.GetOfType<NfRaChargeEffect>() == null && player != null)
                {
                    effect.Owner.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, effect, 0);
                    player.Client.Out.SendUpdateMaxSpeed();
                    check = 1;
                }
                effect.Owner.attackComponent.StopAttack();
                effect.Owner.StopCurrentSpellcast();
                effect.Owner.DisarmedTime = effect.Owner.CurrentRegion.Time + Spell.Duration;
            }
            base.OnEffectStart(effect);
        }

        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            return Spell.Duration;
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override bool IsOverwritable(GameSpellEffect compare)
        {
            return false;
        }

        public override void FinishSpellCast(GameLiving target)
        {
            if (m_spell.SubSpellID > 0)
            {
                Spell spell = SkillBase.GetSpellByID(m_spell.SubSpellID);
                if (spell != null && spell.SubSpellID == 0)
                {
                    ISpellHandler spellhandler = ScriptMgr.CreateSpellHandler(m_caster, spell, SkillBase.GetSpellLine(GlobalSpellsLines.Reserved_Spells));
                    spellhandler.StartSpell(Caster);
                }
            }
            base.FinishSpellCast(target);
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (effect.Owner == null) return 0;

            base.OnEffectExpires(effect, noMessages);

            GamePlayer player = effect.Owner as GamePlayer;

            if (check > 0 && player != null)
            {
                effect.Owner.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, effect);
                player.Client.Out.SendUpdateMaxSpeed();
            }

            //effect.Owner.IsDisarmed = false;
            return 0;
        }
		
		/// <summary>
		/// Do not trigger SubSpells
		/// </summary>
		/// <param name="target"></param>
		public override void CastSubSpells(GameLiving target)
		{
		}

        public GrappleHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //ml5 in database Target shood be Group if PvP..Realm if RvR..Value = spell proc'd (a.k the 80value dd proc)
    #region Battlemaster-5
    [SpellHandler("EssenceFlamesProc")]
    public class EssenceFlamesProcHandler : OffensiveProcSpellHandler
    {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
        /// Handler fired whenever effect target is attacked
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        protected override void EventHandler(CoreEvent e, object sender, EventArgs arguments)
        {
            AttackFinishedEventArgs args = arguments as AttackFinishedEventArgs;
            if (args == null || args.AttackData == null)
            {
                return;
            }
            AttackData ad = args.AttackData;
            if (ad.AttackResult != EAttackResult.HitUnstyled && ad.AttackResult != EAttackResult.HitStyle)
                return;

            int baseChance = Spell.Frequency / 100;

            if (ad.IsMeleeAttack)
            {
                if (sender is GamePlayer)
                {
                    GamePlayer player = (GamePlayer)sender;
                    InventoryItem leftWeapon = player.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
                    // if we can use left weapon, we have currently a weapon in left hand and we still have endurance,
                    // we can assume that we are using the two weapons.
                    if (player.attackComponent.CanUseLefthandedWeapon && leftWeapon != null && leftWeapon.Object_Type != (int)EObjectType.Shield)
                    {
                        baseChance /= 2;
                    }
                }
            }

            if (baseChance < 1)
                baseChance = 1;

            if (UtilCollection.Chance(baseChance))
            {
                ISpellHandler handler = ScriptMgr.CreateSpellHandler((GameLiving)sender, m_procSpell, m_procSpellLine);
                if (handler != null)
                {
                    if (m_procSpell.Target.ToLower() == "enemy")
                        handler.StartSpell(ad.Target);
                    else if (m_procSpell.Target.ToLower() == "self")
                        handler.StartSpell(ad.Attacker);
                    else if (m_procSpell.Target.ToLower() == "group")
                    {
                        GamePlayer player = Caster as GamePlayer;
                        if (Caster is GamePlayer)
                        {
                            if (player.Group != null)
                            {
                                foreach (GameLiving groupPlayer in player.Group.GetMembersInTheGroup())
                                {
                                    if (player.IsWithinRadius(groupPlayer, m_procSpell.Range))
                                    {
                                        handler.StartSpell(groupPlayer);
                                    }
                                }
                            }
                            else
                                handler.StartSpell(player);
                        }
                    }
                    else
                    {
                        log.Warn("Skipping " + m_procSpell.Target + " proc " + m_procSpell.Name + " on " + ad.Target.Name + "; Realm = " + ad.Target.Realm);
                    }
                }
            }
        }

        // constructor
        public EssenceFlamesProcHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

	#region Battlemaster-6
	// LifeFlight
    [SpellHandler("ThrowWeapon")]
    public class ThrowWeaponHandler : DirectDmgHandler
 	{
        #region Disarm Weapon
        protected static Spell Disarm_Weapon;
        public static Spell Disarmed
        {
            get
            {
                if (Disarm_Weapon == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Uninterruptible = true;
                    spell.Icon = 7293;
                    spell.ClientEffect = 7293;
                    spell.Description = "Disarms the caster.";
                    spell.Name = "Throw Weapon(Disarm)";
                    spell.Range = 0;
                    spell.Value = 0;
                    spell.Duration = 10;
                    spell.SpellID = 900100;
                    spell.Target = "Self";
                    spell.Type = ESpellType.Disarm.ToString();
                    Disarm_Weapon = new Spell(spell, 50);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Combat_Styles_Effect, Disarm_Weapon);
                }
                return Disarm_Weapon;
            }
        }
        #endregion
        public const string DISABLE = "ThrowWeapon.Shortened.Disable.Timer";
		public override bool CheckBeginCast(GameLiving selectedTarget)
		{
			GamePlayer player = Caster as GamePlayer;
			if(player == null) 
                return false;

            if (player.IsDisarmed)
            {
                MessageToCaster("You are disarmed and can't use this spell!", EChatType.CT_YouHit);
                return false;
            }

			InventoryItem weapon = null;

            //assign the weapon the player is using, it can be a twohanded or a standard slot weapon
			if (player.ActiveWeaponSlot.ToString() == "TwoHanded") 
                weapon = player.Inventory.GetItem((eInventorySlot)12);
			if (player.ActiveWeaponSlot.ToString() == "Standard")
                weapon = player.Inventory.GetItem((eInventorySlot)10);
            
            //if the weapon is null, ie. they don't have an appropriate weapon active
			if(weapon == null) 
            { 
                MessageToCaster("Equip a weapon before using this spell!",EChatType.CT_SpellResisted); 
                return false; 
            }

            return base.CheckBeginCast(selectedTarget);
		}
		
        //Throw Weapon does not "resist"
		public override int CalculateSpellResistChance(GameLiving target) 
        { 
            return 0; 
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            return base.OnEffectExpires(effect, noMessages);
        }

        
        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target == null) return;

            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            // calc damage
            AttackData ad = CalculateDamageToTarget(target, effectiveness);
            SendDamageMessages(ad);
            DamageTarget(ad, true);            
            target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
        }
        
        
        public override void DamageTarget(AttackData ad, bool showEffectAnimation)
        {
            InventoryItem weapon = null;
            weapon = ad.Weapon;

            if (showEffectAnimation && ad.Target != null)
            {
                byte resultByte = 0;
                int attackersWeapon = (weapon == null) ? 0 : weapon.Model;
                int defendersWeapon = 0;

                switch (ad.AttackResult)
                {
                    case EAttackResult.Missed: resultByte = 0; break;
                    case EAttackResult.Evaded: resultByte = 3; break;
                    case EAttackResult.Fumbled: resultByte = 4; break;
                    case EAttackResult.HitUnstyled: resultByte = 10; break;
                    case EAttackResult.HitStyle: resultByte = 11; break;
                    case EAttackResult.Parried:
                        resultByte = 1;
                        if (ad.Target != null && ad.Target.ActiveWeapon != null)
                        {
                            defendersWeapon = ad.Target.ActiveWeapon.Model;
                        }
                        break;
                    case EAttackResult.Blocked:
                        resultByte = 2;
                        if (ad.Target != null && ad.Target.Inventory != null)
                        {
                            InventoryItem lefthand = ad.Target.Inventory.GetItem(eInventorySlot.LeftHandWeapon);
                            if (lefthand != null && lefthand.Object_Type == (int)EObjectType.Shield)
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
                    //We don't need to send the animiation for the throwning, thats been done earlier.

                    //this is for the defender, which should show the appropriate animation
                    player.Out.SendCombatAnimation(null, ad.Target, (ushort)attackersWeapon, (ushort)defendersWeapon, animationId, 0, resultByte, ad.Target.HealthPercent);
                
                }
            }

            // send animation before dealing damage else dead livings show no animation
            ad.Target.OnAttackedByEnemy(ad);
            ad.Attacker.DealDamage(ad);
            if (ad.Damage == 0 && ad.Target is GameNpc)
            {
                IOldAggressiveBrain aggroBrain = ((GameNpc)ad.Target).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(Caster, 1);
            }
            
        }

        public override void SendDamageMessages(AttackData ad)
        {
			GameObject target = ad.Target;
			InventoryItem weapon = ad.Weapon;
            GamePlayer player = Caster as GamePlayer;

			switch (ad.AttackResult)
			{
				case EAttackResult.TargetNotVisible: player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.NotInView", ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow); break;
                case EAttackResult.OutOfRange: player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.TooFarAway", ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow); break;
                case EAttackResult.TargetDead: player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.AlreadyDead", ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow); break;
                case EAttackResult.Blocked: player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Blocked", ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow); break;
                case EAttackResult.Parried: player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Parried", ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow); break;
                case EAttackResult.Evaded: player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Evaded", ad.Target.GetName(0, true)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow); break;
                case EAttackResult.NoTarget: player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.NeedTarget"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow); break;
                case EAttackResult.NoValidTarget: player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.CantBeAttacked"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow); break;
                case EAttackResult.Missed: player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Miss"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow); break;
                case EAttackResult.Fumbled: player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Fumble"), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow); break;
                case EAttackResult.HitStyle:
                case EAttackResult.HitUnstyled:
					string modmessage = "";
					if (ad.Modifier > 0) modmessage = " (+" + ad.Modifier + ")";
					if (ad.Modifier < 0) modmessage = " (" + ad.Modifier + ")";

					string hitWeapon = "";

					switch (ServerProperties.ServerProperties.SERV_LANGUAGE)
					{
						case "EN":
							if (weapon != null)
								hitWeapon = GlobalConstants.NameToShortName(weapon.Name);
							break;
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
						hitWeapon = " " + LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.WithYour") + " " + hitWeapon;

					string attackTypeMsg = LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.YouAttack");
 
					// intercept messages
					if (target != null && target != ad.Target)
					{
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Intercepted", ad.Target.GetName(0, true), target.GetName(0, false)), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.InterceptedHit", attackTypeMsg, target.GetName(0, false), hitWeapon, ad.Target.GetName(0, false), ad.Damage, modmessage), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
					}
					else
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.InterceptHit", attackTypeMsg, ad.Target.GetName(0, false), hitWeapon, ad.Damage, modmessage), EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);

					// critical hit
					if (ad.CriticalDamage > 0)
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GamePlayer.Attack.Critical", ad.Target.GetName(0, false), ad.CriticalDamage) + $" ({ad.Attacker.attackComponent.AttackCriticalChance(null, ad.Weapon)}%)", EChatType.CT_YouHit, EChatLoc.CL_SystemWindow);
					break;
			}
        }

        public override void FinishSpellCast(GameLiving target)
        {
            base.FinishSpellCast(target);

            //we need to make sure the spell is only disabled if the attack was a success
            int isDisabled = Caster.TempProperties.getProperty<int>(DISABLE);
            
            //if this value is greater than 0 then we know that their weapon did not damage the target
            //the skill's disable timer should be set to their attackspeed 
            if (isDisabled > 0)
            {
                Caster.DisableSkill(Spell, isDisabled);
                
                //remove the temp property
                Caster.TempProperties.removeProperty(DISABLE);
            }
            else
            {
                //they disarm them selves.
                Caster.CastSpell(Disarmed, (SkillBase.GetSpellLine(GlobalSpellsLines.Combat_Styles_Effect)));
            }
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            GamePlayer player = target as GamePlayer;
          
            foreach (GamePlayer visPlayer in Caster.GetPlayersInRadius((ushort)WorldMgr.VISIBILITY_DISTANCE))
            {
                visPlayer.Out.SendCombatAnimation(Caster, target, 0x0000, 0x0000, (ushort)408, 0, 0x00, target.HealthPercent);
            }
            
            OnDirectEffect(target, effectiveness);

        }

        public override AttackData CalculateDamageToTarget(GameLiving target, double effectiveness)
        {
            GamePlayer player = Caster as GamePlayer;

            if (player == null)
                return null;

            InventoryItem weapon = null;

            if (player.ActiveWeaponSlot.ToString() == "TwoHanded")
                weapon = player.Inventory.GetItem((eInventorySlot)12);
            if (player.ActiveWeaponSlot.ToString() == "Standard")
                weapon = player.Inventory.GetItem((eInventorySlot)10);

            if (weapon == null)
                return null;

            //create the AttackData
            AttackData ad = new AttackData();
            ad.Attacker = player;
            ad.Target = target;
            ad.Damage = 0;
            ad.CriticalDamage = 0;
            ad.WeaponSpeed = player.AttackSpeed(weapon) / 100;
            ad.DamageType = player.attackComponent.AttackDamageType(weapon);
            ad.Weapon = weapon;
            ad.IsOffHand = weapon.Hand == 2;
            //we need to figure out which armor piece they are going to hit.
            //figure out the attacktype
            switch (weapon.Item_Type)
            {
                default:
                case Slot.RIGHTHAND:
                case Slot.LEFTHAND:
                    ad.AttackType = AttackData.EAttackType.MeleeOneHand;
                    break;
                case Slot.TWOHAND:
                    ad.AttackType = AttackData.EAttackType.MeleeTwoHand;
                    break;
            }
            //Throw Weapon is subject to all the conventional attack results, parry, evade, block, etc.
            ad.AttackResult = ad.Target.attackComponent.CalculateEnemyAttackResult(null, ad, weapon);

            if (ad.AttackResult == EAttackResult.HitUnstyled || ad.AttackResult == EAttackResult.HitStyle)
            {
                //we only need to calculate the damage if the attack was a success.
                double damage = player.attackComponent.AttackDamage(weapon) * effectiveness;

                if (target is GamePlayer)
                    ad.ArmorHitLocation = ((GamePlayer)target).CalculateArmorHitLocation(ad);

                InventoryItem armor = null;
                if (target.Inventory != null)
                    armor = target.Inventory.GetItem((eInventorySlot)ad.ArmorHitLocation);

                //calculate the lowerboundary of the damage
                int lowerboundary = (player.WeaponSpecLevel(weapon) - 1) * 50 / (ad.Target.EffectiveLevel + 1) + 75;
                lowerboundary = Math.Max(lowerboundary, 75);
                lowerboundary = Math.Min(lowerboundary, 125);

                damage *= (player.GetWeaponSkill(weapon) + 90.68) / (ad.Target.GetArmorAF(ad.ArmorHitLocation) + 20 * 4.67);

                //If they have badge of Valor, we need to modify the damage
				if (ad.Attacker.EffectList.GetOfType<NfRaBadgeOfValorEffect>() != null)
                    damage *= 1.0 + Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));
                else
                    damage *= 1.0 - Math.Min(0.85, ad.Target.GetArmorAbsorb(ad.ArmorHitLocation));

                damage *= (lowerboundary + UtilCollection.Random(50)) * 0.01;

                ad.Modifier = (int)(damage * (ad.Target.GetResist(ad.DamageType) + SkillBase.GetArmorResist(armor, ad.DamageType)) * -0.01);

                damage += ad.Modifier;

                int resist = (int)(damage * ad.Target.GetDamageResist(target.GetResistTypeForDamage(ad.DamageType)) * -0.01);
                EProperty property = ad.Target.GetResistTypeForDamage(ad.DamageType);
                int secondaryResistModifier = ad.Target.SpecBuffBonusCategory[(int)property];
                int resistModifier = 0;
                resistModifier += (int)((ad.Damage + (double)resistModifier) * (double)secondaryResistModifier * -0.01);
                damage += resist;
                damage += resistModifier;
                ad.Modifier += resist;
                ad.Damage = (int)damage;
                ad.Damage = Math.Min(ad.Damage, (int)(player.attackComponent.UnstyledDamageCap(weapon) * effectiveness));
                ad.Damage = (int)((double)ad.Damage * ServerProperties.ServerProperties.PVP_MELEE_DAMAGE);
                if (ad.Damage == 0) ad.AttackResult = DOL.GS.EAttackResult.Missed;
                ad.CriticalDamage = player.attackComponent.GetMeleeCriticalDamage(ad, null, weapon);
            }
            else
            {
                //They failed, do they do not get disarmed, and the spell is not disabled for the full duration,
                //just the modified swing speed, this is in milliseconds
                int attackSpeed = player.AttackSpeed(weapon);
                player.TempProperties.setProperty(DISABLE, attackSpeed);
            }
            return ad;
        }
		public ThrowWeaponHandler(GameLiving caster,Spell spell,SpellLine line) : base(caster,spell,line) {}
	}
	#endregion

    //essence debuff
    #region Battlemaster-7
    [SpellHandlerAttribute("EssenceSearHandler")]
    public class EssenceSearHandler : SpellHandler
    {
        public override int CalculateSpellResistChance(GameLiving target) { return 0; }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            GameLiving living = effect.Owner as GameLiving;
            //value should be 15 to reduce Essence resist
            living.DebuffCategory[(int)EProperty.Resist_Natural] += (int)m_spell.Value;

            if (effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;
                player.Out.SendCharStatsUpdate();
                player.UpdateEncumberance();
                player.UpdatePlayerStatus();
                player.Out.SendUpdatePlayer();
            }
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameLiving living = effect.Owner as GameLiving;
            living.DebuffCategory[(int)EProperty.Resist_Natural] -= (int)m_spell.Value;

            if (effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;
                player.Out.SendCharStatsUpdate();
                player.UpdatePlayerStatus();
                player.Out.SendUpdatePlayer();
            }
            return base.OnEffectExpires(effect, noMessages);
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            base.ApplyEffectOnTarget(target, effectiveness);
            if (target.Realm == 0 || Caster.Realm == 0)
            {
                target.LastAttackedByEnemyTickPvE = target.CurrentRegion.Time;
                Caster.LastAttackTickPvE = Caster.CurrentRegion.Time;
            }
            else
            {
                target.LastAttackedByEnemyTickPvP = target.CurrentRegion.Time;
                Caster.LastAttackTickPvP = Caster.CurrentRegion.Time;
            }
            if (target is GameNpc)
            {
                IOldAggressiveBrain aggroBrain = ((GameNpc)target).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
            }
        }
        public EssenceSearHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    #region Battlemaster-8
    [SpellHandlerAttribute("BodyguardHandler")]
    public class BodyguardHandler : SpellHandler
    {
        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
        //    if (Caster.Group.MemberCount <= 2)
        //    {
        //        MessageToCaster("Your group is to small to use this spell.", eChatType.CT_Important);
        //        return false;
        //    }
              return base.CheckBeginCast(selectedTarget);
        
        }
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add(Spell.Description);
                return list;
            }
        }
        public BodyguardHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //for ML9 in the database u have to add  EssenceDampenHandler  in type (its a new method customly made) 
    #region Battlemaster-9
    [SpellHandlerAttribute("EssenceDampenHandler")]
    public class EssenceDampenHandler : SpellHandler
    {
        protected int DexDebuff = 0;
        protected int QuiDebuff = 0;
        public override int CalculateSpellResistChance(GameLiving target) { return 0; }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            double percentValue = (m_spell.Value) / 100;//15 / 100 = 0.15 a.k (15%) 100dex * 0.15 = 15dex debuff 
            DexDebuff = (int)((double)effect.Owner.GetModified(EProperty.Dexterity) * percentValue);
            QuiDebuff = (int)((double)effect.Owner.GetModified(EProperty.Quickness) * percentValue);
            GameLiving living = effect.Owner as GameLiving;
            living.DebuffCategory[(int)EProperty.Dexterity] += DexDebuff;
            living.DebuffCategory[(int)EProperty.Quickness] += QuiDebuff;

            if (effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;
                player.Out.SendCharStatsUpdate();
                player.UpdatePlayerStatus();
                player.Out.SendUpdatePlayer();
            }
        }
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameLiving living = effect.Owner as GameLiving;
            living.DebuffCategory[(int)EProperty.Dexterity] -= DexDebuff;
            living.DebuffCategory[(int)EProperty.Quickness] -= QuiDebuff;

            if (effect.Owner is GamePlayer)
            {
                GamePlayer player = effect.Owner as GamePlayer;
                player.Out.SendCharStatsUpdate();
                player.UpdatePlayerStatus();
                player.Out.SendUpdatePlayer();
            }
            return base.OnEffectExpires(effect, noMessages);
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            base.ApplyEffectOnTarget(target, effectiveness);
            if (target.Realm == 0 || Caster.Realm == 0)
            {
                target.LastAttackedByEnemyTickPvE = target.CurrentRegion.Time;
                Caster.LastAttackTickPvE = Caster.CurrentRegion.Time;
            }
            else
            {
                target.LastAttackedByEnemyTickPvP = target.CurrentRegion.Time;
                Caster.LastAttackTickPvP = Caster.CurrentRegion.Time;
            }
            if (target is GameNpc)
            {
                IOldAggressiveBrain aggroBrain = ((GameNpc)target).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
            }
        }
        public EssenceDampenHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
    #endregion

    //ml10 in database Type shood be RandomBuffShear
}

#region KeepDamageCalc

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The melee damage bonus percent calculator
    ///
    /// BuffBonusCategory1 is used for buffs
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 is used for debuff
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(EProperty.KeepDamage)]
    public class KeepDamagePercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, EProperty property)
        {
            int percent = 100
                +living.BaseBuffBonusCategory[(int)property];

            return percent;
        }
    }
}

#endregion