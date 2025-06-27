# Service Management System

**Document Status:** Core architecture documented  
**Verification:** Code-verified from ECS service implementations  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Service management coordinates all the behind-the-scenes systems that make the game run smoothly. This includes managing combat calculations, spell effects, movement, and other core systems to ensure everything happens at the right time with optimal performance. Players benefit from lag-free gameplay and responsive actions.

The Service Management System coordinates all Entity Component System (ECS) services, managing their lifecycle, performance monitoring, and inter-service communication. This system forms the backbone of OpenDAoC's high-performance architecture.

## Core Architecture

### Service Base Interface
```csharp
public interface IGameService
{
    void Initialize();
    void Start();
    void Stop();
    void Update(long tick);
}

public abstract class GameService : IGameService
{
    protected bool _isRunning = false;
    protected long _lastUpdateTick = 0;
    protected int _updateInterval = 10; // Default 10ms
    
    public virtual void Initialize() { }
    public virtual void Start() { _isRunning = true; }
    public virtual void Stop() { _isRunning = false; }
    public abstract void Update(long tick);
}
```

### Service Object Management
```csharp
public interface IServiceObject
{
    ServiceObjectId ServiceObjectId { get; set; }
}

public class ServiceObjectId
{
    public int Index { get; }
    public ServiceObjectType Type { get; }
    public long CreationTick { get; }
    
    public ServiceObjectId(ServiceObjectType type)
    {
        Type = type;
        CreationTick = GameLoop.GameLoopTime;
        Index = ServiceObjectStore.GetNextIndex(type);
    }
}

public enum ServiceObjectType
{
    AttackComponent,
    CastingComponent,
    EffectListComponent,
    MovementComponent,
    CraftComponent,
    Timer,
    Client
}
```

## Core Game Services

### Attack Service
```csharp
public static class AttackService
{
    private static readonly ComponentArray<AttackComponent> _components = new(2048);
    
    public static void Tick()
    {
        var components = ServiceObjectStore.UpdateAndGetAll<AttackComponent>(
            ServiceObjectType.AttackComponent, out int lastValidIndex);
            
        for (int i = 0; i <= lastValidIndex; i++)
        {
            var component = components[i];
            if (component?.Owner?.ObjectState == eObjectState.Active)
            {
                ProcessAttackComponent(component);
            }
        }
    }
    
    private static void ProcessAttackComponent(AttackComponent component)
    {
        if (ServiceUtils.ShouldTick(component.NextTick))
        {
            AttackComponent.ProcessAttack(component);
        }
    }
}
```

### Casting Service
```csharp
public static class CastingService
{
    private static readonly ComponentArray<CastingComponent> _components = new(2048);
    
    public static void Tick()
    {
        var components = ServiceObjectStore.UpdateAndGetAll<CastingComponent>(
            ServiceObjectType.CastingComponent, out int lastValidIndex);
            
        for (int i = 0; i <= lastValidIndex; i++)
        {
            var component = components[i];
            if (component?.Owner?.ObjectState == eObjectState.Active)
            {
                ProcessCastingComponent(component);
            }
        }
    }
}
```

### Effect List Service
```csharp
public static class EffectListService
{
    public static void Tick()
    {
        var components = ServiceObjectStore.UpdateAndGetAll<EffectListComponent>(
            ServiceObjectType.EffectListComponent, out int lastValidIndex);
            
        for (int i = 0; i <= lastValidIndex; i++)
        {
            var component = components[i];
            if (component?.Owner?.ObjectState == eObjectState.Active)
            {
                ProcessEffects(component);
            }
        }
    }
    
    private static void ProcessEffects(EffectListComponent effectList)
    {
        // Process all active effects
        for (int i = effectList.Effects.Count - 1; i >= 0; i--)
        {
            var effect = effectList.Effects[i];
            if (effect.HasExpired)
            {
                effectList.RemoveEffect(effect);
            }
            else
            {
                effect.Tick();
            }
        }
    }
}
```

## Service Object Store

