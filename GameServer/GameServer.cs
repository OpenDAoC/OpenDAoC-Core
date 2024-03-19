using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using DOL.Config;
using DOL.Database;
using DOL.Database.Attributes;
using DOL.Events;
using DOL.GS.Behaviour;
using DOL.GS.DatabaseUpdate;
using DOL.GS.Housing;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests;
using DOL.GS.ServerProperties;
using DOL.GS.ServerRules;
using DOL.Language;
using DOL.Mail;
using DOL.Network;
using JNogueira.Discord.Webhook.Client;
using log4net;
using log4net.Config;
using log4net.Core;

namespace DOL.GS
{
	/// <summary>
	/// Class encapsulates all game server functionality
	/// </summary>
	public class GameServer : BaseServer
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		#region Variables

		public DateTime StartupTime;

		/// <summary>
		/// Minute conversion from milliseconds
		/// </summary>
		protected const int MINUTE_CONV = 60000;

		/// <summary>
		/// The instance!
		/// </summary>
		protected static GameServer m_instance;

		/// <summary>
		/// The textwrite for log operations
		/// </summary>
		protected ILog m_cheatLog;
		
		/// <summary>
		/// The textwrite for log operations
		/// </summary>
		protected ILog m_dualIPLog;

		/// <summary>
		/// Database instance
		/// </summary>
		protected IObjectDatabase m_database;

		/// <summary>
		/// The textwrite for log operations
		/// </summary>
		protected ILog m_gmLog;

		/// <summary>
		/// The textwrite for log operations
		/// </summary>
		protected ILog m_inventoryLog;

		/// <summary>
		/// Holds instance of current server rules
		/// </summary>
		protected IServerRules m_serverRules;

		/// <summary>
		/// Holds the instance of the current keep manager
		/// </summary>
		protected IKeepManager m_keepManager;

		/// <summary>
		/// Holds the startSystemTick when server is up.
		/// </summary>
		protected int m_startTick;

		/// <summary>
		/// Game server status variable
		/// </summary>
		protected EGameServerStatus m_status;

		/// <summary>
		/// World save timer
		/// </summary>
		protected Timer m_timer;

		/// <summary>
		/// A general logger for the server
		/// </summary>
		public ILog Logger
		{
			get { return log; }
		}

		public List<String> PatchNotes;

		#endregion

		#region Properties
		/// <summary>
		/// Returns the instance
		/// </summary>
		public static GameServer Instance => m_instance;

		/// <summary>
		/// Retrieves the server configuration
		/// </summary>
		public new virtual GameServerConfiguration Configuration => base.Configuration as GameServerConfiguration;

		/// <summary>
		/// Gets the server status
		/// </summary>
		public EGameServerStatus ServerStatus
		{
			get { return m_status; }
		}

		/// <summary>
		/// Gets the server Scheduler
		/// </summary>
		public Scheduler.SimpleScheduler Scheduler { get; protected set; }

		/// <summary>
		/// Gets the server WorldManager
		/// </summary>
		public WorldManager WorldManager { get; protected set; }

		/// <summary>
		/// Gets the server PlayerManager
		/// </summary>
		public PlayerManager PlayerManager { get; protected set; }

		/// <summary>
		/// Gets the server NpcManager
		/// </summary>
		public NpcManager NpcManager { get; protected set; }

		protected virtual IServerRules ServerRulesImpl
		{
			get
			{
				if (Instance.m_serverRules == null)
				{
					Instance.m_serverRules = ScriptMgr.CreateServerRules(Instance.Configuration.ServerType);
					if (Instance.m_serverRules != null)
					{
						Instance.m_serverRules.Initialize();
					}
					else
					{
						if (log.IsErrorEnabled)
						{
							log.Error("ServerRules null on access and failed to create.");
						}
					}
				}
				return Instance.m_serverRules;
			}
		}

		/// <summary>
		/// Gets the current rules used by server
		/// </summary>
		public static IServerRules ServerRules => m_instance.ServerRulesImpl;

		public static IKeepManager KeepManager
		{
			get
			{
				if (Instance.m_keepManager == null)
				{
					Instance.StartKeepManager();
					if (Instance.m_keepManager == null && log.IsErrorEnabled)
					{
						log.Error("Could not get or start Keep Manager!");
					}
				}

				return Instance.m_keepManager;
			}
		}

		protected virtual IObjectDatabase DataBaseImpl
		{
			get
			{
				return Instance.m_database;
			}
		}

