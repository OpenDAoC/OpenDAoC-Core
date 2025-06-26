# Damage Add & Shield System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview
Damage adds and damage shields are supplemental damage effects that trigger during combat. Damage adds provide additional damage when attacking, while damage shields reflect damage back to attackers. Both systems use similar mechanics but differ in their trigger conditions and stacking behavior.

## Core Mechanics

### Damage Add System

#### Overview
Damage adds trigger when the owner successfully hits a target with a melee or ranged attack.
- **Effect Type**: `eEffect.DamageAdd`
- **Spell Type**: `eSpellType.DamageAdd`
- **Trigger**: On `HitUnstyled` or `HitStyle` attack results

#### Damage Calculation
```csharp
// Base variance for damage adds
Variance = 0.85 to 1.42 (0.85 * 5/3)

// Effectiveness including buff bonus
Effectiveness *= 1 + BuffEffectiveness * 0.01

// Fixed damage calculation
Damage = Spell.Damage * Variance * Effectiveness * AttackInterval * 0.001

// Percentage damage (negative spell damage values)
if (Spell.Damage < 0)
    Damage = AttackDamage * Spell.Damage / -100
```

**Key Points**:
- Attack interval scales damage (faster weapons = less damage per hit)
- Variance provides 85% to 142% damage range
- Buff effectiveness increases damage
- Not affected by resistances

#### Stacking Mechanics

##### Effect Group 99999 (Unaffected by Stacking)
- Usually RA-based damage adds (Anger of the Gods, etc.)
- Apply at 100% effectiveness regardless of other adds
- Do not count towards stacking penalties

##### Regular Damage Adds
```csharp
// Sort by damage (highest first)
damageAddEffects.OrderByDescending(e => e.SpellHandler.Spell.Damage)

// First regular add: 100% effectiveness
// Additional adds: 50% effectiveness
effectiveness = numRegularDmgAddsApplied > 0 ? 0.5 : 1.0
```

**Stacking Order**:
1. Apply all EffectGroup 99999 adds first at 100%
2. Apply regular adds in damage order
3. First regular add at 100%
4. All subsequent adds at 50%

### Damage Shield System

#### Overview
Damage shields trigger when the owner is hit by an attack, reflecting damage to the attacker.
- **Effect Type**: `eEffect.FocusShield`
- **Spell Type**: `eSpellType.DamageShield`
- **Trigger**: When owner takes damage from any attack

#### Damage Calculation
```csharp
// Base variance for damage shields
Variance = 0.9 to 1.5 (0.9 * 5/3)

// Same effectiveness and damage calculation as damage adds
Effectiveness *= 1 + BuffEffectiveness * 0.01
Damage = Spell.Damage * Variance * Effectiveness * AttackInterval * 0.001
```

**Key Points**:
- Slightly wider variance range than damage adds
- Uses attacker's weapon speed for scaling
- Not affected by resistances
- Can trigger multiple shields per attack

#### Stacking Behavior
- All active damage shields trigger independently
- No stacking penalties between shields
- Each calculates damage separately
- Total reflected damage is sum of all shields

### Shared Mechanics

#### Attack Data Creation
```csharp
protected AttackData CreateAttackData(double damage, GameLiving attacker, GameLiving target)
{
    return new()
    {
        Attacker = attacker,
        Target = target,
        Damage = (int) damage,
        DamageType = Spell.DamageType,    // From spell definition
        SpellHandler = this,
        AttackType = AttackData.eAttackType.Spell,
        AttackResult = eAttackResult.HitUnstyled
    };
}
```

#### Damage Application
```csharp
// Apply combat modifiers
ad.Damage = (int)(ad.Damage * effectiveness);

// Deal damage
target.OnAttackedByEnemy(ad);
attacker.DealDamage(ad);

// Show special animation
foreach (GamePlayer player in ad.Attacker.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
    player.Out.SendCombatAnimation(null, target, 0, 0, 0, 0, 0x0A, target.HealthPercent);
```

### Special Cases

#### Ablative Armor Interaction
- Damage adds and shields bypass ablative armor
- Deal direct damage to health
- Do not consume ablative charges

#### Percentage-Based Damage Adds
```csharp
// Negative spell damage indicates percentage
if (Spell.Damage < 0)
{
    // Convert to positive percentage
    percentage = Math.Abs(Spell.Damage);
    damage = attackDamage * percentage / 100;
}
```

#### Shield of Immunity (RR5)
- Reduces damage by 90% from melee/ranged/archery
- Affects damage before adds/shields calculate
- Does not prevent add/shield triggers

### Damage Type System

