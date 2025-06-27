# Buff Effect System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Complete
- **Verification**: Code-verified from ECSGameEffect.cs, EffectListComponent.cs, SpellHandler.cs
- **Implementation**: Complete

## Overview

**Game Rule Summary**: The buff effect system manages all temporary magical effects on characters - from stat enhancements and speed boosts to debuffs and damage-over-time effects. When you cast or receive magical effects, this system determines how they stack with existing effects, how long they last, and whether stronger effects replace weaker ones. Understanding how effects interact helps you optimize your magical support and avoid conflicts when multiple players are casting similar spells.

The buff effect system manages all temporary effects on game entities including buffs, debuffs, auras, and damage over time effects. It uses an Entity Component System (ECS) architecture with complex stacking rules and effect management.

## Core Architecture

### Effect System Components

**Game Rule Summary**: Every magical effect in the game is created from a standard template that defines what it does, how long it lasts, and how effective it is. Effects track their caster, target, duration, and all the information needed to apply their benefits or penalties. This consistent structure allows the game to handle thousands of different spells and abilities using the same underlying system.

#### Base Effect Class
```csharp
public abstract class ECSGameEffect
{
    public eEffect EffectType { get; set; }
    public GameLiving Owner { get; protected set; }
    public GameLiving OwnerPlayer { get; }
    public long Duration { get; protected set; }
    public double Effectiveness { get; set; }
    public long StartTick { get; protected set; }
    public long ExpireTick { get; protected set; }
    public long NextTick { get; protected set; }
    public int PulseFreq { get; set; }
    public ushort Icon { get; }
    public string Name { get; }
}
```

#### Spell Effect Extension
```csharp
public class ECSGameSpellEffect : ECSGameEffect, IConcentrationEffect
{
    public ISpellHandler SpellHandler;
    public override ushort Icon => SpellHandler.Spell.Icon;
    public override string Name => SpellHandler.Spell.Name;
    public bool IsAllowedToPulse => NextTick > 0 && PulseFreq > 0;
}
```

### Effect Categories

**Game Rule Summary**: Effects are organized into specific types based on what they do to you. Buffs improve your abilities (like strength or armor), debuffs weaken you (like reducing stats or slowing movement), and special effects control your actions (like stuns or mezmerize). Each type has its own rules for how it interacts with other effects and how long it lasts.

#### Primary Effect Types
```csharp
public enum eEffect
{
    // Buffs
    DamageAdd,
    ArmorFactorBuff,
    DexterityBuff,
    StrengthBuff,
    ConstitutionBuff,
    QuicknessBuff,
    
    // Debuffs
    DexterityDebuff,
    StrengthDebuff,
    ConstitutionDebuff,
    ArmorFactorDebuff,
    
    // Special Effects
    Stun,
    Mez,
    Snare,
    Disease,
    DamageOverTime,
    HealOverTime,
    
    // System Effects
    Pulse,
    Concentration,
    StunImmunity,
    MezImmunity
}
```

### Effect List Component
```csharp
public class EffectListComponent
{
    private Dictionary<eEffect, List<ECSGameEffect>> _effects;
    private Dictionary<ushort, ECSGameEffect> _effectIdToEffect;
    private List<ECSGameSpellEffect> _concentrationEffects;
    private int _usedConcentration;
}
```

## Effect Stacking System

**Game Rule Summary**: When multiple people try to cast similar effects on you, the game uses specific rules to decide which effects stay active. Generally, stronger effects replace weaker ones, and effects from the same caster refresh their duration. If a weaker effect can't replace a stronger one, it waits in the background to activate when the stronger effect ends. This prevents effect spam while ensuring you always have the best possible magical enhancement.

### Stacking Decision Tree

```
1. Dead owners cannot receive effects
2. Ability effects always stack
3. Same spell ID or effect group:
   - Renew if same caster
   - Replace if different caster
4. Different spell, same effect type:
   - Check if overwritable
   - Use IsBetterThan comparison
   - Add as disabled if worse
```

### IsBetterThan Comparison
```csharp
public virtual bool IsBetterThan(ECSGameEffect effect)
{
    // Compare effectiveness-adjusted values
    return SpellHandler.Spell.Value * Effectiveness > 
           effect.SpellHandler.Spell.Value * effect.Effectiveness ||
           SpellHandler.Spell.Damage * Effectiveness > 
           effect.SpellHandler.Spell.Damage * effect.Effectiveness;
}
```

### Effect Addition Process
```csharp
public enum AddEffectResult
{
    Failed,
    Added,
    Updated,
    Removed,
    Replaced,
    AddedAsDisabled,
    RenewedActive,
    RenewedDisabled,
    OverwrittenActive,
    OverwrittenDisabled
}
```

### Disabled Effects

