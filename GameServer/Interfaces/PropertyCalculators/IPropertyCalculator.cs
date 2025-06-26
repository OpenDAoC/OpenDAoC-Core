using DOL.GS.Interfaces.Core;
using DOL.GS.Interfaces.Character;
using System.Collections.Generic;
using System.Linq.Expressions;
using System;

namespace DOL.GS.Interfaces.PropertyCalculators
{
    /// <summary>
    /// Registry for property calculators
    /// </summary>
    public interface IPropertyCalculatorRegistry
    {
        void Register(eProperty property, IPropertyCalculator calculator);
        IPropertyCalculator Get(eProperty property);
        int Calculate(IPropertySource source, eProperty property);
    }

    /// <summary>
    /// Interface for calculating final property values
    /// </summary>
    public interface IPropertyCalculator
    {
        eProperty TargetProperty { get; }
        int Calculate(IPropertySource source);
    }

    /// <summary>
    /// Source for property calculations (character, equipment, buffs, etc.)
    /// </summary>
    public interface IPropertySource
    {
        int GetBase(eProperty property);
        int GetItemBonus(eProperty property);
        int GetBuffBonus(eProperty property);
        int GetDebuffPenalty(eProperty property);
        IEnumerable<IPropertyModifier> GetModifiers(eProperty property);
    }

    /// <summary>
    /// Modifier that affects a property value
    /// </summary>
    public interface IPropertyModifier
    {
        eProperty Target { get; }
        int Value { get; }
        ModifierType Type { get; }
        object Source { get; }
    }

    /// <summary>
    /// Calculator for Constitution with death penalty
    /// </summary>
    public interface IConstitutionCalculator : IPropertyCalculator
    {
        int CalculateWithDeathPenalty(IPropertySource source, int deathCount);
    }

    /// <summary>
    /// Calculator for stats (Str, Dex, Con, Qui, Int, Pie, Emp, Cha)
    /// </summary>
    public interface IStatCalculator : IPropertyCalculator
    {
        int CalculateEffectiveStat(IPropertySource source, eProperty stat);
    }

    /// <summary>
    /// Calculator for armor factor
    /// </summary>
    public interface IArmorFactorCalculator : IPropertyCalculator
    {
        int CalculateArmorFactor(IPropertySource source);
    }

    /// <summary>
    /// Calculator for resistances
    /// </summary>
    public interface IResistanceCalculator : IPropertyCalculator
    {
        int CalculateResistance(IPropertySource source, eProperty resistance);
    }

    /// <summary>
    /// Calculator for attack speeds
    /// </summary>
    public interface IMeleeSpeedCalculator : IPropertyCalculator
    {
        double CalculateSpeed(IPropertySource source, double baseSpeed);
    }

    /// <summary>
    /// Calculator for casting speeds
    /// </summary>
    public interface ICastingSpeedCalculator : IPropertyCalculator
    {
        double CalculateSpeed(IPropertySource source, double baseSpeed);
    }

    /// <summary>
    /// Calculator for damage modifiers
    /// </summary>
    public interface IMeleeDamageCalculator : IPropertyCalculator
    {
        int CalculateDamageBonus(IPropertySource source);
    }

    /// <summary>
    /// Calculator for critical hit chances
    /// </summary>
    public interface ICriticalHitCalculator : IPropertyCalculator
    {
        double CalculateCriticalChance(IPropertySource source);
    }

    /// <summary>
    /// Calculator for power regeneration
    /// </summary>
    public interface IPowerRegenCalculator : IPropertyCalculator
    {
        int CalculatePowerRegen(IPropertySource source);
    }

    /// <summary>
    /// Service for property calculations
    /// </summary>
    public interface IPropertyService : IGameService
    {
        IPropertyCalculatorRegistry Calculators { get; }
        void RecalculateProperties(IPropertySource source);
        int GetModifiedValue(IPropertySource source, Property property);
    }

    // Property source implementations
    public interface ICharacterPropertySource : IPropertySource
    {
        ICharacter Character { get; }
        Dictionary<Property, int> ItemBonuses { get; }
        Dictionary<Property, int> BuffBonuses { get; }
        Dictionary<Property, List<IPropertyModifier>> Modifiers { get; }
    }

    public interface IItemPropertySource : IPropertySource
    {
        IItem Item { get; }
    }

    // Calculator implementations
    public interface IPropertyCalculatorFactory
    {
        IPropertyCalculator CreateCalculator(Property property);
        T CreateSpecificCalculator<T>() where T : IPropertyCalculator;
    }

