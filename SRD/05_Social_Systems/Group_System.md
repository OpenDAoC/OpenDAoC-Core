# Group System

## Document Status
- Status: Comprehensive
- Implementation: Complete

## Overview
The group system allows players to form parties for shared experience, loot distribution, and coordinated gameplay. Groups support up to 8 members with automated loot/coin splitting and experience sharing mechanics.

## Core Mechanics

### Group Formation

#### Size Limits
- **Maximum Members**: 8 players (`GROUP_MAX_MEMBER`)
- **Minimum for Group**: 2 players
- **Leader Required**: Yes (first member becomes leader)

#### Invitation Process
1. Target player or use `/invite <name>`
2. Target must not be in another group
3. Server rules must allow grouping
4. Recipient accepts/declines invitation

### Leadership & Management

#### Leader Privileges
- Invite new members
- Remove members
- Set loot rules
- Promote new leader
- Disband group

#### Leader Commands
```
/invite <player>     - Invite player to group
/disband             - Disband entire group
/makeleader <player> - Transfer leadership
/remove <player>     - Remove from group
```

### Experience Sharing

#### Level Range Requirements
```csharp
// Color range calculation
int levelRange = Level / 10 + 1;

// Example: Level 40 player
// Yellow: 36-40
// Blue: 31-35
// Green: 26-30
// Grey: 25 and below
```

#### Challenge Code
```csharp
// Minimum con color by group size
if (memberCount >= 8)
    conColorThreshold = ConColor.RED;
else if (memberCount >= 4)
    conColorThreshold = ConColor.ORANGE;
else
    conColorThreshold = ConColor.YELLOW;
```

#### Experience Distribution
- **Same Color Range**: Split evenly
- **Challenge Met**: Full XP / member count
- **Challenge Not Met**: Reduced as if soloing lower con
- **Grey Con to Highest**: No XP for anyone

### Loot Distribution

#### Autosplit Settings
- **Loot**: Items distributed to random eligible member
- **Coins**: Money split evenly among nearby members
- **Self**: Individual opts out of item autosplit

#### Eligibility Requirements
- Must be active (not linkdead)
- Must be within visual range of item
- Must have `AutoSplitLoot` enabled (for items)
- Must have inventory space

#### Loot Distribution Process
```csharp
public TryPickUpResult TryPickUpItem(GamePlayer source, WorldInventoryItem item)
{
    if (!AutosplitLoot)
        return TryPickUpResult.DOES_NOT_HANDLE;

    List<GamePlayer> eligibleMembers = new(8);

    foreach (GamePlayer member in GetPlayersInTheGroup())
    {
        if (member.ObjectState is eObjectState.Active && 
            member.AutoSplitLoot && 
            member.CanSeeObject(item))
        {
            eligibleMembers.Add(member);
        }
    }

    // Random selection from eligible
    // Check inventory space
    // Award to selected member
}
```

#### Coin Distribution
```csharp
// Split calculation
long splitMoney = (long) Math.Ceiling((double) money.Value / eligibleMembers.Count);

// Apply guild dues
moneyToPlayer = eligibleMember.ApplyGuildDues(splitMoney);

// Award to each eligible member
eligibleMember.AddMoney(moneyToPlayer, ...);
```

### Group Window & Status

#### Member Information
- Name and class
- Level
- Current/Max health
- Current/Max mana  
- Current/Max endurance
- Location (zone)
- Status effects

#### Update Triggers
- Member joins/leaves
- Health/mana/endurance changes
- Status effect changes
- Zone transitions
- Level changes

### Battlegroup Integration

#### Battlegroup vs Group
- Groups remain within battlegroups
- Battlegroup for large-scale coordination
- Groups for experience/loot sharing
- Maximum 8 per group still applies

### Mission System

#### Group Missions
- Shared mission objectives
- All members see same mission
- Progress shared across group
- Rewards may be individual or shared

## System Interactions

### Combat System
- Shared kill credit
- Group consideration for aggro
- BAF (Bring A Friend) affects whole group

### Spell System
- Group buffs affect all members in range
- Group heals prioritize members
- Resurrection priority for group members

### RvR System
- Group members share realm point proximity bonus
- Keep capture credit shared
- Relic capture coordination

### Chat System
- `/g` or `/group` for group chat
- `/p` or `/party` as alternatives
- Group chat visible to all members

## Implementation Notes

### Class Structure
```csharp
public class Group
{
    private HybridDictionary m_groupMembers;
    private GameLiving m_groupLeader;
    private bool m_autosplitLoot = true;
    private bool m_autosplitCoins = true;
    private byte m_status = 0x0A;
    private AbstractMission m_mission = null;
}
```

### Thread Safety
- Synchronized member list access
- Atomic leadership transfers
- Thread-safe status updates

### Performance
- Lazy update propagation
- Cached member arrays
- Efficient distance checks

## Test Scenarios

### Formation Tests
- Create 2-player group
- Expand to 8 players
- Attempt 9th player (should fail)
- Leader disconnect handling

### Experience Tests
- Same level party XP split
- Mixed level with color ranges
- Challenge code thresholds
- Grey con mob XP denial

### Loot Tests
- Item autosplit with eligible members
- Coin distribution with guild dues
- Out-of-range member exclusion
- Inventory full scenarios

### Edge Cases
- Leader linkdeath
- Simultaneous invites
- Cross-realm grouping attempts
- Zone transition updates

## Change Log
- Initial documentation created
- Added experience sharing formulas
- Detailed loot distribution
- Added battlegroup integration

## References
- GameServer/gameutils/Group.cs
- GameServer/packets/Client/168/InviteToGroupHandler.cs
- GameServer/commands/playercommands/invite.cs
- ServerProperties.Properties.GROUP_MAX_MEMBER 