# Backup & Recovery System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

The Backup & Recovery System provides comprehensive data protection for character information, ensuring data integrity during critical operations like character moves, database maintenance, and error recovery. It includes automated backup creation, database schema evolution, and recovery procedures.

## Core Architecture

### Character Backup System

```csharp
[DataTable(TableName = "DOLCharactersBackup")]
public class DbCoreCharacterBackup : DbCoreCharacter
{
    private string _dolCharactersID = string.Empty;
    private DateTime _deleteDate;
    
    public DbCoreCharacterBackup() : base()
    {
        _deleteDate = DateTime.Now;
    }
    
    public DbCoreCharacterBackup(DbCoreCharacter character) : base()
    {
        DOLCharacters_ID = character.ObjectId;
        DeleteDate = DateTime.Now;
        
        // Copy all character properties
        CopyCharacterData(character);
        
        // Copy custom parameters
        CustomParams = character.CustomParams?.Select(param => 
            new DbCoreCharacterBackupXCustomParam(param.DOLCharactersObjectId, param.KeyName, param.Value)
        ).ToArray() ?? new DbCoreCharacterBackupXCustomParam[0];
    }
    
    [DataElement(AllowDbNull = false, Varchar = 255)]
    public string DOLCharacters_ID
    {
        get => _dolCharactersID;
        set { Dirty = true; _dolCharactersID = value; }
    }
    
    [DataElement(AllowDbNull = false)]
    public DateTime DeleteDate
    {
        get => _deleteDate;
        set { Dirty = true; _deleteDate = value; }
    }
    
    // Name indexed but not unique for backups
    [DataElement(AllowDbNull = false, Index = true)]
    public override string Name
    {
        get => base.Name;
        set => base.Name = value;
    }
}
```

### Custom Parameter Backup

```csharp
[DataTable(TableName = "DOLCharactersBackupXCustomParam")]
public class DbCoreCharacterBackupXCustomParam : DbCoreCharacterXCustomParam
{
    public DbCoreCharacterBackupXCustomParam(string dolCharactersObjectId, string keyName, string value)
        : base(dolCharactersObjectId, keyName, value)
    {
    }
    
    public DbCoreCharacterBackupXCustomParam() { }
}
```

## Automated Backup Operations

### Character Slot Rearrangement Backup

```csharp
public class CharacterRearrangementBackup
{
    public static void CreateRearrangementBackup(DbCoreCharacter source, DbCoreCharacter target)
    {
        // Create backup of source character
        var sourceBackup = new DbCoreCharacterBackup(source);
        sourceBackup.DOLCharacters_ID += "-Rearranged"; // Easier for admins to find
        GameServer.Database.AddObject(sourceBackup);
        
        DbCoreCharacterBackup targetBackup = null;
        if (target != null)
        {
            targetBackup = new DbCoreCharacterBackup(target);
            targetBackup.DOLCharacters_ID += "-Rearranged";
            GameServer.Database.AddObject(targetBackup);
        }
        
        try
        {
            // Perform the slot changes
            PerformSlotRearrangement(source, target);
            
            // Clean up temporary backups on success
            GameServer.Database.DeleteObject(sourceBackup);
            if (targetBackup != null)
                GameServer.Database.DeleteObject(targetBackup);
        }
        catch (Exception ex)
        {
            log.Error($"Character rearrangement failed: {ex}");
            
            // Keep backups for manual recovery
            log.Warn($"Character backups preserved for manual recovery: {sourceBackup.DOLCharacters_ID}");
            throw;
        }
    }
    
    private static void PerformSlotRearrangement(DbCoreCharacter source, DbCoreCharacter target)
    {
        int sourceSlot = source.AccountSlot;
        int targetSlot = target?.AccountSlot ?? -1;
        
        // Delete original characters
        GameServer.Database.DeleteObject(source);
        if (target != null)
            GameServer.Database.DeleteObject(target);
        
        // Update slots
        source.AccountSlot = targetSlot != -1 ? targetSlot : source.AccountSlot;
        if (target != null)
            target.AccountSlot = sourceSlot;
        
        // Re-add characters with new slots
        GameServer.Database.AddObject(source);
        if (target != null)
            GameServer.Database.AddObject(target);
    }
}
```

## Database Schema Evolution

### Table Alteration with Backup

