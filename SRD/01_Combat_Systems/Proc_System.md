# Proc System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview

**Game Rule Summary**: The proc system allows your weapons and armor to trigger magical effects during combat, adding an element of chance and excitement to every fight. When you hit an enemy with a weapon that has a "proc," there's a chance it will cast a spell automatically - this could be extra damage, a debuff on your target, or even a beneficial effect on yourself. Similarly, some armor pieces have "reactive procs" that trigger when you're hit, potentially stunning your attacker or healing you. The chance for procs to trigger depends on your weapon's speed (slower weapons have higher per-hit chances but attack less often), and some weapons can have multiple different procs. Understanding procs helps you choose equipment that complements your fighting style and adds unpredictable elements that can turn the tide of battle.

The proc (procedure) system allows weapons and armor to trigger magical effects during combat. Procs can be offensive (triggering when attacking) or defensive (triggering when being attacked). The system includes weapon procs, armor reactive procs, spell-based procs, and style procs with specific chance calculations and stacking rules.

## Core Mechanics

### Proc Types

#### Weapon Procs
- **Trigger**: On successful melee/ranged attack
- **Requirements**: Hit result (HitStyle or HitUnstyled)
- **Database Fields**: `ProcSpellID`, `ProcSpellID1`, `ProcChance`
- **Default Chance**: 10% if ProcChance not specified

#### Armor Reactive Procs
- **Trigger**: When struck on specific armor piece
- **Requirements**: Armor hit in combat
- **Shield Special**: Triggers on block
- **Same fields as weapon procs

#### Offensive Spell Procs
- **Effect Type**: `eEffect.OffensiveProc`
- **Spell Type**: `eSpellType.OffensiveProc`
- **Trigger**: When owner attacks
- **Target**: Based on proc spell target type

#### Defensive Spell Procs
- **Effect Type**: `eEffect.DefensiveProc`
- **Spell Type**: `eSpellType.DefensiveProc`
- **Trigger**: When owner is attacked
- **Target**: Based on proc spell target type

### Chance Calculation

#### Weapon Proc Chance
```csharp
// Base formula: 2.5% per speed point (SPD_ABS/10)
double procChance = (weapon.ProcChance > 0 ? weapon.ProcChance : 10) * (weapon.SPD_ABS / 35.0) * 0.01;

// Examples:
// 3.5 speed weapon: 10% base chance
// 5.0 speed weapon: 14.3% base chance
// 2.0 speed weapon: 5.7% base chance
```

**Key Points**:
- Speed normalized around 3.5 (35 SPD_ABS)
- Faster weapons have lower per-hit chance
- Overall proc rate balanced by attack frequency
- Custom ProcChance overrides base 10%

#### Spell Proc Chance
```csharp
// Frequency is stored as percentage * 100
int baseChance = Spell.Frequency / 100;

if (baseChance < 1)
    baseChance = 1;

if (Util.Chance(baseChance))
{
    // Trigger proc
}
```

#### Reactive Armor Proc Chance
```csharp
int chance = reactiveItem.ProcChance > 0 ? reactiveItem.ProcChance : 10;

if (Util.Chance(chance))
{
    // Trigger reactive proc
}
```

### Proc Resolution

#### Weapon Proc Execution
```csharp
public virtual void CheckWeaponMagicalEffect(AttackData ad, DbInventoryItem weapon)
{
    // Check hit result
    if (weapon == null || (ad.AttackResult != eAttackResult.HitStyle && 
        ad.AttackResult != eAttackResult.HitUnstyled))
        return;
        
    // Calculate chance
    double procChance = (weapon.ProcChance > 0 ? weapon.ProcChance : 10) * 
                       (weapon.SPD_ABS / 35.0) * 0.01;
    
    // Proc #1
    if (procSpell != null && Util.ChanceDouble(procChance))
        StartWeaponMagicalEffect(weapon, ad, line, weapon.ProcSpellID, false);
        
    // Proc #2 (independent roll)
    if (procSpell1 != null && Util.ChanceDouble(procChance))
        StartWeaponMagicalEffect(weapon, ad, line, weapon.ProcSpellID1, false);
}
```

