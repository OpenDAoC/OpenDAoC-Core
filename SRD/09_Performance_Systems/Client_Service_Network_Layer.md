# Client Service and Network Layer

## Document Status
- **Last Updated**: 2024-01-20
- **Verification**: Code-verified from ClientService.cs, GameClient.cs, BaseClient.cs, PacketProcessor.cs
- **Implementation Status**: ✅ Fully Implemented

## Overview

**Game Rule Summary**: The client service and network layer handles all communication between your game client and the server, ensuring your actions are transmitted reliably and your character responds quickly to commands. It manages connections, prevents cheating, and maintains smooth gameplay even during peak server activity.

The Client Service and Network Layer provides comprehensive client connection management, packet processing, and session handling. It supports TCP/UDP dual protocol communication, spam protection, packet validation, and efficient client lifecycle management through ECS architecture.

## Core Architecture

### Client Service ECS Integration
```csharp
public static class ClientService
{
    private static List<GameClient> _clients = new();
    private static GameClient[] _clientsBySessionId = new GameClient[ushort.MaxValue];
    private static Trie<GamePlayer> _playerNameTrie = new();
    private static SimpleDisposableLock _lock = new(LockRecursionPolicy.SupportsRecursion);
    
    public static void BeginTick()
    {
        using (_lock)
        {
            _clients = ServiceObjectStore.UpdateAndGetAll<GameClient>(ServiceObjectType.Client, out _lastValidIndex);
        }
        
        GameLoop.ExecuteWork(_lastValidIndex + 1, BeginTickInternal);
    }
}
```

### GameClient State Machine
```csharp
public enum eClientState : byte
{
    NotConnected = 0,  // Initial state
    Connecting,        // Version check in progress
    CharScreen,        // Character selection
    WorldEnter,        // Entering world
    Playing,           // In game
    Linkdead,          // Disconnected with LD timer
    Disconnected       // Final state
}
```

### Client Processing Pipeline
```csharp
private static void BeginTickInternal(int index)
{
    GameClient client = _clients[index];
    
    switch (client.ClientState)
    {
        case eClientState.NotConnected:
        case eClientState.Connecting:
        case eClientState.CharScreen:
        case eClientState.WorldEnter:
            Receive(client);
            CheckPingTimeout(client);
            break;
            
        case eClientState.Playing:
            Receive(client);
            CheckPingTimeout(client);
            
            if (player != null)
            {
                CheckInGameActivityTimeout(client);
                if (ServiceUtils.ShouldTick(player.NextWorldUpdate))
                {
                    UpdateWorld(player);
                    player.NextWorldUpdate = GameLoop.GameLoopTime + Properties.WORLD_PLAYER_UPDATE_INTERVAL;
                }
            }
            break;
    }
}
```

## Network Layer Components

### BaseClient Foundation
```csharp
public class BaseClient
{
    public const int TCP_SEND_BUFFER_SIZE = 8192;
    public const int UDP_SEND_BUFFER_SIZE = 1024;
    private const int TCP_RECEIVE_BUFFER_SIZE = 1024;
    
    private SocketAsyncEventArgs _receiveArgs = new();
    private bool _isReceivingAsync;
    private long _isReceivingAsyncCompleted;
    
    public Socket Socket { get; }
    public byte[] ReceiveBuffer { get; }
    public int ReceiveBufferOffset { get; set; }
    public SessionId SessionId { get; private set; }
}
```

### Socket Management
```csharp
public void Receive()
{
    if (Socket?.Connected != true)
    {
        Disconnect();
        return;
    }
    
    // Handle async operations
    if (_isReceivingAsync && !ReceivingAsyncCompleted)
        return;
        
    int available = ReceiveBuffer.Length - ReceiveBufferOffset;
    if (available <= 0)
    {
        // Buffer overflow - disconnect client
        Disconnect();
        return;
    }
    
    try
    {
        _receiveArgs.SetBuffer(ReceiveBufferOffset, available);
        
        if (Socket.ReceiveAsync(_receiveArgs))
        {
            ReceivingAsyncCompleted = false;
            _isReceivingAsync = true;
        }
        else
            OnReceiveCompletion();
    }
    catch (SocketException e)
    {
        Disconnect();
    }
}
```

