using System;

namespace Core.GS.PropertyCalc;

/// <summary>
/// The Fatigue Consumption bonus percent calculator
///
/// BuffBonusCategory1 is used for buffs
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 is used for debuff
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.FatigueConsumption)]
public class EnduranceConsumptionPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		return Math.Max(1, 100
			- living.BaseBuffBonusCategory[(int)property] // less is faster = buff
			+ living.DebuffCategory[(int)property] // more is slower = debuff
			- Math.Min(10, living.ItemBonus[(int)property])); // ?
	}
}