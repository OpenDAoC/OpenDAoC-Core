# OpenDAoC Clean Architecture Refactoring Plan v3.0

## Executive Summary

This refined plan addresses critical architectural gaps identified in v2.0, focusing on implementing proper clean architecture principles, comprehensive dependency injection, and interface-driven design while maintaining our aggressive performance targets for 10,000+ concurrent players.

## Critical Architecture Gaps Identified

### 1. **Pervasive Static Dependencies**
- `GameServer.Instance` used throughout (1,000+ references)
- Static managers and services bypassing DI
- `ServiceLocator` anti-pattern in existing code
- Direct database access from entities

### 2. **Missing Layer Separation**
- No clear domain/application/infrastructure boundaries
- Business logic embedded in entities (GameLiving: 3,798 lines)
- Data access mixed with business logic
- UI concerns in domain objects

### 3. **Inconsistent Service Patterns**
- Some services use constructor injection
- Others use static access patterns
- Mix of singleton and instance-based services
- No service lifecycle management

### 4. **Interface Design Issues**
- Fat interfaces violating ISP
- Missing interface segregation
- Concrete dependencies in core logic
- Limited mockability for testing

## Clean Architecture Layers

### Layer Structure
```
┌─────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                        │
│  (GameClient, PacketHandlers, API Controllers)                 │
├─────────────────────────────────────────────────────────────────┤
│                       Application Layer                          │
│  (Use Cases, Application Services, DTOs, Mappers)              │
├─────────────────────────────────────────────────────────────────┤
│                         Domain Layer                             │
│  (Entities, Value Objects, Domain Services, Interfaces)        │
├─────────────────────────────────────────────────────────────────┤
│                      Infrastructure Layer                        │
│  (Repositories, External Services, Persistence, Caching)       │
└─────────────────────────────────────────────────────────────────┘
```

### Dependency Rule
- Dependencies point inward only
- Domain layer has zero dependencies
- Application layer depends only on Domain
- Infrastructure and Presentation depend on Application and Domain

## Dependency Injection Architecture

### 1. **Container Selection**
```csharp
// Use Microsoft.Extensions.DependencyInjection for consistency
public class GameServerHost
{
    private IServiceProvider _serviceProvider;
    
    public void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<IGameDatabase, MySqlGameDatabase>();
        services.AddSingleton<ICache, RedisCache>();
        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        
        // Domain Services
        services.AddSingleton<ICombatService, CombatService>();
        services.AddSingleton<IPropertyCalculatorRegistry, PropertyCalculatorRegistry>();
        services.AddScoped<ICharacterProgressionService, CharacterProgressionService>();
        
        // Application Services
        services.AddScoped<ICharacterUseCase, CharacterUseCase>();
        services.AddScoped<ICombatUseCase, CombatUseCase>();
        
        // Performance-Critical Services
        services.AddSingleton<IObjectPool<AttackComponent>, AttackComponentPool>();
        services.AddSingleton<IServiceObjectStore, HighPerformanceServiceObjectStore>();
    }
}
```

### 2. **Service Lifetime Management**
```csharp
public interface IServiceLifecycle
{
    void OnServiceStart();
    void OnServiceStop();
    Task OnServiceStartAsync();
    Task OnServiceStopAsync();
}

public interface IGameService : IServiceLifecycle
{
    string ServiceName { get; }
    ServiceStatus Status { get; }
    ServicePriority Priority { get; }
}

public enum ServicePriority
{
    Critical = 0,    // Game loop, networking
    High = 1,        // Combat, movement
    Normal = 2,      // Character, inventory
    Low = 3          // Housing, cosmetics
}
```

### 3. **Performance-Optimized DI**
```csharp
// Compile-time DI for hot paths
public interface IServiceFactory<T>
{
    T Create();
    void Return(T instance);
}

// Pre-compiled delegates for performance
public class CompiledServiceFactory<T> : IServiceFactory<T>
{
    private readonly Func<T> _factory;
    private readonly ObjectPool<T> _pool;
    
    public CompiledServiceFactory(Func<T> factory)
    {
        _factory = factory;
        _pool = new DefaultObjectPool<T>(new PooledObjectPolicy<T>());
    }
    
    public T Create() => _pool.Get() ?? _factory();
    public void Return(T instance) => _pool.Return(instance);
}
```

## Interface-First Design Patterns

### 1. **Domain Interfaces**
```csharp
namespace OpenDAoC.Domain.Combat
{
    // Segregated interfaces following ISP
    public interface IAttackable
    {
        bool CanBeAttacked();
        void OnAttacked(IAttackContext context);
    }
    
    public interface IAttacker
    {
        bool CanAttack(IAttackable target);
        IAttackContext PrepareAttack(IAttackable target);
    }
    
    public interface IDamageable
    {
        void TakeDamage(IDamageContext damage);
        int CurrentHealth { get; }
        int MaxHealth { get; }
    }
    
    public interface IDefender
    {
        IDefenseResult TryDefend(IAttackContext attack);
        IDefenseCapabilities DefenseCapabilities { get; }
    }
}
```

