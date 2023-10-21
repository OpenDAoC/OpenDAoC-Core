using System;

namespace Core.GS.Calculators;

[PropertyCalculator(EProperty.RealmPoints)]
public class RealmPointsCalculator : PropertyCalculator
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