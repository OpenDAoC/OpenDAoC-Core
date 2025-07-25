using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Spell to change up to 3 property bonuses at once
	/// in one their specific given bonus category
	/// </summary>
	public abstract class PropertyChangingSpell : SpellHandler
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public override ECSGameSpellEffect CreateECSEffect(in ECSGameEffectInitParams initParams)
		{
			return new StatBuffECSEffect(initParams);
		}

		/// <summary>
		/// Execute property changing spell
		/// </summary>
		/// <param name="target"></param>
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		protected override int CalculateEffectDuration(GameLiving target)
		{
			if (HasPositiveEffect)
			{
				double duration = Spell.Duration;
				duration *= 1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01;

				if (Spell.InstrumentRequirement != 0)
				{
					DbInventoryItem instrument = Caster.ActiveWeapon;
					if (instrument != null)
					{
						duration *= 1.0 + Math.Min(1.0, instrument.Level / (double)Caster.Level); // up to 200% duration for songs
						duration *= instrument.Condition / (double)instrument.MaxCondition * instrument.Quality / 100;
					}
				}

				if (duration < 1)
					duration = 1;
				else if (duration > (Spell.Duration * 4))
					duration = Spell.Duration * 4;

				return (int) duration;
			}

			return base.CalculateEffectDuration(target);
		}

		public override void ApplyEffectOnTarget(GameLiving target)
		{
			
			GamePlayer player = target as GamePlayer;
			if (player != null)
			{
// 				//vampiir, they cannot be buffed except with resists/armor factor/ haste / power regen
// 				if (HasPositiveEffect && player.CharacterClass.ID == (int)eCharacterClass.Vampiir && m_caster != player)
// 				{
// 					//restrictions
// 					//if (this is PropertyChangingSpell
// 					//    && this is ArmorFactorBuff == false
// 					//    && this is CombatSpeedBuff == false
// 					//    && this is AbstractResistBuff == false
// 					//    && this is EnduranceRegenSpellHandler == false
// 					//    && this is EvadeChanceBuff == false
// 					//    && this is ParryChanceBuff == false)
// 					//{
// 					if (this is StrengthBuff || this is DexterityBuff || this is ConstitutionBuff || this is QuicknessBuff || this is StrengthConBuff || this is DexterityQuiBuff || this is AcuityBuff)
// 					{
// 						GamePlayer caster = m_caster as GamePlayer;
// 						if (caster != null)
// 						{
// 							caster.Out.SendMessage("Your buff has no effect on the Vampiir!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
// 						}
// 						player.Out.SendMessage("This buff has no effect on you!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
// 						return;
// 					}
// 					if (this is ArmorFactorBuff)
// 					{
// 						if (/*SpellHandler.FindEffectOnTarget(target, "ArmorFactorBuff")*/
// 							EffectListService.GetEffectOnTarget(target, eEffect.BaseAFBuff) != null && m_spellLine.IsBaseLine != true)
// 						{
// 							MessageToLiving(target, "You already have this effect!", eChatType.CT_SpellResisted);
// 							return;
// 						}
// 					}
// 				}
				if (target is GamePlayer && (target as GamePlayer).NoHelp && Caster is GamePlayer && target != Caster && target.Realm == Caster.Realm)
				{
					//player not grouped, anyone else
					//player grouped, different group
					if ((target as GamePlayer).Group == null ||
					    (Caster as GamePlayer).Group == null ||
					    (Caster as GamePlayer).Group != (target as GamePlayer).Group)
					{
						MessageToCaster("That player does not want assistance", eChatType.CT_SpellResisted);
						return;
					}
				}


				if (this is HeatColdMatterBuff || this is AllMagicResistsBuff)
				{
					if (this.Spell.Frequency <= 0)
					{
						//GameSpellEffect Matter = FindEffectOnTarget(player, "MatterResistBuff");
						//GameSpellEffect Cold = FindEffectOnTarget(player, "ColdResistBuff");
						//GameSpellEffect Heat = FindEffectOnTarget(player, "HeatResistBuff");
						ECSGameEffect Matter = EffectListService.GetEffectOnTarget(player, eEffect.MatterResistBuff);
						ECSGameEffect Cold = EffectListService.GetEffectOnTarget(player, eEffect.ColdResistBuff);
						ECSGameEffect Heat = EffectListService.GetEffectOnTarget(player, eEffect.HeatResistBuff);
						if (Matter != null || Cold != null || Heat != null)
						{
							MessageToCaster(target.Name + " already has this effect", eChatType.CT_SpellResisted);
							return;
						}
					}
				}
				
				if (this is BodySpiritEnergyBuff || this is AllMagicResistsBuff)
				{
					if (this.Spell.Frequency <= 0)
					{
						//GameSpellEffect Body = FindEffectOnTarget(player, "BodyResistBuff");
						//GameSpellEffect Spirit = FindEffectOnTarget(player, "SpiritResistBuff");
						//GameSpellEffect Energy = FindEffectOnTarget(player, "EnergyResistBuff");
						ECSGameEffect Body = EffectListService.GetEffectOnTarget(player, eEffect.BodyResistBuff);
						ECSGameEffect Spirit = EffectListService.GetEffectOnTarget(player, eEffect.SpiritResistBuff);
						ECSGameEffect Energy = EffectListService.GetEffectOnTarget(player, eEffect.EnergyResistBuff);
						if (Body != null || Spirit != null || Energy != null)
						{
							MessageToCaster(target.Name + " already has this effect", eChatType.CT_SpellResisted);
							return;
						}
					}
				}
			}

			base.ApplyEffectOnTarget(target);
		}

		/// <summary>
		/// start changing effect on target
		/// </summary>
		/// <param name="effect"></param>
		public override void OnEffectStart(GameSpellEffect effect)
		{
			ApplyBonus(effect.Owner, BonusCategory1, Property1, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory2, Property2, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory3, Property3, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory4, Property4, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory5, Property5, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory6, Property6, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory7, Property7, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory8, Property8, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory9, Property9, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory10, Property10, (int)(Spell.Value * effect.Effectiveness), false);

			SendUpdates(effect.Owner);

			eChatType toLiving = eChatType.CT_SpellPulse;
			eChatType toOther = eChatType.CT_SpellPulse;
			if (Spell.Pulse == 0 || !HasPositiveEffect)
			{
				toLiving = eChatType.CT_Spell;
				toOther = eChatType.CT_System;
				SendEffectAnimation(effect.Owner, 0, false, 1);
			}

			GameLiving player = null;

			if (Caster is GameNPC && (Caster as GameNPC).Brain is IControlledBrain)
				player = ((Caster as GameNPC).Brain as IControlledBrain).Owner;
			else if (effect.Owner is GameNPC && (effect.Owner as GameNPC).Brain is IControlledBrain)
				player = ((effect.Owner as GameNPC).Brain as IControlledBrain).Owner;

			if (player != null)
			{
				// Controlled NPC. Show message in blue writing to owner...

				MessageToLiving(player, String.Format(Spell.Message2,
													  effect.Owner.GetName(0, true)), toLiving);

				// ...and in white writing for everyone else.

				foreach (GamePlayer gamePlayer in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
					if (gamePlayer != player)
						MessageToLiving(gamePlayer, String.Format(Spell.Message2,
																  effect.Owner.GetName(0, true)), toOther);
			}
			else
			{
				MessageToLiving(effect.Owner, Spell.Message1, toLiving);
				Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), toOther, effect.Owner);
			}

		}

		/// <summary>
		/// When an applied effect expires.
		/// Duration spells only.
		/// </summary>
		/// <param name="effect">The expired effect</param>
		/// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
		/// <returns>immunity duration in milliseconds</returns>
		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if (!noMessages && Spell.Pulse == 0)
			{
				MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
				Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
			}

			ApplyBonus(effect.Owner, BonusCategory1, Property1, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory2, Property2, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory3, Property3, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory4, Property4, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory5, Property5, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory6, Property6, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory7, Property7, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory8, Property8, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory9, Property9, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory10, Property10, (int)(Spell.Value * effect.Effectiveness), true);


			SendUpdates(effect.Owner);

			return base.OnEffectExpires(effect, noMessages);
		}

		protected virtual void SendUpdates(GameLiving target)
		{
		}

		protected IPropertyIndexer GetBonusCategory(GameLiving target, eBuffBonusCategory categoryid)
		{
			IPropertyIndexer bonuscat = null;
			switch (categoryid)
			{
				case eBuffBonusCategory.BaseBuff:
					bonuscat = target.BaseBuffBonusCategory;
					break;
				case eBuffBonusCategory.SpecBuff:
					bonuscat = target.SpecBuffBonusCategory;
					break;
				case eBuffBonusCategory.Debuff:
					bonuscat = target.DebuffCategory;
					break;
				case eBuffBonusCategory.OtherBuff:
					bonuscat = target.OtherBonus;
					break;
				case eBuffBonusCategory.SpecDebuff:
					bonuscat = target.SpecDebuffCategory;
					break;
				case eBuffBonusCategory.AbilityBuff:
					bonuscat = target.AbilityBonus;
					break;
				default:
					if (log.IsErrorEnabled)
						log.Error("BonusCategory not found " + categoryid + "!");
					break;
			}
			return bonuscat;
		}

		/// <summary>
		/// Property 1 which bonus value has to be changed
		/// </summary>
		public abstract eProperty Property1 { get; }

		/// <summary>
		/// Property 2 which bonus value has to be changed
		/// </summary>
		public virtual eProperty Property2
		{
			get { return eProperty.Undefined; }
		}

		/// <summary>
		/// Property 3 which bonus value has to be changed
		/// </summary>
		public virtual eProperty Property3
		{
			get { return eProperty.Undefined; }
		}

		/// <summary>
		/// Property 4 which bonus value has to be changed
		/// </summary>
		public virtual eProperty Property4
		{
			get { return eProperty.Undefined; }
		}
		/// <summary>
		/// Property 5 which bonus value has to be changed
		/// </summary>
		public virtual eProperty Property5
		{
			get { return eProperty.Undefined; }
		}

		/// <summary>
		/// Property 6 which bonus value has to be changed
		/// </summary>
		public virtual eProperty Property6
		{
			get { return eProperty.Undefined; }
		}

		/// <summary>
		/// Property 7 which bonus value has to be changed
		/// </summary>
		public virtual eProperty Property7
		{
			get { return eProperty.Undefined; }
		}

		/// <summary>
		/// Property 8 which bonus value has to be changed
		/// </summary>
		public virtual eProperty Property8
		{
			get { return eProperty.Undefined; }
		}

		/// <summary>
		/// Property 9 which bonus value has to be changed
		/// </summary>
		public virtual eProperty Property9
		{
			get { return eProperty.Undefined; }
		}

		/// <summary>
		/// Property 10 which bonus value has to be changed
		/// </summary>
		public virtual eProperty Property10
		{
			get { return eProperty.Undefined; }
		}

		/// <summary>
		/// Bonus Category where to change the Property1
		/// </summary>
		public virtual eBuffBonusCategory BonusCategory1
		{
			get { return eBuffBonusCategory.BaseBuff; }
		}

		/// <summary>
		/// Bonus Category where to change the Property2
		/// </summary>
		public virtual eBuffBonusCategory BonusCategory2
		{
			get { return eBuffBonusCategory.BaseBuff; }
		}

		/// <summary>
		/// Bonus Category where to change the Property3
		/// </summary>
		public virtual eBuffBonusCategory BonusCategory3
		{
			get { return eBuffBonusCategory.BaseBuff; }
		}

		/// <summary>
		/// Bonus Category where to change the Property4
		/// </summary>
		public virtual eBuffBonusCategory BonusCategory4
		{
			get { return eBuffBonusCategory.BaseBuff; }
		}

		/// <summary>
		/// Bonus Category where to change the Property5
		/// </summary>
		public virtual eBuffBonusCategory BonusCategory5
		{
			get { return eBuffBonusCategory.BaseBuff; }
		}

		/// <summary>
		/// Bonus Category where to change the Property6
		/// </summary>
		public virtual eBuffBonusCategory BonusCategory6
		{
			get { return eBuffBonusCategory.BaseBuff; }
		}

		/// <summary>
		/// Bonus Category where to change the Property7
		/// </summary>
		public virtual eBuffBonusCategory BonusCategory7
		{
			get { return eBuffBonusCategory.BaseBuff; }
		}

		/// <summary>
		/// Bonus Category where to change the Property8
		/// </summary>
		public virtual eBuffBonusCategory BonusCategory8
		{
			get { return eBuffBonusCategory.BaseBuff; }
		}

		/// <summary>
		/// Bonus Category where to change the Property9
		/// </summary>
		public virtual eBuffBonusCategory BonusCategory9
		{
			get { return eBuffBonusCategory.BaseBuff; }
		}

		/// <summary>
		/// Bonus Category where to change the Property10
		/// </summary>
		public virtual eBuffBonusCategory BonusCategory10
		{
			get { return eBuffBonusCategory.BaseBuff; }
		}

		public override void OnEffectRestored(GameSpellEffect effect, int[] vars)
		{
			ApplyBonus(effect.Owner, BonusCategory1, Property1, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory2, Property2, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory3, Property3, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory4, Property4, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory5, Property5, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory6, Property6, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory7, Property7, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory8, Property8, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory9, Property9, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory10, Property10, vars[1], false);


			SendUpdates(effect.Owner);
		}

		public override int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
		{
			if (!noMessages && Spell.Pulse == 0)
			{
				MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
				Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
			}

			ApplyBonus(effect.Owner, BonusCategory1, Property1, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory2, Property2, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory3, Property3, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory4, Property4, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory5, Property5, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory6, Property6, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory7, Property7, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory8, Property8, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory9, Property9, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory10, Property10, vars[1], true);


			SendUpdates(effect.Owner);
			return 0;
		}

		/// <summary>
		/// Method used to apply bonuses
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="BonusCat"></param>
		/// <param name="Property"></param>
		/// <param name="Value"></param>
		/// <param name="IsSubstracted"></param>
		protected void ApplyBonus(GameLiving owner,  eBuffBonusCategory BonusCat, eProperty Property, int Value, bool IsSubstracted)
		{
			IPropertyIndexer tblBonusCat;
			if (Property != eProperty.Undefined)
			{
				tblBonusCat = GetBonusCategory(owner, BonusCat);
				if (IsSubstracted)
					tblBonusCat[Property] -= Value;
				else
					tblBonusCat[Property] += Value;
			}
		}

		// constructor
		public PropertyChangingSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}
	}
}
