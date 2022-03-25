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
	public class KillNPCInFrontiersHib : Quests.DailyQuest
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
		
		private static GameNPC Hector = null; // Start NPC

		private int FrontierMobsKilled = 0;

		// Constructors
		public KillNPCInFrontiersHib() : base()
		{
		}

		public KillNPCInFrontiersHib(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public KillNPCInFrontiersHib(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public KillNPCInFrontiersHib(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Hector", eRealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 200 && npc.X == 334793 && npc.Y == 420805)
					{
						Hector = npc;
						break;
					}

			if (Hector == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Hector , creating it ...");
				Hector = new GameNPC();
				Hector.Model = 355;
				Hector.Name = "Hector";
				Hector.GuildName = "Realm Logistics";
				Hector.Realm = eRealm.Hibernia;
				//Druim Ligen Location
				Hector.CurrentRegionID = 200;
				Hector.Size = 50;
				Hector.Level = 59;
				Hector.X = 334793;
				Hector.Y = 420805;
				Hector.Z = 5184;
				Hector.Heading = 1586;
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Hector, GameObjectEvent.Interact, new DOLEventHandler(TalkToHector));
			GameEventMgr.AddHandler(Hector, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHector));

			/* Now we bring to Dean the possibility to give this quest to players */
			Hector.AddQuestToGive(typeof (KillNPCInFrontiersHib));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Hector == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Hector, GameObjectEvent.Interact, new DOLEventHandler(TalkToHector));
			GameEventMgr.RemoveHandler(Hector, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHector));

			/* Now we remove to Dean the possibility to give this quest to players */
			Hector.RemoveQuestToGive(typeof (KillNPCInFrontiersHib));
		}

		protected static void TalkToHector(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Hector.CanGiveQuest(typeof (KillNPCInFrontiersHib), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			KillNPCInFrontiersHib quest = player.IsDoingQuest(typeof (KillNPCInFrontiersHib)) as KillNPCInFrontiersHib;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Hector.SayTo(player, "Kill creatures in any RvR zone to help us clear more room for the armies to maneuver around.");
							break;
						case 2:
							Hector.SayTo(player, "Hello " + player.Name + ", did you [tidy the realm]?");
							break;
					}
				}
				else
				{
					Hector.SayTo(player, "Hello "+ player.Name +", I am Hector. I serve the realm and ensure its borders are always protected. "+
					                       "I heard you are strong. Do you think you're strong enough to help me with some trouble we've been having? \n\n"+
					                       "I need an adventurer to help me [clear the frontiers].");
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
						case "clear the frontiers":
							player.Out.SendQuestSubscribeCommand(Hector, QuestMgr.GetIDForQuestType(typeof(KillNPCInFrontiersHib)), "Will you help Dean "+questTitle+"");
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
			if (player.IsDoingQuest(typeof (KillNPCInFrontiersHib)) != null)
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
			KillNPCInFrontiersHib quest = player.IsDoingQuest(typeof (KillNPCInFrontiersHib)) as KillNPCInFrontiersHib;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(KillNPCInFrontiersHib)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Hector.CanGiveQuest(typeof (KillNPCInFrontiersHib), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (KillNPCInFrontiersHib)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping our realm.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Hector.GiveQuest(typeof (KillNPCInFrontiersHib), player, 1))
					return;

				Hector.SayTo(player, "Killing creatures in any RvR zone will work. Thanks for your service!");

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
						return "Return to Hector in Druim Ligen for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(KillNPCInFrontiersHib)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;
			
			if (e == GameLivingEvent.EnemyKilled && Step == 1)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (player.GetConLevel(gArgs.Target) > -1
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
			get => "KillNPCInFrontiersHib";
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
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 150);
			FrontierMobsKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
