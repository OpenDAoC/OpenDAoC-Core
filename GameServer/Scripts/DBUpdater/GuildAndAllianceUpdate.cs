using System;
using System.Linq;
using Core.Database;
using log4net;

namespace Core.GS.DatabaseUpdate
{
	[DbUpdate]
	public class GuildAndAllianceUpdate : IDbUpdater
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public void Update()
		{
			if (log.IsInfoEnabled)
				log.Info("Start Searching for records that need update...");
			
			// Change the Leader Relation if Missing
			var alliances = CoreDb<DbGuildAlliance>.SelectObjects(DB.Column("LeaderGuildID").IsEqualTo(string.Empty).Or(DB.Column("LeaderGuildID").IsNull()));
			
			if (alliances.Any())
			{
				
				var leadingGuilds = CoreDb<DbGuild>.MultipleSelectObjects(alliances.Select(al => DB.Column("AllianceID").IsEqualTo(al.ObjectId).And(DB.Column("GuildName").IsEqualTo(al.AllianceName))));
				
				var alliancesWithLeader = leadingGuilds.Select((gd, i) => {
				                                               	var al = alliances[i];
				                                               	DbGuild lead = null;
				                                               	try 
				                                               	{
				                                               		lead = gd.SingleOrDefault(gld => al.AllianceName.Equals(gld.GuildName));
				                                               	}
				                                               	catch (Exception e)
				                                               	{
				                                               		if (log.IsErrorEnabled)
				                                               			log.ErrorFormat("Wrong records while trying to retrieve Guild Leader (AllianceID: {0}, AllianceName: {1})\n{2}", al.ObjectId, al.AllianceName, e);
				                                               	}
				                                               	return new { Alliance = al, Leader = lead };
				                                               }).ToArray();
				
				if (log.IsInfoEnabled)
					log.InfoFormat("Fixing Alliances without Leader : {0} records found.", alliancesWithLeader.Length);
				
				foreach (var pair in alliancesWithLeader)
				{
					if (pair.Leader != null)
						pair.Alliance.LeaderGuildID = pair.Leader.GuildID;
					else if (log.IsWarnEnabled)
						log.WarnFormat("Alliance (ID:{0}, Name:{1}) can't resolve its Leading Guild !", pair.Alliance.ObjectId, pair.Alliance.AllianceName);
				}
				
				var saved = GameServer.Database.SaveObject(alliancesWithLeader.Select(pair => pair.Alliance));
				
				if (saved && log.IsInfoEnabled)
					log.InfoFormat("Finished saving Alliances without Leader successfully!");
				if (!saved && log.IsErrorEnabled)
					log.ErrorFormat("Could not save all Alliances without Leader, check logs or records...");
			}
			
			if (log.IsInfoEnabled)
				log.Info("End of Database Update...");
		}
	}
}
