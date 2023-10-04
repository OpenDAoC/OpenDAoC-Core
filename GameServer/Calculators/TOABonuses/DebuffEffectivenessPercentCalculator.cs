using System;

namespace DOL.GS.PropertyCalc;

[PropertyCalculator(eProperty.DebuffEffectivness)]
public class DebuffEffectivenessPercentCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, eProperty property)
    {
        // Hardcap at 25%
        return Math.Min(25, living.ItemBonus[(int)property] - living.DebuffCategory[(int)property]);
    }
}