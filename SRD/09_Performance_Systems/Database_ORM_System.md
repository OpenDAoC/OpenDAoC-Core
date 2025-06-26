# Database ORM System

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from ObjectDatabase.cs, SqlObjectDatabase.cs, DOLDB.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview
The Database ORM System provides a sophisticated Object-Relational Mapping layer for OpenDAoC data persistence. It supports multiple database backends (MySQL, SQLite), automated table management, transaction handling, and asynchronous operations with comprehensive caching and performance optimizations.

## Core Architecture

### Database Abstraction Layer
```csharp
// Primary interface for all database operations
public interface IObjectDatabase
{
    bool RegisterDataObject(Type dataObjectType);
    void FillObjectRelations(DataObject dataObject);
    
    // CRUD Operations
    bool AddObject(DataObject dataObject);
    bool SaveObject(DataObject dataObject);
    bool DeleteObject(DataObject dataObject);
    
    // Query Operations
    TObject FindObjectByKey<TObject>(object key) where TObject : DataObject;
    IList<TObject> SelectAllObjects<TObject>() where TObject : DataObject;
    TObject SelectObject<TObject>(WhereClause whereClause) where TObject : DataObject;
    IList<TObject> SelectObjects<TObject>(WhereClause whereClause) where TObject : DataObject;
}
```

### Database Providers

#### Connection Types
```csharp
public enum EConnectionType
{
    DATABASE_XML,       // File-based XML storage
    DATABASE_MYSQL,     // MySQL database
    DATABASE_SQLITE,    // SQLite database
    DATABASE_MSSQL,     // Microsoft SQL Server
    DATABASE_ODBC,      // ODBC connection
    DATABASE_OLEDB      // OLE DB connection
}
```

#### Factory Pattern
```csharp
public static IObjectDatabase GetObjectDatabase(EConnectionType connectionType, string connectionString)
{
    return connectionType switch
    {
        EConnectionType.DATABASE_MYSQL => new MySqlObjectDatabase(connectionString),
        EConnectionType.DATABASE_SQLITE => new SqliteObjectDatabase(connectionString),
        _ => null
    };
}
```

## Data Object Model

### Base DataObject Class
```csharp
public abstract class DataObject
{
    // Object state tracking
    public bool IsPersisted { get; set; }     // Exists in database
    public bool Dirty { get; set; }          // Has unsaved changes
    public string TableName { get; }         // Database table name
    
    // Primary key management
    public abstract object GetPrimaryKey();
    
    // Change tracking
    protected void MarkDirty() { Dirty = true; }
}
```

### Table Definition Attributes
```csharp
// Define table mapping
[DataTable(TableName = "Character")]
public class DbCharacter : DataObject
{
    [PrimaryKey]
    public string Character_ID { get; set; }
    
    [DataElement(AllowDbNull = false)]
    public string Name { get; set; }
    
    [DataElement(AllowDbNull = true)]
    public int Level { get; set; }
    
    [Relation(LocalField = "Account_ID", RemoteField = "ObjectId")]
    public DbAccount Account { get; set; }
}
```

### Attribute Types
```csharp
// Table definition
[DataTable(TableName = "TableName")]          // Maps class to table
[DataView(ViewName = "ViewName")]              // Maps to database view

// Column mapping  
[PrimaryKey]                                   // Primary key column
[DataElement(AllowDbNull = true)]              // Regular column
[DataElement(AllowDbNull = false, Unique = true)] // Constraints

// Relationships
[Relation(LocalField = "FK", RemoteField = "PK")] // Foreign key relationship
```

## Table Management System

### Automatic Table Creation
```csharp
public abstract class SqlObjectDatabase : ObjectDatabase
{
    public override void RegisterDataObject(Type dataObjectType)
    {
        var tableName = AttributeUtil.GetTableOrViewName(dataObjectType);
        var tableHandler = new DataTableHandler(dataObjectType);
        
        // Check if table exists, create if not
        CheckOrCreateTableImpl(tableHandler);
        
        // Register for future operations
        TableDatasets[tableName] = tableHandler;
    }
}
```

### Table Handler System
```csharp
public class DataTableHandler
{
    public Type ObjectType { get; }                     // .NET type
    public string TableName { get; }                    // Database table name
    public bool UsesPreCaching { get; }                 // Cache all objects
    public ElementBinding[] FieldElementBindings { get; } // Column mappings
    
    // Pre-cached object management
    public DataObject GetPreCachedObject(object key);
    public void SetPreCachedObject(object key, DataObject obj);
}
```

