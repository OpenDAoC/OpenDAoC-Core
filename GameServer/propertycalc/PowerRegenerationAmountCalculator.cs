using System;

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
    [PropertyCalculator(eProperty.PowerRegenerationAmount)]
    public class PowerRegenerationAmountCalculator : PropertyCalculator
    {
        public PowerRegenerationAmountCalculator() { }

        public override int CalcValue(GameLiving living, eProperty property)
        {
            /* PATCH 1.87 COMBAT AND REGENERATION
              - While in combat, health and power regeneration ticks will happen twice as often.
              - Each tick of health and power is now twice as effective.
              - All health and power regeneration aids are now twice as effective.
             */

            // Reverted 1.87 changes.
            // From DoL's `5 + (living.Level / 2.75)`.
            // 12 power per tick at level 50 instead of 20.68.
            double regen = 2.5 + living.Level * 0.2;
            int debuff = living.SpecBuffBonusCategory[(int) property];

            if (debuff < 0)
                debuff = -debuff;

            regen += living.BaseBuffBonusCategory[(int) property] + living.AbilityBonus[(int) property] + living.ItemBonus[(int) property] - debuff;

            if (ServerProperties.Properties.MANA_REGEN_AMOUNT_HALVED_BELOW_50_PERCENT &&
                living is GamePlayer player &&
                player.CharacterClass.ClassType is eClassType.ListCaster &&
                player.ManaPercent < 50)
            {
                regen /= 2;
            }

            regen *= ServerProperties.Properties.MANA_REGEN_AMOUNT_MODIFIER;
            return Math.Max(1, (int) regen);
        }
    }
}
