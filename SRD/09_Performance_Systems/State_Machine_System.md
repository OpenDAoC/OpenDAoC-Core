# State Machine System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

**Game Rule Summary**: The State Machine System controls how NPCs behave by organizing their actions into different "states" like idle, aggressive, returning to spawn, or roaming. Each NPC is always in exactly one state, and they switch between states based on what's happening around them. For example, a peaceful NPC in "idle" state will switch to "aggressive" state when a player gets too close, then switch to "return to spawn" state if the player runs too far away. This system makes NPC behavior predictable and logical - guards will always chase intruders but return to their posts, pets will follow their owners, and monsters will patrol their territory. Understanding these behavioral patterns helps you predict how NPCs will react in different situations.

The State Machine System provides sophisticated Finite State Machine (FSM) functionality for managing NPC AI behavior in OpenDAoC. It enables complex behavior trees through hierarchical state management, transition logic, and specialized state implementations for different NPC types.

## Core Architecture

### Base State System

```csharp
public abstract class FSMState
{
    public string Name { get; protected set; }
    public FSMState ParentState { get; set; }
    public List<FSMState> ChildStates { get; } = new();
    
    public abstract void Enter(object context);
    public abstract void Think(object context);
    public abstract void Exit(object context);
    
    public virtual bool CanTransitionTo(FSMState targetState)
    {
        return true;
    }
}

public class FSMStateMachine
{
    public FSMState CurrentState { get; private set; }
    public FSMState PreviousState { get; private set; }
    
    public void ChangeState(FSMState newState, object context)
    {
        PreviousState = CurrentState;
        CurrentState?.Exit(context);
        CurrentState = newState;
        CurrentState?.Enter(context);
    }
    
    public void Update(object context)
    {
        CurrentState?.Think(context);
    }
}
```

### Standard Mob State Hierarchy

```csharp
public class StandardMobState : FSMState
{
    protected StandardMobBrain Brain { get; }
    
    public StandardMobState(StandardMobBrain brain)
    {
        Brain = brain;
    }
    
    public override void Enter(object context)
    {
        var npc = context as GameNPC;
        OnEnterState(npc);
    }
    
    protected virtual void OnEnterState(GameNPC npc) { }
    protected virtual void OnExitState(GameNPC npc) { }
    protected virtual void OnThink(GameNPC npc) { }
}
```

## Primary State Implementations

### Idle State

```csharp
public class StandardMobState_IDLE : StandardMobState
{
    public StandardMobState_IDLE(StandardMobBrain brain) : base(brain)
    {
        Name = "IDLE";
    }
    
    public override void Enter(object context)
    {
        var npc = context as GameNPC;
        npc.StopMoving();
        npc.TurnTo(npc.SpawnHeading);
        npc.Health = npc.MaxHealth; // Regen while idle
    }
    
    public override void Think(object context)
    {
        var npc = context as GameNPC;
        
        // Check for nearby enemies
        var enemies = npc.GetEnemiesInRadius(npc.AggroRange);
        if (enemies.Count > 0)
        {
            Brain.AddToAggroList(enemies[0]);
            Brain.FSM.ChangeState(Brain.AggroState, npc);
            return;
        }
        
        // Random emotes or actions
        if (Util.Random(100) < 2)
        {
            PerformIdleAction(npc);
        }
    }
    
    private void PerformIdleAction(GameNPC npc)
    {
        var actions = new[] { "looks around", "yawns", "stretches" };
        var action = actions[Util.Random(actions.Length)];
        npc.Emote(eEmote.None, action);
    }
}
```

### Aggro State

```csharp
public class StandardMobState_AGGRO : StandardMobState
{
    private long _lastAttackTime;
    private const int ATTACK_INTERVAL = 2000; // 2 seconds
    
    public StandardMobState_AGGRO(StandardMobBrain brain) : base(brain)
    {
        Name = "AGGRO";
    }
    
    public override void Enter(object context)
    {
        var npc = context as GameNPC;
        Brain.ClearAggroList();
        npc.StartCombat();
    }
    
    public override void Think(object context)
    {
        var npc = context as GameNPC;
        var target = Brain.AggroTable.GetHighestAggroTarget();
        
        if (target == null || !target.IsAlive)
        {
            Brain.FSM.ChangeState(Brain.IdleState, npc);
            return;
        }
        
        // Check if target is too far away
        if (!npc.IsWithinRadius(target, npc.MaxDistance))
        {
            Brain.FSM.ChangeState(Brain.ReturnToSpawnState, npc);
            return;
        }
        
        // Move to target if not in range
        if (!npc.IsWithinRadius(target, npc.AttackRange))
        {
            npc.PathTo(target.X, target.Y, target.Z, npc.MaxSpeed);
        }
        else
        {
            // Attack if cooldown elapsed
            if (Environment.TickCount - _lastAttackTime > ATTACK_INTERVAL)
            {
                PerformAttack(npc, target);
                _lastAttackTime = Environment.TickCount;
            }
        }
    }
    
    private void PerformAttack(GameNPC npc, GameLiving target)
    {
        if (npc.CanUseSpells && Util.Random(100) < 30)
        {
            CastSpell(npc, target);
        }
        else
        {
            npc.StartMeleeAttack(target);
        }
    }
}
```

