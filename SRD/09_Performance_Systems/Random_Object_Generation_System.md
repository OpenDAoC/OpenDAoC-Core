# Random Object Generation (ROG) System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

The Random Object Generation (ROG) System provides sophisticated procedural content generation for OpenDAoC, creating randomized items, loot distributions, and unique equipment. It supports Atlas-style random generation, class-specific loot targeting, and configurable generation parameters for balanced gameplay.

## Core Architecture

### Generator Base System

```csharp
public interface ILootGenerator
{
    LootList GenerateLoot(GameNPC mob, GameObject killer);
    void RegisterGenerator(string mobName, string mobGuild, string mobFaction, int mobRegion);
}

public abstract class LootGeneratorBase : ILootGenerator
{
    public abstract LootList GenerateLoot(GameNPC mob, GameObject killer);
    public virtual int Priority { get; set; } = 0;
    public virtual bool CanGenerate(GameNPC mob, GameObject killer) { return true; }
    
    protected virtual eCharacterClass GetRandomClassFromGroup(Group group)
    {
        var validClasses = GetValidClassesForGroup(group);
        return validClasses[Util.Random(validClasses.Count - 1)];
    }
}
```

### Atlas ROG Manager

```csharp
public class AtlasROGManager
{
    private static readonly Dictionary<int, List<GeneratedUniqueItem>> _generatedItems = new();
    
    public static GeneratedUniqueItem CreateRandomItem(eRealm realm, eCharacterClass charClass, int level)
    {
        var item = new GeneratedUniqueItem(realm, charClass, level, level - Util.Random(-5, 10));
        RegisterGeneratedItem(item);
        return item;
    }
    
    public static GeneratedUniqueItem CreateUniqueItem(eRealm realm, eCharacterClass charClass, int level)
    {
        var item = new GeneratedUniqueItem(realm, charClass, level, level - Util.Random(15, 20));
        item.IsUnique = true;
        return item;
    }
}
```

## Item Generation System

### Generated Unique Item

```csharp
public class GeneratedUniqueItem
{
    public eRealm Realm { get; set; }
    public eCharacterClass CharacterClass { get; set; }
    public byte Level { get; set; }
    public byte Quality { get; set; }
    public bool IsUnique { get; set; }
    public eGenerateType GenerationType { get; set; }
    
    // Generation parameters
    public int BonusLevel { get; set; }
    public List<ItemProperty> Properties { get; set; } = new();
    public string GeneratedName { get; set; }
    
    public GeneratedUniqueItem(eRealm realm, eCharacterClass charClass, byte level)
    {
        Realm = realm;
        CharacterClass = charClass;
        Level = level;
        Quality = (byte)Util.Random(85, 101);
        GenerateProperties();
        GenerateName();
    }
    
    private void GenerateProperties()
    {
        var propertyCount = Util.Random(3, 7);
        for (int i = 0; i < propertyCount; i++)
        {
            var property = GenerateRandomProperty();
            if (!HasProperty(property.Type))
            {
                Properties.Add(property);
            }
        }
    }
}
```

### Generation Types

```csharp
public enum eGenerateType
{
    Armor,
    Weapon,
    Jewelry,
    Shield,
    Instrument,
    Poison,
    Other
}

public enum eArmorType
{
    Cloth,
    Leather,
    Studded,
    Chain,
    Plate
}

public enum eWeaponType
{
    OneHandSword,
    TwoHandSword,
    OneHandAxe,
    TwoHandAxe,
    OneHandHammer,
    TwoHandHammer,
    Spear,
    Polearm,
    Staff,
    Bow,
    Crossbow,
    Dagger
}
```

## Realm-Specific Generation

### Albion Generation

