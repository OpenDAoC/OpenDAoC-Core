# Attack Speed and Timing

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from AttackComponent.cs and related files
- **Implementation Status**: ✅ Fully Implemented

## Overview
Attack speed determines how quickly a character can perform consecutive attacks. The system considers weapon speed, character stats (primarily Quickness), and various modifiers to calculate the actual attack interval.

## Core Mechanics

### Base Attack Speed

#### Weapon Speed
All weapon speeds are stored in SPD_ABS as tenths of seconds:
- **SPD_ABS 30** = 3.0 seconds
- **SPD_ABS 45** = 4.5 seconds

**NPC Default Speeds**:
```
One-Handed: 30 (3.0 seconds)
Two-Handed: 40 (4.0 seconds)
Ranged: 45 (4.5 seconds)
```
**Source**: `AttackComponent.cs:NpcWeaponSpeed()`

#### Speed Formula for Players

**Melee Weapons**:
```
Speed = BaseSpeed * (1 - (Quickness - 60) * 0.002) * MeleeSpeed / 100
```
- **Quickness**: Capped at 250 (soft cap)
- **MeleeSpeed**: Property from buffs/items (100 = normal)
- **Minimum**: 1500ms (1.5 seconds)

**Ranged Weapons (Old Archery)**:
```
Speed = BaseSpeed * (1 - (Quickness - 60) * 0.002)
Speed -= Speed * ArcherySpeed / 100
```

**Ranged Weapons (New Archery)**:
Uses Casting Speed property instead of Archery Speed

**Source**: `AttackComponent.cs:AttackSpeed()`

### Quickness Modifiers

#### Formula Breakdown
```
QuicknessModifier = 1 - (Quickness - 60) * 0.002
```
- At 60 Qui: No modifier (1.0)
- At 160 Qui: 20% faster (0.8)
- At 250 Qui: 38% faster (0.62)

#### Soft Cap
Quickness is soft-capped at 250 for attack speed calculations:
```csharp
int quickness = Math.Min(250, player.Quickness);
```

### Speed Modifiers

#### Melee Speed Property
From items and buffs:
```
FinalSpeed = BaseSpeed * MeleeSpeed / 100
```
- Item bonus capped at 10% (see `MeleeSpeedPercentCalculator.cs`)
- Buffs can exceed this cap

#### Archery Speed Modifiers

**Critical Shot**:
```
Speed = BaseSpeed * 2 - (AbilityLevel - 1) * BaseSpeed / 10
```
- Doubles base speed, reduced by ability level

**Rapid Fire**:
```
Speed = BaseSpeed * 0.5
Minimum = 900ms
```
- 50% speed increase
- **Source**: `RangeAttackComponent.RAPID_FIRE_ATTACK_SPEED_MODIFIER`

### Special Cases

#### Dual Wield Speed
When dual wielding, speed alternates between weapons:
```csharp
switch (UsedHandOnLastDualWieldAttack)
{
    case 2: // Both hands
        speed = (mainWeapon.SPD_ABS + leftWeapon.SPD_ABS) / 2;
        break;
    case 1: // Left hand
        speed = leftWeapon.SPD_ABS;
        break;
    case 0: // Right hand
        speed = mainWeapon.SPD_ABS;
        break;
}
```

#### Pet Attack Speed
Some pets have special modifiers:
```
Amber/Emerald Simulacrum: 145% speed (slower)
Ruby/Sapphire/Jade Simulacrum: 95% speed (faster)
```

### Interrupt Timing

#### Self-Interrupt on Melee
```
InterruptDuration = AttackSpeed / 2
```
- Prevents immediate casting after melee attack
- **Source**: `GameLiving.SelfInterruptDurationOnMeleeAttack`

#### Ranged Interrupt Window
Ranged attacks can be interrupted up to halfway through draw:
```csharp
long halfwayPoint = attackComponent.AttackSpeed(ActiveWeapon) / 2;
if (elapsedTime > halfwayPoint)
    return false; // Cannot interrupt
```

### Combat Round Timing

#### Attack Intervals
- **Successful Attack**: Next attack at weapon speed
- **Fumble**: Double the normal interval
- **Non-attack Round**: 100ms tick

#### Block Rounds
Block effectiveness tied to attacker's speed:
- Each block consumes one "round"
- Shield size determines simultaneous blocks
- Dual wield off-hand doesn't consume rounds

### Speed Caps and Limits

#### Minimum Attack Speeds
- **Melee**: 1500ms (1.5 seconds)
- **Ranged (Rapid Fire)**: 900ms (0.9 seconds)
- **NPC**: 500ms (0.5 seconds)

#### Maximum Speed Reduction
- Theoretical max with 250 Qui + buffs: ~60-70% reduction
- Practical cap depends on available buffs/items

### Projectile Flight Time

#### Calculation
```
FlightTime = Distance * 1000 / PROJECTILE_FLIGHT_SPEED
PROJECTILE_FLIGHT_SPEED = 1800 units/second
```
- Affects arrows, bolts, and thrown weapons
- Damage applied after flight time

## System Interactions

### With Buff System
- Haste buffs reduce MeleeSpeed percentage
- Celerity buffs increase Quickness
- Debuffs can slow attacks

### With Property System
- `eProperty.MeleeSpeed`: Percentage modifier
- `eProperty.ArcherySpeed`: Old archery only
- `eProperty.CastingSpeed`: New archery speed
- `eProperty.Quickness`: Stat affecting speed

### With Style System
- Style execution uses current attack speed
- Endurance cost scaled by weapon speed
- Style chains maintain rhythm

## Implementation Notes

### Database Fields
```sql
SPD_ABS: Weapon speed in tenths of seconds
-- 10 = 1.0 seconds
-- 35 = 3.5 seconds
-- 56 = 5.6 seconds
```

### Combat Animation
- Animation timing synced with attack speed
- Bow prepare animation for ranged
- Different animations for weapon types

## Test Scenarios

### Basic Speed Calculation
```
Given: 3.5s weapon, 150 Quickness, no buffs
QuiModifier: 1 - (150 - 60) * 0.002 = 0.82
Speed: 3500 * 0.82 * 1.0 = 2870ms
```

### Haste Buff Effect
```
Given: 3.0s weapon, 200 Qui, 20% haste
QuiModifier: 1 - (200 - 60) * 0.002 = 0.72
HasteModifier: 0.8 (100 - 20 = 80%)
Speed: 3000 * 0.72 * 0.8 = 1728ms
```

### Rapid Fire Calculation
```
Given: 4.5s bow, any stats
Rapid Fire: 4500 * 0.5 = 2250ms
```

### Speed Cap Test
```
Given: 2.0s weapon, 250 Qui, 30% haste
QuiModifier: 1 - (250 - 60) * 0.002 = 0.62
HasteModifier: 0.7
Speed: 2000 * 0.62 * 0.7 = 868ms
Capped at: 1500ms (minimum)
```

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added all speed formulas and modifiers
- Documented special cases and interactions

## References
- `GameServer/ECS-Components/AttackComponent.cs` - Attack speed calculations
- `GameServer/propertycalc/MeleeSpeedPercentCalculator.cs` - Speed modifiers
- `GameServer/ECS-Components/RangeAttackComponent.cs` - Ranged constants
- Grab bag references in code comments 