using System;

namespace DOL.GS.PropertyCalc;

[APropertyCalculator(eProperty.ResIllnessReduction)]
public class RezSicknessPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, eProperty property)
	{
		return Math.Max(0, living.AbilityBonus[(int)property] - living.DebuffCategory[(int)property]);
	}
}