# Temporary Property System

**Document Status:** Comprehensive Documentation  
**Verification:** Code-verified from TempProperties usage patterns  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: This system tracks all the temporary information about your character that the game needs to remember but doesn't permanently save. It handles things like spell effect timers, item cooldowns, event bonuses, and other short-term conditions that affect your gameplay experience.

The Temporary Property System provides a flexible key-value storage mechanism for tracking temporary state across all game objects. This system enables complex behaviors by storing transient data without requiring database persistence or permanent object modifications.

## Core Architecture

### Property Storage Interface
```csharp
public interface ITempPropertiesContainer
{
    void SetProperty<T>(string key, T value);
    T GetProperty<T>(string key);
    T GetProperty<T>(string key, T defaultValue);
    void RemoveProperty(string key);
    bool HasProperty(string key);
    IEnumerable<string> GetAllProperties();
}
```

### Property Registration System
```csharp
// Server configuration for persistent temp properties
[ServerProperty("system", "tempproperties_to_register", 
    "Serialized list of tempprop string, separated by semi-colon")]
public static string TEMPPROPERTIES_TO_REGISTER;

// Properties that survive logout/login
private static readonly string[] REGISTERED_PROPERTIES = {
    "LastPotionItemUsedTick",
    "SpellAvailableTime", 
    "ItemUseDelay",
    "LastChargeTime",
    "RESURRECT_REZ_SICK_EFFECTIVENESS"
};
```

## Common Usage Patterns

### Combat State Tracking
```csharp
// Track charge state for movement abilities
target.TempProperties.SetProperty("Charging", true);
bool isCharging = target.TempProperties.GetProperty<bool>("Charging");

// Track damage reduction effects
living.TempProperties.SetProperty("ConvertDamage", damageReduction);
int reduction = living.TempProperties.GetProperty<int>("ConvertDamage");

// Track bonus effects
living.TempProperties.SetProperty("BONUS_HP", bonusHP);
living.TempProperties.SetProperty("BONUS_AF", bonusAF);
```

### Timing and Cooldowns
```csharp
// Spell timing restrictions
player.TempProperties.SetProperty(GamePlayer.NEXT_SPELL_AVAIL_TIME_BECAUSE_USE_POTION, 
    GameLoop.GameLoopTime + potionCooldown);

// Quick cast timing
player.TempProperties.SetProperty(GamePlayer.QUICK_CAST_CHANGE_TICK, 
    player.CurrentRegion.Time);

// Stealth timing
player.TempProperties.SetProperty(GamePlayer.STEALTH_CHANGE_TICK, 
    player.CurrentRegion.Time);
```

### Security and Anti-Cheat
```csharp
// Speed violation tracking
var violations = player.TempProperties.GetProperty<int>("SPEED_VIOLATIONS");
player.TempProperties.SetProperty("SPEED_VIOLATIONS", violations + 1);

// Spam protection
var spamViolations = client.TempProperties.GetProperty<int>("SPAM_VIOLATIONS");
client.TempProperties.SetProperty("SPAM_VIOLATIONS", spamViolations + 1);

// Inactivity warnings
player.TempProperties.SetProperty("INACTIVITY_WARNING_SENT", true);
```

### Event System Integration
```csharp
// Event bonuses with expiration
player.TempProperties.SetProperty("event_xp_bonus", DateTime.Now.Add(duration));
player.TempProperties.SetProperty("event_craft_bonus", DateTime.Now.Add(duration));
player.TempProperties.SetProperty("event_speed_bonus", DateTime.Now.Add(duration));

// Event currency tracking
int currentAmount = player.TempProperties.GetProperty(currencyKey, 0);
player.TempProperties.SetProperty(currencyKey, currentAmount + amount);
```

### Crafting State Management
```csharp
// Salvage queue tracking
player.TempProperties.SetProperty(SALVAGE_QUEUE, itemList);

// Crafting progress
GamePlayer player = timer.Properties.GetProperty<GamePlayer>(
    AbstractCraftingSkill.PLAYER_CRAFTER);
```

