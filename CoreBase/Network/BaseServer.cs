using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using DOL.Config;
using log4net;

namespace DOL.Network
{
    /// <summary>
    /// Base class for a server using overlapped socket IO.
    /// </summary>
    public class BaseServer
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Holds the async accept callback delegate
        /// </summary>
        private readonly AsyncCallback _asyncAcceptCallback;

        /// <summary>
        /// The configuration of this server
        /// </summary>
        protected BaseServerConfig _config;

        /// <summary>
        /// Socket that receives connections
        /// </summary>
        protected Socket _listen;

        /// <summary>
        /// Constructor that takes a server configuration as parameter
        /// </summary>
        /// <param name="config">The configuraion for the server</param>
        protected BaseServer(BaseServerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _asyncAcceptCallback = new AsyncCallback(AcceptCallback);
        }

        /// <summary>
        /// Retrieves the server configuration
        /// </summary>
        public virtual BaseServerConfig Configuration => _config;

        /// <summary>
        /// Creates a new client object
        /// </summary>
        /// <returns>A new client object</returns>
        protected virtual BaseClient GetNewClient()
        {
            return new BaseClient(this);
        }

        /// <summary>
        /// Used to get packet buffer.
        /// </summary>
        /// <returns>byte array that will be used as packet buffer.</returns>
        public virtual byte[] AcquirePacketBuffer()
        {
            return new byte[2048];
        }

        /// <summary>
        /// Releases previously acquired packet buffer.
        /// </summary>
        public virtual void ReleasePacketBuffer(byte[] buf) { }

        /// <summary>
        /// Initializes and binds the socket, doesn't listen yet!
        /// </summary>
        /// <returns>true if bound</returns>
        protected virtual bool InitSocket()
        {
            try
            {
                _listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listen.Bind(new IPEndPoint(_config.IP, _config.Port));
            }
            catch (Exception e)
            {
                Log.Error("InitSocket", e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Starts the server
        /// </summary>
        /// <returns>True if the server was successfully started</returns>
        public virtual bool Start()
        {
            if (Configuration.EnableUPnP)
            {
                try
                {
                    UpnpNat nat = new();

                    if (!nat.Discover())
                        throw new Exception("[UPNP] Unable to access the UPnP Internet Gateway Device");

                    Log.Debug("[UPNP] Current UPnP mappings:");

                    foreach (var info in nat.ListForwardedPort())
                    {
                        Log.DebugFormat("[UPNP] {0} - {1} -> {2}:{3}({4}) ({5})",
                                        info.description,
                                        info.externalPort,
                                        info.internalIP,
                                        info.internalPort,
                                        info.protocol,
                                        info.enabled ? "enabled" : "disabled");
                    }

                    IPAddress localAddress = Configuration.IP;
                    nat.ForwardPort(Configuration.UDPPort, Configuration.UDPPort, ProtocolType.Udp, "DOL UDP", localAddress);
                    nat.ForwardPort(Configuration.Port, Configuration.Port, ProtocolType.Tcp, "DOL TCP", localAddress);

                    if (Configuration.DetectRegionIP)
                    {
                        try
                        {
                            Configuration.RegionIP = nat.GetExternalIP();
                            Log.Debug("[UPNP] Found the RegionIP: " + Configuration.RegionIP);
                        }
                        catch(Exception e)
                        {
                            Log.Warn("[UPNP] Unable to detect the RegionIP, It is possible that no mappings exist yet", e);
                        }
                    }
                }
                catch(Exception e)
                {
                    Log.Warn(e.Message, e);
                }
            }

            if (_listen == null && !InitSocket())
                return false;

            try
            {
                _listen.Listen(100);
                _listen.BeginAccept(_asyncAcceptCallback, this);
                Log.Info("Server is now listening to incoming connections!");
            }
            catch (Exception e)
            {
                Log.Error("Start", e);
                _listen?.Close();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called when a client is trying to connect to the server
        /// </summary>
        /// <param name="ar">Async result of the operation</param>
        private void AcceptCallback(IAsyncResult ar)
        {
            Socket socket = null;

            try
            {
                if (_listen == null)
                    return;

                socket = _listen.EndAccept(ar);
                socket.SendBufferSize = Constants.SendBufferSize;
                socket.ReceiveBufferSize = Constants.ReceiveBufferSize;
                socket.NoDelay = Constants.UseNoDelay;
                BaseClient baseClient = null;

                try
                {
                    // Removing this message in favor of connection message in GameClient
                    // This will also reduce spam when server is pinged with 0 bytes - Tolakram
                    //string ip = sock.Connected ? sock.RemoteEndPoint.ToString() : "socket disconnected";
                    //Log.Info("Incoming connection from " + ip);

                    baseClient = GetNewClient();
                    baseClient.Socket = socket;
                    baseClient.OnConnect();
                    baseClient.BeginReceive();
                }
                catch (SocketException)
                {
                    Log.Error("BaseServer SocketException");
                    if (baseClient != null)
                        Disconnect(baseClient);
                }
                catch (Exception e)
                {
                    Log.Error("Client creation", e);

                    if (baseClient != null)
                        Disconnect(baseClient);
                }
            }
            catch
            {
                Log.Error("AcceptCallback: Catch");

                if (socket != null) // Don't leave the socket open on exception
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
                _listen?.BeginAccept(_asyncAcceptCallback, this);
            }
        }

        /// <summary>
        /// Stops the server
        /// </summary>
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
                    Socket socket = _listen;
                    _listen = null;
                    socket.Close();

                    Log.Info("Server is no longer listening for incoming connections");
                }
            }
            catch (Exception e)
            {
                Log.Error("Stop", e);
            }

            Log.Info("Server stopped");
        }

        /// <summary>
        /// Disconnects a client
        /// </summary>
        /// <param name="baseClient">Client to be disconnected</param>
        /// <returns>True if the client was disconnected, false if it doesn't exist</returns>
        public virtual bool Disconnect(BaseClient baseClient)
        {
            try
            {
                baseClient.OnDisconnect();
                baseClient.CloseConnections();
            }
            catch (Exception e)
            {
                Log.Error("Exception", e);
                return false;
            }

            return true;
        }
    }
}
