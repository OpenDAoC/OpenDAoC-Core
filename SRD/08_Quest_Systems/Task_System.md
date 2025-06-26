# Task System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Stable
- **Verification**: Code-verified from AbstractTask.cs, task implementations
- **Implementation**: Stable

## Overview
The task system provides short, repeatable objectives for low-level players (1-20). Tasks offer an alternative advancement method through simple kill or delivery objectives with scaled rewards based on player level.

## Core Mechanics

### Task Types

#### Kill Tasks
- Hunt specific creatures
- Fixed target count
- Location-based objectives

#### Delivery Tasks  
- Transport items between NPCs
- Timed deliveries
- Cross-zone objectives

#### Craft Tasks
- Create specific items
- Quality requirements
- Material gathering

### Level Restrictions

#### Task Availability
```csharp
// Players level 1-20 can do tasks
public static bool CanDoTask(GamePlayer player)
{
    return player.Level >= 1 && player.Level <= 20;
}

// Number of concurrent tasks = player level
public static int MaxConcurrentTasks(GamePlayer player)
{
    return Math.Min(player.Level, 20);
}
```

#### Daily Limits
- Level 1: 1 task per day
- Level 2: 2 tasks per day
- Level 3+: 3 tasks per day
- ...up to level 20

### Task Generation

#### NPC Task Assignment
```csharp
public virtual bool BuildTask(GamePlayer player, GameLiving source)
{
    // Select appropriate task type
    TaskType type = SelectTaskType(player.Level);
    
    // Generate task parameters
    switch (type)
    {
        case TaskType.Kill:
            return GenerateKillTask(player, source);
        case TaskType.Delivery:
            return GenerateDeliveryTask(player, source);
        case TaskType.Craft:
            return GenerateCraftTask(player, source);
    }
}
```

#### Target Selection
```csharp
// Kill task mob selection
protected virtual GameNPC SelectKillTarget(GamePlayer player)
{
    int level = player.Level;
    int range = 2; // +/- 2 levels
    
    // Find mobs in level range
    var validTargets = GetMobsInRange(
        player.CurrentRegion,
        level - range,
        level + range
    );
    
    // Select random target type
    return validTargets[Util.Random(validTargets.Count)];
}
```

### Reward System

#### Experience Rewards
```csharp
public virtual long RewardXP
{
    get
    {
        // 50% of level's total XP divided by max tasks
        long XPNeeded = m_taskPlayer.GetExperienceNeededForLevel(m_taskPlayer.Level);
        return (long)(XPNeeded * 0.50 / (m_taskPlayer.Level - 1));
    }
}
```

#### Money Rewards
- Variable by task type
- Scales with player level
- Bonus for quick completion

#### Item Rewards
- Random level-appropriate items
- Quality based on performance
- Special task tokens (custom servers)

### Task Tracking

#### Task Properties
```csharp
public abstract class AbstractTask : IQuestStep
{
    // Task state
    protected GamePlayer m_taskPlayer;
    protected int m_tasksDone;
    protected DateTime m_timeOut;
    
    // Task data
    protected string m_targetName;
    protected int m_targetCount;
    protected string m_receiverName;
    protected string m_itemName;
    
    // Progress tracking
    public bool TaskActive { get; }
    public int Progress { get; set; }
}
```

#### Custom Properties
```csharp
// Flexible data storage
protected HybridDictionary m_customProperties;

// Common properties
const string RECEIVER_NAME = "ReceiverName";
const string ITEM_NAME = "ItemName";
const string TARGET_REGION = "TargetRegion";
const string KILL_COUNT = "KillCount";
```

## Task NPCs

### Town Criers
- Offer location-appropriate tasks
- Track task history
- Provide task status

### Task Interaction
```csharp
public override bool WhisperReceive(GameLiving source, string text)
{
    GamePlayer player = source as GamePlayer;
    
    switch (text.ToLower())
    {
        case "task":
            if (player.Task == null)
                OfferTask(player);
            else
                ShowTaskStatus(player);
            break;
            
        case "abandon":
            if (player.Task != null)
                player.Task.AbortTask();
            break;
    }
}
```

