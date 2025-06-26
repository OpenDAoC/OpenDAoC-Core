# Teleportation System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Stable
- **Verification**: Code-verified from GameTeleporter.cs, TeleportArea.cs, bind/recall spells
- **Implementation**: Stable

## Overview
The teleportation system provides instant travel mechanics including teleport NPCs, bind stones, recall abilities, portal keeps, and area-based teleportation. The system enforces various restrictions based on combat state, carried items, and zone types.

## Core Mechanics

### Teleportation Types

#### 1. NPC Teleporters
- **Base Class**: GameTeleporter
- **Types**:
  - Regular teleporters (realm-specific destinations)
  - Portal keep teleporters (RvR travel)
  - Throne room teleporters (guild housing)
  - Personal house teleporters

#### 2. Bind/Recall System
- **Bind Points**: City, guild, house, personal locations
- **Recall Methods**:
  - /release command (death recall)
  - Personal bind recall stones
  - Realm ability recalls
  - Spell-based recalls

#### 3. Area Teleportation
- **Teleport Areas**: Automatic teleport on entry
- **Zone Points**: Zone transition teleports
- **Instance Doors**: Special instance routing

### Teleporter NPC Mechanics

#### Destination Loading
```csharp
public class GameTeleporter : GameNPC
{
    // Load destinations for this teleporter's realm
    protected virtual void LoadDestinations()
    {
        if (DestinationRealm == 0 || DestinationRealm > 3)
            DestinationRealm = Realm;
            
        m_destinations = DOLDB<DbTeleport>.SelectObjects(
            DB.Column("Type").IsEqualTo("DestinationType")
            .And(DB.Column("Realm").IsEqualTo(DestinationRealm)
                .Or(DB.Column("Realm").IsEqualTo(0)))
        );
    }
}
```

#### Special Destinations
```csharp
// "entrance" - Housing entrance
if (text.ToLower() == "entrance")
{
    DbTeleport houseEntrance = GetHousingEntrance(player.Realm);
    OnDestinationPicked(player, houseEntrance);
}

// "personal" - Player's personal house
if (text.ToLower() == "personal")
{
    House house = HouseMgr.GetHouseByPlayer(player);
    if (house != null)
    {
        IGameLocation location = house.OutdoorJumpPoint;
        // Create temporary teleport destination
    }
}

// "hearth" - House bind point
if (text.ToLower() == "hearth")
{
    if (player.BindHouseRegion > 0)
    {
        // Verify house still exists
        // Check for bindstone
        // Teleport to bindstone location
    }
}
```

### Bind System

#### Bind Types
```csharp
public enum eBindType
{
    City,       // Regular city bind points
    Guild,      // Guild house/keep bind
    House,      // Personal/guild house bind
    Personal    // Custom bind location (RA)
}

// Bind point storage
public class GamePlayer
{
    // City bind
    public int BindRegion { get; set; }
    public int BindXpos { get; set; }
    public int BindYpos { get; set; }
    public int BindZpos { get; set; }
    public int BindHeading { get; set; }
    
    // House bind
    public int BindHouseRegion { get; set; }
    public int BindHouseXpos { get; set; }
    public int BindHouseYpos { get; set; }
    public int BindHouseZpos { get; set; }
    public int BindHouseHeading { get; set; }
}
```

#### Bind Stone Interaction
```csharp
public class BindStone : GameStaticItem
{
    public override bool Interact(GamePlayer player)
    {
        if (player.Realm != Realm)
            return false;
            
        player.Bind(true);  // Save bind location
        player.Out.SendMessage("You are now bound to this location.", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return true;
    }
}
```

### Recall Mechanics

#### Personal Bind Recall Stone
```csharp
[SpellHandler(eSpellType.GatewayPersonalBind)]
public class GatewayPersonalBind : SpellHandler
{
    public override bool CheckBeginCast(GameLiving selectedTarget)
    {
        GamePlayer player = Caster as GamePlayer;
        
        // Zone restrictions
        if (player.CurrentZone.IsRvR || 
            player.CurrentRegion.IsInstance ||
            player.CurrentRegion.ID == 497)  // Jail
        {
            player.Out.SendMessage("You can't use that here!", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }
        
        // Combat restrictions
        if (player.InCombat || GameRelic.IsPlayerCarryingRelic(player))
        {
            SendInCombatMessage(player);
            return false;
        }
        
        // Movement restrictions
        if (player.IsMoving)
        {
            SendMovingMessage(player);
            return false;
        }
        
        return base.CheckBeginCast(selectedTarget);
    }
}
```

#### Death Release
```csharp
// Release types
public enum eReleaseType
{
    Normal,     // Regular graveyard
    City,       // City bind point
    House,      // House bind point
    Guild,      // Guild keep/house
    RvR         // Closest keep in RvR
}

// /release city command
if (type == eReleaseType.City)
{
    player.MoveTo(player.BindRegion, 
        player.BindXpos, player.BindYpos, player.BindZpos, 
        player.BindHeading);
}
```

### Teleportation Restrictions

#### Combat Restrictions
- No teleportation while in combat
- No teleportation while carrying relics
- Spell interruption rules apply

#### Zone Restrictions
```csharp
// RvR zones - restricted teleportation
if (player.CurrentZone.IsRvR && 
    GameServer.ServerType != EGameServerType.GST_PvE)
    return false;

// Instance zones - special handling
if (player.CurrentRegion.IsInstance)
    return HandleInstanceTeleport(player);

// Jail - no teleportation
if (player.CurrentRegion.ID == 497 && 
    player.Client.Account.PrivLevel == 1)
    return false;
```

