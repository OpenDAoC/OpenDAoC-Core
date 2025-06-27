# Spell Effects System

## Document Status
- Status: Under Development
- Implementation: Complete

## Overview

**Game Rule Summary**: The spell effects system controls how magical enhancements, debuffs, and ongoing spells work on characters. When you cast a buff, debuff, or damage-over-time spell, this system determines whether it stacks with existing effects, how long it lasts, and when it gets replaced by better versions. Understanding effect stacking is crucial for optimizing your magical support and avoiding conflicts between different casters in groups.

The spell effects system manages the application, duration, stacking, and removal of all spell-based effects in the game. It handles buff/debuff stacking through a component-based system, manages concentration effects, and controls effect timers.

## Core Mechanics

### Effect Types (eEffect Enum)

**Game Rule Summary**: Effects are organized into categories based on what they do. Positive effects like buffs and healing help you, while negative effects like debuffs and damage-over-time hurt you. Special effects include crowd control that disables you and utility effects that change how other systems work. Each type has its own rules for stacking and interaction with other effects.

#### Positive Effects
```csharp
// Damage & Protection
Bladeturn               // Absorbs melee attacks
DamageAdd               // Adds damage to melee attacks
FocusShield             // Damage shield that returns damage
AblativeArmor           // Absorbs damage before armor
MeleeDamageBuff         // Increases melee damage %

// Speed & Movement
MeleeHasteBuff          // Increases melee attack speed
MovementSpeedBuff       // Increases movement speed

// Healing
HealOverTime            // Periodic healing
CombatHeal              // Instant heal effect marker

// Stats
StrengthBuff            // Increases Strength
DexterityBuff           // Increases Dexterity
ConstitutionBuff        // Increases Constitution
StrengthConBuff         // Increases Str + Con
DexQuickBuff            // Increases Dex + Qui
AcuityBuff              // Increases casting stat

// Armor
ArmorAbsorptionBuff     // Increases armor absorption %
BaseAFBuff              // Base armor factor buff
SpecAFBuff              // Spec armor factor buff
PaladinAf               // Paladin AF buff (uncapped)

// Resists
BodyResistBuff          // Body magic resist
SpiritResistBuff        // Spirit magic resist
EnergyResistBuff        // Energy magic resist
HeatResistBuff          // Heat magic resist
ColdResistBuff          // Cold magic resist
MatterResistBuff        // Matter magic resist
BodySpiritEnergyBuff    // BSE combo resist buff
HeatColdMatterBuff      // HCM combo resist buff
AllMagicResistsBuff     // All magic resists

// Regeneration
HealthRegenBuff         // Health regeneration
EnduranceRegenBuff      // Endurance regeneration
PowerRegenBuff          // Power regeneration

// Procs & Special
OffensiveProc           // Offensive proc chance
DefensiveProc           // Defensive proc chance
```

#### Negative Effects
```csharp
// Damage Over Time
Bleed                   // Style-based bleed
DamageOverTime          // Spell-based DoT
Disease                 // Disease effect

// Movement Debuffs
MovementSpeedDebuff     // Reduces movement speed
MeleeHasteDebuff        // Reduces melee speed

// Crowd Control
Stun                    // Complete disable
Mez                     // Mesmerize (breaks on damage)
Confusion               // Confusion effect
Nearsight               // Reduces cast range

// Stat Debuffs
StrengthDebuff          // Reduces Strength
DexterityDebuff         // Reduces Dexterity
ConstitutionDebuff      // Reduces Constitution
StrConDebuff            // Reduces Str + Con
DexQuiDebuff            // Reduces Dex + Qui
WsConDebuff             // Reduces WeaponSkill + Con

// Armor Debuffs
ArmorAbsorptionDebuff   // Reduces armor absorption
ArmorFactorDebuff       // Reduces armor factor

// Resist Debuffs
BodyResistDebuff        // Reduces body resist
SpiritResistDebuff      // Reduces spirit resist
EnergyResistDebuff      // Reduces energy resist
HeatResistDebuff        // Reduces heat resist
ColdResistDebuff        // Reduces cold resist
MatterResistDebuff      // Reduces matter resist
SlashResistDebuff       // Reduces slash resist

// Other Debuffs
MeleeDamageDebuff       // Reduces melee damage
FatigueConsumptionDebuff // Increases endurance use
```

