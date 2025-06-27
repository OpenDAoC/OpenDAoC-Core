# Specialization and Skills

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from GamePlayer.cs and Specialization.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: Specializations are your character's training in different combat skills like sword fighting, magic schools, or stealth techniques. As you level up, you earn specialization points to spend on improving these skills. Higher specialization levels unlock new abilities, spells, and combat styles, making you more effective in your chosen areas. You have limited points, so you must choose whether to specialize deeply in a few skills or spread your training across many areas.

Specializations are trainable skill lines that define a character's combat abilities, spells, and styles. Players allocate specialization points gained through leveling to increase their effectiveness in chosen areas.

## Core Mechanics

### Specialization Points

**Game Rule Summary**: Every time you level up, you earn specialization points to spend on training. Fighter classes get more points than casters because they need to train multiple weapon skills, while casters get fewer points but can focus them on powerful magic schools. The higher you want to train a skill, the more expensive each level becomes, forcing you to make strategic choices about your character build.

#### Point Formula
```
Levels 1-5:  Level points per level (1+2+3+4+5 = 15 total)
Levels 6+:   Level * SpecPointsMultiplier / 10 per level
Level 40.5:  Level * SpecPointsMultiplier / 20 (half-level bonus)
```

#### Standard Multipliers by Class Type
- **Base Classes**: 10 (1.0x level)
- **Pure Casters**: 10 (1.0x level)
- **Hybrids**: 15-20 (1.5-2.0x level)
- **Pure Melee**: 20 (2.0x level)

**Source**: `CharacterClassBase.cs:SpecPointsMultiplier`

#### Total Points Available

**Game Rule Summary**: By level 50, pure fighters have earned about 95 specialization points total, while pure casters only get about 62 points. This means fighters can train multiple weapon skills to high levels, while casters must focus on fewer magic schools but can max them out more easily. Hybrid classes fall somewhere in between.

At level 50 with standard multiplier (10):
- Base: 15 + (6-50) * 1.0 = 15 + 45 = 60
- Plus half-level at 40: +2 = 62 total
- Hybrids (1.5x): ~77 points
- Pure melee (2.0x): ~95 points

### Specialization Training

**Game Rule Summary**: Training specializations gets expensive fast. Training from level 0 to 1 costs just 1 point, but training from level 49 to 50 costs 50 points. This means the last few levels of a specialization are extremely expensive, encouraging you to train multiple skills to moderate levels rather than maxing out just one skill early.

#### Training Requirements
- Cannot train above character level
- Costs = current level + 1 points
- Must have sufficient points available

#### Point Cost Formula
```csharp
Cost = SpecLevel + 1
// Example: Training from 0->1 costs 1 point
//          Training from 49->50 costs 50 points
```

#### Total Cost to Max
```
Total = (Level * (Level + 1) - 2) / 2
// Level 50: (50 * 51 - 2) / 2 = 1274 points
```

### Autotrain System

**Game Rule Summary**: The autotrain system gives you free training in some specializations based on your level. For every 4 levels you gain, you get one free level in autotrain specializations, which helps offset the basic training costs and lets you focus your earned points on advanced skills. This system ensures you're not completely helpless if you make poor specialization choices early on.

#### Autotrain Levels
```
AutotrainLevel = PlayerLevel / 4 (rounded down)
```

#### Autotrain Points
Free points up to autotrain level:
```csharp
if (spec.Level < max_autotrain)
    FreePoints = ((max_autotrain * (max_autotrain + 1) - 2) / 2) - 
                 ((spec.Level * (spec.Level + 1) - 2) / 2);
```

**Source**: `GamePlayer.cs:GetAutoTrainPoints()`

### Modified Spec Level

**Game Rule Summary**: Your effective specialization level can be higher than what you've actually trained thanks to bonuses from magical items and realm abilities. This lets you meet requirements for higher-level equipment or gain the benefits of advanced training without spending all your points. However, these bonuses don't unlock new spells or abilities - only your actual trained level does that.

#### Calculation
```csharp
ModifiedLevel = BaseLevel + ItemBonus + RealmAbilityBonus
```

