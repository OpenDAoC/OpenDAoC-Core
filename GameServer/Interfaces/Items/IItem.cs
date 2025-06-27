using System;
using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS.Interfaces.Items
{
    /// <summary>
    /// Core item interface - basic item identification and type
    /// DAoC Rule: All items have templates, names, levels, and types
    /// Following ISP - Maximum 5 methods per interface
    /// </summary>
    public interface IItem
    {
        /// <summary>
        /// Unique template identifier (Id_nb in OpenDAoC)
        /// </summary>
        string TemplateId { get; }
        
        /// <summary>
        /// Display name of the item
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Item level (affects requirements and bonuses)
        /// </summary>
        int Level { get; }
        
        /// <summary>
        /// Item object type (determines weapon/armor class)
        /// </summary>
        eObjectType ObjectType { get; }
        
        /// <summary>
        /// Item type classification (weapon, armor, etc.)
        /// </summary>
        int ItemType { get; }
    }

    /// <summary>
    /// Item physical properties and attributes
    /// DAoC Rule: Items have physical characteristics affecting gameplay
    /// </summary>
    public interface IItemProperties
    {
        /// <summary>
        /// Item quality (85-100%, affects effectiveness)
        /// DAoC Rule: Quality multiplies DPS/AF values
        /// </summary>
        int Quality { get; set; }
        
        /// <summary>
        /// Item weight (affects encumbrance)
        /// </summary>
        int Weight { get; }
        
        /// <summary>
        /// Visual model ID for rendering
        /// </summary>
        int Model { get; }
        
        /// <summary>
        /// Item color for customization
        /// </summary>
        int Color { get; set; }
        
        /// <summary>
        /// Visual effect applied to item
        /// </summary>
        int Effect { get; }
    }

    /// <summary>
    /// Item condition and durability management
    /// DAoC Rule: Items degrade with use and can be repaired
    /// </summary>
    public interface IItemCondition
    {
        /// <summary>
        /// Current condition (affects effectiveness)
        /// </summary>
        int Condition { get; set; }
        
        /// <summary>
        /// Maximum condition when pristine
        /// </summary>
        int MaxCondition { get; }
        
        /// <summary>
        /// Current durability points
        /// </summary>
        int Durability { get; set; }
        
        /// <summary>
        /// Maximum durability points
        /// </summary>
        int MaxDurability { get; }
        
        /// <summary>
        /// Calculate condition as percentage (0-100)
        /// </summary>
        int ConditionPercent { get; }
    }

    /// <summary>
    /// Item magical bonuses and enhancements
    /// DAoC Rule: Items provide stat bonuses with level-based caps
    /// </summary>
    public interface IItemBonuses
    {
        /// <summary>
        /// Get bonus value for specific property
        /// </summary>
        int GetBonus(eProperty property);
        
        /// <summary>
        /// Set bonus value for specific property
        /// </summary>
        void SetBonus(eProperty property, int value);
        
        /// <summary>
        /// Get all bonuses as dictionary
        /// </summary>
        Dictionary<eProperty, int> GetAllBonuses();
        
        /// <summary>
        /// Item bonus level (affects stat caps)
        /// DAoC Rule: Higher bonus level items provide higher caps
        /// </summary>
        int BonusLevel { get; }
        
        /// <summary>
        /// Extra bonus value (special property)
        /// </summary>
        int ExtraBonus { get; }
    }

    /// <summary>
    /// Item requirements and restrictions
    /// DAoC Rule: Items have level, class, and realm requirements
    /// </summary>
    public interface IItemRequirements
    {
        /// <summary>
        /// Minimum level required to use item
        /// </summary>
        int LevelRequirement { get; }
        
        /// <summary>
        /// Allowed character classes (comma-separated)
        /// </summary>
        string AllowedClasses { get; }
        
        /// <summary>
        /// Required realm affiliation
        /// </summary>
        int Realm { get; }
        
        /// <summary>
        /// Check if character can use this item
        /// </summary>
        bool CanUse(GameLiving character);
        
        /// <summary>
        /// Item flags for special restrictions
        /// </summary>
        int Flags { get; }
    }

    /// <summary>
    /// Item trading and value properties
    /// DAoC Rule: Items have trade restrictions and monetary value
    /// </summary>
    public interface IItemTrading
    {
        /// <summary>
        /// Can item be dropped when killed
        /// </summary>
        bool IsDropable { get; set; }
        
        /// <summary>
        /// Can item be picked up from ground
        /// </summary>
        bool IsPickable { get; set; }
        
        /// <summary>
        /// Can item be traded between players
        /// </summary>
        bool IsTradable { get; set; }
        
        /// <summary>
        /// Base price value in copper
        /// </summary>
        long Price { get; }
        
        /// <summary>
        /// Can item drop as loot from creatures
        /// </summary>
        bool CanDropAsLoot { get; set; }
    }

    /// <summary>
    /// Stackable item properties
    /// DAoC Rule: Some items can stack in inventory slots
    /// </summary>
    public interface IStackableItem
    {
        /// <summary>
        /// Current stack count
        /// </summary>
        int Count { get; set; }
        
        /// <summary>
        /// Maximum items in one stack
        /// </summary>
        int MaxCount { get; }
        
        /// <summary>
        /// Package size for vendors
        /// </summary>
        int PackSize { get; }
        
        /// <summary>
        /// Check if item can stack with another
        /// </summary>
        bool CanStackWith(IItem other);
        
        /// <summary>
        /// Whether this item type is stackable
        /// </summary>
        bool IsStackable { get; }
    }

    /// <summary>
    /// Weapon-specific properties and capabilities
    /// DAoC Rule: Weapons have DPS, speed, and damage types
    /// </summary>
    public interface IWeapon : IItem
    {
        /// <summary>
        /// Damage per second rating (base weapon damage)
        /// DAoC Rule: DPS determines weapon damage potential
        /// </summary>
        int DPS { get; }
        
        /// <summary>
        /// Weapon speed in tenths of seconds (SPD_ABS)
        /// DAoC Rule: Lower speed = faster attacks
        /// </summary>
        int Speed { get; }
        
        /// <summary>
        /// Type of damage dealt (slash, thrust, crush, etc.)
        /// </summary>
        eDamageType DamageType { get; }
        
        /// <summary>
        /// Hand requirement (1H, 2H, left hand, etc.)
        /// </summary>
        int Hand { get; }
        
        /// <summary>
        /// Weapon range for reach calculations
        /// </summary>
        int Range { get; }
    }

    /// <summary>
    /// Armor-specific properties and protection
    /// DAoC Rule: Armor provides AF (damage reduction) and ABS (absorption)
    /// </summary>
    public interface IArmor : IItem
    {
        /// <summary>
        /// Armor factor - primary damage reduction
        /// DAoC Rule: Higher AF reduces incoming damage more
        /// </summary>
        int ArmorFactor { get; }
        
        /// <summary>
        /// Absorption value - chance to negate damage
        /// DAoC Rule: ABS provides chance to fully absorb hits
        /// </summary>
        int Absorption { get; }
        
        /// <summary>
        /// Armor class (cloth, leather, chain, etc.)
        /// </summary>
        int ArmorLevel { get; }
        
        /// <summary>
        /// Body location this armor protects
        /// </summary>
        eInventorySlot ProtectedSlot { get; }
        
        /// <summary>
        /// Calculate effective armor factor with condition
        /// </summary>
        int GetEffectiveAF();
    }

    /// <summary>
    /// Consumable item properties and effects
    /// DAoC Rule: Consumables have limited uses and provide temporary effects
    /// </summary>
    public interface IConsumable : IItem
    {
        /// <summary>
        /// Current number of charges/uses remaining
        /// </summary>
        int Charges { get; set; }
        
        /// <summary>
        /// Maximum charges when full
        /// </summary>
        int MaxCharges { get; }
        
        /// <summary>
        /// Spell effect triggered on use
        /// </summary>
        int SpellID { get; }
        
        /// <summary>
        /// Use the item and consume one charge
        /// </summary>
        bool Use(GameLiving user);
        
        /// <summary>
        /// Whether item is consumed completely when used
        /// </summary>
        bool IsConsumable { get; }
    }

    /// <summary>
    /// Magical item with proc effects
    /// DAoC Rule: Magic items can trigger spells on specific events
    /// </summary>
    public interface IMagicalItem : IItem
    {
        /// <summary>
        /// Spell effect that can proc
        /// </summary>
        int ProcSpellID { get; }
        
        /// <summary>
        /// Chance for proc to trigger (0-100)
        /// </summary>
        int ProcChance { get; }
        
        /// <summary>
        /// Secondary spell effect
        /// </summary>
        int SpellID1 { get; }
        
        /// <summary>
        /// Charges for secondary spell
        /// </summary>
        int Charges1 { get; set; }
        
        /// <summary>
        /// Maximum charges for secondary spell
        /// </summary>
        int MaxCharges1 { get; }
    }

    /// <summary>
    /// Poisonable weapon properties
    /// DAoC Rule: Weapons can be coated with poison for additional damage
    /// </summary>
    public interface IPoisonable : IWeapon
    {
        /// <summary>
        /// Poison spell applied to weapon
        /// </summary>
        int PoisonSpellID { get; set; }
        
        /// <summary>
        /// Current poison charges
        /// </summary>
        int PoisonCharges { get; set; }
        
        /// <summary>
        /// Maximum poison charges
        /// </summary>
        int PoisonMaxCharges { get; set; }
        
        /// <summary>
        /// Apply poison to this weapon
        /// </summary>
        bool ApplyPoison(int poisonSpellID, int charges);
        
        /// <summary>
        /// Whether weapon currently has poison
        /// </summary>
        bool IsPoisoned { get; }
    }

    /// <summary>
    /// Artifact item with leveling system
    /// DAoC Rule: Artifacts gain experience and unlock new abilities
    /// </summary>
    public interface IArtifact : IItem
    {
        /// <summary>
        /// Current artifact level (0-10)
        /// </summary>
        int ArtifactLevel { get; set; }
        
        /// <summary>
        /// Experience points toward next level
        /// </summary>
        long ArtifactExperience { get; set; }
        
        /// <summary>
        /// Get experience required for next level
        /// </summary>
        long GetExperienceForNextLevel();
        
        /// <summary>
        /// Get bonuses available at current level
        /// </summary>
        Dictionary<eProperty, int> GetArtifactBonuses();
        
        /// <summary>
        /// Whether artifact can gain more levels
        /// </summary>
        bool CanLevelUp { get; }
    }

    /// <summary>
    /// Unique item with special properties
    /// DAoC Rule: Unique items have special restrictions and enhanced properties
    /// </summary>
    public interface IUniqueItem : IItem
    {
        /// <summary>
        /// Unique item identifier
        /// </summary>
        string UniqueID { get; }
        
        /// <summary>
        /// Whether item is truly unique (only one can exist)
        /// </summary>
        bool IsUnique { get; }
        
        /// <summary>
        /// Special properties dictionary
        /// </summary>
        Dictionary<string, object> SpecialProperties { get; }
        
        /// <summary>
        /// Package or set this unique belongs to
        /// </summary>
        string PackageID { get; }
        
        /// <summary>
        /// Custom description for unique features
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// Comprehensive item interface combining all aspects
    /// Used for dependency injection and complete item representation
    /// </summary>
    public interface IGameItem : IItem, IItemProperties, IItemCondition, IItemBonuses, 
        IItemRequirements, IItemTrading, IStackableItem
    {
        /// <summary>
        /// Inventory slot position
        /// </summary>
        int SlotPosition { get; set; }
        
        /// <summary>
        /// Owner identifier
        /// </summary>
        string OwnerID { get; set; }
        
        /// <summary>
        /// Whether item data has been modified
        /// </summary>
        bool IsDirty { get; set; }
        
        /// <summary>
        /// Item's current location template
        /// </summary>
        DbItemTemplate Template { get; }
        
        /// <summary>
        /// Create a copy of this item
        /// </summary>
        IGameItem Clone();
    }

    /// <summary>
    /// Item creation and validation result
    /// </summary>
    public class ItemCreationResult
    {
        public bool Success { get; set; }
        public IGameItem Item { get; set; }
        public string ErrorMessage { get; set; }
        public ItemCreationError ErrorType { get; set; }

        public static ItemCreationResult Successful(IGameItem item) 
            => new() { Success = true, Item = item };
            
        public static ItemCreationResult Failed(string message, ItemCreationError type = ItemCreationError.General)
            => new() { Success = false, ErrorMessage = message, ErrorType = type };
    }

    /// <summary>
    /// Types of item creation errors
    /// </summary>
    public enum ItemCreationError
    {
        General,
        TemplateNotFound,
        InvalidLevel,
        InvalidQuality,
        InvalidObjectType,
        MissingRequirements
    }

    /// <summary>
    /// Item type classifications for OpenDAoC
    /// </summary>
    public enum eItemType
    {
        GenericItem = 0,
        GenericArmor = 1,
        GenericWeapon = 2,
        Magical = 3,
        Instrument = 4,
        Poison = 5,
        AlchemyTincture = 6,
        SpellcraftGem = 7,
        GardenObject = 8
    }
} 