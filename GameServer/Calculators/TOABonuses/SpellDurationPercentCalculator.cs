using System;

namespace DOL.GS.PropertyCalc;

[APropertyCalculator(eProperty.SpellDuration)]
public class SpellDurationPercentCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, eProperty property)
    {
        //hardcap at 25%
        return Math.Min(25, living.ItemBonus[(int)property] - living.DebuffCategory[(int)property]);
    }
}