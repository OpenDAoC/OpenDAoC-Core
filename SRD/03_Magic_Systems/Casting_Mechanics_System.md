# Casting Mechanics System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview

**Game Rule Summary**: The casting system controls how you actually cast spells - how long they take, how much mana they cost, whether you can be interrupted, and how far they can reach. Unlike swinging a weapon which happens instantly, spells require time to cast and can be disrupted by taking damage or moving. You also have limited concentration to maintain ongoing spells, and some spells have special requirements like instruments for songs or focus items for reduced mana costs.

The casting mechanics system manages all aspects of spell casting including cast time calculation, power costs, interruption mechanics, concentration management, and spell range calculations. It handles both player and NPC casting with various modifiers and special cases.

## Core Mechanics

### Cast Time Calculation

**Game Rule Summary**: Cast time is how long you must stand still and channel before a spell takes effect. Your Dexterity and casting speed bonuses from items make you cast faster, but there's a limit - spells can't be cast faster than 40% of their base time. Some spells are instant, while special abilities like Quick Cast let you cast any spell instantly at the cost of double mana.

#### Base Cast Time
```csharp
BaseCastTime = Spell.CastTime // In milliseconds from database
```

#### Modified Cast Time
```csharp
DexterityModifier = 1 - (Dexterity - 60) / 600
CastingSpeedModifier = 1 - CastingSpeed / 100
FinalCastTime = BaseCastTime * DexterityModifier * CastingSpeedModifier
```

**Minimum Cast Time**:
```csharp
MinCastTime = BaseCastTime * 0.4 // 40% of base
FinalCastTime = Max(FinalCastTime, MinCastTime)
```

#### Special Cast Times
- **Instant Cast**: `CastTime <= 0`
- **Quick Cast**: `0` cast time, costs double power
- **Chamber Spells**: `0` cast time
- **Focus Pull**: `0` cast time
- **Songs**: Modified by instrument quality/condition

**Source**: `GameLiving.cs:CalculateCastingTime()`

### Power Cost System

**Game Rule Summary**: Every spell consumes mana (power) to cast. Some spells cost a percentage of your total mana pool (dangerous for high-level casters), while others cost a fixed amount. Focus casters can specialize in staff magic to reduce the mana cost of spells in their chosen schools. Special abilities and realm abilities can occasionally make spells cost no mana at all, while Quick Cast always doubles the mana cost.

#### Base Power Calculation
```csharp
if (Spell.Power < 0) // Percentage of max mana
{
    if (ManaStat != UNDEFINED)
        Cost = MaxMana * Math.Abs(Spell.Power) * 0.01
    else
        Cost = Caster.MaxMana * Math.Abs(Spell.Power) * 0.01
}
else // Absolute value
    Cost = Spell.Power
```

#### Focus Caster Reduction
```csharp
// For classes with IsFocusCaster = true
FocusProperty = SkillBase.SpecToFocus(SpellLine.Spec)
if (FocusProperty != eProperty.Undefined)
{
    FocusBonus = GetModified(FocusProperty) * 0.4
    if (Spell.Level > 0)
        FocusBonus /= Spell.Level
    
    FocusBonus = Clamp(FocusBonus, 0, 0.4)
    FocusBonus *= Min(1, SpecLevel / SpellLevel)
    PowerCost *= 1.2 - FocusBonus // Range: 80%-120% of base
}
```

#### Power Cost Modifiers

**Game Rule Summary**: Several special abilities can modify mana costs. Quick Cast always doubles the cost, while certain realm abilities like Valhalla's Blessing or Fungal Union can occasionally make spells free. Some Warlock secondary spells are always free when used in combination with primary spells.

1. **Quick Cast**: Doubles power cost
2. **Valhalla's Blessing**: 75% chance of 0 cost
3. **Fungal Union**: 50% chance of 0 cost
4. **Arcane Syphon**: Property value % chance of 0 cost
5. **Powerless Effect**: Warlock secondary spells cost 0

**Source**: `SpellHandler.cs:PowerCost()`

### Concentration System

**Game Rule Summary**: Concentration represents your ability to maintain ongoing magical effects. You have a limited concentration pool that increases with level, and each active spell that requires concentration uses some of it. If you don't have enough concentration, you can't cast new concentration spells. Some concentration spells also require you to stay within range of your target or they'll drop automatically.

