using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;

namespace Core.GS
{
	/// <summary>
	/// The mother class for all class trainers
	/// </summary>
	public class GameTrainer : GameNpc
	{
		// List of disabled classes
		private static List<string> disabled_classes = null;
		
		// Values from live servers
		public enum eChampionTrainerType : int
		{
			Acolyte = 4,
			AlbionRogue = 2,
			Disciple = 7,
			Elementalist = 5,
			Fighter = 1,
			Forester = 12,
			Guardian = 1,
			Mage = 6,
			Magician = 11,
			MidgardRogue = 3,
			Mystic = 9,
			Naturalist = 10,
			Seer = 8,
			Stalker = 2,
			Viking = 1,
			None = 0,
		}
		
		// What kind of Champion trainer is this
		protected eChampionTrainerType m_championTrainerType = eChampionTrainerType.None;

		public virtual EPlayerClass TrainedClass
		{
			get { return EPlayerClass.Unknown; }
		}
		/// <summary>
		/// Constructs a new GameTrainer
		/// </summary>
		public GameTrainer()
		{
		}
		/// <summary>
		/// Constructs a new GameTrainer that will also train Champion levels
		/// </summary>
		public GameTrainer(eChampionTrainerType championTrainerType)
		{
			m_championTrainerType = championTrainerType;
		}

