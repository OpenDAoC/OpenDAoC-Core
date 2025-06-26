# Property Calculator System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from PropertyCalculator.cs, StatCalculator.cs, various calculators
- **Implementation Status**: ✅ Fully Implemented

## Overview
The Property Calculator System is the core engine for all statistical calculations in OpenDAoC. It provides a modular, extensible framework for computing character stats, combat values, resistances, and special properties with proper bonus stacking, capping, and interaction rules.

## Core Architecture

### Property Calculator Base
```csharp
[PropertyCalculator(eProperty.Stat_First, eProperty.Stat_Last)]
public abstract class PropertyCalculator
{
    // Core calculation method
    public abstract int CalcValue(GameLiving living, eProperty property);
    
    // Base value without temporary effects
    public virtual int CalcValueBase(GameLiving living, eProperty property)
    {
        return CalcValue(living, property);
    }
    
    // Value from buffs/debuffs only
    public virtual int CalcValueFromBuffs(GameLiving living, eProperty property)
    {
        return living.BaseBuffBonusCategory[property] +
               living.SpecBuffBonusCategory[property] -
               living.DebuffCategory[property];
    }
    
    // Value from items only
    public virtual int CalcValueFromItems(GameLiving living, eProperty property)
    {
        return Math.Min(living.ItemBonus[property], GetItemBonusCap(living));
    }
}
```

### Calculator Registration
```csharp
// Calculators are registered via attributes
[PropertyCalculator(eProperty.Strength)]
public class StrengthCalculator : PropertyCalculator { }

[PropertyCalculator(eProperty.Resist_First, eProperty.Resist_Last)]
public class ResistCalculator : PropertyCalculator { }
```

## Bonus Categories

### 1. Base Buff Bonuses (BaseBuffBonusCategory)
- **Usage**: Standard single-stat buffs
- **Stacking**: Only highest value applies
- **Capping**: Subject to buff caps
- **Examples**: Strength buff, Constitution buff

### 2. Specialization Buffs (SpecBuffBonusCategory)
- **Usage**: Spec-line armor factor, dual stat buffs
- **Stacking**: Only highest value applies
- **Capping**: Separate cap from base buffs
- **Examples**: Spec AF buff, Str/Con buff, Dex/Qui buff

### 3. Debuffs (DebuffCategory)
- **Values**: Stored as positive (subtracted in calculation)
- **Stacking**: Only highest value applies
- **Effectiveness**: Varies by stat type and target

### 4. Spec Debuffs (SpecDebuffCategory)
- **Usage**: Enhanced debuffs with higher effectiveness
- **Stacking**: Only highest value applies
- **Examples**: Champion debuffs

### 5. Other Bonuses (OtherBonus)
- **Usage**: Uncapped buffs, special modifiers
- **Stacking**: Generally additive
- **Examples**: Paladin AF chants, special abilities

### 6. Ability Bonuses (AbilityBonus)
- **Usage**: Realm abilities, master level abilities
- **Stacking**: Generally stack with spell buffs
- **Examples**: Augmented stats, Toughness

### 7. Item Bonuses (ItemBonus)
- **Usage**: Equipment stat bonuses
- **Capping**: Level-based caps
- **Stacking**: Fully additive from all equipment

## Primary Stat Calculators