### Session Management
```csharp
public class SessionId : IDisposable
{
    private SessionIdAllocator _allocator;
    private ushort _value;
    private bool _disposed;
    
    public ushort Value => _value;
    
    public SessionId(SessionIdAllocator allocator)
    {
        _allocator = allocator;
        _value = allocator.Allocate();
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _allocator.Release(_value);
            _disposed = true;
        }
    }
}
```

### Session ID Allocation
```csharp
public class SessionIdAllocator
{
    private readonly Queue<ushort> _availableIds = new();
    private ushort _nextId = 1;
    private readonly Lock _lock = new();
    
    public ushort Allocate()
    {
        using (_lock)
        {
            if (_availableIds.Count > 0)
                return _availableIds.Dequeue();
                
            if (_nextId == ushort.MaxValue)
                throw new InvalidOperationException("No more session IDs available");
                
            return _nextId++;
        }
    }
    
    public void Release(ushort id)
    {
        using (_lock)
        {
            _availableIds.Enqueue(id);
        }
    }
}
```

## Packet Processing System

### Packet Processing Pipeline
```csharp
public class PacketProcessor
{
    private IPacketHandler[] _packetHandlers = new IPacketHandler[256];
    private PacketPreprocessing _packetPreprocessor = new();
    private Queue<IPacket> _savedPackets = new(SAVED_PACKETS_COUNT);
    
    public void ProcessInboundPacket(GSPacketIn packet)
    {
        int code = packet.Code;
        SavePacket(packet);
        
        // Validate packet code
        if (code >= _packetHandlers.Length)
        {
            log.Error($"Received packet code outside bounds: 0x{code:X2}");
            return;
        }
        
        IPacketHandler packetHandler = _packetHandlers[code];
        if (packetHandler == null)
            return;
            
        // Security preprocessing
        if (!_packetPreprocessor.CanProcessPacket(_client, packet))
        {
            log.Info($"Preprocessor blocked packet ID={packet.Code}");
            return;
        }
        
        // Execute handler
        packetHandler.HandlePacket(_client, packet);
    }
}
```

### Packet Queue Management
```csharp
// TCP packet queuing
private DrainArray<GSTCPPacketOut> _tcpPacketQueue = new();
private DrainArray<GSUDPPacketOut> _udpToTcpPacketQueue = new();

// UDP packet queuing  
private DrainArray<GSUDPPacketOut> _udpPacketQueue = new();

public void QueuePacket(GSTCPPacketOut packet)
{
    if (_client.ClientState is eClientState.Disconnected or eClientState.Linkdead)
        return;
        
    if (!packet.IsSizeSet)
        packet.WritePacketLength();
        
    _tcpPacketQueue.Add(packet);
}

public void SendPendingPackets()
{
    // Process TCP packets
    _tcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendTcpPacketToTcpSendBuffer(packet), this);
    
    // Process UDP-to-TCP fallback
    _udpToTcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToTcpSendBuffer(packet), this);
    
    SendTcp();
    
    // Process UDP packets
    _udpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToUdpSendBuffer(packet), this);
    
    SendUdp();
}
```

### Packet Validation
```csharp
private void AppendTcpPacketToTcpSendBuffer(GSTCPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int) packet.Length;
    
    if (!ValidatePacketSize(packetBuffer, packetSize))
        return;
        
    // Buffer management
    int nextPosition = _tcpSendBufferPosition + packetSize;
    
    if (nextPosition > _tcpSendArgs.Buffer.Length)
    {
        if (!SendTcp())
            return;
            
        // Check if packet still fits after buffer flush
        if (_tcpSendBufferPosition + packetSize > _tcpSendArgs.Buffer.Length)
            return; // Discard oversized packet
    }
    
    Buffer.BlockCopy(packetBuffer, 0, _tcpSendArgs.Buffer, _tcpSendBufferPosition, packetSize);
    _tcpSendBufferPosition = nextPosition;
    SavePacket(packet);
}
```

