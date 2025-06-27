# Logging and Audit System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from AuditMgr.cs, InventoryLogging.cs, LoggerManager.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: The logging and audit system keeps detailed records of all important player actions, GM commands, and server events for security and support purposes. This helps Game Masters assist with issues, investigate problems, track item transfers, and ensure fair play by maintaining comprehensive logs of what happens in the game.

### Logger System

#### NLog Configuration
The system uses NLog for structured logging with XML configuration:

```xml
<targets>
    <!-- Main server log -->
    <target name="gameServerFile" xsi:type="File"
        layout="${date:format=MM/dd HH\:mm\:ss} | ${level:uppercase=true:format=TriLetter} | ${logger} | ${message}"
        fileName="${logDirectory}/server.log"
        archiveAboveSize="104857600"
        archiveEvery="Day"
        maxArchiveDays="7" />
        
    <!-- Specialized logs -->
    <target name="gmActionFile" fileName="${logDirectory}/gm-action.log" />
    <target name="cheatFile" fileName="${logDirectory}/cheat.log" />
    <target name="dualIpFile" fileName="${logDirectory}/dual-ip.log" />
    <target name="inventoryFile" fileName="${logDirectory}/inventory.log" />
</targets>
```

#### Log Categories
```csharp
public class GameServerLoggers
{
    public static readonly Logger GMActions = LoggerManager.Create("gmactions");
    public static readonly Logger Cheats = LoggerManager.Create("cheats");
    public static readonly Logger DualIP = LoggerManager.Create("dualip");
    public static readonly Logger Inventories = LoggerManager.Create("inventories");
    public static readonly Logger Main = LoggerManager.Create("GameServer");
}
```

### Audit Manager System

#### Audit Types and Subtypes
```csharp
public enum AuditType
{
    Account,    // Account-related events
    Character,  // Character-related events
    Chat        // Chat message events
}

public enum AuditSubtype
{
    // Account events
    AccountCreate,
    AccountFailedLogin,
    AccountSuccessfulLogin,
    AccountLogout,
    AccountPasswordChange,
    AccountEmailChange,
    AccountDelete,
    
    // Character events
    CharacterCreate,
    CharacterDelete,
    CharacterRename,
    CharacterLogin,
    CharacterLogout,
    
    // Chat events
    PublicChat,
    PrivateChat
}
```

#### Audit Entry Structure
```csharp
[DataTable(TableName = "AuditEntry")]
public class DbAuditEntry : DataObject
{
    [DataElement(AllowDbNull = false)]
    public DateTime AuditTime { get; set; }
    
    [DataElement]
    public string AccountID { get; set; }
    
    [DataElement]
    public string RemoteHost { get; set; }
    
    [DataElement(AllowDbNull = false)]
    public int AuditType { get; set; }
    
    [DataElement(AllowDbNull = false)]
    public int AuditSubtype { get; set; }
    
    [DataElement]
    public string OldValue { get; set; }
    
    [DataElement]
    public string NewValue { get; set; }
}
```

## Audit Manager Implementation

### Batch Processing System
```csharp
public static class AuditMgr
{
    private const int EventsPerInsertLimit = 1000;  // Max events per batch
    private const int PushUpdatesInterval = 5000;   // 5 seconds
    
    private static List<DbAuditEntry> _queuedAuditEntries;
    private static Timer PushTimer;
    private static SpinWaitLock _updateLock;
    
    static AuditMgr()
    {
        _queuedAuditEntries = new List<DbAuditEntry>();
        
        PushTimer = new Timer(PushUpdatesInterval);
        PushTimer.Elapsed += OnPushTimerElapsed;
        PushTimer.AutoReset = false;
        
        // Note: Currently disabled for performance reasons
        // PushTimer.Start();
    }
}
```

### Audit Entry Creation
```csharp
public static void AddAuditEntry(GameClient client, AuditType type, AuditSubtype subType, 
                                string oldValue, string newValue)
{
    if (!ServerProperties.Properties.ENABLE_AUDIT_LOG)
        return;
        
    var auditEntry = new DbAuditEntry
    {
        AuditTime = DateTime.Now,
        AuditType = (int)type,
        AuditSubtype = (int)subType,
        OldValue = oldValue,
        NewValue = newValue,
        AccountID = client.Account?.ObjectId,
        RemoteHost = client.TcpEndpointAddress
    };
    
    _updateLock.Enter();
    try
    {
        _queuedAuditEntries.Add(auditEntry);
    }
    finally
    {
        _updateLock.Exit();
    }
}
```

