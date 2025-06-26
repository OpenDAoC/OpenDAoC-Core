# Attack Resolution System

## Document Status
- **Last Updated**: 2024-01-20
- **Verified Against**: Code analysis from AttackComponent.cs, GameLiving.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

The Attack Resolution System determines the outcome of all combat attacks in Dark Age of Camelot. It follows a specific order of checks to determine whether an attack hits, misses, or is defended against. This system is fundamental to all combat interactions and must maintain sub-millisecond performance for server stability.

## Core Mechanics

### Attack Resolution Order
**Formula**: Fixed sequence of resolution checks
**Source**: Live DAoC observation and legacy server analysis
**Implementation Requirements**: Must process in exact order, short-circuit on first success

The combat system follows this **exact** order of checks:
1. **Bodyguard Check**: Redirects attacks to bodyguard
2. **Phase Shift Check**: 100% miss if active
3. **Grapple Check**: Special grapple result
4. **Brittle Guard Check**: Cancels attack and consumes effect
5. **Intercept Check** (if not stealth attack)
6. **Defense Checks** (if not disabled):
   - **Evade** 
   - **Parry** (melee only)
   - **Block** (requires shield)
7. **Guard Check** (bodyguard ability)
8. **Hit/Miss Calculation**
9. **Strafing Miss** (30% chance in PvP)
10. **Fumble Check** (subset of miss, melee only)
11. **Damage Calculation** (if hit)
12. **Critical Strike Check**
13. **Damage Application**

**Special Cases**:
- **Stealth Attacks**: Bypass defenses, intercept, and brittle guard
- **Berserk Effect**: Disables all defenses
- **Crowd Control**: Disables evade, parry, block
- **Sitting**: Disables all defenses
- **Casting**: Disables parry and block

**Testing Requirements**:
- Verify defense checks occur in correct order
- Confirm short-circuiting on first successful defense
- Test edge cases for special abilities

### Base Miss Chance Calculation
**Formula**: `MissChance = 18 - ToHitBonus - LevelDiff * 1.33 - MultiAttacker * REDUCTION + ArmorBonus + StyleModifiers`
**Source**: `AttackComponent.cs:GetMissChance()`
**Implementation Requirements**: Must cap at reasonable bounds

**Components**:
- **Base Miss Rate**: 18% (reduced from 20% in patch 1.117C)
- **Intrinsic Weapon Bonus**: -5% (applied to all weapons)
- **ToHit Bonus**: From items, buffs, abilities
- **Level Difference**: -1.33% per level advantage (PvE only)
- **Multi-Attacker Bonus**: `MISSRATE_REDUCTION_PER_ATTACKERS` per extra attacker (default: 0)
- **Armor Bonus**: Target's armor bonus vs attacker's weapon bonus

**PvP vs PvE Armor Bonus**:
```csharp
if (ad.Target is GamePlayer && ad.Attacker is GamePlayer)
    missChance += armorBonus; // Flat addition
else
    missChance += missChance * armorBonus / 100; // Percentage increase
```

**Style Modifiers**:
- Attack style BonusToHit: Reduces miss chance
- Previous style BonusToDefense: Increases miss chance

**Ammo Quality (Archery)**:
- Rough: +15% miss chance
- Standard: No modifier
- Footed: -25% miss chance

**Testing Requirements**:
- Verify level difference calculations for various level gaps
- Test multi-attacker penalty with different attacker counts
- Validate hit chance bounds (never below 5%, never above 95%)

### Fumble Mechanics
**Formula**: `FumbleChance = Max((51 - AttackerLevel) / 1000, DebuffedFumbleChance)`
**Source**: Live DAoC mechanics analysis
**Implementation Requirements**: Fumbles are subset of misses, double attack interval

**Base Fumble Rates**:
- Level 1: 5.0% chance
- Level 50: 0.1% chance  
- Formula: `(51 - Level) / 1000`

**Fumble Effects**:
- Attack interval doubled for next attack
- Combat styles cleared
- Only applies to melee attacks (ranged cannot fumble)

