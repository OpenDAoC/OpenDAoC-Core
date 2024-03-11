using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;
using log4net;
using Timer = System.Threading.Timer;

namespace DOL.GS
{
	/// <summary>
	/// The WorldMgr is used to retrieve information and objects from
	/// the world. It contains lots of functions that can be used. It
	/// is a static class.
	/// </summary>
	public static class WorldMgr
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Holds the distance which player get experience from a living object
		/// </summary>
		public const int MAX_EXPFORKILL_DISTANCE = VISIBILITY_DISTANCE;
		/// <summary>
		/// Is the distance a whisper can be heard
		/// </summary>
		public const int WHISPER_DISTANCE = 512; // tested
		/// <summary>
		/// Is the distance a say is broadcast
		/// </summary>
		public const int SAY_DISTANCE = 512; // tested
		/// <summary>
		/// Is the distance info messages are broadcast (player attacks, spell cast, player stunned/rooted/mezzed, loot dropped)
		/// </summary>
		public const int INFO_DISTANCE = 512; // tested for player attacks, think it's same for rest
		/// <summary>
		/// Is the distance a death message is broadcast when player dies
		/// </summary>
		public const ushort DEATH_MESSAGE_DISTANCE = ushort.MaxValue; // unknown
		/// <summary>
		/// Is the distance a yell is broadcast
		/// </summary>
		public const int YELL_DISTANCE = 1024; // tested
		/// <summary>
		/// Is the distance at which livings can give a item
		/// </summary>
		public const int GIVE_ITEM_DISTANCE = 128;  // tested
		/// <summary>
		/// Is the distance at which livings can interact
		/// </summary>
		public const int INTERACT_DISTANCE = 192;  // tested
		/// <summary>
		/// Is the distance an player can see
		/// </summary>
		public const int VISIBILITY_DISTANCE = 6000;
		/// <summary>
		/// Moving greater than this distance requires the player to do a full world refresh
		/// </summary>
		public const int REFRESH_DISTANCE = 1000;
		/// <summary>
		/// Is the square distance a player can see
		/// </summary>
		public const int VISIBILITY_SQUARE_DISTANCE = 36000000;
		/// <summary>
		/// Holds the distance at which objects are updated
		/// </summary>
		public const int OBJ_UPDATE_DISTANCE = 6144;

		/// <summary>
		/// This will store available teleport destinations as read from the 'teleport' table.  These are
		/// stored in a dictionary of dictionaries because the name of the teleport destination (the
		/// 'TeleportID' field from the table) does not have to be unique across realms.  Duplicate
		/// 'TeleportID' fields are permitted so long as the 'Realm' field is different for each.
		/// </summary>
		private static Dictionary<eRealm, Dictionary<string, DbTeleport>> m_teleportLocations;
		private static object m_syncTeleport = new object();

		// this is used to hold the player ids with timestamp of ld, that ld near an enemy keep structure, to allow grace period relog
		public static Dictionary<string, DateTime> RvRLinkDeadPlayers = new Dictionary<string, DateTime>();

		/// <summary>
		/// Returns the teleport given an ID and a realm
		/// </summary>
		/// <param name="realm">
		/// The home realm identifier of the NPC doing the teleporting.  Whether or not a teleport is
		/// permitted is determined by the home realm of the teleporter NPC, not the home realm of
		/// the player who is teleporting.  A teleport will be allowed so long as the 'Realm' field in
		/// the 'teleport' table matches the 'Realm' field for the teleporter's record in the 'mob' table.
		/// For example, a Lurikeen teleporter with a 'mob' table entry that has the Realm field set to 3
		/// (Hibernia), will happily teleport an Albion player to Jordheim so long as the Jordheim record
		/// in the 'teleport' table is also tagged as Realm 3.  So, the Realm field in the 'teleport'
		/// table is not the realm of the destination, but the realm of the NPCs that are allowed to
		/// teleport a player to that location.
		/// </param>
		/// <param name="teleportKey">Composite key into teleport dictionary.</param>
		/// <returns></returns>
		public static DbTeleport GetTeleportLocation(eRealm realm, String teleportKey)
		{
			lock (m_syncTeleport)
			{
				return (m_teleportLocations.ContainsKey(realm)) ?
					(m_teleportLocations[realm].ContainsKey(teleportKey) ?
					 m_teleportLocations[realm][teleportKey] :
					 null) :
					null;
			}
		}

