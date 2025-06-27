# OpenDAoC Comprehensive Refactoring Plan

## Executive Summary

This document outlines a systematic approach to refactoring the OpenDAoC codebase from its current monolithic structure to a clean, testable, SOLID architecture. The plan leverages the extensive SRD documentation and existing architectural work to guide a chunk-by-chunk transformation over 32 weeks.

## Current State Analysis

### Codebase Characteristics
- **Monolithic Design**: Large classes (GameLiving: 3,798 lines, GameServer: 1,340 lines)
- **Tight Coupling**: Direct dependencies throughout, making testing difficult
- **Mixed Concerns**: Business logic, data access, and presentation mixed within entities
- **Limited Testability**: Lack of interfaces and dependency injection
- **Partial ECS Implementation**: Some components exist but not fully utilized
- **Complex Inheritance**: Deep hierarchies making changes risky

### Existing Assets
- **Comprehensive SRD**: 117 documented systems with formulas and mechanics
- **Interface Designs**: Core interfaces already defined in Helper Docs
- **Test Infrastructure**: Framework established with examples
- **ECS Foundation**: Basic component system in place
- **Architectural Guidelines**: SOLID principles documented

### Technical Debt Hotspots
1. **GameLiving Class**: Central entity with 100+ responsibilities
2. **Direct Database Access**: Scattered throughout codebase
3. **Static Dependencies**: GameServer.Instance pattern everywhere
4. **Event System**: Mixed with business logic
5. **Property System**: Complex calculations embedded in entities

## Refactoring Strategy

### Core Principles
1. **Incremental Transformation**: Small, safe changes that maintain functionality
2. **Test-First Approach**: Write tests before refactoring
3. **Interface Extraction**: Define contracts before implementation
4. **Dependency Injection**: Remove static dependencies
5. **Business Logic Extraction**: Move logic from entities to services
6. **Data Access Abstraction**: Repository pattern for all data access

### Phase-Based Approach

## Phase 1: Foundation (Weeks 1-4)

### Week 1: Dependency Injection Setup

#### 1.1 Create Service Container Infrastructure
```csharp
// Create new file: GameServer/Infrastructure/ServiceContainer.cs
public interface IServiceContainer
{
    void Register<TInterface, TImplementation>() where TImplementation : TInterface;
    void RegisterSingleton<TInterface, TImplementation>() where TImplementation : TInterface;
    void RegisterFactory<TInterface>(Func<TInterface> factory);
    T Resolve<T>();
    object Resolve(Type type);
}

// Temporary anti-pattern for migration
public static class ServiceLocator
{
    private static IServiceContainer _container;
    
    public static void Initialize(IServiceContainer container)
    {
        _container = container;
    }
    
    public static T Get<T>()
    {
        return _container.Resolve<T>();
    }
}
```

#### 1.2 Begin Service Registration
```csharp
// In GameServer.cs Start() method
private void RegisterServices(IServiceContainer container)
{
    // Core services
    container.RegisterSingleton<IPropertyService, PropertyService>();
    container.RegisterSingleton<ICombatService, CombatService>();
    container.RegisterSingleton<IEffectService, EffectService>();
    
    // Repositories
    container.RegisterSingleton<ICharacterRepository, CharacterRepository>();
    container.RegisterSingleton<IItemRepository, ItemRepository>();
    
    // Factories
    container.RegisterFactory<IEntityFactory>(() => new EntityFactory(container));
}
```

#### 1.3 Replace Static Calls Incrementally
```csharp
// Before:
var worldManager = GameServer.Instance.WorldManager;

// After:
var worldManager = ServiceLocator.Get<IWorldManager>();

// Eventually:
public class SomeService
{
    private readonly IWorldManager _worldManager;
    
    public SomeService(IWorldManager worldManager)
    {
        _worldManager = worldManager;
    }
}
```

### Week 2: Property Calculator Extraction

#### 2.1 Extract Property Calculators
```csharp
// Create new file: GameServer/PropertyCalculators/ArmorFactorCalculator.cs
public class ArmorFactorCalculator : IPropertyCalculator
{
    public eProperty Property => eProperty.ArmorFactor;
    
    public int Calculate(IPropertySource source)
    {
        // DAoC Rule: AF = ItemAF + 12.5 + (Level * 20 / 50)
        var itemBonus = Math.Min(source.Level, source.ItemBonus[eProperty.ArmorFactor]);
        var buffBonus = Math.Min(source.Level * 1.875, source.SpecBuffBonusCategory[eProperty.ArmorFactor]);
        var otherBonus = source.OtherBonus[eProperty.ArmorFactor];
        var debuff = Math.Abs(source.DebuffCategory[eProperty.ArmorFactor]);
        
        return (int)(itemBonus + buffBonus + otherBonus - debuff);
    }
}
```

#### 2.2 Create Property Service
```csharp
// Create new file: GameServer/Services/PropertyService.cs
public class PropertyService : IPropertyService
{
    private readonly Dictionary<eProperty, IPropertyCalculator> _calculators = new();
    
    public PropertyService()
    {
        RegisterCalculators();
    }
    
    private void RegisterCalculators()
    {
        Register(new ArmorFactorCalculator());
        Register(new ArmorAbsorptionCalculator());
        Register(new StrengthCalculator());
        // ... register all 30+ calculators
    }
    
    public void Register(IPropertyCalculator calculator)
    {
        _calculators[calculator.Property] = calculator;
    }
    
    public int Calculate(IPropertySource source, eProperty property)
    {
        if (_calculators.TryGetValue(property, out var calculator))
            return calculator.Calculate(source);
            
        return source.GetBase(property);
    }
}
```

#### 2.3 Update GameLiving to Use Service
```csharp
// In GameLiving.cs
public virtual int GetModified(eProperty property)
{
    // Old inline calculation replaced with:
    return ServiceLocator.Get<IPropertyService>().Calculate(this, property);
}
```

### Week 3: Combat System Interface Extraction

