# Script Extension System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

**Game Rule Summary**: The script extension system allows server administrators to add custom content, events, and features without restarting the server. This enables dynamic updates like special events, new quests, custom NPCs, and unique server features that enhance gameplay while maintaining server stability and performance.

The Script Extension System enables dynamic loading, compilation, and execution of C# scripts at runtime, providing extensibility for OpenDAoC without requiring server restarts. It supports hot-reloading of scripts, event handling, command creation, and complete game logic customization through a sophisticated compilation and execution framework.

## Core Architecture

### Script Manager Framework

```csharp
public static class ScriptMgr
{
    private static readonly List<Assembly> m_gameServerScripts = new();
    private static readonly Dictionary<string, GameCommand> m_gameCommands = new();
    private static readonly Assembly m_compiledScripts;
    
    // Script compilation settings
    private static readonly string[] ASSEMBLY_NAMES = {
        "GameServer.dll",
        "CoreBase.dll", 
        "CoreDatabase.dll",
        "System.dll",
        "System.Core.dll"
    };
    
    public static List<Assembly> GameServerScripts => m_gameServerScripts;
    public static Assembly CompiledScripts => m_compiledScripts;
}
```

### Dynamic Script Compilation

```csharp
public static bool CompileScripts(bool compileVB, string scriptFolder, 
                                 string outputPath, string[] assemblyNames)
{
    var outputFile = new FileInfo(outputPath);
    var sourceFiles = ParseDirectory(new DirectoryInfo(scriptFolder), 
                                   compileVB ? "*.vb" : "*.cs", true);
    
    if (!sourceFiles.Any())
    {
        log.Info("No script source files found for compilation");
        return true;
    }
    
    // Check if recompilation needed
    if (outputFile.Exists && !RequiresRecompilation(outputFile, sourceFiles))
    {
        log.Info("Scripts are up to date, skipping compilation");
        return true;
    }
    
    // Setup compiler
    var compiler = compileVB ? 
        CodeDomProvider.CreateProvider("VisualBasic") : 
        CodeDomProvider.CreateProvider("CSharp");
    
    var compilerParams = new CompilerParameters
    {
        GenerateExecutable = false,
        GenerateInMemory = false,
        OutputAssembly = outputPath,
        IncludeDebugInformation = true,
        CompilerOptions = "/target:library /optimize"
    };
    
    // Add referenced assemblies
    foreach (string assembly in assemblyNames)
    {
        compilerParams.ReferencedAssemblies.Add(assembly);
    }
    
    // Compile all source files
    var results = compiler.CompileAssemblyFromFile(compilerParams, 
                    sourceFiles.Select(f => f.FullName).ToArray());
    
    // Handle compilation errors
    if (results.Errors.HasErrors)
    {
        log.Error("Script compilation failed:");
        foreach (CompilerError error in results.Errors)
        {
            log.Error($"{error.FileName}({error.Line},{error.Column}): {error.ErrorText}");
        }
        return false;
    }
    
    log.Info($"Successfully compiled {sourceFiles.Count} script files");
    return true;
}
```

### Hot-Reload System

```csharp
public static class ScriptReloader
{
    private static FileSystemWatcher _scriptWatcher;
    private static readonly Timer _recompileTimer;
    private static volatile bool _recompileScheduled;
    
    public static void StartWatching(string scriptDirectory)
    {
        _scriptWatcher = new FileSystemWatcher(scriptDirectory, "*.cs")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };
        
        _scriptWatcher.Changed += OnScriptChanged;
        _scriptWatcher.Created += OnScriptChanged;
        _scriptWatcher.EnableRaisingEvents = true;
        
        log.Info("Script hot-reload monitoring started");
    }
    
    private static void OnScriptChanged(object sender, FileSystemEventArgs e)
    {
        if (_recompileScheduled)
            return;
            
        _recompileScheduled = true;
        
        // Delay recompilation to avoid multiple rapid recompiles
        _recompileTimer.Change(TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
    }
    
    private static void PerformRecompilation(object state)
    {
        try
        {
            log.Info("Hot-reloading scripts...");
            
            // Unload existing scripts
            UnloadScripts();
            
            // Recompile and reload
            if (ScriptMgr.CompileScripts())
            {
                ScriptMgr.LoadScripts();
                log.Info("Scripts hot-reloaded successfully");
                
                // Notify online GMs
                NotifyGMsOfReload();
            }
            else
            {
                log.Error("Script hot-reload failed");
            }
        }
        finally
        {
            _recompileScheduled = false;
        }
    }
}
```

