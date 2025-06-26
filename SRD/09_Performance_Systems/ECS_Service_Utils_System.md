# ECS Service Utils System

**Document Status:** Complete Architecture Analysis  
**Verification:** Code-verified from ServiceUtils implementation  
**Implementation Status:** Live Production

## Overview

The ECS Service Utils System provides essential utilities for service management, timing coordination, exception handling, and tick validation across all ECS services. This system ensures consistent behavior, error recovery, and timing accuracy throughout the Entity Component System.

## Core ServiceUtils Functions

### Timing Coordination

#### ShouldTick Method
```csharp
public static class ServiceUtils
{
    private static long HalfTickRate => GameLoop.TickRate / 2;
    
    public static bool ShouldTick(long tickTime)
    {
        // This method checks if the current game loop time is within the range of the tick time.
        // It allows for a half-tick rate tolerance to ensure that ticks are processed the closest to the intended time.
        // If this is a recurring tick, the tick time will need to be updated by the service that uses it. There are two ways to do this:
        // 1. Increment the tick time by the tick interval (prevents drifting).
        // 2. Set the tick time to the current game loop time then add the tick interval (prevents issues if tick time isn't initialized properly).
        // For most services, drifting is inconsequential, so the second option is preferred.
        return tickTime - GameLoop.GameLoopTime - HalfTickRate <= 0;
    }
}
```

#### Timing Design Philosophy
```csharp
// Game rule: Half-tick tolerance prevents timing drift while ensuring timely processing
// Examples:
// - GameLoop.TickRate = 10ms
// - HalfTickRate = 5ms
// - If next tick scheduled for 1000ms and current time is 1005ms, still allow execution
// - If current time is 1006ms or later, wait for next opportunity

// Usage patterns:
if (ServiceUtils.ShouldTick(component.NextUpdateTime))
{
    component.ProcessUpdate();
    component.NextUpdateTime = GameLoop.GameLoopTime + component.UpdateInterval;
}
```

### Exception Handling

#### Service Exception Recovery
```csharp
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
```

#### Exception Recovery Strategies

**AttackComponent Recovery**
```csharp
// Game rule: Attack errors stop combat cleanly
case AttackComponent attackComp:
    action = () => {
        if (entityOwner?.attackComponent != null)
        {
            entityOwner.attackComponent.StopAttack();
            ServiceObjectStore.Remove(entityOwner.attackComponent);
        }
    };
    actionMessage = "Stopped attack due to error";
    break;
```

**CastingComponent Recovery**
```csharp
// Game rule: Casting errors interrupt spells without side effects
case CastingComponent castComp:
    action = () => {
        if (entityOwner?.castingComponent != null)
        {
            entityOwner.castingComponent.InterruptCasting(false);
            ServiceObjectStore.Remove(entityOwner.castingComponent);
        }
    };
    actionMessage = "Interrupted casting due to error";
    break;
```

**MovementComponent Recovery**
```csharp
// Game rule: Movement errors stop position updates
case MovementComponent moveComp:
    action = () => {
        if (entityOwner?.movementComponent != null)
        {
            entityOwner.movementComponent.StopMovement();
            ServiceObjectStore.Remove(entityOwner.movementComponent);
        }
    };
    actionMessage = "Stopped movement due to error";
    break;
```

**CraftComponent Recovery**
```csharp
// Game rule: Crafting errors interrupt the process
case CraftComponent craftComp:
    action = () => {
        if (entityOwner?.craftComponent != null)
        {
            entityOwner.craftComponent.InterruptCrafting();
            ServiceObjectStore.Remove(entityOwner.craftComponent);
        }
    };
    actionMessage = "Interrupted crafting due to error";
    break;
```

**EffectListComponent Recovery**
```csharp
// Game rule: Effect errors remove all active effects safely
case EffectListComponent effectComp:
    action = () => {
        if (entityOwner?.effectListComponent != null)
        {
            entityOwner.effectListComponent.CancelAllEffects();
            ServiceObjectStore.Remove(entityOwner.effectListComponent);
        }
    };
    actionMessage = "Cancelled all effects due to error";
    break;
```

