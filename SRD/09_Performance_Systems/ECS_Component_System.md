# ECS Component System

**Document Status:** Comprehensive Analysis  
**Verification:** Code-verified from all ECS components and services  
**Implementation Status:** Live Production

## Overview

OpenDAoC's Entity Component System (ECS) architecture separates data (components) from behavior (services), enabling high-performance parallel processing of game entities. This document covers the complete component system, lifecycle management, and all component types used throughout the server.

## Component Architecture

### Core Component Interface

#### IServiceObject Contract
```csharp
public interface IServiceObject
{
    ServiceObjectId ServiceObjectId { get; set; }
}

public class ServiceObjectId
{
    public const int UNSET_ID = -1;
    private int _value = UNSET_ID;
    private PendingState _pendingState = PendingState.None;
    
    public ServiceObjectType Type { get; }
    public bool IsSet => _value > UNSET_ID;
    public bool IsPendingAddition => _pendingState == PendingState.Adding;
    public bool IsPendingRemoval => _pendingState == PendingState.Removing;
    
    public int Value
    {
        get => _value;
        set
        {
            if (_value != UNSET_ID)
                throw new InvalidOperationException("ServiceObjectId value can only be set once");
            _value = value;
        }
    }
}
```

#### Component Registration Pattern
```csharp
// Components self-register with the service store
public void RequestStartAttack(GameObject attackTarget = null)
{
    _startAttackTarget = attackTarget ?? owner.TargetObject;
    StartAttackRequested = true;
    ServiceObjectStore.Add(this); // Registers for next tick processing
}

// Components self-remove when no longer needed
public void Tick()
{
    if (owner.ObjectState is not eObjectState.Active)
    {
        attackAction.CleanUp();
        ServiceObjectStore.Remove(this); // Auto-cleanup
        return;
    }
    
    // Process component logic
    if (!ProcessComponentTick())
        ServiceObjectStore.Remove(this); // Self-removal when complete
}
```

### Component Types

#### Combat Components

**AttackComponent**
```csharp
public class AttackComponent : IServiceObject
{
    public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.AttackComponent);
    
    // Core attack data
    public GameObject owner { get; }
    public IAttackAction attackAction { get; set; }
    public bool StartAttackRequested { get; set; }
    public int BlockRoundCount { get; set; }
    
    // Attack state management
    private GameObject _startAttackTarget;
    private long _nextCheckForValidAttackers;
    private const int CHECK_ATTACKERS_INTERVAL = 1000; // 1 second
    
    // Game rule: Attack interval based on weapon speed and quickness
    public void RequestStartAttack(GameObject attackTarget = null)
    {
        _startAttackTarget = attackTarget ?? owner.TargetObject;
        StartAttackRequested = true;
        ServiceObjectStore.Add(this);
    }
    
    // Game rule: Combat components process every tick when active
    public void Tick()
    {
        if (owner.ObjectState is not eObjectState.Active)
        {
            attackAction.CleanUp();
            ServiceObjectStore.Remove(this);
            return;
        }
        
        // Check for valid attackers periodically (performance optimization)
        if (ServiceUtils.ShouldTick(_nextCheckForValidAttackers))
        {
            CheckForValidAttackers();
            _nextCheckForValidAttackers = GameLoop.GameLoopTime + CHECK_ATTACKERS_INTERVAL;
        }
        
        // Process attack action if not completed
        if (!attackAction.Tick())
            ServiceObjectStore.Remove(this);
    }
    
    // Game rule: Block rounds decrease every second during combat
    public void StartBlockRoundCountDecrementTimer()
    {
        new BlockRoundCountDecrementTimer(owner, this).Start(1000);
    }
}
```

**CastingComponent**
```csharp
public class CastingComponent : IServiceObject
{
    public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.CastingComponent);
    
    // Spell casting data
    public Spell spellToCast { get; set; }
    public SpellLine spellLine { get; set; }
    public GameObject spellTarget { get; set; }
    public long startCastTime { get; set; }
    public bool interruptRequested { get; set; }
    
    // Game rule: Spell casting requires continuous concentration
    public void Tick()
    {
        if (owner.ObjectState is not eObjectState.Active || interruptRequested)
        {
            InterruptCasting(true);
            return;
        }
        
        // Check if cast time elapsed
        long elapsedTime = GameLoop.GameLoopTime - startCastTime;
        if (elapsedTime >= spellToCast.CastTime)
        {
            CompleteCasting();
            ServiceObjectStore.Remove(this);
        }
    }
    
    // Game rule: Casting interruption stops attack actions
    public void InterruptCasting(bool sendInterruptMessage)
    {
        if (owner.attackComponent?.attackAction != null)
        {
            owner.attackComponent.attackAction.CleanUp();
            ServiceObjectStore.Remove(owner.attackComponent);
        }
        
        ServiceObjectStore.Remove(this);
    }
}
```