### Return to Spawn State

```csharp
public class StandardMobState_RETURN_TO_SPAWN : StandardMobState
{
    public StandardMobState_RETURN_TO_SPAWN(StandardMobBrain brain) : base(brain)
    {
        Name = "RETURN_TO_SPAWN";
    }
    
    public override void Enter(object context)
    {
        var npc = context as GameNPC;
        Brain.ClearAggroList();
        npc.StopCombat();
        npc.Health = npc.MaxHealth; // Full heal when returning
    }
    
    public override void Think(object context)
    {
        var npc = context as GameNPC;
        
        // Return to spawn point
        if (!npc.IsWithinRadius(npc.SpawnPoint, 50))
        {
            npc.PathTo(npc.SpawnPoint.X, npc.SpawnPoint.Y, npc.SpawnPoint.Z, npc.MaxSpeed);
        }
        else
        {
            // Reached spawn point
            npc.TurnTo(npc.SpawnHeading);
            Brain.FSM.ChangeState(Brain.IdleState, npc);
        }
    }
}
```

### Roaming State

```csharp
public class StandardMobState_ROAMING : StandardMobState
{
    private Point3D _roamTarget;
    private long _lastRoamTime;
    
    public StandardMobState_ROAMING(StandardMobBrain brain) : base(brain)
    {
        Name = "ROAMING";
    }
    
    public override void Enter(object context)
    {
        var npc = context as GameNPC;
        SelectNewRoamTarget(npc);
    }
    
    public override void Think(object context)
    {
        var npc = context as GameNPC;
        
        // Check for aggro
        CheckForAggro(npc);
        
        // Move to roam target
        if (_roamTarget != null && !npc.IsWithinRadius(_roamTarget, 100))
        {
            npc.PathTo(_roamTarget.X, _roamTarget.Y, _roamTarget.Z, npc.MaxSpeed * 0.5f);
        }
        else
        {
            // Reached target, select new one or go idle
            if (Environment.TickCount - _lastRoamTime > 10000)
            {
                Brain.FSM.ChangeState(Brain.IdleState, npc);
            }
            else
            {
                SelectNewRoamTarget(npc);
            }
        }
    }
    
    private void SelectNewRoamTarget(GameNPC npc)
    {
        var roamRadius = npc.RoamingRange;
        var angle = Util.Random(360) * Math.PI / 180;
        var distance = Util.Random(roamRadius / 2, roamRadius);
        
        _roamTarget = new Point3D(
            (int)(npc.SpawnPoint.X + Math.Cos(angle) * distance),
            (int)(npc.SpawnPoint.Y + Math.Sin(angle) * distance),
            npc.SpawnPoint.Z
        );
        
        _lastRoamTime = Environment.TickCount;
    }
}
```

## Specialized State Implementations

### Controlled Mob States

```csharp
public class ControlledMobState_DEFENSIVE : StandardMobState_IDLE
{
    public ControlledMobState_DEFENSIVE(ControlledMobBrain brain) : base(brain)
    {
        Name = "DEFENSIVE";
    }
    
    public override void Think(object context)
    {
        var pet = context as GameNPC;
        var owner = (Brain as ControlledMobBrain)?.Owner;
        
        if (owner == null)
        {
            // Owner lost, go passive
            Brain.FSM.ChangeState(Brain.PassiveState, pet);
            return;
        }
        
        // Follow owner if too far
        if (!pet.IsWithinRadius(owner, 500))
        {
            pet.Follow(owner, 50, 200);
        }
        
        // Check for threats to owner
        var threat = FindThreatToOwner(owner);
        if (threat != null)
        {
            Brain.AddToAggroList(threat);
            Brain.FSM.ChangeState(Brain.AggroState, pet);
        }
    }
}

public class ControlledMobState_PASSIVE : StandardMobState
{
    public ControlledMobState_PASSIVE(ControlledMobBrain brain) : base(brain)
    {
        Name = "PASSIVE";
    }
    
    public override void Think(object context)
    {
        var pet = context as GameNPC;
        var owner = (Brain as ControlledMobBrain)?.Owner;
        
        if (owner == null) return;
        
        // Only follow owner, don't engage in combat
        if (!pet.IsWithinRadius(owner, 200))
        {
            pet.Follow(owner, 50, 150);
        }
        else
        {
            pet.StopMoving();
        }
    }
}
```