#### Reactive Proc Execution
```csharp
public virtual void TryReactiveEffect(DbInventoryItem reactiveItem, GameLiving target)
{
    // Level check
    int requiredLevel = reactiveItem.Template.LevelRequirement > 0 ? 
                       reactiveItem.Template.LevelRequirement : 
                       Math.Min(50, reactiveItem.Level);
                       
    if (requiredLevel > Level)
        return;
        
    SpellLine reactiveEffectLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
    
    // Process each proc spell
    if (reactiveItem.ProcSpellID != 0)
    {
        Spell spell = SkillBase.FindSpell(reactiveItem.ProcSpellID, reactiveEffectLine);
        int chance = reactiveItem.ProcChance > 0 ? reactiveItem.ProcChance : 10;
        
        if (Util.Chance(chance))
        {
            ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(this, spell, reactiveEffectLine);
            spellHandler?.StartSpell(target, reactiveItem);
        }
    }
}
```

### Special Proc Mechanics

#### Multiple Procs
- Weapons can have two independent procs (ProcSpellID and ProcSpellID1)
- Each proc rolls independently
- Both can trigger on same attack
- No shared cooldown

#### Poison Procs
- **Separate System**: Uses PoisonSpellID field
- **Consumes Charges**: PoisonCharges decremented
- **Immunity Check**: Blocked by Remedy effect
- **Always Triggers**: No chance roll if charges remain
- **Spymaster Bonus**: 15% chance to not consume charge

#### Spell Line Requirements
- All procs use `GlobalSpellsLines.Item_Effects`
- Enables special variance calculations
- Allows item-specific spell modifications
- Level requirement based on item

### Style Procs

#### Style Proc Info
```csharp
public class StyleProcInfo
{
    public int ClassId { get; set; }    // Class restriction (0 = all)
    public int Chance { get; set; }     // Proc chance percentage
    public Spell Spell { get; set; }    // Spell to cast
}
```

#### Style Proc Execution
```csharp
if (style.Procs.Count > 0)
{
    // All procs roll
    foreach (StyleProcInfo procToExecute in procsToExecute)
    {
        if (Util.Chance(procToExecute.Chance))
        {
            effect = CreateMagicEffect(living, target, procToExecute.Spell.ID);
            styleEffects.Add(effect);
        }
    }
}
```

### Implementation Architecture

#### Base Proc Handler
```csharp
public abstract class BaseProcSpellHandler : SpellHandler
{
    protected Spell m_procSpell;
    protected SpellLine m_procSpellLine;
    
    protected abstract DOLEvent EventType { get; }
    protected abstract string SubSpellLineName { get; }
    
    public override void OnEffectStart(GameSpellEffect effect)
    {
        base.OnEffectStart(effect);
        GameEventMgr.AddHandler(effect.Owner, EventType, new DOLEventHandler(EventHandler));
    }
}
```

#### Offensive Proc Handler
```csharp
[SpellHandler(eSpellType.OffensiveProc)]
public class OffensiveProcSpellHandler : BaseProcSpellHandler
{
    protected override DOLEvent EventType => GameLivingEvent.AttackFinished;
    
    public void EventHandler(AttackData ad)
    {
        if (ad.AttackResult != eAttackResult.HitUnstyled && 
            ad.AttackResult != eAttackResult.HitStyle)
            return;
            
        int baseChance = Spell.Frequency / 100;
        if (baseChance < 1)
            baseChance = 1;
            
        if (Util.Chance(baseChance))
        {
            ISpellHandler handler = ScriptMgr.CreateSpellHandler(
                ad.Attacker, m_procSpell, m_procSpellLine);
                
            // Target based on proc spell
            switch (m_procSpell.Target)
            {
                case eSpellTarget.ENEMY:
                    handler.StartSpell(ad.Target);
                    break;
                default:
                    handler.StartSpell(ad.Attacker);
                    break;
            }
        }
    }
}
```

### Database Schema