		#region GetExamineMessages
		/// <summary>
		/// Adds messages to ArrayList which are sent when object is targeted
		/// </summary>
		/// <param name="player">GamePlayer that is examining this object</param>
		/// <returns>list with string messages</returns>
		public override IList GetExamineMessages(GamePlayer player)
		{
			string TrainerClassName = "";
            switch (player.Client.Account.Language)
			{
                case "DE":
                    {
                        var translation = (DbLanguageGameNpc)LanguageMgr.GetTranslation(player.Client.Account.Language, this);

                        if (translation != null)
                        {
                            int index = -1;
                            if (translation.GuildName.Length > 0)
                                index = translation.GuildName.IndexOf("-Ausbilder");
                            if (index >= 0)
                                TrainerClassName = translation.GuildName.Substring(0, index);
                        }
                        else
                        {
                            TrainerClassName = GuildName;
                        }
                    }
                    break;
				default:
					{
						int index = -1;
						if (GuildName.Length > 0)
							index = GuildName.IndexOf(" Trainer");
						if (index >= 0)
							TrainerClassName = GuildName.Substring(0, index);
					}
					break;
			}

			IList list = new ArrayList();
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.GetExamineMessages.YouTarget", 
                                                GetName(0, false, player.Client.Account.Language, this)));
            list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.GetExamineMessages.YouExamine",
                                                GetName(0, false, player.Client.Account.Language, this), GetPronoun(0, true, player.Client.Account.Language),
                                                GetAggroLevelString(player, false), TrainerClassName));
			list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.GetExamineMessages.RightClick"));
			return list;
		}
		#endregion

		public virtual bool CanTrain(GamePlayer player)
		{
			return player.PlayerClass.ID == (int)TrainedClass || TrainedClass == EPlayerClass.Unknown;
		}

		/// <summary>
		/// Interact with trainer
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player)) return false;

			// Turn to face player
			TurnTo(player, 10000);

			// Unknown class must be used for multitrainer
			if (CanTrain(player))
			{
				player.Out.SendTrainerWindow();
				
				player.GainExperience(EXpSource.Other, 0);//levelup

				if (player.FreeLevelState == 2)
				{
					player.LastFreeLevel = player.Level;
					//long xp = GameServer.ServerRules.GetExperienceForLevel(player.PlayerCharacter.LastFreeLevel + 3) - GameServer.ServerRules.GetExperienceForLevel(player.PlayerCharacter.LastFreeLevel + 2);
					long xp = player.GetExperienceNeededForLevel(player.LastFreeLevel + 1) - player.GetExperienceNeededForLevel(player.LastFreeLevel);
					//player.PlayerCharacter.LastFreeLevel = player.Level;
					player.GainExperience(EXpSource.Other, xp);
					player.LastFreeLeveled = DateTime.Now;
					player.Out.SendPlayerFreeLevelUpdate();
				}
			}

			if (CanTrainChampionLevels(player))
			{
				player.Out.SendChampionTrainerWindow((int)m_championTrainerType);
			}

			return true;
		}

		/// <summary>
		/// Can we offer this player training for Champion levels?
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public virtual bool CanTrainChampionLevels(GamePlayer player)
		{
			return player.Level >= player.MaxLevel && player.Champion && m_championTrainerType != eChampionTrainerType.None && m_championTrainerType != player.PlayerClass.ChampionTrainerType();
		}

		/// <summary>
		/// Talk to trainer
		/// </summary>
		/// <param name="source"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		public override bool WhisperReceive(GameLiving source, string text)
		{
			if (!base.WhisperReceive(source, text)) return false;
			GamePlayer player = source as GamePlayer;
			if (player == null) return false;
			
			//level respec for players
			if (CanTrain(player) && text == LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.Interact.CaseRespecialize"))
			{
				if (player.Level == 5 && !player.IsLevelRespecUsed)
				{
					int specPoints = player.SkillSpecialtyPoints;

					player.RespecAll();

					// Assign full points returned
					if (player.SkillSpecialtyPoints > specPoints)
					{
						player.styleComponent.RemoveAllStyles(); // Kill styles
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.Interact.RegainPoints", (player.SkillSpecialtyPoints - specPoints)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
					player.RefreshSpecDependantSkills(false);
					// Notify Player of points
					player.Out.SendUpdatePlayerSkills();
					player.Out.SendUpdatePoints();
					player.Out.SendUpdatePlayer();
					player.Out.SendTrainerWindow();
					player.SaveIntoDatabase();
				}

			}

			//Now we turn the npc into the direction of the person
			TurnTo(player, 10000);

			return true;
		}

		/// <summary>
		/// Offer respecialize to the player.
		/// </summary>
		/// <param name="player"></param>
		protected virtual void OfferRespecialize(GamePlayer player)
		{
			player.Out.SendMessage(String.Format(LanguageMgr.GetTranslation
			                                     (player.Client, "GameTrainer.Interact.Respecialize", this.Name, player.Name)),
			                       EChatType.CT_Say, EChatLoc.CL_PopupWindow);
		}

		/// <summary>
		/// Check Ability to use Item
		/// </summary>
		/// <param name="player"></param>
		protected virtual void CheckAbilityToUseItem(GamePlayer player)
		{
			// drop any equiped-non usable item, in inventory or on the ground if full
			lock (player.Inventory)
			{
				foreach (DbInventoryItem item in player.Inventory.EquippedItems)
				{
					if (!player.HasAbilityToUseItem(item.Template))
						if (player.Inventory.IsSlotsFree(item.Count, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack) == true)
					{
						player.Inventory.MoveItem((EInventorySlot)item.SlotPosition, player.Inventory.FindFirstEmptySlot(EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack), item.Count);
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.CheckAbilityToUseItem.Text1", item.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
					else
					{
						player.Inventory.MoveItem((EInventorySlot)item.SlotPosition, EInventorySlot.Ground, item.Count);
						player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.CheckAbilityToUseItem.Text1", item.GetName(0, false)), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					}
				}
			}
		}

		/// <summary>
		/// For Recieving Respec Stones.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="item"></param>
		/// <returns></returns>
		public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
		{
			if (source == null || item == null) return false;

			GamePlayer player = source as GamePlayer;
			if (player != null)
			{
				switch (item.Id_nb)
				{
					case "respec_single":
						{
							player.Inventory.RemoveCountFromStack(item, 1);
							InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.Template);
							player.RespecAmountSingleSkill++;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.ReceiveItem.RespecSingle"), EChatType.CT_System, EChatLoc.CL_PopupWindow);
							return true;
						}
					case "respec_full":
						{
							player.Inventory.RemoveCountFromStack(item, 1);
							InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.Template);
							player.RespecAmountAllSkill++;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.ReceiveItem.RespecFull", item.Name), EChatType.CT_System, EChatLoc.CL_PopupWindow);
							return true;
						}
					case "respec_realm":
						{
							player.Inventory.RemoveCountFromStack(item, 1);
							InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.Template);
							player.RespecAmountRealmSkill++;
							player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.ReceiveItem.RespecRealm"), EChatType.CT_System, EChatLoc.CL_PopupWindow);
							return true;
						}
				}
			}
			return base.ReceiveItem(source, item);
		}

		public void PromotePlayer(GamePlayer player)
		{
			if (TrainedClass != EPlayerClass.Unknown)
				PromotePlayer(player, (int)TrainedClass, "", null);
		}
		
		/// <summary>
		/// Check if Player can be Promoted
		/// </summary>
		/// <param name="player"></param>
		public virtual bool CanPromotePlayer(GamePlayer player)
		{
			var baseClass = ScriptMgr.FindCharacterBaseClass((int)TrainedClass);
			IPlayerClass pickedClass = ScriptMgr.FindCharacterClass((int)TrainedClass);

			// Error or Base Trainer...
			if (baseClass == null || baseClass.ID == (int)TrainedClass)
				return false;
			
			if (player.Level < 5 || player.PlayerClass.ID != baseClass.ID)
				return false;
			
			if(pickedClass.EligibleRaces.Exists(s => (short)s.ID == player.Race))
				return false;
			
			if (GlobalConstants.CLASS_GENDER_CONSTRAINTS_DICT.ContainsKey(TrainedClass) && GlobalConstants.CLASS_GENDER_CONSTRAINTS_DICT[TrainedClass] != player.Gender)
				return false;
			    
			return true;
		}

		/// <summary>
		/// Called to promote a player
		/// </summary>
		/// <param name="player">the player to promote</param>
		/// <param name="classid">the new classid</param>
		/// <param name="messageToPlayer">the message for the player</param>
		/// <param name="gifts">Array of inventory items as promotion gifts</param>
		/// <returns>true if successfull</returns>
		public bool PromotePlayer(GamePlayer player, int classid, string messageToPlayer, DbInventoryItem[] gifts)
		{
			if (player == null)
				return false;

			// Player was promoted
			if (player.SetCharacterClass(classid))
			{
				player.styleComponent.RemoveAllStyles();
				player.RemoveAllAbilities();
				player.RemoveAllSpellLines();

				if (messageToPlayer != "")
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Says", this.Name, messageToPlayer), EChatType.CT_System, EChatLoc.CL_PopupWindow);
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Upgraded", player.PlayerClass.Name), EChatType.CT_Important, EChatLoc.CL_SystemWindow);

				player.PlayerClass.OnLevelUp(player, player.Level);
				player.RefreshSpecDependantSkills(true);
				player.StartPowerRegeneration();
				player.Out.SendUpdatePlayerSkills();
				player.Out.SendUpdatePlayer();
				// drop any non usable item
				CheckAbilityToUseItem(player);

				// Initiate equipment
				if (gifts != null && gifts.Length > 0)
				{
					for (int i = 0; i < gifts.Length; i++)
					{
						player.ReceiveItem(this, gifts[i]);
					}
				}

				// after gifts
				player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.PromotePlayer.Accepted", player.PlayerClass.Profession), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				player.SaveIntoDatabase();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Add a gift to the player
		/// </summary>
		/// <param name="template">the template ID of the item</param>
		/// <param name="player">the player to give it to</param>
		/// <returns>true if succesful</returns>
		public virtual bool addGift(String template, GamePlayer player)
		{
			DbItemTemplate temp = GameServer.Database.FindObjectByKey<DbItemTemplate>(template);
			if (temp != null)
			{
				if (!player.Inventory.AddTemplate(GameInventoryItem.Create(temp), 1, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
				{
					player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.AddGift.NotEnoughSpace"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return false;
				}
				InventoryLogging.LogInventoryAction(this, player, EInventoryActionType.Other, temp);
			}
			return true;
		}

		/// <summary>
		/// If we can't train champion levels then dismiss this player
		/// </summary>
		/// <param name="player"></param>
		protected virtual void CheckChampionTraining(GamePlayer player)
		{
			if (CanTrainChampionLevels(player) == false)
			{
				// Check for ambient trigger messages for the NPC in the 'MobXAmbientBehaviour' table
				var triggers = GameServer.Instance.NpcManager.AmbientBehaviour[base.Name];
				// If the NPC has no ambient trigger message assigned, then return this message
				if (triggers == null || triggers.Length == 0)
					SayTo(player, EChatLoc.CL_ChatWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.Train.SeekElsewhere"));
			}
		}

		/// <summary>
		/// Offer training to the player.
		/// </summary>
		/// <param name="player"></param>
		protected virtual void OfferTraining(GamePlayer player)
		{
			SayTo(player, EChatLoc.CL_ChatWindow, LanguageMgr.GetTranslation(player.Client.Account.Language, "GameTrainer.Train.WouldYouLikeTo"));
		}
		
		/// <summary>
		/// No trainer for disabled classes
		/// </summary>
		/// <returns></returns>
		public override bool AddToWorld()
		{
			if (!string.IsNullOrEmpty(ServerProperties.Properties.DISABLED_CLASSES))
			{
				if (disabled_classes == null)
				{
					// creation of disabled_classes list.
					disabled_classes = Util.SplitCSV(ServerProperties.Properties.DISABLED_CLASSES).ToList();
				}

				if (disabled_classes.Contains(TrainedClass.ToString()))
					return false;
			}
			return base.AddToWorld();
		}
	}
}