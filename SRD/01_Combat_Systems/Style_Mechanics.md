# Style Mechanics

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from Style.cs and StyleProcessor.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
Combat styles are special attacks that provide bonus damage and effects when executed under specific conditions. They form chains of attacks that reward skillful play and positioning.

## Core Mechanics

### Style Requirements

#### Opening Types
1. **Offensive** (0): Based on attacker's previous action
2. **Defensive** (1): Based on defender's previous action  
3. **Positional** (2): Based on attacker's position relative to target

**Source**: `Style.cs:eOpening`

#### Attack Result Requirements
Used with Offensive/Defensive openings:
- **Any** (0): No specific requirement
- **Miss** (1): Previous attack must miss
- **Hit** (2): Previous attack must hit
- **Parry** (3): Previous attack must be parried
- **Block** (4): Previous attack must be blocked
- **Evade** (5): Previous attack must be evaded
- **Fumble** (6): Previous attack must fumble
- **Style** (7): Previous attack must be a specific style

**Source**: `Style.cs:eAttackResultRequirement`

#### Positional Requirements
For positional styles:
- **Back** (0): 60° arc (150° to 210°)
- **Side** (1): 105° arc (45°-150° OR 210°-315°)
- **Front** (2): Must be in target's front arc

**Source**: `StyleProcessor.cs:CanUseStyle()`

### Style Damage Calculation

#### Standard Styles
```
ModifiedGrowthRate = GrowthRate * Spec * AttackSpeed / UnstyledDamageCap
StyleDamage = ModifiedGrowthRate * UnstyledDamage
StyleDamageCap = ModifiedGrowthRate * UnstyledDamageCap
```
- **GrowthRate**: Base style growth factor
- **Spec**: Player's specialization level
- **AttackSpeed**: Weapon speed in seconds
- **Source**: `StyleProcessor.cs:ExecuteStyle()`

#### Minimum Damage
```csharp
if (styleDamage < 1 && growthRate > 0)
{
    styleDamage = 1;
    styleDamageCap = 0; // Ignore cap for minimum damage
}
```

#### Stealth Openers
Stealth styles use fixed formulas:

**Backstab I (ID: 335)**
```
Damage = Min(5, Spec/10) + Spec * 14/3
```

**Backstab II (ID: 339)**
```
Damage = Min(45, Spec) + Spec * 6
```

**Perforate Artery (ID: 343)**
```
2H Weapon: Damage = Min(75, Spec * 1.5) + Spec * 12
1H Weapon: Damage = Min(75, Spec * 1.5) + Spec * 9
```
- Apply armor absorption after calculation
- No damage cap for stealth styles

### Style Bonuses

#### To-Hit Bonus
```
MissChance -= Style.BonusToHit
```
- Applied during attack resolution
- Reduces chance to miss

#### To-Defense Bonus
```
NextAttackMissChance += Style.BonusToDefense
```
- Applied to defender's next attack
- Increases attacker's miss chance

### Endurance Cost

#### Base Cost Calculation
```
EnduranceCost = BaseEnduranceCost * WeaponSpeed / 10
```

#### Cost Modifiers
- Realm abilities can reduce cost
- Some styles have 0 endurance cost

### Style Effects and Procs

#### Style Procs
- Can have multiple procs per style
- Class-specific procs take priority
- Random proc selection if `RandomProc = true`
- Proc chance check: `Util.Chance(proc.Chance)`

#### Proc Types
1. **Direct Damage**: Instant damage procs
2. **Damage Over Time**: Bleed effects
3. **Debuffs**: Snare, disease, etc.
4. **Stuns**: Disable target
5. **Other Effects**: Heal procs, buffs, etc.

### Style Chains

#### Chain Validation
```csharp
// Style required before this one?
if (style.OpeningRequirementValue != 0
    && (lastAttackData == null
    || lastAttackData.AttackResult != eAttackResult.HitStyle
    || lastAttackData.Style == null
    || lastAttackData.Style.ID != style.OpeningRequirementValue))
    return false;
```
- Chains work on any target (not same-target only)
- Must execute required style immediately before

### Special Requirements

#### Weapon Type
- Style may require specific weapon type
- Special value 1001 = any weapon
- Checked against equipped weapon

#### Stealth Requirement
- Some styles require stealth
- Stealth is broken on style execution

#### Specialization Level
- Minimum spec level to use style
- Separate from style damage calculations

## System Interactions

### With Attack System
- Styles modify base attack damage
- Add style damage on top of weapon damage
- Can change attack animations

### With Defense System
- Some styles trigger on defensive results
- Parry/Block/Evade can open reactive styles
- Defense bonuses affect follow-up attacks

### With Buff System
- Style damage increased by `eProperty.StyleDamage`
- Absorb effects reduce style damage
- Some buffs grant special styles

### With Property System
- `eProperty.StyleAbsorb`: Reduces incoming style damage
- `eProperty.StyleDamage`: Increases outgoing style damage
- Applied as percentage modifiers

## Implementation Notes

### Database Schema
```sql
-- Style properties
ID: Unique style identifier
Name: Display name
GrowthRate: Damage growth factor
GrowthOffset: Base damage offset
EnduranceCost: Base endurance cost
BonusToHit: Accuracy bonus
BonusToDefense: Defense bonus for target
OpeningRequirementType: 0=Offensive, 1=Defensive, 2=Positional
OpeningRequirementValue: Style ID or position
AttackResultRequirement: Required attack result
WeaponTypeRequirement: Required weapon type
StealthRequirement: Requires stealth
SpecLevelRequirement: Minimum spec level
```

### Animation System
- Styles can override attack animations
- `TwoHandAnimation`: Special 2H animations
- Animation ID passed to combat animation packet

## Test Scenarios

### Basic Style Chain
```
Given: Warrior with 50 Sword spec
Style 1: Anytime style, GR 0.5
Style 2: Follow-up to Style 1, GR 0.75
Expected: Style 2 only usable after hitting with Style 1
```

### Positional Style
```
Given: Assassin behind target
Style: Backstab (Back positional)
Angle: 180° (directly behind)
Expected: Style executes successfully
```

### Defensive Style
```
Given: Defender with reactive style
Requirement: Enemy parries
Action: Enemy parries defender's attack
Expected: Next attack can use reactive style
```

### Growth Rate Calculation
```
Given: 50 spec, GR 0.9, weapon speed 3.5s
Unstyled damage: 200, cap: 600
ModifiedGR: 0.9 * 50 * 3.5 / 600 = 0.2625
Style damage: 0.2625 * 200 = 52.5
```

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added all style mechanics and formulas
- Documented special cases and requirements

## References
- `GameServer/styles/Style.cs` - Style definitions
- `GameServer/styles/StyleProcessor.cs` - Style execution logic
- `GameServer/ECS-Components/PlayerStyleComponent.cs` - Player style management
- Live server testing for positional arcs 