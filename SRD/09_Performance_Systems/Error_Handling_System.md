# Error Handling & Recovery System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

The Error Handling & Recovery System provides comprehensive exception management, crash detection, automated recovery, and system stability features. It includes client crash reporting, database error recovery, network error handling, and graceful degradation mechanisms.

## Core Architecture

### Exception Hierarchy

```csharp
// Base database exception
public class DatabaseException : ApplicationException
{
    public DatabaseException(Exception baseException) : base(string.Empty, baseException) { }
    public DatabaseException(string message, Exception baseException) : base(message, baseException) { }
    public DatabaseException(string message) : base(message) { }
}

// Network-related exceptions
public class NetworkException : ApplicationException
{
    public string ClientAddress { get; }
    public int ErrorCode { get; }
    
    public NetworkException(string clientAddress, int errorCode, string message) 
        : base(message)
    {
        ClientAddress = clientAddress;
        ErrorCode = errorCode;
    }
}

// Game logic exceptions
public class GameLogicException : ApplicationException
{
    public string PlayerName { get; }
    public string GameContext { get; }
    
    public GameLogicException(string playerName, string gameContext, string message)
        : base(message)
    {
        PlayerName = playerName;
        GameContext = gameContext;
    }
}
```

## Client Crash Detection

### Client Crash Packet Handler

```csharp
[PacketHandler(PacketHandlerType.TCP, eClientPackets.ClientCrash, "Handles client crash packets")]
public class ClientCrashPacketHandler : IPacketHandler
{
    private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
    
    public void HandlePacket(GameClient client, GSPacketIn packet)
    {
        try
        {
            string dllName = packet.ReadString(16);
            packet.Position = 0x50;
            uint upTime = packet.ReadInt();
            
            string crashInfo = $"Client crash ({client}) dll:{dllName} clientUptime:{upTime}sec";
            
            if (log.IsInfoEnabled)
                log.Info(crashInfo);
                
            // Store crash data for analysis
            RecordClientCrash(client, dllName, upTime);
            
            // Debug packet logging if enabled
            if (log.IsDebugEnabled && Properties.SAVE_PACKETS)
            {
                log.Debug("Last client sent/received packets (from older to newer):");
                foreach (IPacket packet in client.PacketProcessor.GetLastPackets())
                {
                    log.Debug(packet.ToHumanReadable());
                }
            }
            
            // Attempt graceful cleanup
            HandleClientCrashCleanup(client);
        }
        catch (Exception e)
        {
            log.Error($"Error handling client crash packet from {client}: {e}");
        }
    }
    
    private void RecordClientCrash(GameClient client, string dllName, uint upTime)
    {
        var crashRecord = new DbClientCrash
        {
            AccountName = client.Account?.Name ?? "Unknown",
            PlayerName = client.Player?.Name ?? "Unknown",
            IPAddress = client.TcpEndpointAddress,
            DllName = dllName,
            ClientUptime = upTime,
            CrashTime = DateTime.Now,
            ServerVersion = GameServer.Instance.Configuration.ServerVersion
        };
        
        GameServer.Database.AddObject(crashRecord);
    }
    
    private void HandleClientCrashCleanup(GameClient client)
    {
        if (client.Player != null)
        {
            // Save player state immediately
            client.Player.SaveIntoDatabase();
            
            // Clean up any active transactions
            CleanupPlayerTransactions(client.Player);
            
            // Notify other players if in group/guild
            NotifyPlayerDisconnection(client.Player, "crashed");
        }
    }
}
```

## Database Error Recovery

### SQL Exception Handling

