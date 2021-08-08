using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;
using System;
using System.Reflection;

namespace DOL.GS.Quests.Hibernia
{
    public class WildWilderness : BaseQuest
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected const int MIN_LEVEL = 1;
        protected const int MAX_LEVEL = 5;
        protected const string QUEST_TITLE = "Wild Wilderness";
        protected const string QUEST_GIVER_NAME = "Miach";
        private static GameNPC _miach = null;
        private static ItemTemplate _lungerTail = null;

        public override string Name
        {
            get { return QUEST_TITLE; }
        }

        public override string Description
        {
            get
            {
                switch (Step)
                {
                    case 1:
                        return "[Step #1] Kill a lunger for its tail. They can be found just outside the gates of Domnann.";
                    case 2:
                        return "[Step #2] Bring the tail to Resalg in Grove of Donmann";
                    case 3:
                        return "[Step #3] Talk to Resalg about the [formula]";
                }
                return base.Description;
            }
        }
        public WildWilderness() : base() { }
        public WildWilderness(GamePlayer questingPlayer) : base(questingPlayer) { }
        public WildWilderness(GamePlayer questingPlayer, int step) : base(questingPlayer, step) { }
        public WildWilderness(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest) { }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (!ServerProperties.Properties.LOAD_QUESTS)
            {
                return;
            }
            GameNPC[] npcs = WorldMgr.GetNPCsByName(QUEST_GIVER_NAME, eRealm.Hibernia);
            if (npcs.Length == 0)
            {
                log.Error($"{QUEST_GIVER_NAME} does not exist for {QUEST_TITLE}. Cannot Implement Quest");
            }
            _miach = npcs[0];
            _miach.AddQuestToGive(typeof(WildWilderness));
            if (log.IsInfoEnabled)
            {
                log.Info($"Quest {QUEST_TITLE} initialized");
            }
            _lungerTail = GameServer.Database.FindObjectByKey<ItemTemplate>("lunger_tail");
            if (_lungerTail == null)
            {
                _lungerTail = new ItemTemplate();
                _lungerTail.Name = "Lunger Tail";
                if (log.IsWarnEnabled)
                {
                    log.Warn("Could not find " + _lungerTail.Name + ", creating it ...");
                }
                _lungerTail.Weight = 10;
                _lungerTail.Model = 515;
                _lungerTail.Id_nb = "lunger_tail";
                _lungerTail.IsPickable = true;
                _lungerTail.IsDropable = true;
                _lungerTail.IsTradable = false;
                _lungerTail.Quality = 100;
                _lungerTail.Condition = 1000;
                _lungerTail.MaxCondition = 1000;
                _lungerTail.Durability = 1000;
                _lungerTail.MaxDurability = 1000;
            }
            GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

