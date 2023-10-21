using Core.GS.Enums;

namespace Core.GS.Calculators;

/// <summary>
/// The block chance calculator. Returns 0 .. 1000 chance.
/// 
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.BlockChance)]
public class BlockChanceCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        int chance = 0;

        if (living is GamePlayer player)
        {
            chance += (player.Dexterity * 2 - 100) / 4 + (player.GetModifiedSpecLevel(Specs.Shields) - 1) * (10 / 2) + 50;
            chance += player.AbilityBonus[(int) property] * 10;
        }
        else if (living is GameNpc npc)
            chance += npc.BlockChance * 10;

        return chance;
    }
}