#### Effect Components

**EffectListComponent**
```csharp
public class EffectListComponent : IServiceObject
{
    public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.EffectListComponent);
    
    // Effect management
    private readonly List<GameSpellEffect> _effects = new();
    private readonly Dictionary<string, GameSpellEffect> _immunityEffects = new();
    
    // Game rule: Effects process on every tick
    public void Tick()
    {
        ProcessActiveEffects();
        CleanupExpiredEffects();
        
        // Remove component if no active effects
        if (_effects.Count == 0 && _immunityEffects.Count == 0)
            ServiceObjectStore.Remove(this);
    }
    
    // Game rule: Effect stacking follows specific rules per effect type
    public void AddEffect(GameSpellEffect effect)
    {
        // Check for existing effects of same type
        var existingEffect = _effects.FirstOrDefault(e => e.SpellHandler.Spell.SpellType == effect.SpellHandler.Spell.SpellType);
        
        if (existingEffect != null)
        {
            // Apply stacking rules
            if (effect.IsNewEffectBetter(existingEffect))
            {
                existingEffect.Cancel(false);
                _effects.Remove(existingEffect);
                _effects.Add(effect);
            }
        }
        else
        {
            _effects.Add(effect);
        }
        
        // Register component for processing
        ServiceObjectStore.Add(this);
    }
}
```

#### Movement Components

**MovementComponent**
```csharp
public class MovementComponent : IServiceObject
{
    public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.MovementComponent);
    
    // Movement tracking
    protected Point2D _positionDuringLastSubZoneRelocationCheck;
    protected long _nextSubZoneRelocationCheckTick;
    private const int SUBZONE_RELOCATION_CHECK_INTERVAL = 500; // 500ms
    
    // Game rule: Movement components check for zone transitions periodically
    protected virtual void TickInternal()
    {
        // Only check subzone relocation if object moved
        if (!Owner.IsSamePosition(_positionDuringLastSubZoneRelocationCheck) && 
            ServiceUtils.ShouldTick(_nextSubZoneRelocationCheckTick))
        {
            _nextSubZoneRelocationCheckTick = GameLoop.GameLoopTime + SUBZONE_RELOCATION_CHECK_INTERVAL;
            _positionDuringLastSubZoneRelocationCheck = new Point2D(Owner.X, Owner.Y);
            Owner.SubZoneObject.CheckForRelocation();
        }
    }
}

public class PlayerMovementComponent : MovementComponent
{
    private const int BROADCAST_MINIMUM_INTERVAL = 200; // Network optimization
    private const int SOFT_LINK_DEATH_THRESHOLD = 5000; // 5 second timeout
    
    private bool _needBroadcastPosition;
    private long _nextPositionBroadcast;
    private bool _validateMovementOnNextTick;
    
    // Game rule: Player movement requires additional validation and broadcasting
    protected override void TickInternal()
    {
        // Game rule: Link death detection affects all other components
        if (!Owner.IsLinkDeathTimerRunning)
        {
            if (ServiceUtils.ShouldTick(LastPositionUpdatePacketReceivedTime + SOFT_LINK_DEATH_THRESHOLD))
            {
                Owner.Client.OnLinkDeath(true);
                return; // Stops all other component processing
            }
        }
        
        // Game rule: Anti-cheat movement validation
        if (_validateMovementOnNextTick)
        {
            _playerMovementMonitor.ValidateMovement();
            _validateMovementOnNextTick = false;
        }
        
        // Game rule: Position broadcasting optimization for network performance
        if (_needBroadcastPosition && ServiceUtils.ShouldTick(_nextPositionBroadcast))
        {
            BroadcastPosition();
            _nextPositionBroadcast = GameLoop.GameLoopTime + BROADCAST_MINIMUM_INTERVAL;
            _needBroadcastPosition = false;
        }
        
        base.TickInternal();
    }
}
```

#### Crafting Components

