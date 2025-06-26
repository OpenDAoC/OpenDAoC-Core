# Instance System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The instance system creates dynamic, isolated copies of existing regions for specific groups of players. Instances share the same visual appearance (skin) as regular regions but exist as separate entities, allowing multiple groups to adventure in the same content without interference.

## Core Mechanics

### Instance Types

#### Base Instance Class
```csharp
public class BaseInstance : Region
{
    private ushort m_regionID;      // Unique instance ID
    private ushort m_skinID;        // Visual region to display
    
    public override bool IsInstance => true;
}
```

#### Specialized Instances
- **BaseInstance**: Generic instance implementation
- **TaskDungeonInstance**: Task-specific dungeons
- **AdventureWingInstance**: Personal/group adventure areas
- **RegionInstance**: Extended instance with ownership

### Instance Creation

#### Factory Method
```csharp
public static BaseInstance CreateInstance(ushort skinID, Type instanceType)
{
    return CreateInstance(0, skinID, instanceType);
}

public static BaseInstance CreateInstance(ushort requestedID, ushort skinID, Type instanceType)
{
    // Validate instance type
    if (!instanceType.IsSubclassOf(typeof(BaseInstance)) && instanceType != typeof(BaseInstance))
        return null;
        
    // Get region data for skin
    RegionData data = m_regionData[skinID];
    
    // Find available ID
    ushort ID = requestedID;
    if (requestedID == 0)
    {
        for (ID = DEFAULT_VALUE_FOR_INSTANCE_ID_SEARCH_START; ID <= ushort.MaxValue; ID++)
        {
            if (!m_regions.ContainsKey(ID))
                break;
        }
    }
    
    // Create instance
    instance = (BaseInstance) info.Invoke(new object[] { ID, data });
    m_regions[ID] = instance;
    
    // Create zones
    CreateInstanceZones(instance, data);
    
    // Start instance
    instance.Start();
    return instance;
}
```

#### Instance ID Range
```csharp
public const int DEFAULT_VALUE_FOR_INSTANCE_ID_SEARCH_START = 1000;
```
- Instance IDs start at 1000 to avoid conflicts
- Automatically finds next available ID
- Can request specific ID if needed

### Zone Management

#### Zone Creation
```csharp
// Create zones for instance
foreach (ZoneData dat in list)
{
    for (; zoneID <= ushort.MaxValue; zoneID++)
    {
        if (m_zones.TryAdd(zoneID, null))
        {
            RegisterZone(dat, zoneID, ID, 
                string.Format("{0} (Instance)", dat.Description), 
                0, 0, 0, 0, 0);
            break;
        }
    }
}
```

#### Zone Skin System
```csharp
public class Zone
{
    public ushort ID { get; }           // Actual zone ID
    public ushort ZoneSkinID { get; }   // Client-side zone ID
}
```
- ZoneSkinID maintains client positioning
- Allows multiple instances of same visual zone
- Preserves minimap and client features

### Instance Lifecycle

#### Instance Startup
```csharp
public virtual void Start()
{
    StartRegionMgr();
    BeginAutoClosureCountdown(10);  // 10 minute default
    
    // Map zone skins
    foreach (Zone z in m_zones)
    {
        m_zoneSkinMap.Add(z.ZoneSkinID, z);
    }
}
```

#### Auto-Closure System
```csharp
protected int m_autoCloseMinutes = 10;
protected RegionTimer m_autoCloseRegionTimer;

protected virtual void BeginAutoClosureCountdown(int minutes)
{
    if (m_autoCloseRegionTimer != null)
        return;
        
    m_autoCloseMinutes = minutes;
    m_autoCloseRegionTimer = new RegionTimer(this);
    m_autoCloseRegionTimer.Callback = new RegionTimerCallback(AutoCloseTimerCallback);
    m_autoCloseRegionTimer.Start(60000); // Check every minute
}
```

#### Instance Collapse
```csharp
public override void OnCollapse()
{
    base.OnCollapse();
    
    // Stop timers
    if (m_autoCloseRegionTimer != null)
    {
        m_autoCloseRegionTimer.Stop();
        m_autoCloseRegionTimer = null;
    }
    
    // Clean up
    DOL.Events.GameEventMgr.RemoveAllHandlersForObject(this);
    m_zoneSkinMap.Clear();
    Areas.Clear();
}
```

### Door/Zone Point Handling

#### Custom Zone Points
```csharp
public virtual bool OnInstanceDoor(GamePlayer player, DbZonePoint zonePoint)
{
    // Override zone point behavior in instances
    // Return true to use default behavior
    // Return false to handle custom routing
    return true;
}
```

#### Instance Jump Points
```csharp
// Special handler for instance doors
if (client.Player.CurrentRegion.IsInstance)
{
    string typeName = "DOL.GS.ServerRules.InstanceDoorJumpPoint";
    Type type = ScriptMgr.GetType(typeName);
    customHandler = (IJumpPointHandler) Activator.CreateInstance(type);
}
```