### Guard States

```csharp
public class GuardState_RETURN_TO_SPAWN : StandardMobState_RETURN_TO_SPAWN
{
    public GuardState_RETURN_TO_SPAWN(GuardBrain brain) : base(brain)
    {
        Name = "GUARD_RETURN";
    }
    
    public override void Think(object context)
    {
        var guard = context as GameNPC;
        
        // Guards call for help when returning
        CallForReinforcements(guard);
        
        base.Think(context);
    }
    
    private void CallForReinforcements(GameNPC guard)
    {
        var nearbyGuards = guard.GetNPCsInRadius(1000)
            .Where(npc => npc.Brain is GuardBrain && npc != guard);
            
        foreach (var ally in nearbyGuards)
        {
            if (ally.Brain is GuardBrain guardBrain)
            {
                guardBrain.AddToAggroList(Brain.AggroTable.GetHighestAggroTarget());
            }
        }
    }
}
```

## State Transitions

### Transition Rules

```csharp
public class StateTransitionRule
{
    public FSMState FromState { get; set; }
    public FSMState ToState { get; set; }
    public Func<object, bool> Condition { get; set; }
    public int Priority { get; set; }
    
    public bool CanTransition(object context)
    {
        return Condition?.Invoke(context) ?? false;
    }
}

public class StateTransitionManager
{
    private readonly List<StateTransitionRule> _transitions = new();
    
    public void AddTransition(FSMState from, FSMState to, Func<object, bool> condition, int priority = 0)
    {
        _transitions.Add(new StateTransitionRule
        {
            FromState = from,
            ToState = to,
            Condition = condition,
            Priority = priority
        });
    }
    
    public FSMState FindBestTransition(FSMState currentState, object context)
    {
        return _transitions
            .Where(t => t.FromState == currentState && t.CanTransition(context))
            .OrderByDescending(t => t.Priority)
            .FirstOrDefault()
            ?.ToState;
    }
}
```

### Common Transition Conditions

```csharp
public static class TransitionConditions
{
    public static bool HasAggro(object context)
    {
        var npc = context as GameNPC;
        return npc?.Brain?.AggroTable?.Count > 0;
    }
    
    public static bool NoAggro(object context)
    {
        return !HasAggro(context);
    }
    
    public static bool AtSpawnPoint(object context)
    {
        var npc = context as GameNPC;
        return npc?.IsWithinRadius(npc.SpawnPoint, 50) ?? false;
    }
    
    public static bool HealthLow(object context)
    {
        var npc = context as GameNPC;
        return npc?.HealthPercent < 25;
    }
    
    public static bool OwnerInRange(object context)
    {
        var pet = context as GameNPC;
        var brain = pet?.Brain as ControlledMobBrain;
        return brain?.Owner?.IsWithinRadius(pet, 1000) ?? false;
    }
}
```

## Advanced State Features

### Hierarchical States

```csharp
public class HierarchicalState : FSMState
{
    private FSMStateMachine _subStateMachine = new();
    
    public void AddSubState(FSMState subState)
    {
        ChildStates.Add(subState);
        subState.ParentState = this;
    }
    
    public override void Think(object context)
    {
        // Run sub-state machine
        _subStateMachine.Update(context);
        
        // Check for transitions out of hierarchical state
        CheckParentTransitions(context);
    }
}

// Example: Combat state with sub-states
public class CombatState : HierarchicalState
{
    public CombatState(StandardMobBrain brain)
    {
        AddSubState(new MeleeAttackState(brain));
        AddSubState(new CastSpellState(brain));
        AddSubState(new UseAbilityState(brain));
    }
}
```

### State History

