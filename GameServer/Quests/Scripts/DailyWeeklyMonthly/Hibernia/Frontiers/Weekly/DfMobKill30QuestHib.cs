using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using log4net;

namespace DOL.GS.WeeklyQuest.Hibernia
{
	public class DfMobKill30QuestHib : Quests.WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Weekly] Darkness Falls Invasion";
		private const int minimumLevel = 30;
		private const int maximumLevel = 50;
		
		// Kill Goal
		private const int MAX_KILLED = 200;

		private static GameNpc Stefano = null; // Start NPC

		private int _mobsKilled = 0;

		// Constructors
		public DfMobKill30QuestHib() : base()
		{
		}

		public DfMobKill30QuestHib(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DfMobKill30QuestHib(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DfMobKill30QuestHib(GamePlayer questingPlayer, DbQuests dbQuest) : base(questingPlayer, dbQuest)
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
			if (!ServerProperties.ServerProperties.LOAD_QUESTS)
				return;
			

			#region defineNPCs

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Stefano", ERealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 249 && npc.X == 46083 && npc.Y == 39681)
					{
						Stefano = npc;
						break;
					}

			if (Stefano == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Stefano , creating it ...");
				Stefano = new GameNpc();
				Stefano.Model = 306;
				Stefano.Name = "Stefano";
				Stefano.GuildName = "Realm Logistics";
				Stefano.Realm = ERealm.Hibernia;
				//Darkness Falls Hib Entrance Location
				Stefano.CurrentRegionID = 249;
				Stefano.Size = 50;
				Stefano.Level = 59;
				Stefano.X = 46083;
				Stefano.Y = 39681;
				Stefano.Z = 21357;
				Stefano.Heading = 3066;
				Stefano.Flags |= GameNpc.eFlags.PEACE;
				GameNpcInventoryTemplate templateHib = new GameNpcInventoryTemplate();
				templateHib.AddNPCEquipment(eInventorySlot.TorsoArmor, 734,0,0,3);
				templateHib.AddNPCEquipment(eInventorySlot.LegsArmor, 735);
				templateHib.AddNPCEquipment(eInventorySlot.ArmsArmor, 736);
				templateHib.AddNPCEquipment(eInventorySlot.HandsArmor, 737, 0,0,3);
				templateHib.AddNPCEquipment(eInventorySlot.FeetArmor, 738, 0, 0, 3);
				templateHib.AddNPCEquipment(eInventorySlot.Cloak, 678);
				Stefano.Inventory = templateHib.CloseTemplate();
				Stefano.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Stefano.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Stefano, GameObjectEvent.Interact, new CoreEventHandler(TalkToStefano));
			GameEventMgr.AddHandler(Stefano, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToStefano));

			Stefano.AddQuestToGive(typeof (DfMobKill30QuestHib));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" Hib initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Stefano == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Stefano, GameObjectEvent.Interact, new CoreEventHandler(TalkToStefano));
			GameEventMgr.RemoveHandler(Stefano, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToStefano));

			Stefano.RemoveQuestToGive(typeof (DfMobKill30QuestHib));
		}

		private static void TalkToStefano(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Stefano.CanGiveQuest(typeof (DfMobKill30QuestHib), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DfMobKill30QuestHib quest = player.IsDoingQuest(typeof (DfMobKill30QuestHib)) as DfMobKill30QuestHib;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Stefano.SayTo(player, "Head into Darkness Falls and slay monsters so they don\'t spread in our realm!");
							break;
						case 2:
							Stefano.SayTo(player, "Hello " + player.Name + ", did you [slay monsters] for your reward?");
							break;
					}
				}
				else
				{
					Stefano.SayTo(player, "Hello "+ player.Name +", I am Stefano. I have received word from a ranger that forces are building in Darkness Falls. "+
					                       "Clear out as many demons as you can find, and come back to me only when the halls of the dungeon are purged of their influence. \n\n"+
					                       "Can you [stop the invasion]?");
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
						case "stop the invasion":
							player.Out.SendQuestSubscribeCommand(Stefano, QuestMgr.GetIDForQuestType(typeof(DfMobKill30QuestHib)), "Will you help Stefano "+questTitle+"?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "slay monsters":
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
			if (player.IsDoingQuest(typeof (DfMobKill30QuestHib)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			//if (player.HasFinishedQuest(typeof(Academy_47)) == 0) return false;

			//if (!CheckPartAccessible(player,typeof(CityOfCamelot)))
			//	return false;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		public override void LoadQuestParameters()
		{
			_mobsKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, _mobsKilled.ToString());
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			DfMobKill30QuestHib quest = player.IsDoingQuest(typeof (DfMobKill30QuestHib)) as DfMobKill30QuestHib;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DfMobKill30QuestHib)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Stefano.CanGiveQuest(typeof (DfMobKill30QuestHib), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DfMobKill30QuestHib)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping our realm. That should buy us some time to plot a counterattack.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Stefano.GiveQuest(typeof (DfMobKill30QuestHib), player, 1))
					return;

				Stefano.SayTo(player, "Defend your realm, head into Darkness Falls and kill monsters for your reward.");

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
						return "Head into Darkness Falls and kill monsters for Hibernia. \nKilled: Monster (" +
						       _mobsKilled + " | " + MAX_KILLED + ")";
					case 2:
						return "Return to Stefano in Darkness Falls for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(DfMobKill30QuestHib)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
			
			if (gArgs.Target is GameSummonedPet)
				return;
			
			if (gArgs.Target.Realm != 0 || gArgs.Target is not GameNpc || gArgs.Target.CurrentRegionID != 249 ||
			    !(player.GetConLevel(gArgs.Target) > -2)) return;
			if (player.Group != null)
			{
				double minRequiredCon =Math.Ceiling((double) (player.Group.MemberCount / 3));
				if (minRequiredCon > 3) minRequiredCon = 3;
				if (player.Group.Leader.GetConLevel(gArgs.Target) >= minRequiredCon)
					_mobsKilled++;
				else
				{
					player.Out.SendMessage("[Weekly] Monsters Killed in Darkness Falls - needs a higher level monster to count", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
			}
			else
			{
				if (player.GetConLevel(gArgs.Target) > -1)
					_mobsKilled++;
				else
				{
					player.Out.SendMessage("[Weekly] Monsters Killed in Darkness Falls - needs a higher level monster to count", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					return;
				}
			}
			player.Out.SendMessage("[Weekly] Monsters Killed in Darkness Falls: ("+_mobsKilled+" | "+MAX_KILLED+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (_mobsKilled >= MAX_KILLED)
			{
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "DFMobKillQuestHib";
			set { ; }
		}

		public override void FinishQuest()
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel));
			m_questPlayer.AddMoney(MoneyUtil.GetMoney(0,0,m_questPlayer.Level * 5,32,UtilCollection.Random(50)), "You receive {0} as a reward.");
			RogMgr.GenerateReward(m_questPlayer, 1500);
			_mobsKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}