            GameEventMgr.AddHandler(_miach, GameLivingEvent.Interact, new DOLEventHandler(TalkToMiach));
            GameEventMgr.AddHandler(_miach, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToMiach));
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            if (_miach == null)
            {
                return;
            }
            GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(_miach, GameLivingEvent.Interact, new DOLEventHandler(TalkToMiach));
            GameEventMgr.RemoveHandler(_miach, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToMiach));
            _miach.RemoveQuestToGive(typeof(WildWilderness));
        }

        protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
        {
            QuestEventArgs qargs = args as QuestEventArgs;
            if (qargs == null)
            {
                return;
            }
            if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(WildWilderness)))
            {
                return;
            }
            if (e == GamePlayerEvent.AcceptQuest)
            {
                CheckPlayerAcceptQuest(qargs.Player, 0x01);
            }
            else if (e == GamePlayerEvent.DeclineQuest)
            {
                CheckPlayerAcceptQuest(qargs.Player, 0x00);
            }
        }

        protected static void TalkToMiach(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
            if (player == null)
            {
                return;
            }
            if (_miach.CanGiveQuest(typeof(WildWilderness), player) <= 0)
            {
                return;
            }
            WildWilderness quest = player.IsDoingQuest(typeof(WildWilderness)) as WildWilderness;
            _miach.TurnTo(player);
            if (e == GameObjectEvent.Interact)
            {
                if (quest == null)
                {
                    _miach.SayTo(player, "There was once a time when all of Hy Brasil was tame. There was once a time when nature's beasts were tame. Now, though, the creatures we once had no reason to fear, have become aggressive. They kill not just for food, but for pleasure. They no longer only kill their normal prey. Now, the [hunt] us.");
                    return;
                }
                else
                {
                    _miach.SayTo(player, "What can I do for you, fine Forester?");
                    return;
                }
            }
            else if (e == GameLivingEvent.WhisperReceive)
            {
                WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs)args;
                if (quest == null)
                {
                    switch (wArgs.Text)
                    {
                        case "hunt":
                            _miach.SayTo(player, "Aye. It is not their normal way. I believe it is the evil touch of the Fomorians! They have ruined the homes of many of these creatures, they have destroyed their hunting grounds, and now, some of the creatures have become twisted and abhorrent! The beasts I speak of, close to our town, the lungers, we must get ird of them!");
                            player.Out.SendQuestSubscribeCommand(_miach, QuestMgr.GetIDForQuestType(typeof(WildWilderness)), "Will you help rid the area of young lungers?");
                            break;
                    }
                }
                else
                {
                    switch (wArgs.Text)
                    {
                        case "abort":
                            player.Out.SendCustomDialog("Do you really want to abort this quest, \nall items gained during quest will be lost?", new CustomDialogResponse(CheckPlayerAbortQuest));
                            break;
                        case "formula":
                            _miach.SayTo(player, "Aye. It will calm down the maddened animals, I think. I can't truly be sure, but I'm hoping so! Some of the animals have even been mutated, since the Fomorian's arrival, you know. I blame the foulness of that maleficent race! I would like to aid the Sylvan, I would like to see this ravaged land restored, and the peaceful balance that has taken generations upon generations to develop, once again restored. Aye, and I'll not lie, but I'm willing to bet I'd have a name, if I were to aid the process in some way! Maybe some prestige! Ah, but to work, yes, to work now. Here, I shall pay you for this tail! Good day, friend!");
                            RemoveItem(_miach, player, _lungerTail);
                            player.GainExperience(GameLiving.eXPSource.Quest, 20, true);
                            player.GainBountyPoints(2);
                            player.AddMoney(Money.GetMoney(0, 0, 0, 20, 0), "You are rewarded 6 silver, 20 experience, & 2 bounty points! You have Finished Wild Wilderness Quest");
                            quest.FinishQuest();
                            break;
                    }
                }
            }
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;
            if (player == null || player.IsDoingQuest(typeof(WildWilderness)) == null)
            {
                return;
            }

            if (e == GameLivingEvent.EnemyKilled)
            {
                EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs)args;
                if (Step == 1)
                {
                    if (gArgs.Target.Name == "lunger")
                    {
                        WildWilderness quest = player.IsDoingQuest(typeof(WildWilderness)) as WildWilderness;
                        GiveItem(gArgs.Target, player, _lungerTail);
                        quest.Step = 2;
                    }
                }
            }
            if (e == GamePlayerEvent.GiveItem)
            {
                if (Step == 2)
                {
                    GiveItemEventArgs gArgs = (GiveItemEventArgs)args;
                    if (gArgs.Target.Name == _miach.Name && gArgs.Item.Id_nb == _lungerTail.Id_nb)
                    {
                        _miach.SayTo(player, "Ah! You've brought me a lunger tail! Now, I'm not sure if this is going to work, but I think it just might, yes, it just might. You see, I'm working on a special [formula].");
                        WildWilderness quest = player.IsDoingQuest(typeof(WildWilderness)) as WildWilderness;
                        quest.Step = 3;
                    }
                }
            }
        }

        private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
        {
            WildWilderness quest = player.IsDoingQuest(typeof(WildWilderness)) as WildWilderness;
            if (quest == null)
            {
                return;
            }
            if (response == 0x00)
            {
                SendSystemMessage(player, "Good, no go out there and finish your work!");
            }
            else
            {
                SendSystemMessage(player, "Aborting Quest " + QUEST_TITLE + ". You can start over again if you want.");
                quest.AbortQuest();
            }
        }

        private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
        {
            if (_miach.CanGiveQuest(typeof(WildWilderness), player) <= 0)
                return;

            if (player.IsDoingQuest(typeof(WildWilderness)) != null)
                return;

            if (response == 0x00)
            {
                SendReply(player, "Oh well, if you change your mind, please come back!");
            }
            else
            {
                if (!_miach.GiveQuest(typeof(WildWilderness), player, 1))
                    return;

                SendReply(player, "Great! While you are at it, bring one of the lunger tails to Resalg. He will pay you well for it. He is trying to create some potion. I told him I'd acquire a tail for him.");
            }
        }

        public override bool CheckQuestQualification(GamePlayer player)
        {
            if (player.IsDoingQuest(typeof(WildWilderness)) != null)
            {
                return true;
            }
            if (player.Level < MIN_LEVEL || player.Level > MAX_LEVEL)
            {
                return false;
            }
            if (player.CharacterClass.ID != (int)eCharacterClass.Animist && player.CharacterClass.ID != (int)eCharacterClass.Forester)
            {
                return false;
            }
            return true;
        }

        public override void FinishQuest()
        {
            base.FinishQuest();
        }
    }
}
