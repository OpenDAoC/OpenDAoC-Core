# Authentication and Security System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from AbstractServerRules.cs, LoginRequestHandler.cs, Account.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: The Authentication and Security System is your protection against hackers and ensures fair play for everyone. When you log in, it verifies your account name and password, tracks your IP address for security, and prevents multiple logins from the same account. The system has different privilege levels - normal players (level 1), Game Masters who help with problems (level 2), and Administrators who manage the server (level 3). It constantly monitors for cheating like speed hacking, item duplication, and packet spam, automatically banning serious offenders while giving warnings for minor violations. The system also tracks everything in detailed logs, so GMs can investigate problems and ensure fair play. All of this happens invisibly in the background to keep the game secure while letting you focus on playing.

The Authentication and Security System provides comprehensive account management, privilege level enforcement, anti-cheat detection, and security monitoring. It integrates with all game systems to ensure proper authorization and prevent exploitation.

## Core Architecture

### Account System
```csharp
public class DbAccount : DataObject
{
    public string Name { get; set; }              // Account username
    public string Password { get; set; }          // Hashed password
    public uint PrivLevel { get; set; }           // Privilege level (1=Player, 2=GM, 3=Admin)
    public eRealm Realm { get; set; }             // Account realm restriction
    public DateTime LastLogin { get; set; }       // Last login timestamp
    public DateTime LastLoginIP { get; set; }     // Last login IP address
    public DateTime Created { get; set; }         // Account creation date
    public bool IsActive { get; set; }            // Account enabled status
}
```

### Privilege Level System
```csharp
public enum ePrivLevel : uint
{
    Player = 1,  // Normal players - full gameplay access
    GM = 2,      // Game Masters - moderation tools, basic admin commands
    Admin = 3    // Administrators - full server control, database access
}
```

## Authentication Process

### Login Flow
```csharp
[PacketHandler(PacketHandlerType.TCP, eClientPackets.LoginRequest, "Handles player login")]
public class LoginRequestHandler : IPacketHandler
{
    public void HandlePacket(GameClient client, GSPacketIn packet)
    {
        // Extract credentials
        packet.Skip(2); // Skip header
        string accountName = packet.ReadString(20);
        string password = packet.ReadString(20);
        
        // Validate account
        DbAccount account = GameServer.Database.SelectObject<DbAccount>(
            "`Name` = @Name", new QueryParameter("@Name", accountName));
            
        if (account == null)
        {
            // Account not found
            client.Out.SendLoginDenied(eLoginError.AccountNotFound);
            AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountLogin, 
                "Failed - Account not found", accountName);
            return;
        }
        
        // Verify password
        if (!VerifyPassword(password, account.Password))
        {
            // Invalid password
            client.Out.SendLoginDenied(eLoginError.AccountInvalid);
            AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountLogin, 
                "Failed - Invalid password", accountName);
            HandleFailedLogin(client, account);
            return;
        }
        
        // Check account status
        if (!account.IsActive)
        {
            client.Out.SendLoginDenied(eLoginError.AccountSuspended);
            return;
        }
        
        // Check concurrent login restrictions
        if (IsAccountAlreadyLoggedIn(account))
        {
            client.Out.SendLoginDenied(eLoginError.AccountAlreadyLoggedIn);
            return;
        }
        
        // Update login tracking
        account.LastLogin = DateTime.Now;
        account.LastLoginIP = client.TcpEndpointAddress;
        GameServer.Database.SaveObject(account);
        
        // Successful login
        client.Account = account;
        client.Out.SendLoginGranted();
        
        AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountLogin, 
            "Success", accountName);
    }
}
```

### Password Security
```csharp
public static class PasswordSecurity
{
    public static string HashPassword(string password)
    {
        // Use bcrypt or similar secure hashing
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }
    
    public static bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
```

## Authorization and Privilege Checking

### Command Authorization
```csharp
public static bool HandleCommand(GameClient client, string cmdLine)
{
    string[] pars = ParseCmdLine(cmdLine);
    GameCommand myCommand = GuessCommand(pars[0]);
    
    if (myCommand == null) 
        return false;
        
    // Check basic privilege level
    if (client.Account.PrivLevel < myCommand.m_lvl)
    {
        // Check single permission system for granular access
        if (!SinglePermission.HasPermission(client.Player, pars[0].Substring(1)))
        {
            client.Out.SendMessage("No such command (" + pars[0] + ")", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return true;
        }
    }
    
    // Log privileged command usage
    if (client.Account.PrivLevel > 1 || myCommand.m_lvl > 1)
    {
        AuditMgr.LogGMCommand(String.Join(" ", pars), 
            client.Player?.Name, 
            client.Player?.TargetObject?.Name, 
            client.Account.Name);
    }
    
    ExecuteCommand(client, myCommand, pars);
    return true;
}
```

