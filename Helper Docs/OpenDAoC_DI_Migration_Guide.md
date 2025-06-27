# OpenDAoC Dependency Injection Migration Guide

## Overview

This guide provides step-by-step instructions for migrating from OpenDAoC's legacy static dependency patterns to the new clean architecture with dependency injection. This migration enables better testability, maintainability, and performance while preserving the authentic DAoC gameplay experience.

## Migration Strategy

### Phase 1: Foundation Setup ✅
- [x] Dependency injection container setup
- [x] Service lifecycle management
- [x] Legacy adapters for backward compatibility
- [x] Object pooling infrastructure

### Phase 2: Service Layer Migration (In Progress)
- [ ] Extract business logic to services
- [ ] Interface-driven design
- [ ] Remove static dependencies
- [ ] Comprehensive testing

## Core Concepts

### Before: Static Dependencies
```csharp
// ❌ OLD WAY - Static dependencies
public class SomeClass
{
    public void DoSomething()
    {
        var player = GameServer.Instance.PlayerManager.GetPlayerByName("test");
        var data = GameServer.Database.SelectObject<DbPlayer>(p => p.Name == "test");
        var worldMgr = GameServer.Instance.WorldManager;
    }
}
```

### After: Dependency Injection
```csharp
// ✅ NEW WAY - Constructor injection
public class SomeService : ISomeService
{
    private readonly IPlayerManager _playerManager;
    private readonly IObjectDatabase _database;
    private readonly IWorldManager _worldManager;
    private readonly ILogger<SomeService> _logger;

    public SomeService(
        IPlayerManager playerManager,
        IObjectDatabase database,
        IWorldManager worldManager,
        ILogger<SomeService> logger)
    {
        _playerManager = playerManager;
        _database = database;
        _worldManager = worldManager;
        _logger = logger;
    }

    public void DoSomething()
    {
        var player = _playerManager.GetPlayerByName("test");
        var data = _database.SelectObject<DbPlayer>(p => p.Name == "test");
        // worldManager operations...
    }
}
```

## Step-by-Step Migration Process

### Step 1: Create Interface
```csharp
// Define the interface first
public interface ISomeService
{
    void DoSomething();
    Task DoSomethingAsync();
}
```

### Step 2: Implement Service
```csharp
// Implement with constructor injection
public class SomeService : ServiceLifecycleBase, ISomeService
{
    private readonly IPlayerManager _playerManager;
    private readonly ILogger<SomeService> _logger;

    public SomeService(
        IPlayerManager playerManager,
        ILogger<SomeService> logger) 
        : base("SomeService", ServicePriority.Normal)
    {
        _playerManager = playerManager;
        _logger = logger;
    }

    protected override Task OnStartAsync()
    {
        _logger.LogInformation("SomeService started");
        return Task.CompletedTask;
    }

    protected override Task OnStopAsync()
    {
        _logger.LogInformation("SomeService stopped");
        return Task.CompletedTask;
    }

    public void DoSomething()
    {
        // Implementation here
    }

    public async Task DoSomethingAsync()
    {
        // Async implementation here
        await Task.CompletedTask;
    }
}
```

### Step 3: Register Service
```csharp
// In service registration (ServiceRegistration.cs)
public static class SomeServiceRegistration
{
    public static IServiceCollection AddSomeService(this IServiceCollection services)
    {
        services.AddSingleton<ISomeService, SomeService>();
        return services;
    }
}

// In GameServerHostBuilder
hostBuilder.ConfigureServices(services =>
{
    services.AddSomeService();
});
```

### Step 4: Create Adapter (Transition Period)
```csharp
// Legacy adapter for backward compatibility
public class SomeServiceAdapter
{
    private static ISomeService Instance => 
        GameServer.Instance.GetService<ISomeService>();

    public static void DoSomething()
    {
        Instance?.DoSomething();
    }

    public static async Task DoSomethingAsync()
    {
        if (Instance != null)
            await Instance.DoSomethingAsync();
    }
}
```

### Step 5: Gradual Replacement
```csharp
// Phase 1: Replace static calls with adapter
// OLD: SomeStaticClass.DoSomething();
// NEW: SomeServiceAdapter.DoSomething();

// Phase 2: Inject service directly where possible
public class ModernClass
{
    private readonly ISomeService _someService;
    
    public ModernClass(ISomeService someService)
    {
        _someService = someService;
    }
    
    public void UseService()
    {
        _someService.DoSomething(); // Direct injection
    }
}

// Phase 3: Remove adapter when all usages migrated
```

