using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS.Quests;
using log4net;

namespace Core.GS.WeeklyQuest.Hibernia
{
    public class WeeklyGalladoriaBossLvl45HibQuest : Quests.WeeklyQuest
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string questTitle = "[Weekly] Harder Adversaries";
        private const int minimumLevel = 45;
        private const int maximumLevel = 50;

        // Kill Goal
        private int _deadGallaBossMob = 0;
        private const int MAX_KILLGOAL = 3;

        private static GameNpc Anthony = null; // Start NPC


        // Constructors
        public WeeklyGalladoriaBossLvl45HibQuest() : base()
        {
        }

        public WeeklyGalladoriaBossLvl45HibQuest(GamePlayer questingPlayer) : base(questingPlayer, 1)
        {
        }

        public WeeklyGalladoriaBossLvl45HibQuest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
        {
        }

        public WeeklyGalladoriaBossLvl45HibQuest(GamePlayer questingPlayer, DbQuest dbQuest) : base(questingPlayer, dbQuest)
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
            if (!ServerProperties.Properties.LOAD_QUESTS)
                return;


            #region defineNPCs

            GameNpc[] npcs = WorldMgr.GetNPCsByName("Anthony", ERealm.Hibernia);

            if (npcs.Length > 0)
                foreach (GameNpc npc in npcs)
                    if (npc.CurrentRegionID == 181 && npc.X == 422864 && npc.Y == 444362)
                    {
                        Anthony = npc;
                        break;
                    }

            if (Anthony == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not find Anthony , creating it ...");
                Anthony = new GameNpc();
                Anthony.Model = 289;
                Anthony.Name = "Anthony";
                Anthony.GuildName = "Advisor to the King";
                Anthony.Realm = ERealm.Hibernia;
                //Domnann Location
                Anthony.CurrentRegionID = 181;
                Anthony.Size = 50;
                Anthony.Level = 59;
                Anthony.X = 422864;
                Anthony.Y = 444362;
                Anthony.Z = 5952;
                Anthony.Heading = 1234;
                GameNpcInventoryTemplate templateHib = new GameNpcInventoryTemplate();
                templateHib.AddNPCEquipment(EInventorySlot.TorsoArmor, 1008);
                templateHib.AddNPCEquipment(EInventorySlot.HandsArmor, 361);
                templateHib.AddNPCEquipment(EInventorySlot.FeetArmor, 362);
                Anthony.Inventory = templateHib.CloseTemplate();
                Anthony.AddToWorld();
                if (SAVE_INTO_DATABASE)
                {
                    Anthony.SaveIntoDatabase();
                }
            }

            #endregion

            #region defineItems

            #endregion

            #region defineObject

            #endregion

            GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