		/// <summary>
		/// Gets the database instance
		/// </summary>
		public static IObjectDatabase Database => m_instance.DataBaseImpl;

		/// <summary>
		/// Gets this Instance's Database
		/// </summary>
		public IObjectDatabase IDatabase
		{
			get { return m_database; }
		}

		/// <summary>
		/// Gets or sets the world save interval
		/// </summary>
		public int SaveInterval
		{
			get { return Configuration.SaveInterval; }
			set
			{
				Configuration.SaveInterval = value;
				if (m_timer != null)
					m_timer.Change(value * MINUTE_CONV, Timeout.Infinite);
			}
		}

		/// <summary>
		/// Gets the number of millisecounds elapsed since the GameServer started.
		/// </summary>
		public int TickCount
		{
			get { return Environment.TickCount - m_startTick; }
		}

		#endregion

		#region Initialization

		public static void LoadTestDouble(GameServer server) { m_instance = server; }

		/// <summary>
		/// Creates the gameserver instance
		/// </summary>
		/// <param name="config"></param>
		public static void CreateInstance(GameServerConfiguration config)
		{
			//Only one intance
			if (Instance != null)
				return;

			//Try to find the log.config file, if it doesn't exist
			//we create it
			var logConfig = new FileInfo(config.LogConfigFile);
			if (!logConfig.Exists)
			{
				ResourceUtil.ExtractResource("logconfig.xml", logConfig.FullName);
			}

			//Configure and watch the config file
			XmlConfigurator.ConfigureAndWatch(logConfig);

			//Create the instance
			m_instance = new GameServer(config);
		}
		#endregion

		#region Start

