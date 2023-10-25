using Core.GS.Enums;

namespace Core.GS.Calculators;

[PropertyCalculator(EProperty.MythicalSafeFall)]
public class MythicalSafeFallCalculator : PropertyCalculator
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