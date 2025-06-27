# OpenDAoC Architecture Alignment Guide

## Purpose

This guide ensures all refactoring efforts align with our core architectural principles while maintaining aggressive performance targets. Every code change must satisfy both architectural cleanliness AND performance requirements.

## Core Architectural Principles

### 1. Clean Architecture Layers
```
┌─────────────────────────────────────────────────────────────────┐
│ Presentation │ Packets, Commands, API Controllers              │
├─────────────────────────────────────────────────────────────────┤
│ Application  │ Use Cases, DTOs, Application Services           │
├─────────────────────────────────────────────────────────────────┤
│ Domain       │ Entities, Value Objects, Domain Services       │
├─────────────────────────────────────────────────────────────────┤
│ Infrastructure │ DB, Cache, Network, External Services        │
└─────────────────────────────────────────────────────────────────┘
```

**Dependency Rule**: Dependencies only point inward. Domain has zero dependencies.

### 2. Interface-First Design
Every public API must have an interface:
```csharp
// ❌ BAD: Direct implementation
public class CombatService
{
    public AttackResult ProcessAttack(attacker, defender) { }
}

// ✅ GOOD: Interface-first
public interface ICombatService
{
    AttackResult ProcessAttack(IAttacker attacker, IDefender defender);
}

public class CombatService : ICombatService { }
```

### 3. Dependency Injection
Constructor injection for ALL dependencies:
```csharp
// ❌ BAD: Static dependencies
public class CharacterService
{
    public void Save(Character c)
    {
        GameServer.Database.Save(c); // Static dependency
    }
}

// ✅ GOOD: Injected dependencies
public class CharacterService
{
    private readonly ICharacterRepository _repository;
    
    public CharacterService(ICharacterRepository repository)
    {
        _repository = repository;
    }
}
```

## Performance-Aware Architecture Patterns

### 1. Zero-Allocation Service Design
```csharp
public interface ICombatService
{
    // ❌ BAD: Allocates result object
    AttackResult ProcessAttack(IAttacker attacker, IDefender defender);
    
    // ✅ GOOD: Uses pooled result
    void ProcessAttack(IAttacker attacker, IDefender defender, AttackResult result);
    
    // ✅ BETTER: Struct-based for stack allocation
    CombatOutcome ProcessAttackFast(in CombatContext context);
}

public readonly struct CombatOutcome
{
    public readonly bool Hit;
    public readonly int Damage;
    public readonly CombatFlags Flags;
}
```

### 2. Performance-Optimized DI
```csharp
// For hot paths, use compiled delegates
public class ServiceContainer
{
    private readonly Dictionary<Type, Func<object>> _factories = new();
    
    public void RegisterCompiled<TInterface, TImplementation>()
    {
        // Compile expression tree for fastest instantiation
        var ctor = typeof(TImplementation).GetConstructor(Type.EmptyTypes);
        var lambda = Expression.Lambda<Func<object>>(
            Expression.New(ctor)
        ).Compile();
        
        _factories[typeof(TInterface)] = lambda;
    }
}
```

### 3. Interface Segregation for Performance
```csharp
// Split interfaces by usage frequency
public interface ICharacterCore // Hot path
{
    int Level { get; }
    IStats Stats { get; }
}

public interface ICharacterDetails // Cold path
{
    string Biography { get; }
    DateTime Created { get; }
}

public interface ICharacter : ICharacterCore, ICharacterDetails { }
```

## Architecture Decision Records (ADRs)

### ADR-001: Use Microsoft.Extensions.DependencyInjection
**Status**: Accepted  
**Context**: Need DI container with good performance  
**Decision**: Use MS.Extensions.DI for consistency with .NET ecosystem  
**Consequences**: 
- ✅ Well-tested, performant
- ✅ Integrates with ASP.NET Core
- ❌ Less features than Autofac
- **Performance**: <100ns resolution for singletons

### ADR-002: Struct-Based DTOs for Hot Paths
**Status**: Accepted  
**Context**: Need zero-allocation data transfer  
**Decision**: Use readonly structs for combat/movement DTOs  
**Consequences**:
- ✅ Zero heap allocations
- ✅ Better cache locality
- ❌ No inheritance
- **Performance**: Eliminates GC pressure

### ADR-003: CQRS for Read/Write Separation
**Status**: Accepted  
**Context**: Different performance needs for reads vs writes  
**Decision**: Implement CQRS pattern  
**Consequences**:
- ✅ Optimize reads separately
- ✅ Enable read replicas
- ❌ More complexity
- **Performance**: 10x read throughput

## Code Review Checklist

### Architecture Compliance
- [ ] **Layer Rule**: No outward dependencies
- [ ] **Interface Coverage**: All public APIs have interfaces
- [ ] **DI Pattern**: Constructor injection only
- [ ] **No Statics**: Zero static dependencies
- [ ] **SOLID Principles**: All satisfied

### Performance Compliance
- [ ] **Allocation Budget**: Zero allocations in hot paths
- [ ] **Object Pooling**: Reuse heavy objects
- [ ] **Async/Await**: Proper usage, no blocking
- [ ] **Lock-Free**: Where possible
- [ ] **Benchmarked**: Performance measured

