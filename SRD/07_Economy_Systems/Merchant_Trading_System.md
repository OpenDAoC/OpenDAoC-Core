# Merchant & Trading System

**Document Status:** Core mechanics documented  
**Verification:** Code-verified from merchant implementations  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: The Merchant & Trading System lets you buy items from NPCs and trade safely with other players. Different types of merchants sell different things - equipment merchants have weapons and armor, supplies merchants sell arrows and reagents, and special merchants accept alternative currencies like bounty points. Prices can vary based on your reputation and supply/demand. Player-to-player trading uses a secure trade window where both players must accept before anything changes hands, preventing scams. You can also set up consignment merchants in houses to sell items to other players even when you're offline, with the merchant taking a small commission on sales.

The Merchant & Trading System manages all NPC-based commerce, dynamic pricing, stock management, and player-to-player trading mechanics. This system forms the economic foundation of the game world.

## Core Architecture

### Merchant Types
```csharp
public enum eMerchantType
{
    Equipment,      // Weapons, armor, items
    Supplies,       // Arrows, reagents, consumables
    Trainer,        // Skill training
    Specialty,      // Unique items, artifacts
    Repair,         // Equipment repair services
    Horse,          // Mount vendors
    Consignment,    // Player-owned merchants
    Guild,          // Guild-specific vendors
    Bounty,         // Bounty point merchants
    Realm           // Realm point merchants
}

public interface IMerchant
{
    eMerchantType MerchantType { get; }
    List<ITradeItem> Inventory { get; }
    long Money { get; set; }
    bool CanTrade(GamePlayer player);
    void RefreshStock();
    double GetPriceModifier(IItem item, GamePlayer player);
}
```

## NPC Merchant System

### Basic Trading Mechanics
```csharp
public class GameMerchant : GameNPC, IMerchant
{
    public override bool Interact(GamePlayer player)
    {
        if (!CanTrade(player))
        {
            SayTo(player, "I don't trade with your kind!");
            return false;
        }
        
        ShowTradeWindow(player);
        return true;
    }
    
    public bool CanTrade(GamePlayer player)
    {
        // Realm restrictions
        if (Realm != eRealm.None && Realm != player.Realm)
            return false;
            
        // Faction restrictions
        if (HasFactionRestrictions() && !MeetsFactionRequirements(player))
            return false;
            
        return true;
    }
    
    private void ShowTradeWindow(GamePlayer player)
    {
        var availableItems = GetAvailableItems(player);
        var merchantWindow = new MerchantTradeWindow(this, availableItems);
        player.OpenTradeWindow(merchantWindow);
    }
}
```

### Dynamic Pricing System
```csharp
public class MerchantPricing
{
    public static long CalculateBuyPrice(IItem item, GamePlayer player, GameMerchant merchant)
    {
        long basePrice = item.Value;
        
        // Base price modifiers
        double priceModifier = 1.0;
        
        // Merchant type modifier
        priceModifier *= GetMerchantTypeModifier(merchant.MerchantType);
        
        // Player reputation modifier
        priceModifier *= GetReputationModifier(player, merchant);
        
        // Supply and demand
        priceModifier *= GetSupplyDemandModifier(item, merchant);
        
        // Server economy settings
        priceModifier *= Properties.MERCHANT_PRICE_MODIFIER;
        
        return (long)(basePrice * priceModifier);
    }
    
    public static long CalculateSellPrice(IItem item, GamePlayer player, GameMerchant merchant)
    {
        long buyPrice = CalculateBuyPrice(item, player, merchant);
        
        // Players typically get 40-60% of buy price when selling
        double sellModifier = 0.5;
        
        // Condition affects sell price
        sellModifier *= (double)item.Condition / item.MaxCondition;
        
        // Quality affects sell price
        sellModifier *= item.Quality / 100.0;
        
        return (long)(buyPrice * sellModifier);
    }
    
    private static double GetSupplyDemandModifier(IItem item, GameMerchant merchant)
    {
        int itemCount = merchant.Inventory.Count(i => i.TemplateID == item.TemplateID);
        
        if (itemCount == 0)
            return 1.2; // High demand, higher price
        else if (itemCount > 10)
            return 0.8; // Oversupply, lower price
        
        return 1.0; // Normal pricing
    }
}
```

