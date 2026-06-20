using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// the LootMgr holds pointers to all LootGenerators at 
	/// associates the correct LootGenerator with a given Mob
	/// </summary>
	public sealed class LootMgr
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Map holding one generator for each different class, to reuse similar generators...
		/// </summary>
		static readonly Dictionary<string, ILootGenerator> m_ClassGenerators = new();

		/// <summary>
		/// List of global Lootgenerators 
		/// </summary>
		static readonly List<ILootGenerator> m_globalGenerators = new();

		/// <summary>
		/// List of Lootgenerators related by mobname
		/// </summary>
		static readonly Dictionary<string, List<ILootGenerator>> m_mobNameGenerators = new();

		/// <summary>
		/// List of Lootgenerators related by mobguild
		/// </summary>
		static readonly Dictionary<string, List<ILootGenerator>> m_mobGuildGenerators = new();

		/// <summary>
		/// List of Lootgenerators related by region ID
		/// </summary>
		static readonly Dictionary<int, List<ILootGenerator>> m_mobRegionGenerators = new();

		/// <summary>
		/// List of Lootgenerators related by mobfaction
		/// </summary>
		static readonly Dictionary<int, List<ILootGenerator>> m_mobFactionGenerators = new();

		/// <summary>
		/// Initializes the LootMgr. This function must be called
		/// before the LootMgr can be used!
		/// </summary>
		public static bool Init()
		{
			if (log.IsInfoEnabled)
				log.Info("Loading LootGenerators...");

			List<DbLootGenerator> m_lootGenerators;
			try
			{
				m_lootGenerators = GameServer.Database.SelectAllObjects<DbLootGenerator>();
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.Error("LootMgr: LootGenerators could not be loaded", e);
				return false;
			}

			if (m_lootGenerators != null) // did we find any loot generators
			{
				foreach (DbLootGenerator dbGenerator in m_lootGenerators)
				{
					ILootGenerator generator = GetGeneratorInCache(dbGenerator);
					if (generator == null)
					{
						Type generatorType = null;
						foreach (Assembly asm in ScriptMgr.Scripts)
						{
							generatorType = asm.GetType(dbGenerator.LootGeneratorClass);
							if (generatorType != null)
								break;
						}
						if (generatorType == null)
						{
							generatorType = Assembly.GetAssembly(typeof(GameServer)).GetType(dbGenerator.LootGeneratorClass);
						}

						if (generatorType == null)
						{
							if (log.IsErrorEnabled)
								log.Error("Could not find LootGenerator: " + dbGenerator.LootGeneratorClass + "!!!");
							continue;
						}
						generator = (ILootGenerator)Activator.CreateInstance(generatorType);

						PutGeneratorInCache(dbGenerator, generator);
					}
					RegisterLootGenerator(generator, dbGenerator.MobName, dbGenerator.MobGuild, dbGenerator.MobFaction, dbGenerator.RegionID);
				}
			}
			if (log.IsDebugEnabled)
			{
				log.Debug("Found " + m_globalGenerators.Count + " Global LootGenerators");
				log.Debug("Found " + m_mobNameGenerators.Count + " Mobnames registered by LootGenerators");
				log.Debug("Found " + m_mobGuildGenerators.Count + " Guildnames registered by LootGenerators");
			}

			// no loot generators loaded...
			if (m_globalGenerators.Count == 0 && m_mobNameGenerators.Count == 0 && m_globalGenerators.Count == 0)
			{
				ILootGenerator baseGenerator = new LootGeneratorMoney();
				RegisterLootGenerator(baseGenerator, null, null, null, 0);
				if (log.IsInfoEnabled)
					log.Info("No LootGenerator found, adding LootGeneratorMoney for all mobs as default.");
			}

			if (log.IsInfoEnabled)
				log.Info("LootGenerator initialized: true");
			return true;
		}

		/// <summary>
		/// Stores a generator in a cache to reused the same generators multiple times
		/// </summary>
		/// <param name="dbGenerator"></param>
		/// <param name="generator"></param>
		private static void PutGeneratorInCache(DbLootGenerator dbGenerator, ILootGenerator generator)
		{
			m_ClassGenerators[dbGenerator.LootGeneratorClass + dbGenerator.ExclusivePriority] = generator;
		}

		/// <summary>
		///  Returns a generator from cache
		/// </summary>
		/// <param name="dbGenerator"></param>
		/// <returns></returns>
		private static ILootGenerator GetGeneratorInCache(DbLootGenerator dbGenerator)
		{
			if (m_ClassGenerators.TryGetValue(dbGenerator.LootGeneratorClass + dbGenerator.ExclusivePriority, out ILootGenerator generator))
			{
				return generator;
			}
			return null;
		}

		public static void UnRegisterLootGenerator(ILootGenerator generator, string mobname, string mobguild, string mobfaction)
		{
			UnRegisterLootGenerator(generator, mobname, mobguild, mobfaction, 0);
		}

		/// <summary>
		/// Unregister a generator for the given parameters		
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="mobname"></param>
		/// <param name="mobguild"></param>
		/// <param name="mobfaction"></param>
		public static void UnRegisterLootGenerator(ILootGenerator generator, string mobname, string mobguild, string mobfaction, int mobregion)
		{
			if (generator == null)
				return;

			// Loot Generator Name Indexed
			if (!string.IsNullOrEmpty(mobname))
			{	
				
				try 
				{
					// Parse CSV
					List<string> mobNames = Util.SplitCSV(mobname);
					
					foreach(string mob in mobNames) 
					{
						if (m_mobNameGenerators.TryGetValue(mob, out var list))
						{
							list.Remove(generator);
						}
					}

				}
				catch 
				{
					if (log.IsDebugEnabled)
					{
						log.Debug("Could not Parse mobNames for Removing LootGenerator : " + generator.GetType().FullName);
					}
				}
			}

			// Loot Generator Guild Indexed
			if (!string.IsNullOrEmpty(mobguild))
			{
				
				try 
				{
					// Parse CSV
					List<string> mobGuilds = Util.SplitCSV(mobguild);
					
					foreach(string guild in mobGuilds) 
					{
						if (m_mobGuildGenerators.TryGetValue(guild, out var list))
						{
							list.Remove(generator);
						}
					}

				}
				catch 
				{
					if (log.IsDebugEnabled)
					{
						log.Debug("Could not Parse mobGuilds for Removing LootGenerator : " + generator.GetType().FullName);
					}
				}
			}

			// Loot Generator Faction Indexed
			if (!string.IsNullOrEmpty(mobfaction))
			{

				try
				{
					// Parse CSV
					List<string> mobFactions = Util.SplitCSV(mobfaction);

					foreach (string sfaction in mobFactions)
					{
						try
						{
							int ifaction = int.Parse(sfaction);

							if (m_mobFactionGenerators.TryGetValue(ifaction, out var list))
								list.Remove(generator);
						}
						catch
						{
							if (log.IsDebugEnabled)
								log.Debug("Could not parse faction [" + sfaction + "] into an integer.");
						}
					}

				}
				catch
				{
					if (log.IsDebugEnabled)
					{
						log.Debug("Could not Parse mobFactions for Removing LootGenerator : " + generator.GetType().FullName);
					}
				}
			}

			// Loot Generator Region Indexed
			if (mobregion > 0)
			{
				if (m_mobRegionGenerators.TryGetValue(mobregion, out var regionList))
				{
					regionList.Remove(generator);
				}
			}

			if (string.IsNullOrEmpty(mobname) && string.IsNullOrEmpty(mobguild) && string.IsNullOrEmpty(mobfaction) && mobregion == 0)
			{
				m_globalGenerators.Remove(generator);
			}
		}

		/// <summary>
		/// Register a generator for the given parameters,
		/// If all parameters are null a global generaotr for all mobs will be registered
		/// </summary>
		/// <param name="generator"></param>
		/// <param name="mobname"></param>
		/// <param name="mobguild"></param>
		/// <param name="mobfaction"></param>
		/// <param name="mobregion"></param>
		public static void RegisterLootGenerator(ILootGenerator generator, string mobname, string mobguild, string mobfaction, int mobregion)
		{
			if (generator == null)
				return;

			// Loot Generator Name Indexed
			if (!string.IsNullOrEmpty(mobname))
			{
				// Parse CSV
				try {
					List<string> mobNames = Util.SplitCSV(mobname);
					
					foreach(string mob in mobNames) 
					{
						if (!m_mobNameGenerators.TryGetValue(mob, out var list)) 
						{
							list = new();
							m_mobNameGenerators[mob] = list;
						}
						list.Add(generator);
					}
				}
				catch 
				{
					if (log.IsDebugEnabled)
					{
						log.Debug("Could not Parse mobNames for Registering LootGenerator : " + generator.GetType().FullName);
					}
				}
			}

			// Loot Generator Guild Indexed
			if (!string.IsNullOrEmpty(mobguild))
			{
				// Parse CSV
				try {
					List<string> mobGuilds = Util.SplitCSV(mobguild);
					
					foreach(string guild in mobGuilds) 
					{
						if (!m_mobGuildGenerators.TryGetValue(guild, out var list))
						{
							list = new();
							m_mobGuildGenerators[guild] = list;
						}
						list.Add(generator);
					}
				}
				catch 
				{
					if (log.IsDebugEnabled)
					{
						log.Debug("Could not Parse mobGuilds for Registering LootGenerator : " + generator.GetType().FullName);
					}
				}
			}

			// Loot Generator Mob Faction Indexed
			if (!string.IsNullOrEmpty(mobfaction))
			{
				// Parse CSV
				try
				{
					List<string> mobFactions = Util.SplitCSV(mobfaction);

					foreach (string sfaction in mobFactions)
					{
						try
						{
							int ifaction = int.Parse(sfaction);

							if (!m_mobFactionGenerators.TryGetValue(ifaction, out var list))
							{
								list = new();
								m_mobFactionGenerators[ifaction] = list;
							}

							list.Add(generator);
						}
						catch
						{
							if (log.IsDebugEnabled)
								log.Debug("Could not parse faction string [" + sfaction + "] into an integer.");
						}
					}// foreach
				}
				catch
				{
					if (log.IsDebugEnabled)
					{
						log.Debug("Could not Parse mobFactions for Registering LootGenerator : " + generator.GetType().FullName);
					}
				}
			}

			// Loot Generator Region Indexed
			if (mobregion > 0)
			{
				if (!m_mobRegionGenerators.TryGetValue(mobregion, out var regionList))
				{
					regionList = new();
					m_mobRegionGenerators[mobregion] = regionList;
				}
				regionList.Add(generator);
			}

			if (string.IsNullOrEmpty(mobname) && string.IsNullOrEmpty(mobguild) && string.IsNullOrEmpty(mobfaction) && mobregion == 0)
			{
				m_globalGenerators.Add(generator);
			}
		}

		/// <summary>
		/// Call the refresh method for each generator to update loot, if implemented
		/// </summary>
		/// <param name="mob"></param>
		public static void RefreshGenerators(GameNPC mob)
		{
			if (mob != null)
			{
				foreach (ILootGenerator gen in m_globalGenerators)
				{
					gen.Refresh(mob);
				}
			}
		}


		/// <summary>
		/// Returns the loot for the given Mob
		/// </summary>		
		/// <param name="mob"></param>
		/// <param name="killer"></param>
		/// <returns></returns>
		public static DbItemTemplate[] GetLoot(GameNPC mob, GameObject killer)
		{
			LootList lootList = null;
			List<ILootGenerator> generators = GetLootGenerators(mob);
			foreach (ILootGenerator generator in generators)
			{
				try
				{
					if (lootList == null)
						lootList = generator.GenerateLoot(mob, killer);
					else
						lootList.AddAll(generator.GenerateLoot(mob, killer));
				}
				catch (Exception e)
				{
					if (log.IsErrorEnabled)
						log.Error("GetLoot", e);
				}
			}
			if (lootList != null)
				return lootList.GetLoot();
			else
				return [];
		}

		/// <summary>
		/// Returns the ILootGenerators for the given mobs
		/// </summary>
		/// <param name="mob"></param>
		/// <returns></returns>
		public static List<ILootGenerator> GetLootGenerators(GameNPC mob)
		{
			List<ILootGenerator> filteredGenerators = new();
			ILootGenerator exclusiveGenerator = null;

			List<ILootGenerator> nameGenerators = null;
			if (mob.Name != null)
				m_mobNameGenerators.TryGetValue(mob.Name, out nameGenerators);

			List<ILootGenerator> guildGenerators = null;
			if (mob.GuildName != null)
				m_mobGuildGenerators.TryGetValue(mob.GuildName, out guildGenerators);

			m_mobRegionGenerators.TryGetValue(mob.CurrentRegionID, out var regionGenerators);

			List<ILootGenerator> factionGenerators = null;
			if (mob.Faction != null)
				m_mobFactionGenerators.TryGetValue(mob.Faction.Id, out factionGenerators);

			List<ILootGenerator> allGenerators = [.. m_globalGenerators];

			if (nameGenerators != null)
				allGenerators.AddRange(nameGenerators);
			if (guildGenerators != null)
				allGenerators.AddRange(guildGenerators);
			if (regionGenerators != null)
				allGenerators.AddRange(regionGenerators);
			if (factionGenerators != null)
				allGenerators.AddRange(factionGenerators);

			foreach (ILootGenerator generator in allGenerators)
			{
				if (generator.ExclusivePriority > 0)
				{
					if (exclusiveGenerator == null || exclusiveGenerator.ExclusivePriority < generator.ExclusivePriority)
						exclusiveGenerator = generator;
				}

				// if we found a exclusive generator skip adding other generators, since list will only contain exclusive generator.
				if (exclusiveGenerator != null)
					continue;

				if (!filteredGenerators.Contains(generator))
					filteredGenerators.Add(generator);
			}

			// if an exclusivegenerator is found only this one is used.
			if (exclusiveGenerator != null)
			{
				filteredGenerators.Clear();
				filteredGenerators.Add(exclusiveGenerator);
			}

			return filteredGenerators;
		}
	}
}
