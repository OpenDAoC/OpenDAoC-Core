# Merchant Trading System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from GameMerchant.cs, TradeWindow.cs, ConsignmentMerchant.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
The Merchant Trading System manages NPC merchants, player-to-player trading, consignment merchants, and various currency systems. It supports multiple merchant types, alternative currencies, and comprehensive trade mechanics.

## Merchant Types

### Standard Merchants
Basic NPCs that buy and sell items for gold:

```csharp
public class GameMerchant : GameNPC
{
    protected MerchantTradeItems m_tradeItems;  // Items for sale
    
    public virtual void OnPlayerBuy(GamePlayer player, int item_slot, int number)
    {
        // Get item template
        DbItemTemplate template = TradeItems.GetItem(page, slot);
        long totalValue = number * template.Price;
        
        // Validate purchase
        if (player.GetCurrentMoney() < totalValue) return;
        if (!player.Inventory.AddTemplate(item, amount)) return;
        
        // Complete transaction
        player.RemoveMoney(totalValue);
        InventoryLogging.LogInventoryAction(this, player, template, amount);
    }
}
```

### Specialty Merchants

#### Stable Masters
Transport merchants for horse routes:
- **Items**: Horse route tickets
- **Function**: Regional travel system
- **Payment**: Standard gold currency

#### Bounty Point Merchants
Alternative currency merchants:

```csharp
public override void OnPlayerBuy(GamePlayer player, int item_slot, int number)
{
    long totalValue = number * template.Price;
    
    if (player.BountyPoints < totalValue)
    {
        player.Out.SendMessage("You need " + totalValue + " bounty points.");
        return;
    }
    
    player.BountyPoints -= totalValue;
    // Add item to inventory
}
```

#### Item Currency Merchants
Use specific items as currency:

```csharp
public abstract class GameItemCurrencyMerchant : GameMerchant
{
    protected DbItemTemplate m_itemTemplate;  // Currency item
    
    public override void OnPlayerBuy(GamePlayer player, int item_slot, int number)
    {
        // Count currency items in inventory
        int currencyCount = player.Inventory.CountItemTemplate(m_moneyItem.Id_nb);
        
        if (currencyCount < totalValue) return;
        
        // Remove currency items from inventory
        player.Inventory.RemoveCountFromTemplate(m_moneyItem.Id_nb, totalValue);
    }
}
```

### Guard Merchants
Keep-based merchants with realm restrictions:
- **Location**: Keep towers and structures
- **Access**: Realm-based purchasing
- **Items**: Keep supplies, siege equipment

## Merchant Trade Windows

### Trade Item Organization
```csharp
public class MerchantTradeItems
{
    public const int MAX_ITEM_IN_TRADEWINDOWS = 30;  // Items per page
    public const int MAX_PAGES = 5;                   // Maximum pages
    
    public DbItemTemplate GetItem(int page, eMerchantWindowSlot slot)
    {
        return m_items[page * MAX_ITEM_IN_TRADEWINDOWS + (int)slot];
    }
}
```

### Item Slot Mapping
```csharp
// Convert item_slot to page and slot
int pagenumber = item_slot / MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;
int slotnumber = item_slot % MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;
```

## Purchase Validation

### Distance Checking
```csharp
if (!merchant.IsWithinRadius(player, GS.ServerProperties.Properties.WORLD_PICKUP_DISTANCE))
{
    player.Out.SendMessage("You are too far away from " + merchant.GetName(0, true));
    return;
}
```

### Money Validation
```csharp
lock (player.Inventory.Lock)
{
    if (player.GetCurrentMoney() < totalValue)
    {
        player.Out.SendMessage("You need " + Money.GetString(totalValue));
        return;
    }
}
```

### Inventory Space
```csharp
if (!player.Inventory.AddTemplate(item, amount, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
{
    player.Out.SendMessage("Not enough inventory space");
    return;
}
```

## Selling to Merchants

