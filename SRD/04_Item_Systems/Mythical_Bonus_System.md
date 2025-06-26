# Mythical Bonus System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The Mythical Bonus system (sometimes called Mythirian bonuses) provides enhanced stat and resist cap increases beyond normal item bonuses. These special properties allow players to exceed standard stat and resist caps through specific item slots.

## Core Mechanics

### Mythical Bonus Types

#### Stat Cap Bonuses
```csharp
// Mythical stat cap properties
eProperty.MythicalStatCapBonus_First = 236,
eProperty.MythicalStrCapBonus = 236,
eProperty.MythicalDexCapBonus = 237,
eProperty.MythicalConCapBonus = 238,
eProperty.MythicalQuiCapBonus = 239,
eProperty.MythicalIntCapBonus = 240,
eProperty.MythicalPieCapBonus = 241,
eProperty.MythicalEmpCapBonus = 242,
eProperty.MythicalChaCapBonus = 243,
eProperty.MythicalAcuCapBonus = 244,
eProperty.MythicalStatCapBonus_Last = 244,
```

#### Resist Cap Bonuses
```csharp
// Properties (from market search)
eProperty.BodyResCapBonus    // Body resist cap
eProperty.ColdResCapBonus    // Cold resist cap  
eProperty.CrushResCapBonus   // Crush resist cap
eProperty.EnergyResCapBonus  // Energy resist cap
eProperty.HeatResCapBonus    // Heat resist cap
eProperty.MatterResCapBonus  // Matter resist cap
eProperty.SlashResCapBonus   // Slash resist cap
eProperty.SpiritResCapBonus  // Spirit resist cap
eProperty.ThrustResCapBonus  // Thrust resist cap
```

#### Utility Bonuses
```csharp
eProperty.MythicalSafeFall       // Safe fall ability
eProperty.MythicalDiscumbering   // Encumbrance reduction
eProperty.MythicalCoin          // Bonus money drops
```

### Cap Calculations

#### Mythical Stat Cap Increase
```csharp
public static int GetMythicalItemBonusCapIncrease(GameLiving living, eProperty property)
{
    if (living == null)
        return 0;

    int mythicalItemBonusCapIncreaseCap = GetMythicalItemBonusCapIncreaseCap(living);
    int mythicalItemBonusCapIncrease = living.ItemBonus[
        eProperty.MythicalStatCapBonus_First - eProperty.Stat_First + property];
    int itemBonusCapIncrease = GetItemBonusCapIncrease(living, property);

    // Include Acuity for mana stat classes
    if (living is GamePlayer player)
    {
        if (property == (eProperty) player.CharacterClass.ManaStat)
        {
            if (IsClassAffectedByAcuityAbility(player.CharacterClass))
                mythicalItemBonusCapIncrease += living.ItemBonus[eProperty.MythicalAcuCapBonus];
        }
    }

    // Combined cap of 52 for regular + mythical
    if (mythicalItemBonusCapIncrease + itemBonusCapIncrease > 52)
        mythicalItemBonusCapIncrease = 52 - itemBonusCapIncrease;

    return Math.Min(mythicalItemBonusCapIncrease, mythicalItemBonusCapIncreaseCap);
}
```

#### Cap Limits
```csharp
// Mythical cap increase limits
public static int GetMythicalItemBonusCapIncreaseCap(GameLiving living)
{
    return living == null ? 0 : 52;  // Hard cap of 52
}

// Regular item bonus cap
public static int GetItemBonusCapIncreaseCap(GameLiving living)
{
    return living == null ? 0 : living.Level / 2 + 1;
}
```

### Combined Cap Formula

#### Total Stat Cap
```csharp
// Final stat calculation with all caps
int itemBonus = living.ItemBonus[property];
int itemBonusCap = GetItemBonusCap(living);  // Level * 1.5
int itemBonusCapIncrease = GetItemBonusCapIncrease(living, property);
int mythicalItemBonusCapIncrease = GetMythicalItemBonusCapIncrease(living, property);

int finalCap = itemBonusCap + itemBonusCapIncrease + mythicalItemBonusCapIncrease;
int finalValue = Math.Min(itemBonus, finalCap);
```

#### Resist Cap Calculations
```csharp
public static int GetItemBonusCapIncrease(GameLiving living, eProperty property)
{
    if (living == null)
        return 0;

    // Resist caps are hardcapped at 5%
    return Math.Min(living.ItemBonus[
        eProperty.ResCapBonus_First - eProperty.Resist_First + property], 5);
}
```

## Implementation Details

### Item Bonus Stacking