```csharp
public class AlbionItemGenerator
{
    public static string GetAlbionArmorType(eCharacterClass charClass, int level)
    {
        switch (charClass)
        {
            case eCharacterClass.Paladin:
            case eCharacterClass.Armsman:
            case eCharacterClass.Mercenary:
                return level >= 20 ? "Plate" : "Chain";
                
            case eCharacterClass.Scout:
            case eCharacterClass.Infiltrator:
                return level >= 10 ? "Studded" : "Leather";
                
            case eCharacterClass.Minstrel:
                return "Reinforced";
                
            case eCharacterClass.Cleric:
            case eCharacterClass.Friar:
                return level >= 15 ? "Chain" : "Studded";
                
            case eCharacterClass.Theurgist:
            case eCharacterClass.Wizard:
            case eCharacterClass.Sorcerer:
            case eCharacterClass.Necromancer:
            case eCharacterClass.Cabalist:
                return "Cloth";
                
            default:
                return "Leather";
        }
    }
    
    public static string GetAlbionWeapon(eCharacterClass charClass)
    {
        switch (charClass)
        {
            case eCharacterClass.Paladin:
                return Util.Random(2) == 0 ? "TwoHandSword" : "OneHandSword";
                
            case eCharacterClass.Armsman:
                return GetRandomArmsmanWeapon();
                
            case eCharacterClass.Mercenary:
                return GetRandomMercenaryWeapon();
                
            case eCharacterClass.Scout:
                return "Bow";
                
            case eCharacterClass.Infiltrator:
                return GetRandomInfiltratorWeapon();
                
            case eCharacterClass.Minstrel:
                return "Instrument";
                
            case eCharacterClass.Cleric:
                return "Staff";
                
            case eCharacterClass.Friar:
                return "Staff";
                
            default:
                return "Staff";
        }
    }
}
```

### Midgard Generation

```csharp
public class MidgardItemGenerator
{
    private static readonly string[] MidgardWeapons = 
    {
        "Sword", "Axe", "Hammer", "Spear", "Bow", "Crossbow", "Staff"
    };
    
    private static readonly string[] MidgardArmor =
    {
        "Cloth", "Leather", "Studded", "Chain"
    };
    
    public static string GetMidgardArmorType(eCharacterClass charClass, int level)
    {
        switch (charClass)
        {
            case eCharacterClass.Warrior:
            case eCharacterClass.Thane:
            case eCharacterClass.Berserker:
                return level >= 20 ? "Chain" : "Studded";
                
            case eCharacterClass.Hunter:
            case eCharacterClass.Shadowblade:
                return level >= 10 ? "Studded" : "Leather";
                
            case eCharacterClass.Skald:
            case eCharacterClass.Savage:
                return "Studded";
                
            case eCharacterClass.Healer:
            case eCharacterClass.Shaman:
                return level >= 15 ? "Chain" : "Studded";
                
            case eCharacterClass.Runemaster:
            case eCharacterClass.Spiritmaster:
            case eCharacterClass.Bonedancer:
                return "Cloth";
                
            default:
                return "Leather";
        }
    }
}
```

### Hibernia Generation

```csharp
public class HiberniaItemGenerator
{
    public static string GetHiberniaArmorType(eCharacterClass charClass, int level)
    {
        switch (charClass)
        {
            case eCharacterClass.Hero:
            case eCharacterClass.Champion:
            case eCharacterClass.Blademaster:
                return level >= 20 ? "Scale" : "Reinforced";
                
            case eCharacterClass.Ranger:
            case eCharacterClass.Nightshade:
                return level >= 10 ? "Reinforced" : "Leather";
                
            case eCharacterClass.Bard:
                return "Reinforced";
                
            case eCharacterClass.Druid:
            case eCharacterClass.Warden:
                return level >= 15 ? "Reinforced" : "Leather";
                
            case eCharacterClass.Eldritch:
            case eCharacterClass.Enchanter:
            case eCharacterClass.Mentalist:
            case eCharacterClass.Animist:
                return "Cloth";
                
            default:
                return "Leather";
        }
    }
}
```

## Loot Distribution System

### Class-Based Loot Targeting

```csharp
public class LootDistributionManager
{
    public static eCharacterClass GetRandomClassFromGroup(Group group)
    {
        var validClasses = new List<eCharacterClass>();
        
        foreach (var member in group.GetMembersInTheGroup())
        {
            if (member is GamePlayer player)
            {
                validClasses.Add((eCharacterClass)player.CharacterClass.ID);
            }
        }
        
        return validClasses.Count > 0 ? 
            validClasses[Util.Random(validClasses.Count - 1)] : 
            GetRandomClassFromRealm(group.Leader.Realm);
    }
    
    public static eCharacterClass GetRandomClassFromBattlegroup(BattleGroup battlegroup)
    {
        var validClasses = new List<eCharacterClass>();
        
        foreach (var group in battlegroup.Groups.Values)
        {
            foreach (var member in group.GetMembersInTheGroup())
            {
                if (member is GamePlayer player)
                {
                    validClasses.Add((eCharacterClass)player.CharacterClass.ID);
                }
            }
        }
        
        return validClasses.Count > 0 ? 
            validClasses[Util.Random(validClasses.Count - 1)] : 
            eCharacterClass.Unknown;
    }
    
    public static eCharacterClass GetRandomClassFromRealm(eRealm realm)
    {
        var classesForRealm = GetClassesForRealm(realm);
        return classesForRealm[Util.Random(classesForRealm.Count - 1)];
    }
}
```

