using DOL.AI.Brain;

namespace DOL.GS.PropertyCalc;

/// <summary>
/// The parry chance calculator. Returns 0 .. 1000 chance.
/// 
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.ParryChance)]
public class ParryChanceCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        int chance = 0;

        if (living is GamePlayer player)
        {
            if (player.HasSpecialization(Specs.Parry))
                chance += (player.Dexterity * 2 - 100) / 4 + (player.GetModifiedSpecLevel(Specs.Parry) - 1) * (10 / 2) + 50;

            chance += player.BaseBuffBonusCategory[(int) property] * 10;
            chance += player.SpecBuffBonusCategory[(int) property] * 10;
            chance -= player.DebuffCategory[(int) property] * 10;
            chance += player.BuffBonusCategory4[(int) property] * 10;
            chance += player.AbilityBonus[(int) property] * 10;
        }
        else if (living is GameNpc npc)
        {
            chance += npc.ParryChance * 10;

            if (living is NecromancerPet pet && pet.Brain is IControlledBrain)
            {
                chance += pet.BaseBuffBonusCategory[(int) property] * 10;
                chance += pet.SpecBuffBonusCategory[(int) property] * 10;
                chance -= pet.DebuffCategory[(int) property] * 10;
                chance += pet.BuffBonusCategory4[(int) property] * 10;
                chance += pet.AbilityBonus[(int) property] * 10;
                chance += (pet.GetModified(EProperty.Dexterity) * 2 - 100) / 4;
            }
        }

        return chance;
    }
}