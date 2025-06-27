# Configuration Management System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

**Game Rule Summary**: The Configuration Management System controls all the server settings that affect your gameplay experience, from combat damage multipliers to experience rates to weather patterns. These settings let server administrators adjust how the game feels - they can increase experience gain for faster leveling, modify PvP damage for more balanced combat, or change loot drop rates for better rewards. Many settings can be changed while the server is running, so administrators can fine-tune the game based on player feedback without requiring restarts. Understanding these configurations helps explain why different servers might feel different - one server might have 2x experience while another focuses on challenging, slower progression.

The Configuration Management System provides comprehensive settings management for OpenDAoC through server properties, XML configuration files, and runtime configuration updates. It handles everything from combat parameters to database connections with type safety, validation, and hot-reloading capabilities.

## Core Architecture

### Base Configuration System

```csharp
public abstract class BaseServerConfig
{
    protected Dictionary<string, ConfigElement> _elements = new();
    
    public ConfigElement this[string key]
    {
        get => _elements.GetValueOrDefault(key);
        set => _elements[key] = value;
    }
    
    public void LoadFromXMLFile(FileInfo configFile)
    {
        var xmlConfig = XmlConfigFile.ParseXMLFile(configFile);
        ApplyConfiguration(xmlConfig);
    }
    
    public void SaveToXMLFile(FileInfo configFile)
    {
        var xmlConfig = CreateXmlConfiguration();
        xmlConfig.Save(configFile);
    }
}

public class GameServerConfiguration : BaseServerConfig
{
    public string ServerName { get; set; } = "OpenDAoC Server";
    public string ServerNameShort { get; set; } = "OpenDAoC";
    public int Port { get; set; } = 10300;
    public int UDPPort { get; set; } = 10400;
    public string IP { get; set; } = "any";
    
    // Database configuration
    public string DatabaseConnectionString { get; set; }
    public string DatabaseType { get; set; } = "MySQL";
    public bool DatabaseAutoCreate { get; set; } = true;
    
    // Logging configuration
    public string LogConfigFile { get; set; } = "config/logconfig.xml";
    
    // Performance settings
    public int MaxClientCount { get; set; } = 500;
    public int CPUUse { get; set; } = 2;
    public bool EnableCompilation { get; set; } = true;
    
    public GameServerConfiguration() : base()
    {
        LoadDefaults();
    }
}
```

### XML Configuration System

```csharp
public class XmlConfigFile : ConfigElement
{
    public void Save(FileInfo configFile)
    {
        var doc = new XmlDocument();
        var root = doc.CreateElement("configuration");
        doc.AppendChild(root);
        
        foreach (var child in Children)
        {
            AppendElement(doc, root, child.Key, child.Value);
        }
        
        doc.Save(configFile.FullName);
    }
    
    public static XmlConfigFile ParseXMLFile(FileInfo configFile)
    {
        var config = new XmlConfigFile();
        
        if (!configFile.Exists)
            return config;
            
        var doc = new XmlDocument();
        doc.Load(configFile.FullName);
        
        foreach (XmlNode node in doc.DocumentElement.ChildNodes)
        {
            ParseNode(config, node);
        }
        
        return config;
    }
    
    private static void ParseNode(ConfigElement parent, XmlNode node)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            var element = new ConfigElement(parent) { Value = node.InnerText };
            
            foreach (XmlAttribute attr in node.Attributes)
            {
                element.Attributes[attr.Name] = attr.Value;
            }
            
            parent.Children[node.Name] = element;
            
            foreach (XmlNode childNode in node.ChildNodes)
            {
                ParseNode(element, childNode);
            }
        }
    }
}
```

## Server Properties System

### Property Declaration

