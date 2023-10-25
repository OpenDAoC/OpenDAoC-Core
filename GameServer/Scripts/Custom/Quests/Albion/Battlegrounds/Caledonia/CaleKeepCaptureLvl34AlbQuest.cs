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

public class CaleKeepCaptureLvl34AlbQuest : Quests.DailyQuest
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	private const string questTitle = "[Daily] Caledonia Conquerer";
	private const int minimumLevel = 34;
	private const int maximumLevel = 39;

	// Capture Goal
	private const int MAX_CAPTURED = 1;
	
	private static GameNpc PazzAlb = null; // Start NPC

	private int _isCaptured = 0;

	// Constructors
	public CaleKeepCaptureLvl34AlbQuest() : base()
	{
	}

	public CaleKeepCaptureLvl34AlbQuest(GamePlayer questingPlayer) : base(questingPlayer, 1)
	{
	}

	public CaleKeepCaptureLvl34AlbQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
	{
	}

	public CaleKeepCaptureLvl34AlbQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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
		GameNpc[] npcs = WorldMgr.GetNPCsByName("Pazz", ERealm.Albion);

		if (npcs.Length > 0)
			foreach (GameNpc npc in npcs)
			{
				if (npc.CurrentRegionID == 250 && npc.X == 37283 && npc.Y == 51881)
				{
					PazzAlb = npc;
					break;
				}
			}

		if (PazzAlb == null)
		{
			if (log.IsWarnEnabled)
				log.Warn("Could not find PazzAlb, creating it ...");
			PazzAlb = new GameNpc();
			PazzAlb.Model = 26;
			PazzAlb.Name = "Pazz";
			PazzAlb.GuildName = "Bone Collector";
			PazzAlb.Realm = ERealm.Albion;
			//Druim Ligen Location
			PazzAlb.CurrentRegionID = 250;
			PazzAlb.Size = 40;
			PazzAlb.Level = 59;
			PazzAlb.X = 37283;
			PazzAlb.Y = 51881;
			PazzAlb.Z = 3944;
			PazzAlb.Heading = 4090;
			PazzAlb.Flags |= ENpcFlags.PEACE;
			PazzAlb.AddToWorld();
			if (SAVE_INTO_DATABASE)
			{
				PazzAlb.SaveIntoDatabase();
			}
		}

		#endregion

		#region defineItems
		#endregion

		#region defineObject
		#endregion

		GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
		GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

		GameEventMgr.AddHandler(PazzAlb, GameObjectEvent.Interact, new CoreEventHandler(TalkToHaszan));
		GameEventMgr.AddHandler(PazzAlb, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToHaszan));

		/* Now we bring to Haszan the possibility to give this quest to players */
		PazzAlb.AddQuestToGive(typeof (CaleKeepCaptureLvl34AlbQuest));

		if (log.IsInfoEnabled)
			log.Info("Quest \"" + questTitle + "\" initialized");
	}

	[ScriptUnloadedEvent]
	public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
	{
		//if not loaded, don't worry
		if (PazzAlb == null)
			return;
		// remove handlers
		GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
		GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

		GameEventMgr.RemoveHandler(PazzAlb, GameObjectEvent.Interact, new CoreEventHandler(TalkToHaszan));
		GameEventMgr.RemoveHandler(PazzAlb, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToHaszan));

		/* Now we remove to Haszan the possibility to give this quest to players */
		PazzAlb.RemoveQuestToGive(typeof (CaleKeepCaptureLvl34AlbQuest));
	}

	private static void TalkToHaszan(CoreEvent e, object sender, EventArgs args)
	{
		//We get the player from the event arguments and check if he qualifies		
		GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
		if (player == null)
			return;

		if(PazzAlb.CanGiveQuest(typeof (CaleKeepCaptureLvl34AlbQuest), player)  <= 0)
			return;

		//We also check if the player is already doing the quest
		CaleKeepCaptureLvl34AlbQuest quest = player.IsDoingQuest(typeof (CaleKeepCaptureLvl34AlbQuest)) as CaleKeepCaptureLvl34AlbQuest;

		if (e == GameObjectEvent.Interact)
		{
			if (quest != null)
			{
				switch (quest.Step)
				{
					case 1:
						PazzAlb.SayTo(player, "Find an enemy occupied keep and capture it. If you succeed come back for your reward.");
						break;
					case 2:
						PazzAlb.SayTo(player, "Hello " + player.Name + ", did you [capture] a keep?");
						break;
				}
			}
			else
			{
				PazzAlb.SayTo(player, "Look "+ player.Name +", I'll cut to the chase. " +
				                      "We need the central keep back because I left some... contraband in the basement that I'd really like to reclaim before its found by the guards. Can you [help a skeleton] out?");
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
					case "help a skeleton":
						player.Out.SendQuestSubscribeCommand(PazzAlb, QuestMgr.GetIDForQuestType(typeof(CaleKeepCaptureLvl34AlbQuest)), "Will you help Pazz with "+questTitle+"");
						break;
				}
			}
			else
			{
				switch (wArgs.Text)
				{
					case "capture":
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
		if (player.IsDoingQuest(typeof (CaleKeepCaptureLvl34AlbQuest)) != null)
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
		CaleKeepCaptureLvl34AlbQuest quest = player.IsDoingQuest(typeof (CaleKeepCaptureLvl34AlbQuest)) as CaleKeepCaptureLvl34AlbQuest;

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

	protected static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
	{
		QuestEventArgs qargs = args as QuestEventArgs;
		if (qargs == null)
			return;

		if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(CaleKeepCaptureLvl34AlbQuest)))
			return;

		if (e == GamePlayerEvent.AcceptQuest)
			CheckPlayerAcceptQuest(qargs.Player, 0x01);
		else if (e == GamePlayerEvent.DeclineQuest)
			CheckPlayerAcceptQuest(qargs.Player, 0x00);
	}

	private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
	{
		if(PazzAlb.CanGiveQuest(typeof (CaleKeepCaptureLvl34AlbQuest), player)  <= 0)
			return;

		if (player.IsDoingQuest(typeof (CaleKeepCaptureLvl34AlbQuest)) != null)
			return;

		if (response == 0x00)
		{
			player.Out.SendMessage("Thank you for helping me out.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
		}
		else
		{
			//Check if we can add the quest!
			if (!PazzAlb.GiveQuest(typeof (CaleKeepCaptureLvl34AlbQuest), player, 1))
				return;

			PazzAlb.SayTo(player, "Thank you "+player.Name+", be an enrichment for our realm!");

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
					return "Go to the battlefield and conquer a keep. \nCaptured: Keep ("+ _isCaptured +" | 1)";
				case 2:
					return "Return to Pazz in Caledonia Portal Keep for your reward.";
			}
			return base.Description;
		}
	}

	public override void Notify(CoreEvent e, object sender, EventArgs args)
	{
		GamePlayer player = sender as GamePlayer;

		if (player?.IsDoingQuest(typeof(CaleKeepCaptureLvl34AlbQuest)) == null)
			return;
		
		if (sender != m_questPlayer)
			return;

		if (Step != 1 || e != GamePlayerEvent.CapturedKeepsChanged) return;
		_isCaptured = 1;
		player.Out.SendMessage("[Daily] Captured Keep: ("+_isCaptured+" | "+MAX_CAPTURED+")", EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
		player.Out.SendQuestUpdate(this);
				
		if (_isCaptured >= MAX_CAPTURED)
		{
			// FinishQuest or go back to Dean
			Step = 2;
		}

	}
	
	public override string QuestPropertyKey
	{
		get => "CaleKeepCaptureAlb";
		set { ; }
	}
	
	public override void LoadQuestParameters()
	{
		
	}

	public override void SaveQuestParameters()
	{
		
	}

	public override void AbortQuest()
	{
		base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
	}

	public override void FinishQuest()
	{
		if (m_questPlayer.Inventory.IsSlotsFree(1, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack))
		{
			m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel) / 2);
			m_questPlayer.AddMoney(MoneyMgr.GetMoney(0, 0, m_questPlayer.Level * 2, 0, Util.Random(50)),
				"You receive {0} as a reward.");
			CoreRogMgr.GenerateBattlegroundToken(m_questPlayer, 1);
			_isCaptured = 0;
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
		}
		else
		{
			m_questPlayer.Out.SendMessage("Clear one slot of your inventory for your reward", EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}
	}
}