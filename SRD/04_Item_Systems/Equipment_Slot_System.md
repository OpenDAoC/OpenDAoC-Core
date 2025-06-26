# Equipment Slot System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The equipment slot system manages the placement and restrictions of items on a character. Each slot has specific requirements and limitations, with some slots affecting others (e.g., two-handed weapons preventing shield use).

## Core Mechanics

### Equipment Slot Definitions

#### Available Equipment Slots
```csharp
public static readonly eInventorySlot[] EQUIP_SLOTS =
{
    eInventorySlot.Horse,           // 9  - Mount
    eInventorySlot.HorseArmor,      // 7  - Horse armor
    eInventorySlot.HorseBarding,    // 8  - Horse decoration
    eInventorySlot.RightHandWeapon, // 10 - Primary weapon
    eInventorySlot.LeftHandWeapon,  // 11 - Shield/off-hand
    eInventorySlot.TwoHandWeapon,   // 12 - Two-handed weapon
    eInventorySlot.DistanceWeapon,  // 13 - Ranged weapon
    eInventorySlot.FirstQuiver,     // 14 - Arrow storage
    eInventorySlot.SecondQuiver,    // 15 - Arrow storage
    eInventorySlot.ThirdQuiver,     // 16 - Arrow storage
    eInventorySlot.FourthQuiver,    // 17 - Arrow storage
    eInventorySlot.HeadArmor,       // 21 - Helm
    eInventorySlot.HandsArmor,      // 22 - Gloves
    eInventorySlot.FeetArmor,       // 23 - Boots
    eInventorySlot.Jewelry,         // 24 - Jewel
    eInventorySlot.TorsoArmor,      // 25 - Chest
    eInventorySlot.Cloak,           // 26 - Cloak
    eInventorySlot.LegsArmor,       // 27 - Legs
    eInventorySlot.ArmsArmor,       // 28 - Arms
    eInventorySlot.Neck,            // 29 - Necklace
    eInventorySlot.Waist,           // 32 - Belt
    eInventorySlot.LeftBracer,      // 33 - Left wrist
    eInventorySlot.RightBracer,     // 34 - Right wrist
    eInventorySlot.LeftRing,        // 35 - Left ring
    eInventorySlot.RightRing,       // 36 - Right ring
    eInventorySlot.Mythical,        // 37 - Special slot
};
```

### Slot Restrictions

#### Item Type to Slot Mapping
| Item Type | Valid Slots |
|-----------|-------------|
| Sword/Axe/Hammer | RightHand, LeftHand (if small), TwoHand |
| Shield | LeftHand only |
| Staff/Polearm | TwoHand only |
| Bow/Crossbow | Distance only |
| Armor pieces | Specific armor slot only |
| Jewelry | Specific jewelry slot only |
| Rings | LeftRing or RightRing |
| Bracers | LeftBracer or RightBracer |

### Equip Validation

#### Basic Slot Validation
```csharp
private bool CheckItemSlotRestriction(DbInventoryItem item, eInventorySlot slot)
{
    switch (slot)
    {
        case eInventorySlot.RightHandWeapon:
            if ((eObjectType)item.Object_Type == eObjectType.Shield)
                return false; // Shields can't go in right hand
            if (!IsWeaponSlot(item))
                return false;
            break;
            
        case eInventorySlot.LeftHandWeapon:
            if (!IsShieldOrLeftWeapon(item))
                return false;
            break;
            
        case eInventorySlot.TwoHandWeapon:
            if (!IsTwoHandCapable(item))
                return false;
            break;
    }
}
```

#### Class Restrictions
```csharp
private bool CheckItemClassRestriction(DbInventoryItem item, eInventorySlot slot)
{
    if (string.IsNullOrEmpty(item.AllowedClasses))
        return true; // No restrictions
        
    foreach (string allowed in Util.SplitCSV(item.AllowedClasses, true))
    {
        if (m_player.CharacterClass.ID == int.Parse(allowed))
            return true;
    }
    
    return false; // Class not allowed
}
```

#### Skill Requirements
```csharp
// Weapon skills
if (!m_player.HasAbilityToUseItem(item.Template))
{
    Out.SendMessage("You have no skill in using this weapon type!", 
        eChatType.CT_System, eChatLoc.CL_SystemWindow);
    return false;
}

// Armor skills
if (!m_player.HasAbilityToUseItem(item.Template))
{
    Out.SendMessage("You have no skill in wearing this armor type!", 
        eChatType.CT_System, eChatLoc.CL_SystemWindow);
    return false;
}
```

### Active Weapon System

#### Weapon Slot Management
```csharp
// Active weapon slots encoded in single byte
// Low nibble: Right hand slot (0=standard, 2=twohanded, 3=distance, F=none)
// High nibble: Left hand slot (1=left, 2=twohanded, F=none)

public byte VisibleActiveWeaponSlots
{
    get 
    { 
        int rightHand = GetActiveRightHandSlot();
        int leftHand = GetActiveLeftHandSlot();
        return (byte)(((leftHand & 0x0F) << 4) | (rightHand & 0x0F));
    }
}
```

