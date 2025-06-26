# Server Performance System

## Document Status
- Status: Complete
- Implementation: Complete

## Overview
The server performance system manages game loop timing, multi-threading, memory optimization, packet processing, and performance monitoring. It uses an Entity Component System (ECS) architecture with service-based processing and advanced optimization techniques to handle hundreds of concurrent players.

## Core Architecture

### Game Loop System

#### Main Game Loop
```csharp
public static class GameLoop
{
    public const string THREAD_NAME = "GameLoop";
    public static long TickRate { get; private set; }  // 10ms default
    public static long GameLoopTime { get; private set; }
    public static string CurrentServiceTick { get; set; }
}
```

#### Tick Rate Management
**Default Configuration**:
```csharp
TickRate = Properties.GAME_LOOP_TICK_RATE; // Default: 10ms (100 TPS)
```

**Service Execution Order**:
```csharp
// Each tick processes services in specific order
ClientService.BeginTick();          // Handle client updates
ServiceObjectStore.Update();        // Update object store
AttackService.Tick();               // Process attacks
EffectListService.Tick();           // Process effects
CastingService.Tick();              // Process spell casting
MovementService.Tick();             // Process movement
ZoneService.Tick();                 // Process zone updates
CraftingService.Tick();             // Process crafting
ReaperService.Tick();               // Process death cleanup
ClientService.EndTick();            // Finalize client updates
```

#### Precision Timing
**Busy-Wait Optimization**:
```csharp
private static int _busyWaitThreshold;  // Dynamic calculation
private const bool DYNAMIC_BUSY_WAIT_THRESHOLD = true;

void Sleep()
{
    int sleepFor = (int)(TickRate - stopwatch.Elapsed.TotalMilliseconds);
    int busyWaitThreshold = _busyWaitThreshold;
    
    if (sleepFor >= busyWaitThreshold)
        Thread.Sleep(sleepFor - busyWaitThreshold);
    else
        Thread.Yield();
        
    // Precise busy-wait for final timing
    if (TickRate > stopwatch.Elapsed.TotalMilliseconds)
    {
        SpinWait spinWait = new();
        while (TickRate > stopwatch.Elapsed.TotalMilliseconds)
            spinWait.SpinOnce(-1);
    }
}
```

### Multi-Threading Architecture

#### Thread Pool System
```csharp
public class GameLoopThreadPool
{
    public void ExecuteWork(int count, Action<int> action)
    {
        // Parallel execution across worker threads
        // Optimized for CPU core count
    }
}
```

#### Service Parallelization
**ECS Service Processing**:
```csharp
// All services use parallel processing pattern
GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);

private static void TickInternal(int index)
{
    // Process component at index
    // Thread-safe operations only
}
```

#### Thread Safety
- **Lock-Free Operations**: Most ECS operations avoid locks
- **Service Isolation**: Each service processes independently
- **Atomic Updates**: Critical sections use atomic operations

### Memory Management

#### Object Pooling System
```csharp
public interface IPooledObject<T>
{
    long IssuedTimestamp { get; set; }
}

public enum PooledObjectKey
{
    InPacket,
    TcpOutPacket,
    UdpOutPacket
}
```

**Tick-Local Pools**:
```csharp
private sealed class TickObjectPool<T> : ITickObjectPool
{
    private const int INITIAL_CAPACITY = 64;
    private const double TRIM_SAFETY_FACTOR = 2.5;
    private const int HALF_LIFE = 300_000; // 5 minutes
    
    private T[] _items = new T[INITIAL_CAPACITY];
    private double _smoothedUsage; // EMA for pool sizing
    
    public T GetForTick()
    {
        // Reuse pooled object or create new
        // Automatic pool resizing based on usage
    }
}
```

**Exponential Moving Average (EMA) Pool Sizing**:
```csharp
static TickObjectPool()
{
    DECAY_FACTOR = Math.Exp(-Math.Log(2) / (GameLoop.TickRate * HALF_LIFE / 1000.0));
}

public void Reset()
{
    _smoothedUsage = Math.Max(_used, _smoothedUsage * DECAY_FACTOR + _used * (1 - DECAY_FACTOR));
    int newLogicalSize = (int)(_smoothedUsage * TRIM_SAFETY_FACTOR);
    
    // Trim pool if significantly oversized
    if (_logicalSize > newLogicalSize)
    {
        for (int i = newLogicalSize; i < _logicalSize; i++)
            _items[i] = default;
        _logicalSize = newLogicalSize;
    }
}
```

