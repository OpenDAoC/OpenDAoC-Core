# Guild Banner & Heraldry System

**Document Status:** Production Ready  
**Implementation Status:** Live  
**Verification:** Code Verified

## Overview

**Game Rule Summary**: Guilds can design custom emblems displayed on cloaks and shields, and high-level guilds can purchase powerful guild banners that provide combat bonuses to group members in RvR zones. These banners make your group a high-priority target but offer significant tactical advantages - if the banner carrier dies, enemies can capture it as a trophy while your guild faces a cooldown before purchasing a new one.

The Guild Banner & Heraldry System manages visual guild representation through emblems and banners. Guilds can design custom emblems displayed on equipment and summon powerful guild banners in RvR combat that provide group benefits and serve as tactical objectives.

## Core Architecture

### Guild Emblem System

#### Emblem Structure
```csharp
// Emblem encoded as integer with bitwise components
int emblem = ((logo << 9) | (pattern << 7) | (primarycolor << 3) | secondarycolor);

// Component ranges:
// - Logo: 8 bits (0-255)
// - Pattern: 2 bits (0-3) 
// - Primary Color: 4 bits (0-15)
// - Secondary Color: 3 bits (0-7)
```

#### Emblem Creation Process
```csharp
public void HandlePacket(GameClient client, GSPacketIn packet)
{
    if (client.Player.Guild == null) return;
    if (!client.Player.Guild.HasRank(client.Player, Guild.eRank.Leader)) return;
    
    int primarycolor = packet.ReadByte() & 0x0F;   // 4 bits
    int secondarycolor = packet.ReadByte() & 0x07; // 3 bits  
    int pattern = packet.ReadByte() & 0x03;        // 2 bits
    int logo = packet.ReadByte();                  // 8 bits
    
    int oldemblem = client.Player.Guild.Emblem;
    int newemblem = ((logo << 9) | (pattern << 7) | (primarycolor << 3) | secondarycolor);
    GuildMgr.ChangeGuildEmblem(client.Player, oldemblem, newemblem);
}
```

### Guild Banner System

#### Banner Requirements
- **Guild Level**: Minimum level 7 required
- **Cost**: Guild level × 100 bounty points
- **Group Requirement**: Must be in group to summon
- **RvR Zones Only**: Cannot summon outside RvR areas
- **Cooldown**: Time restriction after banner loss

#### Banner Acquisition
```csharp
// Banner purchase validation
if (client.Player.Guild.GuildLevel < 7)
{
    // Guild level requirement not met
    return;
}

long bannerPrice = (client.Player.Guild.GuildLevel * 100);

if (client.Player.Guild.BountyPoints > bannerPrice)
{
    client.Out.SendCustomDialog("Are you sure you buy a guild banner for " + 
        bannerPrice + " guild bounty points?", ConfirmBannerBuy);
}
```

#### Banner Loss Mechanics
```csharp
// Cooldown after banner loss
TimeSpan lostTime = DateTime.Now.Subtract(client.Player.Guild.GuildBannerLostTime);

if (lostTime.TotalMinutes < Properties.GUILD_BANNER_LOST_TIME)
{
    int hoursLeft = (int)((Properties.GUILD_BANNER_LOST_TIME - lostTime.TotalMinutes + 30) / 60);
    // Display waiting time to player
}
```

## Banner Summoning System

### Summoning Process
```csharp
public void Start()
{
    if (m_player.Group != null)
    {
        bool groupHasBanner = false;
        
        // Check if group already has banner
        foreach (GamePlayer groupPlayer in m_player.Group.GetPlayersInTheGroup())
        {
            if (groupPlayer.GuildBanner != null)
            {
                groupHasBanner = true;
                break;
            }
        }
        
        if (!groupHasBanner)
        {
            // Create banner item
            GuildBannerItem item = new GuildBannerItem(GuildBannerTemplate);
            item.OwnerGuild = m_player.Guild;
            item.SummonPlayer = m_player;
            
            m_player.GuildBanner = this;
            m_player.Stealth(false); // Break stealth
        }
    }
}
```

### Banner Restrictions
- **One Per Group**: Only one banner per group allowed
- **One Per Guild**: Only one active banner per guild
- **Stealth Breaking**: Summoning breaks stealth
- **Combat State**: Cannot unsummon while in combat

### Banner Visual Representation
```csharp
public DbItemTemplate GuildBannerTemplate
{
    get
    {
        m_guildBannerTemplate = new DbItemTemplate();
        m_guildBannerTemplate.Emblem = m_player.Guild.Emblem;
        
        // Realm-specific models
        switch (m_player.Realm)
        {
            case eRealm.Albion:
                m_guildBannerTemplate.Model = 3223;
                break;
            case eRealm.Midgard:
                m_guildBannerTemplate.Model = 3224;
                break;
            case eRealm.Hibernia:
                m_guildBannerTemplate.Model = 3225;
                break;
        }
        
        m_guildBannerTemplate.Name = m_player.Guild.Name + "'s Banner";
        return m_guildBannerTemplate;
    }
}
```

