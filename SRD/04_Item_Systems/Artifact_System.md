# Artifact System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview

**Game Rule Summary**: Artifacts are some of the most powerful and unique items in the game, but they require significant effort to obtain. You must participate in specific encounters, complete associated quests, and collect special materials to activate them. Each artifact provides unique abilities or creates powerful weapons that can't be obtained any other way. Once you have an artifact, it's permanently bound to your character and can't be traded, dropped, or lost.

The artifact system provides powerful, unique items that players can obtain through specialized encounters and quests. Artifacts have unique bonuses, abilities, and often special activation requirements including encounter participation and credit systems.

## Core Mechanics

### Artifact Structure

**Game Rule Summary**: Artifacts are stored in a special database system that tracks which encounters you've completed, what materials you've collected, and which artifacts you're eligible to obtain. Each artifact has specific requirements including participating in certain encounters, completing quests, and gathering materials like scrolls and books from the scholar NPCs.

#### Database Schema
```csharp
[DataTable(TableName = "Artifact")]
public class DbArtifact : DataObject
{
    public string ArtifactID { get; set; }     // Unique identifier
    public string EncounterID { get; set; }   // Required encounter
    public string QuestID { get; set; }       // Unlock quest
    public string Zone { get; set; }          // Zone location
    public string ScholarID { get; set; }     // Scholar NPC
    public int ReuseTimer { get; set; }       // Reuse timer
    public int XpRate { get; set; }           // Experience rate
    public string BookID { get; set; }        // Book template
    public int BookModel { get; set; }        // Book model
    public string Credit { get; set; }        // Credit requirements
    // ... scroll and message properties
}
```

#### Artifact Bonuses
```csharp
[DataTable(TableName = "ArtifactBonus")]
public class DbArtifactBonus : DataObject
{
    public string ArtifactID { get; set; }    // Links to artifact
    public int BonusID { get; set; }          // Bonus slot (0-13)
    public int Level { get; set; }           // Required level
}
```

**Bonus ID Structure**:
```csharp
public enum ID
{
    // Stat bonuses (0-9)
    Bonus1 = 0, Bonus2 = 1, Bonus3 = 2, Bonus4 = 3, Bonus5 = 4,
    Bonus6 = 5, Bonus7 = 6, Bonus8 = 7, Bonus9 = 8, Bonus10 = 9,
    
    // Spell bonuses (10-13)
    Spell = 10, Spell1 = 11, ProcSpell = 12, ProcSpell1 = 13
}
```

### Artifact Acquisition

**Game Rule Summary**: Getting an artifact requires several steps that must be completed in order. First, you must participate in specific encounters to earn credit. Then you need to complete the associated quest line. Finally, you must collect the required scrolls and books, which you combine and turn in to scholar NPCs. This ensures that artifacts remain rare and are only obtained by players who put in significant effort.

#### Encounter System
**Prerequisites**:
1. **Encounter Participation**: Must complete specific encounters
2. **Credit System**: Obtain encounter credit through participation
3. **Quest Completion**: Complete associated artifact quest
4. **Scholar Interaction**: Turn in materials to scholar NPC

#### Credit Requirements
```csharp
public string Credit { get; set; } // Credit requirements for artifact
```
- **Encounter Credit**: Participation in designated encounters
- **Kill Credit**: Specific mob kills or boss defeats
- **Group Credit**: Shared credit for group encounters
- **Time Limits**: Credit may expire after certain periods

#### Material System

**Game Rule Summary**: Artifact materials come in the form of books and scrolls that drop from encounters or are given as quest rewards. Some scrolls can be combined together to form more powerful scrolls. You need to collect all the required materials before you can activate the artifact with the scholar NPC.

**Book and Scroll Components**:
```csharp
public string BookID { get; set; }        // Book item template
public string Scroll1 { get; set; }      // First scroll
public string Scroll2 { get; set; }      // Second scroll  
public string Scroll3 { get; set; }      // Third scroll
public string Scroll12 { get; set; }     // Combined scroll 1+2
public string Scroll13 { get; set; }     // Combined scroll 1+3
public string Scroll23 { get; set; }     // Combined scroll 2+3
```

