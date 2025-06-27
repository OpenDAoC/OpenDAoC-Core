# Crafting System

## Document Status
- Status: Under Development
- Implementation: Partial

## Overview

**Game Rule Summary**: The crafting system lets you create weapons, armor, and other useful items from raw materials. There are primary crafts like weaponcrafting and armorcrafting that make finished items, and secondary crafts like metalworking and leatherworking that process raw materials. Your success chance depends on your skill level versus the recipe difficulty - green and blue recipes almost always succeed, yellow recipes have some failure chance, and red recipes are quite difficult. Higher skill also improves the quality and reduces the crafting time. You'll need access to appropriate tools like forges for metalworking, and capital cities provide bonuses to skill gain and crafting speed.

The crafting system allows players to create items using gathered materials. It includes primary crafts (weaponcrafting, armorcrafting, tailoring, fletching, siegecrafting), secondary crafts (material processing), and advanced crafts (alchemy, spellcrafting).

## Core Mechanics

### Crafting Skills

#### Primary Crafts
- **Weaponcrafting**: Melee weapons (requires forge)
- **Armorcrafting**: Metal armor (requires forge)
- **Tailoring**: Cloth/leather armor (forge for metal bars)
- **Fletching**: Bows and ammunition
- **Siegecrafting**: Siege equipment

#### Secondary Crafts (Support Skills)
- **Metalworking**: Metal materials (sub-skill cap applies)
- **Leatherworking**: Leather materials (sub-skill cap applies)
- **Clothworking**: Cloth materials (sub-skill cap applies)
- **Woodworking**: Wood materials (sub-skill cap applies)
- **Herbcraft**: Potions/tinctures (sub-skill cap applies)
- **Gemcutting**: Magical gems (sub-skill cap applies)

#### Advanced Crafts
- **Alchemy**: Tinctures, dyes, poisons
- **Spellcrafting**: Item enchantment

### Success Chance Calculation

#### Base Success Chance
```csharp
public virtual int CalculateChanceToMakeItem(GamePlayer player, int craftingLevel)
{
    int con = GetItemCon(player.GetCraftingSkillValue(m_eskill), craftingLevel);
    if (con < -3) con = -3;
    if (con > 3) con = 3;

    switch (con)
    {
        // Chance to MAKE (100 - chance to fail)
        case -3: return 100;     // Grey
        case -2: return 100;     // Green
        case -1: return 100;     // Blue
        case 0:  return 92;      // Yellow
        case 1:  return 84;      // Orange
        case 2:  return 68;      // Red
        case 3:  return 0;       // Purple
        default: return 0;
    }
}
```

### Skill Gain Chance

#### Gain Point Calculation
```csharp
public virtual int CalculateChanceToGainPoint(GamePlayer player, int recipeLevel)
{
    int con = GetItemCon(player.GetCraftingSkillValue(m_eskill), recipeLevel);
    if (con < -3) con = -3;
    if (con > 3) con = 3;
    
    int chance;
    switch (con)
    {
        case -3: return 0;       // Grey - no gain
        case -2: chance = 15;    // Green
            break;
        case -1: chance = 30;    // Blue
            break;
        case 0:  chance = 45;    // Yellow
            break;
        case 1:  chance = 55;    // Orange
            break;
        case 2:  chance = 65;    // Red
            break;
        case 3:  return 0;       // Purple - no gain
        default: return 0;
    }

    // Capital city bonus
    if (player.CurrentRegion.IsCapitalCity)
    {
        chance += player.CraftingSkillBonus;
        chance = Math.Clamp(chance, 0, 100);
    }

    return chance;
}
```

### Quality Determination

#### Quality Calculation
```csharp
private int GetQuality(GamePlayer player, int recipeLevel)
{
    // 2% chance masterpiece, minimum 96% quality
    
    // Legendary (1000+ skill)
    if (player.GetCraftingSkillValue(m_eskill) >= 1000)
    {
        if (Util.Chance(2))
            return 100;  // 2% masterpiece
        return 96 + Util.Random(3);  // 96-99%
    }

    int delta = GetItemCon(player.GetCraftingSkillValue(m_eskill), recipeLevel);
    
    // Grey items (very easy)
    if (delta < -2)
    {
        if (Util.Chance(2))
            return 100;  // 2% masterpiece
        return 96 + Util.Random(3);  // 96-99%
    }

    // Weighted random selection based on con
    // Higher skill relative to recipe = higher quality chance
    delta = delta * 100;
    int[] chancePart = new int[4];
    int sum = 0;
    
    for (int i = 0; i < 4; i++)
    {
        chancePart[i] = Math.Max((4 - i) * 100 - delta, 0);
        sum += chancePart[i];
    }

    // Random selection
    int rand = Util.Random(sum);
    for (int i = 3; i >= 0; i--)
    {
        if (rand < chancePart[i])
            return 96 + i;
        rand -= chancePart[i];
    }

    return 96;  // Minimum quality
}
```

### Crafting Time

