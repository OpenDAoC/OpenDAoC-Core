# Character Progression

## Document Status
- **Completeness**: 95% (missing some class-specific progression details)
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from GamePlayer.cs and CharacterClassBase.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
Character progression in DAoC encompasses level advancement, stat gains, and specialization points. Players advance from level 1 to 50, gaining power through increased stats, health, mana, and skill specializations.

## Core Mechanics

### Experience System

#### Experience Requirements
Experience needed for each level (cumulative):
```
Level 1->2:    50
Level 2->3:    250  
Level 3->4:    850
Level 4->5:    2,300
Level 5->6:    6,350
Level 6->7:    15,950
Level 7->8:    37,950
Level 8->9:    88,950
Level 9->10:   203,950
Level 10->11:  459,950
Level 11->12:  839,950
Level 12->13:  1,399,950
Level 13->14:  2,199,950
Level 14->15:  3,399,950
Level 15->16:  5,199,950
Level 16->17:  7,899,950
Level 17->18:  11,799,950
Level 18->19:  17,499,950
Level 19->20:  25,899,950
Level 20->21:  38,199,950
Level 21->22:  54,699,950
Level 22->23:  76,999,950
Level 23->24:  106,999,950
Level 24->25:  146,999,950
Level 25->26:  199,999,950
Level 26->27:  269,999,950
Level 27->28:  359,999,950
Level 28->29:  479,999,950
Level 29->30:  639,999,950
Level 30->31:  849,999,950
Level 31->32:  1,119,999,950
Level 32->33:  1,469,999,950
Level 33->34:  1,929,999,950
Level 34->35:  2,529,999,950
Level 35->36:  3,319,999,950
Level 36->37:  4,299,999,950
Level 37->38:  5,499,999,950
Level 38->39:  6,899,999,950
Level 39->40:  8,599,999,950
Level 40->41:  12,899,999,950
Level 41->42:  20,699,999,950
Level 42->43:  29,999,999,950
Level 43->44:  40,799,999,950
Level 44->45:  53,999,999,950
Level 45->46:  69,599,999,950
Level 46->47:  88,499,999,950
Level 47->48:  110,999,999,950
Level 48->49:  137,999,999,950
Level 49->50:  169,999,999,950
```
**Source**: `GamePlayer.cs:XPForLevel[]`

#### Experience Sources
- `eXPSource.NPC`: Killing monsters
- `eXPSource.Quest`: Quest completion
- `eXPSource.Praying`: Gravestone prayers
- `eXPSource.Other`: Miscellaneous sources

#### Experience Modifiers

**Zone Bonuses**:
- Applied when `ENABLE_ZONE_BONUSES` is true
- Zone-specific XP multipliers
- Source: `ZoneBonus.GetXPBonus()`

**Region Type Modifiers**:
- RvR zones: `RvR_XP_RATE` multiplier
- PvE zones: `XP_RATE` multiplier

**Property Bonus**:
```
XpBonus = GetModified(eProperty.XpPoints)
BaseXP += BaseXP * XpBonus / 100
```

#### Level Progress
Level progress tracked in "bubbles" (1 bubble = 100 permill):
```csharp
LevelPermill = 1000 * (Experience - ExperienceForCurrentLevel) / 
               (ExperienceForNextLevel - ExperienceForCurrentLevel)
```

### Stat Progression

#### Stat Gain Schedule
Stats begin increasing at **level 6**:

- **Primary Stat**: +1 every level (starting at 6)
- **Secondary Stat**: +1 every 2 levels (6, 8, 10, 12...)
- **Tertiary Stat**: +1 every 3 levels (6, 9, 12, 15...)

**Source**: `GamePlayer.cs:OnLevelUp()`

#### Stat Gain Formula
```csharp
for (int i = Level; i > Math.Max(previouslevel, 5); i--)
{
    // Primary stat every level
    if (CharacterClass.PrimaryStat != eStat.UNDEFINED)
        ChangeBaseStat(CharacterClass.PrimaryStat, 1);
        
    // Secondary stat every 2 levels
    if (CharacterClass.SecondaryStat != eStat.UNDEFINED && ((i - 6) % 2 == 0))
        ChangeBaseStat(CharacterClass.SecondaryStat, 1);
        
    // Tertiary stat every 3 levels
    if (CharacterClass.TertiaryStat != eStat.UNDEFINED && ((i - 6) % 3 == 0))
        ChangeBaseStat(CharacterClass.TertiaryStat, 1);
}
```

