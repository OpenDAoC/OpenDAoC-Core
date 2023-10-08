namespace DOL.GS.PropertyCalc;

/// <summary>
/// The evade chance calculator. Returns 0 .. 1000 chance.
/// 
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.EvadeChance)]
public class EvadeChanceCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        int chance = 0;

        if (living is GamePlayer player)
        {
            if (player.HasAbility(Abilities.Evade))
                chance += (int) ((((player.Dexterity + player.Quickness) / 2 - 50) * 0.05 + player.GetAbilityLevel(Abilities.Evade) * 5) * 10);

            chance += player.BaseBuffBonusCategory[(int) property] * 10;
            chance += player.SpecBuffBonusCategory[(int) property] * 10;
            chance -= player.DebuffCategory[(int) property] * 10;
            chance += player.BuffBonusCategory4[(int) property] * 10;
            chance += player.AbilityBonus[(int) property] * 10;
        }
        else if (living is GameNPC npc)
            chance += npc.AbilityBonus[(int)property] * 10 + npc.EvadeChance * 10;

        return chance;
    }
}