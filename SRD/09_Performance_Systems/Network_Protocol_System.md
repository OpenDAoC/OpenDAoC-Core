# Network Protocol System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

**Game Rule Summary**: The network protocol system handles all communication between your game client and the server, ensuring your actions are transmitted quickly and reliably. It manages data compression, security, and error correction to provide smooth, responsive gameplay with minimal lag, even during large battles or busy server periods.

The Network Protocol System provides high-performance, bidirectional client-server communication for OpenDAoC. It manages packet encoding/decoding, reliability, security, and protocol versioning through a sophisticated packet handler architecture with support for both TCP and UDP protocols.

## Core Architecture

### Packet Infrastructure

```csharp
// Base packet interfaces
public interface IPacket
{
    byte[] Buffer { get; }
    int Offset { get; }
    int Size { get; }
}

public abstract class PacketIn : MemoryStream, IPacket
{
    public abstract byte ReadByte();
    public abstract ushort ReadShort();
    public abstract uint ReadInt();
    public abstract string ReadString(int maxLength);
    public virtual void Skip(long bytes);
}

public abstract class PacketOut : MemoryStream, IPacket
{
    public abstract void WriteByte(byte value);
    public abstract void WriteShort(ushort value);
    public abstract void WriteInt(uint value);
    public abstract void WriteString(string value);
    public virtual void WritePacketLength();
}
```

### Protocol-Specific Implementations

```csharp
// UDP packet with object pooling
public class GSUDPPacketOut : PacketOut, IPooledObject<GSUDPPacketOut>
{
    private static readonly ObjectPool<GSUDPPacketOut> _pool = new();
    private byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public static GSUDPPacketOut GetFromPool()
    {
        var packet = _pool.Get();
        packet.Reset();
        return packet;
    }
    
    public void ReturnToPool()
    {
        Reset();
        _pool.Return(this);
    }
    
    public void Reset()
    {
        _position = 0;
        IsSizeSet = false;
        IssuedTimestamp = 0;
    }
}

// TCP packet for reliable delivery
public class GSTCPPacketOut : PacketOut, IPooledObject<GSTCPPacketOut>
{
    private readonly byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public override void WriteByte(byte value)
    {
        if (_position < _buffer.Length)
            _buffer[_position++] = value;
    }
    
    public override void WriteShort(ushort value)
    {
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value & 0xFF));
    }
}

// Incoming packet parser
public class GSPacketIn : PacketIn, IPooledObject<GSPacketIn>
{
    public const ushort HDR_SIZE = 12; // Header + checksum
    
    private ushort _code;      // Packet ID
    private ushort _parameter; // Packet parameter
    private ushort _psize;     // Packet size
    private ushort _sequence;  // Packet sequence
    private ushort _sessionID; // Session ID
    
    public ushort Code => _code;
    public ushort SessionID => _sessionID;
    public ushort PacketSize => (ushort)(_psize + HDR_SIZE);
    public ushort DataSize => _psize;
    
    public void Load(byte[] buffer, int offset, int count)
    {
        if (count < HDR_SIZE)
            throw new ArgumentException("Packet too small");
            
        // Parse header
        _psize = Marshal.ConvertToUInt16(buffer, offset);
        _sessionID = Marshal.ConvertToUInt16(buffer, offset + 2);
        _parameter = Marshal.ConvertToUInt16(buffer, offset + 4);
        _sequence = Marshal.ConvertToUInt16(buffer, offset + 6);
        _code = Marshal.ConvertToUInt16(buffer, offset + 8);
        
        // Load packet data
        SetLength(0);
        Write(buffer, offset + 10, count - HDR_SIZE);
        Seek(0, SeekOrigin.Begin);
    }
}
```

## Packet Processing Pipeline

### PacketProcessor Architecture

```csharp
public class PacketProcessor
{
    private const int SAVED_PACKETS_COUNT = 16;
    private readonly GameClient _client;
    private readonly IPacketHandler[] _packetHandlers = new IPacketHandler[256];
    private readonly PacketPreprocessing _packetPreprocessor = new();
    private readonly Queue<IPacket> _savedPackets = new(SAVED_PACKETS_COUNT);
    private readonly Lock _savedPacketsLock = new();
    
    // Packet queues for batching
    private readonly DrainArray<GSTCPPacketOut> _tcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpToTcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpPacketQueue = new();
    
    // Send buffer management
    private SocketAsyncEventArgs _tcpSendArgs;
    private SocketAsyncEventArgs _udpSendArgs;
    private int _tcpSendBufferPosition;
    private int _udpSendBufferPosition;
    private uint _udpCounter;
    
    public PacketProcessor(GameClient client)
    {
        _client = client;
        LoadPacketHandlers();
        GetAvailableTcpSendArgs();
        GetAvailableUdpSendArgs();
    }
}
```

### Packet Handler Registration

```csharp
private void LoadPacketHandlers()
{
    string version = "v168";
    
    lock (_loadPacketHandlersLock)
    {
        // Check cache first
        if (_cachedPacketHandlerSearchResults.TryGetValue(version, out var cachedHandlers))
        {
            _packetHandlers = cachedHandlers.Clone() as IPacketHandler[];
            return;
        }
        
        // Search assemblies for packet handlers
        _packetHandlers = new IPacketHandler[256];
        int count = SearchAndAddPacketHandlers(version, Assembly.GetAssembly(typeof(GameServer)), _packetHandlers);
        
        // Search script assemblies
        foreach (Assembly asm in ScriptMgr.Scripts)
            count += SearchAndAddPacketHandlers(version, asm, _packetHandlers);
            
        // Cache results
        _cachedPacketHandlerSearchResults[version] = _packetHandlers.Clone() as IPacketHandler[];
        
        log.Info($"Loaded {count} packet handlers for {version}");
    }
}

private int SearchAndAddPacketHandlers(string version, Assembly assembly, IPacketHandler[] packetHandlers)
{
    int count = 0;
    
    foreach (Type type in assembly.GetTypes())
    {
        if (!type.IsClass || type.IsAbstract || !typeof(IPacketHandler).IsAssignableFrom(type))
            continue;
            
        var packetHandlerAttributes = type.GetCustomAttributes(typeof(PacketHandlerAttribute), false);
        if (packetHandlerAttributes.Length == 0)
            continue;
            
        var attribute = (PacketHandlerAttribute)packetHandlerAttributes[0];
        
        // Version filtering logic here
        
        var handler = Activator.CreateInstance(type) as IPacketHandler;
        int packetCode = attribute.Code;
        
        if (packetCode >= 0 && packetCode < packetHandlers.Length)
        {
            packetHandlers[packetCode] = handler;
            count++;
        }
    }
    
    return count;
}
```

### Inbound Packet Processing

```csharp
public void ProcessInboundPacket(GSPacketIn packet)
{
    int code = packet.Code;
    SavePacket(packet);
    
    // Validate packet code
    if (code >= _packetHandlers.Length)
    {
        log.Error($"Packet code {code:X2} outside handler array bounds");
        LogInvalidPacket(packet);
        return;
    }
    
    IPacketHandler handler = _packetHandlers[code];
    if (handler == null)
        return;
        
    // Security preprocessing
    if (!_packetPreprocessor.CanProcessPacket(_client, packet))
    {
        log.Info($"Preprocessor blocked packet 0x{packet.Code:X2}");
        return;
    }
    
    try
    {
        long startTick = GameLoop.GetRealTime();
        handler.HandlePacket(_client, packet);
        long elapsed = GameLoop.GetRealTime() - startTick;
        
        // Performance monitoring
        if (elapsed > Diagnostics.LongTickThreshold)
        {
            log.Warn($"Long packet processing: 0x{packet.Code:X2} took {elapsed}ms for {_client.Player?.Name}");
        }
    }
    catch (Exception e)
    {
        log.Error($"Error processing packet 0x{packet.Code:X2} from {_client}: {e}");
    }
}

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
```

## Outbound Packet Management

### Packet Queuing System

```csharp
public void QueuePacket(GSTCPPacketOut packet)
{
    if (_client.ClientState is eClientState.Disconnected or eClientState.Linkdead)
        return;
        
    // Ensure packet size is set
    if (!packet.IsSizeSet)
        packet.WritePacketLength();
        
    _tcpPacketQueue.Add(packet);
}

public void QueuePacket(GSUDPPacketOut packet, bool forced = false)
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

### Batch Packet Sending

```csharp
public void SendPendingPackets()
{
    // Process TCP packets
    _tcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendTcpPacketToTcpSendBuffer(packet), this);
    
    // Process UDP-to-TCP fallback packets
    _udpToTcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToTcpSendBuffer(packet), this);
    
    SendTcp();
    
    // Process UDP packets
    _udpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToUdpSendBuffer(packet), this);
    
    SendUdp();
}

