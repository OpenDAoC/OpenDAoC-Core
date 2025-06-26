# Trainer System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from GameTrainer.cs, Specialization.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
The Trainer System manages NPC trainers who provide specialization training, level 5 respec, and champion level training. Each class has dedicated trainers in their realm's starting cities and major towns.

## Core Mechanics

### Trainer Types

#### Class Trainers
Each character class has a dedicated trainer:

**Albion Trainers**:
- Fighter Trainer → Armsman, Mercenary, Paladin, Reaver
- Acolyte Trainer → Cleric, Friar, Heretic
- Rogue Trainer → Infiltrator, Minstrel, Scout
- Elementalist Trainer → Theurgist, Wizard
- Mage Trainer → Cabalist, Necromancer, Sorcerer

**Midgard Trainers**:
- Viking Trainer → Berserker, Savage, Skald, Thane, Valkyrie, Warrior
- Mystic Trainer → Bonedancer, Runemaster, Spiritmaster, Warlock
- Seer Trainer → Healer, Shaman
- Rogue Trainer → Hunter, Shadowblade

**Hibernia Trainers**:
- Guardian Trainer → Blademaster, Champion, Hero
- Naturalist Trainer → Druid, Warden
- Stalker Trainer → Nightshade, Ranger, Vampiir
- Magician Trainer → Eldritch, Enchanter, Mentalist
- Forester Trainer → Animist, Valewalker

#### Trainer Implementation
```csharp
[NPCGuildScript("Fighter Trainer", eRealm.Albion)]
public class FighterTrainer : GameTrainer
{
    public override eCharacterClass TrainedClass
    {
        get { return eCharacterClass.Fighter; }
    }
    
    public const string PRACTICE_WEAPON_ID = "practice_sword";
    public const string PRACTICE_SHIELD_ID = "small_training_shield";
}
```

### Training Services

#### Specialization Training
- **Point Allocation**: Spend specialization points on weapon/magic skills
- **Progressive Costs**: Higher levels cost more points
- **Immediate Effect**: Skills unlock instantly upon training

#### Level 5 Respec
```csharp
if (player.Level == 5 && !player.IsLevelRespecUsed)
{
    int specPoints = player.SkillSpecialtyPoints;
    player.RespecAll();
    
    // Assign full points returned
    if (player.SkillSpecialtyPoints > specPoints)
    {
        player.styleComponent.RemoveAllStyles();
        player.Out.SendMessage("You regain " + 
            (player.SkillSpecialtyPoints - specPoints) + " points!", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
    
    player.RefreshSpecDependantSkills(false);
    player.Out.SendUpdatePlayerSkills(true);
    player.Out.SendUpdatePoints();
    player.Out.SendTrainerWindow();
    player.SaveIntoDatabase();
}
```

#### Champion Training
```csharp
public enum eChampionTrainerType : int
{
    Acolyte = 4,
    AlbionRogue = 2,
    Disciple = 7,
    Elementalist = 5,
    Fighter = 1,
    Forester = 12,
    Guardian = 1,
    Mage = 6,
    Magician = 11,
    MidgardRogue = 3,
    Mystic = 9,
    Naturalist = 10,
    Seer = 8,
    Stalker = 2,
    Viking = 1,
    None = 0,
}
```

### Training Validation

#### Can Train Check
```csharp
public virtual bool CanTrain(GamePlayer player)
{
    if (player == null)
        return false;
        
    // Check if player can train with this trainer
    if (TrainedClass != eCharacterClass.Unknown)
    {
        if (player.CharacterClass.BaseClass != TrainedClass &&
            player.CharacterClass.ID != (int)TrainedClass)
            return false;
    }
    
    return true;
}
```

#### Realm Restrictions
- **Same Realm Only**: Players can only train with their realm's trainers
- **Class Matching**: Must match base class or exact class
- **Level Requirements**: Some training requires minimum levels

### Specialization System

#### Specialization Properties
```csharp
public class Specialization : NamedSkill
{
    public int LevelRequired { get; set; }    // Minimum level to train
    public bool Trainable { get; set; }       // Can be trained by players
    public bool AllowSave { get; set; }       // Saves to character record
    public override eSkillPage SkillType { get; } = eSkillPage.Specialization;
}
```

#### Training Mechanics
```csharp
public void OnSkillTrained(Specialization skill)
{
    Out.SendMessage("You spend " + skill.Level + " points in " + skill.Name + "!", 
                   eChatType.CT_System, eChatLoc.CL_SystemWindow);
    Out.SendMessage("You have " + SkillSpecialtyPoints + " specialization points left.", 
                   eChatType.CT_System, eChatLoc.CL_SystemWindow);
    
    CharacterClass.OnSkillTrained(this, skill);
    RefreshSpecDependantSkills(true);
}
```

### Skill Dependencies

