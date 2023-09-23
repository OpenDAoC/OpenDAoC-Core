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

namespace DOL.GS.WeeklyQuest.Midgard
{
	public class ForTheRealmQuestMid : Quests.WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Weekly] For The Realm";
		private const int minimumLevel = 40;
		private const int maximumLevel = 50;

		private static GameNPC ReyMid = null; // Start NPC

		private int _playersKilledHib = 0;
		private int _playersKilledAlb = 0;
		private const int MAX_KILLGOAL = 50;
		
		// prevent grey killing
		private const int MIN_PLAYER_CON = -3;
		// Constructors
		public ForTheRealmQuestMid() : base()
		{
		}

		public ForTheRealmQuestMid(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public ForTheRealmQuestMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public ForTheRealmQuestMid(GamePlayer questingPlayer, DbQuests dbQuest) : base(questingPlayer, dbQuest)
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
				ReyMid = new GameNPC();
				ReyMid.Model = 26;
				ReyMid.Name = "Rey";
				ReyMid.GuildName = "Bone Collector";
				ReyMid.Realm = eRealm.Midgard;
				//Svasud Faste Location
				ReyMid.CurrentRegionID = 100;
				ReyMid.Size = 60;
				ReyMid.Level = 59;
				ReyMid.X = 766491;
				ReyMid.Y = 670375;
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
			ReyMid.AddQuestToGive(typeof (ForTheRealmQuestMid));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" Mid initialized");
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
			ReyMid.RemoveQuestToGive(typeof (ForTheRealmQuestMid));
		}

		protected static void TalkToRey(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(ReyMid.CanGiveQuest(typeof (ForTheRealmQuestMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			ForTheRealmQuestMid quest = player.IsDoingQuest(typeof (ForTheRealmQuestMid)) as ForTheRealmQuestMid;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							ReyMid.SayTo(player, "Head into the enemy frontiers and slay their forces. There are many Hibernia and Albion enemies, you will find them.");
							break;
						case 2:
							ReyMid.SayTo(player, "Hello " + player.Name + ", did you [slay their forces] for your reward?");
							break;
					}
				}
				else
				{
					ReyMid.SayTo(player, "You won't believe it, "+ player.Name +". We got a bulk order for exotic bones too. "+
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
							player.Out.SendQuestSubscribeCommand(ReyMid, QuestMgr.GetIDForQuestType(typeof(ForTheRealmQuestMid)), "Will you undertake " + questTitle + "?");
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
			if (player.IsDoingQuest(typeof (ForTheRealmQuestMid)) != null)
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
			ForTheRealmQuestMid quest = player.IsDoingQuest(typeof (ForTheRealmQuestMid)) as ForTheRealmQuestMid;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(ForTheRealmQuestMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(ReyMid.CanGiveQuest(typeof (ForTheRealmQuestMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (ForTheRealmQuestMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping me.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!ReyMid.GiveQuest(typeof (ForTheRealmQuestMid), player, 1))
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
						return "Head into the enemy frontiers and slay their forces. \n" +
						       "Players Killed: Albion ("+ _playersKilledAlb +" | "+ MAX_KILLGOAL +")\n" +
						       "Players Killed: Hibernia ("+ _playersKilledHib +" | "+ MAX_KILLGOAL +")";
					case 2:
						return "Return to Rey in Svasud Faste for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(ForTheRealmQuestMid)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (e != GameLivingEvent.EnemyKilled || Step != 1) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Realm == eRealm.Hibernia && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON && _playersKilledHib < MAX_KILLGOAL) 
			{
				_playersKilledHib++;
				player.Out.SendMessage("[Daily] Killed Hibernia Enemy: (" + _playersKilledHib + " | " + MAX_KILLGOAL + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (gArgs.Target.Realm == eRealm.Albion && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON && _playersKilledAlb < MAX_KILLGOAL) 
			{
				_playersKilledAlb++;
				player.Out.SendMessage("[Daily] Killed Albion Enemy: (" + _playersKilledAlb + " | " + MAX_KILLGOAL + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
				
			if (_playersKilledHib >= MAX_KILLGOAL && _playersKilledAlb >= MAX_KILLGOAL)
			{
				// FinishQuest or go back to Rey
				Step = 2;
			}
		}
		
		public override string QuestPropertyKey
		{
			get => "ForTheRealmQuestMid";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_playersKilledAlb = GetCustomProperty("ForTheRealmKilledAlb") != null ? int.Parse(GetCustomProperty("ForTheRealmKilledAlb")) : 0;
			_playersKilledHib = GetCustomProperty("ForTheRealmKilledHib") != null ? int.Parse(GetCustomProperty("ForTheRealmKilledHib")) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty("ForTheRealmKilledAlb", _playersKilledAlb.ToString());
			SetCustomProperty("ForTheRealmKilledHib", _playersKilledHib.ToString());
		}

		public override void FinishQuest()
		{
			int reward = ServerProperties.Properties.WEEKLY_RVR_REWARD;
			
			m_questPlayer.ForceGainExperience( (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel));
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level * 5,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateReward(m_questPlayer, 1500);
			AtlasROGManager.GenerateJewel(m_questPlayer, (byte)(m_questPlayer.Level + 1), m_questPlayer.Level + Util.Random(10, 20));
			_playersKilledHib = 0;
			_playersKilledAlb = 0;
			
			if (reward > 0)
			{
				m_questPlayer.Out.SendMessage($"You have been rewarded {reward} Realmpoints for finishing Weekly Quest.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				m_questPlayer.GainRealmPoints(reward, false);
				m_questPlayer.Out.SendUpdatePlayer();
			}
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