		/// <summary>
		/// Add a new teleport destination (used by /teleport add).
		/// </summary>
		/// <param name="teleport"></param>
		/// <returns></returns>
		public static bool AddTeleportLocation(DbTeleport teleport)
		{
			eRealm realm = (eRealm)teleport.Realm;
			String teleportKey = String.Format("{0}:{1}", teleport.Type, teleport.TeleportID);

			lock (m_syncTeleport)
			{
				Dictionary<String, DbTeleport> teleports = null;
				if (m_teleportLocations.ContainsKey(realm))
				{
					if (m_teleportLocations[realm].ContainsKey(teleportKey))
						return false;   // Double entry.

					teleports = m_teleportLocations[realm];
				}

				if (teleports == null)
				{
					teleports = new Dictionary<String, DbTeleport>();
					m_teleportLocations.Add(realm, teleports);
				}

				teleports.Add(teleportKey, teleport);
				return true;
			}
		}

		/// <summary>
		/// This hashtable holds all regions in the world
		/// </summary>
		private static readonly ConcurrentDictionary<ushort, Region> m_regions = new();

		public static IDictionary<ushort, Region> Regions
		{
			get { return m_regions; }
		}

		/// <summary>
		/// This hashtable holds all zones in the world, for easy access
		/// </summary>
		private static readonly ConcurrentDictionary<ushort, Zone> m_zones = new();

		public static IDictionary<ushort, Zone> Zones
		{
			get { return m_zones; }
		}

		/// <summary>
		/// This constant defines the day constant
		/// </summary>
		private const int DAY = 86400000;

		/// <summary>
		/// This holds the tick when the day started
		/// </summary>
		private static int m_dayStartTick;

		/// <summary>
		/// This holds the speed of our days
		/// </summary>
		private static uint m_dayIncrement;

		/// <summary>
		/// A timer that will send the daytime to all playing
		/// clients after a certain intervall;
		/// </summary>
		private static Timer m_dayResetTimer;

		/// <summary>
		/// Region ID INI field
		/// </summary>
		private const string ENTRY_REG_ID = "id";
		/// <summary>
		/// Region IP INI field
		/// </summary>
		private const string ENTRY_REG_IP = "ip";
		/// <summary>
		/// Region port INI field
		/// </summary>
		private const string ENTRY_REG_PORT = "port";
		/// <summary>
		/// Region description INI field
		/// </summary>
		private const string ENTRY_REG_DESC = "description";
		/// <summary>
		/// Region diving enable INI field
		/// </summary>
		private const string ENTRY_REG_DIVING_ENABLE = "isDivingEnabled";
		/// <summary>
		/// Region diving enable INI field
		/// </summary>
		private const string ENTRY_REG_HOUSING_ENABLE = "isHousingEnabled";
		/// <summary>
		/// Region water level INI field
		/// </summary>
		private const string ENTRY_REG_WATER_LEVEL = "waterLevel";
		/// <summary>
		/// Region expansion INI field
		/// </summary>
		private const string ENTRY_REG_EXPANSION = "expansion";

		/// <summary>
		/// Zone ID INI field
		/// </summary>
		private const string ENTRY_ZONE_ZONEID = "zoneID";
		/// <summary>
		/// Zone region INI field
		/// </summary>
		private const string ENTRY_ZONE_REGIONID = "regionID";
		/// <summary>
		/// Zone description INI field
		/// </summary>
		private const string ENTRY_ZONE_DESC = "description";
		/// <summary>
		/// Zone X offset INI field
		/// </summary>
		private const string ENTRY_ZONE_OFFX = "offsetx";
		/// <summary>
		/// Zone Y offset INI field
		/// </summary>
		private const string ENTRY_ZONE_OFFY = "offsety";
		/// <summary>
		/// Zone width INI field
		/// </summary>
		private const string ENTRY_ZONE_WIDTH = "width";
		/// <summary>
		/// Zone height INI field
		/// </summary>
		private const string ENTRY_ZONE_HEIGHT = "height";
		/// <summary>
		/// Zone water level INI field
		/// </summary>
		private const string ENTRY_ZONE_WATER_LEVEL = "waterlevel";
		
		/// <summary>
		/// Does this zone contain Lava
		/// </summary>
		private const string ENTRY_ZONE_LAVA = "IsLava";

