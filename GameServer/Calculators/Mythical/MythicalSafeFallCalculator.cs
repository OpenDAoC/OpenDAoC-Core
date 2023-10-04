namespace DOL.GS.PropertyCalc;

/// <summary>
/// Calculator for Mythical Safe Fall
/// </summary>
[PropertyCalculator(eProperty.MythicalSafeFall)]
public class MythicalSafeFallCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, eProperty property)
    {
        if (living is GamePlayer)
        {
            return living.ItemBonus[(int)property];
        }
        return 0;
    }
}