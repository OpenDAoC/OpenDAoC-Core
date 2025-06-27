# Ranged Attack System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Complete
- **Verification**: Code-verified from AttackComponent.cs, RangeAttackComponent.cs, AttackAction.cs
- **Implementation**: Complete

## Overview

**Game Rule Summary**: Ranged combat with bows, crossbows, and thrown weapons requires you to draw, aim, and fire in sequence. Your archery skill and quickness determine how fast you can shoot, while ammunition type affects your damage, range, and accuracy. Taking any damage while drawing interrupts your shot, making positioning and timing crucial for effective ranged combat.

The ranged attack system handles all projectile-based combat including bows, crossbows, and thrown weapons. It features a multi-phase attack sequence (draw, aim, fire), ammunition mechanics, and complex range/accuracy calculations.

## Core Mechanics

### Attack Phases

#### 1. Draw Phase
- **Duration**: Weapon speed modified by Quickness and Archery Speed
- **Interruption**: Any damage interrupts drawing
- **Maximum Draw Time**: 15 seconds (except crossbows - no limit)
- **Animation**: BowPrepare (0x1A stance)

#### 2. Aim Phase
- **Requirements**: Target in range and line of sight
- **State Check**: Continuous validation during hold
- **Interruption**: Damage cancels aim

#### 3. Fire Phase
- **Projectile Speed**: 1800 units/second
- **Flight Time**: `Distance * 1000 / 1800` milliseconds
- **Animation**: BowShoot with calculated flight duration
- **Damage Application**: Delayed until projectile arrival

### Weapon Types and Ranges

#### Base Ranges (Archery Range 100%)
```
Longbow: 1760 units
Recurved Bow: 1680 units  
Composite Bow: 1600 units
Crossbow: 1200 units
Shortbow: 1200 units
Thrown Weapons: 1160 units (1450 for weighted)
```

#### Range Modifiers
- **Archery Range Property**: Percentage modifier (capped at 32 minimum)
- **Elevation Bonus**: `(AttackerZ - TargetZ) / 2` (max +500 from patch 1.70m)
- **Ammo Range Modifiers** (SPD_ABS bits 2-3):
  - Clout (0): 85% range
  - Standard (1): 100% range
  - Flight (3): 125% range

### Attack Speed Calculations

#### Old Archery System
```
DrawTime = BaseSpeed * (1 - (Quickness - 60) * 0.002) * (1 - ArcherySpeed% / 100)
```

#### New Archery System
```
DrawTime = BaseSpeed * (1 - (Quickness - 60) * 0.002) * (1 - CastingSpeed% / 100)
```

#### Special Attack Speeds
- **Critical Shot**: `BaseSpeed * 2 - (AbilityLevel - 1) * BaseSpeed / 10`
- **Rapid Fire**: `BaseSpeed * 0.5` (minimum 900ms)

### Ammunition System

#### Ammo Types
- **Arrows**: For Longbow, Recurved, Composite bows
- **Bolts**: For Crossbows
- **Thrown**: Self-contained projectiles

#### Ammo Properties (SPD_ABS Encoding)
```
Bits 0-1: Damage Type
  0: Blunt/Light (85% damage)
  1: Bodkin/Medium (100% damage)
  2: Barbed (115% damage - unused)
  3: Broadhead/X-Heavy (125% damage)

Bits 2-3: Range Modifier  
  0: Clout (-15% range)
  1: Standard (no modifier)
  3: Flight (+25% range)

Bits 4-5: Accuracy Modifier
  0: Rough (+15% miss chance)
  1: Standard (no modifier)
  3: Footed (-25% miss chance)
```

#### Ammo Management
- **Storage**: Quiver slots (4) or backpack
- **Selection**: Active quiver slot or auto-find in backpack
- **Consumption**: 1 arrow per shot unless Arrow Recovery
- **Arrow Recovery**: `100 - ArrowRecovery%` chance to consume

### Damage Calculation

#### Base Damage
Same as melee with ranged weapon:
```
BaseDamage = WeaponDPS * Speed * 0.1 * SlowWeaponModifier * TwoHandedModifier
```

#### Ammo Damage Modifier
Based on SPD_ABS bits 0-1:
```
0: 85% damage (Light)
1: 100% damage (Bodkin)
2: 115% damage (Barbed - unused)
3: 125% damage (Broadhead)
```

#### Critical Shot
```
Effectiveness = 2.0 + (CasterLevel - TargetLevel) * 0.075
Clamped between 1.1 and 2.0
```

#### Sure Shot
- 50% damage effectiveness
- Cannot miss

#### Rapid Fire
- 50% attack speed
- Damage scales with charge time if interrupted early

### Accuracy System

