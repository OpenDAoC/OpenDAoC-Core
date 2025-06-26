# Defense Mechanics

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from GameLiving.cs and AttackComponent.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
Defense mechanics in DAoC provide multiple layers of avoiding damage through evade, parry, and block. These defenses are checked in a specific order and are affected by factors like attacker count, weapon types, and positioning.

## Core Mechanics

### Defense Resolution Order
Defense checks occur in this exact order:
1. **Bodyguard** (redirects attack to bodyguard)
2. **Phase Shift** (100% miss if active)
3. **Grapple** (special grapple result)
4. **Brittle Guard** (cancels attack and consumes effect)
5. **Intercept** (if not stealth attack)
6. **Evade**
7. **Parry** (melee only)
8. **Block** (requires shield)
9. **Guard** (special block for others)
10. **Miss** (see Attack Resolution)

**Source**: `AttackComponent.cs:CalculateEnemyAttackResult()`

### Defense Penetration Factors

#### Base Defense Penetration
All attacks have base defense penetration calculated as:
```csharp
DefensePenetration = WeaponSkill * 0.08 / 100
```

#### Weapon Type Modifiers
**Source**: `GameLiving.cs`
```csharp
// Default factors (what percentage of defense is used)
DualWieldDefensePenetrationFactor = 0.5    // 50% defense effectiveness
TwoHandedDefensePenetrationFactor = 0.5    // 50% defense effectiveness
```

**Applied Defenses**:
- **Dual Wield**: Reduces evade, parry, block, and guard by 50%
- **Two-Handed**: Reduces parry only by 50%
- **Penetrating Arrow**: 25% + (AbilityLevel * 25%) reduction

### 1. Evade

#### Base Evade Calculation
```
BaseEvade = ((Dex + Qui) / 2 - 50) * 0.05 + EvadeAbilityLevel * 5
```
- **Source**: `EvadeChanceCalculator.cs`
- **Requirements**: 
  - Must have Evade ability (or Enhanced/Advanced Evade)
  - Target must be in front arc (180° standard, 360° with Advanced Evade)
- **Conversion**: Result in 0-1000, divide by 1000 for percentage

#### Multiple Attacker Penalty
```
EvadeChance -= (AttackerCount - 1) * 0.03
```
- **Source**: `GameLiving.cs:TryEvade()`
- 3% reduction per additional attacker

#### Ranged Attack Modifier
```
RangedEvadeChance = BaseEvadeChance / 5
```
- Evade is 80% less effective against ranged attacks

#### Defense Penetration
```
FinalEvadeChance = BaseEvadeChance * (1 - DefensePenetration)
```
- Attacker's defense penetration reduces evade chance

#### Special Modifiers
- **Dual Wield**: `EvadeChance *= 0.5` (50% defense effectiveness)
- **Style Bonus**: Kelgor's Claw adds 15% evade
- **RvR Cap**: 50% maximum in PvP (Properties.EVADE_CAP)
- **Overwhelm (RR5)**: Reduces by 15% (flat reduction)
- **Combat Awareness**: Allows evade from all directions
- **Rune of Utter Agility**: Allows evade from all directions

### 2. Parry

#### Base Parry Calculation
```
BaseParry = (Dex * 2 - 100) / 40 + ParrySpec / 2 + MasteryOfParry * 3 + 5
```
- **Source**: `ParryChanceCalculator.cs`
- **Requirements**:
  - Must have Parry specialization or buff
  - Must have weapon equipped (not bow)
  - Target must be in front arc (120°)
- **Conversion**: Result in 0-1000, divide by 1000 for percentage

#### Multiple Attacker Penalty
```
ParryChance /= (AttackerCount + 1) / 2
```
- **Source**: `GameLiving.cs:TryParry()`
- More forgiving than evade reduction

#### Two-Handed Weapon Penalty
```
ParryChance *= 0.5
```
- Parry is 50% less effective against two-handed weapons

#### Special Cases
- **Blade Barrier**: 90% parry chance, overrides all calculations
- **Style Bonus**: Tribal Wrath adds 25% parry
- **RvR Cap**: 50% maximum in PvP (Properties.PARRY_CAP)
- **Overwhelm (RR5)**: Reduces by 15% (flat reduction)
- **Savage Parry Buff**: Allows parry without spec

### 3. Block

#### Base Block Calculation  
```
BaseBlock = 5% + 0.5% * ShieldSpec + (Dex * 2 - 100) / 40 + MasteryOfBlocking * 3
```
- **Source**: `BlockChanceCalculator.cs`
- **Requirements**:
  - Must have Shield ability
  - Shield must be equipped
  - Target must be in front arc (120°, 360° with Engage)
