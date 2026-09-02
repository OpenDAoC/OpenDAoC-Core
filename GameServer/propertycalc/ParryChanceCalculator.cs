using DOL.AI.Brain;

namespace DOL.GS.PropertyCalc
{
    [PropertyCalculator(eProperty.ParryChance)]
    public class ParryChanceCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            // The returned value is multiplied by 10 to allow for 1 decimal place of precision.
            // Caller should divide by 10 to get the actual percentage.

            int chance = 0;

            if (living is GamePlayer player)
            {
                if (player.HasSpecialization(Specs.Parry))
                    chance += (player.Dexterity * 2 - 100) / 4 + (player.GetModifiedSpecLevel(Specs.Parry) - 1) * (10 / 2) + 50;

                chance += player.BaseBuffBonusCategory[property] * 10;
                chance += player.SpecBuffBonusCategory[property] * 10;
                chance -= player.DebuffCategory[property] * 10;
                chance += player.OtherBonus[property] * 10;
                chance += player.AbilityBonus[property] * 10;
            }
            else if (living is GameNPC npc)
            {
                chance += npc.ParryChance * 10;

                if (living is NecromancerPet pet && pet.Brain is IControlledBrain)
                {
                    chance += pet.BaseBuffBonusCategory[property] * 10;
                    chance += pet.SpecBuffBonusCategory[property] * 10;
                    chance -= pet.DebuffCategory[property] * 10;
                    chance += pet.OtherBonus[property] * 10;
                    chance += pet.AbilityBonus[property] * 10;
                    chance += (pet.GetModified(eProperty.Dexterity) * 2 - 100) / 4;
                }
            }

            return chance;
        }
    }
}
