using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.API;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests;
using DOL.GS.WeeklyQuests.Albion;
using log4net;

namespace DOL.GS.MonthlyQuest.Albion
{
	public class FrontiersMonthlyQuestAlb : Quests.MonthlyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Monthly] Bring Order and Security to Albion";
		private const int minimumLevel = 45;
		private const int maximumLevel = 50;

		private static GameNPC Kelteen = null; // Start NPC

		private int PlayersKilled = 0;
		private int CapturedKeeps = 0;
		
		// Kill Goal
		private static int MAX_KILLING_GOAL = 500;
		private static int MAX_CAPTURED_KEEPS_GOAL = 20;
		
		// prevent grey killing
		private const int MIN_PLAYER_CON = -3;

		// Constructors
		public FrontiersMonthlyQuestAlb() : base()
		{
		}

		public FrontiersMonthlyQuestAlb(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public FrontiersMonthlyQuestAlb(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public FrontiersMonthlyQuestAlb(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
		{
		}

		public override int Level
		{
			get
			{
				// Quest Level
				return minimumLevel;
			}
		}
		
		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;

			#region defineNPCs

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Kelteen", eRealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
				{
					if (npc.CurrentRegionID == 1 && npc.X == 584592 && npc.Y == 476805)
					{
						Kelteen = npc;
						break;
					}
				}

			if (Kelteen == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find KelteenAlb, creating it ...");
				Kelteen = new GameNPC();
				Kelteen.Model = 37;
				Kelteen.Name = "Kelteen";
				Kelteen.GuildName = "Atlas Logistics";
				Kelteen.Realm = eRealm.Albion;
				//Castle Sauvage Location
				Kelteen.CurrentRegionID = 1;
				Kelteen.Size = 60;
				Kelteen.Level = 59;
				Kelteen.X = 584592;
				Kelteen.Y = 476805;
				Kelteen.Z = 2600;
				Kelteen.Heading = 4066;
				Kelteen.Flags |= GameNPC.eFlags.PEACE;
				GameNpcInventoryTemplate templateAlb = new GameNpcInventoryTemplate();
				templateAlb.AddNPCEquipment(eInventorySlot.Cloak, 1722);
				templateAlb.AddNPCEquipment(eInventorySlot.HeadArmor, 1288);
				templateAlb.AddNPCEquipment(eInventorySlot.TorsoArmor, 2517);
				templateAlb.AddNPCEquipment(eInventorySlot.HandsArmor, 1645);
				templateAlb.AddNPCEquipment(eInventorySlot.FeetArmor, 1643);
				templateAlb.AddNPCEquipment(eInventorySlot.DistanceWeapon, 3239);
				Kelteen.Inventory = templateAlb.CloseTemplate();
				Kelteen.VisibleActiveWeaponSlots = (byte) eInventorySlot.DistanceWeapon;
				Kelteen.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Kelteen.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Kelteen, GameObjectEvent.Interact, new DOLEventHandler(TalkToKelteen));
			GameEventMgr.AddHandler(Kelteen, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToKelteen));

			/* Now we bring to Kelteen the possibility to give this quest to players */
			Kelteen.AddQuestToGive(typeof (FrontiersMonthlyQuestAlb));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Kelteen == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Kelteen, GameObjectEvent.Interact, new DOLEventHandler(TalkToKelteen));
			GameEventMgr.RemoveHandler(Kelteen, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToKelteen));

			/* Now we remove to Kelteen the possibility to give this quest to players */
			Kelteen.RemoveQuestToGive(typeof (FrontiersMonthlyQuestAlb));
		}

		private static void TalkToKelteen(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Kelteen.CanGiveQuest(typeof (FrontiersMonthlyQuestAlb), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			FrontiersMonthlyQuestAlb quest = player.IsDoingQuest(typeof (FrontiersMonthlyQuestAlb)) as FrontiersMonthlyQuestAlb;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Kelteen.SayTo(player, $"Hello ${player.CharacterClass.Name}, you will find enemies in Midgard, Hibernia or in our lands. " +
							                      $"Come back when you have killed enough enemies and taken keeps for our safety.");
							break;
						case 2:
							Kelteen.SayTo(player, "Hello " + player.Name + ", did you success [capturing keeps and killing enemies]?");
							break;
					}
				}
				else
				{
					Kelteen.SayTo(player, "Oh Hey, "+ player.Name +". "+
					                      "Can I steal a brief moment of your time and tell you something? " +
					                      "Enemies have invaded our lands and we need everyone to help us defeat them and restore [order and security] to our realm.");
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
						case "order and security":
							player.Out.SendQuestSubscribeCommand(Kelteen, QuestMgr.GetIDForQuestType(typeof(FrontiersMonthlyQuestAlb)), "Will you help "+Kelteen.Name+" to slay enemies and capture keeps? " + questTitle + "?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "capturing keeps and killing enemies":
							if (quest.Step == 2)
							{
								player.Out.SendMessage("Thank you for your help! Albion will thank you for your contribution.", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
								quest.FinishQuest();
							}
							break;
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
			if (player.IsDoingQuest(typeof (FrontiersMonthlyQuestAlb)) != null)
				return true;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			FrontiersMonthlyQuestAlb quest = player.IsDoingQuest(typeof (FrontiersMonthlyQuestAlb)) as FrontiersMonthlyQuestAlb;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and shed some blood!");
			}
			else
			{
				SendSystemMessage(player, "Aborting Quest " + questTitle + ". You can start over again if you want.");
				quest.AbortQuest();
			}
		}

		private static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(FrontiersMonthlyQuestAlb)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Kelteen.CanGiveQuest(typeof (FrontiersMonthlyQuestAlb), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (FrontiersMonthlyQuestAlb)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you that you decide to help.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Kelteen.GiveQuest(typeof (FrontiersMonthlyQuestAlb), player, 1))
					return;

				Kelteen.SayTo(player, "You will find suitable players in the old frontiers.");

			}
		}

		//Set quest name
		public override string Name
		{
			get { return questTitle; }
		}

		// Define Steps
		public override string Description
		{
			get
			{
				switch (Step)
				{
					case 1:
						return "Defend your realm!\nSlay enemies in the frontiers and capture Keeps for Albion." +
						       "\nEnemies Killed: ("+ PlayersKilled +" | "+ MAX_KILLING_GOAL +")" +
						       "\nCaptured Keeps: ("+ CapturedKeeps + " | "+ MAX_CAPTURED_KEEPS_GOAL +")";
					case 2:
						return "Return to Kelteen in Castle Sauvage for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(FrontiersMonthlyQuestAlb)) == null)
				return;

			if (sender != m_questPlayer)
				return;
			
			
			if (e == GameLivingEvent.EnemyKilled && Step == 1 && PlayersKilled != MAX_KILLING_GOAL)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (gArgs.Target.Realm == 0 || gArgs.Target.Realm == player.Realm || gArgs.Target is not GamePlayer ||
				    !(player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON)) return;
				if (gArgs.Target.CurrentRegionID != 100 || gArgs.Target.CurrentRegionID != 200 ||
				    gArgs.Target.CurrentRegionID != 1)
				{
					player.Out.SendMessage("[Monthly] You need to find enemies in the old frontiers.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}
				PlayersKilled++;
				player.Out.SendMessage("[Monthly] Enemies Killed: ("+PlayersKilled+" | "+MAX_KILLING_GOAL+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			if (e == GamePlayerEvent.CapturedKeepsChanged && Step == 1 && CapturedKeeps != MAX_CAPTURED_KEEPS_GOAL)
			{
				CapturedKeeps++;
				player.Out.SendMessage("[Monthly] Captured Keeps: ("+CapturedKeeps+" | "+MAX_CAPTURED_KEEPS_GOAL+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}

			if (PlayersKilled >= MAX_KILLING_GOAL && CapturedKeeps >= MAX_CAPTURED_KEEPS_GOAL)
			{
				Step = 2;
			}
		}
		
		public override string QuestPropertyKey
		{
			get => "FrontiersMonthlyQuestAlb";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			PlayersKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
			CapturedKeeps = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, PlayersKilled.ToString());
			SetCustomProperty(QuestPropertyKey, CapturedKeeps.ToString());
		}

		public override void FinishQuest()
		{
			if (m_questPlayer.Inventory.IsSlotsFree(3, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
				m_questPlayer.GainExperience(eXPSource.Quest,
					(m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel), false);
				m_questPlayer.AddMoney(Money.GetMoney(0, 0, m_questPlayer.Level * 8, 32, Util.Random(50)),
					"You receive {0} as a reward.");
				AtlasROGManager.GenerateOrbAmount(m_questPlayer, 3000);
				AtlasROGManager.GenerateBeetleCarapace(m_questPlayer, 2);
				AtlasROGManager.GenerateJewel(m_questPlayer, 50);
				PlayersKilled = 0;
				CapturedKeeps = 0;
				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			}
			else
			{
				m_questPlayer.Out.SendMessage("Clear three slots of your inventory for your reward", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
	}
}
