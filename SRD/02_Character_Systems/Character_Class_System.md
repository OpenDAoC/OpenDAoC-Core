# Character Class System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Complete
- **Verification**: Code-verified from CharacterClassBase.cs, ICharacterClass.cs, and class implementations
- **Implementation**: Complete

## Overview

**Game Rule Summary**: Your character class determines everything about how your character plays - what weapons you can use, what spells you can cast, how tough you are, and how you gain power as you level up. Each class is designed for a specific role like tank, healer, caster, or stealth, and your choice shapes your entire gameplay experience. Classes are grouped by realm (Albion, Midgard, Hibernia) and you can only play with others from your realm.

The character class system defines the fundamental attributes, capabilities, and progression mechanics for all playable classes in DAoC. Classes are organized by realm and type, with inheritance hierarchies from base classes to advanced specializations.

## Core Architecture

### Class Interface

**Game Rule Summary**: Every class has core properties that define how it works - which stats are most important, how much health they start with, how many skill points they get, and what type of combat style they use. These properties determine whether you'll be a tough fighter, a fragile but powerful wizard, or something in between.

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

**Game Rule Summary**: Classes fall into three main combat styles. List Casters are pure spellcasters with powerful magic but weak melee combat. Pure Tanks are the opposite - excellent fighters with no magic at all. Hybrids combine both fighting and magic, but aren't as good at either as the specialists.

```csharp
public enum eClassType : int
{
    ListCaster,  // Full spellcasters with all spells
    Hybrid,      // Mixed melee/caster with limited spells
    PureTank     // Melee only, no spells
}
```

## Class Categories

**Game Rule Summary**: Every character starts as a base class and advances to a specialized class at level 5. Base classes like Fighter, Mage, or Rogue represent broad combat styles, while advanced classes like Armsman, Wizard, or Infiltrator are specialized versions with unique abilities. This system lets new players learn the basics before committing to a specific specialization.

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

**Game Rule Summary**: Every class has three stats that matter most for their role. Your primary stat goes up every level and determines your main effectiveness - Strength for fighters, Intelligence for wizards, etc. Secondary stats go up every other level and provide important support. Tertiary stats go up every third level and give minor benefits. This automatic progression ensures your character develops properly for their role.

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

**Game Rule Summary**: Different classes start with different amounts of health based on their role. Heavy armored tanks have the most health to absorb damage in combat, while fragile casters have much less health but rely on armor spells and staying out of melee range. This creates natural strengths and weaknesses for each class type.

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

**Game Rule Summary**: Specialization points determine how many skills you can train and how far you can advance them. Pure fighters get fewer points but focus them for maximum effectiveness, while versatile classes get more points to spread across many different skills. This creates meaningful choices about whether to specialize deeply or diversify broadly.

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

**Game Rule Summary**: Your base weapon skill determines how accurately you hit with melee weapons and ranged attacks. Fighter classes have high weapon skill and hit reliably, while casters have low weapon skill and often miss with physical attacks. This encourages each class to use their intended combat style.

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

**Game Rule Summary**: Different magical classes use different stats to power their spells. Wizards use Intelligence for raw magical power, healers use Piety for divine magic, druids use Empathy for nature magic, and some unusual classes like archers use Dexterity for their magical arrows. This means different casters need to focus on different stats to be effective.

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

**Game Rule Summary**: Some classes can fight with two weapons at once (dual wielding), giving them more attacks but each individual hit does less damage. Only certain classes have trained in this fighting style - most characters use a weapon and shield, or a single two-handed weapon.

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

**Game Rule Summary**: Some classes have special abilities that others don't. Assassin classes can use stealth and poisons, while focus casters can use magical staffs for enhanced spellcasting. These special abilities define the unique playstyle of each class beyond their basic stats and skills.

```csharp
// Assassin classes (special stealth/poison mechanics)
public virtual bool IsAssassin => false;

// Assassins: Infiltrator, Shadowblade, Nightshade

// Focus casters (staff specialization)
public virtual bool IsFocusCaster => false;

// Focus users: Base caster classes (Mage, Mystic, Magician)
```

### Multiple Pulsing Spells

**Game Rule Summary**: Most casters can only maintain one ongoing spell effect at a time, but some classes like Theurgists can maintain multiple summoned creatures simultaneously. This gives them unique tactical advantages in managing multiple magical effects.

```csharp
public virtual ushort MaxPulsingSpells
{
    get { return 1; }  // Default
}

// Theurgist override: Can maintain multiple summons
```

## Advanced Classes

**Game Rule Summary**: Here are examples of how different class types work in practice. Heavy tanks like Armsmen get lots of specialization points and health to be effective frontline fighters. Hybrids like Thanes balance fighting and magic with moderate points in both areas. Pure casters like Eldritches get fewer specialization points but focus entirely on magical power.

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

**Game Rule Summary**: From level 6 to 50, your character automatically gains 91 stat points distributed according to your class design. This ensures that fighters become stronger and tougher while casters become smarter and more magically powerful, without you having to manually assign points each level.

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