    // Repository interfaces
    public interface IRepository<T> where T : class
    {
        T GetById(object id);
        IList<T> GetAll();
        IList<T> Find(Expression<Func<T, bool>> predicate);
        void Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        void SaveChanges();
    }

    public interface ICharacterRepository : IRepository<ICharacter>
    {
        ICharacter GetByName(string name);
        IList<ICharacter> GetByAccount(string accountId);
        IList<ICharacter> GetByGuild(string guildId);
    }

    public interface IItemRepository : IRepository<IItem>
    {
        IList<IItem> GetByOwner(string ownerId);
        IList<IItem> GetByTemplate(string templateId);
    }

    // Test helper interfaces
    public interface IModifierBuilder
    {
        IModifierBuilder WithProperty(Property property);
        IModifierBuilder WithType(ModifierType type);
        IModifierBuilder WithValue(int value);
        IModifierBuilder WithPriority(int priority);
        IModifierBuilder WithSource(string source);
        IPropertyModifier Build();
    }

    public interface IPropertySourceBuilder
    {
        IPropertySourceBuilder WithBase(Property property, int value);
        IPropertySourceBuilder WithItemBonus(Property property, int value);
        IPropertySourceBuilder WithBuffBonus(Property property, int value);
        IPropertySourceBuilder WithModifier(IPropertyModifier modifier);
        IPropertySource Build();
    }

    // NOTE: Mock classes moved to unit test files to avoid interface compilation conflicts

    /// <summary>
    /// Service for managing property calculations
    /// </summary>
    public interface IPropertyCalculationService : IGameService
    {
        int GetModifiedProperty(IPropertySource source, eProperty property);
        void RegisterCalculator(eProperty property, IPropertyCalculator calculator);
        void UnregisterCalculator(eProperty property);
        IList<IPropertyCalculator> GetAllCalculators();
    }

    /// <summary>
    /// Service for managing character modifiers and buffs
    /// </summary>
    public interface IModifierService : IGameService
    {
        void ApplyModifier(IPropertySource target, IPropertyModifier modifier);
        void RemoveModifier(IPropertySource target, IPropertyModifier modifier);
        IList<IPropertyModifier> GetActiveModifiers(IPropertySource target, eProperty property);
        void ClearAllModifiers(IPropertySource target);
    }

    /// <summary>
    /// Calculator for buff and debuff stacking
    /// </summary>
    public interface IBuffStackingCalculator
    {
        int CalculateStackedValue(IList<IPropertyModifier> modifiers, eProperty property);
        bool CanStackWith(IPropertyModifier existing, IPropertyModifier newModifier);
        IPropertyModifier GetHighestPriorityModifier(IList<IPropertyModifier> modifiers);
    }

    /// <summary>
    /// Formula calculator interface for complex calculations
    /// </summary>
    public interface IFormulaCalculator
    {
        TResult Calculate<TResult>(Expression<Func<TResult>> formula);
        int CalculateWithVariables(string formula, Dictionary<string, object> variables);
        double CalculatePercentage(int baseValue, double percentage);
    }

    /// <summary>
    /// Service for caching property calculations for performance
    /// </summary>
    public interface IPropertyCacheService : IGameService
    {
        void CacheValue(IPropertySource source, eProperty property, int value);
        bool TryGetCachedValue(IPropertySource source, eProperty property, out int value);
        void InvalidateCache(IPropertySource source);
        void InvalidateProperty(IPropertySource source, eProperty property);
    }

    /// <summary>
    /// Factory for creating property modifiers
    /// </summary>
    public interface IPropertyModifierFactory
    {
        IPropertyModifier CreateItemBonus(eProperty property, int value);
        IPropertyModifier CreateBuffEffect(eProperty property, int value, int duration);
        IPropertyModifier CreateDebuffEffect(eProperty property, int value, int duration);
        IPropertyModifier CreateStatModifier(eStat stat, int value, ModifierType type);
    }

    /// <summary>
    /// Configuration service for property calculation rules
    /// </summary>
    public interface IPropertyCalculationConfig : IGameService
    {
        int GetPropertyCap(eProperty property, int level);
        double GetResistanceCap(eDamageType damageType);
        int GetItemBonusCap(int level);
        bool IsPropertyStackable(eProperty property);
    }

    // NOTE: Mock implementations have been moved to the unit test files to avoid conflicts

    #region Enums

    public enum BuffCategory
    {
        Base,
        Spec,
        Other,
        Realm
    }

    #endregion
} 