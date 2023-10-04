namespace DOL.GS.PropertyCalc;

[PropertyCalculator(eProperty.ExtraHP)]
public class ExtraHitPointPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, eProperty property)
	{
		return living.ItemBonus[(int)property]+living.BaseBuffBonusCategory[(int)property];
	}
}