# Group System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Complete
- **Verification**: Code-verified from Group.cs, AbstractServerRules.cs, and related files
- **Implementation**: Complete

## Overview

**Game Rule Summary**: Groups let you team up with other players to share experience and loot automatically. When you're in a group, everyone gets a share of experience from kills and can split loot and money evenly. Groups provide better experience than soloing through bonus experience, and the group window shows everyone's health and status. You can have up to 8 players in a group, with the leader controlling invitations and loot rules.

The group system allows players to form parties for shared experience, loot distribution, and coordinated gameplay. Groups support up to 8 members with automated loot/coin splitting and experience sharing mechanics.

## Core Mechanics

### Group Formation

**Game Rule Summary**: Anyone can start a group by inviting another player, automatically becoming the group leader. Groups can have up to 8 members total, all from the same realm. Players must accept invitations to join, and you can't be in multiple groups at once. Once formed, the group leader can invite more members, set loot rules, and manage the group.

#### Size Limits
- **Maximum Members**: 8 players (`GROUP_MAX_MEMBER`)
- **Minimum for Group**: 2 players
- **Leader Required**: Yes (first member becomes leader)
- **Cross-Realm**: Not allowed

#### Invitation Process
```csharp
public class Group : IGameStaticItemOwner
{
    protected readonly ReaderWriterList<GameLiving> m_groupMembers;
    protected readonly Lock _groupMembersLock = new();
    
    public Group(GameLiving leader)
    {
        LivingLeader = leader;
        m_groupMembers = new ReaderWriterList<GameLiving>(8);
    }
}
```

#### Joining Restrictions
- Target must not be in another group
- Must be same realm
- Must accept invitation
- Server rules must allow grouping

### Leadership & Management

**Game Rule Summary**: The group leader has special powers to manage the group, including inviting new members, removing troublemakers, setting how loot is distributed, and transferring leadership to someone else. Only the leader can disband the entire group. If the leader disconnects, leadership automatically transfers to another member to keep the group going.

#### Leader Privileges
- Invite new members
- Remove members  
- Set loot rules
- Promote new leader
- Disband group
- Set group status (LFG)

#### Leader Commands
```
/invite <player>     - Invite player to group
/disband             - Disband entire group  
/makeleader <player> - Transfer leadership
/remove <player>     - Remove from group
/autosplit loot      - Toggle item autosplit
/autosplit coins     - Toggle coin autosplit
/autosplit self      - Toggle personal autosplit
```

### Experience Sharing

**Game Rule Summary**: Groups get bonus experience compared to soloing, with 12.5% extra experience per additional group member. However, larger groups need to fight tougher enemies to get full experience - 8-person groups need to fight red (higher level) monsters. If the highest level player in your group sees an enemy as grey (worthless), nobody gets experience. Experience is split evenly among group members, but the bonus makes it worthwhile.

#### Con Color System
```csharp
// Level range for same con color
int levelDiff = Math.Abs(level1 - level2);
int levelRange = higherLevel / 10 + 1;

if (levelDiff <= levelRange)
    return ConColor.YELLOW; // Same range
else if (levelDiff <= levelRange * 2)
    return ConColor.BLUE;   // One range below
else if (levelDiff <= levelRange * 3)
    return ConColor.GREEN;  // Two ranges below
else
    return ConColor.GREY;   // Too low
```

#### Challenge Code
Groups must meet minimum challenge to get full XP:
```csharp
ConColor conColorThreshold;

if (memberCount >= 8)
    conColorThreshold = ConColor.RED;    // Need red+ mobs
else if (memberCount >= 4)
    conColorThreshold = ConColor.ORANGE; // Need orange+ mobs
else
    conColorThreshold = ConColor.YELLOW; // Need yellow+ mobs
```

