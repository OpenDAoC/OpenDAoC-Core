# Zone Bonus System

**Document Status:** Production Ready
**Implementation Status:** Live
**Verification:** Code Verified

## Overview

**Game Rule Summary**: The Zone Bonus System creates rotating hotspots across the game world to encourage players to explore different areas and balance server population. Every few hours, different zones get experience bonuses, and RvR realms get rotating bonuses for realm points and bounty points. When your PvE zone has a bonus, you'll earn extra experience there. When your realm has the RvR bonus, you'll get extra rewards for PvP activities. This system keeps the world feeling dynamic and gives you incentives to try new areas or participate in RvR during your realm's bonus periods.

The Zone Bonus System provides dynamic, rotating experience and reward bonuses across different game zones. It cycles between PvE zone bonuses (experience) and RvR realm bonuses (experience, RP, and BP) to encourage player activity in specific areas and maintain balanced population distribution.

## Core Architecture

### Bonus Types

#### PvE Zone Bonuses
```csharp
public static class ZoneBonusRotator
{
    // Current bonus zones per realm
    private static int currentAlbionZone;
    private static int currentAlbionZoneSI;
    private static int currentMidgardZone;
    private static int currentMidgardZoneSI;
    private static int currentHiberniaZone;
    private static int currentHiberniaZoneSI;
    
    // Zone database references
    private static DbZone albDBZone;
    private static DbZone albDBZoneSI;
    private static DbZone midDBZone;
    private static DbZone midDBZoneSI;
    private static DbZone hibDBZone;
    private static DbZone hibDBZoneSI;
}
```

#### RvR Realm Bonuses
```csharp
// Current RvR realm with active bonuses
private static int currentRvRRealm = 0;  // 0=None, 1=Albion, 2=Midgard, 3=Hibernia
```

### Bonus Properties
```csharp
public static int PvETimer { get; set; }              // PvE rotation timer
public static int RvRTimer { get; set; }              // RvR rotation timer
public static int PvEExperienceBonusAmount { get; set; }  // PvE XP bonus percentage
public static int RvRExperienceBonusAmount { get; set; }  // RvR XP bonus percentage
public static int RPBonusAmount { get; set; }         // Realm Point bonus percentage
public static int BPBonusAmount { get; set; }         // Bounty Point bonus percentage
```

## Rotation Mechanics

### Timer Configuration
```csharp
private static int RvRTickTime = 2700;    // 45 minutes (2700 seconds)
private static int PvETickTime = 7200;    // 2 hours (7200 seconds)
```

### PvE Zone Rotation
```csharp
// Rotates every 2 hours
public static void RotatePvEBonuses()
{
    // Clear previous bonuses
    ClearPvEZones();
    
    // Select new random zones for each realm
    currentAlbionZone = Util.Random(1, GetMaxZoneForRealm(eRealm.Albion));
    currentMidgardZone = Util.Random(1, GetMaxZoneForRealm(eRealm.Midgard));
    currentHiberniaZone = Util.Random(1, GetMaxZoneForRealm(eRealm.Hibernia));
    
    // Select SI expansion zones
    currentAlbionZoneSI = Util.Random(GetSIZoneRange(eRealm.Albion));
    currentMidgardZoneSI = Util.Random(GetSIZoneRange(eRealm.Midgard));
    currentHiberniaZoneSI = Util.Random(GetSIZoneRange(eRealm.Hibernia));
    
    // Apply bonuses to selected zones
    ApplyPvEBonuses();
    
    // Announce to players
    AnnouncePvERotation();
}
```

### RvR Realm Rotation
```csharp
// Rotates every 45 minutes
public static void RotateRvRBonuses()
{
    // Clear previous realm bonuses
    ClearRvRZones();
    
    // Rotate to next realm (1=Alb, 2=Mid, 3=Hib, 0=None)
    currentRvRRealm = (currentRvRRealm % 3) + 1;
    
    // Apply RvR bonuses
    ApplyRvRBonuses();
    
    // Announce to players
    AnnounceRvRRotation();
}
```

## Zone Selection Algorithm

### PvE Zone Eligibility
```csharp
// Zone selection criteria
public static bool IsValidPvEZone(DbZone zone)
{
    // Exclude capital cities
    if (zone.IsCapitalCity) return false;
    
    // Exclude RvR zones
    if (zone.IsRvR) return false;
    
    // Exclude housing zones
    if (zone.IsHousing) return false;
    
    // Exclude instance zones
    if (zone.IsInstance) return false;
    
    // Must have appropriate level range
    if (zone.MinLevel < 1 || zone.MaxLevel > 50) return false;
    
    return true;
}
```

