namespace DOL.GS.PropertyCalc
{
    [PropertyCalculator(eProperty.BlockChance)]
    public class BlockChanceCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            // The returned value is multiplied by 10 to allow for 1 decimal place of precision.
            // Caller should divide by 10 to get the actual percentage.

            int chance = 0;

            if (living is GamePlayer player)
            {
                chance += (player.Dexterity * 2 - 100) / 4 + (player.GetModifiedSpecLevel(Specs.Shields) - 1) * (10 / 2) + 50;
                chance += player.AbilityBonus[property] * 10;
            }
            else if (living is GameNPC npc)
                chance += npc.BlockChance * 10;

            return chance;
        }
    }
}
