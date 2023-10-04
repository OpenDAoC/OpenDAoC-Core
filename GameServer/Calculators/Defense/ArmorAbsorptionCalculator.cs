using System;

namespace DOL.GS.PropertyCalc;

[APropertyCalculator(eProperty.ArmorAbsorption)]
public class ArmorAbsorptionCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, eProperty property)
    {
        int buffBonus = living.BaseBuffBonusCategory[property];
        int debuffMalus = Math.Abs(living.DebuffCategory[property]);
        int itemBonus = living.ItemBonus[property];
        int abilityBonus = living.AbilityBonus[property];
        int hardCap = 50;
        return Math.Min(hardCap, buffBonus - debuffMalus + itemBonus + abilityBonus);
    }
}