		/// <summary>
		/// Initializes the most important things that is needed for some code
		/// </summary>
		/// <param name="regionsData">The loaded regions data</param>
		public static bool EarlyInit(out RegionData[] regionsData)
		{
			log.Debug(GC.GetTotalMemory(true) / 1024 / 1024 + "MB - World Manager: EarlyInit");

			m_regions.Clear();
			m_zones.Clear();

			#region Instances

			//Dinberg: We now need to save regionData, indexed by regionID, for instances.
			//The information generated here is oddly ordered by number of mbos in the region,
			//so I'm contriving to generate this list myself.
			m_regionData = new Dictionary<ushort, RegionData>();

			//We also will need to store zones, because we need at least one zone per region - hence
			//we will create zones inside our instances or the player gets banned by anti-telehack scripts.
			m_zonesData = new Dictionary<ushort, List<ZoneData>>();

			#endregion

			log.Info(LoadTeleports());

			// sort the regions by mob count

			log.Debug("loading mobs from DB...");

			var mobList = new List<DbMob>();
			if (ServerProperties.Properties.DEBUG_LOAD_REGIONS != string.Empty)
			{
				foreach (string loadRegion in Util.SplitCSV(ServerProperties.Properties.DEBUG_LOAD_REGIONS, true))
				{
					mobList.AddRange(DOLDB<DbMob>.SelectObjects(DB.Column("Region").IsEqualTo(loadRegion)));
				}
			}
			else
			{
				mobList.AddRange(GameServer.Database.SelectAllObjects<DbMob>());
			}

			var mobsByRegionId = new Dictionary<ushort, List<DbMob>>(512);
			foreach (DbMob mob in mobList)
			{
				List<DbMob> list;

				if (!mobsByRegionId.TryGetValue(mob.Region, out list))
				{
					list = new List<DbMob>(1024);
					mobsByRegionId.Add(mob.Region, list);
				}

				list.Add(mob);
			}

			bool hasFrontierRegion = false;

			var regions = new List<RegionData>(512);
			foreach (DbRegion dbRegion in GameServer.Database.SelectAllObjects<DbRegion>())
			{
				var data = new RegionData();

				data.Id = dbRegion.RegionID;
				data.Name = dbRegion.Name;
				data.Description = dbRegion.Description;
				data.Ip = dbRegion.IP;
				data.Port = dbRegion.Port;
				data.Expansion = dbRegion.Expansion;
				data.HousingEnabled = dbRegion.HousingEnabled;
				data.DivingEnabled = dbRegion.DivingEnabled;
				data.WaterLevel = dbRegion.WaterLevel;
				data.ClassType = dbRegion.ClassType;
				data.IsFrontier = dbRegion.IsFrontier;

				hasFrontierRegion |= data.IsFrontier;

				List<DbMob> mobs;

				if (!mobsByRegionId.TryGetValue(data.Id, out mobs))
					data.Mobs = new DbMob[0];
				else
					data.Mobs = mobs.ToArray();

				regions.Add(data);

				//Dinberg - save the data by ID.
				if (m_regionData.ContainsKey(data.Id))
					log.ErrorFormat("Duplicate key in region table - {0}, EarlyInit in WorldMgr failed.", data.Id);
				else
					m_regionData.Add(data.Id, data);
			}

			regions.Sort();

			log.DebugFormat("{0}MB - Region Data Loaded", GC.GetTotalMemory(true) / 1024 / 1024);

			for (int i = 0; i < regions.Count; i++)
			{
				var region = regions[i];
				RegisterRegion(region);
			}

			log.DebugFormat("{0}MB - {1} Regions Loaded", GC.GetTotalMemory(true) / 1024 / 1024, m_regions.Count);

			// if we don't have at least one frontier region add the default
			if (hasFrontierRegion == false)
			{
				Region frontier;
				if (m_regions.TryGetValue(Keeps.DefaultKeepManager.DEFAULT_FRONTIERS_REGION, out frontier))
				{
					frontier.IsFrontier = true;
				}
				else
				{
					log.ErrorFormat("Can't find default Frontier region {0}!", Keeps.DefaultKeepManager.DEFAULT_FRONTIERS_REGION);
				}
			}

			foreach (DbZone dbZone in GameServer.Database.SelectAllObjects<DbZone>())
			{
				ZoneData zoneData = new ZoneData();
				zoneData.Height = (byte)dbZone.Height;
				zoneData.Width = (byte)dbZone.Width;
				zoneData.OffY = (byte)dbZone.OffsetY;
				zoneData.OffX = (byte)dbZone.OffsetX;
				zoneData.Description = dbZone.Name;
				zoneData.RegionID = dbZone.RegionID;
				zoneData.ZoneID = (ushort)dbZone.ZoneID;
				zoneData.WaterLevel = dbZone.WaterLevel;
				zoneData.DivingFlag = dbZone.DivingFlag;
				zoneData.IsLava = dbZone.IsLava;
				RegisterZone(zoneData, zoneData.ZoneID, zoneData.RegionID, zoneData.Description,
							 dbZone.Experience, dbZone.Realmpoints, dbZone.Bountypoints, dbZone.Coin, dbZone.Realm);

				//Save the zonedata.
				if (!m_zonesData.ContainsKey(zoneData.RegionID))
					m_zonesData.Add(zoneData.RegionID, new List<ZoneData>());

				m_zonesData[zoneData.RegionID].Add(zoneData);
			}


			log.DebugFormat("{0}MB - Zones Loaded for All Regions", GC.GetTotalMemory(true) / 1024 / 1024);

			regionsData = regions.ToArray();
			return true;
		}


