# Database Migration System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

**Game Rule Summary**: The database migration system safely updates the game's data storage without losing your character information, items, or progress. When the server adds new features or fixes bugs that require database changes, this system ensures everything transfers correctly while creating backups in case of problems.

The Database Migration System provides sophisticated schema evolution capabilities for OpenDAoC, enabling seamless database updates, table migrations, data preservation, and backward compatibility. It supports complex schema transformations, data type conversions, and automated backup/recovery during migration operations.

## Core Architecture

### Migration Framework

```csharp
public abstract class ObjectDatabase : IObjectDatabase
{
    protected readonly Dictionary<string, DataTableHandler> TableDatasets = new();
    
    public void CheckOrCreateTableImpl(DataTableHandler table)
    {
        if (!DoesTableExist(table.TableName))
        {
            CreateTable(table);
            log.Info($"Created new table: {table.TableName}");
        }
        else
        {
            // Check if migration is needed
            if (RequiresMigration(table))
            {
                PerformTableMigration(table);
            }
        }
    }
    
    protected abstract bool RequiresMigration(DataTableHandler table);
    protected abstract void PerformTableMigration(DataTableHandler table);
}
```

### Schema Comparison Engine

```csharp
public class SchemaComparator
{
    public SchemaChanges CompareSchemas(DataTableHandler newTable, TableSchema currentSchema)
    {
        var changes = new SchemaChanges();
        
        // Compare columns
        CompareColumns(newTable, currentSchema, changes);
        
        // Compare constraints
        CompareConstraints(newTable, currentSchema, changes);
        
        // Compare indexes
        CompareIndexes(newTable, currentSchema, changes);
        
        return changes;
    }
    
    private void CompareColumns(DataTableHandler newTable, TableSchema currentSchema, SchemaChanges changes)
    {
        var newColumns = newTable.FieldElementBindings.ToDictionary(b => b.ColumnName, b => b);
        var currentColumns = currentSchema.Columns.ToDictionary(c => c.Name, c => c);
        
        // Find added columns
        foreach (var newColumn in newColumns)
        {
            if (!currentColumns.ContainsKey(newColumn.Key))
            {
                changes.AddedColumns.Add(newColumn.Value);
            }
        }
        
        // Find removed columns
        foreach (var currentColumn in currentColumns)
        {
            if (!newColumns.ContainsKey(currentColumn.Key))
            {
                changes.RemovedColumns.Add(currentColumn.Value);
            }
        }
        
        // Find modified columns
        foreach (var newColumn in newColumns)
        {
            if (currentColumns.TryGetValue(newColumn.Key, out var currentColumn))
            {
                if (HasColumnChanged(newColumn.Value, currentColumn))
                {
                    changes.ModifiedColumns.Add(new ColumnChange
                    {
                        OldColumn = currentColumn,
                        NewColumn = newColumn.Value
                    });
                }
            }
        }
    }
}

public class SchemaChanges
{
    public List<ElementBinding> AddedColumns { get; } = new();
    public List<ColumnInfo> RemovedColumns { get; } = new();
    public List<ColumnChange> ModifiedColumns { get; } = new();
    public List<IndexChange> IndexChanges { get; } = new();
    public List<ConstraintChange> ConstraintChanges { get; } = new();
    
    public bool HasChanges => AddedColumns.Any() || RemovedColumns.Any() || 
                             ModifiedColumns.Any() || IndexChanges.Any() || ConstraintChanges.Any();
}
```

## SQLite Migration Implementation

### Advanced Table Migration

