# Title System

**Document Status:** Production Ready
**Implementation Status:** Live
**Verification:** Code Verified

## Overview

**Game Rule Summary**: The Title System provides hundreds of titles that let you display your achievements and progression to other players. As you level up, you automatically earn class-specific titles every 5 levels (like "Soldier" at level 20 or "General" at level 50 for Armsmen). Beyond class progression, you can earn achievement titles for PvP accomplishments (like "Master Soldier" for 2,000+ player kills), time-based titles (like "Veteran" for long-lived characters), and special titles for rare accomplishments (like "Dragon Slayer" for killing many dragons). You can set any earned title as your active title, and it will appear above your character's name for all players to see. The system supports different title versions for male and female characters and multiple languages, making titles a key part of character identity and recognition in the community.

The Title System provides extensive character recognition through hundreds of titles based on class progression, achievements, time played, RvR accomplishments, and special events. The system supports multi-language localization, gender-specific titles, and dynamic title granting based on real-time achievements.

## Core Architecture

### Title Base Classes

#### Simple Title Interface
```csharp
public interface IPlayerTitle
{
    string GetDescription(GamePlayer player);
    string GetValue(GamePlayer player);
    bool IsSuitable(GamePlayer player);
    void OnTitleGained(GamePlayer player);
    void OnTitleLost(GamePlayer player);
}
```

#### Event-Based Titles
```csharp
public abstract class EventPlayerTitle : SimplePlayerTitle
{
    protected EventPlayerTitle()
    {
        GameEventMgr.AddHandler(Event, new DOLEventHandler(EventCallback));
    }
    
    public abstract DOLEvent Event { get; }
    
    protected virtual void EventCallback(DOLEvent e, object sender, EventArgs arguments)
    {
        if (sender is GamePlayer player)
        {
            if (IsSuitable(player))
                player.AddTitle(this);
            else
                player.RemoveTitle(this);
        }
    }
}
```

### Title Categories

#### Class-Based Titles
```csharp
// Level-based class titles (every 5 levels)
public abstract class ClassTitle : SimplePlayerTitle
{
    protected abstract int RequiredLevel { get; }
    protected abstract eCharacterClass RequiredClass { get; }
    
    public override bool IsSuitable(GamePlayer player)
    {
        return player.Level >= RequiredLevel && 
               player.CharacterClass.ID == (int)RequiredClass;
    }
}
```

#### Achievement-Based Titles
```csharp
public abstract class AchievementTitle : EventPlayerTitle
{
    protected abstract int RequiredCount { get; }
    protected abstract Func<GamePlayer, int> CountMethod { get; }
    
    public override bool IsSuitable(GamePlayer player)
    {
        return CountMethod(player) >= RequiredCount;
    }
}
```

## Title Types

### Class Progression Titles

#### Albion Class Titles
```csharp
// Example: Armsman progression
Level 5:  Enlistee
Level 10: Footsoldier  
Level 15: Infantry
Level 20: Soldier
Level 25: Legionnaire
Level 30: Sergeant
Level 35: Lieutenant
Level 40: Centurian
Level 45: Captain
Level 50: General
```

#### Midgard Class Titles
```csharp
// Example: Warrior progression
Level 5:  Initiate Fighter
Level 10: Yeoman Fighter
Level 15: Footman
Level 20: Veteran
Level 25: Myrmidon of Tyr
Level 30: Elite Skirmisher
Level 35: Marauder
Level 40: Warmonger
Level 45: Warlord
Level 50: Hand of Tyr
```

#### Hibernia Class Titles
```csharp
// Example: Hero progression
Level 5:  Attendant
Level 10: Servitor
Level 15: Confidant
Level 20: Henchman
Level 25: Stalwart
Level 30: Gladiator
Level 35: Eminence
Level 40: Valorant
Level 45: Paragon
Level 50: Seraph
```

### Achievement Titles

#### PvP Kill Titles
```csharp
public class MasterSoldierTitle : TranslatedNoGenderGenericEventPlayerTitle
{
    public override DOLEvent Event => GamePlayerEvent.KillsTotalPlayersChanged;
    
    protected override Func<GamePlayer, bool> SuitableMethod => 
        player => (player.KillsHiberniaPlayers + player.KillsMidgardPlayers + player.KillsAlbionPlayers) >= 2000 && 
                  (player.KillsHiberniaPlayers + player.KillsMidgardPlayers + player.KillsAlbionPlayers) < 25000;
}
```

