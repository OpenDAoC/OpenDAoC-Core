# ECS Performance System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: The ECS performance system is the engine that makes DAoC run smoothly with hundreds of players online simultaneously. It organizes all game activities into efficient "components" and processes them in parallel across multiple CPU cores, ensuring your actions feel responsive even during massive battles or peak server activity.

The Entity Component System (ECS) Performance System manages the efficient processing of game entities through component-based architecture, parallel processing, and optimized update cycles. It provides the foundation for high-performance game loop execution with hundreds of concurrent players.

## Core ECS Architecture

### Entity Management
```csharp
public abstract class GameObject : IComponent
{
    public ComponentContainer componentContainer { get; set; }
    public long ComponentUpdateTick { get; set; }
    public static readonly ComponentManager ComponentManager = new ComponentManager();
    
    // Core game object properties
    public eObjectState ObjectState { get; set; }
    public Region CurrentRegion { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
}
```

### Component System
```csharp
public interface IComponent
{
    GameObject Owner { get; set; }
    ComponentUpdateTick UpdateTick { get; set; }
    
    void Tick();
    bool ShouldUpdateProperties();
}

public class ComponentContainer
{
    private readonly Dictionary<Type, IComponent> _components = new();
    
    public T GetComponent<T>() where T : class, IComponent
    {
        _components.TryGetValue(typeof(T), out var component);
        return component as T;
    }
    
    public void AddComponent<T>(T component) where T : class, IComponent
    {
        _components[typeof(T)] = component;
        component.Owner = Owner;
    }
}
```

### Component Types

#### Core Components
- **AttackComponent**: Combat state and targeting
- **CastingComponent**: Spell casting state
- **MovementComponent**: Position and movement
- **EffectListComponent**: Active spell effects
- **InventoryComponent**: Item management
- **CraftComponent**: Crafting state

#### Specialized Components
- **GroupComponent**: Group membership
- **GuildComponent**: Guild information
- **QuestComponent**: Quest tracking
- **TradeComponent**: Trading state

## Service-Based Processing

### Service Architecture
```csharp
public abstract class GameService
{
    protected static readonly Logging.Logger log;
    
    public static void Tick()
    {
        GameLoop.CurrentServiceTick = GetServiceName();
        
        // Performance monitoring
        Diagnostics.StartPerfCounter(GetServiceName());
        
        ProcessComponents();
        
        Diagnostics.StopPerfCounter(GetServiceName());
    }
    
    protected abstract void ProcessComponents();
}
```

### Core Services

#### AttackService
```csharp
public static class AttackService
{
    private static AttackComponent[] _components;
    private static int _lastValidIndex;
    
    public static void Tick()
    {
        // Get all attack components for this tick
        _components = ServiceObjectStore.UpdateAndGetAll<AttackComponent>(
            ServiceObjectType.AttackComponent, out _lastValidIndex);
            
        // Process in parallel
        GameLoop.ExecuteWork(_lastValidIndex + 1, TickInternal);
    }
    
    private static void TickInternal(int index)
    {
        AttackComponent component = _components[index];
        
        long startTick = GameLoop.GetRealTime();
        component.Tick();
        long stopTick = GameLoop.GetRealTime();
        
        // Long tick detection
        if (stopTick - startTick > Diagnostics.LongTickThreshold)
            log.Warn($"Long {SERVICE_NAME} tick: {component.Owner.Name} " +
                    $"Time: {stopTick - startTick}ms");
    }
}
```

#### EffectListService
```csharp
public static class EffectListService
{
    public static void Tick()
    {
        var components = ServiceObjectStore.UpdateAndGetAll<EffectListComponent>(
            ServiceObjectType.EffectListComponent, out int lastValidIndex);
            
        GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);
    }
    
    private static void TickInternal(int index)
    {
        EffectListComponent component = components[index];
        
        // Process all active effects
        foreach (var effect in component.GetAllEffects())
        {
            if (effect.ShouldExpire())
            {
                effect.Cancel(false);
            }
            else
            {
                effect.Tick();
            }
        }
        
        // Cleanup expired effects
        component.CleanupExpiredEffects();
    }
}
```

#### MovementService
```csharp
public static class MovementService
{
    private const int BROADCAST_MINIMUM_INTERVAL = 200; // 200ms
    
    public static void Tick()
    {
        var components = ServiceObjectStore.UpdateAndGetAll<MovementComponent>(
            ServiceObjectType.MovementComponent, out int lastValidIndex);
            
        GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);
    }
    
    private static void TickInternal(int index)
    {
        MovementComponent component = components[index];
        
        // Update position
        component.UpdatePosition();
        
        // Broadcast position if needed
        if (component.ShouldBroadcastPosition())
        {
            component.BroadcastPosition();
        }
        
        // Check for zone transitions
        component.CheckZoneTransition();
    }
}
```

