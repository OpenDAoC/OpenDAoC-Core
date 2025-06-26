# NPC Movement and Pathing System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from NpcMovementComponent.cs, PathCalculator.cs, MovementMgr.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
The NPC Movement and Pathing System provides sophisticated AI movement capabilities including patrol routes, intelligent pathfinding, following behavior, and complex navigation around obstacles. It supports both scripted paths and dynamic pathfinding.

## Core Components

### Movement Component Architecture

#### NpcMovementComponent
```csharp
public class NpcMovementComponent : MovementComponent
{
    public GameNPC Owner { get; }
    public Vector3 Velocity { get; private set; }
    public Point3D Destination { get; private set; }
    public GameLiving FollowTarget { get; private set; }
    public int FollowMinDistance { get; private set; } = 100;
    public int FollowMaxDistance { get; private set; } = 3000;
    public string PathID { get; set; }
    public PathPoint CurrentWaypoint { get; set; }
    public bool IsReturningToSpawnPoint { get; private set; }
    public int RoamingRange { get; set; }
    public bool IsMovingOnPath { get; private set; }
}
```

#### Movement States
```csharp
[Flags]
private enum MovementState
{
    NONE = 0,
    REQUEST = 1 << 1,      // Was requested to move
    WALK_TO = 1 << 2,      // Is moving and has a destination
    FOLLOW = 1 << 3,       // Is following an object
    ON_PATH = 1 << 4,      // Is following a path / is patrolling
    AT_WAYPOINT = 1 << 5,  // Is waiting at a waypoint
    PATHING = 1 << 6,      // Is moving using PathCalculator
    TURN_TO = 1 << 7       // Is facing a direction for duration
}
```

### Movement Types

#### Basic Movement
```csharp
// Direct movement to location
public void WalkTo(Point3D destination, short speed)

// Pathfinding-enabled movement  
public void PathTo(Point3D destination, short speed)

// Stop all movement
public void StopMoving()
```

#### Following Behavior
```csharp
public void Follow(GameLiving target, int minDistance, int maxDistance)
{
    if (target == null || target.ObjectState is not eObjectState.Active)
        return;
        
    FollowTarget = target;
    FollowMinDistance = minDistance;
    FollowMaxDistance = maxDistance;
    SetFlag(MovementState.FOLLOW);
}
```

**Follow Constants**:
- `MIN_ALLOWED_FOLLOW_DISTANCE`: 100 units
- `MIN_ALLOWED_PET_FOLLOW_DISTANCE`: 90 units
- **Max Follow Distance**: 3000 units default

#### Patrol System
```csharp
public void MoveOnPath(short speed)
{
    StopMoving();
    _moveOnPathSpeed = speed;
    
    // Move to first waypoint if we don't have any
    if (CurrentWaypoint == null)
    {
        CurrentWaypoint = MovementMgr.LoadPath(PathID);
        SetFlag(MovementState.ON_PATH);
        PathTo(CurrentWaypoint, Math.Min(_moveOnPathSpeed, CurrentWaypoint.MaxSpeed));
    }
}
```

### Path System

#### PathPoint Structure
```csharp
public class PathPoint
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public short MaxSpeed { get; set; }
    public EPathType Type { get; set; }
    public int WaitTime { get; set; }
    public PathPoint Next { get; set; }
    public PathPoint Prev { get; set; }
    public bool FiredFlag { get; set; }
}
```

#### Path Types
```csharp
public enum EPathType
{
    Once,         // Walk path once then stop
    Loop,         // Continuously loop through path
    Path_Reverse  // Walk to end, then reverse back
}
```

#### Path Loading and Caching
```csharp
public static PathPoint LoadPath(string pathID)
{
    // Loads from cache or database
    EPathType pathType = dbpath?.PathType ?? EPathType.Once;
    
    PathPoint prev = null;
    PathPoint first = null;
    
    foreach (DbPathPoint pp in pathPoints.Values)
    {
        PathPoint p = new(pp.X, pp.Y, pp.Z, pp.MaxSpeed, pathType)
        {
            WaitTime = pp.WaitTime
        };
        
        first ??= p;
        p.Prev = prev;
        if (prev != null)
            prev.Next = p;
        prev = p;
    }
    
    return first;
}
```

### Intelligent Pathfinding

#### PathCalculator
```csharp
public sealed class PathCalculator
{
    public const int MIN_PATHING_DISTANCE = 80;
    public const int MIN_TARGET_DIFF_REPLOT_DISTANCE = 80;
    public const int NODE_REACHED_DISTANCE = 24;
    public const int DOOR_SEARCH_DISTANCE = 512;
    
    public GameNPC Owner { get; }
    public bool ForceReplot { get; set; }
    public bool DidFindPath { get; private set; }
}
```

