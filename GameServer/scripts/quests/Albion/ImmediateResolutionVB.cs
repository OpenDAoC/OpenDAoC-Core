using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Quests.Albion
{
    public class ImmediateResolutionVB : BaseQuest
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected const string questTitle = "Immediate Resolution";
        protected const string stewardWillieNpcName = "Steward Willie";
        protected const string masterTorNpc = "Master Torr";
        protected const string waxSealedNoteInDb = "wax_sealed_note";
        protected const string waxSealedNoteName = "Wax Sealed Note";
        protected const int MIN_LEVEL = 1;
        protected const int MAX_LEVEL = 5;

        private static ItemTemplate waxSealedNote = null;

        private static GameNPC _masterTor = null;
        private static GameNPC _stewardWillie = null;

        private static GameNPC _gaurdsman1 = null;
        private static GameGuard _gaurdsman2 = null;
        private static GameGuard _gaurdsman3 = null;
        private static GameGuard _gaurdsman4 = null;

        public ImmediateResolutionVB() : base() { }
        public ImmediateResolutionVB(GamePlayer questingPlayer) : this(questingPlayer, 1) { }
        public ImmediateResolutionVB(GamePlayer questingPlayer, int step) : base(questingPlayer, step) { }
        public ImmediateResolutionVB(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest) { }


        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (!ServerProperties.Properties.LOAD_QUESTS)
                return;
            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" initializing ...");

            GameNPC[] gameNpcQuery = WorldMgr.GetNPCsByName(masterTorNpc, eRealm.Albion);
            if (gameNpcQuery.Length == 0)
            {
                _logReasonQuestCantBeImplemented(masterTorNpc);
                return;
            }
            else
            {
                _masterTor = gameNpcQuery[0];
            }
            gameNpcQuery = WorldMgr.GetNPCsByName(stewardWillieNpcName, eRealm.Albion);
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

            waxSealedNote = GameServer.Database.FindObjectByKey<ItemTemplate>(waxSealedNoteInDb);
            if (waxSealedNote == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Could not find {waxSealedNoteName}, creating it ...");
                waxSealedNote = new ItemTemplate();
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

            GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(_stewardWillie, GameLivingEvent.Interact, new DOLEventHandler(TalkToStewardWillie));
            GameEventMgr.AddHandler(_stewardWillie, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToStewardWillie));
            GameEventMgr.AddHandler(_masterTor, GameLivingEvent.Interact, new DOLEventHandler(TalkToMasterTor));
            GameEventMgr.AddHandler(_masterTor, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToMasterTor));

            _masterTor.AddQuestToGive(typeof(ImmediateResolutionVB));

            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" initialized");
        }        

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

            GameEventMgr.RemoveHandler(_stewardWillie, GameObjectEvent.Interact, new DOLEventHandler(TalkToStewardWillie));
            GameEventMgr.RemoveHandler(_stewardWillie, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToStewardWillie));

            GameEventMgr.RemoveHandler(_masterTor, GameLivingEvent.Interact, new DOLEventHandler(TalkToMasterTor));
            GameEventMgr.RemoveHandler(_masterTor, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToMasterTor));

            _masterTor.RemoveQuestToGive(typeof(ImmediateResolutionVB));
        }

        protected static void TalkToStewardWillie(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
            if (player == null)
                return;

            ImmediateResolutionVB quest = player.IsDoingQuest(typeof(ImmediateResolutionVB)) as ImmediateResolutionVB;

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

        protected static void TalkToMasterTor(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
            if (player == null)
                return;

            if (_masterTor.CanGiveQuest(typeof(ImmediateResolutionVB), player) <= 0)
                return;

            ImmediateResolutionVB quest = player.IsDoingQuest(typeof(ImmediateResolutionVB)) as ImmediateResolutionVB;

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
                            player.Out.SendQuestSubscribeCommand(_masterTor, QuestMgr.GetIDForQuestType(typeof(ImmediateResolutionVB)), $"Do you accept this task?");
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
            ImmediateResolutionVB quest = player.IsDoingQuest(typeof(ImmediateResolutionVB)) as ImmediateResolutionVB;

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

        protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
        {
            QuestEventArgs qargs = args as QuestEventArgs;
            if (qargs == null)
                return;

            if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(ImmediateResolutionVB)))
                return;

            if (e == GamePlayerEvent.AcceptQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x01);
            else if (e == GamePlayerEvent.DeclineQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x00);
        }

        private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
        {
            if (_masterTor.CanGiveQuest(typeof(ImmediateResolutionVB), player) <= 0)
                return;

            if (player.IsDoingQuest(typeof(ImmediateResolutionVB)) != null)
                return;

            if (response == 0x00)
            {
                SendReply(player, "Oh well, if you change your mind, please come back!");
            }
            else
            {
                if (!_masterTor.GiveQuest(typeof(ImmediateResolutionVB), player, 1))
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
            if (player.IsDoingQuest(typeof(ImmediateResolutionVB)) != null)
                return true;

            if (player.Level < MIN_LEVEL || player.Level > MAX_LEVEL)
                return false;

            return true;
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
            _gaurdsman1 = new GameNPC(template)
            {
                X = 519273,
                Y = 452388,
                Z = 2329,
                Heading = 1883,
                CurrentRegionID = 1,
                SaveInDB = false,
                Realm = eRealm.Albion
            };
            _gaurdsman1.Flags ^= GameNPC.eFlags.GHOST;
            _gaurdsman1.AddToWorld();
            _gaurdsman2 = new GameGuard(template)
            {
                X = 519829,
                Y = 452156,
                Z = 2368,
                Heading = 1860,
                CurrentRegionID = 1,
                SaveInDB = false,
                Realm = eRealm.Albion
            };
            _gaurdsman2.Flags ^= GameNPC.eFlags.GHOST;
            _gaurdsman2.AddToWorld();
            _gaurdsman3 = new GameGuard(template)
            {
                X = 523091,
                Y = 452281,
                Z = 2330,
                Heading = 1995,
                CurrentRegionID = 1,
                SaveInDB = false,
                Realm = eRealm.Albion
            };
            _gaurdsman3.Flags ^= GameNPC.eFlags.GHOST;
            _gaurdsman3.AddToWorld();
            _gaurdsman4 = new GameGuard(template)
            {
                X = 522534,
                Y = 452024,
                Z = 2354,
                Heading = 2371,
                CurrentRegionID = 1,
                SaveInDB = false,
                Realm = eRealm.Albion
            };
            _gaurdsman4.Flags ^= GameNPC.eFlags.GHOST;
            _gaurdsman4.AddToWorld();
        }
    }
}
