using System;
using DOL.Database;

namespace DOL.GS.Interfaces
{
    /// <summary>
    /// Attack type enumeration for combat
    /// </summary>
    public enum AttackType : byte
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Spell = 3,
        DoT = 4
    }

    /// <summary>
    /// Interface for entities that can be attacked
    /// DAoC Rule: Attack validation includes range, line of sight, and realm rules
    /// </summary>
    public interface IAttackable
    {
        /// <summary>
        /// Whether this entity can currently be attacked
        /// </summary>
        bool CanBeAttacked();
        
        /// <summary>
        /// Handle being attacked by an attacker
        /// </summary>
        void OnAttacked(IAttackContext context);
        
        /// <summary>
        /// Get the effective level for attack calculations
        /// </summary>
        int EffectiveLevel { get; }
        
        /// <summary>
        /// Whether the entity is currently visible (not stealthed)
        /// </summary>
        bool IsVisible { get; }
        
        /// <summary>
        /// Current realm for PvP attack validation
        /// </summary>
        eRealm Realm { get; }
    }

    /// <summary>
    /// Interface for entities that can perform attacks
    /// DAoC Rule: Attack capabilities depend on weapon, stats, and abilities
    /// </summary>
    public interface IAttacker
    {
        /// <summary>
        /// Check if this attacker can attack the specified target
        /// </summary>
        bool CanAttack(IAttackable target);
        
        /// <summary>
        /// Prepare an attack against the target
        /// </summary>
        IAttackContext PrepareAttack(IAttackable target);
        
        /// <summary>
        /// Get the attack range for current weapon
        /// </summary>
        int AttackRange { get; }
        
        /// <summary>
        /// Get weapon skill for attack calculations
        /// </summary>
        double GetWeaponSkill(DbInventoryItem weapon);
        
        /// <summary>
        /// Get effective attack speed in milliseconds
        /// </summary>
        int AttackSpeed { get; }
    }

    /// <summary>
    /// Interface for defensive capabilities
    /// DAoC Rule: Defense includes evade, parry, block with specific calculations
    /// </summary>
    public interface IDefender
    {
        /// <summary>
        /// Attempt to evade an incoming attack
        /// </summary>
        IDefenseResult TryEvade(IAttackContext attack);
        
        /// <summary>
        /// Attempt to parry an incoming attack
        /// </summary>
        IDefenseResult TryParry(IAttackContext attack);
        
        /// <summary>
        /// Attempt to block an incoming attack
        /// </summary>
        IDefenseResult TryBlock(IAttackContext attack);
        
        /// <summary>
        /// Get defensive capabilities summary
        /// </summary>
        IDefenseCapabilities DefenseCapabilities { get; }
    }

    /// <summary>
    /// Interface for armor and damage reduction
    /// DAoC Rule: Armor provides both AF (damage reduction) and ABS (damage absorption)
    /// </summary>
    public interface IArmorable
    {
        /// <summary>
        /// Get armor factor for a specific slot
        /// </summary>
        double GetArmorAF(eArmorSlot slot);
        
        /// <summary>
        /// Get armor absorption for a specific slot
        /// </summary>
        double GetArmorAbsorb(eArmorSlot slot);
        
        /// <summary>
        /// Get overall effective armor factor
        /// </summary>
        int EffectiveOverallAF { get; }
        
        /// <summary>
        /// Get resistance to a specific damage type
        /// </summary>
        int GetResist(eDamageType damageType);
    }

    /// <summary>
    /// Interface for critical hit capabilities
    /// DAoC Rule: Critical hits use weapon skill vs. target level
    /// </summary>
    public interface ICriticalCapable
    {
        /// <summary>
        /// Calculate critical hit chance for melee
        /// </summary>
        double CalculateCriticalChance(DbInventoryItem weapon, IAttackable target);
        
        /// <summary>
        /// Calculate critical hit chance for spells
        /// </summary>
        int SpellCriticalChance { get; }
        
        /// <summary>
        /// Calculate critical hit damage multiplier
        /// </summary>
        double CalculateCriticalDamage(DbInventoryItem weapon);
    }

    /// <summary>
    /// Interface for weapon specialization
    /// DAoC Rule: Weapon spec affects damage, speed, and critical hits
    /// </summary>
    public interface IWeaponSpecialist
    {
        /// <summary>
        /// Get specialization level for weapon type
        /// </summary>
        int WeaponSpecLevel(eObjectType weaponType, int slotPosition);
        
        /// <summary>
        /// Get specialization level for specific weapon
        /// </summary>
        int WeaponSpecLevel(DbInventoryItem weapon);
        
        /// <summary>
        /// Get weapon damage including spec bonuses
        /// </summary>
        double WeaponDamage(DbInventoryItem weapon);
        
        /// <summary>
        /// Get weapon stat (STR/DEX) for calculations
        /// </summary>
        int GetWeaponStat(DbInventoryItem weapon);
    }

    /// <summary>
    /// Attack context containing all attack information
    /// </summary>
    public interface IAttackContext
    {
        /// <summary>
        /// Entity performing the attack
        /// </summary>
        IAttacker Attacker { get; }
        
        /// <summary>
        /// Target of the attack
        /// </summary>
        IAttackable Target { get; }
        
        /// <summary>
        /// Weapon being used for attack
        /// </summary>
        DbInventoryItem Weapon { get; }
        
        /// <summary>
        /// Type of attack being performed
        /// </summary>
        AttackType AttackType { get; }
        
        /// <summary>
        /// Weapon slot being used
        /// </summary>
        eActiveWeaponSlot WeaponSlot { get; }
        
        /// <summary>
        /// Attack timestamp for speed calculations
        /// </summary>
        long AttackTime { get; }
    }

    /// <summary>
    /// Result of a defensive action
    /// </summary>
    public interface IDefenseResult
    {
        /// <summary>
        /// Type of defense that was attempted
        /// </summary>
        eDefenseType DefenseType { get; }
        
        /// <summary>
        /// Whether the defense was successful
        /// </summary>
        bool Success { get; }
        
        /// <summary>
        /// Damage reduction from this defense (0-1.0)
        /// </summary>
        double DamageReduction { get; }
        
        /// <summary>
        /// Animation to display for this defense
        /// </summary>
        eDefenseAnimation Animation { get; }
    }

    /// <summary>
    /// Summary of defensive capabilities
    /// </summary>
    public interface IDefenseCapabilities
    {
        /// <summary>
        /// Evade chance (0-100)
        /// </summary>
        double EvadeChance { get; }
        
        /// <summary>
        /// Parry chance (0-100)
        /// </summary>
        double ParryChance { get; }
        
        /// <summary>
        /// Block chance (0-100)  
        /// </summary>
        double BlockChance { get; }
        
        /// <summary>
        /// Whether entity can parry with current weapon
        /// </summary>
        bool CanParry { get; }
        
        /// <summary>
        /// Whether entity has a shield equipped for blocking
        /// </summary>
        bool CanBlock { get; }
    }

    /// <summary>
    /// Damage context for damage calculations
    /// </summary>
    public interface IDamageContext
    {
        /// <summary>
        /// Source of the damage
        /// </summary>
        GameObject Source { get; }
        
        /// <summary>
        /// Type of damage (slash, thrust, crush, etc.)
        /// </summary>
        eDamageType DamageType { get; }
        
        /// <summary>
        /// Base damage amount before modifiers
        /// </summary>
        int BaseDamage { get; }
        
        /// <summary>
        /// Critical damage amount
        /// </summary>
        int CriticalDamage { get; }
        
        /// <summary>
        /// Whether this was a critical hit
        /// </summary>
        bool IsCritical { get; }
        
        /// <summary>
        /// Weapon or spell that caused the damage
        /// </summary>
        object DamageSource { get; }
    }

    /// <summary>
    /// Damage result after all calculations
    /// </summary>
    public interface IDamageResult
    {
        /// <summary>
        /// Final damage amount after all reductions
        /// </summary>
        int FinalDamage { get; }
        
        /// <summary>
        /// Damage absorbed by armor
        /// </summary>
        int AbsorbedDamage { get; }
        
        /// <summary>
        /// Damage resisted
        /// </summary>
        int ResistedDamage { get; }
        
        /// <summary>
        /// Whether target was killed by this damage
        /// </summary>
        bool IsKilling { get; }
        
        /// <summary>
        /// Damage modifier applied (0-1.0+)
        /// </summary>
        double DamageModifier { get; }
    }

    /// <summary>
    /// Defense type enumeration
    /// </summary>
    public enum eDefenseType : byte
    {
        None = 0,
        Evade = 1,
        Parry = 2,
        Block = 3,
        MagicResist = 4
    }

    /// <summary>
    /// Defense animation enumeration
    /// </summary>
    public enum eDefenseAnimation : byte
    {
        None = 0,
        Evade = 1,
        Parry = 2,
        Block = 3,
        MagicResist = 4
    }
} 