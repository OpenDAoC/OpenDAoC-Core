﻿using System;
using System.Collections.Generic;

using DOL.Events;
using DOL.Database;

namespace DOL.GS.GameEvents
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
			
			DbCoreCharacters ch = chArgs.Character;
			
			// Add all Crafting skills at level 1
			var collectionAllCraftingSkills = new List<string>();
			foreach (int craftingSkillId in Enum.GetValues(typeof(eCraftingSkill)))
			{
				if (craftingSkillId > 0)
				{
					collectionAllCraftingSkills.Add(string.Format("{0}|1", craftingSkillId));
					if (craftingSkillId == (int)eCraftingSkill._Last)
						break;
				}
			}
			
			// Set Primary Skill to Basic.
			ch.SerializedCraftingSkills = string.Join(";", collectionAllCraftingSkills);
			ch.CraftingPrimarySkill = (int)eCraftingSkill.BasicCrafting;
		}

	}
}