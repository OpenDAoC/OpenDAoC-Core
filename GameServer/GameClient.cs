using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Network;

namespace DOL.GS
{
    public class GameClient : BaseClient, ICustomParamsValuable, IServiceObject
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private DbAccount _account;
        private eClientState _clientState = eClientState.NotConnected;
        private GamePlayer _player;
        private IPEndPoint _udpEndpoint;
        private Dictionary<string, List<string>> _customParams = [];
        private ConcurrentDictionary<int, ConcurrentDictionary<int, long>> _tooltipRequestTimes = new();
        private readonly Lock _disconnectLock = new();

        public DbAccount Account
        {
            get => _account;
            set
            {
                _account = value;
                this.InitFromCollection(value.CustomParams, param => param.KeyName, param => param.Value);
                GameEventMgr.Notify(GameClientEvent.AccountLoaded, this);
            }
        }

        public eClientState ClientState
        {
            get => _clientState;
            set
            {
                eClientState oldState = _clientState;

                // Refresh ping timeouts immediately when we change into playing state or char screen.
                if ((oldState != eClientState.Playing && value == eClientState.Playing) ||
                    (oldState != eClientState.CharScreen && value == eClientState.CharScreen))
                {
                    PingTime = GameLoop.GameLoopTime;

                    if (_player != null)
                        _player.LastPositionUpdatePacketReceivedTime = GameLoop.GameLoopTime;
                }

                _clientState = value;
                GameEventMgr.Notify(GameClientEvent.StateChanged, this);
            }
        }

        public GamePlayer Player
        {
            get => _player;
            set
            {
                if (_player != null && _player.ObjectState is not GameObject.eObjectState.Deleted)
                {
                    if (log.IsErrorEnabled)
                    {
                        string message = $"GameClient.Player is being replaced but hasn't been deleted yet (current: {_player}) (new: {_player}){Environment.NewLine}{Environment.StackTrace}";
                        log.Error(message);
                    }

                    _player.Delete();
                }

                _player = value;
                GameEventMgr.Notify(GameClientEvent.PlayerLoaded, this); // Seems not right.
            }
        }

        public IPEndPoint UdpEndPoint
        {
            get => _udpEndpoint;
            set => _udpEndpoint = value;
        }

        public Dictionary<string, List<string>> CustomParamsDictionary
        {
            get => _customParams;
            set => _customParams = value;
        }