#### Experience Distribution
```csharp
// Check highest level player's con to mob
ConColor conColorForHighestLevel = GetConColor(highestPlayer, killedNpc);

if (conColorForHighestLevel == ConColor.GREY)
    return 0; // No XP for anyone

// If challenge met
if (conColorForHighestLevel >= conColorThreshold)
    return killedNpc.ExperienceValue / memberCount;

// If challenge not met and player could get better XP solo
if (highestPlayer != playerToAward && 
    GetConColor(playerToAward, killedNpc) > conColorForHighestLevel)
{
    // Treat as if fighting lower con mob
    return CalculateReducedXP(playerToAward, conColorForHighestLevel);
}
```

#### Group Bonus
```csharp
// 12.5% bonus per extra group member
long groupBonus = (long)(baseXpReward * (groupSize - 1) * 0.125);
```

### Loot Distribution

**Game Rule Summary**: Groups can automatically split loot and money among members. When autosplit is enabled, items go to a random group member who wants them and has inventory space. Money is divided equally among all group members in range. Each player can choose whether they want to participate in item autosplit, but money is always shared if the group has autosplit enabled. This prevents arguments about loot and ensures fair distribution.

#### Autosplit Settings
```csharp
public class Group
{
    protected bool m_autosplitLoot = true;   // Items
    protected bool m_autosplitCoins = true;  // Money
    
    public bool AutosplitLoot { get; set; }
    public bool AutosplitCoins { get; set; }
}
```

#### Item Distribution
```csharp
public TryPickUpResult TryPickUpItem(GamePlayer source, WorldInventoryItem item)
{
    if (!AutosplitLoot)
        return TryPickUpResult.DOES_NOT_HANDLE;

    List<GamePlayer> eligibleMembers = new(8);

    foreach (GamePlayer member in GetPlayersInTheGroup())
    {
        if (member.ObjectState is eObjectState.Active && 
            member.AutoSplitLoot &&     // Personal setting
            member.CanSeeObject(item))  // Visual range
        {
            eligibleMembers.Add(member);
        }
    }

    if (eligibleMembers.Count == 0)
    {
        source.Out.SendMessage("No one in group wants this", ...);
        return TryPickUpResult.FAILED;
    }

    // Random selection with inventory check
    eligibleMembers = Util.ShuffleList(eligibleMembers);
    
    foreach (GamePlayer member in eligibleMembers)
    {
        if (member.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item.Item))
        {
            member.Out.SendMessage($"You receive {item.Name}", ...);
            return TryPickUpResult.SUCCESS;
        }
    }
    
    // No one has space
    source.Out.SendMessage("No one has room", ...);
    return TryPickUpResult.FAILED;
}
```

#### Coin Distribution
```csharp
public TryPickUpResult TryPickUpMoney(GamePlayer source, GameMoney money)
{
    if (!AutosplitCoins)
        return TryPickUpResult.DOES_NOT_HANDLE;

    List<GamePlayer> eligibleMembers = new(8);

    // Members must be in visible range (ignores AutoSplitLoot setting)
    foreach (GamePlayer member in GetPlayersInTheGroup())
    {
        if (member.ObjectState is eObjectState.Active && 
            member.CanSeeObject(money))
        {
            eligibleMembers.Add(member);
        }
    }

    // Split evenly
    long splitMoney = (long)Math.Ceiling((double)money.Value / eligibleMembers.Count);
    
    foreach (GamePlayer member in eligibleMembers)
    {
        // Apply guild dues
        long moneyToPlayer = member.ApplyGuildDues(splitMoney);
        
        if (moneyToPlayer > 0)
        {
            member.AddMoney(moneyToPlayer, "Your loot share");
            InventoryLogging.LogInventoryAction("(ground)", member, 
                eInventoryActionType.Loot, splitMoney);
        }
    }
    
    money.RemoveFromWorld();
    return TryPickUpResult.SUCCESS;
}
```

### Group Window & Status

**Game Rule Summary**: The group window shows important information about all group members including their health, mana, endurance, location, and any status effects like buffs or debuffs. This lets you quickly see who needs healing, who's low on mana, or who's in trouble. The window updates automatically when members take damage, change zones, or gain effects.

#### Member Information Display
- Name and class icon
- Level
- Current/Max health (bar + percentage)
- Current/Max mana (bar)
- Current/Max endurance (bar)
- Location (zone name)
- Status effects (icons)
- Connection status