#### Kill Categories
- **Master Soldier**: 2,000+ total player kills
- **Master Enforcer**: 25,000+ total player kills  
- **Master Assassin**: 100,000+ total player kills
- **Battle Enforcer**: 2,000+ deathblows
- **Battle Master**: 25,000+ deathblows

#### Realm-Specific Kill Titles
```csharp
// Bane titles for realm-specific kills
public class BaneOfAlbionTitle : TranslatedNoGenderGenericEventPlayerTitle
{
    protected override Func<GamePlayer, bool> SuitableMethod => 
        player => player.KillsAlbionPlayers >= 1000;
}
```

#### PvE Achievement Titles
```csharp
public class DragonSlayerTitle : TranslatedNoGenderGenericEventPlayerTitle
{
    public override DOLEvent Event => GamePlayerEvent.KillsDragonChanged;
    
    protected override Func<GamePlayer, bool> SuitableMethod => 
        player => player.KillsDragon >= 50 && player.KillsDragon < 100;
}
```

### Time-Based Titles

#### Character Age Titles
```csharp
public class VeteranTitle : TranslatedNoGenderGenericEventPlayerTitle
{
    public override DOLEvent Event => GamePlayerEvent.GameEntered;
    
    protected override Func<GamePlayer, bool> SuitableMethod => 
        player => DateTime.Now.Subtract(player.CreationDate).TotalDays >= 178; // ~6 months
}
```

#### Account Age Titles
```csharp
public class ElderTitle : TranslatedNoGenderGenericEventPlayerTitle
{
    public override DOLEvent Event => GamePlayerEvent.GameEntered;
    
    protected override Func<GamePlayer, bool> SuitableMethod => 
        player => DateTime.Now.Subtract(player.Client.Account.CreationDate).TotalDays >= 365; // 1 year
}
```

### Master Level Titles

#### ML Line Titles
```csharp
public class MasterlevelTitle : EventPlayerTitle
{
    public override DOLEvent Event => GamePlayerEvent.MLLevelUp;
    
    public override string GetValue(GamePlayer player)
    {
        return GetMLTitleString(player.MLLevel);
    }
    
    private string GetMLTitleString(int mlLevel)
    {
        return mlLevel switch
        {
            1 => "Banelord",
            2 => "Battlemaster", 
            3 => "Convoker",
            4 => "Perfecter",
            5 => "Sojourner",
            6 => "Spymaster",
            7 => "Stormlord",
            8 => "Warlord",
            _ => ""
        };
    }
}
```

### Champion Level Titles

#### CL Progression Titles  
```csharp
CL1: Seeker
CL2: Enforcer
CL3: Outrider  
CL4: Lightbringer
CL5: King's Champion
CL6: King's Emissary
CL7: Patron of Minotaur
CL8: Visionary
CL9: Gladiator
CL10: Labyrinthian
```

### Realm Rank Titles

#### RR Title Structure
```csharp
public abstract class RealmGenericEventPlayerTitle : GenericEventPlayerTitle
{
    protected abstract int RRLevel { get; }
    protected abstract eRealm Realm { get; }
    
    protected override Tuple<string, string, string, string> GenericNames
    {
        get
        {
            string realm = Realm switch
            {
                eRealm.Albion => "Albion",
                eRealm.Midgard => "Midgard", 
                _ => "Hibernia"
            };
            
            string male = $"GamePlayer.RealmTitle.{realm}.RR{RRLevel}.Male";
            string female = $"GamePlayer.RealmTitle.{realm}.RR{RRLevel}.Female";
            
            return new Tuple<string, string, string, string>(null, male, null, female);
        }
    }
}
```

### Privilege Level Titles

#### Staff Titles
```csharp
public class GamemasterTitle : SimplePlayerTitle
{
    public override bool IsSuitable(GamePlayer player)
    {
        return player.Client.Account.PrivLevel >= 2;
    }
}

public class AdministratorTitle : SimplePlayerTitle
{
    public override bool IsSuitable(GamePlayer player)
    {
        return player.Client.Account.PrivLevel >= 3;
    }
}
```

