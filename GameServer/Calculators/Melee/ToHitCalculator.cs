namespace Core.GS.PropertyCalc;

[PropertyCalculator(EProperty.ToHitBonus)]
public class ToHitBonusCalculator : PropertyCalculator
{
	public ToHitBonusCalculator() { }

	public override int CalcValue(GameLiving living, EProperty property)
	{
		return (int)(
			+living.BaseBuffBonusCategory[(int)property]
			+ living.SpecBuffBonusCategory[(int)property]
			- living.DebuffCategory[(int)property]
			+ living.BuffBonusCategory4[(int)property]);
	}
}