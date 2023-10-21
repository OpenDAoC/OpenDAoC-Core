using System;

namespace Core.GS.Calculators;

[PropertyCalculator(EProperty.OffhandDamage)]
public class OffhandDamageCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		return Math.Max(0, living.AbilityBonus[(int) property]);
	}
}

[PropertyCalculator(EProperty.OffhandChance)]
public class OffhandChanceCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		return Math.Max(0, living.AbilityBonus[(int) property]);
	}
}

[PropertyCalculator(EProperty.OffhandDamageAndChance)]
public class OffhandDamageAndChanceCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		return Math.Max(0, living.AbilityBonus[(int) property]);
	}
}