### Stock Management
```csharp
public class MerchantStockManager
{
    public void RefreshStock(GameMerchant merchant)
    {
        // Remove items that have been in stock too long
        var expiredItems = merchant.Inventory
            .Where(item => item.StockTime < DateTime.Now.AddHours(-24))
            .ToList();
            
        foreach (var item in expiredItems)
        {
            merchant.Inventory.Remove(item);
        }
        
        // Add new random items based on merchant type
        AddRandomStock(merchant);
        
        // Restock essential items
        RestockEssentialItems(merchant);
    }
    
    private void AddRandomStock(GameMerchant merchant)
    {
        var itemPool = GetItemPoolForMerchant(merchant.MerchantType);
        int itemsToAdd = Util.Random(3, 8);
        
        for (int i = 0; i < itemsToAdd; i++)
        {
            var randomItem = itemPool[Util.Random(itemPool.Count)];
            var merchantItem = CreateMerchantItem(randomItem);
            merchant.Inventory.Add(merchantItem);
        }
    }
    
    private void RestockEssentialItems(GameMerchant merchant)
    {
        var essentialItems = GetEssentialItemsForMerchant(merchant.MerchantType);
        
        foreach (var essentialTemplate in essentialItems)
        {
            int currentCount = merchant.Inventory.Count(i => i.TemplateID == essentialTemplate);
            int targetCount = GetTargetStockLevel(essentialTemplate);
            
            for (int i = currentCount; i < targetCount; i++)
            {
                var item = CreateMerchantItem(essentialTemplate);
                merchant.Inventory.Add(item);
            }
        }
    }
}
```

## Player-to-Player Trading

### Secure Trade System
```csharp
public class PlayerTrade
{
    public GamePlayer Player1 { get; set; }
    public GamePlayer Player2 { get; set; }
    public List<IItem> Player1Items { get; set; } = new();
    public List<IItem> Player2Items { get; set; } = new();
    public long Player1Money { get; set; }
    public long Player2Money { get; set; }
    public bool Player1Accepted { get; set; }
    public bool Player2Accepted { get; set; }
    public bool TradeCompleted { get; set; }
    
    public bool AddItemToTrade(GamePlayer player, IItem item)
    {
        if (TradeCompleted)
            return false;
            
        if (!CanTradeItem(item))
        {
            player.SendMessage("This item cannot be traded.");
            return false;
        }
        
        if (player == Player1)
        {
            Player1Items.Add(item);
            Player1Accepted = false; // Reset acceptance
        }
        else if (player == Player2)
        {
            Player2Items.Add(item);
            Player2Accepted = false; // Reset acceptance
        }
        
        UpdateTradeWindow();
        return true;
    }
    
    public void AcceptTrade(GamePlayer player)
    {
        if (player == Player1)
            Player1Accepted = true;
        else if (player == Player2)
            Player2Accepted = true;
            
        if (Player1Accepted && Player2Accepted)
        {
            CompleteTrade();
        }
        
        UpdateTradeWindow();
    }
    
    private void CompleteTrade()
    {
        // Validate trade is still possible
        if (!ValidateTrade())
        {
            CancelTrade("Trade validation failed.");
            return;
        }
        
        // Transfer items
        TransferItems(Player1, Player2, Player1Items);
        TransferItems(Player2, Player1, Player2Items);
        
        // Transfer money
        if (Player1Money > 0)
        {
            Player1.RemoveMoney(Player1Money);
            Player2.AddMoney(Player1Money);
        }
        
        if (Player2Money > 0)
        {
            Player2.RemoveMoney(Player2Money);
            Player1.AddMoney(Player2Money);
        }
        
        TradeCompleted = true;
        
        Player1.SendMessage("Trade completed successfully.");
        Player2.SendMessage("Trade completed successfully.");
        
        // Log trade for audit purposes
        LogTrade();
    }
}
```

