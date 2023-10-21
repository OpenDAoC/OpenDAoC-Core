using System;

namespace DOL.GS.PropertyCalc;

/// <summary>
/// The ArcaneSyphon calculator: 25% cap like live server: http://www.camelotherald.com/more/3202.shtml
///
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.ArcaneSyphon)]
public class ArcaneSyphonCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        GamePlayer player = living as GamePlayer;
        if (player == null)
        {
            return 0;
        }

        return Math.Min(living.ItemBonus[(int)property], 25);
    }
}