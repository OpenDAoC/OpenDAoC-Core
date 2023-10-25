using System;
using Core.GS.Enums;

namespace Core.GS.Calculators;

[PropertyCalculator(EProperty.SpellDuration)]
public class SpellDurationPercentCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        //hardcap at 25%
        return Math.Min(25, living.ItemBonus[(int)property] - living.DebuffCategory[(int)property]);
    }
}