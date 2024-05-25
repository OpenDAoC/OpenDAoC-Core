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
    [PropertyCalculator(eProperty.HealthRegenerationRate)]
    public class HealthRegenerationRateCalculator : PropertyCalculator
    {
        public HealthRegenerationRateCalculator() { }

        public override int CalcValue(GameLiving living, eProperty property)
        {
            if (living.IsDiseased || living.effectListComponent.ContainsEffectForEffectType(eEffect.Bleed))
                return 0;

            /* PATCH 1.87 COMBAT AND REGENERATION
              - While in combat, health and power regeneration ticks will happen twice as often.
              - Each tick of health and power is now twice as effective.
              - All health and power regeneration aids are now twice as effective.
             */

            double regen = 5 + living.Level * 0.5;

            if (living is GameNPC npc)
            {
                if (npc.InCombat)
                    regen /= 2.0;
                else if (npc is not NecromancerPet)
                    regen *= 5;
            }
            else if (living is GamePlayer)
            {
                if (living.IsSitting)
                    regen *= 1.75;
            }

            regen *= ServerProperties.Properties.HEALTH_REGEN_RATE;

            double decimals = regen - (int) regen;

            // Compensate for int rounding.
            if (Util.ChanceDouble(decimals))
                regen += 1;

            regen += living.ItemBonus[(int)property];

            int debuff = living.SpecBuffBonusCategory[(int) property];

            if (debuff < 0)
                debuff = -debuff;

            regen += living.BaseBuffBonusCategory[(int) property] - debuff;

            if (regen < 1)
                regen = 1;

            return (int) regen;
        }
    }
}