#### Item Proc Fields
```sql
-- Weapon/Armor proc fields
ProcSpellID: Primary proc spell ID
ProcSpellID1: Secondary proc spell ID  
ProcChance: Override chance (0 = default 10%)

-- Poison fields
PoisonSpellID: Poison effect spell ID
PoisonCharges: Remaining poison applications
PoisonMaxCharges: Maximum poison charges

-- Common values
ProcChance: 5-25 typical range
ProcSpellID: References spell in Spell table
```

#### Spell Proc Fields
```sql
-- Proc spell configuration
Frequency: Proc chance as percentage * 100 (1000 = 10%)
Value: For some procs, the triggered spell ID
Target: Determines proc target (Enemy/Self/Group)
Duration: How long the proc buff lasts
```

### Proc Spell Modifications

#### Item Effects Line
When procs use Item_Effects spell line:
- Variance calculations modified
- Damage scaling adjusted
- Special item rules apply
- Level requirements enforced

#### Level Scaling
```csharp
protected virtual void StartWeaponMagicalEffect(DbInventoryItem weapon, AttackData ad, 
    SpellLine spellLine, int spellID, bool ignoreLevel)
{
    if (!ignoreLevel)
    {
        int requiredLevel = weapon.Template.LevelRequirement > 0 ? 
                           weapon.Template.LevelRequirement : 
                           Math.Min(50, weapon.Level);
                           
        if (requiredLevel > Level)
        {
            // "You are not powerful enough to use this item's spell!"
            return;
        }
    }
}
```

## System Interactions

### With Combat System
- Triggers after attack resolution
- Requires successful hit
- Independent of critical strikes
- Can trigger additional attacks

### With Effect System
- Offensive/Defensive procs managed as effects
- Standard duration and stacking rules
- Can be dispelled/purged
- Subject to immunity

### With Spell System
- Procs cast through spell handlers
- Subject to resist checks
- Can be interrupted (if casting time)
- Uses caster's spell power

### With Item System
- Level requirements enforced
- Quality/condition don't affect chance
- Procs tied to specific items
- Can have multiple proc items

## Visual Feedback

### Combat Messages
```csharp
// Proc trigger (based on spell messages)
"Your weapon glows with power!"
"{0}'s weapon glows with the power of the gods!"

// Level requirement failure
"You are not powerful enough to use this item's spell!"

// Poison immunity
"Your target is protected from poisons!"
```

### Spell Delve Information
```csharp
// Weapon procs shown as
"Magical Ability:"
"- Spell Name"
"Function: Spell description"
"Strikes with weapon"

// Armor procs shown as  
"Magical Ability:"
"- Spell Name"
"Function: Spell description"
"Struck by enemy"
```

## Balance Considerations

### Proc Rate Balance
- Speed normalization prevents fast weapon dominance
- Per-hit chance scaled by weapon speed
- Overall DPS contribution consistent
- Multiple procs don't share cooldowns

### Power Level Control
- Item level requirements
- Proc spell levels match items
- Variance uses Item_Effects rules
- No proc stacking from same spell

### Special Restrictions
- Poisons consume limited charges
- Reactive procs require being hit
- Some procs class-restricted
- Level requirements strictly enforced

## Test Scenarios

### Basic Proc Tests
1. **Weapon Proc**: Verify trigger on hit
2. **Speed Scaling**: Compare different weapon speeds
3. **Dual Proc**: Test both procs triggering
4. **Miss Result**: Confirm no proc on miss

### Reactive Proc Tests
1. **Armor Hit**: Verify location-based trigger
2. **Shield Block**: Test shield reactive on block
3. **Level Check**: Confirm level requirements
4. **Multiple Pieces**: Test multiple reactive items

### Edge Cases
1. **Dead Target**: Proc shouldn't trigger
2. **Immune Target**: Check immunity respect
3. **Out of Range**: Verify range checks
4. **Poison Charges**: Test depletion

## Performance Notes

### Optimization Strategies
- Proc spells cached on load
- Chance calculations minimized
- Event handlers efficient
- No recursive proc chains

### Known Limitations
- Maximum 2 procs per weapon
- Poisons separate from procs  
- No proc cooldowns
- Fixed speed normalization

## Change Log
- Initial documentation of proc mechanics
- Includes weapon and reactive procs
- Documents offensive/defensive spell procs
- Covers poison and style proc systems 