## Script Loading Framework

### Assembly Loading and Validation

```csharp
public static bool LoadScripts()
{
    try
    {
        // Clear existing scripts
        m_gameServerScripts.Clear();
        m_gameCommands.Clear();
        
        // Load compiled script assembly
        string scriptAssemblyPath = Path.Combine(GameServer.Instance.Configuration.RootDirectory, 
                                                 "scripts", "GameServerScripts.dll");
        
        if (File.Exists(scriptAssemblyPath))
        {
            var scriptAssembly = Assembly.LoadFrom(scriptAssemblyPath);
            m_gameServerScripts.Add(scriptAssembly);
            
            log.Info($"Loaded script assembly: {scriptAssembly.GetName().Name}");
        }
        
        // Load additional script assemblies
        LoadAdditionalAssemblies();
        
        // Initialize script components
        return InitializeScriptComponents();
    }
    catch (Exception ex)
    {
        log.Error("Failed to load scripts", ex);
        return false;
    }
}

private static bool InitializeScriptComponents()
{
    bool success = true;
    
    // Load commands
    success &= LoadCommands();
    
    // Register database tables
    success &= InitializeScriptDatabaseTables();
    
    // Load skills
    success &= LoadSkills();
    
    // Register event handlers
    success &= RegisterEventHandlers();
    
    return success;
}
```

### Command Registration System

```csharp
public static bool LoadCommands(bool quiet = false)
{
    m_gameCommands.Clear();
    
    // Build array of disabled commands
    string[] disabledCommands = ServerProperties.Properties.DISABLED_COMMANDS.Split(';');
    
    foreach (var scriptAssembly in GameServerScripts)
    {
        foreach (Type type in scriptAssembly.GetTypes())
        {
            if (!type.IsClass || type.GetInterface("DOL.GS.Commands.ICommandHandler") == null)
                continue;
            
            // Get command attributes
            var cmdAttributes = type.GetCustomAttributes<CmdAttribute>(false);
            
            foreach (var cmdAttribute in cmdAttributes)
            {
                string commandName = cmdAttribute.Cmd;
                
                // Check if command is disabled
                if (disabledCommands.Contains(commandName.Substring(1)))
                {
                    log.Info($"Command '{commandName}' is disabled, skipping registration");
                    continue;
                }
                
                try
                {
                    // Create command handler instance
                    var cmdHandler = Activator.CreateInstance(type) as ICommandHandler;
                    
                    var gameCommand = new GameCommand
                    {
                        m_cmd = commandName,
                        m_cmdHandler = cmdHandler,
                        m_lvl = (uint)cmdAttribute.Level,
                        m_desc = cmdAttribute.Description,
                        m_usage = cmdAttribute.Usage
                    };
                    
                    // Register command and aliases
                    m_gameCommands[commandName] = gameCommand;
                    
                    foreach (string alias in cmdAttribute.Aliases)
                    {
                        m_gameCommands[alias] = gameCommand;
                    }
                    
                    if (!quiet)
                        log.Debug($"Registered command: {commandName} (Level: {cmdAttribute.Level})");
                }
                catch (Exception ex)
                {
                    log.Error($"Failed to register command '{commandName}'", ex);
                }
            }
        }
    }
    
    log.Info($"Loaded {m_gameCommands.Count} commands from scripts");
    return true;
}
```

## Event System Integration

### Script Event Registration

```csharp
private static bool RegisterEventHandlers()
{
    try
    {
        foreach (Assembly scriptAssembly in GameServerScripts)
        {
            // Register global events
            GameEventMgr.RegisterGlobalEvents(scriptAssembly, 
                typeof(GameServerStartedEventAttribute), GameServerEvent.Started);
            GameEventMgr.RegisterGlobalEvents(scriptAssembly, 
                typeof(GameServerStoppedEventAttribute), GameServerEvent.Stopped);
            GameEventMgr.RegisterGlobalEvents(scriptAssembly, 
                typeof(ScriptLoadedEventAttribute), ScriptEvent.Loaded);
            GameEventMgr.RegisterGlobalEvents(scriptAssembly, 
                typeof(ScriptUnloadedEventAttribute), ScriptEvent.Unloaded);
        }
        
        log.Info("Script event handlers registered successfully");
        return true;
    }
    catch (Exception ex)
    {
        log.Error("Failed to register script event handlers", ex);
        return false;
    }
}
```

