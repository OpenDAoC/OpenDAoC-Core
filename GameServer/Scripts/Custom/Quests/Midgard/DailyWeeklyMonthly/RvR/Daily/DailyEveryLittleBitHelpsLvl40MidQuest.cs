using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;
using Core.GS.Packets;
using Core.GS.Quests;
using log4net;

namespace Core.GS.DailyQuest.Midgard
{
	public class DailyEveryLittleBitHelpsLvl40MidQuest : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Daily] Every Little Bit Helps";
		private const int minimumLevel = 40;
		private const int maximumLevel = 50;

		private static GameNpc ReyMid = null; // Start NPC

		private int _playersKilledAlb = 0;
		private int _playersKilledHib = 0;
		private const int MAX_KILLGOAL = 5;
		
		// prevent grey killing
		private const int MIN_PLAYER_CON = -3;

		// Constructors
		public DailyEveryLittleBitHelpsLvl40MidQuest() : base()
		{
		}

		public DailyEveryLittleBitHelpsLvl40MidQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DailyEveryLittleBitHelpsLvl40MidQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DailyEveryLittleBitHelpsLvl40MidQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Rey", ERealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
				{
					if (npc.CurrentRegionID == 100 && npc.X == 766491 && npc.Y == 670375)
					{
						ReyMid = npc;
						break;
					}
				}

			if (ReyMid == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Rey , creating it ...");
				ReyMid = new GameNpc();
				ReyMid.Model = 26;
				ReyMid.Name = "Rey";
				ReyMid.GuildName = "Bone Collector";
				ReyMid.Realm = ERealm.Midgard;
				//Svasud Faste Location
				ReyMid.CurrentRegionID = 100;
				ReyMid.Size = 60;
				ReyMid.Level = 59;
				ReyMid.X = 766491;
				ReyMid.Y = 670375;
				ReyMid.Z = 5736;
				ReyMid.Heading = 2358;
				ReyMid.Flags |= ENpcFlags.PEACE;
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(ReyMid, GameObjectEvent.Interact, new CoreEventHandler(TalkToRey));
			GameEventMgr.AddHandler(ReyMid, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToRey));

			/* Now we bring to Rey the possibility to give this quest to players */
			ReyMid.AddQuestToGive(typeof (DailyEveryLittleBitHelpsLvl40MidQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" Mid initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (ReyMid == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(ReyMid, GameObjectEvent.Interact, new CoreEventHandler(TalkToRey));
			GameEventMgr.RemoveHandler(ReyMid, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToRey));

			/* Now we remove to Rey the possibility to give this quest to players */
			ReyMid.RemoveQuestToGive(typeof (DailyEveryLittleBitHelpsLvl40MidQuest));
		}

		private static void TalkToRey(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(ReyMid.CanGiveQuest(typeof (DailyEveryLittleBitHelpsLvl40MidQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DailyEveryLittleBitHelpsLvl40MidQuest quest = player.IsDoingQuest(typeof (DailyEveryLittleBitHelpsLvl40MidQuest)) as DailyEveryLittleBitHelpsLvl40MidQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							ReyMid.SayTo(player, "Find and kill enemies of Albion and Hibernia. You will find suitable players in the frontiers.");
							break;
						case 2:
							ReyMid.SayTo(player, "Hello " + player.Name + ", did you [kill enemies] for your reward?");
							break;
					}
				}
				else
				{
					ReyMid.SayTo(player, "Hello "+ player.Name +", I am Rey.My master, Fen, has a need for some... exotic bones. "+
					                     "Stuff you can't really get here in Midgard, if you catch my drift.\n"+
					                     "\nThink you could [take the toeknuckle] off of a firbolg for me? A highlander could probably work too.");
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
						case "take the toeknuckle":
							player.Out.SendQuestSubscribeCommand(ReyMid, QuestMgr.GetIDForQuestType(typeof(DailyEveryLittleBitHelpsLvl40MidQuest)), "Will you undertake " + questTitle + "?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "kill enemies":
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
			if (player.IsDoingQuest(typeof (DailyEveryLittleBitHelpsLvl40MidQuest)) != null)
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
			DailyEveryLittleBitHelpsLvl40MidQuest quest = player.IsDoingQuest(typeof (DailyEveryLittleBitHelpsLvl40MidQuest)) as DailyEveryLittleBitHelpsLvl40MidQuest;

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

		private static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DailyEveryLittleBitHelpsLvl40MidQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(ReyMid.CanGiveQuest(typeof (DailyEveryLittleBitHelpsLvl40MidQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DailyEveryLittleBitHelpsLvl40MidQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping me.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!ReyMid.GiveQuest(typeof (DailyEveryLittleBitHelpsLvl40MidQuest), player, 1))
					return;

				ReyMid.SayTo(player, "You will find suitable players in the frontiers.");

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
						return "You will find suitable players in the frontiers. \n" +
						       "Players Killed: Hibernia ("+ _playersKilledHib +" | "+ MAX_KILLGOAL +")\n" +
						       "Players Killed: Albion ("+ _playersKilledAlb +" | "+ MAX_KILLGOAL +")";
					case 2:
						return "Return to Rey in Svasud Faste for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(DailyEveryLittleBitHelpsLvl40MidQuest)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (e != GameLivingEvent.EnemyKilled || Step != 1) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Realm == ERealm.Albion && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON && _playersKilledAlb < MAX_KILLGOAL) 
			{
				_playersKilledAlb++;
				player.Out.SendMessage("[Daily] Albion Enemy Killed: (" + _playersKilledAlb + " | " + MAX_KILLGOAL + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (gArgs.Target.Realm == ERealm.Hibernia && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON && _playersKilledHib < MAX_KILLGOAL) 
			{
				_playersKilledHib++;
				player.Out.SendMessage("[Daily] Hibernia Enemy Killed: (" + _playersKilledHib + " | " + MAX_KILLGOAL + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
				
			if (_playersKilledAlb >= MAX_KILLGOAL && _playersKilledHib >= MAX_KILLGOAL)
			{
				// FinishQuest or go back to Rey
				Step = 2;
			}
		}
		
		public override string QuestPropertyKey
		{
			get => "EveryLittleBitHelpsQuestAlb";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_playersKilledHib = GetCustomProperty("PlayersKilledHib") != null ? int.Parse(GetCustomProperty("PlayersKilledHib")) : 0;
			_playersKilledAlb = GetCustomProperty("PlayersKilledAlb") != null ? int.Parse(GetCustomProperty("PlayersKilledAlb")) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty("PlayersKilledHib", _playersKilledHib.ToString());
			SetCustomProperty("PlayersKilledAlb", _playersKilledAlb.ToString());
		}
		public override void FinishQuest()
		{
			int reward = ServerProperties.Properties.DAILY_RVR_REWARD;
			
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/5);
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level,32,Util.Random(50)), "You receive {0} as a reward.");
			CoreRogMgr.GenerateReward(m_questPlayer, 250);
			CoreRogMgr.GenerateJewel(m_questPlayer, (byte)(m_questPlayer.Level + 1), m_questPlayer.Level + Util.Random(5, 15));
			_playersKilledHib = 0;
			_playersKilledAlb = 0;
			
			if (reward > 0)
			{
				m_questPlayer.Out.SendMessage($"You have been rewarded {reward} Realmpoints for finishing Daily Quest.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				m_questPlayer.GainRealmPoints(reward, false);
				m_questPlayer.Out.SendUpdatePlayer();
			}
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