## Specific Loot Generators

### Atlas Mob Loot Generator

```csharp
public class AtlasMobLootGenerator : LootGeneratorBase
{
    public override LootList GenerateLoot(GameNPC mob, GameObject killer)
    {
        var loot = new LootList();
        
        if (killer is GamePlayer player)
        {
            var classForLoot = DetermineTargetClass(player);
            var itemLevel = DetermineItemLevel(mob, player);
            
            if (ShouldGenerateAtlasItem())
            {
                var atlasItem = GenerateAtlasItem(player.Realm, classForLoot, itemLevel);
                loot.AddLoot(atlasItem);
            }
        }
        
        return loot;
    }
    
    private eCharacterClass DetermineTargetClass(GamePlayer player)
    {
        if (player.Group != null)
        {
            return GetRandomClassFromGroup(player.Group);
        }
        else if (player.TempProperties.getProperty<BattleGroup>(BattleGroup.BATTLEGROUP_PROPERTY) is BattleGroup bg)
        {
            return GetRandomClassFromBattlegroup(bg);
        }
        else
        {
            return (eCharacterClass)player.CharacterClass.ID;
        }
    }
    
    private int DetermineItemLevel(GameNPC mob, GamePlayer player)
    {
        var baseLevel = Math.Max(mob.Level, player.Level);
        var variance = Util.Random(-2, 3);
        return Math.Max(1, Math.Min(50, baseLevel + variance));
    }
}
```

### Random Loot Generator

```csharp
public class LootGeneratorRandom : LootGeneratorBase
{
    private readonly Dictionary<string, List<DbItemTemplate>> _itemsByType = new();
    
    public override LootList GenerateLoot(GameNPC mob, GameObject killer)
    {
        var loot = new LootList();
        var dropCount = CalculateDropCount(mob);
        
        for (int i = 0; i < dropCount; i++)
        {
            var item = GenerateRandomItem(mob.Level);
            if (item != null)
            {
                loot.AddLoot(item);
            }
        }
        
        return loot;
    }
    
    private int CalculateDropCount(GameNPC mob)
    {
        var baseChance = mob.Level / 10.0;
        var randomValue = Util.RandomDouble();
        
        if (randomValue < baseChance * 0.1) return 3;
        if (randomValue < baseChance * 0.3) return 2;
        if (randomValue < baseChance * 0.7) return 1;
        return 0;
    }
    
    private DbItemTemplate GenerateRandomItem(int mobLevel)
    {
        var itemTypes = new[] { "weapon", "armor", "jewelry", "misc" };
        var selectedType = itemTypes[Util.Random(itemTypes.Length)];
        
        var availableItems = _itemsByType.GetValueOrDefault(selectedType, new List<DbItemTemplate>());
        var levelAppropriate = availableItems
            .Where(item => Math.Abs(item.Level - mobLevel) <= 5)
            .ToList();
            
        return levelAppropriate.Count > 0 ? 
            levelAppropriate[Util.Random(levelAppropriate.Count)] : 
            null;
    }
}
```

## Special Currency Generators

### Aurulite Generator

```csharp
public class LootGeneratorAurulite : LootGeneratorBase
{
    public override LootList GenerateLoot(GameNPC mob, GameObject killer)
    {
        var loot = new LootList();
        
        if (ShouldDropAurulite(mob))
        {
            var amount = CalculateAuruliteAmount(mob);
            var aurulite = CreateAuruliteItem(amount);
            loot.AddLoot(aurulite);
        }
        
        return loot;
    }
    
    private bool ShouldDropAurulite(GameNPC mob)
    {
        var baseChance = ServerProperties.Properties.LOOTGENERATOR_AURULITE_BASE_CHANCE;
        var levelModifier = mob.Level / 50.0;
        var finalChance = baseChance * levelModifier;
        
        if (mob.Brain is INamedBrain)
        {
            finalChance *= ServerProperties.Properties.LOOTGENERATOR_AURULITE_NAMED_COUNT;
        }
        
        return Util.RandomDouble() < finalChance / 100.0;
    }
    
    private int CalculateAuruliteAmount(GameNPC mob)
    {
        var baseAmount = mob.Level / 10;
        var ratio = ServerProperties.Properties.LOOTGENERATOR_AURULITE_AMOUNT_RATIO;
        return (int)(baseAmount * ratio);
    }
}
```