## Common Migration Patterns

### 1. Manager Classes
```csharp
// BEFORE: Static manager
public static class PlayerManager
{
    public static GamePlayer GetPlayerByName(string name) { }
}

// AFTER: Injected service
public interface IPlayerManager : IServiceLifecycle
{
    GamePlayer GetPlayerByName(string name);
    Task<GamePlayer> GetPlayerByNameAsync(string name);
}

public class PlayerManager : ServiceLifecycleBase, IPlayerManager
{
    public PlayerManager() : base("PlayerManager", ServicePriority.High) { }
    
    public GamePlayer GetPlayerByName(string name)
    {
        // Implementation
    }
    
    public async Task<GamePlayer> GetPlayerByNameAsync(string name)
    {
        // Async implementation
        return await Task.FromResult(GetPlayerByName(name));
    }
}
```

### 2. Database Access
```csharp
// BEFORE: Direct static access
var data = GameServer.Database.SelectObjects<DbItem>();

// AFTER: Repository pattern
public interface IItemRepository
{
    Task<IEnumerable<DbItem>> GetAllAsync();
    Task<DbItem> GetByIdAsync(string id);
    Task SaveAsync(DbItem item);
}

public class ItemRepository : IItemRepository
{
    private readonly IObjectDatabase _database;
    
    public ItemRepository(IObjectDatabase database)
    {
        _database = database;
    }
    
    public async Task<IEnumerable<DbItem>> GetAllAsync()
    {
        return await Task.FromResult(_database.SelectAllObjects<DbItem>());
    }
}
```

### 3. Configuration Access
```csharp
// BEFORE: Static properties
var value = Properties.SOME_SETTING;

// AFTER: Configuration service
public interface IGameConfiguration
{
    T GetValue<T>(string key);
    bool SomeSetting { get; }
}

public class GameConfiguration : IGameConfiguration
{
    public T GetValue<T>(string key)
    {
        // Implementation using Properties or IConfiguration
    }
    
    public bool SomeSetting => GetValue<bool>("SOME_SETTING");
}
```

## Testing with Dependency Injection

### Unit Testing
```csharp
[TestFixture]
public class SomeServiceTests
{
    private IServiceProvider _serviceProvider;
    private ISomeService _someService;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // Register test doubles
        services.AddSingleton<IPlayerManager, MockPlayerManager>();
        services.AddSingleton<IObjectDatabase, MockDatabase>();
        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton<ISomeService, SomeService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _someService = _serviceProvider.GetRequiredService<ISomeService>();
    }

    [Test]
    public void DoSomething_ShouldWork_WhenCalled()
    {
        // Arrange
        // Test setup

        // Act
        _someService.DoSomething();

        // Assert
        // Verify expectations
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider?.Dispose();
    }
}
```

### Integration Testing
```csharp
[TestFixture]
public class SomeServiceIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task DoSomethingAsync_ShouldIntegrateCorrectly()
    {
        // Arrange
        var service = ServiceProvider.GetRequiredService<ISomeService>();

        // Act
        await service.DoSomethingAsync();

        // Assert
        // Verify integration behavior
    }
}
```

## Performance Considerations

### Object Pooling
```csharp
public class PerformanceCriticalService
{
    private readonly ObjectPool<AttackContext> _attackPool;
    private readonly ObjectPool<DamageCalculation> _damagePool;

    public PerformanceCriticalService(
        ObjectPool<AttackContext> attackPool,
        ObjectPool<DamageCalculation> damagePool)
    {
        _attackPool = attackPool;
        _damagePool = damagePool;
    }

    public void ProcessAttack()
    {
        var context = _attackPool.Get();
        var damage = _damagePool.Get();
        
        try
        {
            // Use pooled objects - zero allocations
            ProcessAttackLogic(context, damage);
        }
        finally
        {
            // Always return to pools
            _attackPool.Return(context);
            _damagePool.Return(damage);
        }
    }
}
```

### Compiled Delegates for Hot Paths
```csharp
// For performance-critical services
services.AddSingleton<Func<ICombatService>>(provider =>
{
    // Pre-compiled factory delegate
    var compiledFactory = Expression.Lambda<Func<ICombatService>>(
        Expression.New(typeof(CombatService))
    ).Compile();
    
    return compiledFactory;
});
```

## Error Handling and Logging

