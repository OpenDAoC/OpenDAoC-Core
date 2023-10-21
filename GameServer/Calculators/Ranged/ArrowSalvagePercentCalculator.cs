using System;

namespace Core.GS.Calculators;

/// <summary>
/// The Archery Speed bonus percent calculator
///
/// BuffBonusCategory1 is used for buffs
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 is used for debuff
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.ArrowRecovery)]
public class ArrowSalvagePercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		return Math.Max(0, living.AbilityBonus[(int)property] - living.DebuffCategory[(int)property]);
	}
}