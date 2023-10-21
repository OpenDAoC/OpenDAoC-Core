using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Commands;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Server;
using log4net;

namespace Core.GS.GameEvents
{
	/// <summary>
	/// This class makes sure that all the startup guilds are created in the database
	/// </summary>
	public static class StartupGuilds
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
		/// <summary>
		/// Enable Starter Guilds
		/// </summary>
		[ServerProperty("startup", "starting_guild", "Starter Guild - Edit this to enable/disable the starter guilds", true)]
		public static bool STARTING_GUILD;

		/// <summary>
		/// This method runs the checks and listen to new player creation.
		/// </summary>
		/// <param name="e">The event</param>
		/// <param name="sender">The sender</param>
		/// <param name="args">The arguments</param>
		[ScriptLoadedEvent]
		public static void OnScriptCompiled(CoreEvent e, object sender, EventArgs args)
		{
            GameEventMgr.AddHandler(DatabaseEvent.CharacterCreated, new CoreEventHandler(AddNewbieToStarterGuild));

            if (!STARTING_GUILD)
                return;
            
            CheckStartupGuilds();            
		}
		
		/// <summary>
		/// Try to recreate Startup Guild
		/// </summary>
		[RefreshCommand]
		public static void CheckStartupGuilds()
		{
            foreach (ERealm currentRealm in Enum.GetValues(typeof(ERealm)))
			{
				if (currentRealm == ERealm.None || currentRealm == ERealm.Door)
					continue;
				
				CheckGuild(currentRealm,LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, string.Format("Guild.StartupGuild.{0}", GlobalConstants.RealmToName(currentRealm))));
			}
		}
		
		/// <summary>
		/// Remove event handler on server shutdown.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		[ScriptUnloadedEvent]
		public static void OnScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			GameEventMgr.RemoveHandler(DatabaseEvent.CharacterCreated, new CoreEventHandler(AddNewbieToStarterGuild));
		}
		
		/// <summary>
		/// Add newly created player to startup guild.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public static void AddNewbieToStarterGuild(CoreEvent e, object sender, EventArgs args)
		{
			if (!STARTING_GUILD)
				return;
			
			// Check Args
			var chArgs = args as CharacterEventArgs;
			
			if (chArgs == null)
				return;
			
			DbCoreCharacter ch = chArgs.Character;
			DbAccount account = chArgs.GameClient.Account;
			

			var guildname = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE, string.Format("Guild.StartupGuild.{0}", GlobalConstants.RealmToName((ERealm)ch.Realm)));
			ch.GuildID = GuildMgr.GuildNameToGuildID(guildname);

			if (ch.GuildID != "")
				ch.GuildRank = 8;
			
		}

		/// <summary>
		/// This method checks if a guild exists
		/// if not, the guild is created with default values
		/// </summary>
		/// <param name="currentRealm">Current Realm being checked</param>
		/// <param name="guildName">The guild name that is being checked</param>
		private static void CheckGuild(ERealm currentRealm, string guildName)
		{
			if (!GuildMgr.DoesGuildExist(guildName))
			{
				GuildUtil newguild = GuildMgr.CreateGuild(currentRealm, guildName);
				newguild.Ranks[8].OcHear = true;
				newguild.Motd = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE,"Guild.StartupGuild.Motd");
				newguild.Omotd = LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE,"Guild.StartupGuild.Omotd");
				newguild.BonusType = EGuildBonusType.Experience;
				newguild.BonusStartTime = DateTime.Now;
				newguild.Ranks[8].Title =  LanguageMgr.GetTranslation(ServerProperty.SERV_LANGUAGE,"Guild.StartupGuild.Title");
				newguild.Ranks[8].Invite = true;
				newguild.IsStartingGuild = true;
			}
		}
	}
}