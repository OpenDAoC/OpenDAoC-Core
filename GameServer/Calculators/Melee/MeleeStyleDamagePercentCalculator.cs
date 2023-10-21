using System;
using Core.GS.Enums;

namespace Core.GS.Calculators;

[PropertyCalculator(EProperty.StyleDamage)]
public class MeleeStyleDamagePercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		return Math.Max(0, 100
			+Math.Min(10,living.ItemBonus[(int)property]));
	}
}