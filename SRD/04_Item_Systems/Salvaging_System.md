# Salvaging System

**Document Status:** Complete Documentation  
**Completeness:** 90%  
**Verification:** Code-verified from Salvage.cs, SalvageCalculator.cs  
**Implementation Status:** Live

## Overview

The Salvaging System allows players to break down crafted items and equipment into raw materials. The system uses complex formulas to determine yield based on item properties, player skill, and salvage configuration.

## Core Mechanics

### Salvage Process

#### Basic Workflow
```csharp
public static int BeginWork(GamePlayer player, DbInventoryItem item)
{
    // 1. Validate salvage eligibility
    if (!IsAllowedToBeginWork(player, item))
        return 0;
        
    // 2. Find salvage yield configuration
    DbSalvageYield salvageYield = FindSalvageYield(item);
    
    // 3. Validate material template
    DbItemTemplate material = GetMaterialTemplate(salvageYield);
    
    // 4. Start salvage timer
    // 5. Process yield calculation
    // 6. Award materials
}
```

#### Salvage Requirements
- **Player State**: Must not be crafting or salvaging
- **Item State**: Item must be salvageable
- **Tool Requirements**: Appropriate salvage kit
- **Space Check**: Inventory space for materials
- **Skill Level**: Minimum skill to attempt

### Salvage Yield System

#### Database Structure
```csharp
[DataTable(TableName="SalvageYield")]
public class DbSalvageYield : DataObject
{
    public int ID { get; set; }                    // Primary key
    public int ObjectType { get; set; }            // Item object type (0 if unused)
    public int SalvageLevel { get; set; }          // Salvage tier (0 if unused)  
    public string MaterialId_nb { get; set; }      // Material template ID
    public int Count { get; set; }                 // Base yield (0 = calculated)
    public int Realm { get; set; }                 // Realm restriction (0 = any)
    public string PackageID { get; set; }          // Content package identifier
}
```

#### Yield Lookup Process
```csharp
// Primary lookup by item's SalvageYieldID
if (item.SalvageYieldID > 0)
{
    whereClause = DB.Column("ID").IsEqualTo(item.SalvageYieldID);
}
else
{
    // Fallback lookup by ObjectType and level
    int salvageLevel = CraftingMgr.GetItemCraftLevel(item) / 100;
    if (salvageLevel > 9) salvageLevel = 9; // Max level 9
    
    whereClause = DB.Column("ObjectType").IsEqualTo(item.Object_Type)
        .And(DB.Column("SalvageLevel").IsEqualTo(salvageLevel));
}

// Realm filtering
if (ServerProperties.Properties.USE_SALVAGE_PER_REALM)
{
    whereClause = whereClause.And(
        DB.Column("Realm").IsEqualTo((int)eRealm.None)
        .Or(DB.Column("Realm").IsEqualTo(item.Realm)));
}
```

### Material Yield Calculation

#### New Salvage System
```csharp
public static int GetCountForSalvage(DbInventoryItem item, DbItemTemplate rawMaterial)
{
    // Base calculation from item properties
    long maxCount = item.Price * 45 / rawMaterial.Price / 100; // 45% value return
    
    // Crafted item penalty
    if (item.IsCrafted)
        maxCount = (long)Math.Ceiling((double)maxCount / 2);
    
    // Apply condition/durability modifiers
    if (item.Condition < item.MaxCondition)
    {
        long usure = (maxCount * ((item.Condition / 5) / 1000)) / 100;
        maxCount = usure;
    }
    
    // Special handling for Atlas ROG items
    if (item.Description.Contains("Atlas ROG"))
        maxCount = 2;
    
    // Clamp values
    if (maxCount < 1) maxCount = 1;
    if (maxCount > 500) maxCount = 500;
    
    return (int)maxCount;
}
```

#### Legacy Salvage System
```csharp
// Old system using price-based calculation
int maxCount = (int)(item.Price * 0.45 / rawMaterial.Price);

if (item.IsCrafted)
    maxCount = (int)Math.Ceiling((double)maxCount / 2);
```