### Realm-Specific Zone Ranges
```csharp
// Zone ID ranges per realm
public static class ZoneRanges
{
    // Classic zones
    public const int ALB_CLASSIC_MIN = 1;
    public const int ALB_CLASSIC_MAX = 40;
    public const int MID_CLASSIC_MIN = 100;
    public const int MID_CLASSIC_MAX = 140;
    public const int HIB_CLASSIC_MIN = 200;
    public const int HIB_CLASSIC_MAX = 240;
    
    // Shrouded Isles zones
    public const int ALB_SI_MIN = 51;
    public const int ALB_SI_MAX = 60;
    public const int MID_SI_MIN = 151;
    public const int MID_SI_MAX = 160;
    public const int HIB_SI_MIN = 251;
    public const int HIB_SI_MAX = 260;
}
```

## Bonus Application System

### Experience Bonus Application
```csharp
public static long GetExperienceBonus(GamePlayer player, long baseExperience)
{
    long bonusXP = 0;
    
    // Check for PvE zone bonus
    if (IsPlayerInBonusZone(player))
    {
        bonusXP += (baseExperience * PvEExperienceBonusAmount) / 100;
    }
    
    // Check for RvR realm bonus
    if (IsPlayerInRvRZone(player) && player.Realm == (eRealm)currentRvRRealm)
    {
        bonusXP += (baseExperience * RvRExperienceBonusAmount) / 100;
    }
    
    return bonusXP;
}
```

### Realm Point Bonus Application
```csharp
public static long GetRealmPointBonus(GamePlayer player, long baseRP)
{
    if (IsPlayerInRvRZone(player) && player.Realm == (eRealm)currentRvRRealm)
    {
        return (baseRP * RPBonusAmount) / 100;
    }
    
    return 0;
}
```

### Bounty Point Bonus Application
```csharp
public static long GetBountyPointBonus(GamePlayer player, long baseBP)
{
    if (IsPlayerInRvRZone(player) && player.Realm == (eRealm)currentRvRRealm)
    {
        return (baseBP * BPBonusAmount) / 100;
    }
    
    return 0;
}
```

## Player Notification System

### Login Notifications
```csharp
[GamePlayerEvent.GameEntered]
public static void PlayerEntered(DOLEvent e, object sender, EventArgs arguments)
{
    if (sender is GamePlayer player)
    {
        // Notify of current bonuses
        SendBonusNotification(player);
    }
}
```

### Zone Change Notifications
```csharp
public static void SendBonusNotification(GamePlayer player)
{
    // Check if player entered bonus zone
    if (IsPlayerInBonusZone(player))
    {
        player.Out.SendMessage(
            $"You are in a bonus experience zone! +{PvEExperienceBonusAmount}% XP bonus!",
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
    
    // Check if player is in RvR bonus realm
    if (IsPlayerInRvRZone(player) && player.Realm == (eRealm)currentRvRRealm)
    {
        player.Out.SendMessage(
            $"Your realm has RvR bonuses! +{RvRExperienceBonusAmount}% XP, +{RPBonusAmount}% RP, +{BPBonusAmount}% BP!",
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
}
```

### Server-Wide Announcements
```csharp
public static void AnnouncePvERotation()
{
    string message = "PvE bonus zones have rotated! Check your realm's new bonus zones.";
    
    foreach (GameClient client in WorldMgr.GetAllPlayingClients())
    {
        if (client.Player != null)
            client.Player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
}

public static void AnnounceRvRRotation()
{
    string realmName = GlobalConstants.RealmToName((eRealm)currentRvRRealm);
    string message = $"RvR bonuses have rotated to {realmName}!";
    
    foreach (GameClient client in WorldMgr.GetAllPlayingClients())
    {
        if (client.Player != null)
            client.Player.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
}
```

## Persistence and State Management

### Database Storage
```csharp
// Store current bonus state
public static void SaveBonusState()
{
    ServerProperties.Properties.CURRENT_PVE_ALBION_ZONE = currentAlbionZone;
    ServerProperties.Properties.CURRENT_PVE_MIDGARD_ZONE = currentMidgardZone;
    ServerProperties.Properties.CURRENT_PVE_HIBERNIA_ZONE = currentHiberniaZone;
    ServerProperties.Properties.CURRENT_RVR_REALM = currentRvRRealm;
    
    ServerProperties.Properties.LAST_PVE_ROTATION = DateTime.Now.Ticks;
    ServerProperties.Properties.LAST_RVR_ROTATION = DateTime.Now.Ticks;
}
```

