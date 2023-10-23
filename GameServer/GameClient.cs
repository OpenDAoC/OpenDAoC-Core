using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Core.Base;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameLoop;
using Core.GS.GameUtils;
using Core.GS.Packets;
using Core.GS.Packets.Server;
using Core.GS.Players;
using Core.GS.Scripts;
using Core.GS.Server;
using Core.GS.Skills;
using log4net;

namespace Core.GS
{
	/// <summary>
	/// Represents a single connection to the game server
	/// </summary>
	public class GameClient : BaseClient, ICustomParamsValuable, IManagedEntity
	{
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
		protected EClientAddons m_clientAddons;

		/// <summary>
		/// Holds the current clientstate
		/// </summary>
		protected volatile EClientState m_clientState = EClientState.NotConnected;

		/// <summary>
		/// Holds client software type
		/// </summary>
		protected EClientType m_clientType = EClientType.Unknown;

		protected EClientVersion m_clientVersion = EClientVersion.VersionNotChecked;

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
		protected long m_pingTime = GameLoopMgr.GetCurrentTime(); // give ping time on creation

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
		protected long m_udpPingTime = GameLoopMgr.GetCurrentTime();

		/// <summary>
		/// Custom Account Params
		/// </summary>
		protected Dictionary<string, List<string>> m_customParams = new Dictionary<string, List<string>>();

		// Trainer window Cache, (Object Type, Object ID) => Skill
		public List<Tuple<Specialization, List<Tuple<int, int, Skill>>>> TrainerSkillCache = null;

		// Tooltip Request Time Cache, (Object Type => (Object ID => expires))
		private ConcurrentDictionary<int, ConcurrentDictionary<int, long>> m_tooltipRequestTimes = new();

		public EntityManagerId EntityManagerId { get; set; } = new(EEntityType.Client, false);

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
			foreach (Tuple<int, int> keys in m_tooltipRequestTimes.SelectMany(e => e.Value.Where(it => it.Value < GameLoopMgr.GetCurrentTime()).Select(el => new Tuple<int, int>(e.Key, el.Key))))
				m_tooltipRequestTimes[keys.Item1].TryRemove(keys.Item2, out _);
			
			// Query hit ?
			if (m_tooltipRequestTimes[type].ContainsKey(id))
				return false;
		
			// Query register
			m_tooltipRequestTimes[type].TryAdd(id, GameLoopMgr.GetCurrentTime()+3600000);
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
		public EClientState ClientState
		{
			get { return m_clientState; }
			set
			{
				EClientState oldState = m_clientState;

				// refresh ping timeouts immediately when we change into playing state or charscreen
				if ((oldState != EClientState.Playing && value == EClientState.Playing) ||
				    (oldState != EClientState.CharScreen && value == EClientState.CharScreen))
				{
					PingTime = GameLoopMgr.GetCurrentTime();
				}

				m_clientState = value;
				GameEventMgr.Notify(GameClientEvent.StateChanged, this);
				//DOLConsole.WriteSystem("New State="+value.ToString());
			}
		}

		/// <summary>
		/// When the linkdeath occured. 0 if there wasn't any
		/// </summary>
		public long LinkDeathTime { get; set; }

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
				return m_clientState == EClientState.Playing || m_clientState == EClientState.Linkdead;
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
		public EClientVersion Version
		{
			get { return m_clientVersion; }
			set { m_clientVersion = value; }
		}

		/// <summary>
		/// Gets/sets client software type (classic/SI/ToA/Catacombs)
		/// </summary>
		public EClientType ClientType
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
		public EClientAddons ClientAddons
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
			if (Version == EClientVersion.VersionNotChecked)
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

				EClientVersion ver;
				IPacketLib lib = APacketLib.CreatePacketLibForVersion(version, this, out ver);

				if (lib == null)
				{
					Version = EClientVersion.VersionUnknown;
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

			if (Version != EClientVersion.VersionUnknown)
			{
				m_packetProcessor.ReceiveBytes(numBytes);
			}
		}

		/// <summary>
		/// Called when this client has been disconnected
		/// </summary>
		public override void OnDisconnect()
		{
			try
			{
				//If we went linkdead and we were inside the game
				//we don't let the player disappear!
				if (ClientState == EClientState.Playing)
				{
					OnLinkdeath();
					return;
				}

				if (ClientState == EClientState.WorldEnter && Player != null)
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
				if (ClientState != EClientState.Linkdead)
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
			LoadPlayer(accountindex, ServerProperty.PLAYER_CLASS);
		} 
		public void LoadPlayer(DbCoreCharacter dolChar)
		{
			LoadPlayer(dolChar, ServerProperty.PLAYER_CLASS);
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
					if (ServerProperty.KICK_IDLE_PLAYER_STATUS)
					{
						//Time playing
						var connectedtime = DateTime.Now.Subtract(m_account.LastLogin).TotalMinutes;
						//Lets get our player from DB.
						var getp = GameServer.Database.FindObjectByKey<DbCoreCharacter>(m_player.InternalID);
						//Let get saved poistion from DB.
						int[] oldloc = { getp.Xpos, getp.Ypos, getp.Zpos, getp.Direction, getp.Region };
						//Lets get current player Gloc.
						int[] currentloc = { m_player.X, m_player.Y, m_player.Z, m_player.Heading, m_player.CurrentRegionID };
						//Compapre Old and Current.
						bool check = oldloc.SequenceEqual(currentloc);
						//If match
						if (check)
						{
							if (connectedtime > ServerProperty.KICK_IDLE_PLAYER_TIME)
							{
								//Kick player
								m_player.Out.SendPlayerQuit(true);
								m_player.SaveIntoDatabase();
								m_player.Quit(true);
								//log
								if (log.IsErrorEnabled)
									log.Debug("Player " + m_player.Name + " Kicked due to Inactivity ");
							}
						}
						else
						{
							m_player.SaveIntoDatabase();
						}
					}

					else
					{
						m_player.SaveIntoDatabase();
					}

				}
			}

			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("SavePlayer", e);
			}
		}

		/// <summary>
		/// Called when a player goes linkdead
		/// </summary>
		protected void OnLinkdeath()
		{
			if (log.IsDebugEnabled)
				log.Debug("Linkdeath called (" + Account.Name + ")  client state=" + ClientState);

			//If we have no sessionid we simply disconnect
			GamePlayer curPlayer = Player;
			if (SessionID == 0 || curPlayer == null)
			{
				Quit();
			}
			else
			{
				ClientState = EClientState.Linkdead;
				LinkDeathTime = GameLoopMgr.GameLoopTime;
				// If we have a good sessionid, we won't remove the client yet!
				// OnLinkdeath() can start a timer to remove the client "a bit later"
				curPlayer.OnLinkdeath();
			}
		}

		/// <summary>
		/// Quits a client from the world
		/// </summary>
		protected internal void Quit()
		{
			lock (this)
			{
				try
				{
					EClientState oldClientState = ClientState;

					if (SessionID != 0)
					{
						if (oldClientState is EClientState.Playing or EClientState.WorldEnter or EClientState.Linkdead)
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

						try
						{
							Player?.Delete();
						}
						catch (Exception e)
						{
							log.Error("client cleanup on quit", e);
						}
					}

					ClientState = EClientState.Disconnected;
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
						AuditMgr.AddAuditEntry(this, EAuditType.Account, EAuditSubType.AccountLogout, "", Account.Name);
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
				.Append(" class:").Append(Player == null ? "null" : Player.PlayerClass.ID.ToString())
				.ToString();
		}
	}
}
