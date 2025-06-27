# Timer Service System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

**Game Rule Summary**: The Timer Service System controls the precise timing of everything in DAoC. It ensures your attack speed is exactly right, spell effects last the correct duration, NPCs think at proper intervals, and all game mechanics run smoothly. When you swing a weapon, cast a spell, or trigger an ability, timers coordinate the exact moment each effect happens. This system prevents lag from affecting game balance by keeping all timing consistent regardless of server load. Whether it's a 2.5-second spell cast, a 30-minute guild buff, or checking if you're still in combat, the timer system makes sure everything happens when it should.

The Timer Service System provides high-performance, ECS-integrated timing functionality for all game mechanics in OpenDAoC. It manages everything from combat timing to spell durations, movement actions, and periodic game events through a sophisticated timer architecture built on the service object system.

## Core Architecture

### Timer Base Classes

```csharp
// Base timer interface for service integration
public interface IServiceObject
{
    ServiceObjectId ServiceObjectId { get; set; }
}

// Core ECS timer implementation
public class ECSGameTimer : IServiceObject
{
    public ServiceObjectId ServiceObjectId { get; set; }
    public GameObject Owner { get; private set; }
    public long Interval { get; private set; }
    public long TimeUntilElapsed { get; private set; }
    public bool IsAlive { get; private set; }
    
    public ECSGameTimer(GameObject owner)
    {
        Owner = owner;
        ServiceObjectId = new ServiceObjectId(ServiceObjectType.Timer);
        IsAlive = true;
    }
    
    public virtual int OnTick()
    {
        // Return next tick interval in milliseconds
        // Return 0 to stop timer
        return 0;
    }
    
    public void Start(long interval)
    {
        Interval = interval;
        TimeUntilElapsed = interval;
        ServiceObjectStore.Add(this);
    }
    
    public void Stop()
    {
        IsAlive = false;
        ServiceObjectStore.Remove(this);
    }
}

// Wrapper base class for common timer operations
public abstract class ECSGameTimerWrapperBase : ECSGameTimer
{
    protected ECSGameTimerWrapperBase(GameObject owner) : base(owner) { }
    
    protected abstract int OnTick(ECSGameTimer timer);
    
    public sealed override int OnTick()
    {
        return OnTick(this);
    }
}
```

### Timer Service Management

```csharp
// Timer service for coordinating timer lifecycle
public class TimerService : IServiceObject
{
    public ServiceObjectId ServiceObjectId { get; set; }
    
    public TimerService()
    {
        ServiceObjectId = new ServiceObjectId(ServiceObjectType.TimerService);
    }
    
    // Continuation-based async timer support
    public void StartTimer<T>(T state, int interval, Func<T, int> callback)
    {
        var timer = new ContinuationActionTimer<T>(null, state, callback);
        timer.Start(interval);
    }
    
    // Simple callback timer
    public ECSGameTimer StartCallback(GameObject owner, int interval, Func<int> callback)
    {
        var timer = new CallbackTimer(owner, callback);
        timer.Start(interval);
        return timer;
    }
}

// Continuation-based timer for async operations
private class ContinuationActionTimer<T> : ECSGameTimerWrapperBase
{
    private readonly T _state;
    private readonly Func<T, int> _callback;
    
    public ContinuationActionTimer(GameObject owner, T state, Func<T, int> callback) 
        : base(owner)
    {
        _state = state;
        _callback = callback;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        return _callback(_state);
    }
}
```

## Timer Categories

### 1. Combat Timers

```csharp
// Attack round timer for combat mechanics
public class StandardAttackersCheckTimer : AttackersCheckTimer
{
    public StandardAttackersCheckTimer(GameObject owner, AttackComponent attackComponent) 
        : base(owner, attackComponent) { }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Check for valid attackers every 100ms
        _attackComponent.CheckForValidAttackers();
        return 100;
    }
}

// Epic NPC special attack timing
public class EpicNpcAttackersCheckTimer : AttackersCheckTimer
{
    protected override int OnTick(ECSGameTimer timer)
    {
        // Longer interval for epic NPCs (performance optimization)
        _attackComponent.CheckForValidAttackers();
        return 500; // 500ms interval
    }
}

// Block round countdown timer
class BlockRoundCountDecrementTimer : ECSGameTimerWrapperBase
{
    private readonly AttackComponent _attackComponent;
    
    public BlockRoundCountDecrementTimer(GameObject owner, AttackComponent attackComponent) 
        : base(owner)
    {
        _attackComponent = attackComponent;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        _attackComponent.BlockRoundCount--;
        if (_attackComponent.BlockRoundCount <= 0)
        {
            return 0; // Stop timer
        }
        return 1000; // 1 second intervals
    }
}
```

