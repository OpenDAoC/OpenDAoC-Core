namespace DOL.GS.PropertyCalc;

[APropertyCalculator(eProperty.MythicalDiscumbering)]
public class MythicalDiscumberingCalculator : PropertyCalculator
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