#### Element Types
Damage adds and shields can use any damage type:
- **Physical**: Slash, Thrust, Crush
- **Magical**: Heat, Cold, Matter, Body, Spirit, Energy
- **Determines**: Resistance checks (though adds/shields ignore resist)

#### Animation Effects
- Each damage type has specific visual effect
- Shows on successful trigger
- Client effect ID from spell definition

### Implementation Details

#### Handler Architecture
```csharp
public abstract class AbstractDamageAddSpellHandler : SpellHandler
{
    public abstract void Handle(AttackData attackData, double effectiveness);
    
    protected virtual bool AreArgumentsValid(AttackData attackData, 
        out GameLiving attacker, out GameLiving target)
    {
        // Validation logic
    }
}
```

#### Event Integration
```csharp
// In WeaponAction.cs
private static void HandleDamageAdds(AttackData ad)
{
    List<ECSGameSpellEffect> damageAddEffects = 
        ad.Attacker.effectListComponent.GetSpellEffects(eEffect.DamageAdd);
        
    // Process stacking logic
    // Call Handle() on each effect
}

private static void HandleDamageShields(AttackData ad)
{
    List<ECSGameSpellEffect> damageShieldEffects = 
        ad.Target.effectListComponent.GetSpellEffects(eEffect.FocusShield);
        
    // Call Handle() on each shield
}
```

### Database Schema

#### Spell Properties
```sql
-- Core properties
Damage: Fixed damage amount (negative for percentage)
Duration: Effect duration in seconds
DamageType: Element type (eDamageType enum)
EffectGroup: Stacking group (99999 = unaffected)

-- Visual properties
ClientEffect: Animation ID
Icon: Spell icon ID
Message1: Message to caster
Message2: Message to area
```

#### Common Values
```sql
-- Damage adds
Damage: 1-50 typical range
EffectGroup: 0 (normal) or 99999 (no stacking)

-- Damage shields
Damage: 10-150 typical range
EffectGroup: 0 (all shields stack)
```

## System Interactions

### With Attack System
- Triggers checked after attack resolution
- Uses attack interval for damage scaling
- Respects hit/miss results
- Independent of critical strikes

### With Effect System
- Managed through EffectListComponent
- Standard buff duration/concentration rules
- Can be dispelled/sheared normally
- Subject to buff caps

### With Resistance System
- **Important**: Adds/shields ignore all resistances
- Damage type used for animation only
- No resistance or vulnerability applies
- Cannot be resisted or diminished

### With Conversion System
- Damage from adds/shields can be converted
- Follows standard conversion mechanics
- Affects final damage dealt

## Balance Considerations

### Damage Add Balance
- First add provides full benefit
- Diminishing returns prevent stacking abuse
- RA adds exempt to preserve uniqueness
- Attack speed scaling prevents fast weapon dominance

### Damage Shield Balance
- All shields stack without penalty
- Encourages defensive buff diversity
- Attack speed scaling maintains balance
- Range limitation prevents kiting abuse

## Test Scenarios

### Damage Add Tests
1. **Single Add**: Verify base damage calculation
2. **Multiple Adds**: Test 50% effectiveness stacking
3. **RA Add Exemption**: Confirm 100% for group 99999
4. **Speed Scaling**: Compare fast vs slow weapons

### Damage Shield Tests
1. **Single Shield**: Verify reflection mechanics
2. **Multiple Shields**: Confirm independent triggers
3. **Damage Types**: Test various element types
4. **Range Check**: Verify attacker must be alive

### Edge Cases
1. **Dead Attacker**: Shield shouldn't trigger
2. **Percentage Adds**: Test negative damage values
3. **Simultaneous Triggers**: Multiple adds same attack
4. **Buff Effectiveness**: Verify damage scaling

## Visual Feedback

### Combat Messages
```csharp
// Damage add
"You hit {0} for {1} extra damage!"
"{0} does {1} extra damage to you!"

// Damage shield  
"Your damage shield does {0} damage to {1}!"
"{0} is struck by {1}'s damage shield for {2} damage!"
```

### Animations
- Special combat animation (0x0A) for procs
- Element-specific visual effects
- Shows damage numbers to nearby players

## Performance Notes

### Optimization Strategies
- Effects cached per attack
- Stacking calculated once
- Damage variance pre-computed
- Minimal object allocation

### Profiling Results
- Negligible impact on combat performance
- Linear scaling with active effects
- No memory leaks identified

## Change Log
- Initial documentation based on damage add/shield analysis
- Includes complete stacking mechanics
- Documents percentage-based damage adds
- Covers shield of immunity interaction 