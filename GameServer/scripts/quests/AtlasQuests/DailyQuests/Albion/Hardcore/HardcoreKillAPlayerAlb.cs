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
	public class HardcoreKillAPlayerAlb : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Hardcore] Apex Predator";
		private const int minimumLevel = 1;
		private const int maximumLevel = 50;

		private static GameNPC SucciAlb = null; // Start NPC

		private int PlayerKilled = 0;
		private int MAX_KillGoal = 1;

		// Constructors
		public HardcoreKillAPlayerAlb() : base()
		{
		}

		public HardcoreKillAPlayerAlb(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public HardcoreKillAPlayerAlb(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public HardcoreKillAPlayerAlb(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Succi", eRealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
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
				SucciAlb = new GameNPC();
				SucciAlb.Model = 902;
				SucciAlb.Name = "Succi";
				SucciAlb.GuildName = "Spectre of Death";
				SucciAlb.Realm = eRealm.Albion;
				//Sauvage Location
				SucciAlb.CurrentRegionID = 1;
				SucciAlb.Size = 60;
				SucciAlb.Level = 59;
				SucciAlb.X = 584652;
				SucciAlb.Y = 477773;
				SucciAlb.Z = 2600;
				SucciAlb.Heading = 2257;
				SucciAlb.Flags |= GameNPC.eFlags.PEACE;
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(SucciAlb, GameObjectEvent.Interact, new DOLEventHandler(TalkToSucci));
			GameEventMgr.AddHandler(SucciAlb, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToSucci));

			/* Now we bring to Dean the possibility to give this quest to players */
			SucciAlb.AddQuestToGive(typeof (HardcoreKillAPlayerAlb));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (SucciAlb == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(SucciAlb, GameObjectEvent.Interact, new DOLEventHandler(TalkToSucci));
			GameEventMgr.RemoveHandler(SucciAlb, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToSucci));

			/* Now we remove to Dean the possibility to give this quest to players */
			SucciAlb.RemoveQuestToGive(typeof (HardcoreKillAPlayerAlb));
		}

		private static void TalkToSucci(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(SucciAlb.CanGiveQuest(typeof (HardcoreKillAPlayerAlb), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			HardcoreKillAPlayerAlb oranges = player.IsDoingQuest(typeof (HardcoreKillAPlayerAlb)) as HardcoreKillAPlayerAlb;

			if (e == GameObjectEvent.Interact)
			{
				if (oranges != null)
				{
					switch (oranges.Step)
					{
						case 1:
							SucciAlb.SayTo(player, "Hunt, or be hunted.");
							break;
						case 2:
							SucciAlb.SayTo(player, "" + player.Name + ". You have earned [another sunrise].");
							break;
					}
				}
				else
				{
					SucciAlb.SayTo(player, "The flash of steel's bite. \n"+
					                     "One stands above, one below. \n" +
					                     "[Predator] eats well.");
					SucciAlb.SayTo(player, " NOTE: This is a HARDCORE quest. If you die or join a group while doing this quest, it will be aborted automatically.");
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
						case "predator":
							player.Out.SendQuestSubscribeCommand(SucciAlb, QuestMgr.GetIDForQuestType(typeof(HardcoreKillAPlayerAlb)), "Will you undertake " + questTitle + "?");
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
								player.Out.SendMessage("Enjoy your meal. With luck, it shall not be your last.", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
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
			if (player.IsDoingQuest(typeof (HardcoreKillAPlayerAlb)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			HardcoreKillAPlayerAlb oranges = player.IsDoingQuest(typeof (HardcoreKillAPlayerAlb)) as HardcoreKillAPlayerAlb;

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

		private static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(HardcoreKillAPlayerAlb)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(SucciAlb.CanGiveQuest(typeof (HardcoreKillAPlayerAlb), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (HardcoreKillAPlayerAlb)) != null)
				return;

			if (player.Group != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Look them in the eye.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!SucciAlb.GiveQuest(typeof (HardcoreKillAPlayerAlb), player, 1))
					return;

				SucciAlb.SayTo(player, "Hunt, or be hunted. Only one shall eat this night.");

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
						return "Kill another player without dying. \n Life Taken: ("+ PlayerKilled +" | "+MAX_KillGoal+")";
					case 2:
						return "Return to Succi in Castle Sauvage for your grim reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(HardcoreKillAPlayerAlb)) == null)
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
			if (!(player.GetConLevel(gArgs.Target) > -3) || gArgs.Target is not GamePlayer enemyPlayer ||
			    enemyPlayer.Realm == 0 || player.Realm == enemyPlayer.Realm) return;
			PlayerKilled = 1;
			player.Out.SendMessage("[Hardcore] Enemy Killed: ("+PlayerKilled+" | "+MAX_KillGoal+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
			// FinishQuest or go back to npc
			Step = 2;

		}
		
		public override string QuestPropertyKey
		{
			get => "HardcorePlayerKillQuestAlb";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			
		}

		public override void SaveQuestParameters()
		{
			
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/2, false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level*2,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 250);
			PlayerKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}

		private void FailQuest()
		{
			m_questPlayer.Out.SendMessage(questTitle + " failed.", eChatType.CT_ScreenCenter_And_CT_System, eChatLoc.CL_SystemWindow);

			PlayerKilled = 0;
			Step = -1;
			// move quest from active list to finished list...
			m_questPlayer.QuestList.Remove(this);
			m_questPlayer.QuestListFinished.Add(this);
			
			m_questPlayer.Out.SendQuestListUpdate();
		}
	}
}