### Stat Calculator (STR, CON, DEX, etc.)
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    // Get racial base + level progression
    int baseStat = living.GetBaseStat((eStat)property);
    
    // Add capped item bonuses
    int itemBonus = CalcValueFromItems(living, property);
    
    // Calculate buff contribution
    int buffBonus = CalcValueFromBuffs(living, property);
    
    // Apply debuff effectiveness
    int baseDebuff = Math.Abs(living.DebuffCategory[property]);
    int specDebuff = Math.Abs(living.SpecDebuffCategory[property]);
    int baseAndItemStat = baseStat + itemBonus;
    
    // Debuff effectiveness rules
    ApplyDebuffs(ref baseDebuff, ref specDebuff, ref buffBonus, ref baseAndItemStat);
    
    // Add ability bonuses (RAs, MLs)
    int abilityBonus = living.AbilityBonus[property];
    
    // Apply multiplicative bonuses
    int stat = baseAndItemStat + buffBonus + abilityBonus;
    stat = (int)(stat * living.BuffBonusMultCategory1.Get((int)property));
    
    // Apply constitution death penalty (players only)
    if (property == eProperty.Constitution && living is GamePlayer player)
        stat -= player.TotalConstitutionLostAtDeath;
    
    return Math.Max(1, stat);
}
```

#### Debuff Effectiveness Rules
```csharp
private static void ApplyDebuffs(ref int baseDebuff, ref int specDebuff, 
                                ref int buffBonus, ref int baseAndItemStat)
{
    // vs Buffs: 100% base debuff, 200% spec debuff
    buffBonus -= (int)(baseDebuff * BASE_DEBUFF_VS_BUFF_MODIFIER);
    buffBonus -= (int)(specDebuff * SPEC_DEBUFF_VS_BUFF_MODIFIER);
    
    // vs Base+Item: 50% effectiveness
    baseAndItemStat -= (int)((baseDebuff + specDebuff) / DEBUFF_VS_BASE_AND_ITEM_MODIFIER);
}
```

## Specialized Calculators

### Armor Factor Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    if (living is GamePlayer player)
    {
        // Player AF: SpecBuffs + Items + Other - Debuffs
        int specBuffs = player.SpecBuffBonusCategory[property];
        int itemBonus = Math.Min(player.ItemBonus[property], GetItemBonusCap(player));
        int otherBonus = player.OtherBonus[property];
        int debuffs = player.DebuffCategory[property];
        
        return Math.Max(0, specBuffs + itemBonus + otherBonus - debuffs);
    }
    else if (living is GameNPC npc)
    {
        // NPC AF: All categories apply
        return npc.BaseBuffBonusCategory[property] + 
               npc.SpecBuffBonusCategory[property] +
               npc.OtherBonus[property] + 
               npc.AbilityBonus[property] -
               npc.DebuffCategory[property];
    }
}
```

### Resistance Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    // Layer 1: Primary resists (items + buffs)
    int itemBonus = CalcValueFromItems(living, property);
    int buffBonus = CalcValueFromBuffs(living, property);
    int result = itemBonus + buffBonus;
    
    // Layer 2: Secondary resists (RAs, abilities)
    int abilityBonus = living.AbilityBonus[property];
    
    // Magic Absorption affects all magic resists
    if (IsMagicResist(property))
        abilityBonus += living.AbilityBonus[eProperty.MagicAbsorption];
    
    // Apply secondary as multiplicative layer
    result += (int)((1 - result * 0.01) * abilityBonus);
    
    // Layer 3: NPC constitution bonus
    if (living is GameNPC)
    {
        double resistFromCon = StatCalculator.CalculateBuffContributionToAbsorbOrResist(
            living, eProperty.Constitution) / 8 * 100;
        result += (int)((1 - result * 0.01) * resistFromCon);
    }
    
    // Add racial bonus after multiplicative calculation
    result += SkillBase.GetRaceResist(living.Race, (eResist)property);
    
    return Math.Min(result, HardCap);
}
```

### Hit Points Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    GamePlayer player = living as GamePlayer;
    
    // Base HP from level and constitution
    int hpBase = player.CalculateMaxHealth(player.Level, player.GetModified(eProperty.Constitution));
    
    // Percentage buff bonuses
    int buffBonus = player.BaseBuffBonusCategory[property];
    if (buffBonus < 0)  // Percentage debuffs
        buffBonus = (int)((1 + buffBonus / -100.0) * hpBase) - hpBase;
    
    // Capped item bonuses
    int itemBonus = Math.Min(player.ItemBonus[property], GetItemBonusCap(player));
    
    // Ability bonuses
    int flatAbilityBonus = living.AbilityBonus[property];           // New Toughness
    int multiplicativeAbilityBonus = living.AbilityBonus[eProperty.Of_Toughness]; // Old Toughness
    
    // Special: Scars of Battle (level 40+)
    if (player.HasAbility(Abilities.ScarsOfBattle) && player.Level >= 40)
    {
        int levelBonus = Math.Min(player.Level - 40, 10);
        hpBase = (int)(hpBase * (100 + levelBonus) * 0.01);
    }
    
    // Final calculation
    double result = hpBase;
    result *= 1 + multiplicativeAbilityBonus * 0.01;  // Multiplicative first
    result += itemBonus + buffBonus + flatAbilityBonus;  // Then additive
    
    return (int)result;
}
```