### Sell Value Calculation
```csharp
public virtual void OnPlayerSell(GamePlayer player, DbInventoryItem item)
{
    if (!item.IsDropable)
    {
        player.Out.SendMessage("This item can't be sold");
        return;
    }
    
    long itemValue = OnPlayerAppraise(player, item, true);
    
    if (itemValue == 0)
    {
        player.Out.SendMessage(GetName(0, true) + " isn't interested in " + item.GetName(0, false));
        return;
    }
    
    // Remove item and give money
    player.Inventory.RemoveItem(item);
    player.AddMoney(itemValue);
}
```

### Item Appraisal
```csharp
public virtual long OnPlayerAppraise(GamePlayer player, DbInventoryItem item, bool silent)
{
    if (item == null) return 0;
    
    long val = item.Price / 2;  // Default 50% of item price
    
    if (!silent)
    {
        string message = GetName(0, true) + " offers " + Money.GetString(val) + 
                        " for " + item.GetName(0, false);
        player.Out.SendMessage(message, eChatType.CT_Merchant);
    }
    
    return val;
}
```

## Player-to-Player Trading

### Trade Window System
```csharp
public class TradeWindow
{
    private List<DbInventoryItem> m_tradeItems;  // Items offered
    private long m_tradeMoney;                   // Money offered
    private bool m_tradeAccept;                  // Acceptance status
    private bool m_combine;                      // Combine flag
}
```

### Trade Process
1. **Initiate**: `/trade` command or right-click player
2. **Add Items**: Drag items to trade window
3. **Add Money**: Set money amount
4. **Accept**: Both players must accept
5. **Complete**: Items and money exchange

### Trade Validation
```csharp
// Both players must accept
if (!player1.TradeWindow.TradeAccept || !player2.TradeWindow.TradeAccept)
    return false;

// Validate inventory space
if (!ValidateTradeSpace(player1, player2.TradeWindow.TradeItems))
    return false;

// Execute trade
ExecuteTrade(player1, player2);
```

## Consignment Merchants

### Player-Owned Merchants
```csharp
public class ConsignmentMerchant : GameMerchant
{
    public int HouseNumber { get; set; }          // Associated house
    public long TotalMoney { get; set; }          // Money earned
    public Dictionary<string, DbInventoryItem> Items { get; set; }  // Items for sale
}
```

### Consignment Mechanics
- **Setup**: Players place items with set prices
- **Commission**: Server takes percentage fee
- **Access**: Available 24/7 via Market Explorer
- **Payment**: Money held until player retrieval

### Market Explorer Integration
```csharp
public void OnPlayerBuy(GamePlayer player, int item_slot, bool usingMarketExplorer)
{
    int purchasePrice = item.SellPrice;
    
    // Add market fee if using explorer
    if (usingMarketExplorer && ServerProperties.Properties.MARKET_FEE_PERCENT > 0)
        purchasePrice += purchasePrice * ServerProperties.Properties.MARKET_FEE_PERCENT / 100;
        
    // Process purchase
    ProcessConsignmentPurchase(player, item, purchasePrice);
}
```

## Currency Systems

### Standard Currency (Copper)
```csharp
public class Money
{
    public static string GetString(long copperValue)
    {
        long gold = copperValue / 10000;
        long silver = (copperValue % 10000) / 100;  
        long copper = copperValue % 100;
        
        return $"{gold}g {silver}s {copper}c";
    }
}
```

### Alternative Currencies

#### Bounty Points
- **Acquisition**: RvR kills, keep captures
- **Usage**: Special equipment, siege weapons
- **Storage**: Character property

#### Realm Points
- **Acquisition**: RvR participation
- **Usage**: Realm abilities, ranks
- **Storage**: Character property

#### Custom Token Systems
```csharp
// Example: Orb Merchant
public class AtlasAchievementMerchant : GameItemCurrencyMerchant
{
    public override string MoneyKey => ServerProperties.Properties.ALT_CURRENCY_ID;
    
    // Special validation for achievement requirements
    protected override bool ValidatePurchase(GamePlayer player, DbItemTemplate template)
    {
        var mobRequirement = KillCreditUtils.GetRequiredKillMob(template.Id_nb);
        return AchievementUtils.CheckPlayerCredit(mobRequirement, player, (int)player.Realm);
    }
}
```

## Transaction Logging

