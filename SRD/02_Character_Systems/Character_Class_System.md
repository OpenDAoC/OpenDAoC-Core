# Character Class System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Complete
- **Verification**: Code-verified from CharacterClassBase.cs, ICharacterClass.cs, and class implementations
- **Implementation**: Complete

## Overview
The character class system defines the fundamental attributes, capabilities, and progression mechanics for all playable classes in DAoC. Classes are organized by realm and type, with inheritance hierarchies from base classes to advanced specializations.

## Core Architecture

### Class Interface
```csharp
public interface ICharacterClass
{
    // Identity
    int ID { get; }
    string Name { get; }
    string FemaleName { get; }
    string BaseName { get; }
    
    // Class Type
    eClassType ClassType { get; }
    bool CanUseLefthandedWeapon { get; }
    
    // Primary Attributes
    eStat PrimaryStat { get; }
    eStat SecondaryStat { get; }
    eStat TertiaryStat { get; }
    eStat ManaStat { get; }
    
    // Progression
    int BaseHP { get; }
    int SpecPointsMultiplier { get; }
    int WeaponSkillBase { get; }
    int WeaponSkillRangedBase { get; }
}
```

### Class Types
```csharp
public enum eClassType : int
{
    ListCaster,  // Full spellcasters with all spells
    Hybrid,      // Mixed melee/caster with limited spells
    PureTank     // Melee only, no spells
}
```

## Class Categories

### Base Classes
Base classes are the starting point for character creation:

#### Albion
- **Fighter**: STR-based melee (Arms, Paladin, Reaver, Mercenary, Mauler)
- **Acolyte**: PIE-based healer (Cleric, Friar, Heretic)
- **Mage**: INT-based caster (Wizard, Theurgist, Cabalist, Sorcerer, Necromancer)
- **Rogue**: DEX-based (Scout, Infiltrator, Minstrel)

#### Midgard
- **Viking**: STR-based melee (Warrior, Berserker, Savage, Skald, Thane, Valkyrie, Mauler)
- **Mystic**: PIE-based support (Runemaster, Spiritmaster, Bonedancer, Warlock)
- **Seer**: PIE-based healer (Healer, Shaman)
- **Rogue**: DEX-based (Hunter, Shadowblade)

#### Hibernia
- **Guardian**: STR-based melee (Hero, Champion, Blademaster, Mauler)
- **Stalker**: DEX-based (Ranger, Nightshade, Vampiir)
- **Naturalist**: EMP-based hybrid (Druid, Bard, Warden)
- **Magician**: INT-based caster (Eldritch, Enchanter, Mentalist)
- **Forester**: INT-based pet (Animist, Valewalker)

## Class Attributes

### Primary Stats
```csharp
// Stat progression by level
public virtual void OnLevelUp(GamePlayer player, int previousLevel)
{
    // Primary stat: Every level
    if (PrimaryStat != eStat.UNDEFINED)
        player.ChangeBaseStat(PrimaryStat, 1);
        
    // Secondary stat: Every 2nd level
    if (SecondaryStat != eStat.UNDEFINED && level % 2 == 0)
        player.ChangeBaseStat(SecondaryStat, 1);
        
    // Tertiary stat: Every 3rd level
    if (TertiaryStat != eStat.UNDEFINED && level % 3 == 0)
        player.ChangeBaseStat(TertiaryStat, 1);
}
```

### Hit Point Calculation
```csharp
// Base HP varies by class
protected int m_baseHP = 700;  // Default

// Examples:
// Casters: 560 HP
// Light tanks: 720 HP
// Medium tanks: 760-880 HP
// Heavy tanks: 880+ HP
// Maulers: 600 HP (special)
```

### Specialization Points
```csharp
// Points per level in tenths
protected int m_specializationMultiplier = 10;

// Standard values:
// Pure casters: 10 (1.0 per level)
// Hybrids: 15 (1.5 per level)
// Light tanks: 20 (2.0 per level)
// Assassins: 22-25 (2.2-2.5 per level)

// Total spec points = Level * Multiplier / 10
```