**Debuff Integration**:
- Debuffs can increase fumble chance
- Total fumble chance capped at reasonable maximum
- Miss chance adjusted to be at least fumble chance

**Edge Cases**:
- Certain abilities grant fumble immunity
- Some weapon masteries reduce fumble chance
- Fumble chance cannot exceed miss chance

**Testing Requirements**:
- Verify fumble rates at different character levels
- Test fumble effect on attack timing
- Validate debuff interactions

### Intercept Mechanics
**Source**: `InterceptECSEffect.cs`, `AttackComponent.cs:CalculateAttackResult()`
**Ability**: Realm ability that redirects attacks to the interceptor

**Intercept Chance**:
- Players: 50%
- Brittle Guard Pets: 100%
- Bonedancer Sub-pets: 30% (reduced from 50% in patch 1.123)
- Spirit Warrior: 75% (patch 1.125 reduced from 75% to 60%)

**Requirements**:
- Interceptor must be within `INTERCEPT_DISTANCE` of target
- Interceptor cannot be incapacitated or sitting
- Roll must succeed against intercept chance

**Process**:
```csharp
if (intercept != null && !stealthStyle)
{
    if (intercept.Source is not GamePlayer || intercept.Stop())
    {
        ad.Target = intercept.Source;
        return eAttackResult.HitUnstyled;
    }
}
```

### Guard Mechanics
**Source**: `AttackComponent.cs:CheckGuard()`
**Ability**: Allows a player to block attacks for another player

**Requirements**:
- Guard source must be active and alive
- Cannot be crowd controlled, sitting, or casting
- Must have shield equipped (or be NPC)
- Must be within `GUARD_DISTANCE` of target
- Target must be in guard source's 180° frontal arc
- Cannot use ranged weapons

**Guard Chance Calculation**:
```csharp
// NPCs
guardChance = source.GetModified(eProperty.BlockChance);

// Players
guardChance = source.GetModified(eProperty.BlockChance) 
    * (shield.Quality * 0.01) 
    * (shield.Condition / shield.MaxCondition);

guardChance *= 0.001;
guardChance += source.GetAbilityLevel(Abilities.Guard) * 0.05; // 5% per level
guardChance *= 1 - ad.DefensePenetration;
```

**Shield Size Caps** (RvR only):
- Small Shield: 80% max
- Medium Shield: 90% max
- Large Shield: 99% max

**Note**: Guard is not affected by shield size limitations for multiple attackers

### Bodyguard Mechanics
**Source**: `AttackComponent.cs:CalculateAttackResult()`
**Effect**: Redirects melee attacks to designated bodyguard

**Requirements**:
- Attacker must be melee (not ranged)
- Target must have active bodyguard
- Bodyguard must be valid

**Process**:
- Sends messages to all involved parties
- Returns `eAttackResult.Bodyguarded`
- Attack is completely redirected

### Strafing Mechanics
**Source**: `AttackComponent.cs:action.Execute()`
**PvP Only**: 30% chance to force a miss when strafing

```csharp
if (playerOwner != null && playerOwner.IsStrafing && ad.Target is GamePlayer && Util.Chance(30))
{
    ad.MissChance = 0; // Special marker for strafing miss
    ad.AttackResult = eAttackResult.Missed;
}
```

## Server Configuration

### Defense Caps (RvR Only)
**Source**: `ServerProperties.cs`
- `EVADE_CAP`: Default 0.50 (50%)
- `PARRY_CAP`: Default 0.50 (50%)
- `BLOCK_CAP`: Default 1.00 (100%)

### Multi-Attacker Settings
- `MISSRATE_REDUCTION_PER_ATTACKERS`: Default 0 (configurable)
  - Reduces miss chance per additional attacker
  - Applied in PvE and PvP

### Other Combat Properties
- `OVERRIDE_DECK_RNG`: Use standard RNG vs player's deck
- `SPELL_HITCHANCE_DAMAGE_REDUCTION_MULTIPLIER`: Spell damage reduction

## System Interactions

### Defense Penetration
All defense chances are modified by attacker's defense penetration:
```csharp
defenseChance *= 1 - ad.DefensePenetration;
```

