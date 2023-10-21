using System;

namespace DOL.GS.PropertyCalc;

[PropertyCalculator(EProperty.BuffEffectiveness)]
public class BuffEffectivenessPercentCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        GameLiving livingToCheck;

        if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
            livingToCheck = playerOwner;
        else
            livingToCheck = living;

        // Hardcap at 25%
        return Math.Min(25, livingToCheck.ItemBonus[(int) property] + livingToCheck.AbilityBonus[(int) property] - living.DebuffCategory[(int) property]);
    }
}