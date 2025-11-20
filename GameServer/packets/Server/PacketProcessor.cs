using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using DOL.Network;
using ECS.Debug;

namespace DOL.GS.PacketHandler
{
    public class PacketProcessor
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const int SEND_ARGS_POOL_SIZE = 4;

        private static Dictionary<string, IPacketHandler[]> _cachedPacketHandlerSearchResults = [];
        private static Dictionary<string, List<PacketHandlerAttribute>> _cachedPreprocessorSearchResults = [];
        private static Lock _loadPacketHandlersLock = new();
        private static long _sendBufferPoolExhaustedCount;

        private readonly GameClient _client;
        private readonly PacketPreprocessing _packetPreprocessor = new();
        private IPacketHandler[] _packetHandlers = new IPacketHandler[256];

        private readonly DrainArray<GSTCPPacketOut> _tcpPacketQueue = new();
        private readonly DrainArray<GSUDPPacketOut> _udpToTcpPacketQueue = new();
        private readonly ConcurrentQueue<SocketAsyncEventArgs> _tcpSendArgsPool = [];

        private readonly DrainArray<GSUDPPacketOut> _udpPacketQueue = new();
        private readonly ConcurrentQueue<SocketAsyncEventArgs> _udpSendArgsPool = [];
        private uint _udpCounter;

        private readonly SendContext _sendContext = new();

        public static long SendBufferPoolExhaustedCount => Volatile.Read(ref _sendBufferPoolExhaustedCount);
        public IPacketEncoding Encoding { get; } = new PacketEncoding168();

        public PacketProcessor(GameClient client)
        {
            _client = client;
            CreateTcpSendArgs();
            CreateUdpSendArgs();
            LoadPacketHandlers();

            void CreateTcpSendArgs()
            {
                for (int i = 0; i < SEND_ARGS_POOL_SIZE; i++)
                {
                    SocketAsyncEventArgs args = new();
                    args.SetBuffer(new byte[BaseClient.TCP_SEND_BUFFER_SIZE], 0, 0);
                    args.Completed += OnTcpSendCompletion;
                    _tcpSendArgsPool.Enqueue(args);
                }
            }

            void CreateUdpSendArgs()
            {
                for (int i = 0; i < SEND_ARGS_POOL_SIZE; i++)
                {
                    SocketAsyncEventArgs args = new();
                    args.SetBuffer(new byte[BaseClient.UDP_SEND_BUFFER_SIZE], 0, 0);
                    args.Completed += OnUdpSendCompletion;
                    _udpSendArgsPool.Enqueue(args);
                }
            }

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

        public void ProcessInboundPacket(GSPacketIn packet)
        {
            int code = packet.Code;

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
                        log.Warn($"Long {nameof(PacketProcessor)}.{nameof(ProcessInboundPacket)} ({(eClientPackets) packet.Code}) for: {_client.Player?.Name}({_client.Player?.ObjectID}) Time: {stopTick - startTick}ms");
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
            if (!_client.Socket.Connected)
            {
                packet.ReleasePooledObject();
                return;
            }

            // This is dangerous if the same packet is passed down to multiple `PacketProcessor` at the same time.
            if (!packet.IsSizeSet)
                packet.WritePacketLength();

            try
            {
                _tcpPacketQueue.Add(packet);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(QueuePacket)} failed when adding to {nameof(_tcpPacketQueue)}. Skipping this packet.", e);
            }
        }

