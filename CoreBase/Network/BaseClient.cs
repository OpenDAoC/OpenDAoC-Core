using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using log4net;

namespace DOL.Network
{
    public class BaseClient
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public const int TCP_SEND_BUFFER_SIZE = 8192;
        public const int UDP_SEND_BUFFER_SIZE = 1024;
        // To prevent fragmentation (bad with UDP), this should be smaller than Ethernet's MTU (1500) minus headers (20 for IPv4, 8 for UDP).
        // But other restrictions may apply, so leaving a reasonable margin is advisable.

        private const int TCP_RECEIVE_BUFFER_SIZE = 1024;
        // UDP_RECEIVE_BUFFER_SIZE is in `BaseServer`.

        private BaseServer _server;
        private SocketAsyncEventArgs _receiveArgs = new();
        private bool _isReceivingAsync;
        private long _isReceivingAsyncCompleted; // Use `ReceivingAsyncCompleted` instead.

        public Socket Socket { get; }
        public byte[] ReceiveBuffer { get; }
        public int ReceiveBufferOffset { get; set; }

        private bool ReceivingAsyncCompleted
        {
            get => Interlocked.Read(ref _isReceivingAsyncCompleted) == 1;
            set => Interlocked.Exchange(ref _isReceivingAsyncCompleted, Convert.ToInt64(value));
        }

        public string TcpEndpointAddress
        {
            get
            {
                if (Socket != null && Socket.Connected && Socket.RemoteEndPoint is IPEndPoint ipEndPoint)
                    return ipEndPoint.Address.ToString();

                return "[not connected]";
            }
        }

        public BaseClient(BaseServer server, Socket socket)
        {
            _server = server;

            if (socket != null)
            {
                socket.NoDelay = true;
                Socket = socket;
            }

            ReceiveBuffer = new byte[TCP_RECEIVE_BUFFER_SIZE];
            _receiveArgs.SetBuffer(ReceiveBuffer, 0, ReceiveBuffer.Length);
            _receiveArgs.Completed += OnAsyncReceiveCompletion;

            void OnAsyncReceiveCompletion(object sender, SocketAsyncEventArgs tcpReceiveArgs)
            {
                ReceivingAsyncCompleted = true;
            }
        }

        protected virtual void OnReceive(int size) { }

        public virtual void OnConnect() { }

        public virtual void OnDisconnect() { }

        public void Receive()
        {
            // If an async operation is running, wait for it to be completed.
            // We could work with a new `SocketAsyncEventArgs` instead, but the implementation is tricky and I don't think there would be any benefit.
            // We could also let the callback call `OnReceiveCompletion`, but the packet would be then processed outside of the game loop.
            if (_isReceivingAsync)
            {
                if (!ReceivingAsyncCompleted)
                    return;

                OnReceiveCompletion();
                _isReceivingAsync = false;
            }

            // Must be checked after calling `OnReceiveCompletion`.
            if (!Socket.Connected)
                return;

            int available = ReceiveBuffer.Length - ReceiveBufferOffset;

            if (available <= 0)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Disconnecting client because of receive buffer overflow. (Client: {this}) (Available: {available})");

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
            catch (ObjectDisposedException) { }
            catch (SocketException e)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"Socket exception on TCP receive (Client: {this}) (Code: {e.SocketErrorCode})");

                Disconnect();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Unhandled exception on TCP receive (Client: {this}): {e}");

                Disconnect();
            }
        }

        private void OnReceiveCompletion()
        {
            int received = _receiveArgs.BytesTransferred;

            if (received <= 0)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"Disconnecting client because of 0 bytes received. (Client: {this}");

                Disconnect();
                return;
            }

            OnReceive(received);
        }

        public void CloseConnections()
        {
            if (Socket != null)
            {
                if (Socket.Connected)
                {
                    try
                    {
                        Socket.Shutdown(SocketShutdown.Send);
                    }
                    catch { }
                }

                try
                {
                    Socket.Close();
                }
                catch { }
            }
        }

        public void Disconnect()
        {
            try
            {
                _server.Disconnect(this);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error("Exception", e);
            }
        }
    }
}