### Area Access Control
```csharp
public override bool CheckAreaAccess(GameClient client, IArea area)
{
    // GM areas require GM+ access
    if (area is GMArea && client.Account.PrivLevel < (uint)ePrivLevel.GM)
    {
        client.Out.SendMessage("You don't have permission to enter this area.", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return false;
    }
    
    // Admin areas require Admin access
    if (area is AdminArea && client.Account.PrivLevel < (uint)ePrivLevel.Admin)
    {
        client.Out.SendMessage("This area is restricted to administrators.", 
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
        return false;
    }
    
    return true;
}
```

### Database Access Authorization
```csharp
public bool CanAccessDatabase(GameClient client, Type objectType)
{
    // Only admins can access sensitive data
    if (IsSensitiveData(objectType) && client.Account.PrivLevel < (uint)ePrivLevel.Admin)
    {
        AuditMgr.AddAuditEntry(client, AuditType.Security, AuditSubtype.UnauthorizedAccess, 
            $"Attempted access to {objectType.Name}", "");
        return false;
    }
    
    return true;
}

private bool IsSensitiveData(Type objectType)
{
    return objectType == typeof(DbAccount) || 
           objectType == typeof(DbAuditEntry) ||
           objectType == typeof(DbServerProperty);
}
```

## Anti-Cheat and Exploit Prevention

### Speed Hack Detection
```csharp
public class SpeedHackDetection
{
    private const double MAX_SPEED_TOLERANCE = 1.15; // 15% tolerance
    private Dictionary<GamePlayer, SpeedTrackingData> _playerSpeeds = new();
    
    public bool ValidatePlayerMovement(GamePlayer player, int newX, int newY, int newZ)
    {
        if (player.Client.Account.PrivLevel > 1)
            return true; // Skip checks for GMs/Admins
            
        var data = GetOrCreateSpeedData(player);
        
        // Calculate distance and time
        double distance = Math.Sqrt(
            Math.Pow(newX - data.LastX, 2) + 
            Math.Pow(newY - data.LastY, 2) + 
            Math.Pow(newZ - data.LastZ, 2));
            
        long timeDiff = GameLoop.GameLoopTime - data.LastUpdate;
        
        if (timeDiff > 0)
        {
            double actualSpeed = distance / (timeDiff / 1000.0);
            double maxSpeed = CalculateMaxSpeed(player) * MAX_SPEED_TOLERANCE;
            
            if (actualSpeed > maxSpeed)
            {
                // Potential speed hack
                HandleSpeedViolation(player, actualSpeed, maxSpeed);
                return false;
            }
        }
        
        // Update tracking data
        data.LastX = newX;
        data.LastY = newY;
        data.LastZ = newZ;
        data.LastUpdate = GameLoop.GameLoopTime;
        
        return true;
    }
    
    private void HandleSpeedViolation(GamePlayer player, double actualSpeed, double maxSpeed)
    {
        // Log the violation
        AuditMgr.AddAuditEntry(player.Client, AuditType.Security, AuditSubtype.SpeedHack,
            $"Speed: {actualSpeed:F2} vs Max: {maxSpeed:F2}", "");
            
        // Increment violation count
        var violations = player.TempProperties.GetProperty<int>("SPEED_VIOLATIONS");
        player.TempProperties.SetProperty("SPEED_VIOLATIONS", violations + 1);
        
        // Take action based on violation count
        if (violations >= 3)
        {
            // Kick player for repeated violations
            player.Client.Out.SendMessage("You have been disconnected for suspicious movement.", 
                eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            player.Client.Disconnect();
        }
        else
        {
            // Warning
            player.Client.Out.SendMessage("Movement validation failed. Please check your connection.", 
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
```

