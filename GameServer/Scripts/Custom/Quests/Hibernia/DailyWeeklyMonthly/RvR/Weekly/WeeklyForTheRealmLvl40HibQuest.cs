using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.Quests;
using log4net;

namespace Core.GS.WeeklyQuest.Hibernia
{
	public class WeeklyForTheRealmLvl40HibQuest : Quests.WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Weekly] For The Realm";
		private const int minimumLevel = 40;
		private const int maximumLevel = 50;

		private static GameNpc ReyHib = null; // Start NPC

		private int _playersKilledMid = 0;
		private int _playersKilledAlb = 0;
		private const int MAX_KILLGOAL = 50;

		// prevent grey killing
		private const int MIN_PLAYER_CON = -3;
		
		// Constructors
		public WeeklyForTheRealmLvl40HibQuest() : base()
		{
		}

		public WeeklyForTheRealmLvl40HibQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public WeeklyForTheRealmLvl40HibQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public WeeklyForTheRealmLvl40HibQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			/* Now we bring to Rey the possibility to give this quest to players */
			ReyHib.AddQuestToGive(typeof (WeeklyForTheRealmLvl40HibQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" Hib initialized");
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

			/* Now we remove to Rey the possibility to give this quest to players */
			ReyHib.RemoveQuestToGive(typeof (WeeklyForTheRealmLvl40HibQuest));
		}

		private static void TalkToRey(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(ReyHib.CanGiveQuest(typeof (WeeklyForTheRealmLvl40HibQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			WeeklyForTheRealmLvl40HibQuest quest = player.IsDoingQuest(typeof (WeeklyForTheRealmLvl40HibQuest)) as WeeklyForTheRealmLvl40HibQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							ReyHib.SayTo(player, "Head into the enemy frontiers and slay their forces. There are many Midgard and Albion enemies, you will find them.");
							break;
						case 2:
							ReyHib.SayTo(player, "Hello " + player.Name + ", did you [slay their forces] for your reward?");
							break;
					}
				}
				else
				{
					ReyHib.SayTo(player, "You won't believe it, "+ player.Name +". We got a bulk order for exotic bones too. "+
					                     "My workload is way too high, I'm going to ask for a raise. Well, at least after you bring me a [bunch of toeknuckles]. \n\n"+
					                     "I have an order to fill, after all...");
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
						case "bunch of toeknuckles":
							player.Out.SendQuestSubscribeCommand(ReyHib, QuestMgr.GetIDForQuestType(typeof(WeeklyForTheRealmLvl40HibQuest)), "Will you undertake " + questTitle + "?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "slay their forces":
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
			if (player.IsDoingQuest(typeof (WeeklyForTheRealmLvl40HibQuest)) != null)
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
			WeeklyForTheRealmLvl40HibQuest quest = player.IsDoingQuest(typeof (WeeklyForTheRealmLvl40HibQuest)) as WeeklyForTheRealmLvl40HibQuest;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(WeeklyForTheRealmLvl40HibQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(ReyHib.CanGiveQuest(typeof (WeeklyForTheRealmLvl40HibQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (WeeklyForTheRealmLvl40HibQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping me.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!ReyHib.GiveQuest(typeof (WeeklyForTheRealmLvl40HibQuest), player, 1))
					return;

				ReyHib.SayTo(player, "You will find suitable players in the frontiers.");

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
						return "Head into the enemy frontiers and slay their forces. \n" +
						       "Players Killed: Albion ("+ _playersKilledAlb +" | "+ MAX_KILLGOAL +")\n" +
						       "Players Killed: Midgard ("+ _playersKilledMid +" | "+ MAX_KILLGOAL +")";
					case 2:
						return "Return to Rey in Druim Ligen for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(WeeklyForTheRealmLvl40HibQuest)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (e != GameLivingEvent.EnemyKilled || Step != 1) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Realm == ERealm.Midgard && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON && _playersKilledMid < MAX_KILLGOAL) 
			{
				_playersKilledMid++;
				player.Out.SendMessage("[Daily] Midgard Enemy Killed: (" + _playersKilledMid + " | " + MAX_KILLGOAL + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (gArgs.Target.Realm == ERealm.Albion && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON && _playersKilledAlb < MAX_KILLGOAL) 
			{
				_playersKilledAlb++;
				player.Out.SendMessage("[Daily] Albion Enemy Killed: (" + _playersKilledAlb + " | " + MAX_KILLGOAL + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
				
			if (_playersKilledMid >= MAX_KILLGOAL && _playersKilledAlb >= MAX_KILLGOAL)
			{
				// FinishQuest or go back to Rey
				Step = 2;
			}
		}
		
		public override string QuestPropertyKey
		{
			get => "ForTheRealmQuestHib";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_playersKilledAlb = GetCustomProperty("ForTheRealmKilledAlb") != null ? int.Parse(GetCustomProperty("ForTheRealmKilledAlb")) : 0;
			_playersKilledMid = GetCustomProperty("ForTheRealmKilledMid") != null ? int.Parse(GetCustomProperty("ForTheRealmKilledMid")) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty("ForTheRealmKilledAlb", _playersKilledAlb.ToString());
			SetCustomProperty("ForTheRealmKilledMid", _playersKilledMid.ToString());
		}


		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			int reward = ServerProperties.Properties.WEEKLY_RVR_REWARD;
			
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel));
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level * 5,32,Util.Random(50)), "You receive {0} as a reward.");
			CoreRoGMgr.GenerateReward(m_questPlayer, 1500);
			CoreRoGMgr.GenerateJewel(m_questPlayer, (byte)(m_questPlayer.Level + 1), m_questPlayer.Level + Util.Random(10, 20));
			_playersKilledAlb = 0;
			_playersKilledMid = 0;
			
			if (reward > 0)
			{
				m_questPlayer.Out.SendMessage($"You have been rewarded {reward} Realmpoints for finishing Weekly Quest.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				m_questPlayer.GainRealmPoints(reward, false);
				m_questPlayer.Out.SendUpdatePlayer();
			}
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