#### 3.1 Define Combat Interfaces
```csharp
// Create new file: GameServer/Combat/Interfaces/ICombatService.cs
public interface ICombatService
{
    AttackResult ProcessAttack(IAttacker attacker, IDefender defender, AttackContext context);
    DamageResult CalculateDamage(AttackData attackData);
    void ApplyDamage(ILiving target, DamageResult damage);
    bool CanAttack(IAttacker attacker, IDefender defender);
}

public interface IAttackResolver
{
    AttackResult ResolveAttack(AttackData data);
    double CalculateHitChance(AttackData data);
    DefenseResult CheckDefenses(AttackData data);
}

public interface IDamageCalculator
{
    DamageResult Calculate(AttackData data);
    int CalculateBaseDamage(IWeapon weapon);
    double CalculateVariance(AttackData data);
    int CalculateCritical(AttackData data);
}
```

#### 3.2 Extract Combat Logic
```csharp
// Create new file: GameServer/Combat/CombatService.cs
public class CombatService : ICombatService
{
    private readonly IAttackResolver _attackResolver;
    private readonly IDamageCalculator _damageCalculator;
    private readonly IPropertyService _propertyService;
    private readonly IEffectService _effectService;
    
    public CombatService(
        IAttackResolver attackResolver,
        IDamageCalculator damageCalculator,
        IPropertyService propertyService,
        IEffectService effectService)
    {
        _attackResolver = attackResolver;
        _damageCalculator = damageCalculator;
        _propertyService = propertyService;
        _effectService = effectService;
    }
    
    public AttackResult ProcessAttack(IAttacker attacker, IDefender defender, AttackContext context)
    {
        // DAoC Rule: Attack resolution order
        // 1. Range check
        // 2. Line of sight check
        // 3. Defense checks (evade, parry, block)
        // 4. Hit/miss calculation
        // 5. Damage calculation
        
        var attackData = new AttackData
        {
            Attacker = attacker,
            Defender = defender,
            Context = context
        };
        
        // Check if attack is possible
        if (!CanAttack(attacker, defender))
            return AttackResult.CannotAttack;
        
        // Resolve attack
        var result = _attackResolver.ResolveAttack(attackData);
        
        if (result == AttackResult.Hit)
        {
            var damage = _damageCalculator.Calculate(attackData);
            ApplyDamage(defender, damage);
        }
        
        return result;
    }
}
```

### Week 4: Integration Test Framework

#### 4.1 Create Test Database Setup
```csharp
// Create new file: Tests/IntegrationTests/TestDatabaseSetup.cs
public class TestDatabaseSetup
{
    private static IObjectDatabase _testDatabase;
    
    public static IObjectDatabase GetTestDatabase()
    {
        if (_testDatabase == null)
        {
            _testDatabase = new SqliteObjectDatabase(":memory:");
            InitializeSchema();
            SeedTestData();
        }
        return _testDatabase;
    }
    
    private static void InitializeSchema()
    {
        // Create all tables
        _testDatabase.RegisterDataObject(typeof(DbAccount));
        _testDatabase.RegisterDataObject(typeof(DbCharacter));
        _testDatabase.RegisterDataObject(typeof(DbItem));
        // ... register all data objects
    }
    
    private static void SeedTestData()
    {
        // Create test accounts
        var testAccount = new DbAccount
        {
            Name = "testaccount",
            Password = "testpass"
        };
        _testDatabase.AddObject(testAccount);
        
        // Create test characters
        var testCharacter = new DbCharacter
        {
            AccountName = "testaccount",
            Name = "TestWarrior",
            Level = 50,
            Class = (int)eCharacterClass.Warrior
        };
        _testDatabase.AddObject(testCharacter);
    }
}
```

#### 4.2 Create Integration Test Base
```csharp
// Create new file: Tests/IntegrationTests/IntegrationTestBase.cs
public abstract class IntegrationTestBase
{
    protected IServiceContainer Container { get; private set; }
    protected IObjectDatabase Database { get; private set; }
    
    [SetUp]
    public virtual void Setup()
    {
        // Setup test database
        Database = TestDatabaseSetup.GetTestDatabase();
        
        // Setup service container
        Container = new ServiceContainer();
        RegisterServices();
        
        // Initialize service locator for legacy code
        ServiceLocator.Initialize(Container);
    }
    
    protected virtual void RegisterServices()
    {
        // Register all services with test implementations
        Container.RegisterSingleton<IObjectDatabase>(() => Database);
        Container.RegisterSingleton<IPropertyService, PropertyService>();
        Container.RegisterSingleton<ICombatService, CombatService>();
        // ... register all services
    }
    
    [TearDown]
    public virtual void TearDown()
    {
        // Clean up
        Database.CloseConnections();
    }
}
```

### Phase 1 Deliverables Checklist
- [ ] Service container implemented and working
- [ ] 30+ property calculators extracted with tests
- [ ] Combat service interface defined and implemented
- [ ] Integration test framework operational
- [ ] 80% test coverage on extracted components
- [ ] Performance benchmarks established

## Phase 2: Core System Services (Weeks 5-12)

### Weeks 5-6: Character Progression Service

#### 5.1 Define Progression Interfaces
```csharp
// Create new file: GameServer/Character/Interfaces/ICharacterProgressionService.cs
public interface ICharacterProgressionService
{
    void GrantExperience(ICharacter character, long amount, eXPSource source);
    void LevelUp(ICharacter character, int levels = 1);
    void AllocateStatPoint(ICharacter character, eStat stat);
    void TrainSpecialization(ICharacter character, string spec, int points);
    bool CanLevelUp(ICharacter character);
    long GetExperienceForLevel(int level);
}

public interface ISpecializationService
{
    int GetSpecLevel(ICharacter character, string spec);
    int GetAvailablePoints(ICharacter character);
    bool CanTrain(ICharacter character, string spec, int points);
    void Train(ICharacter character, string spec, int points);
    IList<string> GetAvailableSpecs(ICharacter character);
}
```

