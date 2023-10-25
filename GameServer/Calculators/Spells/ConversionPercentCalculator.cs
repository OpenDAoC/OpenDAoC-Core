using Core.GS.Enums;

namespace Core.GS.Calculators;

/// <summary>
/// The Spell Range bonus percent calculator
///
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 is used for debuff
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.Conversion)]
public class ConversionPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		return living.ItemBonus[(int)property]+living.BaseBuffBonusCategory[(int)property];
	}
}