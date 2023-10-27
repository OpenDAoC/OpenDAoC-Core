using Core.GS.Enums;

namespace Core.GS.Calculators;

[PropertyCalculator(EProperty.ExtraHP)]
public class ExtraHitPointPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		return living.ItemBonus[(int)property]+living.BaseBuffBonusCategory[(int)property];
	}
}