#### Update Triggers
```csharp
public void UpdateGroupWindow()
{
    foreach (GamePlayer player in GetPlayersInTheGroup())
        player.Out.SendGroupWindowUpdate();
}
```

Triggered by:
- Member joins/leaves
- Health/mana/endurance changes
- Status effect changes
- Zone transitions
- Level changes
- Connection state changes

#### Status Byte
```csharp
protected byte m_status = 0x0A; // Default status

// Status bits:
// Bit 0-3: Looking for members class filter
// Bit 4: Looking for members flag
// Bit 5-7: Reserved
```

### Mission System

**Game Rule Summary**: Groups can undertake special missions that provide structured objectives and rewards. When a group accepts a mission, all members receive the mission details and can contribute to completing the objectives. Missions are designed for group play and typically provide better rewards than regular hunting.

#### Group Missions
```csharp
private Quests.AbstractMission m_mission = null;

public Quests.AbstractMission Mission
{
    get { return m_mission; }
    set
    {
        m_mission = value;
        foreach (GamePlayer player in m_groupMembers.OfType<GamePlayer>())
        {
            player.Out.SendQuestListUpdate();
            if (value != null)
                player.Out.SendMessage(m_mission.Description, ...);
        }
    }
}
```

### ROG (Random Object Generation) Integration

**Game Rule Summary**: When groups kill monsters, the loot system considers the entire group when generating random items. Larger groups have a chance for more items to drop, and the items generated can be suitable for any class in the group, not just the person who killed the monster. This ensures that group members have a better chance of finding useful equipment.

#### Group-Based Loot Generation
```csharp
// Atlas loot system considers group composition
if (player.Group != null)
{
    var MaxDropCap = Math.Round((decimal)(player.Group.MemberCount) / 3);
    if (MaxDropCap < 1) MaxDropCap = 1;
    if (MaxDropCap > 3) MaxDropCap = 3;
    
    // Higher level mobs increase cap
    if (mob.Level > 65) MaxDropCap++;
    
    // Roll for each group member in range
    foreach (var groupPlayer in player.Group.GetPlayersInTheGroup())
    {
        if (groupPlayer.GetDistance(player) > WorldMgr.VISIBILITY_DISTANCE)
            continue;
            
        if (Util.Chance(chance) && numDrops < MaxDropCap)
        {
            // Generate item for random group member's class
            classForLoot = GetRandomClassFromGroup(player.Group);
            var item = GenerateItemTemplate(player, classForLoot, ...);
            loot.AddFixed(item, 1);
            numDrops++;
        }
    }
}
```

## System Interactions

### With Battlegroup System
- Groups remain intact within battlegroups
- Experience still shared by group, not battlegroup
- Loot rules follow group settings
- Maximum 8 per group still enforced

### With Combat System
- Shared kill credit for group members
- Damage tracking per group for rewards
- Group-wide threat management

### With Quest System
- Some quests require groups
- Kill credit shared for quest objectives
- Mission progress synchronized

### With Guild System
- Guild dues applied to coin splits
- Guild bonuses may affect group XP

### With Property System
- Group bonuses calculated by property system
- Buff sharing within visual range
- Group-wide effect considerations

## Implementation Notes

### Thread Safety
```csharp
protected readonly ReaderWriterList<GameLiving> m_groupMembers;
protected readonly Lock _groupMembersLock = new();
```
- Uses reader/writer locks for member list
- Thread-safe collection operations

### Performance Considerations
- Member updates batched when possible
- Range checks limit update frequency
- Efficient member iteration patterns

### Network Protocol
- Group window updates use specific packets
- Member status compressed to single byte
- Updates sent only to affected players

## Test Scenarios

### Basic Group Formation
```
1. Player A invites Player B
2. Player B accepts
3. Group forms with A as leader
4. Both see group window
```

### Experience Sharing Test
```
Group of 4 kills orange mob:
- Base XP: 1000
- Per member: 250
- Group bonus: 250 * 3 * 0.125 = 93.75
- Total per member: 343.75
```

