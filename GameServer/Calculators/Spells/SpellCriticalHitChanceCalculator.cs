using System;
using Core.AI.Brain;
using Core.GS.AI.Brains;

namespace Core.GS.PropertyCalc;

/// <summary>
/// The critical hit chance calculator. Returns 0 .. 100 chance.
/// 
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// AbilityBonus used
/// </summary>
[PropertyCalculator(EProperty.CriticalSpellHitChance)]
public class SpellCriticalHitChanceCalculator : PropertyCalculator
{
	public SpellCriticalHitChanceCalculator() {}

	public override int CalcValue(GameLiving living, EProperty property) 
	{
		int chance = living.AbilityBonus[(int)property];

		if (living is GamePlayer player)
		{
			if (player.PlayerClass.ClassType == EPlayerClassType.ListCaster)
				chance += 10;
		}
		else if (living is NecromancerPet necroPet)
		{
			chance += 10;
			chance += necroPet.Owner.AbilityBonus[(int)property];
		}
        // Summoned or Charmed pet.
        else if (living is GameNpc npc && ServerProperties.Properties.EXPAND_WILD_MINION)
        {
            if (npc.Brain is IControlledBrain petBrain && petBrain.GetPlayerOwner() is GamePlayer playerOwner)
                chance += playerOwner.GetAbility<RealmAbilities.OfRaWildMinionAbility>()?.Amount ?? 0;
        }

		return Math.Min(chance, 50);
	}
}