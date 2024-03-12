using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Network;
using log4net;

namespace DOL.GS
{
	/// <summary>
	/// Represents a single connection to the game server
	/// </summary>
	public class GameClient : BaseClient, ICustomParamsValuable, IManagedEntity
	{
		#region eClientAddons enum

		/// <summary>
		/// The client addons enum
		/// </summary>
		[Flags]
		public enum eClientAddons
		{
			bit4 = 0x10,
			NewNewFrontiers = 0x20,
			Foundations = 0x40,
			NewFrontiers = 0x80,
		}

		#endregion

		#region eClientState enum

		/// <summary>
		/// Current state of the client
		/// </summary>
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

		#endregion

		#region eClientType enum

		/// <summary>
		/// The client software type enum
		/// </summary>
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

		#endregion

		#region eClientVersion enum

		/// <summary>
		/// the version enum
		/// </summary>
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

		#endregion

		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// This variable holds the accountdata
		/// </summary>
		protected DbAccount m_account;

		/// <summary>
		/// This variable holds the active charindex
		/// </summary>
		protected int m_activeCharIndex = -1;

		/// <summary>
		/// Holds installed client addons
		/// </summary>
		protected eClientAddons m_clientAddons;

		/// <summary>
		/// Holds the current clientstate
		/// </summary>
		protected volatile eClientState m_clientState = eClientState.NotConnected;

		/// <summary>
		/// Holds client software type
		/// </summary>
		protected eClientType m_clientType = eClientType.Unknown;

		protected eClientVersion m_clientVersion = eClientVersion.VersionNotChecked;

		/// <summary>
		/// Holds the time of the last UDP ping
		/// </summary>
		protected string m_localIP = "";

		/// <summary>
		/// The packetsender of this client
		/// </summary>
		protected IPacketLib m_packetLib;

		/// <summary>
		/// The packetreceiver of this client
		/// </summary>
		protected PacketProcessor m_packetProcessor;

		/// <summary>
		/// Holds the time of the last ping
		/// </summary>
		protected long m_pingTime = GameLoop.GameLoopTime;

		/// <summary>
		/// This variable holds all info about the active player
		/// </summary>
		protected GamePlayer m_player;

		/// <summary>
		/// This variable holds the UDP endpoint of this client
		/// </summary>
		protected volatile bool m_udpConfirm;

		/// <summary>
		/// This variable holds the UDP endpoint of this client
		/// </summary>
		protected IPEndPoint m_udpEndpoint;

		/// <summary>
		/// Holds the time of the last UDP ping
		/// </summary>
		protected long m_udpPingTime = GameLoop.GameLoopTime;

		/// <summary>
		/// Custom Account Params
		/// </summary>
		protected Dictionary<string, List<string>> m_customParams = new Dictionary<string, List<string>>();

		// Trainer window Cache, (Object Type, Object ID) => Skill
		public List<Tuple<Specialization, List<Tuple<int, int, Skill>>>> TrainerSkillCache = null;

		// Tooltip Request Time Cache, (Object Type => (Object ID => expires))
		private ConcurrentDictionary<int, ConcurrentDictionary<int, long>> m_tooltipRequestTimes = new();

		public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.Client, false);

		/// <summary>
		/// Try to Send Tooltip to Client, return false if cache hit.
		/// Return true and register cache before you can send tooltip !
		/// </summary>
		/// <param name="type"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool CanSendTooltip(int type, int id)
		{
			m_tooltipRequestTimes.TryAdd(type, new());

			// Queries cleanup
			foreach (Tuple<int, int> keys in m_tooltipRequestTimes.SelectMany(e => e.Value.Where(it => it.Value < GameLoop.GameLoopTime).Select(el => new Tuple<int, int>(e.Key, el.Key))))
				m_tooltipRequestTimes[keys.Item1].TryRemove(keys.Item2, out _);
			
			// Query hit ?
			if (m_tooltipRequestTimes[type].ContainsKey(id))
				return false;
		
			// Query register
			m_tooltipRequestTimes[type].TryAdd(id, GameLoop.GameLoopTime + 3600000);
			return true;
		}

		/// <summary>
		/// Constructor for a game client
		/// </summary>
		/// <param name="srvr">The server that's communicating with this client</param>
		public GameClient(BaseServer srvr) : base(srvr) { }

		/// <summary>
		/// UDP address for this client
		/// </summary>
		public IPEndPoint UdpEndPoint
		{
			get { return m_udpEndpoint; }
			set { m_udpEndpoint = value; }
		}

