using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using DOL.GS.ServerProperties;
using DOL.Network;
using ECS.Debug;
using static DOL.GS.GameClient;

namespace DOL.GS.PacketHandler
{
    public class PacketProcessor
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const int SAVED_PACKETS_COUNT = 16;
        private static Dictionary<string, IPacketHandler[]> _cachedPacketHandlerSearchResults = [];
        private static Dictionary<string, List<PacketHandlerAttribute>> _cachedPreprocessorSearchResults = [];
        private static Lock _loadPacketHandlersLock = new();

        private GameClient _client;
        private IPacketHandler[] _packetHandlers = new IPacketHandler[256];
        private PacketPreprocessing _packetPreprocessor = new();
        private Queue<IPacket> _savedPackets = new(SAVED_PACKETS_COUNT);
        private readonly Lock _savedPacketsLock = new();

        private DrainArray<GSTCPPacketOut> _tcpPacketQueue = new();
        private DrainArray<GSUDPPacketOut> _udpToTcpPacketQueue = new();
        private ConcurrentQueue<SocketAsyncEventArgs> _tcpSendArgsPool = [];
        private SocketAsyncEventArgs _tcpSendArgs;
        private int _tcpSendBufferPosition;

        private DrainArray<GSUDPPacketOut> _udpPacketQueue = new();
        private ConcurrentQueue<SocketAsyncEventArgs> _udpSendArgsPool = [];
        private SocketAsyncEventArgs _udpSendArgs;
        private int _udpSendBufferPosition;
        private uint _udpCounter;

        public IPacketEncoding Encoding { get; } = new PacketEncoding168();

        static PacketProcessor()
        {
            if (Properties.SAVE_PACKETS && log.IsWarnEnabled)
                log.Warn($"\"{nameof(Properties.SAVE_PACKETS)}\" is true. This reduces performance and should only be enabled for debugging a specific issue.");
        }

        public PacketProcessor(GameClient client)
        {
            _client = client;
            GetAvailableTcpSendArgs();
            GetAvailableUdpSendArgs();
            LoadPacketHandlers();

            void LoadPacketHandlers()
            {
                string version = "v168";

                lock (_loadPacketHandlersLock)
                {
                    if (_cachedPacketHandlerSearchResults.TryGetValue(version, out IPacketHandler[] packetHandlers))
                    {
                        _packetHandlers = packetHandlers.Clone() as IPacketHandler[];
                        int count = 0;

                        foreach (IPacketHandler packetHandler in _packetHandlers)
                        {
                            if (packetHandler != null)
                                count++;
                        }

                        if (log.IsInfoEnabled)
                            log.Info($"Loaded {count} handlers from cache for {version}");
                    }
                    else
                    {
                        _packetHandlers = new IPacketHandler[256];
                        int count = SearchAndAddPacketHandlers(version, Assembly.GetAssembly(typeof(GameServer)), _packetHandlers);

                        if (log.IsInfoEnabled)
                            log.Info($"Loaded {count} handlers from GameServer Assembly");

                        count = 0;

                        foreach (Assembly asm in ScriptMgr.Scripts)
                            count += SearchAndAddPacketHandlers(version, asm, _packetHandlers);

                        if (log.IsInfoEnabled)
                            log.Info($"Loaded {count} handlers from Script Assembly");

                        _cachedPacketHandlerSearchResults.Add(version, _packetHandlers.Clone() as IPacketHandler[]);
                    }
                }

                _cachedPreprocessorSearchResults.TryGetValue(version, out List<PacketHandlerAttribute> attributes);

                if (log.IsInfoEnabled)
                    log.Info($"Loaded {attributes.Count} preprocessors from cache for {version}");

                foreach (PacketHandlerAttribute attribute in attributes)
                    _packetPreprocessor.RegisterPacketDefinition(attribute.Code, attribute.PreprocessorID);

                static int SearchAndAddPacketHandlers(string version, Assembly assembly, IPacketHandler[] packetHandlers)
                {
                    int count = 0;

                    foreach (Type type in assembly.GetTypes())
                    {
                        if (type.IsClass != true)
                            continue;

                        if (type.GetInterface("DOL.GS.PacketHandler.IPacketHandler") == null)
                            continue;

                        if (!type.Namespace.EndsWith(version, StringComparison.OrdinalIgnoreCase))
                            continue;

                        PacketHandlerAttribute[] packetHandlerAttributes = type.GetCustomAttributes(typeof(PacketHandlerAttribute), true) as PacketHandlerAttribute[];

                        if (packetHandlerAttributes.Length > 0)
                        {
                            count++;
                            int packetCode = packetHandlerAttributes[0].Code;
                            IPacketHandler handler = Activator.CreateInstance(type) as IPacketHandler;

                            if (log.IsDebugEnabled)
                            {
                                if (packetHandlers[packetCode] != null)
                                    log.Debug($"Overwriting Client Packet Code {packetCode}, with handler {handler.GetType().FullName}");
                            }

                            packetHandlers[packetCode] = handler;

                            if (!_cachedPreprocessorSearchResults.ContainsKey(version))
                                _cachedPreprocessorSearchResults.Add(version, []);

                            _cachedPreprocessorSearchResults[version].Add(packetHandlerAttributes[0]);
                        }
                    }

                    return count;
                }
            }
        }

