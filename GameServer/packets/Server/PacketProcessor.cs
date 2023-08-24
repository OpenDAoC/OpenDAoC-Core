/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

#define LOGACTIVESTACKS

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using DOL.GS.ServerProperties;
using DOL.Network;
using log4net;
using Timer = System.Timers.Timer;

namespace DOL.GS.PacketHandler
{
    /// <summary>
    /// This class handles the packets, receiving and sending
    /// </summary>
    public class PacketProcessor
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Sync Lock Object
        /// </summary>
        private readonly object m_SyncLock = new object();
        
        /// <summary>
        /// Holds the current client for this processor
        /// </summary>
        protected readonly GameClient m_client;

        /// <summary>
        /// Holds the encoding used to encrypt/decrypt the packets
        /// </summary>
        protected readonly IPacketEncoding m_encoding;

        /// <summary>
        /// Stores all packet handlers found when searching the gameserver assembly
        /// </summary>
        protected IPacketHandler[] m_packetHandlers = new IPacketHandler[256];

        /// <summary>
        /// currently active packet handler
        /// </summary>
        protected IPacketHandler m_activePacketHandler;

        /// <summary>
        /// thread id of running packet handler
        /// </summary>
        protected int m_handlerThreadID;

        /// <summary>
        /// packet preprocessor that performs initial packet checks for this Packet Processor.
        /// </summary>
        protected PacketPreprocessing m_packetPreprocessor;

        /// <summary>
        /// Constructs a new PacketProcessor
        /// </summary>
        /// <param name="client">The processor client</param>
        public PacketProcessor(GameClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            m_client = client;
            m_packetPreprocessor = new PacketPreprocessing();

            LoadPacketHandlers(client);

            m_udpCounter = 0;
            //TODO set encoding based on client version in the future :)
            m_encoding = new PacketEncoding168();
            m_asyncUdpCallback = new AsyncCallback(AsyncUdpSendCallback);
            m_tcpSendBuffer = client.Server.AcquirePacketBuffer();
            m_udpSendBuffer = client.Server.AcquirePacketBuffer();
            
        }

        #region Last Packets

        /// <summary>
        /// The count of last sent/received packets to keep.
        /// </summary>
        protected const int MAX_LAST_PACKETS = 16;

        /// <summary>
        /// Holds the last sent/received packets.
        /// </summary>
        protected readonly Queue<IPacket> m_lastPackets = new Queue<IPacket>(MAX_LAST_PACKETS);

        /// <summary>
        /// Saves the sent packet for debugging
        /// </summary>
        /// <param name="pak">The sent packet</param>
        protected void SavePacket(IPacket pak)
        {
            lock (((ICollection)m_lastPackets).SyncRoot)
            {
                while (m_lastPackets.Count >= MAX_LAST_PACKETS)
                    m_lastPackets.Dequeue();

                m_lastPackets.Enqueue(pak);
            }
        }

        /// <summary>
        /// Makes a copy of last sent/received packets.
        /// </summary>
        /// <returns></returns>
        public IPacket[] GetLastPackets()
        {
            lock (((ICollection)m_lastPackets).SyncRoot)
            {
                return m_lastPackets.ToArray();
            }
        }

        #endregion

        /// <summary>
        /// Gets the encoding for this processor
        /// </summary>
        public IPacketEncoding Encoding
        {
            get { return m_encoding; }
        }

        /// <summary>
        /// Caches packet handlers loaded for a given client version (in string format, used for namespace search).
        /// </summary>
        private static Dictionary<string, IPacketHandler[]> m_cachedPacketHandlerSearchResults = new Dictionary<string, IPacketHandler[]>();
        /// <summary>
        /// Stores packet handler attributes for each version, required to load preprocessors.
        /// </summary>
        private static Dictionary<string, List<PacketHandlerAttribute>> m_cachedPreprocessorSearchResults = new Dictionary<string, List<PacketHandlerAttribute>>();
        private static object m_packetHandlerCacheLock = new object();

