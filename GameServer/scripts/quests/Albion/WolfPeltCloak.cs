using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Quests.Albion
{
    public class WolfPeltCloak : BaseQuest
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected const string questTitle = "Wolf Pelt Cloak";
        protected const string stewardWillieNpcName = "Steward Willie";
        protected const string streamstressLynnetNpcName = "Seamstress Lynnet";
        protected const string wolfPeltCloak = "wolf_pelt_cloak";
        protected const string wolfFur = "wolf_fur";
        protected const string wolfHeadToken = "wolf_head_token";

        protected const int MIN_LEVEL = 1;
        protected const int MAX_LEVEL = 11;

        private static GameNPC _stewardWillie = null;
        private static GameNPC _lynett = null;

        private static ItemTemplate _wolfPeltCloak = null;
        private static ItemTemplate _wolfFur = null;
        private static ItemTemplate _wolfHeadToken = null;

        public WolfPeltCloak() : base() { }
        public WolfPeltCloak(GamePlayer questingPlayer) : this(questingPlayer, 1) { }
        public WolfPeltCloak(GamePlayer questingPlayer, int step) : base(questingPlayer, step) { }
        public WolfPeltCloak(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest) { }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (!ServerProperties.Properties.LOAD_QUESTS)
                return;
            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" initializing ...");

            GameNPC[] gameNpcQuery = WorldMgr.GetNPCsByName(stewardWillieNpcName, eRealm.Albion);
            if (gameNpcQuery.Length == 0)
            {
                _logReasonQuestCantBeImplemented(stewardWillieNpcName);
                return;
            }
            else
            {
                _stewardWillie = gameNpcQuery[0];
            }
            gameNpcQuery = WorldMgr.GetNPCsByName(streamstressLynnetNpcName, eRealm.Albion);
            if (gameNpcQuery.Length == 0)
            {
                _logReasonQuestCantBeImplemented(streamstressLynnetNpcName);
                return;
            }
            else
            {
                _lynett = gameNpcQuery[0];
            }

            _wolfPeltCloak = GameServer.Database.FindObjectByKey<ItemTemplate>(wolfPeltCloak);
            if (_wolfPeltCloak == null)
            {
                _logReasonQuestCantBeImplemented(wolfPeltCloak);
                return;
            }
            _wolfFur = GameServer.Database.FindObjectByKey<ItemTemplate>(wolfFur);
            if (_wolfFur == null)
            {
                _logReasonQuestCantBeImplemented(wolfFur);
                return;
            }

            _wolfHeadToken = GameServer.Database.FindObjectByKey<ItemTemplate>(wolfHeadToken);
            if (_wolfHeadToken == null)
            {
                _logReasonQuestCantBeImplemented(wolfHeadToken);
                return;
            }
            GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(_stewardWillie, GameLivingEvent.Interact, new DOLEventHandler(TalkToStewardWillie));
            GameEventMgr.AddHandler(_stewardWillie, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToStewardWillie));
            GameEventMgr.AddHandler(_lynett, GameLivingEvent.Interact, new DOLEventHandler(TalkToSeamstressLynnet));

            _stewardWillie.AddQuestToGive(typeof(WolfPeltCloak));

            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" initialized");
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            if (_stewardWillie == null)
                return;

            GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(_stewardWillie, GameObjectEvent.Interact, new DOLEventHandler(TalkToStewardWillie));
            GameEventMgr.RemoveHandler(_stewardWillie, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToStewardWillie));
            GameEventMgr.RemoveHandler(_lynett, GameObjectEvent.Interact, new DOLEventHandler(TalkToSeamstressLynnet));
            _stewardWillie.RemoveQuestToGive(typeof(WolfPeltCloak));
        }

        protected static void TalkToStewardWillie(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
            if (player == null)
                return;

            if (_stewardWillie.CanGiveQuest(typeof(WolfPeltCloak), player) <= 0)
                return;

            WolfPeltCloak quest = player.IsDoingQuest(typeof(WolfPeltCloak)) as WolfPeltCloak;

            _stewardWillie.TurnTo(player);
            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {
                    if (player.Inventory.GetFirstItemByID(_wolfFur.Id_nb, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) != null)
                        _stewardWillie.SayTo(player, "Ah, well done! His Lordship will be pleased to know there is one less mongrel in the pack! Give me the fur so I can throw it with the others.");
                    else if (player.Inventory.GetFirstItemByID(_wolfHeadToken.Id_nb, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) != null)
                        _stewardWillie.SayTo(player, "Give the token to Seamstress Lynnet in Ludlow, she'll give ye your reward. Thank ye for your fine services to His Lordship.");
                    else
                        _stewardWillie.SayTo(player, "Good! I know we ca'count on ye. I will reward ye for the pelt ye bring me from one of those vile beasts!");
                    return;
                }
                else
                {
                    _stewardWillie.SayTo(player, "Aye, hello there! Have ye been sent t'help with our [problem]");
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
                        case "problem":
                            _stewardWillie.SayTo(player, "What? Ye haven't heard? Hhhmm, then I wonder if ye would [like to help]");
                            break;
                        case "pack of wolves":
                            _stewardWillie.SayTo(player, "There should be some around the area of this village, take a look near the road to Camelot. Kill any wolf pups you can find, and bring me its fur.");
                            break;
                        case "like to help":
                            _stewardWillie.SayTo(player, "That's wonderful! We've been havin' a serious problem with a [pack of wolves]. His Lordship wants'em eliminated because they have been a-bothering the people here about. His Lordship has authorized me to reward those who [serve him well].");
                            break;
                        case "serve him well":
                            player.Out.SendQuestSubscribeCommand(_stewardWillie, QuestMgr.GetIDForQuestType(typeof(WolfPeltCloak)), "Do you accept the Wolf Pelt Cloak quest?");
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

        protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
        {
            QuestEventArgs qargs = args as QuestEventArgs;
            if (qargs == null)
                return;

            if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(WolfPeltCloak)))
                return;

            if (e == GamePlayerEvent.AcceptQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x01);
            else if (e == GamePlayerEvent.DeclineQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x00);
        }

        protected static void TalkToSeamstressLynnet(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
            if (player == null)
                return;

            if (_stewardWillie.CanGiveQuest(typeof(WolfPeltCloak), player) <= 0)
                return;

            WolfPeltCloak quest = player.IsDoingQuest(typeof(WolfPeltCloak)) as WolfPeltCloak;

            _lynett.TurnTo(player);
            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {             
                    _lynett.SayTo(player, "I hear you have a token for me, as proof of your valuable work for his Lordship. Give it to me and I will reward you.");
                }
            }
        }

        public override bool CheckQuestQualification(GamePlayer player)
        {            
            if (player.IsDoingQuest(typeof(WolfPeltCloak)) != null)
                return true;
                        
            if (player.Level < MIN_LEVEL || player.Level > MAX_LEVEL)
                return false;

            return true;
        }
        private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
        {
            WolfPeltCloak quest = player.IsDoingQuest(typeof(WolfPeltCloak)) as WolfPeltCloak;

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

        private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
        {
            if (_stewardWillie.CanGiveQuest(typeof(WolfPeltCloak), player) <= 0)
                return;

            if (player.IsDoingQuest(typeof(WolfPeltCloak)) != null)
                return;

            if (response == 0x00)
            {
                SendReply(player, "Oh well, if you change your mind, please come back!");
            }
            else
            {
                if (!_stewardWillie.GiveQuest(typeof(WolfPeltCloak), player, 1))
                    return;

                _stewardWillie.SayTo(player, "Good! I know we ca'count on ye. I will reward ye for the pelt ye bring me from one of those vile beasts!");
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
                        return "[Step #1] Go out into the fields to hunt a wolf pup and flay its fur.";
                    case 2:
                        return "[Step #2] Bring the fur back to Steward Willie in Humberton Fort.";
                    case 3:
                        return "[Step #3] Go to Seamstress Lynnet in Ludlow and bring her the wolf head token.";
                }
                return base.Description;
            }
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

            if (player == null)
                return;

            if (player.IsDoingQuest(typeof(WolfPeltCloak)) == null)
                return;

            if (Step == 1 && e == GameLivingEvent.EnemyKilled)
            {
                EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs)args;
                if (gArgs.Target.Name.IndexOf("wolf") >= 0)
                {
                    SendSystemMessage("You've killed the " + gArgs.Target.Name + " and flayed the fur from it.!");
                    _wolfFur.Name = gArgs.Target.GetName(1, true) + " fur";
                    GiveItem(player, _wolfFur);
                    Step = 2;
                    return;
                }
            }
            else if (Step == 2 && e == GamePlayerEvent.GiveItem)
            {
                GiveItemEventArgs gArgs = (GiveItemEventArgs)args;
                if (gArgs.Target.Name == _stewardWillie.Name && gArgs.Item.Id_nb == _wolfFur.Id_nb)
                {
                    _stewardWillie.TurnTo(m_questPlayer);
                    _stewardWillie.SayTo(m_questPlayer, "Take this token from His Lordship. If ye give it to Seamstress Lynnet in Ludlow, she'll give ye your reward. Thank ye for your fine services to His Lordship.");

                    RemoveItem(_stewardWillie, player, _wolfFur);
                    GiveItem(_stewardWillie, player, _wolfHeadToken);
                    Step = 3;
                    return;
                }
            }
            else if (Step == 3 && e == GamePlayerEvent.GiveItem)
            {
                GiveItemEventArgs gArgs = (GiveItemEventArgs)args;
                if (gArgs.Target.Name == _lynett.Name && gArgs.Item.Id_nb == _wolfHeadToken.Id_nb)
                {
                    RemoveItem(_lynett, player, _wolfHeadToken);
                    _lynett.SayTo(player, "Well done! Here's your fine wolf pelt cloak. Wear it with pride knowing you have helped his Lordship.");
                    FinishQuest();
                    return;
                }
            }
        }

        public override void AbortQuest()
        {
            base.AbortQuest();
            RemoveItem(m_questPlayer, _wolfFur, false);
            RemoveItem(m_questPlayer, _wolfHeadToken, false);
        }

        public override void FinishQuest()
        {
            base.FinishQuest();
            GiveItem(_lynett, m_questPlayer, _wolfPeltCloak);

            m_questPlayer.GainExperience(eXPSource.Quest, 50, true);
            long money = Money.GetMoney(0, 0, 0, 0, 50);
            m_questPlayer.AddMoney(money, "You recieve {0} for your service.");
            InventoryLogging.LogInventoryAction("(QUEST;" + Name + ")", m_questPlayer, eInventoryActionType.Quest, money);
        }

        public static void _logReasonQuestCantBeImplemented(string entity)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn($"Could not find {entity}, cannot load quest: Wolf Pelt Quest");
            }
        }
    }
}
