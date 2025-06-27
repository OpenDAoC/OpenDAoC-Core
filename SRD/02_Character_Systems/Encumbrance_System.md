# Encumbrance System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Encumbrance limits how much you can carry before becoming slow and clumsy. Strong characters can carry more weight, and carrying too much will slow your movement or stop you entirely. Keep your load light for maximum mobility in combat and travel.

The encumbrance system limits how much weight a character can carry before suffering movement penalties. Carrying capacity is based on strength, with penalties that range from reduced movement speed to complete immobilization when severely overloaded.

## Core Mechanics

### Weight System

#### Item Weights
Item weight is stored in database as base units, divided by 10 for display:
```csharp
// Database stores weight * 10
// Display weight = stored_weight / 10
// 80 stored = 8.0 lbs displayed
```

#### Standard Item Weights (in display units)
| Item Type | Weight |
|-----------|--------|
| **Armor** |
| Cloth (Chest) | 2.0 lbs |
| Cloth (Other) | 0.8 lbs |
| Leather (Chest) | 4.0 lbs |
| Leather (Legs) | 2.8 lbs |
| Leather (Arms) | 2.4 lbs |
| Studded (Chest) | 6.0 lbs |
| Studded (Legs) | 4.2 lbs |
| Chain (Chest) | 8.0 lbs |
| Chain (Legs) | 5.6 lbs |
| Plate (Chest) | 9.0 lbs |
| Plate (Legs) | 6.3 lbs |
| **Weapons** |
| One-Handed | 2.5-4.0 lbs |
| Two-Handed | 5.0-6.0 lbs |
| Shield (Small) | 3.1 lbs |
| Shield (Medium) | 3.5 lbs |
| Shield (Large) | 3.8 lbs |
| Staff | 4.0 lbs |
| Instrument | 1.5 lbs |
| **Items** |
| Magical/Jewelry | 0.5 lbs |
| Stackable | Per unit |

### Carrying Capacity

#### Base Formula
```csharp
public virtual int MaxCarryingCapacity
{
    get
    {
        int result = (int)(MaxCarryingCapacityBase * 0.1);
        
        // Apply Lifter RA bonus
        var lifter = GetAbility<AtlasOF_LifterAbility>();
        if (lifter != null)
            result *= 1 + lifter.Amount * 0.01;
            
        return result;
    }
}
```

#### Capacity Calculation
```csharp
// Base capacity
MaxCarryingCapacityBase = Strength * 8

// Display capacity (in pounds)
MaxCarryingCapacity = MaxCarryingCapacityBase * 0.1
// Example: 100 STR = 80 lbs capacity
```

### Inventory Weight Calculation

#### Weight Tracking
```csharp
public override bool UpdateInventoryWeight()
{
    int newInventoryWeight = 0;
    
    foreach (var pair in m_items)
    {
        if (IsValidSlot(pair.Key))
            newInventoryWeight += pair.Value.Weight;
    }
    
    // Convert to display units
    newInventoryWeight /= 10;
    _inventoryWeight = newInventoryWeight;
}
```

#### Valid Slots for Weight
- Backpack slots (FirstBackpack to LastBackpack)
- Equipment slots (MinEquipable to MaxEquipable)
- Vault slots NOT counted
- Horse bags NOT counted

### Encumbrance States

#### Light Load (0-35% capacity)
- No movement penalty
- Full sprint capability
- No combat penalties

#### Medium Load (35-100% capacity)
```csharp
// Speed reduction formula
double maxCarryingCapacityRatio = maxCarryingCapacity * 0.35;
double speedModifier = 1 - inventoryWeight / maxCarryingCapacityRatio + maxCarryingCapacity / maxCarryingCapacityRatio;
```
- Progressive speed reduction
- Sprint still available
- No other penalties

#### Overloaded (>100% capacity)
```csharp
if (inventoryWeight > maxCarryingCapacity)
{
    IsEncumbered = true;
    // Movement severely restricted or prevented
}
```
- Severe speed penalty
- May be unable to move
- Warning messages displayed

### Speed Penalty Calculation

#### Formula
When over 35% capacity:
```csharp
SpeedModifier = 1 - (CurrentWeight / (MaxCapacity * 0.35)) + (MaxCapacity / (MaxCapacity * 0.35))
```

#### Example Calculations
```
Given: 100 STR (80 lbs capacity), carrying 40 lbs
Threshold: 80 * 0.35 = 28 lbs
Over threshold: 40 - 28 = 12 lbs
Speed modifier calculation applies
```

