# ECS Debugging and Diagnostics System

**Document Status:** Complete Architecture Analysis  
**Verification:** Code-verified from ECS.Debug namespace and diagnostic tools  
**Implementation Status:** Live Production

## Overview

**Game Rule Summary**: The ECS debugging system monitors game performance in real-time, tracking how long different actions take and identifying potential problems before they affect players. Game Masters can use these tools to diagnose lag issues, monitor server health, and ensure optimal performance during peak usage times.

OpenDAoC's ECS Debugging and Diagnostics System provides comprehensive monitoring, performance tracking, and troubleshooting capabilities for the Entity Component System. This system enables real-time performance analysis, entity counting, long tick detection, and detailed profiling of all ECS services.

## Core Diagnostics Architecture

### Diagnostics System Interface

#### Central Diagnostics Manager
```csharp
public static class Diagnostics
{
    private static StreamWriter _perfStreamWriter;
    private static bool _perfCountersEnabled;
    private static Dictionary<string, Stopwatch> _perfCounters = new();
    private static readonly Lock _perfCountersLock = new();
    
    public static bool CheckEntityCounts { get; private set; }
    public static int LongTickThreshold { get; set; } = 25; // 25ms default
    
    public static void Tick()
    {
        ReportPerfCounters();
        ProcessEntityCountRequests();
        
        // Game rule: Performance monitoring runs every tick when enabled
        if (_gameEventMgrNotifyProfilingEnabled)
            ProcessGameEventNotifyTimes();
    }
}
```

#### Performance Counter Management
```csharp
public static void StartPerfCounter(string uniqueID)
{
    if (!_perfCountersEnabled) return;
    
    InitializeStreamWriter();
    Stopwatch stopwatch = Stopwatch.StartNew();
    
    lock (_perfCountersLock)
    {
        _perfCounters.TryAdd(uniqueID, stopwatch);
    }
}

public static void StopPerfCounter(string uniqueID)
{
    if (!_perfCountersEnabled) return;
    
    lock (_perfCountersLock)
    {
        if (_perfCounters.TryGetValue(uniqueID, out Stopwatch stopwatch))
            stopwatch.Stop();
    }
}

private static void ReportPerfCounters()
{
    if (!_perfCountersEnabled) return;
    
    lock (_perfCountersLock)
    {
        if (_perfCounters.Count > 0)
        {
            string logString = "[PerfCounters] ";
            
            foreach (var counter in _perfCounters)
            {
                string counterName = counter.Key;
                string elapsedString = $"{counter.Value.Elapsed.TotalMilliseconds:0.##}";
                logString += $"{counterName} {elapsedString}ms | ";
            }
            
            _perfStreamWriter.WriteLine(logString);
            _perfCounters.Clear();
        }
    }
}
```

## Service Performance Monitoring

### Automatic Performance Tracking

#### Service Performance Integration
```csharp
// Every ECS service includes standardized performance monitoring
public static class AttackService
{
    public static void Tick()
    {
        GameLoop.CurrentServiceTick = SERVICE_NAME;
        Diagnostics.StartPerfCounter(SERVICE_NAME); // Start timing
        
        // Service processing...
        _list = ServiceObjectStore.UpdateAndGetAll<AttackComponent>(
            ServiceObjectType.AttackComponent, out int lastValidIndex);
        GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);
        
        // Entity counting for capacity monitoring
        if (Diagnostics.CheckEntityCounts)
            Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);
        
        Diagnostics.StopPerfCounter(SERVICE_NAME); // End timing
    }
}
```

#### Long Tick Detection
```csharp
private static void TickInternal(int index)
{
    AttackComponent attackComponent = _list[index];
    
    long startTick = GameLoop.GetRealTime();
    attackComponent.Tick();
    long stopTick = GameLoop.GetRealTime();
    
    // Game rule: Warn about components taking longer than threshold
    if (stopTick - startTick > Diagnostics.LongTickThreshold)
    {
        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for: " +
                $"{attackComponent.owner.Name}({attackComponent.owner.ObjectID}) " +
                $"Time: {stopTick - startTick}ms");
    }
}
```

### Entity Count Monitoring

#### Real-Time Entity Counting
```csharp
public static void PrintEntityCount(string serviceName, ref int nonNull, int total)
{
    log.Debug($"==== {FormatCount(nonNull),-4} / {FormatCount(total),4} " +
             $"non-null entities in {serviceName}'s list ====");
    nonNull = 0;
    
    static string FormatCount(int count)
    {
        return count >= 1000000 ? (count / 1000000.0).ToString("G3") + "M" :
               count >= 1000 ? (count / 1000.0).ToString("G3") + "K" :
               count.ToString();
    }
}

// Usage in services
private static void TickInternal(int index)
{
    if (Diagnostics.CheckEntityCounts)
        Interlocked.Increment(ref _entityCount); // Thread-safe counting
    
    // Process component...
}
```