### Inventory Logging
```csharp
// Log merchant purchases
InventoryLogging.LogInventoryAction(merchant, player, eInventoryActionType.Merchant, template, amount);

// Log money transactions
InventoryLogging.LogInventoryAction(player, merchant, eInventoryActionType.Merchant, totalValue);
```

### Audit Trail
- **Player Actions**: All buy/sell transactions
- **Money Changes**: Complete money flow tracking  
- **Item Movement**: Item creation/destruction
- **Merchant Activity**: Transaction volumes

## Special Merchant Features

### Pack Size Support
```csharp
// Items sold in packs (arrows, bolts, etc.)
int amountToBuy = number;
if (template.PackSize > 0)
    amountToBuy *= template.PackSize;
```

### Level Restrictions
```csharp
// Champion merchants with level requirements
public class GameChampionMerchant : GameMerchant
{
    public override void OnPlayerBuy(GamePlayer player, int item_slot, int number)
    {
        int page = item_slot / MerchantTradeItems.MAX_ITEM_IN_TRADEWINDOWS;
        if (player.ChampionLevel >= page + 2)
            base.OnPlayerBuy(player, item_slot, number);
        else
            player.Out.SendMessage("You must be Champion Level " + (page + 2) + " or higher");
    }
}
```

### Realm Restrictions
- **Guard Merchants**: Only sell to own realm
- **Keep Merchants**: Based on keep ownership  
- **Frontier Merchants**: Realm-specific access

## Configuration Options

### Server Properties
```ini
# Market system settings
MARKET_FEE_PERCENT = 20          # Consignment fee percentage
CONSIGNMENT_USE_BP = false       # Use BP for consignment purchases
WORLD_PICKUP_DISTANCE = 128      # Max merchant interaction distance

# Currency exchange
CURRENCY_EXCHANGE_ALLOW = true
CURRENCY_EXCHANGE_VALUES = "item1|100;item2|50"

# Special events
ORBS_FIRE_SALE = false          # Free orb purchases
```

## Test Scenarios

### Standard Purchase Flow
```csharp
// Given: Player with 1000 copper, merchant selling 100c item
// When: Player buys item
// Then: Player has 900 copper, item in inventory

// Given: Player with insufficient funds
// When: Attempt purchase
// Then: "You need X copper" message, no transaction
```

### Alternative Currency
```csharp
// Given: Player with 500 BP, item costs 300 BP
// When: Purchase from BP merchant
// Then: Player has 200 BP, item acquired

// Given: Player with orb tokens, achievement unlocked
// When: Purchase from achievement merchant  
// Then: Token consumed, special item granted
```

### Trade Window
```csharp
// Given: Two players in trade
// When: Both add items and accept
// Then: Items exchanged between players

// Given: Trade with insufficient space
// When: Accept trade
// Then: "Not enough inventory space" error
```

## Error Handling

### Common Failures
- **Insufficient Funds**: Clear money requirement message
- **No Space**: Inventory full notification
- **Distance**: Too far from merchant warning
- **Restrictions**: Level/realm requirement messages

### Transaction Safety
```csharp
// All transactions use inventory locks
lock (player.Inventory.Lock)
{
    // Validate state hasn't changed
    if (player.GetCurrentMoney() < totalValue)
        throw new Exception("Money amount changed while adding items");
    
    // Execute atomic transaction
    ExecuteTransaction();
}
```

## Integration Points

### Housing System
- **Consignment Merchants**: House-based selling
- **Merchant Permissions**: House access controls

### Keep System  
- **Guard Merchants**: Keep-based vendors
- **Siege Merchants**: Realm war supplies

### Quest System
- **Quest Items**: Special merchant interactions
- **Quest Rewards**: Item turn-ins

## Change Log
- 2024-01-20: Initial comprehensive documentation
- TODO: Document seasonal merchants
- TODO: Add bulk purchase mechanics

## References
- `GameServer/gameobjects/GameMerchant.cs`
- `GameServer/gameobjects/CustomNPC/ConsignmentMerchant.cs`
- `GameServer/packets/Client/168/PlayerBuyRequestHandler.cs`
- `GameServer/packets/Client/168/PlayerSellRequestHandler.cs` 