### High-Performance Object Management
```csharp
public static class ServiceObjectStore
{
    private static readonly Dictionary<ServiceObjectType, IServiceObjectContainer> _containers = new();
    private static readonly ReaderWriterLockSlim _lock = new();
    
    public static bool Add<T>(T serviceObject) where T : class, IServiceObject
    {
        _lock.EnterWriteLock();
        try
        {
            var container = GetContainer<T>(serviceObject.ServiceObjectId.Type);
            return container.Add(serviceObject);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    public static List<T> UpdateAndGetAll<T>(ServiceObjectType type, out int lastValidIndex) 
        where T : class, IServiceObject
    {
        _lock.EnterReadLock();
        try
        {
            var container = GetContainer<T>(type);
            return container.UpdateAndGetAll(out lastValidIndex);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}

public class ServiceObjectContainer<T> : IServiceObjectContainer where T : class, IServiceObject
{
    private readonly T[] _objects;
    private readonly bool[] _activeSlots;
    private int _count = 0;
    
    public ServiceObjectContainer(int capacity)
    {
        _objects = new T[capacity];
        _activeSlots = new bool[capacity];
    }
    
    public bool Add(T obj)
    {
        for (int i = 0; i < _objects.Length; i++)
        {
            if (!_activeSlots[i])
            {
                _objects[i] = obj;
                _activeSlots[i] = true;
                obj.ServiceObjectId.Index = i;
                _count++;
                return true;
            }
        }
        return false; // Container full
    }
}
```

## Service Intervals and Timing

### Service Update Frequencies
```csharp
public static class ServiceIntervals
{
    public const int ATTACK_SERVICE = 10;      // Every tick (10ms)
    public const int CASTING_SERVICE = 10;     // Every tick
    public const int EFFECT_SERVICE = 10;      // Every tick
    public const int MOVEMENT_SERVICE = 10;    // Every tick
    public const int ZONE_SERVICE = 100;       // Every 10 ticks (100ms)
    public const int CRAFTING_SERVICE = 100;   // Every 10 ticks
    public const int REAPER_SERVICE = 1000;    // Every 100 ticks (1s)
    public const int CLIENT_SERVICE = 50;      // Every 5 ticks (50ms)
}
```

### Game Loop Integration
```csharp
public static class GameLoopService
{
    private static readonly Dictionary<Type, IGameService> _services = new();
    private static long _currentTick = 0;
    
    public static void Tick()
    {
        _currentTick = GameLoop.GameLoopTime;
        
        // Update services based on their intervals
        if (ShouldUpdateService(typeof(AttackService), ServiceIntervals.ATTACK_SERVICE))
            AttackService.Tick();
            
        if (ShouldUpdateService(typeof(CastingService), ServiceIntervals.CASTING_SERVICE))
            CastingService.Tick();
            
        if (ShouldUpdateService(typeof(EffectListService), ServiceIntervals.EFFECT_SERVICE))
            EffectListService.Tick();
            
        if (ShouldUpdateService(typeof(MovementService), ServiceIntervals.MOVEMENT_SERVICE))
            MovementService.Tick();
            
        if (ShouldUpdateService(typeof(ClientService), ServiceIntervals.CLIENT_SERVICE))
            ClientService.Tick();
            
        // Less frequent services
        if (ShouldUpdateService(typeof(ZoneService), ServiceIntervals.ZONE_SERVICE))
            ZoneService.Tick();
            
        if (ShouldUpdateService(typeof(CraftingService), ServiceIntervals.CRAFTING_SERVICE))
            CraftingService.Tick();
            
        if (ShouldUpdateService(typeof(ReaperService), ServiceIntervals.REAPER_SERVICE))
            ReaperService.Tick();
    }
}
```

## Performance Monitoring

