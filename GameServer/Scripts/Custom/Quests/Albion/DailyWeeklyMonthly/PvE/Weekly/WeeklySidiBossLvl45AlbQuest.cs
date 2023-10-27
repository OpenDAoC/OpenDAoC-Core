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

public class WeeklySidiBossLvl45AlbQuest : Quests.WeeklyQuest
{
    /// <summary>
    /// Defines a logger for this class.
    /// </summary>
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private const string questTitle = "[Weekly] Harder Adversaries";
    private const int minimumLevel = 45;
    private const int maximumLevel = 50;

    // Kill Goal
    private int _deadSidiBossMob = 0;
    private const int MAX_KILLGOAL = 3;

    private static GameNpc James = null; // Start NPC


    // Constructors
    public WeeklySidiBossLvl45AlbQuest() : base()
    {
    }

    public WeeklySidiBossLvl45AlbQuest(GamePlayer questingPlayer) : base(questingPlayer, 1)
    {
    }

    public WeeklySidiBossLvl45AlbQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
    {
    }

    public WeeklySidiBossLvl45AlbQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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

        James.AddQuestToGive(typeof(WeeklySidiBossLvl45AlbQuest));

        if (log.IsInfoEnabled)
            log.Info("Quest \"" + questTitle + "\" Alb initialized");
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

        James.RemoveQuestToGive(typeof(WeeklySidiBossLvl45AlbQuest));
    }

    protected static void TalkToJames(CoreEvent e, object sender, EventArgs args)
    {
        //We get the player from the event arguments and check if he qualifies		
        GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
        if (player == null)
            return;

        if (James.CanGiveQuest(typeof(WeeklySidiBossLvl45AlbQuest), player) <= 0)
            return;

        //We also check if the player is already doing the quest
        WeeklySidiBossLvl45AlbQuest quest = player.IsDoingQuest(typeof(WeeklySidiBossLvl45AlbQuest)) as WeeklySidiBossLvl45AlbQuest;

        if (e == GameObjectEvent.Interact)
        {
            if (quest != null)
            {
                switch (quest.Step)
                {
                    case 1:
                        James.SayTo(player,
                            "Please, enter Caer Sidi and slay strong opponents. If you succeed come back for your reward.");
                        break;
                    case 2:
                        James.SayTo(player, "Hello " + player.Name + ", did you [succeed]?");
                        break;
                }
            }
            else
            {
                James.SayTo(player, "Hello " + player.Name + ", I am James. " +
                                     "An infiltrator has reported the forces in Caer Sidi are planning an attack. \n" +
                                     "We want to pre-empt them and [end their plotting] before they have the chance. Care to help?");
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
                    case "end their plotting":
                        player.Out.SendQuestSubscribeCommand(James,
                            QuestMgr.GetIDForQuestType(typeof(WeeklySidiBossLvl45AlbQuest)),
                            "Will you help James " + questTitle + "");
                        break;
                }
            }
            else
            {
                switch (wArgs.Text)
                {
                    case "succeed":
                        if (quest.Step == 2)
                        {
                            player.Out.SendMessage("Thank you for your contribution!", EChatType.CT_Chat,
                                EChatLoc.CL_PopupWindow);
                            quest.FinishQuest();
                        }

                        break;
                    case "abort":
                        player.Out.SendCustomDialog(
                            "Do you really want to abort this quest, \nall items gained during quest will be lost?",
                            new CustomDialogResponse(CheckPlayerAbortQuest));
                        break;
                }
            }
        }
    }

    public override bool CheckQuestQualification(GamePlayer player)
    {
        // if the player is already doing the quest his level is no longer of relevance
        if (player.IsDoingQuest(typeof(WeeklySidiBossLvl45AlbQuest)) != null)
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
        WeeklySidiBossLvl45AlbQuest quest = player.IsDoingQuest(typeof(WeeklySidiBossLvl45AlbQuest)) as WeeklySidiBossLvl45AlbQuest;

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

        if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(WeeklySidiBossLvl45AlbQuest)))
            return;

        if (e == GamePlayerEvent.AcceptQuest)
            CheckPlayerAcceptQuest(qargs.Player, 0x01);
        else if (e == GamePlayerEvent.DeclineQuest)
            CheckPlayerAcceptQuest(qargs.Player, 0x00);
    }

    private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
    {
        if (James.CanGiveQuest(typeof(WeeklySidiBossLvl45AlbQuest), player) <= 0)
            return;

        if (player.IsDoingQuest(typeof(WeeklySidiBossLvl45AlbQuest)) != null)
            return;

        if (response == 0x00)
        {
            player.Out.SendMessage("Thank you for helping Albion.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
        }
        else
        {
            //Check if we can add the quest!
            if (!James.GiveQuest(typeof(WeeklySidiBossLvl45AlbQuest), player, 1))
                return;

            James.SayTo(player, "Thank you " + player.Name + ", be an enrichment for our realm!");
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
                    return "Find a way to Caer Sidi and kill strong opponents. \nKilled: Bosses in Caer Sidi (" +
                           _deadSidiBossMob + " | "+ MAX_KILLGOAL +")";
                case 2:
                    return "Return to James in Caer Gothwaite for your Reward.";
            }

            return base.Description;
        }
    }

    public override void Notify(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = sender as GamePlayer;

        if (player == null || player.IsDoingQuest(typeof(WeeklySidiBossLvl45AlbQuest)) == null)
            return;

        if (sender != m_questPlayer)
            return;

        if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
        EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
            
        // check if a GameEpicBoss died + if its in Caer Sidi
        if (gArgs.Target.Realm != 0 || gArgs.Target is not GameEpicBoss ||
            gArgs.Target.CurrentRegionID != 60) return;
        _deadSidiBossMob++;
        player.Out.SendMessage(
            "[Weekly] Bosses killed in Caer Sidi: (" + _deadSidiBossMob + " | " + MAX_KILLGOAL + ")",
            EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
        player.Out.SendQuestUpdate(this);

        if (_deadSidiBossMob >= MAX_KILLGOAL)
        {
            Step = 2;
        }
    }

    public override string QuestPropertyKey
    {
        get => "SidiBossQuestAlb";
        set { ; }
    }

    public override void LoadQuestParameters()
    {
        _deadSidiBossMob = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
    }

    public override void SaveQuestParameters()
    {
        SetCustomProperty(QuestPropertyKey, _deadSidiBossMob.ToString());
    }

    public override void FinishQuest()
    {
        m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel));
        m_questPlayer.AddMoney(MoneyMgr.GetMoney(0, 0, m_questPlayer.Level * 5, 0, Util.Random(50)), "You receive {0} as a reward.");
        CoreRogMgr.GenerateReward(m_questPlayer, 1500);
        _deadSidiBossMob = 0;
        base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
    }
}