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

namespace DOL.GS.DailyQuest.Midgard
{
	public class EveryLittleBitHelpsQuestMid : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Daily] Every Little Bit Helps";
		protected const int minimumLevel = 40;
		protected const int maximumLevel = 50;

		private static GameNPC ReyMid = null; // Start NPC

		private int _playersKilledAlb = 0;
		private int _playersKilledHib = 0;
		protected const int MAX_KILLGOAL = 5;

		// Constructors
		public EveryLittleBitHelpsQuestMid() : base()
		{
		}

		public EveryLittleBitHelpsQuestMid(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public EveryLittleBitHelpsQuestMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public EveryLittleBitHelpsQuestMid(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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
				ReyMid.Heading = 2358;
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
			ReyMid.AddQuestToGive(typeof (EveryLittleBitHelpsQuestMid));

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
			ReyMid.RemoveQuestToGive(typeof (EveryLittleBitHelpsQuestMid));
		}

		protected static void TalkToRey(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(ReyMid.CanGiveQuest(typeof (EveryLittleBitHelpsQuestMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			EveryLittleBitHelpsQuestMid quest = player.IsDoingQuest(typeof (EveryLittleBitHelpsQuestMid)) as EveryLittleBitHelpsQuestMid;

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
							player.Out.SendQuestSubscribeCommand(ReyMid, QuestMgr.GetIDForQuestType(typeof(EveryLittleBitHelpsQuestMid)), "Will you undertake " + questTitle + "?");
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
			if (player.IsDoingQuest(typeof (EveryLittleBitHelpsQuestMid)) != null)
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
			EveryLittleBitHelpsQuestMid quest = player.IsDoingQuest(typeof (EveryLittleBitHelpsQuestMid)) as EveryLittleBitHelpsQuestMid;

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

		protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(EveryLittleBitHelpsQuestMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(ReyMid.CanGiveQuest(typeof (EveryLittleBitHelpsQuestMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (EveryLittleBitHelpsQuestMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping me.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!ReyMid.GiveQuest(typeof (EveryLittleBitHelpsQuestMid), player, 1))
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
						       "Players Killed: Hibernia ("+ _playersKilledHib +" | "+ MAX_KILLGOAL +")" +
						       "Players Killed: Albion ("+ _playersKilledAlb +" | "+ MAX_KILLGOAL +")";
					case 2:
						return "Return to Rey in Svasud Faste for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(EveryLittleBitHelpsQuestMid)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

				if (gArgs.Target.Realm == eRealm.Albion && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && _playersKilledAlb < MAX_KILLGOAL) 
				{
					_playersKilledAlb++;
					player.Out.SendMessage("[Daily] Killed Albion Enemy: (" + _playersKilledAlb + " | " + MAX_KILLGOAL + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
					player.Out.SendQuestUpdate(this);
				}
				else if (gArgs.Target.Realm == eRealm.Hibernia && gArgs.Target.Realm != player.Realm && gArgs.Target is GamePlayer && _playersKilledHib < MAX_KILLGOAL) 
				{
					_playersKilledHib++;
					player.Out.SendMessage("[Daily] Killed Hibernia Enemy: (" + _playersKilledHib + " | " + MAX_KILLGOAL + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
					player.Out.SendQuestUpdate(this);
				}
				
				if (_playersKilledAlb >= MAX_KILLGOAL && _playersKilledHib >= MAX_KILLGOAL)
				{
					// FinishQuest or go back to Rey
					Step = 2;
				}
				
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


		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/5, false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level * 2,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 1000);
			_playersKilledHib = 0;
			_playersKilledAlb = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}
	}
}
