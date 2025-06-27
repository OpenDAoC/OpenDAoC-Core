using System;
using System.Collections.Generic;

namespace DOL.GS.Interfaces.Core
{
    /// <summary>
    /// Base interface for all living entities
    /// </summary>
    public interface ILiving
    {
        int Level { get; }
        string Name { get; }
    }

    /// <summary>
    /// Interface for character statistics - uses OpenDAoC eStat enum
    /// </summary>
    public interface IStats
    {
        int this[eStat stat] { get; set; }
        int GetModified(eStat stat);
        void ApplyModifier(IStatModifier modifier);
        void RemoveModifier(IStatModifier modifier);
    }

    /// <summary>
    /// Interface for stat modifiers - uses OpenDAoC eStat enum
    /// </summary>
    public interface IStatModifier
    {
        eStat Target { get; }
        int Value { get; }
        ModifierType Type { get; }
        int Priority { get; }
    }

    /// <summary>
    /// Interface for effects on living entities
    /// </summary>
    public interface IEffect
    {
        eProperty Property { get; }
        int Value { get; }
        EffectType Type { get; }
        int Duration { get; }
        bool IsExpired { get; }
    }

    /// <summary>
    /// Interface for items - uses OpenDAoC eObjectType and eInventorySlot
    /// </summary>
    public interface IItem
    {
        string Id_nb { get; } // OpenDAoC uses Id_nb for template ID
        string Name { get; }
        int Level { get; }
        int Quality { get; set; }
        int Condition { get; set; }
        int Durability { get; }
        eObjectType Object_Type { get; } // Use OpenDAoC object type
        eInventorySlot Slot { get; } // Use OpenDAoC inventory slot
        Dictionary<eProperty, int> Bonuses { get; } // Use OpenDAoC property enum
        bool IsUnique { get; }
        Dictionary<string, object> SpecialProperties { get; }
    }

    /// <summary>
    /// Interface for weapons - extends IItem
    /// </summary>
    public interface IWeapon : IItem
    {
        int DPS { get; }
        int SPD_ABS { get; } // OpenDAoC uses SPD_ABS for weapon speed
        eDamageType Type_Damage { get; } // OpenDAoC damage type
        int Range { get; }
        IList<IWeaponProc> Procs { get; }
    }

    /// <summary>
    /// Interface for armor/equipment - extends IItem  
    /// </summary>
    public interface IArmor : IItem
    {
        int Armor_Factor { get; } // OpenDAoC uses Armor_Factor
        int Absorb { get; } // OpenDAoC uses Absorb for absorption
        IEquipmentRequirements Requirements { get; }
    }

    /// <summary>
    /// Interface for shields - extends IArmor
    /// </summary>
    public interface IShield : IArmor
    {
        int ShieldSize { get; } // Use int instead of enum to avoid conflicts
        int GetMaxSimultaneousBlocks();
    }

    /// <summary>
    /// Interface for ammo items
    /// </summary>
    public interface IAmmo : IItem
    {
        int AmmoType { get; } // Use int to avoid enum conflicts
    }

    /// <summary>
    /// Interface for combat styles
    /// </summary>
    public interface IStyle
    {
        string KeyName { get; } // OpenDAoC uses KeyName for styles
        string Name { get; }
        int PositionalRequirement { get; } // Use int to avoid enum conflicts
        int OpeningRequirement { get; } // Use int to avoid enum conflicts
        int GrowthRate { get; }
        int SpecLevelRequirement { get; }
        eDamageType DamageType { get; }
    }

    /// <summary>
    /// Interface for spells
    /// </summary>
    public interface ISpell
    {
        string ID { get; }
        string Name { get; }
        int Damage { get; }
        int Level { get; }
        SpellType Type { get; }
    }

    /// <summary>
    /// Interface for spell casters
    /// </summary>
    public interface ICaster : ILiving
    {
        int GetAbilityLevel(Abilities ability);
        IStats ModifiedStats { get; }
    }

    /// <summary>
    /// Interface for modifiers
    /// </summary>
    public interface IModifier
    {
        ModifierType Type { get; }
        double Value { get; }
    }

    /// <summary>
    /// Statistics enumeration
    /// </summary>
    public enum Stat
    {
        Strength,
        Constitution,
        Dexterity,
        Quickness,
        Intelligence,
        Piety,
        Empathy,
        Charisma,
        None
    }

    /// <summary>
    /// Property enumeration for bonuses and calculations
    /// </summary>
    public enum Property
    {
        // Stats
        Strength,
        Constitution,
        Dexterity,
        Quickness,
        Intelligence,
        Piety,
        Empathy,
        Charisma,
        
        // Combat
        ArmorFactor,
        MeleeDamage,
        MeleeSpeed,
        CastingSpeed,
        CriticalMeleeHit,
        
        // Resistances
        Resist_Body,
        Resist_Cold,
        Resist_Energy,
        Resist_Heat,
        Resist_Matter,
        Resist_Spirit,
        
        // Resources
        PowerRegen,
        EnduranceRegen,
        HealthRegen,
        HitPoints
    }

    /// <summary>
    /// Modifier types
    /// </summary>
    public enum ModifierType
    {
        Additive,
        Multiplicative,
        Override
    }

    /// <summary>
    /// Effect types
    /// </summary>
    public enum EffectType
    {
        Buff,
        Debuff,
        Poison,
        Disease
    }

    /// <summary>
    /// Item types
    /// </summary>
    public enum ItemType
    {
        GenericItem,
        Weapon,
        Armor,
        Magical,
        Instrument,
        Poison,
        AlchemyTincture,
        SpellcraftGem,
        GardenObject
    }