#### Skill-Based Yield Modifier
```csharp
public static int GetMaterialYield(GamePlayer player, DbInventoryItem item, 
    DbSalvageYield salvageYield, DbItemTemplate rawMaterial)
{
    int maxCount = GetCountForSalvage(item, rawMaterial);
    
    // Player skill calculation
    int playerPercent = player.GetCraftingSkillValue(
        CraftingMgr.GetSecondaryCraftingSkillToWorkOnItem(item)) * 100 
        / CraftingMgr.GetItemCraftLevel(item);
    
    // Clamp skill percentage
    if (playerPercent > 100) playerPercent = 100;
    else if (playerPercent < 75) playerPercent = 75;
    
    // Calculate minimum yield
    int minCount = (int)(((maxCount - 1) / 25f) * playerPercent) - ((3 * maxCount) - 4);
    // At 75% skill: min = 1
    // At 100% skill: min = maxCount
    
    if (minCount < 1) minCount = 1;
    if (minCount > maxCount) minCount = maxCount;
    
    return Util.Random(minCount, maxCount);
}
```

### Material Categories

#### Cloth Salvage Materials
```csharp
// Realm-specific cloth squares
string[] ClothSalvageList = new string[30];

// Albion (0-9)
ClothSalvageList[0] = "woolen_cloth_squares";
ClothSalvageList[1] = "linen_cloth_squares";
ClothSalvageList[2] = "brocade_cloth_squares";
ClothSalvageList[3] = "silk_cloth_squares";
ClothSalvageList[4] = "gossamer_cloth_squares";
ClothSalvageList[5] = "sylvan_cloth_squares";
ClothSalvageList[6] = "seamist_cloth_squares";
ClothSalvageList[7] = "nightshade_cloth_squares";
ClothSalvageList[8] = "wyvernskin_cloth_squares";
ClothSalvageList[9] = "silksteel_cloth_squares";

// Midgard (10-19) - Same materials
// Hibernia (20-29) - Same materials
```

#### Metal Salvage Materials
- **Iron**: Base metal material
- **Steel**: Improved metal
- **Alloy**: Advanced metal
- **Meteoric**: Rare metal
- **Arcanium**: Magical metal

#### Leather Salvage Materials
- **Rough Leather**: Basic material
- **Cured Leather**: Processed leather
- **Hard Leather**: Tough material
- **Rigid Leather**: Advanced material

### Siege Weapon Salvaging

#### Special Salvage Process
```csharp
public static int BeginWork(GamePlayer player, GameSiegeWeapon siegeWeapon)
{
    siegeWeapon.ReleaseControl();
    siegeWeapon.RemoveFromWorld();
    
    // Get crafting recipe
    var recipe = DOLDB<DbCraftedItem>.SelectObject(
        DB.Column("Id_nb").IsEqualTo(siegeWeapon.ItemId));
    
    // Get raw materials used
    var rawMaterials = DOLDB<DbCraftedXItem>.SelectObjects(
        DB.Column("CraftedItemId_nb").IsEqualTo(recipe.Id_nb));
    
    // Return all raw materials
    foreach (DbCraftedXItem material in rawMaterials)
    {
        DbItemTemplate template = GameServer.Database.FindObjectByKey<DbItemTemplate>(
            material.IngredientId_nb);
        
        DbInventoryItem item = GameInventoryItem.Create(template);
        item.Count = material.Count;
        player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, item);
    }
}
```

#### Siege Salvage Rules
- **100% Material Return**: All raw materials recovered
- **No Skill Check**: Always successful
- **Instant Process**: No timer required
- **Inventory Space**: Must have room for all materials

### Salvage Timing System

