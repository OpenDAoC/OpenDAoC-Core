# Spell Mechanics

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from SpellHandler.cs and Spell.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: Spells in DAoC work like advanced weapon attacks - they have cast times instead of swing speeds, use mana instead of endurance, and can miss or be resisted instead of being blocked or parried. Your magical skill level, stats, and equipment all affect how fast you cast, how much damage you deal, and how reliable your spells are. Different spell types like damage, healing, and crowd control each have their own mechanics and limitations.

Spell mechanics in DAoC govern how magic is cast, its cost, range, damage, and effectiveness. The system balances power consumption, cast time, and specialization to create strategic magical combat.

## Core Mechanics

### Cast Time

**Game Rule Summary**: Cast time is how long it takes to cast a spell, similar to weapon speed for fighters. Higher Dexterity makes you cast faster, and magical items can provide casting speed bonuses. However, spells can't be cast faster than 40% of their base time no matter how much speed you have. Quick Cast abilities let you cast instantly but cost double the mana.

#### Base Cast Time
```
BaseCastTime = Spell.CastTime (in milliseconds)
```

#### Modified Cast Time
```csharp
CastTime = BaseCastTime * DexterityModifier * BonusModifier
DexterityModifier = 1 - (Dexterity - 60) / 600
BonusModifier = 1 - CastingSpeed / 100
FinalCastTime = Max(CastTime, BaseCastTime * 0.4) // 40% minimum
```

**Special Cases**:
- Quick Cast: 0 cast time (costs double power)
- Chamber spells: 0 cast time
- Focus Pull: 0 cast time

**Source**: `GameLiving.cs:CalculateCastingTime()`

### Power Cost

**Game Rule Summary**: Spells consume mana (power) to cast, with more powerful spells costing more. Some spells cost a percentage of your total mana pool, while others cost a fixed amount. Focus specialization reduces the mana cost of spells in that magic school. Special abilities like Quick Cast double the mana cost, while some realm abilities can occasionally make spells cost no mana at all.

#### Base Power Calculation
```csharp
if (PowerCost < 0) // Percentage of max mana
{
    if (ManaStat != UNDEFINED)
        Cost = MaxMana * PowerCost * -0.01
    else
        Cost = Caster.MaxMana * PowerCost * -0.01
}
else // Absolute value
    Cost = PowerCost
```

#### Focus Caster Reduction
```csharp
FocusBonus = GetModified(FocusProperty) * 0.4
if (Spell.Level > 0)
    FocusBonus /= Spell.Level
    
FocusBonus = Clamp(FocusBonus, 0, 0.4)
FocusBonus *= Min(1, SpecLevel / SpellLevel)
PowerCost *= 1.2 - FocusBonus // 80%-120% of base
```

#### Special Modifiers
- Quick Cast: 2x power cost
- Valhalla's Blessing: 75% chance of 0 cost
- Fungal Union: 50% chance of 0 cost
- Arcane Syphon: % chance of 0 cost

**Source**: `SpellHandler.cs:PowerCost()`

### Spell Range

**Game Rule Summary**: Each spell has a maximum range beyond which it cannot be cast. Magical items can extend your spell range, making you more effective at longer distances. Self-targeted spells have no range limit, while area effect spells use their radius instead of range. The minimum spell range is always at least 32 units, even if range penalties would reduce it further.

#### Base Range Calculation
```csharp
Range = Max(32, Spell.Range * GetModified(eProperty.SpellRange) / 100)
```

#### Special Cases
- Self spells: 0 range
- Group spells: 2000 units
- PBAE spells: Uses radius instead
- Item/Artifact spells: Fixed range

### Spell Damage

**Game Rule Summary**: Spell damage is based on the spell's base power, your magical stat (Intelligence, Piety, etc.), and your specialization level in that magic school. Higher magic stats make all your spells hit harder, while specialization makes spells in that school more effective. Damage spells have random variance, so they don't always hit for exactly the same amount - higher specialization gives more consistent damage.