		/// <summary>
		/// Gets or sets the client state
		/// </summary>
		public eClientState ClientState
		{
			get { return m_clientState; }
			set
			{
				eClientState oldState = m_clientState;

				// refresh ping timeouts immediately when we change into playing state or charscreen
				if ((oldState != eClientState.Playing && value == eClientState.Playing) ||
				    (oldState != eClientState.CharScreen && value == eClientState.CharScreen))
				{
					PingTime = GameLoop.GameLoopTime;
					PositionUpdateTime = GameLoop.GameLoopTime;
				}

				m_clientState = value;
				GameEventMgr.Notify(GameClientEvent.StateChanged, this);
			}
		}

		/// <summary>
		/// When the linkdeath occured. 0 if there wasn't any
		/// </summary>
		public long LinkDeathTime { get; set; }

		public long PositionUpdateTime { get; set; }

		public GSPacketIn LastPositionUpdatePacketReceived { get; set; }

		/// <summary>
		/// Variable is false if account/player is Ban, for a wrong password, if server is closed etc ... 
		/// </summary>
		public bool IsConnected = true;

		/// <summary>
		/// Gets whether or not the client is playing
		/// </summary>
		public bool IsPlaying
		{
			get
			{
				//Linkdead players also count as playing :)
				return m_clientState == eClientState.Playing || m_clientState == eClientState.Linkdead;
			}
		}

		/// <summary>
		/// Gets or sets the account being used by this client
		/// </summary>
		public DbAccount Account
		{
			get { return m_account; }
			set
			{
				m_account = value;
				// Load Custom Params
				this.InitFromCollection<DbAccountXCustomParam>(value.CustomParams, param => param.KeyName, param => param.Value);
				GameEventMgr.Notify(GameClientEvent.AccountLoaded, this);
			}
		}

		/// <summary>
		/// Gets or sets the player this client is using
		/// </summary>
		public GamePlayer Player
		{
			get { return m_player; }
			set
			{
				GamePlayer oldPlayer = Interlocked.Exchange(ref m_player, value);
				if (oldPlayer != null)
				{
					oldPlayer.Delete();
				}

				GameEventMgr.Notify(GameClientEvent.PlayerLoaded, this); // hmm seems not right
			}
		}

		/// <summary>
		/// Gets or sets the character index for the player currently being used
		/// </summary>
		public int ActiveCharIndex
		{
			get { return m_activeCharIndex; }
			set { m_activeCharIndex = value; }
		}

		/// <summary>
		/// Gets or sets the session ID for this client
		/// </summary>
		public int SessionID => EntityManagerId.Value + 1;

		/// <summary>
		/// Gets/Sets the time of last ping packet
		/// </summary>
		public long PingTime
		{
			get { return m_pingTime; }
			set { m_pingTime = value; }
		}

		/// <summary>
		/// UDP address for this client
		/// </summary>
		public string LocalIP
		{
			get { return m_localIP; }
			set { m_localIP = value; }
		}

		/// <summary>
		/// Gets/Sets the time of last UDP ping packet
		/// </summary>
		public long UdpPingTime
		{
			get { return m_udpPingTime; }
			set { m_udpPingTime = value; }
		}

		/// <summary>
		/// UDP confirm flag from this client
		/// </summary>
		public bool UdpConfirm
		{
			get { return m_udpConfirm; }
			set { m_udpConfirm = value; }
		}

		/// <summary>
		/// Gets or sets the packet sender
		/// </summary>
		public IPacketLib Out
		{
			get { return m_packetLib; }
			set { m_packetLib = value; }
		}

		/// <summary>
		/// Gets or Sets the packet receiver
		/// </summary>
		public PacketProcessor PacketProcessor
		{
			get { return m_packetProcessor; }
			set { m_packetProcessor = value; }
		}

		/// <summary>
		/// the version of this client
		/// </summary>
		public eClientVersion Version
		{
			get { return m_clientVersion; }
			set { m_clientVersion = value; }
		}

		/// <summary>
		/// Gets/sets client software type (classic/SI/ToA/Catacombs)
		/// </summary>
		public eClientType ClientType
		{
			get { return m_clientType; }
			set { m_clientType = value; }
		}

		public string MinorRev = "";
		public byte MajorBuild = 0;
		public byte MinorBuild = 0;

		/// <summary>
		/// Gets/sets installed client addons (housing/new frontiers)
		/// </summary>
		public eClientAddons ClientAddons
		{
			get { return m_clientAddons; }
			set { m_clientAddons = value; }
		}

		/// <summary>
		/// Get the Custom Params from this Game Client
		/// </summary>
		public Dictionary<string, List<string>> CustomParamsDictionary
		{
			get { return m_customParams; }
			set
			{
				Account.CustomParams = value.SelectMany(kv => kv.Value.Select(val => new DbAccountXCustomParam(Account.Name, kv.Key, val))).ToArray();
				m_customParams = value;
			}
		}

