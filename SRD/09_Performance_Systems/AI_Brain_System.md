# AI Brain System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: The AI Brain system controls how NPCs behave and react to players. Each NPC has a "brain" that determines if they're aggressive, how they detect enemies, and what they do in combat. Aggressive NPCs will attack you when you get within their aggro range, and they remember who has done the most damage to them (threat/aggro). NPCs follow priority rules: they target whoever has done the most damage recently, but being closer makes you more threatening. When you attack an NPC, nearby NPCs of the same type might join the fight (bring-a-friend). Pets and guards have special AI that makes them follow commands or protect areas. The smarter the NPC, the more often their brain "thinks" about what to do next.

The AI Brain system manages NPC behavior through Finite State Machines (FSM) and intelligent target selection. Each NPC has a brain that controls movement, combat decisions, spell casting, and social interactions with players and other NPCs.

## Core Architecture

### Brain Hierarchy

#### Base Brain Classes
```csharp
public abstract class ABrain
{
    protected FSM FSM;              // Finite State Machine
    protected GameNPC Body;         // The NPC this brain controls
    public int ThinkInterval;       // How often brain processes (ms)
    public long NextThinkTick;      // When next think occurs
}

public class StandardMobBrain : APlayerVicinityBrain, IOldAggressiveBrain
{
    // Most common brain for regular NPCs
    // Handles aggro, combat, patrol, roaming
}

public class ControlledMobBrain : StandardMobBrain, IControlledBrain
{
    // For pet NPCs controlled by players
    // Follows owner, obeys commands
}
```

#### Brain Specializations
- **GuardBrain**: City/keep guards with fast think intervals
- **KeepGuardBrain**: RvR guards with specific targeting
- **FearBrain**: NPCs that flee from players
- **FriendBrain**: NPCs that assist spell casters
- **ProcPetBrain**: Temporary pets from item procs

### Finite State Machine

#### State Types
```csharp
public enum eFSMStateType
{
    WAKING_UP,           // Initial spawn state
    IDLE,                // Default peaceful state
    AGGRO,               // In combat
    RETURN_TO_SPAWN,     // Moving back to spawn point
    PATROLLING,          // Following patrol path
    ROAMING,             // Random movement
    PASSIVE              // Non-aggressive mode
}
```

#### IDLE State
```csharp
public class StandardMobState_IDLE : StandardMobState
{
    public override void Think()
    {
        // Priority order:
        if (_brain.CheckSpells(eCheckSpellType.Defensive))
            return;
            
        if (_brain.HasPatrolPath())
            _brain.FSM.SetCurrentState(eFSMStateType.PATROLLING);
        else if (!_brain.Body.IsNearSpawn)
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        else if (_brain.CheckProximityAggro())
            _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
        else if (_brain.Body.CanRoam)
            _brain.FSM.SetCurrentState(eFSMStateType.ROAMING);
    }
}
```

#### AGGRO State
```csharp
public class StandardMobState_AGGRO : StandardMobState
{
    private const int LEAVE_WHEN_OUT_OF_COMBAT_FOR = 25000; // 25 seconds
    
    public override void Think()
    {
        // Check if should leave combat
        if (!_brain.Body.InCombatInLast(LEAVE_WHEN_OUT_OF_COMBAT_FOR))
        {
            _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
            return;
        }
        
        _brain.AttackMostWanted();
        
        if (!_brain.HasAggro)
            _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
    }
}
```

## Aggro System

### Aggro List Management
```csharp
public class StandardMobBrain
{
    // Max aggro distances
    public const int MAX_AGGRO_DISTANCE = 3600;
    public const int MAX_AGGRO_LIST_DISTANCE = 6000;
    
    // Effective aggro distance threshold
    private const int EFFECTIVE_AGGRO_DISTANCE_THRESHOLD = 250;
    
    // Aggro storage
    private ConcurrentDictionary<GameLiving, AggroAmount> AggroList;
    private List<(GameLiving, long)> OrderedAggroList; // Cached sorted list
    
    public struct AggroAmount
    {
        public long Base;      // Damage + hate from spells/abilities
        public long Effective; // Distance-modified for targeting
    }
}
```

### Aggro Calculation
```csharp
public void AddToAggroList(GameLiving living, long aggroAmount)
{
    // Base aggro from damage/abilities
    aggroAmount.Base += damage;
    
    // Distance-based effective aggro
    double distance = Body.GetDistanceTo(living);
    double distanceOverThreshold = distance - EFFECTIVE_AGGRO_DISTANCE_THRESHOLD;
    
    if (distanceOverThreshold <= 0)
        aggroAmount.Effective = aggroAmount.Base;
    else
        aggroAmount.Effective = (long)Math.Ceiling(
            aggroAmount.Base * Math.Exp(EFFECTIVE_AGGRO_EXPONENT * distanceOverThreshold));
}
```