private void AppendTcpPacketToTcpSendBuffer(GSTCPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
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

### UDP Packet Handling

```csharp
private void AppendUdpPacketToUdpSendBuffer(GSUDPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
    if (!ValidatePacketSize(packetBuffer, packetSize))
        return;
        
    int nextPosition = _udpSendBufferPosition + packetSize;
    
    if (nextPosition > _udpSendArgs.Buffer.Length)
    {
        if (!SendUdp())
            return;
            
        if (_udpSendBufferPosition + packetSize > _udpSendArgs.Buffer.Length)
            return;
            
        nextPosition = packetSize;
    }
    
    // Copy packet and add UDP counter
    Buffer.BlockCopy(packetBuffer, 0, _udpSendArgs.Buffer, _udpSendBufferPosition, packetSize);
    _udpCounter++; // Let it overflow
    _udpSendArgs.Buffer[_udpSendBufferPosition + 2] = (byte)(_udpCounter >> 8);
    _udpSendArgs.Buffer[_udpSendBufferPosition + 3] = (byte)_udpCounter;
    _udpSendBufferPosition = nextPosition;
    SavePacket(packet);
}
```

## Security and Validation

### Packet Preprocessing

```csharp
public class PacketPreprocessing
{
    private readonly Dictionary<int, int> _packetIdToPreprocessMap = new();
    private readonly Dictionary<int, Func<GameClient, GSPacketIn, bool>> _preprocessors = new();
    
    public PacketPreprocessing()
    {
        RegisterPreprocessors((int)eClientStatus.LoggedIn, 
            (client, packet) => client.Account != null);
        RegisterPreprocessors((int)eClientStatus.PlayerInGame, 
            (client, packet) => client.Player != null);
    }
    
    public bool CanProcessPacket(GameClient client, GSPacketIn packet)
    {
        if (!_packetIdToPreprocessMap.TryGetValue(packet.Code, out int preprocessorId))
            return true; // No preprocessor = allow
            
        if (_preprocessors.TryGetValue(preprocessorId, out var preprocessor))
            return preprocessor(client, packet);
            
        return true;
    }
    
    private void RegisterPreprocessors(int preprocessorId, Func<GameClient, GSPacketIn, bool> func)
    {
        _preprocessors[preprocessorId] = func;
    }
}
```

### Packet Validation

```csharp
private static bool ValidatePacketIssuedTimestamp<T>(T packet) where T : PacketOut, IPooledObject<T>
{
    if (!packet.IsValidForTick())
    {
        if (packet.IssuedTimestamp != 0)
        {
            log.Error($"Packet not issued in current game loop time (Code: 0x{packet.Code:X2}) " +
                     $"(Issued: {packet.IssuedTimestamp}) (Current: {GameLoop.GameLoopTime})");
            return false;
        }
        
        log.Debug($"Packet issued outside game loop (Code: 0x{packet.Code:X2})");
    }
    
    return true;
}

private bool ValidatePacketSize(byte[] packetBuffer, int packetSize)
{
    if (packetSize <= 2048)
        return true;
        
    log.Error($"Discarding oversized packet. Code: 0x{packetBuffer[2]:X2}, " +
             $"Account: {_client.Account?.Name ?? _client.TcpEndpointAddress}, Size: {packetSize}");
             
    _client.Out.SendMessage($"Oversized packet detected (code: 0x{packetBuffer[2]:X2}) (size: {packetSize}). " +
                           "Please report this issue!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
    return false;
}
```

### Checksum Verification

```csharp
public static int CalculateChecksum(byte[] data, int start, int count)
{
    ushort checksum = 0;
    ushort val1 = 0;
    ushort val2 = 0;
    
    int dataPtr = start;
    int len = count - 2; // Exclude checksum bytes
    
    for (int i = 0; i < len; i += 2)
    {
        if (i + 1 < len)
        {
            val1 = (ushort)((data[dataPtr] << 8) | data[dataPtr + 1]);
        }
        else
        {
            val1 = (ushort)(data[dataPtr] << 8);
        }
        
        val2 = (ushort)(val2 + val1);
        if ((val2 & 0x80000000) != 0)
        {
            val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
        }
        
        dataPtr += 2;
    }
    
    val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
    checksum = (ushort)(~val2);
    
    return checksum;
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

## Performance Optimizations

### Object Pooling

```csharp
public static class PacketPool<T> where T : class, IPooledObject<T>, new()
{
    private static readonly ConcurrentQueue<T> _pool = new();
    private static readonly int MAX_POOL_SIZE = 1000;
    private static int _poolSize = 0;
    
    public static T Get()
    {
        if (_pool.TryDequeue(out var packet))
        {
            Interlocked.Decrement(ref _poolSize);
            return packet;
        }
        return new T();
    }
    
    public static void Return(T packet)
    {
        if (_poolSize < MAX_POOL_SIZE)
        {
            packet.Reset();
            _pool.Enqueue(packet);
            Interlocked.Increment(ref _poolSize);
        }
    }
}
```

### Asynchronous Socket Operations

```csharp
private void GetAvailableTcpSendArgs()
{
    if (!_tcpSendArgsPool.TryDequeue(out _tcpSendArgs))
    {
        _tcpSendArgs = new SocketAsyncEventArgs();
        _tcpSendArgs.SetBuffer(new byte[8192], 0, 8192);
        _tcpSendArgs.Completed += OnTcpSendCompleted;
    }
    
    _tcpSendBufferPosition = 0;
}

private void OnTcpSendCompleted(object sender, SocketAsyncEventArgs e)
{
    try
    {
        if (e.SocketError == SocketError.Success)
        {
            // Return to pool for reuse
            _tcpSendArgsPool.Enqueue(e);
        }
        else
        {
            log.Debug($"TCP send error: {e.SocketError}");
            _client.Disconnect();
        }
    }
    catch (Exception ex)
    {
        log.Error($"Error in TCP send completion: {ex}");
    }
}
```

## Protocol Versioning

### Version Detection

```csharp
private bool CheckVersion()
{
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

### Packet Encoding

```csharp
public interface IPacketEncoding
{
    void EncodePacket(PacketOut packet);
    void DecodePacket(PacketIn packet);
}

public class PacketEncoding168 : IPacketEncoding
{
    public void EncodePacket(PacketOut packet)
    {
        // XOR encoding with version-specific key
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
    
    public void DecodePacket(PacketIn packet)
    {
        // Reverse XOR encoding
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
}
```

## Error Handling and Recovery

### Connection Recovery

```csharp
private bool SendTcp()
{
    if (!_client.Socket.Connected)
        return false;
        
    try
    {
        if (_tcpSendBufferPosition > 0)
        {
            _tcpSendArgs.SetBuffer(0, _tcpSendBufferPosition);
            
            if (_client.SendAsync(_tcpSendArgs))
                GetAvailableTcpSendArgs();
                
            _tcpSendBufferPosition = 0;
        }
        
        return true;
    }
    catch (ObjectDisposedException) { }
    catch (SocketException e)
    {
        log.Debug($"Socket exception on TCP send (Client: {_client}) (Code: {e.SocketErrorCode})");
    }
    catch (Exception e)
    {
        log.Error($"Unhandled exception on TCP send (Client: {_client}): {e}");
    }
    
    return false;
}
```

### Packet Loss Handling

```csharp
public class PacketReliability
{
    private readonly Dictionary<ushort, PendingPacket> _pendingPackets = new();
    private readonly Timer _retransmissionTimer;
    
    public void SendReliablePacket(GSUDPPacketOut packet)
    {
        packet.Sequence = GetNextSequence();
        _pendingPackets[packet.Sequence] = new PendingPacket
        {
            Packet = packet,
            SendTime = GameLoop.GameLoopTime,
            RetryCount = 0
        };
        
        SendPacket(packet);
    }
    
    public void AcknowledgePacket(ushort sequence)
    {
        _pendingPackets.Remove(sequence);
    }
    
    private void CheckRetransmissions()
    {
        long currentTime = GameLoop.GameLoopTime;
        
        foreach (var kvp in _pendingPackets.ToArray())
        {
            var pending = kvp.Value;
            
            if (currentTime - pending.SendTime > RETRANSMISSION_TIMEOUT)
            {
                if (pending.RetryCount < MAX_RETRIES)
                {
                    pending.RetryCount++;
                    pending.SendTime = currentTime;
                    SendPacket(pending.Packet);
                }
                else
                {
                    // Give up and disconnect
                    _pendingPackets.Remove(kvp.Key);
                    HandlePacketLoss();
                }
            }
        }
    }
}
```

## Configuration

### Network Settings

```csharp
[ServerProperty("network", "tcp_send_buffer_size", "TCP send buffer size", 8192)]
public static int TCP_SEND_BUFFER_SIZE;

[ServerProperty("network", "udp_send_buffer_size", "UDP send buffer size", 4096)]
public static int UDP_SEND_BUFFER_SIZE;

[ServerProperty("network", "packet_pool_size", "Maximum packet pool size", 1000)]
public static int PACKET_POOL_SIZE;

[ServerProperty("network", "save_packets", "Save packets for debugging", false)]
public static bool SAVE_PACKETS;

[ServerProperty("network", "packet_timeout", "Packet processing timeout (ms)", 5000)]
public static int PACKET_TIMEOUT;
```

### Protocol Constants

```csharp
public static class ProtocolConstants
{
    public const int MAX_PACKET_SIZE = 2048;
    public const int MIN_PACKET_SIZE = 12;
    public const int UDP_PING_TIMEOUT = 70000; // 70 seconds
    public const int RETRANSMISSION_TIMEOUT = 1000; // 1 second
    public const int MAX_RETRIES = 3;
    public const int CHECKSUM_SIZE = 2;
}
```

## Integration Points

### Game Loop Integration

```csharp
public static void TickNetworking()
{
    // Process all client packet queues
    ClientService.ProcessAllClientPackets();
    
    // Handle UDP packet reception
    ProcessPendingUdpPackets();
    
    // Clean up disconnected clients
    CleanupDisconnectedClients();
}
```

### Event System Integration

```csharp
public class NetworkEvents
{
    public static readonly GameEventMgr.EventType PacketReceived = "PacketReceived";
    public static readonly GameEventMgr.EventType PacketSent = "PacketSent";
    public static readonly GameEventMgr.EventType ClientConnected = "ClientConnected";
    public static readonly GameEventMgr.EventType ClientDisconnected = "ClientDisconnected";
}

// Usage
GameEventMgr.AddHandler(NetworkEvents.PacketReceived, OnPacketReceived);
```

## Performance Metrics

### Target Performance

- **Packet Processing**: <1ms per packet
- **Memory Allocation**: <100 bytes per packet (pooled)
- **Throughput**: 1000+ packets/second per client
- **Latency**: <50ms round trip time
- **Reliability**: >99.9% packet delivery

### Monitoring

```csharp
public static class NetworkMetrics
{
    public static long PacketsReceived { get; private set; }
    public static long PacketsSent { get; private set; }
    public static long BytesReceived { get; private set; }
    public static long BytesSent { get; private set; }
    public static long PacketErrors { get; private set; }
    
    public static void LogMetrics()
    {
        log.Info($"Network Stats: RX={PacketsReceived} TX={PacketsSent} " +
                $"Bytes RX={BytesReceived} TX={BytesSent} Errors={PacketErrors}");
    }
}
```

## Implementation Status

**Completed**:
- ✅ Core packet infrastructure
- ✅ TCP/UDP dual protocol support
- ✅ Object pooling for performance
- ✅ Packet validation and security
- ✅ Asynchronous socket operations
- ✅ Protocol versioning
- ✅ Error handling and recovery

**In Progress**:
- 🔄 Advanced packet compression
- 🔄 Encryption layer
- 🔄 Quality of service monitoring

**Planned**:
- ⏳ IPv6 support
- ⏳ WebSocket protocol support
- ⏳ Advanced anti-cheat integration

## References

- **Core Implementation**: `GameServer/packets/Server/PacketProcessor.cs`
- **Packet Base Classes**: `CoreBase/Network/PacketIn.cs`, `CoreBase/Network/PacketOut.cs`
- **Game Packets**: `GameServer/packets/Client/GSPacketIn.cs`
- **Handler Framework**: `GameServer/packets/Server/IPacketHandler.cs`

# Network Protocol System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

The Network Protocol System provides high-performance, bidirectional client-server communication for OpenDAoC. It manages packet encoding/decoding, reliability, security, and protocol versioning through a sophisticated packet handler architecture with support for both TCP and UDP protocols.

## Core Architecture

### Packet Infrastructure

```csharp
// Base packet interfaces
public interface IPacket
{
    byte[] Buffer { get; }
    int Offset { get; }
    int Size { get; }
}

public abstract class PacketIn : MemoryStream, IPacket
{
    public abstract byte ReadByte();
    public abstract ushort ReadShort();
    public abstract uint ReadInt();
    public abstract string ReadString(int maxLength);
    public virtual void Skip(long bytes);
}

public abstract class PacketOut : MemoryStream, IPacket
{
    public abstract void WriteByte(byte value);
    public abstract void WriteShort(ushort value);
    public abstract void WriteInt(uint value);
    public abstract void WriteString(string value);
    public virtual void WritePacketLength();
}
```

### Protocol-Specific Implementations

```csharp
// UDP packet with object pooling
public class GSUDPPacketOut : PacketOut, IPooledObject<GSUDPPacketOut>
{
    private static readonly ObjectPool<GSUDPPacketOut> _pool = new();
    private byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public static GSUDPPacketOut GetFromPool()
    {
        var packet = _pool.Get();
        packet.Reset();
        return packet;
    }
    
    public void ReturnToPool()
    {
        Reset();
        _pool.Return(this);
    }
    
    public void Reset()
    {
        _position = 0;
        IsSizeSet = false;
        IssuedTimestamp = 0;
    }
}

// TCP packet for reliable delivery
public class GSTCPPacketOut : PacketOut, IPooledObject<GSTCPPacketOut>
{
    private readonly byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public override void WriteByte(byte value)
    {
        if (_position < _buffer.Length)
            _buffer[_position++] = value;
    }
    
    public override void WriteShort(ushort value)
    {
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value & 0xFF));
    }
}

// Incoming packet parser
public class GSPacketIn : PacketIn, IPooledObject<GSPacketIn>
{
    public const ushort HDR_SIZE = 12; // Header + checksum
    
    private ushort _code;      // Packet ID
    private ushort _parameter; // Packet parameter
    private ushort _psize;     // Packet size
    private ushort _sequence;  // Packet sequence
    private ushort _sessionID; // Session ID
    
    public ushort Code => _code;
    public ushort SessionID => _sessionID;
    public ushort PacketSize => (ushort)(_psize + HDR_SIZE);
    public ushort DataSize => _psize;
    
    public void Load(byte[] buffer, int offset, int count)
    {
        if (count < HDR_SIZE)
            throw new ArgumentException("Packet too small");
            
        // Parse header
        _psize = Marshal.ConvertToUInt16(buffer, offset);
        _sessionID = Marshal.ConvertToUInt16(buffer, offset + 2);
        _parameter = Marshal.ConvertToUInt16(buffer, offset + 4);
        _sequence = Marshal.ConvertToUInt16(buffer, offset + 6);
        _code = Marshal.ConvertToUInt16(buffer, offset + 8);
        
        // Load packet data
        SetLength(0);
        Write(buffer, offset + 10, count - HDR_SIZE);
        Seek(0, SeekOrigin.Begin);
    }
}
```

## Packet Processing Pipeline

### PacketProcessor Architecture

```csharp
public class PacketProcessor
{
    private const int SAVED_PACKETS_COUNT = 16;
    private readonly GameClient _client;
    private readonly IPacketHandler[] _packetHandlers = new IPacketHandler[256];
    private readonly PacketPreprocessing _packetPreprocessor = new();
    private readonly Queue<IPacket> _savedPackets = new(SAVED_PACKETS_COUNT);
    private readonly Lock _savedPacketsLock = new();
    
    // Packet queues for batching
    private readonly DrainArray<GSTCPPacketOut> _tcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpToTcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpPacketQueue = new();
    
    // Send buffer management
    private SocketAsyncEventArgs _tcpSendArgs;
    private SocketAsyncEventArgs _udpSendArgs;
    private int _tcpSendBufferPosition;
    private int _udpSendBufferPosition;
    private uint _udpCounter;
    
    public PacketProcessor(GameClient client)
    {
        _client = client;
        LoadPacketHandlers();
        GetAvailableTcpSendArgs();
        GetAvailableUdpSendArgs();
    }
}
```

### Packet Handler Registration

```csharp
private void LoadPacketHandlers()
{
    string version = "v168";
    
    lock (_loadPacketHandlersLock)
    {
        // Check cache first
        if (_cachedPacketHandlerSearchResults.TryGetValue(version, out var cachedHandlers))
        {
            _packetHandlers = cachedHandlers.Clone() as IPacketHandler[];
            return;
        }
        
        // Search assemblies for packet handlers
        _packetHandlers = new IPacketHandler[256];
        int count = SearchAndAddPacketHandlers(version, Assembly.GetAssembly(typeof(GameServer)), _packetHandlers);
        
        // Search script assemblies
        foreach (Assembly asm in ScriptMgr.Scripts)
            count += SearchAndAddPacketHandlers(version, asm, _packetHandlers);
            
        // Cache results
        _cachedPacketHandlerSearchResults[version] = _packetHandlers.Clone() as IPacketHandler[];
        
        log.Info($"Loaded {count} packet handlers for {version}");
    }
}

private int SearchAndAddPacketHandlers(string version, Assembly assembly, IPacketHandler[] packetHandlers)
{
    int count = 0;
    
    foreach (Type type in assembly.GetTypes())
    {
        if (!type.IsClass || type.IsAbstract || !typeof(IPacketHandler).IsAssignableFrom(type))
            continue;
            
        var packetHandlerAttributes = type.GetCustomAttributes(typeof(PacketHandlerAttribute), false);
        if (packetHandlerAttributes.Length == 0)
            continue;
            
        var attribute = (PacketHandlerAttribute)packetHandlerAttributes[0];
        
        // Version filtering logic here
        
        var handler = Activator.CreateInstance(type) as IPacketHandler;
        int packetCode = attribute.Code;
        
        if (packetCode >= 0 && packetCode < packetHandlers.Length)
        {
            packetHandlers[packetCode] = handler;
            count++;
        }
    }
    
    return count;
}
```

### Inbound Packet Processing

```csharp
public void ProcessInboundPacket(GSPacketIn packet)
{
    int code = packet.Code;
    SavePacket(packet);
    
    // Validate packet code
    if (code >= _packetHandlers.Length)
    {
        log.Error($"Packet code {code:X2} outside handler array bounds");
        LogInvalidPacket(packet);
        return;
    }
    
    IPacketHandler handler = _packetHandlers[code];
    if (handler == null)
        return;
        
    // Security preprocessing
    if (!_packetPreprocessor.CanProcessPacket(_client, packet))
    {
        log.Info($"Preprocessor blocked packet 0x{packet.Code:X2}");
        return;
    }
    
    try
    {
        long startTick = GameLoop.GetRealTime();
        handler.HandlePacket(_client, packet);
        long elapsed = GameLoop.GetRealTime() - startTick;
        
        // Performance monitoring
        if (elapsed > Diagnostics.LongTickThreshold)
        {
            log.Warn($"Long packet processing: 0x{packet.Code:X2} took {elapsed}ms for {_client.Player?.Name}");
        }
    }
    catch (Exception e)
    {
        log.Error($"Error processing packet 0x{packet.Code:X2} from {_client}: {e}");
    }
}

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
```

## Outbound Packet Management

### Packet Queuing System

```csharp
public void QueuePacket(GSTCPPacketOut packet)
{
    if (_client.ClientState is eClientState.Disconnected or eClientState.Linkdead)
        return;
        
    // Ensure packet size is set
    if (!packet.IsSizeSet)
        packet.WritePacketLength();
        
    _tcpPacketQueue.Add(packet);
}

public void QueuePacket(GSUDPPacketOut packet, bool forced = false)
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

### Batch Packet Sending

```csharp
public void SendPendingPackets()
{
    // Process TCP packets
    _tcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendTcpPacketToTcpSendBuffer(packet), this);
    
    // Process UDP-to-TCP fallback packets
    _udpToTcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToTcpSendBuffer(packet), this);
    
    SendTcp();
    
    // Process UDP packets
    _udpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToUdpSendBuffer(packet), this);
    
    SendUdp();
}

