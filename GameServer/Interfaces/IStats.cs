using System;
using System.Collections.Generic;
using DOL.GS.PropertyCalc;

namespace DOL.GS.Interfaces
{
    /// <summary>
    /// Core interface for base stat access
    /// DAoC Rule: Base stats come from race and level progression
    /// </summary>
    public interface IStats
    {
        /// <summary>
        /// Get base stat value without any modifiers
        /// </summary>
        int GetBaseStat(eStat stat);
        
        /// <summary>
        /// Set base stat value (used for character creation and leveling)
        /// </summary>
        void SetBaseStat(eStat stat, int value);
        
        /// <summary>
        /// Change base stat by delta amount
        /// </summary>
        void ChangeBaseStat(eStat stat, short delta);
        
        /// <summary>
        /// Get all base stats as array
        /// </summary>
        short[] GetBaseStats();
        
        /// <summary>
        /// Whether stats can be modified (not for NPCs usually)
        /// </summary>
        bool CanModifyStats { get; }
    }

    /// <summary>
    /// Interface for calculated property values including all modifiers
    /// DAoC Rule: Final values calculated from base + items + buffs + abilities - debuffs
    /// </summary>
    public interface IModifiedStats : IStats
    {
        /// <summary>
        /// Get final calculated property value with all modifiers
        /// </summary>
        int GetModified(eProperty property);
        
        /// <summary>
        /// Get base property value without buffs/debuffs (includes items)
        /// </summary>
        int GetModifiedBase(eProperty property);
        
        /// <summary>
        /// Get property value from buffs only
        /// </summary>
        int GetModifiedFromBuffs(eProperty property);
        
        /// <summary>
        /// Get property value from items only
        /// </summary>
        int GetModifiedFromItems(eProperty property);
        
        /// <summary>
        /// Notify system that properties have changed and need recalculation
        /// </summary>
        void PropertiesChanged();
    }

    /// <summary>
    /// Interface for property calculators following the plugin pattern
    /// DAoC Rule: Each property type has its own calculation logic
    /// </summary>
    public interface IPropertyCalculator
    {
        /// <summary>
        /// Calculate final property value with all modifiers
        /// </summary>
        int CalcValue(GameLiving living, eProperty property);
        
        /// <summary>
        /// Calculate base property value without buffs
        /// </summary>
        int CalcValueBase(GameLiving living, eProperty property);
        
        /// <summary>
        /// Calculate property value from buffs only
        /// </summary>
        int CalcValueFromBuffs(GameLiving living, eProperty property);
        
        /// <summary>
        /// Calculate property value from items only
        /// </summary>
        int CalcValueFromItems(GameLiving living, eProperty property);
    }

    /// <summary>
    /// Interface for bonus categories used in property calculations
    /// DAoC Rule: Different bonus types stack differently and have different caps
    /// </summary>
    public interface IBonusContainer
    {
        /// <summary>
        /// Base buff bonus category (single stat buffs, base AF)
        /// Stacking: Only highest applies
        /// </summary>
        IPropertyIndexer BaseBuffBonusCategory { get; }
        
        /// <summary>
        /// Specialization buff bonus category (spec AF, dual stat buffs)
        /// Stacking: Only highest applies
        /// </summary>
        IPropertyIndexer SpecBuffBonusCategory { get; }
        
        /// <summary>
        /// Debuff category (stored as positive values)
        /// Stacking: Only highest applies
        /// </summary>
        IPropertyIndexer DebuffCategory { get; }
        
        /// <summary>
        /// Specialized debuff category
        /// Stacking: Only highest applies
        /// </summary>
        IPropertyIndexer SpecDebuffCategory { get; }
        
        /// <summary>
        /// Other bonuses (uncapped, special modifiers)
        /// Stacking: Generally additive
        /// </summary>
        IPropertyIndexer OtherBonus { get; }
        
        /// <summary>
        /// Item bonuses from equipment
        /// Stacking: Additive from all items
        /// Capping: Level-based caps
        /// </summary>
        IPropertyIndexer ItemBonus { get; }
        
        /// <summary>
        /// Ability bonuses (realm abilities, master levels)
        /// Stacking: Generally stack with spell buffs
        /// </summary>
        IPropertyIndexer AbilityBonus { get; }
    }

    /// <summary>
    /// Interface for property indexer allowing array-like access
    /// </summary>
    public interface IPropertyIndexer
    {
        /// <summary>
        /// Get bonus value for a property
        /// </summary>
        int this[eProperty property] { get; set; }
        
        /// <summary>
        /// Get bonus value for a property by index
        /// </summary>
        int this[int propertyIndex] { get; set; }
        
