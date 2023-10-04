namespace DOL.GS.PropertyCalc;

[PropertyCalculator(eProperty.StyleAbsorb)]
public class MeleeStyleAbsorbPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, eProperty property)
	{
        return living.ItemBonus[(int)property] + living.BaseBuffBonusCategory[(int)property];
	}
}