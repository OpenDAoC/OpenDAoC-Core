namespace DOL.GS.PropertyCalc;

[APropertyCalculator(eProperty.ExtraHP)]
public class ExtraHitPointPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, eProperty property)
	{
		return living.ItemBonus[(int)property]+living.BaseBuffBonusCategory[(int)property];
	}
}