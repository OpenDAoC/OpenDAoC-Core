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
[PropertyCalculator(EProperty.MissHit)]
public class MissHitPercentCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        return (int)(
            +living.BaseBuffBonusCategory[(int)property]
            + living.SpecBuffBonusCategory[(int)property]
            - living.DebuffCategory[(int)property]
            + living.BuffBonusCategory4[(int)property]);
    }
}