### State Recovery
```csharp
public static void LoadBonusState()
{
    currentAlbionZone = ServerProperties.Properties.CURRENT_PVE_ALBION_ZONE;
    currentMidgardZone = ServerProperties.Properties.CURRENT_PVE_MIDGARD_ZONE;
    currentHiberniaZone = ServerProperties.Properties.CURRENT_PVE_HIBERNIA_ZONE;
    currentRvRRealm = ServerProperties.Properties.CURRENT_RVR_REALM;
    
    _lastPvEChangeTick = ServerProperties.Properties.LAST_PVE_ROTATION;
    _lastRvRChangeTick = ServerProperties.Properties.LAST_RVR_ROTATION;
    
    // Restore zone database references
    RestoreZoneReferences();
}
```

## Server Integration

### Server Startup
```csharp
[GameServerStartedEvent]
public static void OnServerStart(DOLEvent e, object sender, EventArgs arguments)
{
    Initialize();
    GameEventMgr.AddHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(PlayerEntered));
}
```

### Scheduler Integration
```csharp
private static SimpleScheduler scheduler = new SimpleScheduler();

public static void Initialize()
{
    LoadBonusState();
    
    // Schedule PvE rotations every 2 hours
    scheduler.Start(typeof(PvERotationTask), PvETickTime * 1000);
    
    // Schedule RvR rotations every 45 minutes
    scheduler.Start(typeof(RvRRotationTask), RvRTickTime * 1000);
}
```

## Zone Bonus Effects

### PvE Zone Benefits
- **Experience Bonus**: Configurable percentage bonus (typically 25-50%)
- **Zone Marking**: Visual indicators in client
- **Activity Encouragement**: Directs players to less populated areas

### RvR Realm Benefits
- **Experience Bonus**: PvP experience enhancement
- **Realm Point Bonus**: Additional RP for kills
- **Bounty Point Bonus**: Additional BP for objectives
- **Realm Pride**: Encourages realm loyalty

## Configuration Options

### Server Properties
```ini
# Zone bonus system configuration
ENABLE_ZONE_BONUSES = true
PVE_BONUS_AMOUNT = 25              # 25% experience bonus
RVR_BONUS_XP_AMOUNT = 30          # 30% RvR experience bonus
RVR_BONUS_RP_AMOUNT = 25          # 25% realm point bonus
RVR_BONUS_BP_AMOUNT = 25          # 25% bounty point bonus

# Rotation timers (seconds)
PVE_ROTATION_TIMER = 7200         # 2 hours
RVR_ROTATION_TIMER = 2700         # 45 minutes

# Zone selection
INCLUDE_SI_ZONES = true           # Include Shrouded Isles zones
MIN_ZONE_LEVEL = 1               # Minimum zone level for bonuses
MAX_ZONE_LEVEL = 50              # Maximum zone level for bonuses
```

### Dynamic Configuration
```csharp
// Bonus amounts can be adjusted at runtime
public static void SetBonusAmounts(int pveBonus, int rvRXPBonus, int rpBonus, int bpBonus)
{
    PvEExperienceBonusAmount = pveBonus;
    RvRExperienceBonusAmount = rvRXPBonus;
    RPBonusAmount = rpBonus;
    BPBonusAmount = bpBonus;
    
    // Notify all players of changes
    AnnounceConfigurationChange();
}
```

## Administrative Commands

### Manual Rotation
```csharp
[CommandHandler("rotatezonebonuses")]
public class RotateZoneBonusesCommand : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length > 1 && args[1].ToLower() == "pve")
        {
            ZoneBonusRotator.RotatePvEBonuses();
            DisplayMessage(client, "PvE zone bonuses rotated manually.");
        }
        else if (args.Length > 1 && args[1].ToLower() == "rvr")
        {
            ZoneBonusRotator.RotateRvRBonuses();
            DisplayMessage(client, "RvR realm bonuses rotated manually.");
        }
        else
        {
            ZoneBonusRotator.RotatePvEBonuses();
            ZoneBonusRotator.RotateRvRBonuses();
            DisplayMessage(client, "All zone bonuses rotated manually.");
        }
    }
}
```