```csharp
public abstract class SqlObjectDatabase : ObjectDatabase
{
    protected virtual bool HandleException(Exception e)
    {
        bool canRetry = false;
        
        // Handle socket exceptions (network issues)
        var socketException = ExtractSocketException(e);
        if (socketException != null)
        {
            canRetry = HandleSocketException(socketException);
        }
        
        return canRetry;
    }
    
    private bool HandleSocketException(SocketException socketException)
    {
        // Network error codes that allow retry
        switch (socketException.ErrorCode)
        {
            case 10052: // Network dropped connection on reset
            case 10053: // Software caused connection abort
            case 10054: // Connection reset by peer
            case 10057: // Socket is not connected
            case 10058: // Cannot send after socket shutdown
                if (log.IsWarnEnabled)
                    log.Warn($"Socket exception (retryable): ({socketException.ErrorCode}) {socketException.Message}");
                return true;
                
            default:
                if (log.IsErrorEnabled)
                    log.Error($"Socket exception (fatal): ({socketException.ErrorCode}) {socketException.Message}");
                return false;
        }
    }
    
    protected override bool HandleSQLException(Exception e)
    {
        if (e is MySqlException mysqlEx)
        {
            return HandleMySqlException(mysqlEx);
        }
        else if (e is SQLiteException sqliteEx)
        {
            return HandleSQLiteException(sqliteEx);
        }
        
        return false;
    }
    
    private bool HandleMySqlException(MySqlException ex)
    {
        switch ((MySqlErrorCode)ex.Number)
        {
            case MySqlErrorCode.DuplicateUnique:
            case MySqlErrorCode.DuplicateKeyEntry:
                // Non-fatal constraint violations
                log.Warn($"MySQL constraint violation: {ex.Message}");
                return true;
                
            case MySqlErrorCode.LockWaitTimeout:
            case MySqlErrorCode.Deadlock:
                // Transient locking issues
                log.Warn($"MySQL locking issue (will retry): {ex.Message}");
                return true;
                
            default:
                log.Error($"MySQL error (fatal): {ex.Message}");
                return false;
        }
    }
    
    private bool HandleSQLiteException(SQLiteException ex)
    {
        switch (ex.ResultCode)
        {
            case SQLiteErrorCode.Constraint:
            case SQLiteErrorCode.Constraint_Check:
            case SQLiteErrorCode.Constraint_ForeignKey:
            case SQLiteErrorCode.Constraint_Unique:
            case SQLiteErrorCode.Constraint_NotNull:
                // Non-fatal constraint violations
                log.Warn($"SQLite constraint violation: {ex.Message}");
                return true;
                
            case SQLiteErrorCode.Locked:
            case SQLiteErrorCode.Busy:
                // Database is temporarily locked
                log.Warn($"SQLite database locked (will retry): {ex.Message}");
                Thread.Sleep(100); // Brief delay before retry
                return true;
                
            default:
                log.Error($"SQLite error (fatal): {ex.Message}");
                return false;
        }
    }
}
```

### Database Connection Recovery

```csharp
public class DatabaseConnectionManager
{
    private const int MAX_RETRY_ATTEMPTS = 3;
    private const int RETRY_DELAY_MS = 1000;
    
    public T ExecuteWithRetry<T>(Func<T> operation, string operationName)
    {
        int attempts = 0;
        Exception lastException = null;
        
        while (attempts < MAX_RETRY_ATTEMPTS)
        {
            try
            {
                return operation();
            }
            catch (Exception e)
            {
                attempts++;
                lastException = e;
                
                if (IsRetryableException(e) && attempts < MAX_RETRY_ATTEMPTS)
                {
                    log.Warn($"Database operation '{operationName}' failed (attempt {attempts}/{MAX_RETRY_ATTEMPTS}): {e.Message}");
                    Thread.Sleep(RETRY_DELAY_MS * attempts); // Exponential backoff
                    continue;
                }
                
                // Non-retryable or max attempts reached
                break;
            }
        }
        
        log.Error($"Database operation '{operationName}' failed after {attempts} attempts: {lastException?.Message}");
        throw new DatabaseException($"Operation '{operationName}' failed after {attempts} attempts", lastException);
    }
    
    private bool IsRetryableException(Exception e)
    {
        return e is SocketException ||
               (e is SQLiteException sqlite && IsRetryableSQLiteError(sqlite)) ||
               (e is MySqlException mysql && IsRetryableMySqlError(mysql));
    }
}
```

## Network Error Handling

### Socket Error Recovery

