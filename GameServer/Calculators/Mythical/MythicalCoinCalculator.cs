namespace DOL.GS.PropertyCalc;

[APropertyCalculator(eProperty.MythicalCoin)]
public class MythicalCoinCalculator : PropertyCalculator
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