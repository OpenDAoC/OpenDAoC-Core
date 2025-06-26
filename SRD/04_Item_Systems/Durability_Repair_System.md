# Item Durability & Repair System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The durability system tracks item wear and degradation through use. Items have both Condition (current state) and Durability (maximum possible repairs), requiring maintenance through NPC smiths or player repairs.

## Core Mechanics

### Durability Properties

#### Item Condition
```csharp
// Current state of the item
public virtual int Condition { get; set; }       // Current condition
public virtual int MaxCondition { get; set; }    // Maximum condition (typically 50000)

// Condition as percentage
public int ConditionPercent => (Condition * 100) / Math.Max(1, MaxCondition);
```

#### Item Durability
```csharp
public virtual int Durability { get; set; }      // Repair capacity remaining
public virtual int MaxDurability { get; set; }   // Maximum durability

// Special flag for items that don't lose durability
public bool IsNotLosingDur { get; set; }
```

### Condition Loss

#### Combat Damage (Weapons)
```csharp
public virtual void OnStrikeTarget(GameLiving owner, GameLiving target)
{
    if (ConditionPercent > 70 && Util.Chance(ServerProperties.Properties.ITEM_CONDITION_LOSS_CHANCE))
    {
        int con = GameObject.GetConLevel(target.Level, Level);
        int sub = con + 4;
        
        if (ConditionPercent < 91)
            sub *= 2;  // Double loss when below 91%
            
        Condition -= sub;
        if (Condition < 0)
            Condition = 0;
    }
}
```

#### Combat Damage (Armor)
```csharp
public virtual void OnStruckByEnemy(GameLiving owner, GameLiving enemy)
{
    // Same formula as weapons
    // Applied to armor pieces when hit
}
```

#### Condition Thresholds
- **90%**: "could be in better condition"
- **80%**: "in need of repairs"
- **70%**: "in dire need of repairs"
- **0%**: Item becomes unusable

### Repair System

#### NPC Repair (Blacksmith)

**Repair Cost Formula**:
```csharp
public virtual long RepairCost
{
    get
    {
        return ((Template.MaxCondition - Condition) * Template.Price) / Template.MaxCondition;
    }
}
```

**Repair Process**:
```csharp
// Calculate condition to recover
var ToRecoverCond = item.MaxCondition - item.Condition;

// Check if repair will exhaust durability
if (ToRecoverCond + 1 >= item.Durability)
{
    // Partial repair, item can't be fully repaired again
    item.Condition = item.Condition + item.Durability;
    item.Durability = 0;
    // Warning: "Item is rather old. I won't be able to repair it again!"
}
else
{
    // Full repair
    item.Condition = item.MaxCondition;
    if (!item.IsNotLosingDur)
        item.Durability -= ToRecoverCond + 1;
}
```

#### Player Repair (Crafting)

**Success Chance**:
```csharp
protected static int CalculateSuccessChances(GamePlayer player, DbInventoryItem item)
{
    eCraftingSkill skill = CraftingMgr.GetSecondaryCraftingSkillToWorkOnItem(item);
    
    // 50% skill = 10% chance, 100% skill = 100% chance
    int chancePercent = (int)((90 / (CraftingMgr.GetItemCraftLevel(item) * 0.5)) 
                              * player.GetCraftingSkillValue(skill)) - 80;
    
    return Math.Clamp(chancePercent, 0, 100);
}
```

**Repair Amount**:
```csharp
// Player repairs restore 1% of max condition
int toRecoverCond = (int)((item.MaxCondition - item.Condition) * 0.01 / item.MaxCondition) + 1;
```

### Repair All Feature

#### Cost Calculation
```csharp
private long CalculateCost(DbInventoryItem item)
{
    // Base repair cost
    long NeededMoney = ((item.Template.MaxCondition - item.Condition) * item.Template.Price) / 
                      item.Template.MaxCondition;
    
    // 20% tax for repair all
    var tax = NeededMoney * REPAIR_ALL_TAX;
    
    return NeededMoney + (long)tax;
}
```

### Keep/Siege Repair

#### Wood Requirements
| Object Level | Wood Units Needed |
|--------------|-------------------|
| 1-3 | 1 unit |
| 4 | 2 units |
| 5 | 3 units |
| 6 | 4 units |
| 7 | 5 units |
| 8 | 10 units |
| 9 | 15 units |
| 10 | 20 units |