## Instance Types Implementation

### Task Dungeon Instance

#### Creation
```csharp
public class TaskDungeonInstance : BaseInstance
{
    public AbstractMission Mission { get; set; }
    
    // Create task instance
    TaskDungeonInstance instance = (TaskDungeonInstance)
        WorldMgr.CreateInstance(rid, typeof(TaskDungeonInstance));
    instance.Mission = this;
    
    // Load from database
    string keyname = "TaskDungeon" + rid + ".1";
    instance.LoadFromDatabase(keyname);
}
```

#### Level-Based Selection
```csharp
private static ushort GetRegionFromLevel(int level, eRealm realm, eDungeonType type)
{
    if (level <= 10)
    {
        switch (realm)
        {
            case eRealm.Albion:
                return type == eDungeonType.Ranged ? 
                    GetRandomRegion(alb_harbor_long) : 
                    GetRandomRegion(alb_harbor_laby);
            // etc...
        }
    }
    // More level ranges...
}
```

### Adventure Wing Instance

#### Ownership System
```csharp
public class AdventureWingInstance : RegionInstance
{
    private Group m_group;
    
    public Group Group
    {
        get { return m_group; }
        set { m_group = value; }
    }
    
    // Auto-destroy when empty or objectives complete
    protected override void CheckAutoClose()
    {
        if (NumPlayers == 0 || AllObjectivesComplete())
            BeginAutoClosureCountdown(5);
    }
}
```

### Region Instance Base

#### Extended Features
```csharp
public class RegionInstance : BaseInstance
{
    public GameObject Owner { get; set; }
    public DateTime CreationTime { get; set; }
    public bool AllowAdd { get; set; }
    
    // Access control
    public virtual bool CanEnter(GamePlayer player)
    {
        if (Owner is GamePlayer && Owner == player)
            return true;
        if (Owner is Group && ((Group)Owner).IsInTheGroup(player))
            return true;
        return false;
    }
}
```

## System Interactions

### Player Movement
```csharp
// Check for instance on login
if (WorldMgr.Regions[player.CurrentRegionID] == null || 
    player.CurrentRegion == null || 
    player.CurrentRegion.IsInstance)
{
    Log.WarnFormat($"{player.Name} logging into instance, moving to bind!");
    player.MoveToBind();
}
```

### Zone Transitions
```csharp
// Allow region to handle zone point
if (client.Player.CurrentRegion.OnZonePoint(client.Player, zonePoint) == false)
    return;
```

### Client Synchronization
```csharp
// Send zone skin ID to client
pak.WriteByte((byte) playerZone.ZoneSkinID);
```

## Configuration

### Instance Properties
```csharp
// Auto-close settings
protected int m_autoCloseMinutes = 10;      // Default 10 minutes
protected int m_delayCloseMinutes = 1;      // Delay before closing

// Player limits
public virtual int MaxPlayers => 50;        // Override per instance type
```

### Database Configuration
```csharp
// Instance elements stored with keyname
string keyname = "TaskDungeon" + regionID + "." + variation;
instance.LoadFromDatabase(keyname);
```

## Edge Cases

### Instance Full
```csharp
if (NumPlayers >= MaxPlayers)
{
    player.Out.SendMessage("Instance is full!", 
        eChatType.CT_System, eChatLoc.CL_SystemWindow);
    return false;
}
```

### Skin ID Conflicts
```csharp
// Validate skin exists
if (m_regionData[skinID] == null)
{
    log.Error("Data for region " + skinID + " not found!");
    return null;
}
```

### Zone Creation Failure
```csharp
if (list == null)
{
    log.Warn("No zones found for skinID " + skinID);
    return null;
}
```

## Test Scenarios

1. **Basic Creation**
   - Create instance with valid skin
   - Verify unique ID assignment
   - Check zone creation
   - Confirm client display

2. **Access Control**
   - Test solo player access
   - Verify group access
   - Check non-member rejection
   - Test ownership transfer

3. **Auto-Closure**
   - Empty instance timeout
   - Player re-entry prevention
   - Timer cancellation
   - Proper cleanup

4. **Zone Points**
   - Custom routing in instances
   - Exit to proper location
   - Multi-exit handling
   - Cross-instance travel

## Implementation Notes

### Thread Safety
- Instance creation synchronized
- Zone registration thread-safe
- Player count tracking atomic
- Timer management protected

### Performance
- Instances share region data
- Minimal memory overhead
- Efficient zone mapping
- Cleanup prevents leaks

### Networking
- Client sees skin region
- Server tracks real region
- Seamless transition
- Position sync maintained

## TODO
- Document instance variations
- Add performance metrics
- Detail boss spawn systems
- Explain loot instance rules
- Add PvP instance support 