```csharp
public class SqliteObjectDatabase : SqlObjectDatabase
{
    protected override void PerformTableMigration(DataTableHandler table)
    {
        using var conn = CreateConnection();
        conn.Open();
        
        using var transaction = conn.BeginTransaction();
        try
        {
            // Step 1: Create backup table
            string backupTableName = $"{table.TableName}_migration_backup_{DateTime.Now:yyyyMMddHHmmss}";
            string renameQuery = $"ALTER TABLE `{table.TableName}` RENAME TO `{backupTableName}`";
            
            using (var command = new SQLiteCommand(renameQuery, conn))
            {
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
            
            // Step 2: Create new table with updated schema
            CreateTable(table);
            
            // Step 3: Migrate data with type conversion and null handling
            var columnMapping = GenerateColumnMapping(table, backupTableName, conn, transaction);
            MigrateDataWithConversion(table.TableName, backupTableName, columnMapping, conn, transaction);
            
            // Step 4: Verify migration success
            if (VerifyMigration(table, backupTableName, conn, transaction))
            {
                // Step 5: Drop backup table
                using (var command = new SQLiteCommand($"DROP TABLE `{backupTableName}`", conn))
                {
                    command.Transaction = transaction;
                    command.ExecuteNonQuery();
                }
                
                transaction.Commit();
                log.Info($"Successfully migrated table: {table.TableName}");
            }
            else
            {
                throw new DatabaseMigrationException($"Migration verification failed for table: {table.TableName}");
            }
        }
        catch (Exception e)
        {
            transaction.Rollback();
            log.Error($"Table migration failed for {table.TableName}, rolled back", e);
            
            // Attempt to restore original table
            RestoreFromBackup(table.TableName, conn);
            throw;
        }
    }
    
    private void MigrateDataWithConversion(string newTableName, string backupTableName, 
                                         List<ColumnMapping> mappings, DbConnection conn, DbTransaction transaction)
    {
        if (!mappings.Any())
        {
            log.Warn($"No column mappings found for migration from {backupTableName} to {newTableName}");
            return;
        }
        
        // Build dynamic SELECT and INSERT statements with type conversion
        var selectParts = new List<string>();
        var insertColumns = new List<string>();
        
        foreach (var mapping in mappings)
        {
            insertColumns.Add($"`{mapping.TargetColumn}`");
            
            if (mapping.RequiresConversion)
            {
                selectParts.Add(BuildConversionExpression(mapping));
            }
            else
            {
                selectParts.Add($"`{mapping.SourceColumn}`");
            }
        }
        
        string selectClause = string.Join(", ", selectParts);
        string insertClause = string.Join(", ", insertColumns);
        
        string migrationQuery = $@"
            INSERT INTO `{newTableName}` ({insertClause}) 
            SELECT {selectClause} 
            FROM `{backupTableName}`";
        
        using var command = new SQLiteCommand(migrationQuery, conn);
        command.Transaction = transaction;
        
        int rowsAffected = command.ExecuteNonQuery();
        log.Info($"Migrated {rowsAffected} rows from {backupTableName} to {newTableName}");
    }
    
    private string BuildConversionExpression(ColumnMapping mapping)
    {
        // Handle various type conversions
        return mapping.ConversionType switch
        {
            ConversionType.StringToInteger => $"CAST(COALESCE(`{mapping.SourceColumn}`, '0') AS INTEGER)",
            ConversionType.IntegerToString => $"CAST(`{mapping.SourceColumn}` AS TEXT)",
            ConversionType.DateTimeToString => $"datetime(`{mapping.SourceColumn}`)",
            ConversionType.StringToDateTime => $"COALESCE(`{mapping.SourceColumn}`, '2000-01-01 00:00:00')",
            ConversionType.NullToDefault => BuildNullToDefaultConversion(mapping),
            _ => $"`{mapping.SourceColumn}`"
        };
    }
    
    private string BuildNullToDefaultConversion(ColumnMapping mapping)
    {
        if (mapping.TargetType == typeof(string))
            return $"COALESCE(`{mapping.SourceColumn}`, '')";
        if (mapping.TargetType == typeof(DateTime))
            return $"COALESCE(`{mapping.SourceColumn}`, '2000-01-01 00:00:00')";
        if (mapping.TargetType.IsNumeric())
            return $"COALESCE(`{mapping.SourceColumn}`, 0)";
            
        return $"`{mapping.SourceColumn}`";
    }
}
```

### Column Mapping Generation