```csharp
public class SafeTableAlteration
{
    public static void AlterTableWithBackup(DataTableHandler table, string alterScript)
    {
        using var conn = CreateConnection();
        conn.Open();
        
        using var transaction = conn.BeginTransaction();
        try
        {
            // Step 1: Rename existing table to backup
            string backupTableName = $"{table.TableName}_bkp";
            using (var command = new SQLiteCommand($"ALTER TABLE `{table.TableName}` RENAME TO `{backupTableName}`", conn))
            {
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
            
            // Step 2: Create new table with updated schema
            using (var command = new SQLiteCommand(GetTableDefinition(table), conn))
            {
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
            
            // Step 3: Create indexes
            foreach (var indexDef in GetIndexesDefinition(table))
            {
                using (var command = new SQLiteCommand(indexDef, conn))
                {
                    command.Transaction = transaction;
                    command.ExecuteNonQuery();
                }
            }
            
            // Step 4: Migrate data from backup to new table
            var columnMapping = GenerateColumnMapping(table, backupTableName, conn, transaction);
            MigrateData(table.TableName, backupTableName, columnMapping, conn, transaction);
            
            // Step 5: Drop backup table
            using (var command = new SQLiteCommand($"DROP TABLE `{backupTableName}`", conn))
            {
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
            
            transaction.Commit();
            log.Info($"Successfully altered table {table.TableName}");
        }
        catch (Exception e)
        {
            transaction.Rollback();
            log.Warn($"Table alteration failed for {table.TableName}, rolled back: {e}");
            throw;
        }
    }
    
    private static void MigrateData(string newTableName, string backupTableName, 
                                  List<ColumnMapping> mapping, DbConnection conn, DbTransaction transaction)
    {
        string sourceColumns = string.Join(", ", mapping.Select(c => c.Source));
        string targetColumns = string.Join(", ", mapping.Select(c => c.Target));
        
        string insertQuery = $"INSERT INTO `{newTableName}` ({targetColumns}) SELECT {sourceColumns} FROM `{backupTableName}`";
        
        using var command = new SQLiteCommand(insertQuery, conn);
        command.Transaction = transaction;
        int rowsAffected = command.ExecuteNonQuery();
        
        log.Debug($"Migrated {rowsAffected} rows from {backupTableName} to {newTableName}");
    }
}

public class ColumnMapping
{
    public string Source { get; set; }
    public string Target { get; set; }
    public Type DataType { get; set; }
}
```

## Salvage System Migration

### Legacy to New System Migration

```csharp
[DatabaseUpdate]
public class SalvageYieldsUpdate : IDatabaseUpdater
{
    public void Update()
    {
        var newSalvage = GameServer.Database.SelectAllObjects<DbSalvageYield>();
        
        if (newSalvage == null || newSalvage.Count == 0)
        {
            log.Info("Migrating legacy salvage data to new SalvageYield table...");
            
            var oldSalvage = GameServer.Database.SelectAllObjects<DbSalvage>();
            
            if (oldSalvage != null && oldSalvage.Count > 0)
            {
                var newEntries = new List<DbSalvageYield>();
                
                foreach (var legacy in oldSalvage)
                {
                    var newEntry = new DbSalvageYield
                    {
                        ObjectType = legacy.ObjectType,
                        SalvageLevel = legacy.SalvageLevel,
                        MaterialId_nb = legacy.Id_nb,
                        Realm = legacy.Realm,
                        Count = 0, // Calculated in code
                        PackageID = "Migrated from legacy system"
                    };
                    
                    newEntries.Add(newEntry);
                }
                
                // Batch insert new entries
                foreach (var entry in newEntries)
                {
                    GameServer.Database.AddObject(entry);
                }
                
                log.Info($"Migrated {newEntries.Count} salvage entries to new system");
                
                // Mark legacy system as migrated
                var legacyMarker = new DbSalvageYield
                {
                    ID = -1,
                    PackageID = DbSalvageYield.LEGACY_SALVAGE_ID,
                    ObjectType = 0,
                    SalvageLevel = 0,
                    MaterialId_nb = "MIGRATED",
                    Count = newEntries.Count,
                    Realm = 0
                };
                
                GameServer.Database.AddObject(legacyMarker);
            }
        }
    }
}
```

## Periodic Save System

### Database Save Management