### Dragon Scales Generator

```csharp
public class LootGeneratorDragonscales : LootGeneratorBase
{
    public override LootList GenerateLoot(GameNPC mob, GameObject killer)
    {
        var loot = new LootList();
        
        if (IsDragonType(mob) && ShouldDropScales(mob))
        {
            var scaleCount = CalculateScaleCount(mob);
            var scales = CreateDragonScales(scaleCount);
            loot.AddLoot(scales);
        }
        
        return loot;
    }
    
    private bool IsDragonType(GameNPC mob)
    {
        return mob.Name.ToLower().Contains("dragon") || 
               mob.RaceType == (int)eRace.Dragon ||
               mob.BodyType == (ushort)NpcTemplateMgr.eBodyType.Dragon;
    }
    
    private bool ShouldDropScales(GameNPC mob)
    {
        var baseChance = ServerProperties.Properties.LOOTGENERATOR_DRAGONSCALES_BASE_CHANCE;
        return Util.Random(100) < baseChance;
    }
}
```

## Quality and Property Generation

### Quality Calculation

```csharp
public static class QualityCalculator
{
    public static int CalculateBaseQuality(int mobLevel, int playerLevel)
    {
        var levelDifference = mobLevel - playerLevel;
        var baseQuality = 85;
        
        // Higher level mobs give better quality
        if (levelDifference > 0)
        {
            baseQuality += Math.Min(15, levelDifference * 2);
        }
        
        // Add random variance
        baseQuality += Util.Random(-5, 11);
        
        return Math.Max(85, Math.Min(100, baseQuality));
    }
    
    public static int CalculatePropertyCount(int quality, bool isUnique)
    {
        var baseCount = quality >= 95 ? 6 : (quality >= 90 ? 5 : 4);
        
        if (isUnique)
        {
            baseCount += Util.Random(1, 3);
        }
        
        return Math.Min(8, baseCount);
    }
}
```

### Property Generation

```csharp
public static class PropertyGenerator
{
    private static readonly Dictionary<eCharacterClass, List<eProperty>> ClassProperties = new()
    {
        [eCharacterClass.Warrior] = new List<eProperty>
        {
            eProperty.Strength, eProperty.Constitution, eProperty.Dexterity,
            eProperty.MeleeHaste, eProperty.MeleeDamage, eProperty.Resist_Body
        },
        [eCharacterClass.Theurgist] = new List<eProperty>
        {
            eProperty.Intelligence, eProperty.Dexterity, eProperty.Power,
            eProperty.CastingHaste, eProperty.SpellDamage, eProperty.Resist_Spirit
        }
        // ... more class mappings
    };
    
    public static List<ItemProperty> GenerateProperties(eCharacterClass charClass, int propertyCount, int level)
    {
        var properties = new List<ItemProperty>();
        var availableProperties = ClassProperties.GetValueOrDefault(charClass, GetGenericProperties());
        
        for (int i = 0; i < propertyCount; i++)
        {
            var property = availableProperties[Util.Random(availableProperties.Count)];
            var value = CalculatePropertyValue(property, level);
            
            if (!properties.Any(p => p.Type == property))
            {
                properties.Add(new ItemProperty { Type = property, Value = value });
            }
        }
        
        return properties;
    }
    
    private static int CalculatePropertyValue(eProperty property, int level)
    {
        var baseValue = GetBasePropertyValue(property);
        var levelModifier = level / 50.0;
        var randomVariance = Util.Random(80, 121) / 100.0;
        
        return (int)(baseValue * levelModifier * randomVariance);
    }
}
```

## Configuration

### ROG Server Properties