        /// <summary>
        /// Clear all bonuses
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Copy from another indexer
        /// </summary>
        void CopyFrom(IPropertyIndexer source);
    }

    /// <summary>
    /// Interface for stat-specific calculations
    /// DAoC Rule: Stats have special rules for buffs, debuffs, and caps
    /// </summary>
    public interface IStatCalculator : IPropertyCalculator
    {
        /// <summary>
        /// Calculate effective stat including item caps
        /// DAoC Rule: Item bonuses are capped by level
        /// </summary>
        int CalculateStatWithCaps(GameLiving living, eProperty stat);
        
        /// <summary>
        /// Get item bonus cap for character level
        /// DAoC Rule: Caps increase every 5 levels starting at 15
        /// </summary>
        int GetItemBonusCapForLevel(int level);
        
        /// <summary>
        /// Apply debuff effectiveness rules
        /// DAoC Rule: Debuffs are most effective against buffs, less against base+items
        /// </summary>
        void ApplyDebuffEffectiveness(ref int baseDebuff, ref int specDebuff, 
            ref int buffBonus, ref int baseAndItemStat);
    }

    /// <summary>
    /// Interface for resistance calculations
    /// DAoC Rule: Resistances have complex stacking and capping rules
    /// </summary>
    public interface IResistanceCalculator : IPropertyCalculator
    {
        /// <summary>
        /// Calculate resistance with racial bonuses
        /// </summary>
        int CalculateResistanceWithRacial(GameLiving living, eProperty resistance);
        
        /// <summary>
        /// Get resistance cap for level
        /// DAoC Rule: Resistance caps vary by level and type
        /// </summary>
        int GetResistanceCap(int level, eProperty resistance);
        
        /// <summary>
        /// Calculate mythical resistance cap bonuses
        /// DAoC Rule: Mythical items can increase resistance caps
        /// </summary>
        int GetMythicalResistanceCapBonus(GameLiving living, eProperty resistance);
    }

    /// <summary>
    /// Interface for armor factor calculations
    /// DAoC Rule: AF is calculated differently for players vs NPCs
    /// </summary>
    public interface IArmorFactorCalculator : IPropertyCalculator
    {
        /// <summary>
        /// Calculate base armor factor from equipped items
        /// </summary>
        int CalculateBaseArmorFactor(GameLiving living);
        
        /// <summary>
        /// Calculate armor factor buffs
        /// DAoC Rule: Base AF and spec AF buffs use different categories
        /// </summary>
        int CalculateArmorFactorBuffs(GameLiving living);
        
        /// <summary>
        /// Get effective armor factor for damage calculations
        /// </summary>
        int GetEffectiveArmorFactor(GameLiving living, eDamageType damageType);
    }

    /// <summary>
    /// Interface for hit point calculations
    /// DAoC Rule: HP based on constitution and class multipliers
    /// </summary>
    public interface IHitPointCalculator : IPropertyCalculator
    {
        /// <summary>
        /// Calculate maximum hit points
        /// DAoC Rule: BaseHP + (Level-1) * HPPerLevel + Constitution Bonus
        /// </summary>
        int CalculateMaxHitPoints(GameLiving living);
        
        /// <summary>
        /// Calculate constitution bonus to hit points
        /// DAoC Rule: Con bonus varies by class and level
        /// </summary>
        int CalculateConstitutionBonus(GameLiving living);
        
        /// <summary>
        /// Apply death constitution penalty for players
        /// DAoC Rule: Players lose constitution when dying
        /// </summary>
        int ApplyDeathConstitutionPenalty(GameLiving living, int baseHitPoints);
    }

    /// <summary>
    /// Interface for power/mana calculations
    /// DAoC Rule: Power based on relevant stat (Int/Pie/Emp) and class
    /// </summary>
    public interface IPowerCalculator : IPropertyCalculator
    {
        /// <summary>
        /// Calculate maximum power/mana
        /// DAoC Rule: Based on mana stat and class multipliers
        /// </summary>
        int CalculateMaxPower(GameLiving living);
        
        /// <summary>
        /// Get mana stat for character class
        /// DAoC Rule: Different classes use different stats for mana
        /// </summary>
        eStat GetManaStatForClass(ICharacterClass characterClass);
        
        /// <summary>
        /// Calculate acuity bonus for list casters
        /// DAoC Rule: List casters get acuity bonus to mana stat
        /// </summary>
        int CalculateAcuityBonus(GameLiving living);
    }

    /// <summary>
    /// Interface for skill and specialization caps
    /// DAoC Rule: Skills and specs have level-based caps
    /// </summary>
    public interface ISkillCapCalculator
    {
        /// <summary>
        /// Get maximum skill level for character level
        /// </summary>
        int GetSkillCap(int characterLevel, string skillName);
        
