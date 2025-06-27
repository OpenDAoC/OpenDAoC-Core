# Loot System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Complete
- **Verification**: Code-verified from LootMgr.cs, LootList.cs, AbstractServerRules.cs
- **Implementation**: Complete

## Overview

**Game Rule Summary**: The loot system determines what items and money drop when you kill monsters. Different creatures have different loot tables that define what they can drop and how often. When you kill something, the game checks various loot generators to see what drops - some items are guaranteed, others have a chance. The person or group that did the most damage gets first pick at the loot, and items on the ground have ownership timers before anyone can take them. Higher level monsters generally drop better loot and more money, and there are special loot systems for unique items, currencies like dragon scales, and group-based random item generation.

The loot system manages item and currency drops from NPCs, including generator registration, drop calculations, ownership rules, and distribution mechanics. It supports multiple generator types including template-based, random, money, and special generators.

## Core Architecture

### Loot Manager
```csharp
public sealed class LootMgr
{
    // Generator caches
    static readonly HybridDictionary m_ClassGenerators;
    static readonly IList m_globalGenerators;
    static readonly HybridDictionary m_mobNameGenerators;
    static readonly HybridDictionary m_mobGuildGenerators;
    static readonly HybridDictionary m_mobRegionGenerators;
    static readonly HybridDictionary m_mobFactionGenerators;
}
```

### Loot List
```csharp
public class LootList
{
    public int DropCount { get; set; }  // Random items to drop
    private readonly List<LootEntry> m_randomItemDrops;
    private readonly List<DbItemTemplate> m_fixedItemDrops;
}
```

### Generator Interface
```csharp
public interface ILootGenerator
{
    int ExclusivePriority { get; set; }
    LootList GenerateLoot(GameNPC mob, GameObject killer);
    void Refresh(GameNPC mob);
}
```

## Loot Generator Types

### 1. Money Generator
```csharp
public class LootGeneratorMoney : LootGeneratorBase
{
    // Formula: 2 + ((level^3) >> 3)
    int minLoot = 2 + ((lvl * lvl * lvl) >> 3);
    long moneyCount = minLoot + Util.Random(minLoot >> 1);
    moneyCount = (long)(moneyCount * MONEY_DROP);
}
```

### 2. Template Generator
Uses database tables:
- **LootTemplate**: Links template names to items with drop chances
- **MobXLootTemplate**: Links mobs to loot templates with conditions

```csharp
// Database structure
DbLootTemplate:
    TemplateName    // e.g., "dragon_loot"
    ItemTemplateID  // Item to drop
    Chance          // 0-100%
    Count           // Stack size

DbMobXLootTemplate:
    MobName         // Specific mob
    LootTemplateName // Template to use
    DropCount       // Items to select
```

### 3. Random Generator
Generates random items based on level ranges:
```csharp
// Items grouped by level ranges: 1-5, 6-10, 11-15, etc.
protected static DbItemTemplate[][] m_itemTemplatesAlb;
protected static DbItemTemplate[][] m_itemTemplatesMid;
protected static DbItemTemplate[][] m_itemTemplatesHib;
```

### 4. Chest Generator
Adds money chests as additional drops:
```csharp
// Small chest: SMALLCHEST_MULTIPLIER * (level^2)
// Large chest: LARGECHEST_MULTIPLIER * (level^2)
```

### 5. Special Generators
- **LootGeneratorDragonscales**: Dragon scale drops
- **LootGeneratorDreadedSeals**: PvP seal drops
- **LootGeneratorAtlanteanGlass**: Atlantis currency
- **AtlasMobLoot**: ROG (Random Object Generation) system

## Drop Mechanics

### Generator Priority System
```csharp
// Registration order (checked in sequence):
1. Mob-specific generators (by name)
2. Guild-based generators
3. Faction-based generators  
4. Region-based generators
5. Global generators

// Exclusive priority:
// Higher priority generators can prevent lower ones
```

### Drop Calculation Process
```csharp
public static DbItemTemplate[] GetLoot(GameNPC mob, GameObject killer)
{
    LootList lootList = null;
    IList generators = GetLootGenerators(mob);
    
    foreach (ILootGenerator generator in generators)
    {
        if (lootList == null)
            lootList = generator.GenerateLoot(mob, killer);
        else
            lootList.AddAll(generator.GenerateLoot(mob, killer));
    }
    
    return lootList?.GetLoot() ?? new DbItemTemplate[0];
}
```

