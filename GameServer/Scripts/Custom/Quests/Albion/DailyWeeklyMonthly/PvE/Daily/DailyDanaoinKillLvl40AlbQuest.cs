using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.Quests;
using log4net;

namespace Core.GS.DailyQuest.Albion
{
	public class DailyDanaoinKillLvl40AlbQuest : Quests.DailyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Daily] Danaoin Invasion";
		private const int minimumLevel = 40;
		private const int maximumLevel = 50;

		// Kill Goal
		private const int MAX_KILLED = 10;
		
		private static GameNpc James = null; // Start NPC

		private int danaoinKilled = 0;

		// Constructors
		public DailyDanaoinKillLvl40AlbQuest() : base()
		{
		}

		public DailyDanaoinKillLvl40AlbQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DailyDanaoinKillLvl40AlbQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DailyDanaoinKillLvl40AlbQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNpc[] npcs = WorldMgr.GetNPCsByName("James", ERealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 51 && npc.X == 534044 && npc.Y == 549664)
					{
						James = npc;
						break;
					}

			if (James == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find James , creating it ...");
				James = new GameNpc();
				James.Model = 254;
				James.Name = "James";
				James.GuildName = "Advisor To The King";
				James.Realm = ERealm.Albion;
				James.CurrentRegionID = 51;
				James.Size = 50;
				James.Level = 59;
				//Caer Gothwaite Location
				James.X = 534044;
				James.Y = 549664;
				James.Z = 4940;
				James.Heading = 3143;
				GameNpcInventoryTemplate templateAlb = new GameNpcInventoryTemplate();
				templateAlb.AddNPCEquipment(EInventorySlot.TorsoArmor, 1005);
				templateAlb.AddNPCEquipment(EInventorySlot.HandsArmor, 142);
				templateAlb.AddNPCEquipment(EInventorySlot.FeetArmor, 143);
				James.Inventory = templateAlb.CloseTemplate();
				James.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					James.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(James, GameObjectEvent.Interact, new CoreEventHandler(TalkToJames));
			GameEventMgr.AddHandler(James, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToJames));

			James.AddQuestToGive(typeof (DailyDanaoinKillLvl40AlbQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (James == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(James, GameObjectEvent.Interact, new CoreEventHandler(TalkToJames));
			GameEventMgr.RemoveHandler(James, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToJames));

			James.RemoveQuestToGive(typeof (DailyDanaoinKillLvl40AlbQuest));
		}

		private static void TalkToJames(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(James.CanGiveQuest(typeof (DailyDanaoinKillLvl40AlbQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DailyDanaoinKillLvl40AlbQuest quest = player.IsDoingQuest(typeof (DailyDanaoinKillLvl40AlbQuest)) as DailyDanaoinKillLvl40AlbQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							James.SayTo(player, "You will find Danaoin Farmers in the North or West of Lyonesse.");
							break;
						case 2:
							James.SayTo(player, "Hello " + player.Name + ", did you [kill] the Danaoin Farmers?");
							break;
					}
				}
				else
				{
					James.SayTo(player, "Hello "+ player.Name +", I am James. "+
					                       "The Danaoin Farmers down in Lyonesse are overfarming the land and destroying Albion's natural resources.\n"+
					                       "\nCan you [clear the Danaoin] to save Albion?");
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
						case "clear the Danaoin":
							player.Out.SendQuestSubscribeCommand(James, QuestMgr.GetIDForQuestType(typeof(DailyDanaoinKillLvl40AlbQuest)), "Will you help James "+questTitle+"");
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
			if (player.IsDoingQuest(typeof (DailyDanaoinKillLvl40AlbQuest)) != null)
				return true;

			// This checks below are only performed is player isn't doing quest already

			//if (player.HasFinishedQuest(typeof(Academy_47)) == 0) return false;

			//if (!CheckPartAccessible(player,typeof(CityOfCamelot)))
			//	return false;

			return player.Level >= minimumLevel && player.Level <= maximumLevel;
		}
		
		public override void LoadQuestParameters()
		{
			danaoinKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, danaoinKilled.ToString());
		}


		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			DailyDanaoinKillLvl40AlbQuest quest = player.IsDoingQuest(typeof (DailyDanaoinKillLvl40AlbQuest)) as DailyDanaoinKillLvl40AlbQuest;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DailyDanaoinKillLvl40AlbQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(James.CanGiveQuest(typeof (DailyDanaoinKillLvl40AlbQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DailyDanaoinKillLvl40AlbQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for your help.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!James.GiveQuest(typeof (DailyDanaoinKillLvl40AlbQuest), player, 1))
					return;

				James.SayTo(player, "You will find the Danaoin Farmers in Lyonesse.");

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
						return "Find Danaoin Farmers in the West or North in Lyonesse. \nKilled: Danaoin Farmers ("+ danaoinKilled +" | 10)";
					case 2:
						return "Return to James in Caer Gothwaite for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(DailyDanaoinKillLvl40AlbQuest)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
			if (gArgs.Target.Name.ToLower() != "danaoin farmer") return;
			danaoinKilled++;
			player.Out.SendMessage("[Daily] Danaoin Farmers Killed: ("+danaoinKilled+" | "+MAX_KILLED+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (danaoinKilled >= MAX_KILLED)
			{
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "DanaoinKillQuestAlb";
			set { ; }
		}

		public override void FinishQuest()
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/10);
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level,50,Util.Random(50)), "You receive {0} as a reward.");
			CoreRoGMgr.GenerateReward(m_questPlayer, 100);
			danaoinKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