        /// <summary>
        /// Get maximum specialization level for character level
        /// </summary>
        int GetSpecializationCap(int characterLevel, string specName);
        
        /// <summary>
        /// Calculate skill points available at level
        /// DAoC Rule: Skill points granted per level vary by class
        /// </summary>
        int GetSkillPointsForLevel(int level, ICharacterClass characterClass);
        
        /// <summary>
        /// Calculate specialization points cost
        /// DAoC Rule: Spec points cost increases with level
        /// </summary>
        int CalculateSpecializationCost(int currentLevel, int targetLevel);
    }

    /// <summary>
    /// Interface for mythical bonus calculations
    /// DAoC Rule: Mythical items provide special bonuses and cap increases
    /// </summary>
    public interface IMythicalBonusCalculator
    {
        /// <summary>
        /// Calculate mythical stat cap increase
        /// DAoC Rule: Mythical items can increase stat caps up to 52 combined
        /// </summary>
        int GetMythicalStatCapIncrease(GameLiving living, eProperty property);
        
        /// <summary>
        /// Calculate mythical resistance cap increase
        /// DAoC Rule: Mythical items can increase resistance caps
        /// </summary>
        int GetMythicalResistanceCapIncrease(GameLiving living, eProperty resistance);
        
        /// <summary>
        /// Get mythical utility bonuses (coin, safe fall, etc.)
        /// </summary>
        int GetMythicalUtilityBonus(GameLiving living, eProperty property);
        
        /// <summary>
        /// Validate mythical bonus caps
        /// DAoC Rule: Combined regular + mythical caps cannot exceed 52
        /// </summary>
        bool ValidateMythicalCaps(GameLiving living, eProperty property, int bonusValue);
    }

    /// <summary>
    /// Interface for combat stat calculations
    /// DAoC Rule: Combat stats like DPS, spell damage have special rules
    /// </summary>
    public interface ICombatStatCalculator : IPropertyCalculator
    {
        /// <summary>
        /// Calculate weapon DPS bonus
        /// </summary>
        int CalculateDPSBonus(GameLiving living);
        
        /// <summary>
        /// Calculate spell damage bonus
        /// </summary>
        int CalculateSpellDamageBonus(GameLiving living);
        
        /// <summary>
        /// Calculate spell level bonus
        /// DAoC Rule: Spell level bonus capped at +10 from items
        /// </summary>
        int CalculateSpellLevelBonus(GameLiving living);
        
        /// <summary>
        /// Calculate to-hit bonus
        /// </summary>
        int CalculateToHitBonus(GameLiving living);
    }

    /// <summary>
    /// Interface for speed calculations
    /// DAoC Rule: Movement and combat speeds have different modifiers
    /// </summary>
    public interface ISpeedCalculator : IPropertyCalculator
    {
        /// <summary>
        /// Calculate melee combat speed
        /// </summary>
        double CalculateMeleeSpeed(GameLiving living, double baseSpeed);
        
        /// <summary>
        /// Calculate casting speed
        /// </summary>
        double CalculateCastingSpeed(GameLiving living, double baseSpeed);
        
        /// <summary>
        /// Calculate movement speed
        /// </summary>
        int CalculateMovementSpeed(GameLiving living);
        
        /// <summary>
        /// Calculate water movement speed
        /// </summary>
        int CalculateWaterSpeed(GameLiving living);
    }

    /// <summary>
    /// Enumeration for modifier types in property calculations
    /// </summary>
    public enum eModifierType : byte
    {
        Base = 0,
        Item = 1,
        BaseBuff = 2,
        SpecBuff = 3,
        Debuff = 4,
        SpecDebuff = 5,
        Other = 6,
        Ability = 7,
        Multiplicative = 8
    }

    /// <summary>
    /// Context for property calculations including source and modifiers
    /// </summary>
    public struct PropertyCalculationContext
    {
        /// <summary>
        /// Living entity being calculated
        /// </summary>
        public GameLiving Living { get; set; }
        
        /// <summary>
        /// Property being calculated
        /// </summary>
        public eProperty Property { get; set; }
        
        /// <summary>
        /// Whether to include buffs in calculation
        /// </summary>
        public bool IncludeBuffs { get; set; }
        
        /// <summary>
        /// Whether to include items in calculation
        /// </summary>
        public bool IncludeItems { get; set; }
        
        /// <summary>
        /// Whether to apply caps
        /// </summary>
        public bool ApplyCaps { get; set; }
        
        /// <summary>
        /// Timestamp for calculation caching
        /// </summary>
        public long CalculationTime { get; set; }
    }
} 