        public virtual void LoadPacketHandlers(GameClient client)
        {
            string baseVersion = "v168";
            //String may seem cumbersome but I would like to leave the open of custom clients open without core modification (for this reason I cannot use eClientVersion).
            //Also I am merely reusing some already written search functionality, which searches a namespace and thus expects a string.

            List<PacketHandlerAttribute> attributes = new List<PacketHandlerAttribute>();
            LoadPacketHandlers(baseVersion, out m_packetHandlers, out attributes);

            //todo: load different handlers for cumulative client versions, overwriting duplicate entries in m_PacketHandlers with later version.

            //Add preprocessors for each packet handler
            foreach (PacketHandlerAttribute pha in attributes)
            {
                m_packetPreprocessor.RegisterPacketDefinition(pha.Code, pha.PreprocessorID);
            }
        }

        /// <summary>
        /// Loads packet handlers to be used for handling incoming data from this game client.
        /// </summary>
        /// <param name="client"></param>
        public virtual void LoadPacketHandlers(string version, out IPacketHandler[] packetHandlers, out List<PacketHandlerAttribute> attributes)
        {
            packetHandlers = new IPacketHandler[256];
            attributes = new List<PacketHandlerAttribute>();

            Array.Clear(packetHandlers, 0, packetHandlers.Length);
            lock (m_packetHandlerCacheLock)
            {
                if (!m_cachedPacketHandlerSearchResults.ContainsKey(version))
                {
                    int count = SearchAndAddPacketHandlers(version, Assembly.GetAssembly(typeof(GameServer)), packetHandlers);
                    if (log.IsInfoEnabled)
                        log.Info("PacketProcessor: Loaded " + count + " handlers from GameServer Assembly!");

                    count = 0;
                    foreach (Assembly asm in ScriptMgr.Scripts)
                    {
                        count += SearchAndAddPacketHandlers(version, asm, packetHandlers);
                    }
                    if (log.IsInfoEnabled)
                        log.Info("PacketProcessor: Loaded " + count + " handlers from Script Assemblys!");

                    //save search result for next login
                    m_cachedPacketHandlerSearchResults.Add(version, (IPacketHandler[])packetHandlers.Clone());
                }
                else
                {
                    packetHandlers = (IPacketHandler[])m_cachedPacketHandlerSearchResults[version].Clone();
                    int count = 0;
                    foreach (IPacketHandler ph in packetHandlers) if (ph != null) count++;
                    log.Info("PacketProcessor: Loaded " + count + " handlers from cache for version="+version+"!");
                }

                if (m_cachedPreprocessorSearchResults.ContainsKey(version))
                    attributes = m_cachedPreprocessorSearchResults[version];
                log.Info("PacketProcessor: Loaded " + attributes.Count + " preprocessors from cache for version=" + version + "!");
            }
        }

        /// <summary>
        /// Registers a packet handler
        /// </summary>
        /// <param name="handler">The packet handler to register</param>
        /// <param name="packetCode">The packet ID to register it with</param>
        public void RegisterPacketHandler(int packetCode, IPacketHandler handler, IPacketHandler[] packetHandlers)
        {
            if (packetHandlers[packetCode] != null)
            {
                log.InfoFormat("Overwriting Client Packet Code {0}, with handler {1} in PacketProcessor", packetCode, handler.GetType().FullName);
            }

            packetHandlers[packetCode] = handler;
        }

        /// <summary>
        /// Searches an assembly for packet handlers
        /// </summary>
        /// <param name="version">namespace of packethandlers to search eg. 'v167'</param>
        /// <param name="assembly">Assembly to search</param>
        /// <returns>The number of handlers loaded</returns>
        protected int SearchAndAddPacketHandlers(string version, Assembly assembly, IPacketHandler[] packetHandlers)
        {
            int count = 0;

            // Walk through each type in the assembly
            foreach (Type type in assembly.GetTypes())
            {
                // Pick up a class
                if (type.IsClass != true)
                    continue;

                if (type.GetInterface("DOL.GS.PacketHandler.IPacketHandler") == null)
                    continue;

                if (!type.Namespace.ToLower().EndsWith(version.ToLower()))
                    continue;

                var packethandlerattribs =
                    (PacketHandlerAttribute[]) type.GetCustomAttributes(typeof (PacketHandlerAttribute), true);
                if (packethandlerattribs.Length > 0)
                {
                    count++;
                    RegisterPacketHandler(packethandlerattribs[0].Code, (IPacketHandler) Activator.CreateInstance(type), packetHandlers);

                    if (!m_cachedPreprocessorSearchResults.ContainsKey(version)) m_cachedPreprocessorSearchResults.Add(version, new List<PacketHandlerAttribute>());
                    m_cachedPreprocessorSearchResults[version].Add(packethandlerattribs[0]);
                }
            }
            return count;
        }