```csharp
private List<ColumnMapping> GenerateColumnMapping(DataTableHandler table, string backupTableName, 
                                                DbConnection conn, DbTransaction transaction)
{
    var mappings = new List<ColumnMapping>();
    
    // Get current table schema from backup
    var backupSchema = GetTableSchema(backupTableName, conn, transaction);
    var backupColumns = backupSchema.ToDictionary(c => c.Name.ToLower(), c => c);
    
    // Generate mappings for each new column
    foreach (var binding in table.FieldElementBindings)
    {
        var targetColumn = binding.ColumnName;
        var targetType = binding.ValueType;
        
        if (backupColumns.TryGetValue(targetColumn.ToLower(), out var sourceColumn))
        {
            // Column exists in backup - check if conversion needed
            var mapping = new ColumnMapping
            {
                SourceColumn = sourceColumn.Name,
                TargetColumn = targetColumn,
                SourceType = GetClrType(sourceColumn.DataType),
                TargetType = targetType,
                RequiresConversion = RequiresTypeConversion(sourceColumn.DataType, targetType),
                ConversionType = DetermineConversionType(sourceColumn.DataType, targetType)
            };
            
            mappings.Add(mapping);
        }
        else
        {
            // New column - will use default value
            log.Info($"New column detected: {targetColumn} - will use default value");
        }
    }
    
    return mappings;
}

public class ColumnMapping
{
    public string SourceColumn { get; set; }
    public string TargetColumn { get; set; }
    public Type SourceType { get; set; }
    public Type TargetType { get; set; }
    public bool RequiresConversion { get; set; }
    public ConversionType ConversionType { get; set; }
}

public enum ConversionType
{
    None,
    StringToInteger,
    IntegerToString,
    DateTimeToString,
    StringToDateTime,
    NullToDefault,
    Truncation,
    Expansion
}
```

## MySQL Migration Implementation

### Advanced ALTER TABLE Operations

```csharp
public class MySqlObjectDatabase : SqlObjectDatabase
{
    protected override void PerformTableMigration(DataTableHandler table)
    {
        var currentSchema = GetCurrentTableSchema(table.TableName);
        var requiredSchema = GenerateRequiredSchema(table);
        var changes = new SchemaComparator().CompareSchemas(table, currentSchema);
        
        if (!changes.HasChanges)
        {
            log.Debug($"No schema changes required for table: {table.TableName}");
            return;
        }
        
        log.Info($"Performing schema migration for table: {table.TableName}");
        
        using var conn = CreateConnection();
        conn.Open();
        
        using var transaction = conn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            // Generate and execute ALTER TABLE statements
            var alterStatements = GenerateAlterStatements(table, changes);
            
            foreach (string statement in alterStatements)
            {
                using var command = conn.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = statement;
                command.ExecuteNonQuery();
                
                log.Debug($"Executed: {statement}");
            }
            
            // Handle null to non-null conversions
            HandleNullToNonNullConversions(table, changes, conn, transaction);
            
            transaction.Commit();
            log.Info($"Successfully migrated MySQL table: {table.TableName}");
        }
        catch (Exception e)
        {
            transaction.Rollback();
            log.Error($"MySQL table migration failed for {table.TableName}", e);
            throw;
        }
    }
    
    private List<string> GenerateAlterStatements(DataTableHandler table, SchemaChanges changes)
    {
        var statements = new List<string>();
        var alterClauses = new List<string>();
        
        // Add new columns
        foreach (var column in changes.AddedColumns)
        {
            string columnDef = GenerateColumnDefinition(column);
            alterClauses.Add($"ADD COLUMN {columnDef}");
        }
        
        // Modify existing columns
        foreach (var change in changes.ModifiedColumns)
        {
            string columnDef = GenerateColumnDefinition(change.NewColumn);
            alterClauses.Add($"MODIFY COLUMN {columnDef}");
        }
        
        // Add new indexes
        foreach (var index in changes.IndexChanges.Where(i => i.ChangeType == IndexChangeType.Add))
        {
            if (index.IsUnique)
            {
                alterClauses.Add($"ADD UNIQUE KEY `{index.IndexName}` ({string.Join(", ", index.Columns.Select(c => $"`{c}`"))})");
            }
            else
            {
                alterClauses.Add($"ADD KEY `{index.IndexName}` ({string.Join(", ", index.Columns.Select(c => $"`{c}`"))})");
            }
        }
        
        // Generate single ALTER TABLE statement if possible
        if (alterClauses.Any())
        {
            string alterStatement = $"ALTER TABLE `{table.TableName}` {string.Join(", ", alterClauses)}";
            statements.Add(alterStatement);
        }
        
        return statements;
    }
    
    private void HandleNullToNonNullConversions(DataTableHandler table, SchemaChanges changes, 
                                              DbConnection conn, DbTransaction transaction)
    {
        var nullToNonNullColumns = changes.ModifiedColumns
            .Where(c => c.OldColumn.AllowDbNull && !c.NewColumn.DataElement.AllowDbNull)
            .ToList();
        
        foreach (var column in nullToNonNullColumns)
        {
            string defaultValue = GetDefaultValueForType(column.NewColumn.ValueType);
            string updateQuery = $"UPDATE `{table.TableName}` SET `{column.NewColumn.ColumnName}` = {defaultValue} WHERE `{column.NewColumn.ColumnName}` IS NULL";
            
            using var command = conn.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = updateQuery;
            
            int updatedRows = command.ExecuteNonQuery();
            if (updatedRows > 0)
            {
                log.Info($"Updated {updatedRows} NULL values to default for column: {column.NewColumn.ColumnName}");
            }
        }
    }
}
```