```ini
# Aurulite generation
LOOTGENERATOR_AURULITE_BASE_CHANCE = 5
LOOTGENERATOR_AURULITE_AMOUNT_RATIO = 1.5
LOOTGENERATOR_AURULITE_NAMED_COUNT = 3.0

# Atlantean Glass generation
LOOTGENERATOR_ATLANTEANGLASS_BASE_CHANCE = 3
LOOTGENERATOR_ATLANTEANGLASS_NAMED_COUNT = 2.0

# Dragon Scales generation
LOOTGENERATOR_DRAGONSCALES_BASE_CHANCE = 10
LOOTGENERATOR_DRAGONSCALES_NAMED_COUNT = 5

# Dreaded Seals generation
LOOTGENERATOR_DREADEDSEALS_STARTING_LEVEL = 35
LOOTGENERATOR_DREADEDSEALS_DROP_CHANCE_PER_LEVEL = 2
LOOTGENERATOR_DREADEDSEALS_BASE_CHANCE = 1
LOOTGENERATOR_DREADEDSEALS_NAMED_CHANCE = 3.0
```

### Generation Settings

```csharp
public static class ROGConfiguration
{
    public static bool ENABLE_ATLAS_GENERATION = true;
    public static int MAX_PROPERTIES_PER_ITEM = 8;
    public static int MIN_QUALITY = 85;
    public static int MAX_QUALITY = 100;
    public static bool ALLOW_CROSS_REALM_ITEMS = false;
    public static double UNIQUE_ITEM_CHANCE = 0.01; // 1%
    public static int MAX_ITEMS_PER_DROP = 3;
}
```

## System Integration

### Loot Manager Integration

```csharp
public static class LootMgr
{
    public static void RegisterLootGenerator(ILootGenerator generator, string mobName, string mobGuild, string mobFaction, int mobRegion)
    {
        var key = GenerateKey(mobName, mobGuild, mobFaction, mobRegion);
        _lootGenerators.Add(key, generator);
    }
    
    public static LootList GetLoot(GameNPC mob, GameObject killer)
    {
        var combinedLoot = new LootList();
        var applicableGenerators = GetLootGenerators(mob);
        
        foreach (var generator in applicableGenerators.OrderBy(g => g.Priority))
        {
            if (generator.CanGenerate(mob, killer))
            {
                var generatedLoot = generator.GenerateLoot(mob, killer);
                combinedLoot.Merge(generatedLoot);
            }
        }
        
        return combinedLoot;
    }
}
```

## Performance Considerations

### Caching Strategies

```csharp
public class ROGCache
{
    private static readonly LRUCache<string, List<DbItemTemplate>> _templateCache = new(1000);
    private static readonly ConcurrentDictionary<eCharacterClass, List<eProperty>> _propertyCache = new();
    
    public static List<DbItemTemplate> GetCachedTemplates(string itemType, int level)
    {
        var key = $"{itemType}_{level}";
        return _templateCache.GetOrAdd(key, () => LoadTemplatesFromDatabase(itemType, level));
    }
    
    public static void WarmCache()
    {
        // Pre-load commonly used templates
        var itemTypes = new[] { "weapon", "armor", "jewelry" };
        var levels = Enumerable.Range(1, 50).Where(l => l % 5 == 0);
        
        foreach (var type in itemTypes)
        {
            foreach (var level in levels)
            {
                GetCachedTemplates(type, level);
            }
        }
    }
}
```

### Generation Limits

```csharp
public class ROGLimiter
{
    private static readonly Dictionary<string, DateTime> _lastGeneration = new();
    private const int GENERATION_COOLDOWN_MS = 1000;
    
    public static bool CanGenerate(string mobName, string playerName)
    {
        var key = $"{mobName}_{playerName}";
        
        if (_lastGeneration.TryGetValue(key, out var lastTime))
        {
            if (DateTime.UtcNow - lastTime < TimeSpan.FromMilliseconds(GENERATION_COOLDOWN_MS))
            {
                return false;
            }
        }
        
        _lastGeneration[key] = DateTime.UtcNow;
        return true;
    }
}
```

## Implementation Status

**Completed**:
- ✅ Core generation framework
- ✅ Realm-specific item generation
- ✅ Class-based loot targeting
- ✅ Special currency generators
- ✅ Quality calculation system
- ✅ Property generation

**In Progress**:
- 🔄 Advanced caching mechanisms
- 🔄 Performance optimizations
- 🔄 Generation analytics

**Planned**:
- ⏳ Machine learning-based generation
- ⏳ Player preference tracking
- ⏳ Dynamic rarity adjustment

## References

- **Core Implementation**: `GameServer/Managers/RandomObjectGeneration/`
- **Atlas System**: `GameServer/gameutils/LootGenerator*.cs`
- **Item Templates**: Database-driven template system
- **Performance**: Caching and batching optimizations 