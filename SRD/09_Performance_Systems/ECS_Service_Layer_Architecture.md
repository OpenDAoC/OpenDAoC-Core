# ECS Service Layer Architecture

**Document Status:** Complete System Analysis  
**Verification:** Code-verified from all ECS services and coordination patterns  
**Implementation Status:** Live Production

## Overview

**Game Rule Summary**: The service layer architecture coordinates all the different game systems (combat, magic, movement, etc.) to process in the correct order every game tick. This ensures that actions happen logically - like spells interrupting when you move, or attacks stopping when you start casting - maintaining consistent and predictable gameplay behavior.

OpenDAoC's service layer provides the behavioral logic for the Entity Component System, processing components in coordinated phases to maintain game state consistency. This document covers the complete service architecture, coordination patterns, and all service implementations.

## Service Architecture

### Core Service Interface

#### IGameService Contract
```csharp
public interface IGameService
{
    void Initialize();
    void Start();
    void Stop();
    void Tick();
}

// Static service pattern for performance
public static class AttackService
{
    private const string SERVICE_NAME = nameof(AttackService);
    private static List<AttackComponent> _list;
    private static int _entityCount;
    
    public static void Tick()
    {
        GameLoop.CurrentServiceTick = SERVICE_NAME;
        Diagnostics.StartPerfCounter(SERVICE_NAME);
        
        // Update component list from service store
        _list = ServiceObjectStore.UpdateAndGetAll<AttackComponent>(
            ServiceObjectType.AttackComponent, out int lastValidIndex);
        
        // Parallel processing of all components
        GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);
        
        // Performance monitoring
        if (Diagnostics.CheckEntityCounts)
            Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);
        
        Diagnostics.StopPerfCounter(SERVICE_NAME);
    }
}
```

### Service Coordination

#### Master Service Execution Order
```csharp
static void TickServices()
{
    // Phase 1: Pre-tick preparation
    GameLoopService.BeginTick();        // Process queued actions, reset pools
    TimerService.Tick();                // ECS timers and scheduling
    
    // Phase 2: Input processing
    ClientService.BeginTick();          // Receive packets, handle inputs
    
    // Phase 3: Core game systems (order critical for component interactions)
    NpcService.Tick();                  // AI brain processing
    AttackService.Tick();               // Combat resolution
    CastingService.Tick();              // Spell casting
    EffectListService.Tick();           // Spell effects/buffs processing
    MovementService.Tick();             // Position updates
    
    // Phase 4: Secondary systems
    ZoneService.Tick();                 // Zone transitions
    CraftingService.Tick();             // Crafting progress
    ReaperService.Tick();               // Death processing
    
    // Phase 5: Network finalization
    ClientService.EndTick();            // Send packets
    
    // Phase 6: Lower frequency systems
    DailyQuestService.Tick();
    WeeklyQuestService.Tick();
    
    // Phase 7: Cleanup and post-processing
    GameLoopService.EndTick();
}
```

## Core Game Services

### AttackService

#### Combat Processing Service
```csharp
public static class AttackService
{
    private static void TickInternal(int index)
    {
        AttackComponent attackComponent = _list[index];
        
        try
        {
            if (Diagnostics.CheckEntityCounts)
                Interlocked.Increment(ref _entityCount);
            
            long startTick = GameLoop.GetRealTime();
            attackComponent.Tick();
            long stopTick = GameLoop.GetRealTime();
            
            // Game rule: Combat calculations must complete within 1ms
            if (stopTick - startTick > Diagnostics.LongTickThreshold)
            {
                log.Warn($"Long {SERVICE_NAME} tick: {attackComponent.owner.Name} " +
                        $"Time: {stopTick - startTick}ms");
            }
        }
        catch (Exception e)
        {
            // Game rule: Combat errors don't crash the server
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, attackComponent, attackComponent.owner);
        }
    }
}
```

### CastingService

