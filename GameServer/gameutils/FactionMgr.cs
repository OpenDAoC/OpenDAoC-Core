using System.Collections;
using System.Reflection;
using DOL.Database;
using log4net;

namespace DOL.GS
{
	/// <summary>
	/// FactionMgr manage all the faction system
	/// </summary>
	public class FactionMgr
	{
		private FactionMgr() {}

		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static Hashtable m_factions;

		public static Hashtable Factions
		{
			get	{ return m_factions;}
		}
		/// <summary>
		/// this function load all faction from DB
		/// </summary>	
		public static bool Init()
		{
			m_factions = new Hashtable(1);

			var dbfactions =	GameServer.Database.SelectAllObjects<DbFaction>();
			foreach(DbFaction dbfaction in dbfactions)
			{
				Faction myfaction = new Faction();
				myfaction.LoadFromDatabase(dbfaction);
				m_factions.Add(dbfaction.ID,myfaction);
			}

			var dblinkedfactions =	GameServer.Database.SelectAllObjects<DbFactionLinks>();
			foreach(DbFactionLinks dblinkedfaction in dblinkedfactions)
			{
				Faction faction = GetFactionByID(dblinkedfaction.LinkedFactionID);
				Faction linkedFaction = GetFactionByID(dblinkedfaction.FactionID);
				if (faction == null || linkedFaction == null) 
				{
					log.Warn("Missing Faction or friend faction with Id :"+dblinkedfaction.LinkedFactionID+"/"+dblinkedfaction.FactionID);
					continue;
				}
				if (dblinkedfaction.IsFriend)
					faction.FriendFactions.Add(linkedFaction);
				else
					faction.EnemyFactions.Add(linkedFaction);
			}

			var dbfactionAggroLevels =	GameServer.Database.SelectAllObjects<DbFactionAggroLevel>();
			foreach(DbFactionAggroLevel dbfactionAggroLevel in dbfactionAggroLevels)
			{
				Faction faction = GetFactionByID(dbfactionAggroLevel.FactionID);
				if (faction == null)
				{
					log.Warn("Missing Faction with Id :"+dbfactionAggroLevel.FactionID);
					continue;
				}
				faction.AggroToPlayers.TryAdd(dbfactionAggroLevel.CharacterID, dbfactionAggroLevel.AggroLevel);
			}
			return true;
		}
		/// <summary>
		/// get the faction with id
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public static Faction GetFactionByID(int id)
		{
			if (m_factions.ContainsKey(id))
				return m_factions[id] as Faction;
			else
				return null;
		}

		/// <summary>
		/// save all faction aggro level of player who have change faction aggro level
		/// </summary>
		public static int SaveAllAggroToFaction()
		{
			if (m_factions == null)
				return 0;

			int count = 0;

			foreach (Faction faction in m_factions.Values)
				count += faction.SaveFactionAggroToPlayers();

			return count;
		}

		public static bool CanLivingAttack(GameLiving attacker, GameLiving defender)
		{
			// someone who cares about factions should write this
			// TODO Improve this !
			if(attacker == null || defender == null)
				return false;
			
			if(attacker is GameNPC && defender is GameNPC)
				return !(((GameNPC)attacker).IsFriend((GameNPC)defender));
			
			return true;//false;
		}
	}
}
