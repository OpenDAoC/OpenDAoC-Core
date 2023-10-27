using System;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Calculators;
using Core.GS.ECS;
using Core.GS.Effects;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS.Spells;

/// <summary>
/// Spell to change up to 3 property bonuses at once
/// in one their specific given bonus category
/// </summary>
public abstract class PropertyChangingSpell : SpellHandler
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public override void CreateECSEffect(EcsGameEffectInitParams initParams)
	{
		new StatBuffEcsSpellEffect(initParams);
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

	/// <summary>
	/// Calculates the effect duration in milliseconds
	/// </summary>
	/// <param name="target">The effect target</param>
	/// <param name="effectiveness">The effect effectiveness</param>
	/// <returns>The effect duration in milliseconds</returns>
	protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
	{
		double duration = Spell.Duration;
		if (HasPositiveEffect)
		{	
			duration *= (1.0 + m_caster.GetModified(EProperty.SpellDuration) * 0.01);
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
				duration = (Spell.Duration * 4);
			return (int)duration; 
		}
		duration = base.CalculateEffectDuration(target, effectiveness);
		return (int)duration;
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
					MessageToCaster("That player does not want assistance", EChatType.CT_SpellResisted);
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
					EcsGameEffect Matter = EffectListService.GetEffectOnTarget(player, EEffect.MatterResistBuff);
					EcsGameEffect Cold = EffectListService.GetEffectOnTarget(player, EEffect.ColdResistBuff);
					EcsGameEffect Heat = EffectListService.GetEffectOnTarget(player, EEffect.HeatResistBuff);
					if (Matter != null || Cold != null || Heat != null)
					{
						MessageToCaster(target.Name + " already has this effect", EChatType.CT_SpellResisted);
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
					EcsGameEffect Body = EffectListService.GetEffectOnTarget(player, EEffect.BodyResistBuff);
					EcsGameEffect Spirit = EffectListService.GetEffectOnTarget(player, EEffect.SpiritResistBuff);
					EcsGameEffect Energy = EffectListService.GetEffectOnTarget(player, EEffect.EnergyResistBuff);
					if (Body != null || Spirit != null || Energy != null)
					{
						MessageToCaster(target.Name + " already has this effect", EChatType.CT_SpellResisted);
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

		EChatType toLiving = EChatType.CT_SpellPulse;
		EChatType toOther = EChatType.CT_SpellPulse;
		if (Spell.Pulse == 0 || !HasPositiveEffect)
		{
			toLiving = EChatType.CT_Spell;
			toOther = EChatType.CT_System;
			SendEffectAnimation(effect.Owner, 0, false, 1);
		}

		GameLiving player = null;

		if (Caster is GameNpc && (Caster as GameNpc).Brain is IControlledBrain)
			player = ((Caster as GameNpc).Brain as IControlledBrain).Owner;
		else if (effect.Owner is GameNpc && (effect.Owner as GameNpc).Brain is IControlledBrain)
			player = ((effect.Owner as GameNpc).Brain as IControlledBrain).Owner;

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
			MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), toOther, effect.Owner);
		}

	}

	BuffCheckAction m_buffCheckAction = null;

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
			MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
			MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), EChatType.CT_SpellExpires, effect.Owner);
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

		if (m_buffCheckAction != null)
		{
			m_buffCheckAction.Stop();
			m_buffCheckAction = null;
		}

		return base.OnEffectExpires(effect, noMessages);
	}

	protected virtual void SendUpdates(GameLiving target)
	{
	}

	protected IPropertyIndexer GetBonusCategory(GameLiving target, EBuffBonusCategory categoryid)
	{
		IPropertyIndexer bonuscat = null;
		switch (categoryid)
		{
			case EBuffBonusCategory.BaseBuff:
				bonuscat = target.BaseBuffBonusCategory;
				break;
			case EBuffBonusCategory.SpecBuff:
				bonuscat = target.SpecBuffBonusCategory;
				break;
			case EBuffBonusCategory.Debuff:
				bonuscat = target.DebuffCategory;
				break;
			case EBuffBonusCategory.Other:
				bonuscat = target.BuffBonusCategory4;
				break;
			case EBuffBonusCategory.SpecDebuff:
				bonuscat = target.SpecDebuffCategory;
				break;
			case EBuffBonusCategory.AbilityBuff:
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
	public abstract EProperty Property1 { get; }

	/// <summary>
	/// Property 2 which bonus value has to be changed
	/// </summary>
	public virtual EProperty Property2
	{
		get { return EProperty.Undefined; }
	}

	/// <summary>
	/// Property 3 which bonus value has to be changed
	/// </summary>
	public virtual EProperty Property3
	{
		get { return EProperty.Undefined; }
	}

	/// <summary>
	/// Property 4 which bonus value has to be changed
	/// </summary>
	public virtual EProperty Property4
	{
		get { return EProperty.Undefined; }
	}
	/// <summary>
	/// Property 5 which bonus value has to be changed
	/// </summary>
	public virtual EProperty Property5
	{
		get { return EProperty.Undefined; }
	}

	/// <summary>
	/// Property 6 which bonus value has to be changed
	/// </summary>
	public virtual EProperty Property6
	{
		get { return EProperty.Undefined; }
	}

	/// <summary>
	/// Property 7 which bonus value has to be changed
	/// </summary>
	public virtual EProperty Property7
	{
		get { return EProperty.Undefined; }
	}

	/// <summary>
	/// Property 8 which bonus value has to be changed
	/// </summary>
	public virtual EProperty Property8
	{
		get { return EProperty.Undefined; }
	}

	/// <summary>
	/// Property 9 which bonus value has to be changed
	/// </summary>
	public virtual EProperty Property9
	{
		get { return EProperty.Undefined; }
	}

	/// <summary>
	/// Property 10 which bonus value has to be changed
	/// </summary>
	public virtual EProperty Property10
	{
		get { return EProperty.Undefined; }
	}

	/// <summary>
	/// Bonus Category where to change the Property1
	/// </summary>
	public virtual EBuffBonusCategory BonusCategory1
	{
		get { return EBuffBonusCategory.BaseBuff; }
	}

	/// <summary>
	/// Bonus Category where to change the Property2
	/// </summary>
	public virtual EBuffBonusCategory BonusCategory2
	{
		get { return EBuffBonusCategory.BaseBuff; }
	}

	/// <summary>
	/// Bonus Category where to change the Property3
	/// </summary>
	public virtual EBuffBonusCategory BonusCategory3
	{
		get { return EBuffBonusCategory.BaseBuff; }
	}

	/// <summary>
	/// Bonus Category where to change the Property4
	/// </summary>
	public virtual EBuffBonusCategory BonusCategory4
	{
		get { return EBuffBonusCategory.BaseBuff; }
	}

	/// <summary>
	/// Bonus Category where to change the Property5
	/// </summary>
	public virtual EBuffBonusCategory BonusCategory5
	{
		get { return EBuffBonusCategory.BaseBuff; }
	}

	/// <summary>
	/// Bonus Category where to change the Property6
	/// </summary>
	public virtual EBuffBonusCategory BonusCategory6
	{
		get { return EBuffBonusCategory.BaseBuff; }
	}

	/// <summary>
	/// Bonus Category where to change the Property7
	/// </summary>
	public virtual EBuffBonusCategory BonusCategory7
	{
		get { return EBuffBonusCategory.BaseBuff; }
	}

	/// <summary>
	/// Bonus Category where to change the Property8
	/// </summary>
	public virtual EBuffBonusCategory BonusCategory8
	{
		get { return EBuffBonusCategory.BaseBuff; }
	}

	/// <summary>
	/// Bonus Category where to change the Property9
	/// </summary>
	public virtual EBuffBonusCategory BonusCategory9
	{
		get { return EBuffBonusCategory.BaseBuff; }
	}

	/// <summary>
	/// Bonus Category where to change the Property10
	/// </summary>
	public virtual EBuffBonusCategory BonusCategory10
	{
		get { return EBuffBonusCategory.BaseBuff; }
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
			MessageToLiving(effect.Owner, Spell.Message3, EChatType.CT_SpellExpires);
			MessageUtil.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), EChatType.CT_SpellExpires, effect.Owner);
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
	protected void ApplyBonus(GameLiving owner,  EBuffBonusCategory BonusCat, EProperty Property, int Value, bool IsSubstracted)
	{
		IPropertyIndexer tblBonusCat;
		if (Property != EProperty.Undefined)
		{
			tblBonusCat = GetBonusCategory(owner, BonusCat);
			if (IsSubstracted)
				tblBonusCat[(int)Property] -= Value;
			else
				tblBonusCat[(int)Property] += Value;
		}
	}

	// constructor
	public PropertyChangingSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
	{
	}
}

public class BuffCheckAction : EcsGameTimerWrapperBase
{
	public const int BUFFCHECKINTERVAL = 60000;//60 seconds

	private GameLiving m_caster = null;
	private GameLiving m_owner = null;
	private GameSpellEffect m_effect = null;

	public BuffCheckAction(GameLiving caster, GameLiving owner, GameSpellEffect effect)
		: base(caster)
	{
		m_caster = caster;
		m_owner = owner;
		m_effect = effect;
	}

	/// <summary>
	/// Called on every timer tick
	/// </summary>
	protected override int OnTick(EcsGameTimer timer)
	{
		if (m_caster == null ||
		    m_owner == null ||
		    m_effect == null)
			return 0;

		if ( !m_caster.IsWithinRadius( m_owner, Server.ServerProperty.BUFF_RANGE ) )
			m_effect.Cancel(false);
		else
			return BUFFCHECKINTERVAL;

		return 0;
	}
}