            GameEventMgr.AddHandler(Anthony, GameObjectEvent.Interact, new CoreEventHandler(TalkToAnthony));
            GameEventMgr.AddHandler(Anthony, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToAnthony));

            Anthony.AddQuestToGive(typeof(WeeklyGalladoriaBossLvl45HibQuest));

            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" Hib initialized");
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
        {
            //if not loaded, don't worry
            if (Anthony == null)
                return;
            // remove handlers
            GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

            GameEventMgr.RemoveHandler(Anthony, GameObjectEvent.Interact, new CoreEventHandler(TalkToAnthony));
            GameEventMgr.RemoveHandler(Anthony, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToAnthony));

            Anthony.RemoveQuestToGive(typeof(WeeklyGalladoriaBossLvl45HibQuest));
        }

        private static void TalkToAnthony(CoreEvent e, object sender, EventArgs args)
        {
            //We get the player from the event arguments and check if he qualifies		
            GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
            if (player == null)
                return;

            if (Anthony.CanGiveQuest(typeof(WeeklyGalladoriaBossLvl45HibQuest), player) <= 0)
                return;

            //We also check if the player is already doing the quest
            WeeklyGalladoriaBossLvl45HibQuest quest = player.IsDoingQuest(typeof(WeeklyGalladoriaBossLvl45HibQuest)) as WeeklyGalladoriaBossLvl45HibQuest;

            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {
                    switch (quest.Step)
                    {
                        case 1:
                            Anthony.SayTo(player,
                                "Please, enter Galladoria and slay strong opponents. If you succeed come back for your reward.");
                            break;
                        case 2:
                            Anthony.SayTo(player, "Hello " + player.Name + ", did you [succeed]?");
                            break;
                    }
                }
                else
                {
                    Anthony.SayTo(player, "Hello " + player.Name + ", I am Anthony. " +
                                       "A nightshade has reported the forces in Galladoria are planning an attack. \n" +
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
                            player.Out.SendQuestSubscribeCommand(Anthony,
                                QuestMgr.GetIDForQuestType(typeof(WeeklyGalladoriaBossLvl45HibQuest)),
                                "Will you help Anthony " + questTitle + "");
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
            if (player.IsDoingQuest(typeof(WeeklyGalladoriaBossLvl45HibQuest)) != null)
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
            WeeklyGalladoriaBossLvl45HibQuest quest = player.IsDoingQuest(typeof(WeeklyGalladoriaBossLvl45HibQuest)) as WeeklyGalladoriaBossLvl45HibQuest;

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

        private static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
        {
            QuestEventArgs qargs = args as QuestEventArgs;
            if (qargs == null)
                return;

            if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(WeeklyGalladoriaBossLvl45HibQuest)))
                return;

            if (e == GamePlayerEvent.AcceptQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x01);
            else if (e == GamePlayerEvent.DeclineQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x00);
        }

        private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
        {
            if (Anthony.CanGiveQuest(typeof(WeeklyGalladoriaBossLvl45HibQuest), player) <= 0)
                return;

            if (player.IsDoingQuest(typeof(WeeklyGalladoriaBossLvl45HibQuest)) != null)
                return;

            if (response == 0x00)
            {
                player.Out.SendMessage("Thank you for your help.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
            }
            else
            {
                //Check if we can add the quest!
                if (!Anthony.GiveQuest(typeof(WeeklyGalladoriaBossLvl45HibQuest), player, 1))
                    return;

                Anthony.SayTo(player, "Thank you " + player.Name + ", be an enrichment for our realm!");
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
                        return "Find a way to Galladoria and kill strong opponents. \nKilled: Bosses in Galladoria (" +
                               _deadGallaBossMob + " | "+ MAX_KILLGOAL +")";
                    case 2:
                        return "Return to Anthony in Grove of Domnann for your Reward.";
                }

                return base.Description;
            }
        }

        public override void Notify(CoreEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

            if (player?.IsDoingQuest(typeof(WeeklyGalladoriaBossLvl45HibQuest)) == null)
                return;

            if (sender != m_questPlayer)
                return;

            if (Step != 1 || e != GameLivingEvent.EnemyKilled) return;
            EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
                
            // check if a GameEpicBoss died + if its in Galladoria
            if (gArgs.Target.Realm == 0 && gArgs.Target is GameEpicBoss && gArgs.Target.CurrentRegionID == 191)
            {
                _deadGallaBossMob++;
                player.Out.SendMessage(
                    "[Weekly] Bosses killed in Galladoria: (" + _deadGallaBossMob + " | " + MAX_KILLGOAL + ")",
                    EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);
                player.Out.SendQuestUpdate(this);

                if (_deadGallaBossMob >= MAX_KILLGOAL)
                {
                    Step = 2;
                }
            }
        }

        public override string QuestPropertyKey
        {
            get => "GalladoriaBossQuestHib";
            set { ; }
        }

        public override void LoadQuestParameters()
        {
            _deadGallaBossMob = GetCustomProperty(QuestPropertyKey) != null ? int.Parse(GetCustomProperty(QuestPropertyKey)) : 0;
        }

        public override void SaveQuestParameters()
        {
            SetCustomProperty(QuestPropertyKey, _deadGallaBossMob.ToString());
        }

        public override void FinishQuest()
        {
            m_questPlayer.ForceGainExperience((m_questPlayer.ExperienceForNextLevel - m_questPlayer.ExperienceForCurrentLevel));
            m_questPlayer.AddMoney(MoneyMgr.GetMoney(0, 0, m_questPlayer.Level * 5, 0, Util.Random(50)), "You receive {0} as a reward.");
            CoreRoGMgr.GenerateReward(m_questPlayer, 1500);
            _deadGallaBossMob = 0;
            base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
        }
    }
}