# Time System

## Document Status
- **Last Updated**: 2025-01-20
- **Status**: Stable
- **Verification**: Code-verified from Region.cs, WorldMgr.cs
- **Implementation**: Stable

## Overview
The time system manages in-game time progression including day/night cycles, time-based conditions, and time display. Game time runs faster than real time, allowing multiple day/night cycles within a play session.

## Core Mechanics

### Time Calculation

#### Game Time Formula
```csharp
public virtual uint GameTime
{
    get
    {
        ulong gameTime = (uint)(WorldMgr.GetCurrentGameTime() * m_timeMultiplier / 1000);
        return (uint)(gameTime % DayDuration);
    }
}
```

#### Time Constants
- **Day Duration**: 86,400,000 milliseconds (24 hours)
- **Time Multiplier**: Region-specific speed modifier
- **Base Game Time**: Server start reference point

### Time Properties

#### Time of Day Checks
```csharp
// Check if PM (12:00 - 23:59)
public virtual bool IsPM
{
    get
    {
        uint hour = GameTime / 1000 / 60 / 60;
        return hour >= 12 && hour <= 23;
    }
}

// Check if night (18:00 - 05:59)
public virtual bool IsNightTime
{
    get
    {
        uint hour = GameTime / 1000 / 60 / 60;
        return hour >= 18 || hour < 6;
    }
}
```

### Time Display

#### Client Time Format
```csharp
// Time sent to client as day + daytime
public static void SendTimeUpdate(GameClient client)
{
    uint time = WorldMgr.GetCurrentGameTime();
    uint day = time / DAY_IN_MILLISECONDS;
    uint daytime = time % DAY_IN_MILLISECONDS;
    
    client.Out.SendTime(day, daytime);
}
```

#### Time Components
- **Day**: Number of complete game days since server start
- **Daytime**: Milliseconds elapsed in current day (0-86,399,999)
- **Hour**: Daytime / 3,600,000
- **Minute**: (Daytime / 60,000) % 60
- **Second**: (Daytime / 1,000) % 60

## Time-Based Systems

### Conditional Spells
Some spells have time-based requirements:
- Night-only spells (18:00 - 05:59)
- Day-only spells (06:00 - 17:59)
- Dawn/dusk specific abilities

### NPC Behaviors
- Shop hours (some merchants)
- Guard patrol changes
- Quest NPC availability

### Visual Effects
- Sky color transitions
- Lighting changes
- Shadow directions
- Ambient sounds

## Region Time Settings

### Time Multiplier
```csharp
public class Region
{
    private float m_timeMultiplier = 1.0f;
    
    // Faster time progression
    // Example: 10.0 = 10x speed (1 real hour = 10 game hours)
    public float TimeMultiplier 
    { 
        get => m_timeMultiplier;
        set => m_timeMultiplier = value;
    }
}
```

### Common Multipliers
- **Normal**: 1.0 (real-time)
- **Standard DAoC**: 12.0 (2-hour days)
- **Fast**: 24.0 (1-hour days)
- **Instance**: Variable per instance

## Time Synchronization

### Server-Wide Time
```csharp
public static class WorldMgr
{
    // Base time reference point
    private static long m_startTime = GameTimer.GetTickCount();
    
    public static uint GetCurrentGameTime()
    {
        return (uint)(GameTimer.GetTickCount() - m_startTime);
    }
}
```

### Client Synchronization
- Time updates sent on:
  - Player login
  - Zone changes
  - Periodic refresh (every 3 minutes)
  - Time-sensitive action triggers

## Time-Based Calculations

### Dawn and Dusk
```csharp
// Dawn: 5:00 - 7:00 (transitional period)
public bool IsDawn()
{
    uint hour = GameTime / 1000 / 60 / 60;
    return hour >= 5 && hour < 7;
}

// Dusk: 17:00 - 19:00 (transitional period)
public bool IsDusk()
{
    uint hour = GameTime / 1000 / 60 / 60;
    return hour >= 17 && hour < 19;
}
```

### Seasonal Variations
- Currently not implemented
- All days have equal day/night duration
- No seasonal event triggers

## Implementation Details

### Time Storage
- Time stored as milliseconds since server start
- 32-bit unsigned integer (wraps after ~49 days)
- Day counter handles overflow gracefully

### Performance Considerations
- Time calculations cached per game tick
- Minimal computational overhead
- No database persistence required

## Test Scenarios

### Time Progression
```csharp
// Given: Region with 12x time multiplier
// When: 2 real hours pass
// Then: 24 game hours pass (full day cycle)

// Given: Current time is 17:30
// When: Check IsNightTime
// Then: Returns false (not yet 18:00)

// Given: Current time is 23:59
// When: 1 game minute passes
// Then: Time wraps to 00:00, new day begins
```

### Time-Based Conditions
```csharp
// Given: Night-only spell, current time 19:00
// When: Cast spell
// Then: Spell succeeds (night condition met)

// Given: Day-only merchant, current time 03:00
// When: Interact with merchant
// Then: "The shop is closed" message
```

## Configuration

### Server Properties
```ini
# No direct time configuration in properties
# Time multiplier set per region in database
```

### Database Settings
```sql
-- Region table time_multiplier column
UPDATE Regions SET time_multiplier = 12.0 WHERE region_id = 1;
```

## Known Issues
- Time not persisted across server restarts
- All regions share same base time
- No support for different time zones
- Limited time-based game mechanics

## Future Enhancements
- TODO: Persistent time across restarts
- TODO: Regional time zones
- TODO: Seasonal changes
- TODO: More time-based events
- TODO: Lunar cycles
- TODO: Weather correlation with time

## Change Log
- 2025-01-20: Initial documentation created

## References
- `GameServer/world/Region.cs` (Time properties)
- `GameServer/packets/Server/PacketLib1XX.cs` (SendTime)
- `GameServer/GameServer.cs` (WorldMgr time tracking) 