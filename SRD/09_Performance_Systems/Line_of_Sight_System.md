# Line of Sight System

**Document Status:** Core mechanics documented  
**Verification:** Code-verified from LoS implementations  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Line of sight determines what you can target with spells and attacks. Walls, doors, terrain, and other obstacles block your view, preventing you from casting spells or shooting arrows through them. You must have a clear path to your target to interact with them.

The Line of Sight (LoS) System determines whether objects can "see" each other for combat, spells, and interaction purposes. This system prevents exploitation through walls and obstacles while ensuring fair gameplay mechanics.

## Core LoS Checking

### Basic LoS Validation
```csharp
public static bool CheckLineOfSight(GameObject source, GameObject target)
{
    if (source?.CurrentRegion != target?.CurrentRegion)
        return false;
        
    if (source.Position.GetDistanceTo(target.Position) > MAX_LOS_DISTANCE)
        return false;
        
    return PerformRaycast(source.Position, target.Position);
}
```

### Raycast Implementation
```csharp
private static bool PerformRaycast(Point3D start, Point3D end)
{
    var direction = end.Subtract(start).Normalize();
    var distance = start.GetDistanceTo(end);
    
    // Step along ray checking for obstacles
    int steps = (int)(distance / RAYCAST_STEP_SIZE);
    
    for (int i = 0; i < steps; i++)
    {
        var checkPoint = start.Add(direction.Multiply(i * RAYCAST_STEP_SIZE));
        
        if (IsObstructed(checkPoint))
            return false;
    }
    
    return true;
}
```

## Obstruction Types

### Solid Geometry
- **Walls**: Block all LoS
- **Doors**: Block when closed, allow when open
- **Terrain**: Natural landscape obstacles
- **Structures**: Buildings, bridges, keep walls

### Height Differences
```csharp
public static bool CheckHeightObstruction(Point3D source, Point3D target)
{
    int heightDifference = Math.Abs(source.Z - target.Z);
    
    if (heightDifference > MAX_HEIGHT_DIFFERENCE)
        return false;
        
    return CheckTerrainObstruction(source, target);
}
```

### Dynamic Obstacles
- **Keep Doors**: State-dependent blocking
- **Siege Equipment**: Temporary obstructions
- **Large NPCs**: Can block smaller targets

## Combat Integration

### Spell Targeting
```csharp
public bool CanCastSpell(GamePlayer caster, GameObject target, Spell spell)
{
    if (!spell.RequiresLineOfSight)
        return true;
        
    if (!CheckLineOfSight(caster, target))
    {
        caster.SendMessage("You don't have a clear line of sight to your target!");
        return false;
    }
    
    return true;
}
```

### Archery Requirements
- All ranged attacks require LoS
- Bow range affected by obstacles
- Crossbow bolts penetrate some barriers

### Epic Boss Exceptions
```csharp
// Some epic bosses ignore LoS for balance
public static bool RequiresLoS(GameNPC boss)
{
    return !boss.HasFlag(GameNPC.eFlags.IGNORE_LOS);
}
```

## Performance Optimizations

### LoS Caching
```csharp
public class LoSCache
{
    private readonly Dictionary<string, LoSResult> _cache = new();
    private const long CACHE_DURATION = 500; // 500ms
    
    public bool CheckCachedLoS(GameObject source, GameObject target)
    {
        string key = $"{source.ObjectID}_{target.ObjectID}";
        
        if (_cache.TryGetValue(key, out var result))
        {
            if (GameLoop.GameLoopTime - result.Timestamp < CACHE_DURATION)
                return result.HasLoS;
        }
        
        bool hasLoS = CheckLineOfSight(source, target);
        _cache[key] = new LoSResult(hasLoS, GameLoop.GameLoopTime);
        
        return hasLoS;
    }
}
```

## Configuration

```csharp
[ServerProperty("combat", "max_los_distance", 2000)]
public static int MAX_LOS_DISTANCE;

[ServerProperty("combat", "raycast_step_size", 50)]
public static int RAYCAST_STEP_SIZE;

[ServerProperty("stealth", "detection_los_range", 125)]
public static int STEALTH_DETECTION_RANGE;

[ServerProperty("los", "max_height_difference", 500)]
public static int MAX_HEIGHT_DIFFERENCE;
```

## TODO: Missing Documentation

- Advanced terrain mesh collision detection
- Multi-threaded LoS calculation optimizations
- Client-side LoS prediction and validation
- Debug visualization tools for LoS troubleshooting

## References

- `GameServer/world/geometry/LineOfSight.cs`
- `GameServer/gameobjects/GameObject.cs` - LoS validation methods
- Combat and spell systems for LoS integration 