        /// <summary>
        /// Called on client disconnect.
        /// </summary>
        public virtual void OnDisconnect()
        {
            byte[] tcp = m_tcpSendBuffer;
            byte[] udp = m_udpSendBuffer;
            m_tcpSendBuffer = m_udpSendBuffer = null;
            m_client.Server.ReleasePacketBuffer(tcp);
            m_client.Server.ReleasePacketBuffer(udp);
        }

        #region TCP

        /// <summary>
        /// Holds the TCP send buffer
        /// </summary>
        protected byte[] m_tcpSendBuffer;

        /// <summary>
        /// The client TCP packet send queue
        /// </summary>
        protected ConcurrentQueue<byte[]> TcpQueue { get; private set; } = new();

        /// <summary>
        /// Sends a packet via TCP
        /// </summary>
        /// <param name="packet">The packet to be sent</param>
        public void SendTCP(GSTCPPacketOut packet)
        {
            packet.WritePacketLength();
            //SavePacket(packet); // TODO: Fix. This doesn't work since packets are disposed.
            SendTCP(packet.GetBuffer());
        }

        /// <summary>
        /// Sends a packet via TCP
        /// </summary>
        /// <param name="buf">Buffer containing the data to be sent</param>
        public void SendTCP(byte[] buf)
        {
            if (m_tcpSendBuffer == null || !m_client.Socket.Connected)
                return;

            if (buf.Length > 2048)
            {
                if (log.IsErrorEnabled)
                {
                    string desc = $"Sending packets longer than 2048 cause client to crash, check Log for stacktrace. Packet code: 0x{buf[2]:X2}, account: {(m_client.Account != null ? m_client.Account.Name : m_client.TcpEndpoint)}, packet size: {buf.Length}.";
                    log.Error(Marshal.ToHexDump(desc, buf) + "\n" + Environment.StackTrace);

                    if (Properties.IGNORE_TOO_LONG_OUTCOMING_PACKET)
                    {
                        log.Error("ALERT: Oversize packet detected and discarded.");
                        m_client.Out.SendMessage("ALERT: Error sending an update to your client. Oversize packet detected and discarded. Please /report this issue!", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
                    }
                    else
                        GameServer.Instance.Disconnect(m_client);
                }

                return;
            }

            TcpQueue.Enqueue(buf);
        }

        public void ProcessTcpQueue()
        {
            try
            {
                int count;
                bool empty;

                do
                {
                    count = CombinePackets(m_tcpSendBuffer, TcpQueue, out empty);
                    m_client.Socket.BeginSend(m_tcpSendBuffer, 0, count, SocketFlags.None, m_asyncTcpCallback, m_client);
                } while (!empty);
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"({nameof(ProcessTcpQueue)}) It seems {m_client.Account?.Name ?? m_client.TcpEndpoint} went link-dead. Closing connection. {e.GetType()}: {e.Message}");

                GameServer.Instance.Disconnect(m_client);
                return;
            }
        }

        /// <summary>
        /// Holds the TCP AsyncCallback delegate
        /// </summary>
        protected static readonly AsyncCallback m_asyncTcpCallback = AsyncTcpSendCallback;

        /// <summary>
        /// Callback method for async sends
        /// </summary>
        /// <param name="ar"></param>
        protected static void AsyncTcpSendCallback(IAsyncResult ar)
        {
            if (ar == null)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(AsyncTcpSendCallback)}: {nameof(ar)} is null");

