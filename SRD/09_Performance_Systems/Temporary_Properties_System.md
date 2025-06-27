# Temporary Properties System

**Document Status:** Core functionality documented
**Verification:** Code-verified from extensive TempProperties usage patterns
**Implementation Status:** Live

## Overview

**Game Rule Summary**: The game tracks temporary information about your character for things like spell effects, item cooldowns, and special abilities. This system remembers short-term conditions that aren't permanent parts of your character, like whether you're under the effect of a speed boost or if you've recently used a potion.

The Temporary Properties System provides flexible key-value storage for tracking temporary state across all game objects. This foundational system enables complex behaviors without requiring database persistence or permanent object modifications.

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

## Usage Patterns

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

### Security and Anti-Cheat Integration
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

// Event currency tracking
int currentAmount = player.TempProperties.GetProperty(currencyKey, 0);
player.TempProperties.SetProperty(currencyKey, currentAmount + amount);
```

### Spell Effect Management
```csharp
// Resurrection state
targetPlayer.TempProperties.SetProperty(RESURRECT_CASTER_PROPERTY, caster);
player.TempProperties.SetProperty(GamePlayer.RESURRECT_REZ_SICK_EFFECTIVENESS, 
    rezSickEffectiveness);

// Uninterruptible spells
Caster.TempProperties.SetProperty(WARLOCK_UNINTERRUPTABLE_SPELL, Spell);
```

## Common Property Keys

### Combat Properties
- `"Charging"` - Boolean for charge abilities
- `"ConvertDamage"` - Integer for damage conversion effects
- `"DAMAGE_ADD_PROPERTY"` - Damage add values
- `"BONUS_HP"` / `"BONUS_AF"` - Temporary bonuses

### Security Properties
- `"SPEED_VIOLATIONS"` - Speed hack detection counter
- `"SPAM_VIOLATIONS"` - Chat/command spam counter
- `"INACTIVITY_WARNING_SENT"` - Idle warning flag

### Event Properties
- `"event_xp_bonus"` - Event experience bonus expiration
- `"event_craft_bonus"` - Event crafting bonus expiration
- Event currency keys (dynamic strings)

### Effect Properties
- `RESURRECT_CASTER_PROPERTY` - Who cast resurrection
- `GamePlayer.RESURRECT_REZ_SICK_EFFECTIVENESS` - Resurrection penalty
- `WARLOCK_UNINTERRUPTABLE_SPELL` - Uninterruptible spell reference

## TODO: Missing Documentation

- Property serialization mechanisms for persistent properties
- Cross-region property transfer protocols
- Property validation and type safety systems
- Advanced cleanup strategies and memory management
- Performance metrics and monitoring tools
- Debug and inspection utilities

## References

- `GameServer/gameobjects/GameObject.cs` - Base TempProperties implementation
- `GameServer/serverproperty/ServerProperties.cs` - Registration configuration
- Various spell handlers and game systems for usage patterns 