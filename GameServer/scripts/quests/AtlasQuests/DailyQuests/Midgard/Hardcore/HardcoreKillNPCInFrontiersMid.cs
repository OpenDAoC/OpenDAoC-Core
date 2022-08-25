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
	public class HardcoreKillNPCInFrontiersMid : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Hardcore] A Lot Of Bravery";
		private const int minimumLevel = 1;
		private const int maximumLevel = 50;

		private static GameNPC SucciMid = null; // Start NPC

		private int FrontierMobsKilled = 0;
		private int MAX_KillGoal = 25;

		// Constructors
		public HardcoreKillNPCInFrontiersMid() : base()
		{
		}

		public HardcoreKillNPCInFrontiersMid(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public HardcoreKillNPCInFrontiersMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public HardcoreKillNPCInFrontiersMid(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Succi", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
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
				SucciMid = new GameNPC();
				SucciMid.Model = 902;
				SucciMid.Name = "Succi";
				SucciMid.GuildName = "Spectre of Death";
				SucciMid.Realm = eRealm.Midgard;
				//Svasud Location
				SucciMid.CurrentRegionID = 100;
				SucciMid.Size = 60;
				SucciMid.Level = 59;
				SucciMid.X = 766767;
				SucciMid.Y = 670636;
				SucciMid.Z = 5736;
				SucciMid.Heading = 2536;
				SucciMid.Flags |= GameNPC.eFlags.PEACE;
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(SucciMid, GameObjectEvent.Interact, new DOLEventHandler(TalkToSucci));
			GameEventMgr.AddHandler(SucciMid, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToSucci));

			/* Now we bring to Dean the possibility to give this quest to players */
			SucciMid.AddQuestToGive(typeof (HardcoreKillNPCInFrontiersMid));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (SucciMid == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(SucciMid, GameObjectEvent.Interact, new DOLEventHandler(TalkToSucci));
			GameEventMgr.RemoveHandler(SucciMid, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToSucci));

			/* Now we remove to Dean the possibility to give this quest to players */
			SucciMid.RemoveQuestToGive(typeof (HardcoreKillNPCInFrontiersMid));
		}

		private static void TalkToSucci(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(SucciMid.CanGiveQuest(typeof (HardcoreKillNPCInFrontiersMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			HardcoreKillNPCInFrontiersMid oranges = player.IsDoingQuest(typeof (HardcoreKillNPCInFrontiersMid)) as HardcoreKillNPCInFrontiersMid;

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
							player.Out.SendQuestSubscribeCommand(SucciMid, QuestMgr.GetIDForQuestType(typeof(HardcoreKillNPCInFrontiersMid)), "Will you undertake " + questTitle + "?");
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
								player.Out.SendMessage("Tell me, did you face your fears?", eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
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
			if (player.IsDoingQuest(typeof (HardcoreKillNPCInFrontiersMid)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			HardcoreKillNPCInFrontiersMid oranges = player.IsDoingQuest(typeof (HardcoreKillNPCInFrontiersMid)) as HardcoreKillNPCInFrontiersMid;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(HardcoreKillNPCInFrontiersMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(SucciMid.CanGiveQuest(typeof (HardcoreKillNPCInFrontiersMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (HardcoreKillNPCInFrontiersMid)) != null)
				return;

			if (player.Group != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Watch your back.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!SucciMid.GiveQuest(typeof (HardcoreKillNPCInFrontiersMid), player, 1))
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

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;


			if (player?.IsDoingQuest(typeof(HardcoreKillNPCInFrontiersMid)) == null)
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
			
			if (gArgs.Target is GamePet)
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
						    (living is BDPet bdpet &&
						     (bdpet.Owner == player || bdpet.Owner == player.ControlledBrain?.Body)))
							continue;

						return;
					}
				}
			}
			FrontierMobsKilled++;
			player.Out.SendMessage("[Hardcore] Monster Killed: (" + FrontierMobsKilled + " | " + MAX_KillGoal + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
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
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level*2,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 150);
			FrontierMobsKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			
		}

		private void FailQuest()
		{
			m_questPlayer.Out.SendMessage(questTitle + " failed.", eChatType.CT_ScreenCenter_And_CT_System, eChatLoc.CL_SystemWindow);

			FrontierMobsKilled = 0;
			Step = -2;
			// move quest from active list to finished list...
			m_questPlayer.QuestList.Remove(this);
			m_questPlayer.QuestListFinished.Add(this);
			
			m_questPlayer.Out.SendQuestListUpdate();
		}
	}
}
