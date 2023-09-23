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

namespace DOL.GS.DailyQuest.Midgard
{
	public class ThidKeepCaptureMid : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Daily] Frontier Conquerer";
		private const int minimumLevel = 20;
		private const int maximumLevel = 24;

		// Capture Goal
		private const int MAX_CAPTURED = 1;
		
		private static GameNPC PazzMid = null; // Start NPC

		private int _isCaptured = 0;

		// Constructors
		public ThidKeepCaptureMid() : base()
		{
		}

		public ThidKeepCaptureMid(GamePlayer questingPlayer) : base(questingPlayer, 1)
		{
		}

		public ThidKeepCaptureMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public ThidKeepCaptureMid(GamePlayer questingPlayer, DbQuests dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Pazz", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
				{
					if (npc.CurrentRegionID == 252 && npc.X == 54259 && npc.Y == 25234)
					{
						PazzMid = npc;
						break;
					}
				}

			if (PazzMid == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find PazzMid, creating it ...");
				PazzMid = new GameNPC();
				PazzMid.Model = 26;
				PazzMid.Name = "Pazz";
				PazzMid.GuildName = "Bone Collector";
				PazzMid.Realm = eRealm.Midgard;
				//Svasud Faste Location
				PazzMid.CurrentRegionID = 252;
				PazzMid.Size = 40;
				PazzMid.Level = 59;
				PazzMid.X = 54259;
				PazzMid.Y = 25234;
				PazzMid.Z = 4319;
				PazzMid.Heading = 1744;
				PazzMid.Flags |= GameNPC.eFlags.PEACE;
				PazzMid.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					PazzMid.SaveIntoDatabase();
				}
			}


			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(PazzMid, GameObjectEvent.Interact, new DOLEventHandler(TalkToHerou));
			GameEventMgr.AddHandler(PazzMid, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHerou));

			/* Now we bring to Herou the possibility to give this quest to players */
			PazzMid.AddQuestToGive(typeof (ThidKeepCaptureMid));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (PazzMid == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(PazzMid, GameObjectEvent.Interact, new DOLEventHandler(TalkToHerou));
			GameEventMgr.RemoveHandler(PazzMid, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHerou));

			/* Now we remove to Herou the possibility to give this quest to players */
			PazzMid.RemoveQuestToGive(typeof (ThidKeepCaptureMid));
		}

		private static void TalkToHerou(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(PazzMid.CanGiveQuest(typeof (ThidKeepCaptureMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			ThidKeepCaptureMid quest = player.IsDoingQuest(typeof (ThidKeepCaptureMid)) as ThidKeepCaptureMid;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							PazzMid.SayTo(player, "Find an enemy occupied keep and capture it. If you succeed come back for your reward.");
							break;
						case 2:
							PazzMid.SayTo(player, "Hello " + player.Name + ", did you [capture] a keep?");
							break;
					}
				}
				else
				{
					PazzMid.SayTo(player, "Look "+ player.Name +", I'll cut to the chase. " +
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
							player.Out.SendQuestSubscribeCommand(PazzMid, QuestMgr.GetIDForQuestType(typeof(ThidKeepCaptureMid)), "Will you help Pazz with "+questTitle+"");
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
			if (player.IsDoingQuest(typeof (ThidKeepCaptureMid)) != null)
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
			ThidKeepCaptureMid quest = player.IsDoingQuest(typeof (ThidKeepCaptureMid)) as ThidKeepCaptureMid;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(ThidKeepCaptureMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(PazzMid.CanGiveQuest(typeof (ThidKeepCaptureMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (ThidKeepCaptureMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Midgard.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!PazzMid.GiveQuest(typeof (ThidKeepCaptureMid), player, 1))
					return;

				PazzMid.SayTo(player, "Thank you "+player.Name+", you are a true soldier of Midgard!");

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
						return "Go to the battlefield and conquer a keep. \nCaptured: Keep ("+ _isCaptured +" | "+MAX_CAPTURED+")";
					case 2:
						return "Return to Pazz in Thidranki Portal Keep for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(ThidKeepCaptureMid)) == null)
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
			get => "ThidKeepCaptureMid";
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