### Loot Distribution Test
```
Item drops with autosplit on:
1. Check eligible members in range
2. Filter by AutoSplitLoot setting
3. Random selection
4. Inventory space check
5. Award to selected member
```

### Challenge Code Test
```
8-person group fighting yellow mob:
- Threshold: RED
- Mob con: YELLOW
- Result: Reduced XP as if solo
```

## Edge Cases

### Leader Disconnect
- Leadership transfers to next member
- Group persists if 2+ members remain
- All settings maintained

### Full Inventory
- Item distribution tries all eligible members
- If none have space, item stays on ground
- Clear message to group

### Mixed Level Groups
- XP based on highest member's con
- Lower members may get reduced XP
- Grey con = no XP for anyone

### Range Limitations
- Must be in visual range for loot
- Must be within XP range for experience
- Zone transitions don't break group

## Change Log

### 2025-01-20
- Complete rewrite with detailed mechanics
- Added code examples and formulas
- Included Atlas ROG integration
- Added thread safety notes

## References
- Group.cs: Core group implementation
- AbstractServerRules.cs: XP distribution logic
- AtlasMobLoot.cs: Group-aware loot generation
# Group System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Complete
- **Verification**: Code-verified from Group.cs, AbstractServerRules.cs, and related files
- **Implementation**: Complete

## Overview
The group system allows players to form parties for shared experience, loot distribution, and coordinated gameplay. Groups support up to 8 members with automated loot/coin splitting and experience sharing mechanics.

## Core Mechanics

### Group Formation

#### Size Limits
- **Maximum Members**: 8 players (`GROUP_MAX_MEMBER`)
- **Minimum for Group**: 2 players
- **Leader Required**: Yes (first member becomes leader)
- **Cross-Realm**: Not allowed

#### Invitation Process
```csharp
public class Group : IGameStaticItemOwner
{
    protected readonly ReaderWriterList<GameLiving> m_groupMembers;
    protected readonly Lock _groupMembersLock = new();
    
    public Group(GameLiving leader)
    {
        LivingLeader = leader;
        m_groupMembers = new ReaderWriterList<GameLiving>(8);
    }
}
```

#### Joining Restrictions
- Target must not be in another group
- Must be same realm
- Must accept invitation
- Server rules must allow grouping

### Leadership & Management

#### Leader Privileges
- Invite new members
- Remove members  
- Set loot rules
- Promote new leader
- Disband group
- Set group status (LFG)

#### Leader Commands
```
/invite <player>     - Invite player to group
/disband             - Disband entire group  
/makeleader <player> - Transfer leadership
/remove <player>     - Remove from group
/autosplit loot      - Toggle item autosplit
/autosplit coins     - Toggle coin autosplit
/autosplit self      - Toggle personal autosplit
```

### Experience Sharing

#### Con Color System
```csharp
// Level range for same con color
int levelDiff = Math.Abs(level1 - level2);
int levelRange = higherLevel / 10 + 1;

if (levelDiff <= levelRange)
    return ConColor.YELLOW; // Same range
else if (levelDiff <= levelRange * 2)
    return ConColor.BLUE;   // One range below
else if (levelDiff <= levelRange * 3)
    return ConColor.GREEN;  // Two ranges below
else
    return ConColor.GREY;   // Too low
```

#### Challenge Code
Groups must meet minimum challenge to get full XP:
```csharp
ConColor conColorThreshold;

if (memberCount >= 8)
    conColorThreshold = ConColor.RED;    // Need red+ mobs
else if (memberCount >= 4)
    conColorThreshold = ConColor.ORANGE; // Need orange+ mobs
else
    conColorThreshold = ConColor.YELLOW; // Need yellow+ mobs
```

#### Experience Distribution
```csharp
// Check highest level player's con to mob
ConColor conColorForHighestLevel = GetConColor(highestPlayer, killedNpc);

if (conColorForHighestLevel == ConColor.GREY)
    return 0; // No XP for anyone

// If challenge met
if (conColorForHighestLevel >= conColorThreshold)
    return killedNpc.ExperienceValue / memberCount;

// If challenge not met and player could get better XP solo
if (highestPlayer != playerToAward && 
    GetConColor(playerToAward, killedNpc) > conColorForHighestLevel)
{
    // Treat as if fighting lower con mob
    return CalculateReducedXP(playerToAward, conColorForHighestLevel);
}
```