```csharp
public class DatabaseSaveManager
{
    private const int SAVE_INTERVAL_MS = 600000; // 10 minutes
    private readonly Timer _saveTimer;
    
    public DatabaseSaveManager()
    {
        _saveTimer = new Timer(SAVE_INTERVAL_MS);
        _saveTimer.Elapsed += SaveTimerProc;
        _saveTimer.AutoReset = true;
        _saveTimer.Start();
    }
    
    protected void SaveTimerProc(object sender, ElapsedEventArgs e)
    {
        ThreadPriority oldPriority = Thread.CurrentThread.Priority;
        
        try
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            
            long startTick = GameLoop.GetRealTime();
            
            if (log.IsInfoEnabled)
                log.Info("Starting database save...");
            
            var saveResults = new Dictionary<string, (int count, long elapsed)>();
            
            // Save all critical data
            SaveWithTiming("Players", ClientService.SavePlayers, saveResults);
            SaveWithTiming("KeepDoors", DoorMgr.SaveKeepDoors, saveResults);
            SaveWithTiming("Guilds", GuildMgr.SaveAllGuilds, saveResults);
            SaveWithTiming("Boats", BoatMgr.SaveAllBoats, saveResults);
            SaveWithTiming("Factions", FactionMgr.SaveAllAggroToFaction, saveResults);
            SaveWithTiming("Crafting", CraftingProgressMgr.Save, saveResults);
            SaveWithTiming("Appeals", AppealMgr.Save, saveResults);
            
            long totalElapsed = GameLoop.GetRealTime() - startTick;
            
            if (log.IsInfoEnabled)
            {
                log.Info($"Database save completed in {totalElapsed}ms:");
                foreach (var result in saveResults)
                {
                    log.Info($"  {result.Key}: {result.Value.count} objects in {result.Value.elapsed}ms");
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"Error during database save: {ex}");
        }
        finally
        {
            Thread.CurrentThread.Priority = oldPriority;
        }
    }
    
    private void SaveWithTiming(string name, Func<(int count, long elapsed)> saveFunction, 
                               Dictionary<string, (int count, long elapsed)> results)
    {
        try
        {
            var result = saveFunction();
            results[name] = result;
        }
        catch (Exception ex)
        {
            log.Error($"Error saving {name}: {ex}");
            results[name] = (0, 0);
        }
    }
}
```

## Error Recovery Procedures

### Database Exception Handling

```csharp
public class DatabaseErrorRecovery
{
    public static bool HandleDatabaseException(Exception e, string operation, DataObject dataObject)
    {
        bool canRecover = false;
        
        if (e is DatabaseException dbEx)
        {
            log.Error($"Database exception during {operation}: {dbEx.Message}");
            
            // Try to identify recoverable errors
            if (dbEx.InnerException is SqliteException sqliteEx)
            {
                canRecover = HandleSqliteException(sqliteEx, operation, dataObject);
            }
            else if (dbEx.InnerException is MySqlException mysqlEx)
            {
                canRecover = HandleMySqlException(mysqlEx, operation, dataObject);
            }
        }
        
        if (!canRecover)
        {
            // Create emergency backup
            CreateEmergencyBackup(dataObject, operation, e);
        }
        
        return canRecover;
    }
    
    private static bool HandleSqliteException(SqliteException ex, string operation, DataObject dataObject)
    {
        switch (ex.ResultCode)
        {
            case SQLiteErrorCode.Constraint:
            case SQLiteErrorCode.Constraint_Unique:
                log.Warn($"Constraint violation during {operation}, attempting recovery");
                return AttemptConstraintRecovery(dataObject);
                
            case SQLiteErrorCode.Constraint_ForeignKey:
                log.Warn($"Foreign key constraint violation during {operation}");
                return AttemptForeignKeyRecovery(dataObject);
                
            case SQLiteErrorCode.Locked:
            case SQLiteErrorCode.Busy:
                log.Warn($"Database locked during {operation}, will retry");
                return true; // Retry operation
                
            default:
                return false;
        }
    }
    
    private static void CreateEmergencyBackup(DataObject dataObject, string operation, Exception error)
    {
        try
        {
            var backupEntry = new DbEmergencyBackup
            {
                ObjectType = dataObject.GetType().Name,
                ObjectId = dataObject.ObjectId,
                Operation = operation,
                ErrorMessage = error.Message,
                BackupData = SerializeObject(dataObject),
                BackupTime = DateTime.Now
            };
            
            // Try to save backup to separate emergency table
            GameServer.Database.AddObject(backupEntry);
            
            log.Warn($"Created emergency backup for {dataObject.GetType().Name} ID {dataObject.ObjectId}");
        }
        catch (Exception backupEx)
        {
            log.Fatal($"Failed to create emergency backup: {backupEx}");
            
            // Last resort: save to file
            CreateFileBackup(dataObject, operation, error);
        }
    }
}
```