#### Entity Count Request System
```csharp
public static bool RequestCheckEntityCounts { get; set; }

private static void ProcessEntityCountRequests()
{
    if (RequestCheckEntityCounts)
    {
        _checkEntityCountTicks = 1; // Enable for one tick
        RequestCheckEntityCounts = false;
    }
    else if (CheckEntityCounts)
    {
        _checkEntityCountTicks--; // Countdown
    }
}
```

## GameEventMgr Profiling

### Event System Performance Monitoring

#### Event Timing Collection
```csharp
private static Dictionary<string, List<double>> _gameEventMgrNotifyTimes = new();
private static Stopwatch _gameEventMgrNotifyStopwatch;

public static void BeginGameEventMgrNotify()
{
    if (!_gameEventMgrNotifyProfilingEnabled) return;
    
    _gameEventMgrNotifyStopwatch = Stopwatch.StartNew();
}

public static void EndGameEventMgrNotify(DOLEvent e)
{
    if (!_gameEventMgrNotifyProfilingEnabled) return;
    
    _gameEventMgrNotifyStopwatch.Stop();
    
    lock (_gameEventMgrNotifyLock)
    {
        if (_gameEventMgrNotifyTimes.TryGetValue(e.Name, out List<double> eventTimeValues))
            eventTimeValues.Add(_gameEventMgrNotifyStopwatch.Elapsed.TotalMilliseconds);
        else
        {
            eventTimeValues = new();
            eventTimeValues.Add(_gameEventMgrNotifyStopwatch.Elapsed.TotalMilliseconds);
            _gameEventMgrNotifyTimes.TryAdd(e.Name, eventTimeValues);
        }
    }
}
```

#### Event Performance Reporting
```csharp
private static void ReportGameEventMgrNotifyTimes()
{
    string actualInterval = Util.TruncateString(
        (GameLoop.GetRealTime() - _gameEventMgrNotifyTimerStartTick).ToString(), 5);
    
    log.Debug($"==== GameEventMgr Notify() Costs " +
             $"(Requested: {_gameEventMgrNotifyTimerInterval}ms | " +
             $"Actual: {actualInterval}ms) ====");
    
    lock (_gameEventMgrNotifyLock)
    {
        foreach (var notifyData in _gameEventMgrNotifyTimes)
        {
            List<double> eventTimeValues = notifyData.Value;
            string eventNameString = notifyData.Key.PadRight(30);
            
            double totalCost = 0;
            double minCost = double.MaxValue;
            double maxCost = 0;
            
            foreach (double time in eventTimeValues)
            {
                totalCost += time;
                if (time < minCost) minCost = time;
                if (time > maxCost) maxCost = time;
            }
            
            int numValues = eventTimeValues.Count;
            double avgCost = totalCost / numValues;
            
            log.Debug($"{eventNameString} - # Calls: {numValues,4} | " +
                     $"Total: {totalCost:0.00}ms | Avg: {avgCost:0.00}ms | " +
                     $"Min: {minCost:0.00}ms | Max: {maxCost:0.00}ms");
        }
        
        _gameEventMgrNotifyTimes.Clear();
        _gameEventMgrNotifyTimerStartTick = GameLoop.GetRealTime();
    }
}
```

## Administrative Commands

### In-Game Diagnostic Commands

#### /diag Command System
```csharp
[Cmd("&diag", ePrivLevel.GM,
    "Toggle server logging of performance diagnostics.",
    "/diag perf <on|off> to toggle performance diagnostics logging on server.",
    "/diag notify <on|off> <interval> to toggle GameEventMgr Notify profiling.",
    "/diag timer <tickcount> enables debugging of the TimerService.",
    "/diag entity to count non-null service objects in ServiceObjectStore arrays")]
public class ECSDiagnosticsCommandHandler : AbstractCommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (args[1].Equals("entity", StringComparison.OrdinalIgnoreCase))
        {
            Diagnostics.RequestCheckEntityCounts = true;
            DisplayMessage(client, "Counting entities...");
            return;
        }
        
        if (args[1].Equals("perf", StringComparison.OrdinalIgnoreCase))
        {
            if (args[2].Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                Diagnostics.TogglePerfCounters(true);
                DisplayMessage(client, "Performance diagnostics logging turned on. " +
                              "WARNING: This will spam the server logs.");
            }
            else if (args[2].Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                Diagnostics.TogglePerfCounters(false);
                DisplayMessage(client, "Performance diagnostics logging turned off.");
            }
        }
        
        if (args[1].Equals("notify", StringComparison.OrdinalIgnoreCase))
        {
            if (args[2].Equals("on", StringComparison.OrdinalIgnoreCase))
            {
                int interval = int.Parse(args[3]);
                if (interval <= 0)
                {
                    DisplayMessage(client, "Invalid interval. Specify value in milliseconds.");
                    return;
                }
                
                Diagnostics.StartGameEventMgrNotifyTimeReporting(interval);
                DisplayMessage(client, "GameEventMgr Notify() logging turned on. " +
                              "WARNING: This will spam the server logs.");
            }
            else if (args[2].Equals("off", StringComparison.OrdinalIgnoreCase))
            {
                Diagnostics.StopGameEventMgrNotifyTimeReporting();
                DisplayMessage(client, "GameEventMgr Notify() logging turned off.");
            }
        }
    }
}
```

