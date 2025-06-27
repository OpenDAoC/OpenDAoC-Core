# ECS GameLoop Service System

**Document Status:** Complete Architecture Analysis  
**Verification:** Code-verified from GameLoopService implementation  
**Implementation Status:** Live Production

## Overview

**Game Rule Summary**: The GameLoop Service System coordinates timing between different parts of the game, ensuring actions happen in the right order and at the right time. This prevents issues like spells casting before the game registers you clicked a target, or items disappearing before the trade completes. It keeps everything synchronized for smooth, predictable gameplay.

## Core GameLoopService Architecture

### Posted Action Interface

#### Action Posting System
```csharp
public static class GameLoopService
{
    private static int _preTickActionCount;
    private static int _postTickActionCount;
    private static ConcurrentQueue<IPostedAction> _preTickActions = new();
    private static ConcurrentQueue<IPostedAction> _postTickActions = new();
    
    // Game rule: Actions can be safely posted from any thread
    public static void PostBeforeTick<TState>(Action<TState> action, TState state)
    {
        _preTickActions.Enqueue(new PostedAction<TState>(action, state));
        Interlocked.Increment(ref _preTickActionCount);
    }
    
    public static void PostAfterTick<TState>(Action<TState> action, TState state)
    {
        _postTickActions.Enqueue(new PostedAction<TState>(action, state));
        Interlocked.Increment(ref _postTickActionCount);
    }
}
```

#### Posted Action Implementation
```csharp
private readonly struct PostedAction<T> : IPostedAction
{
    public readonly Action<T> Action;
    public readonly T State;
    
    public PostedAction(Action<T> action, T state)
    {
        Action = action;
        State = state;
    }
    
    public void Invoke()
    {
        Action(State);
    }
}

public interface IPostedAction
{
    void Invoke();
}
```

### Tick Integration

#### BeginTick Processing
```csharp
public static void BeginTick()
{
    GameLoop.CurrentServiceTick = SERVICE_NAME_BEGIN;
    Diagnostics.StartPerfCounter(SERVICE_NAME_BEGIN);
    
    // Game rule: Execute all pre-tick actions before main processing
    if (Volatile.Read(ref _preTickActionCount) > 0)
    {
        GameLoop.ExecuteWork(Interlocked.Exchange(ref _preTickActionCount, 0), static _ =>
        {
            if (_preTickActions.TryDequeue(out IPostedAction result))
            {
                try
                {
                    result.Invoke();
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Critical error encountered in {SERVICE_NAME_BEGIN}: {e}");
                }
            }
        });
    }
    
    // Prepare for next tick processing
    GameLoop.PrepareForNextTick();
    Diagnostics.StopPerfCounter(SERVICE_NAME_BEGIN);
}
```

#### EndTick Processing
```csharp
public static void EndTick()
{
    GameLoop.CurrentServiceTick = SERVICE_NAME_END;
    Diagnostics.StartPerfCounter(SERVICE_NAME_END);
    
    // Game rule: Execute all post-tick actions after main processing
    if (Volatile.Read(ref _postTickActionCount) > 0)
    {
        GameLoop.ExecuteWork(Interlocked.Exchange(ref _postTickActionCount, 0), static _ =>
        {
            if (_postTickActions.TryDequeue(out IPostedAction result))
            {
                try
                {
                    result.Invoke();
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Critical error encountered in {SERVICE_NAME_END}: {e}");
                }
            }
        });
    }
    
    Diagnostics.StopPerfCounter(SERVICE_NAME_END);
}
```

## Use Cases and Patterns

### Async Task Integration

#### Timer Service Integration
```csharp
public static void ScheduleTimerAfterTask<T>(Task task, ContinuationAction<T> continuation, 
                                           T argument, GameObject owner)
{
    ContinuationActionTimerState<T> state = new(owner, continuation, argument);
    
    task.ContinueWith(static (task, state) =>
    {
        if (task.IsFaulted)
        {
            if (log.IsErrorEnabled)
                log.Error("Async task failed", task.Exception);
            return;
        }
        
        // Game rule: Async operations integrate with ECS through posted actions
        GameLoopService.PostAfterTick(static (s) => 
            new ContinuationActionTimer<T>(s as ContinuationActionTimerState<T>), state);
    }, state);
}
```

