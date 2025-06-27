using System;
using System.Collections.Generic;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Interfaces
{
    /// <summary>
    /// Core interface for all living entities in the game
    /// Combines multiple focused interfaces following ISP
    /// </summary>
    public interface IGameLiving : IGameObject, IDamageable, IMovable, IStatContainer
    {
        /// <summary>
        /// Race of the living entity
        /// </summary>
        short Race { get; set; }
        
        /// <summary>
        /// Whether the living entity is currently alive
        /// </summary>
        new bool IsAlive { get; }
        
        /// <summary>
        /// Guild name for display purposes
        /// </summary>
        string GuildName { get; }
        
        /// <summary>
        /// Current effectiveness (resurrection illness)
        /// </summary>
        double Effectiveness { get; set; }
        
        /// <summary>
        /// Body type for equipment and spell targeting
        /// </summary>
        ushort BodyType { get; set; }
    }

    /// <summary>
    /// Interface for entities that can take damage
    /// DAoC Rule: Damage is reduced by armor factor and resistances
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage to this entity
        /// </summary>
        void TakeDamage(IDamageContext damage);
        
        /// <summary>
        /// Current health points
        /// </summary>
        int Health { get; set; }
        
        /// <summary>
        /// Maximum health points
        /// </summary>
        int MaxHealth { get; }
        
        /// <summary>
        /// Health as percentage (0-100)
        /// </summary>
        byte HealthPercent { get; }
        
        /// <summary>
        /// Whether entity is currently alive
        /// </summary>
        bool IsAlive { get; }
    }

    /// <summary>
    /// Interface for entities that can move and be positioned
    /// </summary>
    public interface IMovable : IPositionable
    {
        /// <summary>
        /// Current movement speed
        /// </summary>
        short CurrentSpeed { get; set; }
        
        /// <summary>
        /// Maximum movement speed
        /// </summary>
        short MaxSpeedBase { get; set; }
        
        /// <summary>
        /// Whether entity is moving
        /// </summary>
        bool IsMoving { get; }
        
        /// <summary>
        /// Whether entity is stealthed
        /// </summary>
        bool IsStealthed { get; }
        
        /// <summary>
        /// Move to a new position
        /// </summary>
        void MoveTo(int x, int y, int z, ushort heading);
    }

    /// <summary>
    /// Interface for stat management on living entities
    /// DAoC Rule: Stats are calculated from base + equipment + buffs - debuffs
    /// </summary>
    public interface IStatContainer : IStats, IModifiedStats
    {
        /// <summary>
        /// Endurance points (stamina)
        /// </summary>
        int Endurance { get; set; }
        
        /// <summary>
        /// Maximum endurance points
        /// </summary>
        int MaxEndurance { get; }
        
        /// <summary>
        /// Mana/Power points
        /// </summary>
        int Mana { get; set; }
        
        /// <summary>
        /// Maximum mana/power points
        /// </summary>
        int MaxMana { get; }
        
        /// <summary>
        /// Concentration points (for maintaining spells)
        /// </summary>
        int Concentration { get; set; }
        
        /// <summary>
        /// Maximum concentration points
        /// </summary>
        int MaxConcentration { get; }
    }

    /// <summary>
    /// Interface for magic and spell effects
    /// DAoC Rule: Buffs and debuffs affect stats and abilities
    /// </summary>
    public interface ISpellTarget
    {
        /// <summary>
        /// Check if a spell can be cast on this target
        /// </summary>
        bool IsValidTarget(ISpellCaster caster, SpellLine spellLine, Spell spell);
        
        /// <summary>
        /// Apply a spell effect to this target
        /// </summary>
        void OnSpellEffect(ISpellCaster caster, Spell spell, double effectiveness);
        
        /// <summary>
        /// Remove a spell effect from this target
        /// </summary>
        void OnSpellEffectRemoved(Spell spell);
        
        /// <summary>
        /// Check spell immunity
        /// </summary>
        bool HasImmunity(Spell spell);
    }

    /// <summary>
    /// Interface for entities that can cast spells
    /// DAoC Rule: Spell casting requires mana and is affected by interruption
    /// </summary>
    public interface ISpellCaster
    {
        /// <summary>
        /// Cast a spell at a target
        /// </summary>
        void CastSpell(Spell spell, SpellLine spellLine, ISpellTarget target);
        
        /// <summary>
        /// Check if caster can cast a specific spell
        /// </summary>
        bool CanCastSpell(Spell spell, SpellLine spellLine);
        
        /// <summary>
        /// Interrupt spell casting
        /// </summary>
        void InterruptSpellCasting();
        
        /// <summary>
        /// Whether currently casting a spell
        /// </summary>
        bool IsCasting { get; }
        
        /// <summary>
        /// Current spell being cast
        /// </summary>
        Spell CurrentSpell { get; }
    }

    /// <summary>
    /// Interface for effect list management
    /// DAoC Rule: Effects stack according to specific rules
    /// </summary>
    public interface IEffectListOwner
    {
        /// <summary>
        /// Active effects on this entity
        /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete - Legacy system still in use
        GameEffectList EffectList { get; }
#pragma warning restore CS0618
        
        /// <summary>
        /// Add an effect to this entity
        /// </summary>
        void AddEffect(IGameEffect effect);
        
        /// <summary>
        /// Remove an effect from this entity
        /// </summary>
        void RemoveEffect(IGameEffect effect);
        
        /// <summary>
        /// Check if entity has a specific effect
        /// </summary>
        bool HasEffect(Type effectType);
        
        /// <summary>
        /// Find effect by type
        /// </summary>
        T FindEffect<T>() where T : class, IGameEffect;
    }

    /// <summary>
    /// Interface for inventory management
    /// DAoC Rule: Equipment affects stats and capabilities
    /// </summary>
    public interface IInventoryOwner
    {
        /// <summary>
        /// Get equipped item in specific slot
        /// </summary>
        DbInventoryItem GetItem(eInventorySlot slot);
        
        /// <summary>
        /// Equip an item to a slot
        /// </summary>
        bool EquipItem(DbInventoryItem item, eInventorySlot slot);
        
        /// <summary>
        /// Unequip item from slot
        /// </summary>
        bool UnequipItem(eInventorySlot slot);
        
        /// <summary>
        /// Check if item can be equipped
        /// </summary>
        bool CanEquipItem(DbInventoryItem item, eInventorySlot slot);
        
        /// <summary>
        /// Get all equipped items
        /// </summary>
        IList<DbInventoryItem> GetEquippedItems();
    }

    /// <summary>
    /// Interface for communication and emotes
    /// DAoC Rule: Players can communicate and perform emotes
    /// </summary>
    public interface ICommunicator
    {
        /// <summary>
        /// Send a message to this entity
        /// </summary>
        void SendMessage(string message, byte chatType, byte chatLoc);
        
        /// <summary>
        /// Perform an emote
        /// </summary>
        void Emote(int emoteAction);
        
        /// <summary>
        /// Whether entity can receive messages
        /// </summary>
        bool CanReceiveMessages { get; }
        
        /// <summary>
        /// Current language
        /// </summary>
        string Language { get; set; }
    }

    /// <summary>
    /// Interface for grouping functionality
    /// DAoC Rule: Group mechanics provide benefits and coordination
    /// </summary>
    public interface IGroupMember
    {
        /// <summary>
        /// Current group membership
        /// </summary>
        Group Group { get; set; }
        
        /// <summary>
        /// Whether entity can join groups
        /// </summary>
        bool CanJoinGroup { get; }
        
        /// <summary>
        /// Group role or position
        /// </summary>
        string GroupRole { get; set; }
        
        /// <summary>
        /// Leave current group
        /// </summary>
        void LeaveGroup();
    }

    /// <summary>
    /// Interface for aggressive behavior and PvP
    /// DAoC Rule: Aggression and combat state management
    /// </summary>
    public interface IAggressive
    {
        /// <summary>
        /// Current aggression level
        /// </summary>
        int AggressionState { get; set; }
        
        /// <summary>
        /// List of entities that are aggressive toward this one
        /// </summary>
        IList<IGameLiving> Aggressors { get; }
        
        /// <summary>
        /// Add an aggressor
        /// </summary>
        void AddAggressor(IGameLiving aggressor);
        
        /// <summary>
        /// Remove an aggressor
        /// </summary>
        void RemoveAggressor(IGameLiving aggressor);
        
        /// <summary>
        /// Whether currently in combat
        /// </summary>
        bool InCombat { get; }
    }

    /// <summary>
    /// Interface for experience and advancement
    /// DAoC Rule: Experience drives character progression
    /// </summary>
    public interface IExperienceGainer
    {
        /// <summary>
        /// Gain experience points
        /// </summary>
        void GainExperience(GameLiving source, long experience);
        
        /// <summary>
        /// Whether entity can gain experience
        /// </summary>
        bool CanGainExperience { get; }
        
        /// <summary>
        /// Experience modifier (0.0 to 1.0+)
        /// </summary>
        double ExperienceRate { get; set; }
        
        /// <summary>
        /// Check if level up is possible
        /// </summary>
        bool CanLevelUp { get; }
    }
} 