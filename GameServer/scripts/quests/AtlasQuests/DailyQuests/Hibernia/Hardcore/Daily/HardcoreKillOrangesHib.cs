using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Cache;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.API;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests;
using log4net;

namespace DOL.GS.DailyQuest
{
	public class HardcoreKillOrangesHib : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "[Hardcore] Big Man On Campus";
		protected const int minimumLevel = 1;
		protected const int maximumLevel = 49;

		protected static GameNPC SucciHib = null; // Start NPC

		private int OrangeConKilled = 0;
		private int MAX_KillGoal = 10;

		// Constructors
		public HardcoreKillOrangesHib() : base()
		{
		}

		public HardcoreKillOrangesHib(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public HardcoreKillOrangesHib(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public HardcoreKillOrangesHib(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Succi", eRealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
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
				SucciHib = new GameNPC();
				SucciHib.Model = 902;
				SucciHib.Name = "Succi";
				SucciHib.GuildName = "Spectre of Death";
				SucciHib.Realm = eRealm.Hibernia;
				//Svasud Location
				SucciHib.CurrentRegionID = 200;
				SucciHib.Size = 60;
				SucciHib.Level = 59;
				SucciHib.X = 335117;
				SucciHib.Y = 420642;
				SucciHib.Z = 5195;
				SucciHib.Heading = 3723;
				SucciHib.Flags |= GameNPC.eFlags.PEACE;
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(SucciHib, GameObjectEvent.Interact, new DOLEventHandler(TalkToSucci));
			GameEventMgr.AddHandler(SucciHib, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToSucci));

			/* Now we bring to Dean the possibility to give this quest to players */
			SucciHib.AddQuestToGive(typeof (HardcoreKillOrangesHib));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (SucciHib == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(SucciHib, GameObjectEvent.Interact, new DOLEventHandler(TalkToSucci));
			GameEventMgr.RemoveHandler(SucciHib, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToSucci));

			/* Now we remove to Dean the possibility to give this quest to players */
			SucciHib.RemoveQuestToGive(typeof (HardcoreKillOrangesHib));
		}

		protected static void TalkToSucci(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(SucciHib.CanGiveQuest(typeof (HardcoreKillOrangesHib), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			HardcoreKillOrangesHib oranges = player.IsDoingQuest(typeof (HardcoreKillOrangesHib)) as HardcoreKillOrangesHib;

			if (e == GameObjectEvent.Interact)
			{
				if (oranges != null)
				{
					switch (oranges.Step)
					{
						case 1:
							SucciHib.SayTo(player, "Seek out creatures greater in strength than you and cast them into the abyss.");
							break;
						case 2:
							SucciHib.SayTo(player, "" + player.Name + ". You have earned [another sunrise].");
							break;
					}
				}
				else
				{
					SucciHib.SayTo(player, ""+ player.Name +". I have seen visions of your death. "+
					                     "Crushed beneath the blow of a mighty foe. Dashed against the rocks of eternity."+
					                     "\n Will you defy them? Stand tall and let the spirits know [today is not the day].");
					SucciHib.SayTo(player, " NOTE: This is a HARDCORE quest. If you die or join a group while doing this quest, it will be aborted automatically.");
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
							player.Out.SendQuestSubscribeCommand(SucciHib, QuestMgr.GetIDForQuestType(typeof(HardcoreKillOrangesHib)), "Will you undertake " + questTitle + "?");
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
								player.Out.SendMessage("From dust we are born, and to dust we return. Your time will come eventually.", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
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
			if (player.IsDoingQuest(typeof (HardcoreKillOrangesHib)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		protected static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			HardcoreKillOrangesHib oranges = player.IsDoingQuest(typeof (HardcoreKillOrangesHib)) as HardcoreKillOrangesHib;

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

		protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(HardcoreKillOrangesHib)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(SucciHib.CanGiveQuest(typeof (HardcoreKillOrangesHib), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (HardcoreKillOrangesHib)) != null)
				return;

			if (player.Group != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("The titans shall tremble.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!SucciHib.GiveQuest(typeof (HardcoreKillOrangesHib), player, 1))
					return;

				SucciHib.SayTo(player, "Seek out creatures greater in strength than you and cast them into the abyss.");

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
						return "Return to Succi in Druim Ligen for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(HardcoreKillOrangesHib)) == null)
				return;
			
			if(player.Group != null && Step == 1)
				FailQuest();

			if (sender != m_questPlayer)
				return;

			if (e == GameLivingEvent.Dying && Step == 1)
			{
				FailQuest();
			}
			
			if (e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

				if (player.GetConLevel(gArgs.Target) > 0) 
				{
					OrangeConKilled++;
					player.Out.SendMessage("[Hardcore] Monster Killed: ("+OrangeConKilled+" | "+MAX_KillGoal+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
					player.Out.SendQuestUpdate(this);
					
					if (OrangeConKilled >= MAX_KillGoal)
					{
						// FinishQuest or go back to npc
						Step = 2;
					}
				}
				
			}
			
		}
		
		public override string QuestPropertyKey
		{
			get => "HardcorePlayerKillQuestHib";
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


		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/2, false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level*2,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 150);
			OrangeConKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}

		public void FailQuest()
		{
			OrangeConKilled = 0;
			m_questPlayer.Out.SendMessage(questTitle + " failed.", eChatType.CT_ScreenCenter_And_CT_System, eChatLoc.CL_SystemWindow);

			OrangeConKilled = 0;
			Step = -1;
			// move quest from active list to finished list...
			m_questPlayer.QuestList.Remove(this);

			m_questPlayer.Out.SendQuestListUpdate();
		}
	}
}
