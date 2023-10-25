using Core.Database;
using Core.Database.Enums;
using Core.Database.Tables;
using log4net;

namespace Core.GS.Database;

/// <summary>
/// Converts the database format to the version 3
/// </summary>
[DbConverter(4)]
public class Version004 : IDbConverter
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// we need to make use of the new poison fields
	/// </summary>
	public void ConvertDatabase()
	{
		log.Info("Database Version 4 Convert Started");

		if (GameServer.Instance.Configuration.DBType == EConnectionType.DATABASE_XML)
		{
			log.Info("You have an XML database loaded, this converter will only work with MySQL, skipping");
			return;
		}

		var mobs = CoreDb<DbMob>.SelectObjects(DB.Column("ClassType").IsEqualTo("DOL.GS.GameMob"));

		int count = 0;
		foreach (DbMob mob in mobs)
		{
			mob.ClassType = "Core.GS.GameNpc";
			GameServer.Database.SaveObject(mob);
			count++;
		}

		log.Info("Converted " + count + " mobs");

		log.Info("Database Version 4 Convert Finished");
	}
}