```csharp
public static class ServerProperties
{
    // Server configuration
    [ServerProperty("server", "name", "OpenDAoC Server")]
    public static string SERVER_NAME;
    
    [ServerProperty("server", "port", 10300)]
    public static int PORT;
    
    [ServerProperty("server", "max_players", 500)]
    public static int MAX_PLAYERS;
    
    // Combat properties
    [ServerProperty("combat", "pvp_damage_modifier", 1.0)]
    public static double PVP_DAMAGE_MODIFIER;
    
    [ServerProperty("combat", "pve_damage_modifier", 1.0)]
    public static double PVE_DAMAGE_MODIFIER;
    
    [ServerProperty("combat", "miss_reduction_per_attacker", 0.03)]
    public static double MISS_REDUCTION_PER_ATTACKERS;
    
    [ServerProperty("combat", "evade_cap", 50)]
    public static int EVADE_CAP;
    
    [ServerProperty("combat", "parry_cap", 50)]
    public static int PARRY_CAP;
    
    // Character progression
    [ServerProperty("character", "experience_multiplier", 1.0)]
    public static double EXPERIENCE_MULTIPLIER;
    
    [ServerProperty("character", "realm_points_multiplier", 1.0)]
    public static double REALM_POINTS_MULTIPLIER;
    
    [ServerProperty("character", "bounty_points_multiplier", 1.0)]
    public static double BOUNTY_POINTS_MULTIPLIER;
    
    // Loot generation
    [ServerProperty("loot", "aurulite_base_chance", 5)]
    public static int LOOTGENERATOR_AURULITE_BASE_CHANCE;
    
    [ServerProperty("loot", "aurulite_amount_ratio", 1.5)]
    public static double LOOTGENERATOR_AURULITE_AMOUNT_RATIO;
    
    [ServerProperty("loot", "dragon_scales_chance", 10)]
    public static int LOOTGENERATOR_DRAGONSCALES_BASE_CHANCE;
    
    // Weather system
    [ServerProperty("weather", "check_interval", 300000)]
    public static int WEATHER_CHECK_INTERVAL;
    
    [ServerProperty("weather", "chance", 30)]
    public static int WEATHER_CHANCE;
    
    [ServerProperty("weather", "log_events", false)]
    public static bool WEATHER_LOG_EVENTS;
    
    // Performance settings
    [ServerProperty("performance", "tick_rate", 100)]
    public static int TICK_RATE;
    
    [ServerProperty("performance", "max_objects_per_region", 10000)]
    public static int MAX_OBJECTS_PER_REGION;
    
    [ServerProperty("performance", "enable_object_pooling", true)]
    public static bool ENABLE_OBJECT_POOLING;
}
```

### Property Loading System

```csharp
public class PropertyLoader
{
    private static readonly Dictionary<string, PropertyInfo> _properties = new();
    private static readonly Dictionary<string, object> _values = new();
    
    static PropertyLoader()
    {
        LoadPropertyDefinitions();
    }
    
    private static void LoadPropertyDefinitions()
    {
        var type = typeof(ServerProperties);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
        
        foreach (var field in fields)
        {
            var attr = field.GetCustomAttribute<ServerPropertyAttribute>();
            if (attr != null)
            {
                var key = $"{attr.Category}.{attr.Name}";
                _properties[key] = new PropertyInfo
                {
                    Field = field,
                    Category = attr.Category,
                    Name = attr.Name,
                    DefaultValue = attr.DefaultValue,
                    Type = field.FieldType
                };
            }
        }
    }
    
    public static void LoadFromDatabase()
    {
        var properties = GameServer.Database.SelectAllObjects<DbServerProperty>();
        
        foreach (var prop in properties)
        {
            var key = $"{prop.Category}.{prop.Key}";
            if (_properties.TryGetValue(key, out var propInfo))
            {
                var value = ConvertValue(prop.Value, propInfo.Type);
                SetPropertyValue(propInfo.Field, value);
                _values[key] = value;
            }
        }
    }
    
    public static void SaveToDatabase()
    {
        foreach (var kvp in _properties)
        {
            var propInfo = kvp.Value;
            var currentValue = propInfo.Field.GetValue(null);
            
            var dbProp = new DbServerProperty
            {
                Category = propInfo.Category,
                Key = propInfo.Name,
                Value = currentValue?.ToString() ?? "",
                DefaultValue = propInfo.DefaultValue?.ToString() ?? ""
            };
            
            GameServer.Database.SaveObject(dbProp);
        }
    }
}
```

### Property Validation

