# Boat System

**Document Status:** Initial Documentation  
**Completeness:** 85%  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The boat system allows players to own, summon, and control various watercraft for travel across water zones. Boats range from small personal skiffs to large guild warships.

## Core Mechanics

### Boat Types

#### Available Vessels
| Item ID | Model | Type | Capacity | Speed | Required Item |
|---------|-------|------|----------|-------|---------------|
| skiff | 1616 | Personal | 8 | 250 | "skiff" |
| scout_boat | 2648 | Scout | 8 | 500 | "scout_boat" |
| galleon | 2646 | Transport | 16 | 300 | "galleon" |
| warship | 2647 | Combat | 32 | 400 | "warship" |
| Viking_Longship | 1615 | Viking | 32 | 500 | "Viking_Longship" |
| ps_longship | 1595 | Longship | 31 | 600 | "ps_longship" |
| stygian_ship | 1612 | Stygian | 24 | 500 | "stygian_ship" |
| atlantean_ship | 1613 | Atlantean | 64 | 800 | "atlantean_ship" |
| British_Cog | 1614 | Cog | 33 | 700 | "British_Cog" |

### Boat Ownership

#### Database Storage
```csharp
[DataTable(TableName = "PlayerBoats")]
public class DbPlayerBoat : DataObject
{
    public string BoatID { get; set; }
    public string BoatOwner { get; set; }
    public string BoatName { get; set; }
    public ushort BoatModel { get; set; }
    public short BoatMaxSpeedBase { get; set; }
}
```

#### Ownership Rules
- One boat per type per player
- Persistent across sessions
- Transferable through items
- Guild emblems supported

### Summoning Boats

#### Command Usage
```csharp
// /boat summon
if (!client.Player.IsSwimming)
{
    // Must be in water
    return;
}

// Check for boat item in inventory
if (GameBoat.PlayerHasItem(client.Player, "boat_type"))
{
    // Create boat at player location
    GameBoat playerBoat = new GameBoat();
    playerBoat.X = client.Player.X;
    playerBoat.Y = client.Player.Y;
    playerBoat.Z = client.Player.Z;
}
```

#### Summoning Process
1. **Validation**: Player must be swimming
2. **Item Check**: Boat item in inventory
3. **Creation**: Spawn at player location
4. **Ownership**: Assign to player
5. **Mounting**: Auto-mount captain

### Boat Control

#### Movement System
```csharp
// Ground target sets destination
if (player.Steed is GameBoat)
{
    player.Out.SendMessage("You usher your boat forward.", 
        eChatType.CT_System, eChatLoc.CL_SystemWindow);
    player.Steed.WalkTo(player.GroundTarget, player.Steed.MaxSpeed);
}
```

#### Control Methods
- **Ground Target**: Click water to move
- **Follow Command**: `/boat follow`
- **Stop Command**: Automatic at destination
- **Turn Rate**: Based on boat size

### Passenger System

#### Boarding Mechanics
```csharp
public override bool RiderMount(GamePlayer rider, bool forced)
{
    // Check capacity
    if (CurrentRiders.Length >= MAX_PASSENGERS)
        return false;
        
    // Board boat
    return base.RiderMount(rider, forced);
}
```

#### Passenger Commands
- `/boat board` - Board targeted boat
- `/boat leave` - Disembark
- `/vboard` - Quick board
- `/disembark` - Leave boat

#### Seat Management
- Captain always in slot 0
- Passengers fill remaining slots
- No seat switching while moving
- Fall damage on improper exit

### Boat Management

#### Unsummoning
```csharp
// /boat unsummon
if (client.Player.InternalID == playerBoat.OwnerID)
{
    playerBoat.SaveIntoDatabase();
    playerBoat.RemoveFromWorld();
}
```

#### Auto-Removal
- 15 minutes after last passenger leaves
- Saves to database before removal
- Position preserved for next summon
- No penalty for timeout

### Combat Features

#### Boat Combat
- Players can cast from boats
- Ranged attacks allowed
- Melee limited by position
- Boat provides no defense bonus

#### Siege Potential
- Larger boats = siege platforms
- Multiple casters coordinated
- Mobile artillery positions
- Strategic water control