## Connection Management

### Client Connection Process
```csharp
public override void OnConnect(SessionId sessionId)
{
    base.OnConnect(sessionId);
    
    // Post to game loop for thread safety
    GameLoopService.PostBeforeTick(static state =>
    {
        ClientService.OnClientConnect(state);
        GameEventMgr.Notify(GameClientEvent.Connected, state);
    }, this);
}

public static void OnClientConnect(GameClient client)
{
    GameClient existing = _clientsBySessionId[client.SessionId.Value];
    
    if (existing != null)
    {
        log.Warn($"Duplicate session ID {client.SessionId.Value} detected");
    }
    
    // Register client for fast lookup
    _clientsBySessionId[client.SessionId.Value] = client;
    
    // Initialize client state
    client.ClientState = eClientState.Connecting;
    
    // Add to service processing
    ServiceObjectStore.AddOrUpdate(client);
}
```

### Version Detection and Validation
```csharp
private bool CheckVersion()
{
    // Client version is sent in first packet
    if (ReceiveBufferOffset < 17)
        return false;
        
    int version;
    
    if (ReceiveBuffer[12] == 0)
    {
        // Pre-1.115c clients
        version = ReceiveBuffer[10] * 100 + ReceiveBuffer[11];
    }
    else
    {
        // Post-1.115c clients
        version = ReceiveBuffer[11] * 1000 + ReceiveBuffer[12] * 100 + ReceiveBuffer[13];
    }
    
    IPacketLib packetLib = AbstractPacketLib.CreatePacketLibForVersion(version, this, out eClientVersion ver);
    
    if (packetLib == null)
    {
        Version = eClientVersion.VersionUnknown;
        log.Warn($"Unsupported client version {version} from {TcpEndpointAddress}");
        Disconnect();
        return false;
    }
    
    Version = ver;
    Out = packetLib;
    PacketProcessor = new PacketProcessor(this);
    
    log.Info($"Client {TcpEndpointAddress} using version {version}");
    return true;
}
```

### Disconnection Handling
```csharp
protected override void OnDisconnect()
{
    if (ClientState is eClientState.Disconnected)
        return;
        
    GameLoopService.PostBeforeTick(static state =>
    {
        lock (state._disconnectLock)
        {
            if (state.ClientState is eClientState.Disconnected)
                return;
                
            if (state.Player == null)
            {
                state.Quit();
                return;
            }
            
            if (state.ClientState is eClientState.Playing)
            {
                if (!state.Player.IsLinkDeathTimerRunning)
                    state.OnLinkDeath(false);
            }
            else if (state.ClientState is eClientState.WorldEnter)
            {
                state.Player.SaveIntoDatabase();
            }
            
            if (!state.Player.IsLinkDeathTimerRunning)
                state.Quit();
        }
    }, this);
}
```

## Timeout Management

### Ping Timeout Detection
```csharp
private static void CheckPingTimeout(GameClient client)
{
    if (client.Socket?.Connected != true)
        return;
        
    int pingTimeout = client.ClientState == eClientState.Playing ? 
        Properties.CP_Interval * 4 : Properties.UPDATETICK_CAP * 4;
        
    if (GameLoop.GameLoopTime - client.PingTime > pingTimeout)
    {
        log.Info($"Ping timeout for client {client.Account?.Name}({client.SessionID})");
        client.Disconnect();
    }
}
```

