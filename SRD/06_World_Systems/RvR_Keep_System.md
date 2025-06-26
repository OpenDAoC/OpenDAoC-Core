# RvR Keep System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The Realm vs Realm (RvR) Keep System manages the conquest and defense of strategic fortifications in frontier zones. Keeps provide teleportation access, realm bonuses, and territorial control, forming the backbone of large-scale PvP warfare in DAoC.

## Core Architecture

### Keep Types

#### AbstractGameKeep
```csharp
public abstract class AbstractGameKeep : IGameKeep
{
    public ushort KeepID { get; set; }        // Unique keep identifier
    public byte Level { get; set; }          // Keep level (0-10)
    public eRealm Realm { get; set; }        // Current owner realm
    public Guild Guild { get; set; }         // Claiming guild
    public bool InCombat { get; set; }       // Under attack status
    
    // Combat tracking
    public long StartCombatTick { get; set; }
    public long LastAttackedByEnemyTick { get; set; }
    
    // Structural components
    public Dictionary<int, GameKeepComponent> Components { get; set; }
    public Dictionary<string, GameKeepDoor> Doors { get; set; }
    public List<GameKeepGuard> Guards { get; set; }
}
```

#### Keep Specializations
- **GameKeep**: Major fortress with multiple towers
- **GameKeepTower**: Smaller outpost structure
- **Relic Keeps**: Special keeps housing realm relics

### Keep Components

#### Component System
```csharp
public class GameKeepComponent
{
    public int ComponentID { get; set; }        // Database ID
    public byte Height { get; set; }            // Wall/tower height
    public AbstractGameKeep Keep { get; set; }  // Parent keep
    public eComponentSkin Skin { get; set; }    // Visual appearance
    
    // Hookpoints for siege equipment
    public Dictionary<int, GameKeepHookPoint> HookPoints { get; set; }
    
    // Component health and status
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public bool IsRaized { get; set; }
}
```

#### Component Types
| Component | Purpose | Features |
|-----------|---------|----------|
| **Wall** | Defensive barrier | Height levels, hookpoints |
| **Tower** | Elevated position | Guard spawns, LoS advantage |
| **Gate** | Entry control | Doors, repair points |
| **Keep** | Central structure | Lord spawn, claiming |

## Realm Control

### Keep Ownership
```csharp
public virtual int RealmPointsValue()
{
    return GameServer.ServerRules.GetRealmPointsForKeep(this);
}

public virtual int BountyPointsValue()
{
    return GameServer.ServerRules.GetBountyPointsForKeep(this);
}

// Keep capture rewards
public virtual long ExperiencePointsValue()
{
    return GameServer.ServerRules.GetExperienceForKeep(this);
}
```

### Claiming System
```csharp
public class KeepClaiming
{
    // Guild requirements
    public static bool CanClaimKeep(Guild guild, AbstractGameKeep keep)
    {
        // Must be same realm
        if (guild.Realm != keep.Realm)
            return false;
            
        // Guild leader or officers only
        if (!guild.HasPermission(player, GuildPermission.Claim))
            return false;
            
        // Keep must not be in combat
        if (keep.InCombat)
            return false;
            
        return true;
    }
    
    // Claiming process
    private readonly int CLAIM_CALLBACK_INTERVAL = 60 * 60 * 1000; // 1 hour
    protected ECSGameTimer m_claimTimer;
    
    public void ClaimKeep(Guild guild)
    {
        Guild = guild;
        StartClaimTimer();
        
        // Broadcast claim message
        PlayerMgr.BroadcastClaim(this);
    }
}
```

### Keep Bonuses

#### Realm Bonuses
```csharp
public virtual int GetRealmKeepBonusLevel(eRealm realm)
{
    int keepCount = GetKeepCountByRealm(realm);
    int bonusLevel = (7 - keepCount) * ServerProperties.Properties.KEEP_BALANCE_MULTIPLIER;
    return bonusLevel;
}

public virtual int GetRealmTowerBonusLevel(eRealm realm)
{
    int towerCount = GetTowerCountByRealm(realm);
    int bonusLevel = (28 - towerCount) * ServerProperties.Properties.TOWER_BALANCE_MULTIPLIER;
    return bonusLevel;
}
```

