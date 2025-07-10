using System;

namespace DOL.GS.PropertyCalc
{
    [PropertyCalculator(eProperty.MeleeDamage)]
    public class MeleeDamagePercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            int abilityBonus = living.AbilityBonus[property];
            int itemBonus = Math.Min(10, living.ItemBonus[property]);
            int buffBonus = living.BaseBuffBonusCategory[property] + living.SpecBuffBonusCategory[property];
            int debuffMalus = Math.Abs(living.DebuffCategory[property]);
            return abilityBonus + buffBonus + itemBonus - debuffMalus;
        }
    }
}