### Speed Calculators

#### Melee Speed Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    // Speed% = 100 + BaseBuffs + SpecBuffs - Debuffs + OtherBonuses + ItemBonuses
    return 100 + 
           living.BaseBuffBonusCategory[property] +
           living.SpecBuffBonusCategory[property] -
           living.DebuffCategory[property] +
           living.OtherBonus[property] +
           living.ItemBonus[property];
}
```

#### Casting Speed Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    // Casting speed has special item cap of 50%
    int itemBonus = Math.Min(50, living.ItemBonus[property]);
    
    return itemBonus + 
           living.AbilityBonus[property] -
           living.DebuffCategory[property];
}
```

### Defense Chance Calculators

#### Block Chance Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    if (living is GamePlayer player)
    {
        if (!player.HasSpecialization(Specs.Shields))
            return 0;
            
        // Block chance = (DEX*2 - 100)/4 + (ShieldSpec-1)*5 + 50
        int chance = (player.Dexterity * 2 - 100) / 4 + 
                    (player.GetModifiedSpecLevel(Specs.Shields) - 1) * 5 + 
                    50;
        
        // Add ability bonuses (in 0.1% units)
        chance += player.AbilityBonus[property] * 10;
        
        return chance;
    }
    else if (living is GameNPC npc)
    {
        return npc.BlockChance * 10;  // Convert to 0.1% units
    }
}
```

#### Parry Chance Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    if (living is GamePlayer player && player.HasSpecialization(Specs.Parry))
    {
        // Parry chance = (DEX*2 - 100)/4 + (ParrySpec-1)*5 + 50
        int chance = (player.Dexterity * 2 - 100) / 4 + 
                    (player.GetModifiedSpecLevel(Specs.Parry) - 1) * 5 + 
                    50;
                    
        chance += player.AbilityBonus[property] * 10;
        return chance;
    }
    
    return 0;
}
```

#### Evade Chance Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    if (living is GamePlayer player && player.HasAbility(Abilities.Evade))
    {
        // Evade chance = ((DEX+QUI)/2 - 50)*0.05 + EvadeLevel*5
        int avgStat = (player.Dexterity + player.Quickness) / 2;
        double chance = (avgStat - 50) * 0.05 + player.GetAbilityLevel(Abilities.Evade) * 5;
        
        // Add buff bonuses (in 0.1% units)
        chance += (player.BaseBuffBonusCategory[property] +
                  player.SpecBuffBonusCategory[property] -
                  player.DebuffCategory[property] +
                  player.OtherBonus[property] +
                  player.AbilityBonus[property]) * 10;
        
        return (int)(chance * 10);
    }
    
    return 0;
}
```

### Concentration Calculator
```csharp
public override int CalcValue(GameLiving living, eProperty property)
{
    if (living is GamePlayer player)
    {
        if (player.CharacterClass.ManaStat == eStat.UNDEFINED)
            return 1000000;  // Non-casters have unlimited concentration
        
        // Base concentration = Level * 4 * 2.2
        int concBase = (int)(player.Level * 4 * 2.2);
        
        // Stat contribution = (ManaStat - 50) * 2.8
        int stat = player.GetModified((eProperty)player.CharacterClass.ManaStat);
        double statConc = (stat - 50) * 2.8;
        
        // Total = (Base + Stat) / 2
        int conc = (concBase + (int)statConc) / 2;
        
        // Apply effectiveness modifier
        conc = (int)(player.Effectiveness * conc);
        
        // Master Level bonus (Perfecter ML4+)
        if (player.GetSpellLine("Perfecter") != null && player.MLLevel >= 4)
            conc += 20 * conc / 100;
        
        return Math.Max(0, conc);
    }
    
    return 1000000;  // NPCs have unlimited concentration
}
```

## Item Bonus Caps

### Level-Based Item Caps
```csharp
protected virtual int GetItemBonusCap(GameLiving living)
{
    if (living is GamePlayer player)
    {
        return player.Level switch
        {
            < 15 => 0,
            < 20 => 5,
            < 25 => 10,
            < 30 => 15,
            < 35 => 20,
            < 40 => 25,
            < 45 => 30,
            _ => 35
        };
    }
    
    return int.MaxValue;  // NPCs have no item caps
}
```

### Special Property Caps
```csharp
// Hit Points: 6x base cap
int hpCap = GetItemBonusCap(player) * 6;