#### Population Balance
```csharp
// Underpopulated realms get bonuses
public static int GetUnderdogBonus(eRealm realm)
{
    Dictionary<eRealm, int> keepCounts = GetKeepCountsByRealm();
    
    int realmKeeps = keepCounts[realm];
    int totalKeeps = keepCounts.Values.Sum();
    
    if (realmKeeps < totalKeeps / 4) // Less than 25%
        return 100; // 100% bonus
    else if (realmKeeps < totalKeeps / 3) // Less than 33%
        return 50;  // 50% bonus
        
    return 0; // No bonus
}
```

## Keep Warfare

### Combat States
```csharp
public enum eKeepCombatState
{
    Peace,      // No recent combat
    Skirmish,   // Light fighting
    Siege,      // Under sustained attack
    Captured    // Recently taken
}

public bool InCombat
{
    get 
    { 
        return GameLoop.GameLoopTime - LastAttackedByEnemyTick < 60000; // 1 minute
    }
}
```

### Siege Mechanics
```csharp
public void OnAttacked(GameObject attacker)
{
    LastAttackedByEnemyTick = GameLoop.GameLoopTime;
    
    // Start combat if not already in combat
    if (!InCombat)
    {
        StartCombatTick = GameLoop.GameLoopTime;
        BroadcastCombatStart();
    }
    
    // Check for door state
    bool underSiege = false;
    foreach (GameKeepDoor door in Doors.Values)
    {
        if (door.State == eDoorState.Open)
        {
            underSiege = true;
            break;
        }
    }
    
    if (underSiege && StartCombatTick == 0)
        StartCombatTick = GameLoop.GameLoopTime;
}
```

### Lord System
```csharp
public class GuardLord : GameKeepGuard
{
    public override int RealmPointsValue
    {
        get
        {
            // Must survive for minimum time
            long duration = (GameLoop.GameLoopTime - m_lastSpawnTime) / 1000L;
            if (duration < Properties.LORD_RP_WORTH_SECONDS)
                return 0;
                
            return Component?.Keep?.RealmPointsValue() ?? 5000;
        }
    }
    
    public override int MaxHealth => base.MaxHealth * 3; // 3x guard health
    
    public override double GetArmorAbsorb(eArmorSlot slot)
    {
        return base.GetArmorAbsorb(slot) + 0.05; // +5% absorption
    }
}
```

## Teleportation System

### Portal Network
```csharp
public class FrontiersPortalStone
{
    public bool CanTeleportTo(GamePlayer player, AbstractGameKeep keep)
    {
        // Must be same realm
        if (player.Realm != keep.Realm)
            return false;
            
        // Keep must not be in combat
        if (keep.InCombat)
            return false;
            
        // Keep must own all towers (for main keeps)
        if (keep is GameKeep mainKeep && !mainKeep.OwnsAllTowers)
            return false;
            
        // Player must be in frontiers
        if (!KeepManager.FrontierRegionsList.Contains(player.CurrentRegionID))
            return false;
            
        return true;
    }
}
```

### Keep Teleportation
```csharp
public bool OwnsAllTowers
{
    get
    {
        foreach (GameKeepTower tower in ConnectedTowers)
        {
            if (tower.Realm != Realm)
                return false;
        }
        return true;
    }
}
```

## Guard Management

### Guard Types
```csharp
public enum eGuardType
{
    Archer,     // Ranged damage
    Caster,     // Spell damage
    Fighter,    // Melee damage
    Healer,     // Support spells
    Lord,       // Keep leader
    Commander,  // Enhanced fighter
    Hastener    // Speed buffs
}
```

### Guard Spawning
```csharp
public class KeepGuardMgr
{
    public static void SpawnGuards(AbstractGameKeep keep)
    {
        // Lords always spawn
        if (!keep.HasLord)
            SpawnLord(keep);
            
        // Guards based on keep level
        int guardCount = Math.Min(keep.Level * 2, 20);
        
        for (int i = 0; i < guardCount; i++)
        {
            SpawnGuard(keep, GetRandomGuardType());
        }
    }
    
    public virtual int LordRespawnTime
    {
        get
        {
            if (Realm == eRealm.None) // PvE lords
            {
                int variance = Math.Abs(Properties.GUARD_RESPAWN_VARIANCE) * 1000;
                int respawn = Math.Abs(Properties.GUARD_RESPAWN) * 60 * 1000;
                return Math.Max(1000, respawn + Util.Random(-variance, variance));
            }
            return 1000; // RvR lords respawn quickly
        }
    }
}
```