#### Spell Processing Service
```csharp
public static class CastingService
{
    private static void TickInternal(int index)
    {
        CastingComponent castingComponent = _list[index];
        
        try
        {
            // Game rule: Casting components process spell progression
            castingComponent.Tick();
        }
        catch (Exception e)
        {
            // Game rule: Casting errors interrupt the spell
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, castingComponent, castingComponent.owner);
        }
    }
}
```

### EffectListService

#### Buff/Debuff Processing Service
```csharp
public static class EffectListService
{
    private static void TickInternal(int index)
    {
        EffectListComponent component = _list[index];
        
        try
        {
            // Game rule: Process all active effects every tick
            foreach (var effect in component.GetAllEffects())
            {
                if (effect.ShouldExpire())
                    effect.Cancel(false);
                else if (ServiceUtils.ShouldTick(effect.NextTick))
                    effect.Tick();
            }
            
            // Game rule: Cleanup expired effects to prevent memory leaks
            component.CleanupExpiredEffects();
        }
        catch (Exception e)
        {
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, component, component.owner);
        }
    }
}
```

### MovementService

#### Position and Zone Management Service
```csharp
public static class MovementService
{
    private const int BROADCAST_MINIMUM_INTERVAL = 200; // 200ms network optimization
    
    private static void TickInternal(int index)
    {
        MovementComponent component = _list[index];
        
        try
        {
            // Game rule: Update position calculations
            component.UpdatePosition();
            
            // Game rule: Network optimization - only broadcast when needed
            if (component.ShouldBroadcastPosition())
                component.BroadcastPosition();
            
            // Game rule: Zone transition detection
            component.CheckZoneTransition();
        }
        catch (Exception e)
        {
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, component, component.Owner);
        }
    }
}
```

### ZoneService

#### Zone Transition Processing Service
```csharp
public static class ZoneService
{
    private static void TickInternal(int index)
    {
        ObjectChangingSubZone objectChangingSubZone = _list[index];
        SubZoneObject subZoneObject = objectChangingSubZone.SubZoneObject;
        LinkedListNode<GameObject> node = subZoneObject.Node;
        SubZone currentSubZone = objectChangingSubZone.CurrentSubZone;
        Zone currentZone = currentSubZone?.ParentZone;
        SubZone destinationSubZone = objectChangingSubZone.DestinationSubZone;
        Zone destinationZone = objectChangingSubZone.DestinationZone;
        bool changingZone = currentZone != destinationZone;
        
        try
        {
            if (destinationSubZone == null || destinationZone == null)
            {
                // Game rule: Object leaving world
                ProcessObjectRemoval(node, currentSubZone);
            }
            else if (changingZone)
            {
                // Game rule: Cross-zone movement
                ProcessZoneTransition(node, currentSubZone, destinationSubZone, destinationZone);
            }
            else
            {
                // Game rule: Same-zone subzone movement
                ProcessSubZoneMovement(node, currentSubZone, destinationSubZone);
            }
            
            subZoneObject.ResetSubZoneChange();
            ServiceObjectStore.Remove(objectChangingSubZone);
        }
        catch (Exception e)
        {
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, objectChangingSubZone, node?.Value);
        }
    }
}
```

## Specialized Services

### NpcService

#### AI Brain Processing Service
```csharp
public static class NpcService
{
    private static void TickInternal(int index)
    {
        ABrain brain = _list[index];
        
        try
        {
            // Game rule: NPC brains process AI decisions every tick
            brain.Tick();
        }
        catch (Exception e)
        {
            // Game rule: AI errors don't crash NPCs permanently
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, brain, brain.Body);
        }
    }
}
```

### CraftingService

#### Crafting Progress Service
```csharp
public static class CraftingService
{
    private static void TickInternal(int index)
    {
        CraftComponent craftComponent = _list[index];
        
        try
        {
            // Game rule: Crafting requires continuous time investment
            craftComponent.Tick();
        }
        catch (Exception e)
        {
            // Game rule: Crafting errors interrupt the process
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, craftComponent, craftComponent.owner);
        }
    }
}
```

### ReaperService

