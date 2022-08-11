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
using log4net;

namespace DOL.GS.MonthlyQuest.Hibernia
{
	public class MonthlyEpicPvEQuestHib : Quests.MonthlyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Monthly] Annihilation of Malevolence";
		private const int minimumLevel = 45;
		private const int maximumLevel = 50;
		
		// Kill Goal
		private const int MAX_KILLED = 1;
		// Quest Counter
		private int _balorKilled = 0;
		private int _myrddraxisKilled = 0;

		private static GameNPC Anthony = null; // Start NPC

		private const string Balor_NAME = "Balor";
		private const string Myrddraxis_NAME = "Myrddraxis";
		
		
		// Constructors
		public MonthlyEpicPvEQuestHib() : base()
		{
		}

		public MonthlyEpicPvEQuestHib(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public MonthlyEpicPvEQuestHib(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public MonthlyEpicPvEQuestHib(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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
			
			Anthony.AddQuestToGive(typeof (MonthlyEpicPvEQuestHib));

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

			Anthony.RemoveQuestToGive(typeof (MonthlyEpicPvEQuestHib));
		}

		private static void TalkToAnthony(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Anthony.CanGiveQuest(typeof (MonthlyEpicPvEQuestHib), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			MonthlyEpicPvEQuestHib quest = player.IsDoingQuest(typeof (MonthlyEpicPvEQuestHib)) as MonthlyEpicPvEQuestHib;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Anthony.SayTo(player, player.Name + ", please find allies and kill the epic creatures in Tur Suil and Fomor!");
							break;
						case 2:
							Anthony.SayTo(player, "Hello " + player.Name + ", did you [slay the creatures] and return for your reward?");
							break;
					}
				}
				else
				{
					Anthony.SayTo(player, "Hello "+ player.Name +", I am Anthony. For several months the situation in Tur Suil and Fomor has changed. " +
					                    "A place of mineral wealth and natural resources is now a place of violence and poisoning. \n\n"+
					                    "Can you support Hibernia and [kill Balor and Myrddraxis] in Tur Suil and Fomor?");
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
						case "kill Balor and Myrddraxis":
							player.Out.SendQuestSubscribeCommand(Anthony, QuestMgr.GetIDForQuestType(typeof(MonthlyEpicPvEQuestHib)), "Will you help Anthony "+questTitle+"?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "slay the creatures":
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
			if (player.IsDoingQuest(typeof (MonthlyEpicPvEQuestHib)) != null)
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
			MonthlyEpicPvEQuestHib quest = player.IsDoingQuest(typeof (MonthlyEpicPvEQuestHib)) as MonthlyEpicPvEQuestHib;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and slay those creatures!");
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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(MonthlyEpicPvEQuestHib)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Anthony.CanGiveQuest(typeof (MonthlyEpicPvEQuestHib), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (MonthlyEpicPvEQuestHib)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Atlas.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Anthony.GiveQuest(typeof (MonthlyEpicPvEQuestHib), player, 1))
					return;

				Anthony.SayTo(player, "Please, find the epic monsters in Tur Suil and Fomor and return for your reward.");

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
						return "Make your way and defeat the epic creatures in Tur Suil as well as in Fomor! \n" +
						       "Killed: " + Balor_NAME + " ("+ _balorKilled +" | " + MAX_KILLED + ") in Tur Suil\n" +
						       "Killed: " + Myrddraxis_NAME + " ("+ _myrddraxisKilled +" | " + MAX_KILLED + ") in Fomor\n";
					case 2:
						return "Return to Anthony for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(MonthlyEpicPvEQuestHib)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Name.ToLower() == Balor_NAME.ToLower() && gArgs.Target is GameNPC && _balorKilled < MAX_KILLED)
			{
				_balorKilled = 1;
				player.Out.SendMessage("[Monthly] You killed " + Balor_NAME + ": (" + _balorKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (gArgs.Target.Name.ToLower() == Myrddraxis_NAME.ToLower() && gArgs.Target is GameNPC && _myrddraxisKilled < MAX_KILLED)
			{
				_myrddraxisKilled = 1;
				player.Out.SendMessage("[Monthly] You killed " + Myrddraxis_NAME + ": (" + _myrddraxisKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}

			if (_balorKilled >= MAX_KILLED && _myrddraxisKilled >= MAX_KILLED)
			{
				Step = 2;
			}
		}
		
		public override string QuestPropertyKey
		{
			get => "MonthlyEpicPvEQuestHib";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_balorKilled = GetCustomProperty(Balor_NAME) != null ? int.Parse(GetCustomProperty(Balor_NAME)) : 0;
			_myrddraxisKilled = GetCustomProperty(Myrddraxis_NAME) != null ? int.Parse(GetCustomProperty(Myrddraxis_NAME)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(Balor_NAME, _balorKilled.ToString());
			SetCustomProperty(Myrddraxis_NAME, _myrddraxisKilled.ToString());
		}

		public override void FinishQuest()
		{
			if (m_questPlayer.Inventory.IsSlotsFree(3, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
				m_questPlayer.GainExperience(eXPSource.Quest,
					(m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel), false);
				m_questPlayer.AddMoney(Money.GetMoney(0, 0, m_questPlayer.Level * 8, 32, Util.Random(50)),
					"You receive {0} as a reward.");
				AtlasROGManager.GenerateOrbAmount(m_questPlayer, 3000);
				AtlasROGManager.GenerateBeetleCarapace(m_questPlayer, 2);
				AtlasROGManager.GenerateJewel(m_questPlayer, 51);
				_balorKilled = 0;
				_myrddraxisKilled = 0;
				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			}
			else
			{
				m_questPlayer.Out.SendMessage("Clear three slots of your inventory for your reward", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
	}
}