```csharp
public class PropertyValidator
{
    private static readonly Dictionary<string, Func<object, bool>> _validators = new()
    {
        ["server.port"] = value => (int)value > 0 && (int)value <= 65535,
        ["server.max_players"] = value => (int)value > 0 && (int)value <= 10000,
        ["combat.pvp_damage_modifier"] = value => (double)value >= 0.1 && (double)value <= 10.0,
        ["combat.evade_cap"] = value => (int)value >= 0 && (int)value <= 100,
        ["weather.check_interval"] = value => (int)value >= 1000,
        ["weather.chance"] = value => (int)value >= 0 && (int)value <= 100
    };
    
    public static bool ValidateProperty(string key, object value)
    {
        if (_validators.TryGetValue(key, out var validator))
        {
            try
            {
                return validator(value);
            }
            catch
            {
                return false;
            }
        }
        
        return true; // No specific validation, assume valid
    }
    
    public static string GetValidationError(string key, object value)
    {
        if (!ValidateProperty(key, value))
        {
            return GetValidationMessage(key);
        }
        return null;
    }
    
    private static string GetValidationMessage(string key)
    {
        return key switch
        {
            "server.port" => "Port must be between 1 and 65535",
            "server.max_players" => "Max players must be between 1 and 10000",
            "combat.pvp_damage_modifier" => "PvP damage modifier must be between 0.1 and 10.0",
            "combat.evade_cap" => "Evade cap must be between 0 and 100",
            "weather.check_interval" => "Weather check interval must be at least 1000ms",
            "weather.chance" => "Weather chance must be between 0 and 100",
            _ => "Invalid value for property"
        };
    }
}
```

## Runtime Configuration Management

### Hot Reload System

```csharp
public class ConfigurationHotReload
{
    private readonly FileSystemWatcher _watcher;
    private readonly string _configPath;
    
    public event Action<string, object, object> PropertyChanged;
    
    public ConfigurationHotReload(string configPath)
    {
        _configPath = configPath;
        _watcher = new FileSystemWatcher(Path.GetDirectoryName(configPath))
        {
            Filter = Path.GetFileName(configPath),
            NotifyFilter = NotifyFilters.LastWrite
        };
        
        _watcher.Changed += OnConfigFileChanged;
        _watcher.EnableRaisingEvents = true;
    }
    
    private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Debounce multiple rapid changes
            Thread.Sleep(100);
            
            var oldValues = CaptureCurrentValues();
            LoadConfiguration();
            var newValues = CaptureCurrentValues();
            
            foreach (var key in oldValues.Keys.Union(newValues.Keys))
            {
                var oldValue = oldValues.GetValueOrDefault(key);
                var newValue = newValues.GetValueOrDefault(key);
                
                if (!Equals(oldValue, newValue))
                {
                    PropertyChanged?.Invoke(key, oldValue, newValue);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error reloading configuration: {ex}");
        }
    }
}
```

### Property Change Notifications

```csharp
public class PropertyChangeHandler
{
    private static readonly Dictionary<string, List<Action<object, object>>> _handlers = new();
    
    public static void RegisterHandler(string propertyKey, Action<object, object> handler)
    {
        if (!_handlers.ContainsKey(propertyKey))
        {
            _handlers[propertyKey] = new List<Action<object, object>>();
        }
        _handlers[propertyKey].Add(handler);
    }
    
    public static void NotifyPropertyChanged(string propertyKey, object oldValue, object newValue)
    {
        if (_handlers.TryGetValue(propertyKey, out var handlers))
        {
            foreach (var handler in handlers)
            {
                try
                {
                    handler(oldValue, newValue);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error handling property change for {propertyKey}: {ex}");
                }
            }
        }
    }
    
    static PropertyChangeHandler()
    {
        // Register built-in handlers
        RegisterHandler("combat.pvp_damage_modifier", OnCombatModifierChanged);
        RegisterHandler("weather.check_interval", OnWeatherConfigChanged);
        RegisterHandler("performance.tick_rate", OnTickRateChanged);
    }
    
    private static void OnCombatModifierChanged(object oldValue, object newValue)
    {
        Log.Info($"PvP damage modifier changed from {oldValue} to {newValue}");
        // Notify combat system of changes
        CombatManager.RefreshConfiguration();
    }
    
    private static void OnWeatherConfigChanged(object oldValue, object newValue)
    {
        Log.Info($"Weather configuration changed from {oldValue} to {newValue}");
        WeatherManager.RefreshConfiguration();
    }
}
```

## Configuration Categories

### Combat Configuration