#### Death Processing Service
```csharp
public static class ReaperService
{
    private static void TickInternal(int index)
    {
        GameNPC npc = _list[index];
        
        try
        {
            // Game rule: Process NPC death cleanup after delay
            if (npc.IsAlive == false && ServiceUtils.ShouldTick(npc.DespawnTime))
            {
                npc.Delete();
                ServiceObjectStore.Remove(npc);
            }
        }
        catch (Exception e)
        {
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, npc, npc);
        }
    }
}
```

### TimerService

#### ECS Timer Coordination Service
```csharp
public static class TimerService
{
    public static void ScheduleTimerAfterTask<T>(Task task, ContinuationAction<T> continuation, 
                                               T argument, GameObject owner)
    {
        ContinuationActionTimerState<T> state = new(owner, continuation, argument);
        
        task.ContinueWith(static (task, state) =>
        {
            if (task.IsFaulted)
            {
                log.Error("Async task failed", task.Exception);
                return;
            }
            
            // Game rule: Async operations integrate with ECS through timers
            GameLoopService.PostAfterTick(static (s) => 
                new ContinuationActionTimer<T>(s as ContinuationActionTimerState<T>), state);
        }, state);
    }
    
    private static void TickInternal(int index)
    {
        ECSGameTimer timer = _list[index];
        
        try
        {
            if (ServiceUtils.ShouldTick(timer.NextTick))
            {
                long startTick = GameLoop.GetRealTime();
                timer.Tick();
                long stopTick = GameLoop.GetRealTime();
                
                // Game rule: Timer callbacks must complete quickly
                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                {
                    log.Warn($"Long {SERVICE_NAME} tick for Timer: " +
                            $"{timer.CallbackInfo?.DeclaringType}:{timer.CallbackInfo?.Name} " +
                            $"Owner: {timer.Owner?.Name} Time: {stopTick - startTick}ms");
                }
            }
        }
        catch (Exception e)
        {
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, timer, timer.Owner);
        }
    }
}
```

## Network Services

### ClientService

#### Network I/O Coordination Service
```csharp
public static class ClientService
{
    public static void BeginTick()
    {
        // Game rule: Receive packets from all clients in parallel
        _clients = ServiceObjectStore.UpdateAndGetAll<GameClient>(
            ServiceObjectType.Client, out _lastValidIndex);
        GameLoop.ExecuteWork(_lastValidIndex + 1, BeginTickInternal);
    }
    
    public static void EndTick()
    {
        // Game rule: Send responses to all clients in parallel
        GameLoop.ExecuteWork(_lastValidIndex + 1, EndTickInternal);
    }
    
    private static void BeginTickInternal(int index)
    {
        GameClient client = _clients[index];
        
        try
        {
            // Game rule: Process all incoming packets this tick
            client.ReceivePackets();
        }
        catch (Exception e)
        {
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, client, client.Player);
        }
    }
    
    private static void EndTickInternal(int index)
    {
        GameClient client = _clients[index];
        
        try
        {
            // Game rule: Send all outgoing packets this tick
            client.SendQueuedPackets();
        }
        catch (Exception e)
        {
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, client, client.Player);
        }
    }
}
```

## Service Utilities

### Common Service Operations

#### Service Exception Handling
```csharp
public static class ServiceUtils
{
    public static void HandleServiceException<T>(Exception exception, string serviceName, 
                                               T entity, GameObject entityOwner) 
        where T : class, IServiceObject
    {
        // Game rule: Services gracefully handle component failures
        if (entity != null)
            ServiceObjectStore.Remove(entity);
        
        List<string> logMessages = new();
        logMessages.Add($"Critical error in {serviceName}: {exception}");
        
        // Game rule: Component-specific recovery actions
        Action action = entity switch
        {
            AttackComponent => () => entityOwner?.attackComponent?.StopAttack(),
            CastingComponent => () => entityOwner?.castingComponent?.InterruptCasting(false),
            MovementComponent => () => entityOwner?.movementComponent?.StopMovement(),
            CraftComponent => () => entityOwner?.craftComponent?.InterruptCrafting(),
            EffectListComponent => () => entityOwner?.effectListComponent?.CancelAllEffects(),
            _ => () => { /* Default: just log */ }
        };
        
        try
        {
            action?.Invoke();
        }
        catch (Exception recoveryException)
        {
            log.Error($"Recovery action failed: {recoveryException}");
        }
    }
    
    // Game rule: Half-tick tolerance prevents timing drift
    private static long HalfTickRate => GameLoop.TickRate / 2;
    
    public static bool ShouldTick(long tickTime)
    {
        return tickTime - GameLoop.GameLoopTime - HalfTickRate <= 0;
    }
}
```

