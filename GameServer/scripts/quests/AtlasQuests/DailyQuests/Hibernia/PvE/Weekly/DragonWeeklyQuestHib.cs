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

namespace DOL.GS.WeeklyQuest.Hibernia
{
	public class DragonWeeklyQuestHib : Quests.WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		private const string DRAGON_NAME = "Cuuldurach the Glimmer King";

		private const string questTitle = "[Weekly] Extinction of " + DRAGON_NAME;
		private const int minimumLevel = 45;
		private const int maximumLevel = 50;
		
		// Kill Goal
		private const int MAX_KILLED = 1;
		// Quest Counter
		private int DragonKilled = 0;
		
		private static GameNPC Dean = null; // Start NPC

		// Constructors
		public DragonWeeklyQuestHib() : base()
		{
		}

		public DragonWeeklyQuestHib(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DragonWeeklyQuestHib(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DragonWeeklyQuestHib(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Dean", eRealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 200 && npc.X == 334962 && npc.Y == 420687)
					{
						Dean = npc;
						break;
					}

			if (Dean == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Dean , creating it ...");
				Dean = new GameNPC();
				Dean.Model = 355;
				Dean.Name = "Dean";
				Dean.GuildName = "Advisor to the King";
				Dean.Realm = eRealm.Hibernia;
				//Druim Ligen Location
				Dean.CurrentRegionID = 200;
				Dean.Size = 50;
				Dean.Level = 59;
				Dean.X = 334962;
				Dean.Y = 420687;
				Dean.Z = 5184;
				Dean.Heading = 1571;
				Dean.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Dean.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Dean, GameObjectEvent.Interact, new DOLEventHandler(TalkToDean));
			GameEventMgr.AddHandler(Dean, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToDean));

			/* Now we bring to Dean the possibility to give this quest to players */
			Dean.AddQuestToGive(typeof (DragonWeeklyQuestHib));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Dean == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Dean, GameObjectEvent.Interact, new DOLEventHandler(TalkToDean));
			GameEventMgr.RemoveHandler(Dean, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToDean));

			/* Now we remove to Dean the possibility to give this quest to players */
			Dean.RemoveQuestToGive(typeof (DragonWeeklyQuestHib));
		}

		private static void TalkToDean(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Dean.CanGiveQuest(typeof (DragonWeeklyQuestHib), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DragonWeeklyQuestHib quest = player.IsDoingQuest(typeof (DragonWeeklyQuestHib)) as DragonWeeklyQuestHib;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Dean.SayTo(player, player.Name + ", please travel to Sheeroe Hills and kill the dragon for Hibernia!");
							break;
						case 2:
							Dean.SayTo(player, "Hello " + player.Name + ", did you [slay the dragon] and return for your reward?");
							break;
					}
				}
				else
				{
					Dean.SayTo(player, "Hello "+ player.Name +", I am Dean. I bring sad news today. " + DRAGON_NAME + " razed a small settlement in Sheeroe Hills last night. \n" +
					                   "Please, help the king avenge their deaths and keep Hibernia safe from " + DRAGON_NAME +  "\'s influence. \n\n"+
					                   "Can you support Hibernia and [kill the dragon]?");
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
							player.Out.SendQuestSubscribeCommand(Dean, QuestMgr.GetIDForQuestType(typeof(DragonWeeklyQuestHib)), "Will you help Dean with "+questTitle+"?");
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
			if (player.IsDoingQuest(typeof (DragonWeeklyQuestHib)) != null)
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
			DragonWeeklyQuestHib quest = player.IsDoingQuest(typeof (DragonWeeklyQuestHib)) as DragonWeeklyQuestHib;

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

		private static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DragonWeeklyQuestHib)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Dean.CanGiveQuest(typeof (DragonWeeklyQuestHib), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DragonWeeklyQuestHib)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for your help.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Dean.GiveQuest(typeof (DragonWeeklyQuestHib), player, 1))
					return;

				Dean.SayTo(player, "Please, find the dragon in Sheeroe Hills and defend our realm.");

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
						return "Travel to Sheeroe Hills and slay " + DRAGON_NAME + " for Hibernia. \nKilled: " + DRAGON_NAME + " ("+ DragonKilled +" | " + MAX_KILLED + ")";
					case 2:
						return "Return to Dean for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(DragonWeeklyQuestHib)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Name.ToLower() == DRAGON_NAME.ToLower()) 
			{
				DragonKilled = 1;
				player.Out.SendMessage("[Weekly] You killed " + DRAGON_NAME + ": (" + DragonKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
					
				if (DragonKilled >= MAX_KILLED)
				{
					// FinishQuest or go back to Dean
					Step = 2;
				}
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "DragonWeeklyQuestHib";
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
			m_questPlayer.Wallet.AddMoney(WalletHelper.ToMoney(0,0,m_questPlayer.Level * 5,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateReward(m_questPlayer, 1500);
			DragonKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
