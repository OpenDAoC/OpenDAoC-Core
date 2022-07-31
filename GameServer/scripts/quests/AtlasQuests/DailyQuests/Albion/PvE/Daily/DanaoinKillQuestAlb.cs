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

namespace DOL.GS.DailyQuest.Albion
{
	public class DanaoinKillQuestAlb : Quests.DailyQuest
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
		
		private static GameNPC James = null; // Start NPC

		private int danaoinKilled = 0;

		// Constructors
		public DanaoinKillQuestAlb() : base()
		{
		}

		public DanaoinKillQuestAlb(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public DanaoinKillQuestAlb(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public DanaoinKillQuestAlb(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

			GameNPC[] npcs = WorldMgr.GetNPCsByName("James", eRealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 51 && npc.X == 534044 && npc.Y == 549664)
					{
						James = npc;
						break;
					}

			if (James == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find James , creating it ...");
				James = new GameNPC();
				James.Model = 254;
				James.Name = "James";
				James.GuildName = "Advisor To The King";
				James.Realm = eRealm.Albion;
				James.CurrentRegionID = 51;
				James.Size = 50;
				James.Level = 59;
				//Caer Gothwaite Location
				James.X = 534044;
				James.Y = 549664;
				James.Z = 4940;
				James.Heading = 3143;
				GameNpcInventoryTemplate templateAlb = new GameNpcInventoryTemplate();
				templateAlb.AddNPCEquipment(eInventorySlot.TorsoArmor, 1005);
				templateAlb.AddNPCEquipment(eInventorySlot.HandsArmor, 142);
				templateAlb.AddNPCEquipment(eInventorySlot.FeetArmor, 143);
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

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(James, GameObjectEvent.Interact, new DOLEventHandler(TalkToJames));
			GameEventMgr.AddHandler(James, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToJames));

			James.AddQuestToGive(typeof (DanaoinKillQuestAlb));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (James == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(James, GameObjectEvent.Interact, new DOLEventHandler(TalkToJames));
			GameEventMgr.RemoveHandler(James, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToJames));

			James.RemoveQuestToGive(typeof (DanaoinKillQuestAlb));
		}

		private static void TalkToJames(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(James.CanGiveQuest(typeof (DanaoinKillQuestAlb), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			DanaoinKillQuestAlb quest = player.IsDoingQuest(typeof (DanaoinKillQuestAlb)) as DanaoinKillQuestAlb;

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
							player.Out.SendQuestSubscribeCommand(James, QuestMgr.GetIDForQuestType(typeof(DanaoinKillQuestAlb)), "Will you help James "+questTitle+"");
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
			if (player.IsDoingQuest(typeof (DanaoinKillQuestAlb)) != null)
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
			DanaoinKillQuestAlb quest = player.IsDoingQuest(typeof (DanaoinKillQuestAlb)) as DanaoinKillQuestAlb;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DanaoinKillQuestAlb)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(James.CanGiveQuest(typeof (DanaoinKillQuestAlb), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (DanaoinKillQuestAlb)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Thank you for helping Atlas.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!James.GiveQuest(typeof (DanaoinKillQuestAlb), player, 1))
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

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(DanaoinKillQuestAlb)) == null)
				return;
			
			if (sender != m_questPlayer)
				return;

			if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
			EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
			if (gArgs.Target.Name.ToLower() != "danaoin farmer") return;
			danaoinKilled++;
			player.Out.SendMessage("[Daily] Danaoin Farmers Killed: ("+danaoinKilled+" | "+MAX_KILLED+")", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
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
			m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/10, false);
			m_questPlayer.AddMoney(Money.GetMoney(0,0,m_questPlayer.Level,50,Util.Random(50)), "You receive {0} as a reward.");
			AtlasROGManager.GenerateOrbAmount(m_questPlayer, 100);
			danaoinKilled = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
	}
}
