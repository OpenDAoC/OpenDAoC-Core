# Damage Calculation

## Document Status
- **Completeness**: 90% (missing some specific condition formulas)
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from AttackComponent.cs and related files
- **Implementation Status**: ✅ Fully Implemented

## Overview
Damage calculation in DAoC is a multi-step process that considers weapon properties, character stats, target armor, and various modifiers. The system ensures balanced combat while rewarding proper gear and specialization choices.

## Core Mechanics

### 1. Base Damage Calculation

#### Player Weapon Damage
```
BaseDamage = WeaponDPS * WeaponSpeed * 0.1 * SlowWeaponModifier
SlowWeaponModifier = 1 + (WeaponSpeed - 20) * 0.003
```
- **Source**: `AttackComponent.cs:AttackDamage()`
- **Requirements**: 
  - Weapon must be equipped
  - Speed measured in tenths of seconds (37 = 3.7 seconds)
- **Edge Cases**: 
  - Unarmed damage returns 0
  - Speed modifier only applies when speed > 20

#### NPC Damage
```
BaseDamage = (1.0 + Level / F1 + Level² / F2) * WeaponSpeed * 0.1
```
- **F1**: `Properties.PVE_MOB_DAMAGE_F1` (configurable)
- **F2**: `Properties.PVE_MOB_DAMAGE_F2` (configurable)
- **Weapon Speed**: 30 (1H), 40 (2H), 45 (ranged)

### 2. Damage Modifiers

#### Slow Weapon Bonus
Weapons with speed > 2.0 seconds receive a damage bonus:
```
SlowWeaponModifier = 1 + (WeaponSpeed - 20) * 0.003
```

#### Two-Handed Weapon Bonus
```
TwoHandedModifier = 1.1 + SpecLevel * 0.005
```
- **Requirements**: Weapon must be two-handed or ranged
- **Example**: At 50 spec: 1.1 + 50 * 0.005 = 1.35 (35% bonus)

#### Dual Wield (Left Axe) Modifier
```
LeftAxeModifier = 0.625 + LeftAxeSpec * 0.0034
```
- **Base**: 62.5% damage
- **Per Point**: +0.34% damage
- **At 50 Spec**: 62.5% + 17% = 79.5% damage

#### Quality and Condition
```
ModifiedDamage = BaseDamage * (Quality / 100) * (Condition / MaxCondition)
```
- **Quality**: 85-100% typical range
- **Condition**: Degrades with use

#### Ammo Modifiers (Archery)
Ammo type affects damage based on SPD_ABS bits 0-1:
- **Blunt/Light** (0): 85% damage
- **Bodkin/Medium** (1): 100% damage  
- **Barbed** (2): 115% damage (not used on live)
- **Broadhead/X-Heavy** (3): 125% damage

### 3. Damage Cap
```
DamageCap = BaseDamage * Quality * Condition * 3
```
- Applied after all modifiers except resistances
- Prevents extreme damage spikes

### 4. Stat Contributions

#### Melee Weapons
```
StatDamage = Strength / 2
```
- Capped at: `(Level + 1) * 0.6`

#### Ranged Weapons
```
StatDamage = Dexterity / 2
```
- Same cap as melee

### 5. Weapon Skill Calculation

#### Base Weapon Skill
```
WeaponSkill = PlayerWeaponSkill + 90.68 (inherent)
```

#### Spec Variance
```
Variance = 0.25 + Min(0.5, (SpecLevel - 1) / Level)
SpecModifier = 1 + Variance * (SpecLevel - TargetLevel) * 0.01
```
- **Min Variance**: 0.25 (at spec 1)
- **Max Variance**: 1.25 (at spec = target level)

#### Final Weapon Skill
```
FinalWeaponSkill = BaseWeaponSkill * RelicBonus * SpecModifier
```

### 6. Armor Mitigation

#### Armor Factor
```
EffectiveAF = TargetAF + 12.5 (inherent) + PlayerBonus
PlayerBonus = Level * 20 / 50 (players only)
```