### Fixed vs Random Drops
```csharp
// Fixed drops (100% chance)
lootList.AddFixed(itemTemplate, count);

// Random drops (chance-based)
lootList.AddRandom(chance, itemTemplate, count);

// Selection from random pool
foreach (LootEntry entry in m_randomItemDrops)
{
    if (Util.Chance(entry.Chance))
        selectedItems.Add(entry.ItemTemplate);
}
```

## Ownership System

### Damage-Based Ownership
```csharp
public class ItemOwnerTotalDamagePair : IComparable<ItemOwnerTotalDamagePair>
{
    public IGameStaticItemOwner Owner { get; }
    public long TotalDamage { get; }
    
    // Sorted by damage dealt (descending)
    public int CompareTo(ItemOwnerTotalDamagePair other)
    {
        return -TotalDamage.CompareTo(other.TotalDamage);
    }
}
```

### Ownership Rules
1. **Initial Ownership**: All damage dealers get ownership
2. **Pick-up Priority**: Highest damage dealer first
3. **Group/BG Handling**: Group leader represents group
4. **Ownership Duration**: Expires after set time

## Item Creation and Distribution

### Money Creation
```csharp
static void CreateMoney(GameNPC killedNpc, DbItemTemplate itemTemplate, 
    SortedSet<ItemOwnerTotalDamagePair> itemOwners, List<GamePlayer> playersInRadius)
{
    GameMoney money = new(itemTemplate.Price, killedNpc)
    {
        Name = itemTemplate.Name,
        Model = (ushort)itemTemplate.Model
    };

    NotifyNearbyPlayers(killedNpc, money, playersInRadius);
    money.AddToWorld();
    
    // Attempt auto pick up by each owner
    foreach (ItemOwnerTotalDamagePair itemOwner in itemOwners)
    {
        money.AddOwner(itemOwner.Owner);
        if (money.TryAutoPickUp(itemOwner.Owner))
            return;
    }
}
```

### Item Creation
```csharp
static void CreateItem(GameNPC killedNpc, DbItemTemplate itemTemplate,
    SortedSet<ItemOwnerTotalDamagePair> itemOwners, List<GamePlayer> nearbyPlayers)
{
    GameInventoryItem inventoryItem;
    
    // Handle unique items
    if (itemTemplate is DbItemUnique itemUnique)
    {
        inventoryItem = GameInventoryItem.Create(itemUnique);
        if (itemUnique is GeneratedUniqueItem)
            inventoryItem.IsROG = true;
    }
    else
        inventoryItem = GameInventoryItem.Create(itemTemplate);
    
    // Convert to stack if applicable
    if (inventoryItem.PackSize > 1 && inventoryItem.MaxCount >= inventoryItem.PackSize)
        inventoryItem.Count = inventoryItem.PackSize;
    
    // Create world item
    WorldInventoryItem item = new(inventoryItem)
    {
        X = killedNpc.X,
        Y = killedNpc.Y,
        Z = killedNpc.Z,
        CurrentRegion = killedNpc.CurrentRegion
    };
    
    // Add owners and attempt pickup
}
```

## ROG (Random Object Generation) Integration

### Atlas Loot System
```csharp
// Group-aware loot generation
if (player.Group != null)
{
    var MaxDropCap = Math.Round((decimal)(player.Group.MemberCount) / 3);
    if (MaxDropCap < 1) MaxDropCap = 1;
    if (MaxDropCap > 3) MaxDropCap = 3;
    
    // Higher level mobs increase cap
    if (mob.Level > 65) MaxDropCap++;
    
    // Generate items for random group member classes
    foreach (var groupPlayer in player.Group.GetPlayersInTheGroup())
    {
        if (Util.Chance(chance) && numDrops < MaxDropCap)
        {
            classForLoot = GetRandomClassFromGroup(player.Group);
            var item = GenerateItemTemplate(player, classForLoot, ...);
            loot.AddFixed(item, 1);
            numDrops++;
        }
    }
}
```

### ROG Quality System
```csharp
public void GenerateItemQuality(int killedcon)
{
    // Base quality on con color
    switch (killedcon)
    {
        case -3: // Grey
            Quality = 89 + Util.Random(10);
            break;
        case 2: // Orange
            Quality = 95 + Util.Random(5);
            break;
        case 3: // Red
            Quality = 96 + Util.Random(4);
            break;
    }
}
```

## Special Drop Types