#### 5.2 Extract Progression Logic
```csharp
// Create new file: GameServer/Character/CharacterProgressionService.cs
public class CharacterProgressionService : ICharacterProgressionService
{
    private readonly ICharacterRepository _characterRepository;
    private readonly ISpecializationService _specService;
    private readonly IEventAggregator _eventAggregator;
    
    // DAoC XP table from SRD
    private static readonly long[] XP_TABLE = 
    {
        0, // Level 0
        25, // Level 1
        125, // Level 2
        // ... full table
        1073741824 // Level 50
    };
    
    public void GrantExperience(ICharacter character, long amount, eXPSource source)
    {
        // DAoC Rule: Experience caps and modifiers
        var modifiedAmount = CalculateModifiedExperience(character, amount, source);
        
        var oldLevel = character.Level;
        character.Experience += modifiedAmount;
        
        // Check for level up
        while (CanLevelUp(character))
        {
            LevelUp(character);
        }
        
        // Notify
        _eventAggregator.Publish(new ExperienceGainedEvent
        {
            Character = character,
            Amount = modifiedAmount,
            Source = source,
            OldLevel = oldLevel,
            NewLevel = character.Level
        });
        
        // Persist
        _characterRepository.Update(character);
    }
    
    public void LevelUp(ICharacter character, int levels = 1)
    {
        for (int i = 0; i < levels && character.Level < 50; i++)
        {
            character.Level++;
            
            // DAoC Rule: Stat progression
            ApplyStatGains(character);
            
            // DAoC Rule: Spec points
            GrantSpecializationPoints(character);
            
            // DAoC Rule: HP/Mana increase
            UpdateHealthMana(character);
            
            // Grant abilities
            GrantLevelAbilities(character);
        }
    }
    
    private void ApplyStatGains(ICharacter character)
    {
        // DAoC Rule: Primary/Secondary/Tertiary stat gains
        if (character.Level >= 6)
        {
            // Primary: +1 every level
            character.ModifyStat(character.Class.PrimaryStat, 1);
            
            // Secondary: +1 every 2 levels
            if ((character.Level - 6) % 2 == 0)
                character.ModifyStat(character.Class.SecondaryStat, 1);
                
            // Tertiary: +1 every 3 levels
            if ((character.Level - 6) % 3 == 0)
                character.ModifyStat(character.Class.TertiaryStat, 1);
        }
    }
}
```

### Weeks 7-8: Effect System Refactoring

#### 7.1 Modernize Effect Interfaces
```csharp
// Create new file: GameServer/Effects/Interfaces/IEffectService.cs
public interface IEffectService
{
    void AddEffect(ILiving target, IEffect effect);
    void RemoveEffect(ILiving target, IEffect effect);
    void RemoveEffectsOfType(ILiving target, Type effectType);
    void ProcessEffects();
    IEnumerable<IEffect> GetEffects(ILiving target);
    bool HasEffect(ILiving target, Type effectType);
}

public interface IEffectFactory
{
    IEffect CreateSpellEffect(ISpell spell, int duration);
    IEffect CreateBuffEffect(eBuffType type, int value, int duration);
    IEffect CreateDebuffEffect(eDebuffType type, int value, int duration);
}
```

#### 7.2 Implement Effect Service
```csharp
// Create new file: GameServer/Effects/EffectService.cs
public class EffectService : IEffectService
{
    private readonly ConcurrentDictionary<string, List<IEffect>> _activeEffects = new();
    private readonly IEffectStackingRules _stackingRules;
    private readonly IEventAggregator _eventAggregator;
    
    public void AddEffect(ILiving target, IEffect effect)
    {
        var targetId = target.ObjectID;
        var effects = _activeEffects.GetOrAdd(targetId, _ => new List<IEffect>());
        
        lock (effects)
        {
            // Check stacking rules
            var toRemove = _stackingRules.GetEffectsToRemove(effects, effect);
            foreach (var removeEffect in toRemove)
            {
                RemoveEffectInternal(target, removeEffect);
            }
            
            // Add new effect
            effects.Add(effect);
            effect.Start(target);
            
            // Notify
            _eventAggregator.Publish(new EffectAddedEvent
            {
                Target = target,
                Effect = effect
            });
        }
    }
    
    public void ProcessEffects()
    {
        Parallel.ForEach(_activeEffects, kvp =>
        {
            var targetId = kvp.Key;
            var effects = kvp.Value;
            
            lock (effects)
            {
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    var effect = effects[i];
                    
                    if (effect.IsExpired)
                    {
                        RemoveEffectInternal(effect.Target, effect);
                    }
                    else
                    {
                        effect.Pulse();
                    }
                }
            }
        });
    }
}
```

### Weeks 9-10: Inventory and Item Services

#### 9.1 Define Item Service Interfaces
```csharp
// Create new file: GameServer/Items/Interfaces/IInventoryService.cs
public interface IInventoryService
{
    bool AddItem(ICharacter character, IItem item, InventorySlot slot);
    bool RemoveItem(ICharacter character, InventorySlot slot);
    bool MoveItem(ICharacter character, InventorySlot from, InventorySlot to);
    bool EquipItem(ICharacter character, IItem item, EquipmentSlot slot);
    bool UnequipItem(ICharacter character, EquipmentSlot slot);
    bool CanEquipItem(ICharacter character, IItem item);
    IItem GetItem(ICharacter character, InventorySlot slot);
}

public interface IItemService
{
    IItem CreateFromTemplate(string templateId);
    void UpdateItemCondition(IItem item, int conditionChange);
    void RepairItem(IItem item);
    ItemBonuses CalculateBonuses(IItem item);
    bool CanUseItem(ICharacter character, IItem item);
}
```

#### 9.2 Implement Inventory Service
```csharp
// Create new file: GameServer/Items/InventoryService.cs
public class InventoryService : IInventoryService
{
    private readonly IItemService _itemService;
    private readonly IPropertyService _propertyService;
    private readonly IEventAggregator _eventAggregator;
    
    public bool EquipItem(ICharacter character, IItem item, EquipmentSlot slot)
    {
        // DAoC Rule: Check requirements
        if (!CanEquipItem(character, item))
            return false;
            
        // DAoC Rule: Check slot compatibility
        if (!IsValidSlot(item, slot))
            return false;
            
        // Remove existing item
        var existingItem = character.Equipment[slot];
        if (existingItem != null)
        {
            UnequipItem(character, slot);
        }
        
        // Equip new item
        character.Equipment[slot] = item;
        
        // Update bonuses
        RecalculateBonuses(character);
        
        // Notify
        _eventAggregator.Publish(new ItemEquippedEvent
        {
            Character = character,
            Item = item,
            Slot = slot
        });
        
        return true;
    }
    
    private void RecalculateBonuses(ICharacter character)
    {
        // Clear item bonuses
        character.ItemBonus.Clear();
        
        // Calculate bonuses from all equipped items
        foreach (var item in character.Equipment.Values)
        {
            if (item == null) continue;
            
            var bonuses = _itemService.CalculateBonuses(item);
            foreach (var bonus in bonuses)
            {
                // DAoC Rule: Apply level-based caps
                var cappedValue = Math.Min(bonus.Value, GetBonusCap(character.Level, bonus.Property));
                character.ItemBonus[bonus.Property] += cappedValue;
            }
        }
        
        // Notify property system
        _propertyService.RecalculateProperties(character);
    }
}
```