#### Group Bonus
```csharp
// 12.5% bonus per extra group member
long groupBonus = (long)(baseXpReward * (groupSize - 1) * 0.125);
```

### Loot Distribution

#### Autosplit Settings
```csharp
public class Group
{
    protected bool m_autosplitLoot = true;   // Items
    protected bool m_autosplitCoins = true;  // Money
    
    public bool AutosplitLoot { get; set; }
    public bool AutosplitCoins { get; set; }
}
```

#### Item Distribution
```csharp
public TryPickUpResult TryPickUpItem(GamePlayer source, WorldInventoryItem item)
{
    if (!AutosplitLoot)
        return TryPickUpResult.DOES_NOT_HANDLE;

    List<GamePlayer> eligibleMembers = new(8);

    foreach (GamePlayer member in GetPlayersInTheGroup())
    {
        if (member.ObjectState is eObjectState.Active && 
            member.AutoSplitLoot &&     // Personal setting
            member.CanSeeObject(item))  // Visual range
        {
            eligibleMembers.Add(member);
        }
    }

    if (eligibleMembers.Count == 0)
    {
        source.Out.SendMessage("No one in group wants this", ...);
        return TryPickUpResult.FAILED;
    }

    // Random selection with inventory check
    eligibleMembers = Util.ShuffleList(eligibleMembers);
    
    foreach (GamePlayer member in eligibleMembers)
    {
        if (member.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item.Item))
        {
            member.Out.SendMessage($"You receive {item.Name}", ...);
            return TryPickUpResult.SUCCESS;
        }
    }
    
    // No one has space
    source.Out.SendMessage("No one has room", ...);
    return TryPickUpResult.FAILED;
}
```

#### Coin Distribution
```csharp
public TryPickUpResult TryPickUpMoney(GamePlayer source, GameMoney money)
{
    if (!AutosplitCoins)
        return TryPickUpResult.DOES_NOT_HANDLE;

    List<GamePlayer> eligibleMembers = new(8);

    // Members must be in visible range (ignores AutoSplitLoot setting)
    foreach (GamePlayer member in GetPlayersInTheGroup())
    {
        if (member.ObjectState is eObjectState.Active && 
            member.CanSeeObject(money))
        {
            eligibleMembers.Add(member);
        }
    }

    // Split evenly
    long splitMoney = (long)Math.Ceiling((double)money.Value / eligibleMembers.Count);
    
    foreach (GamePlayer member in eligibleMembers)
    {
        // Apply guild dues
        long moneyToPlayer = member.ApplyGuildDues(splitMoney);
        
        if (moneyToPlayer > 0)
        {
            member.AddMoney(moneyToPlayer, "Your loot share");
            InventoryLogging.LogInventoryAction("(ground)", member, 
                eInventoryActionType.Loot, splitMoney);
        }
    }
    
    money.RemoveFromWorld();
    return TryPickUpResult.SUCCESS;
}
```

### Group Window & Status

#### Member Information Display
- Name and class icon
- Level
- Current/Max health (bar + percentage)
- Current/Max mana (bar)
- Current/Max endurance (bar)
- Location (zone name)
- Status effects (icons)
- Connection status

#### Update Triggers
```csharp
public void UpdateGroupWindow()
{
    foreach (GamePlayer player in GetPlayersInTheGroup())
        player.Out.SendGroupWindowUpdate();
}
```

Triggered by:
- Member joins/leaves
- Health/mana/endurance changes
- Status effect changes
- Zone transitions
- Level changes
- Connection state changes

#### Status Byte
```csharp
protected byte m_status = 0x0A; // Default status

// Status bits:
// Bit 0-3: Looking for members class filter
// Bit 4: Looking for members flag
// Bit 5-7: Reserved
```

### Mission System