### Activity Timeout Detection
```csharp
private static void CheckInGameActivityTimeout(GameClient client)
{
    GamePlayer player = client.Player;
    
    if (player?.InCombat == false && 
        GameLoop.GameLoopTime - player.LastPlayerActivityTime > Properties.INACTIVITY_TIMEOUT)
    {
        player.Out.SendMessage("You have been inactive and will be disconnected in 10 seconds!", 
            eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            
        player.TempProperties.SetProperty("INACTIVITY_WARNING_SENT", true);
        
        // Disconnect after warning period
        GameLoopService.PostAfterTickDelay(() =>
        {
            if (player.TempProperties.GetProperty("INACTIVITY_WARNING_SENT", false))
            {
                client.Disconnect();
            }
        }, 10000);
    }
}
```

## UDP Protocol Support

### UDP Initialization
```csharp
[PacketHandler(PacketHandlerType.UDP, eClientPackets.UDPInitRequest)]
public class UDPInitRequestHandler : IPacketHandler
{
    public void HandlePacket(GameClient client, GSPacketIn packet)
    {
        string localIP = packet.ReadString(client.Version >= eClientVersion.Version1124 ? 20 : 22);
        ushort localPort = packet.ReadShort();
        
        client.LocalIP = localIP;
        // UDP endpoint will be set when first UDP packet received
        client.Out.SendUDPInitReply();
    }
}
```

### UDP Packet Processing
```csharp
public static void ProcessUdpPacket(byte[] buffer, int offset, int size, EndPoint endPoint)
{
    // Validate checksum
    int endPosition = offset + size;
    int packetCheck = (buffer[endPosition - 2] << 8) | buffer[endPosition - 1];
    int calculatedCheck = PacketProcessor.CalculateChecksum(buffer, offset, size - 2);
    
    if (packetCheck != calculatedCheck)
    {
        log.Warn($"Bad UDP packet checksum (packet:0x{packetCheck:X4} calculated:0x{calculatedCheck:X4})");
        return;
    }
    
    // Post to game loop for processing
    GameLoopService.PostBeforeTick(static state =>
    {
        GSPacketIn packet = GSPacketIn.GetForTick(p => p.Init());
        packet.Load(state.Buffer, state.Offset, state.Size);
        
        GameClient client = ClientService.GetClientBySessionId(packet.SessionID);
        if (client == null)
        {
            log.Warn($"UDP packet from invalid client ID {packet.SessionID} from {state.EndPoint}");
            return;
        }
        
        // Set UDP endpoint on first packet
        if (client.UdpEndPoint == null)
        {
            client.UdpEndPoint = state.EndPoint as IPEndPoint;
            client.UdpConfirm = false;
        }
        
        // Process packet if from correct endpoint
        if (client.UdpEndPoint.Equals(state.EndPoint))
        {
            client.PacketProcessor.ProcessInboundPacket(packet);
            client.UdpPingTime = GameLoop.GameLoopTime;
            client.UdpConfirm = true;
        }
    }, new { Buffer = buffer, Offset = offset, Size = size, EndPoint = endPoint });
}
```

### UDP Fallback to TCP
```csharp
public void QueuePacket(GSUDPPacketOut packet, bool forced)
{
    if (_client.ClientState is eClientState.Disconnected or eClientState.Linkdead)
        return;
        
    // Check UDP availability
    if (ServiceUtils.ShouldTick(_client.UdpPingTime + 70000))
        _client.UdpConfirm = false;
        
    if (!packet.IsSizeSet)
        packet.WritePacketLength();
        
    // Use UDP if available and confirmed, otherwise fallback to TCP
    if (_udpSendArgs.RemoteEndPoint != null && (forced || _client.UdpConfirm))
        _udpPacketQueue.Add(packet);
    else
        _udpToTcpPacketQueue.Add(packet); // Send via TCP instead
}
```

## Security Features