#### Armor Absorption
Base absorption by armor type:
- **Cloth**: 0%
- **Leather**: 10%
- **Reinforced/Studded**: 19%
- **Scale/Chain**: 27%
- **Plate**: 34%

NPCs: `Level * 0.0054` (27% at level 50)

#### Damage Modifier
```
DamageMod = WeaponSkill / (ArmorFactor / (1 - Absorption))
```

### 7. PvP/PvE Modifiers
- **PvP**: `Properties.PVP_MELEE_DAMAGE` (server configurable)
- **PvE**: `Properties.PVE_MELEE_DAMAGE` (server configurable)

### 8. Critical Damage

#### Critical Chance
From items/abilities:
- Melee: `eProperty.CriticalMeleeHitChance`
- Archery: `eProperty.CriticalArcheryHitChance`

#### Critical Damage Range
- **vs NPCs**: 10% to 100% of base damage
- **vs Players**: 10% to 50% of base damage

#### Berserk Modifiers
- Level 1: 10-25% critical damage
- Level 2: 10-50% critical damage
- Level 3: 10-75% critical damage
- Level 4: 10-99% critical damage

### 9. Resistance Calculation

#### Primary Resistance Layer
```
PrimaryResist = ItemResist + RacialResist + BuffResist + BannerBonus - ResistPierce
DamageReduction1 = Damage * PrimaryResist * 0.01
```

#### Secondary Resistance Layer  
```
SecondaryResist = RealmAbilityResist + SpecBuffResist (capped at 80%)
DamageReduction2 = (Damage - DamageReduction1) * SecondaryResist * 0.01
```

#### Final Damage
```
FinalDamage = Damage - DamageReduction1 - DamageReduction2
```

### 10. Style Damage

#### Growth Rate
```
StyleDamage = BaseDamage * (GrowthRate * SpecLevel / 100 + Bonus)
```
- Applied after base damage calculation
- Subject to same resistances

#### Style Damage Bonus (ToA)
```
StyleDamageBonus = PreResistDamage * StyleDamage% * 0.01
```
- Calculated from base damage
- Added to style damage

## System Interactions

### With Property System
- Weapon skill modified by `eProperty.WeaponSkill`
- Melee damage modified by `eProperty.MeleeDamage`
- Critical chance from various properties

### With Buff System
- Damage type can be modified by procs
- Stat buffs increase damage output
- Damage adds provide extra hits

### With Spec System
- Specialization affects variance
- Two-handed spec increases damage
- Left axe spec improves dual wield

## Implementation Notes

### Database Schema
```sql
-- Weapon properties
DPS_AF: Base DPS (damage per second)
SPD_ABS: Speed in tenths of seconds
Quality: 0-100 quality percentage
Condition: Current durability
MaxCondition: Maximum durability
```

### Performance Considerations
- Damage calculations cached where possible
- Weapon skill recalculated on spec/buff changes
- Critical calculations use simple RNG

## Test Scenarios

### Basic Damage Test
```
Given: Level 50 warrior with 165 DPS sword, speed 37
Spec: 50 in sword
Target: Level 50 with 635 AF, 27% absorb
Expected: ~165 damage before resistances
```

### Slow Weapon Test
```
Given: 165 DPS weapon, speed 56 (5.6 seconds)
SlowWeaponModifier: 1 + (56-20) * 0.003 = 1.108
Base Damage: 165 * 56 * 0.1 * 1.108 = 1024
```

### Critical Hit Test
```
Given: 10% critical chance, vs player
Critical Roll: Success
Damage Range: 10-50% of base damage
Expected: Additional 10-50% damage added
```

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added all damage calculation formulas
- Documented resistance system

## References
- `GameServer/ECS-Components/AttackComponent.cs`
- `GameServer/gameobjects/GamePlayer.cs`
- `GameServer/gameobjects/GameLiving.cs`
- Live testing data from Phoenix forums 