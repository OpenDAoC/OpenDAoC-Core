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

namespace DOL.GS.WeeklyQuest.Albion
{
	public class DFWeeklyKillQuestAlb : Quests.WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Weekly] Femurs From Darkness Falls";
		private const int minimumLevel = 15;
		private const int maximumLevel = 50;
		
		// prevent grey killing
		private const int MIN_PLAYER_CON = -3;
		// Kill Goal
		private const int MAX_KILLED = 50;

		private static GameNPC Joe = null; // Start NPC

		private int EnemiesKilled = 0;

		// Constructors
		public DFWeeklyKillQuestAlb() : base()
		{
		}

		public DFWeeklyKillQuestAlb(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DFWeeklyKillQuestAlb(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DFWeeklyKillQuestAlb(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Joe", eRealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 249 && npc.X == 32526 && npc.Y == 27679)
					{
						Joe = npc;
						break;
					}

			if (Joe == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Joe , creating it ...");
				Joe = new GameNPC();
				Joe.Model = 42;
				Joe.Name = "Joe";
				Joe.GuildName = "Realm Logistics";
				Joe.Realm = eRealm.Albion;
				//Darkness Falls Alb Entrance Location
				Joe.CurrentRegionID = 249;
				Joe.Size = 50;
				Joe.Level = 59;
				Joe.X = 32526;
				Joe.Y = 27679;
				Joe.Z = 22893;
				Joe.Heading = 466;
				Joe.Flags |= GameNPC.eFlags.PEACE;
				GameNpcInventoryTemplate templateAlb = new GameNpcInventoryTemplate();
				templateAlb.AddNPCEquipment(eInventorySlot.TorsoArmor, 713,0,0,3);
				templateAlb.AddNPCEquipment(eInventorySlot.LegsArmor, 714);
				templateAlb.AddNPCEquipment(eInventorySlot.ArmsArmor, 715);
				templateAlb.AddNPCEquipment(eInventorySlot.HandsArmor, 716, 0,0,3);
				templateAlb.AddNPCEquipment(eInventorySlot.FeetArmor, 717, 0, 0, 3);
				templateAlb.AddNPCEquipment(eInventorySlot.Cloak, 676);
				Joe.Inventory = templateAlb.CloseTemplate();
				Joe.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Joe.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Joe, GameObjectEvent.Interact, new DOLEventHandler(TalkToJoe));
			GameEventMgr.AddHandler(Joe, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToJoe));

			Joe.AddQuestToGive(typeof (DFWeeklyKillQuestAlb));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Joe == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Joe, GameObjectEvent.Interact, new DOLEventHandler(TalkToJoe));
			GameEventMgr.RemoveHandler(Joe, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToJoe));

			Joe.RemoveQuestToGive(typeof (DFWeeklyKillQuestAlb));
		}

		private static void TalkToJoe(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Joe.CanGiveQuest(typeof (DFWeeklyKillQuestAlb), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DFWeeklyKillQuestAlb quest = player.IsDoingQuest(typeof (DFWeeklyKillQuestAlb)) as DFWeeklyKillQuestAlb;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Joe.SayTo(player, "Please head into Darkness Falls and defend Albion from enemies!");
							break;
						case 2:
							Joe.SayTo(player, "Hello " + player.Name + ", did you [find the bones] we needed?");
							break;
					}
				}
				else
				{
					Joe.SayTo(player, "Oh, "+ player.Name +", glad you finally returned. Boss has a new recipe that requires bones that have been steeped in a [demonic aura]. \n"+
					                     "Sure hope you know what that means, because I sure don't. My best guess is to try looking in Darkness Falls.");
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
						case "demonic aura":
							player.Out.SendQuestSubscribeCommand(Joe, QuestMgr.GetIDForQuestType(typeof(DFWeeklyKillQuestAlb)), "Will you help Joe "+questTitle+"?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "find the bones":
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
			if (player.IsDoingQuest(typeof (DFWeeklyKillQuestAlb)) != null)
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
			EnemiesKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, EnemiesKilled.ToString());
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			DFWeeklyKillQuestAlb quest = player.IsDoingQuest(typeof (DFWeeklyKillQuestAlb)) as DFWeeklyKillQuestAlb;

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

		private static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DFWeeklyKillQuestAlb)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Joe.CanGiveQuest(typeof (DFWeeklyKillQuestAlb), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DFWeeklyKillQuestAlb)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping me out.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Joe.GiveQuest(typeof (DFWeeklyKillQuestAlb), player, 1))
					return;

				Joe.SayTo(player, "Find your realm's enemies in Darkness Falls and kill them for your reward.");

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
						return "Defend Albion in Darkness Falls. \nKilled: Enemies ("+ EnemiesKilled +" | 50)";
					case 2:
						return "Return to Joe in Darkness Falls for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(DFWeeklyKillQuestAlb)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Realm == 0 || gArgs.Target.Realm == player.Realm || gArgs.Target is not GamePlayer ||
			    !(player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON) || gArgs.Target.CurrentRegionID != 249) return;
			EnemiesKilled++;
			player.Out.SendMessage("[Weekly] Enemy Killed: ("+EnemiesKilled+" | "+MAX_KILLED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (EnemiesKilled >= MAX_KILLED)
			{
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "DFWeeklyKillQuestAlb";
			set { ; }
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			int reward = ServerProperties.Properties.WEEKLY_RVR_REWARD;
			
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel), false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level * 5,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 1500);
			EnemiesKilled = 0;
			
			if (reward > 0)
			{
				m_questPlayer.Out.SendMessage($"You have been rewarded {reward} Realmpoints for finishing Weekly Quest.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				m_questPlayer.GainRealmPoints(reward, false);
			}
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
