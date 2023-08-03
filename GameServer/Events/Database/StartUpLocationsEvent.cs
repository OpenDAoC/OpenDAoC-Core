/*
 * Author:	code by Noret, locations by Avatarius
 * Date:	19.07.2004
 * This script should be put in /scripts/gameevents directory.
 * This event script changes starting location of created characters
 * based on region, class and race
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using log4net;

namespace DOL.GS.GameEvents
{
	/// <summary>
	/// Moves new created Characters to the starting location based on region, class and race
	/// </summary>
	public static class StartUpLocationsEvent
	{
		/// <summary>
		/// Declare a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Cached DB Startup Location
		/// </summary>
		private static readonly List<DbStartUpLocation> m_cachedLocations = new List<DbStartUpLocation>();

		/// <summary>
		/// Current Game Request Tutorial Region ID.
		/// </summary>
		private const int TUTORIAL_REGIONID = 27;
		
		[ScriptLoadedEvent]
		public static void OnScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			GameEventMgr.AddHandler(DatabaseEvent.CharacterCreated, new CoreEventHandler(CharacterCreation));
			GameEventMgr.AddHandler(DatabaseEvent.CharacterSelected, new CoreEventHandler(CharacterSelection));
			
			InitStartupLocation();
			
			if (log.IsInfoEnabled)
				log.Info("StartupLocations initialized");
		}

		[ScriptUnloadedEvent]
		public static void OnScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			GameEventMgr.RemoveHandler(DatabaseEvent.CharacterCreated, new CoreEventHandler(CharacterCreation));
			GameEventMgr.RemoveHandler(DatabaseEvent.CharacterSelected, new CoreEventHandler(CharacterSelection));
		}
		
		/// <summary>
		/// Init Startup Location Static Cache
		/// </summary>
		[RefreshCommand]
		public static void InitStartupLocation()
		{
			m_cachedLocations.Clear();
			
			foreach (var obj in GameServer.Database.SelectAllObjects<DbStartUpLocation>())
				m_cachedLocations.Add(obj);
		}

		/// <summary>
		/// Change location on character creation
		/// </summary>
		public static void CharacterCreation(CoreEvent ev, object sender, EventArgs args)
		{
			// Check Args
			var chArgs = args as CharacterEventArgs;
			
			if (chArgs == null)
				return;
			
			DbCoreCharacters ch = chArgs.Character;
			
			try
			{
				
				var availableLocation = GetAllStartupLocationForCharacter(ch, chArgs.GameClient.Version);

				DbStartUpLocation dbStartupLocation = null;
				
				// get the first entry according to Tutorial Enabling.
				foreach (var location in availableLocation)
				{
					if (ServerProperties.ServerProperties.EVENT_THIDRANKI || ServerProperties.ServerProperties.EVENT_PVP)
						continue;
					
					dbStartupLocation = location;
					break;
				}
				
				if (dbStartupLocation == null)
				{
					log.WarnFormat("startup location not found: account={0}; char name={1}; region={2}; realm={3}; class={4} ({5}); race={6} ({7}); version={8}",
					             ch.AccountName, ch.Name, ch.Region, ch.Realm, ch.Class, (ECharacterClass) ch.Class, ch.Race, (ERace)ch.Race, chArgs.GameClient.Version);
				}
				else
				{
					ch.Xpos = dbStartupLocation.XPos;
					ch.Ypos = dbStartupLocation.YPos;
					ch.Zpos = dbStartupLocation.ZPos;
					ch.Region = dbStartupLocation.Region;
					ch.Direction = dbStartupLocation.Heading;
					BindCharacter(ch);
					Console.WriteLine("startup location: account={0}; char name={1}; region={2}; realm={3}; class={4} ({5}); race={6} ({7}); version={8}",
						ch.AccountName, ch.Name, ch.Region, ch.Realm, ch.Class, (ECharacterClass) ch.Class, ch.Race, (ERace)ch.Race, chArgs.GameClient.Version); 
				}				
			}
			catch (Exception e)
			{
				if (log.IsErrorEnabled)
					log.ErrorFormat("StartupLocations script: error changing location. account={0}; char name={1}; region={2}; realm={3}; class={4} ({5}); race={6} ({7}); version={8}; {9}",
					                ch.AccountName, ch.Name, ch.Region, ch.Realm, ch.Class, (ECharacterClass) ch.Class, ch.Race, (ERace)ch.Race, chArgs.GameClient.Version, e);
			}
		}

		/// <summary>
		/// Change location on character selection if it has any wrong values...
		/// </summary>
		public static void CharacterSelection(CoreEvent ev, object sender, EventArgs args)
		{
			// Check Args
			var chArgs = args as CharacterEventArgs;
			
			if (chArgs == null)
				return;
			
			DbCoreCharacters ch = chArgs.Character;
			
			// check if location looks ok.
			if (ch.Xpos == 0 && ch.Ypos == 0 && ch.Zpos == 0)
			{
				// This character needs to be fixed !
				CharacterCreation(ev, sender, args);
				GameServer.Database.SaveObject(ch);
				return;
			}
			
			// check if bind looks ok.
			if (ch.BindXpos == 0 && ch.BindYpos == 0 && ch.BindZpos == 0)
			{
				// This Bind needs to be fixed !
				BindCharacter(ch);
				GameServer.Database.SaveObject(ch);
			}
		}
		
		public static IList<DbStartUpLocation> GetAllStartupLocationForCharacter(DbCoreCharacters ch, GameClient.EClientVersion cli)
		{
			return m_cachedLocations.Where(sl => sl.MinVersion <= (int)cli)
				.Where(sl => sl.ClassID == 0 || sl.ClassID == ch.Class)
				.Where(sl => sl.RaceID == 0 || sl.RaceID == ch.Race)
				.Where(sl => sl.RealmID == 0 || sl.RealmID == ch.Realm)
				.Where(sl => sl.ClientRegionID == 0 || sl.ClientRegionID == ch.Region)
				.OrderByDescending(sl => sl.MinVersion).ThenByDescending(sl => sl.ClientRegionID)
				.ThenByDescending(sl => sl.RealmID).ThenByDescending(sl => sl.ClassID)
				.ThenByDescending(sl => sl.RaceID).ToList();
		}
		
		public static DbStartUpLocation GetNonTutorialLocation(GamePlayer player)
		{
			try
			{
				return GetAllStartupLocationForCharacter(player.Client.Account.Characters[player.Client.ActiveCharIndex], player.Client.Version).First(sl => sl.ClientRegionID != TUTORIAL_REGIONID);
			}
			catch
			{
				return null;
			}
				
		}

		/// <summary>
		/// Binds character to current location
		/// </summary>
		/// <param name="ch"></param>
		public static void BindCharacter(DbCoreCharacters ch)
		{
			ch.BindRegion = ch.Region;
			ch.BindHeading = ch.Direction;
			ch.BindXpos = ch.Xpos;
			ch.BindYpos = ch.Ypos;
			ch.BindZpos = ch.Zpos;
		}
	}
}