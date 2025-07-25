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

namespace DOL.GS.WeeklyQuest.Midgard
{
	public class EpicRvRMobsWeeklyQuestMid : Quests.WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Weekly] Frontier Cleanup";
		private const int minimumLevel = 50;
		private const int maximumLevel = 50;
		
		// Kill Goal
		private const int MAX_KILLED = 1;
		// Quest Counter
		private int _evernKilled = 0;
		private int _glacierGiantKilled = 0;
		private int _greenKnightKilled = 0;
		
		private static GameNPC Herou = null; // Start NPC

		private const string EVERN_NAME = "Evern";
		private const string GREENKNIGHT_NAME = "Green Knight";
		private const string GLACIERGIANT_NAME = "Glacier Giant";
		
		// Constructors
		public EpicRvRMobsWeeklyQuestMid() : base()
		{
		}

		public EpicRvRMobsWeeklyQuestMid(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public EpicRvRMobsWeeklyQuestMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public EpicRvRMobsWeeklyQuestMid(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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
			Herou.AddQuestToGive(typeof (EpicRvRMobsWeeklyQuestMid));

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
			Herou.RemoveQuestToGive(typeof (EpicRvRMobsWeeklyQuestMid));
		}

		private static void TalkToHerou(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Herou.CanGiveQuest(typeof (EpicRvRMobsWeeklyQuestMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			EpicRvRMobsWeeklyQuestMid quest = player.IsDoingQuest(typeof (EpicRvRMobsWeeklyQuestMid)) as EpicRvRMobsWeeklyQuestMid;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Herou.SayTo(player, player.Name + ", please find allies and kill the epic creatures in frontiers for Midgard!");
							break;
						case 2:
							Herou.SayTo(player, "Hello " + player.Name + ", did you [slay the creatures] and return for your reward?");
							break;
					}
				}
				else
				{
					Herou.SayTo(player, "Hello "+ player.Name +", I am Herou. Some large monsters have blocked the supply lines in our frontier, and I could use your help in getting rid of them.\n"+
					                    "You'll probably need to gather some friends for this one. We've lost a lot of good soldiers already. \n\n"+
					                    "Can you support Midgard and [kill the epic creatures] in frontiers?");
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
						case "kill the epic creatures":
							player.Out.SendQuestSubscribeCommand(Herou, QuestMgr.GetIDForQuestType(typeof(EpicRvRMobsWeeklyQuestMid)), "Will you help Herou "+questTitle+"?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "slay the creatures":
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
			if (player.IsDoingQuest(typeof (EpicRvRMobsWeeklyQuestMid)) != null)
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
			EpicRvRMobsWeeklyQuestMid quest = player.IsDoingQuest(typeof (EpicRvRMobsWeeklyQuestMid)) as EpicRvRMobsWeeklyQuestMid;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and slay those creatures!");
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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(EpicRvRMobsWeeklyQuestMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Herou.CanGiveQuest(typeof (EpicRvRMobsWeeklyQuestMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (EpicRvRMobsWeeklyQuestMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for your help.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Herou.GiveQuest(typeof (EpicRvRMobsWeeklyQuestMid), player, 1))
					return;

				Herou.SayTo(player, "Please, find the epic monsters in frontiers and return for your reward.");

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
						return "Find and slay the three dangerous epic monsters! \n" +
						       "Killed: " + EVERN_NAME + " ("+ _evernKilled +" | " + MAX_KILLED + ")\n" +
						       "Killed: " + GREENKNIGHT_NAME + " ("+ _greenKnightKilled +" | " + MAX_KILLED + ")\n" +
						       "Killed: " + GLACIERGIANT_NAME + " ("+ _glacierGiantKilled +" | " + MAX_KILLED + ")\n";
					case 2:
						return "Return to Herou for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(EpicRvRMobsWeeklyQuestMid)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Name.ToLower() == EVERN_NAME.ToLower() && gArgs.Target is GameNPC && _evernKilled < MAX_KILLED)
			{
				_evernKilled = 1;
				player.Out.SendMessage("[Weekly] You killed " + EVERN_NAME + ": (" + _evernKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (gArgs.Target.Name.ToLower() == GREENKNIGHT_NAME.ToLower() && gArgs.Target is GameNPC && _greenKnightKilled < MAX_KILLED)
			{
				_greenKnightKilled = 1;
				player.Out.SendMessage("[Weekly] You killed " + GREENKNIGHT_NAME + ": (" + _greenKnightKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (gArgs.Target.Name.ToLower() == GLACIERGIANT_NAME.ToLower() && gArgs.Target is GameNPC && _glacierGiantKilled < MAX_KILLED)
			{
				_glacierGiantKilled = 1;
				player.Out.SendMessage("[Weekly] You killed " + GLACIERGIANT_NAME + ": (" + _glacierGiantKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
				
			if (_evernKilled >= MAX_KILLED && _greenKnightKilled >= MAX_KILLED && _glacierGiantKilled>= MAX_KILLED)
			{
				// FinishQuest or go back to Dean
				Step = 2;
			}
		}
		
		public override string QuestPropertyKey
		{
			get => "EpicRvRMobsWeeklyQuestMid";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_evernKilled = GetCustomProperty(EVERN_NAME) != null ? int.Parse(GetCustomProperty(EVERN_NAME)) : 0;
			_glacierGiantKilled = GetCustomProperty(GLACIERGIANT_NAME) != null ? int.Parse(GetCustomProperty(GLACIERGIANT_NAME)) : 0;
			_greenKnightKilled = GetCustomProperty(GREENKNIGHT_NAME) != null ? int.Parse(GetCustomProperty(GREENKNIGHT_NAME)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(EVERN_NAME, _evernKilled.ToString());
			SetCustomProperty(GLACIERGIANT_NAME, _glacierGiantKilled.ToString());
			SetCustomProperty(GREENKNIGHT_NAME, _greenKnightKilled.ToString());
		}

		public override void FinishQuest()
		{
			//m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/10, true);
			m_questPlayer.Wallet.AddMoney(WalletHelper.ToMoney(0,0,m_questPlayer.Level * 5,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateReward(m_questPlayer, 1500);
			AtlasROGManager.GenerateJewel(m_questPlayer, (byte)(m_questPlayer.Level + 1), m_questPlayer.Level + Util.Random(10, 11));
			_evernKilled = 0;
			_glacierGiantKilled = 0;
			_greenKnightKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