### 2. Spell Effect Timers

```csharp
// Spell pulse timer for pulsing effects
private sealed class SpellPulseAction : ECSGameTimerWrapperBase
{
    private readonly PulsingSpellEffect _effect;
    private int _pulseCount;
    
    public SpellPulseAction(GameObject owner, PulsingSpellEffect effect) 
        : base(owner)
    {
        _effect = effect;
        _pulseCount = 0;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        _pulseCount++;
        
        // Execute spell pulse
        _effect.PulseSpell();
        
        // Check if effect should continue
        if (_pulseCount >= _effect.MaxPulses)
        {
            _effect.Cancel(false);
            return 0; // Stop timer
        }
        
        return _effect.PulseFreq; // Next pulse interval
    }
}

// Range check for concentration effects
private sealed class RangeCheckAction : ECSGameTimerWrapperBase
{
    private readonly RegenBuff _effect;
    
    public RangeCheckAction(GameObject owner, RegenBuff effect) 
        : base(owner)
    {
        _effect = effect;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Check if target is still in range
        if (!_effect.IsTargetInRange())
        {
            _effect.Cancel(false);
            return 0;
        }
        
        return 2000; // Check every 2 seconds
    }
}
```

### 3. Movement and Animation Timers

```csharp
// NPC movement timing
private class ResetHeadingAction : ECSGameTimerWrapperBase
{
    private readonly NpcMovementComponent _movementComponent;
    
    public ResetHeadingAction(GameObject owner, NpcMovementComponent movementComponent) 
        : base(owner)
    {
        _movementComponent = movementComponent;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Reset NPC heading after movement pause
        _movementComponent.ResetHeading();
        return 0; // One-shot timer
    }
}

// Horse riding animation timer
protected class HorseRideAction : ECSGameTimerWrapperBase
{
    private readonly GamePlayer _player;
    private readonly int _targetX, _targetY, _targetZ;
    
    public HorseRideAction(GameObject owner, GamePlayer player, int x, int y, int z) 
        : base(owner)
    {
        _player = player;
        _targetX = x;
        _targetY = y;
        _targetZ = z;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Continue horse movement
        bool reachedDestination = _player.MoveTo(_targetX, _targetY, _targetZ);
        
        if (reachedDestination)
        {
            _player.DismountHorse();
            return 0; // Stop timer
        }
        
        return 100; // Continue every 100ms
    }
}
```

### 4. Line of Sight Timers

```csharp
// LoS check timeout timer
public class CheckLosTimer : ECSGameTimerWrapperBase
{
    private readonly NpcAttackAction _attackAction;
    
    public CheckLosTimer(GameObject owner, NpcAttackAction attackAction) 
        : base(owner)
    {
        _attackAction = attackAction;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // LoS check timed out
        _attackAction.OnLosCheckTimeout();
        return 0; // One-shot timer
    }
}

// Client LoS response timeout
public class TimeoutTimer : ECSGameTimerWrapperBase
{
    private readonly CheckLosResponseHandler _handler;
    
    public TimeoutTimer(GameObject owner, CheckLosResponseHandler handler) 
        : base(owner)
    {
        _handler = handler;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Client failed to respond to LoS check
        _handler.OnTimeout();
        return 0;
    }
}
```

### 5. World Object Timers

```csharp
// Auto-close door timer
private class CloseDoorAction : ECSGameTimerWrapperBase
{
    private readonly GameDoor _door;
    
    public CloseDoorAction(GameObject owner, GameDoor door) 
        : base(owner)
    {
        _door = door;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Auto-close door after delay
        _door.Close();
        return 0;
    }
}

// Remove temporary items
protected class RemoveItemAction : ECSGameTimerWrapperBase
{
    private readonly GameStaticItemTimed _item;
    
    public RemoveItemAction(GameObject owner, GameStaticItemTimed item) 
        : base(owner)
    {
        _item = item;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Remove timed item from world
        _item.RemoveFromWorld();
        return 0;
    }
}
```

