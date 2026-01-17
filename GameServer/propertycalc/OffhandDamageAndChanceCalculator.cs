using System;

namespace DOL.GS.PropertyCalc
{
    [PropertyCalculator(eProperty.OffhandDamageAndChance)]
    public class OffhandDamageAndChanceCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            return Math.Max(0, living.AbilityBonus[property]);
        }
    }
}
