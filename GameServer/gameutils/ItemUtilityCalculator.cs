using DOL.Database;

namespace DOL.GS
{
    public static class ItemUtilityCalculator
    {
        public static double GetTotalUtility(GameInventoryItem item)
        {
            // Passing GameInventoryItem.Template to the overload isn't safe.
            // While GameInventoryItem normally delegates to DbItemTemplate, this may not always be the case in the future.
            double totalUtility = 0;
            totalUtility += GetSingleUtility((eProperty) item.Bonus1Type, item.Bonus1);
            totalUtility += GetSingleUtility((eProperty) item.Bonus2Type, item.Bonus2);
            totalUtility += GetSingleUtility((eProperty) item.Bonus3Type, item.Bonus3);
            totalUtility += GetSingleUtility((eProperty) item.Bonus4Type, item.Bonus4);
            totalUtility += GetSingleUtility((eProperty) item.Bonus5Type, item.Bonus5);
            totalUtility += GetSingleUtility((eProperty) item.Bonus6Type, item.Bonus6);
            totalUtility += GetSingleUtility((eProperty) item.Bonus7Type, item.Bonus7);
            totalUtility += GetSingleUtility((eProperty) item.Bonus8Type, item.Bonus8);
            totalUtility += GetSingleUtility((eProperty) item.Bonus9Type, item.Bonus9);
            totalUtility += GetSingleUtility((eProperty) item.Bonus10Type, item.Bonus10);
            totalUtility += GetSingleUtility((eProperty) item.ExtraBonusType, item.ExtraBonus);
            return totalUtility;
        }

        public static double GetTotalUtility(DbItemTemplate item)
        {
            double totalUtility = 0;
            totalUtility += GetSingleUtility((eProperty) item.Bonus1Type, item.Bonus1);
            totalUtility += GetSingleUtility((eProperty) item.Bonus2Type, item.Bonus2);
            totalUtility += GetSingleUtility((eProperty) item.Bonus3Type, item.Bonus3);
            totalUtility += GetSingleUtility((eProperty) item.Bonus4Type, item.Bonus4);
            totalUtility += GetSingleUtility((eProperty) item.Bonus5Type, item.Bonus5);
            totalUtility += GetSingleUtility((eProperty) item.Bonus6Type, item.Bonus6);
            totalUtility += GetSingleUtility((eProperty) item.Bonus7Type, item.Bonus7);
            totalUtility += GetSingleUtility((eProperty) item.Bonus8Type, item.Bonus8);
            totalUtility += GetSingleUtility((eProperty) item.Bonus9Type, item.Bonus9);
            totalUtility += GetSingleUtility((eProperty) item.Bonus10Type, item.Bonus10);
            totalUtility += GetSingleUtility((eProperty) item.ExtraBonusType, item.ExtraBonus);
            return totalUtility;
        }

        public static double GetSingleUtility(int bonusType, int bonus)
        {
            return GetSingleUtility((eProperty) bonusType, bonus);
        }

        public static double GetSingleUtility(eProperty bonusType, int bonus)
        {
            if (bonusType is eProperty.Undefined || bonus == 0)
                return 0;

            if (bonusType is (>= eProperty.Stat_First and <= eProperty.Stat_Last) or eProperty.Acuity)
                return bonus * (2 / 3.0);

            if (bonusType is >= eProperty.Resist_First and <= eProperty.Resist_Last)
                return bonus * 2.0;

            if (bonusType is >= eProperty.Skill_First and <= eProperty.Skill_Last)
                return bonus * 5.0;

            return bonusType switch
            {
                eProperty.MaxMana => bonus * 2.0,
                eProperty.MaxHealth => bonus * 0.25,
                eProperty.AllMagicSkills or
                eProperty.AllMeleeWeaponSkills or
                eProperty.AllDualWieldingSkills or
                eProperty.AllArcherySkills or
                eProperty.AllSkills => bonus * 5.0,
                _ => 0,
            };
        }
    }
}