### Status Checking
```csharp
[CommandHandler("zonebonusstatus")]
public class ZoneBonusStatusCommand : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        var status = ZoneBonusRotator.GetCurrentStatus();
        DisplayMessage(client, $"Current PvE Bonus Zones:");
        DisplayMessage(client, $"Albion: {status.AlbionZone} ({status.AlbionZoneSI})");
        DisplayMessage(client, $"Midgard: {status.MidgardZone} ({status.MidgardZoneSI})");
        DisplayMessage(client, $"Hibernia: {status.HiberniaZone} ({status.HiberniaZoneSI})");
        DisplayMessage(client, $"Current RvR Bonus Realm: {status.RvRRealm}");
    }
}
```

## Performance Considerations

### Efficient Zone Checking
```csharp
// Cache zone bonus status for performance
private static Dictionary<ushort, bool> zoneBonusCache = new Dictionary<ushort, bool>();

public static bool IsPlayerInBonusZone(GamePlayer player)
{
    ushort zoneId = player.CurrentZone.ID;
    
    // Check cache first
    if (zoneBonusCache.TryGetValue(zoneId, out bool isBonusZone))
        return isBonusZone;
    
    // Calculate and cache result
    bool result = CalculateZoneBonusStatus(zoneId, player.Realm);
    zoneBonusCache[zoneId] = result;
    
    return result;
}
```

### Cache Management
```csharp
// Clear cache when bonuses rotate
public static void ClearBonusCache()
{
    zoneBonusCache.Clear();
}
```

## Integration Points

### Experience System Integration
```csharp
// Integration with experience calculation
public override long CalculateExperienceGained(GameLiving target, long baseExperience)
{
    long totalXP = baseExperience;
    
    if (this is GamePlayer player)
    {
        // Add zone bonus if applicable
        totalXP += ZoneBonusRotator.GetExperienceBonus(player, baseExperience);
    }
    
    return totalXP;
}
```

### RvR System Integration
```csharp
// Integration with realm point calculation
public override void AwardRealmPoints(GamePlayer killer, GamePlayer victim, long baseRP)
{
    long totalRP = baseRP;
    
    // Add realm bonus if applicable
    totalRP += ZoneBonusRotator.GetRealmPointBonus(killer, baseRP);
    
    killer.GainRealmPoints(totalRP);
}
```

## Test Scenarios

### Rotation Testing
```csharp
[Test]
public void TestPvEZoneRotation()
{
    // Given: Current PvE bonus zones
    var initialState = ZoneBonusRotator.GetCurrentState();
    
    // When: PvE rotation occurs
    ZoneBonusRotator.RotatePvEBonuses();
    
    // Then: Zones should be different
    var newState = ZoneBonusRotator.GetCurrentState();
    Assert.AreNotEqual(initialState.AlbionZone, newState.AlbionZone);
}
```

### Bonus Application Testing
```csharp
[Test]
public void TestExperienceBonus()
{
    // Given: Player in bonus zone
    var player = CreateTestPlayer();
    PlacePlayerInBonusZone(player);
    
    // When: Experience is calculated
    long bonus = ZoneBonusRotator.GetExperienceBonus(player, 1000);
    
    // Then: Bonus should be applied
    Assert.AreEqual(250, bonus); // 25% of 1000
}
```

### Configuration Testing
```csharp
[Test]
public void TestBonusConfiguration()
{
    // Given: New bonus amounts
    ZoneBonusRotator.SetBonusAmounts(50, 40, 30, 20);
    
    // When: Bonus is calculated
    var player = CreateTestPlayer();
    long bonus = ZoneBonusRotator.GetExperienceBonus(player, 1000);
    
    // Then: New bonus amount should be applied
    Assert.AreEqual(500, bonus); // 50% of 1000
}
```

## Edge Cases and Error Handling

### Invalid Zone Handling
```csharp
public static bool IsValidZoneForBonus(DbZone zone)
{
    if (zone == null) return false;
    if (zone.IsInstance) return false;
    if (zone.IsCapitalCity) return false;
    if (zone.IsDungeon && zone.MaxLevel > 50) return false;
    
    return true;
}
```

### Server Restart Recovery
```csharp
// Ensure state persistence across restarts
public static void OnServerRestart()
{
    SaveBonusState();
    
    // Calculate time remaining in current rotation
    long timeSinceLastPvE = DateTime.Now.Ticks - _lastPvEChangeTick;
    long timeSinceLastRvR = DateTime.Now.Ticks - _lastRvRChangeTick;
    
    // Adjust next rotation timing
    AdjustRotationTimers(timeSinceLastPvE, timeSinceLastRvR);
}
```

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Production documentation |
| Historical | Game | RvR realm bonus system |
| Historical | Game | PvE zone rotation |
| Original | Game | Basic zone bonus framework | 