// Power: 3x base cap  
int powerCap = GetItemBonusCap(player) * 3;

// Resists: 0.74x base cap
int resistCap = (int)(GetItemBonusCap(player) * 0.74);

// Spell Level: Hard cap of +10
int spellLevelBonus = Math.Min(10, living.ItemBonus[eProperty.SpellLevel]);
```

## Buff Effectiveness Rules

### Buff vs Debuff Interactions
```csharp
// Standard debuff effectiveness
public const double BASE_DEBUFF_VS_BUFF_MODIFIER = 1.0;      // 100% vs buffs
public const double SPEC_DEBUFF_VS_BUFF_MODIFIER = 0.5;     // 200% vs buffs
public const double DEBUFF_VS_BASE_AND_ITEM_MODIFIER = 2.0; // 50% vs base/items
```

### Debuff Effectiveness on Players
```csharp
// Debuffs have reduced effectiveness when result goes negative
if (buff < 0 && living is GamePlayer)
    buff /= 2;  // 50% effectiveness on players
```

## Cross-System Integration

### Stat Dependencies
```csharp
// Mana stat gets acuity bonus for casters
if (property == (eProperty)player.CharacterClass.ManaStat)
{
    if (IsClassAffectedByAcuityAbility(player.CharacterClass))
        abilityBonus += player.AbilityBonus[eProperty.Acuity];
}

// Magic resists get magic absorption bonus
if (IsMagicResist(property))
    abilityBonus += living.AbilityBonus[eProperty.MagicAbsorption];
```

### Pet Inheritance
```csharp
// Necromancer pets inherit owner's resistances and item bonuses
if (living is NecromancerPet necroPet && necroPet.Owner is GamePlayer playerOwner)
    livingToCheck = playerOwner;
else
    livingToCheck = living;
```

## Performance Optimizations

### Calculator Caching
- **Static Registration**: Calculators registered once at startup
- **Property Lookup**: O(1) property to calculator mapping
- **Minimal Allocations**: Reuse calculation objects

### Calculation Batching
```csharp
// Bulk property updates
public void UpdateAllStats(GameLiving living)
{
    for (eProperty prop = eProperty.Stat_First; prop <= eProperty.Stat_Last; prop++)
    {
        living.Properties.Set(prop, CalcValue(living, prop));
    }
}
```

## Test Scenarios

### Basic Stat Calculation
```csharp
// Given: Level 50 player, 15 STR racial base, +35 items, +75 buffs
// When: Calculate strength
// Then: Result = 15 + 50*1 + 35 + 75 = 175

// Given: Same player with -50 STR debuff
// When: Calculate strength  
// Then: Debuff affects buffs 100%, base+items 50%
//       = 15 + 50 + 35 + 75 - 50 - 25 = 100
```

### Resistance Stacking
```csharp
// Given: 26% item resist, 30% buff resist, 15% RA resist
// When: Calculate total resist
// Then: Primary = 26 + 30 = 56%
//       Secondary = (1 - 0.56) * 15 = 6.6%
//       Total = 56 + 6.6 = 62.6%
```

### Item Cap Testing
```csharp
// Given: Level 35 player with +50 stat items
// When: Calculate item contribution
// Then: Capped at 25 (level 35 cap)

// Given: Level 50 player with same items
// When: Calculate item contribution  
// Then: Capped at 35 (level 50 cap)
```

## Change Log
- 2024-01-20: Initial comprehensive documentation
- TODO: Add calculator performance benchmarks
- TODO: Document custom calculator creation

## References
- `GameServer/propertycalc/PropertyCalculator.cs`
- `GameServer/propertycalc/StatCalculator.cs`
- `GameServer/propertycalc/ResistCalculator.cs`
- `GameServer/propertycalc/MaxHealthCalculator.cs`
- `Tests/UnitTests/PropertyCalculators/PropertyCalculatorTests.cs` 