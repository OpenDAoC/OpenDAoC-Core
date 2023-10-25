using System;
using Core.GS.Enums;

namespace Core.GS.Calculators;

[PropertyCalculator(EProperty.MeleeDamage)]
public class MeleeDamagePercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		int hardCap = 10;
		int abilityBonus = living.AbilityBonus[(int)property];
		int itemBonus = Math.Min(hardCap, living.ItemBonus[(int)property]);
		int buffBonus = living.BaseBuffBonusCategory[(int)property] + living.SpecBuffBonusCategory[(int)property];
		int debuffMalus = Math.Min(hardCap, Math.Abs(living.DebuffCategory[(int)property]));
		return abilityBonus + buffBonus + itemBonus - debuffMalus;
	}
}