## Task Flow

### Starting Tasks
1. Player talks to Town Crier
2. Check level and daily limit
3. Generate appropriate task
4. Assign objectives
5. Start timeout timer

### During Task
1. Monitor relevant events
2. Update progress
3. Check timeout status
4. Handle abandonment

### Completing Tasks
1. Objectives achieved
2. Return to any Town Crier
3. Verify completion
4. Grant rewards
5. Update daily count

## Special Mechanics

### Timeout System
```csharp
// Default timeout: 2 hours
public virtual DateTime TimeOut
{
    get { return m_dbTask.TimeOut; }
    set 
    { 
        m_dbTask.TimeOut = value;
        // Auto-abandon if expired
        if (DateTime.Now > value)
            AbortTask();
    }
}
```

### Task Chaining
- Some tasks unlock follow-ups
- Bonus rewards for chains
- Story progression

### Group Tasks
- Shared credit for kills
- Group delivery coordination
- Increased rewards

## Database Structure

### Task Templates
```sql
CREATE TABLE TaskTemplate (
    TaskID int PRIMARY KEY,
    TaskType varchar(50) NOT NULL,
    MinLevel tinyint NOT NULL,
    MaxLevel tinyint NOT NULL,
    TargetName varchar(255),
    TargetCount int,
    TimeoutMinutes int DEFAULT 120
);
```

### Character Tasks
```sql
CREATE TABLE Task (
    Task_ID varchar(255) PRIMARY KEY,
    Character_Name varchar(255) NOT NULL,
    TaskType varchar(255),
    TimeOut datetime,
    TasksDone int DEFAULT 0,
    CustomPropsXML text
);
```

## Configuration

### Task Settings
```ini
# Task system configuration
TASK_ENABLED = true
TASK_MAX_LEVEL = 20
TASK_TIMEOUT_MINUTES = 120
TASK_XP_PERCENT = 50  # Percent of level XP
```

### Regional Task Lists
- Each region has appropriate tasks
- Level-scaled objectives
- Faction considerations

## Test Scenarios

### Basic Task Flow
```csharp
// Given: Level 5 player, no active task
// When: Request task from Town Crier
// Then: Receive level-appropriate kill task

// Given: Kill task for 10 wolves
// When: Kill 10 wolves
// Then: Task completes, can turn in

// Given: Completed task
// When: Talk to any Town Crier
// Then: Receive XP reward (50% / 4 tasks)
```

### Edge Cases
```csharp
// Given: Task with 30 minutes remaining
// When: 30 minutes pass
// Then: Task auto-abandons

// Given: Level 20 player at daily limit
// When: Request new task
// Then: "You've done enough tasks today" message
```

## Task Formulas

### Kill Task Targets
```
Target Count = 8 + Random(0, 4)
Target Level = PlayerLevel +/- 2
Search Radius = 5000 units
```

### Delivery Task Distance
```
Min Distance = 1000 * PlayerLevel units
Max Distance = 2000 * PlayerLevel units
Time Limit = 30 + (Distance / 1000) minutes
```

### Craft Task Requirements
```
Item Count = 5 + (PlayerLevel / 4)
Quality Required = 90 + (PlayerLevel / 5)
Material Tier = (PlayerLevel - 1) / 10
```

## Change Log
- 2025-01-20: Initial documentation created
- TODO: Document epic task chains
- TODO: Add seasonal tasks

## References
- `GameServer/quests/Tasks/AbstractTask.cs`
- `GameServer/quests/Tasks/KillTask.cs`
- `GameServer/quests/Tasks/DeliveryTask.cs`
- `GameServer/quests/Tasks/CraftTask.cs`
- `GameServer/gameobjects/CustomNPC/TownCrier.cs` 