#### Special Effects
```csharp
// Control Effects
Charm                   // Pet control
Pet                     // Pet summon marker

// Utility
DirectDamage            // Direct damage marker
FacilitatePainworking   // Painworking enhancement
PiercingMagic           // Resist pierce
ResurrectionIllness     // PvE death penalty
RvrResurrectionIllness  // RvR death penalty

// Pulse Effects
Pulse                   // Pulse effect container

// Immunities
StunImmunity            // Stun immunity
MezImmunity             // Mezz immunity
SnareImmunity           // Snare immunity
NearsightImmunity       // Nearsight immunity
NPCStunImmunity         // NPC stun resistance
NPCMezImmunity          // NPC mezz resistance
```

### Buff Component System

**Game Rule Summary**: The game organizes buffs and debuffs into different categories that have specific stacking rules. Base buffs like single stat bonuses can be overridden by better versions but don't stack with each other. Spec buffs work differently and can exist alongside base buffs. Debuffs reduce your effectiveness and only the worst one of each type affects you. Understanding these categories helps you know which spells will stack and which will conflict.

#### Bonus Categories
```csharp
public enum eBuffBonusCategory
{
    BaseBuff,      // Base buffs (stat buffs, base AF)
    SpecBuff,      // Spec buffs (spec AF, acuity)
    Debuff,        // Standard debuffs
    OtherBuff,     // Uncapped/special buffs
    SpecDebuff,    // Specialized debuffs
    AbilityBuff    // Ability-based buffs
}
```

#### Component Stacking Rules

**Game Rule Summary**: Different types of magical enhancements follow different stacking rules. Base buffs like single stat increases compete with each other - only the best one applies. Spec buffs stack with base buffs but not with other spec buffs. Debuffs always apply but only the worst one of each type affects you. Special buffs like Paladin chants bypass normal limits and stack with everything. This system prevents players from becoming overpowered by stacking unlimited buffs while still allowing meaningful combinations.

**1. Base Buffs (BaseBuff)**
- Single stat buffs (Str, Con, Dex, etc.)
- Base armor factor buffs
- Base resist buffs
- Capped together with item bonuses
- Only highest value applies

**2. Spec Buffs (SpecBuff)**
- Spec armor factor buffs
- Acuity buffs
- Dual stat buffs (Str/Con, Dex/Qui)
- Capped separately from base buffs
- Only highest value applies

**3. Debuffs (Debuff)**
- All standard debuffs
- Values are positive (subtracted from stats)
- Stack with buffs (reduce effectiveness)
- Only highest value applies

**4. Other Buffs (OtherBuff)**
- Uncapped buffs (Paladin AF chants)
- Special buffs that bypass normal limits
- Stack with all other categories

**5. Spec Debuffs (SpecDebuff)**
- Specialized debuffs with different stacking
- Secondary resist layer debuffs

**6. Ability Buffs (AbilityBuff)**
- Realm ability buffs
- Master level buffs
- Generally stack with spell buffs

### Effect Stacking Algorithm

**Game Rule Summary**: When multiple casters try to put the same type of effect on you, the game uses specific rules to decide which effect wins. Generally, stronger effects replace weaker ones, and effects from the same caster refresh their duration. If a weaker effect can't replace a stronger one, it becomes "disabled" and waits in the background to activate when the stronger effect ends. This prevents effect spam while ensuring you always have the best possible enhancement.

#### Stacking Decision Process
For detailed stacking logic, see: [`Effect_Stacking_Logic.md`](Effect_Stacking_Logic.md)

**Quick Reference**:
1. Dead owners cannot receive effects
2. Ability effects always stack without restriction
3. Same spell ID or effect group: renew or replace
4. Overwritable effects: use IsBetterThan comparison
5. Non-overwritable effects: fail or add as disabled

#### IsBetterThan Comparison
```csharp
public virtual bool IsBetterThan(ECSGameEffect effect)
{
    return SpellHandler.Spell.Value * Effectiveness > effect.SpellHandler.Spell.Value * effect.Effectiveness ||
           SpellHandler.Spell.Damage * Effectiveness > effect.SpellHandler.Spell.Damage * effect.Effectiveness;
}
```

