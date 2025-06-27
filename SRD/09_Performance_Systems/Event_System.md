# Event System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from GameEventMgr.cs, DOLEvent.cs, DOLEventHandlerCollection.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: The event system lets different parts of the game respond to things that happen to your character. When you level up, die, cast a spell, or perform other actions, the game automatically triggers related effects like title checks, quest updates, and bonuses without you having to do anything extra.

The Event System provides a robust observer pattern implementation that enables decoupled communication between game systems. It supports both global and object-specific event handlers with weak reference patterns to prevent memory leaks and thread-safe operations.

## Core Architecture

### Event Base Class
```csharp
public abstract class DOLEvent
{
    protected string m_EventName;
    
    public DOLEvent(string name)
    {
        m_EventName = name;
    }
    
    public string Name { get { return m_EventName; } }
    
    public virtual bool IsValidFor(object o)
    {
        return true; // Override for type-specific validation
    }
}
```

### Event Handler Delegate
```csharp
public delegate void DOLEventHandler(DOLEvent e, object sender, EventArgs arguments);
```

### Event Categories

#### GameObject Events
```csharp
public class GameObjectEvent : DOLEvent
{
    public override bool IsValidFor(object o)
    {
        return o is GameObject;
    }
    
    // Standard object events
    public static readonly GameObjectEvent AddToWorld = new GameObjectEvent("GameObject.AddToWorld");
    public static readonly GameObjectEvent RemoveFromWorld = new GameObjectEvent("GameObject.RemoveFromWorld");
    public static readonly GameObjectEvent Interact = new GameObjectEvent("GameObject.Interact");
    public static readonly GameObjectEvent ReceiveItem = new GameObjectEvent("GameObject.ReceiveItem");
    public static readonly GameObjectEvent WalkTo = new GameObjectEvent("GameObject.WalkTo");
}
```

#### GameLiving Events
```csharp
public class GameLivingEvent : GameObjectEvent
{
    public override bool IsValidFor(object o)
    {
        return o is GameLiving;
    }
    
    // Life cycle events
    public static readonly GameLivingEvent Dying = new GameLivingEvent("GameLiving.Dying");
    public static readonly GameLivingEvent ReviveRequested = new GameLivingEvent("GameLiving.ReviveRequested");
    
    // Combat events
    public static readonly GameLivingEvent AttackFinished = new GameLivingEvent("GameLiving.AttackFinished");
    public static readonly GameLivingEvent TakeDamage = new GameLivingEvent("GameLiving.TakeDamage");
    
    // Movement events
    public static readonly GameLivingEvent Moving = new GameLivingEvent("GameLiving.Moving");
    public static readonly GameLivingEvent ArrivedAtTarget = new GameLivingEvent("GameLiving.ArrivedAtTarget");
    
    // Effect events
    public static readonly GameLivingEvent EffectStart = new GameLivingEvent("GameLiving.EffectStart");
    public static readonly GameLivingEvent EffectExpires = new GameLivingEvent("GameLiving.EffectExpires");
    
    // Spell events
    public static readonly GameLivingEvent CastStarting = new GameLivingEvent("GameLiving.CastStarting");
    public static readonly GameLivingEvent CastFinished = new GameLivingEvent("GameLiving.CastFinished");
    public static readonly GameLivingEvent CastFailed = new GameLivingEvent("GameLiving.CastFailed");
}
```