### 2. **Application Layer Interfaces**
```csharp
namespace OpenDAoC.Application.Combat
{
    public interface ICombatUseCase
    {
        Task<AttackResultDto> ProcessAttackAsync(AttackRequestDto request);
        Task<DefenseResultDto> ProcessDefenseAsync(DefenseRequestDto request);
    }
    
    public interface ICombatNotificationService
    {
        Task NotifyAttackResult(Guid attackerId, Guid defenderId, AttackResultDto result);
        Task NotifyDefenseResult(Guid defenderId, DefenseResultDto result);
    }
}
```

### 3. **Infrastructure Interfaces**
```csharp
namespace OpenDAoC.Infrastructure.Persistence
{
    public interface IUnitOfWork : IDisposable
    {
        ICharacterRepository Characters { get; }
        IItemRepository Items { get; }
        IGuildRepository Guilds { get; }
        Task<int> SaveChangesAsync();
        void BeginTransaction();
        void Commit();
        void Rollback();
    }
    
    public interface IRepository<T> where T : class, IAggregateRoot
    {
        Task<T> GetByIdAsync(Guid id);
        Task<IReadOnlyList<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}
```

## Migration Strategy with Zero Downtime

### Phase 1: Foundation Layer (Weeks 1-4)

#### Week 1: DI Infrastructure
```csharp
// Step 1: Create adapter for existing static calls
public interface ILegacyGameServer
{
    IObjectDatabase Database { get; }
    WorldManager WorldManager { get; }
    // ... other legacy properties
}

public class LegacyGameServerAdapter : ILegacyGameServer
{
    public IObjectDatabase Database => GameServer.Database;
    public WorldManager WorldManager => GameServer.Instance.WorldManager;
}

// Step 2: Gradually replace static calls
// Before:
var player = GameServer.Database.FindObjectByKey<DbCharacter>(id);

// Intermediate:
var player = _legacyServer.Database.FindObjectByKey<DbCharacter>(id);

// Final:
var player = await _characterRepository.GetByIdAsync(id);
```

#### Week 2: Interface Extraction
```csharp
// Extract interfaces from existing classes
public interface IGameLiving : IGameObject, IAttackable, IDefender, IDamageable
{
    int Level { get; }
    IStats Stats { get; }
    IEffectList ActiveEffects { get; }
}

// Create adapters for existing implementations
public class GameLivingAdapter : IGameLiving
{
    private readonly GameLiving _legacy;
    
    public GameLivingAdapter(GameLiving legacy)
    {
        _legacy = legacy;
    }
    
    // Delegate to legacy implementation
    public int Level => _legacy.Level;
    public IStats Stats => new StatsAdapter(_legacy);
}
```

### Phase 2: Service Extraction (Weeks 5-12)

#### Combat System Refactoring
```csharp
// Domain Service Interface
public interface ICombatService
{
    IAttackResult ProcessAttack(IAttackContext context);
    IDamageResult CalculateDamage(IAttackData data);
}

// Application Service
public interface ICombatApplicationService
{
    Task<CombatResultDto> ExecuteCombatRoundAsync(CombatRequestDto request);
}

// Infrastructure Service
public interface ICombatPersistenceService
{
    Task SaveCombatLogAsync(CombatLog log);
    Task<IReadOnlyList<CombatLog>> GetCombatHistoryAsync(Guid characterId);
}
```

### Phase 3: Performance Optimization (Weeks 13-20)

#### High-Performance Service Registration
```csharp
public static class PerformanceOptimizedServices
{
    public static IServiceCollection AddHighPerformanceServices(this IServiceCollection services)
    {
        // Use compiled expressions for hot paths
        services.AddSingleton<Func<IAttackContext>>(provider =>
        {
            var compiled = Expression.Lambda<Func<IAttackContext>>(
                Expression.New(typeof(AttackContext))
            ).Compile();
            return compiled;
        });
        
        // Pre-allocate service arrays for game loop
        services.AddSingleton<IServiceArray<ICombatComponent>>(provider =>
        {
            return new PreAllocatedServiceArray<ICombatComponent>(
                capacity: ServerProperties.Properties.MAX_ENTITIES
            );
        });
        
        // Use lock-free collections for concurrent access
        services.AddSingleton<IConcurrentServiceCache>(provider =>
        {
            return new LockFreeServiceCache();
        });
        
        return services;
    }
}
```

