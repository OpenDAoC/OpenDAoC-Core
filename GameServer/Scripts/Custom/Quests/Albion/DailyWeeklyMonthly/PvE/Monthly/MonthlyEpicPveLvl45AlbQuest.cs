using System;
using System.Reflection;
using Core.Database;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.Quests;
using log4net;

namespace Core.GS.MonthlyQuest.Albion
{
	public class MonthlyEpicPveLvl45AlbQuest : Quests.MonthlyQuest
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
		private int _orylleKilled = 0;
		private int _xanxicarKilled = 0;

		private static GameNpc James = null; // Start NPC

		private const string Orylle_NAME = "Orylle";
		private const string Xanxicar_NAME = "Xanxicar";
		
		
		// Constructors
		public MonthlyEpicPveLvl45AlbQuest() : base()
		{
		}

		public MonthlyEpicPveLvl45AlbQuest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public MonthlyEpicPveLvl45AlbQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public MonthlyEpicPveLvl45AlbQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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
			
			James.AddQuestToGive(typeof (MonthlyEpicPveLvl45AlbQuest));

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

			James.RemoveQuestToGive(typeof (MonthlyEpicPveLvl45AlbQuest));
		}

		private static void TalkToJames(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(James.CanGiveQuest(typeof (MonthlyEpicPveLvl45AlbQuest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			MonthlyEpicPveLvl45AlbQuest quest = player.IsDoingQuest(typeof (MonthlyEpicPveLvl45AlbQuest)) as MonthlyEpicPveLvl45AlbQuest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							James.SayTo(player, player.Name + ", please find allies and kill the epic creatures in Krondon and in the Crystal Cave of Avalon City!");
							break;
						case 2:
							James.SayTo(player, "Hello " + player.Name + ", did you [slay the creatures] and return for your reward?");
							break;
					}
				}
				else
				{
					James.SayTo(player, "Hello "+ player.Name +", I am James. For several months the situation in Krondon and in the Crystal Cave of Avalon City has changed. " +
					                    "A place of mineral wealth and natural resources is now a place of violence and poisoning. \n\n"+
					                    "Can you support Albion and [kill Orylle and Xanxicar] in Krondon and in the Crystal Cave of Avalon City?");
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
						case "kill Orylle and Xanxicar":
							player.Out.SendQuestSubscribeCommand(James, QuestMgr.GetIDForQuestType(typeof(MonthlyEpicPveLvl45AlbQuest)), "Will you help James "+questTitle+"?");
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
			if (player.IsDoingQuest(typeof (MonthlyEpicPveLvl45AlbQuest)) != null)
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
			MonthlyEpicPveLvl45AlbQuest quest = player.IsDoingQuest(typeof (MonthlyEpicPveLvl45AlbQuest)) as MonthlyEpicPveLvl45AlbQuest;

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

		private static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(MonthlyEpicPveLvl45AlbQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(James.CanGiveQuest(typeof (MonthlyEpicPveLvl45AlbQuest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (MonthlyEpicPveLvl45AlbQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for your help.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!James.GiveQuest(typeof (MonthlyEpicPveLvl45AlbQuest), player, 1))
					return;

				James.SayTo(player, "Please, find the epic monsters in Krondon and in the Crystal Cave of Avalon City and return for your reward.");

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
						return "Make your way and defeat the epic creatures in Krondon as well as in the Crystal Cave of Avalon City! \n" +
						       "Killed: " + Orylle_NAME + " ("+ _orylleKilled +" | " + MAX_KILLED + ") in Krondon\n" +
						       "Killed: " + Xanxicar_NAME + " ("+ _xanxicarKilled +" | " + MAX_KILLED + ") in Crystal Cave\n";
					case 2:
						return "Return to James for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(MonthlyEpicPveLvl45AlbQuest)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

			if (gArgs.Target.Name.ToLower() == Orylle_NAME.ToLower() && gArgs.Target is GameNpc && _orylleKilled < MAX_KILLED)
			{
				_orylleKilled = 1;
				player.Out.SendMessage("[Monthly] You killed " + Orylle_NAME + ": (" + _orylleKilled + " | " + MAX_KILLED + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}
			else if (gArgs.Target.Name.ToLower() == Xanxicar_NAME.ToLower() && gArgs.Target is GameNpc && _xanxicarKilled < MAX_KILLED)
			{
				_xanxicarKilled = 1;
				player.Out.SendMessage("[Monthly] You killed " + Xanxicar_NAME + ": (" + _xanxicarKilled + " | " + MAX_KILLED + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
				player.Out.SendQuestUpdate(this);
			}

			if (_orylleKilled >= MAX_KILLED && _xanxicarKilled >= MAX_KILLED)
			{
				Step = 2;
			}
		}
		
		public override string QuestPropertyKey
		{
			get => "MonthlyEpicPvEQuestAlb";
			set { ; }
		}
		
		public override void LoadQuestParameters()
		{
			_orylleKilled = GetCustomProperty(Orylle_NAME) != null ? int.Parse(GetCustomProperty(Orylle_NAME)) : 0;
			_xanxicarKilled = GetCustomProperty(Xanxicar_NAME) != null ? int.Parse(GetCustomProperty(Xanxicar_NAME)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(Orylle_NAME, _orylleKilled.ToString());
			SetCustomProperty(Xanxicar_NAME, _xanxicarKilled.ToString());
		}

		public override void FinishQuest()
		{
			if (m_questPlayer.Inventory.IsSlotsFree(3, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
			{
				m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel));
				m_questPlayer.AddMoney(MoneyMgr.GetMoney(0, 0, m_questPlayer.Level * 8, 32, Util.Random(50)),
					"You receive {0} as a reward.");
				CoreRoGMgr.GenerateReward(m_questPlayer, 3000);
				CoreRoGMgr.GenerateBeetleCarapace(m_questPlayer, 2);
				CoreRoGMgr.GenerateJewel(m_questPlayer, 51);
				_orylleKilled = 0;
				_xanxicarKilled = 0;
				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
			}
			else
			{
				m_questPlayer.Out.SendMessage("Clear three slots of your inventory for your reward", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}
	}
}
