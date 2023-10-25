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

public class DailyHardcoreKillAPlayerLvl1MidQuest : Quests.DailyQuest
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	private const string questTitle = "[Hardcore] Apex Predator";
	private const int minimumLevel = 1;
	private const int maximumLevel = 50;

	private static GameNpc SucciMid = null; // Start NPC

	private int PlayerKilled = 0;
	private int MAX_KillGoal = 1;

	// Constructors
	public DailyHardcoreKillAPlayerLvl1MidQuest() : base()
	{
	}

	public DailyHardcoreKillAPlayerLvl1MidQuest(GamePlayer questingPlayer) : base(questingPlayer)
	{
	}

	public DailyHardcoreKillAPlayerLvl1MidQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
	{
	}

	public DailyHardcoreKillAPlayerLvl1MidQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

		GameNpc[] npcs = WorldMgr.GetNPCsByName("Succi", ERealm.Midgard);

		if (npcs.Length > 0)
			foreach (GameNpc npc in npcs)
			{
				if (npc.CurrentRegionID == 100 && npc.X == 766767 && npc.Y == 670636)
				{
					SucciMid = npc;
					break;
				}
			}

		if (SucciMid == null)
		{
			if (log.IsWarnEnabled)
				log.Warn("Could not find SucciMid , creating it ...");
			SucciMid = new GameNpc();
			SucciMid.Model = 902;
			SucciMid.Name = "Succi";
			SucciMid.GuildName = "Spectre of Death";
			SucciMid.Realm = ERealm.Midgard;
			//Svasud Location
			SucciMid.CurrentRegionID = 100;
			SucciMid.Size = 60;
			SucciMid.Level = 59;
			SucciMid.X = 766767;
			SucciMid.Y = 670636;
			SucciMid.Z = 5736;
			SucciMid.Heading = 2536;
			SucciMid.Flags |= ENpcFlags.PEACE;
			SucciMid.AddToWorld();
			if (SAVE_INTO_DATABASE)
			{
				SucciMid.SaveIntoDatabase();
			}
		}

		#endregion

		#region defineItems
		#endregion

		#region defineObject
		#endregion

		GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
		GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

		GameEventMgr.AddHandler(SucciMid, GameObjectEvent.Interact, new CoreEventHandler(TalkToSucci));
		GameEventMgr.AddHandler(SucciMid, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToSucci));

		/* Now we bring to Dean the possibility to give this quest to players */
		SucciMid.AddQuestToGive(typeof (DailyHardcoreKillAPlayerLvl1MidQuest));

		if (log.IsInfoEnabled)
			log.Info("Quest \"" + questTitle + "\" initialized");
	}

	[ScriptUnloadedEvent]
	public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
	{
		//if not loaded, don't worry
		if (SucciMid == null)
			return;
		// remove handlers
		GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
		GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

		GameEventMgr.RemoveHandler(SucciMid, GameObjectEvent.Interact, new CoreEventHandler(TalkToSucci));
		GameEventMgr.RemoveHandler(SucciMid, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToSucci));

		/* Now we remove to Dean the possibility to give this quest to players */
		SucciMid.RemoveQuestToGive(typeof (DailyHardcoreKillAPlayerLvl1MidQuest));
	}

	private static void TalkToSucci(CoreEvent e, object sender, EventArgs args)
	{
		//We get the player from the event arguments and check if he qualifies		
		GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
		if (player == null)
			return;

		if(SucciMid.CanGiveQuest(typeof (DailyHardcoreKillAPlayerLvl1MidQuest), player)  <= 0)
			return;

		//We also check if the player is already doing the quest
		DailyHardcoreKillAPlayerLvl1MidQuest oranges = player.IsDoingQuest(typeof (DailyHardcoreKillAPlayerLvl1MidQuest)) as DailyHardcoreKillAPlayerLvl1MidQuest;

		if (e == GameObjectEvent.Interact)
		{
			if (oranges != null)
			{
				switch (oranges.Step)
				{
					case 1:
						SucciMid.SayTo(player, "Hunt, or be hunted.");
						break;
					case 2:
						SucciMid.SayTo(player, "" + player.Name + ". You have earned [another sunrise].");
						break;
				}
			}
			else
			{
				SucciMid.SayTo(player, "The flash of steel's bite. \n"+
				                     "One stands above, one below. \n" +
				                     "[Predator] eats well.");
				SucciMid.SayTo(player, " NOTE: This is a HARDCORE quest. If you die or join a group while doing this quest, it will be aborted automatically.");
			}
		}
			// The player whispered to the NPC
		else if (e == GameLivingEvent.WhisperReceive)
		{
			WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
			if (oranges == null)
			{
				switch (wArgs.Text.ToLower())
				{
					case "predator":
						player.Out.SendQuestSubscribeCommand(SucciMid, QuestMgr.GetIDForQuestType(typeof(DailyHardcoreKillAPlayerLvl1MidQuest)), "Will you undertake " + questTitle + "?");
						break;
				}
			}
			else
			{
				switch (wArgs.Text)
				{
					case "another sunrise":
						if (oranges.Step == 2)
						{
							player.Out.SendMessage("Enjoy your meal. With luck, it shall not be your last.", EChatType.CT_Chat, EChatLoc.CL_PopupWindow);
							oranges.FinishQuest();
						}
						break;
					case "abort":
						player.Out.SendCustomDialog("To face one's own demise is not for the faint of heart. Death has turned its back on you for today.", new CustomDialogResponse(CheckPlayerAbortQuest));
						break;
				}
			}
		}
	}
	
	public override bool CheckQuestQualification(GamePlayer player)
	{
		// if the player is already doing the quest his level is no longer of relevance
		if (player.IsDoingQuest(typeof (DailyHardcoreKillAPlayerLvl1MidQuest)) != null)
			return true;

		// This checks below are only performed is player isn't doing quest already

		if (player.Level < minimumLevel || player.Level > maximumLevel)
			return false;

		return true;
	}

	private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
	{
		DailyHardcoreKillAPlayerLvl1MidQuest oranges = player.IsDoingQuest(typeof (DailyHardcoreKillAPlayerLvl1MidQuest)) as DailyHardcoreKillAPlayerLvl1MidQuest;

		if (oranges == null)
			return;

		if (response == 0x00)
		{
			SendSystemMessage(player, "To face one's own demise is not for the faint of heart.");
		}
		else
		{
			SendSystemMessage(player, "Aborting Quest " + questTitle + ".");
			oranges.AbortQuest();
		}
	}

	private static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
	{
		QuestEventArgs qargs = args as QuestEventArgs;
		if (qargs == null)
			return;

		if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(DailyHardcoreKillAPlayerLvl1MidQuest)))
			return;

		if (e == GamePlayerEvent.AcceptQuest)
			CheckPlayerAcceptQuest(qargs.Player, 0x01);
		else if (e == GamePlayerEvent.DeclineQuest)
			CheckPlayerAcceptQuest(qargs.Player, 0x00);
	}

	private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
	{
		if(SucciMid.CanGiveQuest(typeof (DailyHardcoreKillAPlayerLvl1MidQuest), player)  <= 0)
			return;

		if (player.IsDoingQuest(typeof (DailyHardcoreKillAPlayerLvl1MidQuest)) != null)
			return;

		if (player.Group != null)
			return;

		if (response == 0x00)
		{
			player.Out.SendMessage("Look them in the eye.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
		}
		else
		{
			//Check if we can add the quest!
			if (!SucciMid.GiveQuest(typeof (DailyHardcoreKillAPlayerLvl1MidQuest), player, 1))
				return;

			SucciMid.SayTo(player, "Hunt, or be hunted. Only one shall eat this night.");

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
				case -1:
					return "Your deeds are done for today.";
				case 1:
					return "Kill another player without dying. \n Life Taken: ("+ PlayerKilled +" | "+MAX_KillGoal+")";
				case 2:
					return "Return to Succi in Svasud Faste for your grim reward.";
			}
			return base.Description;
		}
	}

	public override void Notify(CoreEvent e, object sender, EventArgs args)
	{
		GamePlayer player = sender as GamePlayer;

		if (player?.IsDoingQuest(typeof(DailyHardcoreKillAPlayerLvl1MidQuest)) == null)
			return;

		if (player.Group != null && Step == 1)
		{
			FailQuest();
			return;
		}
			

		if (sender != m_questPlayer)
			return;

		if (e == GameLivingEvent.Dying && Step == 1)
		{
			FailQuest();
			return;
		}

		if (e != GameLivingEvent.EnemyKilled || Step != 1) return;
		EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
		if (!(player.GetConLevel(gArgs.Target) > -3) || gArgs.Target is not GamePlayer enemyPlayer ||
		    enemyPlayer.Realm == 0 || player.Realm == enemyPlayer.Realm) return;
		PlayerKilled = 1;
		player.Out.SendMessage("[Hardcore] Enemy Killed: (" + PlayerKilled + " | " + MAX_KillGoal + ")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
		player.Out.SendQuestUpdate(this);
		// FinishQuest or go back to npc
		Step = 2;

	}
	
	public override string QuestPropertyKey
	{
		get => "HardcorePlayerKillQuestMid";
		set { ; }
	}
	
	public override void LoadQuestParameters()
	{
		
	}

	public override void SaveQuestParameters()
	{
		
	}

	public override void FinishQuest()
	{
		int reward = ServerProperty.HARDCORE_RVR_REWARD;
		
		m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel)/2);
		m_questPlayer.AddMoney(MoneyMgr.GetMoney(0,0,m_questPlayer.Level*2,32,Util.Random(50)), "You receive {0} as a reward.");
		CoreRogMgr.GenerateReward(m_questPlayer, 250);
		PlayerKilled = 0;
		
		if (reward > 0)
		{
			m_questPlayer.Out.SendMessage($"You have been rewarded {reward} Realmpoints for finishing Hardcore Quest.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
			m_questPlayer.GainRealmPoints(reward, false);
			m_questPlayer.Out.SendUpdatePlayer();
		}
		base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		
	}

	private void FailQuest()
	{
		m_questPlayer.Out.SendMessage(questTitle + " failed.", EChatType.CT_ScreenCenter_And_CT_System, EChatLoc.CL_SystemWindow);
		PlayerKilled = 0;
		Step = -1;

		if (m_questPlayer.QuestList.TryRemove(this, out byte value))
			m_questPlayer.AvailableQuestIndexes.Enqueue(value);

		m_questPlayer.AddFinishedQuest(this);
		m_questPlayer.Out.SendQuestListUpdate();
	}
}