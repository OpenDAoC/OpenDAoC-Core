using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DOL.Config;

namespace DOL.Network
{
    public class BaseServer
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public static readonly Encoding DefaultEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);

        [ThreadStatic]
        private static Encoder _encoder;

        private const int UDP_MAX_DATAGRAM_SIZE = 2048;
        private const int UDP_PACKET_BUFFER_COUNT = 256;
        private const int UDP_RECEIVE_TIMEOUT_MS = 1000;

        private Socket _listen;
        private Socket _udpSocket;
        private Channel<byte[]> _udpBufferChannel;
        private CancellationTokenSource _udpReceiveCts;
        private Task _udpReceiveTask;
        private SessionIdAllocator _sessionIdAllocator = new();

        private long _udpPacketsReceived;
        private long _udpPacketsDropped;
        private long _udpReceiveErrors;
        private long _udpBufferReturnFailures;

        public BaseServerConfig Configuration { get; }
        public bool IsRunning => _listen != null; // Not a great way to check if the server is running.
        public long UdpPacketsReceived => Volatile.Read(ref _udpPacketsReceived);
        public long UdpPacketsDropped => Volatile.Read(ref _udpPacketsDropped);
        public long UdpReceiveErrors => Volatile.Read(ref _udpReceiveErrors);
        public long UdpBufferReturnFailures => Volatile.Read(ref _udpBufferReturnFailures);

        protected BaseServer(BaseServerConfig config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));
        }

        public static Encoder GetEncoder()
        {
            // Returns a thread static `Encoder` that must be reset after use.
            return _encoder ??= DefaultEncoding.GetEncoder();
        }

        public virtual bool Start()
        {
            if (!InitializeListenSocket())
                return false;

            InitializeUdpSocket();

            if (Configuration.EnableUPnP)
                ConfigureUpnp();

            if (!StartListen())
                return false;

            StartUdpThread();
            return true;
        }

        private void ConfigureUpnp()
        {
            try
            {
                UpnpNat nat = new();

                if (!nat.Discover())
                    throw new Exception("[UPNP] Unable to access the UPnP Internet Gateway Device");

                if (log.IsDebugEnabled)
                {
                    log.Debug("[UPNP] Current UPnP mappings:");

                    foreach (UpnpNat.PortForwarding info in nat.ListForwardedPort())
                        log.Debug($"[UPNP] {info.description} - {info.externalPort} -> {info.internalIP}:{info.internalPort}({info.protocol}) ({(info.enabled ? "enabled" : "disabled")})");
                }

                IPAddress localAddress = Configuration.IP;
                nat.ForwardPort(Configuration.UDPPort, Configuration.UDPPort, ProtocolType.Udp, "DOL UDP", localAddress);
                nat.ForwardPort(Configuration.Port, Configuration.Port, ProtocolType.Tcp, "DOL TCP", localAddress);

                if (Configuration.DetectRegionIP)
                {
                    try
                    {
                        Configuration.RegionIP = nat.GetExternalIP();

                        if (log.IsDebugEnabled)
                            log.Debug($"[UPNP] Found the RegionIP: {Configuration.RegionIP}");
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("[UPNP] Unable to detect the RegionIP, It is possible that no mappings exist yet", e);
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ConfigureUpnp)}", e);
            }
        }

        private bool StartListen()
        {
            try
            {
                if (!_listen.IsBound)
                    return false;

                _listen.Listen(100);

                if (log.IsInfoEnabled)
                    log.Info($"Server is now listening for incoming connections on {Configuration.IP}:{Configuration.Port}");

                SocketAsyncEventArgs listenArgs = CreateSocketAsyncEventArgs();

                while (!_listen.AcceptAsync(listenArgs))
                    OnListenCompletion(listenArgs);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);

                _listen?.Close();
                return false;
            }

            return true;

            SocketAsyncEventArgs CreateSocketAsyncEventArgs()
            {
                SocketAsyncEventArgs listenArgs = new();
                listenArgs.Completed += OnAsyncListenCompletion;
                return listenArgs;
            }
        }

        private void StartUdpThread()
        {
            if (!IsUdpSocketBound())
                return;

            _udpBufferChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(UDP_PACKET_BUFFER_COUNT)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false
            });

            for (int i = 0; i < UDP_PACKET_BUFFER_COUNT; i++)
                _udpBufferChannel.Writer.TryWrite(new byte[UDP_MAX_DATAGRAM_SIZE + 1]);

            _udpReceiveCts = new();
            _udpReceiveTask = ReceiveUdpAsync(_udpReceiveCts.Token);
        }

        private void OnAsyncListenCompletion(object sender, SocketAsyncEventArgs listenArgs)
        {
            OnListenCompletion(listenArgs);

            try
            {
                while (_listen != null && !_listen.AcceptAsync(listenArgs))
                    OnListenCompletion(listenArgs);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);

                _listen?.Close();
            }
        }

        private void OnListenCompletion(SocketAsyncEventArgs listenArgs)
        {
            BaseClient baseClient = null;
            Socket socket = listenArgs.AcceptSocket;

            try
            {
                if (listenArgs.SocketError is SocketError.ConnectionReset)
                    return;

                SessionId sessionId = new(_sessionIdAllocator);
                baseClient = GetNewClient(socket);
                baseClient.OnConnect(sessionId); // Must be called before `Receive` since `Receive` ends up calling `OnDisconnect` if it fails.
                // Don't call `Receive` here, the client service may be already doing it and it isn't thread safe.
            }
            catch (Exception e)
            {
                try
                {
                    if (log.IsErrorEnabled)
                        log.Error(e);

                    baseClient?.Disconnect();
                }
                catch
                {
                    // May end up be called twice, but this is fine.
                    socket?.Close();
                }
            }
            finally
            {
                listenArgs.AcceptSocket = null;
            }
        }

        private async Task ReceiveUdpAsync(CancellationToken token)
        {
            Socket socket = _udpSocket;

            if (socket == null || _udpBufferChannel == null)
                return;

            try
            {
                SocketAddress sourceAddress = new(AddressFamily.InterNetwork);

                while (!token.IsCancellationRequested)
                {
                    byte[] buffer = await _udpBufferChannel.Reader.ReadAsync(token).ConfigureAwait(false);
                    bool handedOff = false;

                    try
                    {
                        int size = await socket.ReceiveFromAsync(buffer.AsMemory(0, UDP_MAX_DATAGRAM_SIZE + 1), SocketFlags.None, sourceAddress, token).ConfigureAwait(false);

                        Interlocked.Increment(ref _udpPacketsReceived);

                        if (size <= 0 || size > UDP_MAX_DATAGRAM_SIZE || !UdpSource.TryParse(sourceAddress, out UdpSource source))
                        {
                            Interlocked.Increment(ref _udpPacketsDropped);
                            continue;
                        }

                        handedOff = OnUdpReceive(buffer, size, source);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (SocketException e)
                    {
                        Interlocked.Increment(ref _udpReceiveErrors);

                        if (IsRecoverableUdpReceiveError(e.SocketErrorCode))
                        {
                            if (log.IsDebugEnabled)
                                log.Debug($"Recoverable socket exception on UDP receive (Code: {e.SocketErrorCode})");

                            continue;
                        }

                        if (log.IsDebugEnabled)
                            log.Debug($"Socket exception on UDP receive (Code: {e.SocketErrorCode})");

                        if (e.SocketErrorCode is SocketError.OperationAborted or SocketError.Interrupted or SocketError.NotSocket)
                            break;

                        // NetworkDown / NetworkReset / NoBufferSpaceAvailable...
                        // Wait a bit before trying again to avoid spamming the log with errors.
                        await Task.Delay(3000, token).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Interlocked.Increment(ref _udpReceiveErrors);

                        if (log.IsErrorEnabled)
                            log.Error(e);
                    }
                    finally
                    {
                        if (!handedOff)
                            ReturnUdpPacketBuffer(buffer);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (ChannelClosedException) { }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }
        }

        private static bool IsRecoverableUdpReceiveError(SocketError error)
        {
            return error is SocketError.MessageSize
                or SocketError.ConnectionReset
                or SocketError.ConnectionRefused
                or SocketError.NetworkUnreachable
                or SocketError.HostUnreachable;
        }

        protected bool ReturnUdpPacketBuffer(byte[] buffer)
        {
            if (buffer != null && _udpBufferChannel?.Writer.TryWrite(buffer) == true)
                return true;

            Interlocked.Increment(ref _udpBufferReturnFailures);
            return false;
        }

        protected virtual BaseClient GetNewClient(Socket socket)
        {
            return new(socket);
        }

        protected virtual bool InitializeListenSocket()
        {
            try
            {
                _listen = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listen.Bind(new IPEndPoint(Configuration.IP, Configuration.Port));
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);

                return false;
            }

            return true;
        }

        protected virtual bool InitializeUdpSocket()
        {
            try
            {
                _udpSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _udpSocket.Bind(new IPEndPoint(Configuration.UDPIP, Configuration.UDPPort));
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);

                return false;
            }

            return true;
        }

        public bool IsUdpSocketBound()
        {
            return _udpSocket != null && _udpSocket.IsBound;
        }

        public bool SendUdp(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            return _udpSocket.SendToAsync(socketAsyncEventArgs);
        }

        public virtual void Stop()
        {
            try
            {
                _udpReceiveCts?.Cancel();
                _udpBufferChannel?.Writer.TryComplete();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }

            try
            {
                if (_udpSocket != null)
                {
                    _udpSocket.Close();
                    _udpSocket = null;
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }

            try
            {
                _udpReceiveTask?.Wait(UDP_RECEIVE_TIMEOUT_MS);
            }
            catch (AggregateException) { }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }

            try
            {
                _udpReceiveCts?.Dispose();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }

            _udpReceiveCts = null;
            _udpReceiveTask = null;
            _udpBufferChannel = null;

            try
            {
                if (_listen != null)
                {
                    _listen.Close();
                    _listen = null;
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }
        }

        protected virtual bool OnUdpReceive(byte[] buffer, int size, UdpSource source)
        {
            return false;
        }
    }
}
