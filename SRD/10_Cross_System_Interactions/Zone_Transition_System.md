# Zone Transition System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview

**Game Rule Summary**: The Zone Transition System manages how you move between different areas of the game world. When you walk through doors, click on teleporters, use portal stones, or cross zone boundaries, this system coordinates all the complex interactions needed to move you safely from one place to another. It checks if you're allowed to go where you're trying to go (based on your realm, level, combat status, or whether you're carrying a relic), handles loading screens when moving between regions, and makes sure all the different game systems (like quests, guilds, and housing) know about your location change. The system also prevents exploits like teleporting while in combat or entering enemy areas you shouldn't have access to.

The zone transition system manages complex interactions between multiple game systems when players move between zones, regions, and instances. It coordinates with region management, door systems, teleportation mechanics, keep systems, and instance management to provide seamless world navigation.

## Core Mechanics

### Zone Point System

#### Database Structure
```csharp
[DataTable(TableName = "ZonePoint")]
public class DbZonePoint : DataObject
{
    public ushort Region { get; set; }          // Target region
    public int X { get; set; }                  // Target X coordinate
    public int Y { get; set; }                  // Target Y coordinate  
    public int Z { get; set; }                  // Target Z coordinate
    public ushort Heading { get; set; }         // Target heading
    public string ClassType { get; set; }      // Custom handler class
}
```

#### Zone Point Processing
```csharp
public class RegionChangeRequestHandler : IPacketHandler
{
    public void HandlePacket(GameClient client, GSPacketIn packet)
    {
        DbZonePoint zonePoint = WorldMgr.GetZonePoint(zonePointId);
        
        // Custom handler selection
        IJumpPointHandler customHandler = null;
        if (!string.IsNullOrEmpty(zonePoint.ClassType))
        {
            string typeName = client.Player.CurrentRegion.IsInstance 
                ? "DOL.GS.ServerRules.InstanceDoorJumpPoint" 
                : zonePoint.ClassType;
            customHandler = CreateHandler(typeName);
        }
        
        // Process zone transition
        if (customHandler?.IsAllowedToJump(zonePoint, player) ?? true)
            ProcessZoneTransition(player, zonePoint);
    }
}
```

### Area Transition Management

#### Area Detection System
```csharp
// Area transitions during movement
IList<IArea> newAreas = client.Player.CurrentRegion.GetAreasOfZone(newZone, client.Player);

// Check for left areas
if (oldAreas != null)
{
    foreach (IArea area in oldAreas)
    {
        if (!newAreas.Contains(area))
        {
            area.OnPlayerLeave(client.Player);
            
            // Special handling for Border Keep areas
            if (IsBorderKeepArea(area))
                RealmTimer.CheckRealmTimer(client.Player);
        }
    }
}

// Check for entered areas
foreach (IArea area in newAreas)
{
    if (oldAreas == null || !oldAreas.Contains(area))
        area.OnPlayerEnter(client.Player);
}
```

#### Zone Boundary Detection
```csharp
bool zoneChange = newZone != client.Player.LastPositionUpdateZone;

if (zoneChange)
{
    // Prevent falling damage on region changes
    if (client.Player.LastPositionUpdateZone != null && 
        newZone.ZoneRegion.ID != client.Player.LastPositionUpdateZone.ZoneRegion.ID)
        client.Player.MaxLastZ = int.MinValue;
        
    // Zone entry notifications
    SendZoneEntryMessage(client.Player, newZone);
}
```

### Door System Integration

#### Door Type Determination
```csharp
public class DoorMgr
{
    public static bool RegisterDoor(DbDoor door)
    {
        Zone currentZone = WorldMgr.GetZone(zone);
        GameDoorBase mydoor = null;
        
        // Check if door is in keep area
        foreach (AbstractArea area in currentZone.GetAreasOfSpot(door.X, door.Y, door.Z))
        {
            if (area is KeepArea)
            {
                mydoor = new GameKeepDoor();
                break;
            }
        }
        
        // Standard door if not in keep
        if (mydoor == null)
            mydoor = new GameDoor();
            
        mydoor.LoadFromDatabase(door);
        return true;
    }
}
```

#### Keep Door Teleportation
```csharp
[DataTable(TableName = "Keepdoorteleport")]
public class DbKeepDoorTeleport : DataObject
{
    public ushort Region { get; set; }      // Destination region
    public int X { get; set; }              // Destination coordinates
    public int Y { get; set; }
    public int Z { get; set; }
    public ushort Heading { get; set; }
    public int KeepID { get; set; }         // Associated keep
    public string TeleportText { get; set; }// Interaction text
    public string TeleportType { get; set; }// In/Out type
}
```

**Keep Door Interaction**:
```csharp
public class GameKeepDoor : GameDoorBase
{
    public override bool Interact(GamePlayer player)
    {
        if (!GameServer.KeepManager.IsEnemy(this, player) || player.Client.Account.PrivLevel != 1)
        {
            int distance = DoorIndex == 1 ? 150 : 100; // Main vs side door
            
            Point2D keepPoint = IsObjectInFront(player, 180)
                ? GetPointFromHeading(Heading, -distance)
                : GetPointFromHeading(Heading, distance);
                
            player.MoveTo(CurrentRegionID, keepPoint.X, keepPoint.Y, Z, player.Heading);
        }
        return true;
    }
}
```

### Instance System Integration

#### Instance Door Handling
```csharp
public class InstanceDoorJumpPoint : IJumpPointHandler
{
    public bool IsAllowedToJump(DbZonePoint targetPoint, GamePlayer player)
    {
        if (player.CurrentRegion is BaseInstance instance)
        {
            return instance.OnInstanceDoor(player, targetPoint);
        }
        return true;
    }
}
```

#### Instance Zone Point Override
```csharp
public class BaseInstance : Region
{
    public virtual bool OnInstanceDoor(GamePlayer player, DbZonePoint zonePoint)
    {
        // Override zone point behavior in instances
        // Allows custom routing for quest-specific instances
        // Can redirect to different zones than normal
        return true; // Use default behavior
    }
    
    public override void OnCollapse()
    {
        // Instance cleanup on collapse
        m_zoneSkinMap.Clear();
        Areas.Clear();
        DOL.Events.GameEventMgr.RemoveAllHandlersForObject(this);
    }
}
```

### Teleportation System Integration

#### Teleport Areas
```csharp
public class TeleportArea : Area.Circle
{
    public override void OnPlayerEnter(GamePlayer player)
    {
        DbTeleport destination = WorldMgr.GetTeleportLocation(
            player.Realm, 
            String.Format("{0}:{1}", this.GetType(), this.Description));
            
        if (destination != null && CanTeleport(player))
            OnTeleport(player, destination);
    }
    
    protected void OnTeleport(GamePlayer player, DbTeleport destination)
    {
        if (!player.InCombat && !GameRelic.IsPlayerCarryingRelic(player))
        {
            player.LeaveHouse();
            GameLocation currentLocation = new GameLocation("TeleportStart", 
                player.CurrentRegionID, player.X, player.Y, player.Z);
            player.MoveTo((ushort)destination.RegionID, 
                destination.X, destination.Y, destination.Z, (ushort)destination.Heading);
            GameServer.ServerRules.OnPlayerTeleport(player, currentLocation, destination);
        }
    }
}
```

#### Bind Recall Restrictions
```csharp
public class GatewayPersonalBind : SpellHandler
{
    public override bool CheckBeginCast(GameLiving selectedTarget)
    {
        if (Caster is not GamePlayer player)
            return false;
            
        // Restriction checks
        if ((player.CurrentZone?.IsRvR == true || 
             player.CurrentRegion?.IsInstance == true) && 
             GameServer.Instance.Configuration.ServerType != EGameServerType.GST_PvE)
        {
            player.Out.SendMessage("You can't use that here!", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }
        
        if (player.IsMoving || player.InCombat || GameRelic.IsPlayerCarryingRelic(player))
            return false;
            
        return base.CheckBeginCast(selectedTarget);
    }
}
```

### Portal Stone System

#### Frontiers Portal Integration
```csharp
public class FrontiersPortalStone : GameStaticItem, IKeepItem
{
    public override bool Interact(GamePlayer player)
    {
        // Portal stone restrictions
        if (GameServer.KeepManager.FrontierRegionsList.Contains(player.CurrentRegionID))
        {
            if (player.Realm != this.Realm || 
                GameRelic.IsPlayerCarryingRelic(player) ||
                (Component?.Keep is GameKeep keep && (!keep.OwnsAllTowers || keep.InCombat)))
                return false;
                
            // Open warmap window
            eDialogCode code = player.Realm switch
            {
                eRealm.Albion => eDialogCode.WarmapWindowAlbion,
                eRealm.Midgard => eDialogCode.WarmapWindowMidgard,
                eRealm.Hibernia => eDialogCode.WarmapWindowHibernia,
                _ => eDialogCode.WarmapWindowAlbion
            };
            
            player.Out.SendDialogBox(code, 0, 0, 0, 0, eDialogType.Warmap, false, "");
        }
        
        // Border keep teleportation
        if (Component == null && !GameServer.KeepManager.FrontierRegionsList.Contains(player.CurrentRegionID))
        {
            GameServer.KeepManager.ExitBattleground(player);
        }
        
        return true;
    }
}
```

### RvR Warmap Integration

#### Keep Teleportation Validation
```csharp
public class WarmapShowRequestHandler : IPacketHandler
{
    // Teleport case (code = 2)
    if (client.Account.PrivLevel == (int)ePrivLevel.Player &&
        (client.Player.InCombat || 
         client.Player.CurrentRegionID != 163 || 
         GameRelic.IsPlayerCarryingRelic(client.Player)))
        return;
        
    AbstractGameKeep keep = GameServer.KeepManager.GetKeepByID(keepId);
    
    if (keep != null && client.Account.PrivLevel == (int)ePrivLevel.Player)
    {
        // Keep ownership and combat checks
        if (keep.Realm != client.Player.Realm)
            return;
            
        if (keep is GameKeep gameKeep && 
            (!gameKeep.OwnsAllTowers || keep.InCombat))
            return;
    }
    
    // Validate portal stone proximity
    bool foundPortalStone = client.Player.GetItemsInRadius(WorldMgr.INTERACT_DISTANCE)
        .OfType<FrontiersPortalStone>().Any();
}
```

### Death and Release Integration

#### Release Location Determination
```csharp
public class GamePlayer
{
    private void DetermineReleaseLocation()
    {
        int relX = 0, relY = 0, relZ = 0;
        ushort relRegion = 0, relHeading = 0;
        
        // Check for portal keeps in current region
        foreach (AbstractGameKeep keep in GameServer.KeepManager.GetKeepsOfRegion(CurrentRegionID))
        {
            if (keep.IsPortalKeep && keep.OriginalRealm == Realm)
            {
                relRegion = keep.CurrentRegion.ID;
                relX = keep.X;
                relY = keep.Y;
                relZ = keep.Z;
                break;
            }
        }
        
        // Fallback to border keeps
        if (relX == 0)
        {
            relRegion = CurrentRegion.ID;
            GameServer.KeepManager.GetBorderKeepLocation(
                ((byte)Realm * 2) / 1, out relX, out relY, out relZ, out relHeading);
        }
    }
}
```

## System Interactions

### With Region System
- **Region Validation**: Ensures valid transitions between regions
- **Instance Management**: Special handling for instanced content
- **Area Processing**: Coordinates area entry/exit events
- **Zone Boundaries**: Manages cross-zone movement

### With Keep System
- **Door Access**: Realm-based door interaction permissions
- **Teleportation**: Keep-to-keep portal stone travel
- **Combat Restrictions**: Prevents travel during keep combat
- **Tower Requirements**: Validates tower ownership for travel

### With Combat System
- **Combat Restrictions**: Prevents zone transitions during combat
- **Relic Carrying**: Blocks teleportation while carrying relics
- **PvP States**: Manages RvR zone entry/exit
- **Death Processing**: Coordinates release location selection

### With Quest System
- **Instance Routing**: Quest-specific zone point overrides
- **Area Triggers**: Quest progression through zone transitions
- **Custom Handlers**: Quest-specific teleportation logic
- **Validation**: Quest prerequisite checking for transitions

### With Guild System
- **Keep Access**: Guild-based keep entry permissions
- **Teleportation Rights**: Guild member keep travel privileges
- **Area Notifications**: Guild-related area events
- **Claim Validation**: Territory access based on guild claims

## Implementation Notes

### Handler Chain Pattern
```csharp
public interface IJumpPointHandler
{
    bool IsAllowedToJump(DbZonePoint targetPoint, GamePlayer player);
}

// Chain: Custom Handler -> Instance Handler -> Default Handler
```

### Zone Transition Validation
```csharp
public class ZoneTransitionValidator
{
    public bool ValidateTransition(GamePlayer player, DbZonePoint destination)
    {
        // Level restrictions
        // Realm access
        // Combat state
        // Special conditions (relic carrying, etc.)
        // Instance requirements
        return true;
    }
}
```

### Area Event Coordination
```csharp
public abstract class Area
{
    public virtual void OnPlayerEnter(GamePlayer player)
    {
        // Coordinate with multiple systems:
        // - Quest system (area triggers)
        // - Combat system (PvP flags)
        // - Social system (guild notifications)
        // - Housing system (house areas)
    }
}
```

## Performance Considerations

### Zone Caching
- **Area Lists**: Cache area lookups for zones
- **Door Registration**: Pre-register doors by zone
- **Handler Caching**: Cache custom jump point handlers
- **Teleport Validation**: Cache frequently used validation results

### Transition Optimization
- **Batch Processing**: Group area events during movement
- **Lazy Loading**: Load zone data only when needed
- **Memory Management**: Clean up unused instance data
- **Network Efficiency**: Minimize packets during transitions

## Test Scenarios

### Basic Transitions
1. **Zone Boundaries**: Test movement between adjacent zones
2. **Region Changes**: Validate cross-region teleportation
3. **Instance Entry**: Test instance creation and entry
4. **Door Interaction**: Validate door-based transitions

### Complex Interactions
1. **Keep Teleportation**: Test all keep travel restrictions
2. **Combat Transitions**: Validate combat-state restrictions
3. **Quest Instances**: Test quest-specific zone routing
4. **RvR Boundaries**: Test realm-based access restrictions

### Error Conditions
1. **Invalid Destinations**: Handle corrupted zone point data
2. **Permission Failures**: Test access denial scenarios
3. **Instance Failures**: Handle instance creation failures
4. **Network Issues**: Test transition failure recovery

## Security Considerations

### Access Control
- **Realm Restrictions**: Enforce cross-realm travel limitations
- **Level Gates**: Validate level requirements for zones
- **Combat States**: Prevent exploit through transition timing
- **Instance Security**: Validate instance access permissions

### Anti-Exploit Measures
- **Transition Cooldowns**: Prevent rapid transition spamming
- **State Validation**: Verify player state before transitions
- **Location Verification**: Validate destination coordinates
- **Handler Validation**: Secure custom handler execution

## Change Log
- Initial documentation based on zone transition and door systems
- Includes instance integration and keep teleportation mechanics
- Documents area transition coordination and handler patterns
- Covers security considerations and performance optimizations 