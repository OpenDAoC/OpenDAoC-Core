# Region and Zone Mechanics

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from Region.cs, Zone.cs, GameObject.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: DAoC's world is organized into regions (separate map areas) and zones (subdivisions within regions). When you travel between regions, like going from your homeland to the frontiers, there's a loading screen as the game switches maps. Within each region, zones are invisible boundaries that help organize content by level and theme. Understanding this structure helps you navigate efficiently and know where to find appropriate content for your character level.

DAoC's world is organized into regions (separate map areas) and zones (subdivisions within regions). This hierarchical system manages object positioning, visibility, movement, and interactions within the game world.

## Core Mechanics

### Region System

**Game Rule Summary**: Regions are the main map areas that require loading screens to enter. Each region has its own properties like whether PvP is allowed, if it's a dungeon, or if it's part of the frontier zones. You can't see or interact with players in different regions - they're completely separate game worlds. Examples include your realm's homeland, enemy realms, frontier zones, and dungeons.

#### Region Properties
- **ID**: Unique identifier (ushort)
- **Name**: Display name
- **Description**: Detailed description
- **IsDungeon**: Special dungeon handling
- **IsRvR**: PvP-enabled region
- **IsFrontier**: Frontier zone designation
- **WaterLevel**: Default water height

#### Region Management
```csharp
// Getting a region
Region region = WorldMgr.GetRegion(regionID);

// Adding object to region
region.AddObject(gameObject);

// Removing object from region
region.RemoveObject(gameObject);
```

### Zone System

**Game Rule Summary**: Zones are invisible subdivisions within regions that help organize content and optimize performance. You never see zone boundaries, but they determine what level content you'll find and help the game run smoothly. Each zone has level ranges, water levels, and special properties like whether you can dive underwater or if the ground is lava that damages you.

#### Zone Properties
```csharp
public class Zone
{
    ushort ID                // Unique zone identifier
    ushort ZoneSkinID       // Client-side zone ID
    int XOffset             // Zone X coordinate in region
    int YOffset             // Zone Y coordinate in region
    int Width               // Zone width
    int Height              // Zone height
    int Waterlevel          // Water surface height
    bool IsDivingEnabled    // Can players dive
    bool IsLava             // Lava damage zone
    bool IsPathingEnabled   // AI pathing available
}
```

#### Zone Calculations
```csharp
// Get zone from coordinates
Zone zone = region.GetZone(x, y);

// Check if point is in zone
bool inZone = (x >= zone.XOffset && x < zone.XOffset + zone.Width &&
               y >= zone.YOffset && y < zone.YOffset + zone.Height);
```

### SubZone System

**Game Rule Summary**: SubZones are tiny invisible grid squares that help the game efficiently find nearby objects and players. You'll never notice them directly, but they make sure the game can quickly show you other players, NPCs, and items in your area without checking the entire region. This system keeps the game running smoothly even with hundreds of players in busy areas.

#### SubZone Organization
- **Size**: 8192 units (SUBZONE_SIZE)
- **Grid**: 64x64 subzones per zone
- **Purpose**: Efficient object proximity queries

#### SubZone Indexing
```csharp
// Calculate subzone index
int xIndex = (x - XOffset) >> SUBZONE_SHIFT;
int yIndex = (y - YOffset) >> SUBZONE_SHIFT;
int subZoneIndex = yIndex * SUBZONE_NBR_ON_ZONE_SIDE + xIndex;
```

## Object Positioning

### Coordinate System

**Game Rule Summary**: The game world uses a coordinate system where X runs east-west (higher numbers = further east), Y runs north-south (higher numbers = further south), and Z is vertical height (higher numbers = further up). Your heading ranges from 0 to 4095, where 0 points north and the numbers increase clockwise. Understanding this helps when using location coordinates or giving directions to other players.

- **X**: East-West (increases eastward)
- **Y**: North-South (increases southward)
- **Z**: Vertical (increases upward)
- **Heading**: 0-4095 (0 = North, clockwise)

### Movement Between Regions

**Game Rule Summary**: When you travel between regions (like using a portal stone or boat), the game removes you from your current region and adds you to the new one. During this process you'll see a loading screen and your client updates to show the new area. This is different from moving within a region, which happens instantly without loading.