#### GamePlayer Events
```csharp
public class GamePlayerEvent : GameLivingEvent
{
    public override bool IsValidFor(object o)
    {
        return o is GamePlayer;
    }
    
    // Connection events
    public static readonly GamePlayerEvent GameEntered = new GamePlayerEvent("GamePlayer.GameEntered");
    public static readonly GamePlayerEvent Quit = new GamePlayerEvent("GamePlayer.Quit");
    public static readonly GamePlayerEvent Linkdeath = new GamePlayerEvent("GamePlayer.Linkdeath");
    public static readonly GamePlayerEvent RegionChanged = new GamePlayerEvent("GamePlayer.RegionChanged");
    
    // Progression events
    public static readonly GamePlayerEvent LevelUp = new GamePlayerEvent("GamePlayer.LevelUp");
    public static readonly GamePlayerEvent GainedExperience = new GamePlayerEvent("GamePlayer.GainedExperience");
    public static readonly GamePlayerEvent GainedRealmPoints = new GamePlayerEvent("GamePlayer.GainedRealmPoints");
    public static readonly GamePlayerEvent GainedBountyPoints = new GamePlayerEvent("GamePlayer.GainedBountyPoints");
    
    // Social events
    public static readonly GamePlayerEvent AcceptGroup = new GamePlayerEvent("GamePlayer.AcceptGroup");
    public static readonly GamePlayerEvent DeclineGroup = new GamePlayerEvent("GamePlayer.DeclineGroup");
    public static readonly GamePlayerEvent JoinedGroup = new GamePlayerEvent("GamePlayer.JoinedGroup");
    public static readonly GamePlayerEvent LeftGroup = new GamePlayerEvent("GamePlayer.LeftGroup");
    
    // Inventory events
    public static readonly GamePlayerEvent ItemDropped = new GamePlayerEvent("GamePlayer.ItemDropped");
    public static readonly GamePlayerEvent ItemLooted = new GamePlayerEvent("GamePlayer.ItemLooted");
    public static readonly GamePlayerEvent ItemUsed = new GamePlayerEvent("GamePlayer.ItemUsed");
    
    // Command events
    public static readonly GamePlayerEvent UseSlashCommand = new GamePlayerEvent("GamePlayer.UseSlashCommand");
    public static readonly GamePlayerEvent ChatMessage = new GamePlayerEvent("GamePlayer.ChatMessage");
    public static readonly GamePlayerEvent Emote = new GamePlayerEvent("GamePlayer.Emote");
    
    // Guild events
    public static readonly GamePlayerEvent GuildJoin = new GamePlayerEvent("GamePlayer.GuildJoin");
    public static readonly GamePlayerEvent GuildLeave = new GamePlayerEvent("GamePlayer.GuildLeave");
    
    // Realm warfare
    public static readonly GamePlayerEvent KilledByPlayer = new GamePlayerEvent("GamePlayer.KilledByPlayer");
    public static readonly GamePlayerEvent PlayerKilled = new GamePlayerEvent("GamePlayer.PlayerKilled");
}
```

#### Server Events
```csharp
public class GameServerEvent : DOLEvent
{
    // Server lifecycle
    public static readonly GameServerEvent Started = new GameServerEvent("Server.Started");
    public static readonly GameServerEvent Stopped = new GameServerEvent("Server.Stopped");
    public static readonly GameServerEvent WorldSave = new GameServerEvent("Server.WorldSave");
}
```

#### Region Events
```csharp
public class RegionEvent : DOLEvent
{
    public override bool IsValidFor(object o)
    {
        return o is Region;
    }
    
    public static readonly RegionEvent PlayerEnter = new RegionEvent("RegionEvent.PlayerEnter");
    public static readonly RegionEvent PlayerLeave = new RegionEvent("RegionEvent.PlayerLeave");
}
```

#### Area Events
```csharp
public class AreaEvent : DOLEvent
{
    public override bool IsValidFor(object o)
    {
        return o is IArea;
    }
    
    public static readonly AreaEvent PlayerEnter = new AreaEvent("AreaEvent.PlayerEnter");
    public static readonly AreaEvent PlayerLeave = new AreaEvent("AreaEvent.PlayerLeave");
}
```

## Event Handler Management

### Global Event Handlers
Global handlers receive all events of their registered type across the entire server:

```csharp
// Register global handler
GameEventMgr.AddHandler(GamePlayerEvent.LevelUp, OnPlayerLevelUp);

// Handler method
public static void OnPlayerLevelUp(DOLEvent e, object sender, EventArgs args)
{
    GamePlayer player = sender as GamePlayer;
    if (player != null)
    {
        // Process level up globally
        Console.WriteLine($"{player.Name} reached level {player.Level}!");
    }
}

// Remove global handler
GameEventMgr.RemoveHandler(GamePlayerEvent.LevelUp, OnPlayerLevelUp);
```

### Object-Specific Event Handlers
Object handlers only receive events when the registered object is the sender:

```csharp
// Register handler for specific player
GameEventMgr.AddHandler(specificPlayer, GamePlayerEvent.LevelUp, OnThisPlayerLevelUp);

// Handler only called for this specific player
public static void OnThisPlayerLevelUp(DOLEvent e, object sender, EventArgs args)
{
    GamePlayer player = sender as GamePlayer;
    // This will only be called for 'specificPlayer'
}

// Remove object handler
GameEventMgr.RemoveHandler(specificPlayer, GamePlayerEvent.LevelUp, OnThisPlayerLevelUp);
```

### Unique Handler Registration
Prevents duplicate handlers:

```csharp
// Add handler only if not already registered
GameEventMgr.AddHandlerUnique(GamePlayerEvent.LevelUp, OnPlayerLevelUp);
GameEventMgr.AddHandlerUnique(player, GamePlayerEvent.LevelUp, OnThisPlayerLevelUp);
```

### Automatic Registration via Attributes
Register handlers automatically during assembly loading:

```csharp
[GlobalEventHandler(GamePlayerEvent.LevelUp)]
public static void OnAnyPlayerLevelUp(DOLEvent e, object sender, EventArgs args)
{
    // Automatically registered as global handler
}

// Assembly loading
GameEventMgr.RegisterGlobalEvents(assembly, typeof(GlobalEventHandlerAttribute), GamePlayerEvent.LevelUp);
```

## Event Notification

### Basic Notification
```csharp
// Notify with just event
GameEventMgr.Notify(GamePlayerEvent.LevelUp);

// Notify with sender
GameEventMgr.Notify(GamePlayerEvent.LevelUp, player);

// Notify with sender and arguments
GameEventMgr.Notify(GamePlayerEvent.LevelUp, player, new LevelUpEventArgs(newLevel, oldLevel));
```

### Event Arguments
Custom event arguments provide additional context:

```csharp
public class LevelUpEventArgs : EventArgs
{
    public int NewLevel { get; set; }
    public int OldLevel { get; set; }
    public long ExperienceGained { get; set; }
    
    public LevelUpEventArgs(int newLevel, int oldLevel, long experience = 0)
    {
        NewLevel = newLevel;
        OldLevel = oldLevel;
        ExperienceGained = experience;
    }
}

// Usage
var args = new LevelUpEventArgs(player.Level, player.Level - 1, experienceGained);
GameEventMgr.Notify(GamePlayerEvent.LevelUp, player, args);
```

### Combat Event Arguments
```csharp
public class AttackFinishedEventArgs : EventArgs
{
    public AttackData AttackData { get; set; }
    public DamageResult DamageResult { get; set; }
    
    public AttackFinishedEventArgs(AttackData ad)
    {
        AttackData = ad;
        DamageResult = ad.DamageResult;
    }
}

public class CastingEventArgs : EventArgs
{
    public SpellHandler SpellHandler { get; set; }
    public GameObject Target { get; set; }
    public AttackData LastAttackData { get; set; }
    
    public CastingEventArgs(SpellHandler handler, GameObject target, AttackData lastAD)
    {
        SpellHandler = handler;
        Target = target;
        LastAttackData = lastAD;
    }
}
```

## Event Handler Collections

### Weak Reference System
Prevents memory leaks by using weak references:

```csharp
public sealed class DOLEventHandlerCollection
{
    private readonly Dictionary<DOLEvent, WeakMulticastDelegate> _events;
    private readonly ReaderWriterLockSlim _lock;
    
    public void AddHandler(DOLEvent e, DOLEventHandler del)
    {
        if (_lock.TryEnterWriteLock(LOCK_TIMEOUT))
        {
            try
            {
                if (!_events.TryGetValue(e, out WeakMulticastDelegate eventDelegate))
                {
                    eventDelegate = new WeakMulticastDelegate();
                    _events.Add(e, eventDelegate);
                }
                eventDelegate.Combine(del);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
```

### Thread Safety
All event operations are thread-safe with timeout protection:

```csharp
private const int LOCK_TIMEOUT = 3000; // 3 second timeout

public void Notify(DOLEvent e, object sender, EventArgs eArgs)
{
    WeakMulticastDelegate eventDelegate = null;
    
    if (_lock.TryEnterReadLock(LOCK_TIMEOUT))
    {
        try
        {
            if (!_events.TryGetValue(e, out eventDelegate))
                return;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    eventDelegate.InvokeSafe(new[] { e, sender, eArgs });
}
```

## Performance Considerations

### Handler Cleanup
Object handlers are automatically cleaned up when objects are garbage collected due to weak references.

### Lock Contention
Reader-writer locks allow multiple concurrent readers while ensuring exclusive write access:

```csharp
// Multiple readers can access simultaneously
_lock.TryEnterReadLock(LOCK_TIMEOUT)

// Only one writer at a time
_lock.TryEnterWriteLock(LOCK_TIMEOUT)

// Upgradeable read for potential writes
_lock.TryEnterUpgradeableReadLock(LOCK_TIMEOUT)
```

### Event Volume Monitoring
```csharp
public static int NumGlobalHandlers => GlobalHandlerCollection.Count;

public static int NumObjectHandlers
{
    get
    {
        int numHandlers = 0;
        var collections = new Dictionary<object, DOLEventHandlerCollection>(m_gameObjectEventCollections);
        
        foreach (DOLEventHandlerCollection handler in collections.Values)
        {
            numHandlers += handler.Count;
        }
        
        return numHandlers;
    }
}
```

## Common Usage Patterns

### Player Title System
```csharp
public abstract class EventPlayerTitle : SimplePlayerTitle
{
    protected EventPlayerTitle()
    {
        GameEventMgr.AddHandler(Event, new DOLEventHandler(EventCallback));
    }
    
    public abstract DOLEvent Event { get; }
    
    protected virtual void EventCallback(DOLEvent e, object sender, EventArgs arguments)
    {
        GamePlayer player = sender as GamePlayer;
        if (player != null)
        {
            if (IsSuitable(player))
                player.AddTitle(this);
            else
                player.RemoveTitle(this);
        }
    }
}
```

### Quest Behavior System
```csharp
public class BaseBehaviour
{
    public DOLEventHandler NotifyHandler { get; set; }
    
    public BaseBehaviour(GameNPC npc)
    {
        NotifyHandler = new DOLEventHandler(this.Notify);
        // Register for specific events
        foreach (IBehaviourTrigger trigger in Triggers)
        {
            trigger.Register(); // Registers event handlers
        }
    }
    
    public virtual void Notify(DOLEvent e, object sender, EventArgs args)
    {
        // Process behavior triggers
        foreach (IBehaviourTrigger trigger in Triggers)
        {
            if (trigger.Check(e, sender, args))
            {
                // Execute behavior actions
                ProcessActions();
                break;
            }
        }
    }
}
```

### Logging and Auditing
```csharp
[GlobalEventHandler(GamePlayerEvent.UseSlashCommand)]
public static void LogPlayerCommands(DOLEvent e, object sender, EventArgs args)
{
    GamePlayer player = sender as GamePlayer;
    CommandEventArgs cmdArgs = args as CommandEventArgs;
    
    if (player != null && cmdArgs != null)
    {
        AuditMgr.LogCommand(player, cmdArgs.Command, cmdArgs.Arguments);
    }
}
```

### Statistics Collection
```csharp
[GlobalEventHandler(GamePlayerEvent.PlayerKilled)]
public static void TrackPvPKills(DOLEvent e, object sender, EventArgs args)
{
    GamePlayer killer = sender as GamePlayer;
    PvPEventArgs pvpArgs = args as PvPEventArgs;
    
    if (killer != null && pvpArgs != null)
    {
        Statistics.RecordPvPKill(killer, pvpArgs.Victim, pvpArgs.RealmPoints);
    }
}
```

## Event Validation

### Type Safety
Events validate that they are appropriate for the target object:

```csharp
public override bool IsValidFor(object o)
{
    return o is GamePlayer; // Player events only valid for GamePlayer objects
}

// Validation occurs during handler registration
if (!e.IsValidFor(obj))
    throw new ArgumentException("Object is not valid for this event type", "obj");
```

