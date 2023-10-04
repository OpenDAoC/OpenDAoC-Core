using System;

namespace DOL.GS.PropertyCalc;

/// <summary>
/// The critical heal chance calculator. Returns 0 .. 100 chance.
/// 
/// Crit probability is capped to 50%
/// </summary>
[APropertyCalculator(eProperty.CriticalHealHitChance)]
public class HealCriticalHitChanceCalculator : PropertyCalculator
{
    public HealCriticalHitChanceCalculator() { }

    public override int CalcValue(GameLiving living, eProperty property)
    {
        int percent = living.AbilityBonus[(int)property];

        // Hardcap at 50%
        return Math.Min(50, percent);
    }
}