private void AppendTcpPacketToTcpSendBuffer(GSTCPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
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

### UDP Packet Handling

```csharp
private void AppendUdpPacketToUdpSendBuffer(GSUDPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
    if (!ValidatePacketSize(packetBuffer, packetSize))
        return;
        
    int nextPosition = _udpSendBufferPosition + packetSize;
    
    if (nextPosition > _udpSendArgs.Buffer.Length)
    {
        if (!SendUdp())
            return;
            
        if (_udpSendBufferPosition + packetSize > _udpSendArgs.Buffer.Length)
            return;
            
        nextPosition = packetSize;
    }
    
    // Copy packet and add UDP counter
    Buffer.BlockCopy(packetBuffer, 0, _udpSendArgs.Buffer, _udpSendBufferPosition, packetSize);
    _udpCounter++; // Let it overflow
    _udpSendArgs.Buffer[_udpSendBufferPosition + 2] = (byte)(_udpCounter >> 8);
    _udpSendArgs.Buffer[_udpSendBufferPosition + 3] = (byte)_udpCounter;
    _udpSendBufferPosition = nextPosition;
    SavePacket(packet);
}
```

## Security and Validation

### Packet Preprocessing

```csharp
public class PacketPreprocessing
{
    private readonly Dictionary<int, int> _packetIdToPreprocessMap = new();
    private readonly Dictionary<int, Func<GameClient, GSPacketIn, bool>> _preprocessors = new();
    
    public PacketPreprocessing()
    {
        RegisterPreprocessors((int)eClientStatus.LoggedIn, 
            (client, packet) => client.Account != null);
        RegisterPreprocessors((int)eClientStatus.PlayerInGame, 
            (client, packet) => client.Player != null);
    }
    
    public bool CanProcessPacket(GameClient client, GSPacketIn packet)
    {
        if (!_packetIdToPreprocessMap.TryGetValue(packet.Code, out int preprocessorId))
            return true; // No preprocessor = allow
            
        if (_preprocessors.TryGetValue(preprocessorId, out var preprocessor))
            return preprocessor(client, packet);
            
        return true;
    }
    
    private void RegisterPreprocessors(int preprocessorId, Func<GameClient, GSPacketIn, bool> func)
    {
        _preprocessors[preprocessorId] = func;
    }
}
```

### Packet Validation

```csharp
private static bool ValidatePacketIssuedTimestamp<T>(T packet) where T : PacketOut, IPooledObject<T>
{
    if (!packet.IsValidForTick())
    {
        if (packet.IssuedTimestamp != 0)
        {
            log.Error($"Packet not issued in current game loop time (Code: 0x{packet.Code:X2}) " +
                     $"(Issued: {packet.IssuedTimestamp}) (Current: {GameLoop.GameLoopTime})");
            return false;
        }
        
        log.Debug($"Packet issued outside game loop (Code: 0x{packet.Code:X2})");
    }
    
    return true;
}

private bool ValidatePacketSize(byte[] packetBuffer, int packetSize)
{
    if (packetSize <= 2048)
        return true;
        
    log.Error($"Discarding oversized packet. Code: 0x{packetBuffer[2]:X2}, " +
             $"Account: {_client.Account?.Name ?? _client.TcpEndpointAddress}, Size: {packetSize}");
             
    _client.Out.SendMessage($"Oversized packet detected (code: 0x{packetBuffer[2]:X2}) (size: {packetSize}). " +
                           "Please report this issue!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
    return false;
}
```

### Checksum Verification

```csharp
public static int CalculateChecksum(byte[] data, int start, int count)
{
    ushort checksum = 0;
    ushort val1 = 0;
    ushort val2 = 0;
    
    int dataPtr = start;
    int len = count - 2; // Exclude checksum bytes
    
    for (int i = 0; i < len; i += 2)
    {
        if (i + 1 < len)
        {
            val1 = (ushort)((data[dataPtr] << 8) | data[dataPtr + 1]);
        }
        else
        {
            val1 = (ushort)(data[dataPtr] << 8);
        }
        
        val2 = (ushort)(val2 + val1);
        if ((val2 & 0x80000000) != 0)
        {
            val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
        }
        
        dataPtr += 2;
    }
    
    val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
    checksum = (ushort)(~val2);
    
    return checksum;
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

## Performance Optimizations

### Object Pooling

```csharp
public static class PacketPool<T> where T : class, IPooledObject<T>, new()
{
    private static readonly ConcurrentQueue<T> _pool = new();
    private static readonly int MAX_POOL_SIZE = 1000;
    private static int _poolSize = 0;
    
    public static T Get()
    {
        if (_pool.TryDequeue(out var packet))
        {
            Interlocked.Decrement(ref _poolSize);
            return packet;
        }
        return new T();
    }
    
    public static void Return(T packet)
    {
        if (_poolSize < MAX_POOL_SIZE)
        {
            packet.Reset();
            _pool.Enqueue(packet);
            Interlocked.Increment(ref _poolSize);
        }
    }
}
```

### Asynchronous Socket Operations

```csharp
private void GetAvailableTcpSendArgs()
{
    if (!_tcpSendArgsPool.TryDequeue(out _tcpSendArgs))
    {
        _tcpSendArgs = new SocketAsyncEventArgs();
        _tcpSendArgs.SetBuffer(new byte[8192], 0, 8192);
        _tcpSendArgs.Completed += OnTcpSendCompleted;
    }
    
    _tcpSendBufferPosition = 0;
}

private void OnTcpSendCompleted(object sender, SocketAsyncEventArgs e)
{
    try
    {
        if (e.SocketError == SocketError.Success)
        {
            // Return to pool for reuse
            _tcpSendArgsPool.Enqueue(e);
        }
        else
        {
            log.Debug($"TCP send error: {e.SocketError}");
            _client.Disconnect();
        }
    }
    catch (Exception ex)
    {
        log.Error($"Error in TCP send completion: {ex}");
    }
}
```

## Protocol Versioning

### Version Detection

```csharp
private bool CheckVersion()
{
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

### Packet Encoding

```csharp
public interface IPacketEncoding
{
    void EncodePacket(PacketOut packet);
    void DecodePacket(PacketIn packet);
}

public class PacketEncoding168 : IPacketEncoding
{
    public void EncodePacket(PacketOut packet)
    {
        // XOR encoding with version-specific key
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
    
    public void DecodePacket(PacketIn packet)
    {
        // Reverse XOR encoding
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
}
```

## Error Handling and Recovery

### Connection Recovery

```csharp
private bool SendTcp()
{
    if (!_client.Socket.Connected)
        return false;
        
    try
    {
        if (_tcpSendBufferPosition > 0)
        {
            _tcpSendArgs.SetBuffer(0, _tcpSendBufferPosition);
            
            if (_client.SendAsync(_tcpSendArgs))
                GetAvailableTcpSendArgs();
                
            _tcpSendBufferPosition = 0;
        }
        
        return true;
    }
    catch (ObjectDisposedException) { }
    catch (SocketException e)
    {
        log.Debug($"Socket exception on TCP send (Client: {_client}) (Code: {e.SocketErrorCode})");
    }
    catch (Exception e)
    {
        log.Error($"Unhandled exception on TCP send (Client: {_client}): {e}");
    }
    
    return false;
}
```

### Packet Loss Handling

```csharp
public class PacketReliability
{
    private readonly Dictionary<ushort, PendingPacket> _pendingPackets = new();
    private readonly Timer _retransmissionTimer;
    
    public void SendReliablePacket(GSUDPPacketOut packet)
    {
        packet.Sequence = GetNextSequence();
        _pendingPackets[packet.Sequence] = new PendingPacket
        {
            Packet = packet,
            SendTime = GameLoop.GameLoopTime,
            RetryCount = 0
        };
        
        SendPacket(packet);
    }
    
    public void AcknowledgePacket(ushort sequence)
    {
        _pendingPackets.Remove(sequence);
    }
    
    private void CheckRetransmissions()
    {
        long currentTime = GameLoop.GameLoopTime;
        
        foreach (var kvp in _pendingPackets.ToArray())
        {
            var pending = kvp.Value;
            
            if (currentTime - pending.SendTime > RETRANSMISSION_TIMEOUT)
            {
                if (pending.RetryCount < MAX_RETRIES)
                {
                    pending.RetryCount++;
                    pending.SendTime = currentTime;
                    SendPacket(pending.Packet);
                }
                else
                {
                    // Give up and disconnect
                    _pendingPackets.Remove(kvp.Key);
                    HandlePacketLoss();
                }
            }
        }
    }
}
```

## Configuration

### Network Settings

```csharp
[ServerProperty("network", "tcp_send_buffer_size", "TCP send buffer size", 8192)]
public static int TCP_SEND_BUFFER_SIZE;

[ServerProperty("network", "udp_send_buffer_size", "UDP send buffer size", 4096)]
public static int UDP_SEND_BUFFER_SIZE;

[ServerProperty("network", "packet_pool_size", "Maximum packet pool size", 1000)]
public static int PACKET_POOL_SIZE;

[ServerProperty("network", "save_packets", "Save packets for debugging", false)]
public static bool SAVE_PACKETS;

[ServerProperty("network", "packet_timeout", "Packet processing timeout (ms)", 5000)]
public static int PACKET_TIMEOUT;
```

### Protocol Constants

```csharp
public static class ProtocolConstants
{
    public const int MAX_PACKET_SIZE = 2048;
    public const int MIN_PACKET_SIZE = 12;
    public const int UDP_PING_TIMEOUT = 70000; // 70 seconds
    public const int RETRANSMISSION_TIMEOUT = 1000; // 1 second
    public const int MAX_RETRIES = 3;
    public const int CHECKSUM_SIZE = 2;
}
```

## Integration Points

### Game Loop Integration

```csharp
public static void TickNetworking()
{
    // Process all client packet queues
    ClientService.ProcessAllClientPackets();
    
    // Handle UDP packet reception
    ProcessPendingUdpPackets();
    
    // Clean up disconnected clients
    CleanupDisconnectedClients();
}
```

### Event System Integration

```csharp
public class NetworkEvents
{
    public static readonly GameEventMgr.EventType PacketReceived = "PacketReceived";
    public static readonly GameEventMgr.EventType PacketSent = "PacketSent";
    public static readonly GameEventMgr.EventType ClientConnected = "ClientConnected";
    public static readonly GameEventMgr.EventType ClientDisconnected = "ClientDisconnected";
}

// Usage
GameEventMgr.AddHandler(NetworkEvents.PacketReceived, OnPacketReceived);
```

## Performance Metrics

### Target Performance

- **Packet Processing**: <1ms per packet
- **Memory Allocation**: <100 bytes per packet (pooled)
- **Throughput**: 1000+ packets/second per client
- **Latency**: <50ms round trip time
- **Reliability**: >99.9% packet delivery

### Monitoring

```csharp
public static class NetworkMetrics
{
    public static long PacketsReceived { get; private set; }
    public static long PacketsSent { get; private set; }
    public static long BytesReceived { get; private set; }
    public static long BytesSent { get; private set; }
    public static long PacketErrors { get; private set; }
    
    public static void LogMetrics()
    {
        log.Info($"Network Stats: RX={PacketsReceived} TX={PacketsSent} " +
                $"Bytes RX={BytesReceived} TX={BytesSent} Errors={PacketErrors}");
    }
}
```

## Implementation Status

**Completed**:
- ✅ Core packet infrastructure
- ✅ TCP/UDP dual protocol support
- ✅ Object pooling for performance
- ✅ Packet validation and security
- ✅ Asynchronous socket operations
- ✅ Protocol versioning
- ✅ Error handling and recovery

**In Progress**:
- 🔄 Advanced packet compression
- 🔄 Encryption layer
- 🔄 Quality of service monitoring

**Planned**:
- ⏳ IPv6 support
- ⏳ WebSocket protocol support
- ⏳ Advanced anti-cheat integration

## References

- **Core Implementation**: `GameServer/packets/Server/PacketProcessor.cs`
- **Packet Base Classes**: `CoreBase/Network/PacketIn.cs`, `CoreBase/Network/PacketOut.cs`
- **Game Packets**: `GameServer/packets/Client/GSPacketIn.cs`
- **Handler Framework**: `GameServer/packets/Server/IPacketHandler.cs`

# Network Protocol System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

The Network Protocol System provides high-performance, bidirectional client-server communication for OpenDAoC. It manages packet encoding/decoding, reliability, security, and protocol versioning through a sophisticated packet handler architecture with support for both TCP and UDP protocols.

## Core Architecture

### Packet Infrastructure

```csharp
// Base packet interfaces
public interface IPacket
{
    byte[] Buffer { get; }
    int Offset { get; }
    int Size { get; }
}

public abstract class PacketIn : MemoryStream, IPacket
{
    public abstract byte ReadByte();
    public abstract ushort ReadShort();
    public abstract uint ReadInt();
    public abstract string ReadString(int maxLength);
    public virtual void Skip(long bytes);
}

public abstract class PacketOut : MemoryStream, IPacket
{
    public abstract void WriteByte(byte value);
    public abstract void WriteShort(ushort value);
    public abstract void WriteInt(uint value);
    public abstract void WriteString(string value);
    public virtual void WritePacketLength();
}
```

### Protocol-Specific Implementations

```csharp
// UDP packet with object pooling
public class GSUDPPacketOut : PacketOut, IPooledObject<GSUDPPacketOut>
{
    private static readonly ObjectPool<GSUDPPacketOut> _pool = new();
    private byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public static GSUDPPacketOut GetFromPool()
    {
        var packet = _pool.Get();
        packet.Reset();
        return packet;
    }
    
    public void ReturnToPool()
    {
        Reset();
        _pool.Return(this);
    }
    
    public void Reset()
    {
        _position = 0;
        IsSizeSet = false;
        IssuedTimestamp = 0;
    }
}

// TCP packet for reliable delivery
public class GSTCPPacketOut : PacketOut, IPooledObject<GSTCPPacketOut>
{
    private readonly byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public override void WriteByte(byte value)
    {
        if (_position < _buffer.Length)
            _buffer[_position++] = value;
    }
    
    public override void WriteShort(ushort value)
    {
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value & 0xFF));
    }
}

// Incoming packet parser
public class GSPacketIn : PacketIn, IPooledObject<GSPacketIn>
{
    public const ushort HDR_SIZE = 12; // Header + checksum
    
    private ushort _code;      // Packet ID
    private ushort _parameter; // Packet parameter
    private ushort _psize;     // Packet size
    private ushort _sequence;  // Packet sequence
    private ushort _sessionID; // Session ID
    
    public ushort Code => _code;
    public ushort SessionID => _sessionID;
    public ushort PacketSize => (ushort)(_psize + HDR_SIZE);
    public ushort DataSize => _psize;
    
    public void Load(byte[] buffer, int offset, int count)
    {
        if (count < HDR_SIZE)
            throw new ArgumentException("Packet too small");
            
        // Parse header
        _psize = Marshal.ConvertToUInt16(buffer, offset);
        _sessionID = Marshal.ConvertToUInt16(buffer, offset + 2);
        _parameter = Marshal.ConvertToUInt16(buffer, offset + 4);
        _sequence = Marshal.ConvertToUInt16(buffer, offset + 6);
        _code = Marshal.ConvertToUInt16(buffer, offset + 8);
        
        // Load packet data
        SetLength(0);
        Write(buffer, offset + 10, count - HDR_SIZE);
        Seek(0, SeekOrigin.Begin);
    }
}
```

## Packet Processing Pipeline

### PacketProcessor Architecture

```csharp
public class PacketProcessor
{
    private const int SAVED_PACKETS_COUNT = 16;
    private readonly GameClient _client;
    private readonly IPacketHandler[] _packetHandlers = new IPacketHandler[256];
    private readonly PacketPreprocessing _packetPreprocessor = new();
    private readonly Queue<IPacket> _savedPackets = new(SAVED_PACKETS_COUNT);
    private readonly Lock _savedPacketsLock = new();
    
    // Packet queues for batching
    private readonly DrainArray<GSTCPPacketOut> _tcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpToTcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpPacketQueue = new();
    
    // Send buffer management
    private SocketAsyncEventArgs _tcpSendArgs;
    private SocketAsyncEventArgs _udpSendArgs;
    private int _tcpSendBufferPosition;
    private int _udpSendBufferPosition;
    private uint _udpCounter;
    
    public PacketProcessor(GameClient client)
    {
        _client = client;
        LoadPacketHandlers();
        GetAvailableTcpSendArgs();
        GetAvailableUdpSendArgs();
    }
}
```

### Packet Handler Registration

```csharp
private void LoadPacketHandlers()
{
    string version = "v168";
    
    lock (_loadPacketHandlersLock)
    {
        // Check cache first
        if (_cachedPacketHandlerSearchResults.TryGetValue(version, out var cachedHandlers))
        {
            _packetHandlers = cachedHandlers.Clone() as IPacketHandler[];
            return;
        }
        
        // Search assemblies for packet handlers
        _packetHandlers = new IPacketHandler[256];
        int count = SearchAndAddPacketHandlers(version, Assembly.GetAssembly(typeof(GameServer)), _packetHandlers);
        
        // Search script assemblies
        foreach (Assembly asm in ScriptMgr.Scripts)
            count += SearchAndAddPacketHandlers(version, asm, _packetHandlers);
            
        // Cache results
        _cachedPacketHandlerSearchResults[version] = _packetHandlers.Clone() as IPacketHandler[];
        
        log.Info($"Loaded {count} packet handlers for {version}");
    }
}

private int SearchAndAddPacketHandlers(string version, Assembly assembly, IPacketHandler[] packetHandlers)
{
    int count = 0;
    
    foreach (Type type in assembly.GetTypes())
    {
        if (!type.IsClass || type.IsAbstract || !typeof(IPacketHandler).IsAssignableFrom(type))
            continue;
            
        var packetHandlerAttributes = type.GetCustomAttributes(typeof(PacketHandlerAttribute), false);
        if (packetHandlerAttributes.Length == 0)
            continue;
            
        var attribute = (PacketHandlerAttribute)packetHandlerAttributes[0];
        
        // Version filtering logic here
        
        var handler = Activator.CreateInstance(type) as IPacketHandler;
        int packetCode = attribute.Code;
        
        if (packetCode >= 0 && packetCode < packetHandlers.Length)
        {
            packetHandlers[packetCode] = handler;
            count++;
        }
    }
    
    return count;
}
```

### Inbound Packet Processing

```csharp
public void ProcessInboundPacket(GSPacketIn packet)
{
    int code = packet.Code;
    SavePacket(packet);
    
    // Validate packet code
    if (code >= _packetHandlers.Length)
    {
        log.Error($"Packet code {code:X2} outside handler array bounds");
        LogInvalidPacket(packet);
        return;
    }
    
    IPacketHandler handler = _packetHandlers[code];
    if (handler == null)
        return;
        
    // Security preprocessing
    if (!_packetPreprocessor.CanProcessPacket(_client, packet))
    {
        log.Info($"Preprocessor blocked packet 0x{packet.Code:X2}");
        return;
    }
    
    try
    {
        long startTick = GameLoop.GetRealTime();
        handler.HandlePacket(_client, packet);
        long elapsed = GameLoop.GetRealTime() - startTick;
        
        // Performance monitoring
        if (elapsed > Diagnostics.LongTickThreshold)
        {
            log.Warn($"Long packet processing: 0x{packet.Code:X2} took {elapsed}ms for {_client.Player?.Name}");
        }
    }
    catch (Exception e)
    {
        log.Error($"Error processing packet 0x{packet.Code:X2} from {_client}: {e}");
    }
}

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
```

## Outbound Packet Management

### Packet Queuing System

```csharp
public void QueuePacket(GSTCPPacketOut packet)
{
    if (_client.ClientState is eClientState.Disconnected or eClientState.Linkdead)
        return;
        
    // Ensure packet size is set
    if (!packet.IsSizeSet)
        packet.WritePacketLength();
        
    _tcpPacketQueue.Add(packet);
}

public void QueuePacket(GSUDPPacketOut packet, bool forced = false)
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

### Batch Packet Sending

```csharp
public void SendPendingPackets()
{
    // Process TCP packets
    _tcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendTcpPacketToTcpSendBuffer(packet), this);
    
    // Process UDP-to-TCP fallback packets
    _udpToTcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToTcpSendBuffer(packet), this);
    
    SendTcp();
    
    // Process UDP packets
    _udpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToUdpSendBuffer(packet), this);
    
    SendUdp();
}

