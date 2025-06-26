using System;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The Archery Range bonus percent calculator
    ///
    /// BuffBonusCategory1 unused
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 is used for debuff
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.ArcheryRange)]
    public class ArcheryRangePercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            int debuff = living.DebuffCategory[property];
            int item = Math.Max(0, 100 - debuff + Math.Min(10, living.ItemBonus[property]));// http://www.camelotherald.com/more/1325.shtml
            int ability = 0;

            if (living.rangeAttackComponent.RangedAttackType is eRangedAttackType.Long)
                ability = 50;

            return item + ability;
        }
    }
}
