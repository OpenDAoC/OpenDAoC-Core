# Money and Currency System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from Money.cs, GamePlayer.cs, GameMerchant.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: DAoC uses a traditional tiered money system with copper, silver, gold, platinum, and mithril coins. 100 copper equals 1 silver, 100 silver equals 1 gold, 1000 gold equals 1 platinum, and 1000 platinum equals 1 mithril. Besides regular money, there are special currencies like bounty points (earned in RvR), realm points (also from RvR), and various item-based currencies like aurulite and dragon scales. Money drops from monsters, quest rewards, and selling items to merchants. The game also has guild dues that automatically take a percentage of your earnings for your guild's bank account.

DAoC uses a multi-tiered currency system with standard money (copper through mithril) and various alternative currencies for special purchases. The system handles conversion, trading, and various economic interactions.

## Core Mechanics

### Standard Currency

#### Currency Tiers
```
1 Copper = Base unit
100 Copper = 1 Silver
100 Silver = 1 Gold
1000 Gold = 1 Platinum
1000 Platinum = 1 Mithril
```

#### Internal Representation
All money is stored as a single long value in copper:
```csharp
long totalMoney = GetMoney(mithril, platinum, gold, silver, copper);
// Formula: ((((mithril * 1000 + platinum) * 1000 + gold) * 100 + silver) * 100 + copper)
```

#### Extraction Methods
```csharp
int GetMithril(long money) => money / 100L / 100L / 1000L / 1000L % 1000L;
int GetPlatinum(long money) => money / 100L / 100L / 1000L % 1000L;
int GetGold(long money) => money / 100L / 100L % 1000L;
int GetSilver(long money) => money / 100L % 100L;
int GetCopper(long money) => money % 100L;
```

### Player Money Management

#### Adding Money
```csharp
public void AddMoney(long money, string messageFormat, eChatType ct, eChatLoc cl)
{
    long newMoney = GetCurrentMoney() + money;
    
    // Update individual currency values
    Copper = Money.GetCopper(newMoney);
    Silver = Money.GetSilver(newMoney);
    Gold = Money.GetGold(newMoney);
    Platinum = Money.GetPlatinum(newMoney);
    Mithril = Money.GetMithril(newMoney);
    
    Out.SendUpdateMoney();
}
```

#### Removing Money
```csharp
public bool RemoveMoney(long money, string messageFormat, eChatType ct, eChatLoc cl)
{
    if (money > GetCurrentMoney())
        return false;
        
    long newMoney = GetCurrentMoney() - money;
    // Update currencies...
    return true;
}
```

### Alternative Currencies

#### Bounty Points
- **Usage**: Special items, Master Level abilities
- **Source**: RvR kills, keep captures
- **Storage**: Separate from regular money

#### Realm Points
- **Usage**: Realm abilities
- **Source**: RvR combat
- **Storage**: Character-specific

#### Special Item Currencies
```csharp
public abstract class GameItemCurrencyMerchant : GameMerchant
{
    public virtual string MoneyKey { get; }
    protected DbItemTemplate m_itemTemplate;
    protected WorldInventoryItem m_moneyItem;
}
```

Common item currencies:
- **Aurulite**: ML encounters
- **Atlantean Glass**: Atlantis zones
- **Dragon Scales**: Dragon raids
- **Emerald Seals**: Various sources
- **Atlas Orbs**: Server-specific currency

### Money Drops

#### GameMoney Object
```csharp
public class GameMoney : GameStaticItemTimed
{
    public long Value { get; set; } // In copper
    
    // Auto-calculated from Value
    public int Mithril => Money.GetMithril(Value);
    public int Platinum => Money.GetPlatinum(Value);
    public int Gold => Money.GetGold(Value);
    public int Silver => Money.GetSilver(Value);
    public int Copper => Money.GetCopper(Value);
}
```

#### Drop Models
- **Model 488**: Bag of coins (default)
- **Small chest**: Higher values
- **Large chest**: Highest values
- **Copper coins**: Low values

### Guild Dues System

#### Automatic Deduction
```csharp
public long ApplyGuildDues(long money)
{
    Guild guild = Guild;
    if (guild == null || !guild.IsGuildDuesOn())
        return money;
        
    long moneyToGuild = money * guild.GetGuildDuesPercent() / 100;
    
    if (moneyToGuild > 0 && guild.AddToBank(moneyToGuild, false))
        return money - moneyToGuild;
        
    return money;
}
```

#### Guild Bank
- **Storage**: Centralized guild funds
- **Access**: Based on guild rank
- **Usage**: Keep claims, guild items

## Trading System

### Player-to-Player Trade

#### Trade Window
```csharp
public class TradeWindow
{
    List<DbInventoryItem> TradeItems;
    long TradeMoney;
    bool TradeAccept;
}
```