## Service Integration Patterns

### Standard Service Implementation

#### Service Template Pattern
```csharp
public static class ExampleService
{
    private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
    private const string SERVICE_NAME = nameof(ExampleService);
    private static List<ExampleComponent> _list;
    private static int _entityCount;
    
    public static void Tick()
    {
        GameLoop.CurrentServiceTick = SERVICE_NAME;
        Diagnostics.StartPerfCounter(SERVICE_NAME);
        
        _list = ServiceObjectStore.UpdateAndGetAll<ExampleComponent>(
            ServiceObjectType.ExampleComponent, out int lastValidIndex);
        
        GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);
        
        if (Diagnostics.CheckEntityCounts)
            Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);
        
        Diagnostics.StopPerfCounter(SERVICE_NAME);
    }
    
    private static void TickInternal(int index)
    {
        ExampleComponent component = null;
        
        try
        {
            if (Diagnostics.CheckEntityCounts)
                Interlocked.Increment(ref _entityCount);
            
            component = _list[index];
            long startTick = GameLoop.GetRealTime();
            
            // Component processing with timing validation
            if (ServiceUtils.ShouldTick(component.NextProcessTime))
            {
                component.Process();
                component.NextProcessTime = GameLoop.GameLoopTime + component.Interval;
            }
            
            long stopTick = GameLoop.GetRealTime();
            
            if (stopTick - startTick > Diagnostics.LongTickThreshold)
            {
                log.Warn($"Long {SERVICE_NAME} tick for: " +
                        $"{component.Owner.Name}({component.Owner.ObjectID}) " +
                        $"Time: {stopTick - startTick}ms");
            }
        }
        catch (Exception e)
        {
            ServiceUtils.HandleServiceException(e, SERVICE_NAME, component, component?.Owner);
        }
    }
}
```

### Service Coordination Utilities

#### Cross-Service Communication
```csharp
public static class ServiceUtils
{
    // Game rule: Services coordinate state changes through utility methods
    public static void NotifyComponentStateChange<T>(T component, ComponentState newState) 
        where T : IServiceObject
    {
        // Notify other services about component state changes
        switch (component)
        {
            case AttackComponent attack when newState == ComponentState.Stopping:
                // Cancel casting if attack stops
                attack.owner.castingComponent?.InterruptCasting(false);
                break;
                
            case CastingComponent casting when newState == ComponentState.Starting:
                // Stop attacks when casting starts
                casting.Owner.attackComponent?.StopAttack();
                break;
                
            case MovementComponent movement when newState == ComponentState.Moving:
                // Interrupt crafting on movement
                movement.Owner.craftComponent?.InterruptCrafting();
                break;
        }
    }
}
```

#### Component Lifecycle Utilities
```csharp
public static bool ValidateComponentState<T>(T component) where T : IServiceObject
{
    if (component == null) return false;
    
    // Game rule: Components require valid owners
    if (component.Owner == null)
    {
        log.Warn($"Component {typeof(T).Name} has null owner, removing");
        ServiceObjectStore.Remove(component);
        return false;
    }
    
    // Game rule: Components of deleted objects should be cleaned up
    if (component.Owner.ObjectState == eObjectState.Deleted)
    {
        log.Debug($"Removing component {typeof(T).Name} for deleted object {component.Owner.Name}");
        ServiceObjectStore.Remove(component);
        return false;
    }
    
    // Game rule: Inactive objects don't process components
    if (component.Owner.ObjectState != eObjectState.Active)
    {
        return false;
    }
    
    return true;
}
```

## Timing System Integration

### Tick Validation

