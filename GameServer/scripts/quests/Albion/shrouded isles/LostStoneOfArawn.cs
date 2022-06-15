using System;
using System.Net;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.GS.Quests.Albion;

public class LostStoneofArawn : BaseQuest
{
    private const string questTitle = "Lost Stone of Arawn";
    private const int minimumLevel = 48;
    private const int maximumLevel = 50;

    /// <summary>
    ///     Defines a logger for this class.
    /// </summary>
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    private static GameNPC Honaytrt; // Start NPC Honayt'rt
    private static GameNPC Nchever; // N'chever
    private static GameNPC Ohonat; // O'honat
    private static GameNPC Nyaegha; // Nyaegha

    private static readonly GameLocation demonLocation = new("Nyaegha", 51, 348381, 479838, 3320);

    private static AbstractArea demonArea;

    private static ItemTemplate ancient_copper_necklace;
    private static ItemTemplate scroll_wearyall_loststone;
    private static ItemTemplate lost_stone_of_arawn;

    // Constructors
    public LostStoneofArawn()
    {
    }

    public LostStoneofArawn(GamePlayer questingPlayer) : base(questingPlayer)
    {
    }

    public LostStoneofArawn(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
    {
    }

    public LostStoneofArawn(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
    {
    }

    public override int Level =>
        // Quest Level
        minimumLevel;

    //Set quest name
    public override string Name => questTitle;

    // Define Steps
    public override string Description
    {
        get
        {
            switch (Step)
            {
                case 1:
                    return "Speak to Honayt\'rt in Wearyall Village.";
                case 2:
                    return "Speak to N\'chever in Wearyall Village.";
                case 3:
                    return "Speak to O\'honat in Caer Diogel.";
                case 4:
                    return "Leave Caer Diogel and head out of town to the West. As you reach the coast, turn North. " +
                           "The demon that we need to kill usually roams the Plains of Gwyddneau.";
                case 5:
                    return "Go back to Caer Diogel and give O'honat the Stone.";
                case 6:
                    return "Read the speech to see who it is addressed to, and return it for your reward.";
            }

            return base.Description;
        }
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
    {
        if (!Properties.LOAD_QUESTS)
            return;


        #region defineNPCs

        var npcs = WorldMgr.GetNPCsByName("Honayt\'rt", eRealm.Albion);

        if (npcs.Length > 0)
            foreach (var npc in npcs)
                if (npc.CurrentRegionID == 51 && npc.X == 435217 && npc.Y == 495273)
                {
                    Honaytrt = npc;
                    break;
                }

        if (Honaytrt == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Honaytrt, creating it ...");
            Honaytrt = new GameNPC();
            Honaytrt.Model = 759;
            Honaytrt.Name = "Honayt\'rt";
            Honaytrt.GuildName = "";
            Honaytrt.Realm = eRealm.Albion;
            Honaytrt.CurrentRegionID = 51;
            Honaytrt.LoadEquipmentTemplateFromDatabase("097fe8c1-7d7e-4b82-a7ca-04a6e192afc1");
            Honaytrt.Size = 51;
            Honaytrt.Level = 50;
            Honaytrt.X = 435217;
            Honaytrt.Y = 495273;
            Honaytrt.Z = 3134;
            Honaytrt.Heading = 3270;
            Honaytrt.AddToWorld();
            if (SAVE_INTO_DATABASE) Honaytrt.SaveIntoDatabase();
        }

        npcs = WorldMgr.GetNPCsByName("N\'chever", eRealm.Albion);

        if (npcs.Length > 0)
            foreach (var npc in npcs)
                if (npc.CurrentRegionID == 51 && npc.X == 30763 && npc.Y == 29908)
                {
                    Nchever = npc;
                    break;
                }

        if (Nchever == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Nchever , creating it ...");
            Nchever = new GameNPC();
            Nchever.Model = 752;
            Nchever.Name = "N\'chever";
            Nchever.GuildName = "";
            Nchever.Realm = eRealm.Albion;
            Nchever.CurrentRegionID = 51;
            Nchever.LoadEquipmentTemplateFromDatabase("a2639e94-f032-4041-ad67-15dfeaf004d2");
            Nchever.Size = 51;
            Nchever.Level = 52;
            Nchever.X = 435972;
            Nchever.Y = 492370;
            Nchever.Z = 3087;
            Nchever.Heading = 594;
            Nchever.AddToWorld();
            if (SAVE_INTO_DATABASE) Nchever.SaveIntoDatabase();
        }
        // end npc

        npcs = WorldMgr.GetNPCsByName("O\'honat", eRealm.Albion);

        if (npcs.Length > 0)
            foreach (var npc in npcs)
                if (npc.CurrentRegionID == 51 && npc.X == 404696 && npc.Y == 503469)
                {
                    Ohonat = npc;
                    break;
                }

        if (Ohonat == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Ohonat , creating it ...");
            Ohonat = new GameNPC();
            Ohonat.LoadEquipmentTemplateFromDatabase("a58ef747-80e0-4cda-9052-15711ea0f4f7");
            Ohonat.Model = 761;
            Ohonat.Name = "O\'honat";
            Ohonat.GuildName = "";
            Ohonat.Realm = eRealm.Albion;
            Ohonat.CurrentRegionID = 51;
            Ohonat.Size = 52;
            Ohonat.Level = 50;
            Ohonat.X = 404696;
            Ohonat.Y = 503469;
            Ohonat.Z = 5192;
            Ohonat.Heading = 1037;
            Ohonat.VisibleActiveWeaponSlots = 51;
            Ohonat.Flags ^= GameNPC.eFlags.PEACE;
            Ohonat.MaxSpeedBase = 200;
            Ohonat.AddToWorld();
            if (SAVE_INTO_DATABASE) Ohonat.SaveIntoDatabase();
        }
        // end npc

        #endregion

        #region defineItems

        ancient_copper_necklace = GameServer.Database.FindObjectByKey<ItemTemplate>("ancient_copper_necklace");
        if (ancient_copper_necklace == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Ancient Copper Necklace, creating it ...");
            ancient_copper_necklace = new ItemTemplate();
            ancient_copper_necklace.Id_nb = "ancient_copper_necklace";
            ancient_copper_necklace.Name = "Ancient Copper Necklace";
            ancient_copper_necklace.Level = 51;
            ancient_copper_necklace.Durability = 50000;
            ancient_copper_necklace.MaxDurability = 50000;
            ancient_copper_necklace.Condition = 50000;
            ancient_copper_necklace.MaxCondition = 50000;
            ancient_copper_necklace.Item_Type = 29;
            ancient_copper_necklace.Object_Type = (int) eObjectType.Magical;
            ancient_copper_necklace.Model = 101;
            ancient_copper_necklace.Bonus = 35;
            ancient_copper_necklace.IsDropable = true;
            ancient_copper_necklace.IsTradable = true;
            ancient_copper_necklace.IsIndestructible = false;
            ancient_copper_necklace.IsPickable = true;
            ancient_copper_necklace.Bonus1 = 10;
            ancient_copper_necklace.Bonus2 = 10;
            ancient_copper_necklace.Bonus3 = 10;
            ancient_copper_necklace.Bonus4 = 10;
            ancient_copper_necklace.Bonus1Type = 11;
            ancient_copper_necklace.Bonus2Type = 19;
            ancient_copper_necklace.Bonus3Type = 18;
            ancient_copper_necklace.Bonus4Type = 13;
            ancient_copper_necklace.Price = 0;
            ancient_copper_necklace.Realm = (int) eRealm.Albion;
            ancient_copper_necklace.DPS_AF = 0;
            ancient_copper_necklace.SPD_ABS = 0;
            ancient_copper_necklace.Hand = 0;
            ancient_copper_necklace.Type_Damage = 0;
            ancient_copper_necklace.Quality = 100;
            ancient_copper_necklace.Weight = 10;
            ancient_copper_necklace.LevelRequirement = 50;
            ancient_copper_necklace.BonusLevel = 30;
            ancient_copper_necklace.Description = "";
            if (SAVE_INTO_DATABASE) GameServer.Database.AddObject(ancient_copper_necklace);
        }

        scroll_wearyall_loststone = GameServer.Database.FindObjectByKey<ItemTemplate>("scroll_wearyall_loststone");
        if (scroll_wearyall_loststone == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Victory Speech for Albion, creating it ...");
            scroll_wearyall_loststone = new ItemTemplate();
            scroll_wearyall_loststone.Id_nb = "scroll_wearyall_loststone";
            scroll_wearyall_loststone.Name = "Victory Speech for Albion";
            scroll_wearyall_loststone.Level = 5;
            scroll_wearyall_loststone.Item_Type = 0;
            scroll_wearyall_loststone.Model = 498;
            scroll_wearyall_loststone.IsDropable = true;
            scroll_wearyall_loststone.IsTradable = false;
            scroll_wearyall_loststone.IsIndestructible = true;
            scroll_wearyall_loststone.IsPickable = true;
            scroll_wearyall_loststone.DPS_AF = 0;
            scroll_wearyall_loststone.SPD_ABS = 0;
            scroll_wearyall_loststone.Object_Type = 0;
            scroll_wearyall_loststone.Hand = 0;
            scroll_wearyall_loststone.Type_Damage = 0;
            scroll_wearyall_loststone.Quality = 100;
            scroll_wearyall_loststone.Weight = 1;
            scroll_wearyall_loststone.Description =
                "Bring this Speech to Honayt\'rt in Wearyall Village. She will be interested in reading this scroll.";
            if (SAVE_INTO_DATABASE) GameServer.Database.AddObject(scroll_wearyall_loststone);
        }

        lost_stone_of_arawn = GameServer.Database.FindObjectByKey<ItemTemplate>("lost_stone_of_arawn");
        if (lost_stone_of_arawn == null)
        {
            if (log.IsWarnEnabled)
                log.Warn("Could not find Lost Stone of Arawn, creating it ...");
            lost_stone_of_arawn = new ItemTemplate();
            lost_stone_of_arawn.Id_nb = "lost_stone_of_arawn";
            lost_stone_of_arawn.Name = "Lost Stone of Arawn";
            lost_stone_of_arawn.Level = 55;
            lost_stone_of_arawn.Item_Type = 0;
            lost_stone_of_arawn.Model = 110;
            lost_stone_of_arawn.IsDropable = true;
            lost_stone_of_arawn.IsTradable = false;
            lost_stone_of_arawn.IsIndestructible = true;
            lost_stone_of_arawn.IsPickable = true;
            lost_stone_of_arawn.DPS_AF = 0;
            lost_stone_of_arawn.SPD_ABS = 0;
            lost_stone_of_arawn.Object_Type = 0;
            lost_stone_of_arawn.Hand = 0;
            lost_stone_of_arawn.Type_Damage = 0;
            lost_stone_of_arawn.Quality = 100;
            lost_stone_of_arawn.Weight = 1;
            lost_stone_of_arawn.Description = "A stone of infinite power.";
            if (SAVE_INTO_DATABASE) GameServer.Database.AddObject(lost_stone_of_arawn);
        }
        //Item Descriptions End

        #endregion

        const int radius = 1500;
        var region = WorldMgr.GetRegion(demonLocation.RegionID);
        demonArea = new Area.Circle("demonic patch", demonLocation.X, demonLocation.Y, demonLocation.Z,
            radius);
        demonArea.CanBroadcast = false;
        demonArea.DisplayMessage = false;
        region.AddArea(demonArea);
        demonArea.RegisterPlayerEnter(PlayerEnterDemonArea);

        GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, SubscribeQuest);
        GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, SubscribeQuest);

        GameEventMgr.AddHandler(Honaytrt, GameObjectEvent.Interact, TalkToHonaytrt);
        GameEventMgr.AddHandler(Honaytrt, GameLivingEvent.WhisperReceive, TalkToHonaytrt);

        GameEventMgr.AddHandler(Nchever, GameObjectEvent.Interact, TalkToNchever);
        GameEventMgr.AddHandler(Nchever, GameLivingEvent.WhisperReceive, TalkToNchever);

        GameEventMgr.AddHandler(Ohonat, GameObjectEvent.Interact, TalkToOhonat);
        GameEventMgr.AddHandler(Ohonat, GameLivingEvent.WhisperReceive, TalkToOhonat);

        Honaytrt.AddQuestToGive(typeof(LostStoneofArawn));

        if (log.IsInfoEnabled)
            log.Info("Quest \"" + questTitle + "\" initialized");
    }

    [ScriptUnloadedEvent]
    public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
    {
        //if not loaded, don't worry
        if (Honaytrt == null)
            return;
        // remove handlers
        GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, SubscribeQuest);
        GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, SubscribeQuest);

