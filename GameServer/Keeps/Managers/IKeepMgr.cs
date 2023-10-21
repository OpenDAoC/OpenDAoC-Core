using System;
using System.Collections;
using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.Keeps
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class KeepManagerAttribute : Attribute
	{
		/// <summary>
		/// An attribute to identify a keep manager
		/// If one is defined then this will be used as the GameServer KeepManager
		/// </summary>
		public KeepManagerAttribute()
		{
		}
	}

	/// <summary>
	/// Interface for a Keep Manager
	/// To make your own inherit from this interface and implement all methods.  You can also inherit from
	/// DefaultKeepManager and only override what you want to change.  In order to use your keep manager you must also add
	/// the KeepManagerAttribute above ->  [KeepManager]
	/// </summary>
	public interface IKeepMgr
	{
		log4net.ILog Log { get; }
		Hashtable Keeps { get; }
		List<uint> FrontierRegionsList { get; }

		bool Load();
		bool IsNewKeepComponent(int skin);
		void RegisterKeep(int keepID, AGameKeep keep);
		AGameKeep GetKeepByID(int id);
		IEnumerable GetKeepsCloseToSpot(ushort regionid, IPoint3D point3d, int radius);
		AGameKeep GetKeepCloseToSpot(ushort regionid, IPoint3D point3d, int radius);
		ICollection<IGameKeep> GetKeepsByRealmMap(int map);
		AGameKeep GetBGPK(GamePlayer player);
		ICollection<AGameKeep> GetFrontierKeeps();
		ICollection<AGameKeep> GetKeepsOfRegion(ushort region);
		ICollection<AGameKeep> GetKeepsCloseToSpot(ushort regionid, int x, int y, int z, int radius);
		AGameKeep GetKeepCloseToSpot(ushort regionid, int x, int y, int z, int radius);
		int GetTowerCountByRealm(ERealm realm);
		Dictionary<ERealm, int> GetTowerCountAllRealm();
		Dictionary<ERealm, int> GetTowerCountFromZones(List<int> zones);
		int GetKeepCountByRealm(ERealm realm);
		ICollection<AGameKeep> GetAllKeeps();
		bool IsEnemy(AGameKeep keep, GamePlayer target, bool checkGroup);
		bool IsEnemy(AGameKeep keep, GamePlayer target);
		bool IsEnemy(GameKeepGuard checker, GamePlayer target);
		bool IsEnemy(GameKeepGuard checker, GamePlayer target, bool checkGroup);
		bool IsEnemy(GameKeepDoor checker, GamePlayer target);
		bool IsEnemy(GameKeepComponent checker, GamePlayer target);
		byte GetHeightFromLevel(byte level);
		void GetBorderKeepLocation(int keepid, out int x, out int y, out int z, out ushort heading);
		int GetRealmKeepBonusLevel(ERealm realm);
		int GetRealmTowerBonusLevel(ERealm realm);
		void UpdateBaseLevels();
		DbBattleground GetBattleground(ushort region);
		void ExitBattleground(GamePlayer player);
		AGameKeep GetKeepByShortName(string keepname);
	}
}
