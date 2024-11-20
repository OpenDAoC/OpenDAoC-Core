using System;

namespace DOL.GS.PropertyCalc
{
    [PropertyCalculator(eProperty.Resist_Natural)]
    [PropertyCalculator(eProperty.Resist_First, eProperty.Resist_Last)]
    public class ResistCalculator : PropertyCalculator
    {
        public ResistCalculator() { }

        public override int CalcValue(GameLiving living, eProperty property)
        {
            int propertyIndex = (int) property;

            // Necromancer pets receive resistances from Avoidance of Magic.
            GameLiving livingToCheck;
            if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
                livingToCheck = playerOwner;
            else
                livingToCheck = living;

            // Abilities/racials/debuffs.
            int debuff = Math.Abs(living.DebuffCategory[propertyIndex]) + Math.Abs(living.SpecDebuffCategory[propertyIndex]);
            int abilityBonus = livingToCheck.AbilityBonus[propertyIndex];
            int racialBonus = SkillBase.GetRaceResist(living.Race, (eResist)property);

            // Items and buffs.
            int itemBonus = CalcValueFromItems(living, property);
            int buffBonus = CalcValueFromBuffs(living, property);

            switch (property)
            {
                case eProperty.Resist_Body:
                case eProperty.Resist_Cold:
                case eProperty.Resist_Energy:
                case eProperty.Resist_Heat:
                case eProperty.Resist_Matter:
                case eProperty.Resist_Spirit:
                case eProperty.Resist_Natural:
                {
                    debuff += Math.Abs(living.DebuffCategory[eProperty.MagicAbsorption]);
                    abilityBonus += living.AbilityBonus[eProperty.MagicAbsorption];
                    buffBonus += living.BaseBuffBonusCategory[eProperty.MagicAbsorption];
                    break;
                }
            }

            buffBonus -= Math.Abs(debuff);

            if (buffBonus < 0 && living is GamePlayer)
            {
                itemBonus += buffBonus / 2;
                buffBonus = 0;
            }

            int result = itemBonus + buffBonus + abilityBonus + racialBonus;

            if (living is GameNPC)
            {
                double resistanceFromConstitution = StatCalculator.CalculateBuffContributionToAbsorbOrResist(living, eProperty.Constitution) / 8;
                result += (int) ((100 - result) * resistanceFromConstitution);
            }

            // Add up and apply hardcap.
            return Math.Min(result, HardCap);
        }

        public override int CalcValueBase(GameLiving living, eProperty property)
        {
            int propertyIndex = (int)property;
            int debuff = Math.Abs(living.DebuffCategory[propertyIndex]) + Math.Abs(living.SpecDebuffCategory[propertyIndex]);
            int racialBonus = (living is GamePlayer) ? SkillBase.GetRaceResist((living as GamePlayer).Race, (eResist)property) : 0;
            int itemBonus = CalcValueFromItems(living, property);
            int buffBonus = CalcValueFromBuffs(living, property);

            switch (property)
            {
                case eProperty.Resist_Body:
                case eProperty.Resist_Cold:
                case eProperty.Resist_Energy:
                case eProperty.Resist_Heat:
                case eProperty.Resist_Matter:
                case eProperty.Resist_Spirit:
                case eProperty.Resist_Natural:
                {
                    debuff += Math.Abs(living.DebuffCategory[eProperty.MagicAbsorption]);
                    buffBonus += living.BaseBuffBonusCategory[eProperty.MagicAbsorption];
                    break;
                }
            }

            buffBonus -= Math.Abs(debuff);

            if (living is GamePlayer && buffBonus < 0)
            {
                itemBonus += buffBonus / 2;
                buffBonus = 0;

                if (itemBonus < 0)
                    itemBonus = 0;
            }

            return Math.Min(itemBonus + buffBonus + racialBonus, HardCap);
        }

        /// <summary>
        /// Calculate modified resists from buffs only.
        /// </summary>
        public override int CalcValueFromBuffs(GameLiving living, eProperty property)
        {
            // Necromancer pets receive resistances from The Empty Mind.
            GameLiving livingToCheck;

            if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
                livingToCheck = playerOwner;
            else
                livingToCheck = living;

            int buffBonus = living.BaseBuffBonusCategory[(int)property] + living.BuffBonusCategory4[(int)property];
            int RACalcHolder = livingToCheck is GameNPC ? buffBonus : Math.Min(buffBonus, BuffBonusCap);

            return RACalcHolder + livingToCheck.SpecBuffBonusCategory[(int)property];
        }

        /// <summary>
        /// Calculate modified resists from items only.
        /// </summary>
        public override int CalcValueFromItems(GameLiving living, eProperty property)
        {
            // Necromancer pets receive resistances from their owner's items.
            GameLiving livingToCheck;

            if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
                livingToCheck = playerOwner;
            else
                livingToCheck = living;

            if (livingToCheck is GameNPC)
                return 0;

            int itemBonus = livingToCheck.ItemBonus[(int)property];

            // Item bonus cap and cap increase from Mythirians.
            int itemBonusCap = livingToCheck.Level / 2 + 1;
            int itemBonusCapIncrease = GetItemBonusCapIncrease(livingToCheck, property);

            return Math.Min(itemBonus, itemBonusCap + itemBonusCapIncrease);
        }

        /// <summary>
        /// Returns the resist cap increase for the given living and the given
        /// resist type. It is hardcapped at 5% for the time being.
        /// </summary>
        public static int GetItemBonusCapIncrease(GameLiving living, eProperty property)
        {
            if (living == null)
                return 0;

            return Math.Min(living.ItemBonus[(int) (eProperty.ResCapBonus_First - eProperty.Resist_First + property)], 5);
        }

        /// <summary>
        /// Cap for player cast resist buffs.
        /// </summary>
        public static int BuffBonusCap => 24;

        /// <summary>
        /// Hard cap for resists.
        /// </summary>
        public static int HardCap => 70;
    }
}
