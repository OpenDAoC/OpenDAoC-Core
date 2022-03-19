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

namespace DOL.GS.DailyQuest.Albion
{
    public class SidiMobQuestAlb : Quests.DailyQuest
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected const string questTitle = "[Daily] Too many Monsters";
        protected const int minimumLevel = 45;
        protected const int maximumLevel = 50;

        // Kill Goal
        private int _deadSidiMob = 0;
        protected const int MAX_KILLGOAL = 3;

        private static GameNPC Haszan = null; // Start NPC


        // Constructors
        public SidiMobQuestAlb() : base()
        {
        }

        public SidiMobQuestAlb(GamePlayer questingPlayer) : base(questingPlayer, 1)
        {
        }

        public SidiMobQuestAlb(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
        {
        }

        public SidiMobQuestAlb(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
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

            GameNPC[] npcs = WorldMgr.GetNPCsByName("Haszan", eRealm.Albion);

            if (npcs.Length > 0)
                foreach (GameNPC npc in npcs)
                    if (npc.CurrentRegionID == 1 && npc.X == 583866 && npc.Y == 477497)
                    {
                        Haszan = npc;
                        break;
                    }

            if (Haszan == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not find Haszan , creating it ...");
                Haszan = new GameNPC();
                Haszan.Model = 51;
                Haszan.Name = "Haszan";
                Haszan.GuildName = "Atlas Quest";
                Haszan.Realm = eRealm.Albion;
                //Castle Sauvage Location
                Haszan.CurrentRegionID = 1;
                Haszan.Size = 50;
                Haszan.Level = 59;
                Haszan.X = 583866;
                Haszan.Y = 477497;
                Haszan.Z = 2600;
                Haszan.Heading = 3111;
                Haszan.AddToWorld();
                if (SAVE_INTO_DATABASE)
                {
                    Haszan.SaveIntoDatabase();
                }
            }

            #endregion

            #region defineItems

            #endregion

            #region defineObject

            #endregion

            GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

            GameEventMgr.AddHandler(Haszan, GameObjectEvent.Interact, new DOLEventHandler(TalkToHaszan));
            GameEventMgr.AddHandler(Haszan, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHaszan));

            /* Now we bring to Haszan the possibility to give this quest to players */
            Haszan.AddQuestToGive(typeof(SidiMobQuestAlb));

            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" Alb initialized");
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            //if not loaded, don't worry
            if (Haszan == null)
                return;
            // remove handlers
            GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

            GameEventMgr.RemoveHandler(Haszan, GameObjectEvent.Interact, new DOLEventHandler(TalkToHaszan));
            GameEventMgr.RemoveHandler(Haszan, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToHaszan));

            /* Now we remove to Haszan the possibility to give this quest to players */
            Haszan.RemoveQuestToGive(typeof(SidiMobQuestAlb));
        }

        protected static void TalkToHaszan(DOLEvent e, object sender, EventArgs args)
        {
            //We get the player from the event arguments and check if he qualifies		
            GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
            if (player == null)
                return;

            if (Haszan.CanGiveQuest(typeof(SidiMobQuestAlb), player) <= 0)
                return;

            //We also check if the player is already doing the quest
            SidiMobQuestAlb quest = player.IsDoingQuest(typeof(SidiMobQuestAlb)) as SidiMobQuestAlb;

            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {
                    switch (quest.Step)
                    {
                        case 1:
                            Haszan.SayTo(player,
                                "Please, enter Caer Sidi and slay some monsters. If you succeed come back for your reward.");
                            break;
                        case 2:
                            Haszan.SayTo(player, "Hello " + player.Name + ", did you [succeed]?");
                            break;
                    }
                }
                else
                {
                    Haszan.SayTo(player, "Hello " + player.Name + ", I am Haszan. " +
                                         "A scout reported strange wailings coming from inside Caer Sidi. \n" +
                                         "Do you think you could [investigate] for us?");
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
                        case "investigate":
                            player.Out.SendQuestSubscribeCommand(Haszan,
                                QuestMgr.GetIDForQuestType(typeof(SidiMobQuestAlb)),
                                "Will you help Haszan with " + questTitle + "");
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
            if (player.IsDoingQuest(typeof(SidiMobQuestAlb)) != null)
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
            SidiMobQuestAlb quest = player.IsDoingQuest(typeof(SidiMobQuestAlb)) as SidiMobQuestAlb;

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

            if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(SidiMobQuestAlb)))
                return;

            if (e == GamePlayerEvent.AcceptQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x01);
            else if (e == GamePlayerEvent.DeclineQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x00);
        }

        private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
        {
            if (Haszan.CanGiveQuest(typeof(SidiMobQuestAlb), player) <= 0)
                return;

            if (player.IsDoingQuest(typeof(SidiMobQuestAlb)) != null)
                return;

            if (response == 0x00)
            {
                player.Out.SendMessage("Thank you for helping Atlas.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
            }
            else
            {
                //Check if we can add the quest!
                if (!Haszan.GiveQuest(typeof(SidiMobQuestAlb), player, 1))
                    return;

                Haszan.SayTo(player, "Thank you " + player.Name + ", be an enrichment for our realm!");
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
                        return "Find a way to Caer Sidi and kill some monsters. \nKilled: Monsters in Caer Sidi (" +
                               _deadSidiMob + " | "+ MAX_KILLGOAL +")";
                    case 2:
                        return "Return to Haszan for your Reward.";
                }

                return base.Description;
            }
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

            if (player == null || player.IsDoingQuest(typeof(SidiMobQuestAlb)) == null)
                return;

            if (sender != m_questPlayer)
                return;

            if (Step == 1 && e == GameLivingEvent.EnemyKilled)
            {
                EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
                
                // check if a GameNPC died + if its in Caer sidi
                if (gArgs.Target.Realm == 0 && gArgs.Target is GameNPC && gArgs.Target.CurrentRegionID == 60)
                {
                    _deadSidiMob++;
                    player.Out.SendMessage(
                        "[Daily] Monsters killed in Caer Sidi: (" + _deadSidiMob + " | " + MAX_KILLGOAL + ")",
                        eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
                    player.Out.SendQuestUpdate(this);

                    if (_deadSidiMob >= MAX_KILLGOAL)
                    {
                        // FinishQuest or go back to Haszan
                        Step = 2;
                    }
                }
            }
        }

        public override string QuestPropertyKey
        {
            get => "SidiMobQuestAlb";
            set { ; }
        }

        public override void LoadQuestParameters()
        {
            _deadSidiMob = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
        }

        public override void SaveQuestParameters()
        {
            SetCustomProperty(QuestPropertyKey, _deadSidiMob.ToString());
        }

        public override void AbortQuest()
        {
            base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
        }

        public override void FinishQuest()
        {
            m_questPlayer.GainExperience(eXPSource.Quest,
                (m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel) / 5, true);
            m_questPlayer.AddMoney(Money.GetMoney(0, 0, m_questPlayer.Level, 0, Util.Random(50)), "You receive {0} as a reward.");
            AtlasROGManager.GenerateOrbAmount(m_questPlayer, 1000);
            _deadSidiMob = 0;
            base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
        }
    }
}