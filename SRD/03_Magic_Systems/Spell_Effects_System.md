# Spell Effects System

## Document Status
- Status: Under Development
- Implementation: Complete

## Overview
The spell effects system manages the application, duration, stacking, and removal of all spell-based effects in the game. It handles buff/debuff stacking through a component-based system, manages concentration effects, and controls effect timers.

## Core Mechanics

### Effect Types (eEffect Enum)

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

```csharp
// When adding a new effect:
1. Check if effect type already exists
2. If exists and same spell ID or effect group:
   a. Compare effectiveness (spell value * effectiveness)
   b. If new is better:
      - Disable/stop old effect
      - Add new effect
   c. If old is better:
      - Add new as disabled (if different caster)
      - Or reject (if same caster)
3. If different spell type but same effect group:
   - Follow overwrite rules
4. Special cases:
   - Concentration effects cannot stack
   - Pulse effects check active pulses
   - Ablative armor compares remaining value
```

### Effect Groups

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