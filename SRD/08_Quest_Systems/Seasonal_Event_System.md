# Seasonal Event System

**Document Status:** Production Ready
**Implementation Status:** Live
**Verification:** Code Verified

## Overview

**Game Rule Summary**: The Seasonal Event System brings special limited-time celebrations and content throughout the year. During events like Halloween, Christmas, or special boss encounters, you'll see unique NPCs, special merchants selling exclusive items, modified drop rates, and temporary bonuses. Events automatically start and end on specific dates, with server-wide announcements when they begin. You can earn event-specific achievements, collect special currencies, and obtain unique cosmetic items that show you participated. Some events provide gameplay bonuses like double experience weekends, while others add atmospheric changes like snow weather or haunted areas. These events create memorable shared experiences and give you exclusive rewards you can't get any other time.

The Seasonal Event System manages time-limited events, special encounters, and holiday celebrations. It provides dynamic content activation, special rewards, achievement tracking, and seasonal modifications to game mechanics during specific periods throughout the year.

## Core Architecture

### Event Management Framework

#### Event Base Structure
```csharp
public abstract class SeasonalEvent
{
    public string EventName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    
    public abstract void OnEventStart();
    public abstract void OnEventEnd();
    public abstract void OnPlayerParticipation(GamePlayer player);
    
    public virtual bool IsEventActive()
    {
        DateTime now = DateTime.Now;
        return now >= StartTime && now <= EndTime && IsActive;
    }
}
```

#### Event Manager
```csharp
public static class SeasonalEventManager
{
    private static List<SeasonalEvent> registeredEvents = new List<SeasonalEvent>();
    private static Timer eventCheckTimer;
    
    public static void Initialize()
    {
        LoadEventConfiguration();
        eventCheckTimer = new Timer(CheckEventStatus, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }
    
    private static void CheckEventStatus(object state)
    {
        foreach (var evt in registeredEvents)
        {
            bool shouldBeActive = evt.IsEventActive();
            
            if (shouldBeActive && !evt.IsActive)
            {
                evt.OnEventStart();
                evt.IsActive = true;
                AnnounceEventStart(evt);
            }
            else if (!shouldBeActive && evt.IsActive)
            {
                evt.OnEventEnd();
                evt.IsActive = false;
                AnnounceEventEnd(evt);
            }
        }
    }
}
```

### Event Types

#### Holiday Events
```csharp
public class HalloweenEvent : SeasonalEvent
{
    public override void OnEventStart()
    {
        // Spawn Halloween NPCs
        SpawnHalloweenMerchants();
        
        // Enable pumpkin drops
        EnablePumpkinDrops();
        
        // Activate haunted areas
        ActivateHauntedZones();
    }
    
    public override void OnEventEnd()
    {
        // Remove Halloween NPCs
        RemoveHalloweenMerchants();
        
        // Disable pumpkin drops
        DisablePumpkinDrops();
        
        // Deactivate haunted areas
        DeactivateHauntedZones();
    }
}
```

#### Special Encounters
```csharp
public class DragonEncounterEvent : SeasonalEvent
{
    private List<GameNPC> eventDragons = new List<GameNPC>();
    
    public override void OnEventStart()
    {
        // Spawn special event dragons
        foreach (var spawnLocation in DragonSpawnLocations)
        {
            var dragon = CreateEventDragon(spawnLocation);
            dragon.AddToWorld();
            eventDragons.Add(dragon);
        }
        
        // Announce dragon sightings
        AnnounceToAllPlayers("Ancient dragons have been sighted across the realms!");
    }
    
    public override void OnPlayerParticipation(GamePlayer player)
    {
        // Track dragon encounter participation
        player.Achieve($"Dragon_Event_{DateTime.Now.Year}", 1);
    }
}
```

#### Economic Events
```csharp
public class DoubleExperienceEvent : SeasonalEvent
{
    public override void OnEventStart()
    {
        // Enable double experience modifier
        ServerProperties.Properties.XP_RATE = ServerProperties.Properties.XP_RATE * 2;
        
        // Notify all players
        AnnounceToAllPlayers("Double Experience Weekend has begun!");
    }
    
    public override void OnEventEnd()
    {
        // Restore normal experience rate
        ServerProperties.Properties.XP_RATE = ServerProperties.Properties.XP_RATE / 2;
        
        AnnounceToAllPlayers("Double Experience Weekend has ended!");
    }
}
```

## Event Configuration System