## Service Object Store

### Object Store Architecture
```csharp
public static class ServiceObjectStore
{
    private static readonly Dictionary<ServiceObjectType, IServiceObjectContainer> 
        _containers = new();
        
    public static T[] UpdateAndGetAll<T>(ServiceObjectType type, out int lastValidIndex) 
        where T : class, IComponent
    {
        var container = _containers[type] as ServiceObjectContainer<T>;
        return container.UpdateAndGetAll(out lastValidIndex);
    }
    
    public static void RegisterObject<T>(T obj, ServiceObjectType type) 
        where T : class, IComponent
    {
        var container = GetOrCreateContainer<T>(type);
        container.AddObject(obj);
    }
}

public class ServiceObjectContainer<T> : IServiceObjectContainer 
    where T : class, IComponent
{
    private T[] _objects = new T[1024];
    private int _count = 0;
    private readonly Lock _lock = new();
    
    public T[] UpdateAndGetAll(out int lastValidIndex)
    {
        using (_lock)
        {
            // Remove null/inactive objects
            CompactArray();
            
            lastValidIndex = _count - 1;
            return _objects;
        }
    }
    
    private void CompactArray()
    {
        int writeIndex = 0;
        
        for (int readIndex = 0; readIndex < _count; readIndex++)
        {
            T obj = _objects[readIndex];
            
            if (obj != null && IsObjectActive(obj))
            {
                _objects[writeIndex] = obj;
                writeIndex++;
            }
        }
        
        // Clear remaining slots
        for (int i = writeIndex; i < _count; i++)
        {
            _objects[i] = null;
        }
        
        _count = writeIndex;
    }
}
```

## Parallel Processing

### Work Distribution
```csharp
public static class GameLoop
{
    private static GameLoopThreadPool _threadPool;
    
    public static void ExecuteWork(int count, Action<int> action)
    {
        _threadPool.ExecuteWork(count, action);
    }
}

public class GameLoopThreadPoolMultiThreaded : GameLoopThreadPool
{
    private readonly int _workerCount;
    private readonly WorkerThread[] _workers;
    
    public override void ExecuteWork(int count, Action<int> action)
    {
        if (count <= 0) return;
        
        // Distribute work across threads
        int workPerThread = Math.Max(1, count / _workerCount);
        int remainingWork = count;
        int startIndex = 0;
        
        for (int i = 0; i < _workerCount && remainingWork > 0; i++)
        {
            int workAmount = Math.Min(workPerThread, remainingWork);
            
            _workers[i].QueueWork(action, startIndex, workAmount);
            
            startIndex += workAmount;
            remainingWork -= workAmount;
        }
        
        // Wait for completion
        WaitForAllWorkers();
    }
}
```

### Thread Safety

#### Lock-Free Operations
```csharp
public class ComponentTick
{
    private volatile long _lastTickTime;
    private volatile bool _isProcessing;
    
    public bool ShouldTick(long currentTime)
    {
        // Atomic check and update
        long lastTick = Interlocked.Read(ref _lastTickTime);
        
        if (currentTime - lastTick >= TickInterval)
        {
            if (Interlocked.CompareExchange(ref _isProcessing, true, false) == false)
            {
                Interlocked.Exchange(ref _lastTickTime, currentTime);
                return true;
            }
        }
        
        return false;
    }
    
    public void FinishTick()
    {
        Interlocked.Exchange(ref _isProcessing, false);
    }
}
```

#### Service Isolation
```csharp
// Each service processes its components independently
// No shared state between services during processing
// Component updates are atomic operations
```

## Memory Management