### Weapon Skill Base
```csharp
// Base weapon skill affects melee accuracy
protected int m_wsbase = 400;      // Default
protected int m_wsbaseRanged = 360; // Ranged default

// Examples:
// Casters: 280
// Hybrids: 360-400
// Tanks: 400-440
// Assassins: 400
```

## Mana Stats

### Mana Stat by Class Type
```csharp
// Determines which stat affects power pool
protected eStat m_manaStat = eStat.UNDEFINED;

// Standard assignments:
// INT casters: eStat.INT
// PIE healers: eStat.PIE
// EMP druids: eStat.EMP
// DEX archers: eStat.DEX (arrow magic)
// STR maulers: eStat.STR (fist wraps)
// Vampiir: Special case (uses STR for AcuityBonus)
```

## Class Capabilities

### Left-Handed Weapons
```csharp
public virtual bool CanUseLefthandedWeapon
{
    get { return false; }  // Default
}

// Classes with dual wield:
// - Berserker, Savage, Shadowblade
// - Blademaster, Ranger, Nightshade
// - Mercenary, Infiltrator, Minstrel
// - All Maulers
```

### Special Flags
```csharp
// Assassin classes (special stealth/poison mechanics)
public virtual bool IsAssassin => false;

// Assassins: Infiltrator, Shadowblade, Nightshade

// Focus casters (staff specialization)
public virtual bool IsFocusCaster => false;

// Focus users: Base caster classes (Mage, Mystic, Magician)
```

### Multiple Pulsing Spells
```csharp
public virtual ushort MaxPulsingSpells
{
    get { return 1; }  // Default
}

// Theurgist override: Can maintain multiple summons
```

## Advanced Classes

### Realm-Specific Examples

#### Albion Heavy Tanks
```csharp
[CharacterClass((int)eCharacterClass.Armsman, "Armsman", "Fighter")]
public class ClassArmsman : ClassFighter
{
    m_specializationMultiplier = 20;  // 2.0/level
    m_primaryStat = eStat.STR;
    m_secondaryStat = eStat.CON;
    m_tertiaryStat = eStat.DEX;
    m_baseHP = 880;
    m_wsbase = 440;
    
    public override eClassType ClassType => eClassType.PureTank;
}
```

#### Midgard Hybrid
```csharp
[CharacterClass((int)eCharacterClass.Thane, "Thane", "Viking")]
public class ClassThane : ClassViking
{
    m_specializationMultiplier = 15;  // 1.5/level
    m_primaryStat = eStat.STR;
    m_secondaryStat = eStat.PIE;
    m_tertiaryStat = eStat.CON;
    m_manaStat = eStat.PIE;
    m_wsbase = 380;
    
    public override eClassType ClassType => eClassType.Hybrid;
}
```

#### Hibernia Caster
```csharp
[CharacterClass((int)eCharacterClass.Eldritch, "Eldritch", "Magician")]
public class ClassEldritch : ClassMagician
{
    m_specializationMultiplier = 10;  // 1.0/level
    m_primaryStat = eStat.INT;
    m_secondaryStat = eStat.DEX;
    m_tertiaryStat = eStat.QUI;
    m_manaStat = eStat.INT;
    m_baseHP = 560;
    m_wsbase = 280;
    
    public override eClassType ClassType => eClassType.ListCaster;
}
```

## Class Attribute Distribution

### Stat Growth Patterns
```
Primary Stat: +1 every level (50 total)
Secondary Stat: +1 every 2 levels (25 total)
Tertiary Stat: +1 every 3 levels (16 total)
Total stats gained: 91 points by level 50
```

### Starting Stats
Base stats determined by race + class modifiers at character creation

## Champion Levels

