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
	public class MegalocerosKillQuestMid : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Daily] Megaloceros Invasion";
		protected const int minimumLevel = 40;
		protected const int maximumLevel = 50;

		// Kill Goal
		protected const int MAX_KILLED = 10;
		
		private static GameNPC Isaac = null; // Start NPC

		private int megalocerosKilled = 0;

		// Constructors
		public MegalocerosKillQuestMid() : base()
		{
		}

		public MegalocerosKillQuestMid(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public MegalocerosKillQuestMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public MegalocerosKillQuestMid(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Isaac", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 100 && npc.X == 766590 && npc.Y == 670407)
					{
						Isaac = npc;
						break;
					}

			if (Isaac == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Isaac , creating it ...");
				Isaac = new GameNPC();
				Isaac.Model = 774;
				Isaac.Name = "Isaac";
				Isaac.GuildName = "Advisor to the King";
				Isaac.Realm = eRealm.Midgard;
				Isaac.CurrentRegionID = 100;
				Isaac.Size = 50;
				Isaac.Level = 59;
				//Castle Sauvage Location
				Isaac.X = 766590;
				Isaac.Y = 670407;
				Isaac.Z = 5736;
				Isaac.Heading = 2358;
				Isaac.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Isaac.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Isaac, GameObjectEvent.Interact, new DOLEventHandler(TalkToCola));
			GameEventMgr.AddHandler(Isaac, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToCola));

			/* Now we bring to Isaac the possibility to give this quest to players */
			Isaac.AddQuestToGive(typeof (MegalocerosKillQuestMid));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Isaac == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Isaac, GameObjectEvent.Interact, new DOLEventHandler(TalkToCola));
			GameEventMgr.RemoveHandler(Isaac, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToCola));

			/* Now we remove to Isaac the possibility to give this quest to players */
			Isaac.RemoveQuestToGive(typeof (MegalocerosKillQuestMid));
		}

		protected static void TalkToCola(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Isaac.CanGiveQuest(typeof (MegalocerosKillQuestMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			MegalocerosKillQuestMid quest = player.IsDoingQuest(typeof (MegalocerosKillQuestMid)) as MegalocerosKillQuestMid;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Isaac.SayTo(player, "You will find Megaloceros in the South East of Gripklosa Mountains.");
							break;
						case 2:
							Isaac.SayTo(player, "Hello " + player.Name + ", did you [kill] the Megaloceros?");
							break;
					}
				}
				else
				{
					Isaac.SayTo(player, "Hello "+ player.Name +", I am Isaac, Fen\'s friend. "+
					                    "The Megaloceros out in Gripklosa Mountains are devouring the natural flora and fauna of the Shrouded Isles. They may soon destroy the ecosystem entirely.\n"+
					                    "\nCan you [clear the Megaloceros] to save the Shrouded Isles?");
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (quest == null)
				{
					switch (wArgs.Text.ToLower())
					{
						case "clear the megaloceros":
							player.Out.SendQuestSubscribeCommand(Isaac, QuestMgr.GetIDForQuestType(typeof(MegalocerosKillQuestMid)), "Will you help Isaac "+questTitle+"");
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
			if (player.IsDoingQuest(typeof (MegalocerosKillQuestMid)) != null)
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
			megalocerosKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, megalocerosKilled.ToString());
		}


		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			MegalocerosKillQuestMid quest = player.IsDoingQuest(typeof (MegalocerosKillQuestMid)) as MegalocerosKillQuestMid;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(MegalocerosKillQuestMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Isaac.CanGiveQuest(typeof (MegalocerosKillQuestMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (MegalocerosKillQuestMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Atlas.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Isaac.GiveQuest(typeof (MegalocerosKillQuestMid), player, 1))
					return;

				Isaac.SayTo(player, "You will find the Megaloceros in Gripklosa Mountains.");

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
						return "Find Megaloceros in the South East of Gripklosa Mountains. \nKilled: Megaloceros ("+ megalocerosKilled +" | 10)";
					case 2:
						return "Return to Isaac for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(MegalocerosKillQuestMid)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;
			
			if (Step == 1 && e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (gArgs.Target.Name.ToLower() == "megaloceros") 
				{
					megalocerosKilled++;
					player.Out.SendMessage("[Daily] Megaloceros Killed: ("+megalocerosKilled+" | "+MAX_KILLED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
					player.Out.SendQuestUpdate(this);
					
					if (megalocerosKilled >= MAX_KILLED)
					{
						// FinishQuest or go back to Isaac
						Step = 2;
					}
				}
			}
			
		}
		
		public override string QuestPropertyKey
		{
			get => "MegalocerosKillQuestMid";
			set { ; }
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/10, true);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level,50,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 100);
			megalocerosKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
