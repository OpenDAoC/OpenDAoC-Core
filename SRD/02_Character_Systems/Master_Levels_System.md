# Master Levels System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview

**Game Rule Summary**: Master Levels are advanced character progression for level 50 players, offering 10 additional levels of growth (ML1-ML10) with powerful new abilities. Unlike regular levels, MLs focus on gaining unique magical abilities that cross class boundaries - fighters can learn some spells, casters can gain combat techniques, and everyone gets access to special utilities. Advancing through Master Levels requires completing challenging group content and raid encounters designed for experienced players.

The Master Levels (ML) system provides advanced character progression beyond level 50, offering unique abilities and enhancements through 10 master level tiers (ML1-ML10). Players gain Master Level Experience (MLXP) to advance through each tier and unlock powerful abilities.

## Core Mechanics

### Master Level Progression

**Game Rule Summary**: Master Levels require exactly 32,000 experience each, making progression predictable but challenging. Unlike regular levels that get exponentially harder, each ML takes the same amount of effort, but the experience comes from difficult group content rather than solo play. You must be level 50 to start, and ML progression is completely separate from regular levels and champion levels.

#### Level Range
- **ML1-ML10**: 10 master levels total
- **Prerequisite**: Character Level 50
- **Independent**: ML progression separate from regular levels and champion levels

#### Experience Requirements
**Fixed MLXP per Level**:
```csharp
private static readonly long[] MLXPLevel =
{
    0,     // xp to level 0
    32000, // xp to level 1
    32000, // xp to level 2
    32000, // xp to level 3
    32000, // xp to level 4
    32000, // xp to level 5
    32000, // xp to level 6
    32000, // xp to level 7
    32000, // xp to level 8
    32000, // xp to level 9
    32000, // xp to level 10
};
```

**Key Points**:
- **32,000 MLXP** required for each master level
- **MLXP resets** to 0 after each level gain
- **Total MLXP**: 320,000 required for ML10

### Master Level Lines

**Game Rule Summary**: Each Master Level focuses on a specific theme or playstyle, giving you abilities related to that theme. For example, ML1 (Convoker) is about summoning and pets, while ML4 (Battlemaster) enhances melee combat. This thematic approach means each ML feels distinct and adds different tactical options to your character.

#### Line Structure
Each Master Level has specific ability lines:
```csharp
// GlobalSpellsLines constant
Champion_Lines_StartWith = "ML";
```

#### Line Examples
- **ML1**: Convoker line abilities
- **ML2**: Banelord line abilities  
- **ML3**: Stormlord line abilities
- **ML4**: Battlemaster line abilities
- **ML5**: Spymaster line abilities
- **ML6**: Sojourner line abilities
- **ML7**: Perfecter line abilities
- **ML8**: Warlord line abilities
- **ML9**: Conqueror line abilities
- **ML10**: Realm specific unique lines

### Master Level Abilities

**Game Rule Summary**: Master Level abilities break normal class rules, giving you powers you normally couldn't have. A warrior might learn magical spells, a wizard might gain combat techniques, and everyone gets access to unique utility abilities. These abilities are much more powerful than normal spells and often have effects you can't get anywhere else in the game.

#### Ability Types

**Passive Abilities**:
- Stat enhancements beyond normal caps
- Specialized bonuses for class roles
- Unique properties not available elsewhere

**Active Abilities**:
- Powerful spells and effects
- Class-crossing abilities (casters gain melee abilities, etc.)
- Unique utility and combat abilities

**Line-Specific Abilities**:
- Each ML line focuses on specific themes
- Abilities build upon each other within lines
- Higher ML lines offer more powerful versions

#### Example Master Level Lines

**Game Rule Summary**: Here are the themes for each Master Level to help you understand what kind of abilities you'll gain. Early MLs focus on basic concepts like summoning and damage, while later MLs offer advanced capabilities like leadership, siege warfare, and realm-specific ultimate abilities.

**ML1 - Convoker**:
- Theme: Summoning and pet enhancement
- Abilities: Enhanced summoning, pet buffs, creature command

**ML2 - Banelord**:
- Theme: Damage over time and debuffing
- Abilities: Poison enhancement, disease, withering effects

**ML3 - Stormlord**:
- Theme: Area of effect damage
- Abilities: Storm magic, weather effects, area destruction

**ML4 - Battlemaster**:
- Theme: Melee combat enhancement
- Abilities: Combat techniques, weapon mastery, tactical abilities

**ML5 - Spymaster**:
- Theme: Stealth and reconnaissance
- Abilities: Advanced stealth, detection, information gathering

**ML6 - Sojourner**:
- Theme: Movement and travel
- Abilities: Enhanced speed, teleportation, pathfinding

**ML7 - Perfecter**:
- Theme: Crafting and item enhancement
- Abilities: Master crafting, item improvement, material knowledge

**ML8 - Warlord**:
- Theme: Leadership and group tactics
- Abilities: Group enhancements, tactical commands, battle leadership

**ML9 - Conqueror**:
- Theme: Siege warfare and mass combat
- Abilities: Siege techniques, large-scale combat, fortress assault

