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

namespace DOL.GS.DailyQuest.Midgard
{
	public class DFWeeklyKillQuestMid : Quests.WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Weekly] Vacuum Darkness Falls";
		protected const int minimumLevel = 15;
		protected const int maximumLevel = 50;
		
		// prevent grey killing
		protected const int MIN_PLAYER_CON = -3;
		// Kill Goal
		protected const int MAX_KILLED = 50;

		private static GameNPC Herou = null; // Start NPC

		private int EnemiesKilled = 0;

		// Constructors
		public DFWeeklyKillQuestMid() : base()
		{
		}

		public DFWeeklyKillQuestMid(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DFWeeklyKillQuestMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DFWeeklyKillQuestMid(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Herou", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 100 && npc.X == 766401 && npc.Y == 670349)
					{
						Herou = npc;
						break;
					}

			if (Herou == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Herou , creating it ...");
				Herou = new GameNPC();
				Herou.Model = 142;
				Herou.Name = "Herou";
				Herou.GuildName = "Realm Logistics";
				Herou.Realm = eRealm.Midgard;
				//Svasud Faste Location
				Herou.CurrentRegionID = 100;
				Herou.Size = 50;
				Herou.Level = 59;
				Herou.X = 766401;
				Herou.Y = 670349;
				Herou.Z = 5736;
				Herou.Heading = 2284;
				Herou.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Herou.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Herou, GameObjectEvent.Interact, new DOLEventHandler(TalkToHerou));
			GameEventMgr.AddHandler(Herou, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHerou));

			/* Now we bring to Herou the possibility to give this quest to players */
			Herou.AddQuestToGive(typeof (DFWeeklyKillQuestMid));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Herou == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Herou, GameObjectEvent.Interact, new DOLEventHandler(TalkToHerou));
			GameEventMgr.RemoveHandler(Herou, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHerou));

			/* Now we remove to Herou the possibility to give this quest to players */
			Herou.RemoveQuestToGive(typeof (DFWeeklyKillQuestMid));
		}

		protected static void TalkToHerou(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Herou.CanGiveQuest(typeof (DFWeeklyKillQuestMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DFWeeklyKillQuestMid quest = player.IsDoingQuest(typeof (DFWeeklyKillQuestMid)) as DFWeeklyKillQuestMid;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Herou.SayTo(player, "Please enter Darkness Falls and defend Midgard from enemies!");
							break;
						case 2:
							Herou.SayTo(player, "Hello " + player.Name + ", did you [defend Darkness Falls] for your reward?");
							break;
					}
				}
				else
				{
					Herou.SayTo(player, "Hello "+ player.Name +", I am Herou, do you need a task? "+
					                    "One of the scouts reported some enemy movement in Darkness Falls.  \n"+
					                    "We need you to [shut that down] before they get a foothold.");
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
						case "shut that down":
							player.Out.SendQuestSubscribeCommand(Herou, QuestMgr.GetIDForQuestType(typeof(DFWeeklyKillQuestMid)), "Will you help Herou "+questTitle+"?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "defend Darkness Falls":
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
			if (player.IsDoingQuest(typeof (DFWeeklyKillQuestMid)) != null)
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
			DFWeeklyKillQuestMid quest = player.IsDoingQuest(typeof (DFWeeklyKillQuestMid)) as DFWeeklyKillQuestMid;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DFWeeklyKillQuestMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Herou.CanGiveQuest(typeof (DFWeeklyKillQuestMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DFWeeklyKillQuestMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Atlas.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Herou.GiveQuest(typeof (DFWeeklyKillQuestMid), player, 1))
					return;

				Herou.SayTo(player, "Defend your realm in Darkness Falls and kill them for your reward.");

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
						return "Defend Midgard in Darkness Falls. \nKilled: Enemies ("+ EnemiesKilled +" | 50)";
					case 2:
						return "Return to Herou for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(DFWeeklyKillQuestMid)) == null)
				return;

			if (sender != m_questPlayer)
				return;
			
			if (Step == 1 && e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				//prevent grey killing
				
				
				if (gArgs.Target.Realm != 0 && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON && gArgs.Target.CurrentRegionID == 249) 
				{
					EnemiesKilled++;
					player.Out.SendMessage("[Weekly] Enemy Killed: ("+EnemiesKilled+" | "+MAX_KILLED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
					player.Out.SendQuestUpdate(this);
					
					if (EnemiesKilled >= MAX_KILLED)
					{
						// FinishQuest or go back to Herou
						Step = 2;
					}
				}
			}
			
		}
		
		public override string QuestPropertyKey
		{
			get => "DFWeeklyKillQuestMid";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			EnemiesKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, EnemiesKilled.ToString());
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel), false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level * 10,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 1500);
			EnemiesKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
