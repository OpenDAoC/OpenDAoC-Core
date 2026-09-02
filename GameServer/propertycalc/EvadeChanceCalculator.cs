namespace DOL.GS.PropertyCalc
{
    [PropertyCalculator(eProperty.EvadeChance)]
    public class EvadeChanceCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            // The returned value is multiplied by 10 to allow for 1 decimal place of precision.
            // Caller should divide by 10 to get the actual percentage.

            int chance = 0;

            if (living is GamePlayer player)
            {
                if (player.HasAbility(Abilities.Evade))
                    chance += (900 + player.Quickness + player.Dexterity) * player.GetAbilityLevel(Abilities.Evade) * 5 / 100;

                chance += player.BaseBuffBonusCategory[property] * 10;
                chance += player.SpecBuffBonusCategory[property] * 10;
                chance -= player.DebuffCategory[property] * 10;
                chance += player.OtherBonus[property] * 10;
                chance += player.AbilityBonus[property] * 10;
            }
            else if (living is GameNPC npc)
                chance += npc.AbilityBonus[property] * 10 + npc.EvadeChance * 10;

            return chance;
        }
    }
}