#### Priority System
1. Base item bonuses applied first
2. Regular cap increases (ConCapBonus, etc.)
3. Mythical cap increases last
4. Combined regular + mythical capped at 52

#### Acuity Special Case
For caster classes using INT/PIE/EMP as mana stat:
- Acuity bonuses count toward mana stat
- MythicalAcuCapBonus counts toward mana stat cap
- Stacks with specific stat mythical bonuses

### Market Explorer Categories

#### Mythical Bonus IDs
```
62 - Mythical Block
63 - Mythical Coin
64 - Mythical Stat Cap
65 - Mythical Water Breathing
66 - Mythical Crowd Control
67 - Mythical Essence Resist
68 - Mythical Resist and Cap
69 - Mythical Siege Damage
70 - Mythical Run Speed
71 - Mythical DPS
72 - Mythical Realm Points
73 - Mythical Spell Increase
74 - Mythical Resurrection
75 - Mythical Stat and Cap
76 - Mythical Health Regeneration
77 - Mythical Power Regen
78 - Mythical Endurance Regen
79 - Mythical Safe Fall
80 - Mythical Physical Defense
```

## System Interactions

### Stat System
- Increases effective stat caps
- Allows stats beyond normal limits
- Stacks with regular cap bonuses

### Resist System
- Increases resist hard caps
- Limited to 5% increase per resist
- Affects both damage types and magic

### Property Calculator
- Integrated into stat calculations
- Applied after base bonuses
- Respects combined cap limits

### Item Generation
- Can appear on high-level items
- More common on special/unique items
- Balanced by rarity

## Configuration

### Cap Values
```csharp
// Base caps (without mythical)
Stat Cap = Level * 1.5
Resist Cap = Level / 2 + 1

// With mythical
Max Stat Cap Increase = 52
Max Resist Cap Increase = 5%
Combined Regular + Mythical Cap = 52
```

### Item Requirements
- Typically level 45+ items
- Higher chance on artifacts
- Can appear on crafted items

## Edge Cases

### Stat Overflow
```csharp
// Prevent overflow when combining caps
if (mythicalItemBonusCapIncrease + itemBonusCapIncrease > 52)
    mythicalItemBonusCapIncrease = 52 - itemBonusCapIncrease;
```

### Multi-Stat Items
- All Stats mythical affects all 8 stats
- Uses ID 15 in market explorer
- Each stat calculated independently

### Class Restrictions
- Acuity affects INT/PIE/EMP users
- No effect on non-caster stats
- Calculated per character class

## Test Scenarios

1. **Basic Cap Test**
   - Level 50 character
   - 150 Constitution from items
   - 100 Mythical Con Cap
   - Verify 127 final (75 cap + 52 mythical)

2. **Combined Cap Test**
   - 5 Mythical cap bonus
   - 100 regular cap bonus
   - Verify proper stacking
   - Check 52 combined limit

3. **Acuity Test**
   - Animist with Acuity bonuses
   - Verify affects Intelligence
   - Check mythical Acu cap
   - Confirm proper conversion

4. **Resist Cap Test**
   - Mythical resist caps
   - Verify 5% maximum
   - Test all damage types
   - Check stacking limits

## Formulas Summary

### Stat Cap Calculation
```
Base Cap = Level * 1.5
Regular Cap Bonus = Min(ItemBonus, Level/2 + 1)
Mythical Cap Bonus = Min(ItemBonus, 52 - Regular)
Final Cap = Base + Regular + Mythical
```

### Resist Cap Calculation
```
Base Resist Cap = 70%
Item Resist Cap Bonus = Min(ItemBonus, 5%)
Final Resist Cap = Base + Bonus
```

### Combined Limits
```
Regular + Mythical Cap Bonus <= 52
Resist Cap Bonus <= 5%
```

## Property Names

### Display Names
```csharp
"Mythical Stat Cap (Strength)"
"Mythical Stat Cap (Dexterity)"
"Mythical Stat Cap (Constitution)"
"Mythical Stat Cap (Quickness)"
"Mythical Stat Cap (Intelligence)"
"Mythical Stat Cap (Piety)"
"Mythical Stat Cap (Charisma)"
"Mythical Stat Cap (Empathy)"
"Mythical Stat Cap (Acuity)"
"Body cap"
"Cold cap"
"Crush cap"
"Energy cap"
"Heat cap"
"Matter cap"
"Slash cap"
"Spirit cap"
"Thrust cap"
"Mythical Safe Fall"
"Mythical Discumbering"
"Mythical Coin"
```

## TODO
- Document specific item examples
- Add drop rate information
- Detail crafting integration
- Explain visual indicators
- Add PvP impact analysis 