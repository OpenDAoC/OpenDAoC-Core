using System;

namespace DOL.GS.PropertyCalc
{
    [PropertyCalculator(EProperty.Stat_First, EProperty.Stat_Last)]
    public class StatCalculator : PropertyCalculator
    {
        public const double SPEC_DEBUFF_VS_BUFF_MODIFIER = 0.5;
        public const double BASE_DEBUFF_VS_BUFF_MODIFIER = 1;
        public const double DEBUFF_VS_BASE_AND_ITEM_MODIFIER = 2;

        public StatCalculator() { }

        // Special cases:
        // 1) ManaStat (base stat + acuity, players only).
        // 2) As of patch 1.64: - Acuity - This bonus will increase your casting stat, 
        //    whatever your casting stat happens to be. If you're a druid, you should get an increase to empathy, 
        //    while a bard should get an increase to charisma.  http://support.darkageofcamelot.com/kb/article.php?id=540
        // 3) Constitution lost at death, only affects players.

        // DebuffCategory has 100% effectiveness against buffs, 50% effectiveness against item and base stats.
        // SpecDebuffs (includes Champion's only) have 200% effectiveness against buffs.
        public override int CalcValue(GameLiving living, EProperty property)
        {
            int propertyIndex = (int) property;
            int abilityBonus = 0;
            int deathConDebuff = 0;
            GameLiving livingToCheck; // Used to get item and ability bonuses from the owner of a Necromancer pet.

            if (living is GamePlayer player)
            {
                if (property == (EProperty) player.PlayerClass.ManaStat)
                {
                    if (IsClassAffectedByAcuityAbility(player.PlayerClass))
                        abilityBonus += player.AbilityBonus[(int)EProperty.Acuity];
                }

                deathConDebuff = player.TotalConstitutionLostAtDeath;
                livingToCheck = player;
            }
            else if (living is NecromancerPet necromancerPet)
                livingToCheck = necromancerPet.Owner ?? living;
            else
                livingToCheck = living;

            int baseStat = living.GetBaseStat((EStat) property);
            int itemBonus = CalcValueFromItems(livingToCheck, property);
            int buffBonus = CalcValueFromBuffs(living, property);
            int baseDebuff = Math.Abs(living.DebuffCategory[propertyIndex]);
            int specDebuff = Math.Abs(living.SpecDebuffCategory[propertyIndex]);
            abilityBonus += livingToCheck.AbilityBonus[propertyIndex];
            int baseAndItemStat = baseStat + itemBonus;
            ApplyDebuffs(ref baseDebuff, ref specDebuff, ref buffBonus, ref baseAndItemStat);
            int stat = baseAndItemStat + buffBonus + abilityBonus;
            stat = (int) (stat * living.BuffBonusMultCategory1.Get((int) property));
            stat -= (property == EProperty.Constitution) ? deathConDebuff : 0;
            return Math.Max(1, stat);
        }

        public override int CalcValueFromBuffs(GameLiving living, EProperty property)
        {
            if (living == null)
                return 0;

            int propertyIndex = (int) property;
            int baseBuffBonus = living.BaseBuffBonusCategory[propertyIndex];
            int specBuffBonus = living.SpecBuffBonusCategory[propertyIndex];

            if (living is GamePlayer player)
            {
                if (property == (EProperty) player.PlayerClass.ManaStat)
                {
                    if (player.PlayerClass.ClassType == EPlayerClassType.ListCaster)
                        specBuffBonus += player.BaseBuffBonusCategory[(int)EProperty.Acuity];
                }
            }

            // Caps and cap increases. Only players actually have a buff bonus cap, pets don't.
            int baseBuffBonusCap = (living is GamePlayer) ? (int)(living.Level * 1.25) : short.MaxValue;
            int specBuffBonusCap = (living is GamePlayer) ? (int)(living.Level * 1.5 * 1.25) : short.MaxValue;

            baseBuffBonus = Math.Min(baseBuffBonus, baseBuffBonusCap);
            specBuffBonus = Math.Min(specBuffBonus, specBuffBonusCap);
            return baseBuffBonus + specBuffBonus;
        }

        public override int CalcValueFromItems(GameLiving living, EProperty property)
        {
            if (living == null)
                return 0;

            int itemBonus = living.ItemBonus[(int) property];
            int itemBonusCap = GetItemBonusCap(living);

            if (living is GamePlayer player)
            {
                if (property == (EProperty) player.PlayerClass.ManaStat)
                {
                    if (IsClassAffectedByAcuityAbility(player.PlayerClass))
                        itemBonus += living.ItemBonus[(int)EProperty.Acuity];
                }
            }

            int itemBonusCapIncrease = GetItemBonusCapIncrease(living, property);
            int mythicalItemBonusCapIncrease = GetMythicalItemBonusCapIncrease(living, property);
            return Math.Min(itemBonus, itemBonusCap + itemBonusCapIncrease + mythicalItemBonusCapIncrease);
        }