### Packet Preprocessing
```csharp
public class PacketPreprocessing
{
    private Dictionary<int, long> _packetCounts = new();
    private Dictionary<int, long> _lastPacketTimes = new();
    
    public bool CanProcessPacket(GameClient client, GSPacketIn packet)
    {
        // Rate limiting
        if (!CheckPacketRate(packet.Code))
            return false;
            
        // State validation
        if (!ValidateClientState(client, packet))
            return false;
            
        // Privilege checking
        if (!CheckPrivileges(client, packet))
            return false;
            
        return true;
    }
    
    private bool CheckPacketRate(int packetCode)
    {
        long currentTime = GameLoop.GameLoopTime;
        
        if (_lastPacketTimes.TryGetValue(packetCode, out long lastTime))
        {
            if (currentTime - lastTime < PACKET_MIN_INTERVAL)
                return false; // Rate limited
        }
        
        _lastPacketTimes[packetCode] = currentTime;
        return true;
    }
}
```

### Checksum Validation
```csharp
public static int CalculateChecksum(byte[] data, int start, int count)
{
    int sum = 0;
    int end = start + count;
    
    for (int i = start; i < end; i += 2)
    {
        if (i + 1 < end)
            sum += (data[i] << 8) | data[i + 1];
        else
            sum += data[i] << 8;
    }
    
    while ((sum >> 16) != 0)
        sum = (sum & 0xFFFF) + (sum >> 16);
        
    return (~sum) & 0xFFFF;
}
```

### Buffer Overflow Protection
```csharp
protected override void OnReceive(int size)
{
    byte[] buffer = ReceiveBuffer;
    int endPosition = ReceiveBufferOffset + size;
    
    // Minimum packet size check
    if (endPosition < GSPacketIn.HDR_SIZE)
    {
        ReceiveBufferOffset = endPosition;
        return;
    }
    
    ReceiveBufferOffset = 0;
    int currentOffset = 0;
    
    do
    {
        int packetLength = (buffer[currentOffset] << 8) + buffer[currentOffset + 1] + GSPacketIn.HDR_SIZE;
        int dataLeft = endPosition - currentOffset;
        
        // Prevent buffer overflow
        if (dataLeft < packetLength)
        {
            Buffer.BlockCopy(buffer, currentOffset, buffer, 0, dataLeft);
            ReceiveBufferOffset = dataLeft;
            break;
        }
        
        // Process complete packet
        ProcessPacket(buffer, currentOffset, packetLength);
        currentOffset += packetLength;
        
    } while (endPosition - 1 > currentOffset);
}
```

## Performance Optimization

### Connection Pooling
```csharp
public class SocketAsyncEventArgsPool
{
    private readonly ConcurrentQueue<SocketAsyncEventArgs> _pool = new();
    private readonly int _maxPoolSize;
    
    public SocketAsyncEventArgs Rent()
    {
        if (_pool.TryDequeue(out SocketAsyncEventArgs args))
            return args;
            
        return new SocketAsyncEventArgs();
    }
    
    public void Return(SocketAsyncEventArgs args)
    {
        if (_pool.Count < _maxPoolSize)
        {
            args.SetBuffer(null, 0, 0);
            args.RemoteEndPoint = null;
            _pool.Enqueue(args);
        }
        else
        {
            args.Dispose();
        }
    }
}
```

### Packet Object Pooling
```csharp
public static class PacketPool<T> where T : class, IPooledObject<T>, new()
{
    private static readonly ConcurrentQueue<T> _pool = new();
    
    public static T GetForTick(Action<T> initializer = null)
    {
        if (_pool.TryDequeue(out T packet))
        {
            initializer?.Invoke(packet);
            return packet;
        }
        
        packet = new T();
        initializer?.Invoke(packet);
        return packet;
    }
    
    public static void ReturnToPool(T packet)
    {
        packet.Reset();
        _pool.Enqueue(packet);
    }
}
```

### DrainArray for Lock-Free Queuing
```csharp
public class DrainArray<T>
{
    private T[] _items;
    private int _count;
    
    public void Add(T item)
    {
        if (_count >= _items.Length)
            Array.Resize(ref _items, _items.Length * 2);
            
        _items[_count++] = item;
    }
    
    public void DrainTo<TState>(Action<T, TState> action, TState state)
    {
        for (int i = 0; i < _count; i++)
        {
            action(_items[i], state);
            _items[i] = default; // Allow GC
        }
        _count = 0;
    }
}
```

