# Quest Mechanics

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from DataQuest.cs, AbstractQuest.cs, IDataQuestStep.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
DAoC's quest system provides narrative structure and rewards through various quest types including standard quests, collection quests, and reward quests. The system supports complex quest chains, conditional requirements, and dynamic content.

## Core Mechanics

### Quest Types

#### Start Types
```csharp
public enum eStartType : byte
{
    Standard = 0,        // Talk to NPC, accept quest, follow steps
    Collection = 1,      // Turn in items for XP, no quest log
    AutoStart = 2,       // Auto-started on interaction
    KillComplete = 3,    // Kill target grants and completes quest
    InteractComplete = 4,// Interact to grant and complete quest
    SearchStart = 5,     // Started by searching an area
    RewardQuest = 200    // Shows reward dialog on offer/complete
}
```

#### Step Types
```csharp
public enum eStepType : byte
{
    Kill = 0,              // Kill target to advance
    KillFinish = 1,        // Kill target to finish quest
    Deliver = 2,           // Deliver item to advance
    DeliverFinish = 3,     // Deliver item to finish
    Interact = 4,          // Interact with target to advance
    InteractFinish = 5,    // Interact to finish (required for RewardQuest)
    Whisper = 6,           // Whisper to target to advance
    WhisperFinish = 7,     // Whisper to finish
    Search = 8,            // Search location to advance
    SearchFinish = 9       // Search location to finish
}
```

### Quest Structure

#### Quest Definition
```csharp
public class DataQuest : AbstractQuest
{
    // Core Properties
    int ID                      // Unique quest ID
    string Name                 // Quest name
    string Description          // Quest description
    int MinLevel               // Minimum level requirement
    int MaxLevel               // Maximum level allowed
    byte MaxCount              // Times quest can be completed
    
    // Start Configuration
    eStartType StartType       // How quest begins
    string StartName           // NPC/Object name
    ushort StartRegionID       // Starting region
    
    // Steps
    string StepText            // Text for each step
    eStepType StepType         // Type of each step
    string StepItemTemplate    // Items for step
    
    // Rewards
    long RewardMoney          // Money reward
    long RewardXP             // Experience reward
    int RewardRP              // Realm points
    int RewardBP              // Bounty points
}
```

### Quest Indicators

#### Indicator Types
```csharp
public enum eQuestIndicator : byte
{
    None = 0,              // No indicator
    Available = 1,         // Yellow (!)
    Finish = 2,           // Yellow (?)
    Repeatable = 3        // Blue (!)
}
```

#### Indicator Logic
```csharp
public eQuestIndicator GetQuestIndicator(GamePlayer player)
{
    // Check if quest can be finished
    if (CanFinishQuest())
        return eQuestIndicator.Finish;
        
    // Check if quest is available
    if (CheckQuestQualification(player))
        return IsRepeatable ? eQuestIndicator.Repeatable : eQuestIndicator.Available;
        
    return eQuestIndicator.None;
}
```

### Quest Qualification

#### Level Requirements
```csharp
if (player.Level < MinLevel || player.Level > MaxLevel)
    return false;
```

#### Class Restrictions
```csharp
if (m_allowedClasses.Count > 0)
{
    if (!m_allowedClasses.Contains(player.CharacterClass.ID))
        return false;
}
```

#### Quest Dependencies
```csharp
// Check prerequisite quests
foreach (string dependency in m_questDependencies)
{
    if (!HasCompletedQuest(player, dependency))
        return false;
}
```

#### Completion Limits
```csharp
if (Count >= MaxQuestCount && MaxQuestCount >= 0)
    return false; // Already completed max times
```

### Quest Progress

#### Step Advancement
```csharp
protected virtual bool AdvanceQuestStep(GameObject obj)
{
    // Check inventory space for rewards
    if (RequiresInventorySpace && !HasInventorySpace())
        return false;
        
    // Execute custom step logic
    if (!ExecuteCustomQuestStep(player, Step, eStepCheckType.Step))
        return false;
        
    // Handle item drops
    if (StepType == eStepType.Kill && HasDropChance)
    {
        if (Util.Chance(DropChance))
            GiveItem(player, StepItemTemplate);
    }
    
    // Advance to next step
    Step++;
    player.Out.SendQuestUpdate(this);
    
    return true;
}
```

#### Quest Completion
```csharp
public virtual bool FinishQuest(GameObject obj, bool giveReward)
{
    if (giveReward)
    {
        // Give experience
        if (RewardXP > 0)
            player.GainExperience(RewardXP);
            
        // Give realm points
        if (RewardRP > 0)
            player.GainRealmPoints(RewardRP);
            
        // Give bounty points
        if (RewardBP > 0)
            player.GainBountyPoints(RewardBP);
            
        // Give money
        if (RewardMoney > 0)
            player.AddMoney(RewardMoney);
            
        // Give items
        foreach (ItemTemplate item in FinalRewards)
            GiveItem(player, item);
    }
    
    // Update quest status
    CharDataQuest.Step = 0;
    CharDataQuest.Count++;
    
    // Remove from active quests
    player.RemoveQuest(this);
    player.AddFinishedQuest(this);
    
    return true;
}
```

