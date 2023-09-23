using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.API;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests;
using log4net;

namespace DOL.GS.DailyQuest.Hibernia
{
	public class ThidKeepCaptureHib : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Daily] Thidranki Conquerer";
		private const int minimumLevel = 20;
		private const int maximumLevel = 24;

		// Capture Goal
		private const int MAX_CAPTURED = 1;
		
		private static GameNPC PazzHib = null; // Start NPC

		private int _isCaptured = 0;

		// Constructors
		public ThidKeepCaptureHib() : base()
		{
		}

		public ThidKeepCaptureHib(GamePlayer questingPlayer) : base(questingPlayer, 1)
		{
		}

		public ThidKeepCaptureHib(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public ThidKeepCaptureHib(GamePlayer questingPlayer, DbQuests dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Pazz", eRealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
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
				PazzHib = new GameNPC();
				PazzHib.Model = 26;
				PazzHib.Name = "Pazz";
				PazzHib.GuildName = "Bone Collector";
				PazzHib.Realm = eRealm.Hibernia;
				//Druim Ligen Location
				PazzHib.CurrentRegionID = 252;
				PazzHib.Size = 40;
				PazzHib.Level = 59;
				PazzHib.X = 18658;
				PazzHib.Y = 18710;
				PazzHib.Z = 4320;
				PazzHib.Heading = 1424;
				PazzHib.Flags |= GameNPC.eFlags.PEACE;
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(PazzHib, GameObjectEvent.Interact, new DOLEventHandler(TalkToCola));
			GameEventMgr.AddHandler(PazzHib, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToCola));

			/* Now we bring to Dean the possibility to give this quest to players */
			PazzHib.AddQuestToGive(typeof (ThidKeepCaptureHib));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (PazzHib == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(PazzHib, GameObjectEvent.Interact, new DOLEventHandler(TalkToCola));
			GameEventMgr.RemoveHandler(PazzHib, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToCola));

			/* Now we remove to Dean the possibility to give this quest to players */
			PazzHib.RemoveQuestToGive(typeof (ThidKeepCaptureHib));
		}

		private static void TalkToCola(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(PazzHib.CanGiveQuest(typeof (ThidKeepCaptureHib), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			ThidKeepCaptureHib quest = player.IsDoingQuest(typeof (ThidKeepCaptureHib)) as ThidKeepCaptureHib;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							PazzHib.SayTo(player, "Find an enemy occupied keep and capture it. If you succeed come back for your reward.");
							break;
						case 2:
							PazzHib.SayTo(player, "Hello " + player.Name + ", did you [capture] a keep?");
							break;
					}
				}
				else
				{
					PazzHib.SayTo(player, "Look "+ player.Name +", I'll cut to the chase. " +
					                   "We need the central keep back because I left some... contraband in the basement that I'd really like to reclaim before its found by the guards. Can you [help a skeleton] out?");
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
						case "help a skeleton":
							player.Out.SendQuestSubscribeCommand(PazzHib, QuestMgr.GetIDForQuestType(typeof(ThidKeepCaptureHib)), "Will you help Pazz with "+questTitle+"");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "capture":
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
			if (player.IsDoingQuest(typeof (ThidKeepCaptureHib)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			//if (player.HasFinishedQuest(typeof(Academy_47)) == 0) return false;

			//if (!CheckPartAccessible(player,typeof(CityOfCamelot)))
			//	return false;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			ThidKeepCaptureHib quest = player.IsDoingQuest(typeof (ThidKeepCaptureHib)) as ThidKeepCaptureHib;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and find us a keep.");
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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(ThidKeepCaptureHib)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(PazzHib.CanGiveQuest(typeof (ThidKeepCaptureHib), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (ThidKeepCaptureHib)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Hibernia.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!PazzHib.GiveQuest(typeof (ThidKeepCaptureHib), player, 1))
					return;

				PazzHib.SayTo(player, "Thank you "+player.Name+", you are a true soldier of Hibernia!");

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
						return "Go to the battlefield and conquer a keep. \nCaptured: Keep ("+ _isCaptured +" | 1)";
					case 2:
						return "Return to Pazz in Thidranki Portal Keep for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(ThidKeepCaptureHib)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GamePlayerEvent.CapturedKeepsChanged) return;
			_isCaptured = 1;
			player.Out.SendMessage("[Daily] Captured Keep: ("+_isCaptured+" | "+MAX_CAPTURED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (_isCaptured >= MAX_CAPTURED)
			{
				// FinishQuest or go back to Dean
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "ThidKeepCaptureHib";
			set { ; }
		}
		public override void LoadQuestParameters()
		{
			
		}

		public override void SaveQuestParameters()
		{
			
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}

		public override void FinishQuest()
		{
			if (m_questPlayer.Inventory.IsSlotsFree(1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
				m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel) / 2);
				m_questPlayer.AddMoney(Money.GetMoney(0, 0, m_questPlayer.Level * 2, 0, Util.Random(50)),
					"You receive {0} as a reward.");
				AtlasROGManager.GenerateBattlegroundToken(m_questPlayer, 1);
				_isCaptured = 0;
				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			}
			else
			{
				m_questPlayer.Out.SendMessage("Clear one slot of your inventory for your reward", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
	}
}