**ML10 - Realm-Specific**:
- **Albion**: Unnamed final line
- **Midgard**: Unnamed final line  
- **Hibernia**: Unnamed final line
- Theme: Ultimate realm-specific abilities

### MLXP Acquisition

**Game Rule Summary**: Master Level experience comes from challenging group content like raids and special encounters, not from regular monster hunting. You'll need to organize with other players to tackle Master Level dungeons, artifact encounters, and special events. The content is designed to require teamwork and skill, making ML progression a significant accomplishment.

#### MLXP Sources
- **PvE Content**: Specific master level encounters
- **Raid Content**: Group-based master level dungeons
- **Special Events**: ML-specific events and encounters
- **Quest Rewards**: Master level specific quest chains

#### Group MLXP Distribution
```csharp
// Similar to regular XP distribution
GroupMLXP = BaseMLXP * GroupBonus * LevelDifference
```

### Master Level Encounters

**Game Rule Summary**: Master Level content includes some of the most challenging encounters in the game. Some require small coordinated groups, while others need large raids with multiple groups working together. Many encounters are scheduled events that happen at specific times, and some even require temporary cooperation between enemy realms to complete.

#### Encounter Types
- **Single Group**: 8-person encounters for specific ML abilities
- **Multi-Group**: Large raid encounters for higher ML progression
- **Realm vs Realm**: PvP encounters that grant MLXP
- **Special Events**: Scheduled ML events

#### Encounter Mechanics
- **Artifact Integration**: Many ML encounters involve artifacts
- **Realm Cooperation**: Some encounters require cross-realm temporary cooperation
- **Scheduled Content**: Many ML encounters are on timers or schedules

### Master Level Abilities Integration

#### Spell Line Integration
```csharp
// ML abilities integrated into spell system
public const string Champion_Lines_StartWith = "ML";

// ML spells follow standard spell mechanics
// But with enhanced effects and unique properties
```

#### Ability Restrictions
- **Level Gates**: Must have appropriate ML to use abilities
- **Class Compatibility**: Some abilities restricted by class
- **Realm Restrictions**: ML10 abilities are realm-specific

## System Interactions

### With Character Progression
- **Independent Progression**: ML separate from regular levels
- **No Level Requirement Beyond 50**: Can progress ML at any character level 50+
- **Stacks with Champion Levels**: Both systems can be progressed simultaneously

### With Artifact System
- **Encounter Integration**: Many ML encounters involve artifact mechanics
- **Artifact Enhancement**: Some ML abilities enhance artifact effectiveness
- **Shared Objectives**: ML and artifact progression often overlap

### With Realm System
- **Realm-Specific Content**: ML10 lines are unique per realm
- **Cross-Realm Elements**: Some ML encounters require temporary cooperation
- **RvR Integration**: Some MLXP can be earned through RvR content

### With Spell System
- **Spell Line Integration**: ML abilities use standard spell mechanics
- **Enhanced Effects**: ML spells often exceed normal spell limitations
- **Unique Mechanics**: Some ML abilities have mechanics not found elsewhere

## Implementation Notes

### Database Structure
```csharp
// ML experience tracked separately
public virtual long MLExperience { get; set; }
public virtual int MLLevel { get; set; }

// ML abilities stored in spell lines
// Starting with "ML" prefix
```

### Experience Calculation
```csharp
public long GetMLExperienceForLevel(int level)
{
    if (level <= 0 || level > ML_MAX_LEVEL)
        return 0;
        
    return MLXPLevel[level];
}
```

### Level Up Process
```csharp
public virtual void MLLevelUp()
{
    MLLevel++;
    // Unlock new ML abilities
    // Refresh spell lines starting with "ML"
    // Update character capabilities
    
    Notify(GamePlayerEvent.MLLevelUp, this);
    Out.SendMessage("You have gained one master level!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
}
```

### Ability Availability
```csharp
// ML abilities available based on current ML level
// Higher ML levels unlock more powerful abilities
// Each ML line has progression within it
```

## Test Scenarios

### MLXP Progression
1. **Verify** 32,000 MLXP required per level
2. **Test** MLXP resets to 0 after level gain
3. **Validate** ML progression independent of regular levels

### Ability Unlocking
1. **Test** ML abilities unlock at correct levels
2. **Verify** ML spell lines integrate properly
3. **Validate** ability restrictions by class/realm

### Encounter Integration
1. **Test** MLXP gain from various encounter types
2. **Verify** group MLXP distribution
3. **Validate** ML encounter mechanics

### System Integration
1. **Test** ML interaction with artifact system
2. **Verify** ML abilities don't conflict with other systems
3. **Validate** ML progression saves correctly

## Performance Considerations

### MLXP Calculation
- **Efficient Lookup**: Fixed array for MLXP requirements
- **Minimal Overhead**: Simple addition for MLXP gains
- **Fast Level Check**: Direct integer comparison

### Ability Loading
- **Spell Line Caching**: ML spell lines cached for performance
- **Lazy Loading**: ML abilities loaded only when needed
- **Memory Efficiency**: Shared ability definitions

## Change Log
- Initial documentation based on GamePlayer ML implementation
- Includes MLXP requirements and level progression
- Documents ML line structure and ability types
- Covers system interactions and encounter mechanics 