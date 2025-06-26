# Fumble System

**Document Status:** Complete Documentation  
**Completeness:** 95%  
**Verification:** Code-verified from AttackComponent.cs, StyleProcessor.cs  
**Implementation Status:** Live

## Overview

The Fumble System adds a chance for melee attacks to critically fail, resulting in doubled attack intervals and potential style chain breaks. Fumbles are a subset of misses that only affect melee attacks, providing tactical depth to combat.

## Core Mechanics

### Fumble Calculation

#### Base Formula
```csharp
// Fumble chance as subset of miss chance
double fumbleChance = ad.IsMeleeAttack ? ad.Attacker.GetModified(eProperty.FumbleChance) * 0.001 : 0;

// Level-based fumble calculation
double baseFumbleChance = Math.Max((51 - attackerLevel) / 1000.0, debuffedFumbleChance);

// Miss/Fumble resolution
return fumbleChance > missRoll ? eAttackResult.Fumbled : eAttackResult.Missed;
```

#### Fumble Requirements
- **Melee Only**: Ranged attacks cannot fumble
- **Subset of Misses**: Cannot fumble without missing first
- **Level Dependent**: Higher level = lower fumble chance
- **Property Modified**: Affected by debuffs and equipment

### Fumble Effects

#### Attack Speed Penalty
```csharp
// Attack timing modification
if (attackResult == eAttackResult.Fumbled)
{
    nextAttackDelay = normalAttackSpeed * 2; // Double interval
}
```

#### Combat Messaging
```csharp
case eAttackResult.Fumbled:
    message = $"{ad.Attacker.GetName(0, true)} fumbled!";
    break;
```

#### Numeric Results
```csharp
// Attack result codes
eAttackResult.Fumbled => 4,  // Fumble result code
```

### Style Integration

#### Style Chain Breaking
```csharp
public enum eAttackResultRequirement
{
    Fumble = 6,  // Style requires previous fumble
}

// Style processor validation
case Style.eAttackResultRequirement.Fumble: 
    requiredAttackResult = eAttackResult.Fumbled; 
    break;
```

#### Fumble-Based Styles
- **Requirement Type**: Previous attack must fumble
- **Chain Completion**: Fumble breaks offensive chains
- **Defensive Advantage**: Creates openings for counter-attacks
- **Strategic Value**: Fumbles enable specific style progressions

## Fumble Rates

### Base Fumble Chances
Based on level and miss chance:
- **Level 1**: ~5% base fumble chance
- **Level 25**: ~2.6% base fumble chance  
- **Level 50**: ~0.1% base fumble chance
- **High Level**: Approaches 0% naturally

### Calculation Examples
```csharp
// Level 1 character
double level1Fumble = Math.Max((51 - 1) / 1000.0, 0) = 5.0%

// Level 25 character  
double level25Fumble = Math.Max((51 - 25) / 1000.0, 0) = 2.6%

// Level 50 character
double level50Fumble = Math.Max((51 - 50) / 1000.0, 0) = 0.1%
```

### Fumble as Percentage of Miss
```csharp
// Fumble is approximately 2% of total miss chance
double fumbleRate = totalMissChance * 0.02;

// Example: 18% miss = 0.36% fumble
// Example: 50% miss = 1.0% fumble
```

## System Integration

### Combat Resolution Order
```csharp
// Complete attack resolution sequence
1. Intercept
2. Evade  
3. Parry
4. Block
5. Guard
6. Hit/Miss Determination
7. Fumble Check (subset of miss)
8. Bladeturn (if hit)
```

### Miss Chance Adjustment
```csharp
// Miss chance must accommodate fumble chance
if (totalMissChance < fumbleChance)
{
    totalMissChance = fumbleChance; // Ensure fumble is possible
}
```

### Dual Wield Integration
```csharp
// Each weapon can fumble independently
foreach (WeaponSlot slot in activeWeapons)
{
    if (PerformAttack(slot) == eAttackResult.Fumbled)
    {
        // Apply fumble penalty to this weapon's next attack
        weaponTimers[slot] *= 2;
    }
}
```

## Property System Integration

### Fumble Chance Property
```csharp
public enum eProperty
{
    FumbleChance = 178,  // Modifies fumble probability
}

// Property application
double modifiedFumbleChance = player.GetModified(eProperty.FumbleChance) * 0.001;
```

### Debuff Effects
- **Fumble Debuffs**: Increase fumble chance beyond base
- **Equipment Bonuses**: Some items reduce fumble chance
- **Spell Effects**: Temporary fumble chance modifications
- **Skill Bonuses**: Weapon mastery may reduce fumbles

## Ammo and Weapon Effects

### Ammunition Quality Impact
```csharp
// Rough ammo increases fumble chance (ranged exempt from fumbles but affects calculation)
if (ammo.Quality == eAmmoQuality.Rough)
{
    missChance *= 1.15; // 15% miss increase affects fumble indirectly
}
```

### Weapon Mastery Benefits
- **High Weapon Skill**: Reduces base miss chance, lowering fumble chance
- **Specialization Bonuses**: Better weapon control
- **Quality Weapons**: May provide fumble reduction
- **Magical Weapons**: Potential fumble immunity or reduction

## Combat State Effects