### File-Based Emergency Backup

```csharp
public class FileBackupManager
{
    private static readonly string BACKUP_DIRECTORY = Path.Combine(GameServerConfiguration.RootDirectory, "backups", "emergency");
    
    static FileBackupManager()
    {
        Directory.CreateDirectory(BACKUP_DIRECTORY);
    }
    
    public static void CreateFileBackup(DataObject dataObject, string operation, Exception error)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"{dataObject.GetType().Name}_{dataObject.ObjectId}_{timestamp}.backup";
            string filepath = Path.Combine(BACKUP_DIRECTORY, filename);
            
            var backupData = new
            {
                ObjectType = dataObject.GetType().Name,
                ObjectId = dataObject.ObjectId,
                Operation = operation,
                Error = error.Message,
                StackTrace = error.StackTrace,
                Timestamp = DateTime.Now,
                Data = SerializeObjectToDictionary(dataObject)
            };
            
            string json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filepath, json, Encoding.UTF8);
            
            log.Warn($"Created file backup: {filepath}");
        }
        catch (Exception ex)
        {
            log.Fatal($"Failed to create file backup: {ex}");
        }
    }
    
    public static List<FileBackupInfo> GetAvailableBackups()
    {
        var backups = new List<FileBackupInfo>();
        
        foreach (string file in Directory.GetFiles(BACKUP_DIRECTORY, "*.backup"))
        {
            try
            {
                var info = new FileInfo(file);
                var parts = Path.GetFileNameWithoutExtension(file).Split('_');
                
                if (parts.Length >= 3)
                {
                    backups.Add(new FileBackupInfo
                    {
                        FilePath = file,
                        ObjectType = parts[0],
                        ObjectId = parts[1],
                        Timestamp = info.CreationTime,
                        Size = info.Length
                    });
                }
            }
            catch (Exception ex)
            {
                log.Warn($"Error reading backup file {file}: {ex}");
            }
        }
        
        return backups.OrderByDescending(b => b.Timestamp).ToList();
    }
}

public class FileBackupInfo
{
    public string FilePath { get; set; }
    public string ObjectType { get; set; }
    public string ObjectId { get; set; }
    public DateTime Timestamp { get; set; }
    public long Size { get; set; }
}
```

## Recovery Procedures

### Character Recovery Commands

```csharp
[Cmd("&recover", ePrivLevel.Admin, "Recover character from backup")]
public class RecoverCharacterCommand : AbstractCommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length < 3)
        {
            DisplaySyntax(client);
            return;
        }
        
        string characterName = args[1];
        string backupDate = args[2];
        
        try
        {
            var backups = GameServer.Database.SelectObjects<DbCoreCharacterBackup>(
                "Name = @name AND DeleteDate LIKE @date",
                new QueryParameter("@name", characterName),
                new QueryParameter("@date", $"{backupDate}%")
            );
            
            if (backups.Count == 0)
            {
                client.Out.SendMessage($"No backups found for character '{characterName}' on date '{backupDate}'", 
                                      eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            
            if (backups.Count > 1)
            {
                client.Out.SendMessage($"Multiple backups found for '{characterName}' on '{backupDate}':", 
                                      eChatType.CT_System, eChatLoc.CL_SystemWindow);
                
                for (int i = 0; i < backups.Count; i++)
                {
                    client.Out.SendMessage($"  {i}: {backups[i].DeleteDate} (Slot: {backups[i].AccountSlot})", 
                                          eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                
                client.Out.SendMessage("Use '&recover [name] [date] [index]' to select specific backup", 
                                      eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            
            // Recover single backup
            RecoverCharacterFromBackup(backups[0], client);
        }
        catch (Exception ex)
        {
            log.Error($"Error recovering character {characterName}: {ex}");
            client.Out.SendMessage($"Error recovering character: {ex.Message}", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
    
    private void RecoverCharacterFromBackup(DbCoreCharacterBackup backup, GameClient client)
    {
        // Convert backup to regular character
        var character = new DbCoreCharacter();
        CopyBackupToCharacter(backup, character);
        
        // Check if slot is available
        var existingInSlot = GameServer.Database.SelectObjects<DbCoreCharacter>(
            "AccountName = @account AND AccountSlot = @slot",
            new QueryParameter("@account", character.AccountName),
            new QueryParameter("@slot", character.AccountSlot)
        );
        
        if (existingInSlot.Count > 0)
        {
            client.Out.SendMessage($"Slot {character.AccountSlot} is occupied by '{existingInSlot[0].Name}'", 
                                  eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return;
        }
        
        // Add recovered character
        GameServer.Database.AddObject(character);
        
        client.Out.SendMessage($"Successfully recovered character '{character.Name}' to slot {character.AccountSlot}", 
                              eChatType.CT_System, eChatLoc.CL_SystemWindow);
        
        log.Info($"Admin {client.Account.Name} recovered character '{character.Name}' from backup {backup.DOLCharacters_ID}");
    }
}
```

