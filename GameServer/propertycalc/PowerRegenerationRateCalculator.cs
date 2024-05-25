namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The power regen rate calculator
    /// 
    /// BuffBonusCategory1 is used for all buffs
    /// BuffBonusCategory2 is used for all debuffs (positive values expected here)
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.PowerRegenerationRate)]
    public class PowerRegenerationRateCalculator : PropertyCalculator
    {
        public PowerRegenerationRateCalculator() {}

        public override int CalcValue(GameLiving living, eProperty property) 
        {
            /* PATCH 1.87 COMBAT AND REGENERATION
              - While in combat, health and power regeneration ticks will happen twice as often.
              - Each tick of health and power is now twice as effective.
              - All health and power regeneration aids are now twice as effective.
             */

            double regen = living.Level / 10.0 + living.Level / 2.75;

            // What is this? NPCs don't have power.
            if (living is GameNPC && living.InCombat)
                regen /= 2.0;

            regen *= ServerProperties.Properties.MANA_REGEN_RATE;

            double decimals = regen - (int) regen;

            // Compensate for int rounding.
            if (Util.ChanceDouble(decimals))
                regen += 1;

            int debuff = living.SpecBuffBonusCategory[(int) property];

            if (debuff < 0)
                debuff = -debuff;

            regen += living.BaseBuffBonusCategory[(int) property] + living.AbilityBonus[(int) property] + living.ItemBonus[(int) property] - debuff;

            if (regen < 1)
                regen = 1;

            return (int) regen;
        }
    }
}