**CraftComponent**
```csharp
public class CraftComponent : IServiceObject
{
    public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.CraftComponent);
    
    // Crafting progress tracking
    public AbstractCraftingSkill craftingSkill { get; set; }
    public Recipe recipe { get; set; }
    public long startTime { get; set; }
    public long craftingTime { get; set; }
    
    // Game rule: Crafting requires continuous time investment
    public void Tick()
    {
        if (owner.ObjectState is not eObjectState.Active)
        {
            InterruptCrafting();
            return;
        }
        
        long elapsedTime = GameLoop.GameLoopTime - startTime;
        
        // Check if crafting is complete
        if (elapsedTime >= craftingTime)
        {
            CompleteCrafting();
            ServiceObjectStore.Remove(this);
        }
        else
        {
            // Update crafting progress
            double progress = (double)elapsedTime / craftingTime;
            SendCraftingProgress(progress);
        }
    }
    
    // Game rule: Movement or combat interrupts crafting
    private void InterruptCrafting()
    {
        craftingSkill.CraftingState = eCraftingState.Interrupted;
        ServiceObjectStore.Remove(this);
    }
}
```

## Component Lifecycle Management

### Component Registration

#### Service Object Store Integration
```csharp
public static class ServiceObjectStore
{
    private static Dictionary<ServiceObjectType, IServiceObjectArray> _serviceObjectArrays = new()
    {
        { ServiceObjectType.AttackComponent, new ServiceObjectArray<AttackComponent>(1250) },
        { ServiceObjectType.CastingComponent, new ServiceObjectArray<CastingComponent>(1250) },
        { ServiceObjectType.EffectListComponent, new ServiceObjectArray<EffectListComponent>(1250) },
        { ServiceObjectType.MovementComponent, new ServiceObjectArray<MovementComponent>(1250) },
        { ServiceObjectType.CraftComponent, new ServiceObjectArray<CraftComponent>(100) },
        { ServiceObjectType.Timer, new ServiceObjectArray<ECSGameTimer>(500) }
    };
    
    // Thread-safe component registration
    public static bool Add<T>(T serviceObject) where T : class, IServiceObject
    {
        if (serviceObject.ServiceObjectId.IsPendingAddition || serviceObject.ServiceObjectId.IsSet)
            return false;
        
        serviceObject.ServiceObjectId.SetPendingState(PendingState.Adding);
        
        var array = _serviceObjectArrays[serviceObject.ServiceObjectId.Type] as ServiceObjectArray<T>;
        return array.Add(serviceObject);
    }
    
    // Thread-safe component removal
    public static bool Remove<T>(T serviceObject) where T : class, IServiceObject
    {
        if (serviceObject.ServiceObjectId.IsPendingRemoval || !serviceObject.ServiceObjectId.IsSet)
            return false;
        
        serviceObject.ServiceObjectId.SetPendingState(PendingState.Removing);
        
        var array = _serviceObjectArrays[serviceObject.ServiceObjectId.Type] as ServiceObjectArray<T>;
        return array.Remove(serviceObject);
    }
}
```

### Component State Management

#### Pending State Handling
```csharp
public enum PendingState
{
    None,
    Adding,
    Removing
}

// Components in pending states are processed during service updates
public int Update()
{
    if (Interlocked.Exchange(ref _updating, 1) != 0)
        throw new InvalidOperationException($"{typeof(T)} is already being updated.");
    
    try
    {
        // Process removals first to free slots
        if (_itemsToRemove.Any)
        {
            DrainItemsToRemove();
            UpdateLastValidIndexAfterRemoval();
            OptimizeIndexes(); // Compact array for cache efficiency
        }
        
        // Process additions
        if (_itemsToAdd.Any)
            DrainItemsToAdd();
    }
    finally
    {
        _updating = 0;
    }
    
    return _lastValidIndex;
}
```

### Component Memory Management

#### Array Optimization
```csharp
private class ServiceObjectArray<T> : IServiceObjectArray where T : class, IServiceObject
{
    private SortedSet<int> _invalidIndexes = new();
    private DrainArray<T> _itemsToAdd = new();
    private DrainArray<T> _itemsToRemove = new();
    private ExpandableArray<T> Items = new();
    
    // Reuse invalidated slots for cache efficiency
    private void AddInternal(T item)
    {
        ServiceObjectId id = item.ServiceObjectId;
        
        if (_invalidIndexes.Count > 0)
        {
            int index = _invalidIndexes.Min;
            _invalidIndexes.Remove(index);
            Items[index] = item;
            
            if (index > _lastValidIndex)
                _lastValidIndex = index;
            
            id.Value = index;
            return;
        }
        
        // Expand array if needed (20% growth for balanced memory/performance)
        if (++_lastValidIndex >= Items.Capacity)
        {
            int newCapacity = (int)(Items.Capacity * 1.2);
            log.Warn($"{typeof(T)} array resized to {newCapacity}");
            Items.Resize(newCapacity);
        }
        
        Items.Add(item);
        id.Value = _lastValidIndex;
    }
}
```

## Component Interactions

### Cross-Component Dependencies