#### Time Calculation
```csharp
public virtual int GetCraftingTime(GamePlayer player, Recipe recipe)
{
    // Base multiplier from recipe level
    double baseMultiplier = (recipe.Level / 100) + 1;
    if (baseMultiplier < 1) 
        baseMultiplier = 1;

    // Material count
    ushort materialsCount = 0;
    foreach (var ingredient in recipe.Ingredients)
    {
        var countMod = ingredient.Count;
        if (countMod > 100) 
            countMod /= 10;
        materialsCount += (ushort)countMod;
    }

    // Base time calculation
    int craftingTime = (int)(baseMultiplier * materialsCount / 4);

    // Apply crafting speed modifier
    craftingTime = (int)(craftingTime / player.CraftingSpeed);

    // Relic bonus (5% per relic)
    craftingTime = (int)(craftingTime * (1 - (.05 * RelicMgr.GetRelicCount(player.Realm))));

    // Keep bonuses
    if (Keeps.KeepBonusMgr.RealmHasBonus(eKeepBonusType.Craft_Timers_5, player.Realm))
        craftingTime = (int)(craftingTime / 1.05);
    else if (Keeps.KeepBonusMgr.RealmHasBonus(eKeepBonusType.Craft_Timers_3, player.Realm))
        craftingTime = (int)(craftingTime / 1.03);

    // Con modifier
    int con = GetItemCon(player.GetCraftingSkillValue(m_eskill), recipe.Level);
    double mod = 1.0;
    switch (con)
    {
        case -3: mod = 0.4; break;  // Grey
        case -2: mod = 0.6; break;  // Green
        case -1: mod = 0.8; break;  // Blue
        case 0:  mod = 1.0; break;  // Yellow+
    }

    craftingTime = (int)(craftingTime * mod);

    // Enforce limits
    if (craftingTime < 1)
        craftingTime = 1;
    else if (Properties.MAX_CRAFT_TIME > 0 && craftingTime > Properties.MAX_CRAFT_TIME)
        craftingTime = Properties.MAX_CRAFT_TIME;

    return craftingTime;
}
```

### Secondary Skill Requirements

#### Minimum Secondary Skill Levels
- **Weaponcrafting**: Recipe level - 60
- **Armorcrafting**: Recipe level - 60  
- **Tailoring (armor)**: Recipe level - 30
- **Other crafts**: No secondary requirement

### Repair System

#### Repair Success Chance
```csharp
protected static int CalculateSuccessChances(GamePlayer player, DbInventoryItem item)
{
    eCraftingSkill skill = CraftingMgr.GetSecondaryCraftingSkillToWorkOnItem(item);
    if (skill == eCraftingSkill.NoCrafting) 
        return 0;

    // 50% = 10% chance, 100% = 100% chance
    int chancePercent = (int)((90 / (CraftingMgr.GetItemCraftLevel(item) * 0.5)) 
                              * player.GetCraftingSkillValue(skill)) - 80;
    
    return Math.Clamp(chancePercent, 0, 100);
}
```

#### Repair Requirements
- 50% of item's craft level in appropriate secondary skill
- Repair time: Max(1, item.Level / 2) seconds

### Spellcrafting

#### Overcharge Success Rates
```csharp
protected int CalculateChanceToOverchargeItem(GamePlayer player, DbInventoryItem item, 
                                             int maxBonusLevel, int bonusLevel)
{
    // Cannot overcharge more than 5 points
    if(bonusLevel - maxBonusLevel > 5) 
        return 0;
    
    // Not overcharging
    if(bonusLevel - maxBonusLevel < 0) 
        return 100;

    // Base success from item quality
    int success = 34 + ItemQualOCModifiers[item.Quality - 94];
    
    // Reduce by overcharge amount
    success -= OCStartPercentages[bonusLevel-maxBonusLevel];
    
    // Add skill bonus (capped at 100)
    int skillbonus = Math.Min(100, player.GetCraftingSkillValue(eCraftingSkill.SpellCrafting) / 10);
    success += skillbonus;
    
    // Add fudge factor
    int fudgefactor = (int)(100.0 * ((skillbonus / 100.0 - 1.0) * 
                           (OCStartPercentages[bonusLevel-maxBonusLevel] / 200.0)));
    success += fudgefactor;
    
    return Math.Clamp(success, 0, 100);
}
```

#### Item Max Bonus Level Table
```
// Based on item level and imbue points used
// See itemMaxBonusLevel[itemLevel, imbuePtsUsed] array
```

## System Interactions

### Tool Requirements
- **Forge**: Weaponcrafting, Armorcrafting, Metalworking, some Tailoring
- **Lathe**: Fletching, Woodworking, Siegecrafting
- **Alchemy Table**: Alchemy, Herbcraft
- **No Tool**: Clothworking, Leatherworking, Gemcutting

### Capital City Bonuses
- Increased chance to gain skill points
- Reduced crafting time (via CraftingSpeed)

### Keep Bonuses
- **Craft_Timers_3**: 3% faster crafting
- **Craft_Timers_5**: 5% faster crafting

### Relic Bonuses
- 5% crafting time reduction per owned relic

## Implementation Notes

### Recipe System
- Recipes stored in database
- Ingredient requirements with quality minimums
- Product templates define output items

### Craft Distance
- Must be within `CRAFT_DISTANCE` of required tool
- Default: 256 units

### Interruption
- Movement interrupts crafting
- Combat interrupts crafting
- Must remain stationary

## Test Scenarios

### Success Rate Tests
- Test each con level (-3 to 3)
- Verify capital city bonuses
- Test legendary (1000+) skill rates

### Quality Tests
- Verify minimum 96% quality
- Test masterpiece rates (2%)
- Test quality distribution by con

### Time Tests
- Verify con-based time modifiers
- Test keep bonus applications
- Test relic bonus stacking

### Secondary Skill Tests
- Verify minimum requirements
- Test skill gain with/without secondary
- Test repair calculations

## Change Log
- Initial documentation created
- Added detailed formulas
- Documented quality system
- Added spellcrafting mechanics

## References
- GameServer/craft/AbstractCraftingSkill.cs
- GameServer/craft/SpellCrafting.cs
- GameServer/craft/Repair.cs
- Properties.MAX_CRAFT_TIME 