		/// <summary>
		/// Called when a packet has been received.
		/// </summary>
		/// <param name="numBytes">The number of bytes received</param>
		/// <remarks>This function parses the incoming data into individual packets and then calls the appropriate handler.</remarks>
		protected override void OnReceive(int numBytes)
		{
			//This is the first received packet ...
			if (Version == eClientVersion.VersionNotChecked)
			{
				//Disconnect if the packet seems wrong
				if (numBytes < 17) // 17 is correct bytes count for 0xF4 packet
				{
					if (log.IsWarnEnabled)
					{
						log.WarnFormat("Disconnected {0} in login phase because wrong packet size {1}", TcpEndpoint, numBytes);
						log.Warn("numBytes=" + numBytes);
						log.Warn(Marshal.ToHexDump("packet buffer:", _pBuf, 0, numBytes));
					}
					GameServer.Instance.Disconnect(this);
					return;
				}

				int version;
				
				/// <summary>
				/// The First Packet Format Change after 1.115c
				/// If "numbytes" is below 19 we have a pre-1.115c packet !
				/// </summary>
				if (numBytes < 19)
				{
					//Currently, the version is sent with the first packet, no
					//matter what packet code it is
					version = (_pBuf[12] * 100) + (_pBuf[13] * 10) + _pBuf[14];

					// we force the versionning: 200 correspond to 1.100 (1100)
					// thus we could handle logically packets with version number based on the client version
					if (version >= 200) version += 900;
				}
				else
				{
					// post 1.115c
					// first byte is major (1), second byte is minor (1), third byte is version (15)
					// revision (c) is also coded in ascii after that, then a build number appear using two bytes (0x$$$$)
					version = _pBuf[11] * 1000 + _pBuf[12] * 100 + _pBuf[13];
				}

				eClientVersion ver;
				IPacketLib lib = AbstractPacketLib.CreatePacketLibForVersion(version, this, out ver);

				if (lib == null)
				{
					Version = eClientVersion.VersionUnknown;
					if (log.IsWarnEnabled)
						log.Warn(TcpEndpointAddress + " client Version " + version + " not handled on this server!");
					GameServer.Instance.Disconnect(this);
				}
				else
				{
					log.Info("Incoming connection from " + TcpEndpointAddress + " using client version " + version);
					Version = ver;
					Out = lib;
					PacketProcessor = new PacketProcessor(this);
				}
			}

			if (Version != eClientVersion.VersionUnknown)
			{
				m_packetProcessor.ReceiveBytes(numBytes);
			}
		}

		/// <summary>
		/// Called when this client has been disconnected
		/// </summary>
		public override void OnDisconnect()
		{
			bool wasPlaying = false;

			try
			{
				//If we went linkdead and we were inside the game
				//we don't let the player disappear!
				if (ClientState == eClientState.Playing)
				{
					wasPlaying = true;
					OnLinkDeath(false);
					return;
				}

				if (ClientState == eClientState.WorldEnter && Player != null)
					Player.SaveIntoDatabase();
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("OnDisconnect", e);
			}
			finally
			{
				// Make sure the client is disconnected even on errors but only if OnLinkDeath() wasn't called.
				if (!wasPlaying)
					Quit();
			}
		}

		/// <summary>
		/// Called when this client has connected
		/// </summary>
		public override void OnConnect()
		{
			ClientService.OnClientConnect(this);
			GameEventMgr.Notify(GameClientEvent.Connected, this);
		}

		public void LoadPlayer(int accountindex)
		{
			LoadPlayer(accountindex, Properties.PLAYER_CLASS);
		} 
		public void LoadPlayer(DbCoreCharacter dolChar)
		{
			LoadPlayer(dolChar, Properties.PLAYER_CLASS);
		}

		public void LoadPlayer(int accountindex, string playerClass)
		{
			// refreshing Account to load any changes from the DB
			GameServer.Database.FillObjectRelations(m_account);
			DbCoreCharacter dolChar = m_account.Characters[accountindex];
			LoadPlayer(dolChar, playerClass);
		}