## Performance File Logging

### Continuous Performance Logging

#### Performance Log File Management
```csharp
private static void InitializeStreamWriter()
{
    if (_streamWriterInitialized) return;
    
    string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                                   "PerfLog" + DateTime.Now.ToFileTime());
    _perfStreamWriter = new StreamWriter(_filePath, false);
    _streamWriterInitialized = true;
}

public static void TogglePerfCounters(bool enabled)
{
    if (enabled == false)
    {
        _perfStreamWriter?.Close();
        _streamWriterInitialized = false;
    }
    
    _perfCountersEnabled = enabled;
}
```

## Service-Specific Diagnostics

### Timer Service Monitoring

#### Timer Callback Performance
```csharp
private static void TickInternal(int index)
{
    ECSGameTimer timer = _list[index];
    
    if (ServiceUtils.ShouldTick(timer.NextTick))
    {
        long startTick = GameLoop.GetRealTime();
        timer.Tick();
        long stopTick = GameLoop.GetRealTime();
        
        // Game rule: Timer callbacks should complete quickly
        if (stopTick - startTick > Diagnostics.LongTickThreshold)
        {
            log.Warn($"Long {SERVICE_NAME} tick for Timer Callback: " +
                    $"{timer.CallbackInfo?.DeclaringType}:{timer.CallbackInfo?.Name} " +
                    $"Owner: {timer.Owner?.Name} Time: {stopTick - startTick}ms");
        }
    }
}
```

### Client Service Monitoring

#### Network Performance Tracking
```csharp
private static void Receive(GameClient client)
{
    long startTick = GameLoop.GetRealTime();
    client.Receive();
    long stopTick = GameLoop.GetRealTime();
    
    // Game rule: Network operations should be fast
    if (stopTick - startTick > Diagnostics.LongTickThreshold)
    {
        log.Warn($"Long {SERVICE_NAME} Receive for: " +
                $"{client.Player?.Name}({client.SessionId}) " +
                $"Time: {stopTick - startTick}ms");
    }
}
```

### NPC Service Monitoring

#### AI Brain Performance
```csharp
private static void TickInternal(int index)
{
    ABrain brain = _list[index];
    GameNPC npc = brain.Body;
    
    if (ServiceUtils.ShouldTick(brain.NextThinkTick))
    {
        long startTick = GameLoop.GetRealTime();
        brain.Think();
        long stopTick = GameLoop.GetRealTime();
        
        // Game rule: AI processing should be efficient
        if (stopTick - startTick > Diagnostics.LongTickThreshold)
        {
            log.Warn($"Long {SERVICE_NAME} tick for: " +
                    $"{npc.Name}({npc.ObjectID}) " +
                    $"Brain: {brain.GetType().Name} " +
                    $"Time: {stopTick - startTick}ms");
        }
    }
}
```

## Best Practices

### Diagnostic Usage Guidelines

#### When to Enable Diagnostics
- **Performance counters**: Only during performance testing or issue investigation
- **Entity counting**: When diagnosing memory or capacity issues
- **Event profiling**: When investigating event system bottlenecks
- **Long tick warnings**: Always enabled in production for issue detection

#### Performance Impact Considerations
- **Minimal overhead**: Diagnostics designed for minimal impact when disabled
- **Thread-safe**: All diagnostic operations are thread-safe
- **Configurable**: All thresholds and options configurable via server properties
- **Targeted**: Enable only specific diagnostics needed for investigation

#### Interpreting Results
- **Long ticks**: Identify performance bottlenecks in specific components
- **Entity counts**: Monitor memory usage and system load
- **Performance counters**: Track service execution times over time
- **Event timing**: Identify expensive event handlers

## System Interactions

### Diagnostic Data Flow
```
Client Command (/diag) → ECSDiagnosticsCommandHandler → Diagnostics System
Service Processing → Performance Counters → Log Files
Entity Processing → Count Tracking → Debug Output
Event Processing → Timing Collection → Performance Reports
```

### Integration Points
- **GameLoop**: Central coordination of diagnostic timing
- **Services**: Automatic performance monitoring integration
- **Commands**: Administrative control interface
- **Logging**: Output to files and console
- **Configuration**: Server properties integration

---

**References:**
- GameServer/ECS-Debug/ECSDebug.cs
- All ECS service implementations with diagnostic integration
- /diag command implementation
- Performance logging and monitoring systems 