### Weeks 11-12: Repository Implementation

#### 11.1 Define Repository Interfaces
```csharp
// Create new file: GameServer/Database/Interfaces/IRepository.cs
public interface IRepository<T> where T : class
{
    T GetById(object id);
    IList<T> GetAll();
    IList<T> Find(Expression<Func<T, bool>> predicate);
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task SaveChangesAsync();
}

public interface ICharacterRepository : IRepository<Character>
{
    Character GetByName(string name);
    IList<Character> GetByAccount(string accountId);
    IList<Character> GetByGuild(string guildId);
    IList<Character> GetOnlineCharacters();
}

public interface IItemRepository : IRepository<Item>
{
    IList<Item> GetByOwner(string ownerId);
    IList<Item> GetByTemplate(string templateId);
    IList<Item> GetEquippedItems(string characterId);
}
```

#### 11.2 Implement Character Repository
```csharp
// Create new file: GameServer/Database/CharacterRepository.cs
public class CharacterRepository : ICharacterRepository
{
    private readonly IObjectDatabase _database;
    private readonly IEntityFactory _entityFactory;
    private readonly ConcurrentDictionary<string, Character> _cache = new();
    
    public Character GetByName(string name)
    {
        // Check cache first
        var cached = _cache.Values.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (cached != null)
            return cached;
            
        // Load from database
        var dbChar = _database.SelectObject<DbCharacter>(DB.Column("Name").IsEqualTo(name));
        if (dbChar == null)
            return null;
            
        // Convert to domain entity
        var character = _entityFactory.CreateCharacter(dbChar);
        
        // Cache
        _cache[character.ObjectId] = character;
        
        return character;
    }
    
    public void Update(Character character)
    {
        // Convert to database entity
        var dbChar = ConvertToDbEntity(character);
        
        // Update database
        _database.SaveObject(dbChar);
        
        // Update cache
        _cache[character.ObjectId] = character;
    }
    
    private DbCharacter ConvertToDbEntity(Character character)
    {
        return new DbCharacter
        {
            ObjectId = character.ObjectId,
            Name = character.Name,
            Level = character.Level,
            Experience = character.Experience,
            // ... map all properties
        };
    }
}
```

### Phase 2 Deliverables Checklist
- [ ] Character Progression Service extracted and tested
- [ ] Effect System modernized with proper service layer
- [ ] Inventory Service implemented with full functionality
- [ ] Item Service handling all item operations
- [ ] Repository pattern implemented for Characters and Items
- [ ] GameLiving reduced by 50% in size
- [ ] 70% test coverage achieved
- [ ] Performance benchmarks show no degradation

## Phase 3: Entity Refactoring (Weeks 13-20)

### Weeks 13-14: GameLiving Decomposition

#### 13.1 Split GameLiving into Components
```csharp
// Create new file: GameServer/Entities/Components/HealthComponent.cs
public class HealthComponent : IComponent
{
    public int Current { get; set; }
    public int Maximum { get; set; }
    public int RegenRate { get; set; }
    public DateTime LastRegenTime { get; set; }
    
    public int Percentage => Maximum > 0 ? (Current * 100) / Maximum : 0;
    public bool IsAlive => Current > 0;
    public bool IsLowHealth => Percentage < 25;
}

// Create new file: GameServer/Entities/Components/CombatComponent.cs
public class CombatComponent : IComponent
{
    public IWeapon ActiveWeapon { get; set; }
    public IWeapon OffhandWeapon { get; set; }
    public eActiveWeaponSlot ActiveSlot { get; set; }
    public DateTime LastAttackTime { get; set; }
    public GameObject Target { get; set; }
    public bool IsInCombat => LastAttackTime.AddSeconds(10) > DateTime.Now;
}

// Create new file: GameServer/Entities/Components/StatsComponent.cs
public class StatsComponent : IComponent
{
    public Dictionary<eStat, int> BaseStats { get; } = new();
    public Dictionary<eStat, int> ModifiedStats { get; } = new();
    public int Level { get; set; }
    public long Experience { get; set; }
    
    public int GetStat(eStat stat) => ModifiedStats.GetValueOrDefault(stat, 0);
    public void SetBaseStat(eStat stat, int value) => BaseStats[stat] = value;
}
```

#### 13.2 Create New Entity Structure
```csharp
// Create new file: GameServer/Entities/Character.cs
public class Character : Entity, ICharacter
{
    private readonly IServiceContainer _services;
    
    public Character(string id, IServiceContainer services) : base(id)
    {
        _services = services;
        InitializeComponents();
    }
    
    private void InitializeComponents()
    {
        AddComponent(new HealthComponent());
        AddComponent(new CombatComponent());
        AddComponent(new StatsComponent());
        AddComponent(new InventoryComponent());
        AddComponent(new EffectsComponent());
        AddComponent(new PositionComponent());
    }
    
    // Delegate to components
    public int Health => GetComponent<HealthComponent>().Current;
    public int MaxHealth => GetComponent<HealthComponent>().Maximum;
    public int Level => GetComponent<StatsComponent>().Level;
    
    // Use services for logic
    public void TakeDamage(int amount, GameObject source)
    {
        _services.Resolve<ICombatService>().ApplyDamage(this, new DamageResult
        {
            Amount = amount,
            Source = source
        });
    }
    
    public void GrantExperience(long amount, eXPSource source)
    {
        _services.Resolve<ICharacterProgressionService>().GrantExperience(this, amount, source);
    }
}
```

### Weeks 15-16: Component System Full Implementation

