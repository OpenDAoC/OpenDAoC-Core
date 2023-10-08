using System;
using DOL.Database;
using DOL.Events;
using DOL.GS.ServerProperties;

namespace DOL.GS.GameEvents
{
	/// <summary>
	/// Startup Script to Revert Player Class to Base Class.
	/// </summary>
	public static class StartAsBaseClass
	{
		/// <summary>
		/// Should the server start characters as Base Class?
		/// </summary>
		[Properties("startup", "start_as_base_class", "Should we start all players as their base class? true if yes (e.g. Armsmen become Fighters on Creation)", false)]
		public static bool START_AS_BASE_CLASS;

		/// <summary>
		/// Register Character Creation Events
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		[GameServerStartedEvent]
		public static void OnScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			GameEventMgr.AddHandler(DatabaseEvent.CharacterCreated, new CoreEventHandler(OnCharacterCreation));
		}
		
		/// <summary>
		/// Unregister Character Creation Events
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		[GameServerStoppedEvent]
		public static void OnScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			GameEventMgr.RemoveHandler(DatabaseEvent.CharacterCreated, new CoreEventHandler(OnCharacterCreation));
		}
		
		/// <summary>
		/// Triggered When New Character is Created.
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		public static void OnCharacterCreation(CoreEvent e, object sender, EventArgs args)
		{
			// Only act if enabled.
			if (!START_AS_BASE_CLASS)
				return;
			
			// Check Args
			var chArgs = args as CharacterEventArgs;
			
			if (chArgs == null)
				return;
			
			DbCoreCharacter ch = chArgs.Character;

			// Revert to Base Class.
			var chClass = ScriptMgr.FindCharacterBaseClass(ch.Class);
			
			if (chClass != null && chClass.ID != ch.Class)
				ch.Class = chClass.ID;
		}
		
	}
}