## Client Lookup Services

### Fast Session ID Lookup
```csharp
public static GameClient GetClientBySessionId(ushort sessionId)
{
    return _clientsBySessionId[sessionId];
}

public static GameClient GetClientBySessionId(int sessionId)
{
    if (sessionId < 0 || sessionId >= _clientsBySessionId.Length)
        return null;
        
    return _clientsBySessionId[sessionId];
}
```

### Player Name Lookup
```csharp
public static GamePlayer GetPlayerByExactName(string name)
{
    return _playerNameTrie.GetExact(name);
}

public static GamePlayer GetPlayerByPartialName(string name, out string propername)
{
    return _playerNameTrie.GetPartial(name, out propername);
}

public static void RegisterPlayerName(GamePlayer player)
{
    _playerNameTrie.Add(player.Name, player);
}

public static void UnregisterPlayerName(GamePlayer player)
{
    _playerNameTrie.Remove(player.Name);
}
```

### Account Management Integration
```csharp
public DbAccount Account
{
    get => _account;
    set
    {
        _account = value;
        
        // Load custom parameters
        this.InitFromCollection(value.CustomParams, 
            param => param.KeyName, 
            param => param.Value);
            
        // Notify systems
        GameEventMgr.Notify(GameClientEvent.AccountLoaded, this);
    }
}
```

## Error Handling and Logging

### Connection Error Handling
```csharp
public void Receive()
{
    try
    {
        // ... receive logic ...
    }
    catch (ObjectDisposedException)
    {
        // Socket already disposed, ignore
    }
    catch (SocketException e)
    {
        if (log.IsDebugEnabled)
            log.Debug($"Socket exception on receive (Code: {e.SocketErrorCode})");
        Disconnect();
    }
    catch (Exception e)
    {
        if (log.IsErrorEnabled)
            log.Error($"Unhandled exception on receive: {e}");
        Disconnect();
    }
}
```

### Packet Processing Error Handling
```csharp
try
{
    long startTick = GameLoop.GetRealTime();
    packetHandler.HandlePacket(_client, packet);
    long stopTick = GameLoop.GetRealTime();
    
    // Performance monitoring
    if (stopTick - startTick > Diagnostics.LongTickThreshold)
    {
        log.Warn($"Long packet processing (code: 0x{packet.Code:X}) " +
                $"for {_client.Player?.Name} Time: {stopTick - startTick}ms");
    }
}
catch (Exception e)
{
    if (log.IsErrorEnabled)
    {
        log.Error($"Error processing packet " +
                 $"(handler={packetHandler.GetType().FullName}; " +
                 $"client={_client})", e);
    }
}
```

### Debug Packet Saving
```csharp
private void SavePacket(IPacket packet)
{
    if (!Properties.SAVE_PACKETS)
        return;
        
    lock (_savedPacketsLock)
    {
        if (_savedPackets.Count >= SAVED_PACKETS_COUNT)
            _savedPackets.Dequeue();
            
        _savedPackets.Enqueue(packet);
    }
}

public IPacket[] GetLastPackets()
{
    lock (_savedPacketsLock)
    {
        return _savedPackets.ToArray();
    }
}
```

## Configuration

### Network Settings
```ini
# Server Properties
WORLD_PLAYER_UPDATE_INTERVAL = 1000  # Player world update frequency (ms)
CP_INTERVAL = 30000                  # Client ping interval (ms)  
UPDATETICK_CAP = 100                 # Maximum update tick (ms)
INACTIVITY_TIMEOUT = 300000          # Player inactivity timeout (ms)

# Buffer sizes
TCP_SEND_BUFFER_SIZE = 8192          # TCP send buffer size
UDP_SEND_BUFFER_SIZE = 1024          # UDP send buffer size
TCP_RECEIVE_BUFFER_SIZE = 1024       # TCP receive buffer size

# Security
SAVE_PACKETS = false                 # Save packets for debugging
PACKET_MIN_INTERVAL = 10             # Minimum interval between packets (ms)
```