#### Group Missions
```csharp
private Quests.AbstractMission m_mission = null;

public Quests.AbstractMission Mission
{
    get { return m_mission; }
    set
    {
        m_mission = value;
        foreach (GamePlayer player in m_groupMembers.OfType<GamePlayer>())
        {
            player.Out.SendQuestListUpdate();
            if (value != null)
                player.Out.SendMessage(m_mission.Description, ...);
        }
    }
}
```

### ROG (Random Object Generation) Integration

#### Group-Based Loot Generation
```csharp
// Atlas loot system considers group composition
if (player.Group != null)
{
    var MaxDropCap = Math.Round((decimal)(player.Group.MemberCount) / 3);
    if (MaxDropCap < 1) MaxDropCap = 1;
    if (MaxDropCap > 3) MaxDropCap = 3;
    
    // Higher level mobs increase cap
    if (mob.Level > 65) MaxDropCap++;
    
    // Roll for each group member in range
    foreach (var groupPlayer in player.Group.GetPlayersInTheGroup())
    {
        if (groupPlayer.GetDistance(player) > WorldMgr.VISIBILITY_DISTANCE)
            continue;
            
        if (Util.Chance(chance) && numDrops < MaxDropCap)
        {
            // Generate item for random group member's class
            classForLoot = GetRandomClassFromGroup(player.Group);
            var item = GenerateItemTemplate(player, classForLoot, ...);
            loot.AddFixed(item, 1);
            numDrops++;
        }
    }
}
```

## System Interactions

### With Battlegroup System
- Groups remain intact within battlegroups
- Experience still shared by group, not battlegroup
- Loot rules follow group settings
- Maximum 8 per group still enforced

### With Combat System
- Shared kill credit for group members
- Damage tracking per group for rewards
- Group-wide threat management

### With Quest System
- Some quests require groups
- Kill credit shared for quest objectives
- Mission progress synchronized

### With Guild System
- Guild dues applied to coin splits
- Guild bonuses may affect group XP

### With Property System
- Group bonuses calculated by property system
- Buff sharing within visual range
- Group-wide effect considerations

## Implementation Notes

### Thread Safety
```csharp
protected readonly ReaderWriterList<GameLiving> m_groupMembers;
protected readonly Lock _groupMembersLock = new();
```
- Uses reader/writer locks for member list
- Thread-safe collection operations

### Performance Considerations
- Member updates batched when possible
- Range checks limit update frequency
- Efficient member iteration patterns

### Network Protocol
- Group window updates use specific packets
- Member status compressed to single byte
- Updates sent only to affected players

## Test Scenarios

### Basic Group Formation
```
1. Player A invites Player B
2. Player B accepts
3. Group forms with A as leader
4. Both see group window
```

### Experience Sharing Test
```
Group of 4 kills orange mob:
- Base XP: 1000
- Per member: 250
- Group bonus: 250 * 3 * 0.125 = 93.75
- Total per member: 343.75
```

### Loot Distribution Test
```
Item drops with autosplit on:
1. Check eligible members in range
2. Filter by AutoSplitLoot setting
3. Random selection
4. Inventory space check
5. Award to selected member
```

### Challenge Code Test
```
8-person group fighting yellow mob:
- Threshold: RED
- Mob con: YELLOW
- Result: Reduced XP as if solo
```

## Edge Cases

### Leader Disconnect
- Leadership transfers to next member
- Group persists if 2+ members remain
- All settings maintained

### Full Inventory
- Item distribution tries all eligible members
- If none have space, item stays on ground
- Clear message to group

### Mixed Level Groups
- XP based on highest member's con
- Lower members may get reduced XP
- Grey con = no XP for anyone

### Range Limitations
- Must be in visual range for loot
- Must be within XP range for experience
- Zone transitions don't break group

## Change Log

### 2025-01-20
- Complete rewrite with detailed mechanics
- Added code examples and formulas
- Included Atlas ROG integration
- Added thread safety notes

## References
- Group.cs: Core group implementation
- AbstractServerRules.cs: XP distribution logic
- AtlasMobLoot.cs: Group-aware loot generation
- BattleGroup.cs: Battlegroup interactions 