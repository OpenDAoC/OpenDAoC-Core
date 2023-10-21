using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.Quests;
using log4net;

namespace Core.GS.MonthlyQuest.Midgard
{
	public class MonthlyFrontiersLvl45MidQuest : Quests.MonthlyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Monthly] Bring Order and Security to Midgard";
		private const int minimumLevel = 45;
		private const int maximumLevel = 50;

		private static GameNpc Kelteen = null; // Start NPC

		private int PlayersKilled = 0;
		private int CapturedKeeps = 0;
		
		// Kill Goal
		private static int MAX_KILLING_GOAL = 500;
		private static int MAX_CAPTURED_KEEPS_GOAL = 20;
		
		// prevent grey killing
		private const int MIN_PLAYER_CON = -3;

		// Constructors
		public MonthlyFrontiersLvl45MidQuest() : base()
		{
		}

		public MonthlyFrontiersLvl45MidQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public MonthlyFrontiersLvl45MidQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public MonthlyFrontiersLvl45MidQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;

			#region defineNPCs

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Kelteen", ERealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
				{
					if (npc.CurrentRegionID == 100 && npc.X == 765463 && npc.Y == 670000)
					{
						Kelteen = npc;
						break;
					}
				}

			if (Kelteen == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find KelteenMid, creating it ...");
				Kelteen = new GameNpc();
				Kelteen.Model = 164;
				Kelteen.Name = "Kelteen";
				Kelteen.GuildName = "Atlas Logistics";
				Kelteen.Realm = ERealm.Midgard;
				//Svasud Faste Location
				Kelteen.CurrentRegionID = 100;
				Kelteen.Size = 60;
				Kelteen.Level = 59;
				Kelteen.X = 765463;
				Kelteen.Y = 670000;
				Kelteen.Z = 5736;
				Kelteen.Heading = 2245;
				Kelteen.Flags |= ENpcFlags.PEACE;
				GameNpcInventoryTemplate templateMid = new GameNpcInventoryTemplate();
				templateMid.AddNPCEquipment(EInventorySlot.Cloak, 1722);
				templateMid.AddNPCEquipment(EInventorySlot.HeadArmor, 1288);
				templateMid.AddNPCEquipment(EInventorySlot.TorsoArmor, 2517);
				templateMid.AddNPCEquipment(EInventorySlot.HandsArmor, 1645);
				templateMid.AddNPCEquipment(EInventorySlot.FeetArmor, 1643);
				templateMid.AddNPCEquipment(EInventorySlot.DistanceWeapon, 3239);
				Kelteen.Inventory = templateMid.CloseTemplate();
				Kelteen.VisibleActiveWeaponSlots = (byte) EInventorySlot.DistanceWeapon;
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Kelteen, GameObjectEvent.Interact, new CoreEventHandler(TalkToKelteen));
			GameEventMgr.AddHandler(Kelteen, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToKelteen));

			/* Now we bring to Kelteen the possibility to give this quest to players */
			Kelteen.AddQuestToGive(typeof (MonthlyFrontiersLvl45MidQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Kelteen == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Kelteen, GameObjectEvent.Interact, new CoreEventHandler(TalkToKelteen));
			GameEventMgr.RemoveHandler(Kelteen, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToKelteen));

			/* Now we remove to Kelteen the possibility to give this quest to players */
			Kelteen.RemoveQuestToGive(typeof (MonthlyFrontiersLvl45MidQuest));
		}

		private static void TalkToKelteen(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Kelteen.CanGiveQuest(typeof (MonthlyFrontiersLvl45MidQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			MonthlyFrontiersLvl45MidQuest quest = player.IsDoingQuest(typeof (MonthlyFrontiersLvl45MidQuest)) as MonthlyFrontiersLvl45MidQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Kelteen.SayTo(player, $"Hello {player.PlayerClass.Name}, you will find enemies in Albion, Hibernia or in our lands. " +
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
							player.Out.SendQuestSubscribeCommand(Kelteen, QuestMgr.GetIDForQuestType(typeof(MonthlyFrontiersLvl45MidQuest)), "Will you help "+Kelteen.Name+" to slay enemies and capture keeps? " + questTitle + "?");
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
								player.Out.SendMessage("Thank you for your help! Midgard will thank you for your contribution.", EChatType.CT_Chat, EChatLoc.CL_PopupWindow);
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
			if (player.IsDoingQuest(typeof (MonthlyFrontiersLvl45MidQuest)) != null)
				return true;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			MonthlyFrontiersLvl45MidQuest quest = player.IsDoingQuest(typeof (MonthlyFrontiersLvl45MidQuest)) as MonthlyFrontiersLvl45MidQuest;

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

		private static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(MonthlyFrontiersLvl45MidQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Kelteen.CanGiveQuest(typeof (MonthlyFrontiersLvl45MidQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (MonthlyFrontiersLvl45MidQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you that you decide to help.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Kelteen.GiveQuest(typeof (MonthlyFrontiersLvl45MidQuest), player, 1))
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
						return "Defend your realm!\nSlay enemies in the frontiers and capture Keeps for Midgard." +
						       "\nEnemies Killed: ("+ PlayersKilled +" | "+ MAX_KILLING_GOAL +")" +
						       "\nCaptured Keeps: ("+ CapturedKeeps + " | "+ MAX_CAPTURED_KEEPS_GOAL +")";
					case 2:
						return "Return to Kelteen in Svasud Faste for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(MonthlyFrontiersLvl45MidQuest)) == null)
				return;

			if (sender != m_questPlayer)
				return;
			
			if (e == GameLivingEvent.EnemyKilled && Step == 1 && PlayersKilled < MAX_KILLING_GOAL)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (gArgs.Target.Realm == 0 || gArgs.Target.Realm == player.Realm || gArgs.Target is not GamePlayer ||
				    !(player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON)) return;
				if (gArgs.Target.CurrentRegionID != 1 && gArgs.Target.CurrentRegionID != 100 && gArgs.Target.CurrentRegionID != 200)
				{
					player.Out.SendMessage("[Monthly] You need to find enemies in the old frontiers.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
				PlayersKilled++;
				player.Out.SendMessage("[Monthly] Enemies Killed: ("+PlayersKilled+" | "+MAX_KILLING_GOAL+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (e == GamePlayerEvent.CapturedKeepsChanged && Step == 1 && CapturedKeeps < MAX_CAPTURED_KEEPS_GOAL)
			{
				CapturedKeeps++;
				player.Out.SendMessage("[Monthly] Captured Keeps: ("+CapturedKeeps+" | "+MAX_CAPTURED_KEEPS_GOAL+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}

			if (PlayersKilled >= MAX_KILLING_GOAL && CapturedKeeps >= MAX_CAPTURED_KEEPS_GOAL)
			{
				Step = 2;
			}
		}
		
		public override string QuestPropertyKey
		{
			get => "FrontiersMonthlyQuestMid";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			PlayersKilled = GetCustomProperty("FrontiersMonthlyMidKill") != null ? int.Parse(GetCustomProperty("FrontiersMonthlyMidKill")) : 0;
			CapturedKeeps = GetCustomProperty("FrontiersMonthlyMidKeep") != null ? int.Parse(GetCustomProperty("FrontiersMonthlyMidKeep")) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty("FrontiersMonthlyMidKill", PlayersKilled.ToString());
			SetCustomProperty("FrontiersMonthlyMidKeep", CapturedKeeps.ToString());
		}

		public override void FinishQuest()
		{
			int reward = ServerProperties.Properties.MONTHLY_RVR_REWARD;
			
			if (m_questPlayer.Inventory.IsSlotsFree(3, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
			{
				m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel));
				m_questPlayer.AddMoney(MoneyMgr.GetMoney(0, 0, m_questPlayer.Level * 8, 32, Util.Random(50)),
					"You receive {0} as a reward.");
				CoreRoGMgr.GenerateReward(m_questPlayer, 5000);
				CoreRoGMgr.GenerateBeetleCarapace(m_questPlayer, 2);
				CoreRoGMgr.GenerateJewel(m_questPlayer, 51);
				PlayersKilled = 0;
				CapturedKeeps = 0;
				
				if (reward > 0)
				{
					m_questPlayer.Out.SendMessage($"You have been rewarded {reward} Realmpoints for finishing Monthly Quest.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
					m_questPlayer.GainRealmPoints(reward, false);
					m_questPlayer.Out.SendUpdatePlayer();
				}
				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			}
			else
			{
				m_questPlayer.Out.SendMessage("Clear three slots of your inventory for your reward", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}
	}
}
