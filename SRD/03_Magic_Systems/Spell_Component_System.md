# Spell Component System

## Document Status
- Status: Under Development
- Implementation: Complete

## Overview

**Game Rule Summary**: Spells are organized into baseline and specialization components that determine how magical effects stack together. You can have both a baseline strength buff and a specialization strength buff active at the same time, but you can't stack multiple buffs of the same type from the same category. Non-specialist casters get weaker versions of buffs unless they specialize heavily in the magic line.

The spell component system determines how different types of spell effects stack and interact. Spells are categorized into baseline and specialization components, with specific rules governing which effects can coexist and which overwrite each other.

## Core Mechanics

### Component Categories

#### Baseline Components
**Definition**: Spells available to multiple classes from trainers
- Lower level versions of effects
- Generally less powerful
- Stack within same category

**Examples**:
```
Strength buffs (Trainable by multiple classes)
Base AF buffs (Shield of the Realm line)
Single resist buffs (Guard of Valor line)
```

#### Specialization Components  
**Definition**: Spells requiring specialization points
- Higher level, more powerful versions
- Class-specific implementations
- May override baseline versions

**Examples**:
```
Spec AF buffs (Shaman/Cleric/Friar lines)
Acuity buffs (Sorcerer/Theurgist/Wizard)
Dual-stat buffs (Str/Con, Dex/Qui)
```

### Buff Bonus Categories

#### Category Breakdown
```csharp
public enum eBuffBonusCategory
{
    BaseBuff,      // Baseline buffs
    SpecBuff,      // Specialization buffs  
    Debuff,        // Negative effects
    OtherBuff,     // Uncapped/special buffs
    SpecDebuff,    // Special debuffs
    AbilityBuff    // Ability-based buffs
}
```

#### Category Rules

**BaseBuff Category**:
- Single stat buffs (Str, Con, Dex, etc.)
- Base armor factor buffs
- Base resist buffs
- **Stacking**: Only highest value active
- **Cap**: Combined with item bonuses

**SpecBuff Category**:
- Spec armor factor buffs
- Acuity buffs  
- Dual stat buffs
- **Stacking**: Only highest value active
- **Cap**: Separate from base buffs

**Debuff Category**:
- All stat debuffs
- Resist debuffs
- Armor debuffs
- **Stacking**: Only highest value active
- **Values**: Stored as positive, subtracted

**OtherBuff Category**:
- Uncapped effects (Paladin AF chants)
- Special buffs bypassing limits
- **Stacking**: Additive with other categories
- **Cap**: None

### Stacking Rules

#### Same Component Type
When two effects of the same component type are applied:

```
1. Compare spell values (considering effectiveness)
2. If new > existing:
   - Replace existing effect
3. If existing > new:
   - Reject new effect (same caster)
   - Add as disabled (different caster)
```

#### Different Component Types
Base and Spec components can coexist:

```
Example: Strength Buff Stacking
- Base Strength buff: +20 (BaseBuff category)
- Spec Str/Con buff: +25/+25 (SpecBuff category)
- Result: +45 Strength (+20 base, +25 spec)
```

#### Overwriting Rules

**Spell.IsOverwritable() Logic**:
```csharp
// Same effect group overwrites
if (Spell.EffectGroup != 0 || compare.EffectGroup != 0)
    return Spell.EffectGroup == compare.EffectGroup;

// Same spell type check
if (compare.SpellType != Spell.SpellType)
    return false;

// Special handling per spell type
// Duration vs Concentration handling
// Baseline vs Spec line handling
```

### Effectiveness Scaling

#### List Caster Buffs
**Classes**: Sorcerer, Theurgist, Wizard, Cabalist, etc.
- Fixed 100% effectiveness
- No scaling with spec level

#### Non-List Caster Buffs  
**Classes**: Shaman, Cleric, Druid, etc.
- Scale with specialization level
- Formula: `0.75 + (SpecLevel - 1) * 0.5 / SpellLevel`
- Range: 75% to 125% effectiveness