**Game Rule Summary**: When someone casts a weaker version of an effect you already have, it doesn't just disappear - it goes into a "disabled" state waiting its turn. If the stronger effect expires or gets dispelled, the disabled effect automatically activates so you don't lose your magical protection. This prevents gaps in your buffs when multiple casters are helping you.

Effects that are worse than currently active effects are added as disabled:
- Tracked separately from active effects
- Automatically re-enabled when better effect expires
- Maintains effect order for proper restoration
- Prevents effect loss in competitive casting scenarios

## Concentration System

**Game Rule Summary**: Concentration represents your ability to maintain ongoing magical effects that require constant mental focus. You have limited concentration points (usually around 20-35 depending on level), and each concentration spell uses some of them. Concentration effects also have a maximum range - if the caster moves too far away from you, the effect temporarily disables until they return to range. This creates tactical decisions about positioning and which effects to maintain.

### Concentration Management
```csharp
// Maximum concentration: 20 points (varies by character)
// Each spell uses 0-5 concentration points
public bool CheckConcentrationCost(bool checkOnly)
{
    int conc = CalculateNeededConcentration(target);
    
    if (conc > MaxConcentration || conc > Caster.Concentration)
        return false;
        
    if (!checkOnly)
        Caster.Concentration -= conc;
        
    return true;
}
```

### Range Checking
```csharp
// Range check interval: 2500ms
// Default range: BUFF_RANGE property (5000 units)
// Endurance regen range: 1500 units

if (!isWithinRadius)
{
    if (spellEffect.IsActive)
    {
        // Disable effect when out of range
        spellEffect.Disable();
    }
}
else if (spellEffect.IsDisabled)
{
    // Re-enable when back in range
    spellEffect.Enable();
}
```

### Concentration Points by Class
- Varies by level and class type
- Healers/Supporters: Higher concentration
- Hybrids: Medium concentration
- Melee/Tanks: Lower concentration

## Pulse Effects

**Game Rule Summary**: Pulse effects are spells that repeatedly affect multiple targets over time, like area damage spells or group regeneration effects. The caster creates a "pulse container" that manages individual effects on each target within range. When you cancel the pulse spell or move out of range, all the individual effects end immediately. These spells are efficient for affecting many targets but require the caster to maintain concentration and positioning.

### Pulse Effect Architecture
```csharp
public class ECSPulseEffect : ECSGameSpellEffect
{
    public Dictionary<GameLiving, ECSGameSpellEffect> ChildEffects { get; }
    
    public override void OnStartEffect()
    {
        Owner.ActivePulseSpells.AddOrUpdate(spell.SpellType, spell);
    }
}
```

### Pulse Mechanics
- Initial effect creates pulse container
- Child effects applied to targets in range
- Pulse frequency defined by spell
- Cancellation removes all child effects

## Special Effect Types

### Speed Debuffs

**Game Rule Summary**: Speed debuffs like snares pulse very frequently (4 times per second) to provide smooth movement changes rather than jerky updates. These effects also trigger immunity timers when they expire, preventing you from being permanently slowed by repeated applications.

```csharp
// Special handling for speed effects
if (spell.SpellType is eSpellType.SpeedDecrease)
{
    PulseFreq = 250;  // Fast pulse for smooth updates
    NextTick = 1 + Duration / 2 + StartTick + PulseFreq;
    TriggersImmunity = true;
}
```

### Damage Over Time

**Game Rule Summary**: Damage-over-time spells tick at regular intervals for their full damage amount. Unlike direct damage spells, DoTs can't normally critically hit (except with special abilities like Wild Arcana). Multiple DoTs from different casters can stack on the same target, making them effective for sustained damage in group combat.

- Ticks at spell-defined frequency
- Can critically hit with Wild Arcana RA
- Stacks from different casters
- Refreshes duration on reapplication

### Heal Over Time

**Game Rule Summary**: Heal-over-time effects work like damage spells but in reverse, providing steady healing over their duration. They can't critically hit but they do generate aggro on each tick, making them useful for tanks who want to maintain threat. Overhealing is tracked separately and doesn't provide additional benefit.

- Cannot critically hit
- Generates aggro on tick
- Overhealing tracked separately
- Stacks with different spell lines

### Crowd Control

**Game Rule Summary**: Crowd control effects like stuns and mezmerize have special immunity rules to prevent permanent lockdown. After a CC effect ends naturally, you gain immunity to that type of control for a period (usually 60 seconds). NPCs use diminishing returns instead - each successive CC effect lasts half as long as the previous one.

- Immunity granted on expiration
- Standard immunity: 60 seconds
- Style stun immunity: 5x duration
- Diminishing returns for NPCs

## Buff Categories and Stacking

