using System;
using Core.GS.Enums;

namespace Core.GS.Calculators;

/// <summary>
/// The critical heal chance calculator. Returns 0 .. 100 chance.
/// 
/// Crit probability is capped to 50%
/// </summary>
[PropertyCalculator(EProperty.CriticalHealHitChance)]
public class HealCriticalHitChanceCalculator : PropertyCalculator
{
    public HealCriticalHitChanceCalculator() { }

    public override int CalcValue(GameLiving living, EProperty property)
    {
        int percent = living.AbilityBonus[(int)property];

        // Hardcap at 50%
        return Math.Min(50, percent);
    }
}