### Performance Tuning
```ini
# Connection limits
MAX_CLIENTS = 500                    # Maximum concurrent clients
SESSION_TIMEOUT = 150000             # Session timeout (ms)

# Pool sizes
PACKET_POOL_SIZE = 1000             # Packet object pool size
SOCKET_ARGS_POOL_SIZE = 100         # SocketAsyncEventArgs pool size

# Threading
CLIENT_SERVICE_THREADS = 4           # Client service worker threads
PACKET_PROCESSING_THREADS = 2        # Packet processing threads
```

## Integration Points

### Event System Integration
Client events are published through GameEventMgr:

```csharp
// Connection events
GameEventMgr.Notify(GameClientEvent.Connected, client);
GameEventMgr.Notify(GameClientEvent.Disconnected, client);
GameEventMgr.Notify(GameClientEvent.AccountLoaded, client);

// State events  
GameEventMgr.Notify(GameClientEvent.StateChanged, client, new StateChangedEventArgs(oldState, newState));
```

### ECS Service Integration
ClientService integrates with the ECS architecture:

```csharp
// Service registration
ServiceObjectStore.AddOrUpdate(client);

// Service processing
_clients = ServiceObjectStore.UpdateAndGetAll<GameClient>(ServiceObjectType.Client, out _lastValidIndex);
```

### Database Integration
Account and player data persistence:

```csharp
// Account tracking
Account.LastConnected = DateTime.Now;
Account.LastDisconnected = DateTime.Now;
GameServer.Database.SaveObject(Account);

// Audit logging
AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.AccountLogin, "", Account.Name);
```

## Test Scenarios

### Connection Lifecycle
```csharp
// Given: New client connects
// When: Socket connection established
// Then: Client enters Connecting state
// And: Version check begins
// And: Session ID allocated
```

### Version Validation
```csharp
// Given: Client sends version packet
// When: Version is supported
// Then: PacketProcessor created
// And: Client enters CharScreen state
// When: Version is unsupported  
// Then: Client disconnected
```

### Packet Processing
```csharp
// Given: Client in Playing state
// When: Valid packet received
// Then: Packet handler executed
// And: Response sent if needed
// When: Invalid packet received
// Then: Packet ignored/client disconnected
```

### Link Death Recovery
```csharp
// Given: Client in Playing state
// When: Connection lost
// Then: Client enters Linkdead state
// And: Link death timer started
// When: Client reconnects within timer
// Then: Client resumes Playing state
// When: Timer expires
// Then: Character saved and logged out
```

### UDP Communication
```csharp
// Given: Client with established TCP connection
// When: UDP init packet received
// Then: UDP endpoint configured
// And: UDP confirmation enabled
// When: UDP unavailable
// Then: UDP packets sent via TCP fallback
```

## Future Enhancements
- TODO: WebSocket support for web clients
- TODO: HTTP/3 QUIC protocol support
- TODO: Advanced DDoS protection
- TODO: Client bandwidth monitoring and throttling
- TODO: Packet compression for mobile clients
- TODO: IPv6 support
- TODO: Client connection analytics and metrics

## Change Log
- 2024-01-20: Initial documentation created

## References
- `GameServer/ECS-Services/ClientService.cs`
- `GameServer/GameClient.cs`
- `CoreBase/Network/BaseClient.cs`
- `CoreBase/Network/BaseServer.cs`
- `GameServer/packets/Server/PacketProcessor.cs`
- `GameServer/packets/Client/GSPacketIn.cs`
- `GameServer/packets/Client/168/` - Client packet handlers
- `GameServer/GameServer.cs` - UDP processing 