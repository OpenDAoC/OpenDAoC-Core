# Champion Level System

**Document Status:** Initial Documentation  
**Verification:** Code Review Needed  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Champion Levels are an alternative endgame progression system for level 50 players, offering 10 additional levels (CL1-CL10) focused on gaining specialized abilities through mini-lines. Unlike Master Levels which require group content, Champion Levels can be advanced through regular solo play and PvP. Each Champion Level gives you one point to spend on mini-line abilities that enhance your class in unique ways, creating more diverse character builds within the same class.

The Champion Level (CL) system provides additional character advancement beyond level 50, offering 10 champion levels (CL1-CL10) with specialized mini-lines that grant unique abilities and enhancements based on class type.

## Core Mechanics

### Champion Level Activation

**Game Rule Summary**: To become a Champion, you must visit your realm's King once you reach level 50. This is a one-time activation that unlocks the Champion Level system permanently for that character. Pure tank classes also gain a mana pool at this point, allowing them to use abilities that normally require power.

#### Requirements
- **Character Level**: Must be level 50
- **King NPC**: Visit realm's King to activate Champion status
- **Champion Flag**: Set on character once activated

```csharp
if (!player.Champion && player.Level == 50)
{
    // King offers to make player a Champion
}
```

### Experience System

**Game Rule Summary**: Champion Experience works differently from regular experience - it's earned by converting your normal experience gain at a reduced rate. PvE activities give less Champion XP, while RvR combat gives much better conversion rates, encouraging PvP participation. Each Champion Level requires progressively more experience, but the amounts are fixed and predictable rather than exponentially increasing.

#### XP Requirements
```csharp
public static readonly long[] CLXPLevel =
{
    0,      // xp to level 0
    32000,  // xp to level 1
    64000,  // xp to level 2
    96000,  // xp to level 3
    128000, // xp to level 4
    160000, // xp to level 5
    192000, // xp to level 6
    224000, // xp to level 7
    256000, // xp to level 8
    288000, // xp to level 9
    320000, // xp to level 10
    640000, // xp to level 11-15 (unused)
};
```

#### XP Gain Rates

**Game Rule Summary**: The conversion rates heavily favor RvR combat over PvE grinding. In PvE zones, it takes 2 million regular experience to earn 1 Champion XP, making solo advancement very slow. In RvR zones, you only need 333,000 regular experience per Champion XP, making PvP combat about 6 times more efficient for Champion progression.

```csharp
// PvE zones: 1 CLXP per 2 million regular XP
experience = (long)((double)experience * modifier / 2000000);

// RvR zones: 1 CLXP per 333k regular XP
experience = (long)((double)experience * modifier / 333000);

// Modifier from server property CL_XP_RATE
```

#### XP Sources
- Regular mob kills (converted at above rates)
- RvR kills (higher conversion rate)
- Quest rewards (direct CLXP)
- GM commands (direct CLXP)

### Champion Level Progression

#### Level Up Process
```csharp
public virtual void ChampionLevelUp()
{
    ChampionLevel++;
    
    // Pure tanks get full power at CL1
    if (ChampionLevel == 1 && CharacterClass.ClassType == eClassType.PureTank)
    {
        Mana = CalculateMaxMana(Level, 0);
    }
    
    RefreshSpecDependantSkills(true);
    Out.SendUpdatePlayerSkills(true);
    Notify(GamePlayerEvent.ChampionLevelUp, this);
}
```

#### Champion Points

**Game Rule Summary**: Each Champion Level gives you one Champion Point to spend on mini-line abilities. You can spread these points across multiple mini-lines or focus them in one area. Unlike specialization points, Champion Points are limited - you'll only ever get 10 total, so you must choose carefully which abilities matter most for your playstyle.

```csharp
public virtual int ChampionSpecialtyPoints
{
    get 
    { 
        // Total CL - spent points in mini-lines
        return ChampionLevel - GetSpecList()
            .Where(sp => sp is LiveChampionsLineSpec)
            .Sum(sp => sp.Level); 
    }
}
```

### Champion Mini-Lines

**Game Rule Summary**: Mini-lines are specialized ability paths based on your base class, offering abilities that complement or expand your class role. Each base class (like Fighter, Mage, Rogue) has access to different mini-lines with unique themes. You can train multiple mini-lines simultaneously, allowing for creative character builds that mix different ability types within your class family.

#### Line Types by Class
```csharp
// Base class types determine available lines
public class LiveCLAcolyteSpec : LiveChampionsLineSpec { }        // Clerics
public class LiveCLAlbionRogueSpec : LiveChampionsLineSpec { }    // Albion rogues
public class LiveCLDiscipleSpec : LiveChampionsLineSpec { }       // Healers
public class LiveCLElementalistSpec : LiveChampionsLineSpec { }   // Elementalists
public class LiveCLFighterSpec : LiveChampionsLineSpec { }        // Fighters
public class LiveCLForesterSpec : LiveChampionsLineSpec { }       // Hibernia nature
public class LiveCLGuardianSpec : LiveChampionsLineSpec { }       // Hibernia tanks
public class LiveCLMageSpec : LiveChampionsLineSpec { }           // Mages
public class LiveCLMagicianSpec : LiveChampionsLineSpec { }       // Hibernia casters
public class LiveCLMysticSpec : LiveChampionsLineSpec { }         // Midgard mystics
public class LiveCLNaturalistSpec : LiveChampionsLineSpec { }     // Druids/Wardens
public class LiveCLRogueSpec : LiveChampionsLineSpec { }          // Generic rogues
public class LiveCLSeerSpec : LiveChampionsLineSpec { }           // Midgard healers
public class LiveCLStalkerSpec : LiveChampionsLineSpec { }        // Hibernia rogues
public class LiveCLVikingSpec : LiveChampionsLineSpec { }         // Vikings
```