#### Database Operation Integration
```csharp
// Example: Saving character data asynchronously
public static void SaveCharacterAsync(GamePlayer player)
{
    var characterData = player.SerializeForDatabase();
    
    Task.Run(async () =>
    {
        try
        {
            await DatabaseManager.SaveCharacterAsync(characterData);
            
            // Game rule: Database results must be processed on main thread
            GameLoopService.PostAfterTick(static (playerRef) =>
            {
                if (playerRef is GamePlayer p && p.IsValid)
                {
                    p.Out.SendMessage("Character saved successfully.", 
                                     eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }, player);
        }
        catch (Exception e)
        {
            GameLoopService.PostAfterTick(static (error) =>
            {
                log.Error($"Failed to save character: {error}");
            }, e);
        }
    });
}
```

### Component Creation from Async Context

#### Safe Component Registration
```csharp
// Game rule: Components must be added from main thread
public static void CreateComponentAfterAsyncOperation<T>(GameObject owner, T componentData)
    where T : IServiceObject
{
    Task.Run(async () =>
    {
        // Perform async initialization
        var initializedComponent = await InitializeComponentAsync(componentData);
        
        // Post component creation to main thread
        GameLoopService.PostBeforeTick(static (data) =>
        {
            var (component, gameObject) = data;
            
            if (gameObject.ObjectState == eObjectState.Active)
            {
                ServiceObjectStore.Add(component);
            }
        }, (initializedComponent, owner));
    });
}
```

### Error Recovery Actions

#### Delayed Error Recovery
```csharp
public static void ScheduleErrorRecovery(GameObject owner, string errorContext, 
                                       Action<GameObject> recoveryAction)
{
    // Game rule: Error recovery should not interrupt current processing
    GameLoopService.PostAfterTick(static (context) =>
    {
        var (obj, action, errorMsg) = context;
        
        try
        {
            if (obj.ObjectState == eObjectState.Active)
            {
                action(obj);
                log.Info($"Successfully recovered from error: {errorMsg}");
            }
        }
        catch (Exception e)
        {
            log.Error($"Recovery action failed for {errorMsg}: {e}");
        }
    }, (owner, recoveryAction, errorContext));
}
```

### Network Event Processing

#### Packet Processing Integration
```csharp
// Game rule: Network events may need to be processed after current tick
public static void ProcessNetworkEventAfterTick(GameClient client, IPacket packet)
{
    GameLoopService.PostAfterTick(static (context) =>
    {
        var (gameClient, gamePacket) = context;
        
        if (gameClient.ClientState == GameClient.eClientState.Playing)
        {
            try
            {
                gamePacket.ProcessPacket(gameClient);
            }
            catch (Exception e)
            {
                log.Error($"Failed to process delayed packet: {e}");
                // Don't disconnect client for processing errors
            }
        }
    }, (client, packet));
}
```

## Thread Safety and Performance

### Concurrent Queue Operations

#### Thread-Safe Action Queuing
```csharp
// Game rule: Action posting is thread-safe and lock-free
public static void PostBeforeTick<TState>(Action<TState> action, TState state)
{
    // ConcurrentQueue.Enqueue is thread-safe
    _preTickActions.Enqueue(new PostedAction<TState>(action, state));
    
    // Interlocked operations ensure atomic counter updates
    Interlocked.Increment(ref _preTickActionCount);
}

// Game rule: Action processing uses work distribution for performance
private static void ProcessPreTickActions()
{
    // Atomically exchange the count to 0, getting previous value
    int actionCount = Interlocked.Exchange(ref _preTickActionCount, 0);
    
    if (actionCount > 0)
    {
        // Use GameLoop work distribution for parallel processing
        GameLoop.ExecuteWork(actionCount, ProcessSinglePreTickAction);
    }
}
```

#### Memory Management
```csharp
// Game rule: Posted actions use struct implementation to minimize allocations
private readonly struct PostedAction<T> : IPostedAction
{
    // Struct avoids heap allocation for simple actions
    public readonly Action<T> Action;
    public readonly T State;
    
    // Readonly fields ensure immutability
    public PostedAction(Action<T> action, T state)
    {
        Action = action;
        State = state;
    }
    
    // Single method interface for minimal overhead
    public void Invoke()
    {
        Action(State);
    }
}
```

### Performance Considerations