#### Concentration Requirements
```csharp
if (Spell.Concentration > 0 && Caster is GamePlayer)
{
    if (Caster.Concentration < Spell.Concentration)
        return false; // Cannot cast
}
```

#### Concentration Pool
- **Base Maximum**: `20 + Level / 2`
- **Modified Maximum**: `GetModified(eProperty.MaxConcentration)`
- **Current Used**: Sum of all active concentration effects

#### Concentration Effects
- **Buffs**: Use concentration points
- **Songs**: Use concentration points
- **Focus Spells**: Use concentration points
- **Pulse Spells**: Check every 2.5 seconds for range

**Source**: `SpellHandler.cs:CheckConcentrationCost()`

### Spell Range System

**Game Rule Summary**: Each spell has a maximum range beyond which it won't work. Magical bonuses can extend your spell range, letting you cast from further away. There's always a minimum range of 32 units even with penalties. Self-targeted spells work from any distance, while group spells have a fixed long range to affect party members across the battlefield.

#### Range Calculation
```csharp
Range = Max(32, Spell.Range * GetModified(eProperty.SpellRange) * 0.01)
```

#### Special Range Rules
- **Minimum Range**: 32 units
- **Self Spells**: 0 range
- **Group Spells**: 2000 units (SPELL_RANGE_FOR_GROUPSPELLS)
- **PBAoE Spells**: 0 range (centered on caster)
- **Ground Target**: Uses `Caster.GroundTarget` coordinates

#### Range Modifiers
- **Spell Range Property**: From items/buffs
- **Warlock Range Primer**: Special range bonuses
- **Keep Bonuses**: Realm-specific range increases

**Source**: `SpellHandler.cs:CalculateSpellRange()`

## Interruption Mechanics

**Game Rule Summary**: Most spells can be interrupted by taking damage, forcing you to start over and waste the mana. Melee attacks always interrupt unless you have special protection, while ranged attacks and spell damage have chances to disrupt your casting. Some spells are uninterruptible, and certain abilities like Mastery of Concentration can make you immune to interruption during casting.

### Interrupt Timer System

#### Spell Interrupt Duration
```csharp
// Base interrupt duration property
InterruptDuration = Target.SpellInterruptDuration
```

#### Interrupt Triggers
1. **Melee Attacks**: Always interrupt unless uninterruptible
2. **Ranged Attacks**: 65% chance to interrupt (100% vs players)
3. **Spell Damage**: Interrupts based on `SpellInterruptDuration`
4. **Movement**: Interrupts casting (except while moving casts)

#### Interrupt Immunity
- **Uninterruptible Spells**: `Spell.Uninterruptible = true`
- **Mastery of Concentration**: RA prevents interruption
- **Facilitate Painworking**: Necro pet interrupt immunity
- **Quick Cast**: Cannot be interrupted
- **Distance**: Some spells uninterruptible beyond 200 units

### Interrupt Timer Mechanics

**Game Rule Summary**: When your spell is interrupted, you enter an interrupt timer during which you cannot start casting new spells. This prevents you from immediately trying to cast again after being disrupted. The timer length depends on what interrupted you and your interrupt duration bonuses.

```csharp
if (!Spell.Uninterruptible && !Spell.IsInstantCast)
{
    long remaining = Caster.InterruptRemainingDuration;
    if (remaining > 0 && !IsQuickCasting && !HasMoC)
    {
        // Must wait remaining duration
        return false;
    }
}
```

**Source**: `SpellHandler.cs:CheckBeginCast()`

## Special Casting Mechanics

### Quick Cast System

**Game Rule Summary**: Quick Cast is a special ability that lets you cast your next spell instantly, bypassing the normal cast time. However, it costs double the normal mana and has a long cooldown, so it should be saved for crucial moments when you need to get a spell off immediately despite being under attack.

- **Activation**: Uses QuickCast ability
- **Benefits**: 0 cast time on next spell
- **Costs**: Double power consumption
- **Limitations**: One use per ability cooldown

### Moving While Casting

**Game Rule Summary**: Normally, moving during a spell's cast time will interrupt it and waste the mana. However, some special spells allow you to move while casting them, giving you tactical mobility during the casting process. These are typically shorter-range or self-targeted spells.

- **Move Cast Property**: `Spell.MoveCast = true`
- **Restrictions**: Only certain spells allow movement
- **Interruption**: Moving interrupts normal spells

