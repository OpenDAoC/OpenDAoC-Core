using System;
using Core.GS.Enums;

namespace Core.GS.Calculators;

[PropertyCalculator(EProperty.ResIllnessReduction)]
public class RezSicknessPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		return Math.Max(0, living.AbilityBonus[(int)property] - living.DebuffCategory[(int)property]);
	}
}