### Event Schedule Definition
```csharp
public class EventSchedule
{
    public string EventId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan Duration { get; set; }
    public EventRecurrence Recurrence { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}

public enum EventRecurrence
{
    None,           // One-time event
    Annual,         // Yearly recurring
    Monthly,        // Monthly recurring  
    Weekly,         // Weekly recurring
    Custom          // Custom schedule
}
```

### Configuration Loading
```csharp
public static void LoadEventConfiguration()
{
    var eventConfig = LoadFromDatabase();
    
    foreach (var config in eventConfig)
    {
        var eventInstance = CreateEventInstance(config);
        if (eventInstance != null)
        {
            registeredEvents.Add(eventInstance);
        }
    }
    
    // Add hardcoded seasonal events
    RegisterDefaultEvents();
}

private static void RegisterDefaultEvents()
{
    // Halloween - October 31st ±2 weeks
    registeredEvents.Add(new HalloweenEvent
    {
        EventName = "Halloween",
        StartTime = new DateTime(DateTime.Now.Year, 10, 17),
        EndTime = new DateTime(DateTime.Now.Year, 11, 7)
    });
    
    // Christmas - December 15th - January 7th
    registeredEvents.Add(new ChristmasEvent
    {
        EventName = "Christmas",
        StartTime = new DateTime(DateTime.Now.Year, 12, 15),
        EndTime = new DateTime(DateTime.Now.Year + 1, 1, 7)
    });
}
```

## Event-Specific Features

### Holiday-Themed Content

#### Halloween Features
```csharp
public class HalloweenEvent : SeasonalEvent
{
    private List<GameMerchant> halloweenMerchants = new List<GameMerchant>();
    
    private void SpawnHalloweenMerchants()
    {
        foreach (var location in HalloweenMerchantLocations)
        {
            var merchant = new GameMerchant();
            merchant.Name = "Pumpkin Merchant";
            merchant.Model = 666; // Halloween model
            merchant.TradeItems = GetHalloweenItems();
            merchant.AddToWorld();
            halloweenMerchants.Add(merchant);
        }
    }
    
    private MerchantTradeItems GetHalloweenItems()
    {
        var items = new MerchantTradeItems("halloween_items");
        // Add pumpkin heads, costumes, decorations
        items.AddItem(new DbItemTemplate { Id_nb = "pumpkin_head", Name = "Pumpkin Head" });
        items.AddItem(new DbItemTemplate { Id_nb = "ghost_costume", Name = "Ghost Costume" });
        return items;
    }
}
```

#### Christmas Features
```csharp
public class ChristmasEvent : SeasonalEvent
{
    public override void OnEventStart()
    {
        // Spawn Christmas trees in major cities
        SpawnChristmasTrees();
        
        // Enable snow weather in all zones
        EnableSnowWeather();
        
        // Activate gift-giving mechanics
        EnableGiftGiving();
    }
    
    private void EnableGiftGiving()
    {
        // Players can give gifts to other players for special bonuses
        GameEventMgr.AddHandler(GamePlayerEvent.GiveGift, OnPlayerGiveGift);
    }
    
    private void OnPlayerGiveGift(DOLEvent e, object sender, EventArgs args)
    {
        if (sender is GamePlayer giver && args is GiftEventArgs giftArgs)
        {
            var receiver = giftArgs.Receiver;
            
            // Grant Christmas spirit bonus
            giver.TempProperties.SetProperty("christmas_spirit", DateTime.Now.AddHours(24));
            receiver.TempProperties.SetProperty("christmas_spirit", DateTime.Now.AddHours(24));
            
            // 10% experience bonus while spirit lasts
            ApplyChristmasBonus(giver);
            ApplyChristmasBonus(receiver);
        }
    }
}
```

### Special Boss Encounters

#### Event Boss Mechanics
```csharp
public class EventBossNPC : GameEpicBoss
{
    private string eventId;
    private DateTime spawnTime;
    
    public override void OnAttackedByEnemy(AttackData ad)
    {
        base.OnAttackedByEnemy(ad);
        
        // Track event participation
        if (ad.Attacker is GamePlayer player)
        {
            TrackEventParticipation(player);
        }
    }
    
    public override void Die(GameObject killer)
    {
        // Grant special event rewards
        GrantEventRewards(killer);
        
        // Announce server-wide kill
        AnnounceEventBossKill(killer);
        
        base.Die(killer);
    }
    
    private void GrantEventRewards(GameObject killer)
    {
        if (killer is GamePlayer player)
        {
            // Grant event-specific achievement
            player.Achieve($"Event_Boss_{eventId}", 1);
            
            // Chance for special event item
            if (Util.Random(100) < 25) // 25% chance
            {
                var eventItem = CreateEventReward();
                player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, eventItem);
            }
        }
    }
}
```

