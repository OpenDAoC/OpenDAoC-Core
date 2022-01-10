/*
 * Atlas Custom Quest - Atlas 1.65v Classic Freeshard
 */
/*
*Author         : Kelt
*Editor			: Kelt
*Source         : Custom
*Date           : 20 December 2021
*Quest Name     : [Memorial] Play the last Song
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
using DOL.GS.PlayerTitles;
using log4net;

namespace DOL.GS.Quests.Midgard
{
	public class PlayTheLastSong : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Memorial] Play the last Song";
		protected const int minimumLevel = 1;
		protected const int maximumLevel = 50;
		
		private static GameNPC VikingDextz = null; // Start NPC
		private static GameNPC Freeya = null; // Finish NPC
		
		private static WorldObject FreeyasGrave = null; // Object

		private static IList<WorldObject> GetItems()
		{
			string FreeyasGrave = "Name = \"Freeya\'s Grave\"";
			
			return (GameServer.Database.SelectObjects<WorldObject>(FreeyasGrave));
		}

		// Constructors
		public PlayTheLastSong() : base()
		{
		}

		public PlayTheLastSong(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public PlayTheLastSong(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public PlayTheLastSong(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
		{
		}


		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;
			

			#region defineNPCs
			//Freeya
			GameNPC[] npcs = WorldMgr.GetNPCsByName("Freeya", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 100 && npc.X == 763734 && npc.Y == 646142)
					{
						Freeya = npc;
						break;
					}
			
			// Freeya is near Svasud Faste, North West on the Hill between trees
			if (Freeya == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find FreeyaMid , creating it ...");
				Freeya = new GameNPC();
				Freeya.Model = 165;
				Freeya.Name = "Freeya";
				Freeya.GuildName = "Thor Boyaux";
				Freeya.Realm = eRealm.Midgard;
				Freeya.CurrentRegionID = 100;
				Freeya.Flags += (ushort) GameNPC.eFlags.GHOST + (ushort) GameNPC.eFlags.PEACE;
				Freeya.Size = 50;
				Freeya.Level = 65;
				Freeya.X = 763734;
				Freeya.Y = 646142;
				Freeya.Z = 8687;
				Freeya.Heading = 60;
								
				GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
				template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 3341);
				template.AddNPCEquipment(eInventorySlot.TwoHandWeapon, 3342);
				template.AddNPCEquipment(eInventorySlot.Cloak, 326, 43);
				template.AddNPCEquipment(eInventorySlot.TorsoArmor, 771, 50);
				template.AddNPCEquipment(eInventorySlot.LegsArmor, 772);
				template.AddNPCEquipment(eInventorySlot.HandsArmor, 774, 50);
				template.AddNPCEquipment(eInventorySlot.ArmsArmor, 773);
				template.AddNPCEquipment(eInventorySlot.FeetArmor, 775, 50);
				template.AddNPCEquipment(eInventorySlot.HeadArmor, 1227);
				Freeya.Inventory = template.CloseTemplate();
				Freeya.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Freeya.SaveIntoDatabase();
				}
			}
			
			//Viking Dextz
			npcs = WorldMgr.GetNPCsByName("Viking Dextz", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 101 && npc.X == 30621 && npc.Y == 32310)
					{
						VikingDextz = npc;
						break;
					}
			
			// Viking Dextz is near Healer Trainers in Jordheim
			if (VikingDextz == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find VikingDextzMid , creating it ...");
				VikingDextz = new GameNPC();
				VikingDextz.Model = 187;
				VikingDextz.Name = "Viking Dextz";
				VikingDextz.GuildName = "Thor Boyaux";
				VikingDextz.Realm = eRealm.Midgard;
				VikingDextz.CurrentRegionID = 101;
				VikingDextz.Flags += (ushort) GameNPC.eFlags.PEACE;
				VikingDextz.Size = 52;
				VikingDextz.Level = 63;
				VikingDextz.X = 30621;
				VikingDextz.Y = 32310;
				VikingDextz.Z = 8305;
				VikingDextz.Heading = 3346;
				
				GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
				template.AddNPCEquipment(eInventorySlot.RightHandWeapon, 3335);
				template.AddNPCEquipment(eInventorySlot.LeftHandWeapon, 2218);
				template.AddNPCEquipment(eInventorySlot.Cloak, 677);
				template.AddNPCEquipment(eInventorySlot.TorsoArmor, 698, 50);
				template.AddNPCEquipment(eInventorySlot.LegsArmor, 699);
				template.AddNPCEquipment(eInventorySlot.HandsArmor, 701, 50);
				template.AddNPCEquipment(eInventorySlot.ArmsArmor, 700);
				template.AddNPCEquipment(eInventorySlot.FeetArmor, 702, 50);
				template.AddNPCEquipment(eInventorySlot.HeadArmor, 1227);
				VikingDextz.Inventory = template.CloseTemplate();
				VikingDextz.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					VikingDextz.SaveIntoDatabase();
				}
			}
			#endregion

			#region defineItems

			#endregion

			#region defineObject

			var graveCheck = GetItems();
			if (graveCheck.Count == 0)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Freeyas Grave, creating it ...");
				var FreeyasGrave = new WorldObject();
				FreeyasGrave.Name = "Freeya\'s Grave";
				FreeyasGrave.X = 763740;
				FreeyasGrave.Y = 646102;
				FreeyasGrave.Z = 8682;
				FreeyasGrave.Heading = 118;
				FreeyasGrave.Region = 100;
				FreeyasGrave.Model = 145;
				FreeyasGrave.ObjectId = "freeya_grave_questitem";
				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(FreeyasGrave);
					PlayTheLastSong.FreeyasGrave = FreeyasGrave;
				}

			}
			
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(VikingDextz, GameObjectEvent.Interact, new DOLEventHandler(TalkToVikingDextz));
			GameEventMgr.AddHandler(VikingDextz, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToVikingDextz));
			
			GameEventMgr.AddHandler(Freeya, GameObjectEvent.Interact, new DOLEventHandler(TalkToFreeya));
			GameEventMgr.AddHandler(Freeya, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToFreeya));
			
			/* Now we bring to NPC_Name the possibility to give this quest to players */
			VikingDextz.AddQuestToGive(typeof (PlayTheLastSong));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (VikingDextz == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
			
			GameEventMgr.RemoveHandler(VikingDextz, GameObjectEvent.Interact, new DOLEventHandler(TalkToVikingDextz));
			GameEventMgr.RemoveHandler(VikingDextz, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToVikingDextz));
			
			GameEventMgr.RemoveHandler(Freeya, GameObjectEvent.Interact, new DOLEventHandler(TalkToFreeya));
			GameEventMgr.RemoveHandler(Freeya, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToFreeya));

			/* Now we remove to NPC_Name the possibility to give this quest to players */
			VikingDextz.RemoveQuestToGive(typeof (PlayTheLastSong));
		}

		protected static void TalkToVikingDextz(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(VikingDextz.CanGiveQuest(typeof (PlayTheLastSong), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			PlayTheLastSong quest = player.IsDoingQuest(typeof (PlayTheLastSong)) as PlayTheLastSong;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							VikingDextz.SayTo(player, "God dag " +player.Name+ ", the mission is not that easy! Last year we lost a wonderful and helpful Skald. " +
							                          "He tried to help whole Midgard beating monsters, to become stronger to fight the enemy realms. " +
							                          "[Freeya] even helped enemies to beat monsters in Cruachan Gorge, he had a good soul.");
							break;
						case 2:
							VikingDextz.SayTo(player, player.Name +", you will find Freeya's Grave on the hill north west from Svasud Faste. Please check if everything is fine there!");
							break;
						case 3:
							VikingDextz.SayTo(player, "You are probably forsaken by all good spirits! You saw Freeya? " +
							                          "Please tell him, Thor Boyaux and Exiled Vaettir pay great respect for a legend of Midgard!\nRest in Peace my friend.");
							break;
					}
				}
				else
				{
					VikingDextz.SayTo(player, "Hello "+ player.Name +", I am Dextz. "+ 
					                          "I am expecting you could help me, which is a very dangerous task. However I cannot leave Jordheim, because I need to help new budding healers.\n" +
					                       "\nCan you [support Thor Boyaux] and check Freeya\'s Grave in Uppland?");
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
						case "support Thor Boyaux":
							player.Out.SendQuestSubscribeCommand(VikingDextz, QuestMgr.GetIDForQuestType(typeof(PlayTheLastSong)), "Will you help Viking Dextz ([Memorial] Play the last Song)?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "Freeya":
							VikingDextz.SayTo(player, "Freeya a Master Enforcer and a good friend, i miss him a lot.");
							if (quest.Step == 1)
							{
								VikingDextz.Emote(eEmote.Cry);
								VikingDextz.SayTo(player, "We buried him in Uppland on the hill, north west of Svasud Faste. Please [help me] and check if Freeya\'s Grave is fine and not broken." +
								                          "I currently can not leave my post, because i need to help new budding healers. \nYou would be a honorary " + player.Gender.ToString() + "!");
							}
							break;
						case "help me":
							if (quest.Step == 1)
							{
								VikingDextz.SayTo(player, "Thank you " + player.Name + ", thats very kind of you!");
								quest.Step = 2;
							}
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
					
				}
			}
		}
		
		protected static void TalkToFreeya(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;
			
			//We also check if the player is already doing the quest
			PlayTheLastSong quest = player.IsDoingQuest(typeof (PlayTheLastSong)) as PlayTheLastSong;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:

							break;
						case 2:

							break;
						case 3:

							break;
					}
				}
				else
				{

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

					}
				}
			}
			else if (e == GameLivingEvent.ReceiveItem)
			{
				ReceiveItemEventArgs rArgs = (ReceiveItemEventArgs) args;
				if (quest != null)
				{
					
				}
			}
		}
		
		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (PlayTheLastSong)) != null)
				return true;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			PlayTheLastSong quest = player.IsDoingQuest(typeof (PlayTheLastSong)) as PlayTheLastSong;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(PlayTheLastSong)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(VikingDextz.CanGiveQuest(typeof (PlayTheLastSong), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (PlayTheLastSong)) != null)
				return;

			if (response == 0x00)
			{
				
			}
			else
			{
				//Check if we can add the quest!
				if (!VikingDextz.GiveQuest(typeof (PlayTheLastSong), player, 1))
					return;
			}
		}

		//Set quest name
		public override string Name
		{
			get { return "[Memorial] Play the last Song"; }
		}

		// Define Steps
		public override string Description
		{
			get
			{
				switch (Step)
				{
					case 1:
						return "Speak with Viking Dextz to get more information.";
					case 2:
						return "Find Freeya's Grave in Uppland North West from Svasud Faste on the hill.\n" +
						       "(Loc: X:763717 Y:656265 Z:8679)";
					case 3:
						return "Help Freeya to play the last Songs. (/whisper \"last song\")";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player==null || player.IsDoingQuest(typeof (PlayTheLastSong)) == null)
				return;
		}
		
		public class PlayTheLastSongTitle : EventPlayerTitle 
		{
	        /// <summary>
	        /// The title description, shown in "Titles" window.
	        /// </summary>
	        /// <param name="player">The title owner.</param>
	        /// <returns>The title description.</returns>
	        public override string GetDescription(GamePlayer player)
	        {
	            return "Protected by Songs";
	        }

	        /// <summary>
	        /// The title value, shown over player's head.
	        /// </summary>
	        /// <param name="source">The player looking.</param>
	        /// <param name="player">The title owner.</param>
	        /// <returns>The title value.</returns>
	        public override string GetValue(GamePlayer source, GamePlayer player)
	        {
	            return "Protected by Songs";
	        }
			
	        /// <summary>
	        /// The event to hook.
	        /// </summary>
	        public override DOLEvent Event
	        {
	            get { return GamePlayerEvent.GameEntered; }
	        }
			
	        /// <summary>
	        /// Verify whether the player is suitable for this title.
	        /// </summary>
	        /// <param name="player">The player to check.</param>
	        /// <returns>true if the player is suitable for this title.</returns>
	        public override bool IsSuitable(GamePlayer player)
	        {
		        return player.HasFinishedQuest(typeof(PlayTheLastSong)) == 1;
	        }
			
	        /// <summary>
	        /// The event callback.
	        /// </summary>
	        /// <param name="e">The event fired.</param>
	        /// <param name="sender">The event sender.</param>
	        /// <param name="arguments">The event arguments.</param>
	        protected override void EventCallback(DOLEvent e, object sender, EventArgs arguments)
	        {
	            GamePlayer p = sender as GamePlayer;
	            if (p != null && p.Titles.Contains(this))
	            {
	                p.UpdateCurrentTitle();
	                return;
	            }
	            base.EventCallback(e, sender, arguments);
	        }
	    }

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			if (m_questPlayer.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
				m_questPlayer.GainExperience(eXPSource.Quest, 1768448, true);
				m_questPlayer.AddMoney(Money.GetMoney(0,0,2,32,Util.Random(50)), "You receive {0} as a reward.");

				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			}
			else
			{
				m_questPlayer.Out.SendMessage("You do not have enough free space in your inventory!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			}
		}
	}
}