### Spell Effect State
```csharp
// Resurrection state
targetPlayer.TempProperties.SetProperty(RESURRECT_CASTER_PROPERTY, caster);
player.TempProperties.SetProperty(GamePlayer.RESURRECT_REZ_SICK_EFFECTIVENESS, 
    rezSickEffectiveness);

// Effectiveness debuffs
player.TempProperties.SetProperty("PreEffectivenessDebuff", player.Effectiveness);

// Uninterruptible spells
Caster.TempProperties.SetProperty(WARLOCK_UNINTERRUPTABLE_SPELL, Spell);
```

### Object Relationships
```csharp
// Pet relationships
m_pet.TempProperties.SetProperty("target", target);
Caster.TempProperties.SetProperty(NoveltyPetBrain.HAS_PET, true);

// Relic carrying
player.TempProperties.SetProperty(PLAYER_CARRY_RELIC_WEAK, relic);
GameRelic relicOnPlayer = player.TempProperties.GetProperty<GameRelic>(
    PLAYER_CARRY_RELIC_WEAK);
```

### Command Spam Protection
```csharp
// Command cooldown tracking
long tick = player.TempProperties.GetProperty<long>(spamKey);
if (player.CurrentRegion.Time - tick < spamDelay)
{
    return; // Blocked by spam protection
}
player.TempProperties.SetProperty(spamKey, player.CurrentRegion.Time);
```

## Seasonal Event Integration

### Christmas Event Example
```csharp
// Christmas spirit tracking
giver.TempProperties.SetProperty("christmas_spirit", DateTime.Now.AddHours(24));
receiver.TempProperties.SetProperty("christmas_spirit", DateTime.Now.AddHours(24));

// Check for active spirit
if (player.TempProperties.GetProperty("christmas_spirit", DateTime.MinValue) > DateTime.Now)
{
    // Apply Christmas bonuses
}
```

### Event Currency System
```csharp
// Add event currency
public void AddEventCurrency(GamePlayer player, string currencyKey, int amount)
{
    int currentAmount = player.TempProperties.GetProperty(currencyKey, 0);
    player.TempProperties.SetProperty(currencyKey, currentAmount + amount);
}

// Spend event currency
public bool SpendEventCurrency(GamePlayer player, string currencyKey, int amount)
{
    int currentAmount = player.TempProperties.GetProperty(currencyKey, 0);
    if (currentAmount < amount)
        return false;
        
    player.TempProperties.SetProperty(currencyKey, currentAmount - amount);
    return true;
}
```

## Persistence Management

### Registration System
```csharp
// Automatic persistence for registered properties
private void SaveRegisteredProperties(GamePlayer player)
{
    foreach (string propertyKey in REGISTERED_PROPERTIES)
    {
        if (player.TempProperties.HasProperty(propertyKey))
        {
            var value = player.TempProperties.GetProperty<object>(propertyKey);
            SaveToDatabase(player.Character.ObjectId, propertyKey, value);
        }
    }
}

// Restore on login
private void LoadRegisteredProperties(GamePlayer player)
{
    foreach (string propertyKey in REGISTERED_PROPERTIES)
    {
        var value = LoadFromDatabase(player.Character.ObjectId, propertyKey);
        if (value != null)
        {
            player.TempProperties.SetProperty(propertyKey, value);
        }
    }
}
```

### Cleanup on Logout
```csharp
public void OnPlayerLogout(GamePlayer player)
{
    // Save registered properties
    SaveRegisteredProperties(player);
    
    // Clear non-persistent properties
    var allProperties = player.TempProperties.GetAllProperties().ToList();
    foreach (string key in allProperties)
    {
        if (!REGISTERED_PROPERTIES.Contains(key))
        {
            player.TempProperties.RemoveProperty(key);
        }
    }
}
```

## Performance Considerations

### Memory Management
- Properties are stored in ConcurrentDictionary for thread safety
- Automatic cleanup on object disposal
- Regular cleanup of expired time-based properties

