# Effect Stacking Logic

## Document Status
- Status: Under Development
- Implementation: Complete

## Overview
The effect stacking logic manages how spell effects interact, overwrite, disable, and re-enable each other. This is one of the most complex systems in OpenDAoC with intricate rules for handling multiple scenarios.

## Core Stacking Algorithm

### Effect Comparison (IsBetterThan)

Effects are compared using a two-part formula:
```csharp
public virtual bool IsBetterThan(ECSGameEffect effect)
{
    return SpellHandler.Spell.Value * Effectiveness > effect.SpellHandler.Spell.Value * effect.Effectiveness ||
           SpellHandler.Spell.Damage * Effectiveness > effect.SpellHandler.Spell.Damage * effect.Effectiveness;
}
```

**Key Points**:
- Compares `SpellValue × Effectiveness` first
- Falls back to `SpellDamage × Effectiveness` 
- Uses actual effectiveness, not base values
- OR logic - either condition makes it "better"

### AddEffect Decision Tree

When adding an effect, the system follows this decision tree:

#### 1. Dead Owner Check
```csharp
if (!Owner.IsAlive)
    return AddEffectResult.Failed;
```

#### 2. Ability Effects 
```csharp
if (effect is ECSGameAbilityEffect)
{
    // Always add without stacking logic
    existingEffects.Add(effect);
    return AddEffectResult.Added;
}
```

#### 3. Same Spell ID/Effect Group
```csharp
// Exact spell ID or same effect group
if (e.SpellHandler.Spell.ID == newSpell.ID || 
   (newSpell.EffectGroup > 0 && e.SpellHandler.Spell.EffectGroup == newSpell.EffectGroup))
```

**Handling**:
- **Concentration**: Fail if not re-enabling
- **Pulse**: Check ActivePulseSpells registry
- **Poison DoTs**: Extend duration
- **Speed Debuffs**: Silent renewal
- **Others**: Replace silently

#### 4. Special Effect Types
```csharp
// Savage/ArmorAbsorption - one per spell type only
if (effect.EffectType is eEffect.SavageBuff or eEffect.ArmorAbsorptionBuff)
{
    if (!existingGameEffects.Where(e => e.SpellHandler.Spell.SpellType == effect.SpellHandler.Spell.SpellType).Any())
        return AddEffectResult.Added;
    else
        return AddEffectResult.Failed;
}
```

#### 5. Overwritable Effects
```csharp
if (existingSpellHandler.IsOverwritable(effect) || effect.EffectType is eEffect.MovementSpeedDebuff)
{
    foundIsOverwritableEffect = true;
    // Handle stacking logic
}
```

### IsOverwritable Logic

```csharp
public virtual bool IsOverwritable(ECSGameEffect compare)
{
    // Effect groups always overwrite
    if (Spell.EffectGroup != 0 || compare.SpellHandler.Spell.EffectGroup != 0)
        return Spell.EffectGroup == compare.SpellHandler.Spell.EffectGroup;
        
    // Same spell type check
    if (compare.SpellHandler.Spell.SpellType != Spell.SpellType)
        return false;
        
    return true;
}
```

## Effect State Management

### Effect States
- **Starting**: Being added to effect list
- **Active**: Currently providing benefits  
- **Disabled**: Present but not active
- **Enabling**: Being re-enabled
- **Stopping**: Being removed
- **Disabling**: Being moved to disabled

### Disabled Effect System

#### When Effects Get Disabled
1. Better effect from different caster (helpful spells)
2. Concentration range exceeded
3. Potion effects replaced by spells

#### Re-enabling Logic
```csharp
// When concentration comes back in range
if (spellEffect.IsDisabled)
{
    bool isBest = spellHandler.Spell.Value > enabledEffect.SpellHandler.Spell.Value;
    
    if (isBest)
    {
        spellEffect.Enable();
        enabled?.Disable();
    }
}
```

## Special Case Handling

### Bladeturn Effects
```csharp
// PBT only replaces itself
// Self-cast bladeturns never overwritten
if (existingSpell.Target is not eSpellTarget.SELF)
{
    existingEffect.Stop();
    result = AddEffectResult.Added;
}
```

### Ablative Armor
```csharp
// Compare total absorption value
if (newSpell.Value * AblativeArmorSpellHandler.ValidateSpellDamage((int)newSpell.Damage) >
    existingAblativeEffect.RemainingValue * AblativeArmorSpellHandler.ValidateSpellDamage((int)existingSpell.Damage))
{
    existingEffect.Stop();
    result = AddEffectResult.Added;
}
```

### Movement Speed Debuffs
- Always overwritable regardless of IsOverwritable()
- Decreasing effectiveness over time
- Factor: `2.0 - (elapsed / (duration * 0.5))`

### Concentration Effects
```csharp
// Cannot stack concentration effects
if (effect.IsConcentrationEffect() && !effect.IsEnabling)
    return AddEffectResult.Failed;

// Range checking every 2.5 seconds
int radiusToCheck = spellType is eSpellType.EnduranceRegenBuff ? 1500 : BUFF_RANGE;
```

## Caster Relationship Rules

### Same Caster
- Replace existing effect
- Cannot have multiple concentration effects

### Different Caster
- **Helpful spells**: Disable worse, add better as disabled if worse
- **Harmful spells**: Replace if better, reject if worse
- **Potions**: Special stacking rules

### Potion/Item Effects
```csharp
if (SpellLine.KeyName is GlobalSpellsLines.Potions_Effects)
{
    // Always disable, never stop
    if (effect.IsBetterThan(existingEffect))
        existingEffect.Disable();
    else
        result = AddEffectResult.Disabled;
}
```

## Silent Effect Renewal

### When Effects Renew Silently
```csharp
if ((!existingEffect.IsDisabled && !effect.IsEnabling && !newSpell.IsPulsing) ||
    existingSpell.SpellType is eSpellType.SpeedDecrease)
{
    existingEffect.IsSilent = true;
    effect.IsSilent = true;
    // Complex queue management
}
```

**Prevents**:
- Animation spam from recasting
- Effectiveness changes from resurrection illness
- Champion debuff spec forcing
- Speed debuff effectiveness updates

## Effect Groups

### Common Groups
```
1     - Base AF buffs
2     - Spec AF buffs  
4     - Strength buffs
99999 - Non-stacking damage adds
```

### Group Rules
- Same group = always overwritable
- Group 0 = use spell type matching
- Group 99999 = special stacking behavior

## Test Scenarios

### Basic Stacking
1. Same spell, same caster → renew duration
2. Same spell, different caster → replace  
3. Better spell, same type → replace worse
4. Worse spell, same type → reject/disable

### Cross-Caster
1. Helpful, different caster, better → disable existing
2. Helpful, different caster, worse → add disabled
3. Harmful, different caster → replace if better

### Special Cases
1. Concentration range → disable/re-enable
2. Pulse parent stop → cancel children
3. Ablative comparison → value × absorption%
4. Speed debuff → silent renewal

## Change Log
- Initial comprehensive documentation
- Added disabled effect management
- Documented special cases
- Added caster relationship rules 