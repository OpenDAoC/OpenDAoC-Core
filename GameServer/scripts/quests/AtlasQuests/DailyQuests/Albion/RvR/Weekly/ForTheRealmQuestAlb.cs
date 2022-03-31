using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Cache;
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
	public class ForTheRealmQuestAlb : WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Weekly] For The Realm";
		private const int minimumLevel = 40;
		private const int maximumLevel = 50;

		private static GameNPC ReyAlb = null; // Start NPC

		private int _playersKilledMid = 0;
		private int _playersKilledHib = 0;
		private const int MAX_KILLGOAL = 25;
		
		// prevent grey killing
		private const int MIN_PLAYER_CON = -3;

		// Constructors
		public ForTheRealmQuestAlb() : base()
		{
		}

		public ForTheRealmQuestAlb(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public ForTheRealmQuestAlb(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public ForTheRealmQuestAlb(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Rey", eRealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
				{
					if (npc.CurrentRegionID == 1 && npc.X == 583867 && npc.Y == 477355)
					{
						ReyAlb = npc;
						break;
					}
				}

			if (ReyAlb == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Rey , creating it ...");
				ReyAlb = new GameNPC();
				ReyAlb.Model = 26;
				ReyAlb.Name = "Rey";
				ReyAlb.GuildName = "Bone Collector";
				ReyAlb.Realm = eRealm.Albion;
				//Druim Ligen Location
				ReyAlb.CurrentRegionID = 1;
				ReyAlb.Size = 60;
				ReyAlb.Level = 59;
				ReyAlb.X = 583867;
				ReyAlb.Y = 477355;
				ReyAlb.Z = 2600;
				ReyAlb.Heading = 3054;
				ReyAlb.Flags |= GameNPC.eFlags.PEACE;
				ReyAlb.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					ReyAlb.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(ReyAlb, GameObjectEvent.Interact, new DOLEventHandler(TalkToRey));
			GameEventMgr.AddHandler(ReyAlb, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToRey));

			/* Now we bring to Rey the possibility to give this quest to players */
			ReyAlb.AddQuestToGive(typeof (ForTheRealmQuestAlb));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" Alb initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (ReyAlb == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(ReyAlb, GameObjectEvent.Interact, new DOLEventHandler(TalkToRey));
			GameEventMgr.RemoveHandler(ReyAlb, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToRey));

			/* Now we remove to Rey the possibility to give this quest to players */
			ReyAlb.RemoveQuestToGive(typeof (ForTheRealmQuestAlb));
		}

		private static void TalkToRey(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(ReyAlb.CanGiveQuest(typeof (ForTheRealmQuestAlb), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			ForTheRealmQuestAlb quest = player.IsDoingQuest(typeof (ForTheRealmQuestAlb)) as ForTheRealmQuestAlb;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							ReyAlb.SayTo(player, "Head into the enemy frontiers and slay their forces. There are many Midgard and Hibernia enemies, you will find them.");
							break;
						case 2:
							ReyAlb.SayTo(player, "Hello " + player.Name + ", did you [slay their forces] for your reward?");
							break;
					}
				}
				else
				{
					ReyAlb.SayTo(player, "You won't believe it, "+ player.Name +". We got a bulk order for exotic bones too. "+
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
							player.Out.SendQuestSubscribeCommand(ReyAlb, QuestMgr.GetIDForQuestType(typeof(ForTheRealmQuestAlb)), "Will you undertake " + questTitle + "?");
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
			if (player.IsDoingQuest(typeof (ForTheRealmQuestAlb)) != null)
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
			ForTheRealmQuestAlb quest = player.IsDoingQuest(typeof (ForTheRealmQuestAlb)) as ForTheRealmQuestAlb;

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

		private static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(ForTheRealmQuestAlb)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(ReyAlb.CanGiveQuest(typeof (ForTheRealmQuestAlb), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (ForTheRealmQuestAlb)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping me.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!ReyAlb.GiveQuest(typeof (ForTheRealmQuestAlb), player, 1))
					return;

				ReyAlb.SayTo(player, "You will find suitable players in the frontiers.");

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
						       "Players Killed: Hibernia ("+ _playersKilledHib +" | "+ MAX_KILLGOAL +")" +
						       "Players Killed: Midgard ("+ _playersKilledMid +" | "+ MAX_KILLGOAL +")";
					case 2:
						return "Return to Rey in Castle Sauvage for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(ForTheRealmQuestAlb)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Realm == eRealm.Midgard && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON && _playersKilledMid < MAX_KILLGOAL) 
			{
				_playersKilledMid++;
				player.Out.SendMessage("[Daily] Midgard Enemy Killed: (" + _playersKilledMid + " | " + MAX_KILLGOAL + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (gArgs.Target.Realm == eRealm.Hibernia && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON && _playersKilledHib < MAX_KILLGOAL) 
			{
				_playersKilledHib++;
				player.Out.SendMessage("[Daily] Hibernia Enemy Killed: (" + _playersKilledHib + " | " + MAX_KILLGOAL + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
				
			if (_playersKilledMid >= MAX_KILLGOAL && _playersKilledHib >= MAX_KILLGOAL)
			{
				// FinishQuest or go back to Rey
				Step = 2;
			}
		}
		
		public override string QuestPropertyKey
		{
			get => "ForTheRealmQuestAlb";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_playersKilledHib = GetCustomProperty("ForTheRealmKilledHib") != null ? int.Parse(GetCustomProperty("ForTheRealmKilledHib")) : 0;
			_playersKilledMid = GetCustomProperty("ForTheRealmKilledMid") != null ? int.Parse(GetCustomProperty("ForTheRealmKilledMid")) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty("ForTheRealmKilledHib", _playersKilledHib.ToString());
			SetCustomProperty("ForTheRealmKilledMid", _playersKilledMid.ToString());
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel), false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level * 10,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 1500);
			_playersKilledHib = 0;
			_playersKilledMid = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
