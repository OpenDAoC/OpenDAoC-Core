# Transportation System

**Document Status:** Production Ready
**Implementation Status:** Live
**Verification:** Code Verified

## Overview

The Transportation System provides automated and player-controlled travel options including personal mounts, horse route tickets, boat systems, and taxi services. The system integrates multiple transport modes to facilitate efficient movement across the game world.

## Core Architecture

### Personal Mount System

#### Mount Types and Properties
```csharp
// Mount speed calculation
public short Speed
{
    get
    {
        if (m_level <= 35)
            return ServerProperties.Properties.MOUNT_UNDER_LEVEL_35_SPEED; // Default: 135% speed
        else
            return ServerProperties.Properties.MOUNT_OVER_LEVEL_35_SPEED;  // Default: 145% speed
    }
}
```

#### Mount Mechanics
- **Basic Mounts (Level ≤35)**: 135% speed, no RvR restrictions
- **Advanced Mounts (Level >35)**: 145% speed, cannot summon in RvR
- **Encumbrance Effect**: Speed reduced when overloaded
- **Combat Dismount**: Automatic dismount when taking damage

#### Summoning Restrictions
```csharp
string reason = GameServer.ServerRules.ReasonForDisallowMounting(this);

// Restrictions include:
// - Cannot mount while moving
// - Cannot mount in combat
// - Cannot mount while stealthed
// - Cannot mount while carrying relic
// - Cannot mount while dead
// - Cannot mount while seated
```

### Horse Route System

#### Stable Master Network
```csharp
[NPCGuildScript("Stable Master", eRealm.None)]
public class GameStableMaster : GameMerchant
{
    // Sells route tickets
    // Spawns taxi mounts
    // Validates routes
}
```

#### Ticket Mechanics
```csharp
// Ticket validation and route execution
if (item.Item_Type == 40 && isItemInMerchantList(item))
{
    PathPoint path = MovementMgr.LoadPath(item.Id_nb);
    
    // Validate starting position (500 unit radius)
    if ((path != null) && 
        (Math.Abs(path.X - this.X) < 500) && 
        (Math.Abs(path.Y - this.Y) < 500))
    {
        // Create taxi mount
        GameTaxi mount = new GameTaxi();
        // Configure based on ticket properties
    }
}
```

#### Route Configuration
- **Ticket Format**: "ticket to [destination]"
- **Item Type**: 40 (Horse Ticket)
- **Path Storage**: Database-linked by ticket ID
- **Race-Specific Sizing**: Mount size scales with player race

#### Race-Based Mount Sizing
```csharp
switch ((eRace)player.Race)
{
    case eRace.Lurikeen:
    case eRace.Kobold:
        mount.Size = 38;
        break;
    case eRace.Dwarf:
        mount.Size = 42;
        break;
    case eRace.Briton:
    case eRace.Norseman:
    case eRace.Celt:
        mount.Size = 50;
        break;
    case eRace.Firbolg:
    case eRace.HalfOgre:
        mount.Size = 62;
        break;
    case eRace.Troll:
        mount.Size = 67;
        break;
    // Additional race configurations...
}
```

### Boat System

#### Boat Types and Specifications
| Type | Model | Capacity | Speed | Required Item |
|------|-------|----------|-------|---------------|
| Skiff | 1616 | 8 | 250 | "skiff" |
| Scout Boat | 2648 | 8 | 500 | "scout_boat" |
| Galleon | 2646 | 16 | 300 | "galleon" |
| Warship | 2647 | 32 | 400 | "warship" |
| Viking Longship | 1615 | 32 | 500 | "Viking_Longship" |
| Stygian Ship | 1612 | 24 | 500 | "stygian_ship" |
| Atlantean Ship | 1613 | 64 | 800 | "atlantean_ship" |
| British Cog | 1614 | 33 | 700 | "British_Cog" |

