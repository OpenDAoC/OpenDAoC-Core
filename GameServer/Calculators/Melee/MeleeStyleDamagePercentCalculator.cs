using System;

namespace DOL.GS.PropertyCalc;

[PropertyCalculator(eProperty.StyleDamage)]
public class MeleeStyleDamagePercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, eProperty property)
	{
		return Math.Max(0, 100
			+Math.Min(10,living.ItemBonus[(int)property]));
	}
}