# Mission System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Stable
- **Verification**: Code-verified from AbstractMission.cs, TaskDungeonMission.cs
- **Implementation**: Stable

## Overview

**Game Rule Summary**: The mission system provides dynamic, repeatable adventures that scale with your level and group size. Mission Masters in major cities offer three types of missions: personal missions for solo players, group missions for parties, and realm missions for large-scale objectives. Most missions involve clearing dungeons, killing specific creatures, or defeating bosses. Unlike regular quests, missions are generated content that changes each time, offering consistent experience and money rewards. Group missions scale up the difficulty but provide better rewards for all members. Missions have time limits (usually 2 hours) and you can only have one active mission at a time.

The mission system provides dynamic, repeatable objectives for solo players, groups, and realms. Missions are generated content that scales with player level and party composition, offering alternative advancement paths.

## Core Mechanics

### Mission Types

#### Basic Mission Types
```csharp
public enum eMissionType : int
{
    None = 0,
    Personal = 1,    // Solo player missions
    Group = 2,       // Group-based missions
    Realm = 3,       // Realm-wide objectives
    Task = 4         // Task dungeon missions
}
```

#### Task Dungeon Types
```csharp
public enum eTDMissionType : byte
{
    Clear = 0,       // Kill all mobs in dungeon
    Specific = 1,    // Kill X specific mobs
    Boss = 2         // Kill named boss
}
```

### Mission Generation

#### Task Dungeon Selection
```csharp
// Find appropriate dungeon based on level
public static TaskDungeonMission GetTaskDungeonMission(int level)
{
    // Level ranges for dungeon selection
    if (level >= 1 && level <= 5)
        return CreateDungeon("Tomb of Mithra", level);
    else if (level >= 6 && level <= 10)
        return CreateDungeon("Nisse's Lair", level);
    // ... additional ranges
}
```

#### Mission Parameters
- **Clear Missions**: Kill 90-110% of dungeon population
- **Specific Missions**: Kill 8-12 specific mob types
- **Boss Missions**: Kill designated boss NPC

### Mission Rewards

#### Experience Rewards
```csharp
public override long RewardXP
{
    get
    {
        // XP based on mission type
        int xpMultiplier = XPMagicNumber; // 50-75
        GamePlayer player = GetMissionOwner();
        long baseXP = player.ExperienceForNextLevel;
        
        // Scale by mission difficulty
        return (baseXP * xpMultiplier) / 100;
    }
}
```

#### Money Rewards
```csharp
public override long RewardMoney
{
    get
    {
        GamePlayer player = GetMissionOwner();
        return player.Level * player.Level * 100; // Level² * 100 copper
    }
}
```

#### Realm/Bounty Points
- Personal Missions: 1500 RP standard
- Group Missions: Scaled by group size
- Realm Missions: Variable based on objective

### Mission Tracking

#### Progress Monitoring
```csharp
public class TaskDungeonMission : AbstractMission
{
    protected int m_current;    // Current kills
    protected int m_total;      // Required kills
    protected string m_targetName; // Specific target
    protected string m_bossName;   // Boss name
    
    public void UpdateProgress(GameNPC killed)
    {
        if (IsValidTarget(killed))
        {
            m_current++;
            if (m_current >= m_total)
                FinishMission();
        }
    }
}
```

#### Dynamic Descriptions
```csharp
public override string Description
{
    get
    {
        switch (m_missionType)
        {
            case eTDMissionType.Boss: 
                return $"Kill {m_bossName} in the nearby caves.";
            case eTDMissionType.Specific: 
                return $"Kill {m_total} {m_targetName} in the nearby caves.";
            case eTDMissionType.Clear:
                return $"Clear the nearby caves. {m_total - m_current} creatures left!";
        }
    }
}
```

### Group Missions

#### Shared Progress
- All group members receive credit
- Progress tracked collectively
- Rewards split among participants

#### Group Mission Scaling
```csharp
// Scale difficulty based on group
public void ScaleForGroup(Group group)
{
    int avgLevel = group.GetAverageLevel();
    int groupSize = group.MemberCount;
    
    // Increase mob count for larger groups
    m_total = BaseMobCount * (1 + (groupSize - 1) * 0.25);
    
    // Select appropriate dungeon
    m_taskRegion = GetScaledDungeon(avgLevel);
}
```