```csharp
public virtual bool MoveTo(ushort regionID, int x, int y, int z, ushort heading)
{
    // Notify movement
    Notify(GameObjectEvent.MoveTo, this, new MoveToEventArgs(regionID, x, y, z, heading));
    
    // Remove from current region
    if (!RemoveFromWorld())
        return false;
        
    // Update position
    m_x = x;
    m_y = y;
    m_z = z;
    Heading = heading;
    CurrentRegionID = regionID;
    
    // Add to new region
    return AddToWorld();
}
```

### Creating Objects
```csharp
public virtual bool Create(ushort regionID, int x, int y, int z, ushort heading)
{
    CurrentRegionID = regionID;
    m_x = x;
    m_y = y;
    m_z = z;
    Heading = heading;
    return AddToWorld();
}
```

## Distance and Visibility

### Distance Calculations

**Game Rule Summary**: The game constantly calculates distances to determine what you can see, target, and interact with. You can only interact with objects within specific ranges - for example, you can see players up to 5000 units away, but can only whisper to someone within 128 units. Players in different regions are treated as infinitely far away, so you can't see or interact with them at all.

```csharp
// Basic distance
int GetDistanceTo(IPoint3D point)
{
    // Returns int.MaxValue if different regions
    if (this.CurrentRegionID != obj.CurrentRegionID)
        return int.MaxValue;
        
    // 2D distance calculation
    return base.GetDistanceTo(point);
}

// With Z-factor (0-1 to reduce Z influence)
int GetDistanceTo(IPoint3D point, double zfactor)
```

### Visibility Constants

**Game Rule Summary**: The game has built-in distance limits for different interactions. You can see other players and NPCs up to 5000 units away, target them up to 4000 units, yell to 1500 units, say to 512 units, and whisper to only 128 units. You need to be within 128 units to trade with someone or talk to an NPC. These limits help create realistic communication ranges and prevent spam.

```csharp
WorldMgr.VISIBILITY_DISTANCE = 5000    // Maximum visibility range
WorldMgr.INFO_DISTANCE = 4000         // Detailed info range
WorldMgr.UPDATE_DISTANCE = 4000       // Update packet range
WorldMgr.YELL_DISTANCE = 1500         // Yell chat range
WorldMgr.SAY_DISTANCE = 512           // Say chat range
WorldMgr.WHISPER_DISTANCE = 128       // Whisper chat range
WorldMgr.GIVE_ITEM_DISTANCE = 128     // Trade range
WorldMgr.INTERACT_DISTANCE = 128      // NPC interaction range
```

### Object Detection
```csharp
// Get objects in radius
IList<T> GetObjectsInRadius<T>(int radius) where T : GameObject

// Check if within radius
bool IsWithinRadius(GameObject obj, int radius)
{
    // False if null or different region
    if (obj == null || this.CurrentRegionID != obj.CurrentRegionID)
        return false;
        
    return base.IsWithinRadius(obj, radius);
}
```

## Angle and Heading System

**Game Rule Summary**: The game tracks which direction you're facing using a heading system where 0 is north, 1024 is east, 2048 is south, and 3072 is west. This affects many abilities - you can't evade attacks from behind, some combat styles require specific positioning, and spells often have directional requirements. Learning to position yourself relative to enemies is crucial for effective combat.

### Heading Values
- **Range**: 0-4095
- **North**: 0
- **East**: 1024
- **South**: 2048
- **West**: 3072

### Angle Calculations
```csharp
// Get angle to target (0-360 degrees)
float GetAngle(IPoint2D point)
{
    float headingDifference = GetHeading(point) - Heading;
    if (headingDifference < 0)
        headingDifference += 4096.0f;
    return headingDifference * 360.0f / 4096.0f;
}

// Check if object is in front
bool IsObjectInFront(GameObject target, double arcDegrees, int alwaysTrueRange = 32)
{
    float angle = GetAngle(target);
    
    // Check if within arc
    if (angle >= 360 - arcDegrees/2 || angle < arcDegrees/2)
        return true;
        
    // Always true if very close
    return IsWithinRadius(target, alwaysTrueRange);
}
```

## Line of Sight (LoS)

### LoS Check Process

**Game Rule Summary**: Line of sight determines whether you can see and target something. The game checks if there are walls, hills, or other obstacles blocking your view to a target. This affects spellcasting, archery, and many abilities - you can't cast spells or shoot arrows through walls. The check happens on your computer using the 3D environment data, so what you see on screen matches what the game allows.

1. **Client-Based**: LoS checks use client collision data
2. **Async**: Non-blocking checks with callbacks
3. **Timeout**: Configurable timeout (default 2s)