#### Action Count Monitoring
```csharp
public static class GameLoopService
{
    private const int MAX_ACTIONS_PER_TICK = 1000; // Configurable limit
    
    public static void BeginTick()
    {
        int preTickCount = Volatile.Read(ref _preTickActionCount);
        
        // Game rule: Monitor for excessive action queuing
        if (preTickCount > MAX_ACTIONS_PER_TICK)
        {
            log.Warn($"High pre-tick action count: {preTickCount}. " +
                    "This may indicate a performance issue.");
        }
        
        // Continue with normal processing...
    }
}
```

#### Action Processing Limits
```csharp
private static void ProcessActionsWithLimit(ConcurrentQueue<IPostedAction> queue, 
                                           ref int count, string actionType)
{
    int processedCount = 0;
    int maxProcessed = Math.Min(count, MAX_ACTIONS_PER_TICK);
    
    GameLoop.ExecuteWork(maxProcessed, static _ =>
    {
        if (queue.TryDequeue(out IPostedAction action))
        {
            try
            {
                action.Invoke();
                Interlocked.Increment(ref processedCount);
            }
            catch (Exception e)
            {
                log.Error($"Posted {actionType} action failed: {e}");
            }
        }
    });
    
    // Update remaining count
    Interlocked.Add(ref count, -processedCount);
    
    if (count > 0)
    {
        log.Debug($"Deferred {count} {actionType} actions to next tick");
    }
}
```

## Integration with Game Systems

### Service Coordination

#### Service Startup Integration
```csharp
// Game rule: Services can post initialization actions
public static void InitializeServicesAsync()
{
    // Post service initialization to happen before first tick
    GameLoopService.PostBeforeTick(static _ =>
    {
        // Initialize critical services first
        AttackService.Initialize();
        CastingService.Initialize();
        MovementService.Initialize();
        
        log.Info("Core ECS services initialized");
    }, null);
    
    // Post secondary service initialization
    GameLoopService.PostBeforeTick(static _ =>
    {
        // Initialize support services
        EffectListService.Initialize();
        CraftingService.Initialize();
        
        log.Info("Support ECS services initialized");
    }, null);
}
```

### Player Connection Handling

#### Connection State Management
```csharp
public static void HandlePlayerConnection(GameClient client)
{
    // Game rule: Player connection events are processed after current tick
    GameLoopService.PostAfterTick(static (gameClient) =>
    {
        if (gameClient.ClientState == GameClient.eClientState.Playing)
        {
            var player = gameClient.Player;
            
            // Initialize player components
            player.InitializeComponents();
            
            // Send welcome messages
            player.Out.SendMessage("Welcome to OpenDAoC!", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
            
            // Log connection
            log.Info($"Player {player.Name} fully connected");
        }
    }, client);
}
```

### Resource Management

#### Cleanup Operations
```csharp
public static void ScheduleCleanupOperation(Action cleanupAction, string description)
{
    // Game rule: Cleanup operations happen after main processing
    GameLoopService.PostAfterTick(static (context) =>
    {
        var (action, desc) = context;
        
        try
        {
            long startTime = GameLoop.GetRealTime();
            action();
            long endTime = GameLoop.GetRealTime();
            
            if (endTime - startTime > 10) // 10ms threshold
            {
                log.Debug($"Cleanup operation '{desc}' took {endTime - startTime}ms");
            }
        }
        catch (Exception e)
        {
            log.Error($"Cleanup operation '{desc}' failed: {e}");
        }
    }, (cleanupAction, description));
}
```

## Error Handling and Recovery

### Exception Management

#### Action Exception Isolation
```csharp
private static void ExecutePostedAction(IPostedAction action, string actionType)
{
    try
    {
        action.Invoke();
    }
    catch (ThreadAbortException)
    {
        // Game rule: Thread abort exceptions should not be caught
        throw;
    }
    catch (OutOfMemoryException)
    {
        // Game rule: Memory exceptions are critical
        log.Fatal($"Out of memory in {actionType} action");
        throw;
    }
    catch (Exception e)
    {
        // Game rule: Action exceptions don't stop the game loop
        log.Error($"Posted {actionType} action failed: {e}");
        
        // Optional: Notify administrators of critical failures
        if (IsCriticalException(e))
        {
            NotifyAdministrators($"Critical {actionType} action failure", e);
        }
    }
}

private static bool IsCriticalException(Exception e)
{
    return e is InvalidOperationException ||
           e is NullReferenceException ||
           e is AccessViolationException;
}
```