#### Timer Implementation
```csharp
protected static int Proceed(ECSGameTimer timer)
{
    GamePlayer player = timer.Properties.GetProperty<GamePlayer>(AbstractCraftingSkill.PLAYER_CRAFTER);
    DbInventoryItem itemToSalvage = timer.Properties.GetProperty<DbInventoryItem>(SALVAGED_ITEM);
    DbSalvageYield yield = timer.Properties.GetProperty<DbSalvageYield>(SALVAGE_YIELD);
    
    // Validate all properties exist
    if (player == null || itemToSalvage == null || yield == null)
        return 0;
    
    // Remove original item
    player.Inventory.RemoveItem(itemToSalvage);
    
    // Award materials
    DistributeMaterials(player, yield, materialCount);
}
```

#### Material Distribution
```csharp
// Intelligent stacking algorithm
Dictionary<int, int> changedSlots = new Dictionary<int, int>();

// First pass: fill existing stacks
foreach (DbInventoryItem item in player.Inventory.GetItemRange(
    eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
{
    if (item?.Id_nb == rawMaterial.Id_nb && item.Count < item.MaxCount)
    {
        int countFree = item.MaxCount - item.Count;
        if (count > countFree)
        {
            changedSlots.Add(item.SlotPosition, countFree);
            count -= countFree;
        }
        else
        {
            changedSlots.Add(item.SlotPosition, count);
            count = 0;
            break;
        }
    }
}

// Second pass: create new stacks
if (count > 0)
{
    eInventorySlot firstEmptySlot = player.Inventory.FindFirstEmptySlot(
        eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
    changedSlots.Add((int)firstEmptySlot, -count); // Negative = new item
}
```

### Batch Salvaging

#### Queue System
```csharp
public static int BeginWorkList(GamePlayer player, IList<DbInventoryItem> itemList)
{
    player.TempProperties.SetProperty(SALVAGE_QUEUE, itemList);
    player.CraftTimer?.Stop();
    player.Out.SendCloseTimerWindow();
    
    if (itemList == null || itemList.Count == 0) 
        return 0;
        
    return BeginWork(player, itemList[0]); // Start with first item
}
```

#### Queue Processing
- **Sequential Processing**: One item at a time
- **Queue Management**: Items processed in order
- **Interruption Handling**: Queue cleared on interruption
- **Completion**: Auto-advance to next item

## Validation System

### Salvage Eligibility
```csharp
protected static bool IsAllowedToBeginWork(GamePlayer player, DbInventoryItem item)
{
    // Player state checks
    if (player.IsCrafting || player.IsSalvagingOrRepairing)
    {
        player.Out.SendMessage("You must finish your current action first.");
        return false;
    }
    
    // Item validation
    if (item == null || item.SalvageYieldID == 0)
    {
        player.Out.SendMessage("This item cannot be salvaged.");
        return false;
    }
    
    // Inventory space check
    if (!HasInventorySpace(player))
    {
        player.Out.SendMessage("You don't have enough inventory space.");
        return false;
    }
    
    return true;
}
```

### Error Handling
```csharp
// Comprehensive error messages
"This item cannot be salvaged."                    // No salvage data
"Salvage recipe not found for this item."          // Missing database entry
"Material template not found."                     // Invalid material ID
"You don't have enough inventory space."           // Full inventory
"You must finish your current action first."       // Already busy
```

## System Integration

### Crafting System Integration
```csharp
// Uses crafting skill for yield calculation
int craftingSkill = player.GetCraftingSkillValue(
    CraftingMgr.GetSecondaryCraftingSkillToWorkOnItem(item));

// Skill determines success chance and yield
// Higher skill = better yield and success rate
```

### Inventory System Integration
```csharp
// Intelligent item distribution
// Automatic stacking where possible
// Inventory space validation
// Transaction logging for audit trail
```

### Logging Integration
```csharp
// All salvage operations logged
InventoryLogging.LogInventoryAction(player, "(salvage)", 
    eInventoryActionType.Craft, itemToSalvage.Template, itemToSalvage.Count);

InventoryLogging.LogInventoryAction("(salvage)", player, 
    eInventoryActionType.Craft, newItem.Template, newItem.Count);
```

## Configuration Options

