using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.PacketHandler;
using log4net;

namespace Core.GS.Quests.Albion
{
    public class WolfPeltCloakLvl1AlbQuest : BaseQuest
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected const string questTitle = "Wolf Pelt Cloak";
        protected const string stewardWillieNpcName = "Steward Willie";
        protected const string streamstressLynnetNpcName = "Seamstress Lynnet";
        protected const string brothDonNpcName = "Brother Don";
        protected const string wolfPeltCloak = "wolf_pelt_cloak";
        protected const string wolfFur = "wolf_fur";
        protected const string wolfHeadToken = "wolf_head_token";

        protected const int MIN_LEVEL = 1;
        protected const int MAX_LEVEL = 11;

        private static GameNpc _stewardWillie = null;
        private static GameNpc _lynett = null;
        private static GameNpc _don = null;

        private static DbItemTemplate _wolfPeltCloak = null;
        private static DbItemTemplate _wolfFur = null;
        private static DbItemTemplate _wolfHeadToken = null;

        public WolfPeltCloakLvl1AlbQuest() : base() { }
        public WolfPeltCloakLvl1AlbQuest(GamePlayer questingPlayer) : this(questingPlayer, 1) { }
        public WolfPeltCloakLvl1AlbQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step) { }
        public WolfPeltCloakLvl1AlbQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest) { }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
        {
            if (!ServerProperties.Properties.LOAD_QUESTS)
                return;
            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" initializing ...");

            GameNpc[] gameNpcQuery = WorldMgr.GetNPCsByName(stewardWillieNpcName, ERealm.Albion);
            if (gameNpcQuery.Length == 0)
            {
                _logReasonQuestCantBeImplemented(stewardWillieNpcName);
                return;
            }
            else
            {
                _stewardWillie = gameNpcQuery[0];
            }
            gameNpcQuery = WorldMgr.GetNPCsByName(streamstressLynnetNpcName, ERealm.Albion);
            if (gameNpcQuery.Length == 0)
            {
                _logReasonQuestCantBeImplemented(streamstressLynnetNpcName);
                return;
            }
            else
            {
                _lynett = gameNpcQuery[0];
            }
            gameNpcQuery = WorldMgr.GetNPCsByName(brothDonNpcName, ERealm.Albion);
            if (gameNpcQuery.Length == 0)
            {
                _logReasonQuestCantBeImplemented(brothDonNpcName);
                return;
            }
            else
            {
                _don = gameNpcQuery[0];
            }

            _wolfPeltCloak = GameServer.Database.FindObjectByKey<DbItemTemplate>(wolfPeltCloak);
            if (_wolfPeltCloak == null)
            {
                _logReasonQuestCantBeImplemented(wolfPeltCloak);
                return;
            }
            _wolfFur = GameServer.Database.FindObjectByKey<DbItemTemplate>(wolfFur);
            if (_wolfFur == null)
            {
                _logReasonQuestCantBeImplemented(wolfFur);
                return;
            }

            _wolfHeadToken = GameServer.Database.FindObjectByKey<DbItemTemplate>(wolfHeadToken);
            if (_wolfHeadToken == null)
            {
                _logReasonQuestCantBeImplemented(wolfHeadToken);
                return;
            }
            GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(_stewardWillie, GameLivingEvent.Interact, new CoreEventHandler(TalkToStewardWillie));
            GameEventMgr.AddHandler(_stewardWillie, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToStewardWillie));
            GameEventMgr.AddHandler(_don, GameLivingEvent.Interact, new CoreEventHandler(TalkToBrotherDon));
            GameEventMgr.AddHandler(_don, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToBrotherDon));
            GameEventMgr.AddHandler(_lynett, GameLivingEvent.Interact, new CoreEventHandler(TalkToSeamstressLynnet));

            _stewardWillie.AddQuestToGive(typeof(WolfPeltCloakLvl1AlbQuest));

            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" initialized");
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
        {
            if (_stewardWillie == null)
                return;

            GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(_stewardWillie, GameObjectEvent.Interact, new CoreEventHandler(TalkToStewardWillie));
            GameEventMgr.RemoveHandler(_stewardWillie, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToStewardWillie));
            GameEventMgr.RemoveHandler(_don, GameLivingEvent.Interact, new CoreEventHandler(TalkToBrotherDon));
            GameEventMgr.RemoveHandler(_don, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToBrotherDon));
            GameEventMgr.RemoveHandler(_lynett, GameObjectEvent.Interact, new CoreEventHandler(TalkToSeamstressLynnet));
            _stewardWillie.RemoveQuestToGive(typeof(WolfPeltCloakLvl1AlbQuest));
        }

        protected static void TalkToStewardWillie(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
            if (player == null)
                return;

            if (_stewardWillie.CanGiveQuest(typeof(WolfPeltCloakLvl1AlbQuest), player) <= 0)
                return;

            WolfPeltCloakLvl1AlbQuest quest = player.IsDoingQuest(typeof(WolfPeltCloakLvl1AlbQuest)) as WolfPeltCloakLvl1AlbQuest;

            _stewardWillie.TurnTo(player);
            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {
                    if (player.Inventory.GetFirstItemByID(_wolfFur.Id_nb, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack) != null)
                        _stewardWillie.SayTo(player, "Ah, well done! His Lordship will be pleased to know there is one less mongrel in the pack! Give me the fur so I can throw it with the others.");
                    else if (player.Inventory.GetFirstItemByID(_wolfHeadToken.Id_nb, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack) != null)
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
                            player.Out.SendQuestSubscribeCommand(_stewardWillie, QuestMgr.GetIDForQuestType(typeof(WolfPeltCloakLvl1AlbQuest)), "Do you accept the Wolf Pelt Cloak quest?");
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

        protected static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
        {
            QuestEventArgs qargs = args as QuestEventArgs;
            if (qargs == null)
                return;

            if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(WolfPeltCloakLvl1AlbQuest)))
                return;

            if (e == GamePlayerEvent.AcceptQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x01);
            else if (e == GamePlayerEvent.DeclineQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x00);
        }

        protected static void TalkToSeamstressLynnet(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
            if (player == null)
                return;

            if (_stewardWillie.CanGiveQuest(typeof(WolfPeltCloakLvl1AlbQuest), player) <= 0)
                return;

            WolfPeltCloakLvl1AlbQuest quest = player.IsDoingQuest(typeof(WolfPeltCloakLvl1AlbQuest)) as WolfPeltCloakLvl1AlbQuest;

            _lynett.TurnTo(player);
            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {             
                    _lynett.SayTo(player, "I hear you have a token for me, as proof of your valuable work for his Lordship. Give it to me and I will reward you.");
                }
            }
        }
        
        protected static void TalkToBrotherDon(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
            if (player == null)
                return;

            if (e == GameObjectEvent.Interact)
            {
                if (player.Inventory.GetFirstItemByID(_wolfPeltCloak.Id_nb, EInventorySlot.FirstBackpack, EInventorySlot.LastBackpack) != null)
                    _don.SayTo(player, "Hail! You don't perhaps have one of those fine wolf pelt cloaks? If you no longer have need of it, we could greatly use it at the [orphanage].");
                return;
            }
            else if (e == GameLivingEvent.WhisperReceive)
            {
                WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs)args;
                switch (wArgs.Text)
                {
                    case "orphanage":
                        _don.SayTo(player, "Why yes, the little ones can get an awful chill during the long cold nights, so the orphanage could use a good [donation] of wolf cloaks. I would take any that you have.");
                        break;
                    case "donation":
                        _don.SayTo(player, "Do you want to donate your cloak?");
                        break;
                }
            }
        }

        public override bool CheckQuestQualification(GamePlayer player)
        {            
            if (player.IsDoingQuest(typeof(WolfPeltCloakLvl1AlbQuest)) != null)
                return true;
                        
            if (player.Level < MIN_LEVEL || player.Level > MAX_LEVEL)
                return false;

            return true;
        }
        private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
        {
            WolfPeltCloakLvl1AlbQuest quest = player.IsDoingQuest(typeof(WolfPeltCloakLvl1AlbQuest)) as WolfPeltCloakLvl1AlbQuest;

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
            if (_stewardWillie.CanGiveQuest(typeof(WolfPeltCloakLvl1AlbQuest), player) <= 0)
                return;

            if (player.IsDoingQuest(typeof(WolfPeltCloakLvl1AlbQuest)) != null)
                return;

            if (response == 0x00)
            {
                SendReply(player, "Oh well, if you change your mind, please come back!");
            }
            else
            {
                if (!_stewardWillie.GiveQuest(typeof(WolfPeltCloakLvl1AlbQuest), player, 1))
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

        public override void Notify(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

            if (player == null)
                return;

            // brother don
            if (e == GamePlayerEvent.GiveItem)
            {
                GiveItemEventArgs gArgs = (GiveItemEventArgs)args;
                if (gArgs.Target.Name == _don.Name && gArgs.Item.Id_nb == _wolfPeltCloak.Id_nb)
                {
                    _don.SayTo(player, "Thank you! Your service to the church will been noted!");
                    RemoveItem(_don, m_questPlayer, _wolfPeltCloak);
                    _don.SayTo(player, "Well done! You've helped the children get over the harsh winter.");

                    player.ForceGainExperience(200);

                    return;
                }
            }

            if (player.IsDoingQuest(typeof(WolfPeltCloakLvl1AlbQuest)) == null)
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

            m_questPlayer.GainExperience(EXpSource.Quest, 50, true);
            long money = MoneyMgr.GetMoney(0, 0, 0, 0, 50);
            m_questPlayer.AddMoney(money, "You recieve {0} for your service.");
            InventoryLogging.LogInventoryAction("(QUEST;" + Name + ")", m_questPlayer, EInventoryActionType.Quest, money);

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