#### Trade Process
1. **Initiate**: /trade command
2. **Add Items**: Drag to window
3. **Add Money**: Set amount
4. **Accept**: Both players confirm
5. **Complete**: Items/money exchange

### Merchant System

#### Standard Merchants
```csharp
public virtual void OnPlayerBuy(GamePlayer player, int item_slot, int number)
{
    DbItemTemplate template = TradeItems.GetItem(page, slot);
    int amountToBuy = number * template.PackSize;
    long totalValue = number * template.Price;
    
    if (player.GetCurrentMoney() < totalValue)
        return; // Not enough money
        
    if (!player.Inventory.AddTemplate(item, amountToBuy))
        return; // Not enough space
        
    player.RemoveMoney(totalValue, message);
}
```

#### Item Currency Merchants
```csharp
public override void OnPlayerBuy(GamePlayer player, int item_slot, int number)
{
    // Count currency items in inventory
    int currencyCount = player.Inventory.CountItemTemplate(m_moneyItem.Id_nb);
    
    if (currencyCount < totalValue)
        return; // Not enough currency
        
    // Remove currency items
    player.Inventory.RemoveCountFromTemplate(m_moneyItem.Id_nb, totalValue);
}
```

### Selling to Merchants

#### Sell Value Calculation
```csharp
public virtual void OnPlayerSell(GamePlayer player, DbInventoryItem item)
{
    long itemValue = item.Price / 2; // 50% of base price
    
    if (item.Condition < item.MaxCondition)
        itemValue = itemValue * item.Condition / item.MaxCondition;
        
    if (item.Quality < 100)
        itemValue = itemValue * item.Quality / 100;
        
    player.AddMoney(itemValue);
    player.Inventory.RemoveItem(item);
}
```

## Economic Balancing

### Money Sinks
- **Repairs**: Item condition restoration
- **Keep Claims**: Large guild expenses
- **Housing**: Rent and decorations
- **Teleportation**: Convenience costs
- **Respecs**: Character rebuilding

### Money Sources
- **Monster Kills**: Level-based drops
- **Quest Rewards**: Fixed amounts
- **Item Sales**: To merchants
- **Trade Skills**: Crafted items
- **Player Trade**: Economic circulation

### Price Guidelines
- **Basic Items**: 1-50 silver
- **Advanced Items**: 1-50 gold
- **Epic Items**: 1+ platinum
- **Housing**: 1+ platinum
- **Keep Claims**: 100+ gold

## Special Cases

### Consignment Merchants
- **Player-owned**: In housing
- **Commission**: Percentage of sale
- **Search System**: Market-wide
- **Price Setting**: Player-controlled

### Repair Costs
```csharp
RepairCost = (MaxCondition - Condition) * ItemLevel * RepairModifier
```

### Stack Handling
- **Pack Size**: Bundled items
- **Stack Limit**: 200 per slot
- **Split Stacks**: Partial trades

## Performance Considerations

### Money Updates
- **Batched**: Multiple changes
- **Client Sync**: SendUpdateMoney()
- **Database**: Periodic saves

### Trade Validation
- **Server Authority**: All trades verified
- **Atomic Operations**: Prevent duplication
- **Rollback**: On failure

## Test Scenarios

### Basic Purchase
```
Given: Player with 10 gold
Item Cost: 5 gold
Expected:
- Player has 5 gold remaining
- Item in inventory
- Purchase message sent
```

### Currency Exchange
```
Given: Player with 100 aurulite
Item Cost: 50 aurulite
Expected:
- 50 aurulite removed
- Item added to inventory
- Currency-specific message
```

### Guild Dues
```
Given: Guild with 10% dues, player loots 100 gold
Expected:
- Player receives 90 gold
- Guild bank receives 10 gold
- Dues message displayed
```

## Configuration

### Server Properties
```csharp
ALT_CURRENCY_ID              // Alternative currency template
CURRENCY_EXCHANGE_ALLOW      // Enable currency exchange
CURRENCY_EXCHANGE_VALUES     // Exchange rates
WORLD_PICKUP_DISTANCE       // Trading range
```

### Economic Settings
- **Drop Rates**: Monster loot tables
- **Vendor Prices**: Item templates
- **Repair Modifiers**: Server-configurable
- **Trade Restrictions**: Level/realm limits

## Change Log

### 2024-01-20
- Initial documentation based on code analysis
- Added currency system details
- Documented trading mechanics
- Added alternative currencies

## References
- `GameServer/gameutils/Money.cs`
- `GameServer/gameobjects/GamePlayer.cs`
- `GameServer/gameobjects/GameMerchant.cs`
- `GameServer/gameobjects/GameMoney.cs` 