```csharp
public class CombatConfiguration
{
    [ServerProperty("combat", "base_miss_chance", 0.15)]
    public static double BASE_MISS_CHANCE;
    
    [ServerProperty("combat", "fumble_chance_level_1", 0.05)]
    public static double FUMBLE_CHANCE_LEVEL_1;
    
    [ServerProperty("combat", "critical_hit_cap", 0.50)]
    public static double CRITICAL_HIT_CAP;
    
    [ServerProperty("combat", "damage_cap_multiplier", 3.0)]
    public static double DAMAGE_CAP_MULTIPLIER;
    
    [ServerProperty("combat", "archery_miss_penalty", 0.15)]
    public static double ARCHERY_MISS_PENALTY;
    
    [ServerProperty("combat", "dual_wield_penalty", 0.625)]
    public static double DUAL_WIELD_PENALTY;
    
    [ServerProperty("combat", "style_fatigue_cost", 1.0)]
    public static double STYLE_FATIGUE_COST;
    
    [ServerProperty("combat", "spell_interrupt_duration", 4000)]
    public static int SPELL_INTERRUPT_DURATION;
    
    [ServerProperty("combat", "resist_cap_primary", 70)]
    public static int RESIST_CAP_PRIMARY;
    
    [ServerProperty("combat", "resist_cap_secondary", 80)]
    public static int RESIST_CAP_SECONDARY;
}
```

### Database Configuration

```csharp
public class DatabaseConfiguration
{
    [ServerProperty("database", "connection_string", "")]
    public static string CONNECTION_STRING;
    
    [ServerProperty("database", "type", "MySQL")]
    public static string DATABASE_TYPE;
    
    [ServerProperty("database", "auto_create", true)]
    public static bool AUTO_CREATE;
    
    [ServerProperty("database", "connection_timeout", 30)]
    public static int CONNECTION_TIMEOUT;
    
    [ServerProperty("database", "command_timeout", 120)]
    public static int COMMAND_TIMEOUT;
    
    [ServerProperty("database", "max_connections", 100)]
    public static int MAX_CONNECTIONS;
    
    [ServerProperty("database", "enable_logging", false)]
    public static bool ENABLE_LOGGING;
}
```

### Network Configuration

```csharp
public class NetworkConfiguration
{
    [ServerProperty("network", "tcp_timeout", 30000)]
    public static int TCP_TIMEOUT;
    
    [ServerProperty("network", "udp_timeout", 10000)]
    public static int UDP_TIMEOUT;
    
    [ServerProperty("network", "ping_timeout", 30000)]
    public static int PING_TIMEOUT;
    
    [ServerProperty("network", "max_packet_size", 2048)]
    public static int MAX_PACKET_SIZE;
    
    [ServerProperty("network", "packet_compression", true)]
    public static bool PACKET_COMPRESSION;
    
    [ServerProperty("network", "packet_encryption", true)]
    public static bool PACKET_ENCRYPTION;
    
    [ServerProperty("network", "rate_limit_enabled", true)]
    public static bool RATE_LIMIT_ENABLED;
    
    [ServerProperty("network", "rate_limit_packets_per_second", 100)]
    public static int RATE_LIMIT_PACKETS_PER_SECOND;
}
```

## Command-Line Configuration

### Configuration Commands

```csharp
[Cmd("&config", ePrivLevel.Admin)]
public class ConfigurationCommand : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length < 2)
        {
            DisplayUsage(client);
            return;
        }
        
        switch (args[1].ToLower())
        {
            case "list":
                ListProperties(client, args.Length > 2 ? args[2] : null);
                break;
                
            case "get":
                if (args.Length > 2)
                    GetProperty(client, args[2]);
                break;
                
            case "set":
                if (args.Length > 3)
                    SetProperty(client, args[2], args[3]);
                break;
                
            case "reload":
                ReloadConfiguration(client);
                break;
                
            case "save":
                SaveConfiguration(client);
                break;
                
            default:
                DisplayUsage(client);
                break;
        }
    }
    
    private void ListProperties(GameClient client, string category)
    {
        var properties = PropertyLoader.GetProperties(category);
        
        client.Out.SendMessage($"Configuration Properties" + 
            (category != null ? $" (Category: {category})" : ""), 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
            
        foreach (var prop in properties)
        {
            var value = PropertyLoader.GetPropertyValue(prop.Key);
            client.Out.SendMessage($"  {prop.Key} = {value} (default: {prop.DefaultValue})", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
    
    private void SetProperty(GameClient client, string key, string value)
    {
        try
        {
            var oldValue = PropertyLoader.GetPropertyValue(key);
            PropertyLoader.SetPropertyValue(key, value);
            
            client.Out.SendMessage($"Property {key} changed from {oldValue} to {value}", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
        catch (Exception ex)
        {
            client.Out.SendMessage($"Error setting property {key}: {ex.Message}", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
```