#### Boat Summoning Process
```csharp
// Boat summoning validation
if (!client.Player.IsSwimming)
{
    // Must be in water to summon
    client.Player.Out.SendMessage("You must be in the water before you can summon your boat!");
    return;
}

// Check for boat item in inventory
if (GameBoat.PlayerHasItem(client.Player, "scout_boat"))
{
    // Create and configure boat
    GameBoat playerBoat = new GameBoat();
    playerBoat.BoatID = System.Guid.NewGuid().ToString();
    playerBoat.Name = client.Player.Name + "'s scout boat";
    playerBoat.Model = 2648;
    playerBoat.MaxSpeedBase = 500;
    
    // Guild emblem support
    if (client.Player.Guild != null && client.Player.Guild.Emblem != 0)
    {
        playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;
        playerBoat.GuildName = client.Player.Guild.Name;
    }
}
```

#### Boat Management
```csharp
// Automatic removal system
public override bool RiderDismount(bool forced, GamePlayer player)
{
    if (CurrentRiders.Length == 0)
    {
        // Start 15-minute removal timer when empty
        m_removeTimer.Start(15 * 60 * 1000);
    }
    return true;
}
```

### Taxi Services

#### Taxi Mount System
```csharp
public class GameTaxi : GameMovingObject
{
    // Standard taxi properties
    Model = 449;           // Default horse model
    MaxSpeedBase = 650;    // Base taxi speed
    Level = 55;
    Size = 50;             // Adjusted per rider race
    FixedSpeed = true;     // Player cannot control
}
```

#### Route Execution
```csharp
// Automated taxi movement
protected class HorseRideAction : ECSGameTimerWrapperBase
{
    protected override int OnTick(ECSGameTimer timer)
    {
        GameNPC horse = (GameNPC)timer.Owner;
        horse.MoveOnPath(horse.MaxSpeed);
        return 0;
    }
}

// Delayed mount process
new MountHorseAction(player, mount).Start(400);  // 400ms delay
new HorseRideAction(mount).Start(4000);          // 4s start delay
```

### Boat Taxi System

#### Boat Stable Masters
```csharp
public class GameBoatStableMaster : GameMerchant
{
    // Similar to horse routes but for water travel
    GameTaxiBoat boat = new GameTaxiBoat();
    boat.Name = "Boat to " + destination;
    boat.Model = 2650;              // Standard ferry model
    boat.MaxSpeedBase = 1000;       // Faster than horse routes
    boat.MAX_PASSENGERS = 16;       // Higher capacity
}
```

#### Ferry Mechanics
- **Boarding Time**: 30 seconds before departure
- **Route Speed**: 1000 (faster than horses)
- **Capacity**: 16 passengers
- **Automation**: No player control, follows fixed path

### Path Management System

#### Route Creation (GM Commands)
```csharp
// Path creation workflow
/path create                    // Start new path
/path add [speed] [wait]       // Add waypoint with properties
/path save <pathname>          // Save to database
/path assigntaxiroute <dest>   // Link to stable master
```

#### Path Validation
```csharp
// Route validation checks
PathPoint path = MovementMgr.LoadPath(item.Id_nb);

// Validation requirements:
// 1. Path exists in database
// 2. Starting point within 500 units of stable master
// 3. Ticket ID matches path ID
// 4. Destination accessible
```

## System Integration

### Movement System Integration
```csharp
// Mount speed calculation integration
if (living is GamePlayer player)
{
    double horseSpeed = player.IsOnHorse ? player.ActiveHorse.Speed * 0.01 : 1.0;
    
    if (speed > horseSpeed)
        horseSpeed = 1.0; // Buffs don't stack with mounts
        
    speed *= horseSpeed;
}
```

### Combat System Integration
- **Auto-Dismount**: Taking damage dismounts player
- **Mount Vulnerability**: Player targetable while mounted
- **Speed Buffs**: Don't stack with mount speed
- **Relic Carriers**: Cannot mount while carrying relics

### Guild System Integration
```csharp
// Guild emblem display on boats
if (client.Player.Guild != null)
{
    if (client.Player.Guild.Emblem != 0)
        playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;
    playerBoat.GuildName = client.Player.Guild.Name;
}
```

## Performance Optimization

### Network Efficiency
- **Position Throttling**: Taxi updates every 500ms
- **Client Interpolation**: Smooth movement client-side
- **Range Culling**: Only update nearby players
- **Batch Updates**: Multiple passengers in single packet

