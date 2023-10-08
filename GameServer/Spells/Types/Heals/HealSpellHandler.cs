using System;
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.Spells
{
    /// <summary>
    /// 
    /// </summary>
    [SpellHandler("Heal")]
    public class HealSpellHandler : SpellHandler
    {
        // constructor
        public HealSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Execute heal spell
        /// </summary>
        /// <param name="target"></param>
        public override bool StartSpell(GameLiving target)
        {
            if (target is null && Spell.Target == ESpellTarget.PET)
                target = Caster;

            var targets = SelectTargets(target);

            if (targets.Count <= 0)
                return false;

            bool healed = false;
            int minHeal;
            int maxHeal;

            CalculateHealVariance(out minHeal, out maxHeal);

            foreach (GameLiving healTarget in targets)
            {
                int heal = Util.Random(minHeal, maxHeal);

				if (SpellLine.KeyName == GlobalSpellsLines.Item_Effects)
				{
					heal = maxHeal;
				}

                if (healTarget.IsDiseased)
                {
                    MessageToCaster("Your target is diseased!", eChatType.CT_SpellResisted);
                    heal >>= 1;
                }

				if (SpellLine.KeyName == GlobalSpellsLines.Item_Effects)
				{
					healed |= ProcHeal(healTarget, heal);
				}
				else
				{
					healed |= HealTarget(healTarget, heal);
				}
            }

            // group heals seem to use full power even if no heals
            if (!healed && Spell.Target == ESpellTarget.REALM)
                m_caster.Mana -= PowerCost(target) >> 1; // only 1/2 power if no heal
            else
                m_caster.Mana -= PowerCost(target);

            // send animation for non pulsing spells only
            if (Spell.Pulse == 0)
            {
                // show resisted effect if not healed
				foreach (GameLiving healTarget in targets)
					if(healTarget.IsAlive)
						SendEffectAnimation(healTarget, 0, false, healed ? (byte)1 : (byte)0);
            }

            if (!healed && Spell.CastTime == 0) m_startReuseTimer = false;

			return true;
		}

        /// <summary>
        /// Heals hit points of one target and sends needed messages, no spell effects
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount">amount of hit points to heal</param>
        /// <returns>true if heal was done</returns>
        public virtual bool HealTarget(GameLiving target, int amount)
        {
            return HealTarget(target, (double)amount);
        }

        /// <summary>
        /// Heals hit points of one target and sends needed messages, no spell effects
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount">amount of hit points to heal</param>
        /// <returns>true if heal was done</returns>
        public virtual bool HealTarget(GameLiving target, double amount)
        {
            if (target == null || target.ObjectState != GameLiving.eObjectState.Active) return false;

			// we can't heal people we can attack
			if (GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
				return false;

			// no healing of keep components
			if (target is Keeps.GameKeepComponent || target is Keeps.GameKeepDoor)
				return false;

			if (!target.IsAlive)
            {
                //"You cannot heal the dead!" sshot550.tga
                MessageToCaster(target.GetName(0, true) + " is dead!", eChatType.CT_SpellResisted);
                return false;
            }

            if (target is GamePlayer && (target as GamePlayer).NoHelp && Caster is GamePlayer && target != Caster)
            {
                //player not grouped, anyone else
                //player grouped, different group
                if ((target as GamePlayer).Group == null ||
                    (Caster as GamePlayer).Group == null ||
                    (Caster as GamePlayer).Group != (target as GamePlayer).Group)
                {
                    MessageToCaster("That player does not want assistance", eChatType.CT_SpellResisted);
                    return false;
                }
            }

            /// [Atlas - Takii] Disabling MOC effectiveness scaling in OF.
//             double mocFactor = 1.0;
//         	MasteryofConcentrationEffect moc = Caster.EffectList.GetOfType<MasteryofConcentrationEffect>();
//             if (moc != null)
//             {
//                 GamePlayer playerCaster = Caster as GamePlayer;
//                 AtlasOF_MasteryofConcentration ra = playerCaster.GetAbility<AtlasOF_MasteryofConcentration>();
//                 if (ra != null)
//                 	mocFactor = (double)ra.GetAmountForLevel(ra.Level) / 100.0;
//                 amount = amount * mocFactor;
//             }

            double criticalvalue = 0;
            int criticalchance = Caster.GetModified(EProperty.CriticalHealHitChance);
            double effectiveness = 0;
            if (Caster is GamePlayer)
                effectiveness = (Caster as GamePlayer).Effectiveness + (double)(Caster.GetModified(EProperty.HealingEffectiveness)) * 0.01;
            if (Caster is GameNPC)
                effectiveness = 1.0;

            //USE DOUBLE !
            double cache = amount * effectiveness;

            amount = cache;

            int randNum = Util.CryptoNextInt(0, 100); //grab our random number

            if (this.Caster is GamePlayer spellCaster && spellCaster.UseDetailedCombatLog)
            {
                spellCaster.Out.SendMessage($"heal crit chance: {criticalchance} random: {randNum}", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
                spellCaster.Out.SendMessage($"heal effectiveness: {Caster.GetModified(EProperty.HealingEffectiveness)}%", eChatType.CT_DamageAdd, eChatLoc.CL_SystemWindow);
            }

            if (criticalchance > randNum)
            {
                double minValue = amount / 10;
                double maxValue = amount / 2 + 1;
                criticalvalue = Util.RandomDouble() * (maxValue - minValue) + minValue;
                criticalvalue = Math.Round(criticalvalue, MidpointRounding.ToEven); /// [Atlas - Takii] Round crit value for simplicity and to remove decimals from combat log.
            }

            amount += criticalvalue;

            GamePlayer playerTarget = target as GamePlayer;
            /*
			if (playerTarget != null)
			{
				GameSpellEffect HealEffect = SpellHandler.FindEffectOnTarget(playerTarget, "EfficientHealing");
				if (HealEffect != null)
				{
					double HealBonus = amount * ((int)HealEffect.Spell.Value * 0.01);
					amount += (int)HealBonus;
					playerTarget.Out.SendMessage("Your Efficient Healing buff grants you a additional" + HealBonus + " in the Heal!", eChatType.CT_Spell, eChatLoc.CL_ChatWindow);
				}
				GameSpellEffect EndEffect = SpellHandler.FindEffectOnTarget(playerTarget, "EfficientEndurance");
				if (EndEffect != null)
				{
					double EndBonus = amount * ((int)EndEffect.Spell.Value * 0.01);
					//600 / 10 = 60end
					playerTarget.Endurance += (int)EndBonus;
					playerTarget.Out.SendMessage("Your Efficient Endurance buff grants you " + EndBonus + " Endurance from the Heal!", eChatType.CT_Spell, eChatLoc.CL_ChatWindow);
				}
			}*/

            /*
            GameSpellEffect flaskHeal = FindEffectOnTarget(target, "HealFlask");
            if(flaskHeal != null)
            {
                amount += (int) ((amount*flaskHeal.Spell.Value)*0.01);
            }*/

            amount = Math.Round(amount);

            #region change target to Necromancer Pet, if Necromancer was targeted for heal
            // check if its a necro in shade mode
            if (target.EffectList.GetOfType<NecromancerShadeEffect>() != null)
            {
                if (target.ControlledBrain != null)
                {
                    if (target.ControlledBrain.Body != null)
                    {
                        if (target.ControlledBrain.Body is NecromancerPet)
                        {
                            //log.Info("Changing target to Necromancer PET");
                            target = target.ControlledBrain.Body as NecromancerPet;
                        }
                    }
                }
            }
            #endregion

            int heal = target.ChangeHealth(Caster, EHealthChangeType.Spell, (int)amount);

            #region PVP DAMAGE

            long healedrp = 0;

            if (target.DamageRvRMemory > 0 &&
                (target is NecromancerPet &&
                ((target as NecromancerPet).Brain as IControlledBrain).GetPlayerOwner() != null
                || target is GamePlayer))
            {
                healedrp = (long)Math.Max(heal, 0);
                target.DamageRvRMemory -= healedrp;

                //if we heal a target that is in RvR, the healer will get rewards from their heal target's target
                if(target.TargetObject is GamePlayer enemy && enemy.Realm != target.Realm)
                {
                    enemy.AddXPGainer(m_caster, healedrp);
                }
            }

            if (heal == 0)
            {
                if (Spell.Pulse == 0)
                {
                    if (target == m_caster) 
                        MessageToCaster("You are fully healed.", eChatType.CT_SpellResisted);
                    else 
                        MessageToCaster(target.GetName(0, true) + " is fully healed.", eChatType.CT_SpellResisted);
                }
                return false;
            }

            // RP for healing
            
            // if (m_caster is GamePlayer && target is NecromancerPet &&
            //     ((target as NecromancerPet).Brain as IControlledBrain).GetPlayerOwner() != null
            //     || target is GamePlayer && healedrp > 0)
            // {
            //     int POURCENTAGE_SOIN_RP = ServerProperties.Properties.HEAL_PVP_DAMAGE_VALUE_RP; // ...% de bonus RP pour les soins effectués
            //     if(m_caster.Group != null && m_caster.Group.IsInTheGroup(target))
            //     {
            //         //do nothing
            //     }
            //     else if(m_spell.Pulse == 0 && m_caster.CurrentRegionID != 242 && // On Exclu zone COOP
            //         m_spell.SpellType != eSpellType.SpreadHeal && target != m_caster &&
            //         m_spellLine.KeyName != GlobalSpellsLines.Item_Spells &&
            //         m_spellLine.KeyName != GlobalSpellsLines.Potions_Effects &&
            //         m_spellLine.KeyName != GlobalSpellsLines.Combat_Styles_Effect &&
            //         m_spellLine.KeyName != GlobalSpellsLines.Reserved_Spells)
            //     {
            //         GamePlayer player = m_caster as GamePlayer;
            //
            //         if (player != null)
            //         {
            //             long Bonus_RP_Soin = Convert.ToInt64((double)healedrp * POURCENTAGE_SOIN_RP / 100.0);
            //
            //             if (Bonus_RP_Soin >= 1)
            //             {
            //                 PlayerStatistics stats = player.Statistics as PlayerStatistics;
            //
            //                 if (stats != null)
            //                 {
            //                     stats.RPEarnedFromHitPointsHealed += (uint)Bonus_RP_Soin;
            //                     stats.HitPointsHealed += (uint)healedrp;
            //                 }
            //
            //                 player.GainRealmPoints(Bonus_RP_Soin, false);
            //                 player.Out.SendMessage("You gain " + Bonus_RP_Soin.ToString() + " realmpoints for healing a member of your Realm", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            //             }
            //         }
            //     }
            // }

            #endregion PVP DAMAGE

            if (m_caster == target)
            {
                MessageToCaster("You heal yourself for " + heal + " hit points.", eChatType.CT_Spell);
                if (heal < amount)
                {
                    #region PVP DAMAGE

                    if (target is NecromancerPet &&
                        ((target as NecromancerPet).Brain as IControlledBrain).GetPlayerOwner() != null || target is GamePlayer)
                    {
                        if (target.DamageRvRMemory > 0)
                            target.DamageRvRMemory = 0; //Remise a zéro compteur dommages/heal rps
                    }

                    #endregion PVP DAMAGE

                    MessageToCaster("You are fully healed.", eChatType.CT_Spell);
                }
            }
            else
            {
                MessageToCaster("You heal " + target.GetName(0, false) + " for " + heal + " hit points!", eChatType.CT_Spell);
                MessageToLiving(target, "You are healed by " + m_caster.GetName(0, false) + " for " + heal + " hit points.", eChatType.CT_Spell);
                if (heal < amount)
                {

                    #region PVP DAMAGE

                    if (target is NecromancerPet &&
                        ((target as NecromancerPet).Brain as IControlledBrain).GetPlayerOwner() != null || target is GamePlayer)
                    {
                        if (target.DamageRvRMemory > 0)
                            target.DamageRvRMemory = 0; //Remise a zéro compteur dommages/heal rps
                    }

                    #endregion PVP DAMAGE

                    MessageToCaster(target.GetName(0, true) + " is fully healed.", eChatType.CT_Spell);
                }
                if (heal > 0 && criticalvalue > 0)
                    MessageToCaster("Your heal criticals for an extra " + criticalvalue + " hit points!", eChatType.CT_Spell);
            }
            
            //check for conquest activity
            if (target is GamePlayer tp)
            {
                if(ConquestService.ConquestManager.IsPlayerInConquestArea(tp))
                    ConquestService.ConquestManager.AddContributor(tp);
            }

            var attackers = target.attackComponent.Attackers;

            foreach (var gain in attackers.Keys)
            {
                if (gain is GameNPC npc)
                {
                    AttackData ad = new AttackData();
                    ad.Attacker = Caster;
                    ad.Target = target;
                    ad.AttackType = AttackData.eAttackType.Spell;
                    ad.SpellHandler = this;
                    ad.AttackResult = EAttackResult.HitUnstyled;
                    ad.IsSpellResisted = false;
                    ad.Damage = (int)Spell.Value;
                    ad.DamageType = Spell.DamageType;
                    ad.CausesCombat = false;
                    //npc.OnAttackedByEnemy(ad);
                    if (npc.Brain is StandardMobBrain mobBrain)
                    {
                        mobBrain.AddToAggroList(Caster, ad.Damage);
                    }
                    npc.AddXPGainer(Caster, ad.Damage);
                }
            }

            return true;
        }

		/// <summary>
		/// A heal generated by an item proc.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public virtual bool ProcHeal(GameLiving target, int amount)
        { return ProcHeal(target, (double)amount); }


        /// <summary>
        /// A heal generated by an item proc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public virtual bool ProcHeal(GameLiving target, double amount)
        {
			if (target == null || target.ObjectState != GameLiving.eObjectState.Active) return false;

			// we can't heal people we can attack
			if (GameServer.ServerRules.IsAllowedToAttack(Caster, target, true))
				return false;

			if (!target.IsAlive)
				return false;

			// no healing of keep components
			if (target is Keeps.GameKeepComponent || target is Keeps.GameKeepDoor)
				return false;

			int heal = target.ChangeHealth(Caster, EHealthChangeType.Spell, (int)Math.Round(amount));

			if (m_caster == target && heal > 0)
			{
				MessageToCaster("You heal yourself for " + heal + " hit points.", eChatType.CT_Spell);

				if (heal < amount)
				{
					MessageToCaster("You are fully healed.", eChatType.CT_Spell);
                    #region PVP DAMAGE

                    if (target is NecromancerPet &&
                        ((target as NecromancerPet).Brain as IControlledBrain).GetPlayerOwner() != null || target is GamePlayer)
                    {
                        if (target.DamageRvRMemory > 0)
                            target.DamageRvRMemory = 0; //Remise a zéro compteur dommages/heal rps
                    }

                    #endregion PVP DAMAGE
				}
			}
			else if (heal > 0)
			{
				MessageToCaster("You heal " + target.GetName(0, false) + " for " + heal + " hit points!", eChatType.CT_Spell);
				MessageToLiving(target, "You are healed by " + m_caster.GetName(0, false) + " for " + heal + " hit points.", eChatType.CT_Spell);
                
                #region PVP DAMAGE

                if (heal < amount)
                {
                    if (target is NecromancerPet &&
                        ((target as NecromancerPet).Brain as IControlledBrain).GetPlayerOwner() != null || target is GamePlayer)
                    {
                        if (target.DamageRvRMemory > 0)
                            target.DamageRvRMemory = 0; //Remise a zéro compteur dommages/heal rps
                    }
                }
                else
                {
                    if (target is NecromancerPet &&
                        ((target as NecromancerPet).Brain as IControlledBrain).GetPlayerOwner() != null || target is GamePlayer)
                    {
                        if (target.DamageRvRMemory > 0)
                            target.DamageRvRMemory -= (long)Math.Max(heal, 0);
                    }
                }
            }

            #endregion PVP DAMAGE

			return true;
		}


        /// <summary>
        /// Calculates heal variance based on spec
        /// </summary>
        /// <param name="min">store min variance here</param>
        /// <param name="max">store max variance here</param>
        public virtual void CalculateHealVariance(out int min, out int max)
        {
			double spellValue = m_spell.Value;
			GamePlayer casterPlayer = m_caster as GamePlayer;

            if (m_spellLine.KeyName == GlobalSpellsLines.Item_Effects)
			{
				if (m_spell.Value > 0)
				{
					min = (int)(spellValue * 0.75);
					max = (int)(spellValue * 1.25);
					return;
				}
			}

            if (m_spellLine.KeyName == GlobalSpellsLines.Potions_Effects)
            {
                if (m_spell.Value > 0)
                {
                    min = (int)(spellValue * 1.00);
                    max = (int)(spellValue * 1.25);
                    return;
                }
            }

            if (m_spellLine.KeyName == GlobalSpellsLines.Combat_Styles_Effect)
			{
				if (m_spell.Value > 0)
				{
					if (UseMinVariance)
					{
						min = (int)(spellValue * 1.25);
					}
					else
					{
						min = (int)(spellValue * 0.75);
					}

					max = (int)(spellValue * 1.25);
					return;
				}
			}

			if (m_spellLine.KeyName == GlobalSpellsLines.Reserved_Spells)
			{
				min = max = (int)spellValue;
				return;
			}

            // percents if less than zero
            if (spellValue < 0)
            {
                spellValue = (spellValue / -100.0) * m_caster.MaxHealth;

                min = max = (int)spellValue;
                return;
            }

            int upperLimit = (int)(spellValue * 1.25);
            if (upperLimit < 1)
            {
                upperLimit = 1;
            }

            double eff = 1.25;
            if (Caster is GamePlayer)
            {
                double lineSpec = Caster.GetModifiedSpecLevel(m_spellLine.Spec);
                if (lineSpec < 1)
                    lineSpec = 1;
                eff = 0.25;
                if (Spell.Level > 0)
                {
                    eff += (lineSpec - 1.0) / Spell.Level;
                    if (eff > 1.25)
                        eff = 1.25;
                }
            }

            int lowerLimit = (int)(spellValue * eff);
            if (lowerLimit < 1)
            {
                lowerLimit = 1;
            }
            if (lowerLimit > upperLimit)
            {
                lowerLimit = upperLimit;
            }

            min = lowerLimit;
            max = upperLimit;
            return;
        }
    }
}