private void AppendTcpPacketToTcpSendBuffer(GSTCPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
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

### UDP Packet Handling

```csharp
private void AppendUdpPacketToUdpSendBuffer(GSUDPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
    if (!ValidatePacketSize(packetBuffer, packetSize))
        return;
        
    int nextPosition = _udpSendBufferPosition + packetSize;
    
    if (nextPosition > _udpSendArgs.Buffer.Length)
    {
        if (!SendUdp())
            return;
            
        if (_udpSendBufferPosition + packetSize > _udpSendArgs.Buffer.Length)
            return;
            
        nextPosition = packetSize;
    }
    
    // Copy packet and add UDP counter
    Buffer.BlockCopy(packetBuffer, 0, _udpSendArgs.Buffer, _udpSendBufferPosition, packetSize);
    _udpCounter++; // Let it overflow
    _udpSendArgs.Buffer[_udpSendBufferPosition + 2] = (byte)(_udpCounter >> 8);
    _udpSendArgs.Buffer[_udpSendBufferPosition + 3] = (byte)_udpCounter;
    _udpSendBufferPosition = nextPosition;
    SavePacket(packet);
}
```

## Security and Validation

### Packet Preprocessing

```csharp
public class PacketPreprocessing
{
    private readonly Dictionary<int, int> _packetIdToPreprocessMap = new();
    private readonly Dictionary<int, Func<GameClient, GSPacketIn, bool>> _preprocessors = new();
    
    public PacketPreprocessing()
    {
        RegisterPreprocessors((int)eClientStatus.LoggedIn, 
            (client, packet) => client.Account != null);
        RegisterPreprocessors((int)eClientStatus.PlayerInGame, 
            (client, packet) => client.Player != null);
    }
    
    public bool CanProcessPacket(GameClient client, GSPacketIn packet)
    {
        if (!_packetIdToPreprocessMap.TryGetValue(packet.Code, out int preprocessorId))
            return true; // No preprocessor = allow
            
        if (_preprocessors.TryGetValue(preprocessorId, out var preprocessor))
            return preprocessor(client, packet);
            
        return true;
    }
    
    private void RegisterPreprocessors(int preprocessorId, Func<GameClient, GSPacketIn, bool> func)
    {
        _preprocessors[preprocessorId] = func;
    }
}
```

### Packet Validation

```csharp
private static bool ValidatePacketIssuedTimestamp<T>(T packet) where T : PacketOut, IPooledObject<T>
{
    if (!packet.IsValidForTick())
    {
        if (packet.IssuedTimestamp != 0)
        {
            log.Error($"Packet not issued in current game loop time (Code: 0x{packet.Code:X2}) " +
                     $"(Issued: {packet.IssuedTimestamp}) (Current: {GameLoop.GameLoopTime})");
            return false;
        }
        
        log.Debug($"Packet issued outside game loop (Code: 0x{packet.Code:X2})");
    }
    
    return true;
}

private bool ValidatePacketSize(byte[] packetBuffer, int packetSize)
{
    if (packetSize <= 2048)
        return true;
        
    log.Error($"Discarding oversized packet. Code: 0x{packetBuffer[2]:X2}, " +
             $"Account: {_client.Account?.Name ?? _client.TcpEndpointAddress}, Size: {packetSize}");
             
    _client.Out.SendMessage($"Oversized packet detected (code: 0x{packetBuffer[2]:X2}) (size: {packetSize}). " +
                           "Please report this issue!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
    return false;
}
```

### Checksum Verification

```csharp
public static int CalculateChecksum(byte[] data, int start, int count)
{
    ushort checksum = 0;
    ushort val1 = 0;
    ushort val2 = 0;
    
    int dataPtr = start;
    int len = count - 2; // Exclude checksum bytes
    
    for (int i = 0; i < len; i += 2)
    {
        if (i + 1 < len)
        {
            val1 = (ushort)((data[dataPtr] << 8) | data[dataPtr + 1]);
        }
        else
        {
            val1 = (ushort)(data[dataPtr] << 8);
        }
        
        val2 = (ushort)(val2 + val1);
        if ((val2 & 0x80000000) != 0)
        {
            val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
        }
        
        dataPtr += 2;
    }
    
    val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
    checksum = (ushort)(~val2);
    
    return checksum;
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

## Performance Optimizations

### Object Pooling

```csharp
public static class PacketPool<T> where T : class, IPooledObject<T>, new()
{
    private static readonly ConcurrentQueue<T> _pool = new();
    private static readonly int MAX_POOL_SIZE = 1000;
    private static int _poolSize = 0;
    
    public static T Get()
    {
        if (_pool.TryDequeue(out var packet))
        {
            Interlocked.Decrement(ref _poolSize);
            return packet;
        }
        return new T();
    }
    
    public static void Return(T packet)
    {
        if (_poolSize < MAX_POOL_SIZE)
        {
            packet.Reset();
            _pool.Enqueue(packet);
            Interlocked.Increment(ref _poolSize);
        }
    }
}
```

### Asynchronous Socket Operations

```csharp
private void GetAvailableTcpSendArgs()
{
    if (!_tcpSendArgsPool.TryDequeue(out _tcpSendArgs))
    {
        _tcpSendArgs = new SocketAsyncEventArgs();
        _tcpSendArgs.SetBuffer(new byte[8192], 0, 8192);
        _tcpSendArgs.Completed += OnTcpSendCompleted;
    }
    
    _tcpSendBufferPosition = 0;
}

private void OnTcpSendCompleted(object sender, SocketAsyncEventArgs e)
{
    try
    {
        if (e.SocketError == SocketError.Success)
        {
            // Return to pool for reuse
            _tcpSendArgsPool.Enqueue(e);
        }
        else
        {
            log.Debug($"TCP send error: {e.SocketError}");
            _client.Disconnect();
        }
    }
    catch (Exception ex)
    {
        log.Error($"Error in TCP send completion: {ex}");
    }
}
```

## Protocol Versioning

### Version Detection

```csharp
private bool CheckVersion()
{
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

### Packet Encoding

```csharp
public interface IPacketEncoding
{
    void EncodePacket(PacketOut packet);
    void DecodePacket(PacketIn packet);
}

public class PacketEncoding168 : IPacketEncoding
{
    public void EncodePacket(PacketOut packet)
    {
        // XOR encoding with version-specific key
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
    
    public void DecodePacket(PacketIn packet)
    {
        // Reverse XOR encoding
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
}
```

## Error Handling and Recovery

### Connection Recovery

```csharp
private bool SendTcp()
{
    if (!_client.Socket.Connected)
        return false;
        
    try
    {
        if (_tcpSendBufferPosition > 0)
        {
            _tcpSendArgs.SetBuffer(0, _tcpSendBufferPosition);
            
            if (_client.SendAsync(_tcpSendArgs))
                GetAvailableTcpSendArgs();
                
            _tcpSendBufferPosition = 0;
        }
        
        return true;
    }
    catch (ObjectDisposedException) { }
    catch (SocketException e)
    {
        log.Debug($"Socket exception on TCP send (Client: {_client}) (Code: {e.SocketErrorCode})");
    }
    catch (Exception e)
    {
        log.Error($"Unhandled exception on TCP send (Client: {_client}): {e}");
    }
    
    return false;
}
```

### Packet Loss Handling

```csharp
public class PacketReliability
{
    private readonly Dictionary<ushort, PendingPacket> _pendingPackets = new();
    private readonly Timer _retransmissionTimer;
    
    public void SendReliablePacket(GSUDPPacketOut packet)
    {
        packet.Sequence = GetNextSequence();
        _pendingPackets[packet.Sequence] = new PendingPacket
        {
            Packet = packet,
            SendTime = GameLoop.GameLoopTime,
            RetryCount = 0
        };
        
        SendPacket(packet);
    }
    
    public void AcknowledgePacket(ushort sequence)
    {
        _pendingPackets.Remove(sequence);
    }
    
    private void CheckRetransmissions()
    {
        long currentTime = GameLoop.GameLoopTime;
        
        foreach (var kvp in _pendingPackets.ToArray())
        {
            var pending = kvp.Value;
            
            if (currentTime - pending.SendTime > RETRANSMISSION_TIMEOUT)
            {
                if (pending.RetryCount < MAX_RETRIES)
                {
                    pending.RetryCount++;
                    pending.SendTime = currentTime;
                    SendPacket(pending.Packet);
                }
                else
                {
                    // Give up and disconnect
                    _pendingPackets.Remove(kvp.Key);
                    HandlePacketLoss();
                }
            }
        }
    }
}
```

## Configuration

### Network Settings

```csharp
[ServerProperty("network", "tcp_send_buffer_size", "TCP send buffer size", 8192)]
public static int TCP_SEND_BUFFER_SIZE;

[ServerProperty("network", "udp_send_buffer_size", "UDP send buffer size", 4096)]
public static int UDP_SEND_BUFFER_SIZE;

[ServerProperty("network", "packet_pool_size", "Maximum packet pool size", 1000)]
public static int PACKET_POOL_SIZE;

[ServerProperty("network", "save_packets", "Save packets for debugging", false)]
public static bool SAVE_PACKETS;

[ServerProperty("network", "packet_timeout", "Packet processing timeout (ms)", 5000)]
public static int PACKET_TIMEOUT;
```

### Protocol Constants

```csharp
public static class ProtocolConstants
{
    public const int MAX_PACKET_SIZE = 2048;
    public const int MIN_PACKET_SIZE = 12;
    public const int UDP_PING_TIMEOUT = 70000; // 70 seconds
    public const int RETRANSMISSION_TIMEOUT = 1000; // 1 second
    public const int MAX_RETRIES = 3;
    public const int CHECKSUM_SIZE = 2;
}
```

## Integration Points

### Game Loop Integration

```csharp
public static void TickNetworking()
{
    // Process all client packet queues
    ClientService.ProcessAllClientPackets();
    
    // Handle UDP packet reception
    ProcessPendingUdpPackets();
    
    // Clean up disconnected clients
    CleanupDisconnectedClients();
}
```

### Event System Integration

```csharp
public class NetworkEvents
{
    public static readonly GameEventMgr.EventType PacketReceived = "PacketReceived";
    public static readonly GameEventMgr.EventType PacketSent = "PacketSent";
    public static readonly GameEventMgr.EventType ClientConnected = "ClientConnected";
    public static readonly GameEventMgr.EventType ClientDisconnected = "ClientDisconnected";
}

// Usage
GameEventMgr.AddHandler(NetworkEvents.PacketReceived, OnPacketReceived);
```

## Performance Metrics

### Target Performance

- **Packet Processing**: <1ms per packet
- **Memory Allocation**: <100 bytes per packet (pooled)
- **Throughput**: 1000+ packets/second per client
- **Latency**: <50ms round trip time
- **Reliability**: >99.9% packet delivery

### Monitoring

```csharp
public static class NetworkMetrics
{
    public static long PacketsReceived { get; private set; }
    public static long PacketsSent { get; private set; }
    public static long BytesReceived { get; private set; }
    public static long BytesSent { get; private set; }
    public static long PacketErrors { get; private set; }
    
    public static void LogMetrics()
    {
        log.Info($"Network Stats: RX={PacketsReceived} TX={PacketsSent} " +
                $"Bytes RX={BytesReceived} TX={BytesSent} Errors={PacketErrors}");
    }
}
```

## Implementation Status

**Completed**:
- ✅ Core packet infrastructure
- ✅ TCP/UDP dual protocol support
- ✅ Object pooling for performance
- ✅ Packet validation and security
- ✅ Asynchronous socket operations
- ✅ Protocol versioning
- ✅ Error handling and recovery

**In Progress**:
- 🔄 Advanced packet compression
- 🔄 Encryption layer
- 🔄 Quality of service monitoring

**Planned**:
- ⏳ IPv6 support
- ⏳ WebSocket protocol support
- ⏳ Advanced anti-cheat integration

## References

- **Core Implementation**: `GameServer/packets/Server/PacketProcessor.cs`
- **Packet Base Classes**: `CoreBase/Network/PacketIn.cs`, `CoreBase/Network/PacketOut.cs`
- **Game Packets**: `GameServer/packets/Client/GSPacketIn.cs`
- **Handler Framework**: `GameServer/packets/Server/IPacketHandler.cs`

# Network Protocol System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

The Network Protocol System provides high-performance, bidirectional client-server communication for OpenDAoC. It manages packet encoding/decoding, reliability, security, and protocol versioning through a sophisticated packet handler architecture with support for both TCP and UDP protocols.

## Core Architecture

### Packet Infrastructure

```csharp
// Base packet interfaces
public interface IPacket
{
    byte[] Buffer { get; }
    int Offset { get; }
    int Size { get; }
}

public abstract class PacketIn : MemoryStream, IPacket
{
    public abstract byte ReadByte();
    public abstract ushort ReadShort();
    public abstract uint ReadInt();
    public abstract string ReadString(int maxLength);
    public virtual void Skip(long bytes);
}

public abstract class PacketOut : MemoryStream, IPacket
{
    public abstract void WriteByte(byte value);
    public abstract void WriteShort(ushort value);
    public abstract void WriteInt(uint value);
    public abstract void WriteString(string value);
    public virtual void WritePacketLength();
}
```

### Protocol-Specific Implementations

```csharp
// UDP packet with object pooling
public class GSUDPPacketOut : PacketOut, IPooledObject<GSUDPPacketOut>
{
    private static readonly ObjectPool<GSUDPPacketOut> _pool = new();
    private byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public static GSUDPPacketOut GetFromPool()
    {
        var packet = _pool.Get();
        packet.Reset();
        return packet;
    }
    
    public void ReturnToPool()
    {
        Reset();
        _pool.Return(this);
    }
    
    public void Reset()
    {
        _position = 0;
        IsSizeSet = false;
        IssuedTimestamp = 0;
    }
}

// TCP packet for reliable delivery
public class GSTCPPacketOut : PacketOut, IPooledObject<GSTCPPacketOut>
{
    private readonly byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public override void WriteByte(byte value)
    {
        if (_position < _buffer.Length)
            _buffer[_position++] = value;
    }
    
    public override void WriteShort(ushort value)
    {
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value & 0xFF));
    }
}

// Incoming packet parser
public class GSPacketIn : PacketIn, IPooledObject<GSPacketIn>
{
    public const ushort HDR_SIZE = 12; // Header + checksum
    
    private ushort _code;      // Packet ID
    private ushort _parameter; // Packet parameter
    private ushort _psize;     // Packet size
    private ushort _sequence;  // Packet sequence
    private ushort _sessionID; // Session ID
    
    public ushort Code => _code;
    public ushort SessionID => _sessionID;
    public ushort PacketSize => (ushort)(_psize + HDR_SIZE);
    public ushort DataSize => _psize;
    
    public void Load(byte[] buffer, int offset, int count)
    {
        if (count < HDR_SIZE)
            throw new ArgumentException("Packet too small");
            
        // Parse header
        _psize = Marshal.ConvertToUInt16(buffer, offset);
        _sessionID = Marshal.ConvertToUInt16(buffer, offset + 2);
        _parameter = Marshal.ConvertToUInt16(buffer, offset + 4);
        _sequence = Marshal.ConvertToUInt16(buffer, offset + 6);
        _code = Marshal.ConvertToUInt16(buffer, offset + 8);
        
        // Load packet data
        SetLength(0);
        Write(buffer, offset + 10, count - HDR_SIZE);
        Seek(0, SeekOrigin.Begin);
    }
}
```

## Packet Processing Pipeline

### PacketProcessor Architecture

```csharp
public class PacketProcessor
{
    private const int SAVED_PACKETS_COUNT = 16;
    private readonly GameClient _client;
    private readonly IPacketHandler[] _packetHandlers = new IPacketHandler[256];
    private readonly PacketPreprocessing _packetPreprocessor = new();
    private readonly Queue<IPacket> _savedPackets = new(SAVED_PACKETS_COUNT);
    private readonly Lock _savedPacketsLock = new();
    
    // Packet queues for batching
    private readonly DrainArray<GSTCPPacketOut> _tcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpToTcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpPacketQueue = new();
    
    // Send buffer management
    private SocketAsyncEventArgs _tcpSendArgs;
    private SocketAsyncEventArgs _udpSendArgs;
    private int _tcpSendBufferPosition;
    private int _udpSendBufferPosition;
    private uint _udpCounter;
    
    public PacketProcessor(GameClient client)
    {
        _client = client;
        LoadPacketHandlers();
        GetAvailableTcpSendArgs();
        GetAvailableUdpSendArgs();
    }
}
```

### Packet Handler Registration

```csharp
private void LoadPacketHandlers()
{
    string version = "v168";
    
    lock (_loadPacketHandlersLock)
    {
        // Check cache first
        if (_cachedPacketHandlerSearchResults.TryGetValue(version, out var cachedHandlers))
        {
            _packetHandlers = cachedHandlers.Clone() as IPacketHandler[];
            return;
        }
        
        // Search assemblies for packet handlers
        _packetHandlers = new IPacketHandler[256];
        int count = SearchAndAddPacketHandlers(version, Assembly.GetAssembly(typeof(GameServer)), _packetHandlers);
        
        // Search script assemblies
        foreach (Assembly asm in ScriptMgr.Scripts)
            count += SearchAndAddPacketHandlers(version, asm, _packetHandlers);
            
        // Cache results
        _cachedPacketHandlerSearchResults[version] = _packetHandlers.Clone() as IPacketHandler[];
        
        log.Info($"Loaded {count} packet handlers for {version}");
    }
}

private int SearchAndAddPacketHandlers(string version, Assembly assembly, IPacketHandler[] packetHandlers)
{
    int count = 0;
    
    foreach (Type type in assembly.GetTypes())
    {
        if (!type.IsClass || type.IsAbstract || !typeof(IPacketHandler).IsAssignableFrom(type))
            continue;
            
        var packetHandlerAttributes = type.GetCustomAttributes(typeof(PacketHandlerAttribute), false);
        if (packetHandlerAttributes.Length == 0)
            continue;
            
        var attribute = (PacketHandlerAttribute)packetHandlerAttributes[0];
        
        // Version filtering logic here
        
        var handler = Activator.CreateInstance(type) as IPacketHandler;
        int packetCode = attribute.Code;
        
        if (packetCode >= 0 && packetCode < packetHandlers.Length)
        {
            packetHandlers[packetCode] = handler;
            count++;
        }
    }
    
    return count;
}
```

### Inbound Packet Processing

```csharp
public void ProcessInboundPacket(GSPacketIn packet)
{
    int code = packet.Code;
    SavePacket(packet);
    
    // Validate packet code
    if (code >= _packetHandlers.Length)
    {
        log.Error($"Packet code {code:X2} outside handler array bounds");
        LogInvalidPacket(packet);
        return;
    }
    
    IPacketHandler handler = _packetHandlers[code];
    if (handler == null)
        return;
        
    // Security preprocessing
    if (!_packetPreprocessor.CanProcessPacket(_client, packet))
    {
        log.Info($"Preprocessor blocked packet 0x{packet.Code:X2}");
        return;
    }
    
    try
    {
        long startTick = GameLoop.GetRealTime();
        handler.HandlePacket(_client, packet);
        long elapsed = GameLoop.GetRealTime() - startTick;
        
        // Performance monitoring
        if (elapsed > Diagnostics.LongTickThreshold)
        {
            log.Warn($"Long packet processing: 0x{packet.Code:X2} took {elapsed}ms for {_client.Player?.Name}");
        }
    }
    catch (Exception e)
    {
        log.Error($"Error processing packet 0x{packet.Code:X2} from {_client}: {e}");
    }
}

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
```

## Outbound Packet Management

### Packet Queuing System

```csharp
public void QueuePacket(GSTCPPacketOut packet)
{
    if (_client.ClientState is eClientState.Disconnected or eClientState.Linkdead)
        return;
        
    // Ensure packet size is set
    if (!packet.IsSizeSet)
        packet.WritePacketLength();
        
    _tcpPacketQueue.Add(packet);
}

public void QueuePacket(GSUDPPacketOut packet, bool forced = false)
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

### Batch Packet Sending

```csharp
public void SendPendingPackets()
{
    // Process TCP packets
    _tcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendTcpPacketToTcpSendBuffer(packet), this);
    
    // Process UDP-to-TCP fallback packets
    _udpToTcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToTcpSendBuffer(packet), this);
    
    SendTcp();
    
    // Process UDP packets
    _udpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToUdpSendBuffer(packet), this);
    
    SendUdp();
}

