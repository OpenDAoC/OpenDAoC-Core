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

public class WeeklyEpicRvrMobsLvl50HibQuest : Quests.WeeklyQuest
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	private const string questTitle = "[Weekly] Frontier Cleanup";
	private const int minimumLevel = 50;
	private const int maximumLevel = 50;
	
	// Kill Goal
	private const int MAX_KILLED = 1;
	// Quest Counter
	private int _evernKilled = 0;
	private int _glacierGiantKilled = 0;
	private int _greenKnightKilled = 0;
	
	private static GameNpc Cola = null; // Start NPC

	private const string EVERN_NAME = "Evern";
	private const string GREENKNIGHT_NAME = "Green Knight";
	private const string GLACIERGIANT_NAME = "Glacier Giant";
	
	// Constructors
	public WeeklyEpicRvrMobsLvl50HibQuest() : base()
	{
	}

	public WeeklyEpicRvrMobsLvl50HibQuest(GamePlayer questingPlayer) : base(questingPlayer)
	{
	}

	public WeeklyEpicRvrMobsLvl50HibQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
	{
	}

	public WeeklyEpicRvrMobsLvl50HibQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

		GameNpc[] npcs = WorldMgr.GetNPCsByName("Cola", ERealm.Hibernia);

		if (npcs.Length > 0)
			foreach (GameNpc npc in npcs)
				if (npc.CurrentRegionID == 200 && npc.X == 334793 && npc.Y == 420805)
				{
					Cola = npc;
					break;
				}

		if (Cola == null)
		{
			if (log.IsWarnEnabled)
				log.Warn("Could not find Cola , creating it ...");
			Cola = new GameNpc();
			Cola.Model = 583;
			Cola.Name = "Cola";
			Cola.GuildName = "Realm Logistics";
			Cola.Realm = ERealm.Hibernia;
			//Druim Ligen Location
			Cola.CurrentRegionID = 200;
			Cola.Size = 50;
			Cola.Level = 59;
			Cola.X = 334793;
			Cola.Y = 420805;
			Cola.Z = 5184;
			Cola.Heading = 1586;
			Cola.AddToWorld();
			if (SAVE_INTO_DATABASE)
			{
				Cola.SaveIntoDatabase();
			}
		}

		#endregion

		#region defineItems
		#endregion

		#region defineObject
		#endregion

		GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
		GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

		GameEventMgr.AddHandler(Cola, GameObjectEvent.Interact, new CoreEventHandler(TalkToCola));
		GameEventMgr.AddHandler(Cola, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToCola));

		/* Now we bring to Cola the possibility to give this quest to players */
		Cola.AddQuestToGive(typeof (WeeklyEpicRvrMobsLvl50HibQuest));

		if (log.IsInfoEnabled)
			log.Info("Quest \"" + questTitle + "\" initialized");
	}

	[ScriptUnloadedEvent]
	public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
	{
		//if not loaded, don't worry
		if (Cola == null)
			return;
		// remove handlers
		GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
		GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

		GameEventMgr.RemoveHandler(Cola, GameObjectEvent.Interact, new CoreEventHandler(TalkToCola));
		GameEventMgr.RemoveHandler(Cola, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToCola));

		/* Now we remove to Cola the possibility to give this quest to players */
		Cola.RemoveQuestToGive(typeof (WeeklyEpicRvrMobsLvl50HibQuest));
	}

	private static void TalkToCola(CoreEvent e, object sender, EventArgs args)
	{
		//We get the player from the event arguments and check if he qualifies		
		GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
		if (player == null)
			return;

		if(Cola.CanGiveQuest(typeof (WeeklyEpicRvrMobsLvl50HibQuest), player)  <= 0)
			return;

		//We also check if the player is already doing the quest
		WeeklyEpicRvrMobsLvl50HibQuest quest = player.IsDoingQuest(typeof (WeeklyEpicRvrMobsLvl50HibQuest)) as WeeklyEpicRvrMobsLvl50HibQuest;

		if (e == GameObjectEvent.Interact)
		{
			if (quest != null)
			{
				switch (quest.Step)
				{
					case 1:
						Cola.SayTo(player, player.Name + ", please find allies and kill the epic creatures in frontiers for Hibernia!");
						break;
					case 2:
						Cola.SayTo(player, "Hello " + player.Name + ", did you [slay the creatures] and return for your reward?");
						break;
				}
			}
			else
			{
				Cola.SayTo(player, "Hello "+ player.Name +", I am Cola. Some large monsters have blocked the supply lines in our frontier, and I could use your help in getting rid of them.\n"+
				                   "You'll probably need to gather some friends for this one. We've lost a lot of good soldiers already. \n\n"+
				                   "Can you support Hibernia and [kill the epic creatures] in frontiers?");
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
					case "kill the epic creatures":
						player.Out.SendQuestSubscribeCommand(Cola, QuestMgr.GetIDForQuestType(typeof(WeeklyEpicRvrMobsLvl50HibQuest)), "Will you help Cola "+questTitle+"?");
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
		if (player.IsDoingQuest(typeof (WeeklyEpicRvrMobsLvl50HibQuest)) != null)
			return true;

		// This checks below are only performed is player isn't doing quest already

		//if (player.HasFinishedQuest(typeof(Academy_47)) == 0) return false;

		//if (!CheckPartAccessible(player,typeof(CityOfCamelot)))
		//	return false;

		if (player.Level < minimumLevel || player.Level > maximumLevel)
			return false;

		return true;
	}

	public override string QuestPropertyKey
	{
		get => "EpicRvRMobsWeeklyQuestHib";
		set { ; }
	}
	
	public override void LoadQuestParameters()
	{
		_evernKilled = GetCustomProperty(EVERN_NAME) != null ? int.Parse(GetCustomProperty(EVERN_NAME)) : 0;
		_glacierGiantKilled = GetCustomProperty(GLACIERGIANT_NAME) != null ? int.Parse(GetCustomProperty(GLACIERGIANT_NAME)) : 0;
		_greenKnightKilled = GetCustomProperty(GREENKNIGHT_NAME) != null ? int.Parse(GetCustomProperty(GREENKNIGHT_NAME)) : 0;
	}

	public override void SaveQuestParameters()
	{
		SetCustomProperty(EVERN_NAME, _evernKilled.ToString());
		SetCustomProperty(GLACIERGIANT_NAME, _glacierGiantKilled.ToString());
		SetCustomProperty(GREENKNIGHT_NAME, _greenKnightKilled.ToString());
	}

	private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
	{
		WeeklyEpicRvrMobsLvl50HibQuest quest = player.IsDoingQuest(typeof (WeeklyEpicRvrMobsLvl50HibQuest)) as WeeklyEpicRvrMobsLvl50HibQuest;

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

		if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(WeeklyEpicRvrMobsLvl50HibQuest)))
			return;

		if (e == GamePlayerEvent.AcceptQuest)
			CheckPlayerAcceptQuest(qargs.Player, 0x01);
		else if (e == GamePlayerEvent.DeclineQuest)
			CheckPlayerAcceptQuest(qargs.Player, 0x00);
	}

	private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
	{
		if(Cola.CanGiveQuest(typeof (WeeklyEpicRvrMobsLvl50HibQuest), player)  <= 0)
			return;

		if (player.IsDoingQuest(typeof (WeeklyEpicRvrMobsLvl50HibQuest)) != null)
			return;

		if (response == 0x00)
		{
			player.Out.SendMessage("Thank you for your help.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
		}
		else
		{
			//Check if we can add the quest!
			if (!Cola.GiveQuest(typeof (WeeklyEpicRvrMobsLvl50HibQuest), player, 1))
				return;

			Cola.SayTo(player, "Please, find the epic monsters in frontiers and return for your reward.");

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
					return "Find and slay the three dangerous epic monsters! \n" +
					       "Killed: " + EVERN_NAME + " ("+ _evernKilled +" | " + MAX_KILLED + ")\n" +
					       "Killed: " + GREENKNIGHT_NAME + " ("+ _greenKnightKilled +" | " + MAX_KILLED + ")\n" +
					       "Killed: " + GLACIERGIANT_NAME + " ("+ _glacierGiantKilled +" | " + MAX_KILLED + ")\n";
				case 2:
					return "Return to Cola for your Reward.";
			}
			return base.Description;
		}
	}

	public override void Notify(CoreEvent e, object sender, EventArgs args)
	{
		GamePlayer player = sender as GamePlayer;

		if (player?.IsDoingQuest(typeof(WeeklyEpicRvrMobsLvl50HibQuest)) == null)
			return;

		if (sender != m_questPlayer)
			return;

		if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
		EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

		if (gArgs.Target.Name.ToLower() == EVERN_NAME.ToLower() && gArgs.Target is GameNpc && _evernKilled < MAX_KILLED)
		{
			_evernKilled = 1;
			player.Out.SendMessage("[Weekly] You killed " + EVERN_NAME + ": (" + _evernKilled + " | " + MAX_KILLED + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
		}
		else if (gArgs.Target.Name.ToLower() == GREENKNIGHT_NAME.ToLower() && gArgs.Target is GameNpc && _greenKnightKilled < MAX_KILLED)
		{
			_greenKnightKilled = 1;
			player.Out.SendMessage("[Weekly] You killed " + GREENKNIGHT_NAME + ": (" + _greenKnightKilled + " | " + MAX_KILLED + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
		}
		else if (gArgs.Target.Name.ToLower() == GLACIERGIANT_NAME.ToLower() && gArgs.Target is GameNpc && _glacierGiantKilled < MAX_KILLED)
		{
			_glacierGiantKilled = 1;
			player.Out.SendMessage("[Weekly] You killed " + GLACIERGIANT_NAME + ": (" + _glacierGiantKilled + " | " + MAX_KILLED + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
			player.Out.SendQuestUpdate(this);
		}
			
		if (_evernKilled >= MAX_KILLED && _greenKnightKilled >= MAX_KILLED && _glacierGiantKilled>= MAX_KILLED)
		{
			// FinishQuest or go back to Dean
			Step = 2;
		}
	}

	public override void FinishQuest()
	{
		//m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/10, true);
		m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level * 5,32,Util.Random(50)), "You receive {0} as a reward.");
		CoreRogMgr.GenerateReward(m_questPlayer, 1500);
		CoreRogMgr.GenerateJewel(m_questPlayer, (byte)(m_questPlayer.Level + 1), m_questPlayer.Level + Util.Random(10, 11));
		_evernKilled = 0;
		_glacierGiantKilled = 0;
		_greenKnightKilled = 0;
		base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
	}
}