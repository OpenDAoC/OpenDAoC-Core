/*
 * Atlas Custom Quest - Atlas 1.65v Classic Freeshard
 */
/*
*Author         : Kelt
*Editor			: Kelt, Clait
*Source         : Custom
*Date           : 20 December 2021
*Quest Name     : [Memorial] All in the gold
*Quest Classes  : all
*Quest Version  : v1.0
*
*Changes:
* 
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Quests.Albion
{
	public class HelpSirLukas : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Memorial] All in the Gold";
		protected const int minimumLevel = 1;
		protected const int maximumLevel = 50;

		private static GameNPC SirLukas = null; // Start NPC
		private static GameMerchant EllynWeyland = null; // Mob to kill

		private static ItemTemplate funeral_speech_scroll = null;
		private static ItemTemplate FlitzitinaBow = null;

		private static WorldObject FlitzitinasGrave = null;

		private IList<WorldObject> GetItems()
		{
			string FlitzitinaGrave = "Name = 'Flitzitina\'s Grave'";
			
			return (GameServer.Database.SelectObjects<WorldObject>(FlitzitinaGrave));
		}

		// Constructors
		public HelpSirLukas() : base()
		{
		}

		public HelpSirLukas(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public HelpSirLukas(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public HelpSirLukas(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
		{
		}


		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;
			

			#region defineNPCs

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Sir Lukas", eRealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 10 && npc.X == 30763 && npc.Y == 29908)
					{
						SirLukas = npc;
						break;
					}

			if (SirLukas == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find SirLukas , creating it ...");
				SirLukas = new GameNPC();
				SirLukas.Model = 33;
				SirLukas.Name = "Sir Lukas";
				SirLukas.GuildName = "Emissary of the King";
				SirLukas.Realm = eRealm.Albion;
				SirLukas.CurrentRegionID = 10;
				SirLukas.LoadEquipmentTemplateFromDatabase("SirLukas");
				SirLukas.Size = 52;
				SirLukas.Level = 55;
				SirLukas.X = 30763;
				SirLukas.Y = 29908;
				SirLukas.Z = 8000;
				SirLukas.Heading = 3083;
				SirLukas.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					SirLukas.SaveIntoDatabase();
				}
			}
			// end npc

			npcs = WorldMgr.GetNPCsByName("Ellyn Weyland", eRealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNPC merchant in npcs)
					if (merchant.CurrentRegionID == 1 && merchant.X == 561409 && merchant.Y == 509960)
					{
						EllynWeyland = (GameMerchant)merchant;
						break;
					}

			if (EllynWeyland == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find EllynWeyland , creating it ...");
				EllynWeyland = new GameMerchant();
				EllynWeyland.TradeItems = new MerchantTradeItems("00c1e711-8d1b-4b72-8012-932d940f2567");
				EllynWeyland.LoadEquipmentTemplateFromDatabase("AlbMerchantArmorStudded");
				EllynWeyland.Model = 38;
				EllynWeyland.Name = "Ellyn Weyland";
				EllynWeyland.GuildName = "Armor Merchant";
				EllynWeyland.Realm = eRealm.Albion;
				EllynWeyland.CurrentRegionID = 1;
				EllynWeyland.Size = 50;
				EllynWeyland.Level = 1;
				EllynWeyland.X = 561409;
				EllynWeyland.Y = 509960;
				EllynWeyland.Z = 2423;
				EllynWeyland.Heading = 115;
				EllynWeyland.Flags ^= GameNPC.eFlags.PEACE;
				EllynWeyland.MaxSpeedBase = 200;
				EllynWeyland.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					EllynWeyland.SaveIntoDatabase();
				}
			}
			// end npc

				#endregion

			#region defineItems

				funeral_speech_scroll = GameServer.Database.FindObjectByKey<ItemTemplate>("funeral_speech_flitzitina");
			if (funeral_speech_scroll == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Funeral Speech for Flitzitina, creating it ...");
				funeral_speech_scroll = new ItemTemplate();
				funeral_speech_scroll.Id_nb = "funeral_speech_flitzitina";
				funeral_speech_scroll.Name = "Funeral Speech for Flitzitina";
				funeral_speech_scroll.Level = 5;
				funeral_speech_scroll.Item_Type = 0;
				funeral_speech_scroll.Model = 498;
				funeral_speech_scroll.IsDropable = false;
				funeral_speech_scroll.IsTradable = false;
				funeral_speech_scroll.IsIndestructible = false;
				funeral_speech_scroll.IsPickable = false;
				funeral_speech_scroll.DPS_AF = 0;
				funeral_speech_scroll.SPD_ABS = 0;
				funeral_speech_scroll.Object_Type = 0;
				funeral_speech_scroll.Hand = 0;
				funeral_speech_scroll.Type_Damage = 0;
				funeral_speech_scroll.Quality = 100;
				funeral_speech_scroll.Weight = 1;
				funeral_speech_scroll.Description = "";
				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(funeral_speech_scroll);
				}

			}

			FlitzitinaBow = GameServer.Database.FindObjectByKey<ItemTemplate>("FlitzitinaBow");
			if (FlitzitinaBow == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Flitzitina Bow , creating it ...");
				FlitzitinaBow = new ItemTemplate();
				FlitzitinaBow.Id_nb = "FlitzitinaBow";
				FlitzitinaBow.Name = "Flitzitina\'s Bow";
				FlitzitinaBow.Level = 50;
				FlitzitinaBow.Item_Type = 40;
				FlitzitinaBow.Model = 3275;
				FlitzitinaBow.IsDropable = true;
				FlitzitinaBow.IsPickable = true;
				FlitzitinaBow.DPS_AF = 50;
				FlitzitinaBow.SPD_ABS = 0;
				FlitzitinaBow.Object_Type = 9;
				FlitzitinaBow.Quality = 100;
				FlitzitinaBow.Weight = 10;
				FlitzitinaBow.Bonus = 35;
				FlitzitinaBow.MaxCondition = 50000;
				FlitzitinaBow.MaxDurability = 50000;
				FlitzitinaBow.Condition = 50000;
				FlitzitinaBow.Durability = 50000;
				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(FlitzitinaBow);
				}

			} //end item

			//Item Descriptions End

			#endregion

			#region defineObject

			FlitzitinasGrave = GameServer.Database.FindObjectByKey<WorldObject>("Name = 'Flitzitina\'s Grave'");
			if (FlitzitinasGrave == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Flitzitinas Grave, creating it ...");
				FlitzitinasGrave = new WorldObject();
				FlitzitinasGrave.Name = "Flitzitina\'s Grave";
				FlitzitinasGrave.X = 505153;
				FlitzitinasGrave.Y = 496310;
				FlitzitinasGrave.Z = 2432;
				FlitzitinasGrave.Heading = 833;
				FlitzitinasGrave.Region = 1;
				FlitzitinasGrave.Model = 145;
				FlitzitinasGrave.ObjectId = "flitzitina_grave_questitem";
				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(FlitzitinasGrave);
				}

			}

			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(SirLukas, GameObjectEvent.Interact, new DOLEventHandler(TalkToSirLukas));
			GameEventMgr.AddHandler(SirLukas, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToSirLukas));
			
			GameEventMgr.AddHandler(EllynWeyland, GameObjectEvent.Interact, new DOLEventHandler(TalkToEllynWeyland));
			GameEventMgr.AddHandler(EllynWeyland, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToEllynWeyland));

			/* Now we bring to Sir Lukas the possibility to give this quest to players */
			SirLukas.AddQuestToGive(typeof (HelpSirLukas));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (SirLukas == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(SirLukas, GameObjectEvent.Interact, new DOLEventHandler(TalkToSirLukas));
			GameEventMgr.RemoveHandler(SirLukas, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToSirLukas));
			
			GameEventMgr.RemoveHandler(EllynWeyland, GameObjectEvent.Interact, new DOLEventHandler(TalkToEllynWeyland));
			GameEventMgr.RemoveHandler(EllynWeyland, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToEllynWeyland));

			/* Now we remove to Sir Lukas the possibility to give this quest to players */
			SirLukas.RemoveQuestToGive(typeof (HelpSirLukas));
		}

		protected static void TalkToSirLukas(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(SirLukas.CanGiveQuest(typeof (HelpSirLukas), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			HelpSirLukas quest = player.IsDoingQuest(typeof (HelpSirLukas)) as HelpSirLukas;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							SirLukas.SayTo(player, "You will find Ellyn Weyland in the forge of Cotswold, she has something for me, please go and get it for me!");
							break;
						case 2:
							SirLukas.SayTo(player, "Hey "+ player.Name +", good to see you. Do you have Ellyn Weylands [delivery]?");
							break;
						case 3:
							SirLukas.SayTo(player, "Thank you prospective protecter! \n " +
							                       "Flitzitina is my mother, she was a strong and protective scout, her Bow and Arrows were perfectly crafted. " +
							                       "Her eyes are like falcon eyes, her shots were precise and i am proud to be the son of such an incredible woman. " +
							                       "\nThank you for the delivery and I hope we will see you more often in Camelot! \n" +
							                       "I have one last request, please bring [this speech] to Vetusta Abbey, we will prepare a dignified funeral for her.");
							break;
						case 4:
							SirLukas.SayTo(player, "Hey "+ player.Name +", \nI want you to see the grave of my mother! Please bring the funeral speech to the grave in [Vetusta Abbey].");
							break;
					}
				}
				else
				{
					SirLukas.SayTo(player, "Hello "+ player.Name +",I am Sir Lukas and protector of Camelot and Albion. \n"+
					                       "I heard from your "+ player.CharacterClass.Name +" Trainer that you are ready to take on tasks from Camelot. \n"+
					                       "We are expecting a delivery from Ellyn Weyland in the Cotswold Forge, which has to be picked up. \n" +
					                       "\nCan you [support Camelot] and get this for us?");
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (quest == null)
				{
					switch (wArgs.Text)
					{
						case "support Camelot":
							player.Out.SendQuestSubscribeCommand(SirLukas, QuestMgr.GetIDForQuestType(typeof(HelpSirLukas)), "Will you help Sir Lukas [Memorial] All in the Gold]?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "this speech":
							GiveItem(player, funeral_speech_scroll);
							SirLukas.SayTo(player, "Thank you "+ player.Name +", that means a lot for me! Now go to [Vetusta Abbey].");
							break;
						case "Vetusta Abbey":
							SirLukas.SayTo(player, "Go to the North Gates of Camelot. You will find Vetusta Abbey near the gates!");
							if (quest.Step == 3)
							{
								quest.Step = 4;
							}
							break;
						case "delivery":
							SirLukas.SayTo(player, "Fantastic, please hand it to me!");
							RemoveItem(player, FlitzitinaBow);
							quest.Step = 3;
							break;
						case "abort":
							player.Out.SendCustomDialog("Do you really want to abort this quest, \nall items gained during quest will be lost?", new CustomDialogResponse(CheckPlayerAbortQuest));
							break;
					}
				}
			}
			else if (e == GameLivingEvent.ReceiveItem)
			{
				ReceiveItemEventArgs rArgs = (ReceiveItemEventArgs) args;
				if (quest != null)
				{
					if (rArgs.Item.Id_nb == FlitzitinaBow.Id_nb)
					{
						SirLukas.SayTo(player, "Thank you "+ player.Name +", this bow is clearly sad news for me and Camelot! \n");
						//quest.Step = 3;
					}
				}
			}
		}
		
		protected static void TalkToEllynWeyland(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			//We also check if the player is already doing the quest
			HelpSirLukas quest = player.IsDoingQuest(typeof (HelpSirLukas)) as HelpSirLukas;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							EllynWeyland.SayTo(player, "Hello "+ player.Name +",\n" +
							                           "I have sad news for Sir Lukas. This delivery is very important! " +
							                           "The bow is from Flitzitina, his mother. \n" +
							                           "I found it in Pennine Mountains near the merchant routes. \n" +
							                           "\nPlease get [her bow] and return to Sir Lukas.");
							break;
						case 2:
							EllynWeyland.SayTo(player, "Hey "+ player.Name +",\n did you hand the delivery to Sir Lukas? \nPlease do it, it is very important!");
							break;
						case 3:
							EllynWeyland.SayTo(player, "Hello Adventurer,\n" +
							                           "I heard you gave Sir Lukas the delivery. I know he will make a handsome grave for his mother!");
							break;
						case 4:
							EllynWeyland.SayTo(player, "Vetusta Abbey? I know this place, when I was a child, I played there with some pigs and with my friends.");
							break;
					}
				}
				else
				{
					EllynWeyland.SayTo(player, "Hello Adventurer,\nI sell many armor pieces, maybe you find something for your use.");
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (quest == null)
				{
					switch (wArgs.Text)
					{
						
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "her bow":
							EllynWeyland.SayTo(player, "Here is the bow, thank you for helping Camelot!");
							GiveItem(player, FlitzitinaBow);
							quest.Step = 2;
							break;
					}
				}
			}
		}

		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (HelpSirLukas)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			//if (player.HasFinishedQuest(typeof(Academy_47)) == 0) return false;

			//if (!CheckPartAccessible(player,typeof(CityOfCamelot)))
			//	return false;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			HelpSirLukas quest = player.IsDoingQuest(typeof (HelpSirLukas)) as HelpSirLukas;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and finish your work!");
			}
			else
			{
				SendSystemMessage(player, "Aborting Quest " + questTitle + ". You can start over again if you want.");
				quest.AbortQuest();
			}
		}

		protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(HelpSirLukas)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(SirLukas.CanGiveQuest(typeof (HelpSirLukas), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (HelpSirLukas)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Each arrow leaves a memory in your heart and the sum of those memories will make you shoot better every time.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!SirLukas.GiveQuest(typeof (HelpSirLukas), player, 1))
					return;

				SirLukas.SayTo(player, "Hello "+ player.Name +",I am Sir Lukas and protector of Camelot and Albion. \n"+
					"I heard from your "+ player.CharacterClass.Name +" Trainer that you are ready to take on tasks from Camelot. \n"+
					"We are expecting a delivery from Ellyn Weyland in the Cotswold Forge, which has to be picked up. \n" +
					"\nCan you support Camelot and get this for us?");
			}
		}

		//Set quest name
		public override string Name
		{
			get { return "[Memorial] All in the Gold"; }
		}

		// Define Steps
		public override string Description
		{
			get
			{
				switch (Step)
				{
					case 1:
						return "Find Ellyn Weyland in Cotswold inside the forge and get the delivery.";
					case 2:
						return "Return to Sir Lukas and give him the bow of Flitzitina!";
					case 3:
						return "Speak with Sir Lukas and find out where the grave will be placed!";
					case 4:
						return "Find Flitzitina\'s Grave in Vetusta Abbey near North Camelot Entrance.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player==null || player.IsDoingQuest(typeof (HelpSirLukas)) == null)
				return;

			if (Step == 1 && e == GameLivingEvent.Interact)
			{
				InteractEventArgs gArgs = (InteractEventArgs) args;
				if (gArgs.Source.Name == EllynWeyland.Name)
				{
					EllynWeyland.SayTo(player, "Hello "+ player.Name +", I have sad news for Sir Lukas. \n" +
					                           "This delivery is very important! The bow is from Flitzitina, his mother. \n" +
					                           "I found it in Pennine Mountains near the merchant routes. \n" +
					                           "Please get this and go back to him.");
					
					GiveItem(m_questPlayer, FlitzitinaBow);
					Step = 2;
					return;
				}
			}

			if (Step == 2 && e == GamePlayerEvent.ReceiveItem)
			{
				/*RecieveItemEventArgs gArgs = (RecieveItemEventArgs) args;
				if (gArgs.Target.Name == SirLukas.Name && gArgs.Item.Id_nb == FlitzitinaBow.Id_nb)
				{*/
					//SirLukas.SayTo(player, "Thank you "+ player.Name +", this bow is clearly sad news for me and Camelot!");
					Step = 3;
				//}
			}
			if (Step == 3 && e == GameLivingEvent.Interact)
			{
				
				InteractEventArgs gArgs = (InteractEventArgs) args;
				if (gArgs.Source.Name == SirLukas.Name)
				{
					GiveItem(m_questPlayer, funeral_speech_scroll);
					SirLukas.SayTo(player, "We will prepare a dignified funeral for her, please bring this speech to Vetusta Abbey!");
					Step = 4;
				}
			}

			if (Step == 4 && e == GameObjectEvent.Interact)
			{
				InteractEventArgs gArgs = (InteractEventArgs) args;
				if (GetItems() == null)
				{
					RemoveItem(player, funeral_speech_scroll);
					FinishQuest();
				}
			}

		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			RemoveItem(m_questPlayer, FlitzitinaBow, false);
			RemoveItem(m_questPlayer, funeral_speech_scroll, false);
		}

		public override void FinishQuest()
		{
			if (m_questPlayer.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...

				SirLukas.SayTo(m_questPlayer, "Thank you once again, knowing that you helped preserve the Albion heroine's history!");

				m_questPlayer.GainExperience(eXPSource.Quest, 1768448, true);
				m_questPlayer.AddMoney(Money.GetMoney(0,0,2,32,Util.Random(50)), "You recieve {0} as a reward.");		
			}
			else
			{
				m_questPlayer.Out.SendMessage("You do not have enough free space in your inventory!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			}
		}
	}
}
