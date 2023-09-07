/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using System.Linq;
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

        private static int CalculatePlayerArmorFactor(GameLiving living, eProperty property)
        {
            // Base AF buffs are calculated in the item's armor calc since they have the same cap.
            int armorFactor = Math.Min((int) (living.Level * 1.875), living.SpecBuffBonusCategory[(int) property]);
            armorFactor -= Math.Abs(living.DebuffCategory[(int) property]);
            armorFactor += Math.Min(living.Level, living.ItemBonus[(int) property]);
            armorFactor += living.BuffBonusCategory4[(int) property];
            armorFactor /= 6;
            return Math.Max(1, armorFactor);
        }

        private static int CalculateLivingArmorFactor(GameLiving living, eProperty property, double factor, double divisor, bool useBaseBuff)
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
            foreach (GameObject attacker in living.attackComponent.Attackers.ToList())
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
}