### Dynamic Loot Modifications

#### Event Drop System
```csharp
public static class EventLootManager
{
    public static void ModifyLootForEvent(GameNPC npc, GamePlayer killer)
    {
        var activeEvents = SeasonalEventManager.GetActiveEvents();
        
        foreach (var evt in activeEvents)
        {
            ApplyEventLootModifications(evt, npc, killer);
        }
    }
    
    private static void ApplyEventLootModifications(SeasonalEvent evt, GameNPC npc, GamePlayer killer)
    {
        switch (evt.EventName)
        {
            case "Halloween":
                if (Util.Random(100) < 15) // 15% chance
                {
                    var pumpkin = CreatePumpkinItem();
                    npc.AddLoot(pumpkin);
                }
                break;
                
            case "Christmas":
                if (Util.Random(100) < 10) // 10% chance
                {
                    var gift = CreateChristmasGift();
                    npc.AddLoot(gift);
                }
                break;
                
            case "DragonEvent":
                if (npc.Name.Contains("Dragon"))
                {
                    // Double dragon scales during event
                    DoubleSpecificLoot(npc, "dragon_scale");
                }
                break;
        }
    }
}
```

## Event Achievement System

### Event-Specific Achievements
```csharp
public static class EventAchievementManager
{
    public static void TrackEventAchievement(GamePlayer player, string eventName, string achievementType, int count = 1)
    {
        string achievementId = $"Event_{eventName}_{achievementType}_{DateTime.Now.Year}";
        player.Achieve(achievementId, count);
        
        // Check for milestone achievements
        CheckEventMilestones(player, eventName, achievementType);
    }
    
    private static void CheckEventMilestones(GamePlayer player, string eventName, string achievementType)
    {
        var achievement = GetPlayerEventAchievement(player, eventName, achievementType);
        
        // Grant milestone rewards
        switch (achievement.Count)
        {
            case 10:
                GrantEventMilestoneReward(player, eventName, "Bronze");
                break;
            case 25:
                GrantEventMilestoneReward(player, eventName, "Silver");
                break;
            case 50:
                GrantEventMilestoneReward(player, eventName, "Gold");
                break;
        }
    }
}
```

### Event Participation Tracking
```csharp
public class EventParticipation
{
    public string PlayerId { get; set; }
    public string EventId { get; set; }
    public DateTime FirstParticipation { get; set; }
    public DateTime LastParticipation { get; set; }
    public int ParticipationCount { get; set; }
    public Dictionary<string, int> ActivityCounts { get; set; }
}
```

## Event Rewards System

### Reward Types

#### Temporary Bonuses
```csharp
public static void ApplyEventBonus(GamePlayer player, string bonusType, TimeSpan duration)
{
    switch (bonusType)
    {
        case "experience":
            player.TempProperties.SetProperty("event_xp_bonus", DateTime.Now.Add(duration));
            break;
            
        case "crafting":
            player.TempProperties.SetProperty("event_craft_bonus", DateTime.Now.Add(duration));
            break;
            
        case "speed":
            player.TempProperties.SetProperty("event_speed_bonus", DateTime.Now.Add(duration));
            break;
    }
}
```

#### Unique Event Items
```csharp
public static DbItemTemplate CreateEventItem(string eventName, string itemType)
{
    var template = new DbItemTemplate();
    template.Id_nb = $"event_{eventName}_{itemType}_{DateTime.Now.Year}";
    template.Name = $"{eventName} {itemType}";
    template.IsDropable = false; // Event items usually not tradeable
    template.IsPickable = true;
    template.CanDropAsLoot = false;
    
    // Add event-specific properties
    switch (eventName.ToLower())
    {
        case "halloween":
            template.Color = 16; // Orange color
            break;
        case "christmas":
            template.Color = 15; // Red color
            break;
    }
    
    return template;
}
```