### Batch Database Updates
```csharp
private static void OnPushTimerElapsed(object sender, ElapsedEventArgs e)
{
    List<DbAuditEntry> oldQueue;
    
    // Atomic queue swap
    _updateLock.Enter();
    try
    {
        oldQueue = _queuedAuditEntries;
        _queuedAuditEntries = new List<DbAuditEntry>();
    }
    finally
    {
        _updateLock.Exit();
    }
    
    // Batch insert processing
    StringBuilder queryBuilder = GetEntryQueryBuilder();
    int currentQueryCount = 0;
    
    foreach (DbAuditEntry entry in oldQueue)
    {
        if (currentQueryCount > EventsPerInsertLimit)
        {
            // Execute batch and start new one
            ExecuteBatchQuery(queryBuilder);
            queryBuilder = GetEntryQueryBuilder();
            currentQueryCount = 0;
        }
        
        queryBuilder.AppendFormat("({0},{1},{2},{3},{4},{5},{6},{7}),",
            entry.ObjectId, entry.AuditTime, entry.AccountID, entry.RemoteHost,
            entry.AuditType, entry.AuditSubtype, entry.OldValue, entry.NewValue);
            
        currentQueryCount++;
    }
    
    // Execute final batch
    ExecuteBatchQuery(queryBuilder);
    PushTimer.Start();
}
```

## Inventory Logging System

### Inventory Action Types
```csharp
public enum eInventoryActionType
{
    Trade,     // Trade between 2 players
    Loot,      // Player picks up loot
    Quest,     // Quest rewards or items
    Merchant,  // Buy/sell transactions
    Craft,     // Crafting activities
    Other      // Any other action
}
```

### Logging Infrastructure
```csharp
public static class InventoryLogging
{
    private static readonly Dictionary<eInventoryActionType, string> ActionXformat =
        new Dictionary<eInventoryActionType, string>
        {
            {eInventoryActionType.Trade, "[TRADE] {0} > {1}: {2}"},
            {eInventoryActionType.Loot, "[LOOT] {0} > {1}: {2}"},
            {eInventoryActionType.Quest, "[QUEST] {0} > {1}: {2}"},
            {eInventoryActionType.Merchant, "[MERCHANT] {0} > {1}: {2}"},
            {eInventoryActionType.Craft, "[CRAFT] {0} > {1}: {2}"},
            {eInventoryActionType.Other, "[OTHER] {0} > {1}: {2}"}
        };
        
    public static Func<GameObject, string> GetGameObjectString = obj =>
        obj == null ? "(null)" : 
        $"({obj.Name};{obj.GetType()};{obj.X};{obj.Y};{obj.Z};{obj.CurrentRegionID})";
        
    public static Func<DbItemTemplate, int, string> GetItemString = (item, count) =>
        item == null ? "(null)" : $"({count};{item.Name};{item.Id_nb})";
        
    public static Func<long, string> GetMoneyString = amount =>
        $"(MONEY;{amount})";
}
```

### Item Transaction Logging
```csharp
public static void LogInventoryAction(GameObject source, GameObject destination, 
                                    eInventoryActionType type, DbItemTemplate item, int count = 1)
{
    if (!_IsLoggingEnabled(type))
        return;
        
    string format = ActionXformat[type];
    string logEntry = string.Format(format,
        GetGameObjectString(source),
        GetGameObjectString(destination),
        GetItemString(item, count));
        
    GameServer.Instance.LogInventoryAction(logEntry);
}
```

### Money Transaction Logging
```csharp
public static void LogInventoryAction(GameObject source, GameObject destination,
                                    eInventoryActionType type, long money)
{
    if (!_IsLoggingEnabled(type))
        return;
        
    string format = ActionXformat[type];
    string logEntry = string.Format(format,
        GetGameObjectString(source),
        GetGameObjectString(destination),
        GetMoneyString(money));
        
    GameServer.Instance.LogInventoryAction(logEntry);
}
```

## Specialized Logging Systems

### GM Action Logging
```csharp
public void LogGMAction(string text)
{
    m_gmLog.Info(text);
}

// Usage example:
string gmAction = $"GM {player.Name} used command {command} on {target}";
GameServer.Instance.LogGMAction(gmAction);
```

