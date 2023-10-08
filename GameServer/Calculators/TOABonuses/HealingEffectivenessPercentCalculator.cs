using System;

namespace DOL.GS.PropertyCalc;

[PropertyCalculator(EProperty.HealingEffectiveness)]
public class HealingEffectivenessPercentCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        // Hardcap at 25%
        int percent = Math.Min(25, living.BaseBuffBonusCategory[(int)property]
            - living.DebuffCategory[(int)property]
            + living.ItemBonus[(int)property]);
        // Add RA bonus
        percent += living.AbilityBonus[(int)property];

        // Relic bonus calculated before RA bonuses
        if (living is GamePlayer or GameSummonedPet)
            percent += (int)(100 * RelicMgr.GetRelicBonusModifier(living.Realm, eRelicType.Magic));

        return percent;
    }
}