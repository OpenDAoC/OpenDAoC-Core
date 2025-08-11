using System;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The health regen rate calculator
    /// 
    /// BuffBonusCategory1 is used for all buffs
    /// BuffBonusCategory2 is used for all debuffs (positive values expected here)
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.EnduranceRegenerationAmount)]
    public class EnduranceRegenerationAmountCalculator : PropertyCalculator
    {
        public EnduranceRegenerationAmountCalculator() { }

        public override int CalcValue(GameLiving living, eProperty property)
        {
            // In 1.65, tireless is supposed to run on its own timer and regenerate 1 endurance per (9 - level) seconds. Works in and out combat, but is notoriously bad.
            // The way endurance regeneration currently works doesn't allow for it to be implemented that way and we're still using Atlas' version of tireless instead.
            // It has a max level of 1, and always adds 1 endurance per tick.

            /*    Patch 1.87 - COMBAT AND REGENERATION CHANGES
                - The bonus to regeneration while standing out of combat has been greatly increased. The amount of ticks 
                    a player receives while standing has been doubled and it will now match the bonus to regeneration while sitting.
                    Players will no longer need to sit to regenerate faster.
                - Fatigue now regenerates at the standing rate while moving.
            */

            int debuff = living.SpecBuffBonusCategory[property];

            if (debuff < 0)
                debuff = -debuff;

            // Buffs allow to regenerate endurance even in combat and while moving.
            double regen = living.BaseBuffBonusCategory[property] + living.AbilityBonus[property] + living.ItemBonus[property] - debuff;

            if (!living.InCombat && living is GamePlayer player && !player.IsMoving)
                regen += player.IsSitting ? 4 : 1;

            regen *= ServerProperties.Properties.ENDURANCE_REGEN_AMOUNT_MODIFIER;
            return Math.Max(0, (int) regen);
        }
    }
}