                return;
            }

            GameClient client = ar.AsyncState as GameClient;

            try
            {
                if (client.IsConnected)
                    client.Socket.EndSend(ar);
            }
            catch (ObjectDisposedException e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Exception in {nameof(AsyncTcpSendCallback)} (Client: {client})", e);

                GameServer.Instance.Disconnect(client);
            }
            catch (SocketException e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Exception in {nameof(AsyncTcpSendCallback)} (Client: {client})", e);

                GameServer.Instance.Disconnect(client);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Exception in {nameof(AsyncTcpSendCallback)} (Client: {client})", e);

                GameServer.Instance.Disconnect(client);
            }
        }

        /// <summary>
        /// Combines queued packets in one stream.
        /// </summary>
        /// <param name="buffer">The target buffer.</param>
        /// <param name="queue">The queued packets.</param>
        /// <returns>The count of bytes written.</returns>
        private static int CombinePackets(byte[] buffer, ConcurrentQueue<byte[]> queue, out bool empty)
        {
            int count = 0;

            while (queue.TryPeek(out byte[] packet))
            {
                if (count + packet.Length > buffer.Length)
                {
                    empty = false;
                    return count;
                }

                Buffer.BlockCopy(packet, 0, buffer, count, packet.Length);
                count += packet.Length;
                queue.TryDequeue(out _);
            }

            empty = true;
            return count;
        }

        /// <summary>
        /// Send the packet via TCP without changing any portion of the packet
        /// </summary>
        /// <param name="packet">Packet to send</param>
        public void SendTCPRaw(GSTCPPacketOut packet)
        {
            SendTCP((byte[]) packet.GetBuffer().Clone());
        }

        #endregion

        #region UDP

        /// <summary>
        /// Holds the UDP send buffer
        /// </summary>
        protected byte[] m_udpSendBuffer;

        /// <summary>
        /// The client UDP packet send queue
        /// </summary>
        protected readonly ConcurrentQueue<byte[]> m_udpQueue = new();

        /// <summary>
        /// This variable holds the current UDP counter for this sender
        /// </summary>
        protected volatile ushort m_udpCounter;

        /// <summary>
        /// Holds the async udp send callback delegate
        /// </summary>
        private readonly AsyncCallback m_asyncUdpCallback;

        /// <summary>
        /// Indicates whether UDP data is currently being sent
        /// </summary>
        private bool m_sendingUdp;

        /// <summary>
        /// Send the packet via UDP
        /// </summary>
        /// <param name="packet">Packet to be sent</param>
        /// <param name="isForced">Force UDP packet if <code>true</code>, else packet can be sent over TCP</param>
        public virtual void SendUDP(GSUDPPacketOut packet, bool isForced)
        {
            //Fix the packet size
            packet.WritePacketLength();

            SavePacket(packet);

            SendUDP(packet.GetBuffer(), isForced);
        }

        /// <summary>
        /// Send the packet via UDP
        /// </summary>
        /// <param name="buffer">Packet to be sent</param>
        /// <param name="isForced">Force UDP packet if <code>true</code>, else packet can be sent over TCP</param>
        public void SendUDP(byte[] buffer, bool isForced)
        {
            if (m_client.ClientState == GameClient.eClientState.Playing)
            {
                // Would previously timeout after 50 seconds, but clients (1.127) send 'UDPInitRequestHandler' every 65 seconds.
                // May vary depending on the client version.
                if (GameLoop.GetCurrentTime() - m_client.UdpPingTime > 70000)
                    m_client.UdpConfirm = false;
            }

            // If UDP is unavailable, send via TCP instead.
            if (m_client.UdpEndPoint == null || !(isForced || m_client.UdpConfirm))
            {
                byte[] newBuffer = new byte[buffer.Length - 2];
                newBuffer[0] = buffer[0];
                newBuffer[1] = buffer[1];
                Buffer.BlockCopy(buffer, 4, newBuffer, 2, buffer.Length - 4);
                SendTCP(newBuffer);
                return;
            }

            if (m_udpSendBuffer == null)
                return;

            // Let it overflow.
            m_udpCounter++;

            buffer[2] = (byte) (m_udpCounter >> 8);
            buffer[3] = (byte) m_udpCounter;

            if (m_sendingUdp)
            {
                m_udpQueue.Enqueue(buffer);
                return;
            }

            m_sendingUdp = true;

            Buffer.BlockCopy(buffer, 0, m_udpSendBuffer, 0, buffer.Length);

            try
            {
                GameServer.Instance.SendUDP(m_udpSendBuffer, buffer.Length, m_client.UdpEndPoint, m_asyncUdpCallback);
            }
            catch (Exception e)
            {
                int count = m_udpQueue.Count;

                lock (m_udpQueue)
                {
                    m_udpQueue.Clear();
                    m_sendingUdp = false;
                }

                if (log.IsErrorEnabled)
                    log.ErrorFormat($"Exception in {nameof(SendUDP)} (Queue size: {count})", e);
            }
        }

        /// <summary>
        /// Finishes an asynchronous UDP transaction
        /// </summary>
        /// <param name="ar"></param>
        private void AsyncUdpSendCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = ar.AsyncState as Socket;
                int sent = socket.EndSendTo(ar);
                int count = 0;
                byte[] data = m_udpSendBuffer;

                if (data == null)
                    return;

                if (m_udpQueue.Count > 0)
                    count = CombinePackets(data, m_udpQueue, out _);
                if (count <= 0)
                {
                    m_sendingUdp = false;
                    return;
                }

                long start = GameLoop.GetCurrentTime();

                GameServer.Instance.SendUDP(data, count, m_client.UdpEndPoint, m_asyncUdpCallback);

                long took = GameLoop.GetCurrentTime() - start;

                if (took > 25 && log.IsWarnEnabled)
                    log.WarnFormat($"{nameof(AsyncUdpSendCallback)} took {took}ms! (Client: {m_client})");
            }
            catch (Exception e)
            {
                int count = m_udpQueue.Count;

                lock (((ICollection) m_udpQueue).SyncRoot)
                {
                    m_udpQueue.Clear();
                    m_sendingUdp = false;
                }

                if (log.IsErrorEnabled)
                    log.WarnFormat($"{nameof(AsyncUdpSendCallback)} {e}");
            }
        }

        /// <summary>
        /// Send the UDP packet without changing any portion of the packet
        /// </summary>
        /// <param name="packet">Packet to be sent</param>
        public void SendUDPRaw(GSUDPPacketOut packet)
        {
            SendUDP((byte[]) packet.GetBuffer().Clone(), false);
        }

        #endregion

        /// <summary>
        /// Called when the client receives bytes
        /// </summary>
        /// <param name="numBytes">The number of bytes received</param>
        public void ReceiveBytes(int numBytes)
        {
            lock (m_SyncLock)
            {
                byte[] buffer = m_client.ReceiveBuffer;

                //End Offset of buffer
                int bufferSize = m_client.ReceiveBufferOffset + numBytes;

                //Size < minimum
                if (bufferSize < GSPacketIn.HDR_SIZE)
                {
                    m_client.ReceiveBufferOffset = bufferSize; // undo buffer read
                    return;
                }

                //Reset the offset
                m_client.ReceiveBufferOffset = 0;

                //Current offset into the buffer
                int curOffset = 0;

                do
                {
                    int packetLength = (buffer[curOffset] << 8) + buffer[curOffset + 1] + GSPacketIn.HDR_SIZE;
                    int dataLeft = bufferSize - curOffset;

                    if (dataLeft < packetLength)
                    {
                        Buffer.BlockCopy(buffer, curOffset, buffer, 0, dataLeft);
                        m_client.ReceiveBufferOffset = dataLeft;
                        break;
                    }

                    // ** commented out because this hasn't been used in forever and crutching
                    // ** to it only hurts performance in a design that needs to be reworked
                    // ** anyways.                                               
                    // **                                                               - tobz
                    //var curPacket = new byte[packetLength];
                    //Buffer.BlockCopy(buffer, curOffset, curPacket, 0, packetLength);
                    //curPacket = m_encoding.DecryptPacket(buffer, false);

                    int packetEnd = curOffset + packetLength;

                    int calcCheck = CalculateChecksum(buffer, curOffset, packetLength - 2);
                    int pakCheck = (buffer[packetEnd - 2] << 8) | (buffer[packetEnd - 1]);

                    if (pakCheck != calcCheck)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat(
                                "Bad TCP packet checksum (packet:0x{0:X4} calculated:0x{1:X4}) -> disconnecting\nclient: {2}\ncurOffset={3}; packetLength={4}",
                                pakCheck, calcCheck, m_client.ToString(), curOffset, packetLength);

                        if (log.IsInfoEnabled)
                        {
                            log.Info("Last client sent/received packets (from older to newer):");

                            foreach (IPacket prevPak in GetLastPackets())
                            {
                                log.Info(prevPak.ToHumanReadable());
                            }
                            
                            log.Info(Marshal.ToHexDump("Last Received Bytes : ", buffer));
                        }

                        m_client.Disconnect();
                        return;
                    }

                    var pak = new GSPacketIn(packetLength - GSPacketIn.HDR_SIZE);
                    pak.Load(buffer, curOffset, packetLength);

                    try
                    {
                        HandlePacket(pak);
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("HandlePacket(pak)", e);
                    }

                    curOffset += packetLength;
                } while (bufferSize - 1 > curOffset);

                if (bufferSize - 1 == curOffset)
                {
                    buffer[0] = buffer[curOffset];
                    m_client.ReceiveBufferOffset = 1;
                }
            }
        }

        /// <summary>
        /// Calculates the packet checksum
        /// </summary>
        /// <param name="packet">The full received packet including checksum bytes</param>
        /// <param name="dataOffset">The offset of data for checksum calc in the buffer</param>
        /// <param name="dataSize">The size of data for checksum calc</param>
        /// <returns>The calculated checksum</returns>
        public static ushort CalculateChecksum(byte[] packet, int dataOffset, int dataSize)
        {
            byte[] pak = packet;
            byte val1 = 0x7E;
            byte val2 = 0x7E;
            int i = dataOffset;
            int len = i + dataSize;

            while (i < len)
            {
                val1 += pak[i++];
                val2 += val1;
            }

            return (ushort) (val2 - ((val1 + val2) << 8));
        }

        public void HandlePacketTimeout(object sender, ElapsedEventArgs e)
        {
            string source = ((m_client.Account != null) ? m_client.Account.Name : m_client.TcpEndpoint);
            if (log.IsErrorEnabled)
                log.Error("Thread " + m_handlerThreadID + " - Handler " + m_activePacketHandler.GetType() +
                          " takes too much time (>10000ms) <" + source + "> " + "!");
        }

