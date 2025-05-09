using System;
using DOL.GS.Keeps;
using DOL.GS.RealmAbilities;
using DOL.GS.ServerProperties;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The Max HP calculator
    ///
    /// BuffBonusCategory1 is used for absolute HP buffs
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.MaxHealth)]
    public class MaxHealthCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            if (living is GamePlayer player)
            {
                int hpBase = player.CalculateMaxHealth(player.Level, player.GetModified(eProperty.Constitution));
                int buffBonus = player.BaseBuffBonusCategory[property];

                if (buffBonus < 0)
                    buffBonus = (int) ((1 + buffBonus / -100.0) * hpBase) - hpBase;

                int itemBonus = player.ItemBonus[property];
                int cap = GetItemBonusCap(player) + GetItemBonusCapIncrease(player);
                itemBonus = Math.Min(itemBonus, cap);

                if (player.HasAbility(Abilities.ScarsOfBattle) && player.Level >= 40)
                {
                    int levelBonus = Math.Min(player.Level - 40, 10);
                    hpBase = (int) (hpBase * (100 + levelBonus) * 0.01);
                }

                int flatAbilityBonus = living.AbilityBonus[property]; // New Toughness.
                int multiplicativeAbilityBonus = living.AbilityBonus[eProperty.Of_Toughness]; // Old Toughness.

                double result = hpBase;
                result *= 1 + multiplicativeAbilityBonus * 0.01;
                result += itemBonus + buffBonus + flatAbilityBonus;
                return (int) result;
            }
            else if (living is GameKeepComponent keepComponent)
            {
                AbstractGameKeep gameKeep = keepComponent.Keep;

                if (gameKeep != null)
                {
                    int baseHealth = gameKeep.BaseLevel * Properties.KEEP_COMPONENTS_BASE_HEALTH;
                    baseHealth += (int) (baseHealth * (gameKeep.Level - 1) * Properties.KEEP_COMPONENTS_HEALTH_UPGRADE_MODIFIER);
                    return baseHealth;
                }

                return 0;
            }
            else if (living is GameKeepDoor)
            {
                AbstractGameKeep gameKeep = (living as GameKeepDoor)?.Component?.Keep;

                if (gameKeep != null)
                {
                    if (gameKeep.IsRelic)
                        return Properties.RELIC_DOORS_HEALTH;

                    int baseHealth = gameKeep.BaseLevel * Properties.KEEP_DOORS_BASE_HEALTH;
                    baseHealth += (int) (baseHealth * (gameKeep.Level - 1) * Properties.KEEP_DOORS_HEALTH_UPGRADE_MODIFIER);
                    return baseHealth;
                }

                return 0;
            }
            else if (living is NecromancerPet necromancerPet)
            {
                double baseHp = 32.5 * necromancerPet.Level;
                GameLiving livingOwner = necromancerPet.Owner;

                if (livingOwner == null)
                    return (int) baseHp;

                int conFromRa = AtlasRAHelpers.GetStatEnhancerAmountForLevel(AtlasRAHelpers.GetAugConLevel(livingOwner));
                int conFromItems = livingOwner.GetModifiedFromItems(eProperty.Constitution);

                int itemBonus = livingOwner.ItemBonus[property];
                int itemCap = GetItemBonusCap(livingOwner) + GetItemBonusCapIncrease(livingOwner);
                itemBonus = Math.Min(itemBonus, itemCap);

                int flatAbilityBonus = livingOwner.AbilityBonus[property]; // New Toughness.
                int multiplicativeAbilityBonus = livingOwner.AbilityBonus[eProperty.Of_Toughness]; // Old Toughness.

                double result = baseHp;
                result += (conFromItems + conFromRa) * 3;
                result *= 1 + multiplicativeAbilityBonus * 0.01;
                result += itemBonus + flatAbilityBonus;
                return (int) result;
            }
            else if (living is GameSummonedPet pet)
                return CalculateNpcMaxHealth(pet, 17, 0.535, pet.GetBaseStat(eStat.CON), 25, 3, pet.BaseBuffBonusCategory[property]);
            else if (living is GameNPC npc)
                return CalculateNpcMaxHealth(npc, 11, 0.6, npc.GetBaseStat(eStat.CON), 25, 1.8, npc.BaseBuffBonusCategory[property]);

            // Old formula. No idea if it's being used by anything. Leaving it here in case some people want to experiment with it.
            if (living.Level < 10)
                return living.Level * 20 + 20 + living.GetBaseStat(eStat.CON);
            else
            {
                // approx to original formula, thx to mathematica :)
                int hp = (int) (50 + 11 * living.Level + 0.548331 * living.Level * living.Level) + living.GetBaseStat(eStat.CON);

                if (living.Level < 25)
                    hp += 20;

                return hp;
            }

            static int CalculateNpcMaxHealth(GameNPC npc, int hpPerLevelPlusOne, double hpPerSquaredLevel, int constitution, int constitutionOffset, double hpPerConstitution,int hpBonus)
            {
                double hpFromConstitution = (constitution - constitutionOffset) * hpPerConstitution;
                double hpFromLevel = hpPerLevelPlusOne * (npc.Level + 1) + hpPerSquaredLevel * npc.Level * npc.Level;
                return (int) ((hpFromConstitution + hpFromLevel + hpBonus) * npc.MaxHealthScalingFactor);
            }
        }

        public static int GetItemBonusCap(GameLiving living)
        {
            return living == null ? 0 : living.Level * 4;
        }

        public static int GetItemBonusCapIncrease(GameLiving living)
        {
            if (living == null)
                return 0;

            int itemBonusCapIncreaseCap = GetItemBonusCapIncreaseCap(living);
            int itemBonusCapIncrease = living.ItemBonus[eProperty.MaxHealthCapBonus];
            return Math.Min(itemBonusCapIncrease, itemBonusCapIncreaseCap);
        }

        public static int GetItemBonusCapIncreaseCap(GameLiving living)
        {
            return living == null ? 0 : living.Level * 4;
        }
    }
}