#### Combat-Casting Interaction
```csharp
// Game rule: Casting interrupts attack components
public void InterruptCasting(bool sendInterruptMessage)
{
    if (Owner.attackComponent?.attackAction != null)
    {
        Owner.attackComponent.attackAction.CleanUp();
        ServiceObjectStore.Remove(Owner.attackComponent);
    }
    
    ServiceObjectStore.Remove(this);
}

// Game rule: Attacking interrupts casting
public void RequestStartAttack(GameObject attackTarget = null)
{
    if (owner.castingComponent != null)
    {
        owner.castingComponent.InterruptCasting(false);
    }
    
    _startAttackTarget = attackTarget ?? owner.TargetObject;
    StartAttackRequested = true;
    ServiceObjectStore.Add(this);
}
```

#### Movement-Combat Integration
```csharp
// Game rule: Movement can trigger attack validation
public void ValidateMovement()
{
    // Check if movement is valid for current attack
    if (owner.attackComponent?.attackAction != null)
    {
        if (!IsWithinAttackRange(owner.attackComponent.attackAction.Target))
        {
            owner.attackComponent.attackAction.CleanUp();
            ServiceObjectStore.Remove(owner.attackComponent);
        }
    }
}
```

### Component Communication Patterns

#### Event-Driven Updates
```csharp
// Components trigger other systems through service store registration
public void CheckForRelocation()
{
    if (subZoneIndex != currentSubZoneIndex)
    {
        // Create zone transition component
        ObjectChangingSubZone.Create(subZoneObject, newZone, newSubZone);
    }
}

// Service dependency chain ensures proper processing order
static void TickServices()
{
    // Input processing
    ClientService.BeginTick();
    
    // Game logic (order matters for component interactions)
    NpcService.Tick();        // AI decisions
    AttackService.Tick();     // Combat actions (may trigger effects)
    CastingService.Tick();    // Spell casting (may trigger effects)
    EffectListService.Tick(); // Apply effects (may trigger other components)
    MovementService.Tick();   // Position updates (may trigger zones)
    
    // World state updates
    ZoneService.Tick();       // Zone transitions
    CraftingService.Tick();   // Crafting progress
    
    // Output processing
    ClientService.EndTick();
}
```

## Specialized Component Types

### Timer Components

#### ECS Timer Integration
```csharp
public class ECSGameTimer : IServiceObject
{
    public delegate int ECSTimerCallback(ECSGameTimer timer);
    
    public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.Timer);
    public GameObject Owner { get; }
    public ECSTimerCallback Callback { private get; set; }
    public long NextTick { get; protected set; }
    
    // Game rule: Timers integrate with ECS for consistent timing
    public void Tick()
    {
        if (Callback != null)
            Interval = Callback.Invoke(this);
        
        if (Interval == 0)
        {
            Stop(); // Auto-cleanup
            return;
        }
        
        NextTick += Interval; // Schedule next execution
    }
}

// Specialized timer for combat mechanics
public class AttackersCheckTimer : ECSGameTimerWrapperBase
{
    protected readonly AttackComponent _attackComponent;
    
    protected override int OnTick(ECSGameTimer timer)
    {
        _attackComponent.CheckForValidAttackers();
        return _attackComponent.owner is EpicNPC ? 500 : 100; // Adaptive interval
    }
}
```

### Brain Components (NPC AI)

#### AI Brain System Integration
```csharp
public abstract class ABrain : IServiceObject
{
    public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.Brain);
    
    // AI state management
    protected GameNPC Body { get; }
    protected AggroList AggroTable { get; }
    protected FSM<eFSMStateType, eFSMTriggerType> FSM { get; }
    
    // Game rule: NPC brains process AI decisions every tick
    public virtual void Tick()
    {
        if (Body?.ObjectState != eObjectState.Active)
        {
            ServiceObjectStore.Remove(this);
            return;
        }
        
        // Process finite state machine
        FSM.ProcessTriggers();
        
        // Update aggro and target selection
        UpdateAggroList();
        SelectTarget();
        
        // Make AI decisions based on current state
        ProcessCurrentState();
    }
    
    // Game rule: Different NPC types have different AI behaviors
    protected virtual void ProcessCurrentState()
    {
        switch (FSM.CurrentState)
        {
            case eFSMStateType.IDLE:
                ProcessIdleState();
                break;
            case eFSMStateType.AGGRO:
                ProcessAggroState();
                break;
            case eFSMStateType.COMBAT:
                ProcessCombatState();
                break;
        }
    }
}
```

## Component Performance Optimization

### Memory Layout Optimization