#### Disabled Effects Management

**Game Rule Summary**: When a weaker effect can't replace a stronger one, it goes into a "disabled" state instead of being completely rejected. This means if someone casts a weaker buff on you while you have a stronger one, the weaker buff waits invisibly until the stronger one expires, then automatically activates. This prevents gaps in your magical protection when effects from different casters overlap.

- Worse effects from different casters become disabled
- Concentration effects disabled when out of range
- Best disabled effect automatically re-enabled when better effect expires
- Potion effects have special disabled state handling

#### Silent Renewal System
- Prevents OnStop/OnStart calls for same effect renewal
- Maintains effect state without triggering animations
- Critical for speed debuffs and concentration effects
- Uses pending effects queue for complex state management

### Effect Groups

**Game Rule Summary**: Effect groups allow different spells that do similar things to be treated as the same effect for stacking purposes. For example, different strength buffs from different spell lines all belong to the same effect group, so only the best one will apply even if they have different names. This prevents players from stacking multiple similar buffs from different sources.

Effect groups allow different spells to be treated as the same for stacking:

```csharp
// Common Effect Groups:
1     // Base AF buffs
2     // Spec AF buffs  
4     // Strength buffs
9     // Cold resist buffs
200   // Acuity buffs
201   // Constitution buffs
99999 // Non-stacking damage adds (RA-based)
```

### Concentration Effects

**Game Rule Summary**: Concentration effects are spells that the caster must actively maintain, using their limited concentration points. These effects automatically drop if the caster moves too far away from the target (usually about 5000 units). Casters have a limited number of concentration points, so they can only maintain a few concentration effects at once. This creates strategic choices about which effects to maintain and requires casters to stay relatively close to their allies.

#### Mechanics
- Limited by caster's concentration points
- Range check every 2.5 seconds (2500ms)
- Default range: 5000 units (BUFF_RANGE property)
- Endurance regen: 1500 units range
- Drop if caster moves out of range

#### Concentration Management
```csharp
// Maximum concentration effects: 20
// Concentration points vary by spell
// Cannot have multiple concentration effects on same target
```

### Pulse Effects

**Game Rule Summary**: Pulse effects are ongoing spells that repeatedly apply their effect over time, like damage-over-time spells or regeneration effects. Each pulse happens at regular intervals - most effects pulse based on their spell definition, but speed debuffs pulse very quickly (4 times per second) to provide smooth movement changes. Some pulse effects like area spells affect multiple targets and are managed by a parent-child system to optimize performance.

#### Pulse Timing
```csharp
// Standard Pulse Effects:
Frequency = Spell.Frequency  // From spell definition
NextTick = StartTick + Frequency

// Speed Debuffs (Special):
PulseFreq = 250             // 0.25 seconds
NextTick = 1 + Duration/2 + StartTick + PulseFreq

// Concentration Effects:
PulseFreq = 2500            // 2.5 seconds
NextTick = StartTick + PulseFreq
```

#### Pulse Effect Container
- Parent effect manages child effects
- Children linked to specific targets
- Parent cancellation cancels all children
- Prevents effect refresh spam

### Effect Duration

**Game Rule Summary**: Effect duration depends on the base spell duration, your spell duration bonuses, and the target's resistances. Debuffs and crowd control effects last shorter on targets with appropriate resistances. There are minimum and maximum duration limits to prevent effects from being too brief or lasting forever. In RvR, area effect spells have reduced duration on targets further from the center of the effect.

#### Duration Calculation
```csharp
BaseDuration = Spell.Duration
SpellDurationBonus = Caster.GetModified(eProperty.SpellDuration) * 0.01

// Standard Effects:
Duration = BaseDuration * (1 + SpellDurationBonus)

// Debuffs/CC on Players:
ResistReduction = Target.GetResist(DamageType) * 0.01
Duration = BaseDuration * (1 + SpellDurationBonus) * (1 - ResistReduction)

// Caps:
MinDuration = 1ms
MaxDuration = BaseDuration * 4

// AoE Duration Falloff (PvP only, no damage component):
DistanceFactor = 1 - (DistanceFromCenter / Radius * 0.5)
Duration *= DistanceFactor
```