#### Special Cases
- Champion lines: Always 50
- Realm spell lines: Equal to player level
- Career specializations: Equal to player level

**Source**: `GamePlayer.cs:GetModifiedSpecLevel()`

### Specialization Types

**Game Rule Summary**: There are three types of specializations. Trainable specs are the normal ones you spend points on like weapon skills and magic schools. Untrainable specs are automatically granted by your class and level. Career specs like Master Level abilities automatically scale with your character level and don't require points.

#### Trainable Specializations
- Standard weapon/magic skills
- Can be trained with points
- Saved in database
- Examples: Slash, Fire Magic, Stealth

#### Untrainable Specializations
- Granted by class/level
- Cannot be trained
- Examples: Some racial abilities

#### Career Specializations
- Level-based progression
- Not saved in database
- Auto-scale with level
- Examples: Some ML abilities

### Skill Acquisition

**Game Rule Summary**: Training specializations doesn't just give you numbers - it unlocks practical abilities. Weapon specializations give you new combat styles and techniques. Magic specializations unlock new spell lines and more powerful spells. Special skills like stealth give you unique abilities like hiding and poisoning weapons. The higher you train, the more powerful tools you gain for your role.

#### Abilities
Granted at specific spec levels:
```csharp
foreach (Ability ab in spec.GetAbilitiesForLiving(this))
{
    if (!HasAbility(ab.KeyName) || GetAbility(ab.KeyName).Level < ab.Level)
        AddAbility(ab, sendMessages);
}
```

#### Styles
Combat styles learned through specialization:
```csharp
foreach (Style st in spec.GetStylesForLiving(this))
{
    styleComponent.AddStyle(st, sendMessages);
}
```

#### Spell Lines
Magic users gain spell lines:
```csharp
foreach (SpellLine sl in spec.GetSpellLinesForLiving(this))
{
    AddSpellLine(sl, sendMessages);
}
```

## System Interactions

### With Level System
- Spec points gained on level up
- Cannot train above character level
- Autotrain scales with level/4

### With Class System
- Classes define spec multiplier
- Class determines available specs
- Some specs class-restricted

### With Item System
- Items can boost spec levels
- Spec requirements for items
- Skill bonuses from gear

### With Combat System
- Weapon skill based on spec
- Style availability from spec
- Damage bonuses from specialization

## Implementation Notes

### Database Storage
```sql
DOLCharacters_specs table:
- DOLCharacters_ID (character reference)
- SpecName (specialization key)
- SpecLevel (trained level)
```

### Spec Keys
Common spec keys defined in `Specs.cs`:
- Weapon: Slash, Thrust, Crush, Polearm
- Magic: Fire_Magic, Ice_Magic, Earth_Magic
- Stealth: Stealth, Critical_Strike
- Archery: Archery, RecurveBow, CompositeBow

### Training Methods
1. **Trainer Window** (1.105+): `SendTrainerWindow()`
2. **Command**: `/train <spec> <level>`
3. **Programmatic**: `AddSpecialization()`

## Test Scenarios

### Basic Training Test
```
Given: Level 10 character with 10 points
Action: Train Slash from 0 to 5
Cost: 1+2+3+4+5 = 15 points
Result: Insufficient points (need 15, have 10)
```

### Autotrain Test
```
Given: Level 20 character, Slash autotrain spec
Autotrain: 20/4 = 5 levels free
Action: Train Slash to 5
Cost: 0 (covered by autotrain)
```

### Modified Level Test
```
Given: Base Slash 45, +5 from items
ModifiedLevel: 50
Result: Full weapon skill effectiveness
```

### Respec Cost Test
```
Given: Slash at level 30
Respec points: (30*31-2)/2 = 464
Minus autotrain: 464 - autotrain points
```

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added point formulas and calculations
- Documented autotrain system
- Added specialization types

## References
- `GameServer/gameobjects/GamePlayer.cs` - Training logic
- `GameServer/gameutils/Specialization.cs` - Spec definitions
- `GameServer/gameutils/SkillBase.cs` - Skill management
- `GameServer/packets/Client/168/PlayerTrainRequestHandler.cs` - Training packets 