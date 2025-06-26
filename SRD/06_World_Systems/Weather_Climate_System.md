# Weather & Climate System

**Document Status:** Core mechanics documented  
**Verification:** Code-verified from weather system implementations  
**Implementation Status:** Live

## Overview

The Weather & Climate System provides dynamic environmental conditions that affect gameplay mechanics, visual atmosphere, and tactical considerations. Weather patterns influence visibility, movement, combat effectiveness, and immersion.

## Core Weather Types

```csharp
public enum WeatherType
{
    Clear = 0,
    LightRain = 1,
    HeavyRain = 2,
    LightSnow = 3,
    HeavySnow = 4,
    Fog = 5,
    Storm = 6,
    Blizzard = 7,
    Sandstorm = 8
}
```

## Weather Effects on Gameplay

### Visibility and Line of Sight
- **Clear**: Full visibility range
- **Light Rain**: 90% visibility
- **Heavy Rain**: 70% visibility  
- **Light Snow**: 80% visibility
- **Heavy Snow**: 60% visibility
- **Fog**: 50% visibility
- **Storm**: 60% visibility
- **Blizzard**: 40% visibility
- **Sandstorm**: 30% visibility

### Combat Effects
- **Rain/Storm**: Fire spells -10%, Cold spells +10%
- **Snow/Blizzard**: Cold spells +15%, Heat spells -15%
- **Sandstorm**: All magic effectiveness -5%
- **Heavy Weather**: Archery accuracy reduced 15-30%

### Movement Effects
- **Heavy Rain**: 95% movement speed
- **Heavy Snow**: 90% movement speed
- **Blizzard**: 85% movement speed
- **Sandstorm**: 90% movement speed

### Stealth Bonuses
- **Light Rain**: 95% detection chance
- **Heavy Rain**: 85% detection chance
- **Fog**: 75% detection chance
- **Storm**: 80% detection chance
- **Blizzard**: 70% detection chance

## Regional Weather Patterns

### Hibernia (Temperate)
- Clear: 50%, Light Rain: 35%, Fog: 15%
- Frequent rain, mild temperatures

### Midgard (Cold)
- Clear: 40%, Light Snow: 45%, Blizzard: 15%
- Cold climate, frequent snow

### Albion (Mild)
- Clear: 60%, Light Rain: 25%, Storm: 15%
- Moderate climate, occasional storms

## Configuration

```csharp
[ServerProperty("weather", "enable_weather_system", true)]
[ServerProperty("weather", "weather_change_interval", 30)]
[ServerProperty("weather", "weather_affects_combat", true)]
[ServerProperty("weather", "weather_affects_stealth", true)]
```

## References

- `GameServer/world/Weather/WeatherManager.cs`
- `GameServer/packets/Server/GSTCPPacketOut.cs`
- Weather packet handling and client synchronization 