### Column Binding System
```csharp
public class ElementBinding
{
    public string ColumnName { get; }           // Database column name
    public Type ValueType { get; }              // .NET property type
    public PropertyInfo PropertyInfo { get; }   // Reflection info
    
    // Value conversion
    public void SetValue(DataObject obj, object value);
    public object GetValue(DataObject obj);
}
```

## Query System

### DOLDB Helper Class
```csharp
// Simplified database operations
public class DOLDB<T> where T : DataObject
{
    // Synchronous operations
    public static T FindObjectByKey(object key);
    public static IList<T> SelectAllObjects();
    public static T SelectObject(WhereClause whereClause);
    public static IList<T> SelectObjects(WhereClause whereClause);
    
    // Asynchronous operations
    public static async Task<T> FindObjectsByKeyAsync(object key);
    public static async Task<IList<T>> SelectAllObjectsAsync();
    public static async Task<T> SelectObjectAsync(WhereClause whereClause);
    public static async Task<IList<T>> SelectObjectsAsync(WhereClause whereClause);
}
```

### Where Clause Builder
```csharp
// Type-safe query building
var whereClause = DB.Column("Level").IsGreaterThan(40)
    .And(DB.Column("Realm").IsEqualTo((int)eRealm.Albion))
    .And(DB.Column("Name").IsLike("Player%"));

var characters = DOLDB<DbCharacter>.SelectObjects(whereClause);
```

### Query Parameter System
```csharp
public class QueryParameter
{
    public string Name { get; set; }     // Parameter name
    public object Value { get; set; }    // Parameter value
    public Type ValueType { get; set; }  // Value type
}

// Prepared statements with parameters
var parameters = new[] {
    new QueryParameter { Name = "@level", Value = 50, ValueType = typeof(int) },
    new QueryParameter { Name = "@realm", Value = 1, ValueType = typeof(int) }
};
```

## Transaction Management

### Isolation Levels
```csharp
public enum EIsolationLevel
{
    DEFAULT,
    SERIALIZABLE,       // Highest isolation
    REPEATABLE_READ,    // Prevents phantom reads
    READ_COMMITTED,     // Prevents dirty reads
    READ_UNCOMMITTED,   // Lowest isolation
    SNAPSHOT           // MVCC-based isolation
}
```

### Transaction Usage
```csharp
// Transaction with isolation level
using (var transaction = Database.BeginTransaction(EIsolationLevel.READ_COMMITTED))
{
    try
    {
        // Multiple operations in transaction
        Database.AddObject(newCharacter);
        Database.SaveObject(existingCharacter);
        Database.DeleteObject(oldCharacter);
        
        transaction.Commit();
    }
    catch (Exception)
    {
        transaction.Rollback();
        throw;
    }
}
```

## Relationship Management

### Object Relations
```csharp
// Define relationship
[Relation(LocalField = "Account_ID", RemoteField = "ObjectId")]
public DbAccount Account { get; set; }

// Auto-populate relationships
Database.FillObjectRelations(character);
// Now character.Account is populated
```

### Lazy Loading
```csharp
// Relationships loaded on demand
public class DbCharacter : DataObject
{
    private DbAccount _account;
    
    public DbAccount Account
    {
        get
        {
            if (_account == null && Account_ID != null)
                _account = DOLDB<DbAccount>.FindObjectByKey(Account_ID);
            return _account;
        }
        set { _account = value; }
    }
}
```

## Caching System

### Pre-Caching Strategy
```csharp
// Enable pre-caching for frequently accessed tables
[DataTable(TableName = "ItemTemplate", PreCache = true)]
public class DbItemTemplate : DataObject
{
    // All item templates loaded at startup
    // Subsequent queries use in-memory cache
}
```

### Cache Management
```csharp
public class DataTableHandler
{
    private readonly ConcurrentDictionary<object, DataObject> _preCache;
    
    public DataObject GetPreCachedObject(object key)
    {
        return _preCache.TryGetValue(key, out var obj) ? obj : null;
    }
    
    public void SetPreCachedObject(object key, DataObject obj)
    {
        _preCache[key] = obj;
    }
}
```

## Performance Optimizations

### Connection Pooling
```csharp
// MySQL connection string with pooling
string connectionString = "Server=localhost;Database=dol;Pooling=true;Min Pool Size=5;Max Pool Size=50;Connection Timeout=30;";
```

### Batch Operations
```csharp
// Bulk insert/update operations
protected abstract IEnumerable<bool> AddObjectImpl(DataTableHandler tableHandler, IEnumerable<DataObject> dataObjects);
protected abstract IEnumerable<bool> SaveObjectImpl(DataTableHandler tableHandler, IEnumerable<DataObject> dataObjects);

// Usage
var characters = new List<DbCharacter>();
// ... populate list
Database.AddObjects(characters); // Batch insert
```