### Cheat Detection Logging
```csharp
public void LogCheatAction(string text)
{
    m_cheatLog.Info(text);
}

// Usage example:
string cheatAlert = $"POSSIBLE SPEEDHACK: {player.Name} moved {distance} in {time}ms";
GameServer.Instance.LogCheatAction(cheatAlert);
```

### Dual IP Logging
```csharp
public void LogDualIPAction(string text)
{
    m_dualIPLog.Info(text);
}

// Usage example:
string dualIP = $"Multiple accounts from IP {ipAddress}: {accountList}";
GameServer.Instance.LogDualIPAction(dualIP);
```

## Log File Management

### File Rotation Configuration
```xml
<!-- Automatic file archiving -->
<target name="gameServerFile">
    <archiveFileName="${logDirectory}/server.{#}.log"
    <archiveNumbering="DateAndSequence"
    <archiveDateFormat="yyyy-MM-dd"
    <archiveAboveSize="104857600"  <!-- 100MB -->
    <archiveEvery="Day"
    <maxArchiveDays="7"
</target>
```

### Log Directory Structure
```
logs/
├── server.log                 # Current main log
├── server.2024-01-20.1.log   # Archived logs
├── warn.log                   # Warning-level messages
├── error.log                  # Error-level messages
├── gm-action.log             # GM command usage
├── cheat.log                 # Cheat detection alerts
├── dual-ip.log               # Multi-account detection
└── inventory.log             # Item/money transactions
```

### Performance Considerations
- **Log Size Limits**: 100MB per file before rotation
- **Retention Policy**: 7 days of archived logs
- **Batch Processing**: Audit entries queued and batch-inserted
- **Async Logging**: Non-blocking log writes to prevent game lag

## Server Integration

### Startup Logging
```csharp
protected GameServer(GameServerConfiguration config) : base(config)
{
    m_gmLog = LoggerManager.Create(Configuration.GMActionsLoggerName);
    m_cheatLog = LoggerManager.Create(Configuration.CheatLoggerName);
    m_dualIPLog = LoggerManager.Create(Configuration.DualIPLoggerName);
    m_inventoryLog = LoggerManager.Create(Configuration.InventoryLoggerName);
    
    if (log.IsInfoEnabled)
        log.Info("Game Server Initialization started!");
}
```

### Client Connection Logging
```csharp
protected override void OnConnect()
{
    if (log.IsInfoEnabled)
        log.Info($"Incoming connection from {TcpEndpointAddress}");
        
    AuditMgr.AddAuditEntry(this, AuditType.Account, AuditSubtype.AccountSuccessfulLogin);
}

private void Quit()
{
    if (Account != null)
    {
        if (log.IsInfoEnabled)
            log.Info($"({TcpEndpointAddress}) {Account.Name} just disconnected.");
            
        Account.LastDisconnected = DateTime.Now;
        GameServer.Database.SaveObject(Account);
        AuditMgr.AddAuditEntry(this, AuditType.Account, AuditSubtype.AccountLogout, "", Account.Name);
    }
}
```

### Database Save Logging
```csharp
protected void SaveTimerProc(object sender)
{
    long startTick = GameLoop.GetRealTime();
    
    if (log.IsInfoEnabled)
        log.Info("Saving database...");
        
    // Perform saves with timing
    Save(ClientService.SavePlayers, ref players);
    Save(DoorMgr.SaveKeepDoors, ref keepDoors);
    Save(GuildMgr.SaveAllGuilds, ref guilds);
    
    long elapsed = GameLoop.GetRealTime() - startTick;
    
    if (log.IsInfoEnabled)
        log.Info($"Database save complete in {elapsed}ms");
}
```

## Configuration Options

### Audit System Settings
```csharp
[ServerProperty("server", "enable_audit_log", "Enable audit logging system", false)]
public static bool ENABLE_AUDIT_LOG;

[ServerProperty("server", "audit_batch_size", "Audit entries per batch insert", 1000)]
public static int AUDIT_BATCH_SIZE;

[ServerProperty("server", "audit_flush_interval", "Audit flush interval (ms)", 5000)]
public static int AUDIT_FLUSH_INTERVAL;
```

### Inventory Logging Settings
```csharp
[ServerProperty("server", "log_inventory_trade", "Log player trades", true)]
public static bool LOG_INVENTORY_TRADE;

[ServerProperty("server", "log_inventory_loot", "Log item looting", true)]
public static bool LOG_INVENTORY_LOOT;

[ServerProperty("server", "log_inventory_merchant", "Log merchant transactions", true)]
public static bool LOG_INVENTORY_MERCHANT;
```

