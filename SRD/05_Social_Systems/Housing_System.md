# Housing System

## Document Status
- Status: Under Development
- Implementation: Partial

## Overview

**Game Rule Summary**: Houses are private properties you can purchase to have your own space in the game world. They provide storage through personal vaults, decoration opportunities to show off trophies and furniture, and can host a merchant to sell items to other players. Houses require weekly rent payments to maintain ownership, and you can give other players limited access through a permission system. Guild houses allow entire guilds to share resources and meet in a private location.

The housing system allows players to purchase and customize personal or guild houses. Houses provide storage, display space, and social features including consignment merchants and crafting facilities.

## Core Mechanics

### House Types

**Game Rule Summary**: Houses come in four different sizes from cottage (smallest and cheapest) to mansion (largest and most expensive). Each realm has its own architectural style, so Albion houses look different from Midgard or Hibernia houses. Larger houses cost more rent but provide more space for decorations and storage. Your choice depends on your budget and how much space you need.

#### Models by Size
```
Cottage:  Model 1/5/9   (Small)
House:    Model 2/6/10  (Medium)  
Villa:    Model 3/7/11  (Large)
Mansion:  Model 4/8/12  (Extra Large)
```

#### Models by Realm
- **Albion**: Models 1-4
- **Midgard**: Models 5-8
- **Hibernia**: Models 9-12

### Rent System

**Game Rule Summary**: Houses require weekly rent payments to maintain ownership. You pay rent by putting money in a lockbox inside your house - the system automatically deducts rent every week. If you have a consignment merchant, it can also pay rent from its earnings. If you can't pay rent, your house gets repossessed, but your items are saved and can be reclaimed. You can prepay up to 4 weeks of rent in advance.

#### Rent Costs
```csharp
// Based on HouseMgr.GetRentByModel()
long rent = GetRentByModel(house.Model);
```

#### Payment Sources
1. **Lockbox**: Primary payment source
2. **Consignment Merchant**: Secondary source if lockbox insufficient
3. **Auto-deduction**: Every `RENT_DUE_DAYS` (default: 7 days)

#### Maximum Lockbox Storage
```
MaxLockbox = RentAmount * RENT_LOCKBOX_PAYMENTS
// Default: 4 weeks of rent
```

#### Repossession
- Occurs when combined lockbox + consignment funds < rent due
- House contents preserved for later reclaim

### Permission System

**Game Rule Summary**: You can give other players limited access to your house through a 9-level permission system. Each level can have different permissions like entering the house, decorating, using vaults, or paying rent. This lets you control exactly what friends, guild members, or visitors can do in your house. Guild houses use guild ranks to determine permissions automatically.

#### Permission Levels
- **Levels 1-9**: Configurable permissions per level
- **Owner**: Full permissions (level 0)
- **Guild**: Special permissions for guild houses

#### Permission Types

##### Basic Permissions
- **Enter House**: Access to interior
- **Interior Decoration**: Add/remove indoor items
- **Garden Decoration**: Add/remove outdoor items  
- **Pay Rent**: Add money to lockbox
- **Bind Inside**: Set as bind point
- **Use Tools**: Crafting stations access
- **Use Merchants**: Purchase from merchants

##### Vault Permissions (per vault)
```
VaultPermissions:
- None  = 0x00
- View  = 0x04
- Add   = 0x01
- Remove = 0x02
```

##### Consignment Permissions
```
ConsignmentPermissions:
- AddRemove = 0x03
- Withdraw  = 0x10
```

### Vault System

**Game Rule Summary**: Each house comes with 4 personal vaults plus 4 account vaults that work across all your characters. Each vault holds 100 items, giving you massive storage space compared to character inventory. You can set different permissions for each vault, so you might let friends add items to one vault but only let officers remove from another. Account vaults are especially useful for moving items between your characters.

#### Vault Configuration
- **House Vaults**: 4 per house
- **Account Vaults**: 4 additional (account-wide)
- **Vault Size**: 100 items per vault

#### Access Control
- Permission-based per vault
- House owner always has full access
- Account vaults accessible from any owned house

### Decoration System