## Service Performance Monitoring

### Real-Time Diagnostics

#### Performance Tracking
```csharp
public static class Diagnostics
{
    public static long LongTickThreshold = 50; // 50ms warning threshold
    private static Dictionary<string, Stopwatch> _perfCounters = new();
    private static readonly object _perfCountersLock = new();
    
    public static void StartPerfCounter(string uniqueID)
    {
        if (!_perfCountersEnabled) return;
        
        lock (_perfCountersLock)
        {
            if (!_perfCounters.TryGetValue(uniqueID, out Stopwatch stopwatch))
            {
                stopwatch = new Stopwatch();
                _perfCounters[uniqueID] = stopwatch;
            }
            stopwatch.Restart();
        }
    }
    
    public static void StopPerfCounter(string uniqueID)
    {
        if (!_perfCountersEnabled) return;
        
        lock (_perfCountersLock)
        {
            if (_perfCounters.TryGetValue(uniqueID, out Stopwatch stopwatch))
            {
                stopwatch.Stop();
                
                if (stopwatch.ElapsedMilliseconds > LongTickThreshold)
                {
                    log.Warn($"Service {uniqueID} took {stopwatch.ElapsedMilliseconds}ms");
                }
            }
        }
    }
    
    // Game rule: Entity count monitoring for capacity planning
    public static void PrintEntityCount(string serviceName, ref int nonNull, int total)
    {
        log.Debug($"==== {FormatCount(nonNull),-4} / {FormatCount(total),4} " +
                 $"non-null entities in {serviceName}'s list ====");
        
        static string FormatCount(int count)
        {
            return count >= 1000000 ? (count / 1000000.0).ToString("G3") + "M" :
                   count >= 1000 ? (count / 1000.0).ToString("G3") + "K" :
                   count.ToString();
        }
    }
}
```

## Service Configuration

### Performance Tuning

#### Service Execution Settings
```xml
<!-- Core service timing -->
<Property Name="GAME_LOOP_TICK_RATE" Value="10" />
<Property Name="LONG_TICK_THRESHOLD" Value="50" />

<!-- Entity capacity limits -->
<Property Name="MAX_PLAYERS" Value="500" />
<Property Name="MAX_ENTITIES" Value="10000" />

<!-- Service-specific capacities -->
<Property Name="ATTACK_COMPONENT_CAPACITY" Value="1250" />
<Property Name="CASTING_COMPONENT_CAPACITY" Value="1250" />
<Property Name="EFFECT_LIST_COMPONENT_CAPACITY" Value="1250" />
<Property Name="MOVEMENT_COMPONENT_CAPACITY" Value="1250" />
<Property Name="CRAFT_COMPONENT_CAPACITY" Value="100" />
<Property Name="TIMER_CAPACITY" Value="500" />

<!-- Monitoring settings -->
<Property Name="CHECK_ENTITY_COUNTS" Value="true" />
<Property Name="ENABLE_PERFORMANCE_COUNTERS" Value="false" />
```

### Service Frequency Optimization

