namespace Core.GS.PropertyCalc;

[PropertyCalculator(EProperty.MythicalDiscumbering)]
public class MythicalDiscumberingCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        if (living is GamePlayer)
        {
            return living.ItemBonus[(int)property];
        }
        return 0;
    }
}