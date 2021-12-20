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
		private static ItemTemplate SpiritmasterEpicBoots = null;

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

			SpiritmasterEpicBoots = GameServer.Database.FindObjectByKey<ItemTemplate>("SpiritmasterEpicBoots");
			if (SpiritmasterEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Spiritmaster Epic Boots , creating it ...");
				SpiritmasterEpicBoots = new ItemTemplate();
				SpiritmasterEpicBoots.Id_nb = "SpiritmasterEpicBoots";
				SpiritmasterEpicBoots.Name = "Spirit Touched Boots";
				SpiritmasterEpicBoots.Level = 50;
				SpiritmasterEpicBoots.Item_Type = 23;
				SpiritmasterEpicBoots.Model = 803;
				SpiritmasterEpicBoots.IsDropable = true;
				SpiritmasterEpicBoots.IsPickable = true;
				SpiritmasterEpicBoots.DPS_AF = 50;
				SpiritmasterEpicBoots.SPD_ABS = 0;
				SpiritmasterEpicBoots.Object_Type = 32;
				SpiritmasterEpicBoots.Quality = 100;
				SpiritmasterEpicBoots.Weight = 22;
				SpiritmasterEpicBoots.Bonus = 35;
				SpiritmasterEpicBoots.MaxCondition = 50000;
				SpiritmasterEpicBoots.MaxDurability = 50000;
				SpiritmasterEpicBoots.Condition = 50000;
				SpiritmasterEpicBoots.Durability = 50000;

				SpiritmasterEpicBoots.Bonus1 = 16;
				SpiritmasterEpicBoots.Bonus1Type = (int) eStat.CON;

				SpiritmasterEpicBoots.Bonus2 = 16;
				SpiritmasterEpicBoots.Bonus2Type = (int) eStat.DEX;

				SpiritmasterEpicBoots.Bonus3 = 8;
				SpiritmasterEpicBoots.Bonus3Type = (int) eResist.Matter;

				SpiritmasterEpicBoots.Bonus4 = 10;
				SpiritmasterEpicBoots.Bonus4Type = (int) eResist.Heat;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(SpiritmasterEpicBoots);
				}

			} //end item

			//Item Descriptions End

			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(SirLukas, GameObjectEvent.Interact, new DOLEventHandler(TalkToSirLukas));
			GameEventMgr.AddHandler(SirLukas, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToSirLukas));

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
							SirLukas.SayTo(player, "Hello "+ player.Name +",I am Sir Lukas and protector of Camelot and Albion.\n"+
							                       "I heard from your "+ player.CharacterClass.Name +" Trainer that you are ready to take on tasks from Camelot.\n"+
							                       "We are expecting a delivery from Ellyn Weyland in the Cotswold Forge, which has to be picked up.\n" +
							                       "\nCan you [support Camelot] and get this for us?");
							//SirLukas.SayTo(player, "Ellyn Weyland, an armor merchant, has a delivery for Camelot which is very important!\n" +
							//                       "Find her in the forge of Cotswold, get the delivery and return to me.");
							break;
						case 2:
							SirLukas.SayTo(player, "Hey "+ player.Name +", good to see you. Do you have Ellyn Weylands [delivery]?");
							quest.Step = 3;
							break;
						case 3:
							SirLukas.SayTo(player, "Thank you prospective protecter!\n " +
							                       "Flitzitina is my mother, she was a strong and protective scout, her Bow and Arrows were perfectly crafted. " +
							                       "Her eyes are like falcon eyes, her shots were precise and i am proud to be the son of such an incredible woman. " +
							                       "\nThank you for the delivery and I hope we will see you more often in Camelot!\n" +
							                       "I have one last request, please bring [this speech] to Vetusta Abbey, we will prepare a dignified funeral for her.");
							quest.Step = 4;
							break;
					}
				}
				else
				{
					SirLukas.SayTo(player, "");
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
						case "delivery":
							SirLukas.SayTo(player, "Fantastic, please hand it to me!");
							//SirLukas.ReceiveItem(flitzitina_bow);
							//quest.Step = 3;
							break;
						case "this speech":
							SirLukas.SayTo(player, "Thank you "+ player.Name +", that means a lot for me!");
							//player.ReceiveItem(player, funeral_speech_scroll);
							break;
						case "support Camelot":
							player.Out.SendQuestSubscribeCommand(SirLukas, QuestMgr.GetIDForQuestType(typeof(HelpSirLukas)), "Will you face Kelic [Mystic Level 50 Epic]?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "abort":
							player.Out.SendCustomDialog("Do you really want to abort this quest, \nall items gained during quest will be lost?", new CustomDialogResponse(CheckPlayerAbortQuest));
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

				SirLukas.SayTo(player, "Hello "+ player.Name +",I am Sir Lukas and protector of Camelot and Albion.\n"+
					"I heard from your "+ player.CharacterClass.Name +" Trainer that you are ready to take on tasks from Camelot.\n"+
					"We are expecting a delivery from Ellyn Weyland in the Cotswold Forge, which has to be picked up.\n" +
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
						return "[Step #1] Find Ellyn Weyland in Cotswold inside the forge and get the delivery.";
					case 2:
						return "[Step #2] Return to Sir Lukas and give him the bow of Flitzitina!";
					case 3:
						return "[Step #3] Speak with Sir Lukas and find out where the grave will be placed!";
					case 4:
						return "[Step #4] Find Flitzitina\'s Grave in Vetusta Abbey near North Camelot Entrance.";
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
					EllynWeyland.SayTo(player, "Ah, I can see how he wore the curse around the totem. I can now break the curse that is destroying the clan!");

					
					m_questPlayer.Out.SendMessage("Ellyn Weyland drops his Totem and you pick it up!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					GiveItem(m_questPlayer, funeral_speech_scroll);
					Step = 2;
					return;
				}
			}

			if (Step == 2 && e == GamePlayerEvent.GiveItem)
			{
				GiveItemEventArgs gArgs = (GiveItemEventArgs) args;
				if (gArgs.Target.Name == SirLukas.Name && gArgs.Item.Id_nb == funeral_speech_scroll.Id_nb)
				{
					RemoveItem(SirLukas, player, funeral_speech_scroll);
					SirLukas.SayTo(player, "Ah, I can see how he wore the curse around the totem. I can now break the curse that is destroying the clan!");
					SirLukas.SayTo(player, "The curse is broken and the clan is safe. They are in your debt, but I think Arnfinn, has come up with a suitable reward for you. There are six parts to it, so make sure you have room for them. Just let me know when you are ready, and then you can [take them] with our thanks!");
					Step = 3;
				}
			}

		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...

			RemoveItem(m_questPlayer, funeral_speech_scroll, false);
		}

		public override void FinishQuest()
		{
			if (m_questPlayer.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...

				SirLukas.SayTo(m_questPlayer, "May it serve you well, knowing that you have helped preserve the history of Midgard!");

				m_questPlayer.GainExperience(eXPSource.Quest, 1937768448, true);
				//m_questPlayer.AddMoney(Money.GetMoney(0,0,0,2,Util.Random(50)), "You recieve {0} as a reward.");		
			}
			else
			{
				m_questPlayer.Out.SendMessage("You do not have enough free space in your inventory!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			}
		}

		#region Allakhazam Epic Source

		/*
        *#25 talk to Inaksha
        *#26 seek out Loken in Raumarik Loc 47k, 25k, 4k, and kill him purp and 2 blue adds 
        *#27 return to Inaksha 
        *#28 give her the ball of flame
        *#29 talk with Inaksha about Loken�s demise
        *#30 go to Miri in Jordheim 
        *#31 give her the sealed pouch
        *#32 you get your epic armor as a reward
        */

		/*
Spirit Touched Boots 
Spirit Touched Cap 
Spirit Touched Gloves 
Spirit Touched Pants 
Spirit Touched Sleeves 
Spirit Touched Vest 
Raven-Rune Boots 
Raven-Rune Cap 
Raven-Rune Gloves 
Raven-Rune Pants 
Raven-Rune Sleeves 
Raven-Rune Vest 
Raven-boned Boots 
Raven-Boned Cap 
Raven-boned Gloves 
Raven-Boned Pants 
Raven-Boned Sleeves 
Bone-rune Vest 
        */

		#endregion
	}
}