private void AppendTcpPacketToTcpSendBuffer(GSTCPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
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

### UDP Packet Handling

```csharp
private void AppendUdpPacketToUdpSendBuffer(GSUDPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
    if (!ValidatePacketSize(packetBuffer, packetSize))
        return;
        
    int nextPosition = _udpSendBufferPosition + packetSize;
    
    if (nextPosition > _udpSendArgs.Buffer.Length)
    {
        if (!SendUdp())
            return;
            
        if (_udpSendBufferPosition + packetSize > _udpSendArgs.Buffer.Length)
            return;
            
        nextPosition = packetSize;
    }
    
    // Copy packet and add UDP counter
    Buffer.BlockCopy(packetBuffer, 0, _udpSendArgs.Buffer, _udpSendBufferPosition, packetSize);
    _udpCounter++; // Let it overflow
    _udpSendArgs.Buffer[_udpSendBufferPosition + 2] = (byte)(_udpCounter >> 8);
    _udpSendArgs.Buffer[_udpSendBufferPosition + 3] = (byte)_udpCounter;
    _udpSendBufferPosition = nextPosition;
    SavePacket(packet);
}
```

## Security and Validation

### Packet Preprocessing

```csharp
public class PacketPreprocessing
{
    private readonly Dictionary<int, int> _packetIdToPreprocessMap = new();
    private readonly Dictionary<int, Func<GameClient, GSPacketIn, bool>> _preprocessors = new();
    
    public PacketPreprocessing()
    {
        RegisterPreprocessors((int)eClientStatus.LoggedIn, 
            (client, packet) => client.Account != null);
        RegisterPreprocessors((int)eClientStatus.PlayerInGame, 
            (client, packet) => client.Player != null);
    }
    
    public bool CanProcessPacket(GameClient client, GSPacketIn packet)
    {
        if (!_packetIdToPreprocessMap.TryGetValue(packet.Code, out int preprocessorId))
            return true; // No preprocessor = allow
            
        if (_preprocessors.TryGetValue(preprocessorId, out var preprocessor))
            return preprocessor(client, packet);
            
        return true;
    }
    
    private void RegisterPreprocessors(int preprocessorId, Func<GameClient, GSPacketIn, bool> func)
    {
        _preprocessors[preprocessorId] = func;
    }
}
```

### Packet Validation

```csharp
private static bool ValidatePacketIssuedTimestamp<T>(T packet) where T : PacketOut, IPooledObject<T>
{
    if (!packet.IsValidForTick())
    {
        if (packet.IssuedTimestamp != 0)
        {
            log.Error($"Packet not issued in current game loop time (Code: 0x{packet.Code:X2}) " +
                     $"(Issued: {packet.IssuedTimestamp}) (Current: {GameLoop.GameLoopTime})");
            return false;
        }
        
        log.Debug($"Packet issued outside game loop (Code: 0x{packet.Code:X2})");
    }
    
    return true;
}

private bool ValidatePacketSize(byte[] packetBuffer, int packetSize)
{
    if (packetSize <= 2048)
        return true;
        
    log.Error($"Discarding oversized packet. Code: 0x{packetBuffer[2]:X2}, " +
             $"Account: {_client.Account?.Name ?? _client.TcpEndpointAddress}, Size: {packetSize}");
             
    _client.Out.SendMessage($"Oversized packet detected (code: 0x{packetBuffer[2]:X2}) (size: {packetSize}). " +
                           "Please report this issue!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
    return false;
}
```

### Checksum Verification

```csharp
public static int CalculateChecksum(byte[] data, int start, int count)
{
    ushort checksum = 0;
    ushort val1 = 0;
    ushort val2 = 0;
    
    int dataPtr = start;
    int len = count - 2; // Exclude checksum bytes
    
    for (int i = 0; i < len; i += 2)
    {
        if (i + 1 < len)
        {
            val1 = (ushort)((data[dataPtr] << 8) | data[dataPtr + 1]);
        }
        else
        {
            val1 = (ushort)(data[dataPtr] << 8);
        }
        
        val2 = (ushort)(val2 + val1);
        if ((val2 & 0x80000000) != 0)
        {
            val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
        }
        
        dataPtr += 2;
    }
    
    val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
    checksum = (ushort)(~val2);
    
    return checksum;
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

## Performance Optimizations

### Object Pooling

```csharp
public static class PacketPool<T> where T : class, IPooledObject<T>, new()
{
    private static readonly ConcurrentQueue<T> _pool = new();
    private static readonly int MAX_POOL_SIZE = 1000;
    private static int _poolSize = 0;
    
    public static T Get()
    {
        if (_pool.TryDequeue(out var packet))
        {
            Interlocked.Decrement(ref _poolSize);
            return packet;
        }
        return new T();
    }
    
    public static void Return(T packet)
    {
        if (_poolSize < MAX_POOL_SIZE)
        {
            packet.Reset();
            _pool.Enqueue(packet);
            Interlocked.Increment(ref _poolSize);
        }
    }
}
```

### Asynchronous Socket Operations

```csharp
private void GetAvailableTcpSendArgs()
{
    if (!_tcpSendArgsPool.TryDequeue(out _tcpSendArgs))
    {
        _tcpSendArgs = new SocketAsyncEventArgs();
        _tcpSendArgs.SetBuffer(new byte[8192], 0, 8192);
        _tcpSendArgs.Completed += OnTcpSendCompleted;
    }
    
    _tcpSendBufferPosition = 0;
}

private void OnTcpSendCompleted(object sender, SocketAsyncEventArgs e)
{
    try
    {
        if (e.SocketError == SocketError.Success)
        {
            // Return to pool for reuse
            _tcpSendArgsPool.Enqueue(e);
        }
        else
        {
            log.Debug($"TCP send error: {e.SocketError}");
            _client.Disconnect();
        }
    }
    catch (Exception ex)
    {
        log.Error($"Error in TCP send completion: {ex}");
    }
}
```

## Protocol Versioning

### Version Detection

```csharp
private bool CheckVersion()
{
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

### Packet Encoding

```csharp
public interface IPacketEncoding
{
    void EncodePacket(PacketOut packet);
    void DecodePacket(PacketIn packet);
}

public class PacketEncoding168 : IPacketEncoding
{
    public void EncodePacket(PacketOut packet)
    {
        // XOR encoding with version-specific key
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
    
    public void DecodePacket(PacketIn packet)
    {
        // Reverse XOR encoding
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
}
```

## Error Handling and Recovery

### Connection Recovery

```csharp
private bool SendTcp()
{
    if (!_client.Socket.Connected)
        return false;
        
    try
    {
        if (_tcpSendBufferPosition > 0)
        {
            _tcpSendArgs.SetBuffer(0, _tcpSendBufferPosition);
            
            if (_client.SendAsync(_tcpSendArgs))
                GetAvailableTcpSendArgs();
                
            _tcpSendBufferPosition = 0;
        }
        
        return true;
    }
    catch (ObjectDisposedException) { }
    catch (SocketException e)
    {
        log.Debug($"Socket exception on TCP send (Client: {_client}) (Code: {e.SocketErrorCode})");
    }
    catch (Exception e)
    {
        log.Error($"Unhandled exception on TCP send (Client: {_client}): {e}");
    }
    
    return false;
}
```

### Packet Loss Handling

```csharp
public class PacketReliability
{
    private readonly Dictionary<ushort, PendingPacket> _pendingPackets = new();
    private readonly Timer _retransmissionTimer;
    
    public void SendReliablePacket(GSUDPPacketOut packet)
    {
        packet.Sequence = GetNextSequence();
        _pendingPackets[packet.Sequence] = new PendingPacket
        {
            Packet = packet,
            SendTime = GameLoop.GameLoopTime,
            RetryCount = 0
        };
        
        SendPacket(packet);
    }
    
    public void AcknowledgePacket(ushort sequence)
    {
        _pendingPackets.Remove(sequence);
    }
    
    private void CheckRetransmissions()
    {
        long currentTime = GameLoop.GameLoopTime;
        
        foreach (var kvp in _pendingPackets.ToArray())
        {
            var pending = kvp.Value;
            
            if (currentTime - pending.SendTime > RETRANSMISSION_TIMEOUT)
            {
                if (pending.RetryCount < MAX_RETRIES)
                {
                    pending.RetryCount++;
                    pending.SendTime = currentTime;
                    SendPacket(pending.Packet);
                }
                else
                {
                    // Give up and disconnect
                    _pendingPackets.Remove(kvp.Key);
                    HandlePacketLoss();
                }
            }
        }
    }
}
```

## Configuration

### Network Settings

```csharp
[ServerProperty("network", "tcp_send_buffer_size", "TCP send buffer size", 8192)]
public static int TCP_SEND_BUFFER_SIZE;

[ServerProperty("network", "udp_send_buffer_size", "UDP send buffer size", 4096)]
public static int UDP_SEND_BUFFER_SIZE;

[ServerProperty("network", "packet_pool_size", "Maximum packet pool size", 1000)]
public static int PACKET_POOL_SIZE;

[ServerProperty("network", "save_packets", "Save packets for debugging", false)]
public static bool SAVE_PACKETS;

[ServerProperty("network", "packet_timeout", "Packet processing timeout (ms)", 5000)]
public static int PACKET_TIMEOUT;
```

### Protocol Constants

```csharp
public static class ProtocolConstants
{
    public const int MAX_PACKET_SIZE = 2048;
    public const int MIN_PACKET_SIZE = 12;
    public const int UDP_PING_TIMEOUT = 70000; // 70 seconds
    public const int RETRANSMISSION_TIMEOUT = 1000; // 1 second
    public const int MAX_RETRIES = 3;
    public const int CHECKSUM_SIZE = 2;
}
```

## Integration Points

### Game Loop Integration

```csharp
public static void TickNetworking()
{
    // Process all client packet queues
    ClientService.ProcessAllClientPackets();
    
    // Handle UDP packet reception
    ProcessPendingUdpPackets();
    
    // Clean up disconnected clients
    CleanupDisconnectedClients();
}
```

### Event System Integration

```csharp
public class NetworkEvents
{
    public static readonly GameEventMgr.EventType PacketReceived = "PacketReceived";
    public static readonly GameEventMgr.EventType PacketSent = "PacketSent";
    public static readonly GameEventMgr.EventType ClientConnected = "ClientConnected";
    public static readonly GameEventMgr.EventType ClientDisconnected = "ClientDisconnected";
}

// Usage
GameEventMgr.AddHandler(NetworkEvents.PacketReceived, OnPacketReceived);
```

## Performance Metrics

### Target Performance

- **Packet Processing**: <1ms per packet
- **Memory Allocation**: <100 bytes per packet (pooled)
- **Throughput**: 1000+ packets/second per client
- **Latency**: <50ms round trip time
- **Reliability**: >99.9% packet delivery

### Monitoring

```csharp
public static class NetworkMetrics
{
    public static long PacketsReceived { get; private set; }
    public static long PacketsSent { get; private set; }
    public static long BytesReceived { get; private set; }
    public static long BytesSent { get; private set; }
    public static long PacketErrors { get; private set; }
    
    public static void LogMetrics()
    {
        log.Info($"Network Stats: RX={PacketsReceived} TX={PacketsSent} " +
                $"Bytes RX={BytesReceived} TX={BytesSent} Errors={PacketErrors}");
    }
}
```

## Implementation Status

**Completed**:
- ✅ Core packet infrastructure
- ✅ TCP/UDP dual protocol support
- ✅ Object pooling for performance
- ✅ Packet validation and security
- ✅ Asynchronous socket operations
- ✅ Protocol versioning
- ✅ Error handling and recovery

**In Progress**:
- 🔄 Advanced packet compression
- 🔄 Encryption layer
- 🔄 Quality of service monitoring

**Planned**:
- ⏳ IPv6 support
- ⏳ WebSocket protocol support
- ⏳ Advanced anti-cheat integration

## References

- **Core Implementation**: `GameServer/packets/Server/PacketProcessor.cs`
- **Packet Base Classes**: `CoreBase/Network/PacketIn.cs`, `CoreBase/Network/PacketOut.cs`
- **Game Packets**: `GameServer/packets/Client/GSPacketIn.cs`
- **Handler Framework**: `GameServer/packets/Server/IPacketHandler.cs`

# Network Protocol System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

The Network Protocol System provides high-performance, bidirectional client-server communication for OpenDAoC. It manages packet encoding/decoding, reliability, security, and protocol versioning through a sophisticated packet handler architecture with support for both TCP and UDP protocols.

## Core Architecture

### Packet Infrastructure

```csharp
// Base packet interfaces
public interface IPacket
{
    byte[] Buffer { get; }
    int Offset { get; }
    int Size { get; }
}

public abstract class PacketIn : MemoryStream, IPacket
{
    public abstract byte ReadByte();
    public abstract ushort ReadShort();
    public abstract uint ReadInt();
    public abstract string ReadString(int maxLength);
    public virtual void Skip(long bytes);
}

public abstract class PacketOut : MemoryStream, IPacket
{
    public abstract void WriteByte(byte value);
    public abstract void WriteShort(ushort value);
    public abstract void WriteInt(uint value);
    public abstract void WriteString(string value);
    public virtual void WritePacketLength();
}
```

### Protocol-Specific Implementations

```csharp
// UDP packet with object pooling
public class GSUDPPacketOut : PacketOut, IPooledObject<GSUDPPacketOut>
{
    private static readonly ObjectPool<GSUDPPacketOut> _pool = new();
    private byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public static GSUDPPacketOut GetFromPool()
    {
        var packet = _pool.Get();
        packet.Reset();
        return packet;
    }
    
    public void ReturnToPool()
    {
        Reset();
        _pool.Return(this);
    }
    
    public void Reset()
    {
        _position = 0;
        IsSizeSet = false;
        IssuedTimestamp = 0;
    }
}

// TCP packet for reliable delivery
public class GSTCPPacketOut : PacketOut, IPooledObject<GSTCPPacketOut>
{
    private readonly byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public override void WriteByte(byte value)
    {
        if (_position < _buffer.Length)
            _buffer[_position++] = value;
    }
    
    public override void WriteShort(ushort value)
    {
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value & 0xFF));
    }
}

// Incoming packet parser
public class GSPacketIn : PacketIn, IPooledObject<GSPacketIn>
{
    public const ushort HDR_SIZE = 12; // Header + checksum
    
    private ushort _code;      // Packet ID
    private ushort _parameter; // Packet parameter
    private ushort _psize;     // Packet size
    private ushort _sequence;  // Packet sequence
    private ushort _sessionID; // Session ID
    
    public ushort Code => _code;
    public ushort SessionID => _sessionID;
    public ushort PacketSize => (ushort)(_psize + HDR_SIZE);
    public ushort DataSize => _psize;
    
    public void Load(byte[] buffer, int offset, int count)
    {
        if (count < HDR_SIZE)
            throw new ArgumentException("Packet too small");
            
        // Parse header
        _psize = Marshal.ConvertToUInt16(buffer, offset);
        _sessionID = Marshal.ConvertToUInt16(buffer, offset + 2);
        _parameter = Marshal.ConvertToUInt16(buffer, offset + 4);
        _sequence = Marshal.ConvertToUInt16(buffer, offset + 6);
        _code = Marshal.ConvertToUInt16(buffer, offset + 8);
        
        // Load packet data
        SetLength(0);
        Write(buffer, offset + 10, count - HDR_SIZE);
        Seek(0, SeekOrigin.Begin);
    }
}
```

## Packet Processing Pipeline

### PacketProcessor Architecture

```csharp
public class PacketProcessor
{
    private const int SAVED_PACKETS_COUNT = 16;
    private readonly GameClient _client;
    private readonly IPacketHandler[] _packetHandlers = new IPacketHandler[256];
    private readonly PacketPreprocessing _packetPreprocessor = new();
    private readonly Queue<IPacket> _savedPackets = new(SAVED_PACKETS_COUNT);
    private readonly Lock _savedPacketsLock = new();
    
    // Packet queues for batching
    private readonly DrainArray<GSTCPPacketOut> _tcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpToTcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpPacketQueue = new();
    
    // Send buffer management
    private SocketAsyncEventArgs _tcpSendArgs;
    private SocketAsyncEventArgs _udpSendArgs;
    private int _tcpSendBufferPosition;
    private int _udpSendBufferPosition;
    private uint _udpCounter;
    
    public PacketProcessor(GameClient client)
    {
        _client = client;
        LoadPacketHandlers();
        GetAvailableTcpSendArgs();
        GetAvailableUdpSendArgs();
    }
}
```

### Packet Handler Registration

```csharp
private void LoadPacketHandlers()
{
    string version = "v168";
    
    lock (_loadPacketHandlersLock)
    {
        // Check cache first
        if (_cachedPacketHandlerSearchResults.TryGetValue(version, out var cachedHandlers))
        {
            _packetHandlers = cachedHandlers.Clone() as IPacketHandler[];
            return;
        }
        
        // Search assemblies for packet handlers
        _packetHandlers = new IPacketHandler[256];
        int count = SearchAndAddPacketHandlers(version, Assembly.GetAssembly(typeof(GameServer)), _packetHandlers);
        
        // Search script assemblies
        foreach (Assembly asm in ScriptMgr.Scripts)
            count += SearchAndAddPacketHandlers(version, asm, _packetHandlers);
            
        // Cache results
        _cachedPacketHandlerSearchResults[version] = _packetHandlers.Clone() as IPacketHandler[];
        
        log.Info($"Loaded {count} packet handlers for {version}");
    }
}

private int SearchAndAddPacketHandlers(string version, Assembly assembly, IPacketHandler[] packetHandlers)
{
    int count = 0;
    
    foreach (Type type in assembly.GetTypes())
    {
        if (!type.IsClass || type.IsAbstract || !typeof(IPacketHandler).IsAssignableFrom(type))
            continue;
            
        var packetHandlerAttributes = type.GetCustomAttributes(typeof(PacketHandlerAttribute), false);
        if (packetHandlerAttributes.Length == 0)
            continue;
            
        var attribute = (PacketHandlerAttribute)packetHandlerAttributes[0];
        
        // Version filtering logic here
        
        var handler = Activator.CreateInstance(type) as IPacketHandler;
        int packetCode = attribute.Code;
        
        if (packetCode >= 0 && packetCode < packetHandlers.Length)
        {
            packetHandlers[packetCode] = handler;
            count++;
        }
    }
    
    return count;
}
```

### Inbound Packet Processing

```csharp
public void ProcessInboundPacket(GSPacketIn packet)
{
    int code = packet.Code;
    SavePacket(packet);
    
    // Validate packet code
    if (code >= _packetHandlers.Length)
    {
        log.Error($"Packet code {code:X2} outside handler array bounds");
        LogInvalidPacket(packet);
        return;
    }
    
    IPacketHandler handler = _packetHandlers[code];
    if (handler == null)
        return;
        
    // Security preprocessing
    if (!_packetPreprocessor.CanProcessPacket(_client, packet))
    {
        log.Info($"Preprocessor blocked packet 0x{packet.Code:X2}");
        return;
    }
    
    try
    {
        long startTick = GameLoop.GetRealTime();
        handler.HandlePacket(_client, packet);
        long elapsed = GameLoop.GetRealTime() - startTick;
        
        // Performance monitoring
        if (elapsed > Diagnostics.LongTickThreshold)
        {
            log.Warn($"Long packet processing: 0x{packet.Code:X2} took {elapsed}ms for {_client.Player?.Name}");
        }
    }
    catch (Exception e)
    {
        log.Error($"Error processing packet 0x{packet.Code:X2} from {_client}: {e}");
    }
}

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
```

## Outbound Packet Management

### Packet Queuing System

```csharp
public void QueuePacket(GSTCPPacketOut packet)
{
    if (_client.ClientState is eClientState.Disconnected or eClientState.Linkdead)
        return;
        
    // Ensure packet size is set
    if (!packet.IsSizeSet)
        packet.WritePacketLength();
        
    _tcpPacketQueue.Add(packet);
}

public void QueuePacket(GSUDPPacketOut packet, bool forced = false)
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

### Batch Packet Sending

```csharp
public void SendPendingPackets()
{
    // Process TCP packets
    _tcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendTcpPacketToTcpSendBuffer(packet), this);
    
    // Process UDP-to-TCP fallback packets
    _udpToTcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToTcpSendBuffer(packet), this);
    
    SendTcp();
    
    // Process UDP packets
    _udpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToUdpSendBuffer(packet), this);
    
    SendUdp();
}