    /// <summary>
    /// Equipment slots
    /// </summary>
    public enum EquipmentSlot
    {
        None,
        RightHand,
        LeftHand,
        TwoHand,
        Distance,
        Helm,
        Gloves,
        Boots,
        Chest,
        Legs,
        Arms,
        Cloak,
        Neck,
        Jewel,
        Belt,
        Ring,
        RingLeft,
        RingRight,
        Bracer,
        BracerLeft,
        BracerRight,
        Mythical
    }

    /// <summary>
    /// Armor slots
    /// </summary>
    public enum ArmorSlot
    {
        NOTSET,
        HEAD,
        TORSO,
        LEGS,
        ARMS,
        HAND,
        FEET
    }

    /// <summary>
    /// Damage types
    /// </summary>
    public enum DamageType
    {
        Natural,
        Crush,
        Slash,
        Thrust,
        Body,
        Cold,
        Energy,
        Heat,
        Matter,
        Spirit
    }

    /// <summary>
    /// Spell types
    /// </summary>
    public enum SpellType
    {
        DirectDamage,
        DamageOverTime,
        Heal,
        Buff,
        Debuff
    }

    /// <summary>
    /// Property source interface - uses OpenDAoC eProperty enum
    /// </summary>
    public interface IPropertySource
    {
        int GetBase(eProperty property);
        int GetItemBonus(eProperty property);
        int GetBuffBonus(eProperty property);
        int GetDebuffPenalty(eProperty property);
        IList<IPropertyModifier> GetModifiers(eProperty property);
    }

    /// <summary>
    /// Property modifier interface - uses OpenDAoC eProperty enum
    /// </summary>
    public interface IPropertyModifier
    {
        eProperty Target { get; }
        ModifierType Type { get; }
        int Value { get; }
        int Priority { get; }
    }

    /// <summary>
    /// Base interface for all game services
    /// </summary>
    public interface IGameService
    {
        void Initialize();
        void Start();
        void Stop();
        void Update(long tick);
    }

    /// <summary>
    /// Equipment requirements interface
    /// </summary>
    public interface IEquipmentRequirements
    {
        int MinLevel { get; }
        eCharacterClass RequiredClass { get; } // Use OpenDAoC character class enum
        eRealm RequiredRealm { get; } // Use OpenDAoC realm enum
        eStat RequiredStat { get; }
        int RequiredStatValue { get; }
    }

    /// <summary>
    /// Interface for inventory management - uses OpenDAoC eInventorySlot
    /// </summary>
    public interface IInventory
    {
        IItem GetItem(eInventorySlot slot);
        bool AddItem(IItem item, eInventorySlot slot);
        bool RemoveItem(eInventorySlot slot);
        bool MoveItem(eInventorySlot from, eInventorySlot to);
        IList<IItem> GetItemsInSlotRange(eInventorySlot start, eInventorySlot end);
        int GetFreeSlotCount(eInventorySlot start, eInventorySlot end);
    }

    /// <summary>
    /// Interface for abilities
    /// </summary>
    public interface IAbility
    {
        string KeyName { get; } // OpenDAoC uses KeyName for abilities
        string Name { get; }
        int Level { get; }
        int SpecLevelRequirement { get; }
    }

    /// <summary>
    /// Interface for specializations - uses existing OpenDAoC specialization system
    /// </summary>
    public interface ISpecialization
    {
        string KeyName { get; } // OpenDAoC uses string keys from Specs class
        string Name { get; }
        int Level { get; set; }
        int MaxLevel { get; }
        int SkillType { get; } // Use int to avoid enum conflicts
        IList<IAbility> GetAbilitiesAtLevel(int level);
    }

    /// <summary>
    /// Weapon proc interface
    /// </summary>
    public interface IWeaponProc
    {
        int ProcChance { get; }
        int ProcSpellID { get; }
        string Effect { get; }
    }

    // Core game object interfaces
    /// <summary>
    /// Base interface for all game objects with identification
    /// </summary>
    public interface IIdentifiable
    {
        string ObjectId { get; }
        string Name { get; }
        string InternalId { get; }
        eObjectType ObjectType { get; }
        bool IsValid { get; }
    }

    /// <summary>
    /// Interface for objects with position in the game world
    /// </summary>
    public interface IPositionable
    {
        int X { get; }
        int Y { get; }
        int Z { get; }
        ushort Heading { get; }
        ushort CurrentRegionId { get; }
        double GetDistanceTo(IPositionable other);
        bool IsWithinRadius(IPositionable other, int radius);
        void MoveTo(int x, int y, int z, ushort heading, ushort regionId);
    }

    /// <summary>
    /// Core game object interface combining identification and position
    /// </summary>
    public interface IGameObject : IIdentifiable, IPositionable
    {
    }

    // Event system interfaces for adapter support
    /// <summary>
    /// Base interface for all game events
    /// </summary>
    public interface IGameEvent
    {
        string EventType { get; }
        DateTime TimeStamp { get; }
        object Source { get; }
        object Target { get; }
        object EventArgs { get; }
    }

    /// <summary>
    /// Interface for event handlers
    /// </summary>
    public interface IEventHandler
    {
        void HandleEvent(IGameEvent gameEvent);
        bool CanHandle(Type eventType);
        int Priority { get; }
    }

    /// <summary>
    /// Interface for objects that can receive and notify events
    /// </summary>
    public interface IEventNotifier
    {
        void NotifyEvent(IGameEvent gameEvent);
        void Subscribe(Type eventType, IEventHandler handler);
        void Unsubscribe(Type eventType, IEventHandler handler);
        bool HasSubscription(Type eventType);
    }
} 