## Data Type Migration

### Complex Type Conversions

```csharp
public static class TypeConverter
{
    public static object ConvertValue(object value, Type sourceType, Type targetType)
    {
        if (value == null || value == DBNull.Value)
        {
            return GetDefaultValue(targetType);
        }
        
        // Direct assignment if types match
        if (sourceType == targetType)
            return value;
        
        // Handle specific conversions
        return (sourceType, targetType) switch
        {
            (Type s, Type t) when s == typeof(string) && t == typeof(int) => ConvertStringToInt(value.ToString()),
            (Type s, Type t) when s == typeof(int) && t == typeof(string) => value.ToString(),
            (Type s, Type t) when s == typeof(DateTime) && t == typeof(string) => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"),
            (Type s, Type t) when s == typeof(string) && t == typeof(DateTime) => ParseDateTime(value.ToString()),
            (Type s, Type t) when s == typeof(byte) && t == typeof(int) => Convert.ToInt32(value),
            (Type s, Type t) when s == typeof(int) && t == typeof(byte) => TruncateToByteRange(Convert.ToInt32(value)),
            _ => Convert.ChangeType(value, targetType)
        };
    }
    
    private static int ConvertStringToInt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;
            
        return int.TryParse(value, out int result) ? result : 0;
    }
    
    private static DateTime ParseDateTime(string value)
    {
        if (string.IsNullOrEmpty(value))
            return new DateTime(2000, 1, 1);
            
        return DateTime.TryParse(value, out DateTime result) ? result : new DateTime(2000, 1, 1);
    }
    
    private static byte TruncateToByteRange(int value)
    {
        return (byte)Math.Max(0, Math.Min(255, value));
    }
    
    private static object GetDefaultValue(Type type)
    {
        if (type == typeof(string))
            return string.Empty;
        if (type == typeof(DateTime))
            return new DateTime(2000, 1, 1);
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        
        return null;
    }
}
```

### Schema Version Management

