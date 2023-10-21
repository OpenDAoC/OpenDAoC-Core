using System;
using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Enums;

namespace Core.GS.GameEvents
{
	/// <summary>
	/// Enable Crafting Level Upon Character Creation
	/// </summary>
	public static class CharacterCreationCrafting
	{
		/// <summary>
		/// Register Character Creation Events
		/// </summary>
		/// <param name="e"></param>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		[ScriptLoadedEvent]
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
		[ScriptUnloadedEvent]
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
			// Check Args
			var chArgs = args as CharacterEventArgs;
			
			if (chArgs == null)
				return;
			
			DbCoreCharacter ch = chArgs.Character;
			
			// Add all Crafting skills at level 1
			var collectionAllCraftingSkills = new List<string>();
			foreach (int craftingSkillId in Enum.GetValues(typeof(ECraftingSkill)))
			{
				if (craftingSkillId > 0)
				{
					collectionAllCraftingSkills.Add(string.Format("{0}|1", craftingSkillId));
					if (craftingSkillId == (int)ECraftingSkill._Last)
						break;
				}
			}
			
			// Set Primary Skill to Basic.
			ch.SerializedCraftingSkills = string.Join(";", collectionAllCraftingSkills);
			ch.CraftingPrimarySkill = (int)ECraftingSkill.BasicCrafting;
		}

	}
}