#### Examples
```
Level 20 Str buff, 40 spec:
Effectiveness = 0.75 + (40 - 1) * 0.5 / 20
             = 0.75 + 39 * 0.025
             = 0.75 + 0.975
             = 1.725 (capped at 1.25)
             = 125% effectiveness

Level 40 AF buff, 20 spec:
Effectiveness = 0.75 + (20 - 1) * 0.5 / 40
             = 0.75 + 19 * 0.0125
             = 0.75 + 0.2375
             = 0.9875
             = 98.75% effectiveness
```

### Special Component Rules

#### Armor Factor Buffs
```
Base AF (BaseBuff category):
- Capped with item AF bonuses
- Formula: ItemAF + BaseAF ≤ Level * 1.875

Spec AF (SpecBuff category):
- Separate cap from base
- Formula: SpecAF ≤ Level * 1.875

Paladin AF (OtherBuff category):
- Uncapped when chanted
- Stacks with both base and spec
```

#### Resist Buffs
```
Single Resists:
- Base versions in BaseBuff category
- Only highest active per resist type

Combo Resists (HCM/BSE):
- Each component in appropriate category
- Effectively 3 separate buffs

All Magic Resists:
- 6 separate buff components
- Can be partially overwritten
```

#### Stat Combinations
```
Single Stats (Str, Con, Dex, etc.):
- BaseBuff category
- Baseline component

Dual Stats (Str/Con, Dex/Qui):
- SpecBuff category  
- Both stats in same category
- Spec component

Triple+ Stats:
- Multiple categories used
- Each stat in defined category
```

### Concentration Effects

#### Component Interaction
- Cannot have multiple concentration effects
- Concentration buffs follow normal component rules
- Range-based disabling doesn't affect stacking

#### Special Cases
```
Endurance Regen:
- 1500 unit range (not BUFF_RANGE)
- Tighter leash than other buffs

Paladin Chants:
- Concentration-based
- OtherBuff category (uncapped)
```

### Effect Groups

Effect groups create stacking relationships between different spells:

#### Common Groups
```
Group 1: Base AF buffs
Group 2: Spec AF buffs
Group 4: Strength buffs
Group 200: Acuity buffs
Group 201: Constitution buffs
Group 99999: Special damage adds
```

#### Group Behavior
- Same group = same stacking rules
- Different spell types can share groups
- Allows fine control over interactions

## System Interactions

### Property Calculation
Each property sums categories independently:
```
Final Value = Base
            + ItemBonus (capped)
            + BaseBuffCategory (capped with items)
            + SpecBuffCategory (separate cap)
            - DebuffCategory
            + OtherBonusCategory (uncapped)
            + AbilityBonus
```

### Combat System
- Damage adds check stacking order
- Highest damage adds apply first
- 50% effectiveness for subsequent adds

### PvP Considerations
- No special component rules in PvP
- Standard effectiveness applies
- Debuff critical chance active

## Implementation Notes

### Component Resolution
1. Determine spell component type
2. Assign appropriate bonus category
3. Check for existing effects
4. Apply stacking rules
5. Update property calculations

### Database Storage
- Component type inferred from spell
- Effect group stored in spell definition
- Bonus category determined at runtime

### Client Display
- Only active effects shown
- Highest value per component displayed
- Disabled effects hidden

## Test Scenarios

### Basic Stacking
1. Apply base Str buff (+20)
2. Apply spec Str/Con buff (+25/+25)
3. Verify +45 Str, +25 Con

### Overwriting
1. Cast 20 Str buff
2. Cast 25 Str buff
3. Verify only 25 active

### Mixed Sources
1. Player A: Cast base AF buff
2. Player B: Cast stronger base AF
3. Verify B's buff active, A's disabled

### Effect Groups
1. Cast spell with effect group 4
2. Cast different spell, same group
3. Verify overwrite behavior

## Change Log
- Initial documentation
- Added component categories
- Documented effectiveness scaling
- Added effect group system 