#### 15.1 Define Component System
```csharp
// Create new file: GameServer/ECS/IEntity.cs
public interface IEntity
{
    string Id { get; }
    T GetComponent<T>() where T : class, IComponent;
    bool TryGetComponent<T>(out T component) where T : class, IComponent;
    void AddComponent<T>(T component) where T : class, IComponent;
    void RemoveComponent<T>() where T : class, IComponent;
    bool HasComponent<T>() where T : class, IComponent;
    IEnumerable<IComponent> GetAllComponents();
}

// Create new file: GameServer/ECS/Entity.cs
public abstract class Entity : IEntity
{
    private readonly Dictionary<Type, IComponent> _components = new();
    private readonly Lock _componentLock = new();
    
    public string Id { get; }
    
    protected Entity(string id)
    {
        Id = id;
    }
    
    public T GetComponent<T>() where T : class, IComponent
    {
        lock (_componentLock)
        {
            if (_components.TryGetValue(typeof(T), out var component))
                return component as T;
            return null;
        }
    }
    
    public void AddComponent<T>(T component) where T : class, IComponent
    {
        lock (_componentLock)
        {
            _components[typeof(T)] = component;
            component.OnAttached(this);
        }
    }
}
```

#### 15.2 Create Component Systems
```csharp
// Create new file: GameServer/ECS/Systems/HealthRegenerationSystem.cs
public class HealthRegenerationSystem : IComponentSystem
{
    private readonly IEntityManager _entityManager;
    
    public void Update(GameTime gameTime)
    {
        // Process all entities with health components
        var entities = _entityManager.GetEntitiesWithComponent<HealthComponent>();
        
        Parallel.ForEach(entities, entity =>
        {
            var health = entity.GetComponent<HealthComponent>();
            if (health.Current < health.Maximum && health.RegenRate > 0)
            {
                // DAoC Rule: Health regeneration every 6 seconds
                if (gameTime.TotalTime - health.LastRegenTime > TimeSpan.FromSeconds(6))
                {
                    health.Current = Math.Min(health.Maximum, health.Current + health.RegenRate);
                    health.LastRegenTime = gameTime.TotalTime;
                }
            }
        });
    }
}

// Create new file: GameServer/ECS/Systems/CombatSystem.cs
public class CombatSystem : IComponentSystem
{
    private readonly ICombatService _combatService;
    private readonly IEntityManager _entityManager;
    
    public void Update(GameTime gameTime)
    {
        // Process all entities with combat components
        var entities = _entityManager.GetEntitiesWithComponent<CombatComponent>();
        
        foreach (var entity in entities)
        {
            var combat = entity.GetComponent<CombatComponent>();
            if (combat.Target != null && CanAttack(entity, combat, gameTime))
            {
                ProcessAttack(entity, combat);
            }
        }
    }
}
```

### Weeks 17-18: Factory Pattern Implementation

#### 17.1 Create Entity Factories
```csharp
// Create new file: GameServer/Factories/EntityFactory.cs
public class EntityFactory : IEntityFactory
{
    private readonly IServiceContainer _services;
    private readonly ITemplateManager _templateManager;
    
    public ICharacter CreateCharacter(CharacterCreationData data)
    {
        var character = new Character(Guid.NewGuid().ToString(), _services);
        
        // Set up base components
        var stats = character.GetComponent<StatsComponent>();
        stats.Level = 1;
        ApplyRaceStats(stats, data.Race);
        ApplyClassStats(stats, data.Class);
        
        var health = character.GetComponent<HealthComponent>();
        health.Maximum = CalculateMaxHealth(data.Class, stats);
        health.Current = health.Maximum;
        
        // Apply starting equipment
        ApplyStartingEquipment(character, data.Class);
        
        return character;
    }
    
    public INPC CreateNPC(string templateId)
    {
        var template = _templateManager.GetNPCTemplate(templateId);
        if (template == null)
            throw new ArgumentException($"NPC template '{templateId}' not found");
            
        var npc = new NPC(Guid.NewGuid().ToString(), _services);
        
        // Apply template
        ApplyNPCTemplate(npc, template);
        
        return npc;
    }
    
    public IItem CreateItem(string templateId)
    {
        var template = _templateManager.GetItemTemplate(templateId);
        if (template == null)
            throw new ArgumentException($"Item template '{templateId}' not found");
            
        var item = new Item(Guid.NewGuid().ToString());
        
        // Apply template
        item.Name = template.Name;
        item.Level = template.Level;
        item.Type = template.Type;
        
        // Apply bonuses
        foreach (var bonus in template.Bonuses)
        {
            item.AddBonus(bonus.Property, bonus.Value);
        }
        
        return item;
    }
}
```

#### 17.2 Create Component Factories
```csharp
// Create new file: GameServer/Factories/ComponentFactory.cs
public class ComponentFactory : IComponentFactory
{
    private readonly Dictionary<Type, Func<ComponentData, IComponent>> _factories = new();
    
    public ComponentFactory()
    {
        RegisterFactories();
    }
    
    private void RegisterFactories()
    {
        Register<HealthComponent>(data => new HealthComponent
        {
            Current = data.GetInt("current"),
            Maximum = data.GetInt("maximum"),
            RegenRate = data.GetInt("regenRate", 1)
        });
        
        Register<StatsComponent>(data =>
        {
            var stats = new StatsComponent();
            foreach (var stat in Enum.GetValues<eStat>())
            {
                stats.SetBaseStat(stat, data.GetInt(stat.ToString(), 0));
            }
            return stats;
        });
        
        // ... register all component factories
    }
    
    public T CreateComponent<T>(ComponentData data) where T : class, IComponent
    {
        if (_factories.TryGetValue(typeof(T), out var factory))
        {
            return factory(data) as T;
        }
        throw new NotSupportedException($"No factory registered for component type {typeof(T)}");
    }
}
```

### Weeks 19-20: Event System Modernization

#### 19.1 Create Modern Event System
```csharp
// Create new file: GameServer/Events/EventAggregator.cs
public class EventAggregator : IEventAggregator
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    
    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        var handlers = _handlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }
    
    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            lock (handlers)
            {
                handlers.Remove(handler);
            }
        }
    }
    
    public void Publish<TEvent>(TEvent eventData)
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            List<Delegate> handlersCopy;
            lock (handlers)
            {
                handlersCopy = new List<Delegate>(handlers);
            }
            
            Parallel.ForEach(handlersCopy, handler =>
            {
                try
                {
                    (handler as Action<TEvent>)?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    // Log exception but don't crash
                    LogError($"Event handler error: {ex}");
                }
            });
        }
    }
}
```