## Special Quest Types

### Collection Quests
- No quest log entry
- Repeatable turn-ins
- Instant rewards
- No quest steps

```csharp
if (StartType == eStartType.Collection)
{
    // Give reward immediately
    GiveReward(player, item);
    
    // Track completion count
    CharDataQuest.Count++;
    
    // No quest log update needed
    return true;
}
```

### Reward Quests
- Shows reward window
- Player chooses optional rewards
- Must end with InteractFinish

```csharp
if (StartType == eStartType.RewardQuest)
{
    // Show reward selection window
    player.Out.SendQuestRewardWindow(npc, player, this);
    
    // Wait for player choice
    // FinishQuest called after selection
}
```

### Kill Complete Quests
- One-time kill quests
- No quest log entry
- Instant completion

```csharp
if (StartType == eStartType.KillComplete)
{
    // Complete immediately on kill
    FinishQuest(target, true);
}
```

## Quest Rewards

### Standard Rewards
```csharp
public class QuestRewards
{
    long RewardMoney           // Copper value
    long RewardXP             // Experience points
    int RewardRP              // Realm points
    int RewardBP              // Bounty points
    int RewardCLXP            // Champion XP
    int RewardML              // Master level XP
}
```

### Item Rewards
```csharp
// Final rewards (always given)
List<ItemTemplate> FinalRewards

// Optional rewards (player chooses)
List<ItemTemplate> OptionalRewards
int NumOptionalRewardsChoice  // How many to choose
```

### Reward Scaling
- Experience scales with level
- Money may scale with level
- Items are fixed rewards

## Quest Storage

### Active Quests
```csharp
// Player quest list
Dictionary<AbstractQuest, byte> QuestList

// Maximum concurrent quests: 25
const int MAX_QUESTS = 25;
```

### Quest Progress
```csharp
public class CharacterXDataQuest
{
    string Character_ID    // Player ID
    int DataQuestID       // Quest ID
    short Step            // Current step
    short Count           // Times completed
}
```

### Finished Quests
- Stored in database
- Used for dependencies
- Tracks completion count

## Quest Communication

### Quest Dialog
```csharp
// Offer text
SendMessage(player, Description, 0, eChatType.CT_System, eChatLoc.CL_PopupWindow);

// Step text
SendMessage(player, StepText, 0, eChatType.CT_System, eChatLoc.CL_SystemWindow);

// Completion text
SendMessage(player, FinishText, 0, eChatType.CT_System, eChatLoc.CL_PopupWindow);
```

### Quest Updates
```csharp
// Update quest log
player.Out.SendQuestUpdate(quest);

// Update quest list
player.Out.SendQuestListUpdate();

// Update NPC indicators
player.Out.SendNPCsQuestEffect(npc, indicator);
```

## Custom Quest Steps

### Interface
```csharp
public interface IDataQuestStep
{
    bool Execute(DataQuest dataQuest, GamePlayer player, 
                int step, eStepCheckType stepCheckType);
}
```

### Check Types
```csharp
public enum eStepCheckType
{
    Qualification,    // Can player do quest
    Offer,           // Offering quest
    GiveItem,        // Giving quest item
    Step,            // Executing step
    Finish,          // Finishing quest
    RewardsChosen,   // Rewards selected
    PostFinish       // After completion
}
```

## Quest Commands

### Player Commands
- **/quest** - Show quest log
- **/quest abort** - Abandon quest
- **/quest journal** - Detailed view

### Quest Actions
- **Accept**: Start quest
- **Decline**: Refuse quest
- **Progress**: Complete objectives
- **Turn In**: Complete quest

## Test Scenarios

### Basic Quest Flow
```
1. Player meets requirements
2. NPC shows yellow (!)
3. Player interacts, accepts quest
4. Quest added to log
5. Player completes objectives
6. NPC shows yellow (?)
7. Player turns in quest
8. Rewards given
9. Quest marked complete
```

### Collection Quest
```
1. Player has required items
2. Talks to NPC
3. Items removed
4. Rewards given immediately
5. No quest log entry
6. Can repeat if Count < MaxCount
```

### Quest Chain
```
1. Complete Quest A
2. Quest B becomes available
3. NPC indicator updates
4. Continue chain
```

## Configuration

### Quest Properties
```csharp
AllowedClasses      // Class restrictions
MaxQuestCount       // Repeat limit (-1 = infinite)
QuestDependencies   // Required quests
SearchAreas         // For search quests
```

### Quest Limits
- **Active Quests**: 25 maximum
- **Quest Steps**: Unlimited
- **Rewards**: Server configurable
- **Level Range**: Per quest

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added quest types and structure
- Documented reward system
- Added custom step interface

## References
- `GameServer/quests/QuestsMgr/DataQuest.cs`
- `GameServer/quests/QuestsMgr/AbstractQuest.cs`
- `GameServer/quests/QuestsMgr/IDataQuestStep.cs`
- `GameServer/gameobjects/GamePlayer.cs` 