#### Wood Values
```csharp
public static int GetWoodValue(string name)
{
    switch (name.Replace(" wooden boards", ""))
    {
        case "rowan": return 1;
        case "elm": return 4;
        case "oak": return 8;
        case "ironwood": return 16;
        case "heartwood": return 32;
        case "runewood": return 48;
        case "stonewood": return 60;
        case "ebonwood": return 80;
        case "dyrwood": return 104;
        case "duskwood": return 136;
    }
}
```

#### Repair Success
```csharp
// Success chance based on woodworking skill vs object level
double successChance = CalculateRepairChance(player, obj);

// Repair amounts
GameKeepDoor: 5% of max health
GameKeepComponent: 5% of max health
GameSiegeWeapon: 15% of max health (max 3 repairs)
```

## Implementation Details

### Item Effectiveness

#### Quality and Condition Impact
```csharp
// Item effectiveness calculation
Effectiveness = (Quality / 100.0) * (Condition / MaxCondition)

// Applied to:
// - Weapon damage
// - Armor factor
// - Magical bonuses (if configured)
```

### Condition Messages

#### Weapon Messages
```csharp
if (ConditionPercent == 90)
    "Your {0} could be in better condition."
else if (ConditionPercent == 80)
    "Your {0} is in need of repairs."
else if (ConditionPercent == 70)
    "Your {0} is in dire need of repairs!"
```

### Special Cases

#### Non-Decaying Items
```csharp
if (item.IsNotLosingDur)
{
    // Item doesn't lose durability on repair
    // Condition can be restored indefinitely
}
```

#### Stacked Items
- Blacksmiths reject stacked items
- Must unstack before repair
- Each item repaired individually

#### Unrepairable Items
- Generic items
- Magical consumables
- Instruments
- Poisons
- Items with 0 durability

## System Interactions

### Combat System
- Condition loss on hit/being hit
- Effectiveness affects damage/defense
- 0 condition prevents item use

### Crafting System
- Secondary skills used for repair
- Skill level affects success chance
- Crafting tools not required for repair

### Economy System
- Repair costs scale with item value
- Gold sink mechanism
- Repair all has 20% tax

### Property System
- Item effectiveness may affect bonuses
- Condition updates trigger stat recalculation
- Visual updates sent to client

## Configuration

### Server Properties
```csharp
ITEM_CONDITION_LOSS_CHANCE  // Chance to lose condition per hit
// Default: 5 (5% chance)
```

### Item Templates
- MaxCondition: Usually 50000
- MaxDurability: Varies by item
- IsNotLosingDur: Special items flag

## Edge Cases

### Zero Durability
- Item cannot be repaired
- Becomes permanently damaged
- Must be replaced

### Partial Repairs
- When durability < needed repair
- Item partially restored
- Warning message displayed

### Price Calculation
- Items with no template price
- Prevents division by zero
- Uses fallback values

## Test Scenarios

1. **Basic Repair**
   - Damage item in combat
   - Visit blacksmith
   - Verify cost calculation
   - Complete repair

2. **Durability Exhaustion**
   - Repair item multiple times
   - Reach 0 durability
   - Verify unrepairable state
   - Check warnings

3. **Player Repair**
   - Test skill requirements
   - Verify success rates
   - Check repair amounts
   - Monitor durability loss

4. **Repair All**
   - Multiple damaged items
   - Calculate total cost
   - Verify 20% tax
   - Check all items repaired

## Formulas Summary

### Repair Cost
```
Cost = (MaxCondition - CurrentCondition) * ItemPrice / MaxCondition
```

### Durability Loss
```
DurabilityLoss = ConditionRecovered + 1
```

### Condition Loss (Combat)
```
Base = ConLevel + 4
If Condition < 91%: Base *= 2
ConditionLoss = Base
```

### Player Repair Success
```
Chance = ((90 / (ItemLevel * 0.5)) * SkillLevel) - 80
Clamped to 0-100%
```

## TODO
- Document magical item repair restrictions
- Add artifact repair special cases
- Detail condition loss for different damage types
- Clarify repair skill requirements by item type
- Add repair deed system details 