#### 19.2 Define Typed Events
```csharp
// Create new file: GameServer/Events/GameEvents.cs
public class CharacterLevelUpEvent
{
    public ICharacter Character { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CombatDamageEvent
{
    public ILiving Attacker { get; set; }
    public ILiving Target { get; set; }
    public int Damage { get; set; }
    public eDamageType DamageType { get; set; }
    public bool WasCritical { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ItemEquippedEvent
{
    public ICharacter Character { get; set; }
    public IItem Item { get; set; }
    public EquipmentSlot Slot { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class EffectAppliedEvent
{
    public ILiving Target { get; set; }
    public IEffect Effect { get; set; }
    public GameObject Source { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

### Phase 3 Deliverables Checklist
- [ ] GameLiving split into 10+ focused components
- [ ] Full ECS implementation with component systems
- [ ] Entity factory creating all game entities
- [ ] Component factory for flexible entity composition
- [ ] Modern event system replacing GameEventMgr
- [ ] 85% test coverage on entities and components
- [ ] Performance benchmarks showing improved memory usage

## Phase 4: System Integration (Weeks 21-28)

### Weeks 21-22: Service Layer Completion

#### 21.1 Implement Remaining Services
```csharp
// Guild Service
public interface IGuildService
{
    IGuild CreateGuild(string name, ICharacter leader);
    bool AddMember(IGuild guild, ICharacter character);
    bool RemoveMember(IGuild guild, ICharacter character);
    bool SetRank(IGuild guild, ICharacter character, int rank);
    bool DisbandGuild(IGuild guild);
}

// Housing Service
public interface IHousingService
{
    IHouse PurchaseHouse(ICharacter owner, int houseNumber);
    bool SetPermission(IHouse house, ICharacter character, HousePermission permission);
    bool PayRent(IHouse house, int amount);
    bool AddDecoration(IHouse house, IItem decoration, DecorationType type);
}

// Quest Service
public interface IQuestService
{
    IList<IQuest> GetAvailableQuests(ICharacter character);
    bool StartQuest(ICharacter character, IQuest quest);
    bool AdvanceQuest(ICharacter character, IQuest quest, QuestTrigger trigger);
    bool CompleteQuest(ICharacter character, IQuest quest);
    bool AbandonQuest(ICharacter character, IQuest quest);
}
```

#### 21.2 Create Service Facades
```csharp
// Create new file: GameServer/Services/GameplayFacade.cs
public class GameplayFacade : IGameplayFacade
{
    private readonly ICombatService _combat;
    private readonly ICharacterProgressionService _progression;
    private readonly IInventoryService _inventory;
    private readonly IEffectService _effects;
    
    // Coordinate complex operations across services
    public async Task<bool> UseItem(ICharacter character, IItem item)
    {
        // Check if item can be used
        if (!_inventory.CanUseItem(character, item))
            return false;
            
        // Apply item effects
        var itemEffects = item.GetEffects();
        foreach (var effect in itemEffects)
        {
            _effects.AddEffect(character, effect);
        }
        
        // Consume item if needed
        if (item.IsConsumable)
        {
            _inventory.RemoveItem(character, item.Slot);
        }
        
        // Update item condition
        if (item.MaxCondition > 0)
        {
            item.Condition--;
            if (item.Condition <= 0)
            {
                _inventory.RemoveItem(character, item.Slot);
            }
        }
        
        return true;
    }
}
```

### Weeks 23-24: Static Dependency Removal

#### 23.1 Replace GameServer.Instance Calls
```csharp
// Before:
public class SomeOldClass
{
    public void DoSomething()
    {
        var worldMgr = GameServer.Instance.WorldManager;
        var player = GameServer.Instance.PlayerManager.GetPlayerByName("test");
    }
}

// After:
public class SomeRefactoredClass
{
    private readonly IWorldManager _worldManager;
    private readonly IPlayerManager _playerManager;
    
    public SomeRefactoredClass(IWorldManager worldManager, IPlayerManager playerManager)
    {
        _worldManager = worldManager;
        _playerManager = playerManager;
    }
    
    public void DoSomething()
    {
        var player = _playerManager.GetPlayerByName("test");
    }
}
```

#### 23.2 Update Script System
```csharp
// Create new file: GameServer/Scripts/ScriptContext.cs
public class ScriptContext : IScriptContext
{
    public IServiceContainer Services { get; }
    public IEventAggregator Events { get; }
    public ILogger Logger { get; }
    
    public ScriptContext(IServiceContainer services, IEventAggregator events, ILogger logger)
    {
        Services = services;
        Events = events;
        Logger = logger;
    }
}

// Update script base class
public abstract class GameScript
{
    protected IScriptContext Context { get; private set; }
    
    public void Initialize(IScriptContext context)
    {
        Context = context;
        OnInitialize();
    }
    
    protected abstract void OnInitialize();
    
    // Helper properties for common services
    protected IWorldManager WorldManager => Context.Services.Resolve<IWorldManager>();
    protected ICombatService CombatService => Context.Services.Resolve<ICombatService>();
}
```

### Weeks 25-26: Caching Implementation

#### 25.1 Create Cache Service
```csharp
// Create new file: GameServer/Caching/CacheService.cs
public class CacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly Timer _cleanupTimer;
    
    public CacheService()
    {
        // Cleanup expired entries every minute
        _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }
    
    public T Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                return default;
            }
            
            entry.LastAccessed = DateTime.UtcNow;
            return (T)entry.Value;
        }
        return default;
    }
    
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var entry = new CacheEntry
        {
            Value = value,
            Created = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
            Expiration = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : DateTime.MaxValue
        };
        
        _cache[key] = entry;
    }
    
    public T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? expiration = null)
    {
        var value = Get<T>(key);
        if (value != null)
            return value;
            
        value = factory();
        Set(key, value, expiration);
        return value;
    }
}
```

#### 25.2 Implement Caching Strategies
```csharp
// Update PropertyService with caching
public class PropertyService : IPropertyService
{
    private readonly ICacheService _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromSeconds(30);
    