#### Pathfinding Conditions
```csharp
public static bool ShouldPath(GameNPC owner, Vector3 target)
{
    // Too close to path
    if (owner.GetDistanceTo(target) < MIN_PATHING_DISTANCE)
        return false;
        
    // Flying NPCs don't path
    if (owner.Flags.HasFlag(GameNPC.eFlags.FLYING))
        return false;
        
    // Underground check
    if (owner.Z <= 0)
        return false;
        
    // Zone must support pathing
    if (owner.CurrentZone?.IsPathingEnabled != true)
        return false;
        
    return true;
}
```

#### Path Calculation
```csharp
public Vector3? CalculateNextTarget(Vector3 target, out ENoPathReason noPathReason)
{
    if (!ShouldPath(target))
    {
        noPathReason = ENoPathReason.NoPath;
        return null;
    }
    
    // Check if we need to replot
    if (ForceReplot || TargetChanged(target) || PathCompleted())
        ReplotPath(target);
        
    // Handle doors on path
    if (HasClosedDoorOnPath())
    {
        noPathReason = ENoPathReason.ClosedDoor;
        return null;
    }
    
    // Return next waypoint
    return GetNextWaypoint(out noPathReason);
}
```

### Roaming System

#### Roaming Mechanics
```csharp
public void Roam(short speed)
{
    int maxRoamingRadius = Owner.RoamingRange > 0 ? 
                          Owner.RoamingRange : 
                          Owner.CurrentRegion.IsDungeon ? 5 : 500;
    
    if (Owner.CurrentZone.IsPathingEnabled)
    {
        Vector3? target = PathingMgr.Instance.GetRandomPointAsync(
            Owner.CurrentZone, 
            new Vector3(Owner.SpawnPoint.X, Owner.SpawnPoint.Y, Owner.SpawnPoint.Z), 
            maxRoamingRadius);
            
        if (target.HasValue)
            PathTo(new Point3D(target.Value.X, target.Value.Y, target.Value.Z), speed);
    }
    else
    {
        // Fallback random movement
        GenerateRandomDestination(maxRoamingRadius, speed);
    }
}
```

#### Roaming Conditions
```csharp
public bool CanRoam => Properties.ALLOW_ROAM && 
                      RoamingRange > 0 && 
                      string.IsNullOrWhiteSpace(PathID);
```

### Speed and Timing

#### Speed Constants
```csharp
public const short DEFAULT_WALK_SPEED = 70;
public const int PATROL_SPEED = 250;  // For keep patrols
```

#### Speed Calculation
```csharp
public override short MaxSpeed => FixedSpeed ? MaxSpeedBase : base.MaxSpeed;

// Follow speed is dynamic based on distance
short speed = (short)((distance - minAllowedFollowDistance) * 
              (1000.0 / Properties.GAMENPC_FOLLOWCHECK_TIME));
PathToInternal(destination, Math.Min(MaxSpeed, speed));
```

### Waypoint Navigation

#### Waypoint Arrival
```csharp
private void OnArrival()
{
    if (IsFlagSet(MovementState.ON_PATH))
    {
        if (CurrentWaypoint != null)
        {
            if (CurrentWaypoint.WaitTime == 0)
            {
                MoveToNextWaypoint();
                return;
            }
            
            SetFlag(MovementState.AT_WAYPOINT);
            _stopAtWaypointUntil = GameLoop.GameLoopTime + CurrentWaypoint.WaitTime * 100;
        }
        else
            StopMovingOnPath();
    }
}
```

#### Next Waypoint Logic
```csharp
private void MoveToNextWaypoint()
{
    PathPoint oldPathPoint = CurrentWaypoint;
    PathPoint nextPathPoint = CurrentWaypoint.Next;
    
    if ((CurrentWaypoint.Type == EPathType.Path_Reverse) && CurrentWaypoint.FiredFlag)
        nextPathPoint = CurrentWaypoint.Prev;
        
    if (nextPathPoint == null)
    {
        switch (CurrentWaypoint.Type)
        {
            case EPathType.Loop:
                CurrentWaypoint = MovementMgr.FindFirstPathPoint(CurrentWaypoint);
                break;
            case EPathType.Once:
                CurrentWaypoint = null;
                PathID = null;  // Unset path to prevent restart
                break;
            case EPathType.Path_Reverse:
                // Toggle direction
                CurrentWaypoint = oldPathPoint.FiredFlag ? 
                                 CurrentWaypoint.Next : CurrentWaypoint.Prev;
                break;
        }
    }
    else
        CurrentWaypoint = nextPathPoint;
        
    oldPathPoint.FiredFlag = !oldPathPoint.FiredFlag;
    
    if (CurrentWaypoint != null)
        PathTo(CurrentWaypoint, Math.Min(_moveOnPathSpeed, CurrentWaypoint.MaxSpeed));
    else
        StopMovingOnPath();
}
```

## System Interactions

### With Combat System
- **Combat Interruption**: Movement stops during combat
- **Return to Spawn**: After combat, NPCs return to spawn point
- **Tether Range**: NPCs won't move beyond tether distance

