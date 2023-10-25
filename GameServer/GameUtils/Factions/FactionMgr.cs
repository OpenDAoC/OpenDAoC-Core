using System.Collections;
using System.Reflection;
using Core.Database.Tables;
using log4net;

namespace Core.GS.GameUtils;

public class FactionMgr
{
	private FactionMgr(){}

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
			FactionUtil myfaction = new FactionUtil();
			myfaction.LoadFromDatabase(dbfaction);
			m_factions.Add(dbfaction.ID,myfaction);
		}

		var dblinkedfactions =	GameServer.Database.SelectAllObjects<DbFactionLinks>();
		foreach(DbFactionLinks dblinkedfaction in dblinkedfactions)
		{
			FactionUtil faction = GetFactionByID(dblinkedfaction.LinkedFactionID);
			FactionUtil linkedFaction = GetFactionByID(dblinkedfaction.FactionID);
			if (faction == null || linkedFaction == null) 
			{
				log.Warn("Missing Faction or friend faction with Id :"+dblinkedfaction.LinkedFactionID+"/"+dblinkedfaction.FactionID);
				continue;
			}
			if (dblinkedfaction.IsFriend)
				faction.AddFriendFaction(linkedFaction);
			else
				faction.AddEnemyFaction(linkedFaction);
		}

		var dbfactionAggroLevels =	GameServer.Database.SelectAllObjects<DbFactionAggroLevel>();
		foreach(DbFactionAggroLevel dbfactionAggroLevel in dbfactionAggroLevels)
		{
			FactionUtil faction = GetFactionByID(dbfactionAggroLevel.FactionID);
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
	public static FactionUtil GetFactionByID(int id)
	{
		if (m_factions.ContainsKey(id))
			return m_factions[id] as FactionUtil;
		else
			return null;
	}

	/// <summary>
	/// save all faction aggro level of player who have change faction aggro level
	/// </summary>
	public static void SaveAllAggroToFaction()
	{
		if (m_factions == null) return; // nothing to save yet
		foreach(FactionUtil faction in m_factions.Values)
			faction.SaveFactionAggroToPlayers();
	}
	
	public static bool CanLivingAttack(GameLiving attacker, GameLiving defender)
	{
		// someone who cares about factions should write this
		// TODO Improve this !
		if(attacker == null || defender == null)
			return false;
		
		if(attacker is GameNpc && defender is GameNpc)
			return !(((GameNpc)attacker).IsFriend((GameNpc)defender));
		
		return true;//false;
	}
}