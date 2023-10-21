using System;
using Core.GS.Enums;

namespace Core.GS.Calculators;

[PropertyCalculator(EProperty.XpPoints)]
public class ExperiencePointsCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		if (living is GamePlayer)
		{
			return Math.Min(10, living.ItemBonus[(int)property]);
		}

		return 0;
	}
}