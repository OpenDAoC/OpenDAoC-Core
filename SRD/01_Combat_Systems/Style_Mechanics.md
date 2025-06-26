# Style Mechanics

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from Style.cs and StyleProcessor.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: Combat styles are special attack techniques that do extra damage when used in the right situation. Some require attacking from behind, others work after your enemy blocks you, and many form chains where one style leads to another. Mastering these attack combinations separates skilled fighters from novices, as styles can do much more damage than regular attacks.

Combat styles are special attacks that provide bonus damage and effects when executed under specific conditions. They form chains of attacks that reward skillful play and positioning.

## Core Mechanics

### Style Requirements

**Game Rule Summary**: Every combat style has specific requirements to use it. Some work anytime, others only work after certain things happen (like your enemy parrying your attack), and positional styles only work when you're in the right spot relative to your target. Understanding these requirements is key to using styles effectively.

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

**Game Rule Summary**: Positional styles require you to be in the right place relative to your target. Back styles (like assassin backstabs) only work from directly behind, side styles work from the flanks, and front styles work when facing your enemy. Position matters because different angles give different combat advantages.

For positional styles:
- **Back** (0): 60° arc (150° to 210°)
- **Side** (1): 105° arc (45°-150° OR 210°-315°)
- **Front** (2): Must be in target's front arc

**Source**: `StyleProcessor.cs:CanUseStyle()`

### Style Damage Calculation

**Game Rule Summary**: Style damage is calculated based on your weapon specialization level and how fast your weapon is. The more you've trained in a weapon, the more bonus damage your styles do. Slower weapons get bigger style bonuses to balance out their speed disadvantage. This makes specialization very important for effective combat.

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

**Game Rule Summary**: Stealth classes have special opening attacks that do massive damage when attacking from stealth. These attacks use fixed damage formulas instead of the normal style system, and they ignore normal damage caps. The trade-off is that using them breaks your stealth, so timing and positioning are crucial.

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

**Game Rule Summary**: Some styles make your attacks more accurate (bonus to hit) while others make it harder for your enemy to hit you back (bonus to defense). These tactical bonuses can swing the tide of combat, especially in longer fights where accuracy and defensive positioning matter.

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

**Game Rule Summary**: Using combat styles costs endurance (stamina), and slower weapons cost more endurance per style because you're putting more effort into each swing. Running out of endurance means you can't use styles anymore, so managing your stamina is important in long fights.

#### Base Cost Calculation
```
EnduranceCost = BaseEnduranceCost * WeaponSpeed / 10
```

#### Cost Modifiers
- Realm abilities can reduce cost
- Some styles have 0 endurance cost

### Style Effects and Procs

**Game Rule Summary**: Many styles have special effects beyond just doing damage. They might cause bleeding, stun the target, or apply debuffs like movement snares. These effects are what make styles tactically interesting - the right style at the right time can completely change a fight.

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

**Game Rule Summary**: Style chains are sequences of attacks where using one style opens up the opportunity to use another more powerful style. These chains reward skillful play and timing, as you need to successfully land the first style to access the follow-up. Mastering chains is essential for advanced combat.

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

**Game Rule Summary**: Some styles have additional requirements beyond positioning and chains. Certain styles only work with specific weapon types, others require you to be stealthed, and all styles require a minimum specialization level to learn and use.

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