		/// <summary>
		/// Load available teleport locations.
		/// </summary>
		public static string LoadTeleports()
		{
			var objs = GameServer.Database.SelectAllObjects<DbTeleport>();
			m_teleportLocations = new Dictionary<eRealm, Dictionary<string, DbTeleport>>();
			int[] numTeleports = new int[3];
			foreach (DbTeleport teleport in objs)
			{
				Dictionary<string, DbTeleport> teleportList;
				if (m_teleportLocations.ContainsKey((eRealm)teleport.Realm))
					teleportList = m_teleportLocations[(eRealm)teleport.Realm];
				else
				{
					teleportList = new Dictionary<string, DbTeleport>();
					m_teleportLocations.Add((eRealm)teleport.Realm, teleportList);
				}
				String teleportKey = String.Format("{0}:{1}", teleport.Type, teleport.TeleportID);
				if (teleportList.ContainsKey(teleportKey))
				{
					log.Error("WorldMgr.EarlyInit teleporters - Cannot add " + teleportKey + " already exists");
					continue;
				}
				teleportList.Add(teleportKey, teleport);
				if (teleport.Realm >= 1 && teleport.Realm <= 3)
					numTeleports[teleport.Realm - 1]++;
			}

			objs = null;

			return String.Format("Loaded {0} Albion, {1} Midgard and {2} Hibernia teleport locations", numTeleports[0], numTeleports[1], numTeleports[2]);
		}


		/// <summary>
		/// Initializes the WorldMgr. This function must be called
		/// before the WorldMgr can be used!
		/// </summary>
		public static bool Init(RegionData[] regionsData)
		{
			try
			{
				LootMgr.Init();

				long mobs = 0;
				long merchants = 0;
				long items = 0;
				long bindpoints = 0;
				regionsData.AsParallel().WithDegreeOfParallelism(GameServer.Instance.Configuration.CPUUse << 2).ForAll(data => {
				                                	Region reg;
				                                	if (m_regions.TryGetValue(data.Id, out reg))
				                                		reg.LoadFromDatabase(data.Mobs, ref mobs, ref merchants, ref items, ref bindpoints);
				});

				if (log.IsInfoEnabled)
				{
					log.Info("Total Mobs: " + mobs);
					log.Info("Total Merchants: " + merchants);
					log.Info("Total Items: " + items);
					log.Info("Total Bind Points: " + bindpoints);
				}

				m_dayIncrement = Math.Max(0, Math.Min(1000, ServerProperties.Properties.WORLD_DAY_INCREMENT)); // increments > 1000 do not render smoothly on clients
				m_dayStartTick = (int) (GameLoop.GameLoopTime - DAY / Math.Max(1, m_dayIncrement) / 2); // set start time to 12pm
				m_dayResetTimer = new Timer(new TimerCallback(DayReset), null, DAY / Math.Max(1, m_dayIncrement) / 2, DAY / Math.Max(1, m_dayIncrement));
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("Init", e);
				return false;
			}
			return true;
		}

#if NETFRAMEWORK
		[Obsolete("Please use GetFormattedRelocateRegionsStackTrace() instead.")]
		public static StackTrace GetRelocateRegionsStacktrace()
		{
			return Util.GetThreadStack(m_relocationThread);
		}
#endif

		/// <summary>
		/// This timer callback resets the day on all clients
		/// </summary>
		/// <param name="sender"></param>
		private static void DayReset(object sender)
		{
			m_dayStartTick = (int) GameLoop.GameLoopTime;

			foreach (GamePlayer player in ClientService.GetPlayers<object>(Predicate))
				player.Out.SendTime();

			static bool Predicate(GamePlayer player, object unused)
			{
				return player.CurrentRegion?.UseTimeManager == true;
			}
		}

		/// <summary>
		/// Starts a new day with a certain percent of the increment
		/// </summary>
		/// <param name="dayInc"></param>
		/// <param name="percent">0..1</param>
		public static void StartDay( uint dayInc, double percent )
		{
			uint dayStart = (uint)( percent * DAY );
			m_dayIncrement = dayInc;

			if (m_dayIncrement == 0)
			{
				// day should stand still so pause the timer
				m_dayStartTick = (int)(dayStart);
				m_dayResetTimer.Change(Timeout.Infinite, Timeout.Infinite);
			}
			else
			{
				m_dayStartTick = (int) (GameLoop.GameLoopTime - dayStart / m_dayIncrement); // set start time to ...
				m_dayResetTimer.Change((DAY - dayStart) / m_dayIncrement, Timeout.Infinite);
			}

			foreach (GamePlayer player in ClientService.GetPlayers<object>(Predicate))
				player.Out.SendTime();

			static bool Predicate(GamePlayer player, object unused)
			{
				return player.CurrentRegion?.UseTimeManager == true;
			}
		}

		/// <summary>
		/// Gets the game time for a players current region
		/// </summary>
		/// <param name="client"></param>
		/// <returns></returns>
		public static uint GetCurrentGameTime(GamePlayer player)
		{
			if (player.CurrentRegion != null)
				return player.CurrentRegion.GameTime;

			return GetCurrentGameTime();
		}