### Artifact Properties

**Game Rule Summary**: Artifacts have special properties that make them different from normal items. They can't be picked up by other players, dropped, traded, or sold. Each character can only have one of each artifact, and they're permanently bound to you once obtained. This makes artifacts truly personal achievements that showcase your accomplishments.

#### Special Item Properties
```csharp
// Artifact items have unique restrictions
IsPickable = false;     // Cannot be picked up by others
IsDropable = false;     // Cannot be dropped
CanDropAsLoot = false; // Cannot drop as loot
IsTradable = false;    // Cannot be traded
MaxCount = 1;          // Only one per character
```

#### Bonus System Integration
Artifacts use standard item bonus system with extensions:
```csharp
public int GetBonusAmount(DbArtifactBonus.ID bonusID)
{
    switch ((int)bonusID)
    {
        case 0: return Bonus1;
        case 1: return Bonus2;
        // ... through Bonus10
        case 10: return SpellID;
        case 11: return SpellID1;
        case 12: return ProcSpellID;
        case 13: return ProcSpellID1;
    }
}
```

### Artifact Abilities

**Game Rule Summary**: Artifacts don't just provide stat bonuses like normal items - they often have unique abilities that create weapons, cast spells, or provide effects that aren't available anywhere else in the game. Some artifacts create different items based on your class, while others provide the same effect to everyone. These abilities often have cooldowns to prevent overuse.

#### Spell Integration
Artifacts can have multiple spell effects:
- **SpellID**: Primary spell effect
- **SpellID1**: Secondary spell effect
- **ProcSpellID**: Proc on hit effect
- **ProcSpellID1**: Secondary proc effect

#### Unique Spell Effects
Many artifacts have custom spell handlers:
```csharp
// Example: Belt of Sun artifact abilities
[SpellHandler(eSpellType.BeltOfSun)]
public class BeltOfSun : SpellHandler
{
    // Creates realm-specific weapon based on class
    // Weapons have enhanced stats and proc effects
}
```

#### Dynamic Item Creation
Artifacts often create items dynamically:
```csharp
// Example: Belt of Sun creates different weapons
private DbItemTemplate GetWeaponForClass(string weaponType)
{
    // Creates weapon template with:
    // - Level 50 base stats
    // - 150 DPS, appropriate speed
    // - Artifact-level bonuses
    // - Special proc effects
    // - Realm-specific models
}
```

### Example Artifacts

**Game Rule Summary**: Each artifact has its own unique theme and abilities. The Belt of Sun creates powerful melee weapons appropriate to your class with fire-based effects. The Belt of Moon creates magical weapons with spell enhancements. Aten's Shield creates the Golden Trident of Flame, a powerful weapon with fire-based procs. Each artifact fills a different role and appeals to different character builds.

#### Belt of Sun
**Effect**: Creates powerful weapons based on character class
**Bonuses**: 
- Stats: +6 to combat stats
- +27 to primary stat
- +2% resist bonuses
- Weapon proc effect

#### Belt of Moon  
**Effect**: Creates magical weapons with spell effects
**Bonuses**:
- Spell effects and mana enhancements
- Combat bonuses
- Special abilities

#### Aten's Shield
**Effect**: Creates Golden Trident of Flame
**Properties**:
- Realm-specific variants (Albion/Midgard/Hibernia)
- Level 45 requirement
- Unique proc spell effects
- High durability (50,000)

### Reuse and Cooldowns

**Game Rule Summary**: Most artifacts have cooldown timers to prevent spam usage and maintain game balance. You can't use the same artifact ability repeatedly - you must wait for the cooldown to expire. Some artifacts are also limited by encounter availability, meaning you can only obtain them when specific encounters are available or active.

#### Reuse Timer System
```csharp
public int ReuseTimer { get; set; } // Cooldown in seconds
```
- **Prevents Spam**: Cooldowns prevent repeated use
- **Encounter Limits**: Some artifacts limited by encounter availability
- **Server Config**: Reuse timers can be server-configured

