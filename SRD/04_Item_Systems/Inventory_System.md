# Inventory System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Stable
- **Verification**: Code-verified from GamePlayerInventory.cs, IGameInventory.cs, GameInventoryObject.cs
- **Implementation**: Stable

## Overview
The inventory system manages all item storage and movement for players, NPCs, vaults, and merchants. It provides a unified interface for backpacks, equipment slots, vaults, housing storage, and special containers with complex validation and stacking mechanics.

## Core Architecture

### Inventory Interface
```csharp
public interface IGameInventory
{
    bool LoadFromDatabase(string inventoryId);
    bool SaveIntoDatabase(string inventoryId);
    
    bool AddItem(eInventorySlot slot, DbInventoryItem item);
    bool RemoveItem(DbInventoryItem item);
    bool MoveItem(eInventorySlot fromSlot, eInventorySlot toSlot, int itemCount);
    
    DbInventoryItem GetItem(eInventorySlot slot);
    ICollection<DbInventoryItem> GetItemRange(eInventorySlot minSlot, eInventorySlot maxSlot);
    
    void BeginChanges();
    void CommitChanges();
}
```

### Inventory Slots
```csharp
public enum eInventorySlot : int
{
    // Special slots
    LastEmptyBagHorse   = -8,
    FirstEmptyBagHorse  = -7,
    LastEmptyQuiver     = -6,
    FirstEmptyQuiver    = -5,
    LastEmptyVault      = -4,
    FirstEmptyVault     = -3,
    LastEmptyBackpack   = -2,
    FirstEmptyBackpack  = -1,
    
    Invalid             = 0,
    Ground              = 1,
    
    // Equipment (10-37)
    RightHandWeapon     = 10,
    LeftHandWeapon      = 11,
    TwoHandWeapon       = 12,
    DistanceWeapon      = 13,
    FirstQuiver         = 14,  // Through 17
    HeadArmor           = 21,
    HandsArmor          = 22,
    FeetArmor           = 23,
    Jewelry             = 24,
    TorsoArmor          = 25,
    Cloak               = 26,
    LegsArmor           = 27,
    ArmsArmor           = 28,
    Neck                = 29,
    Waist               = 32,
    LeftBracer          = 33,
    RightBracer         = 34,
    LeftRing            = 35,
    RightRing           = 36,
    Mythical            = 37,
    
    // Storage
    FirstBackpack       = 40,
    LastBackpack        = 79,
    FirstBagHorse       = 80,
    LastBagHorse        = 95,
    FirstVault          = 110,
    LastVault           = 149,
    
    // Special ranges
    HousingInventory_First = 150,
    HousingInventory_Last  = 249,
    HouseVault_First    = 1000,
    HouseVault_Last     = 1799,
    Consignment_First   = 2000,
    Consignment_Last    = 2099,
    AccountVault_First  = 2500,
    AccountVault_Last   = 2699,
}
```

## Inventory Types

### Player Inventory
```csharp
public class GamePlayerInventory : GameLivingInventory
{
    // 40 backpack slots
    // 40 vault slots  
    // Equipment slots
    // Money slots
    
    protected Dictionary<eInventorySlot, DbInventoryItem> m_items;
    protected List<DbInventoryItem> _itemsAwaitingDeletion;
}
```

### Vault Systems
```csharp
public class GameVault : GameStaticItem, IGameInventoryObject
{
    private const int VAULT_SIZE = 100;
    
    public virtual eInventorySlot FirstClientSlot => eInventorySlot.HousingInventory_First;
    public virtual eInventorySlot LastClientSlot => eInventorySlot.HousingInventory_Last;
    public virtual int FirstDbSlot => (int)eInventorySlot.HouseVault_First + VaultSize * Index;
    public virtual int LastDbSlot => FirstDbSlot + VaultSize - 1;
}
```

### Special Inventories
- **Horse Bags**: 16 slots (80-95)
- **Quivers**: 4 slots for arrows/bolts
- **Housing Storage**: 100 slots per vault
- **Consignment Merchant**: 100 slots
- **Account Vault**: 200 slots shared