## Darkness Falls Access

### DF Control System
```csharp
public static class DFEnterJumpPoint
{
    public static eRealm DarknessFallOwner { get; private set; }
    public static eRealm PreviousOwner { get; private set; }
    public static long LastRealmSwapTick { get; private set; }
    public static long GracePeriod = 5 * 60 * 1000; // 5 minutes
    
    public static void OnKeepTaken(DOLEvent e, object sender, EventArgs arguments)
    {
        KeepEventArgs args = arguments as KeepEventArgs;
        eRealm newRealm = (eRealm)args.Keep.Realm;
        
        if (newRealm != DarknessFallOwner)
        {
            int currentOwnerKeeps = KeepManager.GetKeepCountByRealm(DarknessFallOwner);
            int challengerKeeps = KeepManager.GetKeepCountByRealm(newRealm);
            
            if (challengerKeeps > currentOwnerKeeps)
            {
                PreviousOwner = DarknessFallOwner;
                LastRealmSwapTick = GameLoop.GameLoopTime;
                DarknessFallOwner = newRealm;
                
                BroadcastDFOwnerChange(newRealm);
            }
        }
    }
}
```

### DF Access Rules
```csharp
public bool CanAccessDarknessFalls(GamePlayer player)
{
    // All realms access if configured
    if (Properties.ALLOW_ALL_REALMS_DF)
        return true;
        
    // Current owner always has access
    if (player.Realm == DFEnterJumpPoint.DarknessFallOwner)
        return true;
        
    // Grace period for previous owner
    if (player.Realm == DFEnterJumpPoint.PreviousOwner)
    {
        long timeSinceSwap = GameLoop.GameLoopTime - DFEnterJumpPoint.LastRealmSwapTick;
        return timeSinceSwap < DFEnterJumpPoint.GracePeriod;
    }
    
    return false;
}
```

## War Map System

### Keep Status Display
```csharp
public void SendWarmapUpdate(ICollection<IGameKeep> keeps)
{
    foreach (AbstractGameKeep keep in keeps)
    {
        byte flags = (byte)keep.Realm; // Base realm flags
        
        // Guild claimed flag
        if (keep.Guild != null)
            flags |= (byte)eRealmWarmapKeepFlags.Claimed;
            
        // Teleportable flag
        if (CanTeleportTo(player, keep))
            flags |= (byte)eRealmWarmapKeepFlags.Teleportable;
            
        // Under siege flag
        if (keep.InCombat)
            flags |= (byte)eRealmWarmapKeepFlags.UnderSiege;
            
        // Write keep data
        pak.WriteByte(CalculateMapPosition(keep));
        pak.WriteByte(flags);
        pak.WritePascalString(keep.Guild?.Name ?? "");
    }
}
```

### Relic Status
```csharp
public void SendWarmapBonuses()
{
    // Keep counts per realm
    int AlbKeeps = KeepManager.GetKeepCountByRealm(eRealm.Albion);
    int MidKeeps = KeepManager.GetKeepCountByRealm(eRealm.Midgard);
    int HibKeeps = KeepManager.GetKeepCountByRealm(eRealm.Hibernia);
    
    // Relic ownership
    int magic = RelicMgr.GetRelicCount(player.Realm, eRelicType.Magic);
    int strength = RelicMgr.GetRelicCount(player.Realm, eRelicType.Strength);
    byte relics = (byte)(magic << 4 | strength);
    
    pak.WriteByte((byte)GetPlayerRealmKeepCount(player));
    pak.WriteByte(relics);
    pak.WriteByte((byte)DFEnterJumpPoint.DarknessFallOwner);
}
```

## Keep Upgrading

### Level System
```csharp
public virtual int Height
{
    get
    {
        return GameServer.KeepManager.GetHeightFromLevel(this.Level);
    }
}

public virtual byte GetHeightFromLevel(byte level)
{
    if (level > 15) return 5;
    if (level > 10) return 4;
    if (level > 7)  return 3;
    if (level > 4)  return 2;
    if (level > 1)  return 1;
    return 0;
}
```

