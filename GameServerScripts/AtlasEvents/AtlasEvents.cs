/*
 * 
 * ATLAS Events
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
	public static class AtlasEventSettings
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
			GameEventMgr.AddHandler(GameLivingEvent.GainedRealmPoints, new DOLEventHandler(OnRPGain));
			GameEventMgr.AddHandler(GamePlayerEvent.GameEntered,new DOLEventHandler(OnPlayerLogin));
			GameEventMgr.AddHandler(GamePlayerEvent.Released, new DOLEventHandler(OnPlayerReleased));
			if (log.IsInfoEnabled)
				log.Info("Atlas Event initialized");
		}
		
		public static int EventLvCap = ServerProperties.Properties.EVENT_LVCAP;
		public static int EventRPCap = ServerProperties.Properties.EVENT_RPCAP;
		public static int SoloPop = ServerProperties.Properties.EVENT_SOLO_POP;
		
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
			GameEventMgr.RemoveHandler(GameLivingEvent.GainedRealmPoints, new DOLEventHandler(OnRPGain));
			GameEventMgr.RemoveHandler(GamePlayerEvent.GameEntered, new DOLEventHandler(OnPlayerLogin));
			GameEventMgr.RemoveHandler(GamePlayerEvent.Released, new DOLEventHandler(OnPlayerReleased));
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
			
			if (EventLvCap == 0)
				return;

			DOLCharacters ch = chArgs.Character;

			//moving and binding newly created characters to the BG event zone
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
				ch.GainXP = false;
				BindCharacter(ch);
			}

			//moving and binding newly created characters to the PVP event zone
			if (ServerProperties.Properties.EVENT_TUTORIAL)
			{
				ch.Xpos = 342521;
				ch.Ypos = 385230;
				ch.Zpos = 5410;
				ch.Direction = 1756;
				ch.Region = 27;
				ch.GainXP = false;
				BindCharacter(ch);
			}
			
		}

		private static void OnPlayerReleased(DOLEvent e, object sender, EventArgs arguments)
		{
			GamePlayer p = sender as GamePlayer;

			if (p.CurrentRegionID == 27 && Properties.EVENT_PVP && WorldMgr.GetAllClientsCount() < SoloPop)
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
		public static void OnRPGain(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer p = sender as GamePlayer;
			
			if (p == null)
				return;
			
			if (EventRPCap == 0)
				return;

			// BG event RP cap check
			if (ServerProperties.Properties.EVENT_THIDRANKI && p.RealmPoints > EventRPCap)
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
			
			// in case we ever have a RP cap with the PVP event
			if (ServerProperties.Properties.EVENT_TUTORIAL && p.RealmPoints > EventRPCap)
			{
				p.MoveTo(27, 342521, 385230, 5410, 1756);
			}
		}
		
		public static void OnPlayerLogin(DOLEvent e, object sender, EventArgs args)
		{
			
			// trying to catch, move and bind existing characters
			
			GamePlayer p = sender as GamePlayer;
			
			if (p == null)
				return;

			if (EventLvCap == 0)
				return;
			
			// Jailed players stay in jail
			if (p.CurrentRegionID == 249)
				return;
			
			// GMs don't get ported at login
			if (p.Client.Account.PrivLevel > 1)
			{
				return;
			}
			
			// BG event login checks
			if (ServerProperties.Properties.EVENT_THIDRANKI && p.CurrentRegionID != 252)
			{
				switch (p.Realm)
				{
					//case eRealm.Albion:
					//	p.MoveTo(252, 38113, 53507, 4160, 3268);
					//	break;
					//case eRealm.Midgard:
					//	p.MoveTo(252, 53568, 23643, 4530, 3268);
					//	break;
					//case eRealm.Hibernia:
					//	p.MoveTo(252, 17367, 18248, 4320, 3268);
					//	break;
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
				p.Bind(true);
			}

			// PVP event login checks
			if (ServerProperties.Properties.EVENT_TUTORIAL)
			{
				p.MoveTo(27, 342521, 385230, 5410, 1756);
				
				// bind characters the first time they port - hopefully they'll use the event level NPC shortly after entering
				if (p.Level != EventLvCap)
				{
					p.Bind(true);
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