#### Weapon Switching
```csharp
public enum eActiveWeaponSlot : byte
{
    Standard = 0x00,   // Right/Left hand weapons
    TwoHanded = 0x01,  // Two-handed weapon
    Distance = 0x02    // Ranged weapon
}

// Switching updates active slots and equipment appearance
public void SwitchWeapon(eActiveWeaponSlot slot)
{
    // Update active weapon references
    // Send inventory updates
    // Update visual appearance
}
```

### Equipment Conflicts

#### Two-Handed Restrictions
```csharp
// Cannot equip shield with two-handed weapon
if (slot == eInventorySlot.LeftHand && 
    GetItem(eInventorySlot.TwoHandWeapon) != null)
{
    return false; // Blocked by two-handed weapon
}

// Cannot equip two-handed with shield equipped
if (slot == eInventorySlot.TwoHand && 
    GetItem(eInventorySlot.LeftHand) != null)
{
    return false; // Blocked by shield/left weapon
}
```

#### Dual Wield Requirements
- Requires dual wield specialization
- Left hand weapon must be smaller than right
- Certain classes only (Berserker, Shadowblade, etc.)

## Item Movement

### Equip Process
1. **Validation**: Check all restrictions
2. **Conflict Resolution**: Remove conflicting items
3. **Equip Item**: Place in slot
4. **Apply Bonuses**: Add item stats
5. **Update Appearance**: Visual update
6. **Send Updates**: Network packets

### Unequip Process
1. **Find Empty Slot**: Locate backpack space
2. **Remove Bonuses**: Remove item stats
3. **Move Item**: Transfer to backpack
4. **Update Appearance**: Visual update
5. **Send Updates**: Network packets

## Visual Equipment

### Visible Slots
```csharp
protected static readonly eInventorySlot[] VISIBLE_SLOTS =
{
    eInventorySlot.RightHandWeapon,
    eInventorySlot.LeftHandWeapon,
    eInventorySlot.TwoHandWeapon,
    eInventorySlot.DistanceWeapon,
    eInventorySlot.HeadArmor,
    eInventorySlot.HandsArmor,
    eInventorySlot.FeetArmor,
    eInventorySlot.TorsoArmor,
    eInventorySlot.Cloak,
    eInventorySlot.LegsArmor,
    eInventorySlot.ArmsArmor
};
```

### Equipment Appearance Updates
- Sent to all players in range
- Includes model/color information
- Updates on equip/unequip
- Weapon visibility based on active slot

## Special Equipment

### Mythical Items
- Single mythical slot (37)
- Special requirements
- Unique bonuses
- Level 45+ requirement

### Horse Equipment
- Horse slot for mount
- Horse armor for protection
- Horse barding for decoration
- All three work together

### Quivers
- Four quiver slots total
- Store arrows/bolts
- Auto-refill from quivers
- Used with ranged weapons

## Implementation Notes

### Slot Encoding
```csharp
// Database slot values
public class Slot
{
    public const int RIGHTHAND = 10;
    public const int LEFTHAND = 11;
    public const int TWOHAND = 12;
    public const int RANGED = 13;
    // ... etc
}
```

### Bonus Application
```csharp
// Items only provide bonuses when equipped
public virtual void RefreshItemBonuses()
{
    ItemBonus.Clear();
    
    foreach (DbInventoryItem item in Inventory.EquippedItems)
    {
        if (ShouldApplyBonus(item))
        {
            ApplyItemBonuses(item);
        }
    }
}
```

### Performance Considerations
- Equipment changes trigger full bonus recalculation
- Visual updates sent to visible players only
- Slot validation cached where possible
- Batch updates for multiple changes

## Edge Cases

### Starter Equipment
- Automatically equipped on creation
- Fills appropriate slots
- Sets initial active weapon
- Avoids slot conflicts

### Item Swapping
- Direct swap between equipped items
- Maintains active weapon state
- Preserves visual continuity
- Handles two-hand conflicts

### Realm Restrictions
- Cross-realm items cannot be equipped
- Warning messages provided
- GM override available
- Checked on every equip attempt

### Level Requirements
- Items may have minimum level
- Checked during equip validation
- Prevents low-level exploitation
- Clear error messaging

## Test Scenarios

1. **Basic Equipping**
   - Equip each slot type
   - Verify bonuses applied
   - Check visual updates
   - Confirm restrictions

2. **Conflict Testing**
   - Two-hand vs shield
   - Dual wield validation
   - Active weapon switching
   - Slot blocking

3. **Edge Cases**
   - Full inventory unequip
   - Rapid slot changes
   - Cross-realm items
   - Level restrictions

## System Interactions

### Combat System
- Active weapon determines attack type
- Weapon switching cooldowns
- Shield provides defense bonuses
- Armor affects damage reduction

### Property System
- Equipment bonuses added to stats
- Caps enforced per level
- Stacking rules applied
- Mythical bonuses separate

### Visual System
- Equipment models displayed
- Dye/color preserved
- Guild emblems on cloaks/shields
- Weapon glow effects

## TODO
- Document special weapon restrictions (artifact weapons)
- Add equipment set bonus system details
- Clarify unique item slot behavior
- Detail GM equipment commands 