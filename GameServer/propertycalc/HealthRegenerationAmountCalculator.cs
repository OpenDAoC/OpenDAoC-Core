using System;
using DOL.AI.Brain;

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
    [PropertyCalculator(eProperty.HealthRegenerationAmount)]
    public class HealthRegenerationAmountCalculator : PropertyCalculator
    {
        public HealthRegenerationAmountCalculator() { }

        public override int CalcValue(GameLiving living, eProperty property)
        {
            if (living.IsDiseased || living.effectListComponent.ContainsEffectForEffectType(eEffect.Bleed))
                return 0;

            /* PATCH 1.87 COMBAT AND REGENERATION
              - While in combat, health and power regeneration ticks will happen twice as often.
              - Each tick of health and power is now twice as effective.
              - All health and power regeneration aids are now twice as effective.
             */

            // Reverted 1.87 changes.
            // From DoL's `living.Level * 0.6` above level 25, `10 + (living.Level * 0.2)` below level 26.
            // 15 health per tick at level 50 instead of 30.
            double regen = 2.5 + living.Level * 0.25;
            int debuff = living.SpecBuffBonusCategory[property];

            if (debuff < 0)
                debuff = -debuff;

            regen += living.BaseBuffBonusCategory[property] + living.AbilityBonus[property] + living.ItemBonus[property] - debuff;

            if (living is GameNPC npc)
            {
                if (npc.InCombat)
                    regen /= 2.0;
                else if (npc is not NecromancerPet && (npc.Brain is not StandardMobBrain brain || !brain.HasAggro))
                {
                    // NPCs that are out of combat and with an empty aggro list regen much faster.
                    // This behavior is meant to be part of the "run away, regen, re-aggro" mechanic.
                    // Live (July 2026) uses about 5% every 2.5~3 seconds.
                    // Uthgard uses 10% every 6 seconds.
                    // We apply it to most pets out of convenience for classes that cannot heal theirs.
                    regen = npc.MaxHealth * 0.1;
                }
            }

            regen *= ServerProperties.Properties.HEALTH_REGEN_AMOUNT_MODIFIER;
            return Math.Max(1, (int) regen);
        }
    }
}
