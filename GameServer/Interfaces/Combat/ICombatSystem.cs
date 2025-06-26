using System.Collections.Generic;
using DOL.GS.Interfaces.Core;
using DOL.GS;  // Add reference to DOL.GS for AttackData

namespace DOL.GS.Interfaces.Combat
{
    /// <summary>
    /// Core combat system interface for processing attacks and damage
    /// </summary>
    public interface ICombatSystem
    {
        eAttackResult ProcessAttack(IAttacker attacker, IDefender defender, AttackContext context);
        DamageResult CalculateDamage(AttackData attackData);
        void ApplyDamage(ILiving target, DamageResult damage);
    }

    /// <summary>
    /// Interface for entities that can attack
    /// </summary>
    public interface IAttacker
    {
        int Level { get; }
        IWeapon ActiveWeapon { get; }
        ICombatStats CombatStats { get; }
        IList<IEffect> ActiveEffects { get; }
        AttackData PrepareAttack(IDefender target, DOL.GS.AttackData.eAttackType type);
    }

    /// <summary>
    /// Interface for entities that can defend
    /// </summary>
    public interface IDefender
    {
        int Level { get; }
        IArmor GetArmor(eArmorSlot slot);
        IDefenseStats DefenseStats { get; }
        DefenseResult TryDefend(AttackData attack);
        void OnAttacked(AttackData attack);
    }

    /// <summary>
    /// Interface for combat statistics
    /// </summary>
    public interface ICombatStats
    {
        int GetWeaponSkill(IWeapon weapon);
        int GetStyleDamage(IStyle style);
        double GetCriticalChance(DOL.GS.AttackData.eAttackType type);
        double GetAttackSpeed(IWeapon weapon);
        IModifier GetDamageModifier(eDamageType type);
    }

    /// <summary>
    /// Interface for defense statistics
    /// </summary>
    public interface IDefenseStats
    {
        double GetEvadeChance(int attackerCount);
        double GetParryChance(int attackerCount);
        double GetBlockChance(int attackerCount);
        int GetArmorFactor(eArmorSlot slot);
        double GetAbsorb(eArmorSlot slot);
        double GetResist(eDamageType type);
    }

    /// <summary>
    /// Interface for combat styles
    /// </summary>
    public interface IStyle
    {
        string KeyName { get; } // OpenDAoC uses KeyName for styles
        string Name { get; }
        eStylePositional PositionalRequirement { get; }
        eStyleOpening OpeningRequirement { get; }
        int GrowthRate { get; }
        int SpecLevelRequirement { get; }
        eDamageType DamageType { get; }
    }

    /// <summary>
    /// Interface for combat modifiers
    /// </summary>
    public interface IModifier
    {
        double Value { get; }
        ModifierType Type { get; }
    }

    /// <summary>
    /// Interface for miss chance calculations
    /// </summary>
    public interface IMissChanceCalculator
    {
        double CalculateBaseMissChance(AttackData attackData);
        double CalculateMissChance(AttackData attackData);
    }

    /// <summary>
    /// Interface for damage calculations
    /// </summary>
    public interface IDamageCalculator
    {
        double CalculateBaseDamage(IWeapon weapon);
        double CalculateWeaponSkill(AttackData attackData);
        double CalculateDamageMod(double weaponSkill, int armorFactor);
        int CalculateCriticalDamage(AttackData attackData);
    }

    /// <summary>
    /// Interface for defense calculations
    /// </summary>
    public interface IDefenseCalculator
    {
        double CalculateEvadeChance(IDefender defender, int evadeAbilityLevel, int attackerCount);
        double CalculateParryChance(IDefender defender, int parrySpec, int masteryOfParry, int attackerCount, IWeapon attackerWeapon = null);
        double CalculateBlockChance(IDefender defender, IShield shield, int shieldSpec);
    }

    /// <summary>
    /// Interface for style validation
    /// </summary>
    public interface IStyleValidator
    {
        bool CanUseStyle(AttackData attackData);
    }

    /// <summary>
    /// Interface for style calculations
    /// </summary>
    public interface IStyleCalculator
    {
        double CalculateEnduranceCost(IStyle style, IWeapon weapon);
        int CalculateStyleDamage(IStyle style, int baseDamage);
    }

    /// <summary>
    /// Interface for spell damage calculations
    /// </summary>
    public interface ISpellDamageCalculator
    {
        int CalculateBaseDamage(ICaster caster, ISpell spell);
        double CalculateHitChance(ICaster caster, IDefender target, ISpell spell);
        (double min, double max) CalculateDamageVariance(ICaster caster, int masteryLevel);
    }
} 