private void AppendTcpPacketToTcpSendBuffer(GSTCPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
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

### UDP Packet Handling

```csharp
private void AppendUdpPacketToUdpSendBuffer(GSUDPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
    if (!ValidatePacketSize(packetBuffer, packetSize))
        return;
        
    int nextPosition = _udpSendBufferPosition + packetSize;
    
    if (nextPosition > _udpSendArgs.Buffer.Length)
    {
        if (!SendUdp())
            return;
            
        if (_udpSendBufferPosition + packetSize > _udpSendArgs.Buffer.Length)
            return;
            
        nextPosition = packetSize;
    }
    
    // Copy packet and add UDP counter
    Buffer.BlockCopy(packetBuffer, 0, _udpSendArgs.Buffer, _udpSendBufferPosition, packetSize);
    _udpCounter++; // Let it overflow
    _udpSendArgs.Buffer[_udpSendBufferPosition + 2] = (byte)(_udpCounter >> 8);
    _udpSendArgs.Buffer[_udpSendBufferPosition + 3] = (byte)_udpCounter;
    _udpSendBufferPosition = nextPosition;
    SavePacket(packet);
}
```

## Security and Validation

### Packet Preprocessing

```csharp
public class PacketPreprocessing
{
    private readonly Dictionary<int, int> _packetIdToPreprocessMap = new();
    private readonly Dictionary<int, Func<GameClient, GSPacketIn, bool>> _preprocessors = new();
    
    public PacketPreprocessing()
    {
        RegisterPreprocessors((int)eClientStatus.LoggedIn, 
            (client, packet) => client.Account != null);
        RegisterPreprocessors((int)eClientStatus.PlayerInGame, 
            (client, packet) => client.Player != null);
    }
    
    public bool CanProcessPacket(GameClient client, GSPacketIn packet)
    {
        if (!_packetIdToPreprocessMap.TryGetValue(packet.Code, out int preprocessorId))
            return true; // No preprocessor = allow
            
        if (_preprocessors.TryGetValue(preprocessorId, out var preprocessor))
            return preprocessor(client, packet);
            
        return true;
    }
    
    private void RegisterPreprocessors(int preprocessorId, Func<GameClient, GSPacketIn, bool> func)
    {
        _preprocessors[preprocessorId] = func;
    }
}
```

### Packet Validation

```csharp
private static bool ValidatePacketIssuedTimestamp<T>(T packet) where T : PacketOut, IPooledObject<T>
{
    if (!packet.IsValidForTick())
    {
        if (packet.IssuedTimestamp != 0)
        {
            log.Error($"Packet not issued in current game loop time (Code: 0x{packet.Code:X2}) " +
                     $"(Issued: {packet.IssuedTimestamp}) (Current: {GameLoop.GameLoopTime})");
            return false;
        }
        
        log.Debug($"Packet issued outside game loop (Code: 0x{packet.Code:X2})");
    }
    
    return true;
}

private bool ValidatePacketSize(byte[] packetBuffer, int packetSize)
{
    if (packetSize <= 2048)
        return true;
        
    log.Error($"Discarding oversized packet. Code: 0x{packetBuffer[2]:X2}, " +
             $"Account: {_client.Account?.Name ?? _client.TcpEndpointAddress}, Size: {packetSize}");
             
    _client.Out.SendMessage($"Oversized packet detected (code: 0x{packetBuffer[2]:X2}) (size: {packetSize}). " +
                           "Please report this issue!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
    return false;
}
```

### Checksum Verification

```csharp
public static int CalculateChecksum(byte[] data, int start, int count)
{
    ushort checksum = 0;
    ushort val1 = 0;
    ushort val2 = 0;
    
    int dataPtr = start;
    int len = count - 2; // Exclude checksum bytes
    
    for (int i = 0; i < len; i += 2)
    {
        if (i + 1 < len)
        {
            val1 = (ushort)((data[dataPtr] << 8) | data[dataPtr + 1]);
        }
        else
        {
            val1 = (ushort)(data[dataPtr] << 8);
        }
        
        val2 = (ushort)(val2 + val1);
        if ((val2 & 0x80000000) != 0)
        {
            val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
        }
        
        dataPtr += 2;
    }
    
    val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
    checksum = (ushort)(~val2);
    
    return checksum;
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

## Performance Optimizations

### Object Pooling

```csharp
public static class PacketPool<T> where T : class, IPooledObject<T>, new()
{
    private static readonly ConcurrentQueue<T> _pool = new();
    private static readonly int MAX_POOL_SIZE = 1000;
    private static int _poolSize = 0;
    
    public static T Get()
    {
        if (_pool.TryDequeue(out var packet))
        {
            Interlocked.Decrement(ref _poolSize);
            return packet;
        }
        return new T();
    }
    
    public static void Return(T packet)
    {
        if (_poolSize < MAX_POOL_SIZE)
        {
            packet.Reset();
            _pool.Enqueue(packet);
            Interlocked.Increment(ref _poolSize);
        }
    }
}
```

### Asynchronous Socket Operations

```csharp
private void GetAvailableTcpSendArgs()
{
    if (!_tcpSendArgsPool.TryDequeue(out _tcpSendArgs))
    {
        _tcpSendArgs = new SocketAsyncEventArgs();
        _tcpSendArgs.SetBuffer(new byte[8192], 0, 8192);
        _tcpSendArgs.Completed += OnTcpSendCompleted;
    }
    
    _tcpSendBufferPosition = 0;
}

private void OnTcpSendCompleted(object sender, SocketAsyncEventArgs e)
{
    try
    {
        if (e.SocketError == SocketError.Success)
        {
            // Return to pool for reuse
            _tcpSendArgsPool.Enqueue(e);
        }
        else
        {
            log.Debug($"TCP send error: {e.SocketError}");
            _client.Disconnect();
        }
    }
    catch (Exception ex)
    {
        log.Error($"Error in TCP send completion: {ex}");
    }
}
```

## Protocol Versioning

### Version Detection

```csharp
private bool CheckVersion()
{
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

### Packet Encoding

```csharp
public interface IPacketEncoding
{
    void EncodePacket(PacketOut packet);
    void DecodePacket(PacketIn packet);
}

public class PacketEncoding168 : IPacketEncoding
{
    public void EncodePacket(PacketOut packet)
    {
        // XOR encoding with version-specific key
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
    
    public void DecodePacket(PacketIn packet)
    {
        // Reverse XOR encoding
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
}
```

## Error Handling and Recovery

### Connection Recovery

```csharp
private bool SendTcp()
{
    if (!_client.Socket.Connected)
        return false;
        
    try
    {
        if (_tcpSendBufferPosition > 0)
        {
            _tcpSendArgs.SetBuffer(0, _tcpSendBufferPosition);
            
            if (_client.SendAsync(_tcpSendArgs))
                GetAvailableTcpSendArgs();
                
            _tcpSendBufferPosition = 0;
        }
        
        return true;
    }
    catch (ObjectDisposedException) { }
    catch (SocketException e)
    {
        log.Debug($"Socket exception on TCP send (Client: {_client}) (Code: {e.SocketErrorCode})");
    }
    catch (Exception e)
    {
        log.Error($"Unhandled exception on TCP send (Client: {_client}): {e}");
    }
    
    return false;
}
```

### Packet Loss Handling

```csharp
public class PacketReliability
{
    private readonly Dictionary<ushort, PendingPacket> _pendingPackets = new();
    private readonly Timer _retransmissionTimer;
    
    public void SendReliablePacket(GSUDPPacketOut packet)
    {
        packet.Sequence = GetNextSequence();
        _pendingPackets[packet.Sequence] = new PendingPacket
        {
            Packet = packet,
            SendTime = GameLoop.GameLoopTime,
            RetryCount = 0
        };
        
        SendPacket(packet);
    }
    
    public void AcknowledgePacket(ushort sequence)
    {
        _pendingPackets.Remove(sequence);
    }
    
    private void CheckRetransmissions()
    {
        long currentTime = GameLoop.GameLoopTime;
        
        foreach (var kvp in _pendingPackets.ToArray())
        {
            var pending = kvp.Value;
            
            if (currentTime - pending.SendTime > RETRANSMISSION_TIMEOUT)
            {
                if (pending.RetryCount < MAX_RETRIES)
                {
                    pending.RetryCount++;
                    pending.SendTime = currentTime;
                    SendPacket(pending.Packet);
                }
                else
                {
                    // Give up and disconnect
                    _pendingPackets.Remove(kvp.Key);
                    HandlePacketLoss();
                }
            }
        }
    }
}
```

## Configuration

### Network Settings

```csharp
[ServerProperty("network", "tcp_send_buffer_size", "TCP send buffer size", 8192)]
public static int TCP_SEND_BUFFER_SIZE;

[ServerProperty("network", "udp_send_buffer_size", "UDP send buffer size", 4096)]
public static int UDP_SEND_BUFFER_SIZE;

[ServerProperty("network", "packet_pool_size", "Maximum packet pool size", 1000)]
public static int PACKET_POOL_SIZE;

[ServerProperty("network", "save_packets", "Save packets for debugging", false)]
public static bool SAVE_PACKETS;

[ServerProperty("network", "packet_timeout", "Packet processing timeout (ms)", 5000)]
public static int PACKET_TIMEOUT;
```

### Protocol Constants

```csharp
public static class ProtocolConstants
{
    public const int MAX_PACKET_SIZE = 2048;
    public const int MIN_PACKET_SIZE = 12;
    public const int UDP_PING_TIMEOUT = 70000; // 70 seconds
    public const int RETRANSMISSION_TIMEOUT = 1000; // 1 second
    public const int MAX_RETRIES = 3;
    public const int CHECKSUM_SIZE = 2;
}
```

## Integration Points

### Game Loop Integration

```csharp
public static void TickNetworking()
{
    // Process all client packet queues
    ClientService.ProcessAllClientPackets();
    
    // Handle UDP packet reception
    ProcessPendingUdpPackets();
    
    // Clean up disconnected clients
    CleanupDisconnectedClients();
}
```

### Event System Integration

```csharp
public class NetworkEvents
{
    public static readonly GameEventMgr.EventType PacketReceived = "PacketReceived";
    public static readonly GameEventMgr.EventType PacketSent = "PacketSent";
    public static readonly GameEventMgr.EventType ClientConnected = "ClientConnected";
    public static readonly GameEventMgr.EventType ClientDisconnected = "ClientDisconnected";
}

// Usage
GameEventMgr.AddHandler(NetworkEvents.PacketReceived, OnPacketReceived);
```

## Performance Metrics

### Target Performance

- **Packet Processing**: <1ms per packet
- **Memory Allocation**: <100 bytes per packet (pooled)
- **Throughput**: 1000+ packets/second per client
- **Latency**: <50ms round trip time
- **Reliability**: >99.9% packet delivery

### Monitoring

```csharp
public static class NetworkMetrics
{
    public static long PacketsReceived { get; private set; }
    public static long PacketsSent { get; private set; }
    public static long BytesReceived { get; private set; }
    public static long BytesSent { get; private set; }
    public static long PacketErrors { get; private set; }
    
    public static void LogMetrics()
    {
        log.Info($"Network Stats: RX={PacketsReceived} TX={PacketsSent} " +
                $"Bytes RX={BytesReceived} TX={BytesSent} Errors={PacketErrors}");
    }
}
```

## Implementation Status

**Completed**:
- ✅ Core packet infrastructure
- ✅ TCP/UDP dual protocol support
- ✅ Object pooling for performance
- ✅ Packet validation and security
- ✅ Asynchronous socket operations
- ✅ Protocol versioning
- ✅ Error handling and recovery

**In Progress**:
- 🔄 Advanced packet compression
- 🔄 Encryption layer
- 🔄 Quality of service monitoring

**Planned**:
- ⏳ IPv6 support
- ⏳ WebSocket protocol support
- ⏳ Advanced anti-cheat integration

## References

- **Core Implementation**: `GameServer/packets/Server/PacketProcessor.cs`
- **Packet Base Classes**: `CoreBase/Network/PacketIn.cs`, `CoreBase/Network/PacketOut.cs`
- **Game Packets**: `GameServer/packets/Client/GSPacketIn.cs`
- **Handler Framework**: `GameServer/packets/Server/IPacketHandler.cs`

# Network Protocol System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

The Network Protocol System provides high-performance, bidirectional client-server communication for OpenDAoC. It manages packet encoding/decoding, reliability, security, and protocol versioning through a sophisticated packet handler architecture with support for both TCP and UDP protocols.

## Core Architecture

### Packet Infrastructure

```csharp
// Base packet interfaces
public interface IPacket
{
    byte[] Buffer { get; }
    int Offset { get; }
    int Size { get; }
}

public abstract class PacketIn : MemoryStream, IPacket
{
    public abstract byte ReadByte();
    public abstract ushort ReadShort();
    public abstract uint ReadInt();
    public abstract string ReadString(int maxLength);
    public virtual void Skip(long bytes);
}

public abstract class PacketOut : MemoryStream, IPacket
{
    public abstract void WriteByte(byte value);
    public abstract void WriteShort(ushort value);
    public abstract void WriteInt(uint value);
    public abstract void WriteString(string value);
    public virtual void WritePacketLength();
}
```

### Protocol-Specific Implementations

```csharp
// UDP packet with object pooling
public class GSUDPPacketOut : PacketOut, IPooledObject<GSUDPPacketOut>
{
    private static readonly ObjectPool<GSUDPPacketOut> _pool = new();
    private byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public static GSUDPPacketOut GetFromPool()
    {
        var packet = _pool.Get();
        packet.Reset();
        return packet;
    }
    
    public void ReturnToPool()
    {
        Reset();
        _pool.Return(this);
    }
    
    public void Reset()
    {
        _position = 0;
        IsSizeSet = false;
        IssuedTimestamp = 0;
    }
}

// TCP packet for reliable delivery
public class GSTCPPacketOut : PacketOut, IPooledObject<GSTCPPacketOut>
{
    private readonly byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public override void WriteByte(byte value)
    {
        if (_position < _buffer.Length)
            _buffer[_position++] = value;
    }
    
    public override void WriteShort(ushort value)
    {
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value & 0xFF));
    }
}

// Incoming packet parser
public class GSPacketIn : PacketIn, IPooledObject<GSPacketIn>
{
    public const ushort HDR_SIZE = 12; // Header + checksum
    
    private ushort _code;      // Packet ID
    private ushort _parameter; // Packet parameter
    private ushort _psize;     // Packet size
    private ushort _sequence;  // Packet sequence
    private ushort _sessionID; // Session ID
    
    public ushort Code => _code;
    public ushort SessionID => _sessionID;
    public ushort PacketSize => (ushort)(_psize + HDR_SIZE);
    public ushort DataSize => _psize;
    
    public void Load(byte[] buffer, int offset, int count)
    {
        if (count < HDR_SIZE)
            throw new ArgumentException("Packet too small");
            
        // Parse header
        _psize = Marshal.ConvertToUInt16(buffer, offset);
        _sessionID = Marshal.ConvertToUInt16(buffer, offset + 2);
        _parameter = Marshal.ConvertToUInt16(buffer, offset + 4);
        _sequence = Marshal.ConvertToUInt16(buffer, offset + 6);
        _code = Marshal.ConvertToUInt16(buffer, offset + 8);
        
        // Load packet data
        SetLength(0);
        Write(buffer, offset + 10, count - HDR_SIZE);
        Seek(0, SeekOrigin.Begin);
    }
}
```

## Packet Processing Pipeline

### PacketProcessor Architecture

```csharp
public class PacketProcessor
{
    private const int SAVED_PACKETS_COUNT = 16;
    private readonly GameClient _client;
    private readonly IPacketHandler[] _packetHandlers = new IPacketHandler[256];
    private readonly PacketPreprocessing _packetPreprocessor = new();
    private readonly Queue<IPacket> _savedPackets = new(SAVED_PACKETS_COUNT);
    private readonly Lock _savedPacketsLock = new();
    
    // Packet queues for batching
    private readonly DrainArray<GSTCPPacketOut> _tcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpToTcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpPacketQueue = new();
    
    // Send buffer management
    private SocketAsyncEventArgs _tcpSendArgs;
    private SocketAsyncEventArgs _udpSendArgs;
    private int _tcpSendBufferPosition;
    private int _udpSendBufferPosition;
    private uint _udpCounter;
    
    public PacketProcessor(GameClient client)
    {
        _client = client;
        LoadPacketHandlers();
        GetAvailableTcpSendArgs();
        GetAvailableUdpSendArgs();
    }
}
```

### Packet Handler Registration

```csharp
private void LoadPacketHandlers()
{
    string version = "v168";
    
    lock (_loadPacketHandlersLock)
    {
        // Check cache first
        if (_cachedPacketHandlerSearchResults.TryGetValue(version, out var cachedHandlers))
        {
            _packetHandlers = cachedHandlers.Clone() as IPacketHandler[];
            return;
        }
        
        // Search assemblies for packet handlers
        _packetHandlers = new IPacketHandler[256];
        int count = SearchAndAddPacketHandlers(version, Assembly.GetAssembly(typeof(GameServer)), _packetHandlers);
        
        // Search script assemblies
        foreach (Assembly asm in ScriptMgr.Scripts)
            count += SearchAndAddPacketHandlers(version, asm, _packetHandlers);
            
        // Cache results
        _cachedPacketHandlerSearchResults[version] = _packetHandlers.Clone() as IPacketHandler[];
        
        log.Info($"Loaded {count} packet handlers for {version}");
    }
}

private int SearchAndAddPacketHandlers(string version, Assembly assembly, IPacketHandler[] packetHandlers)
{
    int count = 0;
    
    foreach (Type type in assembly.GetTypes())
    {
        if (!type.IsClass || type.IsAbstract || !typeof(IPacketHandler).IsAssignableFrom(type))
            continue;
            
        var packetHandlerAttributes = type.GetCustomAttributes(typeof(PacketHandlerAttribute), false);
        if (packetHandlerAttributes.Length == 0)
            continue;
            
        var attribute = (PacketHandlerAttribute)packetHandlerAttributes[0];
        
        // Version filtering logic here
        
        var handler = Activator.CreateInstance(type) as IPacketHandler;
        int packetCode = attribute.Code;
        
        if (packetCode >= 0 && packetCode < packetHandlers.Length)
        {
            packetHandlers[packetCode] = handler;
            count++;
        }
    }
    
    return count;
}
```

### Inbound Packet Processing

```csharp
public void ProcessInboundPacket(GSPacketIn packet)
{
    int code = packet.Code;
    SavePacket(packet);
    
    // Validate packet code
    if (code >= _packetHandlers.Length)
    {
        log.Error($"Packet code {code:X2} outside handler array bounds");
        LogInvalidPacket(packet);
        return;
    }
    
    IPacketHandler handler = _packetHandlers[code];
    if (handler == null)
        return;
        
    // Security preprocessing
    if (!_packetPreprocessor.CanProcessPacket(_client, packet))
    {
        log.Info($"Preprocessor blocked packet 0x{packet.Code:X2}");
        return;
    }
    
    try
    {
        long startTick = GameLoop.GetRealTime();
        handler.HandlePacket(_client, packet);
        long elapsed = GameLoop.GetRealTime() - startTick;
        
        // Performance monitoring
        if (elapsed > Diagnostics.LongTickThreshold)
        {
            log.Warn($"Long packet processing: 0x{packet.Code:X2} took {elapsed}ms for {_client.Player?.Name}");
        }
    }
    catch (Exception e)
    {
        log.Error($"Error processing packet 0x{packet.Code:X2} from {_client}: {e}");
    }
}

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
```

## Outbound Packet Management

### Packet Queuing System

```csharp
public void QueuePacket(GSTCPPacketOut packet)
{
    if (_client.ClientState is eClientState.Disconnected or eClientState.Linkdead)
        return;
        
    // Ensure packet size is set
    if (!packet.IsSizeSet)
        packet.WritePacketLength();
        
    _tcpPacketQueue.Add(packet);
}

public void QueuePacket(GSUDPPacketOut packet, bool forced = false)
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

### Batch Packet Sending

```csharp
public void SendPendingPackets()
{
    // Process TCP packets
    _tcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendTcpPacketToTcpSendBuffer(packet), this);
    
    // Process UDP-to-TCP fallback packets
    _udpToTcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToTcpSendBuffer(packet), this);
    
    SendTcp();
    
    // Process UDP packets
    _udpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToUdpSendBuffer(packet), this);
    
    SendUdp();
}

