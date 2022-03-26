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

namespace DOL.GS.DailyQuest.Albion
{
	public class DanaoinKillQuestAlb : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Daily] Danaoin Invasion";
		protected const int minimumLevel = 40;
		protected const int maximumLevel = 50;

		// Kill Goal
		protected const int MAX_KILLED = 10;
		
		private static GameNPC Hector = null; // Start NPC

		private int danaoinKilled = 0;

		// Constructors
		public DanaoinKillQuestAlb() : base()
		{
		}

		public DanaoinKillQuestAlb(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DanaoinKillQuestAlb(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DanaoinKillQuestAlb(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Hector", eRealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 1 && npc.X == 583860 && npc.Y == 477619)
					{
						Hector = npc;
						break;
					}

			if (Hector == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Hector , creating it ...");
				Hector = new GameNPC();
				Hector.Model = 724;
				Hector.Name = "Hector";
				Hector.GuildName = "Advisor To The King";
				Hector.Realm = eRealm.Albion;
				Hector.CurrentRegionID = 1;
				Hector.Size = 50;
				Hector.Level = 59;
				//Castle Sauvage Location
				Hector.X = 583860;
				Hector.Y = 477619;
				Hector.Z = 2600;
				Hector.Heading = 3111;
				Hector.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Hector.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Hector, GameObjectEvent.Interact, new DOLEventHandler(TalkToHector));
			GameEventMgr.AddHandler(Hector, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHector));

			/* Now we bring to Hector the possibility to give this quest to players */
			Hector.AddQuestToGive(typeof (DanaoinKillQuestAlb));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Hector == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Hector, GameObjectEvent.Interact, new DOLEventHandler(TalkToHector));
			GameEventMgr.RemoveHandler(Hector, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHector));

			/* Now we remove to Hector the possibility to give this quest to players */
			Hector.RemoveQuestToGive(typeof (DanaoinKillQuestAlb));
		}

		protected static void TalkToHector(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Hector.CanGiveQuest(typeof (DanaoinKillQuestAlb), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DanaoinKillQuestAlb quest = player.IsDoingQuest(typeof (DanaoinKillQuestAlb)) as DanaoinKillQuestAlb;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Hector.SayTo(player, "You will find Danaoin Farmers in the North or West of Lyonesse.");
							break;
						case 2:
							Hector.SayTo(player, "Hello " + player.Name + ", did you [kill] the Danaoin Farmers?");
							break;
					}
				}
				else
				{
					Hector.SayTo(player, "Hello "+ player.Name +", I am Hector. "+
					                       "The Danaoin Farmers down in Lyonesse are overfarming the land and destroying Albion's natural resources.\n"+
					                       "\nCan you [clear the Danaoin] to save Albion?");
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
						case "clear the Danaoin":
							player.Out.SendQuestSubscribeCommand(Hector, QuestMgr.GetIDForQuestType(typeof(DanaoinKillQuestAlb)), "Will you help Hector "+questTitle+"");
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
			if (player.IsDoingQuest(typeof (DanaoinKillQuestAlb)) != null)
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
			danaoinKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, danaoinKilled.ToString());
		}


		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			DanaoinKillQuestAlb quest = player.IsDoingQuest(typeof (DanaoinKillQuestAlb)) as DanaoinKillQuestAlb;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DanaoinKillQuestAlb)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Hector.CanGiveQuest(typeof (DanaoinKillQuestAlb), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DanaoinKillQuestAlb)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Atlas.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Hector.GiveQuest(typeof (DanaoinKillQuestAlb), player, 1))
					return;

				Hector.SayTo(player, "You will find the Danaoin Farmers in Lyonesse.");

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
						return "Find Danaoin Farmers in the West or North in Lyonesse. \nKilled: Danaoin Farmers ("+ danaoinKilled +" | 10)";
					case 2:
						return "Return to Hector in Castle Sauvage for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(DanaoinKillQuestAlb)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;
			
			if (Step == 1 && e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (gArgs.Target.Name.ToLower() == "danaoin farmer") 
				{
					danaoinKilled++;
					player.Out.SendMessage("[Daily] Danaoin Farmers Killed: ("+danaoinKilled+" | "+MAX_KILLED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
					player.Out.SendQuestUpdate(this);
					
					if (danaoinKilled >= MAX_KILLED)
					{
						// FinishQuest or go back to Hector
						Step = 2;
					}
				}
			}
			
		}
		
		public override string QuestPropertyKey
		{
			get => "DanaoinKillQuestAlb";
			set { ; }
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/10, false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level,50,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 100);
			danaoinKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
