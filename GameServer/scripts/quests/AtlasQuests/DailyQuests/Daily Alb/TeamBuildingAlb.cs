using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.API;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerClass;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests;
using log4net;

namespace DOL.GS.DailyQuest.Albion
{
	public class TeamBuildingAlb : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Group Daily] A Team Building Exercise";
		protected const int minimumLevel = 1;
		protected const int maximumLevel = 50;

		// Kill Goal
		protected const int MAX_KILLED = 50;
		
		private static GameNPC Cola = null; // Start NPC

		private bool HasFighter = false;
		private bool HasAcolyte = false;
		private bool HasRogue = false;
		private bool HasMageElemDisc = false;

		private int TeamBuildMobsKilled = 0;

		// Constructors
		public TeamBuildingAlb() : base() {
		}

		public TeamBuildingAlb(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public TeamBuildingAlb(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public TeamBuildingAlb(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Cola", eRealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 1 && npc.X == 583860 && npc.Y == 477619)
					{
						Cola = npc;
						break;
					}

			if (Cola == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Cola , creating it ...");
				Cola = new GameNPC();
				Cola.Model = 724;
				Cola.Name = "Cola";
				Cola.GuildName = "Advisor to the King";
				Cola.Realm = eRealm.Albion;
				Cola.CurrentRegionID = 1;
				Cola.Size = 50;
				Cola.Level = 59;
				//Castle Sauvage Location
				Cola.X = 583860;
				Cola.Y = 477619;
				Cola.Z = 2600;
				Cola.Heading = 3111;
				Cola.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Cola.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Cola, GameObjectEvent.Interact, new DOLEventHandler(TalkToCola));
			GameEventMgr.AddHandler(Cola, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToCola));

			/* Now we bring to Dean the possibility to give this quest to players */
			Cola.AddQuestToGive(typeof (TeamBuildingAlb));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Cola == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Cola, GameObjectEvent.Interact, new DOLEventHandler(TalkToCola));
			GameEventMgr.RemoveHandler(Cola, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToCola));

			/* Now we remove to Dean the possibility to give this quest to players */
			Cola.RemoveQuestToGive(typeof (TeamBuildingAlb));
		}

		protected static void TalkToCola(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Cola.CanGiveQuest(typeof (TeamBuildingAlb), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			TeamBuildingAlb quest = player.IsDoingQuest(typeof (TeamBuildingAlb)) as TeamBuildingAlb;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Cola.SayTo(player, "Kill creatures in any RvR zone to help us clear more room for the armies to maneuver around.");
							break;
						case 2:
							Cola.SayTo(player, "Hello " + player.Name + ", did you [forge the bonds of unity]?");
							break;
					}
				}
				else
				{
					Cola.SayTo(player, "Hello "+ player.Name +", I am Cola. I help the king with logistics, and he's tasked me with getting things done around here. "+
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
							player.Out.SendQuestSubscribeCommand(Cola, QuestMgr.GetIDForQuestType(typeof(TeamBuildingAlb)), "Will you help Dean "+questTitle+"");
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
								player.Out.SendMessage("I can feel our realm growing more cohesive every day!", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
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
			if (player.IsDoingQuest(typeof (TeamBuildingAlb)) != null)
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
			TeamBuildingAlb quest = player.IsDoingQuest(typeof (TeamBuildingAlb)) as TeamBuildingAlb;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(TeamBuildingAlb)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Cola.CanGiveQuest(typeof (TeamBuildingAlb), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (TeamBuildingAlb)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping our realm prosper.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Cola.GiveQuest(typeof (TeamBuildingAlb), player, 1))
					return;

				Cola.SayTo(player, "Killing creatures in any RvR zone will work. Thanks for your service!");

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
							       "Orange+ Con Mobs Killed: ("+ TeamBuildMobsKilled +" | 25)";
						}
						else
						{
							StringBuilder output = new StringBuilder("Kill orange con or higher mobs while in a group containing the following base classes:\n");
							if (!HasFighter) output.Append("Fighter required\n");
							if (!HasAcolyte) output.Append("Acolyte required\n");
							if (!HasRogue) output.Append("Rogue required\n");
							if (!HasMageElemDisc) output.Append("Mage/Elementalist/Disciple required\n");
							output.Append("Orange+ Con Mobs Killed: ("+ TeamBuildMobsKilled +" | 25)");
							return output.ToString();
						}
						
					case 2:
						return "Return to Cola in Castle Sauvage for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(TeamBuildingAlb)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;

			if (player.Group != null)
			{
				foreach (var member in player.Group.GetMembersInTheGroup())
				{
					//update class counters
					if (member is GamePlayer gplayer)
					{
						if (gplayer.CharacterClass is ClassFighter)
							HasFighter = true;
						if (gplayer.CharacterClass is ClassAcolyte)
							HasAcolyte = true;
						if (gplayer.CharacterClass is ClassAlbionRogue)
							HasRogue = true;
						if (gplayer.CharacterClass is ClassMage ||
						    gplayer.CharacterClass is ClassElementalist ||
						    gplayer.CharacterClass is ClassDisciple)
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
			
			if (e == GameLivingEvent.EnemyKilled && Step == 1)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (player.GetConLevel(gArgs.Target) >= 1 &&
				    player.Group != null &&
				    HasFighter && HasAcolyte && HasRogue && HasMageElemDisc) 
				{
					TeamBuildMobsKilled++;
					player.Out.SendQuestUpdate(this);
					
					if (TeamBuildMobsKilled >= MAX_KILLED)
					{
						// FinishQuest or go back to npc
						Step = 2;
					}
				}
				
			}
			
		}
		
		public override string QuestPropertyKey
		{
			get => "TeamBuildingAlb";
			set { ; }
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/5, false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level,50,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 300);
			TeamBuildMobsKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
