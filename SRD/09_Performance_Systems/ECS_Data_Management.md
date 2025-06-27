# ECS Data Management System

**Document Status:** Complete Data Flow Analysis  
**Verification:** Code-verified from property systems and data management  
**Implementation Status:** Live Production

## Overview

**Game Rule Summary**: The data management system calculates your character's final stats by combining your base abilities, equipment bonuses, spell effects, and other modifiers. It ensures that when you equip new gear or receive buffs, all your stats update correctly and consistently throughout the game without conflicting with each other.

OpenDAoC's ECS Data Management system handles the complex data flow between components, property calculations, and game state management. This document covers the sophisticated data management patterns, property calculation systems, and data persistence mechanisms that ensure consistent game state across all systems.

## Property Management Architecture

### Property System Foundation

#### Property Calculator Registry
```csharp
public static class PropertyCalculatorRegistry
{
    private static readonly Dictionary<Property, IPropertyCalculator> _calculators = new();
    
    // Game rule: Each property has a dedicated calculator for consistency
    static PropertyCalculatorRegistry()
    {
        RegisterCalculator(Property.Strength, new StatCalculator(Property.Strength));
        RegisterCalculator(Property.ArmorFactor, new ArmorFactorCalculator());
        RegisterCalculator(Property.ArmorAbsorption, new ArmorAbsorptionCalculator());
        RegisterCalculator(Property.MeleeSpeed, new MeleeSpeedCalculator());
        RegisterCalculator(Property.CastingSpeed, new CastingSpeedCalculator());
        RegisterCalculator(Property.Resist_Body, new ResistanceCalculator(Property.Resist_Body));
        // ... register all properties
    }
    
    public static void RegisterCalculator(Property property, IPropertyCalculator calculator)
    {
        _calculators[property] = calculator;
    }
    
    public static int Calculate(IPropertySource source, Property property)
    {
        if (_calculators.TryGetValue(property, out var calculator))
        {
            return calculator.Calculate(source, property);
        }
        
        log.Warn($"No calculator registered for property {property}");
        return 0;
    }
}
```

#### Property Source Interface
```csharp
public interface IPropertySource
{
    int GetBase(Property property);
    int GetItemBonus(Property property);
    int GetBuffBonus(Property property);
    int GetDebuffPenalty(Property property);
    IList<IPropertyModifier> GetModifiers(Property property);
}

// Game rule: GameObjects implement property source for ECS integration
public abstract class GameObject : IPropertySource
{
    protected PropertyCollection _properties = new();
    
    public virtual int GetBase(Property property)
    {
        switch (property)
        {
            case Property.Strength when this is GameLiving living:
                return living.GetBaseStat(Stat.Strength);
            case Property.MaxHealth when this is GameLiving living:
                return living.CharacterClass.BaseHP + (living.Level - 1) * living.CharacterClass.HPPerLevel;
            default:
                return 0;
        }
    }
    
    public virtual int GetItemBonus(Property property)
    {
        if (this is GamePlayer player)
        {
            return player.Inventory.GetTotalBonus(property);
        }
        return 0;
    }
    
    public virtual int GetBuffBonus(Property property)
    {
        return effectListComponent?.GetBonusForProperty(property) ?? 0;
    }
    
    public virtual int GetDebuffPenalty(Property property)
    {
        return effectListComponent?.GetPenaltyForProperty(property) ?? 0;
    }
}
```

### Property Calculation System

