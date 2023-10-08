using System;

namespace DOL.GS.PropertyCalc;

/// <summary>
/// BuffBonusCategory1 is used for all single stat buffs
/// BuffBonusCategory2 is used for all dual stat buffs
/// BuffBonusCategory3 is used for all debuffs (positive values expected here)
/// BuffBonusCategory4 is used for all other uncapped modifications
///                    category 4 kicks in at last
/// BuffBonusMultCategory1 used after all buffs/debuffs
/// </summary>
[PropertyCalculator(EProperty.SpellLevel)]
public class SpellLevelCalculator : PropertyCalculator
{
	public SpellLevelCalculator() { }

	public override int CalcValue(GameLiving living, EProperty property)
	{
		return (int)(
			+living.BaseBuffBonusCategory[(int)property]
			+ living.SpecBuffBonusCategory[(int)property]
			- living.DebuffCategory[(int)property]
			+ living.BuffBonusCategory4[(int)property]
			+ living.AbilityBonus[(int)property]
			+ Math.Min(10, living.ItemBonus[(int)property]));
	}
}