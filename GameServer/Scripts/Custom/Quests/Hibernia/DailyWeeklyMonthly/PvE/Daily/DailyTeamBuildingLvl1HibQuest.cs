using System;
using System.Reflection;
using System.Text;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Packets.Server;
using Core.GS.Players;
using Core.GS.Quests;
using Core.GS.Server;
using Core.GS.World;
using log4net;

namespace Core.GS.Scripts.Custom;

public class DailyTeamBuildingLvl1HibQuest : Quests.DailyQuest
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	private const string questTitle = "[Group Daily] A Team Building Exercise";
	private const int minimumLevel = 1;
	private const int maximumLevel = 50;

	// Kill Goal
	private const int MAX_KILLED = 25;
	
	private static GameNpc Dean = null; // Start NPC

	private bool HasGuardian = false;
	private bool HasNaturalist = false;
	private bool HasStalker = false;
	private bool HasMagicianForester = false;

	private int TeamBuildMobsKilled = 0;

	// Constructors
	public DailyTeamBuildingLvl1HibQuest() : base() {
	}

	public DailyTeamBuildingLvl1HibQuest(GamePlayer questingPlayer) : base(questingPlayer)
	{
	}

	public DailyTeamBuildingLvl1HibQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
	{
	}

	public DailyTeamBuildingLvl1HibQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

		GameNpc[] npcs = WorldMgr.GetNPCsByName("Dean", ERealm.Hibernia);

		if (npcs.Length > 0)
			foreach (GameNpc npc in npcs)
				if (npc.CurrentRegionID == 200 && npc.X == 334962 && npc.Y == 420687)
				{
					Dean = npc;
					break;
				}

		if (Dean == null)
		{
			if (log.IsWarnEnabled)
				log.Warn("Could not find Dean , creating it ...");
			Dean = new GameNpc();
			Dean.Model = 355;
			Dean.Name = "Dean";
			Dean.GuildName = "Advisor to the King";
			Dean.Realm = ERealm.Hibernia;
			//Druim Ligen Location
			Dean.CurrentRegionID = 200;
			Dean.Size = 50;
			Dean.Level = 59;
			Dean.X = 334962;
			Dean.Y = 420687;
			Dean.Z = 5184;
			Dean.Heading = 1571;
			Dean.AddToWorld();
			if (SAVE_INTO_DATABASE)
			{
				Dean.SaveIntoDatabase();
			}
		}

		#endregion

		#region defineItems
		#endregion

		#region defineObject
		#endregion

		GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
		GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

		GameEventMgr.AddHandler(Dean, GameObjectEvent.Interact, new CoreEventHandler(TalkToDean));
		GameEventMgr.AddHandler(Dean, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToDean));

		/* Now we bring to Dean the possibility to give this quest to players */
		Dean.AddQuestToGive(typeof (DailyTeamBuildingLvl1HibQuest));

		if (log.IsInfoEnabled)
			log.Info("Quest \"" + questTitle + "\" initialized");
	}

	[ScriptUnloadedEvent]
	public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
	{
		//if not loaded, don't worry
		if (Dean == null)
			return;
		// remove handlers
		GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
		GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

		GameEventMgr.RemoveHandler(Dean, GameObjectEvent.Interact, new CoreEventHandler(TalkToDean));
		GameEventMgr.RemoveHandler(Dean, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToDean));

		/* Now we remove to Dean the possibility to give this quest to players */
		Dean.RemoveQuestToGive(typeof (DailyTeamBuildingLvl1HibQuest));
	}

	private static void TalkToDean(CoreEvent e, object sender, EventArgs args)
	{
		//We get the player from the event arguments and check if he qualifies		
		GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
		if (player == null)
			return;

		if(Dean.CanGiveQuest(typeof (DailyTeamBuildingLvl1HibQuest), player)  <= 0)
			return;

		//We also check if the player is already doing the quest
		DailyTeamBuildingLvl1HibQuest quest = player.IsDoingQuest(typeof (DailyTeamBuildingLvl1HibQuest)) as DailyTeamBuildingLvl1HibQuest;

		if (e == GameObjectEvent.Interact)
		{
			if (quest != null)
			{
				switch (quest.Step)
				{
					case 1:
						Dean.SayTo(player, "Kill creatures in any RvR zone to help us clear more room for the armies to maneuver around.");
						break;
					case 2:
						Dean.SayTo(player, "Hello " + player.Name + ", did you [forge the bonds of unity]?");
						break;
				}
			}
			else
			{
				Dean.SayTo(player, "Hello "+ player.Name +", I am Dean. I help the king with logistics, and he's tasked me with getting things done around here. "+
				                       "The king recently implemented a new unity initiative and he wants you to help out.\n"+
				                       "What do you say, are you [feeling social]?");
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
					case "feeling social":
						player.Out.SendQuestSubscribeCommand(Dean, QuestMgr.GetIDForQuestType(typeof(DailyTeamBuildingLvl1HibQuest)), "Will you help Dean "+questTitle+"");
						break;
				}
			}
			else
			{
				switch (wArgs.Text)
				{
					case "forge the bonds of unity":
						if (quest.Step == 2)
						{
							player.Out.SendMessage("I can feel our realm growing more cohesive every day!", EChatType.CT_Chat, EChatLoc.CL_PopupWindow);
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
		if (player.IsDoingQuest(typeof (DailyTeamBuildingLvl1HibQuest)) != null)
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
		TeamBuildMobsKilled = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
	}

	public override void SaveQuestParameters()
	{
		SetCustomProperty(QuestPropertyKey, TeamBuildMobsKilled.ToString());
	}


	private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
	{
		DailyTeamBuildingLvl1HibQuest quest = player.IsDoingQuest(typeof (DailyTeamBuildingLvl1HibQuest)) as DailyTeamBuildingLvl1HibQuest;

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

		if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DailyTeamBuildingLvl1HibQuest)))
			return;

		if (e == GamePlayerEvent.AcceptQuest)
			CheckPlayerAcceptQuest(qargs.Player, 0x01);
		else if (e == GamePlayerEvent.DeclineQuest)
			CheckPlayerAcceptQuest(qargs.Player, 0x00);
	}

	private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
	{
		if(Dean.CanGiveQuest(typeof (DailyTeamBuildingLvl1HibQuest), player)  <= 0)
			return;

		if (player.IsDoingQuest(typeof (DailyTeamBuildingLvl1HibQuest)) != null)
			return;

		if (response == 0x00)
		{
			player.Out.SendMessage("Thank you for helping our realm prosper.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
		}
		else
		{
			//Check if we can add the quest!
			if (!Dean.GiveQuest(typeof (DailyTeamBuildingLvl1HibQuest), player, 1))
				return;

			Dean.SayTo(player, "Killing creatures in any RvR zone will work. Thanks for your service!");

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
					if (HasGuardian
					    && HasNaturalist
					    && HasStalker
					    && HasMagicianForester)
					{
						return "The spirit of unity flows through you. \n" +
						       "Kill orange con or higher mobs: \n" + 
						       "Orange+ Con Mobs Killed: ("+ TeamBuildMobsKilled +" | "+MAX_KILLED+")";
					}
					else
					{
						StringBuilder output = new StringBuilder("Kill orange con or higher mobs while in a group containing the following base classes:\n");
						if (!HasGuardian) output.Append("Guardian required\n");
						if (!HasNaturalist) output.Append("Naturalist required\n");
						if (!HasStalker) output.Append("Stalker required\n");
						if (!HasMagicianForester) output.Append("Magician/Forester required\n");
						output.Append("Orange+ Con Mobs Killed: ("+ TeamBuildMobsKilled +" | "+MAX_KILLED+")");
						return output.ToString();
					}
					
				case 2:
					return "Return to Dean in Druim Ligen for your Reward.";
			}
			return base.Description;
		}
	}

	public override void Notify(CoreEvent e, object sender, EventArgs args)
	{
		GamePlayer player = sender as GamePlayer;
		
		if (player?.IsDoingQuest(typeof(DailyTeamBuildingLvl1HibQuest)) == null)
			return;
		
		if (sender != m_questPlayer)
			return;
		
		if (e != GameLivingEvent.EnemyKilled || Step != 1) return;
		
		EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
		
		if (gArgs.Target is GameSummonedPet)
			return;

		if (player.Group != null)
		{
			foreach (var member in player.Group.GetMembersInTheGroup())
			{
				//update class counters
				if (member is GamePlayer gplayer)
				{
					if (gplayer.PlayerClass is ClassGuardian)
						HasGuardian = true;
					if (gplayer.PlayerClass is ClassNaturalist)
						HasNaturalist = true;
					if (gplayer.PlayerClass is ClassStalker)
						HasStalker = true;
					if (gplayer.PlayerClass is ClassMagician ||
					    gplayer.PlayerClass is ClassForester)
						HasMagicianForester = true;
				}
			}
			player.Out.SendQuestUpdate(this);
		}
		else
		{
			//if we're ever ungrouped, clear our grouped class counters
			HasGuardian = false;
			HasNaturalist = false;
			HasStalker = false;
			HasMagicianForester = false;
			player.Out.SendQuestUpdate(this);
		}
		
		if (!(player.GetConLevel(gArgs.Target) >= 1) || player.Group == null || !HasGuardian || !HasNaturalist ||
		    !HasStalker || !HasMagicianForester) return;
		
		TeamBuildMobsKilled++;
		player.Out.SendMessage(
			"[Group Daily] Monster killed: (" + TeamBuildMobsKilled + " | " + MAX_KILLED + ")",
			EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
		player.Out.SendQuestUpdate(this);
				
		if (TeamBuildMobsKilled >= MAX_KILLED)
		{
			// FinishQuest or go back to npc
			Step = 2;
		}

	}
	
	public override string QuestPropertyKey
	{
		get => "TeamBuildingHib";
		set { ; }
	}

	public override void FinishQuest()
	{
		m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/5);
		m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level,50,Util.Random(50)), "You receive {0} as a reward.");
		CoreRogMgr.GenerateReward(m_questPlayer, 300);
		TeamBuildMobsKilled = 0;
		base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
	}
}