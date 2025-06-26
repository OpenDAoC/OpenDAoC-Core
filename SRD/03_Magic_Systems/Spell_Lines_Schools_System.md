# Spell Lines & Schools System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview
The spell lines and schools system organizes spells into logical groups based on specialization, class, and access method. It distinguishes between baseline spells (available to multiple classes) and specialization spells (class-specific), managing spell progression and access through different specializations.

## Core System Architecture

### SpellLine Database Structure
```csharp
[DataTable(TableName="SpellLine")]
public class DbSpellLine : DataObject
{
    public int SpellLineID { get; set; }     // Primary key
    public string KeyName { get; set; }      // Unique identifier
    public string Name { get; set; }         // Display name
    public string Spec { get; set; }         // Specialization reference
    public bool IsBaseLine { get; set; }     // Baseline vs specialization
    public int ClassIDHint { get; set; }     // Class-specific hint
}
```

### SpellLine Object
```csharp
public class SpellLine : NamedSkill
{
    public string Spec { get; }              // Specialization key
    public bool IsBaseLine { get; }          // Type indicator
    public int Level { get; set; }           // Current level for calculations
    public eSkillPage SkillType { get; }     // Returns eSkillPage.Spells
}
```

## Spell Line Categories

### Baseline vs Specialization Lines

#### Baseline Lines
- **Definition**: Spells available to multiple classes from trainers
- **Identification**: `IsBaseLine = true`
- **Access**: Direct training from NPCs
- **Examples**: Basic healing, armor factor buffs, damage shields
- **Progression**: Fixed spell availability by character level

#### Specialization Lines  
- **Definition**: Class-specific spells requiring specialization points
- **Identification**: `IsBaseLine = false`
- **Access**: Through specialization point investment
- **Examples**: Class-specific damage spells, unique abilities
- **Progression**: Spell availability based on specialization level

### Global Spell Lines

#### System-Defined Lines
```csharp
public static class GlobalSpellsLines
{
    public const string Reserved_Spells = "Reserved Spells";
    public const string Mob_Spells = "Mob Spells";
    public const string Item_Effects = "Item Effects";
    public const string Potions_Effects = "Potions Effects";
    public const string Combat_Styles_Effect = "Combat Styles Effect";
    public const string Realm_Spells = "Realm Spells";
    public const string Champion_Lines_StartWith = "ML";
}
```

#### Special Line Types
- **Item Effects**: Spell-like effects from items/artifacts
- **Potion Effects**: Alchemy-created temporary effects
- **Combat Styles**: Style-based magical effects
- **Realm Spells**: Cross-class realm abilities
- **Master Lines**: Champion level abilities (ML1-ML10)
- **Reserved**: Administrative/GM spells

## Spell Line Management

### Line Discovery System
```csharp
protected virtual List<SpellLine> GetSpellLinesForLiving(GameLiving living, int level)
{
    List<SpellLine> list = new List<SpellLine>();
    IList<Tuple<SpellLine, int>> spsl = SkillBase.GetSpecsSpellLines(KeyName);
    
    if (living is GamePlayer player)
    {
        // Select baseline + spec lines based on advanced class status
        var tmp = spsl.Where(item => 
            item.Item1.IsBaseLine || player.CharacterClass.HasAdvancedFromBaseClass())
            .OrderBy(item => item.Item1.IsBaseLine ? 0 : 1)
            .ThenBy(item => item.Item1.ID);
            
        // Apply class hints for targeting
        ProcessClassHints(tmp, player, list, level);
    }
    
    return list;
}
```

### Class Hint System
```csharp
// Baseline with class hint
var baseline = tmp.Where(item => 
    item.Item1.IsBaseLine && item.Item2 == player.CharacterClass.ID);
    
// Fallback to generic baseline
if (!baseline.Any())
{
    baseline = tmp.Where(item => 
        item.Item1.IsBaseLine && item.Item2 == 0);
}

// Specialization with class hint
var specline = tmp.Where(item => 
    !item.Item1.IsBaseLine && item.Item2 == player.CharacterClass.ID);
```

#### Class Hint Purpose
- **Targeting**: Allows different versions for different classes
- **Customization**: Class-specific spell variations
- **Inheritance**: Generic fallback when no class-specific version
- **Organization**: Logical grouping by intended class

## Specialization Integration

### Spell Line Assignment
```csharp
// Lines assigned to specializations via Spec property
public string Spec { get; } = "Destruction"; // Example specialization

// Multiple lines can share same specialization
// Baseline and spec lines often paired
```

### Level Calculation
```csharp
// Baseline: Uses character level
if (spellLine.IsBaseLine)
    spellLine.Level = player.Level;
    
// Specialization: Uses spec investment level
else
    spellLine.Level = specLevel;
```

### Access Control
- **Baseline Access**: Automatic with character level
- **Spec Access**: Requires specialization point investment
- **Advanced Classes**: Can access both baseline and spec lines
- **Base Classes**: Typically baseline only until advancement

## Hybrid Spell Systems

### Hybrid Specializations
```csharp
public class LiveSpellHybridSpecialization : Specialization
{
    public override bool HybridSpellList => true;
    
    // Combines baseline and spec spells in single list
    // Allows multiple spell versions (typically 2 best)
    // Maintains appearance order while showing best spells
}
```

#### Hybrid Mechanics
- **Spell Grouping**: Groups by spell type/target/properties
- **Version Selection**: Takes best 1-2 spells per group
- **Order Preservation**: Maintains original spell progression order
- **Multi-Version**: Some classes can see multiple spell ranks