### Script Lifecycle Events

```csharp
// Script loaded event
[ScriptLoadedEvent]
public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
{
    // Initialize script-specific resources
    InitializeScriptResources();
    
    // Register custom event handlers
    GameEventMgr.AddHandler(GamePlayerEvent.LevelUp, OnPlayerLevelUp);
    GameEventMgr.AddHandler(GamePlayerEvent.Killed, OnPlayerKilled);
}

// Script unloaded event
[ScriptUnloadedEvent]
public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
{
    // Cleanup script resources
    CleanupScriptResources();
    
    // Unregister event handlers
    GameEventMgr.RemoveHandler(GamePlayerEvent.LevelUp, OnPlayerLevelUp);
    GameEventMgr.RemoveHandler(GamePlayerEvent.Killed, OnPlayerKilled);
}
```

## Database Integration

### Script Table Registration

```csharp
protected bool InitializeScriptDatabaseTables()
{
    try
    {
        foreach (Assembly scriptAssembly in GameServerScripts)
        {
            foreach (Type type in scriptAssembly.GetTypes())
            {
                if (type.IsClass && type.BaseType == typeof(DataObject))
                {
                    // Register data object type with database
                    GameServer.Database.RegisterDataObject(type);
                    
                    log.Debug($"Registered database table for type: {type.Name}");
                }
            }
        }
        
        log.Info("Script database tables initialized successfully");
        return true;
    }
    catch (DatabaseException dbex)
    {
        log.Error("Error initializing script database tables", dbex);
        return false;
    }
}
```

### Script Data Persistence

```csharp
// Example script data object
[DataTable(TableName = "ScriptConfiguration")]
public class DbScriptConfig : DataObject
{
    [PrimaryKey]
    public string ScriptName { get; set; }
    
    [DataElement]
    public string ConfigData { get; set; }
    
    [DataElement]
    public DateTime LastModified { get; set; }
    
    [DataElement]
    public bool IsEnabled { get; set; }
}

// Script configuration manager
public static class ScriptConfigMgr
{
    private static readonly Dictionary<string, DbScriptConfig> _configurations = new();
    
    public static void LoadConfigurations()
    {
        var configs = GameServer.Database.SelectAllObjects<DbScriptConfig>();
        foreach (var config in configs)
        {
            _configurations[config.ScriptName] = config;
        }
    }
    
    public static T GetConfig<T>(string scriptName, T defaultValue = default)
    {
        if (_configurations.TryGetValue(scriptName, out var config))
        {
            return JsonConvert.DeserializeObject<T>(config.ConfigData);
        }
        return defaultValue;
    }
    
    public static void SaveConfig<T>(string scriptName, T configData)
    {
        var config = _configurations.GetValueOrDefault(scriptName) ?? new DbScriptConfig
        {
            ScriptName = scriptName
        };
        
        config.ConfigData = JsonConvert.SerializeObject(configData);
        config.LastModified = DateTime.UtcNow;
        config.IsEnabled = true;
        
        GameServer.Database.SaveObject(config);
        _configurations[scriptName] = config;
    }
}
```

## Script Security Framework

### Sandboxing and Validation

```csharp
public static class ScriptSecurityManager
{
    private static readonly string[] FORBIDDEN_NAMESPACES = {
        "System.IO.File",
        "System.Diagnostics.Process",
        "System.Reflection.Emit",
        "System.Runtime.InteropServices"
    };
    
    public static bool ValidateScript(string scriptContent)
    {
        // Check for forbidden operations
        foreach (string forbiddenNamespace in FORBIDDEN_NAMESPACES)
        {
            if (scriptContent.Contains(forbiddenNamespace))
            {
                log.Warn($"Script contains forbidden namespace: {forbiddenNamespace}");
                return false;
            }
        }
        
        // Validate syntax
        return ValidateSyntax(scriptContent);
    }
    
    private static bool ValidateSyntax(string scriptContent)
    {
        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(scriptContent);
            var diagnostics = syntaxTree.GetDiagnostics();
            
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
            if (errors.Any())
            {
                foreach (var error in errors)
                {
                    log.Error($"Script syntax error: {error.GetMessage()}");
                }
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            log.Error("Failed to validate script syntax", ex);
            return false;
        }
    }
}
```