		/// <summary>
		/// Gets the current game time
		/// </summary>
		/// <returns>current time</returns>
		public static uint GetCurrentGameTime()
		{
			if (m_dayIncrement == 0)
			{
				return (uint)m_dayStartTick;
			}
			else
			{
				long diff = GameLoop.GameLoopTime - m_dayStartTick;
				long curTime = diff * m_dayIncrement;
				return (uint)(curTime % DAY);
			}
		}

		/// <summary>
		/// Returns the day increment
		/// </summary>
		/// <returns>the day increment</returns>
		public static uint GetDayIncrement(GamePlayer player)
		{
			if (player.CurrentRegion != null)
				return player.CurrentRegion.DayIncrement;

			return GetDayIncrement();
		}


		public static uint GetDayIncrement()
		{
			return m_dayIncrement;
		}

#if NETFRAMEWORK
		[Obsolete("Use GetFormattedWorldUpdateStackTrace() instead.")]
		public static StackTrace GetWorldUpdateStacktrace()
		{
			return Util.GetThreadStack(m_WorldUpdateThread);
		}
#endif

		/// <summary>
		/// Cleans up and stops all the RegionMgr tasks inside
		/// the regions.
		/// </summary>
		public static void Exit()
		{
			try
			{
				if (m_dayResetTimer != null)
				{
					m_dayResetTimer.Dispose();
					m_dayResetTimer = null;
				}

				//Stop all mobMgrs
				StopRegionMgrs();
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("Exit", e);
			}
		}

		/// <summary>
		/// Creates and adds a new region to the WorldMgr
		/// </summary>
		/// <param name="time">Time manager for the region</param>
		/// <param name="data">The region data</param>
		/// <returns>Registered region</returns>
		public static Region RegisterRegion(RegionData data)
		{
			Region region =  Region.Create(data);
			return m_regions.TryAdd(data.Id, region) ? region : null;
		}

		/// <summary>
		/// Creates an array of region entries
		/// </summary>
		/// <returns>An array of regions available on the server</returns>
		public static RegionEntry[] GetRegionList()
		{
			string ip = GameServer.Instance.Configuration.RegionIP.ToString();
			string port = string.Format("{0:D00000}", GameServer.Instance.Configuration.RegionPort);
			return m_regions.Select(r => new RegionEntry()
			                        {
			                        	id = r.Value.ID,
			                        	ip = ip,
			                        	toPort = port,
			                        	name = r.Value.Name,
			                        	fromPort = port,
			                        	expansion = r.Value.Expansion,
			                        }).ToArray();
		}

		/// <summary>
		/// Returns all the regions of the world
		/// </summary>
		/// <returns></returns>
		public static ICollection<Region> GetAllRegions()
		{
			return m_regions.Values;
		}

		/// <summary>
		/// Registers a Zone into a Region
		/// </summary>
		public static void RegisterZone(ZoneData zoneData, ushort zoneID, ushort regionID, string zoneName, int xpBonus, int rpBonus, int bpBonus, int coinBonus, byte realm)
		{
			Region region = GetRegion(regionID);
			if (region == null)
			{
				log.Warn($"Could not find Region {regionID} for Zone {zoneData.Description}");
				return;
			}
			
			// Making an assumption that a zone waterlevel of 0 means it is not set and we should use the regions waterlevel - Tolakram
			if (zoneData.WaterLevel == 0)
			{
				zoneData.WaterLevel = region.WaterLevel;
			}

			bool isDivingEnabled = region.IsRegionDivingEnabled;

			if (zoneData.DivingFlag == 1)
				isDivingEnabled = true;
			else if (zoneData.DivingFlag == 2)
				isDivingEnabled = false;
			
			Zone zone = new Zone(region,
			                     zoneID,
			                     zoneName,
			                     zoneData.OffX * 8192,
			                     zoneData.OffY * 8192,
			                     zoneData.Width * 8192,
			                     zoneData.Height * 8192,
			                     zoneData.ZoneID,
			                     isDivingEnabled,
			                     zoneData.WaterLevel,
			                     zoneData.IsLava,
			                     xpBonus,
			                     rpBonus,
			                     bpBonus,
			                     coinBonus,
			                     realm);

			//Dinberg:Instances
			//ZoneID will always be constant as last parameter, because ZoneSkinID will effectively be a bluff, to remember
			//the original region that spawned this one!

			/*reg,
                    zoneID,
                    desc,
                    offx * 8192,
                    offy * 8192,
                    width * 8192,
                    height * 8192);*/

			region.Zones.Add(zone);
			m_zones[zoneID] = zone;
			log.InfoFormat("Added a zone, {0}, to region {1}", zoneData.Description, region.Name);
		}