#### Base Miss Chance
Same as melee plus ammo modifiers:
```
MissChance = BaseMissChance * AmmoAccuracyModifier
```

#### Ammo Accuracy Modifiers
- **Rough**: `MissChance * 1.15` (+15%)
- **Standard**: No change
- **Footed**: `MissChance * 0.75` (-25%)

### Endurance Costs
- **Normal Shot**: 5 endurance
- **Critical Shot**: 10 endurance
- **Volley**: 15 endurance
- **Rapid Fire (Rank 2)**: 3 endurance (rounded up from 2.5)

### Special Abilities

#### Critical Shot
- **Levels**: 1-9
- **Draw Time**: 2x base - (level-1) * 10%
- **Damage**: Enhanced by level difference
- **Endurance**: 10
- **Cannot critically hit**

#### Rapid Fire
- **Draw Time**: 50% of base
- **Minimum Speed**: 900ms
- **Damage**: Scales with charge time
- **Rank 2**: 50% endurance cost

#### Sure Shot
- **Effect**: Cannot miss
- **Damage**: 50% effectiveness

#### True Shot (Long Shot)
- **Effect**: Extended range
- **Implementation**: Via RangedAttackType.Long

#### Volley
- **Type**: Ground-targeted AoE
- **Endurance**: 15
- **Mechanics**: Separate implementation

### Ranged Attack States
```csharp
public enum eRangedAttackState
{
    None,
    ReadyToFire,
    Fire,
    AimFire,
    AimFireReload
}
```

### Ranged Attack Types
```csharp
public enum eRangedAttackType
{
    Normal,
    Critical,
    RapidFire,
    SureShot,
    Long,
    Volley
}
```

## System Interactions

### With Interrupt System
- Drawing interrupted by any damage
- Self-interrupt on successful shot
- Aim phase can be held indefinitely (except fatigue)

### With Stealth System
- Firing breaks stealth
- Critical Shot has reduced break radius
- Camouflage affects detection

### With Line of Sight
- Continuous LoS checks during aim
- Projectile path calculation
- Obstacle detection

### With Animation System
- BowPrepare (0x1A) for draw
- BowShoot with flight duration
- Special handling for thrown weapons

## Implementation Notes

### Projectile Flight
```csharp
FlightDuration = Distance * 1000 / PROJECTILE_FLIGHT_SPEED;
// Animation stance encodes flight time:
// 1 = ~350ms (minimum)
// Each increment adds ~75ms
byte stance = (byte)(ticksToTarget > 350 ? 1 + (ticksToTarget - 350) / 75 : 1);
```

### NPC Ranged Behavior
- Automatic weapon switching based on range
- LoS check interval: `CHECK_LOS_DURING_RANGED_ATTACK_MINIMUM_INTERVAL`
- Archer guards don't pursue melee attackers

### Siege Weapons
Special ranged attackers with unique properties:
- **Ballista**: Anti-siege, 1200-3500 range
- **Catapult**: Area damage, 1000-3600 range  
- **Trebuchet**: Anti-door (3x damage), 2000-5000 range
- **Scorpion**: Rapid fire capability

## Test Scenarios

### Basic Shot Calculation
```
Weapon: Longbow (4.5s)
Quickness: 150
Archery Speed: 20%
Ammo: Standard Bodkin

Draw Time: 4500 * (1 - (150-60) * 0.002) * 0.8 = 2952ms
Range: 1760 units
Damage: 100% (bodkin)
```

### Critical Shot Test
```
Level 5 Critical Shot
Base Speed: 3.5s
Draw Time: 3500 * 2 - (5-1) * 350 = 5600ms
vs Lower Level: Up to 2x damage
```

### Elevation Range Bonus
```
Attacker Height: 1000
Target Height: 500
Elevation Bonus: (1000-500)/2 = 250
Total Range: 1760 + 250 = 2010
```

### Ammo Combination
```
Footed Flight Broadhead Arrow:
- Accuracy: -25% miss chance
- Range: +25%
- Damage: +25%
```

## Edge Cases

### Maximum Draw Duration
- After 15 seconds: "Too tired" message
- Crossbows exempt from fatigue
- Timer resets on release

### Ammo Compatibility
- Arrows incompatible with crossbows
- Bolts incompatible with bows
- Thrown weapons self-contained

### Ground Target Validation
- Used for volley
- Must have valid ground target
- Range calculations from ground point

## Change Log

### 2025-01-20
- Initial documentation created
- Compiled from multiple source files
- Added complete ammo encoding details

## References
- AttackComponent.cs: Core range/damage calculations
- RangeAttackComponent.cs: State management
- AttackAction.cs: Attack execution flow
- Various ability handlers: Special attack implementations 