        demonArea.UnRegisterPlayerEnter(PlayerEnterDemonArea);
        WorldMgr.GetRegion(demonLocation.RegionID).RemoveArea(demonArea);

        GameEventMgr.RemoveHandler(Honaytrt, GameObjectEvent.Interact, TalkToHonaytrt);
        GameEventMgr.RemoveHandler(Honaytrt, GameLivingEvent.WhisperReceive, TalkToHonaytrt);

        GameEventMgr.RemoveHandler(Nchever, GameObjectEvent.Interact, TalkToNchever);
        GameEventMgr.RemoveHandler(Nchever, GameLivingEvent.WhisperReceive, TalkToNchever);

        GameEventMgr.RemoveHandler(Ohonat, GameObjectEvent.Interact, TalkToOhonat);
        GameEventMgr.RemoveHandler(Ohonat, GameLivingEvent.WhisperReceive, TalkToOhonat);

        /* Now we remove to Honaytrt the possibility to give this quest to players */
        Honaytrt.RemoveQuestToGive(typeof(LostStoneofArawn));
    }

    protected virtual void CreateNyaegha(GamePlayer player)
    {
        Nyaegha = new GameNPC();
        Nyaegha.LoadEquipmentTemplateFromDatabase("Nyaegha");
        Nyaegha.Model = 605;
        Nyaegha.Name = "Nyaegha";
        Nyaegha.GuildName = "";
        Nyaegha.Realm = eRealm.None;
        Nyaegha.Race = 2001;
        Nyaegha.BodyType = (ushort) NpcTemplateMgr.eBodyType.Demon;
        Nyaegha.CurrentRegionID = 51;
        Nyaegha.Size = 150;
        Nyaegha.Level = 65;
        Nyaegha.ScalingFactor = 60;
        Nyaegha.X = 348381;
        Nyaegha.Y = 479838;
        Nyaegha.Z = 3320;
        Nyaegha.VisibleActiveWeaponSlots = 34;
        Nyaegha.MaxSpeedBase = 250;
        Nyaegha.AddToWorld();

        var brain = new StandardMobBrain();
        brain.AggroLevel = 200;
        brain.AggroRange = 500;
        Nyaegha.SetOwnBrain(brain);

        Nyaegha.AddToWorld();

        Nyaegha.StartAttack(player);
        
        GameEventMgr.AddHandler(Nyaegha, GameLivingEvent.Dying, NyaeghaDying);
    }
    private void NyaeghaDying(DOLEvent e, object sender, EventArgs arguments)
    {
        var args = (DyingEventArgs) arguments;
        
        var player = args.Killer as GamePlayer;
        
        if (player == null)
            return;
        
        if (player.Group != null)
        {
            foreach (var gpl in player.Group.GetPlayersInTheGroup())
            {
                AdvanceAfterKill(gpl);
            }
        }
        else
        {
            AdvanceAfterKill(player);
        }
        
        GameEventMgr.RemoveHandler(Nyaegha, GameLivingEvent.Dying, NyaeghaDying);
        Nyaegha.Delete();
    }
    private static void AdvanceAfterKill(GamePlayer player)
    {
        var quest = player.IsDoingQuest(typeof(LostStoneofArawn)) as LostStoneofArawn;
        if (quest is not {Step: 4}) return;
        if (!player.Inventory.IsSlotsFree(1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
            player.Out.SendMessage(
                "You dont have enough room for " + lost_stone_of_arawn.Name + " and drops on the ground.",
                eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        GiveItem(player, lost_stone_of_arawn);
        quest.Step = 5;
    }

    private static void PlayerEnterDemonArea(DOLEvent e, object sender, EventArgs args)
    {
        var aargs = args as AreaEventArgs;
        var player = aargs?.GameObject as GamePlayer;

        if (player == null)
            return;

        var quest = player.IsDoingQuest(typeof(LostStoneofArawn)) as LostStoneofArawn;

        if (quest is not {Step: 4}) return;

        if (player.Group != null)
            if (player.Group.Leader != player)
                return;

        var existingCopy = WorldMgr.GetNPCsByName("Nyaegha", eRealm.None);

        if (existingCopy.Length > 0) return;

        // player near demon           
        SendSystemMessage(player,
            "This is Marw Gwlad. The ground beneath your feet is cracked and burned, and the air holds a faint scent of brimstone.");
        player.Out.SendMessage("Nyaegha ambushes you!", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
        quest.CreateNyaegha(player);
    }

    private static void TalkToHonaytrt(DOLEvent e, object sender, EventArgs args)
    {
        //We get the player from the event arguments and check if he qualifies		
        var player = ((SourceEventArgs) args).Source as GamePlayer;
        if (player == null)
            return;

        if (Honaytrt.CanGiveQuest(typeof(LostStoneofArawn), player) <= 0)
            return;

        //We also check if the player is already doing the quest
        var quest = player.IsDoingQuest(typeof(LostStoneofArawn)) as LostStoneofArawn;

        if (e == GameObjectEvent.Interact)
        {
            if (quest != null)
                switch (quest.Step)
                {
                    case 1:
                        Honaytrt.SayTo(player,
                            "Thanks for your help! N\'chever, O\'honat and I have been looking for this stone a long time.\n" +
                            "Speak with N\'chever in Wearyall Village, he will be able to tell you more about the [Stone of Arawn].");
                        break;
                    case 2:
                        Honaytrt.SayTo(player,
                            "Hey " + player.Name + ", have you visited N\'chever yet? You can find him near Wearyall.");
                        break;
                    case 3:
                        Honaytrt.SayTo(player,
                            "Greetings, I heard you are on your way to O\'honat, I'm sure she will help you find what we are searching for.");
                        break;
                    case 4:
                        Honaytrt.SayTo(player,
                            "Wow, O\'honat really found something eh?\nI knew she could be counted on!");
                        break;
                    case 5:
                        Honaytrt.SayTo(player,
                            "Oh dear, have you really found the stone?\nPlease bring it to O\'honat first, she has to see it!");
                        break;
                    case 6:
                        Honaytrt.SayTo(player, "I can't really explain how happy I am, thanks for your help " +
                                               player.CharacterClass.Name + "!\n" +
                                               "Here's your [reward].");
                        break;
                }
            else
                Honaytrt.SayTo(player, "Hello " + player.Name +
                                       ", we live in dark times and only finding the lost Stone of Arawn can save us.\n" +
                                       "I've been searching for it for several years with no luck, could you maybe [help me] retrieve the stone?");
        }
        // The player whispered to the NPC
        else if (e == GameLivingEvent.WhisperReceive)
        {
            var wArgs = (WhisperReceiveEventArgs) args;
            if (quest == null)
                switch (wArgs.Text)
                {
                    case "help me":
                        player.Out.SendQuestSubscribeCommand(Honaytrt,
                            QuestMgr.GetIDForQuestType(typeof(LostStoneofArawn)),
                            "Will you help Honayt\'rt retrieve the [Lost Stone of Arawn]?");
                        break;
                }
            else
                switch (wArgs.Text)
                {
                    case "Stone of Arawn":
                        if (quest.Step == 1)
                        {
                            quest.Step = 2;
                            Honaytrt.SayTo(player,
                                "You can find N\'chever North of Wearyall Village, go and speak to him.");
                        }

                        break;
                    case "reward":
                        if (quest.Step == 6) quest.FinishQuest();
                        break;
                    case "abort":
                        player.Out.SendCustomDialog(
                            "Do you really want to abort this quest, \nall items gained during quest will be lost?",
                            CheckPlayerAbortQuest);
                        break;
                }
        }
        else if (e == GameObjectEvent.ReceiveItem)
        {
            var rArgs = (ReceiveItemEventArgs) args;
            if (quest != null)
            {
                /*if (rArgs.Item.Id_nb == .Id_nb)
                {
                    Honaytrt.SayTo(player, "Thank you "+ player.Name +".\n");
                    //quest.Step = 3;
                }*/
            }
        }
    }

    private static void TalkToNchever(DOLEvent e, object sender, EventArgs args)
    {
        //We get the player from the event arguments and check if he qualifies		
        var player = ((SourceEventArgs) args).Source as GamePlayer;
        if (player == null)
            return;

        //We also check if the player is already doing the quest
        var quest = player.IsDoingQuest(typeof(LostStoneofArawn)) as LostStoneofArawn;

        if (e == GameObjectEvent.Interact)
        {
            if (quest != null)
                switch (quest.Step)
                {
                    case 1:
                        Nchever.SayTo(player, "Hey " + player.Name +
                                              ", welcome to Wearyall Village, if you need some rest you can visit our stables.\n" +
                                              "There, you'll also find my dear friend Honayt\'rt, I feel you will like each other!");
                        break;
                    case 2:
                        Nchever.SayTo(player,
                            "Greetings, I see you spoke with Honayt\'rt about our mission already. We are searching for a [stone], do you want to help us?");
                        break;
                    case 3:
                        Nchever.SayTo(player,
                            "Hey " + player.CharacterClass.Name +
                            ", have you visited O\'honat yet? You can find her near Caer Diogel's ramparts.");
                        break;
                    case 4:
                        Nchever.SayTo(player, "Unbelievable, O\'honat really found something?\nThat's great!");
                        break;
                    case 5:
                        Nchever.SayTo(player,
                            "Please bring this stone to O\'honat, she will know what we need to do next.");
                        break;
                    case 6:
                        Nchever.SayTo(player,
                            "Thanks for showing me the Stone, now bring it to Honayt\'rt at the stables, she will reward you.");
                        break;
                }
            else
                Nchever.SayTo(player, "Greetings, isn\'t it a perfect day?");
        }
        // The player whispered to the NPC
        else if (e == GameLivingEvent.WhisperReceive)
        {
            var wArgs = (WhisperReceiveEventArgs) args;
            if (quest == null)
                switch (wArgs.Text)
                {
                }
            else
                switch (wArgs.Text)
                {
                    case "stone":
                        if (quest.Step == 2)
                            Nchever.SayTo(player,
                                "Visit O\'honat in Caer Diogel and ask her about the [Lost Stone of Arawn], I've been told she usually can be found near the ramparts.");
                        break;
                    case "Lost Stone of Arawn":
                        if (quest.Step == 2)
                        {
                            quest.Step = 3;
                            Nchever.SayTo(player, "Visit O\'honat in Caer Diogel!");
                        }

                        break;
                }
        }
    }

    private static void TalkToOhonat(DOLEvent e, object sender, EventArgs args)
    {
        //We get the player from the event arguments and check if he qualifies		
        var player = ((SourceEventArgs) args).Source as GamePlayer;
        if (player == null)
            return;

        //We also check if the player is already doing the quest
        var quest = player.IsDoingQuest(typeof(LostStoneofArawn)) as LostStoneofArawn;

        if (e == GameObjectEvent.Interact)
        {
            if (quest != null)
                switch (quest.Step)
                {
                    case 1:
                        Ohonat.SayTo(player, "Hello Adventurer, I am " + Ohonat.Name +
                                             "! Have visited Wearyall Village?\n" +
                                             "I have some friends there, Honayt\'rt and N\'chever, feel free to speak with them.");
                        break;
                    case 2:
                        Ohonat.SayTo(player,
                            "Hey, have you visited Honayt\'rt or N\'chever yet? They are really nice people.");
                        break;
                    case 3:
                        Ohonat.SayTo(player,
                            "Did N\'chever send you?\nYeah we are on a mission to find the lost Stone of Arawn. " +
                            "I heard of a demon who likes to torture animals and other creatures growing stronger in [Gwyddneau], " +
                            "we have to do something immediately or it will be too late for Albion!");
                        break;
                    case 4:
                        Ohonat.SayTo(player,
                            "Leave Caer Diogel and head out of town to the West. As you reach the coast, turn North. " +
                            "The demon that we need to kill usually roams the Plains of Gwyddneau.\n" +
                            "Kill the demon and bring me the stone!");
                        break;
                    case 5:
                        Ohonat.SayTo(player,
                            "Hey " + player.Name +
                            ", you are the hero we needed. I really thought it would have been [impossible].");
                        break;
                    case 6:
                        Ohonat.SayTo(player, "I know Honayt\'rt will be very happy. Bring her the speech!");
                        break;
                }
            else
                Ohonat.SayTo(player,
                    "Greetings Adventurer, feel free to buy something in our merchant house, if you need anything.");
        }
        // The player whispered to the NPC
        else if (e == GameLivingEvent.WhisperReceive)
        {
            var wArgs = (WhisperReceiveEventArgs) args;
            if (quest == null)
                switch (wArgs.Text)
                {
                }
            else
                switch (wArgs.Text)
                {
                    case "Gwyddneau":
                        if (quest.Step == 3)
                        {
                            quest.Step = 4;
                            Ohonat.SayTo(player,
                                "Leave Caer Diogel and head out of town to the West. As you reach the coast, turn North. " +
                                "The demon that we need to kill usually roams the Plains of Gwyddneau.\n" +
                                "Kill the demon and bring me the stone!");
                        }

                        break;
                    case "impossible":
                        if (quest.Step == 5)
                        {
                            Ohonat.SayTo(player,
                                $"Thanks {player.Name}. Now take this scroll and bring it to Honayt\'rt, she needs to read it as soon as possible!\n[Farewell], hero of Albion!");
                            Ohonat.Emote(eEmote.Cheer);
                        }

                        break;
                    case "Farewell":
                        if (quest.Step == 5 && player.Inventory.IsSlotsFree(1, eInventorySlot.FirstBackpack,
                                eInventorySlot.LastBackpack))
                        {
                            GiveItem(player, scroll_wearyall_loststone);
                            RemoveItem(player, lost_stone_of_arawn);
                            player.Out.SendSpellEffectAnimation(Ohonat, player, 4310, 0, false, 1);
                            new ECSGameTimer(player, timer => TeleportToWearyall(timer, player), 3000);
                            quest.Step = 6;
                            Ohonat.SayTo(player, "I know Honayt\'rt will be very happy. Bring her the speech!");
                        }

                        break;
                }
        }
        else if (e == GameObjectEvent.ReceiveItem)
        {
            var rArgs = (ReceiveItemEventArgs) args;
            if (quest != null)
                if (rArgs.Item.Id_nb == lost_stone_of_arawn.Id_nb)
                {
                    Ohonat.SayTo(player,
                        $"Thanks {player.Name}. Now take this scroll and bring it to Honayt\'rt, she needs to read it as soon as possible!\n[Farewell], hero of Albion!");
                    Ohonat.Emote(eEmote.Cheer);
                }
        }
    }

    private static int TeleportToWearyall(ECSGameTimer timer, GamePlayer player)
    {
        //teleport to wearyall village
        player.MoveTo(51, 435868, 493994, 3088, 3587);
        return 0;
    }

    public override bool CheckQuestQualification(GamePlayer player)
    {
        // if the player is already doing the quest his level is no longer of relevance
        if (player.IsDoingQuest(typeof(LostStoneofArawn)) != null)
            return true;

        if (player.Level < minimumLevel || player.Level > maximumLevel)
            return false;

        return true;
    }

    private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
    {
        var quest = player.IsDoingQuest(typeof(LostStoneofArawn)) as LostStoneofArawn;

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
        var qargs = args as QuestEventArgs;
        if (qargs == null)
            return;

        if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(LostStoneofArawn)))
            return;

        if (e == GamePlayerEvent.AcceptQuest)
            CheckPlayerAcceptQuest(qargs.Player, 0x01);
        else if (e == GamePlayerEvent.DeclineQuest)
            CheckPlayerAcceptQuest(qargs.Player, 0x00);
    }

    private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
    {
        if (Honaytrt.CanGiveQuest(typeof(LostStoneofArawn), player) <= 0)
            return;

        if (player.IsDoingQuest(typeof(LostStoneofArawn)) != null)
            return;

        if (response == 0x00)
        {
            player.Out.SendMessage("Come back if you are ready to help us in our mission.", eChatType.CT_Say,
                eChatLoc.CL_PopupWindow);
        }
        else
        {
            //Check if we can add the quest!
            if (!Honaytrt.GiveQuest(typeof(LostStoneofArawn), player, 1))
                return;

            Honaytrt.SayTo(player, "Thank you, lets talk more about the stone!");
            Honaytrt.SayTo(player,
                "N\'chever, O\'honat and I have been looking for this stone a long time.\n" +
                "Speak with N\'chever in Wearyall Village, he will be able to tell you more about the [Stone of Arawn].");
        }
    }
    public override void AbortQuest()
    {
        base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
        RemoveItem(m_questPlayer, lost_stone_of_arawn);
        RemoveItem(m_questPlayer, scroll_wearyall_loststone);
    }

    public override void FinishQuest()
    {
        if (m_questPlayer.Inventory.IsSlotsFree(1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
        {
            if (m_questPlayer.Level >= 49)
                m_questPlayer.GainExperience(eXPSource.Quest,
                    (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel) / 3, false);
            else
                m_questPlayer.GainExperience(eXPSource.Quest,
                    (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel) / 2, false);
            RemoveItem(m_questPlayer, scroll_wearyall_loststone);
            GiveItem(m_questPlayer, ancient_copper_necklace);
            m_questPlayer.AddMoney(Money.GetMoney(0, 0, 121, 41, Util.Random(50)), "You receive {0} as a reward.");

            base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
        }
        else
        {
            m_questPlayer.Out.SendMessage("You do not have enough free space in your inventory!",
                eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }
    }
}