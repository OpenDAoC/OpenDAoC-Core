using System;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The Spell Range bonus percent calculator
    ///
    /// BuffBonusCategory1 unused
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 is used for debuff
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.StyleDamage)]
    public class StyleDamagePercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            return Math.Min(10, living.ItemBonus[property]);
        }
    }
}