    public int Calculate(IPropertySource source, eProperty property)
    {
        var cacheKey = $"property:{source.ObjectId}:{property}";
        
        return _cache.GetOrAdd(cacheKey, () =>
        {
            if (_calculators.TryGetValue(property, out var calculator))
                return calculator.Calculate(source);
                
            return source.GetBase(property);
        }, _cacheExpiration);
    }
    
    public void InvalidateCache(IPropertySource source)
    {
        // Clear all cached properties for this source
        foreach (var property in Enum.GetValues<eProperty>())
        {
            var cacheKey = $"property:{source.ObjectId}:{property}";
            _cache.Remove(cacheKey);
        }
    }
}

// Cache NPC templates
public class TemplateManager : ITemplateManager
{
    private readonly ICacheService _cache;
    
    public INPCTemplate GetNPCTemplate(string templateId)
    {
        return _cache.GetOrAdd($"npc_template:{templateId}", () =>
        {
            // Load from database
            var dbTemplate = _database.SelectObject<DbNpcTemplate>(t => t.TemplateId == templateId);
            return ConvertToTemplate(dbTemplate);
        }, TimeSpan.FromHours(1)); // NPCs templates rarely change
    }
}
```

### Weeks 27-28: Performance Optimization

#### 27.1 Implement Object Pooling
```csharp
// Create new file: GameServer/Pooling/ObjectPool.cs
public class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _pool = new();
    private readonly Func<T> _factory;
    private readonly Action<T> _reset;
    private readonly int _maxSize;
    
    public ObjectPool(Func<T> factory = null, Action<T> reset = null, int maxSize = 1000)
    {
        _factory = factory ?? (() => new T());
        _reset = reset;
        _maxSize = maxSize;
    }
    
    public T Rent()
    {
        if (_pool.TryTake(out T item))
            return item;
            
        return _factory();
    }
    
    public void Return(T item)
    {
        if (item == null)
            return;
            
        _reset?.Invoke(item);
        
        if (_pool.Count < _maxSize)
            _pool.Add(item);
    }
}

// Use pooling for hot paths
public class AttackDataPool
{
    private static readonly ObjectPool<AttackData> _pool = new(
        factory: () => new AttackData(),
        reset: data =>
        {
            data.Attacker = null;
            data.Target = null;
            data.Weapon = null;
            data.Damage = 0;
            // ... reset all fields
        }
    );
    
    public static AttackData Get() => _pool.Rent();
    public static void Return(AttackData data) => _pool.Return(data);
}
```

#### 27.2 Optimize Database Queries
```csharp
// Batch loading with eager loading
public class CharacterRepository : ICharacterRepository
{
    public async Task<IList<Character>> GetOnlineCharactersOptimized()
    {
        // Single query with joins instead of N+1
        var query = @"
            SELECT c.*, i.*, e.*, g.*
            FROM Characters c
            LEFT JOIN Items i ON i.OwnerId = c.ObjectId
            LEFT JOIN Effects e ON e.TargetId = c.ObjectId
            LEFT JOIN Guilds g ON g.GuildId = c.GuildId
            WHERE c.IsOnline = 1";
            
        var characters = new Dictionary<string, Character>();
        
        await _database.QueryAsync(query, (character, item, effect, guild) =>
        {
            if (!characters.TryGetValue(character.ObjectId, out var existing))
            {
                existing = _entityFactory.CreateCharacter(character);
                characters[character.ObjectId] = existing;
            }
            
            if (item != null)
                existing.Inventory.AddItem(item);
                
            if (effect != null)
                existing.Effects.Add(effect);
                
            if (guild != null)
                existing.Guild = guild;
                
            return existing;
        });
        
        return characters.Values.ToList();
    }
}
```

### Phase 4 Deliverables Checklist
- [ ] All major game services implemented (30+ services)
- [ ] Service facades coordinating complex operations
- [ ] Zero static dependencies (all GameServer.Instance removed)
- [ ] Script system updated to use dependency injection
- [ ] Comprehensive caching reducing calculations by 60%
- [ ] Object pooling for performance-critical paths
- [ ] Database queries optimized (no N+1 queries)
- [ ] 90% test coverage across all services
- [ ] Performance metrics equal or better than baseline

## Phase 5: Polish and Documentation (Weeks 29-32)

### Week 29: Documentation Completion

#### 29.1 Architecture Documentation
```markdown
# OpenDAoC Architecture Guide

## Overview
OpenDAoC uses a modern, service-oriented architecture built on SOLID principles.

## Core Concepts

### Entity Component System (ECS)
- Entities are containers of components
- Components hold data
- Systems process components
- Services provide business logic

### Dependency Injection
- All dependencies injected via constructor
- Service container manages lifetimes
- No static dependencies

### Event-Driven Architecture
- Loosely coupled communication
- Type-safe events
- Async event processing

## Service Catalog

### Combat Services
- `ICombatService`: Main combat orchestration
- `IAttackResolver`: Attack resolution logic
- `IDamageCalculator`: Damage calculations

### Character Services
- `ICharacterProgressionService`: XP, levels, stats
- `ISpecializationService`: Skill training
- `ICharacterRepository`: Data persistence

[... continue for all services ...]
```

#### 29.2 Developer Onboarding Guide
```markdown
# OpenDAoC Developer Guide

## Getting Started

### Prerequisites
- .NET 6.0 SDK
- Visual Studio 2022 or VS Code
- Git

### Building the Project
```bash
git clone https://github.com/OpenDAoC/OpenDAoC-Core.git
cd OpenDAoC-Core
dotnet restore
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Project Structure
- `/GameServer` - Core game logic
- `/GameServer/Services` - Business logic services
- `/GameServer/Entities` - Game entities and components
- `/GameServer/ECS` - Entity Component System
- `/Tests` - Unit and integration tests
- `/SRD` - System Reference Documentation

## Development Workflow

### Adding a New Feature
1. Check SRD for game rules
2. Write tests first (TDD)
3. Implement interfaces
4. Implement services
5. Update documentation
6. Submit PR

### Code Standards
- Use dependency injection
- Follow SOLID principles
- Write unit tests (>80% coverage)
- Document public APIs
- Use async/await for I/O

[... continue with examples ...]
```

### Week 30: Test Coverage Push

