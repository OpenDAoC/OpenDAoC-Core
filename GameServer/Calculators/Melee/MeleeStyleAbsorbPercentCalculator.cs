using Core.GS.Enums;

namespace Core.GS.Calculators;

[PropertyCalculator(EProperty.StyleAbsorb)]
public class MeleeStyleAbsorbPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
        return living.ItemBonus[(int)property] + living.BaseBuffBonusCategory[(int)property];
	}
}