### Instrument Requirements (Songs)

**Game Rule Summary**: Bard and Minstrel songs require instruments to reach their full potential. Higher quality and better condition instruments extend song durations significantly, while broken or poor instruments reduce their effectiveness. The instrument's level compared to your character level also affects the bonus, encouraging you to upgrade your instruments as you advance.

```csharp
if (Spell.InstrumentRequirement != 0)
{
    DbInventoryItem instrument = Caster.ActiveWeapon;
    if (instrument != null)
    {
        // Duration bonus up to 200%
        Duration *= 1.0 + Min(1.0, instrument.Level / Caster.Level);
        // Quality/condition modifiers
        Duration *= instrument.Condition / instrument.MaxCondition * instrument.Quality / 100;
    }
}
```

### Endurance Costs

**Game Rule Summary**: Most spells use mana, but some classes like Savages use endurance instead. These physical-magic hybrids have different resource management, typically using endurance for their magical abilities. Archery spells also use endurance, representing the physical effort of drawing and aiming magical arrows.

```csharp
// For non-mana casters (e.g., Savages)
EnduranceCost = Spell.IsPulsing ? 0 : 5; // Base endurance cost
// For archery spells
EnduranceCost = Caster.MaxEndurance * (Spell.Power * 0.01);
```

**Source**: `SpellHandler.cs:CalculateEnduranceCost()`

## Casting State Management

### Cast States
```csharp
public enum eCastState
{
    Preparation,    // Initial state
    Casting,       // In progress
    Interrupted,   // Was interrupted
    Finished,      // Successfully completed
    Focusing,      // Focus spell maintaining
    Cleanup        // Cleaning up resources
}
```

### State Transitions
1. **Preparation → Casting**: `CheckBeginCast()` succeeds
2. **Casting → Finished**: Cast time elapses
3. **Casting → Interrupted**: Interrupt occurs
4. **Finished → Focusing**: Focus spells continue
5. **Any → Cleanup**: Spell ends

### Timing System
```csharp
// Cast timer management
CastStartTick = GameLoop.GameLoopTime;
CastTimer = new ECSGameTimer(Caster, CastTimerCallback, CastTime);
```

## Error Conditions

### Cast Prevention
- **Dead Caster**: Cannot cast while dead
- **Insufficient Power**: Must have enough mana/endurance
- **Insufficient Concentration**: Must have concentration points
- **In Interrupt Timer**: Must wait for interrupt to expire
- **Target Issues**: Must have valid target for targeted spells
- **Range Issues**: Target must be in range
- **Line of Sight**: Must have LoS to target (some spells)

### Spell Failures
- **Fumble**: Random chance based on level difference
- **Resist**: Target resists the spell effect
- **Immune**: Target immune to spell type
- **Invalid Target**: Wrong target type for spell

## Implementation Notes

### Performance Considerations
- Cast timers use ECS game timer system
- Interrupt checks optimized for common cases
- Power calculations cached when possible

### Networking
- Cast start/progress sent to client
- Interruptions communicated immediately
- Cast completion triggers effect packets

### Special Cases
- **Pet Casting**: Uses owner's properties in some cases
- **NPC Casting**: Simplified power/concentration rules
- **Relic Bonuses**: Affect power costs and effectiveness
- **Realm Abilities**: Modify casting mechanics

## Test Scenarios

### Basic Casting
1. Normal spell with cast time
2. Instant cast spell
3. Quick cast usage
4. Focus spell maintenance

### Interruption
1. Melee interrupt during cast
2. Spell damage interrupt
3. Movement interruption
4. Immune to interruption

### Power Management
1. Insufficient power prevention
2. Focus caster reduction
3. Percentage power spells
4. Special cost modifiers

### Concentration
1. Concentration limit enforcement
2. Multiple concentration effects
3. Concentration range checking
4. Effect dropping on range loss

### Error Handling
1. Invalid target prevention
2. Range checking
3. Line of sight validation
4. Resource availability

## Cross-System Interactions

### With Combat System
- Interruption on damage
- Attack state affects casting
- Combat timing considerations

### With Effect System
- Concentration effect management
- Buff/debuff during casting
- Effect-based modifications

### With Property System
- Casting speed calculations
- Power cost modifications
- Range modifications

### With Character System
- Stat-based modifiers
- Specialization effects
- Ability interactions 