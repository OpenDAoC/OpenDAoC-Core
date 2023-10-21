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

namespace Core.GS.DailyQuest.Hibernia
{
	public class ThidKillLvl20HibQuest : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Daily] Fen's New Friends";
		private const int minimumLevel = 20;
		private const int maximumLevel = 24;

		private static GameNpc PazzHib = null; // Start NPC

		private int PlayersKilled = 0;
		private const int MAX_KILLED = 10;
		
		// prevent grey killing
		private const int MIN_PLAYER_CON = -3;

		// Constructors
		public ThidKillLvl20HibQuest() : base()
		{
		}

		public ThidKillLvl20HibQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public ThidKillLvl20HibQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public ThidKillLvl20HibQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Pazz", ERealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
				{
					if (npc.CurrentRegionID == 252 && npc.X == 18658 && npc.Y == 18710)
					{
						PazzHib = npc;
						break;
					}
				}

			if (PazzHib == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find PazzHib, creating it ...");
				PazzHib = new GameNpc();
				PazzHib.Model = 26;
				PazzHib.Name = "Pazz";
				PazzHib.GuildName = "Bone Collector";
				PazzHib.Realm = ERealm.Hibernia;
				//Druim Ligen Location
				PazzHib.CurrentRegionID = 252;
				PazzHib.Size = 40;
				PazzHib.Level = 59;
				PazzHib.X = 18658;
				PazzHib.Y = 18710;
				PazzHib.Z = 4320;
				PazzHib.Heading = 1424;
				PazzHib.Flags |= ENpcFlags.PEACE;
				PazzHib.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					PazzHib.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(PazzHib, GameObjectEvent.Interact, new CoreEventHandler(TalkToPazz));
			GameEventMgr.AddHandler(PazzHib, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToPazz));

			/* Now we bring to Dean the possibility to give this quest to players */
			PazzHib.AddQuestToGive(typeof (ThidKillLvl20HibQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (PazzHib == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(PazzHib, GameObjectEvent.Interact, new CoreEventHandler(TalkToPazz));
			GameEventMgr.RemoveHandler(PazzHib, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToPazz));

			/* Now we remove to Dean the possibility to give this quest to players */
			PazzHib.RemoveQuestToGive(typeof (ThidKillLvl20HibQuest));
		}

		private static void TalkToPazz(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(PazzHib.CanGiveQuest(typeof (ThidKillLvl20HibQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			ThidKillLvl20HibQuest quest = player.IsDoingQuest(typeof (ThidKillLvl20HibQuest)) as ThidKillLvl20HibQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							PazzHib.SayTo(player, "You will find suitable players in the battlegrounds.");
							break;
						case 2:
							PazzHib.SayTo(player, "Hello " + player.Name + ", did you [hit your quota]?");
							break;
					}
				}
				else
				{
					PazzHib.SayTo(player, "Hello "+ player.Name +", I am Pazz. My master, Fen, has tasked me with collecting bones for a project he's working on. "+
					                     "I'm way behind quota and could use some... subcontractors to [help me out]. \n\n"+
					                     "\nCan you lend me a hand? A leg could probably work too.");
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
						case "help me out":
							player.Out.SendQuestSubscribeCommand(PazzHib, QuestMgr.GetIDForQuestType(typeof(ThidKillLvl20HibQuest)), "Will you undertake " + questTitle + "?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "hit your quota":
							if (quest.Step == 2)
							{
								player.Out.SendMessage("Ugh, some of these are still dripping. Well done, he'll be pleased.", EChatType.CT_Chat, EChatLoc.CL_PopupWindow);
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

		public override string QuestPropertyKey
		{
			get => "ThidKillQuestHib";
			set { ; }
		}

		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (ThidKillLvl20HibQuest)) != null)
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
			PlayersKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, PlayersKilled.ToString());
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			ThidKillLvl20HibQuest quest = player.IsDoingQuest(typeof (ThidKillLvl20HibQuest)) as ThidKillLvl20HibQuest;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and shed some blood!");
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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(ThidKillLvl20HibQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(PazzHib.CanGiveQuest(typeof (ThidKillLvl20HibQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (ThidKillLvl20HibQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping me.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!PazzHib.GiveQuest(typeof (ThidKillLvl20HibQuest), player, 1))
					return;

				PazzHib.SayTo(player, "You will find suitable players in the frontiers or in battlegrounds.");

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
						return "You will find suitable players in the battlegrounds. \nPlayers Killed: ("+ PlayersKilled +" | "+ MAX_KILLED +")";
					case 2:
						return "Return to Pazz in the Thidranki Portal Keep for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(ThidKillLvl20HibQuest)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Realm == 0 || gArgs.Target.Realm == player.Realm || gArgs.Target is not GamePlayer ||
			    !(player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON)) return;
			PlayersKilled++;
			player.Out.SendMessage("[Daily] Enemy Killed: (" + PlayersKilled + " | " + MAX_KILLED + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (PlayersKilled >= MAX_KILLED)
			{
				// FinishQuest or go back to Dean
				Step = 2;
			}
		}

		public override void FinishQuest()
		{
			if (m_questPlayer.Inventory.IsSlotsFree(1, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
			{
				m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel) / 3);
				m_questPlayer.AddMoney(MoneyMgr.GetMoney(0, 0, m_questPlayer.Level * 2, 32, Util.Random(50)),
					"You receive {0} as a reward.");
				CoreRogMgr.GenerateBattlegroundToken(m_questPlayer, 1);
				PlayersKilled = 0;
				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			}
			else
			{
				m_questPlayer.Out.SendMessage("Clear one slot of your inventory for your reward", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}

		}
	}
}
