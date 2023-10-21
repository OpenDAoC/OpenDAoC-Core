using System;
using System.Reflection;
using Core.Database;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.Quests;
using log4net;

namespace Core.GS.WeeklyQuest.Albion
{
	public class WeeklyDragonLvl45AlbQuest : Quests.WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string DRAGON_NAME = "Golestandt";

		private const string questTitle = "[Weekly] Extinction of " + DRAGON_NAME;
		private const int minimumLevel = 45;
		private const int maximumLevel = 50;
		
		// Kill Goal
		private const int MAX_KILLED = 1;
		// Quest Counter
		private int DragonKilled = 0;
		
		private static GameNpc Hector = null; // Start NPC

		// Constructors
		public WeeklyDragonLvl45AlbQuest() : base()
		{
		}

		public WeeklyDragonLvl45AlbQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public WeeklyDragonLvl45AlbQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public WeeklyDragonLvl45AlbQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Hector", ERealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 1 && npc.X == 583860 && npc.Y == 477619)
					{
						Hector = npc;
						break;
					}

			if (Hector == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Hector , creating it ...");
				Hector = new GameNpc();
				Hector.Model = 716;
				Hector.Name = "Hector";
				Hector.GuildName = "Advisor to the King";
				Hector.Realm = ERealm.Albion;
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Hector, GameObjectEvent.Interact, new CoreEventHandler(TalkToHector));
			GameEventMgr.AddHandler(Hector, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToHector));

			/* Now we bring to Haszan the possibility to give this quest to players */
			Hector.AddQuestToGive(typeof (WeeklyDragonLvl45AlbQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Hector == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Hector, GameObjectEvent.Interact, new CoreEventHandler(TalkToHector));
			GameEventMgr.RemoveHandler(Hector, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToHector));

			/* Now we remove to Haszan the possibility to give this quest to players */
			Hector.RemoveQuestToGive(typeof (WeeklyDragonLvl45AlbQuest));
		}

		private static void TalkToHector(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Hector.CanGiveQuest(typeof (WeeklyDragonLvl45AlbQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			WeeklyDragonLvl45AlbQuest quest = player.IsDoingQuest(typeof (WeeklyDragonLvl45AlbQuest)) as WeeklyDragonLvl45AlbQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Hector.SayTo(player, player.Name + ", please travel to Dartmoor and kill the dragon for Albion!");
							break;
						case 2:
							Hector.SayTo(player, "Hello " + player.Name + ", did you [slay the dragon] and return for your reward?");
							break;
					}
				}
				else
				{
					Hector.SayTo(player, "Hello "+ player.Name +", I am Hector. I bring sad news today. " + DRAGON_NAME + " razed a small settlement in Dartmoor last night. \n" +
					                   "Please, help the king avenge their deaths and keep Albion safe from " + DRAGON_NAME +  "\'s influence. \n\n"+
					                   "Can you support Albion and [kill the dragon]?");
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
						case "kill the dragon":
							player.Out.SendQuestSubscribeCommand(Hector, QuestMgr.GetIDForQuestType(typeof(WeeklyDragonLvl45AlbQuest)), "Will you help Hector with "+questTitle+"?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "slay the dragon":
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
			if (player.IsDoingQuest(typeof (WeeklyDragonLvl45AlbQuest)) != null)
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
			WeeklyDragonLvl45AlbQuest quest = player.IsDoingQuest(typeof (WeeklyDragonLvl45AlbQuest)) as WeeklyDragonLvl45AlbQuest;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and scout the dragon!");
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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(WeeklyDragonLvl45AlbQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Hector.CanGiveQuest(typeof (WeeklyDragonLvl45AlbQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (WeeklyDragonLvl45AlbQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Albion.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Hector.GiveQuest(typeof (WeeklyDragonLvl45AlbQuest), player, 1))
					return;

				Hector.SayTo(player, "Please, find the dragon in Dartmoor and defend our realm.");

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
						return "Travel to Dartmoor and slay " + DRAGON_NAME + " for Albion. \nKilled: " + DRAGON_NAME + " ("+ DragonKilled +" | " + MAX_KILLED + ")";
					case 2:
						return "Return to Hector for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(WeeklyDragonLvl45AlbQuest)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Name.ToLower() != DRAGON_NAME.ToLower()) return;
			DragonKilled = 1;
			player.Out.SendMessage("[Weekly] You killed " + DRAGON_NAME + ": (" + DragonKilled + " | " + MAX_KILLED + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (DragonKilled >= MAX_KILLED)
			{
				// FinishQuest or go back to Haszan
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "DragonWeeklyQuestAlb";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			
		}

		public override void SaveQuestParameters()
		{
			
		}

		public override void FinishQuest()
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel));
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level * 5,32,Util.Random(50)), "You receive {0} as a reward.");
			CoreRoGMgr.GenerateReward(m_questPlayer, 1500);
			DragonKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
