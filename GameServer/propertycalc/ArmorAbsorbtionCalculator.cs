using System;

namespace DOL.GS.PropertyCalc
{
    [PropertyCalculator(eProperty.ArmorAbsorption)]
    public class ArmorAbsorptionCalculator : PropertyCalculator
    {
        private static int MAX = 50;

        public override int CalcValue(GameLiving living, eProperty property)
        {
            int absorb = living.BaseBuffBonusCategory[property];
            absorb -= Math.Abs(living.DebuffCategory[property]);
            absorb += living.ItemBonus[property];
            absorb += living.AbilityBonus[property];
            return Math.Min(MAX, absorb);
        }
    }
}
