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

public abstract class PacketIn : IPacket
{
    public abstract byte ReadByte();
    public abstract short ReadShort();
    public abstract int ReadInt();
    public abstract string ReadString();
}

public abstract class PacketOut : IPacket
{
    public abstract void WriteByte(byte value);
    public abstract void WriteShort(short value);
    public abstract void WriteInt(int value);
    public abstract void WriteString(string value);
}
```

### Protocol-Specific Implementations

```csharp
// UDP packet with object pooling
public class GSUDPPacketOut : PacketOut, IPooledObject<GSUDPPacketOut>
{
    private static readonly ObjectPool<GSUDPPacketOut> _pool = new();
    
    public static GSUDPPacketOut GetFromPool()
    {
        return _pool.Get();
    }
    
    public void ReturnToPool()
    {
        Reset();
        _pool.Return(this);
    }
}

// TCP packet for reliable delivery
public class GSTCPPacketOut : PacketOut, IPooledObject<GSTCPPacketOut>
{
    private readonly byte[] _buffer = new byte[MAX_PACKET_SIZE];
    private int _position = 0;
    
    public override void WriteByte(byte value)
    {
        _buffer[_position++] = value;
    }
}

// Incoming packet parser
public class GSPacketIn : PacketIn, IPooledObject<GSPacketIn>
{
    public int ReadPosition { get; set; }
    public GameClient Client { get; set; }
    
    public override byte ReadByte()
    {
        return Buffer[ReadPosition++];
    }
}
```

## Packet Handler System

### Handler Architecture

```csharp
public interface IPacketHandler
{
    void HandlePacket(GameClient client, GSPacketIn packet);
}

public abstract class PacketHandler : IPacketHandler
{
    public abstract void HandlePacket(GameClient client, GSPacketIn packet);
    
    protected virtual bool PreHandle(GameClient client, GSPacketIn packet)
    {
        return client != null && client.IsConnected;
    }
    
    protected virtual void PostHandle(GameClient client, GSPacketIn packet)
    {
        // Cleanup, logging, etc.
    }
}
```

### Handler Registration System

```csharp
public class PacketHandlerRegistry
{
    private readonly Dictionary<int, IPacketHandler> _handlers = new();
    
    public void RegisterHandler(int packetCode, IPacketHandler handler)
    {
        _handlers[packetCode] = handler;
    }
    
    public IPacketHandler GetHandler(int packetCode)
    {
        return _handlers.GetValueOrDefault(packetCode);
    }
}
```

## Client Connection System

### Game Client Management

```csharp
public class GameClient
{
    public string SessionID { get; set; }
    public IPEndPoint RemoteEndpoint { get; set; }
    public GamePlayer Player { get; set; }
    public Account Account { get; set; }
    public ClientState State { get; set; }
    public IPacketLib Out { get; set; }
    
    // Connection management
    public bool IsConnected { get; private set; }
    public DateTime LastActivity { get; set; }
    public int PingTime { get; set; }
    
    // Security
    public bool IsEncrypted { get; set; }
    public byte[] EncryptionKey { get; set; }
    
    // Packet processing
    public void SendTCP(GSTCPPacketOut packet)
    {
        if (IsConnected && packet != null)
        {
            _tcpSocket.Send(packet.Buffer, packet.Size);
        }
    }
    
    public void SendUDP(GSUDPPacketOut packet, bool forced = false)
    {
        if (IsConnected && (forced || _udpSocket != null))
        {
            _udpSocket.SendTo(packet.Buffer, packet.Size, RemoteEndpoint);
        }
    }
}
```

### Connection States

```csharp
public enum ClientState
{
    None,
    Connecting,
    LoggingIn,
    CharScreen,
    WorldEnter,
    Playing,
    Disconnecting,
    Linkdead
}
```

## Packet Library System

### Versioned Protocol Support

```csharp
public abstract class PacketLib : IPacketLib
{
    public abstract byte GetPacketCode(eServerPackets packetType);
    public abstract void SendTCP(GSTCPPacketOut packet);
    public abstract void SendUDP(GSUDPPacketOut packet);
    
    // Version-specific implementations
    public virtual void SendPlayerCreate(GamePlayer player) { }
    public virtual void SendObjectRemove(GameObject obj) { }
    public virtual void SendMessage(string message, eChatType type, eChatLoc location) { }
}

