using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Packets;
using Core.GS.Packets.Server;
using Core.GS.Quests;
using log4net;

namespace Core.GS.DailyQuest
{
	public class HardcoreKillOrangesLvl1AlbQuest : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Hardcore] Big Man On Campus";
		private const int minimumLevel = 1;
		private const int maximumLevel = 49;

		private static GameNpc SucciAlb = null; // Start NPC

		private int OrangeConKilled = 0;
		private int MAX_KillGoal = 10;

		// Constructors
		public HardcoreKillOrangesLvl1AlbQuest() : base()
		{
		}

		public HardcoreKillOrangesLvl1AlbQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public HardcoreKillOrangesLvl1AlbQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public HardcoreKillOrangesLvl1AlbQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Succi", ERealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
				{
					if (npc.CurrentRegionID == 1 && npc.X == 584652 && npc.Y == 477773)
					{
						SucciAlb = npc;
						break;
					}
				}

			if (SucciAlb == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find SucciAlb , creating it ...");
				SucciAlb = new GameNpc();
				SucciAlb.Model = 902;
				SucciAlb.Name = "Succi";
				SucciAlb.GuildName = "Spectre of Death";
				SucciAlb.Realm = ERealm.Albion;
				//Sauvage Location
				SucciAlb.CurrentRegionID = 1;
				SucciAlb.Size = 60;
				SucciAlb.Level = 59;
				SucciAlb.X = 584652;
				SucciAlb.Y = 477773;
				SucciAlb.Z = 2600;
				SucciAlb.Heading = 2257;
				SucciAlb.Flags |= ENpcFlags.PEACE;
				SucciAlb.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					SucciAlb.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(SucciAlb, GameObjectEvent.Interact, new CoreEventHandler(TalkToSucci));
			GameEventMgr.AddHandler(SucciAlb, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToSucci));

			/* Now we bring to Dean the possibility to give this quest to players */
			SucciAlb.AddQuestToGive(typeof (HardcoreKillOrangesLvl1AlbQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (SucciAlb == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(SucciAlb, GameObjectEvent.Interact, new CoreEventHandler(TalkToSucci));
			GameEventMgr.RemoveHandler(SucciAlb, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToSucci));

			/* Now we remove to Dean the possibility to give this quest to players */
			SucciAlb.RemoveQuestToGive(typeof (HardcoreKillOrangesLvl1AlbQuest));
		}

		private static void TalkToSucci(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(SucciAlb.CanGiveQuest(typeof (HardcoreKillOrangesLvl1AlbQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			HardcoreKillOrangesLvl1AlbQuest oranges = player.IsDoingQuest(typeof (HardcoreKillOrangesLvl1AlbQuest)) as HardcoreKillOrangesLvl1AlbQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (oranges != null)
				{
					switch (oranges.Step)
					{
						case 1:
							SucciAlb.SayTo(player, "Seek out creatures greater in strength than you and cast them into the abyss.");
							break;
						case 2:
							SucciAlb.SayTo(player, "" + player.Name + ". You have earned [another sunrise].");
							break;
					}
				}
				else
				{
					SucciAlb.SayTo(player, ""+ player.Name +". I have seen visions of your death. "+
					                     "Crushed beneath the blow of a mighty foe. Dashed against the rocks of eternity."+
					                     "\n Will you defy them? Stand tall and let the spirits know [today is not the day].");
					SucciAlb.SayTo(player, " NOTE: This is a HARDCORE quest. If you die or join a group while doing this quest, it will be aborted automatically.");
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (oranges == null)
				{
					switch (wArgs.Text)
					{
						case "today is not the day":
							player.Out.SendQuestSubscribeCommand(SucciAlb, QuestMgr.GetIDForQuestType(typeof(HardcoreKillOrangesLvl1AlbQuest)), "Will you undertake " + questTitle + "?");
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
								player.Out.SendMessage("From dust we are born, and to dust we return. Your time will come eventually.", EChatType.CT_Chat, EChatLoc.CL_PopupWindow);
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
			if (player.IsDoingQuest(typeof (HardcoreKillOrangesLvl1AlbQuest)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			return player.Level >= minimumLevel && player.Level <= maximumLevel;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			HardcoreKillOrangesLvl1AlbQuest oranges = player.IsDoingQuest(typeof (HardcoreKillOrangesLvl1AlbQuest)) as HardcoreKillOrangesLvl1AlbQuest;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(HardcoreKillOrangesLvl1AlbQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(SucciAlb.CanGiveQuest(typeof (HardcoreKillOrangesLvl1AlbQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (HardcoreKillOrangesLvl1AlbQuest)) != null)
				return;

			if (player.Group != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("The titans shall tremble.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!SucciAlb.GiveQuest(typeof (HardcoreKillOrangesLvl1AlbQuest), player, 1))
					return;

				SucciAlb.SayTo(player, "Seek out creatures greater in strength than you and cast them into the abyss.");

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
						return "Kill mobs orange con or higher. \n Orange Con Monsters Killed: ("+ OrangeConKilled +" | "+MAX_KillGoal+")";
					case 2:
						return "Return to Succi in Castle Sauvage for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(HardcoreKillOrangesLvl1AlbQuest)) == null)
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
			
			if (!(player.GetConLevel(gArgs.Target) > 0)) return;
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
			OrangeConKilled++;
			player.Out.SendMessage("[Hardcore] Monster Killed: ("+OrangeConKilled+" | "+MAX_KillGoal+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (OrangeConKilled >= MAX_KillGoal)
			{
				// FinishQuest or go back to npc
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "HardcorePlayerKillQuestAlb";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			OrangeConKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, OrangeConKilled.ToString());
		}
		

		public override void FinishQuest()
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/2);
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level*2,32,Util.Random(50)), "You receive {0} as a reward.");
			CoreRogMgr.GenerateReward(m_questPlayer, 150);
			OrangeConKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}

		private void FailQuest()
		{
			OrangeConKilled = 0;
			m_questPlayer.Out.SendMessage(questTitle + " failed.", EChatType.CT_ScreenCenter_And_CT_System, EChatLoc.CL_SystemWindow);
			Step = -1;

			if (m_questPlayer.QuestList.TryRemove(this, out byte value))
				m_questPlayer.AvailableQuestIndexes.Enqueue(value);

			m_questPlayer.AddFinishedQuest(this);
			m_questPlayer.Out.SendQuestListUpdate();
		}
	}
}