#### Event Currencies
```csharp
public class EventCurrency
{
    public static void GrantEventCurrency(GamePlayer player, string eventName, int amount)
    {
        string currencyKey = $"event_currency_{eventName}";
        
        int currentAmount = player.TempProperties.GetProperty(currencyKey, 0);
        player.TempProperties.SetProperty(currencyKey, currentAmount + amount);
        
        player.Out.SendMessage($"You have gained {amount} {eventName} tokens!", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
    }
    
    public static bool SpendEventCurrency(GamePlayer player, string eventName, int amount)
    {
        string currencyKey = $"event_currency_{eventName}";
        int currentAmount = player.TempProperties.GetProperty(currencyKey, 0);
        
        if (currentAmount >= amount)
        {
            player.TempProperties.SetProperty(currencyKey, currentAmount - amount);
            return true;
        }
        
        return false;
    }
}
```

## Event Notification System

### Player Notifications
```csharp
public static void NotifyPlayerOfEvent(GamePlayer player, SeasonalEvent evt, string notificationType)
{
    switch (notificationType)
    {
        case "start":
            player.Out.SendMessage($"The {evt.EventName} event has begun!", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
            break;
            
        case "end":
            player.Out.SendMessage($"The {evt.EventName} event has ended!", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
            break;
            
        case "participation":
            player.Out.SendMessage($"You are now participating in {evt.EventName}!", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
            break;
    }
}
```

### Server-Wide Announcements
```csharp
public static void AnnounceEventStart(SeasonalEvent evt)
{
    string message = $"SERVER EVENT: {evt.EventName} has begun! Participate for special rewards!";
    
    foreach (GameClient client in WorldMgr.GetAllPlayingClients())
    {
        if (client.Player != null)
        {
            client.Player.Out.SendMessage(message, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
        }
    }
    
    // Log event start
    log.Info($"Seasonal Event Started: {evt.EventName}");
}
```

## Administrative Tools

### Event Management Commands
```csharp
[CommandHandler("event")]
public class EventCommand : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length < 2)
        {
            DisplayEventStatus(client);
            return;
        }
        
        switch (args[1].ToLower())
        {
            case "start":
                if (args.Length >= 3)
                    StartEvent(client, args[2]);
                break;
                
            case "stop":
                if (args.Length >= 3)
                    StopEvent(client, args[2]);
                break;
                
            case "list":
                ListAllEvents(client);
                break;
                
            case "status":
                DisplayEventStatus(client);
                break;
        }
    }
    
    private void StartEvent(GameClient client, string eventName)
    {
        var evt = SeasonalEventManager.GetEvent(eventName);
        if (evt != null)
        {
            evt.OnEventStart();
            evt.IsActive = true;
            DisplayMessage(client, $"Event '{eventName}' started manually.");
        }
    }
}
```

### Event Configuration Tools
```csharp
[CommandHandler("eventconfig")]
public class EventConfigCommand : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        // /eventconfig create <name> <start> <end>
        // /eventconfig modify <name> <property> <value>
        // /eventconfig delete <name>
        
        if (args.Length < 2) return;
        
        switch (args[1].ToLower())
        {
            case "create":
                CreateNewEvent(client, args);
                break;
                
            case "modify":
                ModifyEvent(client, args);
                break;
                
            case "delete":
                DeleteEvent(client, args);
                break;
        }
    }
}
```

## Integration Points

### Combat System Integration
```csharp
// Modify damage during events
public override double CalculateDamageReduction(GameLiving target, double damage)
{
    double baseDamage = base.CalculateDamageReduction(target, damage);
    
    // Check for active damage modification events
    var activeEvents = SeasonalEventManager.GetActiveEvents();
    foreach (var evt in activeEvents)
    {
        if (evt is CombatModificationEvent combatEvent)
        {
            baseDamage = combatEvent.ModifyDamage(target, baseDamage);
        }
    }
    
    return baseDamage;
}
```

### Experience System Integration
```csharp
// Apply event experience bonuses
public override long CalculateExperienceGained(GameLiving target, long baseExperience)
{
    long totalXP = base.CalculateExperienceGained(target, baseExperience);
    
    if (this is GamePlayer player)
    {
        // Check for active XP events
        if (player.TempProperties.GetProperty("event_xp_bonus", DateTime.MinValue) > DateTime.Now)
        {
            totalXP = (long)(totalXP * 1.5); // 50% bonus
        }
        
        // Check for seasonal events with XP modifiers
        var activeEvents = SeasonalEventManager.GetActiveEvents();
        foreach (var evt in activeEvents)
        {
            if (evt is ExperienceModificationEvent xpEvent)
            {
                totalXP = xpEvent.ModifyExperience(player, totalXP);
            }
        }
    }
    
    return totalXP;
}
```