### With AI Brain System
- **Brain Control**: AI brains control movement decisions
- **Aggro Integration**: Movement affected by aggro state
- **Formation Support**: Group movement for formations

### With Doors and Obstacles
- **Door Detection**: Pathfinding detects closed doors
- **Obstacle Avoidance**: Uses navmesh for navigation
- **Line of Sight**: Respects terrain and buildings

### With Database System
- **Path Storage**: Paths stored in DbPath/DbPathPoint tables
- **Caching**: Paths cached for performance
- **Dynamic Updates**: Path cache updated when modified

## Administrative Tools

### GM Commands
```
/path create <pathtype>    # Create new path
/path add                  # Add waypoint to path
/path save <pathid>        # Save path to database
/path load <pathid>        # Load path from database
/path travel               # Make NPC follow path
/path stop                 # Stop NPC movement
```

### Path Visualization
```csharp
public void TogglePathVisualization()
{
    // Shows colored markers for debugging:
    // Yellow: Standard waypoints
    // Blue: Swimming areas
    // Red: Door waypoints
    // Green: Other special areas
}
```

## Performance Considerations

### Optimization Features
1. **Path Caching**: All paths loaded once and cached
2. **Service Object Store**: Only moving NPCs processed
3. **Follow Tick Intervals**: Configurable follow update rates
4. **Pathfinding Throttling**: Prevents excessive pathfinding

### Configuration Options
```csharp
Properties.ALLOW_ROAM                    # Enable/disable roaming
Properties.GAMENPC_FOLLOWCHECK_TIME      # Follow update interval
Properties.WORLD_PICKUP_DISTANCE         # Interaction distance
```

## Special Movement Types

### Keep Patrols
```csharp
public class Patrol
{
    public const int PATROL_SPEED = 250;
    public List<GameKeepGuard> PatrolGuards;
    public PathPoint PatrolPath;
    
    public void StartPatrol()
    {
        if (PatrolPath == null)
            PatrolPath = PositionMgr.LoadPatrolPath(PatrolID, Component);
            
        foreach (GameKeepGuard guard in PatrolGuards)
        {
            if (guard.CurrentWaypoint == null)
                guard.CurrentWaypoint = PatrolPath;
            guard.MoveOnPath(PATROL_SPEED);
        }
    }
}
```

### Horse Routes
- **Automated Travel**: Fixed routes between locations
- **Player Transport**: Carries players between destinations
- **NPC Removal**: Horses disappear when reaching destination

### Formation Movement
- **Group Coordination**: Multiple NPCs move in formation
- **Leader Following**: Formation members follow leader
- **Position Maintenance**: Maintain relative positions

## Edge Cases and Special Rules

### Movement Restrictions
- **Flying NPCs**: Don't use ground pathfinding
- **Underground**: Z <= 0 prevents pathfinding
- **Tether Range**: NPCs can't move beyond spawn range
- **Zone Boundaries**: Can't path across zone borders

### Combat Interactions
- **Movement Cancellation**: Combat stops movement
- **Interrupt Recovery**: Resume movement after combat
- **Speed Penalties**: Some abilities affect movement speed

### Error Handling
- **No Path Found**: Falls back to direct movement
- **Closed Doors**: Waits and faces door
- **Invalid Destinations**: Cancels movement request
- **Zone Changes**: Handles zone transition cleanup

## Testing Scenarios

### Basic Movement Tests
1. **Walk To**: NPC walks directly to destination
2. **Path To**: NPC uses pathfinding to destination
3. **Follow**: NPC maintains distance from target
4. **Stop**: All movement cancelled properly

### Patrol Tests
1. **Loop Path**: NPC continuously patrols loop
2. **Once Path**: NPC walks path once then stops
3. **Reverse Path**: NPC walks back and forth
4. **Waypoint Wait**: NPC waits at waypoints correctly

### Pathfinding Tests
1. **Obstacle Avoidance**: Paths around walls/objects
2. **Door Detection**: Stops at closed doors
3. **Swimming Areas**: Handles water navigation
4. **Zone Boundaries**: Respects zone limits

### Performance Tests
1. **Mass Movement**: Many NPCs moving simultaneously
2. **Path Caching**: Verify cache performance
3. **Memory Usage**: No memory leaks from movement
4. **CPU Usage**: Efficient pathfinding algorithms

## References
- **Core Component**: `GameServer/ECS-Components/NpcMovementComponent.cs`
- **Pathfinding**: `GameServer/world/Pathing/PathCalculator.cs`
- **Path Management**: `GameServer/gameutils/MovementMgr.cs`
- **Keep Patrols**: `GameServer/keeps/Gameobjects/Guards/Patrol.cs`
- **AI Integration**: `GameServer/ai/brain/RoundsBrain.cs`
- **Database**: `CoreDatabase/Tables/DbPath*.cs` 