## Configuration Profiles

### Environment-Based Configuration

```csharp
public class ConfigurationProfileManager
{
    private static readonly Dictionary<string, GameServerConfiguration> _profiles = new();
    
    static ConfigurationProfileManager()
    {
        LoadProfiles();
    }
    
    private static void LoadProfiles()
    {
        _profiles["development"] = new GameServerConfiguration
        {
            ServerName = "OpenDAoC Development",
            DatabaseAutoCreate = true,
            LogLevel = LogLevel.Debug,
            EnableCompilation = true,
            // Development-specific settings
            ExperienceMultiplier = 5.0,
            RealmPointsMultiplier = 3.0,
            MaxPlayers = 50
        };
        
        _profiles["production"] = new GameServerConfiguration
        {
            ServerName = "OpenDAoC Live",
            DatabaseAutoCreate = false,
            LogLevel = LogLevel.Info,
            EnableCompilation = false,
            // Production settings
            ExperienceMultiplier = 1.0,
            RealmPointsMultiplier = 1.0,
            MaxPlayers = 1000
        };
        
        _profiles["testing"] = new GameServerConfiguration
        {
            ServerName = "OpenDAoC Test",
            DatabaseAutoCreate = true,
            LogLevel = LogLevel.Debug,
            EnableCompilation = true,
            // Testing settings
            ExperienceMultiplier = 10.0,
            RealmPointsMultiplier = 5.0,
            MaxPlayers = 20
        };
    }
    
    public static GameServerConfiguration GetProfile(string profileName)
    {
        return _profiles.GetValueOrDefault(profileName.ToLower()) ?? _profiles["development"];
    }
    
    public static void ApplyProfile(string profileName)
    {
        var profile = GetProfile(profileName);
        ApplyConfiguration(profile);
        Log.Info($"Applied configuration profile: {profileName}");
    }
}
```

## Security and Validation

### Configuration Security

```csharp
public class ConfigurationSecurity
{
    private static readonly HashSet<string> _sensitiveProperties = new()
    {
        "database.connection_string",
        "database.password",
        "network.encryption_key",
        "admin.password_hash"
    };
    
    public static bool IsSensitiveProperty(string propertyKey)
    {
        return _sensitiveProperties.Contains(propertyKey.ToLower());
    }
    
    public static string MaskSensitiveValue(string propertyKey, string value)
    {
        if (IsSensitiveProperty(propertyKey))
        {
            return new string('*', Math.Min(8, value.Length));
        }
        return value;
    }
    
    public static bool CanModifyProperty(GameClient client, string propertyKey)
    {
        // Only allow admins to modify sensitive properties
        if (IsSensitiveProperty(propertyKey))
        {
            return client.Account.PrivLevel >= (uint)ePrivLevel.Admin;
        }
        
        // Allow GMs to modify most properties
        return client.Account.PrivLevel >= (uint)ePrivLevel.GM;
    }
}
```

## Implementation Status

**Completed**:
- ✅ Core configuration framework
- ✅ XML configuration loading/saving
- ✅ Server properties system
- ✅ Property validation
- ✅ Hot reload capability
- ✅ Command-line configuration management

**In Progress**:
- 🔄 Advanced validation rules
- 🔄 Configuration profiles
- 🔄 Security enhancements

**Planned**:
- ⏳ Web-based configuration interface
- ⏳ Configuration versioning
- ⏳ Distributed configuration sync

## References

- **Core Configuration**: `CoreBase/Configs/`
- **Server Properties**: `GameServer/serverproperty/ServerProperties.cs`
- **Game Configuration**: `GameServer/GameServerConfiguration.cs`
- **XML Handling**: `CoreBase/Configs/XmlConfigFile.cs` 