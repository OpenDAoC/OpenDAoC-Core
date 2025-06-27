# Property System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Complete
- **Verification**: Code-verified from PropertyCalculator.cs, GameLiving.cs, and calculator implementations
- **Implementation**: Complete

## Overview

**Game Rule Summary**: The property system calculates your final stats by combining your base abilities, equipment bonuses, magical buffs, and debuffs. It ensures everything stacks properly and respects caps, so the strongest buff of each type applies while weaker ones are ignored.

The property system manages all character attributes, bonuses, and modifiers in DAoC. It provides a unified interface for calculating final values considering base stats, item bonuses, buffs, debuffs, and caps.

## Core Architecture

### Property Calculator System
```csharp
public interface IPropertyCalculator
{
    int CalcValue(GameLiving living, eProperty property);
    int CalcValueBase(GameLiving living, eProperty property);
    int CalcValueFromBuffs(GameLiving living, eProperty property);
    int CalcValueFromItems(GameLiving living, eProperty property);
}
```

### Bonus Categories
The system uses distinct categories to manage stacking and caps:

```csharp
public enum eBuffBonusCategory
{
    BaseBuff,      // Base buffs (single stat, base AF)
    SpecBuff,      // Spec buffs (spec AF, acuity)
    Debuff,        // Standard debuffs (positive values)
    OtherBuff,     // Uncapped/special buffs
    SpecDebuff,    // Specialized debuffs
    AbilityBuff    // Ability-based buffs
}
```

### Property Access
```csharp
// Get final calculated value
int value = living.GetModified(eProperty.Strength);

// Get base value without buffs
int baseValue = living.GetModifiedBase(eProperty.Strength);

// Get buff contribution only
int buffValue = living.GetModifiedFromBuffs(eProperty.Strength);
```

## Buff Bonus Categories

### 1. Base Buffs (BaseBuffBonusCategory)
- **Usage**: Single stat buffs, base armor factor, base resists
- **Stacking**: Only highest value applies
- **Capping**: Combined with item bonuses for cap calculation
- **Examples**: Strength buff, base AF buff, body resist buff

### 2. Spec Buffs (SpecBuffBonusCategory)
- **Usage**: Spec-line armor factor, dual stat buffs, acuity
- **Stacking**: Only highest value applies
- **Capping**: Separate cap from base buffs
- **Examples**: Spec AF buff, Str/Con buff, Dex/Qui buff

### 3. Debuffs (DebuffCategory)
- **Usage**: All standard debuffs
- **Values**: Stored as positive (subtracted in calculation)
- **Stacking**: Only highest value applies
- **Effectiveness**: Varies by stat type

### 4. Other Bonuses (OtherBonus)
- **Usage**: Uncapped buffs, special modifiers
- **Stacking**: Generally additive
- **Capping**: Usually not capped
- **Examples**: Paladin AF chants, special abilities

### 5. Spec Debuffs (SpecDebuffCategory)
- **Usage**: Specialized debuffs with enhanced effectiveness
- **Stacking**: Only highest value applies
- **Examples**: Champion debuffs

### 6. Ability Bonuses (AbilityBonus)
- **Usage**: Realm abilities, master level abilities
- **Stacking**: Generally stack with spell buffs
- **Examples**: Augmented stats, Toughness

## Standard Property Calculators

### Stat Calculator
Handles primary stats (Str, Con, Dex, Qui, Int, Pie, Emp, Cha):

```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    int baseStat = living.GetBaseStat((eStat)property);
    int itemBonus = CalcValueFromItems(living, property);
    int buffBonus = CalcValueFromBuffs(living, property);
    int baseDebuff = Math.Abs(living.DebuffCategory[property]);
    int specDebuff = Math.Abs(living.SpecDebuffCategory[property]);
    int abilityBonus = living.AbilityBonus[property];
    
    // Apply debuff effectiveness
    ApplyDebuffs(ref baseDebuff, ref specDebuff, ref buffBonus, ref baseStat);
    
    int stat = baseStat + itemBonus + buffBonus + abilityBonus;
    stat *= living.BuffBonusMultCategory1.Get((int)property);
    
    return Math.Max(1, stat);
}
```

#### Debuff Effectiveness
- **vs Base/Item Stats**: 50% effectiveness
- **vs Buffs**: 100% effectiveness
- **Spec Debuffs vs Buffs**: 200% effectiveness

### Armor Factor Calculator
```
PlayerAF = SpecBuffs + ItemBonuses + OtherBonuses - Debuffs
NPCAF = BaseBuffs + SpecBuffs + OtherBonuses - Debuffs + AbilityBonuses
```

#### AF Caps
- **Base AF + Item AF**: Combined cap based on level
- **Spec AF**: Separate cap = Level * 1.5
- **NPC AF**: No caps apply