### Server Properties
```csharp
USE_NEW_SALVAGE = true;                    // Enable new calculation system
USE_SALVAGE_PER_REALM = false;            // Realm-specific salvage tables
SALVAGE_SKILL_MINIMUM = 75;               // Minimum skill percentage
SALVAGE_VALUE_RETURN_PERCENT = 45;        // Base value return rate
SALVAGE_CRAFTED_PENALTY = 50;            // Crafted item penalty (%)
```

### Yield Modifiers
- **Base Return**: 45% of item value
- **Crafted Penalty**: 50% reduction
- **Skill Modifier**: 75-100% based on skill
- **Condition Impact**: Reduced yield for damaged items
- **Quality Impact**: Better quality = better yield

## GM Commands

### Salvage Management
```csharp
/crafting salvageinfo <ID>                 // Show salvage yield details
/crafting salvageadd <ID> <material> <count> // Add new salvage yield
/crafting salvageupdate <ID> <material> <count> // Update existing yield
```

### Testing Commands
```csharp
/item salvageinfo [slot]                   // Show item salvage info
/crafting adjustprices                     // Recalculate all prices
```

## Edge Cases & Special Handling

### Atlas ROG Items
```csharp
// Special handling for random generated items
if (item.Description.Contains("Atlas ROG"))
    maxCount = 2; // Fixed yield regardless of other factors
```

### Condition-Based Reduction
```csharp
// Damaged items yield less
if (item.Condition != item.MaxCondition && item.Condition < item.MaxCondition)
{
    long usureoverall = (maxCount * ((item.Condition / 5) / 1000)) / 100;
    maxCount = usureoverall;
}
```

### Merchant List Items
```csharp
// Special handling for merchant items
// Uses Bonus8 field for salvage yield override
if (item.Bonus8 > 0)
    if (item.Bonus8Type == 0 || item.Bonus8Type.ToString() == string.Empty)
        maxCount = item.Bonus8;
```

### Item Value Edge Cases
- **Zero Value Items**: Minimum 1 material
- **Extremely Valuable**: Capped at 500 materials
- **Negative Calculations**: Forced to minimum 1
- **Overflow Protection**: All calculations clamped

## Performance Optimizations

### Database Efficiency
```csharp
// Prepared query optimization
var whereClause = DB.Column("ID").IsEqualTo(item.SalvageYieldID);
salvageYield = DOLDB<DbSalvageYield>.SelectObject(whereClause);

// Minimal database calls
// Cached material templates
// Efficient item lookup
```

### Memory Management
```csharp
// Proper object disposal
// Minimal object creation
// Efficient collection handling
// Memory pool usage where applicable
```

## Test Scenarios

### Basic Salvage Operations
1. **Normal Items**: Standard salvage yield calculation
2. **Crafted Items**: Penalty applied correctly
3. **Damaged Items**: Condition affects yield
4. **Atlas ROG**: Fixed yield of 2

### Skill-Based Testing
1. **Low Skill (75%)**: Minimum yield
2. **High Skill (100%)**: Maximum yield
3. **Mid Skill (90%)**: Proportional yield
4. **Skill Progression**: Yield improves with skill

### Edge Case Testing
1. **Full Inventory**: Proper error handling
2. **Invalid Items**: Cannot salvage message
3. **Missing Data**: Graceful failure
4. **Siege Weapons**: 100% material return

### Batch Processing
1. **Queue Management**: Items processed in order
2. **Interruption**: Queue cleared properly
3. **Mixed Items**: Different yields calculated correctly
4. **Full Inventory**: Stops at appropriate point

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2025-01-20 | 1.0 | Initial comprehensive documentation |

## References
- `GameServer/craft/Salvage.cs` - Core salvage system
- `GameServer/craft/SalvageCalculator.cs` - Yield calculations
- `CoreDatabase/Tables/DbSalvage.cs` - Legacy salvage table
- `CoreDatabase/Tables/DbSalvageYield.cs` - New salvage system
- `GameServer/commands/gmcommands/crafting.cs` - Admin commands 