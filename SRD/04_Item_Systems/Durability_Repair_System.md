# Item Durability & Repair System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Every time you use equipment in combat, it takes damage and becomes less effective. Items have both condition (current damage) and durability (how many times they can be repaired). When your gear gets damaged, you must visit a blacksmith or repair it yourself to restore its effectiveness. Items that reach 0 condition stop working entirely until repaired. Understanding durability helps you maintain your equipment and avoid being caught with broken gear in important situations.

The durability system tracks item wear and degradation through use. Items have both Condition (current state) and Durability (maximum possible repairs), requiring maintenance through NPC smiths or player repairs.

## Core Mechanics

### Durability Properties

**Game Rule Summary**: Items have two important numbers for damage tracking. Condition represents the current state of the item - new items start at 100% and go down to 0% as they're used. Durability represents how many times the item can be repaired - each repair uses some durability, and when it reaches zero, the item can never be repaired again and becomes permanently damaged.

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

**Game Rule Summary**: Your equipment takes damage every time you fight, with about a 5% chance per hit to lose condition. Weapons take damage when you attack, while armor takes damage when you get hit. The amount of damage depends on the level difference between you and your opponent - fighting higher level enemies damages your gear faster. Items below 91% condition take double damage, so it's important to repair them before they get too damaged.

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

**Game Rule Summary**: The game warns you when your equipment needs attention. At 90% condition, items "could be in better condition." At 80%, they're "in need of repairs." At 70%, they're "in dire need of repairs." When items reach 0% condition, they stop working completely until repaired, so don't ignore these warnings.

- **90%**: "could be in better condition"
- **80%**: "in need of repairs"
- **70%**: "in dire need of repairs"
- **0%**: Item becomes unusable

### Repair System

#### NPC Repair (Blacksmith)

**Game Rule Summary**: NPC blacksmiths can fully repair your items for a gold cost based on the item's value and how damaged it is. More expensive items cost more to repair, and more damaged items cost more. Each repair uses up some of the item's durability - when durability reaches zero, the blacksmith will warn you that they can't repair the item again. You can also use "repair all" to fix all your damaged equipment at once, but this adds a 20% tax to the total cost.

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

**Game Rule Summary**: Players with appropriate crafting skills can repair items themselves, but it's much less effective than NPC repair. You need the right secondary crafting skill for the item type, and your success chance depends on your skill level versus the item level. Player repairs only restore about 1% of the item's condition per attempt, so it takes many attempts to fully repair an item. However, player repair still uses durability, so it's mainly useful for emergency repairs.

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

**Game Rule Summary**: The "repair all" feature lets you fix all your damaged equipment with one command, but charges a 20% tax on top of the normal repair costs. This is convenient when you have multiple damaged items, but costs more than repairing each item individually. It's a trade-off between convenience and cost.

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

**Game Rule Summary**: Keep doors, walls, and siege equipment can be repaired by players using wooden boards. Higher level structures require more wood and better quality wood types. Your woodworking skill determines success chance and how much damage you can repair. This is crucial for maintaining defenses during sieges and keeping your realm's fortifications in good condition.

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

**Game Rule Summary**: Damaged items are less effective than undamaged ones. Both quality and condition multiply together to determine how well an item works. A 50% quality, 80% condition item only works at 40% effectiveness. This affects weapon damage, armor protection, and sometimes magical bonuses. Keeping your gear repaired is essential for peak performance.

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

**Game Rule Summary**: Some special items never lose durability when repaired, meaning they can be maintained indefinitely. Most normal items will eventually become unrepairable after many repair cycles. Stacked items must be unstacked before repair, and some item types like consumables and instruments cannot be repaired at all.

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