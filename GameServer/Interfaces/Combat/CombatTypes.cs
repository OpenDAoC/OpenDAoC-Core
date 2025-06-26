using System.Collections.Generic;
using DOL.GS.Interfaces.Core;
using DOL.GS;  // Add reference to DOL.GS for AttackData

namespace DOL.GS.Interfaces.Combat
{
    /// <summary>
    /// Types of attacks
    /// </summary>
    public enum AttackType
    {
        Unknown,
        Melee,
        Ranged,
        Spell
    }

    /// <summary>
    /// Results of attack attempts
    /// </summary>
    public enum AttackResult
    {
        Any,
        Missed,
        Fumbled,
        HitUnstyled,
        HitStyle,
        Evaded,
        Blocked,
        Parried,
        NoTarget,
        NoValidTarget
    }

    /// <summary>
    /// Types of weapons
    /// </summary>
    public enum WeaponType
    {
        OneHanded,
        TwoHanded,
        Longbow,
        Crossbow,
        Staff,
        Polearm
    }

    /// <summary>
    /// Shield sizes
    /// </summary>
    public enum ShieldSize
    {
        Small,
        Medium,
        Large
    }

    /// <summary>
    /// Style positional requirements - use existing OpenDAoC Style enum
    /// </summary>
    public enum eStylePositional : byte
    {
        Any = 255,
        Back = 0,
        Side = 1,
        Front = 2
    }

    /// <summary>
    /// Style opening requirements - use existing OpenDAoC Style enum  
    /// </summary>
    public enum eStyleOpening : byte
    {
        Any = 0,
        Miss = 1,
        Hit = 2,
        Parry = 3,
        Block = 4,
        Evade = 5,
        Fumble = 6,
        Style = 7
    }

    /// <summary>
    /// Armor slots - use actual OpenDAoC eInventorySlot values for armor
    /// </summary>
    public enum eArmorSlot : byte
    {
        NOTSET = 0,
        HeadArmor = 21,
        HandsArmor = 22,
        FeetArmor = 23,
        TorsoArmor = 25,
        Cloak = 26,
        LegsArmor = 27,
        ArmsArmor = 28
    }

    /// <summary>
    /// Character abilities
    /// </summary>
    public enum Abilities
    {
        MasteryOfMagic,
        PenetratingArrow
    }

    /// <summary>
    /// Attack context information
    /// </summary>
    public class AttackContext
    {
        public int AttackerCount { get; set; } = 1;
        public bool IsPvP { get; set; }
    }

    /// <summary>
    /// Data structure containing all information about an attack
    /// Used by combat calculators to determine hit/miss and damage
    /// </summary>
    public class AttackData
    {
        /// <summary>
        /// The entity performing the attack
        /// </summary>
        public IAttacker Attacker { get; set; }
        
        /// <summary>
        /// The entity being attacked
        /// </summary>
        public IDefender Target { get; set; }
        
        /// <summary>
        /// Type of attack being performed - uses existing OpenDAoC enum
        /// </summary>
        public DOL.GS.AttackData.eAttackType Type { get; set; }
        
        /// <summary>
        /// Weapon being used for the attack
        /// </summary>
        public IWeapon Weapon { get; set; }
        
        /// <summary>
        /// Style being used (if any)
        /// </summary>
        public IStyle Style { get; set; }
        
        /// <summary>
        /// Ammunition being used for ranged attacks
        /// Contains both physical type (Arrow/Bolt) and quality modifiers
        /// </summary>
        public IAmmo Ammo { get; set; }
        
        /// <summary>
        /// Damage type of the attack - calculated from weapon and ammo
        /// </summary>
        public eDamageType DamageType => GetDamageType();
        
        /// <summary>
        /// Location where armor was hit
        /// </summary>
        public eArmorSlot ArmorHitLocation { get; set; }
        
        /// <summary>
        /// Result of the attack attempt
        /// </summary>
        public eAttackResult Result { get; set; }
        
        /// <summary>
        /// Additional context for the attack
        /// </summary>
        public AttackContext Context { get; set; }
        
