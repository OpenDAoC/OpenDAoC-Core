# Faction System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from Faction.cs, FactionMgr.cs, GameNPC.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: The faction system tracks your reputation with different groups of NPCs throughout the game world. Your actions affect how various NPC factions view you - kill their friends and they'll become hostile, help them and they'll be friendlier. This reputation determines whether NPCs will attack you on sight, trade with you, or offer special services. Different factions have alliances and enemies, so your actions can have wide-reaching consequences across multiple groups.

The Faction System manages relationships between NPCs and players, controlling aggression, interaction availability, and faction-based rewards. Each NPC can belong to a faction with dynamic standing based on player actions.

## Core Mechanics

### Faction Structure

**Game Rule Summary**: Each faction is a group of NPCs that share relationships and reputation with players. Every faction has friends (allies) and enemies, so when you help or harm one faction, it affects your standing with their allies and enemies too. Most NPCs belong to one primary faction, but some complex NPCs might be connected to multiple groups.

#### Faction Properties
```csharp
public class Faction
{
    public string Name { get; }         // Faction display name
    public int Id { get; }              // Unique faction ID
    public int BaseAggroLevel { get; }  // Starting aggro for new players
    public HashSet<Faction> FriendFactions { get; }  // Allied factions
    public HashSet<Faction> EnemyFactions { get; }   // Hostile factions
}
```

#### NPC Assignment
- **Single Faction**: Each NPC belongs to one primary faction
- **Linked Factions**: NPCs can have multiple faction associations
- **Realm Override**: Same-realm NPCs are always friendly regardless of faction

### Standing Calculation

**Game Rule Summary**: Your reputation with each faction ranges from friendly to aggressive. Friendly NPCs won't attack you and offer full services, neutral ones are cautious, hostile ones are unfriendly but won't always attack, and aggressive ones will attack you on sight. Your starting reputation with new factions depends on the faction's base attitude toward outsiders.

#### Standing Levels
| Standing | Aggro Range | Behavior |
|----------|------------|----------|
| **Friendly** | ≤ 25 | No aggro, full interaction |
| **Neutral** | 26-50 | Limited aggro, restricted interaction |
| **Hostile** | 51-75 | Moderate aggro, minimal interaction |
| **Aggressive** | 76-100 | High aggro, combat on sight |

**Source**: `Faction.cs:GetStandingToFaction()`

#### Aggro Value System
```csharp
// Aggro range: -100 to +100
const int MAX_AGGRO_VALUE = 100;
const int MIN_AGGRO_VALUE = -100;
const int INCREASE_AGGRO_AMOUNT = 1;
const int DECREASE_AGGRO_AMOUNT = -1;
```

- **Initial Value**: Faction.BaseAggroLevel for new players
- **Persistent**: Stored per character in database
- **Dynamic**: Changes based on player actions

### Faction Changes

**Game Rule Summary**: When you kill NPCs, there's about a 20% chance it will affect your faction reputation. Killing someone makes their friends like you less and their enemies like you more. These reputation changes spread through faction networks, so one action can affect multiple groups. The game will notify you when your standing with a faction changes.

#### Triggering Events
1. **Kill Faction Member**: +1 aggro to friend factions, -1 to enemy factions
2. **20% Chance**: Only 20% chance of aggro change per kill
3. **Friend/Enemy Relations**: Changes propagate through faction network

#### Change Calculation
```csharp
public void OnMemberKilled(GamePlayer killer)
{
    foreach (Faction faction in FriendFactions)
        faction.ChangeAggroLevel(killer, INCREASE_AGGRO_AMOUNT);
        
    foreach (Faction faction in EnemyFactions)
        faction.ChangeAggroLevel(killer, DECREASE_AGGRO_AMOUNT);
}
```

#### Player Notification
- **Increase**: "Your relationship with [Faction] has decreased"
- **Decrease**: "Your relationship with [Faction] has increased" 
- **Message Type**: CT_System to system window

### Faction Relationships

**Game Rule Summary**: Factions form complex webs of alliances and rivalries. When you help or harm one faction, the effects ripple through their entire network of friends and enemies. This creates meaningful consequences for your actions - you can't just kill anyone without affecting your relationships with multiple NPC groups throughout the world.

#### Friend Factions
- **Auto-Friend**: Every faction is friend to itself
- **Mutual Support**: Killing one affects all friends
- **Shared Standing**: Friend actions impact reputation

#### Enemy Factions
- **Configured Relations**: Set through database (DbFactionLinks)
- **Opposing Behavior**: Enemy deaths improve standing
- **Combat Assistance**: NPCs assist friends against enemies

#### Database Structure
```sql
-- DbFaction: Core faction definition
CREATE TABLE Faction (
    ID INT PRIMARY KEY,
    Name VARCHAR(255),
    BaseAggroLevel INT
);

-- DbFactionLinks: Faction relationships
CREATE TABLE FactionLinks (
    FactionID INT,
    LinkedFactionID INT,
    IsFriend BOOLEAN
);

-- DbFactionAggroLevel: Player-specific standing
CREATE TABLE FactionAggroLevel (
    CharacterID VARCHAR(100),
    FactionID INT,
    AggroLevel INT
);
```

