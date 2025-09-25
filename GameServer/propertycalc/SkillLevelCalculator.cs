using System;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The Skill Level calculator
    /// 
    /// BuffBonusCategory1 is used for buffs, uncapped
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.Skill_First, eProperty.Skill_Last)]
    public class SkillLevelCalculator : PropertyCalculator
    {
        public SkillLevelCalculator() {}

        public override int CalcValue(GameLiving living, eProperty property)
        {
            if (living is not GamePlayer player)
                return 0;

            int itemBonus = CalcValueFromItems(living, property);
            int buffBonus = player.BaseBuffBonusCategory[property];
            int realmBonus = player.RealmLevel / 10;
            return itemBonus + buffBonus + realmBonus;
        }

        public override int CalcValueFromItems(GameLiving living, eProperty property)
        {
            if (living is not GamePlayer player)
                return 0;

            int itemCap = player.Level / 5 + 1;
            int itemBonus = player.ItemBonus[property];

            if (SkillBase.CheckPropertyType(property, ePropertyType.SkillMeleeWeapon))
                itemBonus += player.ItemBonus[eProperty.AllMeleeWeaponSkills];

            if (SkillBase.CheckPropertyType(property, ePropertyType.SkillMagical))
                itemBonus += player.ItemBonus[eProperty.AllMagicSkills];

            if (SkillBase.CheckPropertyType(property, ePropertyType.SkillDualWield))
                itemBonus += player.ItemBonus[eProperty.AllDualWieldingSkills];

            if (SkillBase.CheckPropertyType(property, ePropertyType.SkillArchery))
                itemBonus += player.ItemBonus[eProperty.AllArcherySkills];

            itemBonus += player.ItemBonus[eProperty.AllSkills];
            return Math.Min(itemBonus, itemCap);
        }
    }
}