#### Cache-Friendly Data Structures
```csharp
// Components are stored in arrays for cache efficiency
public class ExpandableArray<T>
{
    private T[] _items;
    private int _count;
    
    // Linear memory layout for optimal cache performance
    public void Resize(int newCapacity)
    {
        T[] newArray = new T[newCapacity];
        Array.Copy(_items, newArray, _count);
        _items = newArray;
    }
    
    // Iteration optimized for cache lines
    public void ForEach(Action<T> action)
    {
        for (int i = 0; i < _count; i++)
        {
            action(_items[i]);
        }
    }
}
```

### Component Batching

#### Parallel Processing Optimization
```csharp
// Components processed in parallel batches
private static void TickInternal(int index)
{
    AttackComponent attackComponent = _list[index];
    
    try
    {
        long startTick = GameLoop.GetRealTime();
        attackComponent.Tick();
        long stopTick = GameLoop.GetRealTime();
        
        // Performance monitoring per component
        if (stopTick - startTick > Diagnostics.LongTickThreshold)
        {
            log.Warn($"Long AttackService tick: {attackComponent.owner.Name} " +
                    $"Time: {stopTick - startTick}ms");
        }
    }
    catch (Exception e)
    {
        // Graceful error handling preserves system stability
        ServiceUtils.HandleServiceException(e, SERVICE_NAME, attackComponent, attackComponent.owner);
    }
}
```

## Component Configuration

### Capacity Management
```xml
<!-- Component pool sizes optimized for typical server load -->
<Property Name="ATTACK_COMPONENT_CAPACITY" Value="1250" />
<Property Name="CASTING_COMPONENT_CAPACITY" Value="1250" />
<Property Name="EFFECT_LIST_COMPONENT_CAPACITY" Value="1250" />
<Property Name="MOVEMENT_COMPONENT_CAPACITY" Value="1250" />
<Property Name="CRAFT_COMPONENT_CAPACITY" Value="100" />
<Property Name="TIMER_CAPACITY" Value="500" />

<!-- Performance monitoring -->
<Property Name="CHECK_ENTITY_COUNTS" Value="true" />
<Property Name="ENABLE_PERFORMANCE_COUNTERS" Value="false" />
```

### Dynamic Scaling
```csharp
// Arrays automatically grow to handle load spikes
if (++_lastValidIndex >= Items.Capacity)
{
    int newCapacity = (int)(Items.Capacity * 1.2); // 20% growth
    log.Warn($"{typeof(T)} array resized to {newCapacity}");
    Items.Resize(newCapacity);
}
```

## Component Error Handling

### Exception Recovery
```csharp
public static void HandleServiceException<T>(Exception exception, string serviceName, 
                                           T entity, GameObject entityOwner) 
    where T : class, IServiceObject
{
    // Remove problematic entity from service
    if (entity != null)
        ServiceObjectStore.Remove(entity);
    
    // Entity-specific recovery actions
    Action action = entity switch
    {
        AttackComponent => () => entityOwner?.attackComponent?.StopAttack(),
        CastingComponent => () => entityOwner?.castingComponent?.InterruptCasting(false),
        MovementComponent => () => entityOwner?.movementComponent?.StopMovement(),
        CraftComponent => () => entityOwner?.craftComponent?.InterruptCrafting(),
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
```

## Future Component Enhancements

### Planned Optimizations

1. **SIMD Processing**: Vector operations for bulk component updates
2. **Component Pooling**: Reuse component instances to reduce allocations
3. **Hierarchical Processing**: Priority-based component processing
4. **Memory Compression**: Pack component data more efficiently
5. **Predictive Loading**: Pre-allocate components based on usage patterns

### Performance Targets

- **Component Registration**: < 0.1ms per operation
- **Component Processing**: < 0.5ms per component tick
- **Memory Usage**: < 50MB for all components combined
- **Garbage Collection**: < 1MB/second allocation rate

## Conclusion

OpenDAoC's ECS component system provides a robust foundation for high-performance game object processing. The separation of data (components) from behavior (services) enables efficient parallel processing while maintaining clear separation of concerns. The sophisticated lifecycle management, memory optimization, and error handling ensure stable operation under high load while preserving authentic DAoC gameplay mechanics.

## Change Log

- **v1.0** (2025-01-20): Complete component system documentation
  - Component architecture and interfaces
  - Lifecycle management and memory optimization
  - All component types and their game rule implementations
  - Cross-component interaction patterns
  - Performance optimization techniques
  - Error handling and recovery mechanisms

## References

- ECS_Game_Loop_Deep_Dive.md - Game loop integration
- Service_Management_System.md - Service coordination
- Timer_Service_System.md - Timer component details
- Object_Pool_System.md - Memory management strategies 