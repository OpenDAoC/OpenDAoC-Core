# Realm Points and Ranks

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from GamePlayer.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: Realm Points and Realm Ranks are your progression in player vs player combat. Every time you kill an enemy player or capture objectives, you earn Realm Points. As you accumulate points, you advance through Realm Ranks that give you prestigious titles and access to powerful Realm Abilities - special attacks and defenses that make you more effective in PvP combat. Unlike regular levels, there's no cap on Realm Ranks, so dedicated PvP players can continue advancing indefinitely.

Realm Points (RP) and Realm Ranks (RR) represent a character's progression in Realm vs Realm combat. Players earn RP from PvP kills and objectives, advancing through ranks that grant access to powerful Realm Abilities.

## Core Mechanics

### Realm Point Acquisition

**Game Rule Summary**: The realm points you earn from killing an enemy player depend on their level and realm rank. Higher level targets are worth more points, with the biggest reward coming from other level 50 players. Their realm rank also adds bonus points - killing an experienced PvP veteran is worth much more than killing a newcomer. This system encourages fighting worthy opponents rather than picking on weak targets.

#### Player Kill Value
```csharp
// Pre-1.81 formula
ModifiedLevel = TargetLevel - 20
RPValue = Max(1, ModifiedLevel * ModifiedLevel) + TargetRealmLevel
```
- **Level Factor**: Quadratic scaling based on level above 20
- **Realm Level Bonus**: +1 RP per target realm level
- **Minimum**: 1 RP per kill

**Examples**:
- Level 25: (25-20)² + RR = 25 + RR
- Level 35: (35-20)² + RR = 225 + RR  
- Level 50: (50-20)² + RR = 900 + RR

**Source**: `GamePlayer.cs:RealmPointsValue`

#### RP Modifiers

**Game Rule Summary**: Your realm point gain can be increased by server bonuses, special zones, and magical items. Some areas give bonus RP to encourage fighting there, and certain items can boost your RP gain by up to 10%. However, you can turn off RP gain if you want to practice PvP without advancing your rank.

```csharp
// Server rate modifier
Amount *= RP_RATE

// Zone bonuses (if enabled)
ZoneBonus = Amount * GetRPBonus(Zone) / 100

// Item/buff bonuses (capped at 10%)
RPBonus = GetModified(eProperty.RealmPoints)
Amount += Amount * Min(10, RPBonus) / 100
```

### Realm Rank Progression

**Game Rule Summary**: Realm Ranks require increasingly massive amounts of realm points. Getting your first few ranks is relatively quick, but reaching the highest ranks requires dedication and skill over many months or years. Each rank has 10 levels (like RR5L0 to RR5L9), and advancing 10 levels gives you a new rank with a new title. The progression gets much steeper after RR10, separating casual PvP players from the true veterans.

#### RP Requirements Table
| Rank | Level | Total RP | RP to Next |
|------|-------|----------|------------|
| 0L0  | 0     | 0        | 0          |
| 0L1  | 1     | 0        | 25         |
| 0L2  | 2     | 25       | 100        |
| 0L3  | 3     | 125      | 225        |
| 0L4  | 4     | 350      | 400        |
| 0L5  | 5     | 750      | 625        |
| 0L6  | 6     | 1,375    | 900        |
| 0L7  | 7     | 2,275    | 1,225      |
| 0L8  | 8     | 3,500    | 1,600      |
| 0L9  | 9     | 5,100    | 2,025      |
| 1L0  | 10    | 7,125    | 2,500      |
| 2L0  | 20    | 61,750   | 10,000     |
| 3L0  | 30    | 213,875  | 22,500     |
| 4L0  | 40    | 513,500  | 40,000     |
| 5L0  | 50    | 1,010,625| 62,500     |
| 6L0  | 60    | 1,755,250| 90,000     |
| 7L0  | 70    | 2,797,375| 122,500    |
| 8L0  | 80    | 4,187,000| 160,000    |
| 9L0  | 90    | 5,974,125| 202,500    |
| 10L0 | 100   | 8,208,750| 902,963    |
| 11L0 | 110   | 23,308,097| 7,873,404  |
| 12L0 | 120   | 66,181,501| 18,280,035 |
| 13L0 | 130   | 187,917,143| -         |

**Source**: `GamePlayer.cs:REALMPOINTS_FOR_LEVEL[]`

