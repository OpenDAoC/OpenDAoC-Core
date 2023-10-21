using System;

namespace Core.GS.PropertyCalc;

/// <summary>
/// BuffBonusCategory1 is used for buffs
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 is used for debuff
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.SpeedDecreaseDurationReduction)]
public class SpeedDecreaseDurationPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property) 
	{
		int percent = 100
			-living.BaseBuffBonusCategory[(int)property] // buff reduce the duration
			+living.DebuffCategory[(int)property]
			-living.ItemBonus[(int)property]
			-living.AbilityBonus[(int)property];

		if (living.HasAbility(Abilities.Stoicism))
			percent -= 25;

		return Math.Max(1, percent);
	}
}