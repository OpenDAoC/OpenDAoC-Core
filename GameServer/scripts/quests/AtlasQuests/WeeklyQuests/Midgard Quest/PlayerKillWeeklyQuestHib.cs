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

namespace DOL.GS.WeeklyQuests.Midgard
{
	public class PlayerKillWeeklyQuestMid : Quests.WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Weekly] Slay the enemies";
		protected const int minimumLevel = 45;
		protected const int maximumLevel = 50;

		private static GameNPC ReyMid = null; // Start NPC

		private static int PlayersKilled = 0;
		
		// Kill Goal
		private static int MAX_KILLING_GOAL = 100;
		// Prevent Grey Killing
		private static int MIN_LEVEL = 35;

		// Constructors
		public PlayerKillWeeklyQuestMid() : base()
		{
		}

		public PlayerKillWeeklyQuestMid(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public PlayerKillWeeklyQuestMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public PlayerKillWeeklyQuestMid(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Rey", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
				{
					if (npc.CurrentRegionID == 100 && npc.X == 766551 && npc.Y == 670398)
					{
						ReyMid = npc;
						break;
					}
				}

			if (ReyMid == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Rey , creating it ...");
				ReyMid = new GameNPC();
				ReyMid.Model = 26;
				ReyMid.Name = "Rey";
				ReyMid.GuildName = "Bone Collector";
				ReyMid.Realm = eRealm.None;
				//Druim Ligen Location
				ReyMid.CurrentRegionID = 100;
				ReyMid.Size = 60;
				ReyMid.Level = 59;
				ReyMid.X = 766551;
				ReyMid.Y = 670398;
				ReyMid.Z = 5736;
				ReyMid.Heading = 2242;
				ReyMid.Flags |= GameNPC.eFlags.PEACE;
				ReyMid.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					ReyMid.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(ReyMid, GameObjectEvent.Interact, new DOLEventHandler(TalkToRey));
			GameEventMgr.AddHandler(ReyMid, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToRey));

			/* Now we bring to Rey the possibility to give this quest to players */
			ReyMid.AddQuestToGive(typeof (PlayerKillWeeklyQuestMid));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (ReyMid == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(ReyMid, GameObjectEvent.Interact, new DOLEventHandler(TalkToRey));
			GameEventMgr.RemoveHandler(ReyMid, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToRey));

			/* Now we remove to Rey the possibility to give this quest to players */
			ReyMid.RemoveQuestToGive(typeof (PlayerKillWeeklyQuestMid));
		}

		protected static void TalkToRey(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(ReyMid.CanGiveQuest(typeof (PlayerKillWeeklyQuestMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			PlayerKillWeeklyQuestMid quest = player.IsDoingQuest(typeof (PlayerKillWeeklyQuestMid)) as PlayerKillWeeklyQuestMid;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							ReyMid.SayTo(player, "You will find suitable players in the frontiers or in battlegrounds.");
							break;
						case 2:
							ReyMid.SayTo(player, "Hello " + player.Name + ", did you [demolish some skulls]?");
							break;
					}
				}
				else
				{
					ReyMid.SayTo(player, "Hello "+ player.Name +", I am Rey, Fen's Slave. "+
					                     "Fen's insatiable desire to kill players is getting out of hand, and he's starting to outsource. \n\n"+
					                     "\nAre you strong enough to [kill some enemies]?");
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
						case "kill some enemies":
							player.Out.SendQuestSubscribeCommand(ReyMid, QuestMgr.GetIDForQuestType(typeof(PlayerKillWeeklyQuestMid)), "Will you undertake " + questTitle + "?");
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
			if (player.IsDoingQuest(typeof (PlayerKillWeeklyQuestMid)) != null)
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
			PlayerKillWeeklyQuestMid quest = player.IsDoingQuest(typeof (PlayerKillWeeklyQuestMid)) as PlayerKillWeeklyQuestMid;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(PlayerKillWeeklyQuestMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(ReyMid.CanGiveQuest(typeof (PlayerKillWeeklyQuestMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (PlayerKillWeeklyQuestMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping me.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!ReyMid.GiveQuest(typeof (PlayerKillWeeklyQuestMid), player, 1))
					return;
				PlayersKilled = 0;

				ReyMid.SayTo(player, "You will find suitable players in the frontiers or in battlegrounds.");

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
						return "Return to Rey in Svasud Faste for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(PlayerKillWeeklyQuestMid)) == null)
				return;

			if (Step == 1 && e == GameLivingEvent.Interact)
			{
				InteractEventArgs gArgs = (InteractEventArgs) args;
				if (gArgs.Source.Name == ReyMid.Name)
				{
					ReyMid.SayTo(player, "Did you know that Fen is awesome? He pays me 50g every time I say that.");
					return;
				}
			}

			
			
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

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/2, true);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,1,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 5000);
			PlayersKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