## Banner Effects System

### Banner Effect Types

#### Banner of Warding
- **Effect**: 10% bonus to all magic resistances
- **Stacking**: Stacks with other effects
- **Target**: Group members in range

#### Banner of Freedom
- **Effect**: Movement speed enhancement
- **Classes**: Combat-focused classes
- **Duration**: 9 seconds, pulses every 9 seconds

#### Banner of Besieging
- **Effect**: Siege combat bonuses
- **Classes**: Support/caster classes
- **Application**: Realm warfare scenarios

### Effect Application
```csharp
public static GuildBannerEffect CreateEffectOfClass(GamePlayer player, double effectiveness)
{
    switch ((eCharacterClass)player.CharacterClass.ID)
    {
        case eCharacterClass.Armsman:
        case eCharacterClass.Mercenary:
        case eCharacterClass.Paladin:
        // ... other combat classes
            return new BannerOfWardingEffect(effectiveness);
            
        case eCharacterClass.Cleric:
        case eCharacterClass.Heretic:
        // ... other support classes
            return new BannerOfBesiegingEffect(effectiveness);
            
        // Class-specific banner effects
    }
}
```

### Pulse Mechanics
```csharp
protected const int duration = 9000; // 9 second duration
// Pulsing every 9 seconds with 9 second duration
```

## Banner Lifecycle Management

### Banner Dropping
```csharp
protected void PlayerDied(DOLEvent e, object sender, EventArgs args)
{
    DyingEventArgs arg = args as DyingEventArgs;
    GameObject killer = arg.Killer;
    
    // Create world item when banner carrier dies
    gameItem = new WorldInventoryItem(m_item);
    Point2D point = m_player.GetPointFromHeading(m_player.Heading, 30);
    gameItem.X = point.X;
    gameItem.Y = point.Y;
    gameItem.Z = m_player.Z;
    
    // Set pickup permissions
    gameItem.AddOwner(m_player); // Original guild can recover
    
    if (playerKiller != null)
    {
        // Enemy group can capture banner
        if (playerKiller.Group != null)
        {
            foreach (GamePlayer player in playerKiller.Group.GetPlayersInTheGroup())
                gameItem.AddOwner(player);
        }
    }
    
    gameItem.StartPickupTimer(10); // 10 second pickup timer
    gameItem.AddToWorld();
}
```

### Banner Recovery/Capture
- **Friendly Recovery**: Guild members can pick up dropped banner
- **Enemy Capture**: Killing group can take banner as trophy
- **Pickup Timer**: 10 seconds before anyone can interact
- **Guild Notification**: Messages sent to guild about banner status

### Banner Trophy System
```csharp
// Trophy models for captured banners
switch (Model)
{
    case 3223: // Albion banner
        trophyModel = 3359;
        realm = eRealm.Albion;
        break;
    case 3224: // Midgard banner
        trophyModel = 3361;
        realm = eRealm.Midgard;
        break;
    case 3225: // Hibernia banner
        trophyModel = 3360;
        realm = eRealm.Hibernia;
        break;
}
```

## Keep Banner System

### Keep Banner Types
```csharp
public enum eBannerType : int 
{
    Realm = 0,  // Realm-controlled keeps
    Guild = 1,  // Guild-claimed keeps
}
```

### Guild Keep Banners
```csharp
public void ChangeGuildBanner(Guild guild)
{
    int emblem = 0;
    if (guild != null)
    {
        emblem = guild.Emblem;
        this.AddToWorld();
    } 
    else 
    {
        this.RemoveFromWorld();
    }
    
    // Set appropriate model for realm
    ushort model = AlbionGuildModel;
    switch (Component.Keep.Realm)
    {
        case eRealm.Albion: model = AlbionGuildModel; break;
        case eRealm.Midgard: model = MidgardGuildModel; break;
        case eRealm.Hibernia: model = HiberniaGuildModel; break;
    }
    
    this.Model = model;
    this.Emblem = emblem;
    this.Name = GlobalConstants.RealmToName(Component.Keep.Realm) + " Guild Banner";
}
```

## Player Banner Display

### RvR Banner Display
```csharp
public override void SendRvRGuildBanner(GamePlayer player, bool show)
{
    if (player == null) return;
    
    // Cannot show banners for players without guild
    if (show && player.Guild == null) return;
    
    GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.VisualEffect)));
    pak.WriteShort((ushort)player.ObjectID);
    pak.WriteByte(0xC); // show Banner
    pak.WriteByte((byte)((show) ? 0 : 1)); // 0-enable, 1-disable
    
    // New emblem format with extended bit mask
    int newEmblemBitMask = ((player.Guild.Emblem & 0x010000) << 8) | 
                          (player.Guild.Emblem & 0xFFFF);
    pak.WriteInt((uint)newEmblemBitMask);
    SendTCP(pak);
}
```