#### Total Stat Gains (Level 6-50)
- **Primary**: 45 points
- **Secondary**: 22 points  
- **Tertiary**: 15 points

### Specialization Points

#### Point Allocation
```
Levels 1-5:   Level points per level (1+2+3+4+5 = 15 total)
Levels 6-50:  Level * SpecPointsMultiplier / 10 per level
```

#### Standard Multipliers
- Most classes: 10 (1.0x level)
- Some classes may have different multipliers

#### Level 40 Half-Level
At level 40, players gain a "half-level":
- Experience requirement: `(NextLevel - CurrentLevel) / 2`
- Grants: `SpecPointsMultiplier * Level / 20` points
- Resets death penalty
- **Source**: `GamePlayer.cs:OnLevelSecondStage()`

### Health Progression

#### Base Health Formula
```
HP = BaseHP * Level
ConBonus = HP * (Constitution - 50) / 10000
   (if Con < 50, multiply by 2)
ChampionBonus = HPS_PER_CHAMPIONLEVEL * ChampionLevel (if CL >= 1)
TotalHP = 20 + HP / 50 + ConBonus + ChampionBonus
If ExtraHP > 0: TotalHP += TotalHP * ExtraHP / 100
```
**Source**: `GamePlayer.cs:CalculateMaxHealth()`

#### Class Base HP Values
- Light armor classes: ~600-700
- Medium armor classes: ~700-800
- Heavy armor classes: ~800-880
- Varies by specific class

### Mana Progression

#### Base Mana Formula
For classes with mana:
```
MaxMana = max(5, Level * 5 + (ManaStat - 50))
```

**Special Cases**:
- Vampiir: Strength affects mana pool (items only)
- Champions without casting class: 100 mana
- Non-casters: 0 mana

**Source**: `GamePlayer.cs:CalculateMaxMana()`

## System Interactions

### With Class System
- Each class defines Primary/Secondary/Tertiary stats
- Class determines BaseHP and mana stat
- Class-specific OnLevelUp() modifications

### With Death System
- Death count resets on level up
- Death count resets on half-level (40+)
- Experience loss on death based on level

### With Training System
- Skill availability based on level
- Autotrain points: `Level / 4`
- Training restrictions at levels 5, 20, 40

### With Realm System
- Level 20: First realm title and realm point
- Level requirements for realm abilities
- MaxLevel affects RvR calculations

## Implementation Notes

### Level Restrictions
- **Level 5**: Must choose advanced class
- **Level 20**: Can respec once
- **Level 40**: Can respec once, half-level available
- **Level 50**: Maximum level

### Experience Overflow Protection
```csharp
// Cannot lose more XP than current level minimum
if (Experience + expTotal < ExperienceForCurrentLevel)
    expTotal = ExperienceForCurrentLevel - Experience;
```

### Database Persistence
- Experience stored in `DBCharacter.Experience`
- Level stored in `DBCharacter.Level`
- Stats stored individually in character record

## Test Scenarios

### Level Up Test
```
Given: Level 10 with 459,949 XP
Action: Gain 1 XP
Result: Level 11, gain primary stat, spec points
```

### Stat Gain Test
```
Given: Level 11 Armsman (STR/CON/DEX)
Expected gains from 6-11:
- STR: +6 (every level)
- CON: +3 (levels 6,8,10)
- DEX: +2 (levels 6,9)
```

### Half-Level Test
```
Given: Level 40 at minimum XP
Need: (Level41XP - Level40XP) / 2
Result: Level 40.5, gain spec points
```

### Experience Cap Test
```
Given: Any level, attempt to lose XP
Result: Cannot drop below current level minimum
```

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added complete XP tables and formulas
- Documented stat progression mechanics
- Added specialization point calculations

## References
- `GameServer/gameobjects/GamePlayer.cs` - Core progression logic
- `GameServer/gameobjects/CharacterClasses/CharacterClassBase.cs` - Stat definitions
- Experience table from `GamePlayer.XPForLevel[]`
- Various class files for specific stat assignments 