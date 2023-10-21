using System;
using System.Reflection;
using System.Text;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Packets;
using Core.GS.Packets.Server;
using Core.GS.PlayerClass;
using Core.GS.Quests;
using log4net;

namespace Core.GS.DailyQuest.Albion
{
	public class DailyTeamBuildingLvl1AlbQuest : Quests.DailyQuest
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
		
		private static GameNpc Hector = null; // Start NPC

		private bool HasFighter = false;
		private bool HasAcolyte = false;
		private bool HasRogue = false;
		private bool HasMageElemDisc = false;

		private int TeamBuildMobsKilled = 0;

		// Constructors
		public DailyTeamBuildingLvl1AlbQuest() : base() {
		}

		public DailyTeamBuildingLvl1AlbQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DailyTeamBuildingLvl1AlbQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DailyTeamBuildingLvl1AlbQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Hector", ERealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 1 && npc.X == 583860 && npc.Y == 477619)
					{
						Hector = npc;
						break;
					}

			if (Hector == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Hector , creating it ...");
				Hector = new GameNpc();
				Hector.Model = 716;
				Hector.Name = "Hector";
				Hector.GuildName = "Advisor to the King";
				Hector.Realm = ERealm.Albion;
				Hector.CurrentRegionID = 1;
				Hector.Size = 50;
				Hector.Level = 59;
				//Castle Sauvage Location
				Hector.X = 583860;
				Hector.Y = 477619;
				Hector.Z = 2600;
				Hector.Heading = 3111;
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Hector, GameObjectEvent.Interact, new CoreEventHandler(TalkToHector));
			GameEventMgr.AddHandler(Hector, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToHector));

			/* Now we bring to Hector the possibility to give this quest to players */
			Hector.AddQuestToGive(typeof (DailyTeamBuildingLvl1AlbQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Hector == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Hector, GameObjectEvent.Interact, new CoreEventHandler(TalkToHector));
			GameEventMgr.RemoveHandler(Hector, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToHector));

			/* Now we remove to Hector the possibility to give this quest to players */
			Hector.RemoveQuestToGive(typeof (DailyTeamBuildingLvl1AlbQuest));
		}

		protected static void TalkToHector(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Hector.CanGiveQuest(typeof (DailyTeamBuildingLvl1AlbQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DailyTeamBuildingLvl1AlbQuest quest = player.IsDoingQuest(typeof (DailyTeamBuildingLvl1AlbQuest)) as DailyTeamBuildingLvl1AlbQuest;

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
							Hector.SayTo(player, "Hello " + player.Name + ", did you [forge the bonds of unity]?");
							break;
					}
				}
				else
				{
					Hector.SayTo(player, "Hello "+ player.Name +", I am Hector. I help the king with logistics, and he's tasked me with getting things done around here. "+
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
							player.Out.SendQuestSubscribeCommand(Hector, QuestMgr.GetIDForQuestType(typeof(DailyTeamBuildingLvl1AlbQuest)), "Will you help Hector "+questTitle+"");
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
			if (player.IsDoingQuest(typeof (DailyTeamBuildingLvl1AlbQuest)) != null)
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
			DailyTeamBuildingLvl1AlbQuest quest = player.IsDoingQuest(typeof (DailyTeamBuildingLvl1AlbQuest)) as DailyTeamBuildingLvl1AlbQuest;

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

		protected static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DailyTeamBuildingLvl1AlbQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Hector.CanGiveQuest(typeof (DailyTeamBuildingLvl1AlbQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DailyTeamBuildingLvl1AlbQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping our realm prosper.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Hector.GiveQuest(typeof (DailyTeamBuildingLvl1AlbQuest), player, 1))
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
						if (HasFighter
						    && HasAcolyte
						    && HasRogue
						    && HasMageElemDisc)
						{
							return "The spirit of unity flows through you. \n" +
							       "Kill orange con or higher mobs: \n" + 
							       "Orange+ Con Mobs Killed: ("+ TeamBuildMobsKilled +" | "+MAX_KILLED+")";
						}
						else
						{
							StringBuilder output = new StringBuilder("Kill orange con or higher mobs while in a group containing the following base classes:\n");
							if (!HasFighter) output.Append("Fighter required\n");
							if (!HasAcolyte) output.Append("Acolyte required\n");
							if (!HasRogue) output.Append("Rogue required\n");
							if (!HasMageElemDisc) output.Append("Mage/Elementalist/Disciple required\n");
							output.Append("Orange+ Con Mobs Killed: ("+ TeamBuildMobsKilled +" | "+MAX_KILLED+")");
							return output.ToString();
						}
						
					case 2:
						return "Return to Hector in Castle Sauvage for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(DailyTeamBuildingLvl1AlbQuest)) == null)
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
						if (gplayer.PlayerClass is ClassFighter)
							HasFighter = true;
						if (gplayer.PlayerClass is ClassAcolyte)
							HasAcolyte = true;
						if (gplayer.PlayerClass is ClassAlbionRogue)
							HasRogue = true;
						if (gplayer.PlayerClass is ClassMage ||
						    gplayer.PlayerClass is ClassElementalist || gplayer.PlayerClass is ClassDisciple)
							HasMageElemDisc = true;
					}
				}
				player.Out.SendQuestUpdate(this);
			}
			else
			{
				//if we're ever ungrouped, clear our grouped class counters
				HasFighter = false;
				HasAcolyte = false;
				HasRogue = false;
				HasMageElemDisc = false;
				player.Out.SendQuestUpdate(this);
			}

			if (!(player.GetConLevel(gArgs.Target) >= 1) || player.Group == null || !HasFighter || !HasAcolyte ||
			    !HasRogue || !HasMageElemDisc) return;
			
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
			get => "TeamBuildingAlb";
			set { ; }
		}

		public override void FinishQuest()
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/5);
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level,50,Util.Random(50)), "You receive {0} as a reward.");
			CoreRogMgr.GenerateReward(m_questPlayer, 300);
			TeamBuildMobsKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