### Structured Logging
```csharp
public class SomeService : ISomeService
{
    private readonly ILogger<SomeService> _logger;

    public async Task DoSomethingAsync(string playerId)
    {
        using var scope = _logger.BeginScope("Operation:DoSomething PlayerId:{PlayerId}", playerId);
        
        try
        {
            _logger.LogInformation("Starting operation for player {PlayerId}", playerId);
            
            // Operation logic
            
            _logger.LogInformation("Completed operation successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete operation for player {PlayerId}", playerId);
            throw;
        }
    }
}
```

### Service-Specific Exceptions
```csharp
public class PlayerServiceException : Exception
{
    public string PlayerId { get; }
    
    public PlayerServiceException(string playerId, string message) : base(message)
    {
        PlayerId = playerId;
    }
    
    public PlayerServiceException(string playerId, string message, Exception innerException) 
        : base(message, innerException)
    {
        PlayerId = playerId;
    }
}
```

## Migration Checklist

### For Each Service Migration:
- [ ] Create interface with async variants
- [ ] Implement service with constructor injection
- [ ] Add to service registration
- [ ] Create adapter for transition period
- [ ] Write unit tests
- [ ] Write integration tests
- [ ] Update all callers to use DI
- [ ] Remove adapter
- [ ] Add performance tests if critical path

### For Each Static Dependency:
- [ ] Identify all usage locations
- [ ] Create service interface
- [ ] Implement service
- [ ] Register with DI container
- [ ] Update all call sites
- [ ] Add logging and error handling
- [ ] Test thoroughly

## Best Practices

### 1. Interface Design
- Keep interfaces focused and small (ISP)
- Provide both sync and async variants where appropriate
- Use generic constraints for type safety
- Document expected behavior

### 2. Service Implementation
- Inherit from ServiceLifecycleBase for lifecycle management
- Use constructor injection exclusively
- Implement proper async patterns
- Add comprehensive logging

### 3. Registration
- Group related services in extension methods
- Use appropriate lifetimes (Singleton, Scoped, Transient)
- Register interfaces, not concrete types
- Validate dependencies at startup

### 4. Testing
- Use test doubles for all dependencies
- Test service contracts, not implementations
- Include integration tests for cross-service behavior
- Performance test critical paths

### 5. Performance
- Use object pooling for frequently allocated objects
- Minimize service resolution in hot paths
- Consider compiled delegates for ultra-hot paths
- Profile before and after migration

## Troubleshooting

### Common Issues

1. **Circular Dependencies**
   ```csharp
   // Problem: A depends on B, B depends on A
   
   // Solution: Extract shared interface or use mediator pattern
   public interface ISharedService { }
   public class SharedService : ISharedService { }
   ```

2. **Service Not Found**
   ```csharp
   // Problem: Service not registered
   
   // Solution: Add to registration
   services.AddSingleton<IMyService, MyService>();
   ```

3. **Lifetime Issues**
   ```csharp
   // Problem: Singleton depending on Scoped service
   
   // Solution: Use factory pattern
   services.AddSingleton<Func<IScopedService>>(provider => 
       () => provider.GetRequiredService<IScopedService>());
   ```

### Debugging DI Issues

```csharp
// Add logging to understand service resolution
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

// Validate service registration
var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true
});
```

## Migration Progress Tracking

### Week 1 Tasks:
- [x] FDIS-001: DI container selection
- [x] FDIS-002: GameServerHost creation
- [x] FDIS-003: Service lifecycle interfaces
- [x] FDIS-004: Service priority management
- [x] FDIS-005: Legacy adapter interfaces
- [x] FDIS-006: Legacy adapter implementation
- [x] FDIS-007: Service registration setup
- [x] FDIS-008: Performance-optimized factories
- [x] FDIS-009: Compiled delegate factories
- [x] FDIS-010: Object pooling infrastructure
- [x] FDIS-011: Service lifetime management
- [x] FDIS-012: Service orchestration
- [x] FDIS-013: GameServer DI integration
- [x] FDIS-014: Migration guide (this document)
- [ ] FDIS-015: DI performance benchmarks

### Next Steps:
Continue with Week 2 interface extraction tasks...

## Support and Questions

For questions about the migration process:
1. Check this guide first
2. Review the architectural documentation
3. Examine existing migrated services as examples
4. Consult the development team

Remember: This migration improves architecture while maintaining performance and the authentic DAoC experience. 