        public void OnUdpEndpointSet(IPEndPoint endPoint)
        {
            _udpSendArgs.RemoteEndPoint = endPoint;
        }

        public IPacket[] GetLastPackets()
        {
            lock (_savedPacketsLock)
            {
                return _savedPackets.ToArray();
            }
        }

        public void ProcessInboundPacket(GSPacketIn packet)
        {
            int code = packet.Code;
            SavePacket(packet);

            if (code >= _packetHandlers.Length)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error($"Received packet code is outside of {nameof(_packetHandlers)} array bounds");
                    string account = _client.Account != null ? _client.Account.Name : _client.TcpEndpointAddress;
                    string description = $"===> <{account}> Packet 0x{code:X2} (0x{code ^ 168:X2}) length: {packet.PacketSize} (ThreadId={Environment.CurrentManagedThreadId})";
                    log.Error(Marshal.ToHexDump(description, packet.ToArray()));
                }

                return;
            }

            IPacketHandler packetHandler = _packetHandlers[code];

            if (packetHandler == null)
                return;

            if (!_packetPreprocessor.CanProcessPacket(_client, packet))
            {
                if (log.IsInfoEnabled)
                    log.Info($"Preprocessor prevents handling of a packet with packet.ID={packet.Code}");

                return;
            }

            try
            {
                long startTick = GameLoop.GetRealTime();
                packetHandler.HandlePacket(_client, packet);
                long stopTick = GameLoop.GetRealTime();

                if (log.IsWarnEnabled)
                {
                    if (stopTick - startTick > Diagnostics.LongTickThreshold)
                        log.Warn($"Long {nameof(PacketProcessor)}.{nameof(ProcessInboundPacket)} (code: 0x{packet.Code:X}) for {_client.Player?.Name}({_client.Player?.ObjectID}) Time: {stopTick - startTick}ms");
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    string client = _client == null ? "null" : _client.ToString();
                    log.Error($"Error while processing packet (handler={packetHandler.GetType().FullName}; client={client})", e);
                }
            }
        }

        public void QueuePacket(GSTCPPacketOut packet)
        {
            if (_client.ClientState is eClientState.Disconnected or eClientState.Linkdead)
                return;

            // This is dangerous if the same packet is passed down to multiple `PacketProcessor` at the same time.
            if (!packet.IsSizeSet)
                packet.WritePacketLength();

            _tcpPacketQueue.Add(packet);
        }

        public void QueuePacket(GSUDPPacketOut packet, bool forced)
        {
            if (_client.ClientState is eClientState.Disconnected or eClientState.Linkdead)
                return;

            if (_client.ClientState is eClientState.Playing)
            {
                // The rate at which clients send `UDPInitRequestHandler` may vary depending on their version (1.127 = 65 seconds).
                if (ServiceUtils.ShouldTick(_client.UdpPingTime + 70000))
                    _client.UdpConfirm = false;
            }

            // This is dangerous if the same packet is passed down to multiple `PacketProcessor` at the same time.
            if (!packet.IsSizeSet)
                packet.WritePacketLength();

            // If UDP is unavailable, send via TCP instead.
            if (_udpSendArgs.RemoteEndPoint != null && (forced || _client.UdpConfirm))
                _udpPacketQueue.Add(packet);
            else
                _udpToTcpPacketQueue.Add(packet);
        }

        public void SendPendingPackets()
        {
            _tcpPacketQueue.DrainTo(static (packet, processor) => processor.AppendTcpPacketToTcpSendBuffer(packet), this);
            _udpToTcpPacketQueue.DrainTo(static (packet, processor) => processor.AppendUdpPacketToTcpSendBuffer(packet), this);
            SendTcp();
            _udpPacketQueue.DrainTo(static (packet, processor) => processor.AppendUdpPacketToUdpSendBuffer(packet), this);
            SendUdp();
        }

        public void ClearPendingOutboundPackets()
        {
            _tcpSendBufferPosition = 0;
            _udpSendBufferPosition = 0;
        }

        public void Dispose()
        {
            while (_tcpSendArgsPool.TryDequeue(out SocketAsyncEventArgs tcpSendArgs))
                tcpSendArgs.Dispose();

            _tcpSendArgs?.Dispose();

            while (_udpSendArgsPool.TryDequeue(out SocketAsyncEventArgs udpSendArgs))
                udpSendArgs.Dispose();

            _udpSendArgs?.Dispose();

            // Drain all pending packets on the next game loop tick to avoid concurrent modification issues.
            GameLoopService.PostAfterTick(static (state) =>
            {
                state._tcpPacketQueue.DrainTo(static state => { });
                state._udpToTcpPacketQueue.DrainTo(static state => { });
                state._udpPacketQueue.DrainTo(static state => { });
            }, this);
        }

        private void AppendTcpPacketToTcpSendBuffer(GSTCPPacketOut packet)
        {
            if (!ValidatePacketIssuedTimestamp(packet))
                return;

            byte[] packetBuffer = packet.GetBuffer();
            int packetSize = (int) packet.Length;

            if (!ValidatePacketSize(packetBuffer, packetSize))
                return;

            if (_tcpSendArgs.Buffer == null)
                return;

            SavePacket(packet);
            int nextPosition = _tcpSendBufferPosition + packetSize;

            // If the send buffer is full, send whatever we have.
            if (nextPosition > _tcpSendArgs.Buffer.Length)
            {
                if (!SendTcp())
                    return;

                nextPosition = _tcpSendBufferPosition + packetSize;

                // If there still isn't enough room, we'll have to discard the packet.
                if (nextPosition > _tcpSendArgs.Buffer.Length)
                    return;

                nextPosition = packetSize;
            }

            Buffer.BlockCopy(packetBuffer, 0, _tcpSendArgs.Buffer, _tcpSendBufferPosition, packetSize);
            _tcpSendBufferPosition = nextPosition;
        }

        private void AppendUdpPacketToTcpSendBuffer(GSUDPPacketOut packet)
        {
            if (!ValidatePacketIssuedTimestamp(packet))
                return;

            byte[] packetBuffer = packet.GetBuffer();
            int packetSize = (int) packet.Length - 2;

            if (!ValidatePacketSize(packetBuffer, packetSize))
                return;

            if (_tcpSendArgs.Buffer == null)
                return;

            SavePacket(packet);
            int nextPosition = _tcpSendBufferPosition + packetSize;

            // If the send buffer is full, send whatever we have.
            if (nextPosition > _tcpSendArgs.Buffer.Length)
            {
                if (!SendTcp())
                    return;

                nextPosition = _tcpSendBufferPosition + packetSize;

                // If there still isn't enough room, we'll have to discard the packet.
                if (nextPosition > _tcpSendArgs.Buffer.Length)
                    return;

                nextPosition = packetSize;
            }

            // Transform the UDP packet into a TCP one.
            Buffer.BlockCopy(packetBuffer, 4, _tcpSendArgs.Buffer, _tcpSendBufferPosition + 2, packetSize - 2);
            _tcpSendArgs.Buffer[_tcpSendBufferPosition] = packetBuffer[0];
            _tcpSendArgs.Buffer[_tcpSendBufferPosition + 1] = packetBuffer[1];
            _tcpSendBufferPosition = nextPosition;
        }

        private void AppendUdpPacketToUdpSendBuffer(GSUDPPacketOut packet)
        {
            if (!ValidatePacketIssuedTimestamp(packet))
                return;

            byte[] packetBuffer = packet.GetBuffer();
            int packetSize = (int) packet.Length;

            if (!ValidatePacketSize(packetBuffer, packetSize))
                return;

            if (_udpSendArgs.Buffer == null)
                return;

            SavePacket(packet);
            int nextPosition = _udpSendBufferPosition + packetSize;

            // If the send buffer is full, send whatever we have.
            if (nextPosition > _udpSendArgs.Buffer.Length)
            {
                if (!SendUdp())
                    return;

                nextPosition = _udpSendBufferPosition + packetSize;

                // If there still isn't enough room, we'll have to discard the packet.
                if (nextPosition > _udpSendArgs.Buffer.Length)
                    return;

                nextPosition = packetSize;
            }

            // Add `_udpCounter` to the packet's content.
            Buffer.BlockCopy(packetBuffer, 0, _udpSendArgs.Buffer, _udpSendBufferPosition, packetSize);
            _udpCounter++; // Let it overflow.
            _udpSendArgs.Buffer[_udpSendBufferPosition + 2] = (byte) (_udpCounter >> 8);
            _udpSendArgs.Buffer[_udpSendBufferPosition + 3] = (byte) _udpCounter;
            _udpSendBufferPosition = nextPosition;
        }

        private static bool ValidatePacketIssuedTimestamp<T>(T packet) where T : PacketOut, IPooledObject<T>
        {
            if (!packet.IsValidForTick())
            {
                if (packet.IssuedTimestamp != 0)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Packet was not issued in the current game loop time (Code: 0x{packet.Code:X2}) (Issued at: {packet.IssuedTimestamp}) (Current time: {GameLoop.GameLoopTime})");

                    return false;
                }

                if (log.IsDebugEnabled)
                    log.Debug($"Packet was issued outside the game loop (Code: 0x{packet.Code:X2}) (Current time: {GameLoop.GameLoopTime})");
            }

            return true;
        }

        private bool ValidatePacketSize(byte[] packetBuffer, int packetSize)
        {
            if (packetSize <= 2048)
                return true;

            if (log.IsErrorEnabled)
            {
                string account = _client.Account != null ? _client.Account.Name : _client.TcpEndpointAddress;
                string description = $"Discarding oversized packet. Packet code: 0x{packetBuffer[2]:X2}, account: {account}, packet size: {packetSize}.";
                log.Error($"{Marshal.ToHexDump(description, packetBuffer)}\n{Environment.StackTrace}");
            }

            _client.Out.SendMessage($"Oversized packet detected and discarded (code: 0x{packetBuffer[2]:X2}) (size: {packetSize}). Please report this issue!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
            return false;
        }

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
                if (log.IsDebugEnabled)
                    log.Debug($"Socket exception on TCP send (Client: {_client}) (Code: {e.SocketErrorCode})");
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Unhandled exception on TCP send (Client: {_client}): {e}");
            }

            return false;
        }

        private bool SendUdp()
        {
            // Not technically needed to send UDP.
            if (!_client.Socket.Connected)
                return false;

            try
            {
                if (_udpSendBufferPosition > 0)
                {
                    _udpSendArgs.SetBuffer(0, _udpSendBufferPosition);

                    if (GameServer.Instance.SendUdp(_udpSendArgs))
                        GetAvailableUdpSendArgs();

                    _udpSendBufferPosition = 0;
                }

                return true;
            }
            catch (ObjectDisposedException) { }
            catch (SocketException e)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"Socket exception on UDP send (Client: {_client}) (Code: {e.SocketErrorCode})");
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Unhandled exception on UDP send (Client: {_client}): {e}");
            }

            _client.UdpConfirm = false;
            return false;
        }

        public static ushort CalculateChecksum(byte[] packet, int dataOffset, int dataSize)
        {
            byte val1 = 0x7E;
            byte val2 = 0x7E;
            int i = dataOffset;
            int length = i + dataSize;

            while (i < length)
            {
                val1 += packet[i++];
                val2 += val1;
            }

            return (ushort) (val2 - ((val1 + val2) << 8));
        }

        private void GetAvailableTcpSendArgs()
        {
            if (_tcpSendArgsPool.TryDequeue(out SocketAsyncEventArgs tcpSendArgs))
            {
                tcpSendArgs.SetBuffer(0, 0);
                _tcpSendArgs = tcpSendArgs;
                return;
            }

            _tcpSendArgs = new();
            _tcpSendArgs.SetBuffer(new byte[BaseClient.TCP_SEND_BUFFER_SIZE], 0, 0);
            _tcpSendArgs.Completed += OnTcpSendCompletion;

            void OnTcpSendCompletion(object sender, SocketAsyncEventArgs tcpSendArgs)
            {
                _tcpSendArgsPool.Enqueue(tcpSendArgs);
            }
        }

        private void GetAvailableUdpSendArgs()
        {
            if (_udpSendArgsPool.TryDequeue(out SocketAsyncEventArgs udpSendArgs))
            {
                udpSendArgs.SetBuffer(0, 0);
                udpSendArgs.RemoteEndPoint = _client.UdpEndPoint;
                _udpSendArgs = udpSendArgs;
                return;
            }

            _udpSendArgs = new();
            _udpSendArgs.SetBuffer(new byte[BaseClient.UDP_SEND_BUFFER_SIZE], 0, 0);
            _udpSendArgs.Completed += OnUdpSendCompletion;
            _udpSendArgs.RemoteEndPoint = _client.UdpEndPoint;

            void OnUdpSendCompletion(object sender, SocketAsyncEventArgs udpSendArgs)
            {
                _udpSendArgsPool.Enqueue(udpSendArgs);
            }
        }

        private void SavePacket(IPacket packet)
        {
            if (!Properties.SAVE_PACKETS)
                return;

            lock (_savedPacketsLock)
            {
                while (_savedPackets.Count >= SAVED_PACKETS_COUNT)
                    _savedPackets.Dequeue();

                _savedPackets.Enqueue(packet);
            }
        }
    }
}