### Prepared Statements
```csharp
// Automatic prepared statement generation
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT * FROM Character WHERE Level = @level";
    cmd.Parameters.Add(new SqlParameter("@level", level));
    cmd.Prepare(); // Compiled once, executed many times
    
    using (var reader = cmd.ExecuteReader())
    {
        // Process results
    }
}
```

## Error Handling

### Database Exceptions
```csharp
public class DatabaseException : ApplicationException
{
    public DatabaseException(string message) : base(message) { }
    public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
}
```

### Exception Categories
```csharp
// Handle specific database errors
protected virtual bool HandleSQLException(Exception e)
{
    if (e is MySqlException sqlEx)
    {
        switch ((MySqlErrorCode)sqlEx.Number)
        {
            case MySqlErrorCode.DuplicateUnique:
            case MySqlErrorCode.DuplicateKeyEntry:
                return true; // Non-fatal, can continue
            default:
                return false; // Fatal error
        }
    }
    return false;
}
```

### Connection Recovery
```csharp
// Automatic retry on connection failures
protected virtual bool HandleException(Exception e)
{
    var socketException = e.InnerException as System.Net.Sockets.SocketException;
    
    if (socketException != null)
    {
        switch (socketException.ErrorCode)
        {
            case 10052: // Network dropped connection
            case 10053: // Software caused connection abort
            case 10054: // Connection reset by peer
                return true; // Retry operation
        }
    }
    return false;
}
```

## Async Operations

### Task-Based Async Pattern
```csharp
public static async Task<T> FindObjectsByKeyAsync(object key)
{
    return await Task.Factory.StartNew(
        static (state) => GameServer.Database.FindObjectByKey<T>(state),
        key,
        CancellationToken.None,
        TaskCreationOptions.DenyChildAttach,
        TaskScheduler.Default).ConfigureAwait(false);
}
```

### Async Best Practices
```csharp
// Non-blocking database operations
var characters = await DOLDB<DbCharacter>.SelectObjectsAsync(whereClause);

// Parallel operations
var tasks = new[]
{
    DOLDB<DbCharacter>.SelectObjectsAsync(realmClause),
    DOLDB<DbAccount>.SelectObjectsAsync(activeClause),
    DOLDB<DbGuild>.SelectObjectsAsync(guildClause)
};

var results = await Task.WhenAll(tasks);
```

## Database Providers

### MySQL Implementation
```csharp
public class MySqlObjectDatabase : SqlObjectDatabase
{
    // MySQL-specific type mappings
    protected override string GetDatabaseType(ElementBinding bind)
    {
        return bind.ValueType.Name switch
        {
            "String" => $"VARCHAR({bind.MaxLength ?? 255})",
            "Int32" => "INT",
            "Int64" => "BIGINT",
            "DateTime" => "DATETIME",
            "Boolean" => "TINYINT(1)",
            _ => "TEXT"
        };
    }
    
    // MySQL-specific features
    protected override object ExecuteScalar(string command)
    {
        // MySQL last insert ID
        if (command.StartsWith("INSERT"))
            return GetLastInsertId();
        
        return base.ExecuteScalar(command);
    }
}
```

### SQLite Implementation
```csharp
public class SqliteObjectDatabase : SqlObjectDatabase
{
    // SQLite-specific optimizations
    public SqliteObjectDatabase(string connectionString) : base(connectionString)
    {
        // SQLite performance settings
        ExecuteNonQuery("PRAGMA journal_mode = WAL;");
        ExecuteNonQuery("PRAGMA synchronous = NORMAL;");
        ExecuteNonQuery("PRAGMA cache_size = 1000000;");
    }
    
    // SQLite type mappings
    protected override string GetDatabaseType(ElementBinding bind)
    {
        return bind.ValueType.Name switch
        {
            "String" => "TEXT",
            "Int32" => "INTEGER",
            "Int64" => "INTEGER",
            "DateTime" => "TEXT", // ISO 8601 format
            "Boolean" => "INTEGER",
            _ => "BLOB"
        };
    }
}
```

## Configuration

### Connection String Examples
```ini
# MySQL Production
ConnectionString = "Server=localhost;Port=3306;Database=dol;User ID=dol_user;Password=secure_password;Pooling=true;Min Pool Size=5;Max Pool Size=50;Connection Timeout=30;Command Timeout=60;"

# SQLite Development
ConnectionString = "Data Source=dol.sqlite3;Version=3;Pooling=False;Cache Size=1073741824;Journal Mode=WAL;Synchronous=NORMAL;Foreign Keys=True;Default Timeout=60;"

# SQLite In-Memory (Testing)
ConnectionString = "Data Source=:memory:;Version=3;Cache Size=1073741824;Journal Mode=Memory;Synchronous=Off;"
```