        public bool IsPlaying => _clientState is eClientState.Playing or eClientState.Linkdead;
        public ushort SessionID => SessionId.Value;
        public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.Client);
        public bool HasSeenPatchNotes { get; set; }
        public List<Tuple<Specialization, List<Tuple<int, int, Skill>>>> TrainerSkillCache { get; set; }
        public long LinkDeathTime { get; set; }
        public bool IsConnected { get; set; } = true;
        public int ActiveCharIndex { get; set; } = -1;
        public long PingTime { get; set; } = GameLoop.GameLoopTime;
        public string LocalIP { get; set; } = string.Empty;
        public long UdpPingTime { get; set; } = GameLoop.GameLoopTime;
        public bool UdpConfirm { get; set; }
        public IPacketLib Out { get; set; }
        public PacketProcessor PacketProcessor { get; set; }
        public eClientVersion Version { get; set; } = eClientVersion.VersionNotChecked;
        public eClientType ClientType { get; set; } = eClientType.Unknown;
        public eClientAddons ClientAddons { get; set; }
        public string MinorRev { get; set; } = string.Empty;
        public byte MajorBuild { get; set; } = 0;
        public byte MinorBuild { get; set; } = 0;

        public GameClient(Socket socket) : base(socket) { }

        public bool CanSendTooltip(int type, int id)
        {
            _tooltipRequestTimes.TryAdd(type, new());

            foreach (Tuple<int, int> keys in _tooltipRequestTimes.SelectMany(e => e.Value.Where(it => it.Value < GameLoop.GameLoopTime).Select(el => new Tuple<int, int>(e.Key, el.Key))))
                _tooltipRequestTimes[keys.Item1].TryRemove(keys.Item2, out _);

            if (_tooltipRequestTimes[type].ContainsKey(id))
                return false;

            _tooltipRequestTimes[type].TryAdd(id, GameLoop.GameLoopTime + 3600000);
            return true;
        }

        public async Task LoadPlayer(int accountIndex)
        {
            DbCoreCharacter dolChar = _account.Characters[accountIndex];
            ActiveCharIndex = 0;

            foreach (var ch in Account.Characters)
            {
                if (ch.ObjectId == dolChar.ObjectId)
                    break;
                ActiveCharIndex++;
            }

            await DOLDB<DbCoreCharacter>.FillObjectRelationsAsync(dolChar);
            Assembly gameServerAssembly = Assembly.GetAssembly(typeof(GameServer));
            object playerObject = null;

            try
            {
                playerObject = gameServerAssembly.CreateInstance(Properties.PLAYER_CLASS, false, BindingFlags.CreateInstance, null, [this, dolChar], null, null);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }

            if (playerObject == null)
            {
                foreach (Assembly assembly in ScriptMgr.Scripts)
                {
                    try
                    {
                        playerObject = assembly.CreateInstance(Properties.PLAYER_CLASS, false, BindingFlags.CreateInstance, null, [this, dolChar], null, null);
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error( e);
                    }

                    if (playerObject != null)
                        break;
                }
            }

            GamePlayer player = playerObject as GamePlayer;

            if (playerObject is null)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Could not instantiate player class '{Properties.PLAYER_CLASS}', using '{nameof(GamePlayer)}' instead!");

                player = new(this, dolChar);
            }

            await player.LoadFromDatabaseAsync(dolChar);
            Player = player;
        }

        public void SavePlayer()
        {
            try
            {
                if (_player?.ObjectState is GameObject.eObjectState.Active)
                    _player.SaveIntoDatabase();
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }
        }

        public void OnLinkDeath(bool soft)
        {
            lock (_disconnectLock)
            {
                if (SessionID == 0 || Player == null)
                {
                    Quit();
                    return;
                }

                if (ClientState == eClientState.Disconnected || Player.IsLinkDeathTimerRunning)
                    return;

                if (log.IsDebugEnabled)
                    log.Debug($"OnLinkDeath called (Account: {Account.Name}) (State: {ClientState}) (Soft: {soft})");

                if (!soft)
                    ClientState = eClientState.Linkdead;

                LinkDeathTime = GameLoop.GameLoopTime;
                Player.OnLinkDeath();
            }
        }

        public void LinkDeathQuit()
        {
            lock (_disconnectLock)
            {
                // Special handling for soft link deaths
                // Contrary to normal link deaths, the connection is still alive.
                if (ClientState is eClientState.Playing)
                {
                    CloseSocket();
                    Quit();
                    return;
                }

                Quit();
            }
        }

        private void Quit()
        {
            try
            {
                if (SessionID != 0 && Player != null)
                {
                    if (ClientState is eClientState.Playing or eClientState.WorldEnter or eClientState.Linkdead)
                    {
                        try
                        {
                            Player.Quit(true); // Calls delete.
                        }
                        catch (Exception e)
                        {
                            if (log.IsErrorEnabled)
                                log.Error(e);
                        }
                    }
                }

                ClientState = eClientState.Disconnected;
                PacketProcessor?.Dispose();
                ClientService.Instance.OnClientDisconnect(this);
                GameEventMgr.Notify(GameClientEvent.Disconnected, this);

                if (Account != null)
                {
                    if (log.IsInfoEnabled)
                    {
                        if (_udpEndpoint != null)
                            log.Info($"({_udpEndpoint.Address}) {Account.Name} just disconnected.");
                        else
                            log.Info($"({TcpEndpointAddress}) {Account.Name} just disconnected.");
                    }

                    Account.LastDisconnected = DateTime.Now;
                    GameServer.Database.SaveObject(Account);
                    AuditMgr.AddAuditEntry(this, AuditType.Account, AuditSubtype.AccountLogout, "", Account.Name);
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error(e);
            }
        }

        protected override void OnReceive(int size)
        {
            if (Version is eClientVersion.VersionNotChecked && !CheckVersion())
                return;

            if (Version is not eClientVersion.VersionUnknown)
                ProcessInboundTcpBytes();

            bool CheckVersion()
            {
                // This currently assumes the first packet is received in full, which may not be the case.
                // This should eventually be fixed since the connection may fail because of that.

                if (size < 17) // 17 is correct bytes count for 0xF4 packet.
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Disconnected {TcpEndpointAddress} in login phase because wrong packet size {size}");
                        log.Warn(Marshal.ToHexDump("packet buffer:", ReceiveBuffer, 0, size));
                    }

                    Disconnect();
                    return false;
                }

                int version;

                // The first packet format changes after 1.115c. If bytes count is below 19, we have a pre-1.115c packet.
                if (size < 19)
                {
                    // Currently, the version is sent with the first packet, no matter what packet code it is.
                    version = ReceiveBuffer[12] * 100 + ReceiveBuffer[13] * 10 + ReceiveBuffer[14];

                    // We force the versioning: 200 corresponds to 1.100.
                    // Thus we could handle logically packets with version number based on the client version.
                    if (version >= 200)
                        version += 900;
                }
                else
                {
                    // Post 1.115c.
                    // First byte is major (1), second byte is minor (1), third byte is version (15).
                    // Revision (c) is also coded in ASCII after that, then a build number appears using two bytes (0x$$$$).
                    version = ReceiveBuffer[11] * 1000 + ReceiveBuffer[12] * 100 + ReceiveBuffer[13];
                }

                IPacketLib packetLib = AbstractPacketLib.CreatePacketLibForVersion(version, this, out eClientVersion ver);

                if (packetLib == null)
                {
                    Version = eClientVersion.VersionUnknown;

                    if (log.IsWarnEnabled)
                        log.Warn($"{TcpEndpointAddress} client Version {version} not handled on this server.");

                    Disconnect();
                }
                else
                {
                    if (log.IsInfoEnabled)
                        log.Info($"Incoming connection from {TcpEndpointAddress} using client version {version}");

                    Version = ver;
                    Out = packetLib;
                    PacketProcessor = new PacketProcessor(this);
                }

                return true;
            }

            void ProcessInboundTcpBytes()
            {
                byte[] buffer = ReceiveBuffer;
                int endPosition = ReceiveBufferOffset + size;

                if (endPosition < GSPacketIn.HDR_SIZE)
                {
                    ReceiveBufferOffset = endPosition;
                    return;
                }

                ReceiveBufferOffset = 0;
                int currentOffset = 0;

                do
                {
                    int packetLength = (buffer[currentOffset] << 8) + buffer[currentOffset + 1] + GSPacketIn.HDR_SIZE;
                    int dataLeft = endPosition - currentOffset;

                    if (dataLeft < packetLength)
                    {
                        Buffer.BlockCopy(buffer, currentOffset, buffer, 0, dataLeft);
                        ReceiveBufferOffset = dataLeft;
                        break;
                    }

                    int packetEnd = currentOffset + packetLength;
                    int calcCheck = PacketProcessor.CalculateChecksum(buffer, currentOffset, packetLength - 2);
                    int pakCheck = (buffer[packetEnd - 2] << 8) | (buffer[packetEnd - 1]);

                    if (pakCheck != calcCheck)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn($"Bad TCP packet checksum (packet:0x{pakCheck:X4} calculated:0x{calcCheck:X4}) -> disconnecting\nclient: {this}\ncurOffset={currentOffset}; packetLength={packetLength}");

                        if (log.IsDebugEnabled)
                            log.Debug(Marshal.ToHexDump("Last received bytes: ", buffer));

                        Disconnect();
                        return;
                    }

                    var packet = PooledObjectFactory.GetForTick<GSPacketIn>().Init();
                    packet.Load(buffer, currentOffset, packetLength);

                    try
                    {
                        PacketProcessor.ProcessInboundPacket(packet);
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error(e);
                    }
                    finally
                    {
                        packet.ReleasePooledObject();
                    }

                    currentOffset += packetLength;
                } while (endPosition - 1 > currentOffset);

                if (endPosition - 1 == currentOffset)
                {
                    buffer[0] = buffer[currentOffset];
                    ReceiveBufferOffset = 1;
                }
            }
        }

        protected override void OnDisconnect()
        {
            if (ClientState is eClientState.Disconnected)
                return;

            // Posting the disconnect logic to the game loop isn't necessary in most cases.
            // However, this can sometimes be called outside the game loop, for example if a client fails to connect properly.
            ClientService.Instance.Post(static state =>
            {
                lock (state._disconnectLock)
                {
                    if (state.ClientState is eClientState.Disconnected)
                       return;

                    if (state.SessionID == 0 || state.Player == null)
                    {
                        state.Quit();
                        return;
                    }

                    try
                    {
                        if (state.ClientState is eClientState.Playing)
                        {
                            if (!state.Player.IsLinkDeathTimerRunning)
                                state.OnLinkDeath(false);

                            return;
                        }
                        else if (state.ClientState is eClientState.WorldEnter)
                            state.Player.SaveIntoDatabase();
                    }
                    catch (Exception e)
                    {
                        if (log.IsErrorEnabled)
                            log.Error(e);
                    }
                    finally
                    {
                        // Make sure the client is disconnected even on errors, but only if there is no link death timer running.
                        if (!state.Player.IsLinkDeathTimerRunning)
                            state.Quit();
                    }
                }
            }, this);
        }

        public override void OnConnect(SessionId sessionId)
        {
            base.OnConnect(sessionId);

            // `OnConnect` is exclusively called from outside the game loop.
            ClientService.Instance.Post(static state =>
            {
                ClientService.Instance.OnClientConnect(state);
                GameEventMgr.Notify(GameClientEvent.Connected, state);
            }, this);
        }

        public override string ToString()
        {
            return new StringBuilder(128)
                .Append(Version.ToString())
                .Append(" pakLib:").Append(Out == null ? "(null)" : Out.GetType().FullName)
                .Append(" type:").Append(ClientType)
                .Append('(').Append(ClientType).Append(')')
                .Append(" addons:").Append(ClientAddons.ToString("G"))
                .Append(" state:").Append(ClientState.ToString())
                .Append(" IP:").Append(TcpEndpointAddress)
                .Append(" session:").Append(SessionID)
                .Append(" acc:").Append(Account == null ? "null" : Account.Name)
                .Append(" char:").Append(Player == null ? "null" : Player.Name)
                .Append(" class:").Append(Player == null ? "null" : Player.CharacterClass.ID.ToString())
                .ToString();
        }

        [Flags]
        public enum eClientAddons
        {
            bit4 = 0x10,
            NewNewFrontiers = 0x20,
            Foundations = 0x40,
            NewFrontiers = 0x80,
        }

        public enum eClientState
        {
            NotConnected = 0x00,
            Connecting = 0x01,
            CharScreen = 0x02,
            WorldEnter = 0x03,
            Playing = 0x04,
            Linkdead = 0x05,
            Disconnected = 0x06,
        } ;

        public enum eClientType
        {
            Unknown = -1,
            Classic = 1,
            ShroudedIsles = 2,
            TrialsOfAtlantis = 3,
            Catacombs = 4,
            DarknessRising = 5,
            LabyrinthOfTheMinotaur = 6,
        }

        public enum eClientVersion
        {
            VersionNotChecked = -1,
            VersionUnknown = 0,
            _FirstVersion = 168,
            Version168 = 168,
            Version169 = 169,
            Version170 = 170,
            Version171 = 171,
            Version172 = 172,
            Version173 = 173,
            Version174 = 174,
            Version175 = 175,
            Version176 = 176,
            Version177 = 177,
            Version178 = 178,
            Version179 = 179,
            Version180 = 180,
            Version181 = 181,
            Version182 = 182,
            Version183 = 183,
            Version184 = 184,
            Version185 = 185,
            Version186 = 186,
            Version187 = 187,
            Version188 = 188,
            Version189 = 189,
            Version190 = 190,
            Version191 = 191,
            Version192 = 192,
            Version193 = 193,
            Version194 = 194,
            Version195 = 195,
            Version196 = 196,
            Version197 = 197,
            Version198 = 198,
            Version199 = 199,
            Version1100 = 1100,
            Version1101 = 1101,
            Version1102 = 1102,
            Version1103 = 1103,
            Version1104 = 1104,
            Version1105 = 1105,
            Version1106 = 1106,
            Version1107 = 1107,
            Version1108 = 1108,
            Version1109 = 1109,
            Version1110 = 1110,
            Version1111 = 1111,
            Version1112 = 1112,
            Version1113 = 1113,
            Version1114 = 1114,
            Version1115 = 1115,
            Version1116 = 1116,
            Version1117 = 1117,
            Version1118 = 1118,
            Version1119 = 1119,
            Version1120 = 1120,
            Version1121 = 1121,
            Version1122 = 1122,
            Version1123 = 1123,
            Version1124 = 1124,
            Version1125 = 1125,
            Version1126 = 1126,
            Version1127 = 1127,
            Version1128 = 1128,
            _LastVersion = 1128
        }
    }
}
