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
            return CalcValueInternal(living, property, false);
        }

        public override int CalcValueBase(GameLiving living, eProperty property)
        {
            return CalcValueInternal(living, property, true);
        }

        private int CalcValueInternal(GameLiving living, eProperty property, bool forCrowdControlDuration)
        {
            int propertyIndex = (int) property;

            // Necromancer pets receive resistances from Avoidance of Magic and other active RAs.
            GameLiving livingToCheck;

            if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
                livingToCheck = playerOwner;
            else
                livingToCheck = living;

            int itemBonus = CalcValueFromItems(living, property);
            int abilityBonus = livingToCheck.AbilityBonus[propertyIndex]; // Applied as secondary resists.

            // Ability bonuses are taken into account for crowd control duration unless they're put in `OtherBonus`.
            if (!forCrowdControlDuration)
                abilityBonus += livingToCheck.OtherBonus[propertyIndex];

            int racialBonus = SkillBase.GetRaceResist(living.Race, (eResist) property);
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
                    abilityBonus += livingToCheck.AbilityBonus[eProperty.MagicAbsorption];

                    if (!forCrowdControlDuration)
                        abilityBonus += livingToCheck.OtherBonus[eProperty.MagicAbsorption];

                    break;
                }
            }

            int result = itemBonus + buffBonus; // Primary resists.
            result += (int) ((1 - result * 0.01) * abilityBonus); // Secondary resists.

            // Treat NPC resists from constitution buffs as another layer of resists for now.
            if (living is GameNPC)
            {
                double resistanceFromConstitution = StatCalculator.CalculateBuffContributionToAbsorbOrResist(living, eProperty.Constitution) / 8 * 100;
                result += (int) ((1 - result * 0.01) * resistanceFromConstitution);
            }

            // http://www.postcount.net/forum/showthread.php?192979-Primary-Secondary-and-Tertiary-resist-graphs-plus-racial-resist-oddities
            result += racialBonus;
            return Math.Min(result, HardCap);
        }

        /// <summary>
        /// Calculate modified resists from buffs and debuffs.
        /// </summary>
        public override int CalcValueFromBuffs(GameLiving living, eProperty property)
        {
            int propertyIndex = (int) property;

            GameLiving livingToCheck;

            if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
                livingToCheck = playerOwner;
            else
                livingToCheck = living;

            int buff = living.BaseBuffBonusCategory[propertyIndex] + living.SpecBuffBonusCategory[propertyIndex];
            buff = livingToCheck is GameNPC ? buff : Math.Min(buff, BuffBonusCap);
            int debuff = Math.Abs(living.DebuffCategory[property]) + Math.Abs(living.SpecDebuffCategory[property]);

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
                    buff += living.BaseBuffBonusCategory[eProperty.MagicAbsorption] + living.SpecBuffBonusCategory[eProperty.MagicAbsorption];
                    debuff += Math.Abs(living.DebuffCategory[eProperty.MagicAbsorption]);
                    break;
                }
            }

            buff -= Math.Abs(debuff);

            if (buff < 0 && living is GamePlayer)
                buff /= 2;

            return buff;
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
