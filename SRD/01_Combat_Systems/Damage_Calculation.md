# Damage Calculation

## Document Status
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

## Additional Damage Mechanics

### Damage Adds

#### Overview
Damage adds provide additional damage on successful melee/ranged attacks:
- **Source**: `DamageAddSpellHandler.cs`
- **Trigger**: On `HitUnstyled` or `HitStyle` results
- **Damage Type**: Specified by spell

#### Damage Calculation
Fixed damage:
```csharp
Variance = 0.85 to 1.42 (0.85 * 5/3)
Effectiveness *= 1 + BuffEffectiveness * 0.01
Damage = Spell.Damage * Variance * Effectiveness * AttackInterval * 0.001
```

Percentage damage:
```csharp
Damage = AttackDamage * Spell.Damage / -100
```

#### Stacking Rules
1. **Unaffected by stacking** (EffectGroup 99999):
   - Apply first at 100% effectiveness
   - Usually RA-based (Anger of the Gods, etc.)
2. **Regular damage adds**:
   - First add: 100% effectiveness
   - Additional adds: 50% effectiveness
   - Ordered by damage (highest first)

### Damage Shields

#### Overview
Damage shields reflect damage back to attackers:
- **Source**: `DamageShieldSpellHandler.cs`
- **Trigger**: When owner is hit
- **Target**: The attacker

#### Damage Calculation
Similar to damage adds but with different variance:
```csharp
Variance = 0.9 to 1.5 (0.9 * 5/3)
Effectiveness *= 1 + BuffEffectiveness * 0.01
Damage = Spell.Damage * Variance * Effectiveness * AttackInterval * 0.001
```

#### Special Properties
- Not affected by resistances
- Uses spell damage type
- Shows special animation (0x14)

### Conversion Mechanics

#### Overview
Conversion transforms incoming damage into power/endurance:
- **Source**: `AttackComponent.cs`, `Conversion.cs`
- **Property**: `eProperty.Conversion`
- **Cap**: 100% conversion

#### Calculation
```csharp
ConversionMod = 1 - GetModified(eProperty.Conversion) / 100
FinalDamage *= ConversionMod
ConvertedAmount = ConversionMod > 0 ? Damage / ConversionMod - Damage : Damage
```

#### Regeneration
Converted damage is split between power and endurance:
- Cannot exceed maximum values
- Shows gain messages to player

### Reactive Effects

#### Armor Reactive Procs
When armor is struck:
```csharp
if (ad.ArmorHitLocation != eArmorSlot.NOTSET)
{
    item = Inventory.GetItem(ad.ArmorHitLocation);
    TryReactiveEffect(item, attacker);
}
```

#### Shield Reactive Procs
When attack is blocked:
```csharp
if (reactiveItem.Object_Type == eObjectType.Shield)
    TryReactiveEffect(reactiveItem, attacker);
```

### Special Damage Modifiers

#### Sitting Target
- **Effect**: Double damage
- **Applies to**: Melee attacks only
- **Source**: `AttackAction.cs`

#### Defense Penetration
```csharp
DefensePenetration = WeaponSkill * 0.08 / 100
```
- Reduces target's defenses
- Calculated from modified weapon skill

#### Ablative Armor
Absorbs percentage of incoming damage:
- **Default**: 25% absorption
- **Max**: 100% absorption
- **Types**: Melee, Magic, Both
- Damage absorbed reduces ablative pool

#### Damage Immunity Effects
**Shield of Immunity** (RR5):
- 90% damage reduction
- Affects melee, ranged, archery spells

**Testudo** (RR5):
- 90% damage reduction
- Requires shield equipped
- Movement restricted

#### Nature's Shield
- 100% block chance vs ranged/spell
- 120° frontal arc
- Triggered by combat styles

## Damage Type Interactions

### Physical vs Magical
- Physical damage affected by armor
- Magical damage affected by resists
- Some attacks deal both (e.g., bolts)

### Damage Type Conversion
Some effects change damage type:
- Procs can override weapon damage type
- Spells specify their damage type
- Conversion happens before resistance

## Implementation Notes

### Performance Considerations
- Damage calculations cached where possible
- Weapon skill recalculated on spec/buff changes
- Critical calculations use simple RNG
- Damage adds ordered once per effect application

### Order of Operations
1. Calculate base damage
2. Apply weapon modifiers
3. Apply stat contributions
4. Check damage cap
5. Apply armor mitigation
6. Apply resistances
7. Apply conversion
8. Calculate critical damage
9. Process damage adds
10. Process damage shields

### Database Schema
```sql
-- Weapon properties
DPS_AF: Base DPS (damage per second)
SPD_ABS: Speed in tenths of seconds
Quality: 0-100 quality percentage
Condition: Current durability
MaxCondition: Maximum durability

-- Spell properties for adds/shields
Damage: Fixed damage or percentage (negative)
Duration: Effect duration
EffectGroup: Stacking group (99999 = unaffected)
DamageType: Element type for damage
```

## Test Scenarios

### Basic Damage Test
```
Given: Level 50 warrior with 165 DPS sword, speed 37
Spec: 50 in sword
Target: Level 50 with 635 AF, 27% absorb
Expected: ~165 damage before resistances
```

### Damage Add Stacking
```
Given: 3 damage adds (100, 75, 50 damage)
First add: 100 damage at 100% = 100
Second add: 75 damage at 50% = 37.5
Third add: 50 damage at 50% = 25
Total: 162.5 additional damage
```

### Conversion Test
```
Given: 50% conversion, 200 incoming damage
Damage taken: 200 * 0.5 = 100
Power/End gained: 100 each (or to cap)
```

### Critical with Berserk
```
Given: Berserk 3, 10% crit chance, 200 base damage
Critical roll: Success
Critical damage: 20-150 (10-75% of 200)
Total: 220-350 damage
```

## Change Log

### 2024-01-20
- Expanded with damage add mechanics
- Added damage shield documentation
- Added conversion system details
- Documented reactive effects
- Added special damage modifiers
- Added damage type interactions

## References
- `GameServer/ECS-Components/AttackComponent.cs`
- `GameServer/spells/DamageAddAndShield.cs`
- `GameServer/spells/Conversion.cs`
- `GameServer/ECS-Components/Actions/WeaponAction.cs`
- `GameServer/realmabilities/handlers/` - Various RAs 