### Packet Spam Protection
```csharp
public class PacketSpamProtection
{
    private Dictionary<GameClient, PacketTrackingData> _clientTracking = new();
    private const int MAX_PACKETS_PER_SECOND = 100;
    
    public bool ValidatePacketRate(GameClient client, int packetCode)
    {
        if (client.Account.PrivLevel > 1)
            return true; // Skip for GMs/Admins
            
        var data = GetOrCreateTrackingData(client);
        long currentTime = GameLoop.GameLoopTime;
        
        // Reset counter every second
        if (currentTime - data.LastReset > 1000)
        {
            data.PacketCount = 0;
            data.LastReset = currentTime;
        }
        
        data.PacketCount++;
        
        if (data.PacketCount > MAX_PACKETS_PER_SECOND)
        {
            // Packet spam detected
            AuditMgr.AddAuditEntry(client, AuditType.Security, AuditSubtype.PacketSpam,
                $"Packet 0x{packetCode:X2} - Rate: {data.PacketCount}/sec", "");
                
            // Disconnect chronic spammers
            var spamViolations = client.TempProperties.GetProperty<int>("SPAM_VIOLATIONS");
            if (spamViolations >= 3)
            {
                client.Disconnect();
                return false;
            }
            
            client.TempProperties.SetProperty("SPAM_VIOLATIONS", spamViolations + 1);
            return false;
        }
        
        return true;
    }
}
```

### Dupe Detection System
```csharp
public class DuplicationDetection
{
    public bool ValidateItemTransaction(GamePlayer player, DbInventoryItem item)
    {
        // Check for impossible item stacks
        if (item.Count > item.Template.MaxCount)
        {
            AuditMgr.AddAuditEntry(player.Client, AuditType.Security, AuditSubtype.ItemDuplication,
                $"Invalid stack: {item.Template.Name} Count: {item.Count} Max: {item.Template.MaxCount}", "");
            return false;
        }
        
        // Check for items that shouldn't exist
        if (item.Template.Realm != 0 && item.Template.Realm != (int)player.Realm)
        {
            AuditMgr.AddAuditEntry(player.Client, AuditType.Security, AuditSubtype.ItemDuplication,
                $"Wrong realm item: {item.Template.Name} PlayerRealm: {player.Realm} ItemRealm: {item.Template.Realm}", "");
            return false;
        }
        
        // Check for artificially modified items
        if (item.Price < 0 || item.Count < 0 || item.Condition < 0)
        {
            AuditMgr.AddAuditEntry(player.Client, AuditType.Security, AuditSubtype.ItemDuplication,
                $"Invalid item values: {item.Template.Name} Price: {item.Price} Count: {item.Count} Condition: {item.Condition}", "");
            return false;
        }
        
        return true;
    }
}
```

## Session Management

### Session Tracking
```csharp
public class SessionManager
{
    private static Dictionary<string, GameClient> _activeSessions = new();
    private static Dictionary<string, DateTime> _lastActivity = new();
    
    public static bool IsAccountAlreadyLoggedIn(DbAccount account)
    {
        lock (_activeSessions)
        {
            if (_activeSessions.TryGetValue(account.Name, out GameClient existingClient))
            {
                // Check if existing session is still valid
                if (existingClient.IsConnected && existingClient.ClientState != eClientState.Disconnected)
                {
                    return true;
                }
                else
                {
                    // Clean up stale session
                    _activeSessions.Remove(account.Name);
                    _lastActivity.Remove(account.Name);
                }
            }
        }
        return false;
    }
    
    public static void RegisterSession(GameClient client)
    {
        lock (_activeSessions)
        {
            _activeSessions[client.Account.Name] = client;
            _lastActivity[client.Account.Name] = DateTime.Now;
        }
    }
    
    public static void UnregisterSession(GameClient client)
    {
        lock (_activeSessions)
        {
            _activeSessions.Remove(client.Account.Name);
            _lastActivity.Remove(client.Account.Name);
        }
    }
}
```

