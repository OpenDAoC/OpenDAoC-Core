# Weather System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Stable
- **Verification**: Code-verified from WeatherManager.cs, RegionWeather.cs
- **Implementation**: Stable

## Overview
The weather system simulates dynamic weather patterns in outdoor regions including fog, rain, and storm effects. Weather moves across regions from west to east with configurable intensity, speed, and coverage area.

## Core Mechanics

### Weather Properties

#### Physical Characteristics
```csharp
public class RegionWeather
{
    public uint Position { get; }      // X-axis position (west to east)
    public uint Width { get; }         // Storm width (min 15000 units)
    public ushort Speed { get; }       // Movement speed (units/second, min 100)
    public ushort Intensity { get; }   // Rain/fog density (max 120)
    public ushort FogDiffusion { get; }// Fog spread (min 16000)
    public long StartTime { get; }     // When weather started
    public long DueTime { get; }       // When weather ends
}
```

#### Weather Limits
- **Intensity**: 0-120 (values above 120 cause visual artifacts)
- **Width**: Minimum 15,000 game units
- **Speed**: Minimum 100 units/second
- **Fog Diffusion**: Minimum 16,000

### Weather Generation

#### Random Weather Creation
```csharp
public void CreateWeather(long StartTime)
{
    CreateWeather(
        Width: (uint)Util.Random(25000, 90000),        // 25k-90k units wide
        Speed: (ushort)Util.Random(100, 700),          // 100-700 units/s
        Intensity: (ushort)Util.Random(30, 110),       // 30-110 intensity
        FogDiffusion: (ushort)Util.Random(16000, 32000), // 16k-32k diffusion
        StartTime: StartTime
    );
}
```

#### Weather Movement
- Always moves west to east (increasing X coordinate)
- Affects entire Y-axis (north-south) of region
- Duration calculated based on region width and speed

### Weather Manager

#### Region Registration
```csharp
public class WeatherManager
{
    // Automatic registration when region starts
    private void OnRegionStart(DOLEvent e, object sender, EventArgs arguments)
    {
        var region = sender as Region;
        if (region != null && !region.IsDungeon)
            RegisterRegion(region);
    }
}
```

#### Weather Scheduling
```csharp
// Default check interval from server properties
WEATHER_CHECK_INTERVAL = Math.Max(1000, ServerProperties.Properties.WEATHER_CHECK_INTERVAL);

// Weather chance per check
WEATHER_CHANCE = Math.Min(99, ServerProperties.Properties.WEATHER_CHANCE);

// Weather tick logic
private int OnWeatherTick(ushort regionId)
{
    if (!Util.Chance(DefaultWeatherChance))
    {
        // Clear weather if active
        if (weather.StartTime != 0)
            StopWeather(weather);
        return DefaultTimerInterval;
    }
    
    // Create new weather
    weather.CreateWeather(SimpleScheduler.Ticks);
    StartWeather(weather);
    return weather.Duration;
}
```

### Weather Updates

#### Player Notification
```csharp
private void SendWeatherUpdate(RegionWeather weather, GamePlayer player)
{
    if (weather.StartTime == 0)
        // Clear weather
        player.Out.SendWeather(0, 0, 0, 0, 0);
    else
        // Active weather with current position
        player.Out.SendWeather(
            weather.CurrentPosition(SimpleScheduler.Ticks),
            weather.Width,
            weather.Speed,
            weather.FogDiffusion,
            weather.Intensity
        );
}
```

#### Update Triggers
1. **Weather Start**: All players in region notified
2. **Player Enter Region**: Receives current weather state
3. **Weather Stop**: Clear weather packet sent

### Weather Position Calculation

#### Current Position Formula
```csharp
public uint CurrentPosition(long CurrentTime)
{
    try
    {
        return Position + Convert.ToUInt32(
            Math.Ceiling(((CurrentTime - StartTime) / 1000.0) * Speed)
        );
    }
    catch
    {
        return uint.MaxValue;  // Weather has left region
    }
}
```

