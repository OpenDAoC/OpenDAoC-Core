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

namespace DOL.GS.MonthlyQuest.Midgard
{
	public class MonthlyEpicPvEQuestMid : Quests.MonthlyQuest
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
		private int _iarnvidiurKilled = 0;
		private int _nosdodenKilled = 0;

		private static GameNPC Jarek = null; // Start NPC

		private const string Iarnvidiur_NAME = "Iarnvidiur";
		private const string Nosdoden_NAME = "Nosdoden";
		
		
		// Constructors
		public MonthlyEpicPvEQuestMid() : base()
		{
		}

		public MonthlyEpicPvEQuestMid(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public MonthlyEpicPvEQuestMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public MonthlyEpicPvEQuestMid(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Jarek", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 151 && npc.X == 292291 && npc.Y == 354975)
					{
						Jarek = npc;
						break;
					}

			if (Jarek == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Jarek , creating it ...");
				Jarek = new GameNPC();
				Jarek.Model = 774;
				Jarek.Name = "Jarek";
				Jarek.GuildName = "Advisor to the King";
				Jarek.Realm = eRealm.Midgard;
				Jarek.CurrentRegionID = 151;
				Jarek.Size = 50;
				Jarek.Level = 59;
				//Aegirhamn Location
				Jarek.X = 292291;
				Jarek.Y = 354975;
				Jarek.Z = 3867;
				Jarek.Heading = 1239;
				GameNpcInventoryTemplate templateMid = new GameNpcInventoryTemplate();
				templateMid.AddNPCEquipment(eInventorySlot.TorsoArmor, 983);
				templateMid.AddNPCEquipment(eInventorySlot.LegsArmor, 984);
				templateMid.AddNPCEquipment(eInventorySlot.ArmsArmor, 985);
				templateMid.AddNPCEquipment(eInventorySlot.HandsArmor, 986);
				templateMid.AddNPCEquipment(eInventorySlot.FeetArmor, 987);
				Jarek.Inventory = templateMid.CloseTemplate();
				Jarek.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Jarek.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Jarek, GameObjectEvent.Interact, new DOLEventHandler(TalkToJarek));
			GameEventMgr.AddHandler(Jarek, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToJarek));
			
			Jarek.AddQuestToGive(typeof (MonthlyEpicPvEQuestMid));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Jarek == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Jarek, GameObjectEvent.Interact, new DOLEventHandler(TalkToJarek));
			GameEventMgr.RemoveHandler(Jarek, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToJarek));

			Jarek.RemoveQuestToGive(typeof (MonthlyEpicPvEQuestMid));
		}

		private static void TalkToJarek(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Jarek.CanGiveQuest(typeof (MonthlyEpicPvEQuestMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			MonthlyEpicPvEQuestMid quest = player.IsDoingQuest(typeof (MonthlyEpicPvEQuestMid)) as MonthlyEpicPvEQuestMid;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Jarek.SayTo(player, player.Name + ", please find allies and kill the epic creatures in Trollheim and Iarnvidiur's Lair!");
							break;
						case 2:
							Jarek.SayTo(player, "Hello " + player.Name + ", did you [slay the creatures] and return for your reward?");
							break;
					}
				}
				else
				{
					Jarek.SayTo(player, "Hello "+ player.Name +", I am Jarek. For several months the situation in Trollheim and Iarnvidur's Lair has changed. " +
					                    "A place of mineral wealth and natural resources is now a place of violence and poisoning. \n\n"+
					                    "Can you support Midgard and [kill Nosdoden and Iarnvidiur] in Trollheim and Iarnvidur's Lair?");
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
						case "kill Nosdoden and Iarnvidiur":
							player.Out.SendQuestSubscribeCommand(Jarek, QuestMgr.GetIDForQuestType(typeof(MonthlyEpicPvEQuestMid)), "Will you help Jarek "+questTitle+"?");
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
			if (player.IsDoingQuest(typeof (MonthlyEpicPvEQuestMid)) != null)
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
			MonthlyEpicPvEQuestMid quest = player.IsDoingQuest(typeof (MonthlyEpicPvEQuestMid)) as MonthlyEpicPvEQuestMid;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(MonthlyEpicPvEQuestMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Jarek.CanGiveQuest(typeof (MonthlyEpicPvEQuestMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (MonthlyEpicPvEQuestMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Atlas.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Jarek.GiveQuest(typeof (MonthlyEpicPvEQuestMid), player, 1))
					return;

				Jarek.SayTo(player, "Please, find the epic monsters in Trollheim and Iarnvidiur's Lair and return for your reward.");

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
						return "Make your way and defeat the epic creatures in Trollheim as well as in Iarnvidiur's Lair! \n" +
						       "Killed: " + Nosdoden_NAME + " ("+ _nosdodenKilled +" | " + MAX_KILLED + ") in Trollheim\n" +
						       "Killed: " + Iarnvidiur_NAME + " ("+ _iarnvidiurKilled +" | " + MAX_KILLED + ") in Iarnvidiur's Lair\n";
					case 2:
						return "Return to Jarek for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(MonthlyEpicPvEQuestMid)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Name.ToLower() == Iarnvidiur_NAME.ToLower() && gArgs.Target is GameNPC && _iarnvidiurKilled < MAX_KILLED)
			{
				_iarnvidiurKilled = 1;
				player.Out.SendMessage("[Monthly] You killed " + Iarnvidiur_NAME + ": (" + _iarnvidiurKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (gArgs.Target.Name.ToLower() == Nosdoden_NAME.ToLower() && gArgs.Target is GameNPC && _nosdodenKilled < MAX_KILLED)
			{
				_nosdodenKilled = 1;
				player.Out.SendMessage("[Monthly] You killed " + Nosdoden_NAME + ": (" + _nosdodenKilled + " | " + MAX_KILLED + ")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}

			if (_iarnvidiurKilled >= MAX_KILLED && _nosdodenKilled >= MAX_KILLED)
			{
				Step = 2;
			}
		}
		
		public override string QuestPropertyKey
		{
			get => "MonthlyEpicPvEQuestMid";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_iarnvidiurKilled = GetCustomProperty(Iarnvidiur_NAME) != null ? int.Parse(GetCustomProperty(Iarnvidiur_NAME)) : 0;
			_nosdodenKilled = GetCustomProperty(Nosdoden_NAME) != null ? int.Parse(GetCustomProperty(Nosdoden_NAME)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(Iarnvidiur_NAME, _iarnvidiurKilled.ToString());
			SetCustomProperty(Nosdoden_NAME, _nosdodenKilled.ToString());
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
				_iarnvidiurKilled = 0;
				_nosdodenKilled = 0;
				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			}
			else
			{
				m_questPlayer.Out.SendMessage("Clear three slots of your inventory for your reward", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
		}
	}
}