#### Garbage Collection Optimization
- **Object Reuse**: Extensive pooling reduces allocations
- **Struct Usage**: Value types for performance-critical data
- **Array Reuse**: Pre-allocated arrays for collections
- **String Interning**: Cached strings for common values

### Packet Processing Optimization

#### Async Packet Handling
```csharp
public class PacketProcessor
{
    private DrainArray<GSTCPPacketOut> _tcpPacketQueue = new();
    private DrainArray<GSUDPPacketOut> _udpPacketQueue = new();
    private ConcurrentQueue<SocketAsyncEventArgs> _tcpSendArgsPool = [];
    private ConcurrentQueue<SocketAsyncEventArgs> _udpSendArgsPool = [];
}
```

**Packet Queue Management**:
- **DrainArray**: Lock-free arrays for packet queuing
- **Async Send**: Non-blocking network operations
- **Connection Pools**: Reused socket connections
- **Buffer Pooling**: Pre-allocated send buffers

#### Packet Caching
```csharp
private static Dictionary<string, IPacketHandler[]> _cachedPacketHandlerSearchResults = [];
private static Dictionary<string, List<PacketHandlerAttribute>> _cachedPreprocessorSearchResults = [];
```

**Performance Optimizations**:
- **Handler Caching**: Pre-compiled packet handlers
- **Reflection Avoidance**: Cached method lookups
- **Batch Processing**: Multiple packets per network call

### Performance Monitoring

#### Statistics Collection
```csharp
public class StatPrint
{
    private static IPerformanceStatistic _systemCpuUsagePercent;
    private static IPerformanceStatistic _programCpuUsagePercent;
    private static IPerformanceStatistic _pageFaultsPerSecond;
    private static IPerformanceStatistic _diskTransfersPerSecond;
}
```

#### Game Loop Statistics
```csharp
public class GameLoopStats
{
    private readonly double[] _buffer;          // Ring buffer for timing data
    private readonly uint _capacity;            // Power of 2 for efficient modulo
    private readonly List<int> _intervals;     // [60s, 30s, 10s] reporting intervals
    
    public void RecordTick(double gameLoopTime)
    {
        uint index = Interlocked.Increment(ref _writeIndex) - 1;
        _buffer[index & (_capacity - 1)] = gameLoopTime;
    }
    
    public List<(int, double)> GetAverageTicks(long currentTime)
    {
        // Calculate moving averages for different time windows
        // Thread-safe snapshot of ring buffer
    }
}
```

#### Performance Metrics
**Real-Time Monitoring**:
```csharp
StringBuilder stats = new StringBuilder(256)
    .Append($"Mem={GC.GetTotalMemory(false) / 1024 / 1024}MB")
    .Append($"  Clients={ClientService.ClientCount}")
    .AppendFormat($"  Pool={poolCurrent}/{poolMax}({poolMin})")
    .AppendFormat($"  IOCP={iocpCurrent}/{iocpMax}({iocpMin})")
    .AppendFormat($"  GH/OH={globalHandlers}/{objectHandlers}")
    .Append($"  TPS=");
```

**Key Performance Indicators**:
- **Memory Usage**: GC memory tracking
- **Client Count**: Active connections
- **Thread Pool**: Available/max/min threads
- **IOCP Threads**: I/O completion port usage
- **Event Handlers**: Global/object event counts
- **TPS**: Ticks per second over multiple intervals

### Service-Specific Optimizations

#### Client Service Optimization
```csharp
public static class ClientService
{
    private static GameClient[] _clientsBySessionId = new GameClient[ushort.MaxValue];
    private static Trie<GamePlayer> _playerNameTrie = new();
    
    public static void BeginTick()
    {
        using (_lock)
        {
            _clients = ServiceObjectStore.UpdateAndGetAll<GameClient>(
                ServiceObjectType.Client, out _lastValidIndex);
        }
        GameLoop.ExecuteWork(_lastValidIndex + 1, BeginTickInternal);
    }
}
```

**Optimizations**:
- **Fast Lookup**: Array-based client lookup by session ID
- **Trie Structure**: Efficient player name searches
- **Batch Updates**: Process all clients in parallel
- **Update Caching**: Cache expensive calculations

#### Movement Service Optimization
```csharp
public class PlayerMovementComponent : MovementComponent
{
    private const int BROADCAST_MINIMUM_INTERVAL = 200;
    private const int SOFT_LINK_DEATH_THRESHOLD = 5000;
    
    private long _nextPositionBroadcast;
    private bool _needBroadcastPosition;
}
```

**Movement Optimizations**:
- **Update Throttling**: Limit position broadcasts to 200ms
- **Link Death Detection**: 5-second timeout for unresponsive clients
- **Batch Broadcasting**: Group position updates
- **Distance Culling**: Only update nearby players

