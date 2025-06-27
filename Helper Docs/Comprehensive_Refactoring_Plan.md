# OpenDAoC Comprehensive Refactoring Plan

## Executive Summary

This document outlines a systematic approach to refactoring the OpenDAoC codebase from its current monolithic structure to a clean, testable, SOLID architecture. The plan leverages the extensive SRD documentation and existing architectural work to guide a chunk-by-chunk transformation.

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

### Objectives
- Establish dependency injection container
- Create core service interfaces
- Set up integration test harness
- Begin property calculator extraction

### Specific Tasks

#### Week 1: Dependency Injection Setup
```csharp
// 1. Implement DI Container (suggest Autofac or built-in .NET Core DI)
public interface IServiceContainer
{
    T Resolve<T>();
    void Register<TInterface, TImplementation>();
}

// 2. Create ServiceLocator anti-pattern wrapper for transition
public static class ServiceLocator
{
    private static IServiceContainer _container;
    public static T Get<T>() => _container.Resolve<T>();
}

// 3. Begin replacing GameServer.Instance calls
// From: GameServer.Instance.WorldManager
// To: ServiceLocator.Get<IWorldManager>()
```

#### Week 2: Property Calculator Extraction
```csharp
// 1. Extract all property calculators to separate classes
// 2. Implement IPropertyCalculatorRegistry
// 3. Create comprehensive tests for each calculator
// 4. Replace inline calculations with service calls

// Example transformation:
// Before (in GameLiving):
public virtual int GetModified(eProperty property)
{
    // Complex inline calculation
}

// After:
public virtual int GetModified(eProperty property)
{
    return ServiceLocator.Get<IPropertyService>()
        .Calculate(this, property);
}
```

#### Week 3: Combat System Interface Extraction
```csharp
// 1. Create ICombatService with all combat logic
// 2. Extract attack resolution to AttackResolver
// 3. Extract damage calculation to DamageCalculator
// 4. Create comprehensive combat tests

public interface ICombatService
{
    AttackResult ProcessAttack(AttackData data);
    DamageResult CalculateDamage(AttackData data);
    void ApplyDamage(ILiving target, DamageResult damage);
}
```

#### Week 4: Integration Test Framework
- Set up test database with known data
- Create test harness for cross-system testing
- Implement fixture builders for common scenarios
- Document test patterns for team

### Deliverables
- Working DI container
- 30+ property calculators extracted and tested
- Combat service interface implemented
- Integration test framework operational
- 80% test coverage on extracted components

## Phase 2: Core System Services (Weeks 5-12)

### Objectives
- Extract major game systems to services
- Implement repository pattern for data access
- Reduce GameLiving class by 50%
- Achieve 70% test coverage on core systems

### System Extraction Order

#### Weeks 5-6: Character Progression Service
```csharp
public interface ICharacterProgressionService
{
    void GrantExperience(ICharacter character, long amount);
    void LevelUp(ICharacter character);
    void AllocateStatPoint(ICharacter character, eStat stat);
    void TrainSpecialization(ICharacter character, string spec, int points);
}

// Extract from GameLiving:
// - Experience calculation
// - Level up mechanics
// - Stat progression
// - Specialization training
```

#### Weeks 7-8: Effect System Refactoring
```csharp
public interface IEffectService
{
    void AddEffect(ILiving target, IEffect effect);
    void RemoveEffect(ILiving target, IEffect effect);
    void ProcessEffects(ILiving target);
    IEnumerable<IEffect> GetEffects(ILiving target);
}

// Modernize effect system:
// - Remove tight coupling to GameLiving
// - Implement effect stacking rules as separate service
// - Create effect factories for common effects
```

#### Weeks 9-10: Inventory and Item Services
```csharp
public interface IInventoryService
{
    bool AddItem(ICharacter character, IItem item, InventorySlot slot);
    bool RemoveItem(ICharacter character, InventorySlot slot);
    bool MoveItem(ICharacter character, InventorySlot from, InventorySlot to);
    bool CanEquipItem(ICharacter character, IItem item);
}

public interface IItemService
{
    IItem CreateFromTemplate(string templateId);
    void UpdateItemCondition(IItem item, int amount);
    ItemBonuses CalculateBonuses(IItem item);
}
```

#### Weeks 11-12: Repository Implementation
```csharp
// Implement repositories for all major entities
public interface ICharacterRepository : IRepository<Character>
{
    Character GetByName(string name);
    IList<Character> GetByAccount(string accountId);
}

public interface IItemRepository : IRepository<Item>
{
    IList<Item> GetByOwner(string ownerId);
    IList<Item> GetByTemplate(string templateId);
}

// Begin replacing direct database access
```