### Object Pooling
```csharp
public interface IPooledObject<T>
{
    long IssuedTimestamp { get; set; }
}

public class TickObjectPool<T> : ITickObjectPool where T : IPooledObject<T>, new()
{
    private const int INITIAL_CAPACITY = 64;
    private const double TRIM_SAFETY_FACTOR = 2.5;
    private const int HALF_LIFE = 300_000; // 5 minutes
    
    private T[] _items = new T[INITIAL_CAPACITY];
    private int _used = 0;
    private int _logicalSize = 0;
    private double _smoothedUsage = 0;
    
    // Exponential Moving Average for pool sizing
    private static readonly double DECAY_FACTOR = 
        Math.Exp(-Math.Log(2) / (GameLoop.TickRate * HALF_LIFE / 1000.0));
    
    public T GetForTick()
    {
        if (_used < _logicalSize)
        {
            T item = _items[_used];
            item.IssuedTimestamp = GameLoop.GameLoopTime;
            _used++;
            return item;
        }
        
        // Create new item
        T newItem = new T();
        newItem.IssuedTimestamp = GameLoop.GameLoopTime;
        
        // Expand array if needed
        if (_used >= _items.Length)
        {
            Array.Resize(ref _items, _items.Length * 2);
        }
        
        _items[_used] = newItem;
        _used++;
        _logicalSize++;
        
        return newItem;
    }
    
    public void Reset()
    {
        // Update smoothed usage with EMA
        _smoothedUsage = Math.Max(_used, 
            _smoothedUsage * DECAY_FACTOR + _used * (1 - DECAY_FACTOR));
            
        // Trim pool if significantly oversized
        int newLogicalSize = (int)(_smoothedUsage * TRIM_SAFETY_FACTOR);
        
        if (_logicalSize > newLogicalSize)
        {
            for (int i = newLogicalSize; i < _logicalSize; i++)
            {
                _items[i] = default(T);
            }
            _logicalSize = newLogicalSize;
        }
        
        _used = 0;
    }
}
```

### Component Lifecycle
```csharp
public class ComponentLifecycle
{
    public static void CreateComponent<T>(GameObject owner) where T : class, IComponent, new()
    {
        T component = GameLoop.GetForTick<T>(PooledObjectKey.Component, 
            c => c.Initialize());
            
        component.Owner = owner;
        owner.componentContainer.AddComponent(component);
        
        // Register with service
        ServiceObjectStore.RegisterObject(component, GetServiceType<T>());
    }
    
    public static void DestroyComponent<T>(GameObject owner) where T : class, IComponent
    {
        T component = owner.componentContainer.GetComponent<T>();
        
        if (component != null)
        {
            // Unregister from service
            ServiceObjectStore.UnregisterObject(component, GetServiceType<T>());
            
            // Clean up component
            component.OnDestroy();
            
            // Return to pool
            GameLoop.ReturnToPool(component);
        }
    }
}
```

## Update Optimization

### Smart Update System
```csharp
public class ComponentUpdateTracker
{
    private volatile bool _needsUpdate = false;
    private long _lastUpdateTick = 0;
    private readonly int _updateInterval;
    
    public bool ShouldUpdate(long currentTick)
    {
        // Force update if flagged
        if (_needsUpdate)
        {
            _needsUpdate = false;
            _lastUpdateTick = currentTick;
            return true;
        }
        
        // Interval-based update
        if (currentTick - _lastUpdateTick >= _updateInterval)
        {
            _lastUpdateTick = currentTick;
            return true;
        }
        
        return false;
    }
    
    public void FlagForUpdate()
    {
        _needsUpdate = true;
    }
}
```

### Property Update Batching
```csharp
public class PropertyUpdateBatch
{
    private readonly HashSet<eProperty> _dirtyProperties = new();
    private readonly Lock _lock = new();
    
    public void MarkDirty(eProperty property)
    {
        using (_lock)
        {
            _dirtyProperties.Add(property);
        }
    }
    
    public void FlushUpdates(GameObject owner)
    {
        using (_lock)
        {
            foreach (eProperty property in _dirtyProperties)
            {
                owner.RecalculateProperty(property);
            }
            
            _dirtyProperties.Clear();
        }
    }
}
```

## Performance Monitoring

### Component Performance Tracking
```csharp
public static class ComponentPerformanceTracker
{
    private static readonly Dictionary<Type, ComponentStats> _stats = new();
    
    public static void RecordTick(Type componentType, long duration)
    {
        if (!_stats.TryGetValue(componentType, out ComponentStats stats))
        {
            stats = new ComponentStats();
            _stats[componentType] = stats;
        }
        
        stats.TotalTicks++;
        stats.TotalDuration += duration;
        stats.MaxDuration = Math.Max(stats.MaxDuration, duration);
        
        if (duration > Diagnostics.LongTickThreshold)
        {
            stats.LongTicks++;
        }
    }
    
    public static void ReportStats()
    {
        foreach (var kvp in _stats)
        {
            ComponentStats stats = kvp.Value;
            double avgDuration = (double)stats.TotalDuration / stats.TotalTicks;
            
            log.Info($"Component {kvp.Key.Name}: " +
                    $"Avg: {avgDuration:F2}ms, " +
                    $"Max: {stats.MaxDuration}ms, " +
                    $"LongTicks: {stats.LongTicks}");
        }
    }
}
```

