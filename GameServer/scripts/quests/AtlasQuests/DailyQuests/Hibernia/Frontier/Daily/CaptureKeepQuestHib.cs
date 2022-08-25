using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.API;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests;
using log4net;

namespace DOL.GS.DailyQuest.Hibernia
{
	public class CaptureKeepQuestHib : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Daily] Frontier Conquerer";
		private const int minimumLevel = 50;
		private const int maximumLevel = 50;

		// Capture Goal
		private const int MAX_CAPTURED = 1;
		
		private static GameNPC Cola = null; // Start NPC

		private int _isCaptured = 0;

		// Constructors
		public CaptureKeepQuestHib() : base()
		{
		}

		public CaptureKeepQuestHib(GamePlayer questingPlayer) : base(questingPlayer, 1)
		{
		}

		public CaptureKeepQuestHib(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public CaptureKeepQuestHib(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Cola", eRealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 200 && npc.X == 334793 && npc.Y == 420805)
					{
						Cola = npc;
						break;
					}

			if (Cola == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Cola , creating it ...");
				Cola = new GameNPC();
				Cola.Model = 583;
				Cola.Name = "Cola";
				Cola.GuildName = "Realm Logistics";
				Cola.Realm = eRealm.Hibernia;
				//Druim Ligen Location
				Cola.CurrentRegionID = 200;
				Cola.Size = 50;
				Cola.Level = 59;
				Cola.X = 334793;
				Cola.Y = 420805;
				Cola.Z = 5184;
				Cola.Heading = 1586;
				Cola.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Cola.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Cola, GameObjectEvent.Interact, new DOLEventHandler(TalkToCola));
			GameEventMgr.AddHandler(Cola, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToCola));

			/* Now we bring to Cola the possibility to give this quest to players */
			Cola.AddQuestToGive(typeof (CaptureKeepQuestHib));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Cola == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Cola, GameObjectEvent.Interact, new DOLEventHandler(TalkToCola));
			GameEventMgr.RemoveHandler(Cola, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToCola));

			/* Now we remove to Cola the possibility to give this quest to players */
			Cola.RemoveQuestToGive(typeof (CaptureKeepQuestHib));
		}

		private static void TalkToCola(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Cola.CanGiveQuest(typeof (CaptureKeepQuestHib), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			CaptureKeepQuestHib quest = player.IsDoingQuest(typeof (CaptureKeepQuestHib)) as CaptureKeepQuestHib;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Cola.SayTo(player, "Find an enemy occupied keep and capture it. If you succeed come back for your reward.");
							break;
						case 2:
							Cola.SayTo(player, "Hello " + player.Name + ", did you [capture] a keep?");
							break;
					}
				}
				else
				{
					Cola.SayTo(player, "Hello "+ player.Name +", I am Cola. I serve the realm and its interests. \n"+
					                     "Our armies will be pushing the frontier border soon, and I need your assistance in [securing a foothold] for them.");
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
							player.Out.SendQuestSubscribeCommand(Cola, QuestMgr.GetIDForQuestType(typeof(CaptureKeepQuestHib)), "Will you help Cola "+questTitle+"");
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
								player.Out.SendMessage("Thank you for your contribution!", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
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
			if (player.IsDoingQuest(typeof (CaptureKeepQuestHib)) != null)
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
			CaptureKeepQuestHib quest = player.IsDoingQuest(typeof (CaptureKeepQuestHib)) as CaptureKeepQuestHib;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and find us a keep.");
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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(CaptureKeepQuestHib)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Cola.CanGiveQuest(typeof (CaptureKeepQuestHib), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (CaptureKeepQuestHib)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Hibernia.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Cola.GiveQuest(typeof (CaptureKeepQuestHib), player, 1))
					return;

				Cola.SayTo(player, "Thank you "+player.Name+", you are a true soldier of Hibernia!");

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
						return "Go to the battlefield and conquer a keep. \nCaptured: Keep ("+ _isCaptured +" | 1)";
					case 2:
						return "Return to Cola for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(CaptureKeepQuestHib)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GamePlayerEvent.CapturedKeepsChanged) return;
			_isCaptured = 1;
			player.Out.SendMessage("[Daily] Captured Keep: ("+_isCaptured+" | "+MAX_CAPTURED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (_isCaptured >= MAX_CAPTURED)
			{
				// FinishQuest or go back to Dean
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "CaptureKeepQuestHib";
			set { ; }
		}
		public override void LoadQuestParameters()
		{
			
		}

		public override void SaveQuestParameters()
		{
			
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			int reward = ServerProperties.Properties.DAILY_RVR_REWARD;
			
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/5);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level*2,0,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 250);
			_isCaptured = 0;
			
			if (reward > 0)
			{
				m_questPlayer.Out.SendMessage($"You have been rewarded {reward} Realmpoints for finishing Daily Quest.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				m_questPlayer.GainRealmPoints(reward, false);
				m_questPlayer.Out.SendUpdatePlayer();
			}
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