### Timed Drops
```csharp
// Negative chance indicates timed drop
if (lootTemplate.Chance < 0)
{
    // Check cooldown timer
    string timerKey = $"Loot_{lootTemplate.ItemTemplateID}";
    long lastDrop = player.TempProperties.GetProperty<long>(timerKey);
    
    if (lastDrop + cooldown < GameLoop.GameLoopTime)
    {
        // Can drop
        player.TempProperties.SetProperty(timerKey, GameLoop.GameLoopTime);
    }
}
```

### Realm-Specific Items
```csharp
// Check realm restrictions
if (drop.Realm == (int)player.Realm || drop.Realm == 0 || player.CanUseCrossRealmItems)
{
    loot.AddRandom(lootTemplate.Chance, drop, 1);
}
```

### Money Variations
```csharp
// Regular coins
"bag of coins" - Standard money drop

// Chest variations  
"small chest" - SMALLCHEST_MULTIPLIER * level²
"large chest" - LARGECHEST_MULTIPLIER * level²

// Special currency
"dragonscales" - Dragon raid currency
"atlanteanglass" - Atlantis currency
"dreaded_seal" - PvP currency
```

## Configuration

### Server Properties
```properties
MONEY_DROP              # Global money multiplier
SMALLCHEST_CHANCE       # Base % for small chests
LARGECHEST_CHANCE       # Base % for large chests  
SMALLCHEST_MULTIPLIER   # Small chest value multiplier
LARGECHEST_MULTIPLIER   # Large chest value multiplier
LOOT_RADIUS            # Auto-loot pickup radius
```

### Generator Registration
```csharp
public static void RegisterLootGenerator(ILootGenerator generator, 
    string mobname, string mobguild, string mobfaction, int mobregion)
{
    // Parse and register by each criteria
    if (!string.IsNullOrEmpty(mobname))
    {
        string[] mobs = mobname.Split(';');
        foreach (string mob in mobs)
        {
            IList mobList = (IList)m_mobNameGenerators[mob];
            if (mobList == null)
            {
                mobList = new ArrayList();
                m_mobNameGenerators[mob] = mobList;
            }
            mobList.Add(generator);
        }
    }
    
    // Similar for guild, faction, region...
    
    // Global if no specific criteria
    if (all_null)
        m_globalGenerators.Add(generator);
}
```

## Implementation Notes

### Thread Safety
- Generator lists use thread-safe collections
- Item creation synchronized per drop
- Ownership updates atomic

### Performance
- Generators cached by class type
- Template lookup optimized with dictionaries
- Batch notifications for nearby players

### Database Schema
```sql
-- Loot Templates
CREATE TABLE LootTemplate (
    TemplateName VARCHAR(255),
    ItemTemplateID VARCHAR(255),
    Chance INT,
    Count INT
);

-- Mob to Template Mapping
CREATE TABLE MobXLootTemplate (
    MobName VARCHAR(255),
    LootTemplateName VARCHAR(255),
    DropCount INT,
    MinLevel INT,
    MaxLevel INT
);

-- Generator Registration  
CREATE TABLE LootGenerator (
    LootGeneratorClass VARCHAR(255),
    MobName VARCHAR(255),
    MobGuild VARCHAR(255),
    MobFaction VARCHAR(255),
    RegionID INT,
    ExclusivePriority INT
);
```

## Test Scenarios

### Basic Drop Test
```
1. Kill mob with registered loot
2. Verify generators execute in order
3. Check fixed drops always appear
4. Verify random drops respect chances
```

### Group Loot Test
```
1. Group kills mob
2. Verify ownership by damage
3. Test auto-pickup attempts
4. Check loot messages to all
```

### Template Test
```
1. Create mob with template
2. Kill and verify drops
3. Test drop count limits
4. Verify realm restrictions
```

## Edge Cases

### No Killer
- Loot still generates
- No ownership assigned
- Items expire normally

### Multiple Generators
- All execute unless exclusive
- Results combined into single list
- Drop count uses maximum

### Invalid Templates
- Logged but don't crash
- Generator continues with valid items
- Empty loot list possible

### Full Inventory
- Auto-pickup fails gracefully
- Item remains on ground
- Standard pickup rules apply

## Change Log

### 2025-01-20
- Initial documentation created
- Complete generator system documented
- Added ROG integration details
- Included ownership mechanics

## References
- LootMgr.cs: Core loot manager
- LootList.cs: Drop list implementation
- LootGeneratorBase.cs: Base generator class
- AbstractServerRules.cs: Drop creation logic 