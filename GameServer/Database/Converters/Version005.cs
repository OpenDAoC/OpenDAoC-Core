using System;
using System.Collections;
using log4net;
using DOL.Database;

namespace DOL.GS.DatabaseConverters
{
	/// <summary>
	/// Converts the database format to the version 5
	/// </summary>
	[DatabaseConverter(5)]
	public class Version005 : IDatabaseConverter
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

			var objs = GameServer.Database.SelectAllObjects<DbAreas>();
			int count = 0;
			foreach (DbAreas area in objs)
			{
				string orig = area.ClassType;
				if (area.ClassType == "DOL.GS.Area.Circle")
					area.ClassType = "DOL.GS.Area+Circle";
				else if (area.ClassType == "DOL.GS.Area.Square")
					area.ClassType = "DOL.GS.Area+Square";
				else if (area.ClassType == "DOL.GS.Area.BindArea")
					area.ClassType = "DOL.GS.Area+BindArea";
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
}