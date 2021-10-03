/*
 * 
 * ATLAS Thidranki Event
 *
 */
using System;
using System.Reflection;
using log4net;
using DOL.Database;
using DOL.Events;

namespace DOL.GS.GameEvents
{
	/// <summary>
	/// This class hold the Character Creation Custom Settings
	/// This is the best example on how to change Characters parameters on Creation.
	/// </summary>
	/// TODO Pass Level and RRCap as serverproperties
	public static class ThidrankiEventSettings
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
			GameEventMgr.AddHandler(DatabaseEvent.CharacterCreated, new DOLEventHandler(OnCharacterCreation));
			GameEventMgr.AddHandler(GamePlayerEvent.RRLevelUp, new DOLEventHandler(OnRRLevelUp));
			
			if (log.IsInfoEnabled)
				log.Info("Thidranki Event initialized");
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
			GameEventMgr.RemoveHandler(DatabaseEvent.CharacterCreated, new DOLEventHandler(OnCharacterCreation));
			GameEventMgr.RemoveHandler(GamePlayerEvent.RRLevelUp, new DOLEventHandler(OnRRLevelUp));
		}
		
		/// <summary>
		/// Triggered When New Character is Created.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public static void OnCharacterCreation(DOLEvent e, object sender, EventArgs args)
		{
			// Check Args
			var chArgs = args as CharacterEventArgs;
			
			if (chArgs == null)
				return;
			
			DOLCharacters ch = chArgs.Character;

			if (ServerProperties.Properties.EVENT_THIDRANKI)
			{
				switch (ch.Realm)
				{
					case 1:
						ch.Xpos = 52759;
						ch.Ypos = 39528;
						ch.Zpos = 4677;
						ch.Direction = 36;
						ch.Region = 330;
						break;
					case 2:
						ch.Xpos = 52160;
						ch.Ypos = 39862;
						ch.Zpos = 5472; 
						ch.Region = 334;
						ch.Direction = 46;
						break;
					case 3:
						ch.Xpos = 52836;
						ch.Ypos = 40401;
						ch.Zpos = 4672;
						ch.Direction = 441;
						ch.Region = 335;
						break;
				}
				
				ch.Experience = GamePlayer.GetExperienceAmountForLevel(23);
				ch.Level = 24;
				BindCharacter(ch);
			}
		}

		public static void OnRRLevelUp(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer p = sender as GamePlayer;
			
			if (p == null)
				return;

			if (ServerProperties.Properties.EVENT_THIDRANKI && p.RealmLevel >= 2)
			{
				switch (p.Realm)
				{
					case eRealm.Albion:
						p.MoveTo(330, 52759, 39528, 4677, 36);
						break;
					case eRealm.Midgard:
						p.MoveTo(334, 52160, 39862, 5472, 46);
						break;
					case eRealm.Hibernia:
						p.MoveTo(335, 52836, 40401, 4672, 441);
						break;
				}
			}
		}
					
		/// <summary>
		/// Binds character to current location
		/// </summary>
		/// <param name="ch"></param>
		public static void BindCharacter(DOLCharacters ch)
		{
			ch.BindRegion = ch.Region;
			ch.BindHeading = ch.Direction;
			ch.BindXpos = ch.Xpos;
			ch.BindYpos = ch.Ypos;
			ch.BindZpos = ch.Zpos;
		}
	}
}