		/// <summary>
		/// Starts all RegionMgrs inside the Regions
		/// </summary>
		public static bool StartRegionMgrs()
		{
			foreach (Region region in m_regions.Values)
				region.StartRegionMgr();

			return true;
		}

		/// <summary>
		/// Stops all Regionmgrs inside the Regions
		/// </summary>
		public static void StopRegionMgrs()
		{
			if (log.IsDebugEnabled)
				log.Debug("Stopping region managers...");
			
			foreach (Region region in m_regions.Values)
				region.StopRegionMgr();

			if (log.IsDebugEnabled)
				log.Debug("Region managers stopped.");
		}

		/// <summary>
		/// Fetch a Region by it's ID
		/// </summary>
		/// <param name="regionID">ID to search</param>
		/// <returns>Region or null if not found</returns>
		public static Region GetRegion(ushort regionID)
		{
			Region reg;
			if (m_regions.TryGetValue(regionID, out reg))
				return reg;
			
			return null;
		}

		public static ushort m_lastZoneError = 0;

		/// <summary>
		/// Gets a Zone object by it's ID
		/// </summary>
		/// <param name="zoneID">the zoneID</param>
		/// <returns>the zone object or null</returns>
		public static Zone GetZone(ushort zoneID)
		{
			Zone z;
			if (m_zones.TryGetValue(zoneID, out z))
				return z;
			
			if (m_lastZoneError != zoneID)
			{
				log.ErrorFormat("Trying to access inexistent ZoneID {0} {1}", zoneID, Environment.StackTrace);
				m_lastZoneError = zoneID;
			}

			return null;
		}

		public static object[] OfTypeAndToArray<T>(this IEnumerable<T> input, Type type)
		{
			MethodInfo methodOfType = typeof(Enumerable).GetMethod("OfType");
			MethodInfo genericOfType = methodOfType.MakeGenericMethod(new Type[]{ type });
			// Use .NET 4 covariance
			var result = (IEnumerable<object>) genericOfType.Invoke(null, new object[] { input });
			
			MethodInfo methodToArray = typeof(Enumerable).GetMethod("ToArray");
			MethodInfo genericToArray = methodToArray.MakeGenericMethod(new Type[]{ type });
			
			return (object[]) genericToArray.Invoke(null, new object[] { result });
		}

		/// <summary>
		/// Searches for all objects from a specific region
		/// </summary>
		/// <param name="regionID">The region to search</param>
		/// <param name="objectType">The type of the object you search</param>
		/// <returns>All objects with the specified parameters</returns>
		public static GameObject[] GetobjectsFromRegion(ushort regionID, Type objectType)
		{
			Region reg;
			if (!m_regions.TryGetValue(regionID, out reg))
				return new GameObject[0];

			return (GameObject[]) reg.Objects.Where(obj => obj != null).OfTypeAndToArray(objectType);
		}
		
		/// <summary>
		/// Searches for all GameStaticItem from a specific region
		/// </summary>
		/// <param name="regionID">The region to search</param>
		/// <returns>All NPCs with the specified parameters</returns>
		public static GameStaticItem[] GetStaticItemFromRegion(ushort regionID)
		{
			return (GameStaticItem[])GetobjectsFromRegion(regionID, typeof(GameStaticItem));
		}

		/// <summary>
		/// Searches for all objects with the given name, from a specific region and realm
		/// </summary>
		/// <param name="name">The name of the object to search</param>
		/// <param name="regionID">The region to search</param>
		/// <param name="realm">The realm of the object we search!</param>
		/// <param name="objectType">The type of the object you search</param>
		/// <returns>All objects with the specified parameters</returns>
		public static GameObject[] GetObjectsByNameFromRegion(string name, ushort regionID, eRealm realm, Type objectType)
		{
			Region reg;
			if (!m_regions.TryGetValue(regionID, out reg))
				return new GameObject[0];
			
			return (GameObject[]) reg.Objects.Where(obj => obj != null && obj.Realm == realm && obj.Name == name).OfTypeAndToArray(objectType);
		}

		/// <summary>
		/// Returns the npcs in a given region
		/// </summary>
		/// <returns></returns>
		public static GameNPC[] GetNPCsFromRegion(ushort regionID)
		{
			Region reg;
			if (!m_regions.TryGetValue(regionID, out reg))
				return new GameNPC[0];

			return reg.Objects.OfType<GameNPC>().ToArray();
		}

		/// <summary>
		/// Searches for all objects with the given name and realm in ALL regions!
		/// </summary>
		/// <param name="name">The name of the object to search</param>
		/// <param name="realm">The realm of the object we search!</param>
		/// <param name="objectType">The type of the object you search</param>
		/// <returns>All objects with the specified parameters</returns>b
		public static GameObject[] GetObjectsByName(string name, eRealm realm, Type objectType)
		{
			return (GameObject[]) m_regions.Values.Select(reg => GetObjectsByNameFromRegion(name, reg.ID, realm, objectType))
				.SelectMany(objs => objs).OfTypeAndToArray(objectType);
		}