#### Base Damage Calculation
```csharp
BaseDamage = Spell.Damage

// Stat-based modifier
if (Spell.SpellType != Bolt)
    StatBonus = Caster.GetModified(ManaStat)
    Modifier = (StatBonus - 50) / 275.0 // 2.75 per point
    Modifier = Max(0.1, Modifier)
else
    Modifier = 1.0

// Spec-based modifier
SpecBonus = Min(1.2, GetModifiedSpecLevel(Spec) / Level)
FinalBaseDamage = BaseDamage * Modifier * SpecBonus
```

**Source**: `SpellHandler.cs:CalculateDamageBase()`

#### Damage Variance

**Game Rule Summary**: Most spells don't do exactly the same damage every time - there's a random range from minimum to maximum damage. Higher specialization in the spell's school gives you a higher minimum damage, making your spells more reliable. Some special spells like item effects or combat styles have different variance ranges, and a few have no variance at all.

```csharp
// Standard spells
MaxVariance = 1.0
MinVariance = (SpecLevel - 1) / TargetLevel

// Special spell lines
Item Effects: 1.25 max, 0.75 min
Mob Spells: 1.0 max, 0.6 min
Combat Styles: 1.0 max, 1.0 min (no variance)
```

#### Level Difference Modifier
```csharp
if (CasterLevel > TargetLevel)
    Offset = 0.02 + 0.003 * (CasterLevel - TargetLevel)
else
    Offset = 0.02 - 0.02 * (TargetLevel - CasterLevel) / 4.0

MaxVariance += Offset
MinVariance = Clamp(MinVariance + Offset, 0.2, MaxVariance)
```

**Source**: `SpellHandler.cs:CalculateDamageVariance()`

#### Hit Chance Adjustment

**Game Rule Summary**: If your spell has a low chance to hit, the game reduces its damage to compensate. This prevents spells with very poor hit chances from being overpowered when they do occasionally connect. Spells with less than 55% hit chance deal progressively less damage, with very unreliable spells hitting for only 25% of their normal damage.

```csharp
if (HitChance < 55)
    Damage *= 0.25 + 0.5 / 55 * HitChance
```

### To-Hit Calculation

**Game Rule Summary**: Spells can miss just like weapon attacks, but they use different calculations. Higher level casters are more accurate against lower level targets, while casting spells above your level makes them less reliable. Your target's magic resistances reduce your hit chance, and there are hard limits preventing spells from being either completely reliable or completely useless.

#### Base Formula
```csharp
HitChance = 85 + (CasterLevel - TargetLevel) / 2

// Spell level penalty
if (SpellLevel > CasterLevel)
    HitChance -= 10 * (SpellLevel - CasterLevel)

// Resists
HitChance -= TargetResists

// Clamp
HitChance = Clamp(HitChance, 0, 100)
```

**Source**: `SpellHandler.cs:CalculateToHitChance()`

### Duration and Concentration

**Game Rule Summary**: Spell effects last for a specific duration, which can be extended by magical bonuses. Some spells require concentration to maintain - these spells stay active until you cast something else or lose concentration. You have a limited concentration pool, so you can only maintain a few concentration spells at once. Instruments can extend song durations based on the quality and condition of your instrument.

#### Duration Calculation
```csharp
// Positive effects
Duration = Spell.Duration * (1 + SpellDuration / 100)

// Instrument bonus (songs)
if (InstrumentRequirement > 0)
    Duration *= 1 + Min(1.0, InstrumentLevel / CasterLevel)
    Duration *= InstrumentCondition / MaxCondition * Quality / 100

// Resist reduction (negative effects)
Duration -= Duration * ResistBase / 100
```

#### Concentration
- Points consumed: Spell.Concentration
- Maximum: GetModified(eProperty.MaxConcentration)
- Default max: 20 + Level/2

### Resist Calculation