#### Duration Calculation
```csharp
// Time for weather to completely pass through region
DueTime = StartTime + Convert.ToInt64(
    Math.Ceiling(
        ((WeatherMaxPosition + Width - Position) / (double)Speed) * 1000
    )
);
```

## Configuration

### Server Properties
```ini
# Weather check interval in milliseconds (minimum 1000)
WEATHER_CHECK_INTERVAL = 300000  # 5 minutes

# Chance of weather starting per check (0-99)
WEATHER_CHANCE = 30  # 30% chance

# Log weather events to info log
WEATHER_LOG_EVENTS = false
```

### Region Eligibility
- Only outdoor regions (not dungeons)
- Must have defined zones
- Automatically managed by WeatherManager

## Client Communication

### Weather Packet
```csharp
public void SendWeather(uint x, uint width, ushort speed, ushort fogDiffusion, ushort intensity)
{
    // Packet structure:
    // - X Position (current storm position)
    // - Width (storm coverage area)
    // - Speed (movement rate)
    // - Fog Diffusion (fog spread factor)
    // - Intensity (rain/fog density)
}
```

### Visual Effects
- **Low Intensity (30-50)**: Light fog/drizzle
- **Medium Intensity (50-80)**: Moderate rain/fog
- **High Intensity (80-110)**: Heavy storm conditions
- **Max Intensity (110-120)**: Severe weather

## System Interactions

### Zone System
- Weather boundaries calculated from zone extents
- MinPosition: Leftmost zone edge
- MaxPosition: Rightmost zone edge

### Player System
- Weather state sent on region entry
- No gameplay effects (visual only)
- Updates handled automatically

### Performance
- One timer per region with active weather
- Lazy evaluation of weather position
- Batch updates to all players in region

## Admin Commands

### Start Weather
```csharp
// Manual weather start
WorldMgr.GetWeatherManager().StartWeather(regionId);

// Custom weather parameters
WorldMgr.GetWeatherManager().ChangeWeather(regionId, weather =>
{
    weather.CreateWeather(
        Position: 0,
        Width: 50000,
        Speed: 200,
        Intensity: 75,
        FogDiffusion: 20000,
        StartTime: GameTimer.GetTickCount()
    );
});
```

### Stop Weather
```csharp
WorldMgr.GetWeatherManager().StopWeather(regionId);
```

## Technical Details

### Thread Safety
```csharp
private readonly Lock _lock = new();

// All weather state changes synchronized
lock (_lock)
{
    if (!RegionsWeather.TryGetValue(regionId, out weather))
        return false;
        
    change(weather);  // Apply weather change
}
```

### Scheduler Integration
- Uses SimpleScheduler for timing
- Dynamic timer intervals based on weather duration
- Automatic cleanup when weather completes

## Test Scenarios

### Weather Generation
```csharp
// Given: Region with no weather
// When: Weather check passes chance roll
// Then: Random weather generated with valid parameters

// Given: Active weather in region
// When: Weather completes (position > max)
// Then: Weather cleared, timer reset to default
```

### Player Updates
```csharp
// Given: Weather active in region
// When: Player enters region
// Then: Receives current weather state

// Given: Player in region
// When: Weather starts
// Then: Receives weather update packet
```

### Configuration Tests
```csharp
// Given: WEATHER_CHANCE = 0
// When: Weather tick occurs
// Then: No weather created

// Given: WEATHER_CHANCE = 100
// When: Weather tick with no active weather
// Then: Weather always created
```

## Known Limitations
- Weather is visual only (no gameplay effects)
- Cannot have multiple weather systems per region
- Weather always moves west to east
- No seasonal variations
- No weather types (always rain/fog combination)

## Future Enhancements
- TODO: Weather-based spell modifiers
- TODO: Multiple weather fronts
- TODO: Seasonal weather patterns
- TODO: Lightning effects
- TODO: Snow weather type

## Change Log
- 2025-01-20: Initial documentation created

## References
- `GameServer/managers/worldmanager/WeatherManager.cs`
- `GameServer/managers/worldmanager/RegionWeather.cs`
- `GameServer/managers/worldmanager/WorldManager.cs`
- `GameServer/packets/Server/PacketLib1XX.cs` (SendWeather) 