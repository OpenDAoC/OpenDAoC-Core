# Door System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Stable
- **Verification**: Code-verified from GameDoor.cs, GameKeepDoor.cs, DoorMgr.cs
- **Implementation**: Stable

## Overview
The door system manages all interactive doors in the game including regular doors, keep doors, and special relic gates. Doors can be opened/closed, damaged, locked, and have specific behaviors based on their type and location.

## Core Mechanics

### Door Types

#### 1. Regular Doors (GameDoor)
- **Base Type**: GameDoorBase
- **Door ID**: Unique identifier based on zone and position
- **States**: Open/Closed
- **Features**:
  - Auto-close after 5 seconds
  - Can be locked/unlocked
  - Health-based destruction
  - Interaction radius check

#### 2. Keep Doors (GameKeepDoor)
- **Inheritance**: GameDoorBase → GameKeepDoor
- **Special Properties**:
  - Associated with keep components
  - Attackable based on position (main gates)
  - Non-attackable postern doors
  - Health regeneration when keep not in combat
  - Realm-based access control

#### 3. Relic Doors (GameRelicDoor)
- **Purpose**: Protect relic keeps
- **Properties**:
  - Always at full health
  - Cannot be damaged
  - Special teleport mechanics

### Door ID System

#### ID Structure
```csharp
// Door ID = Type * 100000000 + Zone * 1000000 + UniqueID
// Keep Door ID structure:
int doortype = 7;  // Keep door type
int ownerKeepID = keep.KeepID;
int towerIndex = tower.KeepID >> 8;  // For towers
int componentID = component.ID;
int doorIndex = position.TemplateType;

// Final ID calculation:
int id = doortype * 100000000 + 
         ownerKeepID * 100000 + 
         towerIndex * 10000 + 
         componentID * 100 + 
         doorIndex;
```

### Door States and Behaviors

#### State Management
```csharp
public enum eDoorState
{
    Open,
    Closed
}

// State transitions with synchronization
lock (_stateLock)
{
    if (DbDoor.State != value)
    {
        DbDoor.State = (int)value;
        // Notify all players in range
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            player.Out.SendDoorState(CurrentRegion, this);
    }
}
```

#### Auto-Close Mechanism
- **Duration**: 5 seconds (STAYS_OPEN_DURATION)
- **Conditions**: Health > 40% or not destroyed
- **Implementation**: Timer-based automatic closure

### Health and Damage System

#### Regular Doors
```csharp
// Health thresholds
if (Health < MaxHealth * 0.4)  // 40% health
{
    // Door cannot auto-close when heavily damaged
    _openDead = true;
}

// Repair timer: Every 30 seconds
REPAIR_INTERVAL = 30 * 1000;
// Repair amount: Variable based on damage
```

#### Keep Doors
```csharp
// Health regeneration when keep not in combat
public int RepairTimerCallback(ECSGameTimer timer)
{
    if (!Component.Keep.InCombat && HealthPercent < 100)
        Repair(MaxHealth / 100 * 5);  // 5% per interval
    return REPAIR_INTERVAL;
}

// Door closes automatically above 15% health
private const int DOOR_CLOSE_THRESHOLD = 15;
```

### Interaction System

#### Distance Checks
```csharp
// Base interaction distance
int radius = Properties.WORLD_PICKUP_DISTANCE * 4;

// Special cases:
// ToA dungeons: radius *= 4
// Keep doors: Custom teleport distances
```

#### Interaction Permissions
1. **Regular Doors**: Check CanBeOpenedViaInteraction (locked state)
2. **Keep Doors**: Realm check for teleportation
3. **Combat Restrictions**: No interaction while mezzed/stunned

### Keep Door Specifics

#### Teleportation Mechanics
```csharp
// Calculate teleport destination
int keepz = Z, distance = 0;

// Normal door
if (DoorIndex == 1)
    distance = 150;
else  // Side or internal door
    distance = 100;

// Tower entry - raise Z coordinate
if (Component.Keep is GameKeepTower && IsObjectInFront(player, 180))
{
    if (DoorId == 1)
        keepz = Z + 83;  // Verified in-game
    else
        distance = 150;
}

// Keep inner door - raise Z coordinate  
if (IsInnerDoor && IsObjectInFront(player, 180))
    keepz = Z + 92;  // Level 1 keep verified
```

#### Attackable Door Rules
```csharp
public override bool IsAttackableDoor
{
    get
    {
        if (Component.Keep is GameKeepTower)
            return DoorIndex == 1;  // Only main tower door
        else if (Component.Keep is GameKeep || RelicGameKeep)
            return !IsPostern;  // All except postern doors
        return false;
    }
}
```

