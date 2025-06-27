# Weather & Climate System

**Document Status:** Core mechanics documented  
**Verification:** Code-verified from weather system implementations  
**Implementation Status:** Live

## Overview

**Game Rule Summary**: Weather in DAoC isn't just for looks - it actively affects how you fight, move, and hide. Different weather conditions change your visibility range, modify spell effectiveness, and can help or hurt stealth classes. Rain might make fire spells weaker but cold spells stronger, while heavy snow can slow you down but make it harder for enemies to spot you. Each realm has its own typical weather patterns that you'll need to adapt to for optimal gameplay.

The Weather & Climate System provides dynamic environmental conditions that affect gameplay mechanics, visual atmosphere, and tactical considerations. Weather patterns influence visibility, movement, combat effectiveness, and immersion.

## Core Weather Types

**Game Rule Summary**: There are nine different weather types ranging from clear skies to dangerous blizzards and sandstorms. Each type has different effects on gameplay - clear weather gives you full visibility and normal combat, while severe weather like blizzards can cut your vision in half and slow you down significantly. Understanding what each weather type does helps you adjust your tactics accordingly.

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

**Game Rule Summary**: Weather directly affects how far you can see, which impacts everything from spotting enemies to targeting spells at long range. Clear weather gives you full sight distance, but fog can cut your vision in half and blizzards reduce it to less than half. This is especially important for archers and casters who rely on long-range attacks, as well as for spotting incoming enemies in RvR combat.

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

**Game Rule Summary**: Weather creates realistic magic interactions that add tactical depth to spellcasting. Rain weakens fire magic but strengthens cold spells, making fire-based casters less effective while ice mages become more powerful. Snow and blizzards heavily favor cold magic users. Sandstorms interfere with all magic equally. Archers suffer in heavy weather as wind and precipitation make their shots less accurate.

- **Rain/Storm**: Fire spells -10%, Cold spells +10%
- **Snow/Blizzard**: Cold spells +15%, Heat spells -15%
- **Sandstorm**: All magic effectiveness -5%
- **Heavy Weather**: Archery accuracy reduced 15-30%

### Movement Effects

**Game Rule Summary**: Heavy weather slows you down as you struggle through the elements. Heavy rain makes you slightly slower, while snow and blizzards can reduce your movement significantly. This affects everything from combat positioning to escape attempts. Plan your routes and timing carefully during bad weather, especially in RvR situations where speed can mean the difference between life and death.

- **Heavy Rain**: 95% movement speed
- **Heavy Snow**: 90% movement speed
- **Blizzard**: 85% movement speed
- **Sandstorm**: 90% movement speed

### Stealth Bonuses

**Game Rule Summary**: Weather is a stealth class's best friend. Rain, fog, and storms all make it harder for enemies to detect you, with fog providing the biggest advantage. Blizzards are nearly as good as fog for hiding. This makes bad weather an ideal time for assassins and scouts to move around undetected, but it also means you need to be extra cautious about enemy stealth classes when the weather turns poor.

- **Light Rain**: 95% detection chance
- **Heavy Rain**: 85% detection chance
- **Fog**: 75% detection chance
- **Storm**: 80% detection chance
- **Blizzard**: 70% detection chance

## Regional Weather Patterns

**Game Rule Summary**: Each realm has its own typical weather that reflects the lore and influences combat strategies. Hibernia gets lots of rain and fog, making it ideal for stealth gameplay and cold magic users. Midgard is frequently snowy, heavily favoring cold spells while penalizing fire magic. Albion has the most balanced weather with frequent clear skies but occasional storms. Understanding your realm's weather helps you choose classes and tactics that work well with the local conditions.

### Hibernia (Temperate)

**Game Rule Summary**: Hibernian weather favors stealth and nature magic with frequent rain and mysterious fogs. The wet climate makes fire magic less reliable but provides excellent cover for Rangers and other stealth classes. If you're playing in Hibernia, consider classes that benefit from low visibility and wet conditions.

- Clear: 50%, Light Rain: 35%, Fog: 15%
- Frequent rain, mild temperatures

### Midgard (Cold)

**Game Rule Summary**: Midgard's harsh northern climate is perfect for cold-based magic users and tough warriors who don't mind the snow. Fire mages will struggle here, while ice-based spellcasters thrive. The frequent snow provides stealth advantages but can slow movement during critical moments. Hardy Nordic classes feel right at home in these conditions.

- Clear: 40%, Light Snow: 45%, Blizzard: 15%
- Cold climate, frequent snow

### Albion (Mild)

**Game Rule Summary**: Albion enjoys the most balanced weather, making it suitable for all classes and playstyles. The clear skies favor archers and long-range magic users, while occasional storms provide tactical opportunities for different approaches. This stable climate makes Albion a good realm for players who want consistent, predictable conditions.

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