private void AppendTcpPacketToTcpSendBuffer(GSTCPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
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

### UDP Packet Handling

```csharp
private void AppendUdpPacketToUdpSendBuffer(GSUDPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
    if (!ValidatePacketSize(packetBuffer, packetSize))
        return;
        
    int nextPosition = _udpSendBufferPosition + packetSize;
    
    if (nextPosition > _udpSendArgs.Buffer.Length)
    {
        if (!SendUdp())
            return;
            
        if (_udpSendBufferPosition + packetSize > _udpSendArgs.Buffer.Length)
            return;
            
        nextPosition = packetSize;
    }
    
    // Copy packet and add UDP counter
    Buffer.BlockCopy(packetBuffer, 0, _udpSendArgs.Buffer, _udpSendBufferPosition, packetSize);
    _udpCounter++; // Let it overflow
    _udpSendArgs.Buffer[_udpSendBufferPosition + 2] = (byte)(_udpCounter >> 8);
    _udpSendArgs.Buffer[_udpSendBufferPosition + 3] = (byte)_udpCounter;
    _udpSendBufferPosition = nextPosition;
    SavePacket(packet);
}
```

## Security and Validation

### Packet Preprocessing

```csharp
public class PacketPreprocessing
{
    private readonly Dictionary<int, int> _packetIdToPreprocessMap = new();
    private readonly Dictionary<int, Func<GameClient, GSPacketIn, bool>> _preprocessors = new();
    
    public PacketPreprocessing()
    {
        RegisterPreprocessors((int)eClientStatus.LoggedIn, 
            (client, packet) => client.Account != null);
        RegisterPreprocessors((int)eClientStatus.PlayerInGame, 
            (client, packet) => client.Player != null);
    }
    
    public bool CanProcessPacket(GameClient client, GSPacketIn packet)
    {
        if (!_packetIdToPreprocessMap.TryGetValue(packet.Code, out int preprocessorId))
            return true; // No preprocessor = allow
            
        if (_preprocessors.TryGetValue(preprocessorId, out var preprocessor))
            return preprocessor(client, packet);
            
        return true;
    }
    
    private void RegisterPreprocessors(int preprocessorId, Func<GameClient, GSPacketIn, bool> func)
    {
        _preprocessors[preprocessorId] = func;
    }
}
```

### Packet Validation

```csharp
private static bool ValidatePacketIssuedTimestamp<T>(T packet) where T : PacketOut, IPooledObject<T>
{
    if (!packet.IsValidForTick())
    {
        if (packet.IssuedTimestamp != 0)
        {
            log.Error($"Packet not issued in current game loop time (Code: 0x{packet.Code:X2}) " +
                     $"(Issued: {packet.IssuedTimestamp}) (Current: {GameLoop.GameLoopTime})");
            return false;
        }
        
        log.Debug($"Packet issued outside game loop (Code: 0x{packet.Code:X2})");
    }
    
    return true;
}

private bool ValidatePacketSize(byte[] packetBuffer, int packetSize)
{
    if (packetSize <= 2048)
        return true;
        
    log.Error($"Discarding oversized packet. Code: 0x{packetBuffer[2]:X2}, " +
             $"Account: {_client.Account?.Name ?? _client.TcpEndpointAddress}, Size: {packetSize}");
             
    _client.Out.SendMessage($"Oversized packet detected (code: 0x{packetBuffer[2]:X2}) (size: {packetSize}). " +
                           "Please report this issue!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
    return false;
}
```

### Checksum Verification

```csharp
public static int CalculateChecksum(byte[] data, int start, int count)
{
    ushort checksum = 0;
    ushort val1 = 0;
    ushort val2 = 0;
    
    int dataPtr = start;
    int len = count - 2; // Exclude checksum bytes
    
    for (int i = 0; i < len; i += 2)
    {
        if (i + 1 < len)
        {
            val1 = (ushort)((data[dataPtr] << 8) | data[dataPtr + 1]);
        }
        else
        {
            val1 = (ushort)(data[dataPtr] << 8);
        }
        
        val2 = (ushort)(val2 + val1);
        if ((val2 & 0x80000000) != 0)
        {
            val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
        }
        
        dataPtr += 2;
    }
    
    val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
    checksum = (ushort)(~val2);
    
    return checksum;
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

## Performance Optimizations

### Object Pooling

```csharp
public static class PacketPool<T> where T : class, IPooledObject<T>, new()
{
    private static readonly ConcurrentQueue<T> _pool = new();
    private static readonly int MAX_POOL_SIZE = 1000;
    private static int _poolSize = 0;
    
    public static T Get()
    {
        if (_pool.TryDequeue(out var packet))
        {
            Interlocked.Decrement(ref _poolSize);
            return packet;
        }
        return new T();
    }
    
    public static void Return(T packet)
    {
        if (_poolSize < MAX_POOL_SIZE)
        {
            packet.Reset();
            _pool.Enqueue(packet);
            Interlocked.Increment(ref _poolSize);
        }
    }
}
```

### Asynchronous Socket Operations

```csharp
private void GetAvailableTcpSendArgs()
{
    if (!_tcpSendArgsPool.TryDequeue(out _tcpSendArgs))
    {
        _tcpSendArgs = new SocketAsyncEventArgs();
        _tcpSendArgs.SetBuffer(new byte[8192], 0, 8192);
        _tcpSendArgs.Completed += OnTcpSendCompleted;
    }
    
    _tcpSendBufferPosition = 0;
}

private void OnTcpSendCompleted(object sender, SocketAsyncEventArgs e)
{
    try
    {
        if (e.SocketError == SocketError.Success)
        {
            // Return to pool for reuse
            _tcpSendArgsPool.Enqueue(e);
        }
        else
        {
            log.Debug($"TCP send error: {e.SocketError}");
            _client.Disconnect();
        }
    }
    catch (Exception ex)
    {
        log.Error($"Error in TCP send completion: {ex}");
    }
}
```

## Protocol Versioning

### Version Detection

```csharp
private bool CheckVersion()
{
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

### Packet Encoding

```csharp
public interface IPacketEncoding
{
    void EncodePacket(PacketOut packet);
    void DecodePacket(PacketIn packet);
}

public class PacketEncoding168 : IPacketEncoding
{
    public void EncodePacket(PacketOut packet)
    {
        // XOR encoding with version-specific key
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
    
    public void DecodePacket(PacketIn packet)
    {
        // Reverse XOR encoding
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
}
```

## Error Handling and Recovery

### Connection Recovery

```csharp
private bool SendTcp()
{
    if (!_client.Socket.Connected)
        return false;
        
    try
    {
        if (_tcpSendBufferPosition > 0)
        {
            _tcpSendArgs.SetBuffer(0, _tcpSendBufferPosition);
            
            if (_client.SendAsync(_tcpSendArgs))
                GetAvailableTcpSendArgs();
                
            _tcpSendBufferPosition = 0;
        }
        
        return true;
    }
    catch (ObjectDisposedException) { }
    catch (SocketException e)
    {
        log.Debug($"Socket exception on TCP send (Client: {_client}) (Code: {e.SocketErrorCode})");
    }
    catch (Exception e)
    {
        log.Error($"Unhandled exception on TCP send (Client: {_client}): {e}");
    }
    
    return false;
}
```

### Packet Loss Handling

```csharp
public class PacketReliability
{
    private readonly Dictionary<ushort, PendingPacket> _pendingPackets = new();
    private readonly Timer _retransmissionTimer;
    
    public void SendReliablePacket(GSUDPPacketOut packet)
    {
        packet.Sequence = GetNextSequence();
        _pendingPackets[packet.Sequence] = new PendingPacket
        {
            Packet = packet,
            SendTime = GameLoop.GameLoopTime,
            RetryCount = 0
        };
        
        SendPacket(packet);
    }
    
    public void AcknowledgePacket(ushort sequence)
    {
        _pendingPackets.Remove(sequence);
    }
    
    private void CheckRetransmissions()
    {
        long currentTime = GameLoop.GameLoopTime;
        
        foreach (var kvp in _pendingPackets.ToArray())
        {
            var pending = kvp.Value;
            
            if (currentTime - pending.SendTime > RETRANSMISSION_TIMEOUT)
            {
                if (pending.RetryCount < MAX_RETRIES)
                {
                    pending.RetryCount++;
                    pending.SendTime = currentTime;
                    SendPacket(pending.Packet);
                }
                else
                {
                    // Give up and disconnect
                    _pendingPackets.Remove(kvp.Key);
                    HandlePacketLoss();
                }
            }
        }
    }
}
```

## Configuration

### Network Settings

```csharp
[ServerProperty("network", "tcp_send_buffer_size", "TCP send buffer size", 8192)]
public static int TCP_SEND_BUFFER_SIZE;

[ServerProperty("network", "udp_send_buffer_size", "UDP send buffer size", 4096)]
public static int UDP_SEND_BUFFER_SIZE;

[ServerProperty("network", "packet_pool_size", "Maximum packet pool size", 1000)]
public static int PACKET_POOL_SIZE;

[ServerProperty("network", "save_packets", "Save packets for debugging", false)]
public static bool SAVE_PACKETS;

[ServerProperty("network", "packet_timeout", "Packet processing timeout (ms)", 5000)]
public static int PACKET_TIMEOUT;
```

### Protocol Constants

```csharp
public static class ProtocolConstants
{
    public const int MAX_PACKET_SIZE = 2048;
    public const int MIN_PACKET_SIZE = 12;
    public const int UDP_PING_TIMEOUT = 70000; // 70 seconds
    public const int RETRANSMISSION_TIMEOUT = 1000; // 1 second
    public const int MAX_RETRIES = 3;
    public const int CHECKSUM_SIZE = 2;
}
```

## Integration Points

### Game Loop Integration

```csharp
public static void TickNetworking()
{
    // Process all client packet queues
    ClientService.ProcessAllClientPackets();
    
    // Handle UDP packet reception
    ProcessPendingUdpPackets();
    
    // Clean up disconnected clients
    CleanupDisconnectedClients();
}
```

### Event System Integration

```csharp
public class NetworkEvents
{
    public static readonly GameEventMgr.EventType PacketReceived = "PacketReceived";
    public static readonly GameEventMgr.EventType PacketSent = "PacketSent";
    public static readonly GameEventMgr.EventType ClientConnected = "ClientConnected";
    public static readonly GameEventMgr.EventType ClientDisconnected = "ClientDisconnected";
}

// Usage
GameEventMgr.AddHandler(NetworkEvents.PacketReceived, OnPacketReceived);
```

## Performance Metrics

### Target Performance

- **Packet Processing**: <1ms per packet
- **Memory Allocation**: <100 bytes per packet (pooled)
- **Throughput**: 1000+ packets/second per client
- **Latency**: <50ms round trip time
- **Reliability**: >99.9% packet delivery

### Monitoring

```csharp
public static class NetworkMetrics
{
    public static long PacketsReceived { get; private set; }
    public static long PacketsSent { get; private set; }
    public static long BytesReceived { get; private set; }
    public static long BytesSent { get; private set; }
    public static long PacketErrors { get; private set; }
    
    public static void LogMetrics()
    {
        log.Info($"Network Stats: RX={PacketsReceived} TX={PacketsSent} " +
                $"Bytes RX={BytesReceived} TX={BytesSent} Errors={PacketErrors}");
    }
}
```

## Implementation Status

**Completed**:
- ✅ Core packet infrastructure
- ✅ TCP/UDP dual protocol support
- ✅ Object pooling for performance
- ✅ Packet validation and security
- ✅ Asynchronous socket operations
- ✅ Protocol versioning
- ✅ Error handling and recovery

**In Progress**:
- 🔄 Advanced packet compression
- 🔄 Encryption layer
- 🔄 Quality of service monitoring

**Planned**:
- ⏳ IPv6 support
- ⏳ WebSocket protocol support
- ⏳ Advanced anti-cheat integration

## References

- **Core Implementation**: `GameServer/packets/Server/PacketProcessor.cs`
- **Packet Base Classes**: `CoreBase/Network/PacketIn.cs`, `CoreBase/Network/PacketOut.cs`
- **Game Packets**: `GameServer/packets/Client/GSPacketIn.cs`
- **Handler Framework**: `GameServer/packets/Server/IPacketHandler.cs`

# Network Protocol System

**Document Status**: Complete  
**Version**: 1.0  
**Last Updated**: 2025-01-20  

## Overview

The Network Protocol System provides high-performance, bidirectional client-server communication for OpenDAoC. It manages packet encoding/decoding, reliability, security, and protocol versioning through a sophisticated packet handler architecture with support for both TCP and UDP protocols.

## Core Architecture

### Packet Infrastructure

```csharp
// Base packet interfaces
public interface IPacket
{
    byte[] Buffer { get; }
    int Offset { get; }
    int Size { get; }
}

public abstract class PacketIn : MemoryStream, IPacket
{
    public abstract byte ReadByte();
    public abstract ushort ReadShort();
    public abstract uint ReadInt();
    public abstract string ReadString(int maxLength);
    public virtual void Skip(long bytes);
}

public abstract class PacketOut : MemoryStream, IPacket
{
    public abstract void WriteByte(byte value);
    public abstract void WriteShort(ushort value);
    public abstract void WriteInt(uint value);
    public abstract void WriteString(string value);
    public virtual void WritePacketLength();
}
```

### Protocol-Specific Implementations

```csharp
// UDP packet with object pooling
public class GSUDPPacketOut : PacketOut, IPooledObject<GSUDPPacketOut>
{
    private static readonly ObjectPool<GSUDPPacketOut> _pool = new();
    private byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public static GSUDPPacketOut GetFromPool()
    {
        var packet = _pool.Get();
        packet.Reset();
        return packet;
    }
    
    public void ReturnToPool()
    {
        Reset();
        _pool.Return(this);
    }
    
    public void Reset()
    {
        _position = 0;
        IsSizeSet = false;
        IssuedTimestamp = 0;
    }
}

// TCP packet for reliable delivery
public class GSTCPPacketOut : PacketOut, IPooledObject<GSTCPPacketOut>
{
    private readonly byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public override void WriteByte(byte value)
    {
        if (_position < _buffer.Length)
            _buffer[_position++] = value;
    }
    
    public override void WriteShort(ushort value)
    {
        WriteByte((byte)(value >> 8));
        WriteByte((byte)(value & 0xFF));
    }
}

// Incoming packet parser
public class GSPacketIn : PacketIn, IPooledObject<GSPacketIn>
{
    public const ushort HDR_SIZE = 12; // Header + checksum
    
    private ushort _code;      // Packet ID
    private ushort _parameter; // Packet parameter
    private ushort _psize;     // Packet size
    private ushort _sequence;  // Packet sequence
    private ushort _sessionID; // Session ID
    
    public ushort Code => _code;
    public ushort SessionID => _sessionID;
    public ushort PacketSize => (ushort)(_psize + HDR_SIZE);
    public ushort DataSize => _psize;
    
    public void Load(byte[] buffer, int offset, int count)
    {
        if (count < HDR_SIZE)
            throw new ArgumentException("Packet too small");
            
        // Parse header
        _psize = Marshal.ConvertToUInt16(buffer, offset);
        _sessionID = Marshal.ConvertToUInt16(buffer, offset + 2);
        _parameter = Marshal.ConvertToUInt16(buffer, offset + 4);
        _sequence = Marshal.ConvertToUInt16(buffer, offset + 6);
        _code = Marshal.ConvertToUInt16(buffer, offset + 8);
        
        // Load packet data
        SetLength(0);
        Write(buffer, offset + 10, count - HDR_SIZE);
        Seek(0, SeekOrigin.Begin);
    }
}
```

## Packet Processing Pipeline

### PacketProcessor Architecture

```csharp
public class PacketProcessor
{
    private const int SAVED_PACKETS_COUNT = 16;
    private readonly GameClient _client;
    private readonly IPacketHandler[] _packetHandlers = new IPacketHandler[256];
    private readonly PacketPreprocessing _packetPreprocessor = new();
    private readonly Queue<IPacket> _savedPackets = new(SAVED_PACKETS_COUNT);
    private readonly Lock _savedPacketsLock = new();
    
    // Packet queues for batching
    private readonly DrainArray<GSTCPPacketOut> _tcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpToTcpPacketQueue = new();
    private readonly DrainArray<GSUDPPacketOut> _udpPacketQueue = new();
    
    // Send buffer management
    private SocketAsyncEventArgs _tcpSendArgs;
    private SocketAsyncEventArgs _udpSendArgs;
    private int _tcpSendBufferPosition;
    private int _udpSendBufferPosition;
    private uint _udpCounter;
    
    public PacketProcessor(GameClient client)
    {
        _client = client;
        LoadPacketHandlers();
        GetAvailableTcpSendArgs();
        GetAvailableUdpSendArgs();
    }
}
```

### Packet Handler Registration

```csharp
private void LoadPacketHandlers()
{
    string version = "v168";
    
    lock (_loadPacketHandlersLock)
    {
        // Check cache first
        if (_cachedPacketHandlerSearchResults.TryGetValue(version, out var cachedHandlers))
        {
            _packetHandlers = cachedHandlers.Clone() as IPacketHandler[];
            return;
        }
        
        // Search assemblies for packet handlers
        _packetHandlers = new IPacketHandler[256];
        int count = SearchAndAddPacketHandlers(version, Assembly.GetAssembly(typeof(GameServer)), _packetHandlers);
        
        // Search script assemblies
        foreach (Assembly asm in ScriptMgr.Scripts)
            count += SearchAndAddPacketHandlers(version, asm, _packetHandlers);
            
        // Cache results
        _cachedPacketHandlerSearchResults[version] = _packetHandlers.Clone() as IPacketHandler[];
        
        log.Info($"Loaded {count} packet handlers for {version}");
    }
}

private int SearchAndAddPacketHandlers(string version, Assembly assembly, IPacketHandler[] packetHandlers)
{
    int count = 0;
    
    foreach (Type type in assembly.GetTypes())
    {
        if (!type.IsClass || type.IsAbstract || !typeof(IPacketHandler).IsAssignableFrom(type))
            continue;
            
        var packetHandlerAttributes = type.GetCustomAttributes(typeof(PacketHandlerAttribute), false);
        if (packetHandlerAttributes.Length == 0)
            continue;
            
        var attribute = (PacketHandlerAttribute)packetHandlerAttributes[0];
        
        // Version filtering logic here
        
        var handler = Activator.CreateInstance(type) as IPacketHandler;
        int packetCode = attribute.Code;
        
        if (packetCode >= 0 && packetCode < packetHandlers.Length)
        {
            packetHandlers[packetCode] = handler;
            count++;
        }
    }
    
    return count;
}
```

### Inbound Packet Processing

```csharp
public void ProcessInboundPacket(GSPacketIn packet)
{
    int code = packet.Code;
    SavePacket(packet);
    
    // Validate packet code
    if (code >= _packetHandlers.Length)
    {
        log.Error($"Packet code {code:X2} outside handler array bounds");
        LogInvalidPacket(packet);
        return;
    }
    
    IPacketHandler handler = _packetHandlers[code];
    if (handler == null)
        return;
        
    // Security preprocessing
    if (!_packetPreprocessor.CanProcessPacket(_client, packet))
    {
        log.Info($"Preprocessor blocked packet 0x{packet.Code:X2}");
        return;
    }
    
    try
    {
        long startTick = GameLoop.GetRealTime();
        handler.HandlePacket(_client, packet);
        long elapsed = GameLoop.GetRealTime() - startTick;
        
        // Performance monitoring
        if (elapsed > Diagnostics.LongTickThreshold)
        {
            log.Warn($"Long packet processing: 0x{packet.Code:X2} took {elapsed}ms for {_client.Player?.Name}");
        }
    }
    catch (Exception e)
    {
        log.Error($"Error processing packet 0x{packet.Code:X2} from {_client}: {e}");
    }
}

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
```

## Outbound Packet Management

### Packet Queuing System

```csharp
public void QueuePacket(GSTCPPacketOut packet)
{
    if (_client.ClientState is eClientState.Disconnected or eClientState.Linkdead)
        return;
        
    // Ensure packet size is set
    if (!packet.IsSizeSet)
        packet.WritePacketLength();
        
    _tcpPacketQueue.Add(packet);
}

public void QueuePacket(GSUDPPacketOut packet, bool forced = false)
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

### Batch Packet Sending

```csharp
public void SendPendingPackets()
{
    // Process TCP packets
    _tcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendTcpPacketToTcpSendBuffer(packet), this);
    
    // Process UDP-to-TCP fallback packets
    _udpToTcpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToTcpSendBuffer(packet), this);
    
    SendTcp();
    
    // Process UDP packets
    _udpPacketQueue.DrainTo(static (packet, processor) => 
        processor.AppendUdpPacketToUdpSendBuffer(packet), this);
    
    SendUdp();
}

