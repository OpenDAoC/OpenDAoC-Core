# Security & Anti-Cheat System

**Document Status:** Core mechanisms documented  
**Verification:** Code-verified from security monitoring patterns  
**Implementation Status:** Live

## Overview

The Security & Anti-Cheat System provides comprehensive protection against player exploitation through real-time monitoring, violation detection, and automated response mechanisms.

## Core Components

### Speed Violation Detection
```csharp
// Speed hack detection and tracking
public class SpeedViolationMonitor
{
    public void CheckPlayerSpeed(GamePlayer player, float currentSpeed, float maxAllowed)
    {
        if (currentSpeed > maxAllowed * SPEED_TOLERANCE_FACTOR)
        {
            var violations = player.TempProperties.GetProperty<int>("SPEED_VIOLATIONS");
            player.TempProperties.SetProperty("SPEED_VIOLATIONS", violations + 1);
            
            HandleSpeedViolation(player, violations + 1);
        }
    }
    
    private void HandleSpeedViolation(GamePlayer player, int violationCount)
    {
        switch (violationCount)
        {
            case 1:
                LogViolation(player, "First speed violation detected");
                break;
            case 3:
                player.SendMessage("Speed violation detected - please check your connection", 
                    eChatType.CT_Important);
                break;
            case 5:
                // Teleport back to safe location
                player.MoveTo(player.BindRegion, player.BindXpos, player.BindYpos, 
                    player.BindZpos, player.BindHeading);
                break;
            case 10:
                // Temporary suspension
                player.Client.Disconnect();
                break;
        }
    }
}
```

### Player Movement Monitor System

```csharp
public class PlayerMovementMonitor
{
    private const int SPEEDHACK_ACTION_THRESHOLD = 3;
    private const int TELEPORT_THRESHOLD = 3;
    private const int BASE_SPEED_TOLERANCE = 25;
    private const int LATENCY_BUFFER = 650;
    private const int RESET_COUNTER_DELAY = 1250;
    
    private readonly GamePlayer _player;
    private PositionSample _previous;
    private PositionSample _current;
    private PositionSample _teleport;
    private int _speedHackCount;
    private int _teleportCount;
    private long _resetCountersTime;
    private long _pausedUntil;
    
    public void RecordPosition()
    {
        long timestamp = GameLoop.GetRealTime();
        
        if (_pausedUntil > timestamp)
            return;
            
        if (_teleport.Timestamp > 0 && _resetCountersTime <= timestamp)
        {
            _speedHackCount = 0;
            _teleportCount = 0;
            _teleport = default;
        }
        
        short currentMaxSpeed = GetCachedPlayerMaxSpeed();
        PositionSample sample = new(_player.X, _player.Y, _player.Z, 
            GameLoop.GameLoopTime, timestamp, currentMaxSpeed);
            
        if (_current.GameLoopTime == GameLoop.GameLoopTime)
            _current = sample;
        else
        {
            _previous = _current;
            _current = sample;
        }
    }
    
    public void ValidateMovement()
    {
        long timeDiff = _current.Timestamp - _previous.Timestamp;
        
        if (timeDiff <= 0)
            return;
            
        long dx = _current.X - _previous.X;
        long dy = _current.Y - _previous.Y;
        long squaredDistance = dx * dx + dy * dy;
        
        double allowedMaxSpeed = CalculateAllowedMaxSpeed(_current, GameLoop.GetRealTime()) + BASE_SPEED_TOLERANCE;
        double allowedMaxDistance = allowedMaxSpeed * timeDiff / 1000.0;
        double allowedMaxDistanceSquared = allowedMaxDistance * allowedMaxDistance;
        
        if (squaredDistance > allowedMaxDistanceSquared)
        {
            _resetCountersTime = GameLoop.GetRealTime() + RESET_COUNTER_DELAY;
            
            if (_teleport.Timestamp == 0)
            {
                _teleport = _previous.Timestamp > 0 ? _previous : _current;
            }
            
            if (++_speedHackCount >= SPEEDHACK_ACTION_THRESHOLD)
            {
                if (!_player.IsAllowedToFly && _player.Client.Account.PrivLevel <= (uint)ePrivLevel.Player)
                {
                    double actualDistance = Math.Sqrt(squaredDistance);
                    double actualSpeed = actualDistance * 1000.0 / timeDiff;
                    HandleSpeedHack(actualDistance, allowedMaxDistance, actualSpeed, allowedMaxSpeed);
                }
                _speedHackCount = 0;
            }
        }
    }
    
    private void HandleSpeedHack(double actualDistance, double allowedMaxDistance, double actualSpeed, double allowedMaxSpeed)
    {
        string action = _teleportCount >= TELEPORT_THRESHOLD ? "kick" : "teleport";
        string msg = BuildSpeedHackMessage(action, actualDistance, allowedMaxDistance, actualSpeed, allowedMaxSpeed, _teleportCount);
        GameServer.Instance.LogCheatAction(msg);
        
        if (_teleportCount >= TELEPORT_THRESHOLD)
        {
            _player.Out.SendPlayerQuit(true);
            _player.SaveIntoDatabase();
            _player.Quit(true);
            _player.Client.Disconnect();
        }
        else
        {
            _previous = _current;
            _current = _teleport;
            _player.MoveTo(_player.CurrentRegionID, _teleport.X, _teleport.Y, _teleport.Z, _player.Heading);
            _teleportCount++;
        }
    }
}
```

