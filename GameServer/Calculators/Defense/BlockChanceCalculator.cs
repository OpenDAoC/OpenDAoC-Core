namespace DOL.GS.PropertyCalc;

/// <summary>
/// The block chance calculator. Returns 0 .. 1000 chance.
/// 
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[APropertyCalculator(eProperty.BlockChance)]
public class BlockChanceCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, eProperty property)
    {
        int chance = 0;

        if (living is GamePlayer player)
        {
            chance += (player.Dexterity * 2 - 100) / 4 + (player.GetModifiedSpecLevel(Specs.Shields) - 1) * (10 / 2) + 50;
            chance += player.AbilityBonus[(int) property] * 10;
        }
        else if (living is GameNPC npc)
            chance += npc.BlockChance * 10;

        return chance;
    }
}