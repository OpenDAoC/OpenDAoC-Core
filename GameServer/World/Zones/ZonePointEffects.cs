﻿using System;
using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.Events;

namespace DOL.GS.GameEvents
{
	public class ZonePointEffect
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		[ScriptLoadedEvent]
		public static void OnScriptsCompiled(CoreEvent e, object sender, EventArgs args)
		{

			// What npctemplate should we use for the zonepoint ?
			ushort model;
			NpcTemplate zp;
			try{
				model = (ushort)ServerProperties.ServerProperties.ZONEPOINT_NPCTEMPLATE;
				zp = new NpcTemplate(CoreDb<DBNpcTemplate>.SelectObjects(DB.Column("TemplateId").IsEqualTo(model)).FirstOrDefault());
				if (model <= 0 || zp == null) throw new ArgumentNullException();
			}
			catch {
				return;
			}
			
			// processing all the ZP
			IList<DbZonePoints> zonePoints = GameServer.Database.SelectAllObjects<DbZonePoints>();
			foreach (DbZonePoints z in zonePoints)
			{
				if (z.SourceRegion == 0) continue;
				
				// find target region for the current zonepoint
				Region r = WorldMgr.GetRegion(z.TargetRegion);
				if (r == null)
				{
					log.Warn("Zonepoint Id (" + z.Id +  ") references an inexistent target region " + z.TargetRegion + " - skipping, ZP not created");
					continue;
				}
				
				GameNpc npc = new GameNpc(zp);

				npc.CurrentRegionID = z.SourceRegion;
				npc.X = z.SourceX;
				npc.Y = z.SourceY;
				npc.Z = z.SourceZ;
				npc.Name = r.Description;
				npc.GuildName = "ZonePoint (Open)";			
				if (r.IsDisabled) npc.GuildName = "ZonePoint (Closed)";
				
				npc.AddToWorld();
			}
		}
	}
}