### Champion Trainer Types
```csharp
public enum eChampionTrainerType
{
    None,
    Fighter,    // Albion melee
    Acolyte,    // Albion healer
    Mage,       // Albion caster
    AlbionRogue,// Albion stealth/archer
    Viking,     // Midgard melee
    Mystic,     // Midgard caster
    Seer,       // Midgard healer
    MidgardRogue,// Midgard stealth/archer
    Guardian,   // Hibernia melee
    Naturalist, // Hibernia hybrid
    Magician,   // Hibernia caster
    Stalker     // Hibernia stealth
}
```

## Race Restrictions

### Eligible Races
```csharp
public virtual List<PlayerRace> EligibleRaces { get; }

// Example - Paladin:
EligibleRaces => new List<PlayerRace>()
{
    PlayerRace.Avalonian,
    PlayerRace.Briton,
    PlayerRace.Highlander,
    PlayerRace.Saracen
};
```

## Autotrainable Skills

### Automatic Specializations
```csharp
public virtual IList<string> GetAutotrainableSkills()
{
    return AutotrainableSkills;
}

// Examples:
// Scout: Archery, Longbow
// Ranger: Archery, RecurveBow
// Infiltrator: Stealth
// Berserker: Left Axe
```

## Title System

### Level-Based Titles
```csharp
public virtual string GetTitle(GamePlayer player, int level)
{
    // Titles at levels 5, 10, 15, 20, 25, 30, 35, 40, 45, 50
    int clamplevel = Math.Min(50, (level / 5) * 5);
    
    return LanguageMgr.TryTranslateOrDefault(player, 
        $"PlayerClass.{m_name}.GetTitle.{clamplevel}");
}
```

## Class Events

### Level Up Handler
```csharp
public virtual void OnLevelUp(GamePlayer player, int previousLevel)
{
    // Stat increases
    // Ability grants
    // Spell line awards
}
```

### Realm Rank Up Handler
```csharp
public virtual void OnRealmLevelUp(GamePlayer player)
{
    // Realm ability grants
    // RR-based benefits
}
```

### Skill Training Handler
```csharp
public virtual void OnSkillTrained(GamePlayer player, Specialization skill)
{
    // Spell line unlocks
    // Ability grants
    // Style awards
}
```

## Special Class Mechanics

### Vampiir
- No traditional mana stat
- Uses Constitution-based power pool
- Special handling in power calculations

### Mauler
- Uses STR for mana (fist wraps)
- Lower base HP (600)
- Higher weapon skill (440)

### Necromancer
- Pet-based mechanics
- Shade form abilities
- Special death handling

### Animist
- Turret pet system
- Forest heart mechanics
- Area control focus

## Implementation Notes

### Class Loading
```csharp
// Classes registered via attribute
[CharacterClass(id, name, basename, femaleName)]

// Loaded at server start
// Mapped to eCharacterClass enum
```

### Performance
- Class data cached at character creation
- Stat calculations optimized
- Title lookups use language manager

## Test Scenarios

### Class Creation
```
1. Create character of each class
2. Verify starting stats
3. Check base HP calculation
4. Confirm spec points
```

### Level Progression
```
1. Level character to 50
2. Verify stat gains
3. Check spec point totals
4. Validate title changes
```

### Class Capabilities
```
1. Test dual wield eligibility
2. Verify mana stat usage
3. Check class type
4. Validate race restrictions
```

## Edge Cases

### Base Class Characters
- Cannot progress beyond level 5
- Limited abilities/spells
- Special title handling

### Cross-Realm Classes
- Maulers exist in all realms
- Different base classes
- Shared mechanics

### Gender-Specific Names
- Valkyrie (female only historically)
- Hunter/Huntress distinction
- Title localization

## Change Log

### 2025-01-20
- Initial documentation created
- All class types documented
- Stat systems detailed
- Special mechanics included

## References
- CharacterClassBase.cs: Base implementation
- ICharacterClass.cs: Interface definition
- Individual class files: Specific implementations
- eCharacterClass.cs: Class enumeration 