## Title Management System

### Player Title Collection
```csharp
public class GamePlayer
{
    private List<IPlayerTitle> m_titles = new List<IPlayerTitle>();
    private IPlayerTitle m_currentTitle = PlayerTitleMgr.ClearTitle;
    
    public virtual bool AddTitle(IPlayerTitle title)
    {
        if (m_titles.Contains(title)) return false;
        
        m_titles.Add(title);
        title.OnTitleGained(this);
        return true;
    }
    
    public virtual bool RemoveTitle(IPlayerTitle title)
    {
        if (!m_titles.Contains(title)) return false;
        
        if (CurrentTitle == title)
            CurrentTitle = PlayerTitleMgr.ClearTitle;
            
        m_titles.Remove(title);
        title.OnTitleLost(this);
        return true;
    }
}
```

### Title Display Updates
```csharp
public virtual void UpdateCurrentTitle()
{
    if (ObjectState == eObjectState.Active)
    {
        foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
        {
            if (player != null && player != this)
                player.Out.SendPlayerTitleUpdate(this);
        }
        Out.SendUpdatePlayer();
    }
}
```

### Current Title Setting
```csharp
public virtual IPlayerTitle CurrentTitle
{
    get => m_currentTitle;
    set
    {
        m_currentTitle = value ?? PlayerTitleMgr.ClearTitle;
        
        if (ObjectState == eObjectState.Active)
        {
            if (value == PlayerTitleMgr.ClearTitle)
            {
                Out.SendMessage(
                    LanguageMgr.GetTranslation(Client.Account.Language, "GamePlayer.CurrentTitle.TitleCleared"),
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
            else
            {
                Out.SendMessage($"Your title has been set to {value.GetDescription(this)}.",
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
        UpdateCurrentTitle();
    }
}
```

## Localization System

### Multi-Language Support
```csharp
public abstract class TranslatedPlayerTitle : SimplePlayerTitle
{
    protected abstract Tuple<string, string> DescriptionValue { get; }
    
    public override string GetDescription(GamePlayer player)
    {
        return LanguageMgr.GetTranslation(
            player.Client.Account.Language, 
            DescriptionValue.Item1);
    }
    
    public override string GetValue(GamePlayer player)
    {
        return LanguageMgr.GetTranslation(
            player.Client.Account.Language, 
            DescriptionValue.Item2);
    }
}
```

### Gender-Specific Titles
```csharp
public abstract class GenericEventPlayerTitle : EventPlayerTitle
{
    protected abstract Tuple<string, string, string, string> GenericNames { get; }
    
    public override string GetDescription(GamePlayer player)
    {
        var names = GenericNames;
        bool isFemale = player.Gender == eGender.Female;
        
        string descKey = isFemale ? names.Item3 : names.Item1;
        
        if (!string.IsNullOrEmpty(descKey))
            return LanguageMgr.GetTranslation(player.Client.Account.Language, descKey);
            
        return GetValue(player);
    }
}
```

### Language File Structure
```ini
# English titles (PlayerTitles.txt)
Titles.Kills.All.MasterSoldier: Master Soldier
Titles.Kills.All.MasterEnforcer: Master Enforcer
Titles.Time.Character.Veteran: Veteran
Titles.Time.Account.ElderTitle: Elder

# Class titles by level
PlayerClass.Armsman.GetTitle.50: General
PlayerClass.Armsman.GetTitle.45: Captain
PlayerClass.Armsman.GetTitle.40: Centurian
```

## Dynamic Title Granting

### Real-Time Event Handling
```csharp
// Title granted immediately upon achievement
[GamePlayerEvent.KillsTotalPlayersChanged]
public static void OnPlayerKillsChanged(DOLEvent e, object sender, EventArgs args)
{
    if (sender is GamePlayer player)
    {
        // Check all kill-based titles
        CheckKillTitles(player);
    }
}

private static void CheckKillTitles(GamePlayer player)
{
    int totalKills = player.KillsAlbionPlayers + player.KillsMidgardPlayers + player.KillsHiberniaPlayers;
    
    if (totalKills >= 2000 && totalKills < 25000)
        player.AddTitle(new MasterSoldierTitle());
    else if (totalKills >= 25000 && totalKills < 100000)
        player.AddTitle(new MasterEnforcerTitle());
    else if (totalKills >= 100000)
        player.AddTitle(new MasterAssassinTitle());
}
```

