using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.API;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using DOL.GS.Quests;
using log4net;

namespace DOL.GS.DailyQuest.Midgard
{
    public class TuscarianBossQuestMid : WeeklyQuest
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected const string questTitle = "[Weekly] Harder Adversaries";
        protected const int minimumLevel = 45;
        protected const int maximumLevel = 50;

        // Kill Goal
        private int _deadTuscaBossMob = 0;
        protected const int MAX_KILLGOAL = 3;

        private static GameNPC Isaac = null; // Start NPC


        // Constructors
        public TuscarianBossQuestMid() : base()
        {
        }

        public TuscarianBossQuestMid(GamePlayer questingPlayer) : base(questingPlayer, 1)
        {
        }

        public TuscarianBossQuestMid(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
        {
        }

        public TuscarianBossQuestMid(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (!ServerProperties.Properties.LOAD_QUESTS)
                return;


            #region defineNPCs

            GameNPC[] npcs = WorldMgr.GetNPCsByName("Isaac", eRealm.Midgard);

            if (npcs.Length > 0)
                foreach (GameNPC npc in npcs)
                    if (npc.CurrentRegionID == 100 && npc.X == 766590 && npc.Y == 670407)
                    {
                        Isaac = npc;
                        break;
                    }

            if (Isaac == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not find Isaac , creating it ...");
                Isaac = new GameNPC();
                Isaac.Model = 774;
                Isaac.Name = "Isaac";
                Isaac.GuildName = "Advisor to the King";
                Isaac.Realm = eRealm.Midgard;
                Isaac.CurrentRegionID = 100;
                Isaac.Size = 50;
                Isaac.Level = 59;
                //Castle Sauvage Location
                Isaac.X = 766590;
                Isaac.Y = 670407;
                Isaac.Z = 5736;
                Isaac.Heading = 2358;
                Isaac.AddToWorld();
                if (SAVE_INTO_DATABASE)
                {
                    Isaac.SaveIntoDatabase();
                }
            }

            #endregion

            #region defineItems

            #endregion

            #region defineObject

            #endregion

            GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

            GameEventMgr.AddHandler(Isaac, GameObjectEvent.Interact, new DOLEventHandler(TalkToIsaac));
            GameEventMgr.AddHandler(Isaac, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToIsaac));

            /* Now we bring to Herou the possibility to give this quest to players */
            Isaac.AddQuestToGive(typeof(TuscarianBossQuestMid));

            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" Mid initialized");
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            //if not loaded, don't worry
            if (Isaac == null)
                return;
            // remove handlers
            GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

            GameEventMgr.RemoveHandler(Isaac, GameObjectEvent.Interact, new DOLEventHandler(TalkToIsaac));
            GameEventMgr.RemoveHandler(Isaac, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToIsaac));

            /* Now we remove to Herou the possibility to give this quest to players */
            Isaac.RemoveQuestToGive(typeof(TuscarianBossQuestMid));
        }

        protected static void TalkToIsaac(DOLEvent e, object sender, EventArgs args)
        {
            //We get the player from the event arguments and check if he qualifies		
            GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
            if (player == null)
                return;

            if (Isaac.CanGiveQuest(typeof(TuscarianBossQuestMid), player) <= 0)
                return;

            //We also check if the player is already doing the quest
            TuscarianBossQuestMid quest = player.IsDoingQuest(typeof(TuscarianBossQuestMid)) as TuscarianBossQuestMid;

            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {
                    switch (quest.Step)
                    {
                        case 1:
                            Isaac.SayTo(player,
                                "Please, enter Tuscaran Glacier and slay strong opponents. If you succeed come back for your reward.");
                            break;
                        case 2:
                            Isaac.SayTo(player, "Hello " + player.Name + ", did you [succeed]?");
                            break;
                    }
                }
                else
                {
                    Isaac.SayTo(player, "Hello " + player.Name + ", I am Isaac. " +
                                        "A shadowblade has reported the forces in Tuscaren Glacier are planning an attack. \n" +
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
                            player.Out.SendQuestSubscribeCommand(Isaac,
                                QuestMgr.GetIDForQuestType(typeof(TuscarianBossQuestMid)),
                                "Will you help Isaac " + questTitle + "");
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
                                player.Out.SendMessage("Thank you for your contribution!", eChatType.CT_Chat,
                                    eChatLoc.CL_PopupWindow);
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
            if (player.IsDoingQuest(typeof(TuscarianBossQuestMid)) != null)
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
            TuscarianBossQuestMid quest = player.IsDoingQuest(typeof(TuscarianBossQuestMid)) as TuscarianBossQuestMid;

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

            if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(TuscarianBossQuestMid)))
                return;

            if (e == GamePlayerEvent.AcceptQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x01);
            else if (e == GamePlayerEvent.DeclineQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x00);
        }

        private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
        {
            if (Isaac.CanGiveQuest(typeof(TuscarianBossQuestMid), player) <= 0)
                return;

            if (player.IsDoingQuest(typeof(TuscarianBossQuestMid)) != null)
                return;

            if (response == 0x00)
            {
                player.Out.SendMessage("Thank you for helping Midgard.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            }
            else
            {
                //Check if we can add the quest!
                if (!Isaac.GiveQuest(typeof(TuscarianBossQuestMid), player, 1))
                    return;

                Isaac.SayTo(player, "Thank you " + player.Name + ", be an enrichment for our realm!");
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
                        return "Find a way to Tuscaran Glacier and kill strong opponents. \nKilled: Bosses in Tuscaran Glacier (" +
                               _deadTuscaBossMob + " | "+ MAX_KILLGOAL +")";
                    case 2:
                        return "Return to Isaac for your Reward.";
                }

                return base.Description;
            }
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

            if (player == null || player.IsDoingQuest(typeof(TuscarianBossQuestMid)) == null)
                return;

            if (sender != m_questPlayer)
                return;

            if (Step == 1 && e == GameLivingEvent.EnemyKilled)
            {
                EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
                
                // check if a GameEpicBoss died + if its in Tuscaran Glacier
                if (gArgs.Target.Realm == 0 && gArgs.Target is GameEpicBoss && gArgs.Target.CurrentRegionID == 160)
                {
                    _deadTuscaBossMob++;
                    player.Out.SendMessage(
                        "[Weekly] Bosses killed in Tuscaran Glacier: (" + _deadTuscaBossMob + " | " + MAX_KILLGOAL + ")",
                        eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
                    player.Out.SendQuestUpdate(this);

                    if (_deadTuscaBossMob >= MAX_KILLGOAL)
                    {
                        // FinishQuest or go back to Herou
                        Step = 2;
                    }
                }
            }
        }

        public override string QuestPropertyKey
        {
            get => "TuscarianBossQuestMid";
            set { ; }
        }

        public override void LoadQuestParameters()
        {
            _deadTuscaBossMob = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
        }

        public override void SaveQuestParameters()
        {
            SetCustomProperty(QuestPropertyKey, _deadTuscaBossMob.ToString());
        }

        public override void AbortQuest()
        {
            base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
        }

        public override void FinishQuest()
        {
            m_questPlayer.GainExperience(eXPSource.Quest, (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel), false);
            m_questPlayer.AddMoney(Money.GetMoney(0, 0, m_questPlayer.Level * 5, 0, Util.Random(50)), "You receive {0} as a reward.");
            AtlasROGManager.GenerateOrbAmount(m_questPlayer, 1500);
            _deadTuscaBossMob = 0;
            base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
        }
    }
}