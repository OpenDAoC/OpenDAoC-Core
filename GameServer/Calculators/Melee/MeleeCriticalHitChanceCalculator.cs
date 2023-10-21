using System;
using Core.GS.AI.Brains;

namespace Core.GS.Calculators;

/// <summary>
/// The critical hit chance calculator. Returns 0 .. 100 chance.
///
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 for uncapped realm ability bonus
/// BuffBonusMultCategory1 unused
///
/// Crit propability is capped to 50% except for berserk
/// </summary>
[PropertyCalculator(EProperty.CriticalMeleeHitChance)]
public class MeleeCriticalHitChanceCalculator : PropertyCalculator
{
	public MeleeCriticalHitChanceCalculator() { }

	public override int CalcValue(GameLiving living, EProperty property)
	{
		// No berserk for ranged weapons.
		EcsGameEffect berserk = EffectListService.GetEffectOnTarget(living, EEffect.Berserk);

		if (berserk != null)
			return 100;

		// Base 10% chance of critical for all with melee weapons plus ra bonus.
		int chance = living.BuffBonusCategory4[(int)property] + living.AbilityBonus[(int)property];

		// Summoned or Charmed pet.
		if (living is GameNpc npc && npc.Brain is IControlledBrain petBrain && petBrain.GetPlayerOwner() is GamePlayer player)
		{
			if (npc is NecromancerPet)
				chance += 10;

			chance += player.GetAbility<RealmAbilities.OfRaWildMinionAbility>()?.Amount ?? 0;
		}
		else
			chance += 10;

		// 50% hardcap.
		return Math.Min(chance, 50);
	}
}