#### Zero-Allocation Patterns
```csharp
// Use struct-based contexts for zero allocation
public readonly struct CombatContext
{
    public readonly int AttackerId;
    public readonly int DefenderId;
    public readonly CombatType Type;
    public readonly long Timestamp;
    
    public CombatContext(int attackerId, int defenderId, CombatType type)
    {
        AttackerId = attackerId;
        DefenderId = defenderId;
        Type = type;
        Timestamp = GameLoop.GameLoopTime;
    }
}

// Pool all heap-allocated objects
public class CombatService : ICombatService
{
    private readonly ObjectPool<AttackResult> _resultPool;
    private readonly ObjectPool<DamageCalculation> _calcPool;
    
    public IAttackResult ProcessAttack(IAttackContext context)
    {
        var calc = _calcPool.Get();
        try
        {
            // Use pooled object
            calc.Calculate(context);
            
            var result = _resultPool.Get();
            result.Initialize(calc);
            return result;
        }
        finally
        {
            _calcPool.Return(calc);
        }
    }
}
```

### Phase 4: Clean Architecture Completion (Weeks 21-32)

#### Domain Event System
```csharp
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent);
    void RegisterHandler<TEvent>(IDomainEventHandler<TEvent> handler) 
        where TEvent : IDomainEvent;
}

// High-performance event bus
public class HighPerformanceEventBus : IDomainEventDispatcher
{
    private readonly ConcurrentDictionary<Type, List<object>> _handlers;
    private readonly Channel<IDomainEvent> _eventChannel;
    
    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        // Non-blocking write
        await _eventChannel.Writer.WriteAsync(domainEvent);
    }
}
```

## Testing Strategy with DI

### Unit Test Architecture
```csharp
[TestFixture]
public class CombatServiceTests
{
    private IServiceProvider _serviceProvider;
    private ICombatService _combatService;
    
    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Register test doubles
        services.AddSingleton<IPropertyCalculatorRegistry, MockPropertyRegistry>();
        services.AddSingleton<IDamageCalculator, StubDamageCalculator>();
        services.AddSingleton<ICombatService, CombatService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _combatService = _serviceProvider.GetRequiredService<ICombatService>();
    }
    
    [Test]
    public void ProcessAttack_ShouldCalculateCorrectDamage_WhenValidAttack()
    {
        // Pure unit test with injected dependencies
    }
}
```

### Integration Test Architecture
```csharp
public class IntegrationTestBase
{
    protected IServiceProvider ServiceProvider { get; private set; }
    
    [OneTimeSetUp]
    public void BaseSetup()
    {
        var services = new ServiceCollection();
        
        // Use test database
        services.AddDbContext<GameDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
            
        // Register real services
        services.AddGameServices(configuration =>
        {
            configuration.UseTestEnvironment();
        });
        
        ServiceProvider = services.BuildServiceProvider();
    }
}
```

## Performance Monitoring Integration

### Service Performance Metrics
```csharp
public interface IPerformanceMonitor
{
    void RecordServiceCall(string serviceName, long elapsedTicks);
    void RecordAllocation(string serviceName, long bytes);
    IPerformanceReport GenerateReport();
}

public class ServicePerformanceInterceptor : IInterceptor
{
    private readonly IPerformanceMonitor _monitor;
    
    public void Intercept(IInvocation invocation)
    {
        var sw = Stopwatch.StartNew();
        var before = GC.GetTotalMemory(false);
        
        invocation.Proceed();
        
        var after = GC.GetTotalMemory(false);
        _monitor.RecordServiceCall(invocation.Method.Name, sw.ElapsedTicks);
        _monitor.RecordAllocation(invocation.Method.Name, after - before);
    }
}
```

## Success Metrics

### Architecture Quality
- **Dependency Direction**: 100% inward-pointing dependencies
- **Interface Coverage**: 95% of public APIs behind interfaces
- **Layer Isolation**: Zero cross-layer violations
- **DI Coverage**: 100% constructor injection (no service locator)

### Performance Targets
- **Service Resolution**: <100ns for hot path services
- **Memory Overhead**: <5% from DI container
- **Combat Calculation**: <0.5ms with full DI
- **Zero Allocations**: In game loop critical path

### Code Quality
- **Cyclomatic Complexity**: <7 per method
- **Interface Segregation**: <5 methods per interface
- **Test Coverage**: >90% for domain/application layers
- **Mock Usage**: 100% interface-based mocking

## Risk Mitigation

### Performance Risks
- **Mitigation**: Compile-time DI for hot paths
- **Monitoring**: Real-time performance dashboards
- **Fallback**: Direct instantiation for critical paths

### Migration Risks
- **Mitigation**: Adapter pattern for gradual migration
- **Testing**: Parallel run with legacy system
- **Rollback**: Feature flags for instant reversion

This refined plan ensures we achieve clean architecture with proper dependency injection while maintaining our aggressive performance targets for massive multiplayer scalability. 