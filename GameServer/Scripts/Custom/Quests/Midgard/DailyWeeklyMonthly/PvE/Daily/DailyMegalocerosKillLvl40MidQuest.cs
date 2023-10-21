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
using Core.GS.Server;
using log4net;

namespace Core.GS.DailyQuest.Midgard
{
	public class DailyMegalocerosKillLvl40MidQuest : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Daily] Megaloceros Invasion";
		private const int minimumLevel = 40;
		private const int maximumLevel = 50;

		// Kill Goal
		private const int MAX_KILLED = 10;
		
		private static GameNpc Jarek = null; // Start NPC

		private int megalocerosKilled = 0;

		// Constructors
		public DailyMegalocerosKillLvl40MidQuest() : base()
		{
		}

		public DailyMegalocerosKillLvl40MidQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DailyMegalocerosKillLvl40MidQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DailyMegalocerosKillLvl40MidQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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
			if (!ServerProperty.LOAD_QUESTS)
				return;
			

			#region defineNPCs

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Jarek", ERealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 151 && npc.X == 292291 && npc.Y == 354975)
					{
						Jarek = npc;
						break;
					}

			if (Jarek == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Jarek , creating it ...");
				Jarek = new GameNpc();
				Jarek.Model = 774;
				Jarek.Name = "Jarek";
				Jarek.GuildName = "Advisor to the King";
				Jarek.Realm = ERealm.Midgard;
				Jarek.CurrentRegionID = 151;
				Jarek.Size = 50;
				Jarek.Level = 59;
				//Aegirhamn Location
				Jarek.X = 292291;
				Jarek.Y = 354975;
				Jarek.Z = 3867;
				Jarek.Heading = 1239;
				GameNpcInventoryTemplate templateMid = new GameNpcInventoryTemplate();
				templateMid.AddNPCEquipment(EInventorySlot.TorsoArmor, 983);
				templateMid.AddNPCEquipment(EInventorySlot.LegsArmor, 984);
				templateMid.AddNPCEquipment(EInventorySlot.ArmsArmor, 985);
				templateMid.AddNPCEquipment(EInventorySlot.HandsArmor, 986);
				templateMid.AddNPCEquipment(EInventorySlot.FeetArmor, 987);
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Jarek, GameObjectEvent.Interact, new CoreEventHandler(TalkToJarek));
			GameEventMgr.AddHandler(Jarek, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToJarek));
			
			Jarek.AddQuestToGive(typeof (DailyMegalocerosKillLvl40MidQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Jarek == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Jarek, GameObjectEvent.Interact, new CoreEventHandler(TalkToJarek));
			GameEventMgr.RemoveHandler(Jarek, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToJarek));
			
			Jarek.RemoveQuestToGive(typeof (DailyMegalocerosKillLvl40MidQuest));
		}

		private static void TalkToJarek(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Jarek.CanGiveQuest(typeof (DailyMegalocerosKillLvl40MidQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DailyMegalocerosKillLvl40MidQuest quest = player.IsDoingQuest(typeof (DailyMegalocerosKillLvl40MidQuest)) as DailyMegalocerosKillLvl40MidQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Jarek.SayTo(player, "You will find Megaloceros in the South East of Gripklosa Mountains.");
							break;
						case 2:
							Jarek.SayTo(player, "Hello " + player.Name + ", did you [kill] the Megaloceros?");
							break;
					}
				}
				else
				{
					Jarek.SayTo(player, "Hello "+ player.Name +", I am Jarek, Fen\'s friend. "+
					                    "The Megaloceros out in Gripklosa Mountains are devouring the natural flora and fauna of the Shrouded Isles. They may soon destroy the ecosystem entirely.\n"+
					                    "\nCan you [clear the Megaloceros] to save the Shrouded Isles?");
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
						case "clear the megaloceros":
							player.Out.SendQuestSubscribeCommand(Jarek, QuestMgr.GetIDForQuestType(typeof(DailyMegalocerosKillLvl40MidQuest)), "Will you help Jarek "+questTitle+"");
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
								player.Out.SendMessage("Thank you for your contribution!", EChatType.CT_Chat, EChatLoc.CL_PopupWindow);
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
			if (player.IsDoingQuest(typeof (DailyMegalocerosKillLvl40MidQuest)) != null)
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
			megalocerosKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, megalocerosKilled.ToString());
		}


		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			DailyMegalocerosKillLvl40MidQuest quest = player.IsDoingQuest(typeof (DailyMegalocerosKillLvl40MidQuest)) as DailyMegalocerosKillLvl40MidQuest;

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

		private static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DailyMegalocerosKillLvl40MidQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Jarek.CanGiveQuest(typeof (DailyMegalocerosKillLvl40MidQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DailyMegalocerosKillLvl40MidQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for your help.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Jarek.GiveQuest(typeof (DailyMegalocerosKillLvl40MidQuest), player, 1))
					return;

				Jarek.SayTo(player, "You will find the Megaloceros in Gripklosa Mountains.");

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
						return "Find Megaloceros in the South East of Gripklosa Mountains. \nKilled: Megaloceros ("+ megalocerosKilled +" | "+MAX_KILLED+")";
					case 2:
						return "Return to Jarek in Aegirhamn for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(DailyMegalocerosKillLvl40MidQuest)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
			if (gArgs.Target.Name.ToLower() != "megaloceros") return;
			megalocerosKilled++;
			player.Out.SendMessage("[Daily] Megaloceros Killed: ("+megalocerosKilled+" | "+MAX_KILLED+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (megalocerosKilled >= MAX_KILLED)
			{
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "MegalocerosKillQuestMid";
			set { ; }
		}
		public override void FinishQuest()
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/10);
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level,50,Util.Random(50)), "You receive {0} as a reward.");
			CoreRogMgr.GenerateReward(m_questPlayer, 100);
			megalocerosKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