### Anti-Fraud Measures
```csharp
public class TradeSecurity
{
    public static bool ValidateTrade(PlayerTrade trade)
    {
        // Check players are still online and in range
        if (!ArePlayersValid(trade.Player1, trade.Player2))
            return false;
            
        // Validate all items still exist and are owned
        if (!ValidateItems(trade.Player1, trade.Player1Items))
            return false;
            
        if (!ValidateItems(trade.Player2, trade.Player2Items))
            return false;
            
        // Check money amounts
        if (trade.Player1Money > trade.Player1.GetCurrentMoney())
            return false;
            
        if (trade.Player2Money > trade.Player2.GetCurrentMoney())
            return false;
            
        // Check inventory space
        if (!HasInventorySpace(trade.Player1, trade.Player2Items))
            return false;
            
        if (!HasInventorySpace(trade.Player2, trade.Player1Items))
            return false;
            
        return true;
    }
    
    public static bool CanTradeItem(IItem item)
    {
        // Cannot trade bound items
        if (item.IsBound)
            return false;
            
        // Cannot trade quest items
        if (item.IsQuestItem)
            return false;
            
        // Cannot trade equipped items (must unequip first)
        if (item.IsEquipped)
            return false;
            
        // Cannot trade damaged items below certain condition
        if (item.Condition < item.MaxCondition * 0.1)
            return false;
            
        return true;
    }
}
```

## Specialized Merchant Types

### Bounty Point Merchants
```csharp
public class BountyPointMerchant : GameMerchant
{
    public override bool CanBuyItem(GamePlayer player, IItem item)
    {
        var bpCost = GetBountyPointCost(item);
        
        if (player.BountyPoints < bpCost)
        {
            SayTo(player, $"You need {bpCost} bounty points to purchase that item.");
            return false;
        }
        
        return base.CanBuyItem(player, item);
    }
    
    public override bool ProcessPurchase(GamePlayer player, IItem item)
    {
        var bpCost = GetBountyPointCost(item);
        
        player.BountyPoints -= bpCost;
        player.Inventory.AddItem(item);
        
        SayTo(player, $"Thank you for your service! You've spent {bpCost} bounty points.");
        return true;
    }
}
```

### Consignment Merchants
```csharp
public class ConsignmentMerchant : GameMerchant
{
    public GamePlayer Owner { get; set; }
    public long TotalEarnings { get; set; }
    public Dictionary<IItem, long> ItemPrices { get; set; } = new();
    
    public void SetItemPrice(IItem item, long price)
    {
        if (item.Owner != Owner)
        {
            Owner.SendMessage("You can only price your own items.");
            return;
        }
        
        ItemPrices[item] = price;
        Owner.SendMessage($"Price set for {item.Name}: {price} gold");
    }
    
    public override bool ProcessPurchase(GamePlayer buyer, IItem item)
    {
        if (!ItemPrices.TryGetValue(item, out long price))
            return false;
            
        if (buyer.GetCurrentMoney() < price)
        {
            SayTo(buyer, "You don't have enough money.");
            return false;
        }
        
        // Calculate merchant fee
        long merchantFee = (long)(price * CONSIGNMENT_FEE_PERCENT);
        long ownerEarnings = price - merchantFee;
        
        // Transfer money and item
        buyer.RemoveMoney(price);
        buyer.Inventory.AddItem(item);
        
        TotalEarnings += ownerEarnings;
        
        // Notify owner if online
        if (Owner.IsOnline)
        {
            Owner.SendMessage($"Your {item.Name} sold for {price} gold (you earned {ownerEarnings}).");
        }
        
        return true;
    }
}
```

## Configuration

```csharp
[ServerProperty("merchant", "enable_dynamic_pricing", true)]
public static bool ENABLE_DYNAMIC_PRICING;

[ServerProperty("merchant", "price_modifier", 1.0)]
public static double MERCHANT_PRICE_MODIFIER;

[ServerProperty("merchant", "stock_refresh_interval", 3600)]
public static int STOCK_REFRESH_INTERVAL; // seconds

[ServerProperty("merchant", "consignment_fee_percent", 0.1)]
public static double CONSIGNMENT_FEE_PERCENT;

[ServerProperty("merchant", "max_trade_distance", 500)]
public static int MAX_TRADE_DISTANCE;
```

## TODO: Missing Documentation

- Advanced auction house mechanics
- Cross-realm trading restrictions and protocols
- Economic balance monitoring and adjustment systems
- Merchant AI for mobile traders and caravans
- Integration with crafting supply chains

## References

- `GameServer/gameobjects/GameMerchant.cs` - Merchant base implementation
- `GameServer/packets/Client/PlayerTradeHandler.cs` - Trade mechanics
- Various specialized merchant implementations
- Economic configuration systems 