- **Quality/Condition**: `BlockChance *= Quality * 0.01 * Condition / MaxCondition`

#### Shield Size Mechanics
Shield size affects block effectiveness:
- **Small Shield (1)**: Blocks 1 simultaneous attack
- **Medium Shield (2)**: Blocks 2 simultaneous attacks  
- **Large Shield (3)**: Blocks 3 simultaneous attacks

**Source**: `AttackComponent.cs:BlockRoundHandler`

#### Block Round System
The block round handler manages simultaneous blocks:
```csharp
public class BlockRoundHandler
{
    // Tracks used block rounds based on attacker's attack speed
    // Dual wield off-hand doesn't consume block rounds
    // Resets based on defender's attack speed timing
}
```

**Key Points**:
- Block rounds tracked per attacker
- Based on attacker's attack speed (not defender's)
- Dual wield off-hand hits are "free" (don't consume rounds)
- System prevents shield size from being overwhelmed

#### Block Caps by Shield Size
RvR shield size caps (old system, reference only):
```
Small Shield: 80% max
Medium Shield: 90% max
Large Shield: 99% max
```
- **Note**: Superseded by 60% global cap in patch 1.96
- **Current Cap**: Properties.BLOCK_CAP (default 100%)

#### Engage Ability
When engaging a target:
- Block chance increased to 95% minimum
- Works 360° against engaged target
- Normal arc against other attackers
- Costs endurance per block
- Target must not have been attacked recently
- Cancelled if insufficient endurance

#### Special Modifiers
- **Dual Wield**: `BlockChance *= 0.5` (50% defense effectiveness)
- **Overwhelm (RR5)**: Reduces by 15% (flat reduction)
- **Guard**: Can block for others (see Guard section)
- **Nature's Shield**: 100% block vs ranged/spell (120° arc)

### 4. Guard

Guard allows blocking attacks for nearby allies.

#### Guard Requirements
- Guard ability active
- Target within GUARD_DISTANCE
- Source has shield equipped (or is NPC)
- Source facing attacker (180°)
- Not crowd controlled/sitting/casting
- Not using ranged weapon

#### Guard Calculation
```
GuardChance = BlockChance + GuardLevel * 5%
```
- Uses guarder's block chance
- Additional 5% per Guard ability level
- Not affected by shield size for chance
- Not affected by attacker count
- Still uses block rounds system

#### Guard Shield Caps (RvR)
Same as regular block:
```
Small Shield: 80% max
Medium Shield: 90% max  
Large Shield: 99% max
```

### 5. Positional Requirements

#### Front Arc
- **Evade**: 180° (360° with Advanced Evade)
- **Parry**: 120°
- **Block**: 120° (360° with Engage vs target)
- **Guard Source**: 180°

#### Arc Calculation
```csharp
IsObjectInFront(attacker, arcDegrees)
```
- Measured from defender's facing
- Attacker must be within specified arc

## Buff Stacking System

### Defense Buff Categories
1. **Base Buffs**: Standard stat/skill buffs
2. **Spec Buffs**: Specialized defense buffs
3. **Item Bonuses**: From equipment
4. **Ability Bonuses**: From realm/class abilities

### Stacking Rules
- Only highest value in each category applies
- Categories stack additively
- Debuffs subtract from totals
- Some abilities override calculations entirely

### Common Defense Buffs
- **Dexterity**: Improves all defenses
- **Quickness**: Improves evade only
- **Savage Buffs**: Enable evade/parry without spec
- **Blade Barrier**: 90% parry override
- **Mastery RAs**: Direct bonus to defense

## Special Abilities

### Overwhelm (Infiltrator RR5)
**Source**: `OverwhelmAbility.cs`
- **Duration**: 30 seconds
- **Effect**: Reduces target's defenses by 15%
- **Stacking**: Does not stack, refreshes duration

### Mastery of Blocking
**Source**: `MasteryOfPain.cs`
Bonus values per level:
```
Level 1: +2% block
Level 2: +4% block
Level 3: +6% block
Level 4: +9% block
Level 5: +12% block
Level 6: +15% block
Level 7: +18% block
Level 8: +21% block
Level 9: +25% block
```

### Penetrating Arrow
Reduces all defenses by:
```
Reduction = 25% + (AbilityLevel * 25%)
Level 1: 50% reduction
Level 2: 75% reduction
Level 3: 100% reduction (bypasses all defenses)
```

## Implementation Notes

### Database Fields
NPCs have direct defense chances:
```sql
EvadeChance: 0-99 percentage
ParryChance: 0-99 percentage  
BlockChance: 0-99 percentage
```

### Performance Considerations
- Defense checks short-circuit on success
- Positional checks cached per attack
- Multiple attacker lists maintained
- Block rounds tracked efficiently

### Critical Implementation Details
- All percentage values stored as 0-1000 internally
- Divided by 10 or 1000 for display/use
- Defense caps only apply in RvR
- Some abilities bypass normal calculations

## Test Scenarios

### Basic Evade Test
```
Given: Level 50 with 250 Dex, 250 Qui, Evade V
Base: ((250 + 250) / 2 - 50) * 0.05 + 5 * 5 = 35%
vs 1 attacker: 35%
vs 3 attackers: 35% - 6% = 29%
vs Dual Wield: 29% * 0.5 = 14.5%
```

### Parry with Multiple Attackers
```
Given: 250 Dex, 50 Parry spec, no Mastery
Base: (250 * 2 - 100) / 40 + 50 / 2 + 0 + 5 = 10 + 25 + 5 = 40%
vs 2 attackers: 40% / ((2 + 1) / 2) = 40% / 1.5 = 26.7%
vs Two-Handed: 26.7% * 0.5 = 13.35%
```

### Shield Block with Size
```
Given: Large shield, 50 Shield spec, 250 Dex
Base: 5% + 0.5% * 50 + (250 * 2 - 100) / 40 = 5% + 25% + 10% = 40%
Can block up to 3 attacks simultaneously
vs Dual Wield: 40% * 0.5 = 20%
With Overwhelm: 20% - 15% = 5%
```

### Guard Through Multiple Attackers
```
Given: Guard III, 30% base block
Guard Chance: 30% + 3 * 5% = 45%
Not reduced by attacker count
Not reduced by shield size limits
Still affected by weapon type penalties
```

## Change Log

### 2024-01-20
- Expanded with defense penetration factors
- Added block round handler details
- Added buff stacking system
- Documented special abilities and values
- Added comprehensive test scenarios

## References
- `GameServer/gameobjects/GameLiving.cs` - Defense calculations
- `GameServer/ECS-Components/AttackComponent.cs` - Attack resolution and block rounds
- `GameServer/propertycalc/*ChanceCalculator.cs` - Base calculations
- `GameServer/realmabilities/handlers/` - Special abilities
- Live patch notes 1.74, 1.96

## System Interactions

### With Attack System
- Defense checks occur after hit determination
- Successful defense negates all damage
- Some defenses trigger counter-styles

### With Buff System
- Dexterity buffs improve all defenses
- Quickness buffs improve evade
- Specific defense buffs (Evade/Parry/Block buffs)

### With Spec System
- Parry spec directly increases parry chance
- Shield spec increases block chance
- No spec affects evade (ability-based)

### With Property System
- `eProperty.EvadeChance`: Direct evade modifier
- `eProperty.ParryChance`: Direct parry modifier
- `eProperty.BlockChance`: Direct block modifier

## Implementation Notes

### Database Fields
NPCs have direct defense chances:
```sql
EvadeChance: 0-99 percentage
ParryChance: 0-99 percentage  
BlockChance: 0-99 percentage
```

### Performance Considerations
- Defense checks short-circuit on success
- Positional checks cached per attack
- Multiple attacker lists maintained

## Test Scenarios

### Basic Evade Test
```
Given: Level 50 with 250 Dex, 250 Qui, Evade V
Base: ((250 + 250) / 2 - 50) * 0.05 + 5 * 5 = 35%
vs 1 attacker: 35%
vs 3 attackers: 35% - 6% = 29%
```

### Parry with Multiple Attackers
```
Given: 250 Dex, 50 Parry spec, no Mastery
Base: (250 * 2 - 100) / 40 + 50 / 2 + 0 + 5 = 10 + 25 + 5 = 40%
vs 2 attackers: 40% / ((2 + 1) / 2) = 40% / 1.5 = 26.7%
```

### Shield Block with Size
```
Given: Large shield, 50 Shield spec
Base: 5% + 0.5% * 50 = 30%
Can block up to 3 attacks simultaneously
Capped at 75% in RvR (tier 3 large shield)
```

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added all defense formulas and mechanics
- Documented special abilities and modifiers

## References
- `GameServer/gameobjects/GameLiving.cs` - Defense calculations
- `GameServer/ECS-Components/AttackComponent.cs` - Attack resolution
- `GameServer/propertycalc/*ChanceCalculator.cs` - Base calculations
- Grab bag references from comments 