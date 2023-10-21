using System;
using Core.GS.ECS;

namespace Core.GS.Calculators;

/// <summary>
/// The Archery Range bonus percent calculator
///
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 is used for debuff
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.ArcheryRange)]
public class RangeBonusPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		int debuff = living.DebuffCategory[(int)property];
		if(debuff > 0)
		{
			//GameSpellEffect nsreduction = SpellHandler.FindEffectOnTarget(living, "NearsightReduction");
			//if(nsreduction!=null) debuff = (int)(debuff * (1.00 - nsreduction.Spell.Value * 0.01));
		}
		
		int item = Math.Max(0, 100
			- debuff
			+ Math.Min(10, living.ItemBonus[(int)property]));// http://www.camelotherald.com/more/1325.shtml

		int ra = 0;
		if (living.rangeAttackComponent.RangedAttackType == ERangedAttackType.Long)
		{
			ra = 50;
			TrueShotEcsAbilityEffect effect = (TrueShotEcsAbilityEffect)EffectListService.GetAbilityEffectOnTarget(living, EEffect.TrueShot);
			if (effect != null)
				EffectService.RequestImmediateCancelEffect(effect, false);
		}

		return item + ra;
	}
}