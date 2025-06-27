# Realm Abilities System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview

**Game Rule Summary**: Realm Abilities are your endgame character advancement after reaching level 50. Earned through RvR combat, you spend Realm Points to buy powerful passive bonuses like increased stats and damage, or active abilities like temporary speed boosts and crowd control immunity. Every class gets a unique RR5 ability at realm rank 5.

The Realm Abilities (RA) system provides character advancement beyond level 50 through Realm Points earned in RvR combat. Players can purchase passive and active abilities that enhance their characters' capabilities across all aspects of gameplay.

## Core Mechanics

### Realm Ability Types

#### Passive Abilities (Property Enhancers)
**Always Active Bonuses**:
- **Augmented Stats**: STR, CON, DEX, QUI, INT, PIE, EMP, CHA
- **Mastery Abilities**: Arms, Parrying, Blocking, Pain, Stealth, Archery, etc.
- **Avoidance**: Magic, Melee, Parrying
- **Enhancement**: Health, Mana, Endurance regeneration

#### Active Abilities (Timed)
**Triggered Effects**:
- **Purge**: Remove negative effects
- **Determination**: Immunity to crowd control
- **Speed of Sound**: Temporary speed boost
- **Juggernaut**: Damage reduction
- **Vanish**: Stealth for non-stealth classes

#### RR5 Abilities (Class-Specific)
**Unlocked at RR5 (50)**:
- **No RA Point Cost**: Free upon reaching RR5
- **Class-Unique**: Each class has specific RR5 ability
- **Single Level**: Cannot be enhanced further

### RA Point System

#### Point Acquisition
```csharp
RealmAbilityPoints = TotalRealmPoints - SpentOnAbilities
```
- **All RP earned** can be spent on RAs
- **RR5 abilities** don't count against total
- **Respec available** with `/respec realm` command

#### Cost Progression (Standard 5-Level RAs)
```csharp
// NEW_PASSIVES_RAS_SCALING = true
Level 1: 1 point
Level 2: 3 points (total: 4)
Level 3: 6 points (total: 10)
Level 4: 10 points (total: 20)
Level 5: 14 points (total: 34)

// Legacy scaling
Level 1: 1 point
Level 2: 3 points (total: 4)
Level 3: 6 points (total: 10)
Level 4: 10 points (total: 20)
Level 5: 14 points (total: 34)
```

#### Cost Progression (3-Level Active RAs)
```csharp
Level 1: 3 points
Level 2: 6 points (total: 9)
Level 3: 10 points (total: 19)
```

### Ability Prerequisites

#### Augmented Stat Requirements
Many abilities require specific Augmented stat levels:
```csharp
// Examples from AtlasOF system
Mastery of Arms: AugmentedSTR >= 2
Mastery of Archery: AugmentedDEX >= 3
Mastery of Magic: AugmentedINT >= 2
Dodger: AugmentedQUI >= 2
```

#### Level Requirements
```csharp
// RR5 abilities
public bool CheckRequirement(GamePlayer player)
{
    return player.RealmLevel >= 40; // RR5 = level 40
}
```

### Specific Realm Abilities

#### Augmented Stats
**Value Scaling (NEW_PASSIVES_RAS_SCALING)**:
```csharp
Level 1: 15 stat points
Level 2: 30 stat points  
Level 3: 45 stat points
Level 4: 63 stat points
Level 5: 81 stat points
```

#### Mastery of Arms
**Enhancement**: Weapon damage
```csharp
Level 1: 3% damage increase
Level 2: 6% damage increase
Level 3: 9% damage increase
Level 4: 13% damage increase
Level 5: 17% damage increase
```

#### Mastery of Archery
**Enhancement**: Archery speed
```csharp
Level 1: 3% speed increase
Level 2: 6% speed increase
Level 3: 9% speed increase
Level 4: 13% speed increase
Level 5: 17% speed increase
```

#### Mastery of Magic
**Enhancement**: Spell damage
**Prerequisite**: Augmented Acuity >= 2
```csharp
Level 1: 3% spell damage
Level 2: 6% spell damage
Level 3: 9% spell damage
Level 4: 13% spell damage
Level 5: 17% spell damage
```

#### Toughness
**Enhancement**: Health and constitution resistance
```csharp
Level 1: 3% health, 1% con resist
Level 2: 6% health, 2% con resist
Level 3: 9% health, 3% con resist
Level 4: 13% health, 5% con resist
Level 5: 17% health, 7% con resist
```

#### Avoidance of Magic
**Enhancement**: Magic resist chance
```csharp
Level 1: 2% magic avoidance
Level 2: 4% magic avoidance
Level 3: 6% magic avoidance
Level 4: 9% magic avoidance
Level 5: 12% magic avoidance
```

#### Wild Power
**Enhancement**: Power pool
```csharp
Level 1: 3% power increase
Level 2: 6% power increase
Level 3: 9% power increase
Level 4: 13% power increase
Level 5: 17% power increase
```

#### Mastery of Stealth
**Enhancement**: Stealth effectiveness
**Prerequisite**: Augmented Quickness >= 2
**Max Level**: 3
```csharp
Level 1: 5% stealth bonus
Level 2: 10% stealth bonus
Level 3: 15% stealth bonus
```

#### Dodger
**Enhancement**: Evade chance
**Prerequisite**: Augmented Quickness >= 2
```csharp
Level 1: 3% evade chance
Level 2: 6% evade chance
Level 3: 9% evade chance
Level 4: 13% evade chance
Level 5: 17% evade chance
```

### Active Abilities

