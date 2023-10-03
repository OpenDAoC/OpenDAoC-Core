using System;
using DOL.AI.Brain;

namespace DOL.GS.PropertyCalc
{
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
	[PropertyCalculator(eProperty.CriticalSpellHitChance)]
	public class CriticalSpellHitChanceCalculator : PropertyCalculator
	{
		public CriticalSpellHitChanceCalculator() {}

		public override int CalcValue(GameLiving living, eProperty property) 
		{
			int chance = living.AbilityBonus[(int)property];

			if (living is GamePlayer player)
			{
				if (player.CharacterClass.ClassType == eClassType.ListCaster)
					chance += 10;
			}
			else if (living is NecromancerPet necroPet)
			{
				chance += 10;
				chance += necroPet.Owner.AbilityBonus[(int)property];
			}
            // Summoned or Charmed pet.
            else if (living is GameNPC npc && ServerProperties.Properties.EXPAND_WILD_MINION)
            {
                if (npc.Brain is IControlledBrain petBrain && petBrain.GetPlayerOwner() is GamePlayer playerOwner)
                    chance += playerOwner.GetAbility<RealmAbilities.AtlasOF_WildMinionAbility>()?.Amount ?? 0;
            }

			return Math.Min(chance, 50);
		}
	}


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
	[PropertyCalculator(eProperty.CriticalDotHitChance)]
	public class CriticalDotHitChanceCalculator : PropertyCalculator
	{
		public CriticalDotHitChanceCalculator() { }

		public override int CalcValue(GameLiving living, eProperty property)
		{
			int chance = living.AbilityBonus[(int)property];

			if (living is NecromancerPet necroPet && necroPet.Brain is IControlledBrain necroBrain && necroBrain.GetPlayerOwner() is GamePlayer playerOwner)
				chance += playerOwner.GetAbility<RealmAbilities.AtlasOF_WildArcanaAbility>()?.Amount ?? 0;

			return Math.Min(chance, 50);
		}
	}
}