```csharp
public class NetworkErrorHandler
{
    public static void HandleSocketError(BaseClient client, SocketException e)
    {
        switch (e.SocketErrorCode)
        {
            case SocketError.ConnectionReset:
            case SocketError.ConnectionAborted:
                log.Debug($"Client {client} connection terminated: {e.SocketErrorCode}");
                client.Disconnect();
                break;
                
            case SocketError.TimedOut:
                log.Warn($"Client {client} connection timed out");
                client.Disconnect();
                break;
                
            case SocketError.NetworkDown:
            case SocketError.NetworkUnreachable:
                log.Warn($"Network issue with client {client}: {e.SocketErrorCode}");
                client.Disconnect();
                break;
                
            case SocketError.NoBufferSpaceAvailable:
                log.Error($"Server out of buffer space for client {client}");
                // Try to free resources before disconnecting
                FreeNetworkResources();
                client.Disconnect();
                break;
                
            default:
                log.Error($"Unhandled socket error for client {client}: {e.SocketErrorCode} - {e.Message}");
                client.Disconnect();
                break;
        }
    }
    
    private static void FreeNetworkResources()
    {
        // Force garbage collection to free network buffers
        GC.Collect();
        GC.WaitForPendingFinalizers();
        
        // Clean up disconnected clients
        ClientService.CleanupDisconnectedClients();
    }
}
```

### Packet Processing Error Recovery

```csharp
public class PacketErrorHandler
{
    public static void HandlePacketError(GameClient client, GSPacketIn packet, Exception e)
    {
        string errorContext = $"Packet 0x{packet.Code:X2} from {client.Player?.Name ?? client.TcpEndpointAddress}";
        
        // Classify error severity
        var severity = ClassifyError(e);
        
        switch (severity)
        {
            case ErrorSeverity.Low:
                log.Debug($"Minor packet error: {errorContext} - {e.Message}");
                break;
                
            case ErrorSeverity.Medium:
                log.Warn($"Packet processing warning: {errorContext} - {e.Message}");
                // Send error message to client
                client.Out.SendMessage("A minor error occurred. Please try again.", 
                                      eChatType.CT_System, eChatLoc.CL_SystemWindow);
                break;
                
            case ErrorSeverity.High:
                log.Error($"Serious packet error: {errorContext}", e);
                // Save player state and disconnect
                if (client.Player != null)
                {
                    client.Player.SaveIntoDatabase();
                }
                client.Disconnect();
                break;
                
            case ErrorSeverity.Critical:
                log.Fatal($"Critical packet error: {errorContext}", e);
                // Emergency save and server shutdown consideration
                EmergencyPlayerSave(client);
                
                if (ShouldShutdownServer(e))
                {
                    GameServer.Instance.Stop();
                }
                break;
        }
    }
    
    private static ErrorSeverity ClassifyError(Exception e)
    {
        if (e is ArgumentException || e is FormatException)
            return ErrorSeverity.Low;
            
        if (e is InvalidOperationException || e is NotSupportedException)
            return ErrorSeverity.Medium;
            
        if (e is GameLogicException)
            return ErrorSeverity.High;
            
        if (e is OutOfMemoryException || e is StackOverflowException)
            return ErrorSeverity.Critical;
            
        return ErrorSeverity.Medium; // Default
    }
}

public enum ErrorSeverity
{
    Low,
    Medium,
    High,
    Critical
}
```

## Game Loop Error Handling

### Thread Pool Error Recovery