## NPC Behavior Integration

### Aggro Determination
```csharp
// For aggressive NPCs with factions
if (Body.Faction != null)
{
    if (target is GamePlayer player)
        return Body.Faction.GetStandingToFaction(player) is Faction.Standing.AGGRESIVE;
    else if (target is GameNPC npc && Body.Faction.EnemyFactions.Contains(npc.Faction))
        return true;
}
```

### Interaction Restrictions
```csharp
// Hostile+ factions prevent interaction
if (!GameServer.ServerRules.IsSameRealm(this, player, true) && 
    Faction != null && 
    Faction.GetStandingToFaction(player) >= Faction.Standing.HOSTILE)
{
    player.Out.SendMessage("NPC gives you a dirty look", CT_System);
    return false;
}
```

### Examine Messages
Standing affects NPC examine text:
- **Friendly**: "friendly towards you"
- **Neutral**: "neutral towards you" 
- **Hostile**: "hostile towards you"
- **Aggressive**: "aggressive towards you"

## System Interactions

### With Combat System
- **Faction Wars**: Enemy faction NPCs auto-attack each other
- **Friend Assistance**: NPCs assist faction friends in combat
- **Aggro Changes**: PvP kills affect all related factions

### With Realm System
- **Realm Override**: Same realm = always friendly
- **Cross-Realm**: Faction standing applies to enemy realms
- **Neutral NPCs**: No realm restrictions

### With Quest System
- **Quest Availability**: Poor standing may block quests
- **Faction Rewards**: Some quests improve faction standing
- **Faction Requirements**: Certain quests require minimum standing

### With Merchant System
- **Trade Restrictions**: Hostile factions may refuse trade
- **Price Modifiers**: Standing could affect merchant prices
- **Special Items**: Faction-specific merchandise

## Administrative Tools

### GM Commands
```
/faction create <name> <baseAggro>     # Create new faction
/faction assign                        # Assign selected NPC to faction
/faction addfriend <factionID>         # Add friend relationship
/faction addenemy <factionID>          # Add enemy relationship
/faction relations                     # Show faction relationships
/faction list                          # List all factions
/faction select <factionID>            # Select faction for operations
```

### Faction Management
- **Creation**: GMs can create new factions dynamically
- **Assignment**: NPCs can be assigned to factions
- **Relations**: Friend/enemy relationships configurable
- **Debugging**: Comprehensive tools for testing

## Performance Considerations

### Optimization Features
1. **Concurrent Collections**: Thread-safe faction access
2. **Dirty Flagging**: Only save changed aggro levels
3. **Disconnection Cleanup**: Remove offline players from memory
4. **20% Chance Rule**: Reduces database writes

### Database Efficiency
- **Batch Saves**: Periodic faction aggro saves
- **Lazy Loading**: Load aggro on player login
- **Memory Caching**: Keep active player aggro in RAM

## Configuration

### Server Properties
```csharp
// No specific faction server properties found
// Uses general NPC and aggro settings
```

### Faction Creation
```csharp
// Automatic ID assignment
int maxId = FactionMgr.Factions.Keys.Max();
DbFaction newFaction = new()
{
    ID = maxId + 1,
    Name = factionName,
    BaseAggroLevel = baseAggro
};
```

## Edge Cases and Special Rules

### Realm Considerations
- **Same Realm**: Always friendly regardless of faction
- **Enemy Realm**: Faction standing applies
- **Neutral Realm**: Standard faction rules

### NPC Types
- **Merchants**: May refuse trade if hostile
- **Trainers**: Training restricted by faction

### Faction Conflicts
- **Multiple Enemies**: Player can be enemy to multiple factions
- **Friend of Enemy**: Complex relationship chains possible
- **Circular Relations**: System handles complex faction webs

## Testing Scenarios

### Basic Faction Tests
1. **New Player**: Should have BaseAggroLevel standing
2. **Kill Friend**: Should increase aggro to all friend factions
3. **Kill Enemy**: Should decrease aggro to all enemy factions
4. **Standing Persistence**: Faction standing survives logout/login

### Interaction Tests
1. **Hostile Interaction**: Should be blocked
2. **Friendly Interaction**: Should work normally
3. **Examine Text**: Should reflect current standing
4. **Aggro Behavior**: NPCs should aggro based on standing

### Administrative Tests
1. **Faction Creation**: GM commands work correctly
2. **Relationship Management**: Friend/enemy assignments persist
3. **NPC Assignment**: Faction assignment takes effect immediately
4. **Debugging Tools**: All faction info displays correctly

## References
- **Source Code**: `GameServer/gameutils/Faction.cs`
- **Manager**: `GameServer/gameutils/FactionMgr.cs`
- **NPC Integration**: `GameServer/gameobjects/GameNPC.cs`
- **Database**: `CoreDatabase/Tables/DbFaction*.cs`
- **Commands**: `GameServer/commands/gmcommands/Faction.cs` 