### Deliverables
- 6 major services extracted and tested
- Repository pattern implemented for core entities
- GameLiving reduced from 3,798 to ~1,900 lines
- 70% test coverage on extracted services
- Performance benchmarks showing no degradation

## Phase 3: Entity Refactoring (Weeks 13-20)

### Objectives
- Split monolithic entities into focused classes
- Implement proper ECS architecture
- Create entity factories
- Achieve full interface-based design

### Entity Decomposition Strategy

#### Weeks 13-14: GameLiving Decomposition
```csharp
// Split GameLiving into:
// 1. CoreEntity - Base properties (ID, Name, Position)
// 2. CombatEntity - Combat-related properties
// 3. StatsEntity - Statistics and attributes
// 4. EffectsEntity - Active effects container
// 5. InventoryEntity - Equipment and items

// Use composition:
public class Character : ICharacter
{
    private readonly CoreEntity _core;
    private readonly CombatEntity _combat;
    private readonly StatsEntity _stats;
    private readonly EffectsEntity _effects;
    private readonly InventoryEntity _inventory;
    
    // Delegate to appropriate entity
    public int Health => _combat.Health;
    public int Level => _stats.Level;
}
```

#### Weeks 15-16: Component System Full Implementation
```csharp
// Extend existing ECS system
public interface IComponent { }

public interface IEntity
{
    string Id { get; }
    T GetComponent<T>() where T : IComponent;
    void AddComponent<T>(T component) where T : IComponent;
    bool HasComponent<T>() where T : IComponent;
}

// Refactor to component-based:
public class HealthComponent : IComponent
{
    public int Current { get; set; }
    public int Maximum { get; set; }
}

public class PositionComponent : IComponent
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
}
```

#### Weeks 17-18: Factory Pattern Implementation
```csharp
public interface IEntityFactory
{
    ICharacter CreateCharacter(CharacterCreationData data);
    INPC CreateNPC(string templateId);
    IItem CreateItem(string templateId);
}

public interface IComponentFactory
{
    T CreateComponent<T>(ComponentData data) where T : IComponent;
}
```

#### Weeks 19-20: Event System Modernization
```csharp
// Replace GameEventMgr with modern event aggregator
public interface IEventAggregator
{
    void Subscribe<TEvent>(Action<TEvent> handler);
    void Unsubscribe<TEvent>(Action<TEvent> handler);
    void Publish<TEvent>(TEvent eventData);
}

// Type-safe events
public class CharacterLevelUpEvent
{
    public ICharacter Character { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
}
```

### Deliverables
- Monolithic entities decomposed into focused classes
- Full ECS implementation with 20+ components
- Factory pattern for all entity creation
- Modern event system implemented
- 85% test coverage on entities

## Phase 4: System Integration (Weeks 21-28)

### Objectives
- Complete service layer implementation
- Remove all static dependencies
- Implement caching strategies
- Performance optimization

### Integration Tasks

#### Weeks 21-22: Service Layer Completion
- Implement remaining game services
- Create service facades for complex operations
- Document service interactions
- Complete integration tests

#### Weeks 23-24: Static Dependency Removal
- Replace all GameServer.Instance calls
- Remove static property accessors
- Implement proper configuration injection
- Update all scripts to use DI

#### Weeks 25-26: Caching Implementation
```csharp
public interface ICacheService
{
    T Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiration = null);
    void Remove(string key);
    void Clear();
}

// Implement caching for:
// - Property calculations
// - Item bonuses
// - Spell effects
// - NPC templates
```

#### Weeks 27-28: Performance Optimization
- Profile critical paths
- Implement object pooling for hot paths
- Optimize database queries
- Add performance benchmarks to CI/CD

### Deliverables
- Complete service layer with 30+ services
- Zero static dependencies
- Caching system reducing calculations by 60%
- Performance metrics meeting or exceeding original
- 90% test coverage overall

## Phase 5: Polish and Documentation (Weeks 29-32)

### Objectives
- Complete documentation
- Implement remaining tests
- Code cleanup and standardization
- Team training

### Final Tasks

#### Week 29: Documentation Completion
- Update all XML documentation
- Create architecture diagrams
- Write developer onboarding guide
- Document common patterns

#### Week 30: Test Coverage Push
- Achieve 95% coverage on critical systems
- Add mutation testing
- Create load tests
- Document test patterns

#### Week 31: Code Standardization
- Apply consistent formatting
- Remove dead code
- Standardize naming conventions
- Run static analysis tools