#### Special Duration Modifiers
- CC duration reduction properties
- Resurrection illness: -50% spell effectiveness
- Critical debuffs: Random 10-100% bonus duration

### Immunity Effects

**Game Rule Summary**: After certain crowd control effects end, you gain temporary immunity to prevent being permanently disabled by repeated applications. Most immunities last 60 seconds (configurable by server), while style-based stuns give immunity for 5 times the stun duration. NPCs have diminishing returns instead of immunity - each successive crowd control effect lasts half as long as the previous one, eventually becoming very brief.

#### Standard Immunities
```csharp
// Duration based on triggering effect:
StunImmunity:     60 seconds (configurable)
MezImmunity:      60 seconds (configurable)  
SnareImmunity:    60 seconds (configurable)
NearsightImmunity: 60 seconds (configurable)

// Style stun immunity:
Duration = StunDuration * 5

// No immunity from:
- Pet stuns
- Unresistable stuns
```

#### NPC Diminishing Returns
```csharp
// Each application reduces duration:
1st CC: 100% duration
2nd CC: 50% duration  
3rd CC: 25% duration
4th CC: 12.5% duration
// Continues halving...
```

### Effectiveness System

#### Base Effectiveness Calculation
```csharp
// Damage Spells:
BaseEffectiveness = 1.0
+ SpellDamage% * 0.01

// Buffs (non-list casters):
SpecLevel = Caster.GetModifiedSpecLevel(SpellLine)
Effectiveness = 0.75 + (SpecLevel - 1) * 0.5 / SpellLevel
// Clamped 0.75 to 1.25

// Buffs (list casters):
Effectiveness = 1.0

// Debuffs:
BaseEffectiveness = 1.0
+ DebuffEffectiveness% * 0.01
* CriticalModifier  // 1.1 to 2.0 if crit
```

### Effect Icons & Display

#### Icon Priority
1. Spell.Icon if defined
2. Spell.ClientEffect as fallback
3. Effect type defaults
4. 0 = no icon

#### Update Triggers
```csharp
PlayerUpdate flags:
ICONS        = 1 << 7  // Effect bar update
STATUS       = 1 << 6  // HP/Mana/End update
STATS        = 1 << 5  // Stat window update
RESISTS      = 1 << 4  // Resist window update
WEAPON_ARMOR = 1 << 3  // Equipment stats update
ENCUMBERANCE = 1 << 2  // Weight update
CONCENTRATION = 1      // Conc bar update
```

## System Interactions

### Property Calculator Integration
- Effects modify properties through bonus categories
- Each property calculator sums appropriate categories
- Multiplicative modifiers applied after additives

### Combat System
- Damage adds apply in stacking order
- Damage shields trigger on incoming attacks
- Ablative armor absorbs before regular armor

### Crowd Control Interactions
- Break on damage rules
- Immunity generation
- CC duration properties

## Implementation Notes

### ECS Architecture
- Effects stored in EffectListComponent
- Effects grouped by eEffect type
- Ticking handled by EffectService
- One effect per type active at a time

### Performance Optimizations
- Effects indexed by type for fast lookup
- Concentration checks throttled to 2.5s
- Pulse effects use parent-child structure
- Disabled effects tracked separately

### Save/Restore System
- Only duration effects saved
- Concentration/pulse effects not saved
- Effect ID, remaining time, effectiveness stored
- Restored on player login

## Test Scenarios

### Buff Stacking Tests
1. Apply base stat buff, then stronger base buff
2. Apply spec buff with active base buff
3. Apply debuff with active buffs
4. Test effect group overwriting

### Concentration Tests
1. Cast concentration buff and move out of range
2. Test max concentration effects
3. Verify range check timing

### Duration Tests
1. Apply debuff to target with resists
2. Test duration caps (min/max)
3. Verify AoE falloff in RvR

### Immunity Tests
1. Stun player and verify immunity
2. Chain CC NPCs for diminishing returns
3. Test immunity from different sources

## Change Log
- Initial documentation
- Added complete effect type listings
- Documented buff component system
- Added stacking rules and algorithms 