```csharp
public class GameLoopErrorHandler
{
    public static void HandleGameLoopException(Exception e)
    {
        if (log.IsFatalEnabled)
            log.Fatal($"Critical error encountered in {nameof(GameLoop)}: {e}");
            
        // Attempt to save critical data before shutdown
        EmergencyDataSave();
        
        // Notify all players
        NotifyPlayersOfShutdown("A critical error has occurred. The server will restart.");
        
        // Graceful shutdown
        GameServer.Instance.Stop();
    }
    
    public static void HandleWorkerThreadException(Exception e, int workerId)
    {
        if (log.IsFatalEnabled)
            log.Fatal($"Critical error in worker thread {workerId}: {e}");
            
        // Try to restart the worker thread
        if (CanRestartWorkerThread(workerId))
        {
            RestartWorkerThread(workerId);
            log.Info($"Successfully restarted worker thread {workerId}");
        }
        else
        {
            // Can't recover, shutdown server
            GameServer.Instance.Stop();
        }
    }
    
    private static void EmergencyDataSave()
    {
        try
        {
            log.Info("Performing emergency data save...");
            
            // Save all online players immediately
            ClientService.SaveAllPlayers();
            
            // Save critical game state
            DoorMgr.SaveKeepDoors();
            GuildMgr.SaveAllGuilds();
            
            log.Info("Emergency data save completed");
        }
        catch (Exception e)
        {
            log.Fatal($"Emergency data save failed: {e}");
        }
    }
}
```

## Service Error Handling

### Service Exception Management

```csharp
public static class ServiceUtils
{
    public static void HandleServiceException<T>(Exception exception, string serviceName, T entity, GameObject entityOwner) 
        where T : class, IServiceObject
    {
        // Remove problematic entity from service
        if (entity != null)
            ServiceObjectStore.Remove(entity);
            
        List<string> logMessages = new List<string>
        {
            $"Critical error encountered in {serviceName}: {exception}"
        };
        
        string actionMessage;
        Action action;
        
        if (entityOwner is GamePlayer player)
        {
            actionMessage = $"Saving and disconnecting player {player.Name}";
            action = () =>
            {
                try
                {
                    player.SaveIntoDatabase();
                    player.Client.Disconnect();
                }
                catch (Exception saveEx)
                {
                    log.Error($"Error saving player during service error recovery: {saveEx}");
                }
            };
        }
        else if (entityOwner is GameNPC npc)
        {
            actionMessage = $"Removing NPC {npc.Name} from world";
            action = () =>
            {
                try
                {
                    npc.RemoveFromWorld();
                    npc.Delete();
                }
                catch (Exception removeEx)
                {
                    log.Error($"Error removing NPC during service error recovery: {removeEx}");
                }
            };
        }
        else
        {
            actionMessage = "No recovery action available";
            action = () => { };
        }
        
        logMessages.Add(actionMessage);
        
        foreach (string message in logMessages)
        {
            log.Error(message);
        }
        
        // Execute recovery action
        try
        {
            action();
        }
        catch (Exception recoveryEx)
        {
            log.Fatal($"Service error recovery failed: {recoveryEx}");
        }
    }
}
```

## Graceful Degradation

### Performance-Based Error Response

```csharp
public class GracefulDegradation
{
    private static readonly Dictionary<string, int> _errorCounts = new();
    private static readonly Dictionary<string, DateTime> _lastErrorTime = new();
    
    public static bool ShouldGracefullyDegrade(string systemName, Exception e)
    {
        var currentTime = DateTime.Now;
        var errorKey = $"{systemName}:{e.GetType().Name}";
        
        // Track error frequency
        if (!_errorCounts.ContainsKey(errorKey))
        {
            _errorCounts[errorKey] = 0;
            _lastErrorTime[errorKey] = currentTime;
        }
        
        _errorCounts[errorKey]++;
        var timeSinceLastError = currentTime - _lastErrorTime[errorKey];
        _lastErrorTime[errorKey] = currentTime;
        
        // If errors are frequent, enable degradation
        if (_errorCounts[errorKey] > 5 && timeSinceLastError.TotalMinutes < 5)
        {
            log.Warn($"Enabling graceful degradation for {systemName} due to frequent errors");
            return true;
        }
        
        // Reset counter if errors have stopped
        if (timeSinceLastError.TotalMinutes > 15)
        {
            _errorCounts[errorKey] = 1;
        }
        
        return false;
    }
    
    public static void ApplyDegradation(string systemName, DegradationLevel level)
    {
        switch (level)
        {
            case DegradationLevel.Minimal:
                // Reduce update frequency
                ReduceUpdateFrequency(systemName, 0.5);
                break;
                
            case DegradationLevel.Moderate:
                // Disable non-essential features
                DisableNonEssentialFeatures(systemName);
                break;
                
            case DegradationLevel.Severe:
                // Minimal functionality only
                EnableEmergencyMode(systemName);
                break;
        }
    }
}

public enum DegradationLevel
{
    None,
    Minimal,
    Moderate,
    Severe
}
```