#### Stat Calculator Implementation
```csharp
public class StatCalculator : IPropertyCalculator
{
    private readonly Property _targetProperty;
    
    public StatCalculator(Property targetProperty)
    {
        _targetProperty = targetProperty;
    }
    
    // Game rule: Stats calculated from multiple sources with caps
    public int Calculate(IPropertySource source, Property property)
    {
        if (property != _targetProperty)
            return 0;
        
        int baseValue = source.GetBase(property);
        int itemBonus = source.GetItemBonus(property);
        int buffBonus = source.GetBuffBonus(property);
        int debuffPenalty = source.GetDebuffPenalty(property);
        
        // Apply caps based on character level
        int level = (source as GameLiving)?.Level ?? 50;
        int itemCap = GetItemCapForLevel(level);
        int buffCap = GetBuffCapForLevel(level);
        
        itemBonus = Math.Min(itemBonus, itemCap);
        buffBonus = Math.Min(buffBonus, buffCap);
        
        return Math.Max(0, baseValue + itemBonus + buffBonus - debuffPenalty);
    }
    
    private int GetItemCapForLevel(int level)
    {
        // Game rule: Item bonus caps scale with level
        if (level < 15) return 0;
        if (level < 20) return 5;
        if (level < 25) return 10;
        if (level < 30) return 15;
        if (level < 35) return 20;
        if (level < 40) return 25;
        if (level < 45) return 30;
        return 35;
    }
}
```

#### Armor Factor Calculator
```csharp
public class ArmorFactorCalculator : IPropertyCalculator
{
    // Game rule: Armor factor calculation varies by target type
    public int Calculate(IPropertySource source, Property property)
    {
        if (property != Property.ArmorFactor)
            return 0;
        
        var living = source as GameLiving;
        if (living == null) return 0;
        
        if (living is GamePlayer player)
        {
            return CalculatePlayerArmorFactor(player);
        }
        else if (living is GameNPC npc)
        {
            return CalculateNPCArmorFactor(npc);
        }
        else if (living is GameKeepComponent keepComponent)
        {
            return CalculateKeepArmorFactor(keepComponent);
        }
        
        return 0;
    }
    
    private int CalculatePlayerArmorFactor(GamePlayer player)
    {
        // Base armor from equipment
        int itemAF = player.GetItemBonus(Property.ArmorFactor);
        
        // Buff bonuses (different categories with different caps)
        int baseBuffBonus = player.GetBuffBonus(Property.ArmorFactor);
        int specBuffBonus = player.GetSpecBuffBonus(Property.ArmorFactor);
        
        // Apply caps
        int itemCap = player.Level * 2; // Item AF cap
        int specBuffCap = (int)(player.Level * 1.875); // Spec buff cap
        
        itemAF = Math.Min(itemAF, itemCap);
        specBuffBonus = Math.Min(specBuffBonus, specBuffCap);
        
        // Debuff penalties
        int debuffPenalty = Math.Abs(player.GetDebuffPenalty(Property.ArmorFactor));
        
        return Math.Max(0, itemAF + baseBuffBonus + specBuffBonus - debuffPenalty);
    }
    
    private int CalculateNPCArmorFactor(GameNPC npc)
    {
        // NPCs use level-based formula
        int level = npc.Level;
        int baseAF = (int)((1 + level / 50.0) * (level * 2.5));
        
        int buffBonus = npc.GetBuffBonus(Property.ArmorFactor);
        int debuffPenalty = Math.Abs(npc.GetDebuffPenalty(Property.ArmorFactor));
        
        return Math.Max(0, baseAF + buffBonus - debuffPenalty);
    }
}
```

## Data Flow Patterns

### Component Data Synchronization