#### Level Calculation Formula
For levels beyond the table:
```csharp
if (realmLevel < REALMPOINTS_FOR_LEVEL.Length)
    return REALMPOINTS_FOR_LEVEL[realmLevel];
else
    return (25/3) * (level³) - (25/2) * (level²) + (25/6) * level
```

### Realm Titles

**Game Rule Summary**: Each realm rank gives you a prestigious title that shows your PvP accomplishment to other players. The titles are different for each realm and gender, reflecting the lore and culture of your faction. Advancing from one major rank to the next (like RR1 to RR2) changes your title, making your experience visible to everyone you encounter.

#### Title System
- **Rank 0**: No title
- **Rank 1-13**: Unique titles per realm/gender
- **Title Change**: Every 10 levels (RR 1L0, 2L0, etc.)

#### Title Structure
```
Realm.RR[Rank].Gender
Example: "Albion.RR1.Male" = "Guardian"
```

### Realm Abilities

**Game Rule Summary**: Realm Abilities are special powers you can buy with your earned realm points, giving you advantages in PvP combat. Some are passive benefits like being tougher or more accurate, while others are active abilities you trigger in combat like powerful attacks or defensive moves. The most prestigious abilities require high realm ranks to unlock, giving veteran players access to unique tactical options.

#### RA Points
```csharp
RealmAbilityPoints = TotalRealmPoints - SpentOnAbilities
```
- Points available = Total RP earned
- Spent on RAs reduces available points
- RR5 abilities don't count against total

#### RA Costs
Most abilities follow standard cost progression:
- Level 1: Base cost
- Level 2: Additional cost
- Level 3: Higher additional cost
- Costs vary by ability type

#### RA Types

**Game Rule Summary**: There are three types of Realm Abilities. Passive abilities are always working in the background, like increased health or resistances. Active abilities must be triggered during combat, like special attacks or defensive moves with cooldowns. RR5 abilities are unique powers each class gets at Realm Rank 5, defining their specialized role in group PvP.

1. **Passive Abilities**: Always active
   - Augmented stats
   - Toughness
   - Avoidance bonuses

2. **Active Abilities**: Must be triggered
   - Purge
   - Vanish  
   - Various offensive/defensive abilities

3. **RR5 Abilities**: Class-specific
   - Unlocked at RR5 (50)
   - No RA point cost
   - Unique per class

## System Interactions

### With Combat
- RP earned from PvP kills
- RP value based on target worth
- Group RP splitting mechanics

### With Guilds
- Guild RP accumulation
- Merit points from RR advancement
- Guild bonuses to RP gain

### With Zones
- RvR zone bonuses
- Keep/tower ownership bonuses
- Darkness Falls access

### With Items
- +RP gain bonuses (10% cap)
- RA enhancement items
- RR requirements for items

## Implementation Notes

### Database Storage
```sql
DOLCharacters table:
- RealmLevel: Current RR level (0-130)
- RealmPoints: Total RP earned

DOLCharactersXRealmAbility table:
- Ability tracking per character
```

### Level Up Events
- `RRLevelUp`: Every 10 levels
- `RLLevelUp`: Every single level
- Updates titles and appearance

### RP Gain Restrictions
- Must be level 40+ to toggle RP gain
- `/rp on/off` command
- PvE server special rules

## Test Scenarios

### Kill Value Test
```
Given: Level 50 RR5 target
Calculation: (50-20)² + 50 = 900 + 50 = 950 RP
With 2x rate: 1900 RP
```

### Rank Progression Test
```
Given: 7,124 RP (RR0L9)
Action: Gain 1 RP
Result: 7,125 RP = RR1L0
- New title displayed
- RRLevelUp event fired
```

### Zone Bonus Test
```
Given: 100 RP kill, 50% zone bonus
Base: 100 RP
Zone: +50 RP
Total: 150 RP (before rates)
```

### RA Point Test
```
Given: 100,000 total RP
Spent: 50,000 on RAs
Available: 50,000 RA points
```

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added complete RP tables
- Documented rank progression
- Added RA system overview

## References
- `GameServer/gameobjects/GamePlayer.cs` - Core RP/RR logic
- `GameServer/gameutils/GlobalConstants.cs` - Realm titles
- `GameServer/realmabilities/` - RA implementations
- `GameServer/events/gameobjects/GainedRealmPointsEventArgs.cs` - RP events 