using System;
using DOL.GS.Keeps;

namespace DOL.GS.PropertyCalc
{
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
    [PropertyCalculator(eProperty.ArmorFactor)]
    public class ArmorFactorCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            switch (living)
            {
                case GamePlayer:
                case GameTrainingDummy:
                    return CalculatePlayerArmorFactor(living, property);
                case GameKeepDoor:
                case GameKeepComponent:
                    return CalculateKeepComponentArmorFactor(living);
                case IGameEpicNpc epicNpc:
                    return CalculateLivingArmorFactor(living, property, 12.5 * epicNpc.ArmorFactorScalingFactor, 50);
                case NecromancerPet:
                    return CalculateLivingArmorFactor(living, property, 12.5, 121);
                case GameSummonedPet:
                    return CalculateLivingArmorFactor(living, property, 12.5, 175);
                case GuardLord:
                    return CalculateLivingArmorFactor(living, property, 12.5, 134);
                default:
                    return CalculateLivingArmorFactor(living, property, 12.5, 200);
            }

            static int CalculatePlayerArmorFactor(GameLiving living, eProperty property)
            {
                // Base AF buffs are calculated in the item's armor calc since they have the same cap.
                int armorFactor = Math.Min((int) (living.Level * 1.875), living.SpecBuffBonusCategory[property]);
                armorFactor -= Math.Abs(living.DebuffCategory[property]);
                armorFactor += Math.Min(living.Level, living.ItemBonus[property]);
                armorFactor += living.OtherBonus[property];
                return armorFactor;
            }

            static int CalculateLivingArmorFactor(GameLiving living, eProperty property, double factor, double divisor)
            {
                int armorFactor = (int) ((1 + living.Level / divisor) * (living.Level * factor));

                // Some source state either base AF or spec AF isn't supposed to work on NPCs.
                // In any case, having some buffs not doing anything feels pretty bad. Some pets also have a self spec AF buff.
                armorFactor += living.BaseBuffBonusCategory[property] + living.SpecBuffBonusCategory[property];
                armorFactor -= Math.Abs(living.DebuffCategory[property]);
                armorFactor += living.OtherBonus[property];
                return armorFactor;
            }

            static int CalculateKeepComponentArmorFactor(GameLiving living)
            {
                GameKeepComponent component = null;

                if (living is GameKeepDoor keepDoor)
                    component = keepDoor.Component;
                else if (living is GameKeepComponent)
                    component = living as GameKeepComponent;

                if (component == null)
                    return 0;

                double keepLevelMod = 1 + component.Keep.Level * 0.1;
                int typeMod = component.Keep is GameKeep ? 24 : 12;
                return (int) (component.Keep.BaseLevel * keepLevelMod * typeMod);
            }
        }
    }
}
