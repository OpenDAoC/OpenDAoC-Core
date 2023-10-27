using System;
using System.Reflection;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Packets.Server;
using Core.GS.Quests;
using Core.GS.Server;
using Core.GS.World;
using log4net;

namespace Core.GS.Scripts.Custom;

public class WeeklyPlayerKillLvl1AlbQuest : Quests.WeeklyQuest
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	private const string questTitle = "[Weekly] Fen's Many Friends";
	private const int minimumLevel = 1;
	private const int maximumLevel = 50;

	private static GameNpc ReyAlb = null; // Start NPC

	private int PlayersKilled = 0;
	
	// Kill Goal
	private static int MAX_KILLING_GOAL = 100;
	// prevent grey killing
	private const int MIN_PLAYER_CON = -3;

	// Constructors
	public WeeklyPlayerKillLvl1AlbQuest() : base()
	{
	}

	public WeeklyPlayerKillLvl1AlbQuest(GamePlayer questingPlayer) : base(questingPlayer)
	{
	}

	public WeeklyPlayerKillLvl1AlbQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
	{
	}

	public WeeklyPlayerKillLvl1AlbQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

		GameNpc[] npcs = WorldMgr.GetNPCsByName("Rey", ERealm.Albion);

		if (npcs.Length > 0)
			foreach (GameNpc npc in npcs)
			{
				if (npc.CurrentRegionID == 1 && npc.X == 583867 && npc.Y == 477355)
				{
					ReyAlb = npc;
					break;
				}
			}

		if (ReyAlb == null)
		{
			if (log.IsWarnEnabled)
				log.Warn("Could not find Rey , creating it ...");
			ReyAlb = new GameNpc();
			ReyAlb.Model = 26;
			ReyAlb.Name = "Rey";
			ReyAlb.GuildName = "Bone Collector";
			ReyAlb.Realm = ERealm.Albion;
			//Druim Ligen Location
			ReyAlb.CurrentRegionID = 1;
			ReyAlb.Size = 60;
			ReyAlb.Level = 59;
			ReyAlb.X = 583867;
			ReyAlb.Y = 477355;
			ReyAlb.Z = 2600;
			ReyAlb.Heading = 3054;
			ReyAlb.Flags |= ENpcFlags.PEACE;
			ReyAlb.AddToWorld();
			if (SAVE_INTO_DATABASE)
			{
				ReyAlb.SaveIntoDatabase();
			}
		}

		#endregion

		#region defineItems
		#endregion

		#region defineObject
		#endregion

		GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
		GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

		GameEventMgr.AddHandler(ReyAlb, GameObjectEvent.Interact, new CoreEventHandler(TalkToRey));
		GameEventMgr.AddHandler(ReyAlb, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToRey));

		/* Now we bring to Rey the possibility to give this quest to players */
		ReyAlb.AddQuestToGive(typeof (WeeklyPlayerKillLvl1AlbQuest));

		if (log.IsInfoEnabled)
			log.Info("Quest \"" + questTitle + "\" initialized");
	}

	[ScriptUnloadedEvent]
	public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
	{
		//if not loaded, don't worry
		if (ReyAlb == null)
			return;
		// remove handlers
		GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
		GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

		GameEventMgr.RemoveHandler(ReyAlb, GameObjectEvent.Interact, new CoreEventHandler(TalkToRey));
		GameEventMgr.RemoveHandler(ReyAlb, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToRey));

		/* Now we remove to Rey the possibility to give this quest to players */
		ReyAlb.RemoveQuestToGive(typeof (WeeklyPlayerKillLvl1AlbQuest));
	}

	private static void TalkToRey(CoreEvent e, object sender, EventArgs args)
	{
		//We get the player from the event arguments and check if he qualifies		
		GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
		if (player == null)
			return;

		if(ReyAlb.CanGiveQuest(typeof (WeeklyPlayerKillLvl1AlbQuest), player)  <= 0)
			return;

		//We also check if the player is already doing the quest
		WeeklyPlayerKillLvl1AlbQuest quest = player.IsDoingQuest(typeof (WeeklyPlayerKillLvl1AlbQuest)) as WeeklyPlayerKillLvl1AlbQuest;

		if (e == GameObjectEvent.Interact)
		{
			if (quest != null)
			{
				switch (quest.Step)
				{
					case 1:
						ReyAlb.SayTo(player, "You will find suitable players in the frontiers or in battlegrounds.");
						break;
					case 2:
						ReyAlb.SayTo(player, "Hello " + player.Name + ", did you [demolish some skulls]?");
						break;
				}
			}
			else
			{
				ReyAlb.SayTo(player, "Uh oh, "+ player.Name +". "+
				                       "Fen put in a bulk order this time. There's no way I can collect this many bones in a week. \n" +
				                       "I need your help with this, are you up for some [bone harvesting]?");
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
					case "bone harvesting":
						player.Out.SendQuestSubscribeCommand(ReyAlb, QuestMgr.GetIDForQuestType(typeof(WeeklyPlayerKillLvl1AlbQuest)), "Will you undertake " + questTitle + "?");
						break;
				}
			}
			else
			{
				switch (wArgs.Text)
				{
					case "demolish some skulls":
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
		if (player.IsDoingQuest(typeof (WeeklyPlayerKillLvl1AlbQuest)) != null)
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
		WeeklyPlayerKillLvl1AlbQuest quest = player.IsDoingQuest(typeof (WeeklyPlayerKillLvl1AlbQuest)) as WeeklyPlayerKillLvl1AlbQuest;

		if (quest == null)
			return;

		if (response == 0x00)
		{
			SendSystemMessage(player, "Good, now go out there and shed some blood!");
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

		if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(WeeklyPlayerKillLvl1AlbQuest)))
			return;

		if (e == GamePlayerEvent.AcceptQuest)
			CheckPlayerAcceptQuest(qargs.Player, 0x01);
		else if (e == GamePlayerEvent.DeclineQuest)
			CheckPlayerAcceptQuest(qargs.Player, 0x00);
	}

	private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
	{
		if(ReyAlb.CanGiveQuest(typeof (WeeklyPlayerKillLvl1AlbQuest), player)  <= 0)
			return;

		if (player.IsDoingQuest(typeof (WeeklyPlayerKillLvl1AlbQuest)) != null)
			return;

		if (response == 0x00)
		{
			player.Out.SendMessage("Thank you for helping me.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
		}
		else
		{
			//Check if we can add the quest!
			if (!ReyAlb.GiveQuest(typeof (WeeklyPlayerKillLvl1AlbQuest), player, 1))
				return;

			ReyAlb.SayTo(player, "You will find suitable players in the frontiers or in battlegrounds.");

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
					return "You will find suitable players in the frontiers or in battlegrounds. \nPlayers Killed: ("+ PlayersKilled +" | "+ MAX_KILLING_GOAL +")";
				case 2:
					return "Return to Rey in Castle Sauvage for your Reward.";
			}
			return base.Description;
		}
	}

	public override void Notify(CoreEvent e, object sender, EventArgs args)
	{
		GamePlayer player = sender as GamePlayer;

		if (player == null || player.IsDoingQuest(typeof(WeeklyPlayerKillLvl1AlbQuest)) == null)
			return;

		if (sender != m_questPlayer)
			return;

		if (e != GameLivingEvent.EnemyKilled || Step != 1) return;
		EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

		if (gArgs.Target.Realm == 0 || gArgs.Target.Realm == player.Realm || gArgs.Target is not GamePlayer ||
		    !(player.GetConLevel(gArgs.Target) > MIN_PLAYER_CON)) return;
		PlayersKilled++;
		player.Out.SendMessage("[Weekly] Enemy Killed: ("+PlayersKilled+" | "+MAX_KILLING_GOAL+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
		player.Out.SendQuestUpdate(this);
				
		if (PlayersKilled >= MAX_KILLING_GOAL)
		{
			// FinishQuest or go back to Dean
			Step = 2;
		}
	}
	
	public override string QuestPropertyKey
	{
		get => "PlayerKillWeeklyQuestAlb";
		set { ; }
	}
	
	public override void LoadQuestParameters()
	{
		PlayersKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
	}

	public override void SaveQuestParameters()
	{
		SetCustomProperty(QuestPropertyKey, PlayersKilled.ToString());
	}

	public override void FinishQuest()
	{
		int reward = ServerProperty.WEEKLY_RVR_REWARD;
		
		m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/5);
		m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level * 5,32,Util.Random(50)), "You receive {0} as a reward.");
		CoreRogMgr.GenerateReward(m_questPlayer, 1500);
		CoreRogMgr.GenerateJewel(m_questPlayer, (byte)(m_questPlayer.Level + 1), m_questPlayer.Level + Util.Random(10, 20));
		PlayersKilled = 0;
		
		if (reward > 0)
		{
			m_questPlayer.Out.SendMessage($"You have been rewarded {reward} Realmpoints for finishing Weekly Quest.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
			m_questPlayer.GainRealmPoints(reward, false);
			m_questPlayer.Out.SendUpdatePlayer();
		}
		base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		
	}
}