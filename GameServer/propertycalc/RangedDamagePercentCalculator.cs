using System;

namespace DOL.GS.PropertyCalc
{
    [PropertyCalculator(eProperty.RangedDamage)]
    public class RangedDamagePercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            int hardCap = 10;
            int abilityBonus = living.AbilityBonus[property];
            int itemBonus = Math.Min(hardCap, living.ItemBonus[property]);
            int buffBonus = living.BaseBuffBonusCategory[property] + living.SpecBuffBonusCategory[property];
            int debuffMalus = Math.Min(hardCap, Math.Abs(living.DebuffCategory[property]));
            return abilityBonus + buffBonus + itemBonus - debuffMalus;
        }
    }
}