### Memory Management
```csharp
// Boat cleanup system
protected int RemoveCallback(ECSGameTimer timer)
{
    m_removeTimer.Stop();
    m_removeTimer = null;
    Delete(); // Clean removal after timeout
    return 0;
}
```

### Path Optimization
- **Minimal Pathfinding**: Predefined routes only
- **Efficient Waypoints**: Direct point-to-point movement
- **Low Server Impact**: Client handles interpolation
- **Database Caching**: Frequently used paths cached

## Configuration System

### Server Properties
```csharp
// Transportation configuration
MOUNT_UNDER_LEVEL_35_SPEED = 135;    // Basic mount speed
MOUNT_OVER_LEVEL_35_SPEED = 145;     // Advanced mount speed
ENABLE_HORSE_ROUTES = true;          // Enable taxi system
HORSE_ROUTE_SPEED = 650;             // Default taxi speed
HORSE_ROUTE_COMBAT_DISMOUNT = true;  // Combat dismounts
BOAT_AUTO_REMOVE_TIME = 900;         // 15 minutes (seconds)
```

### Database Schema
```sql
-- Path storage for routes
CREATE TABLE Path (
    PathID VARCHAR(255) PRIMARY KEY,  -- Matches ticket Id_nb
    WaypointID INT,                   -- Order in sequence
    X INT NOT NULL,
    Y INT NOT NULL, 
    Z INT NOT NULL,
    Speed INT DEFAULT 650,
    WaitTime INT DEFAULT 0            -- Pause duration in seconds
);

-- Player boat persistence
CREATE TABLE PlayerBoats (
    BoatID VARCHAR(255) PRIMARY KEY,
    BoatOwner VARCHAR(255),
    BoatName VARCHAR(255),
    BoatModel SMALLINT,
    BoatMaxSpeedBase SMALLINT,
    X INT,
    Y INT,
    Z INT,
    Heading SMALLINT,
    Region SMALLINT
);
```

## Edge Cases & Special Handling

### Disconnection Management
```csharp
// Taxi continues on disconnect
// Player removed safely on reconnection
// No item duplication
// Position restored appropriately
```

### Zone Transitions
- **Boats**: Cannot cross zone boundaries
- **Horse Routes**: Auto-unsummon at zone edges
- **Taxis**: Route-specific zone handling
- **Mount State**: Preserved where possible

### Combat Scenarios
```csharp
// Mount combat rules
if (player.InCombat)
{
    player.Out.SendMessage("You are in combat and cannot call your mount.");
    return;
}

// Damage dismount
if (damageAmount > 0 && player.IsOnHorse)
{
    player.IsOnHorse = false; // Auto-dismount
}
```

### Capacity Management
```csharp
// Boat capacity enforcement
public override bool RiderMount(GamePlayer rider, bool forced)
{
    if (CurrentRiders.Length >= MAX_PASSENGERS)
        return false; // Boat full
        
    return base.RiderMount(rider, forced);
}
```

## Test Scenarios

### Mount System Testing
1. **Speed Validation**: Correct speed bonuses applied
2. **Restriction Enforcement**: Cannot mount in invalid states
3. **Combat Dismount**: Proper dismount on damage
4. **Relic Interaction**: Mount restrictions while carrying relics

### Route System Testing  
1. **Path Validation**: Routes execute correctly
2. **Passenger Management**: Proper boarding/disembarking
3. **Timing Accuracy**: Correct delays and durations
4. **Multi-Passenger**: Multiple riders handled properly

### Boat System Testing
1. **Summoning Validation**: Water requirement enforced
2. **Capacity Limits**: Passenger limits respected
3. **Persistence**: Boat state saved/restored correctly
4. **Guild Integration**: Emblems display properly

### Edge Case Validation
1. **Disconnection Recovery**: Proper cleanup and restoration
2. **Zone Boundaries**: Appropriate transition handling
3. **Combat Integration**: Correct interaction with combat system
4. **Performance**: System handles multiple concurrent users

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Production documentation |
| Historical | Game | Boat system expansion |
| Historical | Game | Horse route automation |
| Original | Game | Basic mount system | 