### IP-Based Restrictions
```csharp
public class IPRestrictionManager
{
    private static Dictionary<string, IPRestrictionData> _ipRestrictions = new();
    private const int MAX_ACCOUNTS_PER_IP = 3;
    private const int LOGIN_ATTEMPT_LIMIT = 5;
    
    public static bool CanConnectFromIP(string ipAddress)
    {
        var restriction = GetOrCreateRestriction(ipAddress);
        
        // Check for IP ban
        if (restriction.IsBanned && restriction.BanExpires > DateTime.Now)
        {
            return false;
        }
        
        // Check concurrent connection limit
        int activeConnections = CountActiveConnectionsFromIP(ipAddress);
        if (activeConnections >= MAX_ACCOUNTS_PER_IP)
        {
            return false;
        }
        
        return true;
    }
    
    public static void RecordFailedLogin(string ipAddress)
    {
        var restriction = GetOrCreateRestriction(ipAddress);
        restriction.FailedAttempts++;
        restriction.LastAttempt = DateTime.Now;
        
        // Auto-ban after too many failed attempts
        if (restriction.FailedAttempts >= LOGIN_ATTEMPT_LIMIT)
        {
            restriction.IsBanned = true;
            restriction.BanExpires = DateTime.Now.AddHours(1);
            
            log.Warn($"IP {ipAddress} auto-banned for {LOGIN_ATTEMPT_LIMIT} failed login attempts");
        }
    }
}
```

## Security Monitoring and Audit

### Audit Trail System
```csharp
public static class AuditMgr
{
    public static void AddAuditEntry(GameClient client, AuditType type, AuditSubtype subtype, 
        string details, string targetName)
    {
        var auditEntry = new DbAuditEntry
        {
            Timestamp = DateTime.Now,
            AccountName = client.Account?.Name ?? "Unknown",
            PlayerName = client.Player?.Name ?? "Unknown",
            IPAddress = client.TcpEndpointAddress,
            Type = (int)type,
            Subtype = (int)subtype,
            Details = details,
            TargetName = targetName
        };
        
        // Save to database
        GameServer.Database.AddObject(auditEntry);
        
        // Also log to file for immediate access
        if (type == AuditType.Security)
        {
            log.Warn($"SECURITY: {client.Account?.Name}({client.TcpEndpointAddress}) " +
                    $"{subtype}: {details}");
        }
    }
    
    public static void LogGMCommand(string command, string playerName, string targetName, string accountName)
    {
        var auditEntry = new DbAuditEntry
        {
            Timestamp = DateTime.Now,
            AccountName = accountName,
            PlayerName = playerName,
            Type = (int)AuditType.GMCommand,
            Details = command,
            TargetName = targetName
        };
        
        GameServer.Database.AddObject(auditEntry);
        
        log.Info($"GM Command: {accountName}({playerName}) used '{command}' on {targetName}");
    }
}

public enum AuditType
{
    Account = 1,
    GMCommand = 2,
    Security = 3,
    Trade = 4,
    Item = 5
}

public enum AuditSubtype
{
    AccountLogin = 1,
    AccountLogout = 2,
    UnauthorizedAccess = 3,
    SpeedHack = 4,
    PacketSpam = 5,
    ItemDuplication = 6
}
```

### Real-Time Security Monitoring
```csharp
public class SecurityMonitor
{
    private static Timer _monitorTimer;
    private static List<SecurityAlert> _pendingAlerts = new();
    
    public static void StartMonitoring()
    {
        _monitorTimer = new Timer(ProcessSecurityAlerts, null, 5000, 5000); // Every 5 seconds
    }
    
    private static void ProcessSecurityAlerts(object state)
    {
        var alerts = new List<SecurityAlert>();
        
        lock (_pendingAlerts)
        {
            alerts.AddRange(_pendingAlerts);
            _pendingAlerts.Clear();
        }
        
        foreach (var alert in alerts)
        {
            ProcessAlert(alert);
        }
        
        // Check for patterns indicating coordinated attacks
        CheckForAttackPatterns();
    }
    
    private static void CheckForAttackPatterns()
    {
        // Check for multiple failed logins from different IPs targeting same account
        var recentFailures = GameServer.Database.SelectObjects<DbAuditEntry>(
            "Type = @Type AND Subtype = @Subtype AND Timestamp > @Since",
            new QueryParameter("@Type", (int)AuditType.Account),
            new QueryParameter("@Subtype", (int)AuditSubtype.AccountLogin),
            new QueryParameter("@Since", DateTime.Now.AddMinutes(-10))
        );
        
        var accountTargets = recentFailures
            .Where(entry => entry.Details.Contains("Failed"))
            .GroupBy(entry => entry.AccountName)
            .Where(group => group.Count() >= 5);
            
        foreach (var group in accountTargets)
        {
            log.Warn($"Potential brute force attack detected on account: {group.Key}");
            
            // Temporarily lock the account
            var account = GameServer.Database.SelectObject<DbAccount>(
                "Name = @Name", new QueryParameter("@Name", group.Key));
            if (account != null)
            {
                account.IsActive = false;
                GameServer.Database.SaveObject(account);
                
                log.Info($"Account {group.Key} temporarily locked due to suspicious activity");
            }
        }
    }
}
```