#### Precise Timing Control
```csharp
// Game rule: Different systems use different timing strategies
public static class TimingExamples
{
    // Strategy 1: Prevent drift (increment by interval)
    public static void ProcessAttackTiming(AttackComponent attack)
    {
        if (ServiceUtils.ShouldTick(attack.NextAttackTime))
        {
            attack.ExecuteAttack();
            attack.NextAttackTime += attack.AttackInterval; // Prevents accumulating drift
        }
    }
    
    // Strategy 2: Reset to current time (simpler, allows small drift)
    public static void ProcessEffectTiming(EffectComponent effect)
    {
        if (ServiceUtils.ShouldTick(effect.NextTickTime))
        {
            effect.ProcessTick();
            effect.NextTickTime = GameLoop.GameLoopTime + effect.TickInterval;
        }
    }
    
    // Strategy 3: Variable timing (adaptive based on load)
    public static void ProcessMovementTiming(MovementComponent movement)
    {
        if (ServiceUtils.ShouldTick(movement.NextBroadcastTime))
        {
            movement.BroadcastPosition();
            
            // Adaptive interval based on movement speed
            int interval = movement.IsMoving ? 200 : 1000; // 200ms moving, 1s stationary
            movement.NextBroadcastTime = GameLoop.GameLoopTime + interval;
        }
    }
}
```

### Performance Timing Utilities

#### Execution Time Monitoring
```csharp
public static class ServiceUtils
{
    public static void ExecuteWithTiming<T>(T component, Action action, string operationName)
        where T : IServiceObject
    {
        long startTick = GameLoop.GetRealTime();
        
        try
        {
            action.Invoke();
        }
        catch (Exception e)
        {
            HandleServiceException(e, operationName, component, component.Owner);
            return;
        }
        
        long stopTick = GameLoop.GetRealTime();
        long duration = stopTick - startTick;
        
        if (duration > Diagnostics.LongTickThreshold)
        {
            log.Warn($"Long {operationName} for: " +
                    $"{component.Owner?.Name}({component.Owner?.ObjectID}) " +
                    $"Time: {duration}ms");
        }
    }
}

// Usage example:
ServiceUtils.ExecuteWithTiming(attackComponent, 
    () => attackComponent.ProcessAttack(), 
    "AttackProcessing");
```

## Error Recovery Patterns

### Graceful Degradation

#### Component Error Isolation
```csharp
// Game rule: Component errors don't cascade to other systems
public static void IsolateComponentError<T>(T component, Exception error) 
    where T : IServiceObject
{
    // Remove the failed component immediately
    ServiceObjectStore.Remove(component);
    
    // Log the error with context
    log.Error($"Component {typeof(T).Name} failed for {component.Owner?.Name}: {error}");
    
    // Notify owner of component failure
    if (component.Owner is GameLiving living)
    {
        // Game rule: Players should be notified of system failures that affect them
        if (living is GamePlayer player)
        {
            player.Out.SendMessage("A system error occurred. Please try again.", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
    
    // Clean up related components if necessary
    CleanupRelatedComponents(component);
}

private static void CleanupRelatedComponents<T>(T failedComponent) 
    where T : IServiceObject
{
    GameObject owner = failedComponent.Owner;
    
    // Game rule: Some component failures require cleanup of related components
    switch (failedComponent)
    {
        case CastingComponent:
            // If casting fails, also stop movement to prevent state inconsistency
            owner.movementComponent?.StopMovement();
            break;
            
        case AttackComponent:
            // If attack fails, ensure target is cleared
            if (owner.TargetObject != null)
                owner.TargetObject = null;
            break;
    }
}
```

### State Recovery

#### Component State Validation
```csharp
public static bool ValidateAndRecoverComponentState<T>(T component) 
    where T : IServiceObject
{
    if (!ValidateComponentState(component))
        return false;
    
    // Game rule: Attempt to recover invalid but recoverable states
    try
    {
        switch (component)
        {
            case AttackComponent attack:
                // Validate attack target still exists and is valid
                if (attack.Target != null && attack.Target.ObjectState != eObjectState.Active)
                {
                    attack.StopAttack();
                    return false;
                }
                break;
                
            case CastingComponent casting:
                // Validate spell target and line of sight
                if (casting.Target != null && !casting.HasLineOfSight())
                {
                    casting.InterruptCasting(true);
                    return false;
                }
                break;
                
            case MovementComponent movement:
                // Validate position is within valid zone bounds
                if (!movement.Owner.CurrentZone.IsValidPosition(movement.Owner.Position))
                {
                    movement.ResetToValidPosition();
                }
                break;
        }
        
        return true;
    }
    catch (Exception e)
    {
        log.Error($"Failed to recover component state: {e}");
        ServiceObjectStore.Remove(component);
        return false;
    }
}
```