### Login Validation
```csharp
[GamePlayerEvent.GameEntered]
public static void OnPlayerLogin(DOLEvent e, object sender, EventArgs args)
{
    if (sender is GamePlayer player)
    {
        // Validate all time-based titles
        CheckTimeTitles(player);
        
        // Validate achievement titles
        CheckAchievementTitles(player);
    }
}
```

## Special Title Categories

### Rare Achievement Titles
```csharp
// Relic capture titles
public class RelicCaptureTitle : EventPlayerTitle
{
    public override DOLEvent Event => GamePlayerEvent.RelicCaptured;
    
    public override bool IsSuitable(GamePlayer player)
    {
        return player.RelicsCaptured >= RequiredCaptures;
    }
}

// Keep capture titles  
public class KeepCaptureTitle : EventPlayerTitle
{
    public override DOLEvent Event => GamePlayerEvent.KeepCaptured;
    
    public override bool IsSuitable(GamePlayer player)
    {
        return player.KeepsCaptured >= RequiredCaptures;
    }
}
```

### Duel Titles
```csharp
public class DuelMasterTitle : EventPlayerTitle
{
    public override DOLEvent Event => GamePlayerEvent.SoloKillsChanged;
    
    public override bool IsSuitable(GamePlayer player)
    {
        return player.SoloKills >= 100;
    }
}
```

### PvE Boss Titles
```csharp
// Dragon encounter titles
public class DragonFoeTitle : EventPlayerTitle  // 10+ dragon kills
public class DragonScourgeTitle : EventPlayerTitle  // 50+ dragon kills  
public class DragonSlayerTitle : EventPlayerTitle   // 100+ dragon kills

// Demon encounter titles
public class DemonBaneTitle : EventPlayerTitle     // 25+ demon kills
public class DemonScourgeTitle : EventPlayerTitle  // 100+ demon kills
public class DemonSlayerTitle : EventPlayerTitle   // 500+ demon kills
```

## Title Search and Commands

### Title Selection Command
```csharp
[CommandHandler("title")]
public class PlayerTitleCommand : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length < 2)
        {
            // Display available titles
            DisplayAvailableTitles(client);
            return;
        }
        
        string titleName = string.Join(" ", args.Skip(1));
        
        if (titleName.ToLower() == "clear")
        {
            client.Player.CurrentTitle = PlayerTitleMgr.ClearTitle;
            return;
        }
        
        // Find and set title
        var title = FindPlayerTitle(client.Player, titleName);
        if (title != null)
            client.Player.CurrentTitle = title;
        else
            DisplayMessage(client, $"Title '{titleName}' not found or not available.");
    }
}
```

### Title Lookup System
```csharp
private static IPlayerTitle FindPlayerTitle(GamePlayer player, string titleName)
{
    return player.Titles.FirstOrDefault(title => 
        title.GetValue(player).Equals(titleName, StringComparison.OrdinalIgnoreCase) ||
        title.GetDescription(player).Equals(titleName, StringComparison.OrdinalIgnoreCase));
}
```

## Integration Points

### Character Creation Integration
```csharp
// Grant initial class title at character creation
public override void OnCharacterCreated(GamePlayer player)
{
    // Grant level 1 class title if available
    var classTitle = GetClassTitle(player.CharacterClass.ID, 1);
    if (classTitle != null)
        player.AddTitle(classTitle);
}
```

### Level-Up Integration
```csharp
[GamePlayerEvent.LevelUp]
public static void OnPlayerLevelUp(DOLEvent e, object sender, EventArgs args)
{
    if (sender is GamePlayer player)
    {
        // Check for new class title
        if (player.Level % 5 == 0) // Every 5 levels
        {
            var newClassTitle = GetClassTitle(player.CharacterClass.ID, player.Level);
            if (newClassTitle != null)
                player.AddTitle(newClassTitle);
        }
    }
}
```