### Memory Usage Tracking
```csharp
public static class MemoryTracker
{
    public static void RecordComponentAllocation(Type componentType, int size)
    {
        // Track memory allocations per component type
    }
    
    public static void RecordPoolUsage(string poolName, int used, int capacity)
    {
        double utilizationPercent = (double)used / capacity * 100;
        
        if (utilizationPercent > 90)
        {
            log.Warn($"Pool {poolName} is {utilizationPercent:F1}% full");
        }
    }
}
```

## Diagnostics & Debugging

### Performance Diagnostics
```csharp
public static class Diagnostics
{
    public static long LongTickThreshold = 50; // 50ms
    public static bool CheckEntityCounts = true;
    
    public static void StartPerfCounter(string uniqueID)
    {
        if (!_perfCountersEnabled) return;
        
        Stopwatch stopwatch = Stopwatch.StartNew();
        lock(_perfCountersLock)
        {
            _perfCounters.TryAdd(uniqueID, stopwatch);
        }
    }
    
    public static void StopPerfCounter(string uniqueID)
    {
        if (!_perfCountersEnabled) return;
        
        lock(_perfCountersLock)
        {
            if (_perfCounters.TryRemove(uniqueID, out Stopwatch stopwatch))
            {
                stopwatch.Stop();
                _perfStreamWriter.WriteLine($"{uniqueID},{stopwatch.ElapsedMilliseconds}");
            }
        }
    }
}
```

### Component Validation
```csharp
public static class ComponentValidator
{
    public static void ValidateComponent<T>(T component) where T : IComponent
    {
        if (component.Owner == null)
        {
            throw new InvalidOperationException($"Component {typeof(T).Name} has null owner");
        }
        
        if (component.Owner.ObjectState == eObjectState.Deleted)
        {
            throw new InvalidOperationException($"Component {typeof(T).Name} owner is deleted");
        }
    }
    
    public static void ValidateComponentCounts()
    {
        if (!Diagnostics.CheckEntityCounts) return;
        
        foreach (ServiceObjectType type in Enum.GetValues<ServiceObjectType>())
        {
            int count = ServiceObjectStore.GetObjectCount(type);
            
            if (count > 10000) // Arbitrary threshold
            {
                log.Warn($"High component count for {type}: {count}");
            }
        }
    }
}
```

## Configuration

### Performance Settings
```xml
<Property Name="GAME_LOOP_TICK_RATE" Value="10" />
<Property Name="LONG_TICK_THRESHOLD" Value="50" />
<Property Name="CHECK_ENTITY_COUNTS" Value="true" />
<Property Name="ENABLE_PERFORMANCE_COUNTERS" Value="false" />
<Property Name="COMPONENT_POOL_INITIAL_SIZE" Value="64" />
<Property Name="COMPONENT_POOL_TRIM_FACTOR" Value="2.5" />
<Property Name="PARALLEL_PROCESSING_ENABLED" Value="true" />
<Property Name="WORKER_THREAD_COUNT" Value="-1" />
```

### Service Update Intervals
```csharp
public static class ServiceIntervals
{
    public const int ATTACK_SERVICE = 10;      // Every tick
    public const int CASTING_SERVICE = 10;     // Every tick
    public const int EFFECT_SERVICE = 10;      // Every tick
    public const int MOVEMENT_SERVICE = 10;    // Every tick
    public const int ZONE_SERVICE = 100;       // Every 10 ticks
    public const int CRAFTING_SERVICE = 100;   // Every 10 ticks
    public const int REAPER_SERVICE = 1000;    // Every 100 ticks
}
```

## System Integration

### With Game Loop
- Services called in specific order each tick
- Parallel processing coordinated by thread pool
- Performance monitoring integrated

### With Memory Management
- Object pooling reduces allocations
- Component lifecycle managed efficiently
- Garbage collection minimized

### With Network System
- Component updates trigger network packets
- Batched updates reduce bandwidth
- Client synchronization optimized

## Test Scenarios

### Performance Testing
- 1000+ players in single zone
- Component creation/destruction rates
- Memory usage under load
- Thread pool efficiency

### Stress Testing
- Maximum component counts
- Long tick detection
- Pool exhaustion scenarios
- Memory leak detection

### Integration Testing
- Cross-service dependencies
- Component lifecycle validation
- Property update propagation
- Network synchronization

## Future Optimizations

### Potential Improvements
- **SIMD Processing**: Vector operations for position updates
- **GPU Computing**: Parallel processing on graphics card
- **Cache Optimization**: Improved data locality
- **Compression**: Compressed component data
- **Prediction**: Client-side prediction for movement

### Scalability Enhancements
- **Dynamic Scaling**: Component pools that grow/shrink
- **Priority Systems**: Important components process first
- **LOD Systems**: Level-of-detail for distant objects
- **Spatial Partitioning**: Region-based processing 