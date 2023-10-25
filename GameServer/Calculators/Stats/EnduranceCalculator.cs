using System;
using Core.GS.Enums;

namespace Core.GS.Calculators;

/// <summary>
/// The Fatigue (Endurance) calculator
///
/// BuffBonusCategory1 is used for absolute HP buffs
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.Fatigue)]
public class EnduranceCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		if (living is GamePlayer)
		{
			GamePlayer player = living as GamePlayer;

			int endurance = player.DBMaxEndurance;
			endurance += (int)(endurance * (Math.Min(15, living.ItemBonus[(int)property]) * .01));
			return endurance;
		}

		return 100;
	}
}