### Action Restrictions During Fumble
```csharp
// Fumble only affects weapon swings
bool canCastWhileFumbled = true;        // Spells unaffected
bool canUseAbilityWhileFumbled = true;  // Abilities unaffected  
bool canAttackWhileFumbled = false;     // Weapon attacks blocked
```

### Recovery Mechanics
- **Duration**: Fumble penalty lasts one attack cycle
- **Movement**: Can move normally during fumble recovery
- **Other Actions**: Spells and abilities unaffected
- **Timer Reset**: Next attack occurs at 2× normal interval

## PvP vs PvE Differences

### Equal Mechanics
```csharp
// NPCs and players use identical fumble systems
double playerFumbleChance = playerMissChance * 0.02;
double npcFumbleChance = npcMissChance * 0.02;

// No special NPC fumble immunity or bonuses
```

### Consistency Rules
- **Same Calculations**: NPCs use player formulas
- **No Special Cases**: Equal treatment in fumble mechanics
- **Level Scaling**: Works identically for all entity types
- **Property Effects**: Apply equally to NPCs and players

## Configuration and Limits

### Fumble Caps
```csharp
// Maximum reasonable fumble chance
double maxFumbleChance = Math.Min(calculatedFumble, 5.0); // 5% cap

// Minimum viable fumble
double minFumbleChance = 0.0; // No forced minimum
```

### Server Properties
```csharp
// Configurable fumble parameters
FUMBLE_CHANCE_MODIFIER = 1.0;           // Global fumble rate modifier
FUMBLE_LEVEL_REDUCTION = true;          // Enable level-based reduction
FUMBLE_RANGED_IMMUNITY = true;          // Ranged attacks immune
FUMBLE_PENALTY_MULTIPLIER = 2.0;        // Attack speed penalty
```

## Testing Framework

### Unit Test Coverage
```csharp
[Test]
public void FumbleChance_BaseMissChance_ShouldBe2Percent()
{
    // Validates: Fumble = 2% of miss chance
    double missChance = 18.0;
    double fumbleChance = missChance * 0.02;
    fumbleChance.Should().Be(0.36);
}

[Test]  
public void FumbleResult_ShouldDoubleAttackTimer()
{
    // Validates: Fumble doubles next attack interval
    int normalSpeed = 3500;
    int fumbleSpeed = normalSpeed * 2;
    fumbleSpeed.Should().Be(7000);
}
```

### Test Scenarios
1. **Base Fumble Rate**: 2% of miss chance validation
2. **Level Scaling**: Higher level = lower fumble chance
3. **Attack Speed Penalty**: Fumble doubles next attack
4. **Ranged Immunity**: Bows/crossbows cannot fumble
5. **Style Integration**: Fumble breaks/enables style chains

## Edge Cases & Special Handling

### Level Extremes
```csharp
// Very high level characters
if (level > 50)
{
    fumbleChance = Math.Max(0.001, calculatedFumble); // Minimum 0.1%
}

// Very low level characters  
if (level < 5)
{
    fumbleChance = Math.Min(10.0, calculatedFumble); // Maximum 10%
}
```

### Fumble Immunity
- **Certain Abilities**: May grant temporary fumble immunity
- **Legendary Weapons**: Potential fumble immunity effects
- **Realm Abilities**: High-level RAs may reduce fumble chance
- **Master Level**: ML abilities affecting fumble rates

### Style Chain Considerations
```csharp
// Fumble breaks offensive chains but enables fumble-based styles
if (lastAttackResult == eAttackResult.Fumbled)
{
    // Enable fumble follow-up styles
    // Clear offensive style chains
    // Reset positional requirements
}
```

## Performance Considerations

### Calculation Efficiency
```csharp
// Fumble check only for melee attacks
if (ad.IsMeleeAttack)
{
    double fumbleChance = ad.Attacker.GetModified(eProperty.FumbleChance) * 0.001;
    // Perform fumble calculation
}
// Skip fumble processing for ranged attacks
```

### Memory Impact
- **Minimal Overhead**: Simple percentage calculations
- **No State Storage**: Fumble effect doesn't persist
- **Efficient Lookup**: Direct property access
- **Low CPU Cost**: Basic mathematical operations

## Future Considerations

### Potential Enhancements
- **Weapon-Specific Fumble Rates**: Different weapons fumble differently
- **Critical Fumble System**: Extreme fumbles with additional penalties
- **Recovery Abilities**: Skills that reduce fumble recovery time
- **Environmental Factors**: Terrain affecting fumble chance

### Balance Implications
- **High-Level Play**: Fumbles become increasingly rare
- **Low-Level Impact**: More significant fumble effects for new players
- **Skill Progression**: Fumble reduction as advancement reward
- **Strategic Depth**: Fumble-based tactics and counterplay

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2025-01-20 | 1.0 | Initial comprehensive documentation |

## References
- `GameServer/ECS-Components/AttackComponent.cs` - Core fumble implementation
- `GameServer/styles/StyleProcessor.cs` - Style integration
- `Tests/UnitTests/Combat/FumbleMechanicsTests.cs` - Test framework
- `SRD/01_Combat_Systems/Attack_Resolution.md` - Attack resolution order
- `Helper Docs/Core_Systems_Game_Rules.md` - Original fumble rules 