#### Automatic Unlocks
When specializations increase, dependent skills unlock automatically:
1. **Abilities**: Combat abilities (Taunt, Sprint, etc.)
2. **Styles**: Combat styles for weapon specs
3. **Spell Lines**: Magic spell access

#### Refresh Process
```csharp
public virtual void RefreshSpecDependantSkills(bool sendMessages)
{
    lock (_specializationLock)
    {
        foreach (Specialization spec in m_specialization.Values)
        {
            // Check for new Abilities
            foreach (Ability ab in spec.GetAbilitiesForLiving(this))
            {
                if (!HasAbility(ab.KeyName) || GetAbility(ab.KeyName).Level < ab.Level)
                    AddAbility(ab, sendMessages);
            }
            
            // Check for new Styles
            foreach (Style st in spec.GetStylesForLiving(this))
            {
                styleComponent.AddStyle(st, sendMessages);
            }
            
            // Check for new SpellLines
            foreach (SpellLine sl in spec.GetSpellLinesForLiving(this))
            {
                AddSpellLine(sl, sendMessages);
            }
        }
    }
}
```

## Trainer Interactions

### Training Window
```csharp
public virtual void SendTrainerWindow()
{
    Out.SendTrainerWindow();
}
```
- **Available Specs**: Shows trainable specializations
- **Current Points**: Displays available specialization points
- **Costs**: Shows point cost for next level
- **Requirements**: Level and class restrictions

### Conversation System
```csharp
public override bool WhisperReceive(GameLiving source, string text)
{
    if (player != null && CanTrain(player) && 
        text == "respecialize" && player.Level == 5)
    {
        // Handle level 5 respec
        OfferRespecialize(player);
    }
    
    TurnTo(player, 10000);  // Face the player
    return true;
}
```

### Examine Messages
```csharp
public override IList GetExamineMessages(GamePlayer player)
{
    // Multi-language support
    string trainerClassName = GetTrainerClassName(player.Client.Account.Language);
    return new List<string> { "You examine " + Name + ". " + trainerClassName };
}
```

## System Interactions

### With Character Progression
- **Specialization Points**: Manages point allocation and spending
- **Level Requirements**: Enforces minimum levels for training
- **Class Restrictions**: Ensures proper class/trainer matching

### With Combat System
- **Style Unlocking**: New styles become available with training
- **Ability Grants**: Combat abilities unlock at spec thresholds
- **Weapon Skills**: Specialization directly affects weapon effectiveness

### With Magic System
- **Spell Line Access**: Magic specs unlock spell lines
- **Spell Level**: Higher specs grant access to higher level spells
- **Mana Efficiency**: Some specs affect casting costs

### With Database
- **Specialization Storage**: Persistent character specialization data
- **Respec Tracking**: One-time level 5 respec flag
- **Skill Validation**: Ensures data integrity

## Configuration

### Trainer Placement
- **Starting Cities**: Primary trainers in each realm capital
- **Major Towns**: Secondary trainers in key locations
- **Frontier**: Limited training access in RvR zones

### Disabled Classes
```csharp
private static List<string> disabled_classes = null;
```
- **Server Control**: Administrators can disable certain classes
- **Dynamic Loading**: Checks against disabled list during training

## Special Features

### Practice Equipment
Many trainers provide starting equipment:
```csharp
public const string PRACTICE_WEAPON_ID = "practice_sword";
public const string PRACTICE_SHIELD_ID = "small_training_shield";
```

### Champion Training
```csharp
public GameTrainer(eChampionTrainerType championTrainerType)
{
    m_championTrainerType = championTrainerType;
}
```
- **Post-50 Training**: Champion levels and abilities
- **Specialized Trainers**: Different champion types per base class

### Multi-Language Support
- **Localized Text**: Trainer messages in multiple languages
- **Cultural Names**: Trainer names appropriate to realm/culture

## Testing Scenarios

### Basic Training Test
```
Given: Level 10 Fighter with 20 spec points
When: Trains Slash to level 15 (costs 15 points)
Then: Slash specialization increases to 15
      Available points reduced to 5
      New styles unlock automatically
      Training message displayed
```

### Level 5 Respec Test
```
Given: Level 5 character who hasn't respec'd
When: Says "respecialize" to trainer
Then: All specializations reset to 1
      All spent points returned
      All styles removed
      Respec flag set to true
      Skills refreshed
```

### Cross-Realm Test
```
Given: Albion player approaches Midgard trainer
When: Attempts to interact
Then: Trainer refuses service
      Appropriate error message
      No training window opens
```

### Champion Training Test
```
Given: Level 50 character with champion trainer
When: Attempts champion training
Then: Champion window opens
      Available champion lines displayed
      Champion points shown
```

## References
- **Core System**: `GameServer/gameobjects/CustomNPC/GameTrainer.cs`
- **Specializations**: `GameServer/gameutils/Specialization.cs`
- **Class Trainers**: `GameServer/trainer/[realm]/[class]Trainer.cs`
- **Player Integration**: `GameServer/gameobjects/GamePlayer.cs` 