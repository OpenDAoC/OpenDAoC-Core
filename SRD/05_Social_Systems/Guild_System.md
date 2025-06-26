# Guild System

## Document Status
- **Completeness**: 95% (missing some merit point details)
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from Guild.cs and GuildMgr.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
Guilds in DAoC are player-run organizations that provide social structure, shared resources, and realm warfare coordination. The system supports hierarchical ranks, permissions, emblems, and various guild activities.

## Core Mechanics

### Guild Structure

#### Rank System
- **10 Ranks**: 0 (Leader) through 9 (Lowest)
- Each rank has customizable:
  - Title
  - Permissions
  - Hierarchy level

#### Default Rank Setup
| Rank | Default Title | Default Permissions |
|------|--------------|-------------------|
| 0    | Guildmaster  | All permissions   |
| 1    | Officer      | Most permissions except leader functions |
| 2    | Officer      | Invite, promote, claim/release |
| 3    | Member       | Officer chat, invite, promote |
| 4    | Member       | Officer chat hear |
| 5    | Member       | Alliance chat |
| 6    | Member       | Emblem, alliance hear |
| 7    | Member       | Emblem |
| 8    | Member       | Guild chat speak |
| 9    | Member       | Guild chat hear only |

**Source**: `GuildMgr.cs:CreateRank()`

### Permission System

#### Permission Types
```csharp
public enum eRank
{
    Emblem,      // Wear guild emblem
    AcHear,      // Hear alliance chat
    AcSpeak,     // Speak in alliance chat
    Demote,      // Demote members
    Promote,     // Promote members
    GcHear,      // Hear guild chat
    GcSpeak,     // Speak in guild chat
    Invite,      // Invite new members
    OcHear,      // Hear officer chat
    OcSpeak,     // Speak in officer chat
    Remove,      // Kick members
    Leader,      // Guild leader only
    Alli,        // Alliance management
    View,        // View guild info
    Claim,       // Claim keeps
    Upgrade,     // Upgrade keeps
    Release,     // Release keeps
    Buff,        // Purchase guild buffs
    Dues,        // Set tax rate
    Withdraw     // Access guild bank
}
```

#### Permission Checks
```csharp
bool HasPermission = member.GuildRank.[Permission]
// Leader check: member.GuildRank.RankLevel == 0
```

### Guild Creation

#### Requirements
- Group of minimum players (server configurable)
- Not already in a guild
- Sufficient gold (if required)

#### Creation Process
1. Form group with required members
2. Use `/gc form <guildname>` command
3. Guild created with default ranks
4. Group leader becomes guildmaster

### Guild Properties

#### Basic Information
- **Name**: Unique guild identifier
- **Realm**: Albion/Midgard/Hibernia
- **Level**: Based on guild points
- **Emblem**: Visual identifier on cloaks/shields
- **Webpage**: External website URL
- **Email**: Contact information

#### Messages
- **MOTD**: Message of the Day (all members)
- **OMOTD**: Officer Message of the Day
- **AMOTD**: Alliance Message of the Day

### Guild Bank

#### Deposits
- Any member can deposit
- No withdrawal by default
- Withdrawal requires permission

#### Guild Dues
```
Tax Rate: 0-100% (configurable)
Applied to: Loot money
Destination: Guild bank
```

### Guild Buffs

#### Bonus Types
```csharp
public enum eBonusType
{
    None = 0,
    RealmPoints = 1,    // +RP gain
    BountyPoints = 2,   // +BP gain
    MasterLevelXP = 3,  // +MLXP (not implemented)
    CraftingHaste = 4,  // Faster crafting
    ArtifactXP = 5,     // +Artifact XP
    Experience = 6      // +PvE XP
}
```

#### Buff Mechanics
- Purchased with merit points
- Time-limited duration
- Affects all guild members

### Merit Points

#### Acquisition
Merit points earned from member activities:

**Crafting**:
- 700 skill: 100 points
- 800 skill: 200 points
- 900 skill: 300 points
- 1000 skill: 400 points

**Leveling**:
```csharp
Points = Level * (3.0 + (Level / 25.0))
// Level 50: 50 * (3 + 2) = 250 points
```

**Realm Ranks**:
```csharp
// Every 10 levels
Points = (3 * (RR - 1))²
// RR2: (3 * 1)² = 9 points
// RR5: (3 * 4)² = 144 points
```

### Guild Emblems

#### System
- Visual display on cloaks/shields
- Set by guild leader
- Requires emblem permission to wear

#### Costs
- Initial emblem: Free-200g (configurable)
- Change emblem: 100g

### Alliance System

#### Structure
- Multiple guilds allied together
- Shared alliance chat
- One guild leads alliance

#### Management
- Requires alliance permission
- Leader guild controls:
  - Accepting new guilds
  - Removing guilds
  - Alliance MOTD

## System Interactions

### With Keeps
- Guild claiming of keeps
- Upgrade permissions
- Defense coordination
- Keep-specific benefits

### With Housing
- Guild houses available
- Shared vault access
- Meeting locations

### With Chat
- Guild chat channel
- Officer chat channel
- Alliance chat channel
- Permission-based access

### With Combat
- Guild RP accumulation
- Guild buff effects
- Keep warfare bonuses

## Implementation Notes

### Database Structure
```sql
Guild table:
- GuildID: Unique identifier
- GuildName: Display name
- Realm: 1/2/3
- Motd: Message of the day
- Omotd: Officer message
- GuildBanner: True/false
- GuildBank: Money amount
- Dues: Tax percentage
- BonusType: Active buff
- BonusStartTime: Buff start

GuildRank table:
- GuildID: Reference
- RankLevel: 0-9
- Title: Rank name
- Various permission flags
```

### Commands
- `/gc form <name>`: Create guild
- `/gc invite`: Invite player
- `/gc promote/demote`: Change ranks
- `/gc quit`: Leave guild
- `/gc edit`: Modify rank permissions
- `/gc info`: View guild information
- `/gc motd/omotd`: Set messages

## Test Scenarios

### Creation Test
```
Given: 8 players in group
Action: /gc form TestGuild
Result: Guild created, leader rank 0
```

### Permission Test
```
Given: Rank 5 member, no invite permission
Action: Attempt /gc invite
Result: "No privileges" message
```

### Dues Test
```
Given: 10% guild dues set
Action: Loot 100 gold
Result: 90g to player, 10g to guild bank
```

### Merit Point Test
```
Given: Member reaches level 50
Calculation: 50 * 5 = 250 merit points
Result: Guild gains 250 merit points
```

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added complete permission system
- Documented merit points and buffs
- Added alliance system overview

## References
- `GameServer/gameutils/Guild.cs` - Core guild logic
- `GameServer/gameutils/GuildMgr.cs` - Guild management
- `GameServer/commands/playercommands/guild.cs` - Guild commands
- `GameServer/gameutils/GuildEvents.cs` - Merit point events 