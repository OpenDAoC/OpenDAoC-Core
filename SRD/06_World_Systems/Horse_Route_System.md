# Horse Route System

**Document Status:** Initial Documentation  
**Completeness:** 80%  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The horse route system provides automated travel between major locations using NPC-controlled mounts. Players purchase tickets from stable masters and ride along predefined paths.

## Core Mechanics

### Stable Master NPCs

#### NPC Configuration
```csharp
[NPCGuildScript("Stable Master", eRealm.None)]
public class GameStableMaster : GameMerchant
{
    // Sells tickets
    // Accepts tickets for routes
    // Spawns taxi horses
}
```

#### Ticket System
- **Item Type**: Type 40 (Horse Ticket)
- **Format**: "ticket to [destination]"
- **Unique ID**: Links to specific path
- **Stackable**: Yes, for convenience

### Route Definition

#### Path Storage
```csharp
// Paths stored in database with ticket ID as key
PathPoint path = MovementMgr.LoadPath(item.Id_nb);

// Validation
if ((path != null) && 
    (Math.Abs(path.X - this.X) < 500) && 
    (Math.Abs(path.Y - this.Y) < 500))
{
    // Valid starting point
}
```

#### Path Components
```csharp
public class PathPoint
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public int Speed { get; set; }
    public PathPoint Next { get; set; }
    public int WaitTime { get; set; } // In seconds
}
```

### Horse Spawning

#### Taxi Mount Creation
```csharp
GameTaxi mount = new GameTaxi();

// Standard properties
mount.Model = 449; // Default horse model
mount.MaxSpeedBase = 650;
mount.Size = 50; // Scales with rider race
mount.Level = 55;
mount.Name = "horse";

// Custom ticket models
if (item.Color > 0)
{
    mount = new GameTaxi(NpcTemplateMgr.GetTemplate(item.Color));
}
```

#### Race-Based Sizing
| Race | Mount Size |
|------|------------|
| Lurikeen/Kobold | 38 |
| Dwarf | 42 |
| Inconnu | 45 |
| Frostalf/Shar | 48 |
| Briton/Saracen | 48 |
| Celt/Norseman | 50 |
| Valkyn/Avalonian | 52 |
| Elf/Sylvan | 52-55 |
| Highlander | 55 |
| Firbolg/Half Ogre | 62 |
| Minotaur | 65 |
| Troll | 67 |

### Route Execution

#### Mount Process
```csharp
// Delayed mount action (400ms)
new MountHorseAction(player, mount).Start(400);

// Start movement (4 second delay)
new HorseRideAction(mount).Start(4000);

// Follow path
mount.MoveOnPath(mount.MaxSpeed);
```

#### Travel Mechanics
- **Fixed Speed**: 650 base (configurable per route)
- **No Player Control**: Automated pathing
- **Collision**: Phases through obstacles
- **Combat**: Dismounts on damage

### Boat Routes

#### Boat Stable Masters
```csharp
public class GameBoatStableMaster : GameStableMaster
{
    // Similar to horse routes but spawns boats
    GameTaxiBoat boat = new GameTaxiBoat();
    boat.Name = "Boat to " + destination;
    boat.Model = 2650;
    boat.MaxSpeedBase = 1000;
}
```

#### Boat Specifics
- **Capacity**: 16 passengers
- **Speed**: 1000 (faster than horses)
- **Embarking**: 30 second wait
- **Water Routes**: Over ocean/rivers

### Route Management

#### Path Creation (GM)
```csharp
// GM Commands
/path create - Start new path
/path add [speed] [wait] - Add waypoint
/path save <pathname> - Save to database
/path assigntaxiroute <destination> - Link to stable master
```

#### Route Validation
- Starting point within 500 units
- Path exists in database
- Ticket matches route ID
- Destination name matches

## System Features

### Ticket Purchase
```csharp
public override void OnPlayerBuy(GamePlayer player, int item_slot, int number)
{
    // Standard merchant transaction
    // Validates money
    // Adds ticket to inventory
}
```

### Ticket Usage
```csharp
public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
{
    if (item.Item_Type == 40 && isItemInMerchantList(item))
    {
        // Remove ticket from inventory
        player.Inventory.RemoveCountFromStack(item, 1);
        
        // Spawn and mount horse
        // Start route
    }
}
```

### Route Completion
- **Auto-Dismount**: At destination
- **Horse Removal**: Despawns after arrival
- **Player State**: Restored to normal
- **Location**: At destination waypoint

## Route Types

### Standard Horse Routes
- Between major cities
- Within realm territories
- To hunting grounds
- To dungeon entrances

### Special Routes
- **Boat Routes**: Cross-water travel
- **Dragon Routes**: High-level zones
- **Portal Routes**: Instant travel
- **Seasonal Routes**: Event-specific

### Route Characteristics
| Type | Speed | Cost | Availability |
|------|-------|------|--------------|
| Local | 650 | Low | All levels |
| Express | 1000 | Medium | Level 20+ |
| Boat | 1000 | Medium | Coastal |
| Dragon | 1500 | High | Level 40+ |

## Safety Features

### Anti-Exploit
```csharp
// Cannot control mount direction
mount.FixedSpeed = true;

// Cannot use abilities while mounted
if (player.Steed is GameTaxi)
    return; // Block ability use
```

### Disconnection Handling
- Mount continues on path
- Player removed safely
- No item duplication
- No stuck states

### Combat Interaction
- Dismounts on attack
- Mount despawns
- Player vulnerable
- No refunds

## Implementation Notes

### Performance
- Minimal pathfinding (predefined)
- Efficient waypoint system
- Low server impact
- Client-side smooth movement

### Database Schema
```sql
-- Path storage
PathID (varchar) - Matches ticket Id_nb
WaypointID (int) - Order in path
X, Y, Z (int) - Coordinates
Speed (int) - Movement speed
WaitTime (int) - Pause duration
```

### Network Updates
- Position updates every 500ms
- Smooth interpolation client-side
- Heading changes at waypoints
- Minimal bandwidth usage

## Configuration

### Server Properties
```csharp
// Enable horse routes
ENABLE_HORSE_ROUTES = true

// Default horse speed
HORSE_ROUTE_SPEED = 650

// Allow combat on routes
HORSE_ROUTE_COMBAT_DISMOUNT = true

// Ticket stack size
HORSE_TICKET_STACK = 10
```

## Edge Cases

### Multiple Passengers
- Each gets own mount
- Follow same path
- Independent timing
- No collisions

### Path Interruption
- Zone crash: Player at last position
- Path blocked: Phases through
- Missing waypoint: Stops safely
- Invalid destination: Refund ticket

### Realm Restrictions
- Tickets realm-specific
- Cannot use enemy routes
- Guards at stable points
- Safe passage guaranteed

## Test Scenarios

1. **Basic Route Testing**
   - Purchase ticket
   - Use at stable master
   - Complete journey
   - Arrive at destination

2. **Interruption Testing**
   - Combat dismount
   - Disconnection
   - Zone boundaries
   - Death while mounted

3. **Edge Cases**
   - Invalid tickets
   - Wrong starting point
   - Full inventory
   - Concurrent riders

4. **Performance**
   - Multiple routes active
   - Long distance paths
   - Server boundaries
   - Client prediction

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Initial documentation | 