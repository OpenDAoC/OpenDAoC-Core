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

namespace DOL.GS.DailyQuest.Hibernia
{
	public class KillNPCInFrontiersMid : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Daily] A Bit of Bravery";
		protected const int minimumLevel = 10;
		protected const int maximumLevel = 50;

		// Kill Goal
		protected const int MAX_KILLED = 25;
		
		private static GameNPC Isaac = null; // Start NPC

		private int FrontierMobsKilled = 0;

		// Constructors
		public KillNPCInFrontiersMid() : base()
		{
		}

		public KillNPCInFrontiersMid(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public KillNPCInFrontiersMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public KillNPCInFrontiersMid(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameEventMgr.AddHandler(Isaac, GameObjectEvent.Interact, new DOLEventHandler(TalkToIsaac));
			GameEventMgr.AddHandler(Isaac, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToIsaac));

			/* Now we bring to Dean the possibility to give this quest to players */
			Isaac.AddQuestToGive(typeof (KillNPCInFrontiersMid));

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

			GameEventMgr.RemoveHandler(Isaac, GameObjectEvent.Interact, new DOLEventHandler(TalkToIsaac));
			GameEventMgr.RemoveHandler(Isaac, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToIsaac));

			/* Now we remove to Dean the possibility to give this quest to players */
			Isaac.RemoveQuestToGive(typeof (KillNPCInFrontiersMid));
		}

		protected static void TalkToIsaac(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Isaac.CanGiveQuest(typeof (KillNPCInFrontiersMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			KillNPCInFrontiersMid quest = player.IsDoingQuest(typeof (KillNPCInFrontiersMid)) as KillNPCInFrontiersMid;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Isaac.SayTo(player, "Kill creatures in any RvR zone to help us clear more room for the armies to maneuver around.");
							break;
						case 2:
							Isaac.SayTo(player, "Hello " + player.Name + ", did you [tidy the realm]?");
							break;
					}
				}
				else
				{
					Isaac.SayTo(player, "Hello "+ player.Name +", I am Isaac, Fen\'s friend. I help the king with logistics, and he's tasked me with getting things done around here. "+
					                       "I heard you are strong. Do you think you're strong enough to help me with some trouble we've been having on the border? \n\n"+
					                       "\nCan you help me [clear our frontiers]?");
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
						case "clear our frontiers":
							player.Out.SendQuestSubscribeCommand(Isaac, QuestMgr.GetIDForQuestType(typeof(KillNPCInFrontiersMid)), "Will you help Dean "+questTitle+"");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "tidy the realm":
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
			if (player.IsDoingQuest(typeof (KillNPCInFrontiersMid)) != null)
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
			FrontierMobsKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, FrontierMobsKilled.ToString());
		}


		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			KillNPCInFrontiersMid quest = player.IsDoingQuest(typeof (KillNPCInFrontiersMid)) as KillNPCInFrontiersMid;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(KillNPCInFrontiersMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Isaac.CanGiveQuest(typeof (KillNPCInFrontiersMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (KillNPCInFrontiersMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping our realm.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Isaac.GiveQuest(typeof (KillNPCInFrontiersMid), player, 1))
					return;

				Isaac.SayTo(player, "Killing creatures in any RvR zone will work. Thanks for your service!");

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
						return "Kill yellow con or higher mobs in any RvR zone. \nKilled: ("+ FrontierMobsKilled +" | 25)";
					case 2:
						return "Return to Isaac in Svasud Faste for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(KillNPCInFrontiersMid)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;
			
			if (e == GameLivingEvent.EnemyKilled && Step == 1)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (player.GetConLevel(gArgs.Target) >= 0 
				    && gArgs.Target.CurrentZone.IsRvR && player.CurrentZone.IsRvR) 
				{
					FrontierMobsKilled++;
					player.Out.SendQuestUpdate(this);
					
					if (FrontierMobsKilled >= 25)
					{
						// FinishQuest or go back to npc
						Step = 2;
					}
				}
				
			}
			
		}
		
		public override string QuestPropertyKey
		{
			get => "KillNPCInFrontiersMid";
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
			FrontierMobsKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