**Game Rule Summary**: Targets can resist your spells based on their magic resistances, reducing the spell's effectiveness or negating it entirely. Higher level targets are naturally harder to affect with magic, while targets with specific resistances (like fire resistance against fire spells) are much harder to damage. The level of your spell compared to the target's resistances determines how often your magic will be resisted.

#### Magic Resists
```csharp
ResistChance = GetResist(DamageType) - SpellLevel
if (ResistChance > 0)
    ResistChance /= 10 // 10% per point over spell level
```

#### Level-Based Resists
```csharp
// Target higher level
if (TargetLevel > CasterLevel)
    ResistChance += (TargetLevel - CasterLevel) * 2
```

### Spell Types and Special Rules

**Game Rule Summary**: Different types of spells work in unique ways. Direct damage spells hit instantly and can critically strike. Damage over time spells tick for their full damage reliably but can't critically hit. Healing spells have variance like damage spells but generally favor higher amounts. Crowd control spells create immunity timers to prevent permanent lockdown, and buffs/debuffs can be dispelled or have their durations reduced by resistance.

#### Direct Damage
- Instant effect
- Subject to variance
- Can critical hit

#### DoT (Damage over Time)
- Ticks every Frequency ms
- No variance adjustment
- Cannot critical hit on ticks

#### Heal Spells
```csharp
// Variance calculation
MaxHeal = SpellValue * 1.25
MinHeal = MaxHeal * Efficiency

// Efficiency based on spec
Efficiency = 0.25 + (SpecLevel - 1) / SpellLevel
Efficiency = Clamp(Efficiency, 0.25, 1.25)
```

#### Buff/Debuff
- Duration affected by resists
- Can be sheared based on level
- Concentration-based have no duration

#### CC (Crowd Control)
- Immunity timer on break
- Duration reduced by resists
- Special PvP duration modifiers

## System Interactions

### With Specialization
- Damage scales with spec level
- Hit chance improved by spec
- Focus specs reduce power cost

### With Stats
- Casting stat affects damage
- Dexterity affects cast speed
- Acuity affects magic damage

### With Items
- Spell range bonuses
- Casting speed bonuses
- Spell damage bonuses
- Focus items reduce cost

### With RvR
- Relic bonuses to damage
- Keep bonuses to range
- RvR-specific damage modifiers

## Implementation Notes

### Spell Database
```sql
Spell table fields:
- SpellID: Unique identifier
- Name: Display name
- Cast_Time: In milliseconds
- Power: Cost (negative = %)
- Range: In game units
- Duration: In milliseconds
- Damage: Base damage value
- SpellType: Type enumeration
```

### Spell Lines
- Baseline: Trainable specs
- List caster: List-based
- Chamber: Warlock specific
- Reserved: Special/RA spells
- Item Effects: Procs/charges

### Interrupt System
- Damage interrupts casting
- Interrupt duration based on damage
- Quick cast cannot be interrupted
- Some spells uninterruptible

## Test Scenarios

### Cast Time Test
```
Given: 3000ms spell, 100 Dex, 10% cast speed
DexMod: 1 - (100-60)/600 = 0.933
BonusMod: 1 - 10/100 = 0.9
Result: 3000 * 0.933 * 0.9 = 2520ms
```

### Power Cost Test (Focus)
```
Given: 50 power spell, 25 focus, level 45 spell
FocusBonus: 25 * 0.4 / 45 = 0.222
Clamped: 0.222
Cost: 50 * (1.2 - 0.222) = 48.9 power
```

### Damage Variance Test
```
Given: Level 50 caster, level 45 target, 50 spec
MaxVar: 1.0 + 0.02 + 0.003*5 = 1.035
MinVar: 49/45 + 0.035 = 1.124 (capped at max)
Result: 103.5% damage always
```

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added all core spell mechanics
- Documented damage calculations
- Added special spell type rules

## References
- `GameServer/spells/SpellHandler.cs` - Core spell mechanics
- `GameServer/spells/Spell.cs` - Spell data structure
- `GameServer/gameobjects/GameLiving.cs` - Cast time calculation
- Various spell handler implementations 