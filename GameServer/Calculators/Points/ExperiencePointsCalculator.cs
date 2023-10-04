using System;

namespace DOL.GS.PropertyCalc;

[PropertyCalculator(eProperty.XpPoints)]
public class ExperiencePointsCalculator : PropertyCalculator
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