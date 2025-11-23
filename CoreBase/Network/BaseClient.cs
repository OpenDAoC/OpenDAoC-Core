using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;

namespace DOL.Network
{
    public class BaseClient
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const int TCP_SEND_BUFFER_SIZE = 8192;
        public const int UDP_SEND_BUFFER_SIZE = 1024;
        // To prevent fragmentation (bad with UDP), this should be smaller than Ethernet's MTU (1500) minus headers (20 for IPv4, 8 for UDP).
        // But other restrictions may apply, so leaving a reasonable margin is advisable.

        private const int TCP_RECEIVE_BUFFER_SIZE = 1024;
        // UDP_RECEIVE_BUFFER_SIZE is in `BaseServer`.

        private SocketAsyncEventArgs _receiveArgs = new();
        private bool _isReceivingAsync;
        private long _isReceivingAsyncCompleted; // Use `ReceivingAsyncCompleted` instead.

        public Socket Socket { get; }
        public byte[] ReceiveBuffer { get; }
        public int ReceiveBufferOffset { get; set; }
        public SessionId SessionId { get; private set; }

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

        public BaseClient(Socket socket)
        {
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
                // Processing packets should be done by the game loop.
                ReceivingAsyncCompleted = true;
            }
        }

        protected virtual void OnReceive(int size) { }

        public virtual void OnConnect(SessionId sessionId)
        {
            SessionId = sessionId;
        }

        protected virtual void OnDisconnect()
        {
            SessionId.Dispose();
        }

        public bool SendAsync(SocketAsyncEventArgs tcpSendArgs)
        {
            return Socket.SendAsync(tcpSendArgs);
        }

        public void Receive()
        {
            if (Socket?.Connected != true)
            {
                Disconnect();
                return;
            }

            // If an async operation is running, wait for it to be completed.
            // We could work with a new `SocketAsyncEventArgs` instead, but the implementation is tricky and I don't think there would be any benefit.
            // We could also let the callback call `OnReceiveCompletion`, but the packet would be then processed outside of the game loop.
            if (_isReceivingAsync)
            {
                if (!ReceivingAsyncCompleted)
                    return;

                _isReceivingAsync = false;

                if (!OnReceiveCompletion())
                    return;
            }

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

        private bool OnReceiveCompletion()
        {
            int received = _receiveArgs.BytesTransferred;

            if (received <= 0)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"Disconnecting client because of 0 bytes received. (Client: {this})");

                Disconnect();
                return false;
            }

            OnReceive(received);
            return true;
        }

        public void Disconnect()
        {
            _receiveArgs.Dispose();

            try
            {
                OnDisconnect();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }
            finally
            {
                CloseSocket();
            }
        }

        protected void CloseSocket()
        {
            if (Socket == null)
                return;

            try
            {
                if (Socket.Connected)
                    Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }
            finally
            {
                Socket.Close();
            }
        }
    }
}