#### Property Change Notifications
```csharp
public abstract class GameObject
{
    private readonly Dictionary<Property, int> _cachedProperties = new();
    private bool _propertiesNeedRecalculation = true;
    
    // Game rule: Property changes trigger recalculation cascade
    public void NotifyPropertyChanged()
    {
        _propertiesNeedRecalculation = true;
        
        // Trigger immediate recalculation for critical properties
        RecalculateCriticalProperties();
        
        // Queue full recalculation for next tick
        QueuePropertyRecalculation();
    }
    
    private void RecalculateCriticalProperties()
    {
        // Critical properties that affect ongoing actions
        var criticalProperties = new[]
        {
            Property.MaxHealth,
            Property.ArmorFactor,
            Property.MeleeSpeed,
            Property.CastingSpeed
        };
        
        foreach (var property in criticalProperties)
        {
            var newValue = PropertyCalculatorRegistry.Calculate(this, property);
            var oldValue = _cachedProperties.GetValueOrDefault(property, 0);
            
            if (newValue != oldValue)
            {
                _cachedProperties[property] = newValue;
                OnPropertyChanged(property, oldValue, newValue);
            }
        }
    }
    
    protected virtual void OnPropertyChanged(Property property, int oldValue, int newValue)
    {
        switch (property)
        {
            case Property.MaxHealth:
                OnMaxHealthChanged(oldValue, newValue);
                break;
            case Property.MeleeSpeed:
                OnMeleeSpeedChanged(oldValue, newValue);
                break;
            case Property.CastingSpeed:
                OnCastingSpeedChanged(oldValue, newValue);
                break;
        }
    }
    
    private void OnMaxHealthChanged(int oldMax, int newMax)
    {
        // Adjust current health proportionally
        if (this is GameLiving living && oldMax > 0)
        {
            double healthPercentage = (double)living.Health / oldMax;
            living.Health = (int)(newMax * healthPercentage);
        }
    }
}
```

### Effect Data Management

#### Effect Property Integration
```csharp
public class EffectListComponent : IServiceObject
{
    private readonly Dictionary<Property, List<GameSpellEffect>> _effectsByProperty = new();
    private readonly Dictionary<Property, int> _cachedBonuses = new();
    private bool _bonusesNeedRecalculation = true;
    
    // Game rule: Effects modify properties through bonuses
    public int GetBonusForProperty(Property property)
    {
        if (_bonusesNeedRecalculation)
        {
            RecalculateAllBonuses();
        }
        
        return _cachedBonuses.GetValueOrDefault(property, 0);
    }
    
    private void RecalculateAllBonuses()
    {
        _cachedBonuses.Clear();
        
        foreach (var effect in _effects)
        {
            if (effect.IsActive())
            {
                foreach (var bonus in effect.GetPropertyBonuses())
                {
                    if (!_cachedBonuses.ContainsKey(bonus.Property))
                        _cachedBonuses[bonus.Property] = 0;
                    
                    _cachedBonuses[bonus.Property] += bonus.Value;
                }
            }
        }
        
        _bonusesNeedRecalculation = false;
    }
    
    // Game rule: Adding/removing effects triggers property updates
    public void AddEffect(GameSpellEffect effect)
    {
        _effects.Add(effect);
        _bonusesNeedRecalculation = true;
        
        // Notify owner of property changes
        owner.NotifyPropertyChanged();
        
        // Register for ECS processing
        ServiceObjectStore.Add(this);
    }
    
    public void RemoveEffect(GameSpellEffect effect)
    {
        if (_effects.Remove(effect))
        {
            _bonusesNeedRecalculation = true;
            owner.NotifyPropertyChanged();
        }
    }
}
```

## Data Persistence Patterns

### Component State Persistence

#### Save State Management
```csharp
public interface IPersistableComponent
{
    ComponentSaveData SaveState();
    void LoadState(ComponentSaveData data);
}

public class AttackComponent : IServiceObject, IPersistableComponent
{
    // Game rule: Combat state persists across server restarts
    public ComponentSaveData SaveState()
    {
        return new ComponentSaveData
        {
            ComponentType = typeof(AttackComponent).Name,
            Data = new Dictionary<string, object>
            {
                {"TargetObjectID", _startAttackTarget?.ObjectID ?? 0},
                {"BlockRoundCount", BlockRoundCount},
                {"StartAttackRequested", StartAttackRequested},
                {"NextAttackerCheck", _nextCheckForValidAttackers}
            }
        };
    }
    
    public void LoadState(ComponentSaveData data)
    {
        if (data.Data.TryGetValue("TargetObjectID", out var targetID) && (ushort)targetID != 0)
        {
            _startAttackTarget = EntityManager.GetEntity((ushort)targetID);
        }
        
        if (data.Data.TryGetValue("BlockRoundCount", out var blockCount))
        {
            BlockRoundCount = (int)blockCount;
        }
        
        if (data.Data.TryGetValue("StartAttackRequested", out var attackRequested))
        {
            StartAttackRequested = (bool)attackRequested;
        }
        
        if (data.Data.TryGetValue("NextAttackerCheck", out var nextCheck))
        {
            _nextCheckForValidAttackers = (long)nextCheck;
        }
    }
}
```