        public static int GetItemBonusCap(GameLiving living)
        {
            return living == null ? 0 : (int) (living.Level * 1.5);
        }

        public static int GetItemBonusCapIncrease(GameLiving living, EProperty property)
        {
            if (living == null)
                return 0;

            int itemBonusCapIncreaseCap = GetItemBonusCapIncreaseCap(living);
            int itemBonusCapIncrease = living.ItemBonus[(int)(EProperty.StatCapBonus_First - EProperty.Stat_First + property)];

            if (living is GamePlayer player)
            {
                if (property == (EProperty) player.PlayerClass.ManaStat)
                {
                    if (IsClassAffectedByAcuityAbility(player.PlayerClass))
                        itemBonusCapIncrease += living.ItemBonus[(int)EProperty.AcuCapBonus];
                }
            }

            return Math.Min(itemBonusCapIncrease, itemBonusCapIncreaseCap);
        }

        public static int GetMythicalItemBonusCapIncrease(GameLiving living, EProperty property)
        {
            if (living == null)
                return 0;

            int mythicalItemBonusCapIncreaseCap = GetMythicalItemBonusCapIncreaseCap(living);
            int mythicalItemBonusCapIncrease = living.ItemBonus[(int) (EProperty.MythicalStatCapBonus_First - EProperty.Stat_First + property)];
            int itemBonusCapIncrease = GetItemBonusCapIncrease(living, property);

            if (living is GamePlayer player)
            {
                if (property == (EProperty) player.PlayerClass.ManaStat)
                {
                    if (IsClassAffectedByAcuityAbility(player.PlayerClass))
                        mythicalItemBonusCapIncrease += living.ItemBonus[(int) EProperty.MythicalAcuCapBonus];
                }
            }

            if (mythicalItemBonusCapIncrease + itemBonusCapIncrease > 52)
                mythicalItemBonusCapIncrease = 52 - itemBonusCapIncrease;

            return Math.Min(mythicalItemBonusCapIncrease, mythicalItemBonusCapIncreaseCap);
        }

        public static int GetItemBonusCapIncreaseCap(GameLiving living)
        {
            return living == null ? 0 : living.Level / 2 + 1;
        }

        public static int GetMythicalItemBonusCapIncreaseCap(GameLiving living)
        {
            return living == null ? 0 : 52;
        }

        public static bool IsClassAffectedByAcuityAbility(IPlayerClass playerClass)
        {
            return (EPlayerClass) playerClass.ID is
                not EPlayerClass.Scout and
                not EPlayerClass.Hunter and
                not EPlayerClass.Ranger and
                not EPlayerClass.Nightshade;
        }

        public static void ApplyDebuffs(ref int baseDebuff, ref int specDebuff, ref int buffBonus, ref int baseAndItemStat)
        {
            if (specDebuff > 0 && buffBonus > 0)
                ApplyDebuff(ref specDebuff, ref buffBonus, SPEC_DEBUFF_VS_BUFF_MODIFIER);

            if (specDebuff > 0 && baseAndItemStat > 0)
                ApplyDebuff(ref specDebuff, ref baseAndItemStat, DEBUFF_VS_BASE_AND_ITEM_MODIFIER);

            if (baseDebuff > 0 && buffBonus > 0)
                ApplyDebuff(ref baseDebuff, ref buffBonus, BASE_DEBUFF_VS_BUFF_MODIFIER);

            if (baseDebuff > 0 && baseAndItemStat > 0)
                ApplyDebuff(ref baseDebuff, ref baseAndItemStat, DEBUFF_VS_BASE_AND_ITEM_MODIFIER);

            static void ApplyDebuff(ref int debuff, ref int stat, double modifier)
            {
                double remainingDebuff = debuff - stat * modifier;

                if (remainingDebuff > 0)
                {
                    debuff = (int) remainingDebuff;
                    stat = 0;
                }
                else
                {
                    stat -= (int) (debuff / modifier);
                    debuff = 0;
                }
            }
        }

        // Intended to be used by NPCs to calculate ABS or resist bonus / malus from the difference between currently applied buffs and debuffs.
        public static double CalculateBuffContributionToAbsorbOrResist(GameLiving living, EProperty stat)
        {
            int buff = living.BaseBuffBonusCategory[stat] + living.SpecBuffBonusCategory[stat];
            int baseDebuff = Math.Abs(living.DebuffCategory[stat]);
            int specDebuff =  Math.Abs(living.SpecDebuffCategory[stat]);
            int baseAndItemStat = 0;
            ApplyDebuffs(ref baseDebuff, ref specDebuff, ref buff, ref baseAndItemStat);
            double debuffContribution = (baseDebuff + specDebuff) / DEBUFF_VS_BASE_AND_ITEM_MODIFIER;
            return (buff - debuffContribution) * 0.01;
        }
    }
}
