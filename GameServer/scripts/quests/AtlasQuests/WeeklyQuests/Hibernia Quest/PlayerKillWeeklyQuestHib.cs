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
using log4net;

namespace DOL.GS.WeeklyQuests.Hibernia
{
	public class PlayerKillWeeklyQuestHib : Quests.WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Weekly] Slay the enemies";
		protected const int minimumLevel = 1;
		protected const int maximumLevel = 50;

		private static GameNPC ReyHib = null; // Start NPC

		private int PlayersKilled = 0;
		
		// Kill Goal
		private static int MAX_KILLING_GOAL = 100;
		// Prevent Grey Killing
		private static int MIN_LEVEL = 35;

		// Constructors
		public PlayerKillWeeklyQuestHib() : base()
		{
		}

		public PlayerKillWeeklyQuestHib(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public PlayerKillWeeklyQuestHib(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public PlayerKillWeeklyQuestHib(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Rey", eRealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
				{
					if (npc.CurrentRegionID == 200 && npc.X == 334866 && npc.Y == 420749)
					{
						ReyHib = npc;
						break;
					}
				}

			if (ReyHib == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Rey , creating it ...");
				ReyHib = new GameNPC();
				ReyHib.Model = 26;
				ReyHib.Name = "Rey";
				ReyHib.GuildName = "Bone Collector";
				ReyHib.Realm = eRealm.Hibernia;
				//Druim Ligen Location
				ReyHib.CurrentRegionID = 200;
				ReyHib.Size = 60;
				ReyHib.Level = 59;
				ReyHib.X = 334866;
				ReyHib.Y = 420749;
				ReyHib.Z = 5184;
				ReyHib.Heading = 1640;
				ReyHib.Flags |= GameNPC.eFlags.PEACE;
				ReyHib.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					ReyHib.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(ReyHib, GameObjectEvent.Interact, new DOLEventHandler(TalkToRey));
			GameEventMgr.AddHandler(ReyHib, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToRey));

			/* Now we bring to Rey the possibility to give this quest to players */
			ReyHib.AddQuestToGive(typeof (PlayerKillWeeklyQuestHib));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (ReyHib == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(ReyHib, GameObjectEvent.Interact, new DOLEventHandler(TalkToRey));
			GameEventMgr.RemoveHandler(ReyHib, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToRey));

			/* Now we remove to Rey the possibility to give this quest to players */
			ReyHib.RemoveQuestToGive(typeof (PlayerKillWeeklyQuestHib));
		}

		protected static void TalkToRey(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(ReyHib.CanGiveQuest(typeof (PlayerKillWeeklyQuestHib), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			PlayerKillWeeklyQuestHib quest = player.IsDoingQuest(typeof (PlayerKillWeeklyQuestHib)) as PlayerKillWeeklyQuestHib;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							ReyHib.SayTo(player, "You will find suitable players in the frontiers or in battlegrounds.");
							break;
						case 2:
							ReyHib.SayTo(player, "Hello " + player.Name + ", did you [demolish some skulls]?");
							break;
					}
				}
				else
				{
					ReyHib.SayTo(player, "Uh oh, "+ player.Name +". "+
					                     "Fen put in a bulk order this time. There's no way I can collect this many bones in a week. \n" +
					                     "I need your help with this, are you up for some [bone harvesting]?");
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
						case "bone harvesting":
							player.Out.SendQuestSubscribeCommand(ReyHib, QuestMgr.GetIDForQuestType(typeof(PlayerKillWeeklyQuestHib)), "Will you undertake " + questTitle + "?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "demolish some skulls":
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
			if (player.IsDoingQuest(typeof (PlayerKillWeeklyQuestHib)) != null)
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
			PlayerKillWeeklyQuestHib quest = player.IsDoingQuest(typeof (PlayerKillWeeklyQuestHib)) as PlayerKillWeeklyQuestHib;

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

		protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(PlayerKillWeeklyQuestHib)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(ReyHib.CanGiveQuest(typeof (PlayerKillWeeklyQuestHib), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (PlayerKillWeeklyQuestHib)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping me.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!ReyHib.GiveQuest(typeof (PlayerKillWeeklyQuestHib), player, 1))
					return;

				ReyHib.SayTo(player, "You will find suitable players in the frontiers or in battlegrounds.");

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
						return "You will find suitable players in the frontiers or in battlegrounds. \nPlayers Killed: ("+ PlayersKilled +" | "+ MAX_KILLING_GOAL +")";
					case 2:
						return "Return to Rey in Druim Ligen for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(PlayerKillWeeklyQuestHib)) == null)
				return;

			if (sender != m_questPlayer)
				return;
			
			if (e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

				if (gArgs.Target.Realm != 0 && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && gArgs.Target.Level >= MIN_LEVEL) 
				{
					PlayersKilled++;
					player.Out.SendMessage("[Weekly] Enemy Killed: ("+PlayersKilled+" | "+MAX_KILLING_GOAL+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
					player.Out.SendQuestUpdate(this);
					
					if (PlayersKilled >= MAX_KILLING_GOAL)
					{
						// FinishQuest or go back to Rey
						Step = 2;
					}
				}
				
			}
		}
		
		public override string QuestPropertyKey
		{
			get => "PlayerKillWeeklyQuestHib";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			PlayersKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, PlayersKilled.ToString());
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/5, false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level * 10,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 1500);
			PlayersKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
