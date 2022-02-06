/*
 * 
 * ATLAS Beta
 *
 */
using System;
using System.Reflection;
using log4net;
using DOL.Database;
using DOL.Events;
using DOL.GS.ServerProperties;


namespace DOL.GS.GameEvents
{
	/// <summary>
	/// This class hold the Character Creation Custom Settings
	/// This is the best example on how to change Characters parameters on Creation.
	/// </summary>
	///
	public static class AtlasBetaSettings
	{

		/// <summary>
		/// Declare a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
		/// <summary>
		/// Register Character Creation Events
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		[ScriptLoadedEvent]
		public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			GameEventMgr.AddHandler(GamePlayerEvent.GameEntered,new DOLEventHandler(OnPlayerLogin));
			if (log.IsInfoEnabled)
				log.Info("Atlas Beta initialized");
		}
		
		
		/// <summary>
		/// Unregister Character Creation Events
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		[ScriptUnloadedEvent]
		public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(OnPlayerLogin));
		}
		
		/// <summary>
		/// Saving the Player account Beta participation
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public static void OnPlayerLogin(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer p = sender as GamePlayer;
			const string customKey = "PvEBetaParticipation";
			var hasPvEBetaParticipationTitle = DOLDB<AccountXCustomParam>.SelectObject(DB.Column("Name").IsEqualTo(p.Client.Account.Name).And(DB.Column("KeyName").IsEqualTo(customKey)));
			DateTime betaEnd = new DateTime(2022, 03, 30); // dummy date
			
			if (p == null)
				return;

			if (hasPvEBetaParticipationTitle == null && DateTime.Now < betaEnd)
			{
				var PvEBetaParticipationTitle = new AccountXCustomParam();
				PvEBetaParticipationTitle.Name = p.Client.Account.Name;
				PvEBetaParticipationTitle.KeyName = customKey;
				PvEBetaParticipationTitle.Value = "1";
				GameServer.Database.AddObject(PvEBetaParticipationTitle);
			}

		}
		
	}
}