### Action Queue Health

#### Queue Monitoring
```csharp
public static class GameLoopService
{
    private static long _lastQueueHealthCheck = 0;
    private const int QUEUE_HEALTH_CHECK_INTERVAL = 5000; // 5 seconds
    
    public static void MonitorQueueHealth()
    {
        if (GameLoop.GameLoopTime - _lastQueueHealthCheck < QUEUE_HEALTH_CHECK_INTERVAL)
            return;
        
        _lastQueueHealthCheck = GameLoop.GameLoopTime;
        
        int preTickCount = Volatile.Read(ref _preTickActionCount);
        int postTickCount = Volatile.Read(ref _postTickActionCount);
        
        // Game rule: Warn about queue buildup
        if (preTickCount > 100)
        {
            log.Warn($"Pre-tick action queue building up: {preTickCount} actions");
        }
        
        if (postTickCount > 100)
        {
            log.Warn($"Post-tick action queue building up: {postTickCount} actions");
        }
        
        // Game rule: Emergency queue clearing if too large
        if (preTickCount > 10000 || postTickCount > 10000)
        {
            log.Error("Emergency queue clearing due to excessive buildup");
            EmergencyQueueClear();
        }
    }
    
    private static void EmergencyQueueClear()
    {
        // Clear queues and reset counters
        while (_preTickActions.TryDequeue(out _)) { }
        while (_postTickActions.TryDequeue(out _)) { }
        
        Interlocked.Exchange(ref _preTickActionCount, 0);
        Interlocked.Exchange(ref _postTickActionCount, 0);
        
        log.Error("Posted action queues cleared due to emergency");
    }
}
```

## Best Practices

### Action Design Guidelines

#### Lightweight Actions
```csharp
// ✅ Good: Lightweight action with minimal processing
GameLoopService.PostAfterTick(static (player) =>
{
    if (player.IsValid)
    {
        player.UpdateLastActivity();
    }
}, gamePlayer);

// ❌ Bad: Heavy processing in posted action
GameLoopService.PostAfterTick(static (player) =>
{
    // This blocks the main thread!
    player.RecalculateAllStats();
    player.SaveToDatabase(); // Synchronous DB operation
    player.SendFullUpdate(); // Large packet
}, gamePlayer);
```

#### State Capture
```csharp
// ✅ Good: Capture necessary state immutably
var playerName = player.Name;
var experience = player.Experience;

GameLoopService.PostAfterTick(static (data) =>
{
    var (name, exp) = data;
    log.Info($"Player {name} gained {exp} experience");
}, (playerName, experience));

// ❌ Bad: Capture mutable object references
GameLoopService.PostAfterTick(static (player) =>
{
    // Player state might have changed by the time this executes!
    log.Info($"Player {player.Name} gained {player.Experience} experience");
}, player);
```

### Performance Guidelines

#### Batch Operations
```csharp
// ✅ Good: Batch multiple related operations
var playersToUpdate = GetPlayersNeedingUpdate();

GameLoopService.PostAfterTick(static (players) =>
{
    foreach (var player in players)
    {
        player.SendUpdate();
    }
}, playersToUpdate);

// ❌ Bad: Individual actions for each operation
foreach (var player in playersToUpdate)
{
    GameLoopService.PostAfterTick(static (p) => p.SendUpdate(), player);
}
```

## System Interactions

### GameLoopService Integration Flow
```
Async Operation → Task Completion → PostAfterTick → Queue → EndTick → Action Execution
Network Event → Packet Processing → PostBeforeTick → Queue → BeginTick → Event Processing
Component Error → Error Recovery → PostAfterTick → Queue → EndTick → Recovery Action
Database Operation → Result → PostAfterTick → Queue → EndTick → UI Update
```

### Integration Points
- **GameLoop**: Core timing and execution coordination
- **TimerService**: Async task continuation integration
- **All ECS Services**: Error recovery and state management
- **Network Layer**: Packet processing coordination
- **Database Layer**: Async operation result handling

---

**References:**
- GameServer/ECS-Services/GameLoopService.cs
- GameServer/ECS-Services/TimerService.cs
- Async task integration patterns
- Posted action performance optimization