## Item Management

### Adding Items
```csharp
public virtual bool AddItem(eInventorySlot slot, DbInventoryItem item)
{
    lock (Lock)
    {
        // Validate slot
        slot = GetValidInventorySlot(slot);
        if (slot == eInventorySlot.Invalid) 
            return false;
            
        // Check slot empty
        if (m_items.ContainsKey(slot))
            return false;
            
        // Add to collection
        m_items.Add(slot, item);
        item.SlotPosition = (int)slot;
        item.OwnerID = m_player.InternalID;
        
        // Handle equipment
        if (IsEquippedSlot(slot))
            m_player.OnItemEquipped(item, eInventorySlot.Invalid);
            
        // Notify item
        (item as IGameInventoryItem)?.OnReceive(m_player);
        
        return true;
    }
}
```

### Removing Items
```csharp
public virtual bool RemoveItem(DbInventoryItem item)
{
    lock (Lock)
    {
        var slot = (eInventorySlot)item.SlotPosition;
        
        if (!m_items.Remove(slot))
            return false;
            
        // Handle equipment
        if (IsEquippedSlot(slot))
            m_player.OnItemUnequipped(item, slot);
            
        // Notify item
        (item as IGameInventoryItem)?.OnLose(m_player);
        
        // Mark for deletion
        if (markForDeletion)
            _itemsAwaitingDeletion.Add(item);
            
        return true;
    }
}
```

### Moving Items
```csharp
public virtual bool MoveItem(eInventorySlot fromSlot, eInventorySlot toSlot, int itemCount)
{
    lock (Lock)
    {
        // Get items
        m_items.TryGetValue(fromSlot, out DbInventoryItem fromItem);
        m_items.TryGetValue(toSlot, out DbInventoryItem toItem);
        
        // Try combine first
        if (!CombineItems(fromItem, toItem))
        {
            // Try stacking
            if (!StackItems(fromSlot, toSlot, itemCount))
            {
                // Finally swap
                SwapItems(fromSlot, toSlot);
            }
        }
        
        // Update changed slots
        if (!m_changedSlots.Contains(fromSlot))
            m_changedSlots.Add(fromSlot);
        if (!m_changedSlots.Contains(toSlot))
            m_changedSlots.Add(toSlot);
            
        return true;
    }
}
```

## Stacking System

### Stack Validation
```csharp
public bool IsStackable => Count > 1 || MaxCount > 1;

// Items stack if:
// 1. Same item template ID
// 2. Both are stackable
// 3. Not at max count
// 4. Same properties (condition, quality, etc.)
```

### Stack Operations
```csharp
protected bool StackItems(eInventorySlot fromSlot, eInventorySlot toSlot, int count)
{
    // Validate stackable
    if (!fromItem.IsStackable || !toItem.IsStackable)
        return false;
        
    // Check same item
    if (fromItem.Name != toItem.Name)
        return false;
        
    // Calculate transfer amount
    int transferCount = Math.Min(count, toItem.MaxCount - toItem.Count);
    transferCount = Math.Min(transferCount, fromItem.Count);
    
    // Transfer counts
    fromItem.Count -= transferCount;
    toItem.Count += transferCount;
    
    // Remove empty stack
    if (fromItem.Count <= 0)
        m_items.Remove(fromSlot);
        
    return true;
}
```

## Equipment System

### Equipment Slots
```csharp
public static readonly eInventorySlot[] EQUIP_SLOTS =
{
    eInventorySlot.Horse,
    eInventorySlot.HorseArmor,
    eInventorySlot.HorseBarding,
    eInventorySlot.RightHandWeapon,
    eInventorySlot.LeftHandWeapon,
    eInventorySlot.TwoHandWeapon,
    eInventorySlot.DistanceWeapon,
    eInventorySlot.FirstQuiver,  // Through FourthQuiver
    eInventorySlot.HeadArmor,
    eInventorySlot.HandsArmor,
    eInventorySlot.FeetArmor,
    eInventorySlot.Jewelry,
    eInventorySlot.TorsoArmor,
    eInventorySlot.Cloak,
    eInventorySlot.LegsArmor,
    eInventorySlot.ArmsArmor,
    eInventorySlot.Neck,
    eInventorySlot.Waist,
    eInventorySlot.LeftBracer,
    eInventorySlot.RightBracer,
    eInventorySlot.LeftRing,
    eInventorySlot.RightRing,
    eInventorySlot.Mythical,
};
```