#### Training System

**Game Rule Summary**: Champion abilities are trained at special Champion Weapon Master NPCs found in your realm. Unlike regular specializations, each Champion Point unlocks exactly one ability in a mini-line. You can train different mini-lines simultaneously, allowing you to customize your character's capabilities based on your preferred playstyle and group role.

- Visit Champion Weapon Master NPCs
- Each point unlocks one ability in a mini-line
- Multiple paths available per class
- Can train different lines up to CL total

### Champion Titles

**Game Rule Summary**: Each Champion Level grants you a unique title that displays your Champion progression to other players. These titles are prestigious markers of your endgame advancement, progressing from "Seeker" at CL1 to "Labyrinthian" at CL10. The titles replace your class title while equipped, showing other players that you've achieved Champion status.

#### Title Progression
```csharp
// Title by champion level
switch (player.ChampionLevel)
{
    case 1: return "Seeker";
    case 2: return "Enforcer";
    case 3: return "Outrider";
    case 4: return "Lightbringer";
    case 5: return "King's Champion";
    case 6: return "King's Emissary";
    case 7: return "Patron of Minotaur";
    case 8: return "Visionary";
    case 9: return "Gladiator";
    case 10: return "Labyrinthian";
}
```

### Respecialization

**Game Rule Summary**: You get one free respec of your Champion abilities when you reach CL5, allowing you to experiment early and then settle on a final build. After that, you need special respec stones to change your Champion abilities. This system encourages experimentation while making your final choices meaningful.

#### CL Respec Options
1. **Free Respec at CL5**: Talk to CL Weapon Master
2. **Respec Stone**: Use "respec_cl" item
3. **GM Command**: `/player clrespec`

```csharp
public virtual void RespecChampionSkills()
{
    // Remove all champion line specs
    foreach (var spec in GetSpecList().Where(sp => sp is LiveChampionsLineSpec))
    {
        RemoveSpecialization(spec.KeyName);
    }
    
    RefreshSpecDependantSkills(false);
    Out.SendUpdatePlayer();
    Out.SendUpdatePoints();
    Out.SendUpdatePlayerSkills(true);
}
```

## Implementation Details

### Database Fields
```csharp
// DbCoreCharacter
public bool Champion { get; set; }              // CL activated
public int ChampionLevel { get; set; }          // Current CL (0-10)
public long ChampionExperience { get; set; }    // Current CLXP
public int RespecAmountChampionSkill { get; set; } // Free respecs
```

### Progress Calculation
```csharp
public virtual ushort ChampionLevelPermill
{
    get
    {
        if (ChampionExperience <= ChampionExperienceForCurrentLevel)
            return 0;
        if (ChampionLevel > CL_MAX_LEVEL)
            return 0;
            
        long currentLevelXP = ChampionExperienceForCurrentLevel;
        long nextLevelXP = ChampionExperienceForNextLevel;
        long progress = ChampionExperience - currentLevelXP;
        long needed = nextLevelXP - currentLevelXP;
        
        return (ushort)(1000 * progress / needed);
    }
}
```

## System Interactions

### Character System
- Requires level 50 to activate
- Pure tanks get mana at CL1
- Affects available abilities

### Combat System
- CL abilities enhance combat
- Some abilities scale with CL
- Unlocks new combat options

### Specialization System
- Mini-lines work like specs
- Share point pool (CL total)
- Can mix different lines

### Title System
- Automatic title updates
- Displays current CL title
- Overrides class titles

## Configuration

### Server Properties
```csharp
CL_XP_RATE  // Multiplier for CLXP gain
// Default: 1.0
```

### King NPC Settings
- Must be GameKingThroneNpc class
- Handles CL activation
- Processes level ups on interact

### Trainer NPCs
- CLWeaponNPC class
- Provides mini-line training
- Handles respec at CL5

## Edge Cases

### XP Cap
```csharp
if (ChampionExperience >= 320000)
{
    ChampionExperience = 320000;  // Hard cap
    return;
}
```

### Multiple Level Gains
```csharp
// Process all pending level ups
while (player.ChampionLevel < player.ChampionMaxLevel && 
       player.ChampionExperience >= player.ChampionExperienceForNextLevel)
{
    player.ChampionLevelUp();
}
```

### Praying Prevention
- No CLXP gain while praying
- Prevents death exploitation
- Same as regular XP

## Test Scenarios

1. **Activation Test**
   - Level 50 character
   - Visit King NPC
   - Verify Champion flag
   - Check mana for tanks

2. **XP Gain Test**
   - Kill mobs in PvE
   - Kill players in RvR
   - Verify conversion rates
   - Check XP messages

3. **Training Test**
   - Visit CL trainer
   - Purchase abilities
   - Verify point spending
   - Test multiple lines

4. **Respec Test**
   - Reach CL5
   - Free respec option
   - Use respec stone
   - Verify clean slate

## Formulas Summary

### CLXP Conversion
```
PvE: RegularXP * CL_XP_RATE / 2,000,000
RvR: RegularXP * CL_XP_RATE / 333,000
```

### Total CLXP Required
```
CL1: 32,000
CL2: 64,000
CL3: 96,000
CL4: 128,000
CL5: 160,000
CL6: 192,000
CL7: 224,000
CL8: 256,000
CL9: 288,000
CL10: 320,000
```

### Progress Percentage
```
Progress% = (CurrentCLXP - CurrentLevelXP) / (NextLevelXP - CurrentLevelXP) * 100
```

## TODO
- Document specific mini-line abilities
- Add trainer location details
- Detail class-specific CL paths
- Add PvP impact analysis
- Document CL ability interactions 