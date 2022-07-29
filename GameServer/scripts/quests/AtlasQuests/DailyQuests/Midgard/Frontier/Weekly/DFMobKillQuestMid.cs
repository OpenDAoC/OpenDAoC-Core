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

namespace DOL.GS.DailyQuest.Midgard
{
	public class DFMobKillQuestMid : WeeklyQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string questTitle = "[Weekly] Darkness Falls Invasion";
		private const int minimumLevel = 30;
		private const int maximumLevel = 50;
		
		// Kill Goal
		private const int MAX_KILLED = 200;

		private static GameNPC Patrick = null; // Start NPC

		private int _mobsKilled = 0;

		// Constructors
		public DFMobKillQuestMid() : base()
		{
		}

		public DFMobKillQuestMid(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DFMobKillQuestMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DFMobKillQuestMid(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Patrick", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 249 && npc.X == 16639 && npc.Y == 18947)
					{
						Patrick = npc;
						break;
					}

			if (Patrick == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Patrick , creating it ...");
				Patrick = new GameNPC();
				Patrick.Model = 138;
				Patrick.Name = "Patrick";
				Patrick.GuildName = "Realm Logistics";
				Patrick.Realm = eRealm.Midgard;
				//Darkness Falls Mid Entrance Location
				Patrick.CurrentRegionID = 249;
				Patrick.Size = 50;
				Patrick.Level = 59;
				Patrick.X = 16639;
				Patrick.Y = 18947;
				Patrick.Z = 22892;
				Patrick.Heading = 2117;
				GameNpcInventoryTemplate templateMid = new GameNpcInventoryTemplate();
				templateMid.AddNPCEquipment(eInventorySlot.TorsoArmor, 751,0,0,3);
				templateMid.AddNPCEquipment(eInventorySlot.LegsArmor, 752);
				templateMid.AddNPCEquipment(eInventorySlot.ArmsArmor, 753);
				templateMid.AddNPCEquipment(eInventorySlot.HandsArmor, 754, 0,0,3);
				templateMid.AddNPCEquipment(eInventorySlot.FeetArmor, 755, 0, 0, 3);
				templateMid.AddNPCEquipment(eInventorySlot.Cloak, 677);
				Patrick.Inventory = templateMid.CloseTemplate();
				Patrick.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Patrick.SaveIntoDatabase();
				}
			}

			#endregion

			#region defineItems
			#endregion

			#region defineObject
			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Patrick, GameObjectEvent.Interact, new DOLEventHandler(TalkToPatrick));
			GameEventMgr.AddHandler(Patrick, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToPatrick));

			Patrick.AddQuestToGive(typeof (DFMobKillQuestMid));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" Mid initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Patrick == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Patrick, GameObjectEvent.Interact, new DOLEventHandler(TalkToPatrick));
			GameEventMgr.RemoveHandler(Patrick, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToPatrick));

			Patrick.RemoveQuestToGive(typeof (DFMobKillQuestMid));
		}

		private static void TalkToPatrick(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Patrick.CanGiveQuest(typeof (DFMobKillQuestMid), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DFMobKillQuestMid quest = player.IsDoingQuest(typeof (DFMobKillQuestMid)) as DFMobKillQuestMid;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Patrick.SayTo(player, "Head into Darkness Falls and slay monsters so they don\'t spread in our realm!");
							break;
						case 2:
							Patrick.SayTo(player, "Hello " + player.Name + ", did you [slay monsters] for your reward?");
							break;
					}
				}
				else
				{
					Patrick.SayTo(player, "Hello "+ player.Name +", I am Patrick. I have received word from a hunter that forces are building in Darkness Falls. "+
					                    "Clear out as many demons as you can find, and come back to me only when the halls of the dungeon are purged of their influence. \n\n"+
					                    "Can you [stop the invasion]?");
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
						case "stop the invasion":
							player.Out.SendQuestSubscribeCommand(Patrick, QuestMgr.GetIDForQuestType(typeof(DFMobKillQuestMid)), "Will you help Patrick "+questTitle+"?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "slay monsters":
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
			if (player.IsDoingQuest(typeof (DFMobKillQuestMid)) != null)
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
			_mobsKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
		}

		public override void SaveQuestParameters()
		{
			SetCustomProperty(QuestPropertyKey, _mobsKilled.ToString());
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			DFMobKillQuestMid quest = player.IsDoingQuest(typeof (DFMobKillQuestMid)) as DFMobKillQuestMid;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DFMobKillQuestMid)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Patrick.CanGiveQuest(typeof (DFMobKillQuestMid), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DFMobKillQuestMid)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Midgard.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Patrick.GiveQuest(typeof (DFMobKillQuestMid), player, 1))
					return;

				Patrick.SayTo(player, "Defend your realm, head into Darkness Falls and kill monsters for your reward.");

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
						return "Head into Darkness Falls and kill monsters for Midgard. \nKilled: Monster ("+ _mobsKilled +" | "+ MAX_KILLED +")";
					case 2:
						return "Return to Patrick in Darkness Falls for your Reward.";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player?.IsDoingQuest(typeof(DFMobKillQuestMid)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
			
			if (gArgs.Target is GamePet)
				return;

			if (gArgs.Target.Realm != 0 || gArgs.Target is not GameNPC || gArgs.Target.CurrentRegionID != 249 ||
			    !(player.GetConLevel(gArgs.Target) > -2)) return;
			if (player.Group != null)
			{
				double minRequiredCon = Math.Ceiling((double) (player.Group.MemberCount / 3));
				if (minRequiredCon > 3) minRequiredCon = 3;
				if (player.Group.Leader.GetConLevel(gArgs.Target) >= minRequiredCon)
					_mobsKilled++;
				else
				{
					player.Out.SendMessage("[Weekly] Monsters Killed in Darkness Falls - needs a higher level monster to count", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}
			}
			else
			{
				if(player.GetConLevel(gArgs.Target) > -1)
					_mobsKilled++;	
				else
				{
					player.Out.SendMessage("[Weekly] Monsters Killed in Darkness Falls - needs a higher level monster to count", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					return;
				}
			}
			player.Out.SendMessage("[Weekly] Monsters Killed in Darkness Falls: ("+_mobsKilled+" | "+MAX_KILLED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
					
			if (_mobsKilled >= MAX_KILLED)
			{
				Step = 2;
			}

		}
		
		public override string QuestPropertyKey
		{
			get => "DFMobKillQuestMid";
			set { ; }
		}

		public override void FinishQuest()
		{
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel), false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level * 5,32,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 1500);
			_mobsKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