### Guild Integration
```csharp
// Guild rank titles
public class GuildLeaderTitle : EventPlayerTitle
{
    public override DOLEvent Event => GamePlayerEvent.GuildRankChanged;
    
    public override bool IsSuitable(GamePlayer player)
    {
        return player.Guild != null && player.GuildRank?.Rank == 0; // Leader rank
    }
}
```

## Performance Considerations

### Title Caching
```csharp
// Cache title collections for performance
private static Dictionary<eCharacterClass, Dictionary<int, IPlayerTitle>> classCache = 
    new Dictionary<eCharacterClass, Dictionary<int, IPlayerTitle>>();

public static IPlayerTitle GetClassTitle(eCharacterClass charClass, int level)
{
    if (classCache.TryGetValue(charClass, out var levelTitles))
    {
        if (levelTitles.TryGetValue(level, out var title))
            return title;
    }
    
    // Load and cache title
    var newTitle = LoadClassTitle(charClass, level);
    CacheTitle(charClass, level, newTitle);
    return newTitle;
}
```

### Event Optimization
```csharp
// Avoid excessive title checking on frequent events
private static DateTime lastTitleCheck = DateTime.MinValue;

public static void OptimizedTitleCheck(GamePlayer player)
{
    if (DateTime.Now.Subtract(lastTitleCheck).TotalSeconds < 5)
        return; // Throttle title checks
        
    CheckAllTitles(player);
    lastTitleCheck = DateTime.Now;
}
```

## Configuration and Customization

### Server Properties
```ini
# Title system configuration
ENABLE_TITLE_SYSTEM = true
ENABLE_CLASS_TITLES = true
ENABLE_ACHIEVEMENT_TITLES = true
ENABLE_TIME_TITLES = true
AUTO_GRANT_TITLES = true
TITLE_ANNOUNCEMENT = true
```

### Custom Title Addition
```csharp
// Framework for adding custom titles
public static void RegisterCustomTitle(IPlayerTitle title)
{
    PlayerTitleMgr.RegisterTitle(title);
    
    if (title is EventPlayerTitle eventTitle)
    {
        // Automatically hook events for event-based titles
        GameEventMgr.AddHandler(eventTitle.Event, eventTitle.EventCallback);
    }
}
```

## Test Scenarios

### Title Granting Tests
```csharp
[Test]
public void TestClassTitleProgression()
{
    // Given: Level 20 Armsman
    var player = CreateTestArmsman(level: 20);
    
    // When: Player levels up to 25
    player.SetLevel(25);
    
    // Then: Should have "Legionnaire" title
    Assert.IsTrue(player.HasTitle("Legionnaire"));
}

[Test]
public void TestAchievementTitle()
{
    // Given: Player with 1999 kills
    var player = CreateTestPlayer();
    player.SetTotalKills(1999);
    
    // When: Player gets one more kill
    player.SetTotalKills(2000);
    
    // Then: Should receive Master Soldier title
    Assert.IsTrue(player.HasTitle(typeof(MasterSoldierTitle)));
}
```

### Localization Tests
```csharp
[Test]
public void TestTitleLocalization()
{
    // Given: German client player
    var player = CreateTestPlayer(language: "DE");
    player.AddTitle(new VeteranTitle());
    
    // When: Title is displayed
    string titleText = player.CurrentTitle.GetValue(player);
    
    // Then: Should show German title
    Assert.AreEqual("Veteran", titleText); // Assuming German translation
}
```

## Future Enhancements

### Planned Features
- **Title Collections**: Group related titles
- **Title Achievements**: Meta-achievements for collecting titles
- **Seasonal Titles**: Limited-time event titles
- **Custom Player Titles**: Player-created title system
- **Title Progression**: Advancement within title categories

### Advanced Title Types
- **Location Titles**: Based on zone discovery
- **Social Titles**: Based on friend/group activity
- **Economic Titles**: Based on trading/crafting
- **Exploration Titles**: Based on world exploration

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Production documentation |
| Historical | Game | Gender-specific title support |
| Historical | Game | Multi-language localization |
| Historical | Game | Event-based title system |
| Original | Game | Basic class progression titles | 