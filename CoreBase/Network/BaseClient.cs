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

        // This is mostly important for outbound UDP packets since the client seems to discard some content if its internal buffer is full.
        // Thus if some UDP packets appear to be ignored, try lowering `SEND_UDP_BUFFER_SIZE`.
        // Outbound TCP packets on the other hand seem to always be processed.
        // Inbound TCP packets are generally small and the buffer size irrelevant.
        public const int SEND_BUFFER_SIZE = 8192;
        public const int RECEIVE_BUFFER_SIZE = 1024;
        private const int SOCKET_SEND_BUFFER_SIZE = 8192;
        private const int SOCKET_RECEIVE_BUFFER_SIZE = 1024;

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
                socket.SendBufferSize = SOCKET_SEND_BUFFER_SIZE;
                socket.ReceiveBufferSize = SOCKET_RECEIVE_BUFFER_SIZE;
                socket.NoDelay = true;
                Socket = socket;
            }

            ReceiveBuffer = new byte[RECEIVE_BUFFER_SIZE];
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
            if (!Socket.Connected)
                return;

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

            int remaining = ReceiveBuffer.Length - ReceiveBufferOffset;

            if (remaining <= 0)
            {
                if (remaining == 0)
                    return;

                if (log.IsErrorEnabled)
                    log.Error($"Disconnecting client because of receive buffer overflow. (Client: {this})");

                Disconnect();
                return;
            }

            try
            {
                _receiveArgs.SetBuffer(ReceiveBufferOffset, ReceiveBuffer.Length - ReceiveBufferOffset);

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
