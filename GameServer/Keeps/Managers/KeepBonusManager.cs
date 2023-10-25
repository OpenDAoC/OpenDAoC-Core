using Core.GS.Enums;
using Core.GS.Server;

namespace Core.GS.Keeps;

public class KeepBonusMgr
{
	private static int albCount = 0;
	private static int midCount = 0;
	private static int hibCount = 0;

	/// <summary>
	/// does a realm have the amount of keeps required for a certain bonus
	/// </summary>
	/// <param name="type">the type of bonus</param>
	/// <param name="realm">the realm</param>
	/// <returns>true if the realm has the required amount of keeps</returns>
	public static bool RealmHasBonus(EKeepBonusType type, ERealm realm)
	{
		if (!ServerProperty.USE_LIVE_KEEP_BONUSES)
			return false;

		if (realm == ERealm.None)
			return false;

		int count = 0;
		switch (realm)
		{
			case ERealm.Albion: count = albCount; break;
			case ERealm.Midgard: count = midCount; break;
			case ERealm.Hibernia: count = hibCount; break;
		}

		return count >= (int)type;
	}

	/// <summary>
	/// Update the counts of the keeps that we store locally,
	/// we do this for performance reasons
	/// </summary>
	public static void UpdateCounts()
	{
		albCount = GameServer.KeepManager.GetKeepCountByRealm(ERealm.Albion);
		midCount = GameServer.KeepManager.GetKeepCountByRealm(ERealm.Midgard);
		hibCount = GameServer.KeepManager.GetKeepCountByRealm(ERealm.Hibernia);
	}
}