using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using log4net;

namespace DOL.GS.DailyQuest.Hibernia
{
	public class OctonidKill40QuestHib : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Daily] Octonid Invasion";
		private const int minimumLevel = 40;
		private const int maximumLevel = 50;

		// Kill Goal
		private const int MAX_KILLED = 10;
		
		private static GameNpc Anthony = null; // Start NPC

		private int OctonidKilled = 0;

		// Constructors
		public OctonidKill40QuestHib() : base()
		{
		}

		public OctonidKill40QuestHib(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public OctonidKill40QuestHib(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public OctonidKill40QuestHib(GamePlayer questingPlayer, DbQuests dbQuest) : base(questingPlayer, dbQuest)
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

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Anthony", ERealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 181 && npc.X == 422864 && npc.Y == 444362)
					{
						Anthony = npc;
						break;
					}

			if (Anthony == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Anthony , creating it ...");
				Anthony = new GameNpc();
				Anthony.Model = 289;
				Anthony.Name = "Anthony";
				Anthony.GuildName = "Advisor to the King";
				Anthony.Realm = ERealm.Hibernia;
				//Domnann Location
				Anthony.CurrentRegionID = 181;
				Anthony.Size = 50;
				Anthony.Level = 59;
				Anthony.X = 422864;
				Anthony.Y = 444362;
				Anthony.Z = 5952;
				Anthony.Heading = 1234;
				GameNpcInventoryTemplate templateHib = new GameNpcInventoryTemplate();
				templateHib.AddNPCEquipment(eInventorySlot.TorsoArmor, 1008);
				templateHib.AddNPCEquipment(eInventorySlot.HandsArmor, 361);
				templateHib.AddNPCEquipment(eInventorySlot.FeetArmor, 362);
				Anthony.Inventory = templateHib.CloseTemplate();
				Anthony.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Anthony.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Anthony, GameObjectEvent.Interact, new CoreEventHandler(TalkToAnthony));
			GameEventMgr.AddHandler(Anthony, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToAnthony));

			Anthony.AddQuestToGive(typeof (OctonidKill40QuestHib));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Anthony == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Anthony, GameObjectEvent.Interact, new CoreEventHandler(TalkToAnthony));
			GameEventMgr.RemoveHandler(Anthony, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToAnthony));

			Anthony.RemoveQuestToGive(typeof (OctonidKill40QuestHib));
		}

		private static void TalkToAnthony(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Anthony.CanGiveQuest(typeof (OctonidKill40QuestHib), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			OctonidKill40QuestHib quest = player.IsDoingQuest(typeof (OctonidKill40QuestHib)) as OctonidKill40QuestHib;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Anthony.SayTo(player, "You will find Octonids in the South East of World\'s End.");
							break;
						case 2:
							Anthony.SayTo(player, "Hello " + player.Name + ", did you [kill] the Octonids?");
							break;
					}
				}
				else
				{
					Anthony.SayTo(player, "Hello "+ player.Name +", I am Anthony. I help the king with logistics, and he's tasked me with getting things done around here. "+
					                   "The Octonids out in World's End are devouring the natural flora and fauna of the Shrouded Isles. They may soon destroy the ecosystem entirely.\n"+
					                   "\nCan you [clear the Octonids] to save the Shrouded Isles?");
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (quest == null)
				{
					switch (wArgs.Text.ToLower())
					{
						case "clear the octonids":
							player.Out.SendQuestSubscribeCommand(Anthony, QuestMgr.GetIDForQuestType(typeof(OctonidKill40QuestHib)), "Will you help Anthony "+questTitle+"");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "kill":
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
			if (player.IsDoingQuest(typeof (OctonidKill40QuestHib)) != null)
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
			OctonidKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, OctonidKilled.ToString());
		}


		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			OctonidKill40QuestHib quest = player.IsDoingQuest(typeof (OctonidKill40QuestHib)) as OctonidKill40QuestHib;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(OctonidKill40QuestHib)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Anthony.CanGiveQuest(typeof (OctonidKill40QuestHib), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (OctonidKill40QuestHib)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Atlas.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Anthony.GiveQuest(typeof (OctonidKill40QuestHib), player, 1))
					return;

				Anthony.SayTo(player, "You will find the Octonids in World\'s End.");

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
						return "Find Octonids South East in World\'s End. \nKilled: Octonids ("+ OctonidKilled +" | 10)";
					case 2:
						return "Return to Anthony in Grove of Domnann for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(OctonidKill40QuestHib)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
			if (gArgs.Target.Name.ToLower() != "octonid") return;
			OctonidKilled++;
			player.Out.SendMessage("[Daily] Octonid Killed: ("+OctonidKilled+" | "+MAX_KILLED+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (OctonidKilled >= MAX_KILLED)
			{
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "OctonidKillQuestHib";
			set { ; }
		}

		public override void FinishQuest()
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/10);
			m_questPlayer.AddMoney(MoneyUtil.GetMoney(0,0,m_questPlayer.Level,50,UtilCollection.Random(50)), "You receive {0} as a reward.");
			RogMgr.GenerateReward(m_questPlayer, 100);
			OctonidKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