### Movement Speed Impact

#### Speed Reduction Application
```csharp
if (player.IsEncumbered && Properties.ENABLE_ENCUMBERANCE_SPEED_LOSS)
{
    speed *= player.MaxSpeedModifierFromEncumbrance;
    
    if (speed <= 0)
        speed = 0;  // Cannot move
}
```

#### Movement Messages
```csharp
if (movementComponent.MaxSpeedPercent <= 0)
    message = "GamePlayer.UpdateEncumbrance.EncumberedCannotMove";
else
    message = "GamePlayer.UpdateEncumbrance.EncumberedMoveSlowly";
```

## System Interactions

### Stat System
- Strength directly affects capacity
- STR buffs increase carrying capacity
- STR debuffs reduce capacity
- Capacity updates on stat changes

### Realm Abilities

#### Lifter RA
```csharp
public class AtlasOF_LifterAbility : RAPropertyEnhancer
{
    // 20% additional capacity per level
    public override int GetAmountForLevel(int level)
    {
        return level < 1 ? 0 : level * 20;
    }
}
```
- Level 1: +20% capacity
- Level 2: +40% capacity
- Level 3: +60% capacity
- Level 4: +80% capacity
- Level 5: +100% capacity

### Item System
- Adding items updates weight
- Removing items updates weight
- Stacking items combines weight
- Equipment counts toward total

### Buff System
- STR buffs increase capacity
- No direct weight reduction buffs
- Lifter RA stacks with STR

## Implementation Details

### Update Triggers
```csharp
public void UpdateEncumbrance(bool forced = false)
{
    // Skip if nothing changed
    if (!forced && _previousInventoryWeight == inventoryWeight && 
        _previousMaxCarryingCapacity == maxCarryingCapacity)
        return;
        
    // Recalculate and update
    // Send packets to client
    Out.SendEncumbrance();
    Out.SendUpdateMaxSpeed();
}
```

### Client Packets
```csharp
public virtual void SendEncumbrance()
{
    pak.WriteShort((ushort)m_gameClient.Player.MaxCarryingCapacity);
    pak.WriteShort((ushort)m_gameClient.Player.Inventory.InventoryWeight);
}
```

### Performance Optimization
- Weight cached until inventory changes
- Capacity cached until STR changes
- Updates batched when possible
- Minimal recalculation

## Edge Cases

### Zero Strength
- Minimum capacity enforced
- Cannot have negative capacity
- Base items still carryable

### Extreme Weights
- Single items can exceed capacity
- Cannot pick up if would overencumber
- Dropping items updates immediately

### Death and Resurrection
- Weight persists through death
- No automatic item dropping
- Must manage before moving

### Shapeshifting
- Capacity based on current form
- Weight carries over
- May become encumbered on shift

## Configuration

### Server Properties
```csharp
ENABLE_ENCUMBERANCE_SPEED_LOSS  // Enable speed penalties
// Default: true
```

### GM Override
- PrivLevel > 1 ignores encumbrance
- Full movement regardless of weight
- Testing and support purposes

## Test Scenarios

1. **Basic Weight**
   - Add items to inventory
   - Verify weight calculation
   - Check display values
   - Monitor thresholds

2. **Movement Penalties**
   - Load to 40% capacity
   - Verify speed reduction
   - Test sprint availability
   - Check combat speed

3. **Overload Testing**
   - Exceed 100% capacity
   - Verify movement stop
   - Test item dropping
   - Check recovery

4. **Buff Interactions**
   - Apply STR buffs
   - Verify capacity increase
   - Test Lifter RA
   - Check stacking

## Formulas Summary

### Carrying Capacity
```
Base Capacity = Strength * 8
Display Capacity = Base Capacity * 0.1
With Lifter = Display Capacity * (1 + LifterLevel * 0.2)
```

### Speed Penalty
```
If Weight > Capacity * 0.35:
    SpeedMod = 1 - Weight/(Capacity*0.35) + Capacity/(Capacity*0.35)
If Weight > Capacity:
    IsEncumbered = true
    Severe penalties or immobilization
```

### Weight Display
```
Item Weight Display = Database Weight / 10
Total Weight = Sum of all items in valid slots / 10
```

## TODO
- Document merchant weight restrictions
- Add vault weight management details
- Clarify pet/summon item carrying
- Detail weight-based quest requirements
- Add crafting material weight guidelines 