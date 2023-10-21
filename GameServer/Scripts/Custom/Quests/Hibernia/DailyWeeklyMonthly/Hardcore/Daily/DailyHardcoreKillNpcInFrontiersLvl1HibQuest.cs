using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;
using Core.GS.Packets;
using Core.GS.Quests;
using log4net;

namespace Core.GS.DailyQuest
{
	public class DailyHardcoreKillNpcInFrontiersLvl1HibQuest : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Hardcore] A Lot Of Bravery";
		private const int minimumLevel = 1;
		private const int maximumLevel = 50;

		private static GameNpc SucciHib = null; // Start NPC

		private int FrontierMobsKilled = 0;
		private int MAX_KillGoal = 25;

		// Constructors
		public DailyHardcoreKillNpcInFrontiersLvl1HibQuest() : base()
		{
		}

		public DailyHardcoreKillNpcInFrontiersLvl1HibQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DailyHardcoreKillNpcInFrontiersLvl1HibQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DailyHardcoreKillNpcInFrontiersLvl1HibQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Succi", ERealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
				{
					if (npc.CurrentRegionID == 200 && npc.X == 335117 && npc.Y == 420642)
					{
						SucciHib = npc;
						break;
					}
				}

			if (SucciHib == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find SucciHib , creating it ...");
				SucciHib = new GameNpc();
				SucciHib.Model = 902;
				SucciHib.Name = "Succi";
				SucciHib.GuildName = "Spectre of Death";
				SucciHib.Realm = ERealm.Hibernia;
				//Svasud Location
				SucciHib.CurrentRegionID = 200;
				SucciHib.Size = 60;
				SucciHib.Level = 59;
				SucciHib.X = 335117;
				SucciHib.Y = 420642;
				SucciHib.Z = 5195;
				SucciHib.Heading = 1600;
				SucciHib.Flags |= ENpcFlags.PEACE;
				SucciHib.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					SucciHib.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(SucciHib, GameObjectEvent.Interact, new CoreEventHandler(TalkToSucci));
			GameEventMgr.AddHandler(SucciHib, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToSucci));

			/* Now we bring to Dean the possibility to give this quest to players */
			SucciHib.AddQuestToGive(typeof (DailyHardcoreKillNpcInFrontiersLvl1HibQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (SucciHib == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(SucciHib, GameObjectEvent.Interact, new CoreEventHandler(TalkToSucci));
			GameEventMgr.RemoveHandler(SucciHib, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToSucci));

			/* Now we remove to Dean the possibility to give this quest to players */
			SucciHib.RemoveQuestToGive(typeof (DailyHardcoreKillNpcInFrontiersLvl1HibQuest));
		}

		private static void TalkToSucci(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(SucciHib.CanGiveQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1HibQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DailyHardcoreKillNpcInFrontiersLvl1HibQuest oranges = player.IsDoingQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1HibQuest)) as DailyHardcoreKillNpcInFrontiersLvl1HibQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (oranges != null)
				{
					switch (oranges.Step)
					{
						case 1:
							SucciHib.SayTo(player, "Exemplify bravery.");
							break;
						case 2:
							SucciHib.SayTo(player, "" + player.Name + ". You have earned [another sunrise].");
							break;
					}
				}
				else
				{
					SucciHib.SayTo(player, "One wonders if it is possible to be brave when one is afraid. \n" +
					                       "Perhaps, that is the only time that [one can be brave].");
					SucciHib.SayTo(player, " NOTE: This is a HARDCORE quest. If you die or join a group while doing this quest, it will be aborted automatically.");
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
							player.Out.SendQuestSubscribeCommand(SucciHib, QuestMgr.GetIDForQuestType(typeof(DailyHardcoreKillNpcInFrontiersLvl1HibQuest)), "Will you undertake " + questTitle + "?");
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
			if (player.IsDoingQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1HibQuest)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			DailyHardcoreKillNpcInFrontiersLvl1HibQuest oranges = player.IsDoingQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1HibQuest)) as DailyHardcoreKillNpcInFrontiersLvl1HibQuest;

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

		protected static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DailyHardcoreKillNpcInFrontiersLvl1HibQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(SucciHib.CanGiveQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1HibQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1HibQuest)) != null)
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
				if (!SucciHib.GiveQuest(typeof (DailyHardcoreKillNpcInFrontiersLvl1HibQuest), player, 1))
					return;

				SucciHib.SayTo(player, "Be careful, the line between bravery and foolishness can be a thin one.");

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
						return "Return to Succi in Druim Ligen for your grim reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(DailyHardcoreKillNpcInFrontiersLvl1HibQuest)) == null)
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
			player.Out.SendMessage("[Hardcore] Monster Killed: ("+FrontierMobsKilled+" | "+MAX_KillGoal+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (FrontierMobsKilled >= MAX_KillGoal)
			{
				// FinishQuest or go back to npc
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "HardcoreKillNPCInFrontiersHib";
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
			CoreRogMgr.GenerateReward(m_questPlayer, 150);
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
