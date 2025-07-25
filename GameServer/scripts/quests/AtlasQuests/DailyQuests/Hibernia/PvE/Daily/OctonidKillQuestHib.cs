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

namespace DOL.GS.DailyQuest.Hibernia
{
	public class OctonidKillQuestHib : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Daily] Octonid Invasion";
		private const int minimumLevel = 40;
		private const int maximumLevel = 50;

		// Kill Goal
		private const int MAX_KILLED = 10;
		
		private static GameNPC Anthony = null; // Start NPC

		private int OctonidKilled = 0;

		// Constructors
		public OctonidKillQuestHib() : base()
		{
		}

		public OctonidKillQuestHib(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public OctonidKillQuestHib(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public OctonidKillQuestHib(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Anthony", eRealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 181 && npc.X == 422864 && npc.Y == 444362)
					{
						Anthony = npc;
						break;
					}

			if (Anthony == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Anthony , creating it ...");
				Anthony = new GameNPC();
				Anthony.Model = 289;
				Anthony.Name = "Anthony";
				Anthony.GuildName = "Advisor to the King";
				Anthony.Realm = eRealm.Hibernia;
				//Domnann Location
				Anthony.CurrentRegionID = 181;
				Anthony.Size = 50;
				Anthony.Level = 59;
				Anthony.X = 422864;
				Anthony.Y = 444362;
				Anthony.Z = 5952;
				Anthony.Heading = 1234;
				GameNpcInventoryTemplate templateHib = new GameNpcInventoryTemplate();
				templateHib.AddNPCEquipment(eInventorySlot.TorsoArmor, 1008);
				templateHib.AddNPCEquipment(eInventorySlot.HandsArmor, 361);
				templateHib.AddNPCEquipment(eInventorySlot.FeetArmor, 362);
				Anthony.Inventory = templateHib.CloseTemplate();
				Anthony.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Anthony.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Anthony, GameObjectEvent.Interact, new DOLEventHandler(TalkToAnthony));
			GameEventMgr.AddHandler(Anthony, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToAnthony));

			Anthony.AddQuestToGive(typeof (OctonidKillQuestHib));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Anthony == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Anthony, GameObjectEvent.Interact, new DOLEventHandler(TalkToAnthony));
			GameEventMgr.RemoveHandler(Anthony, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToAnthony));

			Anthony.RemoveQuestToGive(typeof (OctonidKillQuestHib));
		}

		private static void TalkToAnthony(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Anthony.CanGiveQuest(typeof (OctonidKillQuestHib), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			OctonidKillQuestHib quest = player.IsDoingQuest(typeof (OctonidKillQuestHib)) as OctonidKillQuestHib;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Anthony.SayTo(player, "You will find Octonids in the South East of World\'s End.");
							break;
						case 2:
							Anthony.SayTo(player, "Hello " + player.Name + ", did you [kill] the Octonids?");
							break;
					}
				}
				else
				{
					Anthony.SayTo(player, "Hello "+ player.Name +", I am Anthony. I help the king with logistics, and he's tasked me with getting things done around here. "+
					                   "The Octonids out in World's End are devouring the natural flora and fauna of the Shrouded Isles. They may soon destroy the ecosystem entirely.\n"+
					                   "\nCan you [clear the Octonids] to save the Shrouded Isles?");
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (quest == null)
				{
					switch (wArgs.Text.ToLower())
					{
						case "clear the octonids":
							player.Out.SendQuestSubscribeCommand(Anthony, QuestMgr.GetIDForQuestType(typeof(OctonidKillQuestHib)), "Will you help Anthony "+questTitle+"");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "kill":
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
			if (player.IsDoingQuest(typeof (OctonidKillQuestHib)) != null)
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
			OctonidKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, OctonidKilled.ToString());
		}


		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			OctonidKillQuestHib quest = player.IsDoingQuest(typeof (OctonidKillQuestHib)) as OctonidKillQuestHib;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(OctonidKillQuestHib)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Anthony.CanGiveQuest(typeof (OctonidKillQuestHib), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (OctonidKillQuestHib)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for your help.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Anthony.GiveQuest(typeof (OctonidKillQuestHib), player, 1))
					return;

				Anthony.SayTo(player, "You will find the Octonids in World\'s End.");

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
						return "Find Octonids South East in World\'s End. \nKilled: Octonids ("+ OctonidKilled +" | 10)";
					case 2:
						return "Return to Anthony in Grove of Domnann for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(OctonidKillQuestHib)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
			if (gArgs.Target.Name.ToLower() != "octonid") return;
			OctonidKilled++;
			player.Out.SendMessage("[Daily] Octonid Killed: ("+OctonidKilled+" | "+MAX_KILLED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (OctonidKilled >= MAX_KILLED)
			{
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "OctonidKillQuestHib";
			set { ; }
		}

		public override void FinishQuest()
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/10);
			m_questPlayer.Wallet.AddMoney(WalletHelper.ToMoney(0,0,m_questPlayer.Level,50,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateReward(m_questPlayer, 100);
			OctonidKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
