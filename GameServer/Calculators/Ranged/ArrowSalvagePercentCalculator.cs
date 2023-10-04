using System;

namespace DOL.GS.PropertyCalc;

/// <summary>
/// The Archery Speed bonus percent calculator
///
/// BuffBonusCategory1 is used for buffs
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 is used for debuff
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[APropertyCalculator(eProperty.ArrowRecovery)]
public class ArrowSalvagePercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, eProperty property)
	{
		return Math.Max(0, living.AbilityBonus[(int)property] - living.DebuffCategory[(int)property]);
	}
}