### LoS Implementation
```csharp
// Request LoS check
player.Out.SendCheckLos(source, target, new CheckLosResponse(callback));

// Callback signature
void callback(GamePlayer player, eLosCheckResponse response, ushort sourceOID, ushort targetOID)
{
    if (response is eLosCheckResponse.TRUE)
    {
        // Line of sight exists
    }
}
```

### LoS Usage Examples
- **Spell Casting**: Verify target visibility
- **Ranged Attacks**: Check shot path
- **NPC Detection**: Stealth discovery
- **Siege Weapons**: Validate firing solutions

## Water and Swimming

### Water Mechanics

**Game Rule Summary**: Each zone has a water level that determines where you start swimming. Most zones let you swim on the surface but not dive underwater, though some special zones allow diving. Swimming slows you down and some zones have dangerous water that damages you over time. If you stay underwater too long in diving zones, you'll start drowning and take damage.

- **Waterlevel**: Per-zone water surface height
- **IsDivingEnabled**: Allow underwater movement
- **Swimming Speed**: Modified movement rate
- **Drowning**: Damage when underwater too long

### Diving System
```csharp
// Check if underwater
bool isUnderwater = player.Z < zone.Waterlevel;

// Diving enabled zones allow underwater exploration
if (zone.IsDivingEnabled && isUnderwater)
{
    // Apply diving mechanics
}
```

## Special Zone Types

### Lava Zones

**Game Rule Summary**: Some zones have lava instead of water that continuously damages you while you're in it. The damage is heat-based, so fire resistance helps reduce it. These are usually found in volcanic or underground areas and add environmental danger to exploration and combat.

- **IsLava**: Zone deals periodic damage
- **Damage Type**: Heat damage
- **Immunity**: Fire resistance reduces damage

### Dungeon Regions

**Game Rule Summary**: Dungeon regions are indoor areas with special rules. Weather effects don't work in dungeons, you often can't summon mounts, and visibility might be reduced to create a more claustrophobic atmosphere. These areas are designed for group adventures and typically contain the most challenging PvE content.

- **IsDungeon**: Special indoor handling
- **No Weather**: Weather effects disabled
- **Limited View**: Reduced visibility range
- **No Mounted**: Mounts often disabled

### RvR/Frontier Zones

**Game Rule Summary**: RvR (Realm vs Realm) zones allow player vs player combat between different realms. Frontier zones are the border areas where most large-scale PvP happens, including keep sieges and relic raids. These areas have special rules like allowing siege weapons and often provide bonuses for outnumbered realms.

- **IsRvR**: PvP combat enabled
- **IsFrontier**: Special frontier rules
- **Keep Regions**: Siege warfare enabled
- **Relic Areas**: Special objective zones

## Pathing System

### Zone Pathing
- **IsPathingEnabled**: AI pathfinding available
- **Navmesh**: Pre-calculated navigation mesh
- **Dynamic Obstacles**: Doors, players, etc.

### Path Calculation
```csharp
// Check if pathing supported
if (zone.IsPathingEnabled)
{
    // Use pathfinding system
    PathCalculator.CalculatePath(start, end);
}
else
{
    // Use direct movement
}
```

## Performance Optimizations

### SubZone Queries
```csharp
// Efficient radius searches using subzones
GetObjectsInRadius(x, y, z, radius, objectType)
{
    // Calculate affected subzones
    int minX = (x - radius) >> SUBZONE_SHIFT;
    int maxX = (x + radius) >> SUBZONE_SHIFT;
    
    // Only check relevant subzones
    for each subzone in range
        check objects in subzone
}
```

### Update Priorities
- **Close Objects**: Full updates
- **Medium Range**: Reduced updates
- **Far Objects**: Minimal updates
- **Out of Range**: No updates

## Test Scenarios

### Region Transition
```
Given: Player at edge of region 1
Action: Move to region 2
Expected: 
- Remove from region 1
- Add to region 2
- Update client region
- Reload nearby objects
```

### Zone Boundary
```
Given: Object at zone edge
Action: Query objects in radius
Expected: Objects from adjacent zones included
```

### LoS Check
```
Given: Player casting at target behind wall
Action: LoS check initiated
Expected: 
- Client calculates collision
- Returns FALSE
- Spell cast fails
```

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added region/zone mechanics
- Added coordinate systems
- Documented LoS system
- Added special zone types

## References
- `GameServer/world/Region.cs`
- `GameServer/world/Zone.cs`
- `GameServer/gameobjects/GameObject.cs`
- `GameServer/packets/Client/168/CheckLosResponseHandler.cs` 