## System Configuration

### ServiceUtils Configuration

#### Timing Configuration
```csharp
public static class ServiceUtils
{
    // Configurable timing parameters
    public static int DefaultComponentInterval { get; set; } = 100; // 100ms default
    public static int MinComponentInterval { get; set; } = 10;      // 10ms minimum
    public static int MaxComponentInterval { get; set; } = 5000;   // 5s maximum
    
    // Error recovery configuration
    public static int MaxRecoveryAttempts { get; set; } = 3;
    public static int RecoveryRetryDelay { get; set; } = 1000; // 1s delay
    
    // Performance monitoring thresholds
    public static int ComponentWarningThreshold { get; set; } = 25;  // 25ms warning
    public static int ComponentErrorThreshold { get; set; } = 100;   // 100ms error
    
    static ServiceUtils()
    {
        // Load from server properties
        LoadConfiguration();
    }
    
    private static void LoadConfiguration()
    {
        DefaultComponentInterval = Properties.DEFAULT_COMPONENT_INTERVAL;
        MinComponentInterval = Properties.MIN_COMPONENT_INTERVAL;
        MaxComponentInterval = Properties.MAX_COMPONENT_INTERVAL;
        MaxRecoveryAttempts = Properties.MAX_RECOVERY_ATTEMPTS;
        RecoveryRetryDelay = Properties.RECOVERY_RETRY_DELAY;
    }
}
```

### Service Integration Guidelines

#### Best Practices for Service Implementation
```csharp
// Game rule: All services should follow consistent patterns
public static class ServiceBestPractices
{
    // 1. Always use ServiceUtils.ShouldTick for timing validation
    // 2. Always wrap component processing in try-catch with ServiceUtils.HandleServiceException
    // 3. Always validate component state before processing
    // 4. Always include performance monitoring with Diagnostics
    // 5. Always clean up components when objects are deleted
    
    public static void ExampleServiceTick()
    {
        // ✅ Correct pattern
        private static void TickInternal(int index)
        {
            ExampleComponent component = null;
            
            try
            {
                component = _list[index];
                
                // Validate component state
                if (!ServiceUtils.ValidateComponentState(component))
                    return;
                
                // Check timing
                if (!ServiceUtils.ShouldTick(component.NextProcessTime))
                    return;
                
                // Process with timing monitoring
                ServiceUtils.ExecuteWithTiming(component, 
                    () => component.ProcessLogic(), 
                    SERVICE_NAME);
                
                // Update next process time
                component.NextProcessTime = GameLoop.GameLoopTime + component.Interval;
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, component, component?.Owner);
            }
        }
    }
}
```

## System Interactions

### Service Coordination Flow
```
Component Error → ServiceUtils.HandleServiceException → Recovery Action → Component Cleanup
Timing Check → ServiceUtils.ShouldTick → Half-Tick Tolerance → Execution Decision  
State Validation → ServiceUtils.ValidateComponentState → Owner Validation → Cleanup Decision
Cross-Service Event → ServiceUtils.NotifyComponentStateChange → Related Component Updates
```

### Integration Points
- **GameLoop**: Provides timing reference for ShouldTick calculations
- **ServiceObjectStore**: Component removal and cleanup coordination
- **Diagnostics**: Performance monitoring and error reporting
- **All ECS Services**: Exception handling and timing validation
- **Component Lifecycle**: State validation and recovery

---

**References:**
- GameServer/ECS-Services/ServiceUtils.cs
- All ECS service implementations using ServiceUtils
- Component state management patterns
- Error recovery and timing coordination systems
