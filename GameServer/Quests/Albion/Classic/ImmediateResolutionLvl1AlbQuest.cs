using System;
using System.Reflection;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Packets;
using Core.GS.Packets.Server;
using log4net;

namespace Core.GS.Quests.Albion
{
    public class ImmediateResolutionLvl1AlbQuest : BaseQuest
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected const string questTitle = "Immediate Resolution";
        protected const string stewardWillieNpcName = "Steward Willie";
        protected const string masterTorNpc = "Master Torr";
        protected const string waxSealedNoteInDb = "wax_sealed_note";
        protected const string waxSealedNoteName = "Wax Sealed Note";

        protected const int MIN_LEVEL = 1;
        protected const int MAX_LEVEL = 5;

        protected const int _cluster1BaseX = 519203;
        protected const int _cluster1BaseY = 451090;
        protected const int _cluster1BaseZ = 2370;

        protected const int _cluster2BaseX = 523755;
        protected const int _cluster2BaseY = 451229;
        protected const int _cluster2BaseZ = 2379;

        protected const int _xDelta = 75;
        protected const int _yDelta = 20;

        private static DbItemTemplate waxSealedNote = null;

        private static GameNpc _masterTor = null;
        private static GameNpc _stewardWillie = null;

        private static GameRealmGuard _gaurdsman1 = null;
        private static GameRealmGuard _gaurdsman2 = null;
        private static GameRealmGuard _gaurdsman3 = null;
        private static GameRealmGuard _gaurdsman4 = null;