### Target Selection
```csharp
protected virtual GameLiving CalculateNextAttackTarget()
{
    GameLiving highestThreat = null;
    long highestEffectiveAggro = -1;
    
    foreach (var pair in AggroList)
    {
        if (ShouldBeRemovedFromAggroList(pair.Key))
            continue;
            
        if (pair.Value.Effective > highestEffectiveAggro)
        {
            highestEffectiveAggro = pair.Value.Effective;
            highestThreat = pair.Key;
        }
    }
    
    return highestThreat;
}
```

## Aggro Generation

### From Combat Damage
```csharp
public virtual void OnAttackedByEnemy(AttackData ad)
{
    int damage = Math.Max(1, ad.Damage + ad.CriticalDamage);
    GameLiving attacker = ad.Attacker;
    
    // Pet damage split: 85% pet, 15% owner
    if (attacker is GameNPC pet && pet.Brain is ControlledMobBrain)
    {
        int aggroForOwner = (int)(damage * 0.15);
        int aggroForPet = damage - aggroForOwner;
        
        AddToAggroList(pet, aggroForPet);
        if (pet.Owner != null)
            AddToAggroList(pet.Owner, aggroForOwner);
    }
    else
    {
        AddToAggroList(attacker, damage);
    }
}
```

### From Spells
- **Hate Spells**: Direct aggro addition
- **Healing**: Generates aggro on all NPCs attacking heal target
- **Debuffs**: Aggro based on spell level and effectiveness

### Multiple Attackers
```csharp
// Evade chance reduction per additional attacker
double evadeChance = baseEvade * (1.0 - (attackerCount - 1) * 0.03);

// Parry chance reduction
double parryChance = baseParry / ((attackerCount + 1) / 2);
```

## Think Intervals

### Dynamic Think Rates
```csharp
public override int ThinkInterval
{
    get
    {
        if (Body is GameMerchant or GameTrainer or GameHastener)
            return 5000; // Special NPCs think slowly
            
        // Aggressive mobs think faster
        return Math.Max(500, 1500 - (AggroLevel / 10 * 100));
    }
}
```

### Brain-Specific Intervals
| Brain Type | Think Interval | Purpose |
|------------|----------------|---------|
| StandardMobBrain | 1500ms - (aggro/10*100) | Dynamic based on aggro |
| GuardBrain | 2000ms | City guards |
| KeepGuardBrain | 500ms | RvR guards need fast response |
| ControlledMobBrain | 1500ms | Pet consistency |
| FearBrain | 3000ms | Fleeing NPCs |

## Aggro Range & Detection

### Range System
```csharp
public virtual int AggroRange
{
    get => Math.Min(_aggroRange, MAX_AGGRO_DISTANCE);
    set => _aggroRange = value;
}

public virtual int AggroLevel { get; set; } // 0-100, 0 = not aggressive
```

### Detection Checks
```csharp
protected virtual void CheckPlayerAggro()
{
    foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
    {
        if (!CanAggroTarget(player))
            continue;
            
        if (player.IsStealthed || player.Steed != null)
            continue;
            
        if (player.effectListComponent.ContainsEffectForEffectType(eEffect.Shade))
            continue;
            
        // Line of sight check if enabled
        if (Properties.CHECK_LOS_BEFORE_AGGRO)
            SendLosCheckForAggro(player, player);
        else
            AddToAggroList(player, 1);
    }
}
```

### Aggro Conditions
```csharp
public virtual bool CanAggroTarget(GameLiving target)
{
    // Must be allowed to attack
    if (!GameServer.ServerRules.IsAllowedToAttack(Body, target, true))
        return false;
        
    // Must not be grey con
    if (target.IsObjectGreyCon(Body))
        return false;
        
    // Faction-based aggro
    if (Body.Faction != null)
    {
        if (target is GamePlayer player)
            return Body.Faction.GetStandingToFaction(player) 
                   is Faction.Standing.AGGRESIVE;
    }
    
    return AggroLevel > 0;
}
```

## Controlled Pet System

### Pet Commands
```csharp
public enum eAggressionState
{
    Passive,    // No attacking
    Defensive,  // Attack if attacked
    Aggressive  // Attack nearby enemies
}

public enum eWalkState
{
    Follow,     // Follow owner
    Stay,       // Stay at current position
    GoTarget,   // Attack specific target
    ComeHere    // Move to owner
}
```