### Dual Wield Penalty
Dual wielding reduces target's defenses:
```csharp
if (ad.AttackType is eAttackType.MeleeDualWield)
{
    evadeChance *= ad.Attacker.DualWieldDefensePenetrationFactor;
    parryChance *= ad.Attacker.DualWieldDefensePenetrationFactor;
    blockChance *= ad.Attacker.DualWieldDefensePenetrationFactor;
}
```

### Two-Handed Weapon vs Parry
Two-handed weapons have advantage against parry:
```csharp
if (ad.AttackType is eAttackType.MeleeTwoHand)
    parryChance *= ad.Attacker.TwoHandedDefensePenetrationFactor;
```

### Overwhelm Effect (Infiltrator RR5)
Reduces all defense chances:
```csharp
if (Overwhelm != null)
{
    evadeChance = Math.Max(evadeChance - OverwhelmAbility.BONUS, 0);
    parryChance = Math.Max(parryChance - OverwhelmAbility.BONUS, 0);
    blockChance = Math.Max(blockChance - OverwhelmAbility.BONUS, 0);
}
```

## Attack Type Modifiers

### Ranged Attacks
- Evade chance divided by 5
- Cannot fumble
- Can be blocked (normal block mechanics)

### Spell Attacks
- Use spell hit chance calculation
- Can trigger Nature's Shield (100% block, 120° arc)
- Cannot fumble

## Special Defensive Abilities

### Engage
- Increases block chance to 95% minimum vs engaged target
- Requires endurance cost per block
- Target must not have been attacked recently
- Cancelled if insufficient endurance

### Blade Barrier
- 90% parry chance (overrides normal calculation)
- Still requires weapon to parry
- Not affected by other parry modifiers

### Nature's Shield
- 100% block chance vs ranged/spell
- 120° frontal arc requirement
- Triggered by specific combat styles

## Implementation Notes

### Performance Considerations
- Pre-calculate defense modifiers when possible
- Cache attacker count for multi-attacker calculations
- Use efficient frontal arc checks

### Critical Implementation Details
- Defense checks must maintain exact order
- Each successful defense short-circuits remaining checks
- Stealth attacks bypass most defensive checks
- Guard redirects the attack itself, not just damage

## Test Scenarios

### Scenario 1: Intercept Validation
```csharp
[Test]
public void Intercept_ShouldRedirectAttack_WhenChanceSucceeds()
{
    // Setup: 50% intercept chance, valid interceptor
    // Expected: Attack redirected to interceptor
    // Validates: Intercept mechanics and requirements
}
```

### Scenario 2: Multi-Attacker Defense Reduction
```csharp
[Test]
public void Defenses_ShouldReduce_WithMultipleAttackers()
{
    // Setup: Target with 30% evade vs 3 attackers
    // Expected: Evade reduced to 24% (30% - 2*3%)
    // Validates: Multi-attacker defense penalties
}
```

### Scenario 3: Guard Through Shield Size
```csharp
[Test]
public void Guard_ShouldIgnoreShieldSize_ForMultipleAttackers()
{
    // Setup: Small shield guard vs 3 attackers
    // Expected: Guard chance not reduced by attacker count
    // Validates: Guard immunity to shield size limits
}
```

### Scenario 4: Stealth Attack Bypass
```csharp
[Test]
public void StealthAttack_ShouldBypassAllDefenses_WhenValid()
{
    // Setup: Valid stealth attack vs target with defenses
    // Expected: All defenses skipped, direct hit calculation
    // Validates: Stealth attack special rules
}
```

## Change Log
- **2024-01-20**: Major expansion with detailed mechanics
  - Added intercept, guard, bodyguard mechanics
  - Added server configuration details
  - Added special ability interactions
  - Expanded multi-attacker formulas
  - Added defense penetration details

## References
- `AttackComponent.cs` - Core attack resolution implementation
- `GameLiving.cs` - Defense calculations
- `InterceptECSEffect.cs` - Intercept mechanics
- `ServerProperties.cs` - Configuration values
- Live patch notes 1.117C, 1.123, 1.125 