### Permission System

```csharp
public enum ScriptPermission
{
    DatabaseAccess,
    FileSystemAccess,
    NetworkAccess,
    PlayerManagement,
    ServerConfiguration,
    AdminCommands
}

public static class ScriptPermissionManager
{
    private static readonly Dictionary<string, HashSet<ScriptPermission>> _scriptPermissions = new();
    
    public static bool HasPermission(string scriptName, ScriptPermission permission)
    {
        return _scriptPermissions.GetValueOrDefault(scriptName)?.Contains(permission) ?? false;
    }
    
    public static void GrantPermission(string scriptName, ScriptPermission permission)
    {
        if (!_scriptPermissions.ContainsKey(scriptName))
            _scriptPermissions[scriptName] = new HashSet<ScriptPermission>();
        
        _scriptPermissions[scriptName].Add(permission);
        log.Info($"Granted {permission} permission to script: {scriptName}");
    }
    
    public static void RevokePermission(string scriptName, ScriptPermission permission)
    {
        _scriptPermissions.GetValueOrDefault(scriptName)?.Remove(permission);
        log.Info($"Revoked {permission} permission from script: {scriptName}");
    }
}
```

## Script Templates and Framework

### Base Script Template

```csharp
public abstract class BaseScript
{
    protected static readonly Logging.Logger log = 
        Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    
    public virtual string Name => GetType().Name;
    public virtual string Version => "1.0";
    public virtual string Author => "Unknown";
    public virtual string Description => "";
    
    // Script lifecycle methods
    public virtual bool OnScriptLoaded() { return true; }
    public virtual void OnScriptUnloaded() { }
    
    // Event handling helpers
    protected void RegisterEventHandler<T>(T eventType, DOLEventHandler handler) where T : DOLEvent
    {
        GameEventMgr.AddHandler(eventType, handler);
    }
    
    protected void UnregisterEventHandler<T>(T eventType, DOLEventHandler handler) where T : DOLEvent
    {
        GameEventMgr.RemoveHandler(eventType, handler);
    }
}
```

### Quest Script Framework

```csharp
public abstract class BaseQuest : BaseScript
{
    public abstract string QuestTitle { get; }
    public abstract string QuestDescription { get; }
    public abstract int MinLevel { get; }
    public abstract int MaxLevel { get; }
    
    protected GameNPC QuestGiver { get; set; }
    protected List<GamePlayer> ActivePlayers { get; } = new();
    
    public virtual bool CanStartQuest(GamePlayer player)
    {
        return player.Level >= MinLevel && player.Level <= MaxLevel;
    }
    
    public virtual void StartQuest(GamePlayer player)
    {
        ActivePlayers.Add(player);
        OnQuestStarted(player);
    }
    
    protected abstract void OnQuestStarted(GamePlayer player);
    protected abstract void OnQuestCompleted(GamePlayer player);
    protected abstract void OnQuestAborted(GamePlayer player);
}
```

### NPC Behavior Script Framework

```csharp
public abstract class BaseNPCScript : BaseScript
{
    protected GameNPC ControlledNPC { get; set; }
    
    public virtual void Initialize(GameNPC npc)
    {
        ControlledNPC = npc;
        OnInitialized();
    }
    
    protected virtual void OnInitialized() { }
    
    protected virtual bool OnInteract(GamePlayer player)
    {
        return true; // Continue with default interaction
    }
    
    protected virtual void OnAggro(AttackData ad)
    {
        // Custom aggro behavior
    }
    
    protected virtual void OnDeath()
    {
        // Custom death behavior
    }
}
```

## Performance Monitoring

### Script Performance Tracking