```csharp
public class StateHistory
{
    private readonly Queue<FSMState> _history = new();
    private const int MAX_HISTORY = 10;
    
    public void RecordState(FSMState state)
    {
        _history.Enqueue(state);
        if (_history.Count > MAX_HISTORY)
        {
            _history.Dequeue();
        }
    }
    
    public FSMState GetPreviousState(int stepsBack = 1)
    {
        var history = _history.ToArray();
        var index = history.Length - 1 - stepsBack;
        return index >= 0 ? history[index] : null;
    }
    
    public bool WasInState<T>() where T : FSMState
    {
        return _history.Any(s => s is T);
    }
}
```

### State Debugging

```csharp
public class StateDebugger
{
    private readonly Dictionary<GameNPC, StateLog> _logs = new();
    
    public void LogStateChange(GameNPC npc, FSMState oldState, FSMState newState, string reason)
    {
        if (!_logs.ContainsKey(npc))
        {
            _logs[npc] = new StateLog();
        }
        
        _logs[npc].AddEntry(new StateLogEntry
        {
            Timestamp = DateTime.UtcNow,
            FromState = oldState?.Name ?? "None",
            ToState = newState?.Name ?? "None",
            Reason = reason,
            NPCName = npc.Name,
            Position = npc.Position
        });
    }
    
    public IEnumerable<StateLogEntry> GetLog(GameNPC npc)
    {
        return _logs.GetValueOrDefault(npc)?.Entries ?? Enumerable.Empty<StateLogEntry>();
    }
}
```

## Performance Optimization

### State Pooling

```csharp
public class StatePool<T> where T : FSMState, new()
{
    private readonly ConcurrentQueue<T> _pool = new();
    
    public T GetState()
    {
        return _pool.TryDequeue(out var state) ? state : new T();
    }
    
    public void ReturnState(T state)
    {
        state.Reset();
        _pool.Enqueue(state);
    }
}
```

### Batch State Updates

```csharp
public class BatchStateProcessor
{
    private readonly List<(GameNPC npc, FSMStateMachine fsm)> _updateList = new();
    
    public void QueueForUpdate(GameNPC npc, FSMStateMachine fsm)
    {
        _updateList.Add((npc, fsm));
    }
    
    public void ProcessBatch()
    {
        Parallel.ForEach(_updateList, entry =>
        {
            entry.fsm.Update(entry.npc);
        });
        
        _updateList.Clear();
    }
}
```

## System Integration

### Brain Integration

```csharp
public abstract class StandardMobBrain : ABrain
{
    public FSMStateMachine FSM { get; } = new();
    public StandardMobState IdleState { get; }
    public StandardMobState AggroState { get; }
    public StandardMobState ReturnToSpawnState { get; }
    
    protected StandardMobBrain()
    {
        IdleState = new StandardMobState_IDLE(this);
        AggroState = new StandardMobState_AGGRO(this);
        ReturnToSpawnState = new StandardMobState_RETURN_TO_SPAWN(this);
        
        // Set initial state
        FSM.ChangeState(IdleState, null);
    }
    
    public override void Think()
    {
        FSM.Update(Body);
    }
}
```

### Event Integration

```csharp
public class StateEventManager
{
    public event Action<GameNPC, FSMState, FSMState> StateChanged;
    public event Action<GameNPC, FSMState> StateEntered;
    public event Action<GameNPC, FSMState> StateExited;
    
    public void OnStateChange(GameNPC npc, FSMState oldState, FSMState newState)
    {
        StateChanged?.Invoke(npc, oldState, newState);
    }
}
```

## Configuration

### State Machine Settings

```csharp
public static class StateMachineConfiguration
{
    public static int STATE_UPDATE_INTERVAL = 500; // ms
    public static bool ENABLE_STATE_DEBUGGING = false;
    public static int MAX_STATE_HISTORY = 10;
    public static bool USE_PARALLEL_UPDATES = true;
    public static int BATCH_SIZE = 100;
}
```

## Implementation Status

**Completed**:
- ✅ Core FSM architecture
- ✅ Standard mob states
- ✅ Controlled mob states
- ✅ Guard states
- ✅ State transition system
- ✅ Performance optimizations

**In Progress**:
- 🔄 Hierarchical state system
- 🔄 Advanced debugging tools
- 🔄 State persistence

**Planned**:
- ⏳ Visual state editor
- ⏳ Scripted state behaviors
- ⏳ Machine learning integration

## References

- **FSM Theory**: Based on finite state machine principles
- **Game AI**: Implements common game AI patterns
- **Performance**: Optimized for 1000+ concurrent NPCs 