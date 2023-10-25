using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.World;
using log4net;

namespace Core.GS.Database;

/// <summary>
/// Converts the database format to the version 2
/// </summary>
[DbConverter(2)]
public class Version002 : IDbConverter
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// style icon field added this should copy the ID value
	/// realm 6 should be peace flag and realm changed
	/// </summary>
	public void ConvertDatabase()
	{
		log.Info("Database Version 2 Convert Started");

		log.Info("Converting Styles");
		var styles = GameServer.Database.SelectAllObjects<DbStyle>();
		foreach (DbStyle style in styles)
		{
			style.Icon = style.ID;

			GameServer.Database.SaveObject(style);
		}
		log.Info(styles.Count + " Styles Processed");

		log.Info("Converting Mobs");
		var mobs = CoreDb<DbMob>.SelectObjects(DB.Column("Realm").IsEqualTo(6));
		foreach (DbMob mob in mobs)
		{
			if ((mob.Flags & (uint)ENpcFlags.PEACE) == 0)
			{
				mob.Flags ^= (uint)ENpcFlags.PEACE;
			}

			Region region = WorldMgr.GetRegion(mob.Region);
			if (region != null)
			{
				Zone zone = region.GetZone(mob.X, mob.Y);
				if (zone != null)
				{
					mob.Realm = (byte)zone.Realm;
				}
			}

			GameServer.Database.SaveObject(mob);
		}
		log.Info(mobs.Count + " Mobs Processed");

		log.Info("Database Version 2 Convert Finished");
	}
}