### Equipment Emblem Display
- **Cloaks**: Guild emblem displayed on cloak
- **Shields**: Emblem shown on shield face
- **Permission Required**: Must have emblem wearing permission
- **Automatic Display**: Shows when equipment worn

## System Integration

### Guild System Integration
```csharp
// Guild properties for banner system
public bool GuildBanner { get; set; }
public DateTime GuildBannerLostTime { get; set; }
public int Emblem { get; set; }

// Banner status reporting
public string GuildBannerStatus(GamePlayer player)
{
    if (!GuildBanner)
    {
        if (GuildLevel >= 7)
        {
            TimeSpan lostTime = DateTime.Now.Subtract(GuildBannerLostTime);
            if (lostTime.TotalMinutes < Properties.GUILD_BANNER_LOST_TIME)
                return "Banner lost to the enemy";
            else
                return "Banner available for purchase";
        }
        return "None";
    }
    return "Guild Banner available";
}
```

### Combat System Integration
- **Banner Effects**: Apply to group members in range
- **Tactical Value**: High-priority targets in RvR
- **Positioning**: Banner location affects group strategy
- **Visibility**: Makes groups more visible to enemies

### Housing System Integration
```csharp
// House furniture emblem support
if (item.Emblem > 0)
    item.Color = item.Emblem;
    
if (item.Color > 0)
{
    if (item.Color <= 0xFF)
        type |= 1; // colored
    else if (item.Color <= 0xFFFF)
        type |= 2; // old emblem
    else
        type |= 6; // new emblem
}
```

## Configuration & Management

### Server Properties
```csharp
// Guild banner configuration
GUILD_BANNER_LOST_TIME = 180; // Minutes before new banner can be purchased
ENABLE_GUILD_BANNERS = true;  // Enable/disable banner system
BANNER_EFFECT_RANGE = 1000;   // Banner effect radius
```

### Database Storage
```sql
-- Guild table banner fields
GuildBanner BOOLEAN DEFAULT FALSE,
GuildBannerLostTime DATETIME,
Emblem INTEGER DEFAULT 0,

-- Banner tracking
CREATE TABLE GuildBannerLog (
    LogID AUTO_INCREMENT PRIMARY KEY,
    GuildID VARCHAR(255),
    Action VARCHAR(50), -- 'PURCHASED', 'SUMMONED', 'LOST', 'RECOVERED'
    Timestamp DATETIME,
    PlayerName VARCHAR(255),
    LocationX INT,
    LocationY INT,
    RegionID INT
);
```

## Tactical Considerations

### Strategic Value
- **Group Buffs**: Significant combat advantages
- **High-Value Target**: Priority target for enemies
- **Positioning Critical**: Affects group tactics
- **Risk/Reward**: Powerful but makes group visible

### Counter-Strategies
- **Focus Fire**: Target banner carrier first
- **Banner Capture**: Deny enemy benefits and gain trophy
- **Positioning**: Force unfavorable banner positions
- **Timing**: Attack during banner summoning vulnerability

## Edge Cases & Special Scenarios

### Banner Carrier Disconnect
- Banner despawns after timeout
- Guild receives notification
- No penalty or cooldown applied
- Banner ownership retained

### Zone Transition
- Banner automatically unsummoned
- No loss penalty applied
- Can resummon in new zone if RvR
- Effect timers reset

### Group Disbanding
- Banner remains with original summoner
- Effects continue for original group members in range
- New group members don't receive effects
- Manual unsummon required

### Multiple Guild Coordination
- Each guild can have one banner
- Effects stack from different guild banners
- Positioning becomes critical for maximum coverage
- Coordination required for optimal placement

## Test Scenarios

### Banner Lifecycle Testing
1. **Purchase Process**: Verify cost calculation and deduction
2. **Summoning Restrictions**: Group, zone, and cooldown checks
3. **Effect Application**: Proper buff application and stacking
4. **Death Mechanics**: Dropping, pickup permissions, timers

### Emblem System Testing
1. **Design Interface**: All emblem components functional
2. **Display Rendering**: Correct appearance on equipment
3. **Permission Checking**: Only authorized members can wear
4. **Keep Integration**: Guild banners on claimed keeps

### Edge Case Validation
1. **Disconnect Handling**: Proper cleanup and state preservation
2. **Zone Transitions**: Seamless unsummoning and restrictions
3. **Group Changes**: Effect continuity and membership updates
4. **Combat Integration**: Proper interaction with other systems

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Production documentation |
| Historical | Game | Extended emblem bit mask support |
| Historical | Game | Banner effect system implementation |
| Original | Game | Basic guild emblem and banner system | 