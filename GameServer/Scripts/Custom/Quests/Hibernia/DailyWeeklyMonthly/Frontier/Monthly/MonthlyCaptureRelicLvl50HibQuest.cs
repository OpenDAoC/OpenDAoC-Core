using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.Quests;
using log4net;

namespace Core.GS.MonthlyQuest.Hibernia
{
	public class MonthlyCaptureRelicLvl50HibQuest : Quests.MonthlyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Monthly] Aspiring Relic Guardian";
		private const int minimumLevel = 50;
		private const int maximumLevel = 50;

		// Capture Goal
		private const int MAX_CAPTURED = 1;
		
		private static GameNpc Kelteen = null; // Start NPC

		private int _isCaptured = 0;

		// Constructors
		public MonthlyCaptureRelicLvl50HibQuest() : base()
		{
		}

		public MonthlyCaptureRelicLvl50HibQuest(GamePlayer questingPlayer) : base(questingPlayer, 1)
		{
		}

		public MonthlyCaptureRelicLvl50HibQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public MonthlyCaptureRelicLvl50HibQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Kelteen", ERealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
				{
					if (npc.CurrentRegionID == 200 && npc.X == 334215 && npc.Y == 421276)
					{
						Kelteen = npc;
						break;
					}
				}

			if (Kelteen == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find KelteenHib, creating it ...");
				Kelteen = new GameNpc();
				Kelteen.Model = 315;
				Kelteen.Name = "Kelteen";
				Kelteen.GuildName = "Atlas Logistics";
				Kelteen.Realm = ERealm.Hibernia;
				//Druim Ligen Location
				Kelteen.CurrentRegionID = 200;
				Kelteen.Size = 60;
				Kelteen.Level = 59;
				Kelteen.X = 334215;
				Kelteen.Y = 421276;
				Kelteen.Z = 5180;
				Kelteen.Heading = 1566;
				Kelteen.Flags |= ENpcFlags.PEACE;
				GameNpcInventoryTemplate templateHib = new GameNpcInventoryTemplate();
				templateHib.AddNPCEquipment(EInventorySlot.Cloak, 1722);
				templateHib.AddNPCEquipment(EInventorySlot.HeadArmor, 1288);
				templateHib.AddNPCEquipment(EInventorySlot.TorsoArmor, 2517);
				templateHib.AddNPCEquipment(EInventorySlot.HandsArmor, 1645);
				templateHib.AddNPCEquipment(EInventorySlot.FeetArmor, 1643);
				templateHib.AddNPCEquipment(EInventorySlot.DistanceWeapon, 3239);
				Kelteen.Inventory = templateHib.CloseTemplate();
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
			Kelteen.AddQuestToGive(typeof (MonthlyCaptureRelicLvl50HibQuest));

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
			Kelteen.RemoveQuestToGive(typeof (MonthlyCaptureRelicLvl50HibQuest));
		}

		private static void TalkToKelteen(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Kelteen.CanGiveQuest(typeof (MonthlyCaptureRelicLvl50HibQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			MonthlyCaptureRelicLvl50HibQuest quest = player.IsDoingQuest(typeof (MonthlyCaptureRelicLvl50HibQuest)) as MonthlyCaptureRelicLvl50HibQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Kelteen.SayTo(player, "Encourage allies to conquer a relic keep in Albion or Midgard and return the relic to your realm.");
							break;
						case 2:
							Kelteen.SayTo(player, "Hello " + player.Name + ", did you [capture] a relic?");
							break;
					}
				}
				else
				{
					Kelteen.SayTo(player, "Hello " + player.Name +
					                    ", I am Kelteen. I serve the realm and its interests. \n" +
					                    "Our armies will be pushing the enemy relic keeps soon, and I need your assistance in [securing a foothold] for them.");
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
						case "securing a foothold":
							player.Out.SendQuestSubscribeCommand(Kelteen, QuestMgr.GetIDForQuestType(typeof(MonthlyCaptureRelicLvl50HibQuest)), "Will you help Kelteen "+questTitle+"");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "capture":
							if (quest.Step == 2)
							{
								player.Out.SendMessage("Thank you for your contribution!", EChatType.CT_Chat, EChatLoc.CL_PopupWindow);
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
			if (player.IsDoingQuest(typeof (MonthlyCaptureRelicLvl50HibQuest)) != null)
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
			MonthlyCaptureRelicLvl50HibQuest quest = player.IsDoingQuest(typeof (MonthlyCaptureRelicLvl50HibQuest)) as MonthlyCaptureRelicLvl50HibQuest;

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

		private static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(MonthlyCaptureRelicLvl50HibQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Kelteen.CanGiveQuest(typeof (MonthlyCaptureRelicLvl50HibQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (MonthlyCaptureRelicLvl50HibQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Hibernia.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Kelteen.GiveQuest(typeof (MonthlyCaptureRelicLvl50HibQuest), player, 1))
					return;

				Kelteen.SayTo(player, "Thank you "+player.Name+", you are a true guardian of Hibernia!");

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
						return "Encourage allies to conquer a relic keep in Albion or Midgard and return the relic to your realm. \nCaptured: Relic ("+ _isCaptured +" | "+MAX_CAPTURED+")";
					case 2:
						return "Return to Kelteen for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(MonthlyCaptureRelicLvl50HibQuest)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GamePlayerEvent.CapturedRelicsChanged) return;
			_isCaptured = 1;
			player.Out.SendMessage("[Monthly] Captured Relic: ("+_isCaptured+" | "+MAX_CAPTURED+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (_isCaptured >= MAX_CAPTURED)
			{
				// FinishQuest or go back to Dean
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "CaptureRelicQuestHib";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_isCaptured = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, _isCaptured.ToString());
		}

		public override void FinishQuest()
		{
			int reward = ServerProperties.Properties.MONTHLY_RVR_REWARD;
			
			if (m_questPlayer.Inventory.IsSlotsFree(3, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
			{
				m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel));
				m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level*8,0,Util.Random(50)), "You receive {0} as a reward.");
				CoreRoGMgr.GenerateReward(m_questPlayer, 5000);
				CoreRoGMgr.GenerateBeetleCarapace(m_questPlayer, 2);
				CoreRoGMgr.GenerateJewel(m_questPlayer, 51);
				_isCaptured = 0;
				
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
