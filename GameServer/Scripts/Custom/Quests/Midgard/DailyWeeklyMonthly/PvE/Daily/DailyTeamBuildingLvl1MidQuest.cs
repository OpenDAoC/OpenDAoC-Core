using System;
using System.Reflection;
using System.Text;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.PlayerClass;
using Core.GS.Quests;
using log4net;

namespace Core.GS.DailyQuest.Midgard
{
	public class DailyTeamBuildingLvl1MidQuest : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Group Daily] A Team Building Exercise";
		private const int minimumLevel = 1;
		private const int maximumLevel = 50;

		// Kill Goal
		private const int MAX_KILLED = 25;
		
		private static GameNpc Isaac = null; // Start NPC

		private bool HasViking = false;
		private bool HasSeer = false;
		private bool HasRogue = false;
		private bool HasMystic = false;

		private int TeamBuildMobsKilled = 0;

		// Constructors
		public DailyTeamBuildingLvl1MidQuest() : base() {
		}

		public DailyTeamBuildingLvl1MidQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DailyTeamBuildingLvl1MidQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DailyTeamBuildingLvl1MidQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Isaac", ERealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 100 && npc.X == 766590 && npc.Y == 670407)
					{
						Isaac = npc;
						break;
					}

			if (Isaac == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Isaac , creating it ...");
				Isaac = new GameNpc();
				Isaac.Model = 774;
				Isaac.Name = "Isaac";
				Isaac.GuildName = "Advisor to the King";
				Isaac.Realm = ERealm.Midgard;
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Isaac, GameObjectEvent.Interact, new CoreEventHandler(TalkToIsaac));
			GameEventMgr.AddHandler(Isaac, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToIsaac));

			/* Now we bring to Dean the possibility to give this quest to players */
			Isaac.AddQuestToGive(typeof (DailyTeamBuildingLvl1MidQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Isaac == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Isaac, GameObjectEvent.Interact, new CoreEventHandler(TalkToIsaac));
			GameEventMgr.RemoveHandler(Isaac, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToIsaac));

			/* Now we remove to Dean the possibility to give this quest to players */
			Isaac.RemoveQuestToGive(typeof (DailyTeamBuildingLvl1MidQuest));
		}

		private static void TalkToIsaac(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Isaac.CanGiveQuest(typeof (DailyTeamBuildingLvl1MidQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DailyTeamBuildingLvl1MidQuest quest = player.IsDoingQuest(typeof (DailyTeamBuildingLvl1MidQuest)) as DailyTeamBuildingLvl1MidQuest;

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
							Isaac.SayTo(player, "Hello " + player.Name + ", did you [forge the bonds of unity]?");
							break;
					}
				}
				else
				{
					Isaac.SayTo(player, "Hello "+ player.Name +", I am Isaac. I help the king with logistics, and he's tasked me with getting things done around here. "+
					                       "The king recently implemented a new unity initiative and he wants you to help out.\n"+
					                       "What do you say, are you [feeling social]?");
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
						case "feeling social":
							player.Out.SendQuestSubscribeCommand(Isaac, QuestMgr.GetIDForQuestType(typeof(DailyTeamBuildingLvl1MidQuest)), "Will you help Dean "+questTitle+"");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "forge the bonds of unity":
							if (quest.Step == 2)
							{
								player.Out.SendMessage("I can feel our realm growing more cohesive every day!", EChatType.CT_Chat, EChatLoc.CL_PopupWindow);
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
			if (player.IsDoingQuest(typeof (DailyTeamBuildingLvl1MidQuest)) != null)
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
			TeamBuildMobsKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, TeamBuildMobsKilled.ToString());
		}


		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			DailyTeamBuildingLvl1MidQuest quest = player.IsDoingQuest(typeof (DailyTeamBuildingLvl1MidQuest)) as DailyTeamBuildingLvl1MidQuest;

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

		private static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DailyTeamBuildingLvl1MidQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Isaac.CanGiveQuest(typeof (DailyTeamBuildingLvl1MidQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DailyTeamBuildingLvl1MidQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping our realm prosper.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Isaac.GiveQuest(typeof (DailyTeamBuildingLvl1MidQuest), player, 1))
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
						if (HasViking
						    && HasSeer
						    && HasRogue
						    && HasMystic)
						{
							return "The spirit of unity flows through you. \n" +
							       "Kill orange con or higher mobs: \n" + 
							       "Orange+ Con Mobs Killed: ("+ TeamBuildMobsKilled +" | "+MAX_KILLED+")";
						}
						else
						{
							StringBuilder output = new StringBuilder("Kill orange con or higher mobs while in a group containing the following base classes:\n");
							if (!HasViking) output.Append("Viking required\n");
							if (!HasSeer) output.Append("Seer required\n");
							if (!HasRogue) output.Append("Rogue required\n");
							if (!HasMystic) output.Append("Mystic required\n");
							output.Append("Orange+ Con Mobs Killed: ("+ TeamBuildMobsKilled +" | "+MAX_KILLED+")");
							return output.ToString();
						}
						
					case 2:
						return "Return to Isaac in Svasud Faste for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(DailyTeamBuildingLvl1MidQuest)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;
			
			if (e != GameLivingEvent.EnemyKilled || Step != 1) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
			
			if (gArgs.Target is GameSummonedPet)
				return;
				
			if (player.Group != null)
			{
				foreach (var member in player.Group.GetMembersInTheGroup())
				{
					//update class counters
					if (member is GamePlayer gplayer)
					{
						if (gplayer.PlayerClass is ClassViking)
							HasViking = true;
						if (gplayer.PlayerClass is ClassSeer)
							HasSeer = true;
						if (gplayer.PlayerClass is ClassMidgardRogue)
							HasRogue = true;
						if (gplayer.PlayerClass is ClassMystic )
							HasMystic = true;
					}
				}
				player.Out.SendQuestUpdate(this);
			}
			else
			{
				//if we're ever ungrouped, clear our grouped class counters
				HasViking = false;
				HasSeer = false;
				HasRogue = false;
				HasMystic = false;
				player.Out.SendQuestUpdate(this);
			}

			if (!(player.GetConLevel(gArgs.Target) >= 1) || player.Group == null || !HasViking || !HasSeer ||
			    !HasRogue || !HasMystic) return;
			TeamBuildMobsKilled++;
			player.Out.SendMessage(
				"[Group Daily] Monster killed: (" + TeamBuildMobsKilled + " | " + MAX_KILLED + ")",
				EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (TeamBuildMobsKilled >= MAX_KILLED)
			{
				// FinishQuest or go back to npc
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "TeamBuildingMid";
			set { ; }
		}
		public override void FinishQuest()
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/5);
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level,50,Util.Random(50)), "You receive {0} as a reward.");
			CoreRoGMgr.GenerateReward(m_questPlayer, 300);
			TeamBuildMobsKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