### Multiple Spell Versions
```csharp
protected virtual bool AllowMultipleSpellVersions(SpellLine line, GamePlayer player)
{
    bool allow = false;
    switch (line.Spec)
    {
        case Specs.Augmentation:
            allow = true;
            break;
        // Other specializations that allow multiple versions
    }
    return allow;
}
```

## Spell-Line Relationship

### Line-Spell Binding
```csharp
[DataTable(TableName="LineXSpell")]
public class DbLineXSpell : DataObject
{
    public string LineName { get; set; }     // SpellLine KeyName
    public int SpellID { get; set; }         // Spell reference
    public int Level { get; set; }           // Required level for spell
}
```

### Dynamic Spell Lists
```csharp
protected virtual IDictionary<SpellLine, List<Skill>> GetLinesSpellsForLiving(
    GameLiving living, int level)
{
    IDictionary<SpellLine, List<Skill>> dict = new Dictionary<SpellLine, List<Skill>>();
    
    foreach (SpellLine sl in GetSpellLinesForLiving(living, level))
    {
        dict.Add(sl, SkillBase.GetSpellList(sl.KeyName)
                 .Where(item => item.Level <= sl.Level)
                 .OrderBy(item => item.Level)
                 .ThenBy(item => item.ID)
                 .Cast<Skill>().ToList());
    }
    
    return dict;
}
```

#### Spell Filtering
- **Level Requirements**: Spells filtered by line level
- **Availability**: Dynamic based on current progression
- **Ordering**: Consistent ordering by level then ID
- **Type Safety**: Proper casting for skill system integration

## Special Line Types

### Focus Caster Lines
```csharp
if (playerCaster != null && playerCaster.CharacterClass.IsFocusCaster)
{
    eProperty focusProp = SkillBase.SpecToFocus(SpellLine.Spec);
    // Special power cost reduction mechanics
}
```

### Instrument Lines (Songs)
```csharp
if (Spell.InstrumentRequirement != 0)
{
    // Song mechanics with instrument requirements
    // Duration bonuses based on instrument level/quality
}
```

### Champion Lines
```csharp
if (keyName.StartsWith(GlobalSpellsLines.Champion_Lines_StartWith))
    return 50; // Champion level spells always max level
```

#### Champion Line Mechanics
- **ML Prefix**: All start with "ML" (Master Line)
- **Level Override**: Always treated as level 50
- **Cross-Class**: Available to all classes meeting requirements
- **Progression**: Unlocked through master level advancement

### Combat Style Lines
```csharp
if (keyName == GlobalSpellsLines.Combat_Styles_Effect)
{
    // Style-based spell effects
    // Dynamic level based on relevant weapon spec
}
```

## Line Organization Patterns

### Class-Based Organization
- **Primary Specs**: Core class specializations
- **Secondary Specs**: Support specializations
- **Baseline**: Cross-class utility spells
- **Hybrid**: Combined baseline/spec presentation

### Specialization Grouping
```csharp
// Example specialization to spell line mapping
"Destruction Magic" -> ["Destruction Magic Baseline", "Destruction Magic Spec"]
"Healing" -> ["Healing Baseline", "Healing Specialization"]
"Enhancement" -> ["Enhancement Magic"]
```

### Realm-Specific Lines
- **Albion**: Cleric, Wizard, Sorcerer lines
- **Midgard**: Runemaster, Healer, Spiritmaster lines  
- **Hibernia**: Druid, Eldritch, Mentalist lines
- **Cross-Realm**: Baseline spells available to all

## Implementation Details

### Line Loading Process
```csharp
public static void LoadSpellLines()
{
    // 1. Load all DbSpellLine records from database
    // 2. Create SpellLine objects with proper properties
    // 3. Index by KeyName for fast lookup
    // 4. Load LineXSpell relationships
    // 5. Build spell lists per line
    // 6. Cache for runtime access
}
```

### Runtime Management
- **Caching**: Spell lines cached after database load
- **Cloning**: Lines cloned per-character for level tracking
- **Updates**: Dynamic level updates based on progression
- **Validation**: Ensures spell access matches character state

### Performance Considerations
- **Lazy Loading**: Spells loaded only when needed
- **Index Optimization**: Fast lookups by KeyName
- **Memory Management**: Shared objects where possible
- **Query Efficiency**: Minimized database access

## Access Patterns

### Trainer Integration
```csharp
// Trainers provide access to baseline spells
public List<SpellLine> GetTrainableLines(GamePlayer player)
{
    return GetSpellLinesForLiving(player)
        .Where(line => line.IsBaseLine)
        .ToList();
}
```

### Specialization Integration  
```csharp
// Specialization provides access to advanced spells
public List<SpellLine> GetSpecializationLines(GamePlayer player, string specKey)
{
    return GetSpellLinesForLiving(player)
        .Where(line => line.Spec == specKey && !line.IsBaseLine)
        .ToList();
}
```

## Test Scenarios

### Line Discovery
1. Baseline line access by level
2. Specialization line access by spec points
3. Class hint resolution
4. Advanced class line access

### Spell Access
1. Level-appropriate spell filtering
2. Hybrid list generation
3. Multiple version selection
4. Cross-line spell organization

### Special Cases
1. Champion line level override
2. Combat style line dynamics
3. Focus caster line mechanics
4. Instrument requirement validation

## Cross-System Interactions

### With Character System
- Character level affects baseline access
- Specialization points control spec line access
- Class advancement enables new lines

### With Casting System
- Line properties affect casting mechanics
- Focus lines modify power costs
- Special lines have unique behaviors

### With Training System  
- Trainers provide baseline spell access
- Specialization trainers manage spec spells
- Progressive unlocking based on investment

### With Effect System
- Line classification affects effect stacking
- Special lines have unique effect rules
- Component system uses line information 