## Error Reporting and Analytics

### Error Aggregation

```csharp
public class ErrorAnalytics
{
    private static readonly Dictionary<string, ErrorStats> _errorStatistics = new();
    
    public static void RecordError(string category, Exception e, string context = null)
    {
        var errorKey = $"{category}:{e.GetType().Name}";
        
        if (!_errorStatistics.ContainsKey(errorKey))
        {
            _errorStatistics[errorKey] = new ErrorStats
            {
                Category = category,
                ExceptionType = e.GetType().Name,
                FirstOccurrence = DateTime.Now
            };
        }
        
        var stats = _errorStatistics[errorKey];
        stats.Count++;
        stats.LastOccurrence = DateTime.Now;
        stats.LastMessage = e.Message;
        stats.LastContext = context;
    }
    
    public static void GenerateErrorReport()
    {
        var report = new StringBuilder();
        report.AppendLine("Error Statistics Report");
        report.AppendLine("=====================");
        report.AppendLine($"Generated: {DateTime.Now}");
        report.AppendLine();
        
        foreach (var kvp in _errorStatistics.OrderByDescending(e => e.Value.Count))
        {
            var stats = kvp.Value;
            report.AppendLine($"{stats.Category} - {stats.ExceptionType}:");
            report.AppendLine($"  Count: {stats.Count}");
            report.AppendLine($"  First: {stats.FirstOccurrence}");
            report.AppendLine($"  Last: {stats.LastOccurrence}");
            report.AppendLine($"  Message: {stats.LastMessage}");
            if (!string.IsNullOrEmpty(stats.LastContext))
                report.AppendLine($"  Context: {stats.LastContext}");
            report.AppendLine();
        }
        
        log.Info(report.ToString());
    }
}

public class ErrorStats
{
    public string Category { get; set; }
    public string ExceptionType { get; set; }
    public int Count { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
    public string LastMessage { get; set; }
    public string LastContext { get; set; }
}
```

## Configuration

### Error Handling Settings

```csharp
[ServerProperty("error", "enable_crash_reporting", "Enable client crash reporting", true)]
public static bool ENABLE_CRASH_REPORTING;

[ServerProperty("error", "max_retry_attempts", "Maximum retry attempts for database operations", 3)]
public static int MAX_RETRY_ATTEMPTS;

[ServerProperty("error", "enable_graceful_degradation", "Enable graceful degradation on errors", true)]
public static bool ENABLE_GRACEFUL_DEGRADATION;

[ServerProperty("error", "emergency_save_on_crash", "Emergency save on critical errors", true)]
public static bool EMERGENCY_SAVE_ON_CRASH;

[ServerProperty("error", "error_report_interval", "Error report generation interval (minutes)", 60)]
public static int ERROR_REPORT_INTERVAL;
```

## Implementation Status

**Completed**:
- ✅ Client crash detection and handling
- ✅ Database error recovery with retry logic
- ✅ Network error classification and recovery
- ✅ Game loop exception handling
- ✅ Service error management
- ✅ Graceful degradation mechanisms
- ✅ Error analytics and reporting

**In Progress**:
- 🔄 Automated error pattern recognition
- 🔄 Predictive error prevention
- 🔄 Cross-server error correlation

**Planned**:
- ⏳ Machine learning error prediction
- ⏳ Automated recovery procedures
- ⏳ Real-time error dashboard

## References

- **Client Crash Handler**: `GameServer/packets/Client/168/ClientCrashPacketHandler.cs`
- **Database Error Handling**: `CoreDatabase/SqlObjectDatabase.cs`
- **Network Error Recovery**: `CoreBase/Network/BaseClient.cs`
- **Service Error Utils**: `GameServer/ECS-Services/ServiceUtils.cs` 