## Configuration

### Backup Settings

```csharp
[ServerProperty("backup", "auto_backup_enabled", "Enable automatic backups", true)]
public static bool AUTO_BACKUP_ENABLED;

[ServerProperty("backup", "backup_retention_days", "Days to keep backups", 30)]
public static int BACKUP_RETENTION_DAYS;

[ServerProperty("backup", "emergency_backup_enabled", "Enable emergency file backups", true)]
public static bool EMERGENCY_BACKUP_ENABLED;

[ServerProperty("backup", "save_interval_minutes", "Database save interval", 10)]
public static int SAVE_INTERVAL_MINUTES;
```

### Cleanup Procedures

```csharp
public class BackupCleanup
{
    public static void CleanOldBackups()
    {
        DateTime cutoffDate = DateTime.Now.AddDays(-ServerProperties.Properties.BACKUP_RETENTION_DAYS);
        
        // Clean character backups
        var oldBackups = GameServer.Database.SelectObjects<DbCoreCharacterBackup>(
            "DeleteDate < @cutoff",
            new QueryParameter("@cutoff", cutoffDate)
        );
        
        foreach (var backup in oldBackups)
        {
            GameServer.Database.DeleteObject(backup);
        }
        
        log.Info($"Cleaned {oldBackups.Count} old character backups");
        
        // Clean file backups
        CleanOldFileBackups(cutoffDate);
    }
    
    private static void CleanOldFileBackups(DateTime cutoffDate)
    {
        string backupDir = Path.Combine(GameServerConfiguration.RootDirectory, "backups", "emergency");
        
        if (!Directory.Exists(backupDir))
            return;
            
        int deletedFiles = 0;
        
        foreach (string file in Directory.GetFiles(backupDir, "*.backup"))
        {
            try
            {
                var info = new FileInfo(file);
                if (info.CreationTime < cutoffDate)
                {
                    File.Delete(file);
                    deletedFiles++;
                }
            }
            catch (Exception ex)
            {
                log.Warn($"Error deleting old backup file {file}: {ex}");
            }
        }
        
        log.Info($"Cleaned {deletedFiles} old file backups");
    }
}
```

## Implementation Status

**Completed**:
- ✅ Character backup system
- ✅ Custom parameter backup
- ✅ Automated rearrangement backup
- ✅ Database schema evolution
- ✅ Error recovery procedures
- ✅ File-based emergency backup
- ✅ Periodic save system

**In Progress**:
- 🔄 Automated backup verification
- 🔄 Cross-server backup replication
- 🔄 Incremental backup system

**Planned**:
- ⏳ Point-in-time recovery
- ⏳ Automated disaster recovery
- ⏳ Backup compression and encryption

## References

- **Character Backup**: `CoreDatabase/Tables/DbCoreCharacterBackup.cs`
- **Schema Evolution**: `CoreDatabase/Handlers/SqliteObjectDatabase.cs`
- **Save System**: `GameServer/GameServer.cs` (SaveTimerProc)
- **Recovery Commands**: `GameServer/commands/playercommands/rearrange.cs` 