### Performance Tuning
```ini
# MySQL Performance Settings
max_connections = 500
innodb_buffer_pool_size = 1G
innodb_log_file_size = 256M
query_cache_size = 128M

# SQLite Performance Settings
PRAGMA cache_size = 1000000;    # 1GB cache
PRAGMA journal_mode = WAL;      # Write-Ahead Logging
PRAGMA synchronous = NORMAL;    # Balanced safety/performance
PRAGMA temp_store = MEMORY;     # Temp tables in RAM
```

## Usage Examples

### Basic CRUD Operations
```csharp
// Create
var character = new DbCharacter
{
    Character_ID = Guid.NewGuid().ToString(),
    Name = "TestPlayer",
    Level = 1,
    Realm = (int)eRealm.Albion
};
Database.AddObject(character);

// Read
var player = DOLDB<DbCharacter>.FindObjectByKey(characterId);
var highLevelPlayers = DOLDB<DbCharacter>.SelectObjects(
    DB.Column("Level").IsGreaterThan(40)
);

// Update
player.Level = 50;
player.MarkDirty();
Database.SaveObject(player);

// Delete
Database.DeleteObject(player);
```

### Complex Queries
```csharp
// Multi-condition query
var whereClause = DB.Column("Level").IsGreaterOrEqualTo(40)
    .And(DB.Column("Level").IsLessOrEqualTo(50))
    .And(DB.Column("Realm").IsEqualTo((int)eRealm.Albion))
    .And(DB.Column("LastPlayed").IsGreaterThan(DateTime.Now.AddDays(-30)));

var activePlayers = DOLDB<DbCharacter>.SelectObjects(whereClause);

// Batch operations
var batch = new[]
{
    DB.Column("Guild_ID").IsEqualTo("guild1"),
    DB.Column("Guild_ID").IsEqualTo("guild2"),
    DB.Column("Guild_ID").IsEqualTo("guild3")
};

var guildMemberLists = DOLDB<DbCharacter>.MultipleSelectObjects(batch);
```

## Test Scenarios

### Unit Testing Database Operations
```csharp
[Test]
public void TestCharacterCRUD()
{
    // Arrange
    var character = new DbCharacter
    {
        Character_ID = "test_character",
        Name = "TestPlayer",
        Level = 1
    };
    
    // Act & Assert
    // Create
    var added = Database.AddObject(character);
    added.Should().BeTrue();
    
    // Read
    var retrieved = DOLDB<DbCharacter>.FindObjectByKey("test_character");
    retrieved.Should().NotBeNull();
    retrieved.Name.Should().Be("TestPlayer");
    
    // Update
    retrieved.Level = 50;
    retrieved.MarkDirty();
    var saved = Database.SaveObject(retrieved);
    saved.Should().BeTrue();
    
    // Verify update
    var updated = DOLDB<DbCharacter>.FindObjectByKey("test_character");
    updated.Level.Should().Be(50);
    
    // Delete
    var deleted = Database.DeleteObject(updated);
    deleted.Should().BeTrue();
    
    // Verify deletion
    var notFound = DOLDB<DbCharacter>.FindObjectByKey("test_character");
    notFound.Should().BeNull();
}
```

### Performance Testing
```csharp
[Test]
public void TestBatchInsertPerformance()
{
    // Arrange
    var characters = new List<DbCharacter>();
    for (int i = 0; i < 1000; i++)
    {
        characters.Add(new DbCharacter
        {
            Character_ID = $"perf_test_{i}",
            Name = $"Player{i}",
            Level = i % 50 + 1
        });
    }
    
    // Act
    var stopwatch = Stopwatch.StartNew();
    var results = Database.AddObjects(characters);
    stopwatch.Stop();
    
    // Assert
    results.Should().AllBeEquivalentTo(true);
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // < 5 seconds for 1000 inserts
}
```

## Change Log
- 2024-01-20: Initial comprehensive documentation
- TODO: Document migration system
- TODO: Add connection pool monitoring

## References
- `CoreDatabase/ObjectDatabase.cs`
- `CoreDatabase/SqlObjectDatabase.cs`  
- `CoreDatabase/Handlers/MySqlObjectDatabase.cs`
- `CoreDatabase/Handlers/SqliteObjectDatabase.cs`
- `GameServer/database/DOLDB.cs`
- `Tests/IntegrationTests/Database/` 