### Service Performance Metrics
```csharp
public class ServicePerformanceMonitor
{
    private readonly Dictionary<string, ServiceMetrics> _serviceMetrics = new();
    
    public void RecordServiceExecution(string serviceName, long executionTime, int objectsProcessed)
    {
        if (!_serviceMetrics.ContainsKey(serviceName))
            _serviceMetrics[serviceName] = new ServiceMetrics();
            
        var metrics = _serviceMetrics[serviceName];
        metrics.TotalExecutions++;
        metrics.TotalExecutionTime += executionTime;
        metrics.TotalObjectsProcessed += objectsProcessed;
        metrics.AverageExecutionTime = metrics.TotalExecutionTime / metrics.TotalExecutions;
        
        // Track peak performance
        if (executionTime > metrics.PeakExecutionTime)
            metrics.PeakExecutionTime = executionTime;
    }
    
    public ServiceMetrics GetMetrics(string serviceName)
    {
        return _serviceMetrics.GetValueOrDefault(serviceName, new ServiceMetrics());
    }
}

public class ServiceMetrics
{
    public long TotalExecutions { get; set; }
    public long TotalExecutionTime { get; set; }
    public long AverageExecutionTime { get; set; }
    public long PeakExecutionTime { get; set; }
    public long TotalObjectsProcessed { get; set; }
}
```

### Service Health Monitoring
```csharp
public class ServiceHealthMonitor
{
    public void CheckServiceHealth()
    {
        foreach (var service in _services.Values)
        {
            var metrics = GetServiceMetrics(service);
            
            if (metrics.AverageExecutionTime > PERFORMANCE_WARNING_THRESHOLD)
            {
                LogPerformanceWarning(service, metrics);
            }
            
            if (metrics.PeakExecutionTime > PERFORMANCE_CRITICAL_THRESHOLD)
            {
                LogPerformanceCritical(service, metrics);
            }
        }
    }
}
```

## Service Utilities

### Common Service Operations
```csharp
public static class ServiceUtils
{
    public static bool ShouldTick(long nextTick)
    {
        return GameLoop.GameLoopTime >= nextTick;
    }
    
    public static void HandleServiceException<T>(Exception exception, string serviceName, 
        T entity, GameObject entityOwner) where T : class, IServiceObject
    {
        LogServiceException(serviceName, exception, entity, entityOwner);
        
        // Remove problematic entity to prevent cascade failures
        if (entity != null)
        {
            ServiceObjectStore.Remove(entity);
        }
        
        // Alert administrators for critical services
        if (IsCriticalService(serviceName))
        {
            AlertAdministrators(serviceName, exception);
        }
    }
}
```

## Service Lifecycle Management

### Service Registration and Startup
```csharp
public class ServiceManager
{
    private readonly List<IGameService> _registeredServices = new();
    private readonly Dictionary<Type, IGameService> _serviceMap = new();
    
    public void RegisterService<T>(T service) where T : IGameService
    {
        _registeredServices.Add(service);
        _serviceMap[typeof(T)] = service;
    }
    
    public void StartAllServices()
    {
        foreach (var service in _registeredServices)
        {
            try
            {
                service.Initialize();
                service.Start();
                LogServiceStart(service.GetType().Name);
            }
            catch (Exception ex)
            {
                LogServiceStartupError(service.GetType().Name, ex);
            }
        }
    }
    
    public void StopAllServices()
    {
        foreach (var service in _registeredServices.AsEnumerable().Reverse())
        {
            try
            {
                service.Stop();
                LogServiceStop(service.GetType().Name);
            }
            catch (Exception ex)
            {
                LogServiceShutdownError(service.GetType().Name, ex);
            }
        }
    }
}
```

## TODO: Missing Documentation

- Advanced service dependency management and ordering
- Hot-swapping and dynamic service reloading mechanisms
- Cross-service communication patterns and message passing
- Service isolation and fault tolerance strategies
- Advanced performance optimization techniques
- Memory pool management for service objects
- Distributed service coordination for multi-server setups

## References

- `GameServer/ECS-Services/` - All service implementations
- `GameServer/Managers/ServiceObject/` - Service object management
- `GameServer/Managers/GameLoop/GameLoop.cs` - Game loop integration
- `GameServer/ECS-Services/ServiceUtils.cs` - Common utilities 