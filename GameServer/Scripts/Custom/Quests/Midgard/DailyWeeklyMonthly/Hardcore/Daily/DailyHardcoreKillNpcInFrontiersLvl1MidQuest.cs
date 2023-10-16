using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Quests;
using log4net;

namespace DOL.GS.DailyQuest
{
	public class DailyHardcoreKillNpcInFrontiersLvl1MidQuest : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Hardcore] A Lot Of Bravery";
		private const int minimumLevel = 1;
		private const int maximumLevel = 50;

		private static GameNpc SucciMid = null; // Start NPC

		private int FrontierMobsKilled = 0;
		private int MAX_KillGoal = 25;

		// Constructors
		public DailyHardcoreKillNpcInFrontiersLvl1MidQuest() : base()
		{
		}

		public DailyHardcoreKillNpcInFrontiersLvl1MidQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DailyHardcoreKillNpcInFrontiersLvl1MidQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DailyHardcoreKillNpcInFrontiersLvl1MidQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Succi", ERealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
				{
					if (npc.CurrentRegionID == 100 && npc.X == 766767 && npc.Y == 670636)
					{
						SucciMid = npc;
						break;
					}
				}

			if (SucciMid == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find SucciMid , creating it ...");
				SucciMid = new GameNpc();
				SucciMid.Model = 902;
				SucciMid.Name = "Succi";
				SucciMid.GuildName = "Spectre of Death";
				SucciMid.Realm = ERealm.Midgard;
				//Svasud Location
				SucciMid.CurrentRegionID = 100;
				SucciMid.Size = 60;
				SucciMid.Level = 59;
				SucciMid.X = 766767;
				SucciMid.Y = 670636;
				SucciMid.Z = 5736;
				SucciMid.Heading = 2536;
				SucciMid.Flags |= ENpcFlags.PEACE;
				SucciMid.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					SucciMid.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(SucciMid, GameObjectEvent.Interact, new CoreEventHandler(TalkToSucci));
			GameEventMgr.AddHandler(SucciMid, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToSucci));

			/* Now we bring to Dean the possibility to give this quest to players */
			SucciMid.AddQuestToGive(typeof (DailyHardcoreKillNpcInFrontiersLvl1MidQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (SucciMid == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(SucciMid, GameObjectEvent.Interact, new CoreEventHandler(TalkToSucci));
			GameEventMgr.RemoveHandler(SucciMid, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToSucci));

			/* Now we remove to Dean the possibility to give this quest to players */
			SucciMid.RemoveQuestToGive(typeof (DailyHardcoreKillNpcInFrontiersLvl1MidQuest));
		}

		private static void TalkToSucci(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(SucciMid.CanGiveQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1MidQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DailyHardcoreKillNpcInFrontiersLvl1MidQuest oranges = player.IsDoingQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1MidQuest)) as DailyHardcoreKillNpcInFrontiersLvl1MidQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (oranges != null)
				{
					switch (oranges.Step)
					{
						case 1:
							SucciMid.SayTo(player, "Exemplify bravery.");
							break;
						case 2:
							SucciMid.SayTo(player, "" + player.Name + ". You have earned [another sunrise].");
							break;
					}
				}
				else
				{
					SucciMid.SayTo(player, "One wonders if it is possible to be brave when one is afraid. \n" +
					                       "Perhaps, that is the only time that [one can be brave].");
					SucciMid.SayTo(player, " NOTE: This is a HARDCORE quest. If you die or join a group while doing this quest, it will be aborted automatically.");
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (oranges == null)
				{
					switch (wArgs.Text.ToLower())
					{
						case "one can be brave":
							player.Out.SendQuestSubscribeCommand(SucciMid, QuestMgr.GetIDForQuestType(typeof(DailyHardcoreKillNpcInFrontiersLvl1MidQuest)), "Will you undertake " + questTitle + "?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "another sunrise":
							if (oranges.Step == 2)
							{
								player.Out.SendMessage("Tell me, did you face your fears?", EChatType.CT_Chat, EChatLoc.CL_PopupWindow);
								oranges.FinishQuest();
							}
							break;
						case "abort":
							player.Out.SendCustomDialog("To face one's own demise is not for the faint of heart. Death has turned its back on you for today.", new CustomDialogResponse(CheckPlayerAbortQuest));
							break;
					}
				}
			}
		}
		
		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1MidQuest)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			DailyHardcoreKillNpcInFrontiersLvl1MidQuest oranges = player.IsDoingQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1MidQuest)) as DailyHardcoreKillNpcInFrontiersLvl1MidQuest;

			if (oranges == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "To face one's own demise is not for the faint of heart.");
			}
			else
			{
				SendSystemMessage(player, "Aborting Quest " + questTitle + ".");
				oranges.AbortQuest();
			}
		}

		private static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DailyHardcoreKillNpcInFrontiersLvl1MidQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(SucciMid.CanGiveQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1MidQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1MidQuest)) != null)
				return;

			if (player.Group != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Watch your back.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!SucciMid.GiveQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1MidQuest), player, 1))
					return;

				SucciMid.SayTo(player, "Be careful, the line between bravery and foolishness can be a thin one.");

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
					case -1:
						return "Your deeds are done for today.";
					case 1:
						return "Kill 25 mobs in a frontier zone. \n Creatures Killed: ("+ FrontierMobsKilled +" | "+MAX_KillGoal+")";
					case 2:
						return "Return to Succi in Svasud Faste for your grim reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;


			if (player?.IsDoingQuest(typeof(DailyHardcoreKillNpcInFrontiersLvl1MidQuest)) == null)
				return;

			if (player.Group != null && Step == 1)
			{
				FailQuest();
				return;
			}
				

			if (sender != m_questPlayer)
				return;

			if (e == GameLivingEvent.Dying && Step == 1)
			{
				FailQuest();
				return;
			}

			if (e != GameLivingEvent.EnemyKilled || Step != 1) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
			
			if (gArgs.Target is GameSummonedPet)
				return;
			if (!(player.GetConLevel(gArgs.Target) > -1) || !gArgs.Target.CurrentZone.IsRvR ||
			    !player.CurrentZone.IsRvR) return;
			if (gArgs.Target.XPGainers.Count > 1)
			{
				Array gainers = new GameObject[gArgs.Target.XPGainers.Count];
				lock (gArgs.Target._xpGainersLock)
				{

					foreach (GameLiving living in gArgs.Target.XPGainers.Keys)
					{
						if (living == player ||
						    (player.ControlledBrain is {Body: { }} && player.ControlledBrain.Body == living) ||
						    (living is BonedancerPet bdpet &&
						     (bdpet.Owner == player || bdpet.Owner == player.ControlledBrain?.Body)))
							continue;

						return;
					}
				}
			}
			FrontierMobsKilled++;
			player.Out.SendMessage("[Hardcore] Monster Killed: (" + FrontierMobsKilled + " | " + MAX_KillGoal + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (FrontierMobsKilled >= MAX_KillGoal)
			{
				// FinishQuest or go back to npc
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "HardcoreKillNPCInFrontiersMid";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			FrontierMobsKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, FrontierMobsKilled.ToString());
		}

		public override void FinishQuest()
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/2);
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level*2,32,Util.Random(50)), "You receive {0} as a reward.");
			CoreRoGMgr.GenerateReward(m_questPlayer, 150);
			FrontierMobsKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}

		private void FailQuest()
		{
			m_questPlayer.Out.SendMessage(questTitle + " failed.", EChatType.CT_ScreenCenter_And_CT_System, EChatLoc.CL_SystemWindow);
			FrontierMobsKilled = 0;
			Step = -1;

			if (m_questPlayer.QuestList.TryRemove(this, out byte value))
				m_questPlayer.AvailableQuestIndexes.Enqueue(value);

			m_questPlayer.AddFinishedQuest(this);
			m_questPlayer.Out.SendQuestListUpdate();
		}
	}
}