        public void QueuePacket(GSUDPPacketOut packet, bool forced)
        {
            if (!_client.Socket.Connected)
            {
                packet.ReleasePooledObject();
                return;
            }

            // This is dangerous if the same packet is passed down to multiple `PacketProcessor` at the same time.
            if (!packet.IsSizeSet)
                packet.WritePacketLength();

            // If UDP is unavailable, send via TCP instead.
            if (_client.UdpEndPoint != null && (_client.UdpConfirm || forced) && GameServer.Instance.IsUdpSocketBound())
            {
                try
                {
                   _udpPacketQueue.Add(packet);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"{nameof(QueuePacket)} failed when adding to {nameof(_udpPacketQueue)}. Skipping this packet.", e);
                }
            }
            else
            {
                try
                {
                    _udpToTcpPacketQueue.Add(packet);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"{nameof(QueuePacket)} failed when adding to {nameof(_udpToTcpPacketQueue)}. Skipping this packet.", e);
                }
            }
        }

        public void SendPendingPackets()
        {
            if (!_client.Socket.Connected)
                return;

            try
            {
                _tcpPacketQueue.DrainTo(static (packet, processor) => processor.ProcessTcpPacket(packet), this);
                _udpToTcpPacketQueue.DrainTo(static (packet, processor) => processor.ProcessUdpAsTcpPacket(packet), this);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(SendPendingPackets)} failed during queue drain. Some packets may be lost.", e);
            }
            finally
            {
                if (_sendContext.CurrentArgs != null && _sendContext.Position > 0)
                    SendTcpAndResetContext();
            }

            try
            {
                _udpPacketQueue.DrainTo(static (packet, processor) => processor.ProcessUdpPacket(packet), this);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(SendPendingPackets)} failed during queue drain. Some packets may be lost.", e);
            }
            finally
            {
                if (_sendContext.CurrentArgs != null && _sendContext.Position > 0)
                    SendUdpAndResetContext();
            }
        }

        public void Dispose()
        {
            while (_tcpSendArgsPool.TryDequeue(out SocketAsyncEventArgs tcpSendArgs))
                tcpSendArgs.Dispose();

            while (_udpSendArgsPool.TryDequeue(out SocketAsyncEventArgs udpSendArgs))
                udpSendArgs.Dispose();

            // Drain all pending packets on the next game loop tick to avoid concurrent modification issues.
            GameLoopThreadPool.Context.Post(static state =>
            {
                PacketProcessor packetProcessor = state as PacketProcessor;
                packetProcessor._tcpPacketQueue.DrainTo(static packet => packet.ReleasePooledObject());
                packetProcessor._udpToTcpPacketQueue.DrainTo(static packet => packet.ReleasePooledObject());
                packetProcessor._udpPacketQueue.DrainTo(static packet => packet.ReleasePooledObject());
            }, this);
        }

        private void ProcessTcpPacket(GSTCPPacketOut packet)
        {
            try
            {
                byte[] packetBuffer = packet.GetBuffer();
                int packetSize = (int) packet.Length;

                if (!ValidatePacketSize(packetBuffer, packetSize))
                    return;

                if (_sendContext.CurrentArgs == null)
                {
                    _sendContext.CurrentArgs = GetAvailableTcpSendArgs();

                    if (_sendContext.CurrentArgs == null)
                        return;

                    _sendContext.Position = 0;
                }

                // If the current packet doesn't fit, send the current buffer and get a new one.
                if (_sendContext.Position + packetSize > _sendContext.CurrentArgs.Buffer.Length)
                {
                    SendTcpAndResetContext();
                    _sendContext.CurrentArgs = GetAvailableTcpSendArgs();

                    if (_sendContext.CurrentArgs == null)
                        return;
                }

                try
                {
                    Buffer.BlockCopy(packetBuffer, 0, _sendContext.CurrentArgs.Buffer, _sendContext.Position, packetSize);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error($"Failed to copy packet data to send buffer. " +
                            $"(Position: {_sendContext.Position}) " +
                            $"(Buffer Size: {_sendContext.CurrentArgs.Buffer.Length}) " +
                            $"(Client: {_client})\n" +
                            $"{packet.ToHumanReadable()}", e);
                    }

                    return;
                }

                _sendContext.Position += packetSize;
            }
            finally
            {
                packet.ReleasePooledObject();
            }
        }

        private void ProcessUdpAsTcpPacket(GSUDPPacketOut packet)
        {
            try
            {
                byte[] packetBuffer = packet.GetBuffer();
                int packetSize = (int) packet.Length - 2;

                if (!ValidatePacketSize(packetBuffer, packetSize))
                    return;

                if (_sendContext.CurrentArgs == null)
                {
                    _sendContext.CurrentArgs = GetAvailableTcpSendArgs();

                    if (_sendContext.CurrentArgs == null)
                        return;

                    _sendContext.Position = 0;
                }

                // If the current packet doesn't fit, send the current buffer and get a new one.
                if (_sendContext.Position + packetSize > _sendContext.CurrentArgs.Buffer.Length)
                {
                    SendTcpAndResetContext();
                    _sendContext.CurrentArgs = GetAvailableTcpSendArgs();

                    if (_sendContext.CurrentArgs == null)
                        return;
                }

                // Transform the UDP packet into a TCP one.
                try
                {
                    Buffer.BlockCopy(packetBuffer, 4, _sendContext.CurrentArgs.Buffer, _sendContext.Position + 2, packetSize - 2);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error($"Failed to copy packet data to send buffer. " +
                            $"(Position: {_sendContext.Position}) " +
                            $"(Buffer Size: {_sendContext.CurrentArgs.Buffer.Length}) " +
                            $"(Client: {_client})\n" +
                            $"{packet.ToHumanReadable()}", e);
                    }

                    return;
                }

                _sendContext.CurrentArgs.Buffer[_sendContext.Position] = packetBuffer[0];
                _sendContext.CurrentArgs.Buffer[_sendContext.Position + 1] = packetBuffer[1];
                _sendContext.Position += packetSize;
            }
            finally
            {
                packet.ReleasePooledObject();
            }
        }

        private void ProcessUdpPacket(GSUDPPacketOut packet)
        {
            try
            {
                byte[] packetBuffer = packet.GetBuffer();
                int packetSize = (int) packet.Length;

                if (!ValidatePacketSize(packetBuffer, packetSize))
                    return;

                if (_sendContext.CurrentArgs == null)
                {
                    _sendContext.CurrentArgs = GetAvailableUdpSendArgs();

                    if (_sendContext.CurrentArgs == null)
                        return;

                    _sendContext.Position = 0;
                }

                // If the current packet doesn't fit, send the current buffer and get a new one.
                if (_sendContext.Position + packetSize > _sendContext.CurrentArgs.Buffer.Length)
                {
                    SendUdpAndResetContext();
                    _sendContext.CurrentArgs = GetAvailableUdpSendArgs();

                    if (_sendContext.CurrentArgs == null)
                        return;
                }

                try
                {
                    Buffer.BlockCopy(packetBuffer, 0, _sendContext.CurrentArgs.Buffer, _sendContext.Position, packetSize);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                    {
                        log.Error($"Failed to copy packet data to send buffer. " +
                            $"(Position: {_sendContext.Position}) " +
                            $"(Buffer Size: {_sendContext.CurrentArgs.Buffer.Length}) " +
                            $"(Client: {_client})\n" +
                            $"{packet.ToHumanReadable()}", e);
                    }

                    return;
                }

                // Add `_udpCounter` to the packet's content. Let it overflow.
                _udpCounter++;
                _sendContext.CurrentArgs.Buffer[_sendContext.Position + 2] = (byte) (_udpCounter >> 8);
                _sendContext.CurrentArgs.Buffer[_sendContext.Position + 3] = (byte) _udpCounter;
                _sendContext.Position += packetSize;
            }
            finally
            {
                packet.ReleasePooledObject();
            }
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

            // Cannot enqueue packets here.
            GameLoopThreadPool.Context.Post(static state =>
            {
                var s = ((GameClient Client, byte Code, int Size)) state;
                s.Client.Out.SendMessage($"Oversized packet detected and discarded (code: 0x{s.Code:X2}) (size: {s.Size}). Please report this issue!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
            }, (_client, packetBuffer[2], packetSize));

            return false;
        }

        private void SendTcpAndResetContext()
        {
            try
            {
                if (!_client.Socket.Connected)
                {
                    OnFailure();
                    return;
                }

                _sendContext.CurrentArgs.SetBuffer(0, _sendContext.Position);

                if (!_client.SendAsync(_sendContext.CurrentArgs))
                    OnTcpSendCompletion(null, _sendContext.CurrentArgs);
            }
            catch (ObjectDisposedException)
            {
                OnFailure();
            }
            catch (SocketException e)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"Socket exception on TCP send (Client: {_client}) (Code: {e.SocketErrorCode})");

                OnFailure();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Unhandled exception on TCP send (Client: {_client}): {e}");

                OnFailure();
            }
            finally
            {
                _sendContext.CurrentArgs = null;
                _sendContext.Position = 0;
            }

            void OnFailure()
            {
                _tcpSendArgsPool.Enqueue(_sendContext.CurrentArgs);
            }
        }

        private void SendUdpAndResetContext()
        {
            try
            {
                if (!_client.Socket.Connected)
                {
                    OnFailure();
                    return;
                }

                _sendContext.CurrentArgs.SetBuffer(0, _sendContext.Position);

                if (!GameServer.Instance.SendUdp(_sendContext.CurrentArgs))
                    OnUdpSendCompletion(null, _sendContext.CurrentArgs);
            }
            catch (ObjectDisposedException)
            {
                OnFailure();
            }
            catch (SocketException e)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"Socket exception on UDP send (Client: {_client}) (Code: {e.SocketErrorCode})");

                OnFailure();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Unhandled exception on UDP send (Client: {_client}): {e}");

                OnFailure();
            }
            finally
            {
                _sendContext.CurrentArgs = null;
                _sendContext.Position = 0;
            }

            void OnFailure()
            {
                _client.UdpConfirm = false;
                _udpSendArgsPool.Enqueue(_sendContext.CurrentArgs);
            }
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

        private SocketAsyncEventArgs GetAvailableTcpSendArgs()
        {
            if (!_tcpSendArgsPool.TryDequeue(out SocketAsyncEventArgs args))
            {
                Interlocked.Increment(ref _sendBufferPoolExhaustedCount);
                return null;
            }

            return args;
        }

        private SocketAsyncEventArgs GetAvailableUdpSendArgs()
        {
            if (!_udpSendArgsPool.TryDequeue(out SocketAsyncEventArgs args))
            {
                Interlocked.Increment(ref _sendBufferPoolExhaustedCount);
                return null;
            }

            // UdpEndPoint shouldn't change, but can be set a bit late.
            args.RemoteEndPoint = _client.UdpEndPoint;
            return args;
        }

        private void OnTcpSendCompletion(object sender, SocketAsyncEventArgs args)
        {
            _tcpSendArgsPool.Enqueue(args);
        }

        private void OnUdpSendCompletion(object sender, SocketAsyncEventArgs args)
        {
            _udpSendArgsPool.Enqueue(args);
        }

        private class SendContext
        {
            public SocketAsyncEventArgs CurrentArgs;
            public int Position;
        }
    }
}