		/// <summary>
		/// Searches for all NPCs with the given name, from a specific region and realm
		/// </summary>
		/// <param name="name">The name of the object to search</param>
		/// <param name="regionID">The region to search</param>
		/// <param name="realm">The realm of the object we search!</param>
		/// <returns>All NPCs with the specified parameters</returns>
		public static GameNPC[] GetNPCsByNameFromRegion(string name, ushort regionID, eRealm realm)
		{
			return (GameNPC[])GetObjectsByNameFromRegion(name, regionID, realm, typeof(GameNPC));
		}

		/// <summary>
		/// Searches for all NPCs with the given name and realm in ALL regions!
		/// </summary>
		/// <param name="name">The name of the object to search</param>
		/// <param name="realm">The realm of the object we search!</param>
		/// <returns>All NPCs with the specified parameters</returns>b
		public static GameNPC[] GetNPCsByName(string name, eRealm realm)
		{
			return (GameNPC[])GetObjectsByName(name, realm, typeof(GameNPC));
		}

		/// <summary>
		/// Searches for all NPCs with the given guild and realm in ALL regions!
		/// </summary>
		/// <param name="guild">The guild name for the npc</param>
		/// <param name="realm">The realm of the npc</param>
		/// <returns>A collection of NPCs which match the result</returns>
		public static List<GameNPC> GetNPCsByGuild(string guild, eRealm realm)
		{
			return m_regions.Values.Select(r => r.Objects.OfType<GameNPC>().Where(npc => npc.Realm == realm && npc.GuildName == guild))
				.SelectMany(objs => objs).ToList();
		}

		/// <summary>
		/// Searches for all NPCs with the given type and realm in ALL regions!
		/// </summary>
		/// <param name="type"></param>
		/// <param name="realm"></param>
		/// <returns></returns>
		public static List<GameNPC> GetNPCsByType(Type type, eRealm realm)
		{
			return m_regions.Values.Select(r => r.Objects.OfType<GameNPC>().Where(npc => npc.Realm == realm && type.IsInstanceOfType(npc)))
				.SelectMany(objs => objs).ToList();
		}

		/// <summary>
		/// Searches for all NPCs with the given type and realm in a specific region
		/// </summary>
		/// <param name="type"></param>
		/// <param name="realm"></param>
		/// <param name="region"></param>
		/// <returns></returns>
		public static List<GameNPC> GetNPCsByType(Type type, eRealm realm, ushort region)
		{
			Region reg;
			if (!m_regions.TryGetValue(region, out reg))
				return new List<GameNPC>(0);
			
			return reg.Objects.OfType<GameNPC>().Where(npc => npc.Realm == realm && type.IsInstanceOfType(npc)).ToList();
		}

		/// <summary>
		/// Fetch an Object from a specific Region by it's ID
		/// </summary>
		/// <param name="regionID">Region ID of Region to search through</param>
		/// <param name="oID">Object ID to search</param>
		/// <returns>GameObject found in the Region or null</returns>
		public static GameObject GetObjectByIDFromRegion(ushort regionID, ushort oID)
		{
			Region reg = GetRegion(regionID);
			if (reg == null)
				return null;
			return reg.GetObject(oID);
		}

		/// <summary>
		/// Fetch an Object of specific type from a specific Region
		/// </summary>
		/// <param name="regionID">Region ID of Regin to search through</param>
		/// <param name="oID">Object ID to search</param>
		/// <param name="type">Type of Object to search</param>
		/// <returns>GameObject of specific type or null if not found</returns>
		public static GameObject GetObjectTypeByIDFromRegion(ushort regionID, ushort oID, Type type)
		{
			GameObject obj = GetObjectByIDFromRegion(regionID, oID);
			if (obj == null || !type.IsInstanceOfType(obj))
				return null;
			return obj;
		}

		public static List<GamePlayer> GetPlayersCloseToSpot(ushort regionid, int x, int y, int z, ushort radiusToCheck)
		{
			return GetPlayersCloseToSpot(regionid, new Point3D(x, y ,z), radiusToCheck);
		}

		public static List<GamePlayer> GetPlayersCloseToSpot(IGameLocation location, ushort radiusToCheck)
		{
			return GetPlayersCloseToSpot(location.RegionID, location.X, location.Y, location.Z, radiusToCheck);
		}

		public static List<GamePlayer> GetPlayersCloseToSpot(ushort regionid, Point3D point, ushort radiusToCheck)
		{
			Region reg = GetRegion(regionid);

			if (reg == null)
				return new();

			return reg.GetPlayersInRadius(point, radiusToCheck);
		}

		public static List<GameNPC> GetNPCsCloseToSpot(ushort regionid, int x, int y, int z, ushort radiusToCheck)
		{
			return GetNPCsCloseToSpot(regionid, new Point3D( x, y, z), radiusToCheck);
		}