### 6. Player State Timers

```csharp
// Player quit delay timer
public class QuitTimer : ECSGameTimerWrapperBase
{
    private readonly GamePlayer _player;
    
    public QuitTimer(GameObject owner, GamePlayer player) 
        : base(owner)
    {
        _player = player;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Finish player logout process
        _player.CompleteLogout();
        return 0;
    }
}

// Link death recovery timer
public class LinkDeathTimer : ECSGameTimerWrapperBase
{
    private readonly GamePlayer _player;
    
    public LinkDeathTimer(GameObject owner, GamePlayer player) 
        : base(owner)
    {
        _player = player;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Resurrect player after link death timeout
        if (_player.Health <= 0)
        {
            _player.RevivePlayer();
        }
        return 0;
    }
}

// Invulnerability timer for new players
protected class InvulnerabilityTimer : ECSGameTimerWrapperBase
{
    private readonly GamePlayer _player;
    
    public InvulnerabilityTimer(GameObject owner, GamePlayer player) 
        : base(owner)
    {
        _player = player;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Remove new player invulnerability
        _player.TempProperties.Remove("NEW_PLAYER_INVULNERABILITY");
        _player.Out.SendMessage("You are no longer protected from PvP.", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return 0;
    }
}
```

### 7. Keep and Siege Timers

```csharp
// Siege weapon firing timer
public class SiegeTimer : ECSGameTimerWrapperBase
{
    private readonly GameSiegeWeapon _weapon;
    
    public SiegeTimer(GameObject owner, GameSiegeWeapon weapon) 
        : base(owner)
    {
        _weapon = weapon;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Complete siege weapon firing sequence
        _weapon.CompleteFiring();
        return 0;
    }
}

// Keep hookpoint construction timer
public class HookpointTimer : ECSGameTimerWrapperBase
{
    private readonly KeepHookPoint _hookpoint;
    
    public HookpointTimer(GameObject owner, KeepHookPoint hookpoint) 
        : base(owner)
    {
        _hookpoint = hookpoint;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Complete hookpoint construction
        _hookpoint.CompleteConstruction();
        return 0;
    }
}
```

### 8. Guild and Social Timers

```csharp
// Guild buff timer
public class GuildBuffTimer : ECSGameTimerWrapperBase
{
    private readonly Guild _guild;
    
    public GuildBuffTimer(GameObject owner, Guild guild) 
        : base(owner)
    {
        _guild = guild;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Process guild buff effects
        _guild.ProcessGuildBuffs();
        return 60000; // Every minute
    }
}

// Appeal notification timer
public class NotifyTimer : ECSGameTimerWrapperBase
{
    private readonly Appeal _appeal;
    
    public NotifyTimer(GameObject owner, Appeal appeal) 
        : base(owner)
    {
        _appeal = appeal;
    }
    
    protected override int OnTick(ECSGameTimer timer)
    {
        // Send appeal notification to staff
        _appeal.NotifyStaff();
        return 0;
    }
}
```

## Timer Processing Architecture

### Service Integration

```csharp
// Timer processing through service system
public static class ServiceObjectStore
{
    private static readonly ServiceObjectContainer<ECSGameTimer>[] _timerContainers = 
        new ServiceObjectContainer<ECSGameTimer>[32]; // Thread-safe containers
    
    public static bool Add<T>(T serviceObject) where T : class, IServiceObject
    {
        if (serviceObject is ECSGameTimer timer)
        {
            int containerId = GetContainerForCurrentThread();
            return _timerContainers[containerId].Add(timer);
        }
        return false;
    }
    
    public static bool Remove<T>(T serviceObject) where T : class, IServiceObject
    {
        if (serviceObject is ECSGameTimer timer)
        {
            timer.IsAlive = false;
            return true;
        }
        return false;
    }
    
    // Process all timers for current tick
    public static List<T> UpdateAndGetAll<T>(ServiceObjectType type, out int lastValidIndex)
        where T : class, IServiceObject
    {
        if (type == ServiceObjectType.Timer)
        {
            return ProcessTimerTick() as List<T>;
        }
        
        lastValidIndex = 0;
        return new List<T>();
    }
    
    private static List<ECSGameTimer> ProcessTimerTick()
    {
        var activeTimers = new List<ECSGameTimer>();
        long currentTime = GameLoop.GameLoopTime;
        
        foreach (var container in _timerContainers)
        {
            container.ProcessTick(currentTime, activeTimers);
        }
        
        return activeTimers;
    }
}
```

