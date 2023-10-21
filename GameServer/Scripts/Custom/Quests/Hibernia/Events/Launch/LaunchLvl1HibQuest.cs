﻿using System;
using System.Reflection;
using System.Text;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Enums;
using Core.GS.PacketHandler;
using Core.GS.Quests;
using log4net;

namespace Core.GS
{
	public class LaunchLvl1HibQuest : Quests.BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Event] Countdown To Liftoff";
		private const int minimumLevel = 1;
		private const int maximumLevel = 50;

		private static GameNpc ReyHib = null; // Start NPC

		private int PlayersKilled = 0;
		private int KeepsTaken = 0;
		private int RelicsTaken = 0;
		private int RealmPointsEarned = 0;
		private const int PLAYER_KILL_GOAL = 15;
		private const int KEEP_TAKE_GOAL = 1;
		private const int RELIC_CAPTURE_GOAL = 1;
		private const int REALM_POINT_GOAL = 10000;
		
		// prevent grey killing
		private const int MIN_PLAYER_CON = -3;

		// Constructors
		public LaunchLvl1HibQuest() : base()
		{
		}

		public LaunchLvl1HibQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public LaunchLvl1HibQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public LaunchLvl1HibQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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
			// disable the quest for launch
			return;
			
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;

			#region defineNPCs

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Rey", ERealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
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
				ReyHib = new GameNpc();
				ReyHib.Model = 26;
				ReyHib.Name = "Rey";
				ReyHib.GuildName = "Bone Collector";
				ReyHib.Realm = ERealm.Hibernia;
				//Druim Ligen Location
				ReyHib.CurrentRegionID = 200;
				ReyHib.Size = 60;
				ReyHib.Level = 59;
				ReyHib.X = 334866;
				ReyHib.Y = 420749;
				ReyHib.Z = 5184;
				ReyHib.Heading = 1640;
				ReyHib.Flags |= ENpcFlags.PEACE;
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(ReyHib, GameObjectEvent.Interact, new CoreEventHandler(TalkToRey));
			GameEventMgr.AddHandler(ReyHib, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToRey));

			/* Now we bring to Dean the possibility to give this quest to players */
			ReyHib.AddQuestToGive(typeof (LaunchLvl1HibQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (ReyHib == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(ReyHib, GameObjectEvent.Interact, new CoreEventHandler(TalkToRey));
			GameEventMgr.RemoveHandler(ReyHib, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToRey));

			/* Now we remove to Dean the possibility to give this quest to players */
			ReyHib.RemoveQuestToGive(typeof (LaunchLvl1HibQuest));
		}

		protected static void TalkToRey(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(ReyHib.CanGiveQuest(typeof (LaunchLvl1HibQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			LaunchLvl1HibQuest quest = player.IsDoingQuest(typeof (LaunchLvl1HibQuest)) as LaunchLvl1HibQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							ReyHib.SayTo(player, "Thanks, Astronaut. You will find suitable players in the frontiers.");
							break;
						case 2:
							ReyHib.SayTo(player, "Hello " + player.Name + ", [mayhem managed]?");
							break;
					}
				}
				else
				{
					ReyHib.SayTo(player, "Hello, "+ player.Name +". I need your help. Fen seems to be losing his mind, he keeps talking about his lunch... or was it a launch? "+
					                     "Maybe he's doing something with rockets. At any rate, [causing some mayhem] would probably serve as a nice distraction to shut him up for a bit.\n");
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
						case "causing some mayhem":
							player.Out.SendQuestSubscribeCommand(ReyHib, QuestMgr.GetIDForQuestType(typeof(LaunchLvl1HibQuest)), "Will you undertake " + questTitle + "?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "mayhem managed":
							if (quest.Step == 2)
							{
								player.Out.SendMessage("Nice, fixing all that damage should keep him busy for a few more weeks. Maybe I can finally get some sleep.", EChatType.CT_Chat, EChatLoc.CL_PopupWindow);
								quest.FinishQuest();
							}
							break;
						case "abort":
							player.Out.SendCustomDialog("Do you really want to abort this quest? \nAll items gained during quest will be lost", new CustomDialogResponse(CheckPlayerAbortQuest));
							break;
					}
				}
			}
		}
		
		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (LaunchLvl1HibQuest)) != null)
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
			LaunchLvl1HibQuest quest = player.IsDoingQuest(typeof (LaunchLvl1HibQuest)) as LaunchLvl1HibQuest;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and cause some mayhem!");
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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(LaunchLvl1HibQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(ReyHib.CanGiveQuest(typeof (LaunchLvl1HibQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (LaunchLvl1HibQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping me.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!ReyHib.GiveQuest(typeof (LaunchLvl1HibQuest), player, 1))
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
						StringBuilder sb = new StringBuilder();
						sb.Append($"Kill players, captures keeps and relics, and generally cause destruction in the frontiers.");
						if(PlayersKilled < PLAYER_KILL_GOAL)
							sb.Append("\nPlayers Killed: ("+ PlayersKilled +" | "+ PLAYER_KILL_GOAL +")");

						if (KeepsTaken < KEEP_TAKE_GOAL)
							sb.Append("\nKeeps Taken: ("+ KeepsTaken +" | "+ KEEP_TAKE_GOAL +")");
						
						if (RelicsTaken < RELIC_CAPTURE_GOAL)
							sb.Append("\nRelics Taken: ("+ RelicsTaken +" | "+ RELIC_CAPTURE_GOAL +")");
						
						if (RealmPointsEarned < REALM_POINT_GOAL)
							sb.Append("\nRealm Points Earned From Kills: ("+ RealmPointsEarned.ToString("N0") +" | "+ REALM_POINT_GOAL.ToString("N0") +")");
						
						return sb.ToString();
					case 2:
						return "Return to Rey in Druim Ligen for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(LaunchLvl1HibQuest)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

				if (gArgs.Target.Realm == 0 || gArgs.Target.Realm == player.Realm || gArgs.Target is not GamePlayer ||
				    !(player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON)) return;
				if(PlayersKilled < PLAYER_KILL_GOAL)
					PlayersKilled++;
				if (RealmPointsEarned < REALM_POINT_GOAL)
					RealmPointsEarned += gArgs.Target.RealmPointsValue;
				
				player.Out.SendMessage("[Event] Enemy Killed: (" + PlayersKilled + " | " + PLAYER_KILL_GOAL + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendMessage("[Event] Realm Points Gained: (" + RealmPointsEarned + " | " + REALM_POINT_GOAL.ToString("N0") + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}



			if (e == GamePlayerEvent.CapturedKeepsChanged)
			{
				if (KeepsTaken < KEEP_TAKE_GOAL)
					KeepsTaken++;
				player.Out.SendMessage("[Event] Keeps Taken: (" + KeepsTaken + " | " + KEEP_TAKE_GOAL + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			
			if (e == GamePlayerEvent.CapturedRelicsChanged)
			{
				if (RelicsTaken < RELIC_CAPTURE_GOAL)
					RelicsTaken++;
				player.Out.SendMessage("[Event] Relics Taken: (" + RelicsTaken + " | " + RELIC_CAPTURE_GOAL + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
					
			if (PlayersKilled >= PLAYER_KILL_GOAL
			    && KeepsTaken >= KEEP_TAKE_GOAL
			    && RelicsTaken >= RELIC_CAPTURE_GOAL
			    && RealmPointsEarned >= REALM_POINT_GOAL)
			{
				Step = 2;
			}
		}
		
		public string PlayersKilledKey
		{
			get => "EventLaunchQuest";
			set { ; }
		}
		
		public string RelicsTakenKey
		{
			get => "EventLaunchQuestRelicCap";
			set { ; }
		}
		
		public string KeepsTakenKey
		{
			get => "EventLaunchQuestKeeps";
			set { ; }
		}
		
		public string RPSPropertyKey
		{
			get => "EventLaunchQuestRealmPoints";
			set { ; }
		}
		
		public void LoadQuestParameters()
		{
			PlayersKilled = GetCustomProperty(PlayersKilledKey) != null ? int.Parse(GetCustomProperty(PlayersKilledKey)) : 0;
			KeepsTaken = GetCustomProperty(KeepsTakenKey) != null ? int.Parse(GetCustomProperty(KeepsTakenKey)) : 0;
			RelicsTaken = GetCustomProperty(RelicsTakenKey) != null ? int.Parse(GetCustomProperty(RelicsTakenKey)) : 0;
			RealmPointsEarned = GetCustomProperty(RPSPropertyKey) != null ? int.Parse(GetCustomProperty(RPSPropertyKey)) : 0;
		}

		public void SaveQuestParameters()
		{
			SetCustomProperty(PlayersKilledKey, PlayersKilled.ToString());
			SetCustomProperty(KeepsTakenKey, KeepsTaken.ToString());
			SetCustomProperty(RelicsTakenKey, RelicsTaken.ToString());
			SetCustomProperty(RPSPropertyKey, RealmPointsEarned.ToString());
		}


		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/5);
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level*2,32,Util.Random(50)), "You receive {0} as a reward.");
			CoreRoGMgr.GenerateReward(m_questPlayer, 15000);
			PlayersKilled = 0;
			KeepsTaken = 0;
			RelicsTaken = 0;
			RealmPointsEarned = 0;
			DbAccountXCustomParam eventParam = new DbAccountXCustomParam();
			eventParam.Name = m_questPlayer.AccountName;
			eventParam.KeyName = PlayersKilledKey;
			int realmInt = (int)m_questPlayer.Realm;
			eventParam.Value = realmInt.ToString();
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