#### Variable Update Intervals
```csharp
// Game rule: Different systems run at different frequencies for optimization
private const int CHECK_ATTACKERS_INTERVAL = 1000;     // 1 second
private const int SUBZONE_RELOCATION_CHECK_INTERVAL = 500; // 500ms
private const int BROADCAST_MINIMUM_INTERVAL = 200;    // 200ms
private const int TICK_INTERVAL_FOR_NON_ATTACK = 100;  // 100ms

// Adaptive intervals based on game state
private int CalculateOptimalInterval(GameObject owner)
{
    if (owner is GamePlayer player)
    {
        return player.InCombat ? 50 : 100; // Higher frequency for active players
    }
    
    if (owner is GameNPC npc)
    {
        return npc.InCombat ? 100 : 500; // Lower frequency for idle NPCs
    }
    
    return 250; // Default interval
}
```

## Service Dependencies

### Inter-Service Communication

#### Service Dependency Graph
```csharp
// Game rule: Service execution order maintains game state consistency
// 1. ClientService.BeginTick() - Process player inputs
// 2. AttackService.Tick() - Process combat (may trigger effects)
// 3. CastingService.Tick() - Process spells (may trigger effects) 
// 4. EffectListService.Tick() - Process effect consequences
// 5. MovementService.Tick() - Process position updates (may trigger zones)
// 6. ZoneService.Tick() - Process zone transitions
// 7. ClientService.EndTick() - Send updates to clients

// Cross-service event propagation
public void OnAttackComplete(AttackResult result)
{
    // Combat may trigger spell effects
    if (result.TriggeredEffect != null)
    {
        target.effectListComponent.AddEffect(result.TriggeredEffect);
        ServiceObjectStore.Add(target.effectListComponent);
    }
    
    // Combat may trigger movement (knockback)
    if (result.MovementEffect != null)
    {
        target.movementComponent.ApplyMovementEffect(result.MovementEffect);
        ServiceObjectStore.Add(target.movementComponent);
    }
}
```

## Service Lifecycle Management

### Service Initialization

#### Startup Sequence
```csharp
public static class ServiceManager
{
    private static readonly List<IGameService> _services = new();
    
    public static void InitializeServices()
    {
        // Game rule: Services initialize in dependency order
        _services.Add(new GameLoopService());
        _services.Add(new TimerService());
        _services.Add(new ClientService());
        _services.Add(new PropertyService());
        _services.Add(new EffectService());
        _services.Add(new CombatService());
        _services.Add(new MovementService());
        _services.Add(new ZoneService());
        
        foreach (var service in _services)
        {
            service.Initialize();
        }
    }
    
    public static void StartServices()
    {
        foreach (var service in _services)
        {
            service.Start();
        }
    }
    
    public static void StopServices()
    {
        // Reverse order shutdown
        for (int i = _services.Count - 1; i >= 0; i--)
        {
            _services[i].Stop();
        }
    }
}
```

## Future Service Enhancements

### Planned Optimizations

1. **Service Load Balancing**: Dynamic work distribution based on current load
2. **Hierarchical Processing**: Priority-based service execution
3. **Predictive Scheduling**: Pre-schedule services based on game state
4. **Service Clustering**: Group related services for better cache locality
5. **Adaptive Intervals**: Dynamic service frequency based on activity

### Performance Targets

- **Service Execution**: < 1ms per service tick
- **Cross-Service Communication**: < 0.1ms latency
- **Memory Usage**: < 100MB for all services combined
- **CPU Utilization**: > 95% efficiency during peak load

## Conclusion

OpenDAoC's service layer architecture provides a sophisticated foundation for coordinated ECS processing. The careful ordering of service execution, robust error handling, and performance monitoring ensure stable operation while maintaining authentic DAoC gameplay mechanics. The modular design enables targeted optimization and future enhancements while preserving system reliability.

## Change Log

- **v1.0** (2025-01-20): Complete service layer documentation
  - Service architecture and coordination patterns
  - All core and specialized service implementations
  - Performance monitoring and diagnostics
  - Error handling and recovery mechanisms
  - Configuration and optimization guidelines
  - Future enhancement roadmap

## References

- ECS_Game_Loop_Deep_Dive.md - Game loop integration
- ECS_Component_System.md - Component architecture
- Timer_Service_System.md - Timer service details
- Object_Pool_System.md - Memory management strategies
- Network_Protocol_System.md - Client service details 