```csharp
public static class ScriptPerformanceMonitor
{
    private static readonly Dictionary<string, ScriptMetrics> _scriptMetrics = new();
    
    public static void TrackScriptExecution(string scriptName, string methodName, long executionTime)
    {
        if (!_scriptMetrics.TryGetValue(scriptName, out ScriptMetrics metrics))
        {
            metrics = new ScriptMetrics(scriptName);
            _scriptMetrics[scriptName] = metrics;
        }
        
        metrics.RecordExecution(methodName, executionTime);
        
        // Alert on slow scripts
        if (executionTime > 100) // 100ms threshold
        {
            log.Warn($"Slow script execution: {scriptName}.{methodName} took {executionTime}ms");
        }
    }
    
    public static void GeneratePerformanceReport()
    {
        log.Info("=== Script Performance Report ===");
        
        foreach (var kvp in _scriptMetrics.OrderByDescending(x => x.Value.TotalExecutionTime))
        {
            var metrics = kvp.Value;
            log.Info($"{metrics.ScriptName}: {metrics.TotalExecutions} calls, " +
                    $"{metrics.TotalExecutionTime}ms total, " +
                    $"{metrics.AverageExecutionTime:F2}ms avg");
        }
    }
}

public class ScriptMetrics
{
    public string ScriptName { get; }
    public long TotalExecutions { get; private set; }
    public long TotalExecutionTime { get; private set; }
    public double AverageExecutionTime => TotalExecutions > 0 ? (double)TotalExecutionTime / TotalExecutions : 0;
    
    private readonly Dictionary<string, long> _methodCalls = new();
    
    public ScriptMetrics(string scriptName)
    {
        ScriptName = scriptName;
    }
    
    public void RecordExecution(string methodName, long executionTime)
    {
        TotalExecutions++;
        TotalExecutionTime += executionTime;
        
        _methodCalls[methodName] = _methodCalls.GetValueOrDefault(methodName, 0) + 1;
    }
}
```

## Configuration and Deployment

### Script Configuration

```ini
# Script system settings
SCRIPT_COMPILATION_ENABLED = true
SCRIPT_HOT_RELOAD_ENABLED = false
SCRIPT_SECURITY_ENABLED = true
SCRIPT_PERFORMANCE_MONITORING = true

# Compilation settings
SCRIPT_DEBUG_INFORMATION = true
SCRIPT_OPTIMIZATION_ENABLED = false
SCRIPT_COMPILATION_TIMEOUT = 30000

# Security settings
SCRIPT_SANDBOX_ENABLED = true
SCRIPT_PERMISSION_CHECKING = true
FORBIDDEN_NAMESPACES = "System.IO.File;System.Diagnostics.Process"

# Performance settings
SCRIPT_EXECUTION_TIMEOUT = 5000
SCRIPT_PERFORMANCE_ALERT_THRESHOLD = 100
SCRIPT_METRICS_RETENTION_HOURS = 24
```

### Deployment Process

```csharp
public static class ScriptDeploymentManager
{
    public static bool DeployScript(string scriptPath, bool hotReload = false)
    {
        try
        {
            // Validate script
            string scriptContent = File.ReadAllText(scriptPath);
            if (!ScriptSecurityManager.ValidateScript(scriptContent))
            {
                log.Error($"Script validation failed: {scriptPath}");
                return false;
            }
            
            // Copy to scripts directory
            string targetPath = Path.Combine(GetScriptsDirectory(), Path.GetFileName(scriptPath));
            File.Copy(scriptPath, targetPath, true);
            
            // Trigger recompilation if hot reload enabled
            if (hotReload && ServerProperties.Properties.SCRIPT_HOT_RELOAD_ENABLED)
            {
                ScriptReloader.ScheduleRecompilation();
            }
            
            log.Info($"Script deployed successfully: {Path.GetFileName(scriptPath)}");
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"Failed to deploy script: {scriptPath}", ex);
            return false;
        }
    }
}
```

## Integration Points

### Server Integration

The Script Extension System integrates with multiple server components:

- **Command System**: Dynamic command registration and execution
- **Event System**: Script event handling and custom event publishing  
- **Database System**: Script table registration and data persistence
- **Configuration System**: Script-specific configuration management
- **Security System**: Permission validation and sandboxing
- **Performance System**: Execution monitoring and optimization

### External Tool Integration

```csharp
// Visual Studio integration for script debugging
public static class ScriptDebugger
{
    public static bool AttachDebugger(int processId)
    {
        try
        {
            var debugger = System.Diagnostics.Debugger.Launch();
            return debugger;
        }
        catch (Exception ex)
        {
            log.Error("Failed to attach debugger", ex);
            return false;
        }
    }
}
```

This comprehensive Script Extension System enables powerful customization capabilities while maintaining security and performance standards, making OpenDAoC highly extensible for custom content and modifications. 