// Client-specific protocol versions
public class PacketLib1124 : PacketLib  // Client version 1.124
{
    public override void SendPlayerCreate(GamePlayer player)
    {
        using var pak = GSUDPPacketOut.GetFromPool();
        pak.WriteShort((ushort)eServerPackets.PlayerCreate);
        pak.WriteShort((ushort)player.ObjectID);
        pak.WriteString(player.Name);
        // ... write player data
        SendUDP(pak);
    }
}
```

### Packet Processing Pipeline

```csharp
public class PacketProcessor
{
    private readonly PacketHandlerRegistry _handlers;
    private readonly PacketPreprocessing _preprocessing;
    
    public void ProcessInboundPacket(GSPacketIn packet)
    {
        try
        {
            // Security validation
            if (!_preprocessing.CanProcessPacket(packet.Client, packet))
                return;
                
            // Rate limiting
            if (!ValidatePacketRate(packet.Client, packet.ID))
                return;
                
            // Find and execute handler
            var handler = _handlers.GetHandler(packet.ID);
            if (handler != null)
            {
                handler.HandlePacket(packet.Client, packet);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error processing packet {packet.ID}: {ex}");
        }
        finally
        {
            packet.ReturnToPool();
        }
    }
}
```

## Key Packet Types

### Authentication Packets

```csharp
public class LoginRequestHandler : IPacketHandler
{
    public void HandlePacket(GameClient client, GSPacketIn packet)
    {
        string username = packet.ReadString(24);
        string password = packet.ReadString(20);
        
        var account = AuthenticateAccount(username, password);
        if (account != null)
        {
            client.Account = account;
            client.Out.SendLoginGranted();
        }
        else
        {
            client.Out.SendLoginDenied(eLoginError.InvalidCredentials);
        }
    }
}
```

### Movement Packets

```csharp
public class PlayerPositionUpdateHandler : IPacketHandler
{
    public void HandlePacket(GameClient client, GSPacketIn packet)
    {
        var x = packet.ReadInt();
        var y = packet.ReadInt();
        var z = packet.ReadInt();
        var heading = packet.ReadShort();
        
        if (ValidatePosition(client.Player, x, y, z))
        {
            client.Player.MoveTo(x, y, z, heading);
            BroadcastMovement(client.Player);
        }
        else
        {
            // Position correction
            client.Out.SendPlayerPositionAndObjectID();
        }
    }
}
```

### Combat Packets

```csharp
public class AttackRequestHandler : IPacketHandler
{
    public void HandlePacket(GameClient client, GSPacketIn packet)
    {
        var targetId = packet.ReadShort();
        var attackType = (eAttackType)packet.ReadByte();
        
        var target = WorldMgr.GetObjectByID(targetId);
        if (target is GameLiving living)
        {
            client.Player.StartAttack(living, attackType);
        }
    }
}
```

## Network Performance Optimization

### Packet Pooling

```csharp
public static class PacketPool<T> where T : class, IPooledObject<T>, new()
{
    private static readonly ConcurrentQueue<T> _pool = new();
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

### Packet Batching

```csharp
public class PacketBatcher
{
    private readonly List<GSUDPPacketOut> _pendingPackets = new();
    private readonly Timer _flushTimer;
    
    public void QueuePacket(GSUDPPacketOut packet, bool forced = false)
    {
        lock (_pendingPackets)
        {
            _pendingPackets.Add(packet);
            
            if (forced || _pendingPackets.Count >= BATCH_SIZE)
            {
                FlushPendingPackets();
            }
        }
    }
    
    private void FlushPendingPackets()
    {
        foreach (var packet in _pendingPackets)
        {
            SendPacketDirect(packet);
            packet.ReturnToPool();
        }
        _pendingPackets.Clear();
    }
}
```

### Compression System

```csharp
public class PacketCompression
{
    public static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using var gzip = new GZipStream(output, CompressionMode.Compress);
        gzip.Write(data, 0, data.Length);
        return output.ToArray();
    }
    
    public static byte[] Decompress(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}
```

## Protocol Security

### Encryption System

```csharp
public class PacketEncryption
{
    private readonly RC4 _encryptionCipher;
    private readonly RC4 _decryptionCipher;
    
    public void InitializeKey(byte[] key)
    {
        _encryptionCipher.Initialize(key);
        _decryptionCipher.Initialize(key);
    }
    
    public void EncryptPacket(byte[] data, int offset, int length)
    {
        _encryptionCipher.Process(data, offset, length);
    }
    
    public void DecryptPacket(byte[] data, int offset, int length)
    {
        _decryptionCipher.Process(data, offset, length);
    }
}
```

### Anti-Cheat Integration

```csharp
public class PacketValidation
{
    public bool ValidatePacket(GameClient client, GSPacketIn packet)
    {
        // Rate limiting
        if (!ValidatePacketRate(client, packet.ID))
            return false;
            
        // Sequence validation
        if (!ValidateSequence(client, packet))
            return false;
            
        // Size validation
        if (packet.Size > MAX_PACKET_SIZE)
            return false;
            
        // Content validation (packet-specific)
        return ValidatePacketContent(packet);
    }
}
```

## Reliability System

### TCP Reliability

```csharp
public class TCPReliabilityManager
{
    private readonly Dictionary<int, PendingMessage> _pendingMessages = new();
    private int _nextSequenceId = 1;
    
    public void SendReliable(GSTCPPacketOut packet, GameClient client)
    {
        var sequenceId = _nextSequenceId++;
        var pending = new PendingMessage
        {
            SequenceId = sequenceId,
            Packet = packet,
            SendTime = DateTime.UtcNow,
            Attempts = 0
        };
        
        _pendingMessages[sequenceId] = pending;
        SendPacketWithSequence(packet, sequenceId, client);
    }
    
    public void ProcessAcknowledgment(int sequenceId)
    {
        _pendingMessages.Remove(sequenceId);
    }
}
```

### UDP Reliability

```csharp
public class UDPReliabilityManager
{
    private readonly Queue<GSUDPPacketOut> _resendQueue = new();
    private readonly Timer _resendTimer;
    
    public void QueueForResend(GSUDPPacketOut packet, int attempts)
    {
        if (attempts < MAX_RESEND_ATTEMPTS)
        {
            _resendQueue.Enqueue(packet);
        }
    }
    
    private void ProcessResendQueue()
    {
        while (_resendQueue.TryDequeue(out var packet))
        {
            ResendPacket(packet);
        }
    }
}
```

## Configuration

### Network Settings

```csharp
public static class NetworkConfiguration
{
    public static int UDP_PACKET_POOL_SIZE = 2000;
    public static int TCP_PACKET_POOL_SIZE = 1500;
    public static int MAX_PACKET_SIZE = 2048;
    public static int PACKET_BATCH_SIZE = 10;
    public static int RESEND_TIMEOUT_MS = 1000;
    public static int MAX_RESEND_ATTEMPTS = 3;
    public static bool ENABLE_COMPRESSION = true;
    public static bool ENABLE_ENCRYPTION = true;
    public static int PING_TIMEOUT_MS = 30000;
}
```

## System Interactions

### Combat Integration
- Attack packets trigger combat calculations
- Damage packets sent to all observers
- Position updates validate combat range

### Movement Integration  
- Position packets update world state
- Region changes trigger loading
- Speed validation prevents cheating

### Chat Integration
- Message packets routed by channel
- Language scrambling applied
- Ignore lists filtered server-side

## Performance Metrics

### Throughput Targets
- **Packets/Second**: 10,000+ per server
- **Latency**: <50ms average
- **Packet Loss**: <0.1%
- **Memory Usage**: <100MB packet buffers

### Optimization Strategies
- Object pooling reduces GC pressure
- Packet batching improves efficiency
- Compression reduces bandwidth
- Protocol versioning allows upgrades

## Testing

### Network Testing
- Packet corruption simulation
- Connection drop testing
- High latency scenarios
- Bandwidth limitation tests

### Security Testing
- Malformed packet handling
- Rate limiting validation
- Encryption key rotation
- Authentication bypass attempts

## Implementation Status

**Completed**:
- ✅ Core packet architecture
- ✅ Handler registration system
- ✅ Object pooling for performance
- ✅ Protocol versioning support
- ✅ Basic encryption/compression

**In Progress**:
- 🔄 Advanced anti-cheat integration
- 🔄 Dynamic compression selection
- 🔄 Automatic failover systems

**Planned**:
- ⏳ HTTP/2 protocol support
- ⏳ WebSocket client support
- ⏳ Mobile client optimization

## References

- **Client Protocol**: Based on DAoC 1.124+ protocol specification
- **Network Stack**: Custom implementation with .NET sockets
- **Security**: RC4 encryption with custom key exchange
- **Performance**: Optimized for 500+ concurrent connections per server 