## Boat Properties

### Speed Calculations
```csharp
// Base speed from boat type
short baseSpeed = boat.BoatMaxSpeedBase;

// No encumbrance affects boats
// No buffs affect boat speed
// Fixed speed per type
```

### Physical Properties
- **Collision**: With terrain only
- **Draft**: Shallow water limits
- **Turning**: Wider turns for larger boats
- **Momentum**: Gradual speed changes

### Visual Features
```csharp
// Guild emblems
if (client.Player.Guild != null)
{
    playerBoat.Emblem = (ushort)client.Player.Guild.Emblem;
    playerBoat.GuildName = client.Player.Guild.Name;
}
```

## Special Features

### Taxi Boats

#### NPC Ferry Service
```csharp
public class GameTaxiBoat : GameMovingObject
{
    Model = 2650;
    MaxSpeedBase = 1000;
    MaxPassengers = 16;
    Name = "boat";
}
```

#### Ferry Routes
- Fixed paths between ports
- 30 second boarding time
- Automatic departure
- No player control

### Boat Persistence

#### Save System
```csharp
public static int SaveAllBoats()
{
    foreach (GameBoat b in m_boats.Values)
    {
        b.SaveIntoDatabase();
    }
}
```

#### Saved Properties
- Position (X, Y, Z)
- Heading/Rotation
- Owner information
- Custom name
- Model/type

## Commands

### Basic Commands
| Command | Description |
|---------|-------------|
| `/boat summon` | Summon your boat |
| `/boat unsummon` | Store your boat |
| `/boat board` | Board targeted boat |
| `/boat leave` | Leave current boat |
| `/boat follow` | Follow targeted boat |
| `/boat stop` | Stop following |
| `/boat info` | Show boat information |

### Management Commands
| Command | Description |
|---------|-------------|
| `/boat list` | List your boats |
| `/boat name <name>` | Rename boat |
| `/boat allow <player>` | Grant access |
| `/boat remove <player>` | Revoke access |

## System Integration

### Zone System
- Water zones required
- Some zones restrict boats
- Invisible boundaries enforced
- Deep water vs shallow

### Guild System
```csharp
// Guild features
if (boat.GuildName == player.Guild.Name)
{
    // Guild members can board freely
    return true;
}
```

### Trade System
- Boat items tradeable
- Ownership transfers with item
- Cannot trade while boated
- Boat becomes "unclaimed"

## Implementation Notes

### Network Optimization
- Position updates throttled
- Passenger sync maintained
- Smooth client interpolation
- Minimal packet overhead

### Database Schema
```sql
-- PlayerBoats table
BoatID (PK) - Unique identifier
BoatOwner - Character ID
BoatName - Custom name
BoatModel - Visual model
BoatMaxSpeedBase - Speed setting
```

### Performance Considerations
- Boats as moving GameObjects
- Efficient passenger tracking
- Collision simplified for water
- LOD system for distant boats

## Edge Cases

### Zone Transitions
- Boats cannot cross zone lines
- Auto-unsummon at boundaries
- Passengers dismounted safely
- Warning messages provided

### Disconnection Handling
```csharp
if (m_removeTimer == null)
    m_removeTimer = new ECSGameTimer(this, RemoveCallback);
    
m_removeTimer.Start(15 * 60 * 1000); // 15 minutes
```

### Combat Situations
- No boat damage system
- Players targetable on boats
- Falls into water on death
- Boat remains for recovery

### Capacity Overflow
- Hard limit enforced
- Clear error messages
- No boarding when full
- Captain priority maintained

## Test Scenarios

1. **Basic Functionality**
   - Summon in water
   - Board and control
   - Passenger boarding
   - Unsummon process

2. **Movement Testing**
   - Ground target navigation
   - Collision with shores
   - Zone boundary behavior
   - Speed consistency

3. **Multi-passenger**
   - Full capacity test
   - Disconnect handling
   - Combat coordination
   - Access permissions

4. **Persistence**
   - Save/load cycles
   - Cross-session ownership
   - Position accuracy
   - Database integrity

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Initial documentation | 