### Mission NPCs

#### Mission Masters
- Located in major cities
- Offer level-appropriate missions
- Track completion history

#### Mission Selection
```csharp
public override bool Interact(GamePlayer player)
{
    // Check if player has active mission
    if (player.Mission != null)
    {
        SendReply(player, "Complete your current mission first.");
        return true;
    }
    
    // Offer appropriate mission types
    if (player.Group != null)
        OfferGroupMission(player);
    else
        OfferPersonalMission(player);
}
```

## Mission Lifecycle

### Starting Missions
1. Player interacts with Mission Master
2. System checks eligibility
3. Appropriate mission generated
4. Objectives communicated to player
5. Mission timer starts (if applicable)

### During Mission
1. Actions monitored via event system
2. Progress updated real-time
3. UI notifications on milestones
4. Abandon option available

### Completing Missions
1. Final objective achieved
2. Return to any Mission Master
3. Rewards calculated and given
4. Mission cleared from player

## Special Mission Types

### Artifact Missions
- Unlock artifact encounters
- One-time completion
- Specific level requirements

### Epic Missions
- Multi-stage objectives
- Story-driven content
- Unique rewards

### Daily/Weekly Missions
- Reset on schedule
- Bonus rewards
- Limited attempts

## Performance Considerations

### Mission Caching
```csharp
// Cache dungeon configurations
private static Dictionary<string, DungeonConfig> s_dungeonCache = 
    new Dictionary<string, DungeonConfig>();

// Pre-calculate mission parameters
private void InitializeMissionData()
{
    // Load once at startup
    LoadDungeonConfigs();
    CacheMobCounts();
    PrecomputeRewards();
}
```

### Event Optimization
- Subscribe only to relevant events
- Unsubscribe on mission complete
- Batch progress updates

## Database Structure

### Mission Templates
```sql
CREATE TABLE MissionTemplate (
    MissionID int PRIMARY KEY,
    MissionType tinyint NOT NULL,
    MinLevel tinyint NOT NULL,
    MaxLevel tinyint NOT NULL,
    TargetRegion int,
    TargetName varchar(255),
    RequiredKills int,
    BaseRewardXP bigint,
    BaseRewardMoney bigint,
    BaseRewardRP int,
    BaseRewardBP int
);
```

### Player Mission Progress
```sql
CREATE TABLE CharacterMission (
    CharacterID varchar(255),
    MissionID int,
    Progress int NOT NULL DEFAULT 0,
    StartTime datetime NOT NULL,
    CONSTRAINT pk_CharMission PRIMARY KEY (CharacterID, MissionID)
);
```

## Configuration

### Server Properties
```ini
# Mission system settings
MISSION_ENABLED = true
MISSION_MAX_CONCURRENT = 1
MISSION_ABANDON_PENALTY = 300  # 5 minute cooldown
MISSION_GROUP_SCALING = true
```

### Level-Based Dungeons
| Level Range | Dungeon Options |
|------------|-----------------|
| 1-5 | Tomb of Mithra, Burial Tomb |
| 6-10 | Nisse's Lair, Muire Tomb |
| 11-15 | Spraggon Den, Koalinth Caverns |
| 16-20 | Cursed Tomb, Abandoned Mines |
| 21-50 | Various themed dungeons |

## Test Scenarios

### Basic Mission Flow
```csharp
// Given: Player level 10 with no active mission
// When: Interact with Mission Master
// Then: Offered appropriate level 6-10 missions

// Given: Active "Clear Dungeon" mission
// When: Kill required number of mobs
// Then: Mission completes, rewards available
```

### Group Mission Tests
```csharp
// Given: Group of 4 players, avg level 25
// When: Leader takes group mission
// Then: All members receive mission, scaled difficulty

// Given: Group mission in progress
// When: Member leaves group
// Then: Mission continues for remaining members
```

## Change Log
- 2025-01-20: Initial documentation created
- TODO: Document realm missions
- TODO: Add PvP mission types

## References
- `GameServer/quests/Missions/AbstractMission.cs`
- `GameServer/quests/Missions/TaskDungeonMission.cs`
- `GameServer/gameobjects/CustomNPC/MissionMaster.cs` 