		/// <summary>
		/// Loads a player from the DB
		/// </summary>
		/// <param name="accountindex">Index of the character within the account</param>
		public void LoadPlayer(DbCoreCharacter dolChar, string playerClass)
		{
			m_activeCharIndex = 0;
			foreach (var ch in Account.Characters)
			{
				if (ch.ObjectId == dolChar.ObjectId)
					break;
				m_activeCharIndex++;
			}

			Assembly gasm = Assembly.GetAssembly(typeof(GameServer));

			GamePlayer player = null;
			try
			{
				player = (GamePlayer)gasm.CreateInstance(playerClass, false, BindingFlags.CreateInstance, null, new object[] { this, dolChar }, null, null);
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("LoadPlayer", e);
			}

			if (player == null)
			{
				foreach (Assembly asm in ScriptMgr.Scripts)
				{
					try
					{
						player = (GamePlayer)asm.CreateInstance(playerClass, false, BindingFlags.CreateInstance, null, new object[] { this, dolChar }, null, null);
					}
					catch (Exception e)
					{
						if (log.IsErrorEnabled)
							log.Error("LoadPlayer", e);
					}
					if (player != null)
						break;
				}
			}

			if (player == null)
			{
				log.ErrorFormat("Could not instantiate player class '{0}', using GamePlayer instead!", playerClass);
				player = new GamePlayer(this, dolChar);
			}

			Thread.MemoryBarrier();

			Player = player;
		}

		/// <summary>
		/// Saves a player to the DB
		/// </summary>
		public void SavePlayer()
		{
			try
			{
				if (m_player != null)
				{
					//<**loki**>
					if (Properties.KICK_IDLE_PLAYER_STATUS)
					{
						if (ServiceUtils.ShouldTickNoEarly(PositionUpdateTime + Properties.KICK_IDLE_PLAYER_TIME))
						{
							ServiceUtils.KickPlayerToCharScreen(m_player);

							if (log.IsInfoEnabled)
								log.Info($"Player {m_player.Name} kicked due to inactivity.");
						}
						else
							m_player.SaveIntoDatabase();
					}
					else
						m_player.SaveIntoDatabase();
				}
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("SavePlayer", e);
			}
		}

		public bool OnUpdatePosition()
		{
			PositionUpdateTime = GameLoop.GameLoopTime;
			return m_player.OnUpdatePosition();
		}

		/// <summary>
		/// Called when a player goes linkdead
		/// </summary>
		public void OnLinkDeath(bool soft)
		{
			if (log.IsDebugEnabled)
				log.Debug($"OnLinkDeath called (Account: {Account.Name}) (State: {ClientState}) (Soft: {soft})");

			//If we have no sessionid we simply disconnect
			GamePlayer curPlayer = Player;
			if (SessionID == 0 || curPlayer == null)
			{
				Quit();
			}
			else
			{
				if (!soft)
					ClientState = eClientState.Linkdead;

				LinkDeathTime = GameLoop.GameLoopTime;
				// If we have a good sessionid, we won't remove the client yet!
				// OnLinkdeath() can start a timer to remove the client "a bit later"
				curPlayer.OnLinkDeath();
			}
		}

		/// <summary>
		/// Quits a client from the world
		/// </summary>
		protected internal void Quit()
		{
			lock (this)
			{
				if (ClientState is eClientState.Disconnected)
					return;

				try
				{
					if (SessionID != 0)
					{
						if (ClientState is eClientState.Playing or eClientState.WorldEnter or eClientState.Linkdead)
						{
							try
							{
								Player?.Quit(true); // Calls delete.
							}
							catch (Exception e)
							{
								log.Error("player cleanup on client quit", e);
							}
						}
					}

					ClientState = eClientState.Disconnected;
					GameEventMgr.Notify(GameClientEvent.Disconnected, this);

					if (Account != null)
					{
						if (log.IsInfoEnabled)
						{
							if (m_udpEndpoint != null)
							{
								log.Info($"({m_udpEndpoint.Address}) {Account.Name} just disconnected.");
							}
							else
							{
								log.Info($"({TcpEndpoint}) {Account.Name} just disconnected.");
							}
						}

						Account.LastDisconnected = DateTime.Now;
						GameServer.Database.SaveObject(Account);
						AuditMgr.AddAuditEntry(this, AuditType.Account, AuditSubtype.AccountLogout, "", Account.Name);
					}
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error("Quit", e);
				}
			}
		}

		/// <summary>
		/// Returns short informations about the client
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return new StringBuilder(128)
				.Append(Version.ToString())
				.Append(" pakLib:").Append(Out == null ? "(null)" : Out.GetType().FullName)
				.Append(" type:").Append(ClientType)
				.Append('(').Append(ClientType).Append(')')
				.Append(" addons:").Append(ClientAddons.ToString("G"))
				.Append(" state:").Append(ClientState.ToString())
				.Append(" IP:").Append(TcpEndpoint)
				.Append(" session:").Append(SessionID)
				.Append(" acc:").Append(Account == null ? "null" : Account.Name)
				.Append(" char:").Append(Player == null ? "null" : Player.Name)
				.Append(" class:").Append(Player == null ? "null" : Player.CharacterClass.ID.ToString())
				.ToString();
		}
	}
}
