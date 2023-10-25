using Core.GS.Enums;

namespace Core.GS.Calculators;

/// <summary>
/// The melee damage bonus percent calculator
///
/// BuffBonusCategory1 is used for buffs
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 is used for debuff
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.KeepDamage)]
public class KeepDamagePercentCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        int percent = 100
            +living.BaseBuffBonusCategory[(int)property];

        return percent;
    }
}