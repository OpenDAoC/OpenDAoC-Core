using System;

namespace DOL.GS.PropertyCalc
{
    [PropertyCalculator(eProperty.MeleeSpeed)]
    [PropertyCalculator(eProperty.ArcherySpeed)]
    [PropertyCalculator(eProperty.CastingSpeed)]
    public class AttackSpeedPercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            // Based on https://camelotherald.fandom.com/wiki/Melee_Damage
            // Our rounding it a bit different.
            // The returned value is multiplied by 10 to allow for 1 decimal place of precision.
            // Caller should divide by 10 to get the actual percentage.

            GameLiving livingToCheck;

            // Use the player's ability and item bonuses if the caster is a necromancer pet.
            if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
                livingToCheck = playerOwner;
            else
                livingToCheck = living;

            int abilityBonus = livingToCheck.AbilityBonus[property]; // Mastery of Arms, Mastery of Archery, Mastery of the Art (OF).
            int itemBonus = Math.Min(10, livingToCheck.ItemBonus[property]); // ToA item bonus, capped at 10%.
            int buffBonus = living.BaseBuffBonusCategory[property] + living.SpecBuffBonusCategory[property] - Math.Abs(livingToCheck.DebuffCategory[property]);

            // Three layers of bonuses; multiplicative.
            double result = buffBonus;
            result += (1 - result * 0.01) * abilityBonus;
            result += (1 - result * 0.01) * itemBonus;
            return (int) ((100 - result) * 10);
        }
    }
}