		public static List<GameNPC> GetNPCsCloseToSpot(ushort regionid, Point3D point, ushort radiusToCheck)
		{
			Region reg = GetRegion(regionid);

			if (reg == null)
				return new();

			return reg.GetNPCsInRadius(point, radiusToCheck);
		}

		public static List<GameStaticItem> GetItemsCloseToSpot(ushort regionid, int x, int y, int z, ushort radiusToCheck)
		{
			Region reg = GetRegion(regionid);

			if (reg == null)
				return new();

			return reg.GetItemsInRadius(new Point3D(x, y ,z), radiusToCheck);
		}

		#region Instances

		//Dinberg: We must now store the region data here. This is incase admins wish to create instances
		//that require information from regions Data, like instance of underwater ToA areas as a prime
		//example!
		/// <summary>
		/// Stores the region Data parsed from the regions xml file.
		/// </summary>
		private static Dictionary<ushort, RegionData> m_regionData;

		public static IDictionary<ushort, RegionData> RegionData
		{
			get { return m_regionData; }
		}

		/// <summary>
		/// Stores the zone data parsed from the zones file by RegionID.
		/// </summary>
		private static Dictionary<ushort, List<ZoneData>> m_zonesData;

		public static Dictionary<ushort, List<ZoneData>> ZonesData
		{
			get { return m_zonesData; }
		}


		/// <summary>
		/// Creates a new instance, with the given 'skin' (the regionID to display client side).
		/// </summary>
		/// <param name="skinID"></param>
		/// <returns></returns>
		public static BaseInstance CreateInstance(ushort skinID, Type instanceType)
		{
			return CreateInstance(0, skinID, instanceType);
		}

		/// <summary>
		/// Where do we start looking for an instance id from if none is requested?
		/// </summary>
		public const int DEFAULT_VALUE_FOR_INSTANCE_ID_SEARCH_START = 1000;


		/// <summary>
		/// Tries to create an instance with the suggested ID and a given 'skin' (the regionID to display client side).
		/// </summary>
		public static BaseInstance CreateInstance(ushort requestedID, ushort skinID, Type instanceType)
		{
			if ((instanceType.IsSubclassOf(typeof(BaseInstance)) || instanceType == typeof(BaseInstance)) == false)
			{
				log.Error("Invalid type given for instance creation: " + instanceType + ". Returning null instance now.");
				return null;
			}

			BaseInstance instance = null;
			RegionData data = m_regionData[skinID];

			if (data == null)
			{
				log.Error("Data for region " + skinID + " not found on instance create!");
				return null;
			}

			ConstructorInfo info = instanceType.GetConstructor(new Type[] { typeof(ushort), typeof(RegionData)});

			if (info == null)
			{
				log.Error("Classtype " + instanceType + " did not have a cosntructor that matched the requirement!");
				return null;
			}

			bool RequestedAnID = requestedID == 0 ? false : true;
			ushort ID = requestedID;
			bool success = false;

			if (RequestedAnID)
				success = m_regions.TryAdd(ID, instance);
			else
			{
				for (ID = DEFAULT_VALUE_FOR_INSTANCE_ID_SEARCH_START; ID <= ushort.MaxValue; ID++)
				{
					if (!m_regions.ContainsKey(ID))
					{
						success = true;
						break;
					}
				}
			}

			if (!success)
			{
				log.Error($"Failed to add new instance to region table (ID: {ID})");
				return null;
			}

			try
			{
				instance = (BaseInstance) info.Invoke(new object[] { ID, data });
				m_regions[ID] = instance;
			}
			catch (Exception e)
			{
				log.ErrorFormat("Error on instance creation - {0} {1}", e.Message, e.StackTrace);
				return null;
			}

			List<ZoneData> list = null;

			if (m_zonesData.ContainsKey(data.Id))
				list = m_zonesData[data.Id];

			if (list == null)
			{
				log.Warn("No zones found for given skinID on instance creation, " + skinID);
				return null;
			}

			ushort zoneID = 0;

			foreach (ZoneData dat in list)
			{
				for (; zoneID <= ushort.MaxValue; zoneID++)
				{
					if (m_zones.TryAdd(zoneID, null))
					{
						RegisterZone(dat, zoneID, ID, string.Format("{0} (Instance)", dat.Description), 0, 0, 0, 0, 0);
						break;
					}
				}
			}

			instance.Start();
			return instance;
		}

		/// <summary>
		/// Removes the given instance from the server.
		/// </summary>
		public static void RemoveInstance(BaseInstance instance)
		{
			m_regions.TryRemove(instance.ID, out _);

			foreach (Zone zn in instance.Zones)
				m_zones.TryRemove(zn.ID, out _);

			instance.OnCollapse();
		}

		#endregion
	}
}
