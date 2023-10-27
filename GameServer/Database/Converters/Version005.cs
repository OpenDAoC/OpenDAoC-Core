using Core.Database.Tables;
using log4net;

namespace Core.GS.Database;

/// <summary>
/// Converts the database format to the version 5
/// </summary>
[DbConverter(5)]
public class Version005 : IDbConverter
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
		log.Info("Database Version 5 Convert Started");
		log.Info("This fixes some errors with the area classtypes");

		var objs = GameServer.Database.SelectAllObjects<DbArea>();
		int count = 0;
		foreach (DbArea area in objs)
		{
			string orig = area.ClassType;
			if (area.ClassType == "Core.GS.Area.Circle")
				area.ClassType = "Core.GS.Area+Circle";
			else if (area.ClassType == "Core.GS.Area.Square")
				area.ClassType = "Core.GS.Area+Square";
			else if (area.ClassType == "Core.GS.Area.BindArea")
				area.ClassType = "Core.GS.Area+BindArea";
			if (area.ClassType != orig)
			{
				count++;
				GameServer.Database.SaveObject(area);
			}
		}

		log.Info("Converted " + count + " areas");

		log.Info("Database Version 5 Convert Finished");
	}
}