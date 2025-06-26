# Trade System

**Document Status:** Initial Documentation  
**Completeness:** 85%  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

The trade system allows secure player-to-player item and money exchanges through a synchronized trade window interface. Both players must confirm the trade for it to complete.

## Core Mechanics

### Trade Initiation

#### Starting a Trade
```csharp
// Command: /trade <player>
if (Target == null || Target == this || !(Target is GamePlayer))
{
    Out.SendMessage("Select a player to trade with.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
    return;
}
```

#### Trade Requirements
- **Range**: 2048 units (WorldMgr.INTERACT_DISTANCE)
- **State**: Both players must be alive
- **Combat**: Cannot trade while in combat
- **Availability**: Target cannot be already trading

#### Trade Window States
```csharp
public enum eTradeWindowType : byte
{
    Normal = 0x00,
    Crafting = 0x01,
    Pickup = 0x02,
    Vault = 0x03,
    Quiver = 0x04,
    SalvageMagical = 0x08,
    Count = 0x09,
    PlayerTrade = 0x0a,
    HousingMerchant = 0x0b
}
```

### Trade Window Interface

#### Window Layout
- **Slots**: 10 trade slots per player
- **Money Field**: Separate field for currency
- **Accept Buttons**: Both players must accept
- **Cancel Option**: Either player can cancel

#### Item Placement
```csharp
public bool AddTradeItem(eInventorySlot slot, int page)
{
    DbInventoryItem item = m_player.Inventory.GetItem(slot);
    if (item == null)
        return false;
        
    if (!item.IsDropable || !item.IsTradable || item.IsDeleted)
        return false;
        
    lock (m_sync)
    {
        if (m_tradeAccept)
        {
            m_tradeAccept = false;
            m_partnerTradeAccept = false;
        }
    }
}
```

### Trade Validation

#### Item Restrictions
- **Not Tradable**: Items marked as non-tradable
- **Not Droppable**: Items that cannot be dropped
- **Quest Items**: Some quest items restricted
- **Bound Items**: Soul-bound items cannot be traded
- **Damaged Items**: Items at 0% condition (need repair)

#### Pre-Trade Checks
```csharp
// Weight check
if (myTradeItem.Weight + toPlayer.Inventory.InventoryWeight > toPlayer.MaxCarryWeight)
{
    // Trade fails - overweight
}

// Inventory space check
if (toPlayer.Inventory.FindFirstEmptySlot(eInventorySlot.FirstBackpack, 
    eInventorySlot.LastBackpack) == eInventorySlot.Invalid)
{
    // Trade fails - no space
}
```

### Trade Execution

#### Acceptance Process
1. **First Accept**: Locks trade items
2. **Partner Accept**: Both sides locked
3. **Final Confirmation**: Trade executes
4. **Rollback**: On any failure

#### Trade Steps
```csharp
// 1. Remove items from source
fromPlayer.Inventory.RemoveTradeItems(tradeItems);

// 2. Add items to target
bool[] toSlots = toPlayer.Inventory.AddTradeItems(partnerTradeItems);

// 3. Transfer money
if (myTradeMoney > 0)
{
    fromPlayer.RemoveMoney(myTradeMoney);
    toPlayer.AddMoney(myTradeMoney);
}

// 4. Log transaction
InventoryLogging.LogInventoryAction(fromPlayer, toPlayer, 
    eInventoryActionType.Trade, item, count);
```

### Trade Messages

#### System Messages
```csharp
// Trade initiated
"You offer to trade with " + target.Name

// Trade accepted
target.Name + " accepts your trade offer."

// Trade completed
"Trade completed. You receive: [items/money]"

// Trade cancelled
"Trade cancelled."
```

#### Error Messages
- "Target is too far away to trade!"
- "You are already trading with someone else!"
- "Your trade partner doesn't have enough room!"
- "You don't have enough carry capacity!"
- "That item cannot be traded!"

### Safety Features

#### Double Confirmation
- Both players must click accept
- Any change resets acceptance
- Visual indication of accept status

#### Anti-Scam Protection
- Items locked after first accept
- Cannot modify after partner accepts
- Full visibility of all items/money

#### Transaction Atomicity
```csharp
lock (m_sync)
{
    // All operations atomic
    // Either complete success or full rollback
}
```

## System Interactions

### Inventory Integration
- Items moved through inventory system
- Weight calculations enforced
- Stack handling for consumables

### Logging System
```csharp
InventoryLogging.LogInventoryAction(
    fromPlayer,     // Source
    toPlayer,       // Target
    eInventoryActionType.Trade,
    item.Template,
    item.Count
);
```

### Combat System
- Trade cancelled if either player enters combat
- Cannot initiate trade while in combat
- Attack interrupts active trades

### Distance Checks
- Continuous range validation
- Auto-cancel if players move apart
- Uses standard interact distance

## Implementation Notes

### Packet Structure
- OpenTradeWindow packet
- TradeWindowUpdate packets
- CloseTrade packet

### Synchronization
- Thread-safe operations
- Synchronized between clients
- Prevents race conditions

### Performance
- Minimal server load
- Client-side UI handling
- Efficient item validation

## Edge Cases

### Simultaneous Trades
- Player can only have one trade active
- New trade requests rejected
- Clear error messaging

### Disconnection Handling
- Trade auto-cancels on disconnect
- Items returned to original owner
- No item loss possible

### Full Inventory
- Pre-check prevents issues
- Clear messaging to players
- Suggests making space

### Stack Splitting
- Partial stack trades supported
- Automatic stack management
- Prevents overflow exploits

## Test Scenarios

1. **Basic Trade Flow**
   - Initiate trade with valid target
   - Add items and money
   - Both players accept
   - Verify completion

2. **Validation Tests**
   - Non-tradable items rejected
   - Weight limits enforced
   - Inventory space checked
   - Distance maintained

3. **Cancellation Tests**
   - Player cancels trade
   - Moving cancels trade
   - Combat cancels trade
   - Disconnect handling

4. **Edge Case Tests**
   - Full inventory scenarios
   - Maximum weight trades
   - Stack splitting
   - Simultaneous requests

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Initial documentation | 