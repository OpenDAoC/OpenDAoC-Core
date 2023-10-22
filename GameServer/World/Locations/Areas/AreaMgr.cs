using System;
using System.Reflection;
using Core.Database.Tables;
using log4net;

namespace Core.GS.World;

public class AreaMgr
{
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public static bool LoadAllAreas()
	{
		try
		{
			Assembly gasm = Assembly.GetExecutingAssembly();
			var DBAreas = GameServer.Database.SelectAllObjects<DbArea>();
			foreach (DbArea thisArea in DBAreas)
			{
				AArea area = (AArea)gasm.CreateInstance(thisArea.ClassType, false);
				if (area == null)
				{
					foreach (Assembly asm in ScriptMgr.Scripts)
					{
						try
						{
							area = (AArea)asm.CreateInstance(thisArea.ClassType, false);
							
							if (area != null) 
								break;
						}
						catch (Exception e)
						{
							if (log.IsErrorEnabled)
								log.Error("LoadAllAreas", e);
						}
					}

					if (area == null)
					{
						log.Debug("area type " + thisArea.ClassType + " cannot be created, skipping");
						continue;
					}
				}
				area.LoadFromDatabase(thisArea);
				area.Sound = thisArea.Sound;
				area.CanBroadcast = thisArea.CanBroadcast;
				Region region = WorldMgr.GetRegion(thisArea.Region);
				if (region == null)
					continue;
				region.AddArea(area);
				log.Info("Area added: " + thisArea.Description);
			}
			return true;
		}
		catch (Exception ex)
		{
			log.Error("Loading all areas failed", ex);
			return false;
		}
	}
}