#### Week 32: Team Training
- Conduct architecture workshops
- Create video tutorials
- Pair programming sessions
- Knowledge transfer documentation

## Implementation Guidelines

### Refactoring Rules
1. **Never Break Functionality**: Each change must pass all tests
2. **Small Commits**: One logical change per commit
3. **Feature Flags**: Use flags for large changes
4. **Backward Compatibility**: Maintain old interfaces during transition
5. **Performance Monitoring**: Track metrics throughout

### Testing Strategy
```csharp
// Unit Test Structure
[TestFixture]
public class PropertyCalculatorTests
{
    [Test]
    public void CalculateArmorFactor_WithLevel50Character_ReturnsExpectedValue()
    {
        // Test validates: DAoC Rule - AF = ItemAF + 12.5 + (Level * 20 / 50)
        // Arrange
        var character = new CharacterBuilder().WithLevel(50).Build();
        var calculator = new ArmorFactorCalculator();
        
        // Act
        var result = calculator.Calculate(character, eProperty.ArmorFactor);
        
        // Assert
        Assert.That(result, Is.EqualTo(expectedValue));
    }
}
```

### Code Review Checklist
- [ ] Follows SOLID principles
- [ ] Has appropriate test coverage (>80%)
- [ ] Uses dependency injection
- [ ] No static dependencies introduced
- [ ] Proper error handling
- [ ] Performance benchmarked
- [ ] Documentation updated

## Risk Mitigation

### Technical Risks
1. **Performance Degradation**
   - Mitigation: Continuous benchmarking, profiling
   - Fallback: Revert via feature flags

2. **Breaking Changes**
   - Mitigation: Comprehensive test suite
   - Fallback: Parallel run old/new code

3. **Team Resistance**
   - Mitigation: Involve team early, pair programming
   - Fallback: Gradual adoption

### Rollback Strategy
- Each phase independently deployable
- Feature flags for major changes
- Database migrations reversible
- Old code maintained until new code proven

## Success Metrics

### Code Quality Metrics
- **Test Coverage**: >90% on critical paths
- **Cyclomatic Complexity**: <10 per method
- **Class Size**: <500 lines per class
- **Method Size**: <50 lines per method
- **Coupling**: <5 dependencies per class

### Performance Metrics
- **Combat Calculation**: <1ms (maintained)
- **Property Calculation**: <0.5ms (maintained)
- **Database Queries**: <10ms average
- **Memory Usage**: 20% reduction
- **Startup Time**: 30% improvement

### Development Metrics
- **Bug Discovery Rate**: 50% reduction
- **Feature Development Time**: 40% improvement
- **Code Review Time**: 30% reduction
- **Onboarding Time**: 60% reduction

## Tooling Requirements

### Development Tools
- **IDE**: Visual Studio 2022 / VS Code with C# extensions
- **Testing**: NUnit, Moq, FluentAssertions
- **Analysis**: SonarQube, ReSharper/Rider
- **Profiling**: dotMemory, dotTrace
- **Benchmarking**: BenchmarkDotNet

### CI/CD Requirements
- Automated test execution
- Code coverage reporting
- Performance benchmarking
- Static analysis
- Deployment automation

## Timeline Summary

### 32-Week Breakdown
- **Weeks 1-4**: Foundation (DI, Property Calculators, Combat)
- **Weeks 5-12**: Core Services (Progression, Effects, Items)
- **Weeks 13-20**: Entity Refactoring (ECS, Factories)
- **Weeks 21-28**: Integration (Services, Caching, Performance)
- **Weeks 29-32**: Polish (Documentation, Training)

### Milestone Deliverables
- **Month 1**: Working DI, 30% code extracted
- **Month 2**: Core services operational, 50% coverage
- **Month 3**: Major entities refactored, 70% coverage
- **Month 4**: Full service layer, 85% coverage
- **Month 5**: ECS complete, modern architecture
- **Month 6**: Static dependencies removed, 90% coverage
- **Month 7**: Performance optimized, caching implemented
- **Month 8**: Documentation complete, team trained

## Conclusion

This comprehensive refactoring plan transforms the OpenDAoC codebase from a monolithic structure to a modern, testable, maintainable architecture. By following this systematic approach, the project will achieve:

1. **Testability**: 90%+ test coverage with isolated unit tests
2. **Maintainability**: SOLID principles throughout
3. **Performance**: Maintained or improved across all metrics
4. **Flexibility**: Easy to extend and modify
5. **Team Productivity**: Faster development and debugging

The key to success is incremental progress, continuous testing, and maintaining functionality throughout the transformation. 