#### Effect Service Optimization
**Long Tick Detection**:
```csharp
private static void TickInternal(int index)
{
    long startTick = GameLoop.GetRealTime();
    effectListComponent.Tick();
    long stopTick = GameLoop.GetRealTime();
    
    if (stopTick - startTick > Diagnostics.LongTickThreshold)
        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for: {component.Owner.Name} Time: {stopTick - startTick}ms");
}
```

### Configuration System

#### Performance Properties
```csharp
// Server configuration for performance tuning
GAME_LOOP_TICK_RATE = 10;           // 10ms tick rate
STATPRINT_FREQUENCY = 30;           // Stats every 30 seconds
STATSAVE_INTERVAL = 600;            // Save stats every 10 minutes
WORLD_NPC_UPDATE_INTERVAL = 100;    // NPC updates every 100ms
```

#### Dynamic Thresholds
```csharp
public static class Diagnostics
{
    public static long LongTickThreshold = 50;  // Warn on >50ms ticks
    public static bool CheckEntityCounts = true; // Entity count validation
}
```

### Load Balancing

#### Update Interval Management
**Variable Update Rates**:
```csharp
// Different update frequencies for different systems
NPC_UPDATE_INTERVAL = 100ms;      // NPCs
STATIC_OBJECT_UPDATE = 4000ms;    // Static objects
PLAYER_UPDATE_INTERVAL = 200ms;   // Player positions
EFFECT_UPDATE_INTERVAL = 10ms;    // Spell effects
```

#### Priority Systems
- **Critical Systems**: 10ms updates (combat, effects)
- **Important Systems**: 100ms updates (NPCs, movement)
- **Background Systems**: 1000ms+ updates (statistics, cleanup)

## System Interactions

### With ECS Architecture
- **Component-Based**: All game objects use ECS pattern
- **Service Processing**: Systems process components in parallel
- **Entity Lifecycle**: Automatic cleanup and memory management

### With Network System
- **Packet Optimization**: Batched and pooled packet processing
- **Connection Management**: Efficient client handling
- **Bandwidth Optimization**: Smart update throttling

### With Database System
- **Batch Operations**: Group database writes
- **Connection Pooling**: Reuse database connections
- **Async Operations**: Non-blocking database access

## Implementation Notes

### Threading Model
```csharp
// Primary threads
GameLoop Thread          // Main game loop (10ms)
BusyWaitThreshold Thread // Sleep timing optimization
Worker Thread Pool       // Parallel processing (CPU core count)
Network I/O Threads      // Async network operations
Database Threads         // Database operations
```

### Memory Patterns
```csharp
// Memory-efficient patterns used throughout
Ring Buffers            // Fixed-size circular arrays
Object Pools           // Reuse expensive objects
Struct Usage           // Value types for small data
Array Reuse            // Pre-allocated collections
String Interning       // Cache common strings
```

### Performance Targets
- **Tick Rate**: 100 TPS (10ms per tick)
- **Memory Growth**: <1% per hour under normal load
- **CPU Usage**: <50% on target hardware
- **Network Latency**: <100ms for local connections
- **Database Response**: <10ms average query time

## Test Scenarios

### Load Testing
1. **Client Load**: Test with 1000+ concurrent clients
2. **Combat Stress**: Multiple large-scale battles
3. **Memory Pressure**: Extended runtime testing
4. **Network Saturation**: High packet volume scenarios

### Performance Validation
1. **Tick Rate Stability**: Maintain 100 TPS under load
2. **Memory Leak Detection**: Long-running stability tests
3. **Threading Safety**: Concurrent access validation
4. **Resource Usage**: CPU/Memory utilization limits

### Benchmark Scenarios
1. **Service Performance**: Individual service timing
2. **Network Throughput**: Packet processing rates
3. **Database Performance**: Query response times
4. **Memory Allocation**: GC pressure analysis

## Monitoring and Diagnostics

### Real-Time Metrics
- **TPS Monitoring**: Multiple time window averages
- **Memory Tracking**: GC allocation rates
- **Thread Usage**: Pool utilization
- **Client Statistics**: Connection counts and states

### Performance Logging
- **Long Tick Detection**: Warnings for slow operations
- **Entity Count Validation**: Memory leak detection
- **Service Timing**: Per-service performance tracking
- **Error Rate Monitoring**: Exception frequency

## Change Log
- Initial documentation based on GameLoop and service architecture
- Includes threading model and memory optimization strategies
- Documents performance monitoring and load balancing systems
- Covers ECS optimization patterns and async processing 