private void AppendTcpPacketToTcpSendBuffer(GSTCPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
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

### UDP Packet Handling

```csharp
private void AppendUdpPacketToUdpSendBuffer(GSUDPPacketOut packet)
{
    if (!ValidatePacketIssuedTimestamp(packet))
        return;
        
    byte[] packetBuffer = packet.GetBuffer();
    int packetSize = (int)packet.Length;
    
    if (!ValidatePacketSize(packetBuffer, packetSize))
        return;
        
    int nextPosition = _udpSendBufferPosition + packetSize;
    
    if (nextPosition > _udpSendArgs.Buffer.Length)
    {
        if (!SendUdp())
            return;
            
        if (_udpSendBufferPosition + packetSize > _udpSendArgs.Buffer.Length)
            return;
            
        nextPosition = packetSize;
    }
    
    // Copy packet and add UDP counter
    Buffer.BlockCopy(packetBuffer, 0, _udpSendArgs.Buffer, _udpSendBufferPosition, packetSize);
    _udpCounter++; // Let it overflow
    _udpSendArgs.Buffer[_udpSendBufferPosition + 2] = (byte)(_udpCounter >> 8);
    _udpSendArgs.Buffer[_udpSendBufferPosition + 3] = (byte)_udpCounter;
    _udpSendBufferPosition = nextPosition;
    SavePacket(packet);
}
```

## Security and Validation

### Packet Preprocessing

```csharp
public class PacketPreprocessing
{
    private readonly Dictionary<int, int> _packetIdToPreprocessMap = new();
    private readonly Dictionary<int, Func<GameClient, GSPacketIn, bool>> _preprocessors = new();
    
    public PacketPreprocessing()
    {
        RegisterPreprocessors((int)eClientStatus.LoggedIn, 
            (client, packet) => client.Account != null);
        RegisterPreprocessors((int)eClientStatus.PlayerInGame, 
            (client, packet) => client.Player != null);
    }
    
    public bool CanProcessPacket(GameClient client, GSPacketIn packet)
    {
        if (!_packetIdToPreprocessMap.TryGetValue(packet.Code, out int preprocessorId))
            return true; // No preprocessor = allow
            
        if (_preprocessors.TryGetValue(preprocessorId, out var preprocessor))
            return preprocessor(client, packet);
            
        return true;
    }
    
    private void RegisterPreprocessors(int preprocessorId, Func<GameClient, GSPacketIn, bool> func)
    {
        _preprocessors[preprocessorId] = func;
    }
}
```

### Packet Validation

```csharp
private static bool ValidatePacketIssuedTimestamp<T>(T packet) where T : PacketOut, IPooledObject<T>
{
    if (!packet.IsValidForTick())
    {
        if (packet.IssuedTimestamp != 0)
        {
            log.Error($"Packet not issued in current game loop time (Code: 0x{packet.Code:X2}) " +
                     $"(Issued: {packet.IssuedTimestamp}) (Current: {GameLoop.GameLoopTime})");
            return false;
        }
        
        log.Debug($"Packet issued outside game loop (Code: 0x{packet.Code:X2})");
    }
    
    return true;
}

private bool ValidatePacketSize(byte[] packetBuffer, int packetSize)
{
    if (packetSize <= 2048)
        return true;
        
    log.Error($"Discarding oversized packet. Code: 0x{packetBuffer[2]:X2}, " +
             $"Account: {_client.Account?.Name ?? _client.TcpEndpointAddress}, Size: {packetSize}");
             
    _client.Out.SendMessage($"Oversized packet detected (code: 0x{packetBuffer[2]:X2}) (size: {packetSize}). " +
                           "Please report this issue!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
    return false;
}
```

### Checksum Verification

```csharp
public static int CalculateChecksum(byte[] data, int start, int count)
{
    ushort checksum = 0;
    ushort val1 = 0;
    ushort val2 = 0;
    
    int dataPtr = start;
    int len = count - 2; // Exclude checksum bytes
    
    for (int i = 0; i < len; i += 2)
    {
        if (i + 1 < len)
        {
            val1 = (ushort)((data[dataPtr] << 8) | data[dataPtr + 1]);
        }
        else
        {
            val1 = (ushort)(data[dataPtr] << 8);
        }
        
        val2 = (ushort)(val2 + val1);
        if ((val2 & 0x80000000) != 0)
        {
            val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
        }
        
        dataPtr += 2;
    }
    
    val2 = (ushort)((val2 & 0xFFFF) + (val2 >> 16));
    checksum = (ushort)(~val2);
    
    return checksum;
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

## Performance Optimizations

### Object Pooling

```csharp
public static class PacketPool<T> where T : class, IPooledObject<T>, new()
{
    private static readonly ConcurrentQueue<T> _pool = new();
    private static readonly int MAX_POOL_SIZE = 1000;
    private static int _poolSize = 0;
    
    public static T Get()
    {
        if (_pool.TryDequeue(out var packet))
        {
            Interlocked.Decrement(ref _poolSize);
            return packet;
        }
        return new T();
    }
    
    public static void Return(T packet)
    {
        if (_poolSize < MAX_POOL_SIZE)
        {
            packet.Reset();
            _pool.Enqueue(packet);
            Interlocked.Increment(ref _poolSize);
        }
    }
}
```

### Asynchronous Socket Operations

```csharp
private void GetAvailableTcpSendArgs()
{
    if (!_tcpSendArgsPool.TryDequeue(out _tcpSendArgs))
    {
        _tcpSendArgs = new SocketAsyncEventArgs();
        _tcpSendArgs.SetBuffer(new byte[8192], 0, 8192);
        _tcpSendArgs.Completed += OnTcpSendCompleted;
    }
    
    _tcpSendBufferPosition = 0;
}

private void OnTcpSendCompleted(object sender, SocketAsyncEventArgs e)
{
    try
    {
        if (e.SocketError == SocketError.Success)
        {
            // Return to pool for reuse
            _tcpSendArgsPool.Enqueue(e);
        }
        else
        {
            log.Debug($"TCP send error: {e.SocketError}");
            _client.Disconnect();
        }
    }
    catch (Exception ex)
    {
        log.Error($"Error in TCP send completion: {ex}");
    }
}
```

## Protocol Versioning

### Version Detection

```csharp
private bool CheckVersion()
{
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

### Packet Encoding

```csharp
public interface IPacketEncoding
{
    void EncodePacket(PacketOut packet);
    void DecodePacket(PacketIn packet);
}

public class PacketEncoding168 : IPacketEncoding
{
    public void EncodePacket(PacketOut packet)
    {
        // XOR encoding with version-specific key
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
    
    public void DecodePacket(PacketIn packet)
    {
        // Reverse XOR encoding
        byte[] buffer = packet.GetBuffer();
        for (int i = 8; i < buffer.Length - 2; i++)
        {
            buffer[i] ^= (byte)(168 + i);
        }
    }
}
```

## Error Handling and Recovery

### Connection Recovery

```csharp
private bool SendTcp()
{
    if (!_client.Socket.Connected)
        return false;
        
    try
    {
        if (_tcpSendBufferPosition > 0)
        {
            _tcpSendArgs.SetBuffer(0, _tcpSendBufferPosition);
            
            if (_client.SendAsync(_tcpSendArgs))
                GetAvailableTcpSendArgs();
                
            _tcpSendBufferPosition = 0;
        }
        
        return true;
    }
    catch (ObjectDisposedException) { }
    catch (SocketException e)
    {
        log.Debug($"Socket exception on TCP send (Client: {_client}) (Code: {e.SocketErrorCode})");
    }
    catch (Exception e)
    {
        log.Error($"Unhandled exception on TCP send (Client: {_client}): {e}");
    }
    
    return false;
}
```

### Packet Loss Handling

```csharp
public class PacketReliability
{
    private readonly Dictionary<ushort, PendingPacket> _pendingPackets = new();
    private readonly Timer _retransmissionTimer;
    
    public void SendReliablePacket(GSUDPPacketOut packet)
    {
        packet.Sequence = GetNextSequence();
        _pendingPackets[packet.Sequence] = new PendingPacket
        {
            Packet = packet,
            SendTime = GameLoop.GameLoopTime,
            RetryCount = 0
        };
        
        SendPacket(packet);
    }
    
    public void AcknowledgePacket(ushort sequence)
    {
        _pendingPackets.Remove(sequence);
    }
    
    private void CheckRetransmissions()
    {
        long currentTime = GameLoop.GameLoopTime;
        
        foreach (var kvp in _pendingPackets.ToArray())
        {
            var pending = kvp.Value;
            
            if (currentTime - pending.SendTime > RETRANSMISSION_TIMEOUT)
            {
                if (pending.RetryCount < MAX_RETRIES)
                {
                    pending.RetryCount++;
                    pending.SendTime = currentTime;
                    SendPacket(pending.Packet);
                }
                else
                {
                   