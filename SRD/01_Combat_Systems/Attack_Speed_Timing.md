# Attack Speed and Timing

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from AttackComponent.cs and related files
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: Attack speed determines how fast you can swing your weapon or shoot your bow. Faster characters (high Quickness) attack more frequently, and lighter weapons are naturally quicker than heavy ones. Various magical effects can make you attack faster or slower. There are minimum speed limits to prevent attacks from becoming impossibly fast.

Attack speed determines how quickly a character can perform consecutive attacks. The system considers weapon speed, character stats (primarily Quickness), and various modifiers to calculate the actual attack interval.

## Core Mechanics

### Base Attack Speed

**Game Rule Summary**: Every weapon has a base speed that determines how long it takes to swing. This is measured in seconds - a 3.0 second sword means you can attack every 3 seconds with that weapon. Heavier weapons like two-handed swords and crossbows naturally take longer to use than lighter weapons like daggers and shortbows.

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

**Game Rule Summary**: Your actual attack speed is faster than the weapon's base speed if you have high Quickness. Agile characters can use weapons much more efficiently than clumsy ones. Magical speed enhancements further reduce the time between attacks, but there are limits to prevent impossibly fast attacking.

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

**Game Rule Summary**: Quickness makes you attack faster in a predictable way. Characters with 60 Quickness attack at normal weapon speed, while characters with higher Quickness get proportionally faster. The benefit caps at 250 Quickness to prevent excessive speed advantages.

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

**Game Rule Summary**: Magical effects can make you attack faster through haste spells and speed enchantments. These stack with your natural Quickness to make you even faster. Items have caps on how much speed they can provide, but spell effects can exceed these limits.

#### Melee Speed Property
From items and buffs:
```
FinalSpeed = BaseSpeed * MeleeSpeed / 100
```
- Item bonus capped at 10% (see `MeleeSpeedPercentCalculator.cs`)
- Buffs can exceed this cap

#### Archery Speed Modifiers

**Game Rule Summary**: Archers have special abilities that dramatically change their attack timing. Critical Shot makes you draw much more slowly for a guaranteed hit, while Rapid Fire lets you shoot much faster but with normal accuracy. These abilities trade speed for accuracy or vice versa.

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

**Game Rule Summary**: When fighting with two weapons, your attack speed alternates between your main weapon and off-hand weapon. Sometimes you attack with both weapons simultaneously, which uses the average speed of both weapons. This creates a unique rhythm for dual wielders.

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

**Game Rule Summary**: After making a melee attack, there's a brief period where you can't start casting spells because you're recovering from the swing. This prevents instant spell-casting after weapon attacks and makes combat timing more tactical.

#### Self-Interrupt on Melee
```
InterruptDuration = AttackSpeed / 2
```
- Prevents immediate casting after melee attack
- **Source**: `GameLiving.SelfInterruptDurationOnMeleeAttack`

#### Ranged Interrupt Window

**Game Rule Summary**: When drawing a bow or crossbow, you can be interrupted up to the halfway point of your draw. After that, you're committed to the shot and can't be stopped. This creates tactical timing for both archers and their enemies.

Ranged attacks can be interrupted up to halfway through draw:
```csharp
long halfwayPoint = attackComponent.AttackSpeed(ActiveWeapon) / 2;
if (elapsedTime > halfwayPoint)
    return false; // Cannot interrupt
```

### Combat Round Timing

**Game Rule Summary**: Combat operates on a rhythm based on your weapon speed. Normal attacks happen at regular intervals, but fumbling your attack doubles the time until your next swing because you need to recover. The game processes combat in small 100ms increments between major actions.

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

**Game Rule Summary**: There are minimum attack speeds to prevent weapons from becoming impossibly fast. No matter how much Quickness and speed bonuses you have, melee weapons can't swing faster than 1.5 seconds and ranged weapons have their own limits. This maintains combat balance and prevents extreme speed builds.

#### Minimum Attack Speeds
- **Melee**: 1500ms (1.5 seconds)
- **Ranged (Rapid Fire)**: 900ms (0.9 seconds)
- **NPC**: 500ms (0.5 seconds)

#### Maximum Speed Reduction
- Theoretical max with 250 Qui + buffs: ~60-70% reduction
- Practical cap depends on available buffs/items

### Projectile Flight Time

**Game Rule Summary**: Arrows and bolts take time to travel to their target based on distance. This means there's a delay between when you release the shot and when the damage is applied. Skilled players can use this to time their movements and defenses against incoming projectiles.

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