#### Purge
**Effect**: Removes negative effects
**Duration**: Instant
**Reuse**: 15 minutes
**Levels**: 3
```csharp
Level 1: Removes 1 negative effect
Level 2: Removes 2 negative effects  
Level 3: Removes all negative effects
```

#### Determination
**Effect**: Immunity to mezz/stun/fear
**Duration**: Variable by level
**Reuse**: 10 minutes
**Cost**: 3/9/19 points

#### Speed of Sound
**Effect**: 50% speed increase
**Duration**: Variable by level
**Reuse**: 10 minutes
**Special**: Immune to movement impairing effects

#### Juggernaut
**Effect**: Damage reduction
**Duration**: Variable by level
**Reuse**: 15 minutes

### RR5 Abilities by Class

#### Albion RR5s
- **Armsman**: Warrior's Wrath - AoE stun
- **Paladin**: Consecrate Armor - Damage shield
- **Mercenary**: Flawless Strike - Cannot be blocked/parried/evaded
- **Reaver**: Grim Revenge - Health sacrifice for damage
- **Infiltrator**: Shadowstrike - Stealth attack with guaranteed crit
- **Scout**: Camouflage - Enhanced stealth
- **Minstrel**: Warsong - Group speed and melee bonus
- **Theurgist**: Wrath of Champions - Pet enhancement
- **Wizard**: Volcanic Pillar - High damage area spell
- **Sorcerer**: Ruination - Resistance debuff + damage
- **Necromancer**: Lichform - Become undead with immunities
- **Cabalist**: Vaporize - Instant high damage
- **Cleric**: Wrath of Heaven - Damage based on missing health
- **Friar**: Wrath of the Saints - Staff enhancement
- **Heretic**: Heresy - Convert enemy to ally temporarily

#### Midgard RR5s
- **Warrior**: Wrath of the Champion - Damage bonus
- **Thane**: Call of Might - Lightning hammer throw
- **Valkyrie**: Call of Odin - Resurrect fallen allies
- **Berserker**: Rampage - Attack speed and damage increase
- **Savage**: Fury of Nature - Enhanced dual wield
- **Skald**: Song of Vigor - Group damage resistance
- **Hunter**: Call of Hunt - Enhanced archery
- **Shadowblade**: Shadowswipe - Multiple target backstab
- **Bonedancer**: Lord of the Dead - Enhanced minion army
- **Runemaster**: Volcanic Spear - Long-range high damage
- **Spiritmaster**: Possess - Control enemy temporarily
- **Warlock**: Greater Nightmare - Fear with damage
- **Healer**: Greater Heal - Instant large group heal
- **Shaman**: Call of Darkness - Damage aura

#### Hibernia RR5s
- **Hero**: Wrath of Nature - Weapon enhancement
- **Champion**: Fury of the Celts - Attack speed increase
- **Blademaster**: Triple Strike - Three quick attacks
- **Ranger**: Call of the Wild - Enhanced archery with pet
- **Nightshade**: Traitor's Dagger - High damage backstab
- **Vampiir**: Blood Rage - Health steal enhancement
- **Eldritch**: Conversion - Mana steal to damage
- **Enchanter**: Mastery of Illusion - Group invisibility
- **Mentalist**: Ethereal Bond - Mind control
- **Animist**: Fury of Nature - Enhanced pet casting
- **Valewalker**: Rooted in Earth - Damage reflection
- **Druid**: Wrath of Nature - Nature's vengeance
- **Warden**: Call of Hibernia - Group enhancement
- **Bard**: Song of Celerity - Group action speed

## System Interactions

### With Realm Point System
- RA points derived from total RP earned
- RP gain varies by kill value and group participation
- Underpopulated realm bonuses affect RP gain

### With Character Progression
- Available only at level 50+
- Supplements specialization system
- Provides endgame character advancement

### With Combat System
- Many RAs directly affect combat calculations
- Active abilities integrate with normal combat timing
- Prerequisites prevent power stacking

### With Grouping System
- Some RAs provide group benefits
- Group RP distribution affects individual RA gain
- Coordination of group RAs in RvR

## Implementation Notes

### Database Integration
```csharp
[DataTable(TableName = "RealmAbility")]
public class DbRealmAbility : DataObject
{
    public string KeyName { get; set; }
    public string Name { get; set; }
    public string IconID { get; set; }
    public int MaxLevel { get; set; }
    public int CostLvl1 { get; set; }
    public int CostLvl2 { get; set; }
    // ... additional cost levels
}
```

### Class-Specific RA Trees
Each class has specific RA availability defined in:
- `classxrealmability_atlas` database table
- Class-specific RA trees with prerequisites
- Atlas OF vs Legacy RA scaling systems

### Atlas OF System
Modern RA scaling system with:
- Enhanced prerequisite system
- Improved cost progression
- Additional mastery abilities
- Better balance between classes

## Test Scenarios

### RA Point Calculation
1. **Verify** RA points = Total RP - Spent RP
2. **Test** RR5 abilities don't consume points
3. **Validate** respec functionality restores all points

### Prerequisite Testing
1. **Verify** Augmented stat requirements
2. **Test** level requirements for specific RAs
3. **Validate** class restrictions

### Effect Stacking
1. **Test** RA bonuses stack with item bonuses
2. **Verify** RA caps don't exceed intended values
3. **Validate** active ability interactions

### Performance Impact
1. **Benchmark** RA calculation overhead
2. **Test** memory usage with many active RAs
3. **Validate** network packet efficiency

## Change Log
- Initial documentation based on Atlas OF system
- Includes both legacy and modern RA scaling
- Comprehensive coverage of all RA types and mechanics 