## Database Structure

### DbDoor Table
```sql
CREATE TABLE Door (
    Door_ID varchar(255) PRIMARY KEY,
    Type int NOT NULL,              -- Door type (0=regular, 7=keep)
    Name varchar(255),
    InternalID int NOT NULL,        -- Unique door identifier
    Guild varchar(255),             -- Associated guild
    X int NOT NULL,
    Y int NOT NULL,
    Z int NOT NULL,
    Heading int NOT NULL,
    Realm tinyint NOT NULL,
    Level tinyint NOT NULL,
    Flags int NOT NULL DEFAULT 0,
    Locked int NOT NULL DEFAULT 0,
    Health int NOT NULL,
    State int NOT NULL DEFAULT 0,   -- eDoorState
    IsPostern bit NOT NULL DEFAULT 0
);
```

### Keep Door Positions
```sql
CREATE TABLE KeepPosition (
    KeepPosition_ID varchar(255) PRIMARY KEY,
    ComponentSkin int NOT NULL,
    ComponentRotation int NOT NULL,
    TemplateID varchar(255),
    TemplateType int NOT NULL,      -- Door index
    XOff int NOT NULL,              -- Offset from component
    YOff int NOT NULL,
    ZOff int NOT NULL,
    HOff int NOT NULL,
    ClassType varchar(255)
);
```

## Client Interaction

### Door Request Packet
```csharp
[PacketHandler(eClientPackets.DoorRequest)]
public class DoorRequestHandler
{
    // Client sends:
    // - Door ID (int)
    // - Door State (byte)
    
    // Special handling for ToA:
    if (client.Player.CurrentRegion.Expansion == eClientExpansion.TrialsOfAtlantis)
    {
        // Reconstruct door ID using current zone
        doorId -= (doorId / 1000000) * 1000000;
        doorId += client.Player.CurrentZone.ID * 1000000;
    }
}
```

### Door State Updates
- Sent to all players in VISIBILITY_DISTANCE
- Updates position, state, and model
- Synchronized across all clients

## System Interactions

### Keep System
- Doors tied to keep components
- Health scales with keep level
- Access based on keep ownership
- Repair tied to keep combat state

### Combat System
- Doors can be attacked if attackable
- Damage reduces health
- Death opens door permanently until repaired
- Experience/realm points from door destruction

### Zone System
- Door IDs incorporate zone information
- Special handling for instanced zones
- ToA requires zone-based ID reconstruction

## Performance Considerations

### Door Manager
```csharp
public static class DoorMgr
{
    // Cached door lookup by ID
    private static readonly Dictionary<int, List<GameDoorBase>> m_doors;
    
    // Thread-safe registration
    public static void RegisterDoor(GameDoorBase door)
    {
        lock (Lock)
        {
            // Multiple doors can share same ID (instances)
            if (!m_doors.TryGetValue(door.DoorId, out List<GameDoorBase> list))
            {
                list = new List<GameDoorBase>();
                m_doors.Add(door.DoorId, list);
            }
            list.Add(door);
        }
    }
}
```

### Update Optimization
- Only update doors for players in range
- Batch state changes when possible
- Minimize database writes

## Test Scenarios

### Basic Door Interaction
```csharp
// Given: Player near unlocked door
// When: Player interacts
// Then: Door opens, closes after 5 seconds

// Given: Player near locked door  
// When: Player interacts
// Then: No state change, appropriate message

// Given: Damaged door below 40% health
// When: Door opened
// Then: Door remains open (no auto-close)
```

### Keep Door Combat
```csharp
// Given: Enemy at keep main gate
// When: Attack door to 0 health
// Then: Door opens, cannot close until repaired

// Given: Keep door at 14% health
// When: Door repaired to 16% health  
// Then: Door automatically closes

// Given: Postern door
// When: Any attack attempted
// Then: No damage taken (not attackable)
```

### Teleportation Tests
```csharp
// Given: Friendly player at keep door
// When: Interact with door
// Then: Teleport to appropriate location based on door type

// Given: Enemy player at keep door
// When: Interact with door
// Then: No teleportation, combat initiated if configured
```

## Change Log
- 2025-01-20: Initial documentation created
- TODO: Document special door types (instances, dungeons)
- TODO: Add gate keeper interaction specifics

## References
- `GameServer/gameobjects/GameDoor.cs`
- `GameServer/gameobjects/GameDoorBase.cs`
- `GameServer/keeps/Gameobjects/GameKeepDoor.cs`
- `GameServer/keeps/Gameobjects/GameRelicDoor.cs`
- `GameServer/gameutils/DoorMgr.cs`
- `GameServer/packets/Client/168/DoorRequestHandler.cs` 