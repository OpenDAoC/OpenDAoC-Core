using DOL.GS.RealmAbilities;

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
    [PropertyCalculator(eProperty.EnduranceRegenerationRate)]
    public class EnduranceRegenerationRateCalculator : PropertyCalculator
    {
        public EnduranceRegenerationRateCalculator() { }

        public override int CalcValue(GameLiving living, eProperty property)
        {
            GamePlayer player = living as GamePlayer;
            int debuff = living.SpecBuffBonusCategory[(int) property];

            if (debuff < 0)
                debuff = -debuff;

            // Buffs allow to regenerate endurance even in combat and while moving.
            double regenBuff = living.BaseBuffBonusCategory[(int) property] + living.ItemBonus[(int) property];
            double regen = regenBuff;

            if (regen == 0 && player != null)
                regen++;

            AtlasOF_RAEndRegenEnhancer raTireless = living.GetAbility<AtlasOF_RAEndRegenEnhancer>();

            if (raTireless != null)
                regen++;

            /*    Patch 1.87 - COMBAT AND REGENERATION CHANGES
                - The bonus to regeneration while standing out of combat has been greatly increased. The amount of ticks 
                    a player receives while standing has been doubled and it will now match the bonus to regeneration while sitting.
                    Players will no longer need to sit to regenerate faster.
                - Fatigue now regenerates at the standing rate while moving.
            */

            if (!living.InCombat)
            {
                if (player != null && !player.IsSprinting)
                    regen += 4;
            }
            else
            {
                regen -= 3;

                if (regen <= 0)
                    regen = 0.1;

                if (regenBuff > 0)
                    regen = regenBuff;

                if (player != null && raTireless != null)
                    regen++;
            }

            regen -= debuff;

            if (regen < 0)
                regen = 0;
            else
                regen *= ServerProperties.Properties.ENDURANCE_REGEN_RATE;

            double decimals = regen - (int) regen;

            // Compensate for int rounding.
            if (Util.ChanceDouble(decimals))
                regen += 1;

            return (int) regen;
        }
    }
}
