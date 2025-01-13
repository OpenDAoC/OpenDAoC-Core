using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DOL.Config;
using log4net;

namespace DOL.Network
{
    public class BaseServer
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly Encoding defaultEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1252);

        private const int UDP_RECEIVE_BUFFER_SIZE = 8192;
        private const int UDP_RECEIVE_BUFFER_CHUNK_SIZE = 64; // This should be increased if someday clients send UDP packets larger than this.
        private const string UDP_THREAD_NAME = "UDP";

        private Socket _listen;
        private Socket _udpSocket;
        private ConcurrentQueue<SocketAsyncEventArgs> _udpReceiveArgsPool = new();
        private SocketAsyncEventArgs _udpReceiveArgs;
        private static Thread _udpThread;

        public BaseServerConfig Configuration { get; }
        public bool IsRunning => _listen != null; // Not a great way to check if the server is running.

        protected BaseServer(BaseServerConfig config)
        {
            Configuration = config ?? throw new ArgumentNullException(nameof(config));
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

            void ConfigureUpnp()
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
                        log.Error(e.Message, e);
                }
            }

            bool StartListen()
            {
                try
                {
                    if (!_listen.IsBound)
                        return false;

                    _listen.Listen(100);
                    log.Info("Server is now listening to incoming connections!");
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

            void StartUdpThread()
            {
                if (!_udpSocket.IsBound)
                    return;

                ConcurrentQueue<int> availablePositions = [];

                for (int i = 0; i < UDP_RECEIVE_BUFFER_SIZE; i += UDP_RECEIVE_BUFFER_CHUNK_SIZE)
                    availablePositions.Enqueue(i);

                EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] buffer = new byte[UDP_RECEIVE_BUFFER_SIZE];
                int position;

                // This is probably a bit more complicated than it should be if we consider the fact that clients only send UDP packets to notify the server that they can receive UDP packets.
                // Since only one buffer is used and shared, this requires some synchronization to prevent `ReceiveFromAsync` from overwriting data that isn't processed yet.
                // For this reason, the buffer is split in chunks of `UDP_RECEIVE_BUFFER_CHUNK_SIZE` bytes. This assumes no packet can be larger than this.
                // Keep in mind that this is ran by worker threads, outside of the game loop, which may cause issues if clients start sending other packets this way.

                Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            // Spinning isn't great, but clients shouldn't send enough packets, or the buffer size be small enough, or tasks take long enough for this to happen regularly.
                            while (!availablePositions.TryDequeue(out position))
                                Thread.Yield();

                            int offset = position;
                            SocketReceiveFromResult result = await _udpSocket.ReceiveFromAsync(new ArraySegment<byte>(buffer, offset, UDP_RECEIVE_BUFFER_CHUNK_SIZE), endPoint);

                            _ = Task.Run(() =>
                            {
                                OnUdpReceive(buffer, offset, result.ReceivedBytes, result.RemoteEndPoint, FreeBufferPosition);

                                void FreeBufferPosition()
                                {
                                    availablePositions.Enqueue(offset);
                                }
                            });

                            continue;
                        }
                        catch (ObjectDisposedException)
                        {
                            _udpThread = null;
                        }
                        catch (SocketException e)
                        {
                            if (log.IsDebugEnabled)
                                log.Debug($"Socket exception on UDP receive (Code: {e.SocketErrorCode})");
                        }
                        catch (Exception e)
                        {
                            if (log.IsErrorEnabled)
                                log.Error(e);

                            _udpThread = null;

                            if (_udpSocket != null)
                            {
                                try
                                {
                                    _udpSocket.Close();
                                }
                                catch (Exception) { }
                            }
                        }

                        return;
                    }
                });
            }

            void OnAsyncListenCompletion(object sender, SocketAsyncEventArgs listenArgs)
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

            void OnListenCompletion(SocketAsyncEventArgs listenArgs)
            {
                BaseClient baseClient = null;
                Socket socket = listenArgs.AcceptSocket;

                try
                {
                    if (listenArgs.SocketError is SocketError.ConnectionReset)
                        return;

                    baseClient = GetNewClient(socket);
                    baseClient.OnConnect(); // Must be called before `Receive` since `Receive` ends up calling `OnDisconnect` if it fails.
                    // Don't call `Receive` here, the client service may be already doing it and it isn't thread safe.
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error(e);

                    if (baseClient != null)
                        Disconnect(baseClient);

                    if (socket != null)
                    {
                        try
                        {
                            socket.Close();
                        }
                        catch { }
                    }
                }
                finally
                {
                    listenArgs.AcceptSocket = null;
                }
            }
        }

        protected virtual BaseClient GetNewClient(Socket socket)
        {
            return new BaseClient(this, socket);
        }

        protected virtual bool InitializeListenSocket()
        {
            try
            {
                _listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
                _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
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

        public bool SendUdp(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            return _udpSocket.SendToAsync(socketAsyncEventArgs);
        }

        public virtual void Stop()
        {
            /*if(Configuration.EnableUPNP)
            {
                try
                {
                    if(Log.IsDebugEnabled)
                        Log.Debug("Removing UPnP Mappings");
                    UPnPNat nat = new UPnPNat();
                    PortMappingInfo pmiUDP = new PortMappingInfo("UDP", Configuration.UDPPort);
                    PortMappingInfo pmiTCP = new PortMappingInfo("TCP", Configuration.Port);
                    nat.RemovePortMapping(pmiUDP);
                    nat.RemovePortMapping(pmiTCP);
                }
                catch(Exception ex)
                {
                    if(Log.IsDebugEnabled)
                        Log.Debug("Failed to remove UPnP Mappings", ex);
                }
            }*/

            try
            {
                if (_listen != null)
                {
                    _listen.Close();
                    _listen = null;
                    log.Info("Server is no longer listening for incoming connections");
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }

            if (_udpThread != null)
            {
                _udpThread.Interrupt();
                _udpThread.Join();
                _udpThread = null;
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

            if (log.IsInfoEnabled)
                log.Info("Server stopped");
        }

        public virtual bool Disconnect(BaseClient baseClient)
        {
            try
            {
                baseClient.OnDisconnect();
                baseClient.CloseConnections();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("Exception", e);

                return false;
            }

            return true;
        }

        protected virtual void OnUdpReceive(byte[] buffer, int offset, int size, EndPoint endPoint, Action freeBufferCallback) { }
    }
}
