# Guild System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from Guild.cs and GuildMgr.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: Guilds are player-run organizations that let you form lasting groups with friends and allies. They provide private communication channels, shared resources like a guild bank, and organized hierarchy with customizable ranks and permissions. Guilds are essential for realm warfare, housing, and coordinating group activities. Being in a guild gives you access to special buffs, shared emblems, and alliance partnerships with other guilds.

Guilds in DAoC are player-run organizations that provide social structure, shared resources, and realm warfare coordination. The system supports hierarchical ranks, permissions, emblems, and various guild activities.

## Core Mechanics

### Guild Structure

**Game Rule Summary**: Guilds have 10 ranks numbered 0-9, where 0 is the guild leader and 9 is the lowest rank. Each rank can have custom titles and specific permissions. This system lets guild leaders create organized hierarchies where officers have more responsibilities than regular members, and new recruits can be given limited permissions until they prove trustworthy.

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

**Game Rule Summary**: Guild permissions control what each rank can do within the guild. Basic permissions include speaking in different chat channels, inviting new members, promoting/demoting people, and managing guild resources. Higher ranks get more permissions, while lower ranks might only be able to listen to guild chat or wear the guild emblem. This system prevents new or untrusted members from disrupting the guild.

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

**Game Rule Summary**: To form a guild, you need to gather a minimum number of players (usually 8) who aren't already in other guilds. The group leader uses a command to create the guild, automatically becoming the guildmaster with full permissions. This requirement ensures guilds start with an active player base rather than being created by solo players.

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

**Game Rule Summary**: Guilds have different message systems to communicate with members. The Message of the Day (MOTD) is seen by all guild members when they log in. Officer MOTD is only for officers and leaders. Alliance MOTD is shared between allied guilds. These messages help coordinate activities and share important information.

- **MOTD**: Message of the Day (all members)
- **OMOTD**: Officer Message of the Day
- **AMOTD**: Alliance Message of the Day

### Guild Bank

**Game Rule Summary**: The guild bank is a shared pool of money that any member can contribute to, but only those with withdrawal permissions can take from. Guild leaders can set a tax rate that automatically takes a percentage of money you loot and puts it in the guild bank. This creates a shared resource for guild activities like buying buffs or funding keep upgrades.

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

**Game Rule Summary**: Guilds can purchase temporary buffs that benefit all members using merit points earned from member activities. These buffs might increase experience gain, crafting speed, or realm point rewards. The buffs last for a limited time and must be renewed, creating an ongoing benefit for active guilds that accumulate merit points.

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

**Game Rule Summary**: Merit points are earned automatically when guild members accomplish various activities like leveling up, gaining realm ranks, or mastering crafting skills. These points accumulate in the guild treasury and can be spent on guild buffs that benefit everyone. This system rewards active guilds whose members are constantly progressing.

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

**Game Rule Summary**: Guild emblems are visual symbols that appear on cloaks and shields to identify guild members. Only players with emblem permission can display the guild emblem. The guild leader can set or change the emblem design, usually for a gold cost. This creates a visible identity that helps allies and enemies recognize guild members in the field.

#### System
- Visual display on cloaks/shields
- Set by guild leader
- Requires emblem permission to wear

#### Costs
- Initial emblem: Free-200g (configurable)
- Change emblem: 100g

### Alliance System

**Game Rule Summary**: Multiple guilds can form alliances to create larger organized groups. Allied guilds share a common chat channel and can coordinate activities together. One guild serves as the alliance leader and controls who can join or leave the alliance. This system allows smaller guilds to work together while maintaining their individual identities.

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