		/// <summary>
		/// Starts the server
		/// </summary>
		/// <returns>True if the server was successfully started</returns>
		public override bool Start()
		{
			try
			{
				//Manually set ThreadPool min thread count.
				ThreadPool.GetMinThreads(out int minWorkerThreads, out int minIOCThreads);
				ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxIOCThreads);
				log.Info($"Default ThreadPool minworkthreads {minWorkerThreads} minIOCThreads {minIOCThreads} maxworkthreads {maxWorkerThreads} maxIOCThreads {maxIOCThreads}");

				if (log.IsDebugEnabled)
					log.DebugFormat("Starting Server, Memory is {0}MB", GC.GetTotalMemory(false) / 1024 / 1024);

				m_status = EGameServerStatus.GSS_Closed;
				Thread.CurrentThread.Priority = ThreadPriority.Normal;

				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
				//---------------------------------------------------------------
				//Try to compile the Scripts
				if (!InitComponent(CompileScripts(), "Script compilation"))
					return false;

				//---------------------------------------------------------------
				//Try to init Server Properties
				if (!InitComponent(Properties.InitProperties, "Server Properties Lookup"))
					return false;

				//---------------------------------------------------------------
				//Try loading the commands
				if (!InitComponent(ScriptMgr.LoadCommands(), "Loading Commands"))
					return false;

				//---------------------------------------------------------------
				//Check and update the database if needed
				if (!UpdateDatabase())
					return false;

				//---------------------------------------------------------------
				//Try to init the RSA key
				/* No Cryptlib currently
					if (log.IsInfoEnabled)
						log.Info("Generating RSA key, may take a minute, please wait...");
					if (!InitComponent(CryptLib168.GenerateRSAKey(), "RSA key generation"))
						return false;
				 */

				//---------------------------------------------------------------
				//Try to initialize the Scheduler
				if (!InitComponent(() => Scheduler = new Scheduler.SimpleScheduler(), "Scheduler Initialization"))
					return false;

				//---------------------------------------------------------------
				//Try to initialize the WorldManager
				if (!InitComponent(() => WorldManager = new WorldManager(this), "Instancied World Manager Initialization"))
					return false;

				//---------------------------------------------------------------
				//Try to initialize the PlayerManager
				if (!InitComponent(() => PlayerManager = new PlayerManager(this), "Player Manager Initialization"))
					return false;

				//---------------------------------------------------------------
				//Try to initialize the NpcManager
				if (!InitComponent(() => NpcManager = new NpcManager(this), "NPC Manager Initialization"))
					return false;

				//---------------------------------------------------------------
				//Try to start the Language Manager
				if (!InitComponent(LanguageMgr.Init(), "Multi Language Initialization"))
					return false;

				//Init the mail manager
				InitComponent(MailMgr.Init(), "Mail Manager Initialization");

				//---------------------------------------------------------------
				//Try to initialize the WorldMgr in early state
				RegionData[] regionsData;
				if (!InitComponent(WorldMgr.EarlyInit(out regionsData), "World Manager PreInitialization"))
					return false;

				//---------------------------------------------------------------
				//Try to initialize the Pathing Manager
				if (!InitComponent(PathingMgr.Init(), "Pathing Manager Initialization"))
					return false;

				//---------------------------------------------------------------
				//Try to initialize the script components
				if (!InitComponent(StartScriptComponents(), "Script components"))
					return false;

				//---------------------------------------------------------------
				//Load all faction managers
				if (!InitComponent(FactionMgr.Init(), "Faction Managers"))
					return false;

				//---------------------------------------------------------------
				//Load all calculators
				if (!InitComponent(GameLiving.LoadCalculators(), "GameLiving.LoadCalculators()"))
					return false;

				//---------------------------------------------------------------
				//Try to start the npc equipment
				if (!InitComponent(GameNpcInventoryTemplate.Init(), "Npc Equipment"))
					return false;

				//---------------------------------------------------------------
				//Try to start the Npc Templates Manager
				if (!InitComponent(NpcTemplateMgr.Init(), "Npc Templates Manager"))
					return false;

				//---------------------------------------------------------------
				//Load the house manager
				if (!InitComponent(HouseMgr.Start(), "House Manager"))
					return false;

				//---------------------------------------------------------------
				//Create the market cache
				if (!InitComponent(MarketCache.Initialize(), "Market Cache"))
					return false;

				//---------------------------------------------------------------
				//Load the region managers
				if (!InitComponent(WorldMgr.StartRegionMgrs(), "Region Managers"))
					return false;

				//---------------------------------------------------------------
				//Enable Worldsave timer now
				if (m_timer != null)
				{
					m_timer.Change(Timeout.Infinite, Timeout.Infinite);
					m_timer.Dispose();
				}
				m_timer = new Timer(SaveTimerProc, null, SaveInterval * MINUTE_CONV, Timeout.Infinite);
				if (log.IsInfoEnabled)
					log.Info("World save timer: true");

				//---------------------------------------------------------------
				//Load all boats
				if (!InitComponent(BoatMgr.LoadAllBoats(), "Boat Manager"))
					return false;

				//---------------------------------------------------------------
				//Load all guilds
				if (!InitComponent(GuildMgr.LoadAllGuilds(), "Guild Manager"))
					return false;

				//---------------------------------------------------------------
				//Load the keep manager
				if (!InitComponent(StartKeepManager(), "Keep Manager"))
					return false;

				//---------------------------------------------------------------
				//Load the door manager
				if (!InitComponent(DoorMgr.Init(), "Door Manager"))
					return false;

				//---------------------------------------------------------------
				//Load the area manager
				if (!InitComponent(AreaMgr.LoadAllAreas(), "Areas"))
					return false;

				//---------------------------------------------------------------
				//Try to initialize the WorldMgr
				if (!InitComponent(WorldMgr.Init(regionsData), "World Manager Initialization"))
					return false;
				regionsData = null;

				//---------------------------------------------------------------
				//Load the relic manager
				if (!InitComponent(RelicMgr.Init(), "Relic Manager"))
					return false;

				//---------------------------------------------------------------
				//Load all crafting managers
				if (!InitComponent(CraftingMgr.Init(), "Crafting Managers"))
					return false;

				//---------------------------------------------------------------
				//Load player titles manager
				if (!InitComponent(PlayerTitleMgr.Init(), "Player Titles Manager"))
					return false;

				//---------------------------------------------------------------
				//Load behaviour manager
				if (!InitComponent(BehaviourMgr.Init(), "Behaviour Manager"))
					return false;

				//Load the quest managers if enabled
				if (Properties.LOAD_QUESTS)
				{
					if (!InitComponent(QuestMgr.Init(), "Quest Manager"))
						return false;
				}
				else
				{
					log.InfoFormat("Not Loading Quest Manager : Obeying Server Property <load_quests> - {0}", Properties.LOAD_QUESTS);
				}

				//---------------------------------------------------------------
				//Notify our scripts that everything went fine!
				GameEventMgr.Notify(ScriptEvent.Loaded);

				//---------------------------------------------------------------
				//Set the GameServer StartTick
				m_startTick = Environment.TickCount;

				//---------------------------------------------------------------
				//Notify everyone that the server is now started!
				GameEventMgr.Notify(GameServerEvent.Started, this);

				//---------------------------------------------------------------
				//Try to start the base server (open server port for connections)
				if (!InitComponent(base.Start(), "base.Start()"))
					return false;

				if (!InitComponent(GameLoop.Init(), "GameLoop Init"))
					return false;

				GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

				log.Info($"GarbageCollection IsServerGC: {System.Runtime.GCSettings.IsServerGC}" );

				//---------------------------------------------------------------
				//Open the server, players can now connect if webhook, inform Discord!
				m_status = EGameServerStatus.GSS_Open;
				StartupTime = DateTime.Now;

				if (Properties.DISCORD_ACTIVE && (!string.IsNullOrEmpty(Properties.DISCORD_WEBHOOK_ID)))
				{

					var client = new DiscordWebhookClient(Properties.DISCORD_WEBHOOK_ID);
					
 					var message = new DiscordMessage(
 						"",
 						username: "Game Server",
 						avatarUrl: "",
 						tts: false,
 						embeds: new[]
 						{
 							new DiscordMessageEmbed(
	                            color: 3066993,
	                            description: "Server open for connections!",
                                thumbnail: new DiscordMessageEmbedThumbnail("")
                            )
 						}
 					);

					client.SendToDiscord(message);
				}

				if (log.IsInfoEnabled)
					log.Info("GameServer is now open for connections!");

				if (Properties.ATLAS_API)
				{
					var webserver = new DOL.GS.API.ApiHost();
					log.Info("Game WebAPI open for connections.");
				}

				GetPatchNotes();
				return true;
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("Failed to start the server", e);

				return false;
			}
		}

		public async void GetPatchNotes()
		{
			try
			{
				using var newsClient = new HttpClient();
				var url = Properties.PATCH_NOTES_URL;
				var newsResult = await newsClient.GetStringAsync(url);
				PatchNotes = [newsResult];
				log.Debug("Patch notes updated.");
				newsClient.Dispose();
			}
			catch (Exception)
			{
				log.Debug("Cannot retrieve patch notes.");
			}
		}

		/// <summary>
		/// Logs unhandled exceptions
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			log.Fatal("Unhandled exception!\n" + e.ExceptionObject);
			if (e.IsTerminating)
				LogManager.Shutdown();
		}

		/// <summary>
		/// Recompiles or loads the scripts dll
		/// </summary>
		/// <returns></returns>
		public bool CompileScripts()
		{
			string scriptDirectory = Path.Combine(Configuration.RootDirectory, "scripts");
			if (!Directory.Exists(scriptDirectory))
				Directory.CreateDirectory(scriptDirectory);

			bool compiled = false;

			// Check if Configuration Forces to use Pre-Compiled Game Server Scripts Assembly
			if (!Configuration.EnableCompilation)
			{
				log.Info("Script Compilation Disabled in Server Configuration, Loading pre-compiled Assembly...");

				if (File.Exists(Configuration.ScriptCompilationTarget))
				{
					ScriptMgr.LoadAssembly(Configuration.ScriptCompilationTarget);
				}
				else
				{
					log.WarnFormat("Compilation Disabled - Could not find pre-compiled Assembly : {0} - Server starting without Scripts Assembly!", Configuration.ScriptCompilationTarget);
				}

				compiled = true;
			}
			else
			{
				compiled = ScriptMgr.CompileScripts(false, scriptDirectory, Configuration.ScriptCompilationTarget, Configuration.AdditionalScriptAssemblies);
			}

			if (compiled)
			{
				//---------------------------------------------------------------
				//Register Script Tables
				if (log.IsInfoEnabled)
					log.Info("GameServerScripts Tables Initializing...");

				try
				{
					// Walk through each assembly in scripts
					foreach (Assembly asm in ScriptMgr.Scripts)
					{
						// Walk through each type in the assembly
						foreach (Type type in asm.GetTypes())
						{
							if (type.IsClass != true || !typeof(DataObject).IsAssignableFrom(type))
								continue;

							object[] attrib = type.GetCustomAttributes(typeof(DataTable), false);
							if (attrib.Length > 0)
							{
								if (log.IsInfoEnabled)
									log.Info("Registering Scripts table: " + type.FullName);

								GameServer.Database.RegisterDataObject(type);
							}
						}
					}
				}
				catch (DatabaseException dbex)
				{
					if (log.IsErrorEnabled)
						log.Error("Error while registering Script Tables", dbex);

					return false;
				}

				if (log.IsInfoEnabled)
					log.Info("GameServerScripts Database Tables Initialization: true");

				return true;
			}

			return false;
		}

		/// <summary>
		/// Initialize all script components
		/// </summary>
		/// <returns>true if successfull, false if not</returns>
		protected bool StartScriptComponents()
		{
			try
			{
				//---------------------------------------------------------------
				//Create the server rules
				m_serverRules = ScriptMgr.CreateServerRules(Configuration.ServerType);
				m_serverRules.Initialize();

				if (log.IsInfoEnabled)
					log.Info("Server rules: true");

				//---------------------------------------------------------------
				//Load the skills
				SkillBase.LoadSkills();
				if (log.IsInfoEnabled)
					log.Info("Loading skills: true");

				//---------------------------------------------------------------
				//Register all event handlers
				foreach (Assembly asm in ScriptMgr.GameServerScripts)
				{
					GameEventMgr.RegisterGlobalEvents(asm, typeof(GameServerStartedEventAttribute), GameServerEvent.Started);
					GameEventMgr.RegisterGlobalEvents(asm, typeof(GameServerStoppedEventAttribute), GameServerEvent.Stopped);
					GameEventMgr.RegisterGlobalEvents(asm, typeof(ScriptLoadedEventAttribute), ScriptEvent.Loaded);
					GameEventMgr.RegisterGlobalEvents(asm, typeof(ScriptUnloadedEventAttribute), ScriptEvent.Unloaded);
				}
				if (log.IsInfoEnabled)
					log.Info("Registering global event handlers: true");
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("StartScriptComponents", e);
				return false;
			}
			//---------------------------------------------------------------
			return true;
		}

		/// <summary>
		/// Find the keep manager and start it
		/// </summary>
		/// <returns></returns>
		protected bool StartKeepManager()
		{
			Type keepManager = null;

			// first search in scripts
			foreach (Assembly script in ScriptMgr.Scripts)
			{
				foreach (Type type in script.GetTypes())
				{
					if (type.IsClass == false) continue;
					if (type.GetInterface("DOL.GS.Keeps.IKeepManager") == null) continue;

					// look for attribute
					try
					{
						object[] objs = type.GetCustomAttributes(typeof(KeepManagerAttribute), false);
						if (objs.Length == 0) continue;

						// found a keep manager, use it
						keepManager = type;
						break;
					}
					catch (Exception e)
					{
						if (log.IsErrorEnabled)
							log.Error("StartKeepManager, Script Search", e);
					}

					if (keepManager != null) break;
				}
			}

			if (keepManager == null)
			{
				// second search in gameserver
				foreach (Type type in Assembly.GetAssembly(typeof(GameServer)).GetTypes())
				{
					if (type.IsClass == false) continue;
					if (type.GetInterface("DOL.GS.Keeps.IKeepManager") == null) continue;

					// look for attribute
					try
					{
						object[] objs = type.GetCustomAttributes(typeof(KeepManagerAttribute), false);
						if (objs.Length == 0) continue;

						// found a keep manager, use it
						keepManager = type;
						break;
					}
					catch (Exception e)
					{
						if (log.IsErrorEnabled)
							log.Error("StartKeepManager, GameServer Search", e);
					}
					if (keepManager != null) break;
				}

			}

			if (keepManager != null)
			{
				try
				{
					IKeepManager manager = Activator.CreateInstance(keepManager, null) as IKeepManager;

					if (log.IsInfoEnabled)
						log.Info("Found KeepManager " + manager.GetType().FullName);

					m_keepManager = manager;
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error("StartKeepManager, CreateInstance", e);
				}
			}

			if (m_keepManager == null)
			{
				m_keepManager = new DefaultKeepManager();

				if (m_keepManager != null)
				{
					log.Warn("No Keep manager found, using " + m_keepManager.GetType().FullName);
				}
				else
				{
					log.Error("Cannot create Keep manager!");
					return false;
				}
			}

			return m_keepManager.Load();
		}

		/// <summary>
		/// Do any required updates to the database
		/// </summary>
		/// <returns>true if all went fine, false if errors</returns>
		protected virtual bool UpdateDatabase()
		{
			bool result = true;
			try
			{
				log.Info("Checking database for updates ...");

				foreach (Assembly asm in ScriptMgr.GameServerScripts)
				{

					foreach (Type type in asm.GetTypes())
					{
						if (!type.IsClass)
							continue;
						if (!typeof(IDatabaseUpdater).IsAssignableFrom(type))
							continue;

						object[] attributes = type.GetCustomAttributes(typeof(DatabaseUpdateAttribute), false);
						if (attributes.Length <= 0)
							continue;

						try
						{
							var instance = Activator.CreateInstance(type) as IDatabaseUpdater;
							instance.Update();
						}
						catch (Exception uex)
						{
							if (log.IsErrorEnabled)
								log.ErrorFormat("Error While Updating Database with Script {0} - {1}", type, uex);

							result = false;
						}
					}
				}
			}
			catch (Exception e)
			{
				log.Error("Error checking/updating database: ", e);
				return false;
			}

			log.Info("Database update complete.");
			return result;
		}

		/// <summary>
		/// Prints out some text info on component initialisation
		/// and stops the server again if the component failed
		/// </summary>
		/// <param name="componentInitState">The state</param>
		/// <param name="text">The text to print</param>
		/// <returns>false if startup should be interrupted</returns>
		protected bool InitComponent(bool componentInitState, string text)
		{
			if (log.IsDebugEnabled)
				log.DebugFormat("Start Memory {0}: {1}MB", text, GC.GetTotalMemory(false) / 1024 / 1024);

			if (log.IsInfoEnabled)
				log.InfoFormat("{0}: {1}", text, componentInitState);

			if (!componentInitState)
				Stop();

			if (log.IsDebugEnabled)
				log.DebugFormat("Finish Memory {0}: {1}MB", text, GC.GetTotalMemory(false) / 1024 / 1024);

			return componentInitState;
		}

		protected bool InitComponent(Action componentInitMethod, string text)
		{
			if (log.IsDebugEnabled)
				log.DebugFormat("Start Memory {0}: {1}MB", text, GC.GetTotalMemory(false) / 1024 / 1024);

			bool componentInitState = false;
			try
			{
				componentInitMethod();
				componentInitState = true;
			}
			catch (Exception ex)
			{
				if (log.IsErrorEnabled)
					log.ErrorFormat("{0}: Error While Initialization\n{1}", text, ex);
			}

			if (log.IsInfoEnabled)
				log.InfoFormat("{0}: {1}", text, componentInitState);

			if (!componentInitState)
				Stop();

			if (log.IsDebugEnabled)
				log.DebugFormat("Finish Memory {0}: {1}MB", text, GC.GetTotalMemory(false) / 1024 / 1024);

			return componentInitState;
		}
		#endregion

		#region Stop

		public void Close()
		{
			m_status = EGameServerStatus.GSS_Closed;
		}

		public void Open()
		{
			m_status = EGameServerStatus.GSS_Open;
		}

		/// <summary>
		/// Stops the server, disconnects all clients, and writes the database to disk
		/// </summary>
		public override void Stop()
		{
			//Stop new clients from logging in
			m_status = EGameServerStatus.GSS_Closed;

			log.Info("GameServer.Stop() - enter method");

			//Notify our scripthandlers
			GameEventMgr.Notify(ScriptEvent.Unloaded);

			//Notify of the global server stop event
			//We notify before we shutdown the database
			//so that event handlers can use the datbase too
			GameEventMgr.Notify(GameServerEvent.Stopped, this);
			GameEventMgr.RemoveAllHandlers(true);

			//Stop the World Save timer
			if (m_timer != null)
			{
				m_timer.Change(Timeout.Infinite, Timeout.Infinite);
				m_timer.Dispose();
				m_timer = null;
			}

			//Stop the base server
			base.Stop();

			//Stop all mobMgrs
			WorldMgr.StopRegionMgrs();

			//Stop the WorldMgr, save all players
			//WorldMgr.SaveToDatabase();
			SaveTimerProc(null);

			WorldMgr.Exit();
			GameLoop.Exit();

			//Save the database
			// 2008-01-29 Kakuri - Obsolete
			/*if ( m_database != null )
				{
					m_database.WriteDatabaseTables();
				}*/

			m_serverRules = null;

			// Stop Server Scheduler
			if (Scheduler != null)
				Scheduler.Shutdown();
			Scheduler = null;

			Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

			if (log.IsInfoEnabled)
				log.Info("Server Stopped");

			LogManager.Shutdown();
		}

		#endregion

		#region Client

		/// <summary>
		/// Creates a new client
		/// </summary>
		/// <returns>An instance of a new client</returns>
		protected override BaseClient GetNewClient(Socket socket)
		{
			var client = new GameClient(this, socket);
			GameEventMgr.Notify(GameClientEvent.Created, client);
			client.UdpConfirm = false;

			return client;
		}

		protected override void OnUdpReceive(byte[] buffer, int offset, int size, EndPoint endPoint, Action freeBufferCallback)
		{
			if (m_status is not EGameServerStatus.GSS_Open)
				return;

			if (size == 0)
			{
				log.Debug("Received bytes = 0");
				return;
			}

			int endPosition = offset + size;
			int packetCheck = (buffer[endPosition - 2] << 8) | buffer[endPosition - 1];
			int calculatedCheck = PacketProcessor.CalculateChecksum(buffer, offset, size - 2);

			if (packetCheck != calculatedCheck)
			{
				if (log.IsWarnEnabled)
					log.Warn($"Bad UDP packet checksum (packet:0x{packetCheck:X4} calculated:0x{calculatedCheck:X4})");

				if (log.IsDebugEnabled)
					log.Debug(Marshal.ToHexDump($"UDP buffer dump, received {size} bytes", buffer));

				return;
			}

			GSPacketIn packet = new(endPosition - GSPacketIn.HDR_SIZE);
			packet.Load(buffer, offset, size);
			freeBufferCallback(); // Notify the caller we're no longer going to use the buffer.
			GameClient client = ClientService.GetClientFromId(packet.SessionID);

			if (client == null)
			{
				if (log.IsErrorEnabled)
					log.Error($"Got an UDP packet from invalid client id or ip: client id = {packet.SessionID}, ip = {endPoint},  code = {packet.ID:x2}");

				return;
			}

			// If this is the first message from this client, we save the endpoint.
			if (client.UdpEndPoint == null)
			{
				client.UdpEndPoint = endPoint as IPEndPoint;
				client.UdpConfirm = false;
			}

			// Only handle the packet if it comes from a valid client.
			if (client.UdpEndPoint.Equals(endPoint))
				client.PacketProcessor.ProcessInboundPacket(packet);
		}

		#endregion

		#region Logging

		/// <summary>
		/// Writes a line to the gm log file
		/// </summary>
		/// <param name="text">the text to log</param>
		public void LogGMAction(string text)
		{
			m_gmLog.Logger.Log(typeof(GameServer), Level.Alert, text, null);
		}

		/// <summary>
		/// Writes a line to the cheat log file
		/// </summary>
		/// <param name="text">the text to log</param>
		public void LogCheatAction(string text)
		{
			m_cheatLog.Logger.Log(typeof(GameServer), Level.Alert, text, null);
			log.Debug(text);
		}
		
		/// <summary>
		/// Writes a line to the cheat log file
		/// </summary>
		/// <param name="text">the text to log</param>
		public void LogDualIPAction(string text)
		{
			m_dualIPLog.Logger.Log(typeof(GameServer), Level.Alert, text, null);
			log.Debug(text);
		}

		/// <summary>
		/// Writes a line to the inventory log file
		/// </summary>
		/// <param name="text">the text to log</param>
		public void LogInventoryAction(string text)
		{
			m_inventoryLog.Logger.Log(typeof(GameServer), Level.Alert, text, null);
		}

		#endregion

		#region Database

		/// <summary>
		/// Initializes the database
		/// </summary>
		/// <returns>True if the database was successfully initialized</returns>
		public bool InitDB()
		{
			if (m_database == null)
			{
				m_database = ObjectDatabase.GetObjectDatabase(Configuration.DBType, Configuration.DBConnectionString);

				try
				{
					//We will search our assemblies for DataTables by reflection so
					//it is not neccessary anymore to register new tables with the
					//server, it is done automatically!
					foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
					{
						// Walk through each type in the assembly
						assembly.GetTypes().AsParallel().ForAll(type =>
						{
							if (!type.IsClass || type.IsAbstract)
							{
								return;
							}

							var attrib = type.GetCustomAttributes<DataTable>(false);
							if (attrib.Any())
							{
								if (log.IsInfoEnabled)
								{
									log.InfoFormat("Registering table: {0}", type.FullName);
								}

								m_database.RegisterDataObject(type);
							}
						});
					}
				}
				catch (DatabaseException e)
				{
					if (log.IsErrorEnabled)
						log.Error("Error registering Tables", e);
					return false;
				}
			}
			if (log.IsInfoEnabled)
				log.Info("Database Initialization: true");
			return true;
		}

		/// <summary>
		/// Function called at X interval to write the database to disk
		/// </summary>
		/// <param name="sender">Object that generated the event</param>
		protected void SaveTimerProc(object sender)
		{
			ThreadPriority oldPriority = Thread.CurrentThread.Priority;

			try
			{
				long startTick = GameLoop.GetCurrentTime();
				long startTick2 = GameLoop.GetCurrentTime();

				if (log.IsInfoEnabled)
					log.Info("Saving database...");

				(int count, long elapsed) players = (0, 0);
				(int count, long elapsed) keepDoors = (0, 0);
				(int count, long elapsed) guilds = (0, 0);
				(int count, long elapsed) boats = (0, 0);
				(int count, long elapsed) factions = (0, 0);
				(int count, long elapsed) crafting = (0, 0);

				if (m_database != null)
				{
					Thread.CurrentThread.Priority = ThreadPriority.Lowest;

					// The following line goes through EACH region and EACH object is tested for savability. A real waste of time, so it is commented out.
					// Only save players instead.
					//WorldMgr.SaveToDatabase();
					Save(ClientService.SavePlayers, ref players);
					Save(DoorMgr.SaveKeepDoors, ref keepDoors);
					Save(GuildMgr.SaveAllGuilds, ref guilds);
					Save(BoatMgr.SaveAllBoats, ref boats);
					Save(FactionMgr.SaveAllAggroToFaction, ref factions);
					Save(CraftingProgressMgr.Save, ref crafting);
				}

				startTick = GameLoop.GetCurrentTime() - startTick;

				if (log.IsInfoEnabled)
				{
					StringBuilder stringBuilder = new();
					stringBuilder.Append($"Saving completed in {startTick}ms\n");
					stringBuilder.Append($"   {nameof(players)}: {players.count} in {players.elapsed}ms\n");
					stringBuilder.Append($" {nameof(keepDoors)}: {keepDoors.count} in {keepDoors.elapsed}ms\n");
					stringBuilder.Append($"    {nameof(guilds)}: {guilds.count} in {guilds.elapsed}ms\n");
					stringBuilder.Append($"     {nameof(boats)}: {boats.count} in {boats.elapsed}ms\n");
					stringBuilder.Append($"  {nameof(factions)}: {factions.count} in {factions.elapsed}ms\n");
					stringBuilder.Append($"  {nameof(crafting)}: {crafting.count} in {crafting.elapsed}ms");

					log.Info(stringBuilder.ToString());
				}
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("SaveTimerProc", e);
			}
			finally
			{
				m_timer?.Change(SaveInterval * MINUTE_CONV, Timeout.Infinite);
				Thread.CurrentThread.Priority = oldPriority;
			}

			static void Save(Func<int> save, ref (int count, long elapsed) result)
			{
				result.elapsed = GameLoop.GetCurrentTime();
				result.count = save();
				result.elapsed = GameLoop.GetCurrentTime() - result.elapsed;
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Default game server constructor
		/// </summary>
		protected GameServer() : this(new GameServerConfiguration()) { }

		/// <summary>
		/// Constructor with a given configuration
		/// </summary>
		/// <param name="config">A valid game server configuration</param>
		protected GameServer(GameServerConfiguration config) : base(config)
		{
			m_gmLog = LogManager.GetLogger(Configuration.GMActionsLoggerName);
			m_cheatLog = LogManager.GetLogger(Configuration.CheatLoggerName);
			m_dualIPLog = LogManager.GetLogger(Configuration.DualIPLoggerName);
			m_inventoryLog = LogManager.GetLogger(Configuration.InventoryLoggerName);

			if (log.IsDebugEnabled)
			{
				log.Debug("Current directory is: " + Directory.GetCurrentDirectory());
				log.Debug("Gameserver root directory is: " + Configuration.RootDirectory);
				log.Debug("Changing directory to root directory");
			}

			Directory.SetCurrentDirectory(Configuration.RootDirectory);

			try
			{
				CheckAndInitDB();

				if (log.IsInfoEnabled)
					log.Info("Game Server Initialization finished!");
			}
			catch (Exception e)
			{
				if (log.IsFatalEnabled)
					log.Fatal("GameServer initialization failed!", e);
				throw new ApplicationException("Fatal Error: Could not initialize Game Server", e);
			}
		}

		protected virtual void CheckAndInitDB()
		{
			if (!InitDB() || m_database == null)
			{
				if (log.IsErrorEnabled)
					log.Error("Could not initialize DB, please check path/connection string");
				throw new ApplicationException("DB initialization error");
			}
		}
		#endregion
	}
}