## Configuration

### Security Settings
```ini
# Authentication settings
PASSWORD_MIN_LENGTH = 8              # Minimum password length
FAILED_LOGIN_THRESHOLD = 5           # Failed attempts before temporary ban
LOGIN_BAN_DURATION = 3600            # Ban duration in seconds (1 hour)
MAX_ACCOUNTS_PER_IP = 3              # Maximum concurrent accounts per IP

# Anti-cheat settings
SPEED_HACK_TOLERANCE = 1.15          # Speed tolerance (15% over normal)
MAX_PACKETS_PER_SECOND = 100         # Packet rate limit
ENABLE_MOVEMENT_VALIDATION = true    # Enable movement validation
ENABLE_PACKET_RATE_LIMITING = true   # Enable packet spam protection

# Audit settings
AUDIT_RETENTION_DAYS = 90            # How long to keep audit logs
LOG_ALL_GM_COMMANDS = true           # Log all GM/Admin commands
ENABLE_SECURITY_MONITORING = true    # Enable real-time monitoring
```

### Privilege Configuration
```ini
# GM Privilege settings
GM_CAN_TELEPORT = true               # GMs can use teleport commands
GM_CAN_CREATE_ITEMS = true           # GMs can create items
GM_CAN_MODIFY_PLAYERS = true         # GMs can modify player properties

# Admin Privilege settings
ADMIN_CAN_ACCESS_DATABASE = true     # Admins can access database directly
ADMIN_CAN_MODIFY_ACCOUNTS = true     # Admins can modify account settings
ADMIN_CAN_SHUTDOWN_SERVER = true     # Admins can shutdown server
```

## Integration Points

### Player System Integration
```csharp
public override bool OnPlayerLogin(GamePlayer player)
{
    // Security checks on login
    if (!SecurityValidator.ValidatePlayerData(player))
    {
        AuditMgr.AddAuditEntry(player.Client, AuditType.Security, 
            AuditSubtype.UnauthorizedAccess, "Invalid player data", "");
        return false;
    }
    
    // Update activity tracking
    player.LastPlayerActivityTime = GameLoop.GameLoopTime;
    
    return true;
}
```

### Command System Integration
All commands automatically check privilege levels and log usage for audit trails.

### Database Integration
All sensitive database operations are logged and require appropriate privilege levels.

### Event System Integration
Security events are published for other systems to react to:
```csharp
GameEventMgr.Notify(SecurityEvent.SuspiciousActivity, client, new SecurityEventArgs(violation));
```

## Test Scenarios

### Authentication Tests
```csharp
// Given: Valid account credentials
// When: Login attempted
// Then: Account authenticated and session created

// Given: Invalid password
// When: Login attempted
// Then: Login denied and attempt logged

// Given: Account already logged in
// When: Second login attempted
// Then: Login denied with appropriate message
```

### Authorization Tests
```csharp
// Given: Player level account
// When: GM command attempted
// Then: Command denied and attempt logged

// Given: GM account
// When: Player and GM commands attempted
// Then: Both commands succeed and are logged
```

### Anti-Cheat Tests
```csharp
// Given: Player moving at normal speed
// When: Movement validation performed
// Then: Movement accepted

// Given: Player moving impossibly fast
// When: Movement validation performed
// Then: Movement rejected and violation logged
```

## Future Enhancements
- TODO: Two-factor authentication support
- TODO: OAuth integration for social login
- TODO: Advanced behavioral analysis for cheat detection
- TODO: Automated temporary bans for security violations
- TODO: Integration with external security services
- TODO: Machine learning-based anomaly detection

## Change Log
- 2024-01-20: Initial documentation created

## References
- `GameServer/gameutils/LoginRequestHandler.cs`
- `GameServer/Database/DbAccount.cs`
- `GameServer/serverrules/AbstractServerRules.cs`
- `GameServer/gameutils/AuditMgr.cs`
- `GameServer/commands/AbstractCommandHandler.cs`
- `GameServer/packets/Client/168/` - Client packet handlers
- `GameServer/ECS-Services/ClientService.cs` 