#if LOGACTIVESTACKS
        /// <summary>
        /// Holds a list of all currently active handler threads!
        /// This list is updated in the HandlePacket method
        /// </summary>
        public static Hashtable m_activePacketThreads = Hashtable.Synchronized(new Hashtable());
#endif

        /// <summary>
        /// Retrieves a textual description of all active packet handler thread stacks
        /// </summary>
        /// <returns>A string with the stacks</returns>
        public static string GetConnectionThreadpoolStacks()
        {
#if LOGACTIVESTACKS
            var builder = new StringBuilder();
            //When enumerating over a synchronized hashtable, we need to
            //lock it's syncroot! Only for reading, not for writing locking
            //is needed!
            lock (m_activePacketThreads.SyncRoot)
            {
                foreach (DictionaryEntry entry in m_activePacketThreads)
                {
                    try
                    {
                        var thread = (Thread) entry.Key;
                        var client = (GameClient) entry.Value;

                        builder.Append("Stack for thread from account: ");
                        if (client != null && client.Account != null)
                        {
                            builder.Append(client.Account.Name);
                            if (client.Player != null)
                            {
                                builder.Append(" (");
                                builder.Append(client.Player.Name);
                                builder.Append(")");
                            }
                        }
                        else
                        {
                            builder.Append("null");
                        }
                        builder.Append("\n");
                        builder.Append(Util.GetFormattedStackTraceFrom(Thread.CurrentThread));
                        builder.Append("\n\n");
                    }
                    catch (Exception e)
                    {
                        builder.Append("Error getting stack for thread: ");
                        builder.Append("\n");
                        builder.Append(e);
                        builder.Append("\n\n");
                    }
                }
            }
            return builder.ToString();
#else
            return "LOGACTIVESTACKS is not defined in PacketProcessor";
#endif
        }


        public void HandlePacket(GSPacketIn packet)
        {
            if (packet == null || m_client == null)
                return;

            int code = packet.ID;

            SavePacket(packet);

            IPacketHandler packetHandler = null;
            if (code < m_packetHandlers.Length)
            {
                packetHandler = m_packetHandlers[code];
            }

            else if (log.IsErrorEnabled)
            {
                log.ErrorFormat("Received packet code is outside of m_packetHandlers array bounds! " + m_client);
                log.Error(Marshal.ToHexDump(
                            String.Format("===> <{2}> Packet 0x{0:X2} (0x{1:X2}) length: {3} (ThreadId={4})", code, code ^ 168,
                                          (m_client.Account != null) ? m_client.Account.Name : m_client.TcpEndpoint,
                                          packet.PacketSize, Thread.CurrentThread.ManagedThreadId),
                            packet.ToArray()));
            }

            // make sure we can handle this packet at this stage
            var preprocess = m_packetPreprocessor.CanProcessPacket(m_client, packet);
            if(!preprocess)
            {
                // this packet can't be processed by this client right now, for whatever reason
                log.Info("PacketPreprocessor: Preprocessor prevents handling of a packet with packet.ID=" + packet.ID);
                return;
            }

            if (packetHandler != null)
            {
                Timer monitorTimer = null;
                if (log.IsDebugEnabled)
                {
                    try
                    {
                        monitorTimer = new Timer(10000);
                        m_activePacketHandler = packetHandler;
                        m_handlerThreadID = Thread.CurrentThread.ManagedThreadId;
                        monitorTimer.Elapsed += HandlePacketTimeout;
                        monitorTimer.Start();
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Starting packet monitor timer", e);

                        if (monitorTimer != null)
                        {
                            monitorTimer.Stop();
                            monitorTimer.Close();
                            monitorTimer = null;
                        }
                    }
                }

#if LOGACTIVESTACKS
                //Put the current thread into the active thread list!
                //No need to lock the hashtable since we created it
                //synchronized! One reader, multiple writers supported!
                m_activePacketThreads.Add(Thread.CurrentThread, m_client);
#endif
                long start = GameLoop.GetCurrentTime();
                try
                {
                    packetHandler.HandlePacket(m_client, packet);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                    {
                        string client = (m_client == null ? "null" : m_client.ToString());
                        log.Error(
                            "Error while processing packet (handler=" + packetHandler.GetType().FullName + "  client: " + client + ")", e);
                    }
                }
#if LOGACTIVESTACKS
                finally
                {
                    //Remove the thread from the active list after execution
                    //No need to lock the hashtable since we created it
                    //synchronized! One reader, multiple writers supported!
                    m_activePacketThreads.Remove(Thread.CurrentThread);
                }
#endif
                long timeUsed = GameLoop.GetCurrentTime() - start;
                if (monitorTimer != null)
                {
                    monitorTimer.Stop();
                    monitorTimer.Close();
                }
                m_activePacketHandler = null;
                if (timeUsed > 1000)
                {
                    string source = ((m_client.Account != null) ? m_client.Account.Name : m_client.TcpEndpoint);
                    if (log.IsWarnEnabled)
                        log.Warn("(" + source + ") Handle packet Thread " + Thread.CurrentThread.ManagedThreadId + " " + packetHandler +
                                 " took " + timeUsed + "ms!");
                }
            }
        }
    }
}
