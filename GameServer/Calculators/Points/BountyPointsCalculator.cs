using System;

namespace DOL.GS.PropertyCalc;

[APropertyCalculator(eProperty.BountyPoints)]
public class BountyPointsCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, eProperty property)
	{
		if (living is GamePlayer)
		{
			return Math.Min(10, living.ItemBonus[(int)property]);
		}

		return 0;
	}
}