### Database Integration

#### Entity Data Mapping
```csharp
public class EntityDataMapper
{
    // Game rule: Entity data maps to database for persistence
    public static CharacterEntity MapToDatabase(GamePlayer player)
    {
        var entity = new CharacterEntity
        {
            Name = player.Name,
            Level = player.Level,
            Experience = player.Experience,
            Health = player.Health,
            Mana = player.Mana,
            X = player.X,
            Y = player.Y,
            Z = player.Z,
            Heading = player.Heading,
            Region = player.CurrentRegion.ID
        };
        
        // Save component states
        entity.ComponentStates = SaveComponentStates(player);
        
        // Save properties
        entity.PropertyValues = SavePropertyValues(player);
        
        return entity;
    }
    
    private static List<ComponentSaveData> SaveComponentStates(GameObject obj)
    {
        var componentStates = new List<ComponentSaveData>();
        
        if (obj.attackComponent is IPersistableComponent persistableAttack)
        {
            componentStates.Add(persistableAttack.SaveState());
        }
        
        if (obj.effectListComponent is IPersistableComponent persistableEffects)
        {
            componentStates.Add(persistableEffects.SaveState());
        }
        
        return componentStates;
    }
}
```

## Data Validation and Integrity

### Property Validation

#### Data Consistency Checks
```csharp
public static class DataValidator
{
    // Game rule: Data validation prevents corruption
    public static ValidationResult ValidatePlayerData(GamePlayer player)
    {
        var result = new ValidationResult();
        
        // Validate stats are within reasonable bounds
        foreach (Stat stat in Enum.GetValues<Stat>())
        {
            var value = player.GetModifiedProperty(GetPropertyForStat(stat));
            if (value < 0 || value > 999)
            {
                result.AddError($"Stat {stat} value {value} is out of bounds");
            }
        }
        
        // Validate health/mana don't exceed maximums
        if (player.Health > player.MaxHealth)
        {
            result.AddError($"Health {player.Health} exceeds maximum {player.MaxHealth}");
            player.Health = player.MaxHealth; // Auto-fix
        }
        
        if (player.Mana > player.MaxMana)
        {
            result.AddError($"Mana {player.Mana} exceeds maximum {player.MaxMana}");
            player.Mana = player.MaxMana; // Auto-fix
        }
        
        // Validate component consistency
        ValidateComponentConsistency(player, result);
        
        return result;
    }
    
    private static void ValidateComponentConsistency(GameObject obj, ValidationResult result)
    {
        // Attack and casting components shouldn't coexist
        if (obj.attackComponent != null && obj.castingComponent != null)
        {
            result.AddWarning($"Object {obj.Name} has both attack and casting components");
        }
        
        // Components should have valid owners
        if (obj.attackComponent?.owner != obj)
        {
            result.AddError($"Attack component owner mismatch for {obj.Name}");
        }
    }
}
```

### Data Recovery

