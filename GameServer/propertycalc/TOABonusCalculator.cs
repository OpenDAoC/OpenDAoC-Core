using System;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The Spell Range bonus percent calculator
    /// 
    /// BuffBonusCategory1 unused
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 is used for debuff
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>

    //Debuff Effectivness
    [PropertyCalculator(eProperty.DebuffEffectiveness)]
    public class DebuffEffectivnessPercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            // Hardcap at 25%
            return Math.Min(25, living.ItemBonus[property] - living.DebuffCategory[property]);
        }
    }

    //Buff Effectivness
    [PropertyCalculator(eProperty.BuffEffectiveness)]
    public class BuffEffectivenessPercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            GameLiving livingToCheck;

            // Use the player's ability and item bonuses if the caster is a necromancer pet.
            if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
                livingToCheck = playerOwner;
            else
                livingToCheck = living;

            // Hardcap at 25%
            return Math.Min(25, livingToCheck.ItemBonus[property] + livingToCheck.AbilityBonus[property] - living.DebuffCategory[property]);
        }
    }

    // Healing Effectivness
    [PropertyCalculator(eProperty.HealingEffectiveness)]
    public class HealingEffectivenessPercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            // Hardcap at 25%
            int percent = Math.Min(25, living.BaseBuffBonusCategory[property]
                - living.DebuffCategory[property]
                + living.ItemBonus[property]);
            // Add RA bonus
            percent += living.AbilityBonus[property];
            return percent;
        }
    }

    /// <summary>
    /// The critical heal chance calculator. Returns 0 .. 100 chance.
    /// 
    /// Crit propability is capped to 50%
    /// </summary>
    [PropertyCalculator(eProperty.CriticalHealHitChance)]
    public class CriticalHealHitChanceCalculator : PropertyCalculator
    {
        public CriticalHealHitChanceCalculator() { }

        public override int CalcValue(GameLiving living, eProperty property)
        {
            int percent = living.AbilityBonus[property];

            // Hardcap at 50%
            return Math.Min(50, percent);
        }
    }

    //Cast Speed
    [PropertyCalculator(eProperty.CastingSpeed)]
    public class SpellCastSpeedPercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            GameLiving livingToCheck;

            // Use the player's ability and item bonuses if the caster is a necromancer pet.
            if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
                livingToCheck = playerOwner;
            else
                livingToCheck = living;

            // Only custom server settings should have both ability and item bonuses. But this allows both despite the different cap values.
            int abilityBonus = livingToCheck.AbilityBonus[property]; // Mastery of the Art (OF), capped at 15%.
            int abilityBonusOverCap = Math.Max(0, abilityBonus - 15);
            int itemBonus = livingToCheck.ItemBonus[property]; // ToA item bonus, capped at 10%.
            int itemBonusOverCap = Math.Max(0, itemBonus - 10);
            int cappedBonus = (abilityBonus - abilityBonusOverCap) + (itemBonus - itemBonusOverCap);
            int remainingDebuff = Math.Max(0, living.DebuffCategory[property] - (abilityBonusOverCap + itemBonusOverCap));
            return cappedBonus - remainingDebuff;
        }
    }

    //Spell Duration
    [PropertyCalculator(eProperty.SpellDuration)]
    public class SpellDurationPercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            //hardcap at 25%
            return Math.Min(25, living.ItemBonus[property] - living.DebuffCategory[property]);
        }
    }

    //Spell Damage
    [PropertyCalculator(eProperty.SpellDamage)]
    public class SpellDamagePercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            GameLiving livingToCheck;

            // Use the player's ability and item bonuses if the caster is a necromancer pet.
            if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
                livingToCheck = playerOwner;
            else
                livingToCheck = living;

            int abilityBonus = living.AbilityBonus[property];
            int itemBonus = Math.Min(10, livingToCheck.ItemBonus[property]);
            int buffBonus = living.BaseBuffBonusCategory[property] + living.SpecBuffBonusCategory[property];
            int debuffMalus = Math.Abs(livingToCheck.DebuffCategory[property]);
            return abilityBonus + buffBonus + itemBonus - debuffMalus;
        }
    }
}