```csharp
[DataTable(TableName = "SchemaVersions")]
public class DbSchemaVersion : DataObject
{
    [PrimaryKey]
    public string TableName { get; set; }
    
    [DataElement]
    public int Version { get; set; }
    
    [DataElement]
    public DateTime LastUpdated { get; set; }
    
    [DataElement]
    public string UpdatedBy { get; set; }
    
    [DataElement]
    public string MigrationScript { get; set; }
}

public static class SchemaVersionManager
{
    private static readonly Dictionary<string, int> _currentVersions = new();
    
    public static void RegisterTableVersion(string tableName, int version)
    {
        _currentVersions[tableName] = version;
    }
    
    public static bool RequiresMigration(string tableName)
    {
        int currentVersion = _currentVersions.GetValueOrDefault(tableName, 1);
        int databaseVersion = GetDatabaseVersion(tableName);
        
        return currentVersion > databaseVersion;
    }
    
    private static int GetDatabaseVersion(string tableName)
    {
        var versionRecord = GameServer.Database.SelectObjects<DbSchemaVersion>(
            "`TableName` = @tableName", new QueryParameter("@tableName", tableName))
            .FirstOrDefault();
        
        return versionRecord?.Version ?? 0;
    }
    
    public static void UpdateDatabaseVersion(string tableName, int version, string migrationScript = "")
    {
        var versionRecord = GetDatabaseVersion(tableName) > 0 ? 
            GameServer.Database.SelectObjects<DbSchemaVersion>("`TableName` = @tableName", 
                new QueryParameter("@tableName", tableName)).First() :
            new DbSchemaVersion { TableName = tableName };
        
        versionRecord.Version = version;
        versionRecord.LastUpdated = DateTime.UtcNow;
        versionRecord.UpdatedBy = Environment.UserName;
        versionRecord.MigrationScript = migrationScript;
        
        GameServer.Database.SaveObject(versionRecord);
    }
}
```

## Migration Validation and Testing

### Migration Verification

```csharp
public class MigrationValidator
{
    public bool VerifyMigration(DataTableHandler table, string backupTableName, 
                               DbConnection conn, DbTransaction transaction)
    {
        try
        {
            // Verify row count preservation
            if (!VerifyRowCount(table.TableName, backupTableName, conn, transaction))
            {
                log.Error("Row count verification failed");
                return false;
            }
            
            // Verify data integrity for key columns
            if (!VerifyDataIntegrity(table, backupTableName, conn, transaction))
            {
                log.Error("Data integrity verification failed");
                return false;
            }
            
            // Verify schema structure
            if (!VerifySchemaStructure(table, conn, transaction))
            {
                log.Error("Schema structure verification failed");
                return false;
            }
            
            log.Info($"Migration verification passed for table: {table.TableName}");
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"Migration verification failed with exception: {ex.Message}");
            return false;
        }
    }
    
    private bool VerifyRowCount(string newTable, string backupTable, 
                               DbConnection conn, DbTransaction transaction)
    {
        string countQuery = $"SELECT COUNT(*) FROM `{newTable}`";
        string backupCountQuery = $"SELECT COUNT(*) FROM `{backupTable}`";
        
        using var newCommand = new SQLiteCommand(countQuery, conn);
        using var backupCommand = new SQLiteCommand(backupCountQuery, conn);
        
        newCommand.Transaction = transaction;
        backupCommand.Transaction = transaction;
        
        long newCount = (long)newCommand.ExecuteScalar();
        long backupCount = (long)backupCommand.ExecuteScalar();
        
        if (newCount != backupCount)
        {
            log.Error($"Row count mismatch: {newTable}={newCount}, {backupTable}={backupCount}");
            return false;
        }
        
        return true;
    }
    
    private bool VerifyDataIntegrity(DataTableHandler table, string backupTableName, 
                                   DbConnection conn, DbTransaction transaction)
    {
        // Verify primary key integrity
        var primaryKey = table.PrimaryKey;
        if (primaryKey != null)
        {
            string integrityQuery = $@"
                SELECT COUNT(*) FROM `{table.TableName}` t1
                INNER JOIN `{backupTableName}` t2 ON t1.`{primaryKey.ColumnName}` = t2.`{primaryKey.ColumnName}`";
            
            using var command = new SQLiteCommand(integrityQuery, conn);
            command.Transaction = transaction;
            
            long matchingRows = (long)command.ExecuteScalar();
            
            // Get total count for comparison
            using var countCommand = new SQLiteCommand($"SELECT COUNT(*) FROM `{table.TableName}`", conn);
            countCommand.Transaction = transaction;
            long totalRows = (long)countCommand.ExecuteScalar();
            
            if (matchingRows != totalRows)
            {
                log.Error($"Primary key integrity check failed: {matchingRows}/{totalRows} rows match");
                return false;
            }
        }
        
        return true;
    }
}
```

### Rollback Capabilities

