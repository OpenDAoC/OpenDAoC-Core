using System;

namespace DOL.GS.PropertyCalc;

/// <summary>
/// The ranged damage bonus percent calculator
///
/// BuffBonusCategory1 is used for buffs
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 is used for debuff
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.RangedDamage)]
public class RangedDamageBonusPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		if (living is GameNpc)
		{
			int rangedDamagePercent = 8;
			var rangedBuffBonus = living.BaseBuffBonusCategory[EProperty.Dexterity] + living.SpecBuffBonusCategory[EProperty.Dexterity];
			var rangedDebuff = living.DebuffCategory[EProperty.Dexterity] + living.SpecDebuffCategory[EProperty.Dexterity];
			return ((living as GameNpc).Dexterity + (rangedBuffBonus - rangedDebuff)) / rangedDamagePercent;
		}

		// Hardcap 10%
		int hardCap = 10;
		int abilityBonus = living.AbilityBonus[(int)property];
		int itemBonus = Math.Min(hardCap, living.ItemBonus[(int)property]);
		int buffBonus = living.BaseBuffBonusCategory[(int)property] + living.SpecBuffBonusCategory[(int)property];
		int debuff = Math.Min(hardCap, Math.Abs(living.DebuffCategory[(int)property]));
		
		return abilityBonus + buffBonus + itemBonus - debuff;
	}
}