### Interface Design
- [ ] **Segregated**: Small, focused interfaces
- [ ] **Mockable**: Can create test doubles
- [ ] **Versioned**: Consider future changes
- [ ] **Documented**: Clear contracts
- [ ] **Consistent**: Follow naming conventions

## Migration Patterns

### 1. Strangler Fig Pattern
```csharp
// Step 1: Create new interface
public interface INewCombatService { }

// Step 2: Adapter wraps old implementation
public class CombatAdapter : INewCombatService
{
    private readonly OldCombatSystem _old;
    
    public AttackResult ProcessAttack(...)
    {
        // Delegate to old system
        return _old.DoOldAttack(...);
    }
}

// Step 3: Gradually move logic to new service
// Step 4: Remove adapter when complete
```

### 2. Branch by Abstraction
```csharp
// Create abstraction point
public interface IPropertyCalculator
{
    int Calculate(IPropertySource source, eProperty property);
}

// Toggle between implementations
services.AddSingleton<IPropertyCalculator>(() =>
{
    return Features.UseNewCalculator 
        ? new OptimizedPropertyCalculator()
        : new LegacyPropertyCalculator();
});
```

## Performance Testing Requirements

### Every Service Must Have:
1. **Unit Tests**: Functional correctness
2. **Integration Tests**: Cross-system behavior
3. **Performance Tests**: Speed and allocation benchmarks
4. **Load Tests**: Behavior under stress

### Benchmark Example:
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60)]
public class CombatServiceBenchmarks
{
    private ICombatService _service;
    
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddCombatServices();
        var provider = services.BuildServiceProvider();
        _service = provider.GetRequiredService<ICombatService>();
    }
    
    [Benchmark]
    public void ProcessAttack()
    {
        _service.ProcessAttack(_attacker, _defender, _result);
    }
}
```

## Common Anti-Patterns to Avoid

### 1. Service Locator
```csharp
// ❌ NEVER DO THIS
public class BadService
{
    public void DoWork()
    {
        var db = ServiceLocator.Get<IDatabase>(); // Hidden dependency
    }
}
```

### 2. Leaky Abstractions
```csharp
// ❌ BAD: Exposes implementation details
public interface IBadRepository
{
    SqlConnection GetConnection(); // Leaks SQL dependency
}

// ✅ GOOD: Clean abstraction
public interface IGoodRepository
{
    Task<Character> GetByIdAsync(Guid id);
}
```

### 3. Anemic Domain Model
```csharp
// ❌ BAD: No behavior
public class Character
{
    public int Level { get; set; }
    public int Experience { get; set; }
}

// ✅ GOOD: Rich domain model
public class Character
{
    public void GrantExperience(int amount)
    {
        Experience += amount;
        CheckLevelUp();
    }
}
```

## Monitoring and Metrics

### Required Metrics for Each Service:
```csharp
public class InstrumentedCombatService : ICombatService
{
    private readonly ICombatService _inner;
    private readonly IMetrics _metrics;
    
    public AttackResult ProcessAttack(IAttacker attacker, IDefender defender)
    {
        using (_metrics.Measure.Timer.Time("combat.process_attack"))
        {
            var result = _inner.ProcessAttack(attacker, defender);
            
            _metrics.Measure.Counter.Increment("combat.attacks.total");
            if (result.Hit)
                _metrics.Measure.Counter.Increment("combat.attacks.hit");
                
            return result;
        }
    }
}
```

## Continuous Validation

### Automated Architecture Tests:
```csharp
[Test]
public void Domain_ShouldNotDependOn_Infrastructure()
{
    var domainAssembly = typeof(Character).Assembly;
    var infraAssembly = typeof(MySqlRepository).Assembly;
    
    domainAssembly.Should().NotReference(infraAssembly);
}

[Test]
public void AllServices_ShouldHaveInterfaces()
{
    var services = typeof(CombatService).Assembly
        .GetTypes()
        .Where(t => t.Name.EndsWith("Service"));
        
    foreach (var service in services)
    {
        service.GetInterfaces()
            .Should().Contain(i => i.Name == $"I{service.Name}");
    }
}
```

## Success Criteria

### Architecture Quality Gates:
- **Interface Coverage**: >95% of public APIs
- **DI Coverage**: 100% (no service locator)
- **Layer Violations**: 0
- **Cyclomatic Complexity**: <7
- **Test Coverage**: >90%

### Performance Quality Gates:
- **Combat Calculation**: <0.5ms p99
- **Property Calculation**: <0.1ms with cache
- **Service Resolution**: <100ns
- **Memory Allocation**: 0 in hot paths
- **GC Pressure**: <0.1 Gen2/sec

## Final Checklist

Before ANY merge to main:
- [ ] Passes all architecture tests
- [ ] Meets performance benchmarks
- [ ] Has proper interfaces
- [ ] Uses dependency injection
- [ ] Follows layer boundaries
- [ ] Includes unit tests
- [ ] Includes performance tests
- [ ] Updates documentation
- [ ] Reviewed by architect
- [ ] Metrics dashboard updated

Remember: **Clean architecture enables performance, not hinders it.** Every architectural decision should make the code both cleaner AND faster. 