```csharp
public class MigrationRollbackManager
{
    public bool RollbackMigration(string tableName, string backupTableName)
    {
        try
        {
            using var conn = CreateConnection();
            conn.Open();
            
            using var transaction = conn.BeginTransaction();
            
            // Drop current table
            using (var dropCommand = new SQLiteCommand($"DROP TABLE IF EXISTS `{tableName}`", conn))
            {
                dropCommand.Transaction = transaction;
                dropCommand.ExecuteNonQuery();
            }
            
            // Restore from backup
            using (var restoreCommand = new SQLiteCommand($"ALTER TABLE `{backupTableName}` RENAME TO `{tableName}`", conn))
            {
                restoreCommand.Transaction = transaction;
                restoreCommand.ExecuteNonQuery();
            }
            
            transaction.Commit();
            log.Info($"Successfully rolled back migration for table: {tableName}");
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"Failed to rollback migration for table: {tableName}", ex);
            return false;
        }
    }
    
    public void CreateRecoveryPoint(string tableName)
    {
        string recoveryTableName = $"{tableName}_recovery_{DateTime.Now:yyyyMMddHHmmss}";
        
        using var conn = CreateConnection();
        conn.Open();
        
        string createRecoveryQuery = $"CREATE TABLE `{recoveryTableName}` AS SELECT * FROM `{tableName}`";
        
        using var command = new SQLiteCommand(createRecoveryQuery, conn);
        command.ExecuteNonQuery();
        
        log.Info($"Created recovery point: {recoveryTableName}");
    }
}
```

## Configuration and Monitoring

### Migration Settings

```ini
# Database migration settings
ENABLE_AUTO_MIGRATION = true
MIGRATION_BACKUP_ENABLED = true
MIGRATION_VERIFICATION_ENABLED = true
MIGRATION_TIMEOUT_SECONDS = 300

# Safety settings
MAX_MIGRATION_TABLE_SIZE_MB = 1024
REQUIRE_EXPLICIT_APPROVAL = false
MIGRATION_LOG_LEVEL = INFO

# Recovery settings
KEEP_BACKUP_TABLES = true
BACKUP_RETENTION_DAYS = 30
AUTO_CLEANUP_BACKUPS = true
```

### Migration Monitoring

```csharp
public static class MigrationMonitor
{
    private static readonly List<MigrationEvent> _migrationHistory = new();
    
    public static void RecordMigrationEvent(string tableName, MigrationEventType eventType, 
                                           TimeSpan duration, bool success, string details = "")
    {
        var migrationEvent = new MigrationEvent
        {
            TableName = tableName,
            EventType = eventType,
            Timestamp = DateTime.UtcNow,
            Duration = duration,
            Success = success,
            Details = details
        };
        
        _migrationHistory.Add(migrationEvent);
        
        // Alert on failures
        if (!success)
        {
            log.Error($"Migration failed for {tableName}: {details}");
            NotifyAdministrators($"Database migration failed for table {tableName}");
        }
    }
    
    public static void GenerateMigrationReport()
    {
        log.Info("=== Database Migration Report ===");
        
        var recentMigrations = _migrationHistory
            .Where(m => m.Timestamp > DateTime.UtcNow.AddDays(-7))
            .GroupBy(m => m.TableName);
        
        foreach (var tableGroup in recentMigrations)
        {
            var events = tableGroup.OrderBy(e => e.Timestamp).ToList();
            var successCount = events.Count(e => e.Success);
            var totalCount = events.Count;
            
            log.Info($"{tableGroup.Key}: {successCount}/{totalCount} successful migrations");
            
            if (events.Any(e => !e.Success))
            {
                var failures = events.Where(e => !e.Success);
                foreach (var failure in failures)
                {
                    log.Warn($"  Failed {failure.EventType} at {failure.Timestamp}: {failure.Details}");
                }
            }
        }
    }
}

public class MigrationEvent
{
    public string TableName { get; set; }
    public MigrationEventType EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string Details { get; set; }
}

public enum MigrationEventType
{
    SchemaCheck,
    BackupCreation,
    TableMigration,
    DataMigration,
    Verification,
    Rollback,
    Cleanup
}
```

This comprehensive Database Migration System ensures safe and reliable schema evolution while preserving data integrity and providing robust recovery capabilities for OpenDAoC's database infrastructure. 