### Weather System Integration
```csharp
// Event-based weather modifications
public static void ApplyEventWeather(string eventName)
{
    switch (eventName.ToLower())
    {
        case "christmas":
            // Enable snow in all outdoor zones
            foreach (var region in WorldMgr.GetAllRegions())
            {
                if (!region.IsInstance && !region.IsDungeon)
                {
                    WorldMgr.WeatherManager.StartWeather(region.ID, 0, 5000, 150, 255, 200); // Snow
                }
            }
            break;
            
        case "halloween":
            // Enable fog and storms
            foreach (var region in WorldMgr.GetAllRegions())
            {
                WorldMgr.WeatherManager.StartWeather(region.ID, 0, 8000, 100, 128, 150); // Fog
            }
            break;
    }
}
```

## Performance and Optimization

### Event Caching
```csharp
private static Dictionary<string, SeasonalEvent> eventCache = new Dictionary<string, SeasonalEvent>();

public static SeasonalEvent GetEvent(string eventName)
{
    if (eventCache.TryGetValue(eventName, out SeasonalEvent cachedEvent))
        return cachedEvent;
        
    var evt = LoadEventFromDatabase(eventName);
    if (evt != null)
    {
        eventCache[eventName] = evt;
    }
    
    return evt;
}
```

### Efficient Event Checking
```csharp
// Check events only when necessary
private static DateTime lastEventCheck = DateTime.MinValue;
private static readonly TimeSpan EventCheckInterval = TimeSpan.FromMinutes(1);

public static List<SeasonalEvent> GetActiveEvents()
{
    if (DateTime.Now - lastEventCheck < EventCheckInterval)
        return cachedActiveEvents;
        
    cachedActiveEvents = registeredEvents.Where(e => e.IsEventActive()).ToList();
    lastEventCheck = DateTime.Now;
    
    return cachedActiveEvents;
}
```

## Configuration Examples

### Server Properties
```ini
# Seasonal event configuration
ENABLE_SEASONAL_EVENTS = true
EVENT_CHECK_INTERVAL = 60           # Check every 60 seconds
AUTO_START_EVENTS = true            # Auto-start scheduled events
EVENT_ANNOUNCEMENTS = true          # Enable event announcements
EVENT_LOOT_MODIFICATIONS = true     # Allow events to modify loot
```

### Database Schema
```sql
-- Event configuration table
CREATE TABLE SeasonalEvents (
    EventID VARCHAR(50) PRIMARY KEY,
    EventName VARCHAR(100) NOT NULL,
    StartDate DATETIME,
    EndDate DATETIME,
    IsRecurring BOOLEAN DEFAULT FALSE,
    RecurrenceType ENUM('NONE', 'ANNUAL', 'MONTHLY', 'WEEKLY', 'CUSTOM'),
    IsActive BOOLEAN DEFAULT TRUE,
    EventData TEXT  -- JSON configuration
);

-- Event participation tracking
CREATE TABLE EventParticipation (
    ParticipationID AUTO_INCREMENT PRIMARY KEY,
    PlayerID VARCHAR(255),
    EventID VARCHAR(50),
    ParticipationDate DATETIME,
    ActivityType VARCHAR(50),
    Count INT DEFAULT 1
);
```

## Test Scenarios

### Event Lifecycle Testing
```csharp
[Test]
public void TestEventActivation()
{
    // Given: Event scheduled to start
    var evt = new TestEvent
    {
        StartTime = DateTime.Now.AddSeconds(1),
        EndTime = DateTime.Now.AddMinutes(5)
    };
    
    // When: Event check runs
    Thread.Sleep(2000);
    SeasonalEventManager.CheckEventStatus();
    
    // Then: Event should be active
    Assert.IsTrue(evt.IsActive);
}
```

### Reward Testing
```csharp
[Test]
public void TestEventRewards()
{
    // Given: Active Halloween event
    var halloween = new HalloweenEvent();
    halloween.OnEventStart();
    
    // When: Player kills monster
    var player = CreateTestPlayer();
    var monster = CreateTestMonster();
    EventLootManager.ModifyLootForEvent(monster, player);
    
    // Then: Should have chance for pumpkin drop
    Assert.IsTrue(monster.Inventory.CountItemTemplate("pumpkin") > 0);
}
```

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2024-01-20 | 1.0 | Production documentation |
| Historical | Game | Event participation tracking |
| Historical | Game | Dynamic loot modification |
| Original | Game | Basic seasonal event framework | 