### Pet AI Behavior
```csharp
public class ControlledMobBrain : StandardMobBrain
{
    private const int MAX_PET_AGGRO_DISTANCE = 512;
    private const short MIN_OWNER_FOLLOW_DIST = 50;
    private const short MAX_OWNER_FOLLOW_DIST = 10000;
    
    public override void Think()
    {
        // Check if owner is valid
        if (Owner == null || !Owner.IsAlive)
        {
            // Pet dies or despawns
            return;
        }
        
        // Follow/stay behavior based on walk state
        switch (WalkState)
        {
            case eWalkState.Follow:
                FollowOwner();
                break;
            case eWalkState.Stay:
                // Don't move unless attacking
                break;
        }
        
        // Aggression behavior
        switch (AggressionState)
        {
            case eAggressionState.Passive:
                // Only defend self
                break;
            case eAggressionState.Defensive:
                CheckDefensiveAggro();
                break;
            case eAggressionState.Aggressive:
                CheckAggressiveAggro();
                break;
        }
    }
}
```

## Special Brain Types

### Guard Brains
```csharp
public class GuardBrain : StandardMobBrain
{
    public override int AggroLevel => 90;
    public override int AggroRange => 750;
    
    // Guards attack even grey NPCs
    public override bool CanAggroTarget(GameLiving target)
    {
        return AggroLevel > 0 && 
               GameServer.ServerRules.IsAllowedToAttack(Body, target, true);
    }
}
```

### Fear Brain
```csharp
public class FearBrain : StandardMobBrain
{
    public override void Think()
    {
        foreach (GamePlayer player in Body.GetPlayersInRadius(750))
        {
            CalculateFleeTarget(player);
            break; // Flee from first player found
        }
    }
    
    protected virtual void CalculateFleeTarget(GameLiving target)
    {
        // Calculate opposite direction from threat
        ushort fleeAngle = (ushort)((Body.GetHeading(target) + 2048) % 4096);
        Point2D fleePoint = Body.GetPointFromHeading(fleeAngle, 300);
        
        Body.WalkTo(new Point3D(fleePoint.X, fleePoint.Y, Body.Z), Body.MaxSpeed);
    }
}
```

## Bring-A-Friend (BAF) System

### BAF Mechanics
```csharp
public bool CanBaf = true; // Resets when dropping out of combat

protected virtual void BringFriends(GameLiving target)
{
    if (!CanBaf)
        return;
        
    CanBaf = false; // Prevent repeated BAF
    
    foreach (GameNPC npc in Body.GetNPCsInRadius(BAF_RANGE))
    {
        if (npc.Brain is StandardMobBrain brain && 
            brain.CanBaf && 
            brain.CanAggroTarget(target))
        {
            brain.AddToAggroList(target, 1);
            brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
        }
    }
}
```

## System Integration

### Combat System
- Brain receives attack notifications
- Manages target selection during combat
- Handles interruption and flee conditions

### Spell System
- Checks for defensive/offensive spells to cast
- Manages spell cooldowns and power
- Integrates with healing and buff logic

### Movement System
- Controls NPC movement between waypoints
- Manages return to spawn logic
- Handles follow and patrol behavior

### Faction System
- Determines aggro eligibility
- Manages friend/enemy relationships
- Controls cross-realm interactions

## Performance Optimization

### Think Interval Management
- Merchants/trainers: 5000ms (slow)
- Combat NPCs: 1500ms - aggro bonus (dynamic)
- Guards: 500-2000ms (fast response)

### Aggro List Cleanup
```csharp
private void CleanupAggroList()
{
    foreach (var entry in AggroList.ToList())
    {
        if (ShouldBeRemovedFromAggroList(entry.Key))
            AggroList.TryRemove(entry.Key, out _);
    }
}
```

### Cached Target Selection
- Ordered aggro list cached until next cleanup
- Distance calculations optimized
- LoS checks batched when possible

## Configuration

### Server Properties
```xml
<Property Name="CHECK_LOS_BEFORE_AGGRO" Value="false" />
<Property Name="GUARD_RESPAWN" Value="5" />
<Property Name="GUARD_RESPAWN_VARIANCE" Value="1" />
<Property Name="MISSRATE_REDUCTION_PER_ATTACKERS" Value="0.03" />
```

### Brain Settings
- **AggroLevel**: 0-100 aggression percentage
- **AggroRange**: Detection radius in units
- **ThinkInterval**: Processing frequency in milliseconds
- **BAF**: Bring-a-friend on first aggro

## Test Scenarios

### Basic Aggro
- NPC detects player in range
- Aggro list management
- Target switching based on threat

### Pet Control
- Follow/stay commands
- Aggressive/defensive/passive modes
- Owner death handling

### Multi-Target Combat
- Multiple attacker penalties
- Target switching logic
- Aggro decay over distance

### Performance
- Think interval scaling
- Large aggro list cleanup
- Memory usage under load 