**Game Rule Summary**: Buffs are organized into categories that determine how they stack with each other and with item bonuses. Base buffs compete with each other and share caps with your equipment bonuses. Spec buffs stack with base buffs but not with other spec buffs. Debuffs always apply but only the worst one of each type affects you. Special "other" buffs like Paladin chants bypass normal limits and stack with everything.

### Property Buff Categories
```csharp
public enum eBuffBonusCategory
{
    BaseBuff,      // Base stat/AF buffs
    SpecBuff,      // Spec line buffs
    Debuff,        // Standard debuffs
    OtherBuff,     // Uncapped buffs
    SpecDebuff,    // Enhanced debuffs
    AbilityBuff    // RA/ML buffs
}
```

### Stacking Rules by Category

#### Base Buffs
- Single stat buffs (Str, Con, Dex, etc.)
- Base armor factor buffs
- Only highest applies
- Shares cap with items

#### Spec Buffs
- Dual stat buffs (Str/Con, Dex/Qui)
- Spec line armor factor
- Acuity buffs
- Separate cap from base

#### Debuffs
- Always positive values (subtracted in calc)
- Only highest applies
- Different effectiveness vs buffs/stats

## Effect Groups

**Game Rule Summary**: Effect groups allow different spells that do similar things to be treated as the same effect for stacking purposes. For example, all strength buffs from different spell lines belong to the same effect group, so only the best one applies even if they have different names and come from different classes. This prevents stacking multiple similar buffs from different sources.

Effect groups allow different spells to stack as same type:

```csharp
// Common Effect Groups
1     // Base AF buffs
2     // Spec AF buffs
4     // Strength buffs
9     // Cold resist buffs
200   // Acuity buffs
201   // Constitution buffs
99999 // Non-stacking damage adds
```

## Implementation Details

### Effect Storage
```csharp
// Effects stored by type for efficient lookup
Dictionary<eEffect, List<ECSGameEffect>> _effects;

// Icon lookup for client updates
Dictionary<ushort, ECSGameEffect> _effectIdToEffect;

// Concentration effects tracked separately
List<ECSGameSpellEffect> _concentrationEffects;
```

### Update System
```csharp
public enum PlayerUpdate
{
    NONE = 0,
    HEALTH = 1,
    ENDURANCE = 2,
    MANA = 4,
    CONCENTRATION = 8,
    SPEED = 16,
    STATE = 32,
}
```

### Thread Safety
- Effects use concurrent collections
- Concentration list has dedicated lock
- Update batching for performance

## Save/Restore System

### Effect Persistence
```csharp
public virtual DbPlayerXEffect GetSavedEffect()
{
    DbPlayerXEffect eff = new()
    {
        Var1 = SpellHandler.Spell.ID,
        Var2 = Effectiveness,
        Var3 = (int)SpellHandler.Spell.Value,
        IsHandler = true,
        SpellLine = SpellHandler.SpellLine.KeyName,
        Duration = (int)(ExpireTick - GameLoop.GameLoopTime)
    };
    
    return eff;
}
```

### Restoration Rules
- Self-only concentration effects saved
- No effects from other casters
- Reserved spell lines excluded
- Duration adjusted for time offline

## Performance Optimizations

### Tick Management
- Effects only process when needed
- Batch updates for multiple effects
- Efficient range checking algorithms
- Minimal allocations in hot paths

### Client Updates
- Updates batched by type
- Only send changes
- Icon management for quick lookup
- Compressed effect data

## Test Scenarios

### Basic Buff Application
```
1. Cast single target buff
2. Verify effect added
3. Check property modified
4. Confirm duration/icon
```

### Stacking Test
```
1. Apply base Strength buff
2. Apply spec Str/Con buff
3. Verify both active
4. Apply stronger base buff
5. Verify replacement
```

### Concentration Test
```
1. Cast concentration buff
2. Move out of range
3. Verify effect disabled
4. Move back in range
5. Verify effect restored
```

### Pulse Effect Test
```
1. Cast pulsing spell
2. Verify child effects created
3. Target enters range
4. Verify effect applied
5. Cancel pulse
6. Verify all effects removed
```

## Edge Cases

### Multiple Overwrites
- Track all disabled effects
- Restore best available on removal
- Maintain proper effect order

### Concentration Limits
- Cannot exceed max concentration
- Older effects auto-cancel if needed
- Priority system for important buffs

### Death Handling
- Non-positive effects persist
- Concentration effects cancel
- Resurrection illness applied

### Zone Transitions
- Self buffs persist
- Concentration range reset
- Pulse effects interrupted

## Change Log

### 2025-01-20
- Initial documentation created
- Complete stacking logic documented
- Added concentration mechanics
- Included save/restore system

## References
- ECSGameEffect.cs: Base effect implementation
- EffectListComponent.cs: Effect management
- SpellHandler.cs: Spell effect creation
- ConcentrationList.cs: Legacy concentration tracking 