#### Corruption Recovery System
```csharp
public static class DataRecovery
{
    // Game rule: System attempts to recover from data corruption
    public static void AttemptDataRecovery(GameObject corruptedObject)
    {
        log.Warn($"Attempting data recovery for {corruptedObject.Name}");
        
        try
        {
            // Remove invalid components
            CleanupInvalidComponents(corruptedObject);
            
            // Reset corrupted properties
            ResetCorruptedProperties(corruptedObject);
            
            // Reinitialize based on templates
            ReinitializeFromTemplate(corruptedObject);
            
            log.Info($"Data recovery successful for {corruptedObject.Name}");
        }
        catch (Exception e)
        {
            log.Error($"Data recovery failed for {corruptedObject.Name}: {e}");
            
            // Last resort: remove from world
            corruptedObject.RemoveFromWorld();
        }
    }
    
    private static void CleanupInvalidComponents(GameObject obj)
    {
        // Remove components with invalid state
        if (obj.attackComponent?.owner != obj)
        {
            ServiceObjectStore.Remove(obj.attackComponent);
            obj.attackComponent = null;
        }
        
        if (obj.castingComponent?.owner != obj)
        {
            ServiceObjectStore.Remove(obj.castingComponent);
            obj.castingComponent = null;
        }
    }
    
    private static void ResetCorruptedProperties(GameObject obj)
    {
        // Reset properties to safe defaults
        if (obj is GameLiving living)
        {
            if (living.Health < 0 || living.Health > living.MaxHealth)
            {
                living.Health = Math.Max(1, living.MaxHealth);
            }
            
            if (living.Mana < 0 || living.Mana > living.MaxMana)
            {
                living.Mana = living.MaxMana;
            }
        }
    }
}
```

## Performance Optimization

### Data Caching Strategies

#### Property Caching System
```csharp
public class PropertyCache
{
    private readonly Dictionary<Property, CachedValue> _cache = new();
    private readonly object _cacheLock = new object();
    
    // Game rule: Cache frequently accessed properties for performance
    public int GetCachedProperty(IPropertySource source, Property property)
    {
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(property, out var cached))
            {
                if (GameLoop.GameLoopTime - cached.LastUpdated < CACHE_VALIDITY_TIME)
                {
                    return cached.Value;
                }
            }
            
            // Recalculate and cache
            var newValue = PropertyCalculatorRegistry.Calculate(source, property);
            _cache[property] = new CachedValue
            {
                Value = newValue,
                LastUpdated = GameLoop.GameLoopTime
            };
            
            return newValue;
        }
    }
    
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cache.Clear();
        }
    }
}
```

### Batch Data Operations

#### Bulk Property Updates
```csharp
public static class BulkDataOperations
{
    // Game rule: Batch operations for performance
    public static void UpdateGroupProperties(IEnumerable<GamePlayer> players)
    {
        var propertiesToUpdate = new[]
        {
            Property.MaxHealth,
            Property.MaxMana,
            Property.ArmorFactor,
            Property.MeleeSpeed
        };
        
        // Process in parallel for performance
        Parallel.ForEach(players, player =>
        {
            var changedProperties = new List<Property>();
            
            foreach (var property in propertiesToUpdate)
            {
                var oldValue = player.GetCachedProperty(property);
                var newValue = PropertyCalculatorRegistry.Calculate(player, property);
                
                if (oldValue != newValue)
                {
                    player.SetCachedProperty(property, newValue);
                    changedProperties.Add(property);
                }
            }
            
            // Notify client of changes
            if (changedProperties.Count > 0)
            {
                player.Client?.SendPropertyUpdate(changedProperties);
            }
        });
    }
}
```

## Conclusion

OpenDAoC's ECS Data Management system provides sophisticated data flow coordination, property calculation, and persistence mechanisms. The layered approach to property calculation, robust validation systems, and performance optimizations ensure consistent game state while maintaining high performance across all ECS systems.

## Change Log

- **v1.0** (2025-01-20): Complete data management documentation
  - Property management architecture and calculators
  - Data flow patterns and synchronization
  - Persistence and database integration
  - Data validation and recovery systems
  - Performance optimization strategies

## References

- ECS_Game_Loop_Deep_Dive.md - Core ECS architecture
- ECS_Component_System.md - Component management
- Property_Calculator_System.md - Detailed property calculations
- Database_ORM_System.md - Database integration patterns 