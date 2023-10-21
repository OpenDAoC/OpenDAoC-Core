using System;
using Core.GS.Enums;
using Core.GS.Keeps;

namespace Core.GS.Calculators;

/// <summary>
/// The Armor Factor calculator
///
/// BuffBonusCategory1 is used for base buffs directly in player.GetArmorAF because it must be capped by item AF cap
/// BuffBonusCategory2 is used for spec buffs, level*1.875 cap for players
/// BuffBonusCategory3 is used for debuff, uncapped
/// BuffBonusCategory4 is used for buffs, uncapped
/// BuffBonusMultCategory1 unused
/// ItemBonus is used for players TOA bonuse, living.Level cap
/// </summary>
[PropertyCalculator(EProperty.ArmorFactor)]
public class ArmorFactorCalculator : PropertyCalculator
{
    public override int CalcValue(GameLiving living, EProperty property)
    {
        switch (living)
        {
            case GamePlayer:
            case GameTrainingDummy:
                return CalculatePlayerArmorFactor(living, property);
            case GameKeepDoor:
            case GameKeepComponent:
                return CalculateKeepComponentArmorFactor(living);
            case GameEpicNPC:
            case GameEpicBoss:
            {
                double epicScaleFactor = CalculateEpicScaleFactor(living);
                return CalculateLivingArmorFactor(living, property, 12.0 * epicScaleFactor, 50.0, false);
            }
            case GameSummonedPet:
                return CalculateLivingArmorFactor(living, property, 12.0, living is NecromancerPet ? 121.0 : 175.0, true);
            default:
                return CalculateLivingArmorFactor(living, property, 12.0, living is GuardLord ? 134.0 : 200.0, false);
        }
    }

    private static int CalculatePlayerArmorFactor(GameLiving living, EProperty property)
    {
        // Base AF buffs are calculated in the item's armor calc since they have the same cap.
        int armorFactor = Math.Min((int) (living.Level * 1.875), living.SpecBuffBonusCategory[(int) property]);
        armorFactor -= Math.Abs(living.DebuffCategory[(int) property]);
        armorFactor += Math.Min(living.Level, living.ItemBonus[(int) property]);
        armorFactor += living.BuffBonusCategory4[(int) property];
        armorFactor /= 6;
        return Math.Max(1, armorFactor);
    }

    private static int CalculateLivingArmorFactor(GameLiving living, EProperty property, double factor, double divisor, bool useBaseBuff)
    {
        int armorFactor = (int) ((1 + living.Level / divisor) * (living.Level * factor));

        if (useBaseBuff)
            armorFactor += living.BaseBuffBonusCategory[(int) property];

        armorFactor += living.SpecBuffBonusCategory[(int) property];
        armorFactor -= Math.Abs(living.DebuffCategory[(int) property]);
        armorFactor += living.BuffBonusCategory4[(int) property];
        armorFactor /= 6;
        return Math.Max(1, armorFactor);
    }

    private static int CalculateKeepComponentArmorFactor(GameLiving living)
    {
        GameKeepComponent component = null;

        if (living is GameKeepDoor keepDoor)
            component = keepDoor.Component;
        else if (living is GameKeepComponent)
            component = living as GameKeepComponent;

        if (component == null)
            return 1;

        double keepLevelMod = 1 + component.Keep.Level * 0.1;
        int typeMod = component.Keep is GameKeep ? 4 : 2;
        return Math.Max(1, (int) (component.Keep.BaseLevel * keepLevelMod * typeMod));
    }

    private static double CalculateEpicScaleFactor(GameLiving living)
    {
        double epicScaleFactor;
        int petCap;

        if (living is GameEpicBoss)
        {
            epicScaleFactor = 1.6;
            petCap = 24;
        }
        else
        {
            epicScaleFactor = 0.8;
            petCap = 16;
        }

        int petCount = 0;

        // TODO: Find a way to remove `ToList` call.
        foreach (GameLiving attacker in living.attackComponent.Attackers.Keys)
        {
            if (attacker is GamePlayer)
                epicScaleFactor -= 0.04;
            else if (attacker is GameSummonedPet && petCount <= petCap)
            {
                epicScaleFactor -= 0.01;
                petCount++;
            }

            if (epicScaleFactor < 0.4)
            {
                epicScaleFactor = 0.4;
                break;
            }
        }

        return epicScaleFactor;
    }
}