### Resistance Calculators
```
Resistance = ItemBonuses + BuffBonuses - Debuffs
```

#### Buff Contribution (NPCs)
- Calculated as: `(Buffs - Debuffs/2) * 0.01`
- Used for dynamic resistance adjustments

### Speed Calculators

#### Melee Speed
```
Speed% = 100 + BaseBuffs + SpecBuffs - Debuffs + OtherBonuses + ItemBonuses
```

#### Casting Speed
```
Speed% = Math.Min(50, ItemBonuses) + AbilityBonuses - Debuffs
```

### Critical Hit Calculators
```
CritChance = BaseBuffs + SpecBuffs - Debuffs + OtherBonuses + ItemBonuses
```

### Hit Points Calculator
```csharp
int hpBase = player.CalculateMaxHealth(level, constitution);
int buffBonus = player.BaseBuffBonusCategory[property];

// Percentage debuffs
if (buffBonus < 0)
    buffBonus = (int)((1 + buffBonus / -100.0) * hpBase) - hpBase;

int itemBonus = Math.Min(player.ItemBonus[property], cap);
double result = hpBase;
result *= 1 + multiplicativeAbilityBonus * 0.01;
result += itemBonus + buffBonus + flatAbilityBonus;
```

## Property Caps

### Item Bonus Caps by Level
```
Level 1-14: 0
Level 15-19: 5
Level 20-24: 10
Level 25-29: 15
Level 30-34: 20
Level 35-39: 25
Level 40-44: 30
Level 45+: 35
```

### Special Property Caps
- **Hit Points**: 6x base cap
- **Power**: 3x base cap  
- **Resists**: 0.74x base cap
- **AF**: Base AF + Item AF combined
- **Spec AF**: Level * 1.5 (separate)

### Hard Caps
- **Buff/Debuff Effectiveness**: 25%
- **Casting Speed**: 50% from items
- **Spell Level**: +10 from items
- **XP/RP/BP Bonus**: +10%

## Calculator Implementation Patterns

### Simple Additive Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    return living.BaseBuffBonusCategory[property]
         + living.SpecBuffBonusCategory[property]
         - living.DebuffCategory[property]
         + living.OtherBonus[property]
         + living.ItemBonus[property];
}
```

### Percentage Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    int percent = 100
        - living.BaseBuffBonusCategory[property]  // Buffs reduce
        + living.DebuffCategory[property]         // Debuffs increase
        - living.ItemBonus[property];
        
    return Math.Max(1, percent);
}
```

### Capped Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    int value = living.ItemBonus[property];
    value = Math.Min(value, GetCap(living.Level));
    value += living.BaseBuffBonusCategory[property];
    return value;
}
```

## Special Cases

### Acuity
- Affects mana stat for casters
- List casters: Adds to both primary stat and acuity buff category
- Hybrid casters: Adds only to primary stat

### Constitution Death Penalty
- Only affects players
- Subtracted after all other calculations
- Persists until removed by healer

### Necromancer Pets
- Use owner's item and ability bonuses
- Apply own buffs/debuffs
- Special handling in stat calculator

### Multi-Stat Buffs
- Str/Con, Dex/Qui buffs go to SpecBuff category
- Apply to both stats equally
- Stack separately from single stat buffs

## Testing Considerations

### Calculator Test Pattern
```csharp
[Test]
public void Calculator_ShouldApplyBonuses_WhenBuffed()
{
    // Arrange
    var calculator = new StatCalculator();
    living.BaseBuffBonusCategory[property] = 50;
    living.ItemBonus[property] = 25;
    living.DebuffCategory[property] = 10;
    
    // Act
    var result = calculator.CalcValue(living, property);
    
    // Assert
    result.Should().Be(expectedValue);
}
```

### Edge Cases
- Minimum value enforcement (usually 1)
- Debuff vs buff effectiveness
- Cap interactions
- Multiplicative bonuses

## Implementation Notes

### Property Registration
```csharp
[PropertyCalculator(eProperty.Strength)]
public class StrengthCalculator : StatCalculator { }

[PropertyCalculator(eProperty.Stat_First, eProperty.Stat_Last)]
public class StatCalculator : PropertyCalculator { }
```

### Calculator Discovery
- Uses reflection to find PropertyCalculator attributes
- Automatically registers on server startup
- One calculator can handle multiple properties

### Performance Optimization
- Calculators cached per property type
- Values recalculated on demand
- No persistent caching of results

## Change Log

### 2025-01-20
- Initial documentation created
- Compiled from multiple calculator implementations
- Added buff category details

## References
- PropertyCalculator.cs: Base calculator class
- StatCalculator.cs: Primary stat calculations
- GameLiving.cs: GetModified implementation
- Various calculator implementations in propertycalc/ 