### Upgrade Mechanics
```csharp
protected ECSGameTimer m_changeLevelTimer;

public void StartLevelChangeTimer()
{
    if (m_changeLevelTimer != null)
        m_changeLevelTimer.Stop();
        
    int upgradeTime = CalculateUpgradeTime();
    m_changeLevelTimer = new ECSGameTimer(this, UpgradeKeep, upgradeTime);
    
    KeepGuildMgr.SendChangeLevelTimeMessage(this);
}

private int UpgradeKeep(ECSGameTimer timer)
{
    if (Level < ServerProperties.Properties.MAX_KEEP_LEVEL)
    {
        Level++;
        KeepGuildMgr.SendLevelChangeMessage(this);
        
        // Continue upgrading if not at max
        if (Level < ServerProperties.Properties.MAX_KEEP_LEVEL)
            StartLevelChangeTimer();
    }
    
    return 0; // Stop timer
}
```

## System Integration

### With Guild System
```csharp
public class KeepGuildMgr
{
    public static void SendDoorDestroyedMessage(GameKeepDoor door)
    {
        string message = $"{door.Name} in your area {door.Component.Keep.Name} has been destroyed";
        SendMessageToGuild(message, door.Component.Keep.Guild);
    }
    
    public static void SendMessageToGuild(string message, Guild guild)
    {
        if (guild == null) return;
        
        message = "[Guild] [" + message + "]";
        guild.SendMessageToGuildMembers(message, eChatType.CT_Guild, eChatLoc.CL_ChatWindow);
    }
}
```

### With Combat System
- Keep guards use enhanced AI brains
- Lords have increased health and armor
- Siege weapons required for door destruction

### With Quest System
- Daily RvR quests involve keep captures
- Realm tasks for coordinated attacks
- Personal achievements for keep participation

## Configuration

### Server Properties
```xml
<Property Name="MAX_KEEP_LEVEL" Value="10" />
<Property Name="STARTING_KEEP_LEVEL" Value="4" />
<Property Name="KEEP_BALANCE_MULTIPLIER" Value="2" />
<Property Name="TOWER_BALANCE_MULTIPLIER" Value="1" />
<Property Name="KEEP_RP_BASE" Value="1000" />
<Property Name="KEEP_RP_MULTIPLIER" Value="100" />
<Property Name="TOWER_RP_BASE" Value="500" />
<Property Name="TOWER_RP_CLAIM_MULTIPLIER" Value="50" />
<Property Name="UPGRADE_MULTIPLIER" Value="200" />
<Property Name="LORD_RP_WORTH_SECONDS" Value="300" />
<Property Name="ALLOW_ALL_REALMS_DF" Value="false" />
```

### Keep Database
```csharp
public class DbKeep
{
    public int KeepID { get; set; }
    public byte Level { get; set; }
    public string Name { get; set; }
    public eRealm OriginalRealm { get; set; }
    public int Region { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public ushort Heading { get; set; }
}
```

## Performance Optimization

### Keep Loading
- Keeps loaded once at server startup
- Components cached in memory
- Guard spawning on-demand

### Combat Performance
- InCombat calculated dynamically
- Combat state cached for 1 minute
- LoS checks optimized for walls

### War Map Efficiency
- Keep status cached between updates
- Batch updates every 30 seconds
- Differential updates for changes only

## Test Scenarios

### Basic Keep Functionality
- Keep claiming by guild officers
- Teleportation access control
- Combat state transitions
- Guard respawn mechanics

### RvR Combat
- Multi-realm keep battles
- Siege weapon deployment
- Lord kill progression
- Realm bonus calculations

### Darkness Falls
- DF ownership changes
- Grace period mechanics
- Access restrictions
- Cross-realm notifications

### Performance Testing
- Large scale battles (100+ players)
- Multiple simultaneous keep attacks
- War map update frequency
- Memory usage during combat

## Implementation Notes

### Thread Safety
- Keep state changes are atomic
- Combat tracking uses game loop time
- Guard lists protected by locks

### Network Protocol
- War map packets optimized for size
- Keep info sent on demand
- Combat status broadcast to realm

### Database Integration
- Keep ownership persisted
- Guild claims saved
- Component states stored 