### Event Lifecycle
```csharp
// Events fire in specific order for logical consistency
GameEventMgr.Notify(GameLivingEvent.CastStarting, caster, castArgs);
// ... casting logic ...
GameEventMgr.Notify(GameLivingEvent.CastFinished, caster, castArgs);
// ... or on failure ...
GameEventMgr.Notify(GameLivingEvent.CastFailed, caster, castArgs);
```

## Error Handling

### Safe Invocation
Event handlers are invoked safely to prevent exceptions from breaking the event chain:

```csharp
public void InvokeSafe(object[] args)
{
    try
    {
        foreach (var handler in GetValidHandlers())
        {
            try
            {
                handler.Invoke(args);
            }
            catch (Exception ex)
            {
                // Log but don't break other handlers
                log.Error($"Event handler failed: {ex}");
            }
        }
    }
    catch (Exception ex)
    {
        log.Error($"Event invocation failed: {ex}");
    }
}
```

### Memory Leak Prevention
Weak references automatically clean up handlers when objects are collected:

```csharp
public class WeakMulticastDelegate
{
    private readonly List<WeakReference> _handlers = new List<WeakReference>();
    
    public void Combine(DOLEventHandler handler)
    {
        // Clean up dead references periodically
        _handlers.RemoveAll(wr => !wr.IsAlive);
        _handlers.Add(new WeakReference(handler));
    }
}
```

## Best Practices

### Handler Registration
- Use `AddHandlerUnique` to prevent duplicate registrations
- Register handlers in object constructors or initialization
- Always unregister handlers in cleanup/disposal

### Event Arguments
- Create specific EventArgs classes for complex events
- Include all relevant context in event arguments
- Make event arguments immutable when possible

### Performance
- Avoid heavy processing in event handlers
- Use object-specific handlers instead of global when possible
- Consider async processing for time-consuming operations

### Error Handling
- Never throw exceptions from event handlers
- Log errors appropriately
- Fail gracefully to avoid breaking other handlers

## Test Scenarios

### Basic Event Flow
```csharp
// Given: Handler registered for player level up
GameEventMgr.AddHandler(GamePlayerEvent.LevelUp, OnLevelUp);

// When: Player levels up
player.Level++;
GameEventMgr.Notify(GamePlayerEvent.LevelUp, player, new LevelUpEventArgs(player.Level, player.Level - 1));

// Then: Handler is called with correct arguments
```

### Memory Leak Prevention
```csharp
// Given: Object with event handlers
var npc = new GameNPC();
GameEventMgr.AddHandler(npc, GameLivingEvent.Dying, OnNPCDeath);

// When: Object is destroyed and collected
npc = null;
GC.Collect();

// Then: Handler is automatically cleaned up
```

### Thread Safety
```csharp
// Given: Multiple threads accessing events
Task.Run(() => GameEventMgr.AddHandler(GamePlayerEvent.LevelUp, Handler1));
Task.Run(() => GameEventMgr.AddHandler(GamePlayerEvent.LevelUp, Handler2));
Task.Run(() => GameEventMgr.Notify(GamePlayerEvent.LevelUp, player));

// Then: All operations complete without corruption
```

## Integration Points

### Quest System
Events trigger quest progression and behavior activation

### Combat System
Events coordinate attack resolution, damage application, and effect processing

### Social Systems
Events manage group changes, guild activities, and chat interactions

### Housing System
Events handle permissions, decorations, and merchant activities

### Economy System
Events track trades, crafting, and market activities

## Future Enhancements
- TODO: Event priority system for ordered handler execution
- TODO: Async event handlers for non-blocking operations  
- TODO: Event aggregation for batched processing
- TODO: Event replay system for debugging
- TODO: Performance metrics per event type

## Change Log
- 2024-01-20: Initial documentation created

## References
- `GameServer/events/GameEventMgr.cs`
- `GameServer/events/DOLEvent.cs`
- `GameServer/events/DOLEventHandlerCollection.cs`
- `GameServer/events/gameobjects/` - All game object event definitions
- `GameServer/playertitles/EventPlayerTitle.cs`
- `GameServer/behaviour/BaseBehaviour.cs` 