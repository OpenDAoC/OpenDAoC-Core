using DOL.AI.Brain;

namespace DOL.GS.PropertyCalc;

/// <summary>
/// The critical hit chance calculator. Returns 0 .. 100 chance.
/// 
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 for uncapped realm ability bonus
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.CriticalArcheryHitChance)]
public class RangedCriticalHitChanceCalculator : PropertyCalculator
{
	public RangedCriticalHitChanceCalculator() {}

	public override int CalcValue(GameLiving living, EProperty property) 
	{
		int chance = living.BuffBonusCategory4[(int)property] + living.AbilityBonus[(int)property];

		//Volley effect apply crit chance during volley effect
		EcsGameEffect volley = EffectListService.GetEffectOnTarget(living, EEffect.Volley);
		if (living is GamePlayer archer && volley != null)
		{
			chance += 10;
			if (archer.GetAbility<RealmAbilities.OfRaFalconsEyeAbility>() is RealmAbilities.OfRaFalconsEyeAbility falcon_eye)
				chance += falcon_eye.Amount;
		}
		if (living is GameSummonedPet gamePet)
		{
			if (ServerProperties.Properties.EXPAND_WILD_MINION && gamePet.Brain is IControlledBrain playerBrain
				&& playerBrain.GetPlayerOwner() is GamePlayer player
				&& player.GetAbility<RealmAbilities.OfRaWildMinionAbility>() is RealmAbilities.OfRaWildMinionAbility ab)
				chance += ab.Amount;
		}
		else // not a pet
			chance += 10;

		return chance;
	}
}