#### Credit Expiration
- **Credit Timeout**: Encounter credit may expire
- **Seasonal Events**: Some artifacts tied to scheduled events
- **Group Requirements**: Credit may require group participation

### Guild Integration

**Game Rule Summary**: Guilds can purchase buffs using merit points that increase artifact experience gain for all guild members. This makes artifact acquisition easier for organized guilds and provides another benefit of guild membership. These buffs are temporary and must be renewed periodically.

#### Guild Buffs
Artifacts affected by guild bonuses:
```csharp
// Guild buff types include artifact XP
public enum eBonusType
{
    Experience,
    RealmPoints,
    BountyPoints,
    CraftingHaste,
    ArtifactXP     // Enhances artifact-related experience
}
```

#### Merit Point Costs
Guild artifact XP buffs cost merit points:
- **Standard Cost**: 1000 merit points
- **Duration**: Server-configurable
- **Effect**: Increases artifact experience gain

## System Interactions

### With Encounter System
- **Encounter Credit**: Required for artifact access
- **Group Content**: Many artifacts require group encounters
- **Scheduled Content**: Some artifacts from timed encounters

### With Quest System
- **Prerequisite Quests**: Artifacts often require quest completion
- **Scholar NPCs**: Turn-in quests to activate artifacts
- **Material Collection**: Gather scrolls and books through quests

### With Spell System
- **Custom Handlers**: Many artifacts have unique spell effects
- **Standard Integration**: Artifact spells use normal spell mechanics
- **Enhanced Effects**: Artifact spells often exceed normal limitations

### With Item System
- **Unique Properties**: Artifacts bypass normal item restrictions
- **Bonus Integration**: Uses standard bonus system with extensions
- **Dynamic Creation**: Can create items on demand

## Implementation Notes

### Database Design
```sql
-- Artifact table defines encounter requirements
CREATE TABLE Artifact (
    ArtifactID VARCHAR(255) PRIMARY KEY,
    EncounterID VARCHAR(255),
    QuestID VARCHAR(255),
    Credit VARCHAR(255),
    -- ... additional fields
);

-- ArtifactBonus defines progressive bonuses
CREATE TABLE ArtifactBonus (
    ArtifactID VARCHAR(255),
    BonusID INT,
    Level INT,
    -- Composite key (ArtifactID, BonusID, Level)
);
```

### Item Template Extensions
```csharp
// Artifacts extend normal item templates
public class ArtifactItemTemplate : DbItemTemplate
{
    // Additional artifact-specific properties
    // Integration with encounter/credit system
    // Dynamic bonus application
}
```

### Credit Validation
```csharp
public bool ValidateArtifactCredit(GamePlayer player, string artifactID)
{
    // Check encounter participation
    // Validate credit requirements
    // Ensure prerequisites met
    // Handle group credit sharing
}
```

## Test Scenarios

### Encounter Credit
1. **Verify** encounter participation grants credit
2. **Test** credit expiration timers
3. **Validate** group credit distribution

### Artifact Creation
1. **Test** scroll combination mechanics
2. **Verify** scholar interaction requirements
3. **Validate** prerequisite checking

### Ability Functionality
1. **Test** artifact spell effects
2. **Verify** proc chance calculations
3. **Validate** dynamic item creation

### System Integration
1. **Test** guild buff effects on artifacts
2. **Verify** artifact interaction with other systems
3. **Validate** reuse timer functionality

## Performance Considerations

### Dynamic Creation
- **Template Caching**: Cache created item templates
- **Lazy Loading**: Create items only when needed
- **Memory Management**: Prevent template duplication

### Credit Tracking
- **Efficient Lookup**: Index credit tables by player/encounter
- **Cleanup Procedures**: Remove expired credits
- **Group Optimization**: Batch group credit updates

## Change Log
- Initial documentation based on artifact database schema
- Includes encounter credit and material systems
- Documents artifact abilities and spell integration
- Covers guild integration and performance considerations 