### Equipment Validation
```csharp
protected bool IsValidSlotForItem(DbInventoryItem item, eInventorySlot slot)
{
    switch (slot)
    {
        case eInventorySlot.TwoHandWeapon:
            return item.Hand == 1; // Two-handed
            
        case eInventorySlot.LeftHandWeapon:
            return item.Object_Type == (int)eObjectType.Shield ||
                   item.Hand == 2; // Left hand
                   
        case eInventorySlot.HeadArmor:
            return item.Item_Type == Slot.HELM;
            
        case eInventorySlot.LeftRing:
        case eInventorySlot.RightRing:
            return item.Item_Type == Slot.LEFTRING ||
                   item.Item_Type == Slot.RIGHTRING;
                   
        case eInventorySlot.FirstQuiver: // Through FourthQuiver
            return item.Object_Type == (int)eObjectType.Arrow ||
                   item.Object_Type == (int)eObjectType.Bolt;
    }
}
```

### Active Weapon System
```csharp
// Visible active weapon slots encoded in byte
// Low nibble: Right hand (0x00-0x02)
// High nibble: Left hand (0x10-0x30)

public bool IsEquippedSlot(eInventorySlot slot)
{
    switch (slot)
    {
        case eInventorySlot.RightHandWeapon:
            return (m_player.VisibleActiveWeaponSlots & 0x0F) == 0x00;
            
        case eInventorySlot.LeftHandWeapon:
            return (m_player.VisibleActiveWeaponSlots & 0xF0) == 0x10;
            
        case eInventorySlot.TwoHandWeapon:
            return (m_player.VisibleActiveWeaponSlots & 0x0F) == 0x02;
    }
}
```

## Special Item Types

### Combinable Items
```csharp
public interface IGameInventoryItem
{
    bool Combine(GamePlayer player, DbInventoryItem targetItem);
}

// Examples:
// - Alchemy ingredients
// - Spell crafting gems
// - Siege ammunition
```

### Charges/Uses
```csharp
// Items with limited uses
public int Charges { get; set; }  // Current charges
public int MaxCharges { get; set; } // Maximum charges

// Charge types:
// - Poison charges
// - Magic item charges
// - Instrument uses
// - Repair kit uses
```

## Inventory Updates

### Change Tracking
```csharp
protected List<eInventorySlot> m_changedSlots = [];
protected int m_changesCounter;

public void BeginChanges()
{
    m_changesCounter++;
}

public void CommitChanges()
{
    if (--m_changesCounter <= 0)
    {
        m_changesCounter = 0;
        UpdateChangedSlots();
    }
}

protected virtual void UpdateChangedSlots()
{
    if (m_changedSlots.Count > 0)
    {
        // Send updates to client
        player.Out.SendInventorySlotsUpdate(m_changedSlots);
        m_changedSlots.Clear();
    }
}
```

### Client Updates
```csharp
// Full inventory update
player.Out.SendInventoryItemsUpdate(eInventoryWindowType.Equipment, items);

// Partial slot updates
player.Out.SendInventorySlotsUpdate(changedSlots);

// Weight update
player.Out.SendUpdateMaxEncumbrance();
```

## Inventory Windows

### Window Types
```csharp
public enum eInventoryWindowType : byte
{
    Equipment = 0x00,   // Character equipment
    Inventory = 0x01,   // Backpack
    HouseVault = 0x02,  // Housing vault
    Consignment = 0x03, // Merchant
    Trade = 0x04,       // Trade window
}
```