### Thread-Safe Container

```csharp
public class ServiceObjectContainer<T> : IServiceObjectContainer where T : class, IServiceObject
{
    private readonly T[] _objects;
    private readonly Lock _lock = new();
    private int _count;
    
    public ServiceObjectContainer(int capacity)
    {
        _objects = new T[capacity];
        _count = 0;
    }
    
    public bool Add(T obj)
    {
        using (_lock.EnterScope())
        {
            if (_count >= _objects.Length)
                return false;
                
            _objects[_count++] = obj;
            return true;
        }
    }
    
    public void ProcessTick(long currentTime, List<ECSGameTimer> activeTimers)
    {
        using (_lock.EnterScope())
        {
            for (int i = 0; i < _count; i++)
            {
                var timer = _objects[i] as ECSGameTimer;
                if (timer == null || !timer.IsAlive)
                {
                    // Remove dead timer
                    RemoveAt(i--);
                    continue;
                }
                
                timer.TimeUntilElapsed -= GameLoop.TICK_INTERVAL;
                
                if (timer.TimeUntilElapsed <= 0)
                {
                    // Timer elapsed, call OnTick
                    int nextInterval = timer.OnTick();
                    
                    if (nextInterval <= 0)
                    {
                        // Timer finished
                        RemoveAt(i--);
                        timer.IsAlive = false;
                    }
                    else
                    {
                        // Schedule next tick
                        timer.TimeUntilElapsed = nextInterval;
                        timer.Interval = nextInterval;
                    }
                }
                
                if (timer.IsAlive)
                    activeTimers.Add(timer);
            }
        }
    }
    
    private void RemoveAt(int index)
    {
        if (index < _count - 1)
        {
            Array.Copy(_objects, index + 1, _objects, index, _count - index - 1);
        }
        _objects[--_count] = null;
    }
}
```

## Performance Characteristics

### Timing Precision

```csharp
public static class GameLoop
{
    public const int TICK_INTERVAL = 10; // 10ms ticks = 100 FPS
    public static long GameLoopTime { get; private set; }
    
    // High-resolution timing for critical systems
    public static long GetHighResolutionTime()
    {
        return Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;
    }
}

// Timer precision levels
public enum TimerPrecision
{
    Normal,    // 10ms resolution (game tick)
    High,      // 1ms resolution
    Critical   // Sub-millisecond for combat
}
```

### Memory Management

```csharp
// Object pooling for timer instances
public static class TimerPool
{
    private static readonly ConcurrentQueue<ECSGameTimer> _timerPool = new();
    private static readonly object _lockObj = new();
    
    public static ECSGameTimer GetTimer(GameObject owner)
    {
        if (_timerPool.TryDequeue(out var timer))
        {
            timer.Reset(owner);
            return timer;
        }
        
        return new ECSGameTimer(owner);
    }
    
    public static void ReturnTimer(ECSGameTimer timer)
    {
        if (timer != null)
        {
            timer.Reset(null);
            _timerPool.Enqueue(timer);
        }
    }
}
```

### Load Balancing

```csharp
// Distribute timer load across threads
private static int GetContainerForCurrentThread()
{
    // Use thread ID to distribute timers
    return Thread.CurrentThread.ManagedThreadId % _timerContainers.Length;
}

// Timer interval optimization
public static class TimerOptimization
{
    // Batch similar timers together
    public static int OptimizeInterval(int requestedInterval, TimerType type)
    {
        return type switch
        {
            TimerType.Combat => AlignToInterval(requestedInterval, 10), // 10ms alignment
            TimerType.Movement => AlignToInterval(requestedInterval, 50), // 50ms alignment
            TimerType.Effect => AlignToInterval(requestedInterval, 100), // 100ms alignment
            _ => requestedInterval
        };
    }
    
    private static int AlignToInterval(int interval, int alignment)
    {
        return ((interval + alignment - 1) / alignment) * alignment;
    }
}
```

## Usage Patterns

### Common Timer Creation