**Game Rule Summary**: Houses can be customized with furniture and decorations both inside and outside. Interior items go on specific hookpoints (predefined spots) and can be rotated to face different directions. Garden decorations let you customize the outdoor area. You can display trophies from your adventures, functional items like crafting stations, and decorative furniture to make the house uniquely yours.

#### Interior Decorations
- **Hookpoints**: Predefined placement locations
- **Interior Items**: Furniture, trophies, etc.
- **Rotation**: Objects can be rotated to specific angles

#### Exterior Decorations
- **Garden Items**: Outdoor decorations
- **Porch System**: Optional porch addition
- **Guild Emblems**: Banners and shields

#### Hookpoint Types
```
ID Ranges:
< 0x20:  Red (Guards)
> 0x20:  Blue (Siege)
> 0x40:  Green/Yellow (Specialized)
0x41:    Ballista
0x61:    Trebuchet  
0x81:    Cauldron
```

### Consignment Merchant

**Game Rule Summary**: A consignment merchant is an NPC you can place in your house to sell items to other players automatically. Players can browse your merchant and buy items at the prices you set, even when you're offline. The merchant requires a porch to place, stores up to 100 items, and keeps the money from sales separate from your house funds. You can control who can add items or withdraw money through permissions.

#### Setup Requirements
- Requires porch to place merchant
- Uses "housing_consignment_deed" item
- One merchant per house maximum

#### Functionality
- **Item Storage**: 100 item slots (using slots 1350-1449)
- **Money Storage**: Separate from house funds
- **Price Setting**: Owner sets individual item prices
- **Permissions**: Control who can buy/withdraw

#### Money Management
```csharp
// Money stored separately from house
consignmentMerchant.TotalMoney
// Can be used for rent if lockbox empty
```

### Market Explorer Integration

**Game Rule Summary**: If the server enables the market system, your consignment merchant items can be searched by players throughout the region. This makes it much easier for buyers to find what they want and for you to reach more customers. Players can even buy items remotely and have them delivered for an extra fee, making your merchant accessible from anywhere.

- Searchable if `MARKET_ENABLED` = true
- Items indexed for region-wide searches
- Buy remotely with delivery fee

## System Interactions

### Guild Integration
- Guild houses use guild permissions
- Guild rank determines access level
- Guild funds can pay rent

### Crafting Integration
- Hookpoints for crafting stations
- Indoor crafting bonus in capital cities
- Tool access controlled by permissions

### Trade System
- Consignment merchant for automated sales
- Market explorer for searching items
- Direct player-to-player trade in houses

## Implementation Notes

### Database Tables
- `DBHouse`: Core house data
- `DBHousePermissions`: Permission levels
- `DBHouseCharsXPerms`: Player permissions
- `DBHouseHookpointItem`: Placed items
- `DBHouseIndoorItem`: Interior decorations
- `DBHouseOutdoorItem`: Exterior decorations
- `HouseConsignmentMerchant`: Merchant data

### Server Properties
```csharp
RENT_DUE_DAYS          // How often rent is due (default: 7)
RENT_CHECK_INTERVAL    // Check interval in minutes (default: 120)
RENT_LOCKBOX_PAYMENTS  // Max payments storable (default: 4)
CONSIGNMENT_USE_BP     // Use BPs instead of gold
MARKET_ENABLED         // Enable market system
```

## Test Scenarios

### Rent Payment Tests
- Sufficient lockbox funds
- Insufficient lockbox, sufficient consignment
- Total insufficient funds (repossession)
- Bounty point payment option

### Permission Tests
- Each permission level 1-9
- Guild rank permissions
- Vault access combinations
- Consignment merchant permissions

### Decoration Tests
- Interior item placement
- Garden decoration limits
- Porch addition/removal
- Hookpoint conflicts

## Change Log
- Initial documentation created
- Added detailed permission system
- Documented rent mechanics
- Added consignment merchant details

## References
- GameServer/housing/
- GameServer/packets/Client/168/HousingPlaceItemHandler.cs
- GameServer/packets/Client/168/HousePermissionsSetHandler.cs
- CoreDatabase/Tables/DbHouse*.cs 