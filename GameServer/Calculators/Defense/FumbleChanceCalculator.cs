using System;

namespace DOL.GS.PropertyCalc;

/// <summary>
/// The fumble chance calculator. Returns 0 .. 1000 chance.
/// 
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 is used for debuff
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.FumbleChance)]
public class FumbleChanceCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		// 5% level 1, 0.1% level 50
		return Math.Max(51 - living.Level, (10 * living.DebuffCategory[(int)property]) + (10 * living.AbilityBonus[(int)property]));
	}
}
[PropertyCalculator(EProperty.SpellFumbleChance)]
public class SpellFumbleChanceCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		return Math.Min(100, living.DebuffCategory[(int)property]);
	}
}