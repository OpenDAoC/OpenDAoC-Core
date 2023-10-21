using System;

namespace Core.GS.PropertyCalc;

[PropertyCalculator(EProperty.CastingSpeed)]
public class SpellCastSpeedPercentCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        GameLiving livingToCheck;

        if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
            livingToCheck = playerOwner;
        else
            livingToCheck = living;

        // Only custom server settings should have both ability and item bonuses. But this allows both despite the different cap values.
        int abilityBonus = livingToCheck.AbilityBonus[(int) property]; // Mastery of the Art (OF), capped at 15%.
        int abilityBonusOverCap = Math.Max(0, abilityBonus - 15);
        int itemBonus = livingToCheck.ItemBonus[(int) property]; // ToA item bonus, capped at 10%.
        int itemBonusOverCap = Math.Max(0, itemBonus - 10);
        int cappedBonus = (abilityBonus - abilityBonusOverCap) + (itemBonus - itemBonusOverCap);
        int remainingDebuff = Math.Max(0, living.DebuffCategory[(int) property] - (abilityBonusOverCap + itemBonusOverCap));
        return cappedBonus - remainingDebuff;
    }
}