```csharp
// Simple one-shot timer
public static void DelayedAction(GameObject owner, int delay, Action action)
{
    var timer = new CallbackTimer(owner, () => { action(); return 0; });
    timer.Start(delay);
}

// Repeating timer with condition
public static ECSGameTimer RepeatUntil(GameObject owner, int interval, Func<bool> condition)
{
    var timer = new ConditionalTimer(owner, condition);
    timer.Start(interval);
    return timer;
}

// Spell effect timer
public static void StartSpellEffect(GameLiving caster, GameLiving target, int duration)
{
    var timer = new SpellEffectTimer(caster, target);
    timer.Start(duration);
}
```

### Timer Best Practices

```csharp
// 1. Always check timer validity
protected override int OnTick(ECSGameTimer timer)
{
    if (Owner == null || !Owner.IsAlive)
        return 0; // Stop timer
        
    // Timer logic here
    return interval;
}

// 2. Use appropriate intervals
// Combat: 10-100ms
// Effects: 100-1000ms  
// Maintenance: 1000ms+

// 3. Cleanup resources
public override void Stop()
{
    try
    {
        // Cleanup timer-specific resources
        CleanupResources();
    }
    finally
    {
        base.Stop();
    }
}
```

## Game Rules Integration

### Combat Timing Rules

- **Attack intervals**: Based on weapon speed and stats
- **Block rounds**: 3-second window with decreasing effectiveness
- **Spell interruption**: 2-second delay after taking damage
- **Style chains**: Must execute within timing windows

### Effect Duration Rules

- **Spell effects**: Exact duration matching spell data
- **Poison/Disease**: Tick every 4 seconds until cured
- **Regeneration**: Pulse every 4 seconds
- **Buffs**: Duration reduced by resistances

### Movement Timing Rules

- **Speed calculation**: Updates every 100ms
- **Position sync**: Client updates every 50ms
- **Horse movement**: Smooth interpolation every 100ms

## Error Handling

### Timer Fault Tolerance

```csharp
protected override int OnTick(ECSGameTimer timer)
{
    try
    {
        return ExecuteTimerLogic();
    }
    catch (Exception ex)
    {
        log.Error($"Timer {GetType().Name} failed on owner {Owner?.Name}: {ex}");
        
        // Fail safe - stop timer on error
        return 0;
    }
}

// Automatic cleanup for dead objects
public override void ValidateOwner()
{
    if (Owner == null || Owner.ObjectState != eObjectState.Active)
    {
        Stop();
    }
}
```

## Testing Framework

### Mock Timer Implementation

```csharp
public class MockTimer : ECSGameTimer
{
    public bool TickCalled { get; private set; }
    public int TickCount { get; private set; }
    public int LastReturnValue { get; private set; }
    
    public MockTimer(GameObject owner) : base(owner) { }
    
    public override int OnTick()
    {
        TickCalled = true;
        TickCount++;
        
        // Test-specific logic
        LastReturnValue = TestTickLogic();
        return LastReturnValue;
    }
    
    protected virtual int TestTickLogic()
    {
        return 0; // Override in tests
    }
}
```

### Timer Testing Utilities

```csharp
public static class TimerTestHelper
{
    public static void AdvanceTime(int milliseconds)
    {
        // Simulate time passage for testing
        GameLoop.GameLoopTime += milliseconds;
        
        // Force timer processing
        ServiceObjectStore.ProcessAllTimers();
    }
    
    public static bool WaitForTimer(ECSGameTimer timer, int maxWaitMs = 5000)
    {
        int waited = 0;
        while (timer.IsAlive && waited < maxWaitMs)
        {
            AdvanceTime(10);
            waited += 10;
        }
        return !timer.IsAlive;
    }
}
```

## Future Enhancements

### TODO: Missing Features
- Timer prioritization system
- Dynamic interval adjustment based on server load
- Timer clustering for batch processing
- Persistent timers that survive server restarts
- Advanced profiling and monitoring tools

## Change Log

- **v1.0** (2025-01-20): Initial comprehensive documentation
  - Complete timer architecture
  - All timer categories documented
  - Performance optimization strategies
  - Testing framework
  - Integration with service system

## References

- ECS_Performance_System.md - Service object architecture
- Server_Performance_System.md - Game loop and threading
- Service_Management_System.md - Service coordination
- Regeneration_System.md - Timer-based health/mana regeneration 