#### State Restrictions
- Cannot teleport while mezzed
- Cannot teleport while stunned
- Cannot teleport while moving (some types)
- Cannot teleport while stealthed (breaks stealth)

### Portal Keep System

#### Gate Keeper NPCs
```csharp
public class GateKeeperIn : GameKeepGuard
{
    // Inside keep - teleport out
    protected List<DbKeepDoorTeleport> m_destinationsIn;
    
    public override bool WhisperReceive(GameLiving source, string text)
    {
        // Find destination for this keep
        foreach (DbKeepDoorTeleport t in m_destinationsIn)
        {
            if (t.KeepID == Component.Keep.KeepID)
            {
                OnTeleportSpell(player, t, delayed: text != "interact");
                break;
            }
        }
    }
}

public class GateKeeperOut : GameKeepGuard
{
    // Outside keep - teleport in
    protected List<DbKeepDoorTeleport> m_destinationsOut;
    
    // Similar implementation for entry
}
```

#### Portal Keep Spell
```csharp
public class UniPortalKeep : SpellHandler
{
    private DbKeepDoorTeleport m_destination;
    
    public override void FinishSpellCast(GameLiving target)
    {
        target.MoveTo(m_destination.Region, 
            m_destination.X, m_destination.Y, m_destination.Z, 
            m_destination.Heading);
    }
}
```

### Zone Transition Teleports

#### Zone Points
```csharp
public class ZonePoint : Point3D
{
    public ushort TargetRegion { get; set; }
    public int TargetX { get; set; }
    public int TargetY { get; set; }
    public int TargetZ { get; set; }
    public ushort TargetHeading { get; set; }
    
    public void TeleportPlayer(GamePlayer player)
    {
        if (CheckConstraints(player))
        {
            player.MoveTo(TargetRegion, TargetX, TargetY, TargetZ, TargetHeading);
        }
    }
}
```

#### Instance Doors
```csharp
public virtual bool OnInstanceDoor(GamePlayer player, DbZonePoint zonePoint)
{
    // Check instance availability
    if (!InstanceMgr.CanEnterInstance(player, zonePoint.TargetRegion))
        return false;
        
    // Create/join instance
    Instance instance = InstanceMgr.GetOrCreateInstance(player, zonePoint);
    
    // Custom routing for quest instances
    if (instance.IsQuestInstance)
        return HandleQuestInstanceEntry(player, instance);
        
    // Standard instance entry
    return true;
}
```

## Database Structure

### Teleport Destinations
```sql
CREATE TABLE Teleport (
    Teleport_ID varchar(255) PRIMARY KEY,
    TeleportID varchar(255) NOT NULL,
    Type varchar(255),
    Realm tinyint NOT NULL,
    RegionID int NOT NULL,
    X int NOT NULL,
    Y int NOT NULL,
    Z int NOT NULL,
    Heading int NOT NULL
);
```

### Keep Door Teleports
```sql
CREATE TABLE KeepDoorTeleport (
    KeepID int NOT NULL,
    Region int NOT NULL,
    X int NOT NULL,
    Y int NOT NULL,
    Z int NOT NULL,
    Heading int NOT NULL,
    ComponentID int,
    DoorIndex int
);
```

## Performance Considerations

### Destination Caching
```csharp
public class GameTeleporter
{
    // Cache destinations on first interaction
    private List<DbTeleport> m_destinations;
    
    public override bool Interact(GamePlayer player)
    {
        if (m_destinations == null)
            LoadDestinations();
            
        // Present destination list
    }
}
```

### Teleportation Events
```csharp
// Pre-teleport validation
GameServer.ServerRules.IsAllowedToTeleport(player, destination);

// Teleportation execution
GameLocation oldLocation = new GameLocation(player);
player.MoveTo(destination);

// Post-teleport events
GameServer.ServerRules.OnPlayerTeleport(player, oldLocation, destination);
```

## System Integration

### Combat System
- InCombat flag prevents most teleportation
- Combat timer affects recall abilities
- Teleportation breaks stealth

### Housing System
- House teleporters for personal/guild houses
- Bindstone requirements for hearth recall
- House repossession affects binds

### RvR System
- Portal keeps enable strategic movement
- Keep ownership determines access
- Relic carriers cannot teleport

## Test Scenarios

### Basic Teleportation
```csharp
// Given: Player at city teleporter
// When: Select valid destination
// Then: Instant transport to destination

// Given: Player in combat
// When: Attempt teleport
// Then: "You can't teleport in combat!" message

// Given: Player carrying relic
// When: Attempt any teleportation
// Then: Teleportation blocked
```

### Bind/Recall Tests
```csharp
// Given: Player at bind stone
// When: Interact with stone
// Then: Bind point updated

// Given: Player with personal bind recall
// When: Use in RvR zone
// Then: "You can't use that here!" message

// Given: Dead player
// When: /release city
// Then: Resurrect at city bind point
```

### Portal Keep Tests
```csharp
// Given: Player at friendly portal keep
// When: Talk to gate keeper
// Then: Show available destinations

// Given: Enemy at portal keep
// When: Approach gate keeper
// Then: No interaction possible
```

## Change Log
- 2025-01-20: Initial documentation created
- TODO: Document boat routes
- TODO: Add summoning mechanics
- TODO: Document griffin/horse routes

## References
- `GameServer/gameobjects/GameTeleporter.cs`
- `GameServer/gameobjects/CustomNPC/Teleporters/`
- `GameServer/spells/Teleport/GatewayPersonalBind.cs`
- `GameServer/keeps/Gameobjects/Guards/GateKeeper.cs`
- `GameServer/world/ZonePoint.cs` 