#### 30.1 Add Missing Tests
```csharp
// Test for complex combat scenarios
[TestFixture]
public class CombatIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task Combat_WithMultipleAttackersAndBuffs_CalculatesCorrectly()
    {
        // Arrange
        var attacker1 = CreateWarrior(level: 50);
        var attacker2 = CreateWarrior(level: 50);
        var defender = CreateWarrior(level: 50);
        
        // Apply buffs
        await ApplyStrengthBuff(attacker1, 50);
        await ApplyDexterityBuff(defender, 50);
        
        // Act
        var result1 = await ProcessAttack(attacker1, defender);
        var result2 = await ProcessAttack(attacker2, defender);
        
        // Assert - verify multi-attacker penalties applied
        Assert.That(result2.HitChance, Is.LessThan(result1.HitChance));
    }
}
```

#### 30.2 Add Performance Tests
```csharp
[TestFixture]
public class PerformanceTests
{
    [Test]
    public void PropertyCalculation_ShouldComplete_Within500Microseconds()
    {
        // Arrange
        var character = CreateMaxLevelCharacter();
        var propertyService = Container.Resolve<IPropertyService>();
        
        // Warm up
        for (int i = 0; i < 100; i++)
        {
            propertyService.Calculate(character, eProperty.Strength);
        }
        
        // Act
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            propertyService.Calculate(character, eProperty.Strength);
        }
        sw.Stop();
        
        // Assert
        var avgTime = sw.Elapsed.TotalMilliseconds / 1000;
        Assert.That(avgTime, Is.LessThan(0.5), $"Average time {avgTime}ms exceeds 0.5ms target");
    }
}
```

### Week 31: Code Standardization

#### 31.1 Apply Consistent Formatting
```xml
<!-- .editorconfig updates -->
[*.cs]
# Indentation
indent_style = space
indent_size = 4

# New line preferences
end_of_line = crlf
insert_final_newline = true

# Language conventions
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning

# Naming conventions
dotnet_naming_rule.interface_should_be_prefixed_with_i.severity = warning
dotnet_naming_rule.interface_should_be_prefixed_with_i.symbols = interface
dotnet_naming_rule.interface_should_be_prefixed_with_i.style = prefix_interface_with_i
```

#### 31.2 Remove Dead Code
```csharp
// Run code analysis tools
// - Remove unused usings
// - Remove unused private members
// - Remove commented out code
// - Remove empty catch blocks
// - Remove redundant casts
```

### Week 32: Team Training

#### 32.1 Architecture Workshop Outline
```markdown
# OpenDAoC Architecture Workshop

## Session 1: Core Concepts (2 hours)
- SOLID Principles in Practice
- Dependency Injection Deep Dive
- Entity Component System
- Service Layer Pattern

## Session 2: Testing Strategies (2 hours)
- Unit Testing with Mocks
- Integration Testing
- Performance Testing
- Test Data Builders

## Session 3: Common Patterns (2 hours)
- Repository Pattern
- Factory Pattern
- Observer Pattern
- Command Pattern

## Session 4: Hands-On Refactoring (4 hours)
- Refactor a legacy class together
- Write tests for legacy code
- Extract services
- Add dependency injection
```

### Phase 5 Deliverables Checklist
- [ ] Complete architecture documentation
- [ ] Developer onboarding guide with examples
- [ ] API documentation for all public interfaces
- [ ] 95% test coverage on critical systems
- [ ] Performance test suite with benchmarks
- [ ] Code standardized across entire codebase
- [ ] Dead code removed
- [ ] Team trained on new architecture
- [ ] Video tutorials created
- [ ] Knowledge transfer completed

## Risk Mitigation Strategies

### Technical Risks

#### 1. Performance Degradation
**Risk**: Refactoring might introduce performance issues
**Mitigation**:
- Continuous benchmarking with BenchmarkDotNet
- Performance tests in CI/CD pipeline
- Feature flags to toggle between old/new code
- Profiling before and after each phase

#### 2. Breaking Changes
**Risk**: Refactoring might break existing functionality
**Mitigation**:
- Comprehensive test suite before refactoring
- Parallel run of old and new code
- Gradual rollout with feature flags
- Automated regression testing

#### 3. Data Migration Issues
**Risk**: Entity changes might require data migration
**Mitigation**:
- Backward compatible entity design
- Versioned serialization
- Migration scripts tested in staging
- Rollback procedures documented

### Process Risks

#### 1. Scope Creep
**Risk**: Refactoring scope expanding beyond plan
**Mitigation**:
- Strict phase boundaries
- Weekly progress reviews
- Change control process
- Focus on MVP for each phase

#### 2. Team Resistance
**Risk**: Developers resistant to new patterns
**Mitigation**:
- Early team involvement
- Pair programming sessions
- Gradual adoption curve
- Celebrate early wins

## Success Metrics Dashboard

### Code Quality Metrics
```yaml
Test Coverage:
  Target: >90%
  Current: Track weekly
  
Cyclomatic Complexity:
  Target: <10 per method
  Current: Track per PR
  
Class Size:
  Target: <500 lines
  Current: Track monthly
  
Technical Debt:
  Target: <5% of codebase
  Current: Track monthly
```

### Performance Metrics
```yaml
Combat Calculation:
  Target: <1ms
  Baseline: Measure before
  Current: Track daily
  
Property Calculation:
  Target: <0.5ms
  Baseline: Measure before
  Current: Track daily
  
Memory Usage:
  Target: -20% from baseline
  Baseline: Measure before
  Current: Track weekly
```

### Development Metrics
```yaml
PR Merge Time:
  Target: <2 days
  Baseline: Current average
  Track: Weekly
  
Bug Discovery Rate:
  Target: -50%
  Baseline: Last 3 months
  Track: Monthly
  
Feature Velocity:
  Target: +40%
  Baseline: Last 3 months
  Track: Per sprint
```

## Conclusion

This comprehensive refactoring plan provides a clear path from the current monolithic architecture to a modern, maintainable codebase. The key success factors are:

1. **Incremental Progress**: Small, safe changes that maintain functionality
2. **Test Coverage**: Comprehensive tests before and after refactoring
3. **Team Involvement**: Early and continuous team engagement
4. **Performance Focus**: Continuous monitoring and optimization
5. **Documentation**: Clear, up-to-date documentation throughout

By following this plan, the OpenDAoC project will achieve a sustainable, high-quality codebase that supports rapid development and easy maintenance for years to come. 