### Property Naming Conventions
```csharp
// Standard naming patterns
public static class TempPropertyKeys
{
    // Timing properties
    public const string LAST_ACTION_TIME = "LastActionTime";
    public const string NEXT_AVAILABLE_TIME = "NextAvailableTime";
    
    // State flags
    public const string IS_CHARGING = "Charging";
    public const string HAS_WARNED = "HasWarned";
    
    // Effect tracking
    public const string EFFECT_TIMER = "EffectTimer";
    public const string EFFECT_PROPERTY = "EffectProperty";
    
    // Security tracking
    public const string VIOLATION_COUNT = "ViolationCount";
    public const string LAST_VIOLATION_TIME = "LastViolationTime";
}
```

### Property Type Safety
```csharp
// Type-safe property access helpers
public static class TempPropertyExtensions
{
    public static void SetTimer(this GameObject obj, string key, long gameTime)
    {
        obj.TempProperties.SetProperty(key, gameTime);
    }
    
    public static bool IsTimerExpired(this GameObject obj, string key, long currentTime)
    {
        long timerValue = obj.TempProperties.GetProperty<long>(key, 0);
        return currentTime >= timerValue;
    }
    
    public static void SetFlag(this GameObject obj, string key, bool value = true)
    {
        obj.TempProperties.SetProperty(key, value);
    }
    
    public static bool HasFlag(this GameObject obj, string key)
    {
        return obj.TempProperties.GetProperty<bool>(key, false);
    }
}
```

## Debug and Monitoring

### Property Inspection
```csharp
// Debug command to inspect player properties
[Cmd("viewprops", ePrivLevel.GM, "View player temporary properties")]
public class ViewPropsCommand : AbstractCommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        var player = client.Player;
        var properties = player.TempProperties.GetAllProperties();
        
        foreach (string key in properties)
        {
            var value = player.TempProperties.GetProperty<object>(key);
            client.Out.SendMessage($"{key}: {value}", eChatType.CT_System, 
                eChatLoc.CL_SystemWindow);
        }
    }
}
```

### Property Cleanup Service
```csharp
public class PropertyCleanupService : IGameService
{
    private readonly Timer _cleanupTimer;
    
    public void Start()
    {
        _cleanupTimer = new Timer(CleanupExpiredProperties, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }
    
    private void CleanupExpiredProperties(object state)
    {
        // Clean up time-based properties
        foreach (var player in WorldMgr.GetAllPlayers())
        {
            CleanupPlayerProperties(player);
        }
    }
}
```

## Common Property Keys

### Combat Properties
- `"Charging"` - Boolean for charge abilities
- `"ConvertDamage"` - Integer for damage conversion effects
- `"DAMAGE_ADD_PROPERTY"` - Damage add values
- `"BONUS_HP"` / `"BONUS_AF"` - Temporary bonuses

### Timing Properties
- `GamePlayer.NEXT_SPELL_AVAIL_TIME_BECAUSE_USE_POTION`
- `GamePlayer.QUICK_CAST_CHANGE_TICK`
- `GamePlayer.STEALTH_CHANGE_TICK`
- `"LastActionTime"` + ActionType

### Security Properties
- `"SPEED_VIOLATIONS"`
- `"SPAM_VIOLATIONS"`
- `"INACTIVITY_WARNING_SENT"`

### Event Properties
- `"event_xp_bonus"`
- `"event_craft_bonus"`
- `"event_speed_bonus"`
- Event currency keys (dynamic)

### Effect Properties
- `RESURRECT_CASTER_PROPERTY`
- `GamePlayer.RESURRECT_REZ_SICK_EFFECTIVENESS`
- `WARLOCK_UNINTERRUPTABLE_SPELL`
- `"PreEffectivenessDebuff"`

## TODO: Missing Documentation

- Property serialization mechanisms
- Cross-region property transfer
- Property validation systems
- Advanced cleanup strategies
- Performance metrics collection

## References

- `GameServer/gameobjects/GameObject.cs` - Base TempProperties implementation
- `GameServer/serverproperty/ServerProperties.cs` - Registration configuration
- `GameServer/gameobjects/GamePlayer.cs` - Player-specific property usage
- Various spell handlers for effect property patterns 