### Anti-Cheat Detection Types

#### Flying Hack Detection
```csharp
// In PlayerPositionUpdateHandler.cs - detects unauthorized flying
if (state == 3 && !client.Player.TempProperties.GetProperty<bool>(GamePlayer.DEBUG_MODE_PROPERTY) && !client.Player.IsAllowedToFly)
{
    StringBuilder builder = new();
    builder.Append("HACK_FLY");
    builder.Append(": CharName=");
    builder.Append(client.Player.Name);
    builder.Append(" Account=");
    builder.Append(client.Account.Name);
    builder.Append(" IP=");
    builder.Append(client.TcpEndpointAddress);
    
    GameServer.Instance.LogCheatAction(builder.ToString());
    
    if (ServerProperties.Properties.BAN_HACKERS)
    {
        DbBans ban = new()
        {
            Author = "SERVER",
            Ip = client.TcpEndpointAddress,
            Account = client.Account.Name,
            DateBan = DateTime.Now,
            Type = "B",
            Reason = $"Autoban flying hack: on player:{client.Player.Name}"
        };
        
        GameServer.Database.AddObject(ban);
        GameServer.Database.SaveObject(ban);
    }
}
```

#### State Flag Validation
```csharp
// Validates player state flags to prevent forged packets
if ((stateFlags & StateFlags.RIDING) is StateFlags.RIDING && !player.IsRiding)
{
    if (ServerProperties.Properties.BAN_HACKERS)
    {
        player.Client.BanAccount($"Autoban forged position update packet ({nameof(stateFlags)}: {StateFlags.RIDING})");
        player.Out.SendPlayerQuit(true);
        player.Client.Disconnect();
        return false;
    }
    stateFlags &= ~StateFlags.RIDING;
}

if ((stateFlags & StateFlags.FLYING) is StateFlags.FLYING && !player.TempProperties.GetProperty<bool>(GamePlayer.DEBUG_MODE_PROPERTY) && !player.IsAllowedToFly)
{
    if (ServerProperties.Properties.BAN_HACKERS)
    {
        player.Client.BanAccount($"Autoban forged position update packet ({nameof(stateFlags)}: {StateFlags.FLYING})");
        player.Out.SendPlayerQuit(true);
        player.Client.Disconnect();
        return false;
    }
    stateFlags &= ~StateFlags.FLYING;
}
```

### Spam Protection System
```csharp
// Chat and command spam protection
public class SpamProtectionMonitor
{
    private readonly Dictionary<string, long> _lastCommandTimes = new();
    private readonly Dictionary<string, int> _commandCounts = new();
    
    public bool CheckCommandSpam(GameClient client, string command)
    {
        string key = $"{client.Account.Name}_{command}";
        long currentTime = GameLoop.GameLoopTime;
        
        if (_lastCommandTimes.TryGetValue(key, out long lastTime))
        {
            if (currentTime - lastTime < COMMAND_SPAM_THRESHOLD)
            {
                var spamViolations = client.TempProperties.GetProperty<int>("SPAM_VIOLATIONS");
                client.TempProperties.SetProperty("SPAM_VIOLATIONS", spamViolations + 1);
                
                HandleSpamViolation(client, spamViolations + 1);
                return false; // Block command
            }
        }
        
        _lastCommandTimes[key] = currentTime;
        return true; // Allow command
    }
}
```

### Inactivity Monitoring
```csharp
// Player inactivity detection
public class InactivityMonitor
{
    public void CheckPlayerActivity(GamePlayer player)
    {
        long inactiveTime = GameLoop.GameLoopTime - player.LastPlayerActivityTime;
        
        if (inactiveTime > Properties.INACTIVITY_TIMEOUT)
        {
            if (!player.TempProperties.GetProperty("INACTIVITY_WARNING_SENT", false))
            {
                player.SendMessage("You will be disconnected for inactivity in 60 seconds", 
                    eChatType.CT_Important);
                player.TempProperties.SetProperty("INACTIVITY_WARNING_SENT", true);
            }
            else
            {
                // Disconnect after warning period
                player.Client.Disconnect();
            }
        }
    }
}
```