        public ImmediateResolutionLvl1AlbQuest() : base() { }
        public ImmediateResolutionLvl1AlbQuest(GamePlayer questingPlayer) : this(questingPlayer, 1) { }
        public ImmediateResolutionLvl1AlbQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step) { }
        public ImmediateResolutionLvl1AlbQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest) { }


        public override void Notify(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

            if(player == null)
            {
                return;
            }
            if(player.IsDoingQuest(typeof(ImmediateResolutionLvl1AlbQuest)) == null)
            {
                return;
            }

            if(Step == 2 && e == GamePlayerEvent.GiveItem)
            {
                GiveItemEventArgs gArgs = (GiveItemEventArgs)args;
                if(gArgs.Target == _gaurdsman1 || gArgs.Target == _gaurdsman2 || gArgs.Target == _gaurdsman3 || gArgs.Target == _gaurdsman4)
                {
                    if(gArgs.Item.Id_nb == waxSealedNoteInDb)
                    {
                        GameNpc selectedGuard = gArgs.Target as GameNpc;
                        INpcTemplate template = NpcTemplateMgr.GetTemplate(12211);
                        RemoveItem(selectedGuard, player, waxSealedNote);
                        selectedGuard.SayTo(player, "If you wish to assist us in our defense we would welcome it.");
                        selectedGuard.SayTo(player, "Prepare yourselves!");
                        FinishQuest();
                        if (gArgs.Target == _gaurdsman1 || gArgs.Target == _gaurdsman2)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                GameNpc mob = new GameNpc(template)
                                {
                                    X = _cluster1BaseX - (i * _xDelta),
                                    Y = _cluster1BaseY + (i * _yDelta),
                                    Z = _cluster1BaseZ,
                                    CurrentRegionID = 1,
                                    SaveInDB = false
                                };
                                mob.AddBrain(new StandardMobBrain());
                                mob.TargetObject = gArgs.Target;
                                mob.AddToWorld();
                                if (i <= 5)
                                {                                    
                                    mob.WalkTo(_gaurdsman2, 180);
                                    (mob.Brain as StandardMobBrain).AddToAggroList(_gaurdsman2, 1000);
                                }
                                else
                                {
                                    mob.WalkTo(_gaurdsman1, 180);
                                    (mob.Brain as StandardMobBrain).AddToAggroList(_gaurdsman1, 1000);
                                }
                            }
                        }

                        if (gArgs.Target == _gaurdsman3 || gArgs.Target == _gaurdsman4)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                GameNpc mob = new GameNpc(template)
                                {
                                    X = _cluster2BaseX - (i * _xDelta),
                                    Y = _cluster2BaseY + (i * _yDelta),
                                    Z = _cluster2BaseZ,
                                    CurrentRegionID = 1,
                                    SaveInDB = false
                                };
                                mob.AddBrain(new StandardMobBrain());
                                mob.TargetObject = gArgs.Target;
                                mob.AddToWorld();
                                if (i <= 5)
                                {
                                    mob.WalkTo(_gaurdsman4, 180);
                                    (mob.Brain as StandardMobBrain).AddToAggroList(_gaurdsman4, 1000);
                                }
                                else
                                {
                                    mob.WalkTo(_gaurdsman3, 180);
                                    (mob.Brain as StandardMobBrain).AddToAggroList(_gaurdsman3, 1000);
                                }
                            }
                        }
                    }
                }
            }
            base.Notify(e, sender, args);
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            if (!ServerProperties.Properties.LOAD_QUESTS)
                return;
            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" initializing ...");

            GameNpc[] gameNpcQuery = WorldMgr.GetNPCsByName(masterTorNpc, ERealm.Albion);
            if (gameNpcQuery.Length == 0)
            {
                _logReasonQuestCantBeImplemented(masterTorNpc);
                return;
            }
            else
            {
                _masterTor = gameNpcQuery[0];
            }
            gameNpcQuery = WorldMgr.GetNPCsByName(stewardWillieNpcName, ERealm.Albion);
            if (gameNpcQuery.Length == 0)
            {
                _logReasonQuestCantBeImplemented(stewardWillieNpcName);
                return;
            }
            else
            {
                _stewardWillie = gameNpcQuery[0];
            }
            _loadGuards();

            waxSealedNote = GameServer.Database.FindObjectByKey<DbItemTemplate>(waxSealedNoteInDb);
            if (waxSealedNote == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Could not find {waxSealedNoteName}, creating it ...");
                waxSealedNote = new DbItemTemplate();
                waxSealedNote.Object_Type = 0;
                waxSealedNote.Id_nb = waxSealedNoteInDb;
                waxSealedNote.Name = waxSealedNoteName;
                waxSealedNote.Level = 1;
                waxSealedNote.Model = 499;
                waxSealedNote.IsDropable = false;
                waxSealedNote.IsPickable = false;
                waxSealedNote.IsTradable = false;
                GameServer.Database.AddObject(waxSealedNote);
            }

            GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(_stewardWillie, GameLivingEvent.Interact, new CoreEventHandler(TalkToStewardWillie));
            GameEventMgr.AddHandler(_stewardWillie, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToStewardWillie));
            GameEventMgr.AddHandler(_masterTor, GameLivingEvent.Interact, new CoreEventHandler(TalkToMasterTor));
            GameEventMgr.AddHandler(_masterTor, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToMasterTor));

            _masterTor.AddQuestToGive(typeof(ImmediateResolutionLvl1AlbQuest));

            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" initialized");
        }        

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
        {
            GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

            GameEventMgr.RemoveHandler(_stewardWillie, GameObjectEvent.Interact, new CoreEventHandler(TalkToStewardWillie));
            GameEventMgr.RemoveHandler(_stewardWillie, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToStewardWillie));

            GameEventMgr.RemoveHandler(_masterTor, GameLivingEvent.Interact, new CoreEventHandler(TalkToMasterTor));
            GameEventMgr.RemoveHandler(_masterTor, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToMasterTor));

            _masterTor.RemoveQuestToGive(typeof(ImmediateResolutionLvl1AlbQuest));
        }

        protected static void TalkToStewardWillie(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
            if (player == null)
                return;

            ImmediateResolutionLvl1AlbQuest quest = player.IsDoingQuest(typeof(ImmediateResolutionLvl1AlbQuest)) as ImmediateResolutionLvl1AlbQuest;

            _stewardWillie.TurnTo(player);
            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {
                    _stewardWillie.SayTo(player, "You have been chosen to assist us in delivering [a message]. I'm sure it will be worth your time.");
                    return;
                }
            }
            else if (e == GameLivingEvent.WhisperReceive)
            {
                WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs)args;

                if (quest != null)
                {
                    switch (wArgs.Text)
                    {
                        case "a message":
                            _stewardWillie.SayTo(player, "Here! There is a large bridge to the northeast of this keep. It leads to the northern regions and then on into the frontier lands. You will take this message to the guards at the opposite side of the bridge. They are about to be [attacked] so you must hurry!");
                            break;
                        case "attacked":
                            _stewardWillie.SayTo(player, "Barbarians have long attacked our borders! So long as they live, we shall never rest! Now off with ye");
                            if (quest.Step == 1)
                            {
                                quest.Step = 2;
                                GiveItem(_stewardWillie, player, waxSealedNote);
                            }
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
                    }
                }

            }
        }

        protected static void TalkToMasterTor(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
            if (player == null)
                return;

            if (_masterTor.CanGiveQuest(typeof(ImmediateResolutionLvl1AlbQuest), player) <= 0)
                return;

            ImmediateResolutionLvl1AlbQuest quest = player.IsDoingQuest(typeof(ImmediateResolutionLvl1AlbQuest)) as ImmediateResolutionLvl1AlbQuest;

            _masterTor.TurnTo(player);
            if (e == GameObjectEvent.Interact)
            {
                if (quest == null)
                {
                    _masterTor.SayTo(player, "Welcome young warrior! It is good that you have decided to take up the fight in Albion. Our borders are in need of [adventurers] such as you");
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
                        case "adventurers":
                            player.Out.SendQuestSubscribeCommand(_masterTor, QuestMgr.GetIDForQuestType(typeof(ImmediateResolutionLvl1AlbQuest)), $"Do you accept this task?");
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
                    }
                }

            }
        }

        private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
        {
            ImmediateResolutionLvl1AlbQuest quest = player.IsDoingQuest(typeof(ImmediateResolutionLvl1AlbQuest)) as ImmediateResolutionLvl1AlbQuest;

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

            if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(ImmediateResolutionLvl1AlbQuest)))
                return;

            if (e == GamePlayerEvent.AcceptQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x01);
            else if (e == GamePlayerEvent.DeclineQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x00);
        }

        private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
        {
            if (_masterTor.CanGiveQuest(typeof(ImmediateResolutionLvl1AlbQuest), player) <= 0)
                return;

            if (player.IsDoingQuest(typeof(ImmediateResolutionLvl1AlbQuest)) != null)
                return;

            if (response == 0x00)
            {
                SendReply(player, "Oh well, if you change your mind, please come back!");
            }
            else
            {
                if (!_masterTor.GiveQuest(typeof(ImmediateResolutionLvl1AlbQuest), player, 1))
                    return;

                _masterTor.SayTo(player, "Good! Locate Steward Willie in the keep overlooking Humberton!");
            }
        }

        public override string Name
        {
            get { return questTitle; }
        }

        public override string Description
        {
            get
            {
                switch (Step)
                {
                    case 1:
                        return "Locate Steward Willie in the keep overlooking Humberton!";
                    case 2:
                        return "Locate the guards who defend a bridge just east of Humberton. Deliver the wax-sealed note to them. Do not tarry!";
                }
                return base.Description;
            }
        }

        public override bool CheckQuestQualification(GamePlayer player)
        {
            if (player.IsDoingQuest(typeof(ImmediateResolutionLvl1AlbQuest)) != null)
                return true;

            if (player.Level < MIN_LEVEL || player.Level > MAX_LEVEL)
                return false;

            return true;
        }

        public override void FinishQuest()
        {
            base.FinishQuest();
            m_questPlayer.ForceGainExperience( 50);
            long money = MoneyMgr.GetMoney(0, 0, 0, 0, 30 + Util.Random(50));
            m_questPlayer.AddMoney(money, "You recieve {0} for your service.");
            InventoryLogging.LogInventoryAction("(QUEST;" + Name + ")", m_questPlayer, EInventoryActionType.Quest, money);

        }

        public static void _logReasonQuestCantBeImplemented(string entity)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn($"Could not find {entity}, cannot load quest: {questTitle}");
            }
        }

        private static void _loadGuards()
        {
            NpcTemplateMgr.Reload();
            INpcTemplate template = NpcTemplateMgr.GetTemplate(12964);
            _gaurdsman1 = new GameRealmGuard(template)
            {
                X = 519273,
                Y = 452388,
                Z = 2329,
                Heading = 1883,
                CurrentRegionID = 1,
                SaveInDB = false,
                Realm = ERealm.Albion
            };
            _gaurdsman1.Flags ^= ENpcFlags.GHOST;
            _gaurdsman1.AddToWorld();
            _gaurdsman2 = new GameRealmGuard(template)
            {
                X = 519829,
                Y = 452156,
                Z = 2368,
                Heading = 1860,
                CurrentRegionID = 1,
                SaveInDB = false,
                Realm = ERealm.Albion
            };
            _gaurdsman2.Flags ^= ENpcFlags.GHOST;
            _gaurdsman2.AddToWorld();
            _gaurdsman3 = new GameRealmGuard(template)
            {
                X = 523091,
                Y = 452281,
                Z = 2330,
                Heading = 1995,
                CurrentRegionID = 1,
                SaveInDB = false,
                Realm = ERealm.Albion
            };
            _gaurdsman3.Flags ^= ENpcFlags.GHOST;
            _gaurdsman3.AddToWorld();
            _gaurdsman4 = new GameRealmGuard(template)
            {
                X = 522534,
                Y = 452024,
                Z = 2354,
                Heading = 2371,
                CurrentRegionID = 1,
                SaveInDB = false,
                Realm = ERealm.Albion
            };
            _gaurdsman4.Flags ^= ENpcFlags.GHOST;
            _gaurdsman4.AddToWorld();
        }
    }
}
