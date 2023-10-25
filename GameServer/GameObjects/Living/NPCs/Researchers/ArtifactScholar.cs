// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Text;
// using Core.GS.Packets.Server;
// using Core.GS.Quests;
// using Core.Database;
//
// namespace Core.GS
// {
//     /// <summary>
//     /// Artifact scholars.
//     /// </summary>
//     /// <author>Aredhel</author>
//     public class ArtifactScholar : Researcher
//     {
//         public ArtifactScholar()
//             : base() { }
//
// 		/// <summary>
// 		/// Invoked when scholar receives an item.
// 		/// </summary>
// 		/// <param name="source"></param>
// 		/// <param name="item"></param>
// 		/// <returns></returns>
// 		public override bool ReceiveItem(GameLiving source, InventoryItem item)
// 		{
// 			GamePlayer player = source as GamePlayer;
//
// 			if (player != null)
// 			{
// 				lock (QuestListToGive.SyncRoot)
// 				{
// 					foreach (AbstractQuest quest in player.QuestList)
// 					{
// 						if (quest is ArtifactTurnInQuest)
// 						{
// 							ArtifactTurnInQuest aquest = quest as ArtifactTurnInQuest;
//
// 							if (HasQuest(aquest.GetType()) != null && aquest.ReceiveItem(player, this, item))
// 							{
// 								return true;
// 							}
// 						}
// 					}
// 				}
// 			}
//
// 			return base.ReceiveItem(source, item);
// 		}
//
// 		/// <summary>
// 		/// When someone whispers to this scholar
// 		/// </summary>
// 		/// <param name="source"></param>
// 		/// <param name="text"></param>
// 		/// <returns></returns>
// 		public override bool WhisperReceive(GameLiving source, string text)
// 		{
// 			GamePlayer player = source as GamePlayer;
// 			if (player != null)
// 			{
// 				lock (QuestListToGive.SyncRoot)
// 				{
// 					// Start new quest...
//
// 					foreach (ArtifactTurnInQuest quest in QuestListToGive)
// 					{
// 						if (text.ToLower() == "interested" && quest.CheckQuestQualification(player))
// 						{
// 							ArtifactTurnInQuest newQuest = new ArtifactTurnInQuest(player, this.Name);
// 							newQuest.Step = 0;
// 							player.AddQuest(newQuest);
// 							player.Out.SendQuestListUpdate();
// 							SayTo(player, "Since you are still interested, simply hand me your Artifact and I shall begin.");
// 							return true;
// 						}
// 					}
//
// 					// ...or continuing a quest?
//
// 					foreach (AbstractQuest quest in player.QuestList)
// 					{
// 						if (quest is ArtifactTurnInQuest && HasQuest(quest.GetType()) != null)
// 							if ((quest as ArtifactTurnInQuest).WhisperReceive(player, this, text))
// 								return true;
// 					}
// 				}
// 			}
// 			return base.WhisperReceive(source, text);
// 		}
//
// 		/// <summary>
// 		/// When someone interacts with this scholar
// 		/// </summary>
// 		/// <param name="player"></param>
// 		/// <returns></returns>
// 		public override bool Interact(GamePlayer player)
// 		{
// 			if (player != null)
// 			{
// 				lock (QuestListToGive.SyncRoot)
// 				{
// 					// See if they player has the quest first
// 					foreach (AbstractQuest quest in player.QuestList)
// 					{
// 						if (quest is ArtifactTurnInQuest && HasQuest(quest.GetType()) != null)
// 							if ((quest as ArtifactTurnInQuest).Interact(player, this))
// 								return true;
// 					}
//
// 					// Give the intro text
// 					foreach (ArtifactTurnInQuest quest in QuestListToGive)
// 					{
// 						List<string> arts = ArtifactMgr.GetArtifacts(player);
// 						foreach (string art in arts)
// 						{
// 							Dictionary<String, ItemTemplate> versions = ArtifactMgr.GetArtifactVersions(art, 
// 								(eCharacterClass)player.CharacterClass.ID, (eRealm)player.Realm);
// 							if (versions.Count > 1)
// 							{
// 								this.SayTo(player, string.Format("{0}, I see that you carry {1}. If you are interested, I will exchange yours for you but there is a price to pay for the change. Any experience or levels your artifact has gained will be lost when the change occurs. Are you still [interested]?", player.CharacterClass.Name, art));
// 							}
// 						}
// 						return true;
// 					}
// 				}
// 			}
// 			return base.Interact(player);
// 		}
//     }
// }

namespace Core.GS;