## Violation Categories

### Movement Violations
- **Speed Hacking** - Exceeding maximum movement speeds
- **Teleport Hacking** - Instant position changes
- **Wall Clipping** - Moving through solid objects
- **Flying** - Vertical movement violations

### Combat Violations  
- **Damage Hacking** - Dealing impossible damage amounts
- **Attack Speed** - Attacking faster than possible
- **Range Hacking** - Attacking beyond weapon range
- **Invulnerability** - Taking no damage when should

### Communication Violations
- **Chat Spam** - Excessive message frequency
- **Command Spam** - Rapid command execution
- **Channel Abuse** - Improper channel usage

### Economic Violations
- **Duplication** - Item/money duplication attempts
- **Trade Exploitation** - Unfair trade mechanics abuse
- **Market Manipulation** - Price manipulation attempts

## Response Mechanisms

### Graduated Response System
```csharp
public enum ViolationResponse
{
    Log = 1,           // Log for review
    Warning = 2,       // Send warning message
    Correction = 3,    // Correct player state
    Temporary = 4,     // Temporary restrictions
    Disconnect = 5,    // Force disconnect
    Suspension = 6     // Account suspension
}

public void ProcessViolation(GamePlayer player, ViolationType type, int severity)
{
    var response = DetermineResponse(type, severity, GetViolationHistory(player));
    
    switch (response)
    {
        case ViolationResponse.Log:
            LogViolation(player, type, severity);
            break;
        case ViolationResponse.Warning:
            SendWarning(player, type);
            break;
        case ViolationResponse.Correction:
            CorrectPlayerState(player, type);
            break;
        case ViolationResponse.Disconnect:
            player.Client.Disconnect();
            break;
    }
}
```

### Automated Corrections
```csharp
// Automatic state correction
public class StateCorrection
{
    public void CorrectSpeedViolation(GamePlayer player)
    {
        // Teleport to last known good position
        player.MoveTo(player.LastValidPosition);
        
        // Reset movement state
        player.IsMoving = false;
        player.IsRunning = false;
    }
    
    public void CorrectCombatViolation(GamePlayer player)
    {
        // Reset attack state
        player.AttackComponent.StopAttack();
        
        // Clear invalid buffs
        RemoveInvalidEffects(player);
    }
}
```

## Configuration

### Security Settings
```csharp
// Server properties for security configuration
[ServerProperty("security", "speed_tolerance_factor", 
    "Speed tolerance multiplier before violation", 1.1)]
public static double SPEED_TOLERANCE_FACTOR;

[ServerProperty("security", "command_spam_threshold", 
    "Minimum time between commands (ms)", 1000)]
public static int COMMAND_SPAM_THRESHOLD;

[ServerProperty("security", "inactivity_timeout", 
    "Inactivity timeout before warning (ms)", 300000)]
public static int INACTIVITY_TIMEOUT;

[ServerProperty("security", "max_violations_before_disconnect", 
    "Maximum violations before auto-disconnect", 10)]
public static int MAX_VIOLATIONS_BEFORE_DISCONNECT;
```

### Violation Thresholds
```csharp
public static class ViolationThresholds
{
    public const int SPEED_WARNING = 3;
    public const int SPEED_CORRECTION = 5;
    public const int SPEED_DISCONNECT = 10;
    
    public const int SPAM_WARNING = 5;
    public const int SPAM_MUTE = 10;
    public const int SPAM_DISCONNECT = 20;
    
    public const int COMBAT_WARNING = 2;
    public const int COMBAT_CORRECTION = 4;
    public const int COMBAT_SUSPENSION = 8;
}
```

## Integration with GM Tools

### GM Alert System
```csharp
public void AlertOnlineGMs(SecurityAuditEntry logEntry)
{
    var onlineGMs = WorldMgr.GetAllPlayers()
        .Where(p => p.Client.Account.PrivLevel > 1)
        .ToList();
        
    foreach (var gm in onlineGMs)
    {
        gm.SendMessage($"[SECURITY] {logEntry.PlayerName}: {logEntry.Details}", 
            eChatType.CT_Staff);
    }
}
```

## TODO: Missing Documentation

- Advanced pattern recognition algorithms for sophisticated cheats
- Machine learning integration for anomaly detection
- Cross-server violation tracking and sharing
- Detailed forensic analysis tools for post-incident investigation
- Integration with external anti-cheat services
- Performance impact monitoring of security systems

## References

- `GameServer/gameobjects/GamePlayer.cs` - Player state monitoring
- `GameServer/ECS-Services/ClientService.cs` - Network monitoring
- Various command handlers for GM security tools
- Server configuration properties for security settings 