        /// <summary>
        /// Get the damage type for this attack based on weapon and ammo
        /// Follows OpenDAoC ECS pattern from AttackComponent
        /// </summary>
        private eDamageType GetDamageType()
        {
            // For ranged attacks, ammo can override weapon damage type
            if (Type == DOL.GS.AttackData.eAttackType.Ranged && Ammo != null)
            {
                // Implementation would check ammo damage type if it exists
                // For now, fall back to weapon damage type
            }
            
            return Weapon?.Type_Damage ?? eDamageType.Natural;
        }
    }

    /// <summary>
    /// Combat result data structure
    /// </summary>
    public class CombatResult
    {
        public bool Hit { get; set; }
        public int Damage { get; set; }
        public bool WasCritical { get; set; }
        public eAttackResult Result { get; set; }
    }

    /// <summary>
    /// Damage calculation result
    /// </summary>
    public class DamageResult
    {
        public int BaseDamage { get; set; }
        public int ModifiedDamage { get; set; }
        public int CriticalDamage { get; set; }
        public int ResistAmount { get; set; }
        public int AbsorbAmount { get; set; }
        public int TotalDamage => ModifiedDamage + CriticalDamage - ResistAmount - AbsorbAmount;
        public bool WasCritical { get; set; }
        public List<DamageModifier> Modifiers { get; set; } = new();
    }

    /// <summary>
    /// Damage modifier class
    /// </summary>
    public class DamageModifier
    {
        public string Source { get; set; }
        public double Value { get; set; }
        public ModifierType Type { get; set; }
    }

    /// <summary>
    /// Defense result data structure
    /// </summary>
    public class DefenseResult
    {
        public bool Success { get; set; }
        public eDefenseType Type { get; set; }
    }

    /// <summary>
    /// Types of defense
    /// </summary>
    public enum eDefenseType : byte
    {
        None,
        Evade,
        Parry,
        Block
    }

    /// <summary>
    /// Represents ammunition used for ranged attacks
    /// Follows OpenDAoC pattern: eObjectType distinguishes Arrow vs Bolt, 
    /// while AmmoQuality provides hit chance modifiers
    /// </summary>
    public interface IAmmo : IItem
    {
        /// <summary>
        /// Physical ammo type (Arrow, Bolt) - uses existing OpenDAoC enum
        /// This determines weapon compatibility
        /// </summary>
        eObjectType PhysicalType { get; }
        
        /// <summary>
        /// Quality modifier affecting hit chance
        /// Rough: +15% miss chance, Standard: no modification, Footed: -25% miss chance
        /// Named AmmoQuality to avoid conflict with IItem.Quality
        /// </summary>
        eAmmoQuality AmmoQuality { get; }
        
        /// <summary>
        /// Check if this ammo is compatible with the given weapon
        /// </summary>
        bool IsCompatibleWith(IWeapon weapon);
    }

    /// <summary>
    /// Ammo quality types with different hit chance modifiers
    /// Follows OpenDAoC naming pattern with 'e' prefix
    /// </summary>
    public enum eAmmoQuality : byte
    {
        /// <summary>
        /// Standard quality ammo - no hit chance modifier
        /// </summary>
        Standard = 0,
        
        /// <summary>
        /// Rough quality ammo - +15% miss chance (worse accuracy)
        /// </summary>
        Rough = 1,
        
        /// <summary>
        /// Footed quality ammo - -25% miss chance (better accuracy)
        /// </summary>
        Footed = 2
    }

    /// <summary>
    /// Service for handling ammunition calculations and compatibility
    /// Follows interface-first design pattern for combat systems
    /// </summary>
    public interface IAmmoService
    {
        /// <summary>
        /// Calculate the miss chance modifier based on ammo quality
        /// Rough: +15%, Standard: 0%, Footed: -25%
        /// </summary>
        double GetMissChanceModifier(eAmmoQuality quality);
        
        /// <summary>
        /// Check if ammo is compatible with the given weapon
        /// Arrows work with bows, bolts work with crossbows
        /// </summary>
        bool IsAmmoCompatible(IAmmo ammo, IWeapon weapon);
        
        /// <summary>
        /// Get the best available ammo from inventory for the given weapon
        /// Prioritizes higher quality ammo if multiple types available
        /// </summary>
        IAmmo GetBestAmmoForWeapon(IInventory inventory, IWeapon weapon);
        
        /// <summary>
        /// Apply ammo wear/degradation after use
        /// Ammo can break or become unusable over time
        /// </summary>
        void ApplyAmmoWear(IAmmo ammo, int wearAmount = 1);
    }
} 