### Observer Pattern
```csharp
public class GameVault : GameStaticItem, IGameInventoryObject
{
    protected Dictionary<string, GamePlayer> _observers = [];
    
    public virtual void AddObserver(GamePlayer player)
    {
        _observers[player.Name] = player;
        player.ActiveInventoryObject = this;
    }
    
    public virtual void RemoveObserver(GamePlayer player)
    {
        _observers.Remove(player.Name);
        player.ActiveInventoryObject = null;
    }
}
```

## Database Persistence

### Save Strategy
```csharp
public virtual bool SaveIntoDatabase(string inventoryId)
{
    lock (Lock)
    {
        // Save all items
        foreach (DbInventoryItem item in m_items.Values)
        {
            if (item.IsDirty)
                GameInventoryObjectExtensions.SaveItem(item);
        }
        
        // Delete removed items
        foreach (DbInventoryItem item in _itemsAwaitingDeletion)
        {
            GameInventoryObjectExtensions.DeleteItem(item);
        }
        
        _itemsAwaitingDeletion.Clear();
    }
}
```

### Load Strategy
```csharp
public virtual bool LoadFromDatabase(string inventoryId)
{
    var items = DOLDB<DbInventoryItem>.SelectObjects(
        DB.Column("OwnerID").IsEqualTo(inventoryId));
        
    foreach (DbInventoryItem item in items)
    {
        var slot = (eInventorySlot)item.SlotPosition;
        m_items[slot] = item;
    }
    
    return true;
}
```

## Special Operations

### Find Empty Slot
```csharp
protected virtual eInventorySlot GetValidInventorySlot(eInventorySlot slot)
{
    switch (slot)
    {
        case eInventorySlot.FirstEmptyBackpack:
            return FindFirstEmptySlot(eInventorySlot.FirstBackpack, 
                                    eInventorySlot.LastBackpack);
                                    
        case eInventorySlot.LastEmptyBackpack:
            return FindLastEmptySlot(eInventorySlot.FirstBackpack,
                                   eInventorySlot.LastBackpack);
    }
    
    return slot;
}
```

### Template Operations
```csharp
public bool AddTemplate(DbInventoryItem template, int count, 
                       eInventorySlot minSlot, eInventorySlot maxSlot)
{
    // Find existing stack
    foreach (DbInventoryItem item in GetItemRange(minSlot, maxSlot))
    {
        if (item.Id_nb == template.Id_nb && item.Count < item.MaxCount)
        {
            int add = Math.Min(count, item.MaxCount - item.Count);
            item.Count += add;
            count -= add;
        }
    }
    
    // Create new stacks
    while (count > 0)
    {
        eInventorySlot slot = FindFirstEmptySlot(minSlot, maxSlot);
        if (slot == eInventorySlot.Invalid)
            return false;
            
        var newItem = GameInventoryItem.Create(template);
        newItem.Count = Math.Min(count, newItem.MaxCount);
        AddItem(slot, newItem);
        count -= newItem.Count;
    }
    
    return true;
}
```

## Test Scenarios

### Basic Operations
```
1. Add item to empty slot
2. Move item between slots
3. Stack items
4. Split stacks
5. Equip/unequip items
```

### Validation Tests
```
1. Invalid slot rejection
2. Equipment restrictions
3. Stack limit enforcement
4. Weight calculations
```

### Cross-System Tests
```
1. Vault to inventory transfer
2. Trade window operations
3. Merchant interactions
4. Ground drops/pickups
```

## Edge Cases

### Full Inventory
- Find empty slot returns Invalid
- Stack attempts check max count
- Template additions may partially succeed

### Item Corruption
- Invalid slot positions logged
- Owner ID mismatches detected
- Database save failures handled

### Concurrent Access
- All operations use inventory lock
- Observer updates batched
- Change tracking atomic

## Change Log

### 2025-01-20
- Initial documentation created
- Complete slot system documented
- Added stacking mechanics
- Included persistence details

## References
- GamePlayerInventory.cs: Player inventory implementation
- IGameInventory.cs: Core inventory interface
- GameInventoryObject.cs: Vault/merchant extensions
- PlayerMoveItemRequestHandler.cs: Client packet handling 