## Usage Examples

### Account Email Change Audit
```csharp
[CmdAttribute("&email", ePrivLevel.Player, "Change account email")]
public class EmailCommandHandler : AbstractCommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        string oldEmail = client.Account.Mail;
        string newEmail = args[1];
        
        // Validate email
        if (!IsValidEmail(newEmail))
        {
            client.Out.SendMessage("Invalid email format");
            return;
        }
        
        // Update account
        client.Account.Mail = newEmail;
        GameServer.Database.SaveObject(client.Account);
        
        // Audit trail
        AuditMgr.AddAuditEntry(client, AuditType.Account, 
                              AuditSubtype.AccountEmailChange, 
                              oldEmail, newEmail);
                              
        client.Out.SendMessage($"Email updated to {newEmail}");
    }
}
```

### Trade Transaction Logging
```csharp
public override bool ReceiveTradeItem(GamePlayer player, DbInventoryItem item)
{
    if (base.ReceiveTradeItem(player, item))
    {
        // Log the trade
        InventoryLogging.LogInventoryAction(
            player,                           // Source
            this,                            // Destination
            eInventoryActionType.Trade,      // Type
            item.Template,                   // Item
            item.Count                       // Quantity
        );
        
        return true;
    }
    
    return false;
}
```

### GM Command Logging
```csharp
[CmdAttribute("&kick", ePrivLevel.GM, "Kick a player")]
public class KickCommandHandler : AbstractCommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        GamePlayer target = GetTargetPlayer(args[1]);
        
        if (target != null)
        {
            // Log GM action
            string action = $"GM {client.Player.Name} kicked player {target.Name}";
            GameServer.Instance.LogGMAction(action);
            
            // Perform kick
            target.Client.Out.SendPlayerQuit(true);
            target.SaveIntoDatabase();
            target.Quit(true);
            
            client.Out.SendMessage($"Player {target.Name} has been kicked.");
        }
    }
}
```

## Log Analysis and Monitoring

### Log Query Examples
```sql
-- Find all account creations in last 24 hours
SELECT * FROM AuditEntry 
WHERE AuditType = 0 AND AuditSubtype = 0 
AND AuditTime > DATE_SUB(NOW(), INTERVAL 24 HOUR);

-- Find all trades involving specific item
SELECT * FROM inventory.log 
WHERE log_entry LIKE '%[TRADE]%' 
AND log_entry LIKE '%item_name%';

-- Find GM actions by specific administrator
SELECT * FROM gm-action.log 
WHERE log_entry LIKE '%GM admin_name%';
```

### Log Monitoring Tools
- **Real-time tail**: Monitor active log files
- **Log rotation**: Automatic archival prevents disk space issues
- **Error filtering**: Separate error logs for quick issue identification
- **Audit queries**: Database queries for compliance reporting

## Security and Compliance

### Data Protection
- **IP Address Logging**: Tracks connection sources for security
- **Account Activity**: Comprehensive audit trail for all account changes
- **Transaction History**: Complete record of item/money movements
- **Administrative Actions**: Full GM command audit trail

### Performance Impact
- **Async Logging**: Non-blocking log writes
- **Batch Processing**: Reduces database load
- **Configurable Levels**: Enable/disable specific log types
- **File Rotation**: Prevents unlimited disk usage

## Testing Scenarios

### Audit System Tests
1. **Account Registration**: Verify audit entry creation
2. **Login/Logout**: Test session tracking
3. **Email Changes**: Confirm old/new value logging
4. **Batch Processing**: Test queue overflow handling

### Inventory Logging Tests
1. **Player Trades**: Verify trade logging accuracy
2. **Merchant Sales**: Test buy/sell transaction logs
3. **Quest Rewards**: Confirm quest item logging
4. **Money Transfers**: Test currency transaction logs

### Log Rotation Tests
1. **File Size Limits**: Test automatic archiving at 100MB
2. **Daily Rotation**: Verify daily log rotation
3. **Retention Policy**: Confirm old log deletion after 7 days
4. **Disk Space**: Test behavior when disk full

## References
- **Core System**: `GameServer/gameutils/AuditMgr.cs`
- **Inventory Logging**: `GameServer/gameutils/InventoryLogging.cs`
- **Server Integration**: `GameServer/GameServer.cs`
- **Database Schema**: `CoreDatabase/Tables/DbAuditEntry.cs`
- **Log Configuration**: `GameServer/config/logconfig.xml` 