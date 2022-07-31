/*
 * Atlas Custom Quest - Atlas 1.65v Classic Freeshard
 */
/*
*Author         : Kelt
*Editor			: Kelt
*Source         : Custom
*Date           : 03 July 2022
*Quest Name     : [Memorial] Power of Nature
*Quest Classes  : all
*Quest Version  : v1.0
*
*Changes:
* 
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.PlayerTitles;
using log4net;

namespace DOL.GS.Quests.Hibernia
{
    public class PowerOfNature : BaseQuest
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected const string questTitle = "[Memorial] Power of Nature";
        protected const int minimumLevel = 1;
        protected const int maximumLevel = 50;

        private static GameNPC Theresa = null; // Start + Finish NPC
        private static GameNPC Karl = null; // Speak with Karl
        private static GameNPC MobEffect = null; // Speak with Karl

        private static ItemTemplate theresas_doll = null;
        private static ItemTemplate magical_theresas_doll = null;
        // Constructors
        public PowerOfNature() : base()
        {
        }

        public PowerOfNature(GamePlayer questingPlayer) : base(questingPlayer)
        {
        }

        public PowerOfNature(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
        {
        }

        public PowerOfNature(GamePlayer questingPlayer, DBQuest dbQuest) : base(questingPlayer, dbQuest)
        {
        }


        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            if (!ServerProperties.Properties.LOAD_QUESTS)
                return;


            #region defineNPCs

            GameNPC[] npcs = WorldMgr.GetNPCsByName("Theresa", eRealm.Hibernia);

            if (npcs.Length > 0)
                foreach (GameNPC npc in npcs)
                    if (npc.CurrentRegionID == 201 && npc.X == 31401 && npc.Y == 30076)
                    {
                        Theresa = npc;
                        break;
                    }

            if (Theresa == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not find Theresa, creating it ...");
                Theresa = new GameNPC();
                Theresa.Model = 310;
                Theresa.Name = "Theresa";
                Theresa.GuildName = "";
                Theresa.Realm = eRealm.Hibernia;
                Theresa.CurrentRegionID = 201;
                Theresa.LoadEquipmentTemplateFromDatabase("Theresa");
                Theresa.Size = 48;
                Theresa.Level = 50;
                Theresa.X = 31401;
                Theresa.Y = 30076;
                Theresa.Z = 8011;
                Theresa.Heading = 1505;
                Theresa.AddToWorld();
                if (SAVE_INTO_DATABASE)
                    Theresa.SaveIntoDatabase();
            }

            npcs = WorldMgr.GetNPCsByName("Karl", eRealm.Hibernia);

            if (npcs.Length > 0)
                foreach (GameNPC npc in npcs)
                    if (npc.CurrentRegionID == 200 && npc.X == 328521 && npc.Y == 518534)
                    {
                        Karl = npc;
                        break;
                    }

            if (Karl == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not find Karl, creating it ...");
                Karl = new GameNPC();
                Karl.Model = 956;
                Karl.Name = "Karl";
                Karl.GuildName = "";
                Karl.Realm = eRealm.Hibernia;
                Karl.CurrentRegionID = 200;
                Karl.LoadEquipmentTemplateFromDatabase("Karl");
                Karl.Size = 50;
                Karl.Level = 50;
                Karl.X = 328521;
                Karl.Y = 518534;
                Karl.Z = 4285;
                Karl.Heading = 2612;
                Karl.AddToWorld();
                if (SAVE_INTO_DATABASE)
                    Karl.SaveIntoDatabase();
            }

            // end npc

            #endregion

            #region defineItems

            theresas_doll = GameServer.Database.FindObjectByKey<ItemTemplate>("theresas_doll");
            if (theresas_doll == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not find Theresa's doll, creating it ...");
                theresas_doll = new ItemTemplate();
                theresas_doll.Id_nb = "theresas_doll";
                theresas_doll.Name = "Theresa's doll";
                theresas_doll.Level = 5;
                theresas_doll.Item_Type = 0;
                theresas_doll.Model = 1879;
                theresas_doll.IsDropable = false;
                theresas_doll.IsTradable = false;
                theresas_doll.IsIndestructible = false;
                theresas_doll.IsPickable = false;
                theresas_doll.DPS_AF = 0;
                theresas_doll.SPD_ABS = 0;
                theresas_doll.Object_Type = 0;
                theresas_doll.Hand = 0;
                theresas_doll.Type_Damage = 0;
                theresas_doll.Quality = 100;
                theresas_doll.Weight = 1;
                theresas_doll.Description = "This doll was a present of Karl the Hero of Hibernia.";
                if (SAVE_INTO_DATABASE)
                    GameServer.Database.AddObject(theresas_doll);
            }

            magical_theresas_doll = GameServer.Database.FindObjectByKey<ItemTemplate>("magical_theresas_doll");
            if (magical_theresas_doll == null)
            {
                if (log.IsWarnEnabled)
                    log.Warn("Could not find Magical Theresa's doll, creating it ...");
                magical_theresas_doll = new ItemTemplate();
                magical_theresas_doll.Id_nb = "magical_theresas_doll";
                magical_theresas_doll.Name = "Theresa's magical doll";
                magical_theresas_doll.Level = 50;
                magical_theresas_doll.Item_Type = 0;
                magical_theresas_doll.Model = 1879;
                magical_theresas_doll.IsDropable = false;
                magical_theresas_doll.IsTradable = false;
                magical_theresas_doll.IsIndestructible = false;
                magical_theresas_doll.IsPickable = false;
                magical_theresas_doll.DPS_AF = 0;
                magical_theresas_doll.SPD_ABS = 0;
                magical_theresas_doll.Object_Type = 0;
                magical_theresas_doll.Hand = 0;
                magical_theresas_doll.Type_Damage = 0;
                magical_theresas_doll.Quality = 100;
                magical_theresas_doll.Weight = 1;
                magical_theresas_doll.Description = "This magical doll from Karl is a sign of love and is a present for Theresa.";
                if (SAVE_INTO_DATABASE)
                    GameServer.Database.AddObject(theresas_doll);
            }
            #endregion

            GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

            GameEventMgr.AddHandler(Theresa, GameObjectEvent.Interact, new DOLEventHandler(TalkToTheresa));
            GameEventMgr.AddHandler(Theresa, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToTheresa));

            GameEventMgr.AddHandler(Karl, GameObjectEvent.Interact, new DOLEventHandler(TalkToKarl));
            GameEventMgr.AddHandler(Karl, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToKarl));

            Theresa.AddQuestToGive(typeof(PowerOfNature));

            if (log.IsInfoEnabled)
                log.Info("Quest \"" + questTitle + "\" initialized");
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            //if not loaded, don't worry
            if (Theresa == null)
                return;

            // remove handlers
            GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
            GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

            GameEventMgr.RemoveHandler(Theresa, GameObjectEvent.Interact, new DOLEventHandler(TalkToTheresa));
            GameEventMgr.RemoveHandler(Theresa, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToTheresa));

            GameEventMgr.RemoveHandler(Karl, GameObjectEvent.Interact, new DOLEventHandler(TalkToKarl));
            GameEventMgr.RemoveHandler(Karl, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToKarl));

            Theresa.RemoveQuestToGive(typeof(PowerOfNature));
        }

        protected static void TalkToTheresa(DOLEvent e, object sender, EventArgs args)
        {
            //We get the player from the event arguments and check if he qualifies		
            GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
            if (player == null)
                return;

            if (Theresa.CanGiveQuest(typeof(PowerOfNature), player) <= 0)
                return;

            //We also check if the player is already doing the quest
            PowerOfNature quest = player.IsDoingQuest(typeof(PowerOfNature)) as PowerOfNature;

            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {
                    switch (quest.Step)
                    {
                        case 1:
                            Theresa.SayTo(player,
                                $"Greetings, {player.Name}, I don't know what to say. Thank you very much for helping me. I will give you some [information] about him now.");
                            break;
                        case 2:
                            Theresa.SayTo(player,
                                $"{player.Name}, exit the East Entrance to Lough Derg and move south to the little lake. I hope you will find my father there.");
                            break;
                        case 3:
                            Theresa.SayTo(player, $"Hello {player.Name}, you found my father? What did he [say]?");
                            break;
                        case 4:
                            Theresa.SayTo(player,
                                "Thank you so much, I've never met a person as kind as you. You helped me more than you realize, and I want to reward you with some silver. You told me something about [Power of Nature], what does that mean?");
                            break;
                    }
                }
                else
                {
                    Theresa.SayTo(player,
                        $"Hello {player.CharacterClass.Name}. For many years there has been war in our areas and I am afraid that those days will come back. " +
                        $"My father hasn't been to Tir na Nog since before the wars. I miss him dearly, and I hope he's doing well. Could you [help me] to find him?");
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
                        case "help me":
                            player.Out.SendQuestSubscribeCommand(Theresa,
                                QuestMgr.GetIDForQuestType(typeof(PowerOfNature)),
                                "Will you help Theresa to find her father? [Memorial] Power of Nature");
                            break;
                    }
                }
                else
                {
                    switch (wArgs.Text)
                    {
                        case "information":
                            Theresa.SayTo(player,
                                "Karl the fighter, the defender, the honorable. My father is an amazing person. " +
                                "When I was younger, he always brought me things from his travels. " +
                                "I still have them to this day and will never lose them! As I got older, the trips got longer and I started to miss him more. " +
                                "However, my mother suffered even more than I. She fell sick and needed him... and now she is gone, and he has not been to Tir na Nog for several years. We all [needed him].");
                            break;
                        case "needed him":
                            Theresa.SayTo(player,
                                "When I was a kid we used to walk to the little lake in Lough Derg and look at the trees and the bugs, sometimes for several hours. I loved it. " +
                                "I always kept a [toy] with me on the way, which my father gave me from his travels. " +
                                "It would be nice if you could go to this lake. Maybe he is there, that would be my greatest hope.");
                            break;
                        case "toy":
                            Theresa.SayTo(player,
                                "(She pauses for a moment) I want to give you this toy to take it with you on your way to the lake. If you meet him, give him this as a sign of love. I will never forget him!");
                            if (quest.Step == 1 && player.Inventory.IsSlotsFree(1, eInventorySlot.FirstBackpack,
                                    eInventorySlot.LastBackpack))
                            {
                                GiveItem(player, theresas_doll);
                                quest.Step = 2;
                            }
                            else
                            {
                                Theresa.SayTo(player,
                                    "Oh you have too much in your inventory. Come back when you can hold this [toy].");
                            }

                            break;
                        case "say":
                            if (quest.Step == 3)
                            {
                                Theresa.SayTo(player,
                                    "I am so glad that I sent you. Knowing that he is still alive and healthy gives me peace and strength.");
                            }
                            break;
                        case "Power of Nature":
                            if (quest.Step == 4)
                            {
                                Theresa.SayTo(player,
                                    "I see now. The small lake in Lough Derg is the source of [natural powers]. Now I understand why we spent so much time there in my youth.");
                            }
                            break;
                        case "natural powers":
                            if (quest.Step == 4)
                            {
                                RemoveItem(player, magical_theresas_doll);
                                Theresa.SayTo(player,
                                "Father imbued this doll with magic, and I can feel his presence within it. I am confident he will return soon, and I can go with peace now. Thank you so much for bringing my father back to me, and to all of us in Tir na Nog. ");
                                Theresa.Emote(eEmote.Cheer);
                                new ECSGameTimer(Theresa, new ECSGameTimer.ECSTimerCallback(StartTheresaEffect), 2000);
                                Theresa.SayTo(player,
                                    "I can feel the power of nature!");
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

        protected static void TalkToKarl(DOLEvent e, object sender, EventArgs args)
        {
            //We get the player from the event arguments and check if he qualifies		
            GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
            if (player == null)
                return;

            //We also check if the player is already doing the quest
            PowerOfNature quest = player.IsDoingQuest(typeof(PowerOfNature)) as PowerOfNature;

            if (e == GameObjectEvent.Interact)
            {
                if (quest != null)
                {
                    switch (quest.Step)
                    {
                        case 1:
                            Karl.SayTo(player, "Hello Adventurer, I hope you have a great day.");
                            break;
                        case 2:
                            Karl.SayTo(player,
                                $"Hello {player.CharacterClass.Name}, you are very brave to come here. This lake swarms with monsters and vermin, but I am happy for every visitor to [this place].");
                            break;
                        case 3:
                            Karl.SayTo(player,
                                $"{player.Name}, if you are ready then we can [begin] with the ceremony.");
                            break;
                        case 4:
                            Karl.SayTo(player,
                                $"Okay, now bring this magical doll to my daughter in Tir na Nog and tell her about the Power of Nature." +
                                $"I will come back soon and will stay by her side. \nThank you {player.Name}, that you visited me, I really appreciated that!");
                            break;
                    }
                }
                else
                {
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
                    }
                }
                else
                {
                    switch (wArgs.Text)
                    {
                        case "this place":
                            Karl.SayTo(player,
                                "This place is very special to me, it's not just a retreat. Here I feel the nature that blossoms in all of Hibernia. " +
                                "I wanted to bring this power and this life closer to everybody who comes here. " +
                                "I wanted to show this to [my daughter] too, but she was too young to travel here and her mother used to worry.");
                            break;
                        case "my daughter":
                            Karl.SayTo(player,
                                $"That [doll] peeking out of your backpack, I remember it. I brought it back from a tour in Hadrian's Wall many years ago and gave it to my daughter. Why do you have it? (He eyes you suspiciously)");
                            break;
                        case "doll":
                            Karl.SayTo(player,
                                $"(You hand the doll to him and tell him of Theresa. Tears begin to form in his eyes) I am so sorry for all these years... I heard about my wife passing and it broke me. Theresa needed me more than ever, and I was not there. " +
                                $"Please give me the doll. I will show her the [strength and aura] of all forces of nature, and give her proof that I'm still alive.");
                            break;
                        case "strength and aura":
                            if (quest.Step == 2)
                            {
                                RemoveItem(player, theresas_doll);
                                Karl.SayTo(player,
                                    $"Thank you {player.Name}, if you are ready then we can [begin] with the ceremony.");
                                quest.Step = 3;
                            }

                            break;
                        case "begin":
                            if (quest.Step == 3)
                            {
                                new ECSGameTimer(Karl, new ECSGameTimer.ECSTimerCallback(CreateEffect), 3000);
                                
                                new ECSGameTimer(Karl, new ECSGameTimer.ECSTimerCallback(timer => StartEffectPlayer(timer, player)), 1000);
                                quest.Step = 4;
                                GiveItem(player, magical_theresas_doll);
                                Karl.SayTo(player,
                                    $"Okay, now bring this magical doll to my daughter in Tir na Nog and tell her about the Power of Nature. " +
                                    $"I will return soon and stay by her side. \nThank you {player.Name}, for visiting me. Truly!");
                            }

                            break;
                    }
                }
            }
        }

        private static int CreateEffect(ECSGameTimer timer)
        {
            // dont change it
            int effectCount = 5;
            for (int i = 0; i <= effectCount; i++)
            {
                MobEffect = new GameNPC();
                MobEffect.Model = 1;
                MobEffect.Name = "power of nature";
                MobEffect.GuildName = "";
                MobEffect.Realm = eRealm.Hibernia;
                MobEffect.Race = 2007;
                MobEffect.BodyType = (ushort) NpcTemplateMgr.eBodyType.Magical;
                MobEffect.Size = 100;
                MobEffect.Level = 65;
                MobEffect.Flags ^= GameNPC.eFlags.CANTTARGET;
                MobEffect.Flags ^= GameNPC.eFlags.DONTSHOWNAME;
                MobEffect.Flags ^= GameNPC.eFlags.PEACE;
                switch (i)
                {
                    case 0:
                        MobEffect.CurrentRegionID = 200;
                        MobEffect.X = 328403;
                        MobEffect.Y = 518387;
                        MobEffect.Z = 4273;
                        break;
                    case 1:
                        MobEffect.CurrentRegionID = 200;
                        MobEffect.X = 328293;
                        MobEffect.Y = 518506;
                        MobEffect.Z = 4366;
                        break;
                    case 2:
                        MobEffect.CurrentRegionID = 200;
                        MobEffect.X = 328323;
                        MobEffect.Y = 518693;
                        MobEffect.Z = 4416;
                        break;
                    case 3:
                        MobEffect.CurrentRegionID = 200;
                        MobEffect.X = 328482;
                        MobEffect.Y = 518752;
                        MobEffect.Z = 4364;
                        break;
                    case 4:
                        MobEffect.CurrentRegionID = 200;
                        MobEffect.X = 328612;
                        MobEffect.Y = 518663;
                        MobEffect.Z = 4274;
                        break;
                    case 5:
                        MobEffect.CurrentRegionID = 200;
                        MobEffect.X = 328700;
                        MobEffect.Y = 518409;
                        MobEffect.Z = 4233;
                        break;
                }
                MobEffect.AddToWorld();

                var brain = new StandardMobBrain();
                brain.AggroLevel = 200;
                brain.AggroRange = 500;
                MobEffect.SetOwnBrain(brain);

                MobEffect.AddToWorld();
            }

            new ECSGameTimer(Karl, new ECSGameTimer.ECSTimerCallback(StartEffect), 1000);
            
            new ECSGameTimer(Karl, new ECSGameTimer.ECSTimerCallback(StartEffect), 1000);
            
            return 0;
        }

        private static int StartEffect(ECSGameTimer timer)
        {
            foreach (GameNPC effect in Karl.GetNPCsInRadius(600))
            {
                if (effect.Name.ToLower() == "power of nature")
                {
                    effect.CastSpell(EffectSpell, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
            }
            return 0;
        }

        private static int StartTheresaEffect(ECSGameTimer timer)
        {
            Theresa.CastSpell(EffectSpell, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            return 0;
        }

        private static int StartEffectPlayer(ECSGameTimer timer, GamePlayer player)
        {
            player.Out.SendSpellEffectAnimation(player, player, 5005, 0, false, 1);
            
            RemoveEffectMob();
            return 0;
        }
        
        private static void RemoveEffectMob()
        {
            foreach (GameNPC effect in Karl.GetNPCsInRadius(600))
            {
                if (effect.Name.ToLower() == "power of nature")
                    effect.RemoveFromWorld();
            }
        }

        #region EffectSpell
        private static Spell m_effect;
        /// <summary>
        /// The Health Regen Song.
        /// </summary>
        protected static Spell EffectSpell
        {
            get
            {
                if (m_effect == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Icon = 5005;
                    spell.ClientEffect = 5005;
                    spell.Damage = 0;
                    spell.Duration = 1;
                    spell.Name = "Power of Nature";
                    spell.Range = 0;
                    spell.Radius = 0;
                    spell.SpellID = 5005;
                    spell.Target = "Self";
                    spell.Type = "StrengthBuff";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = 0;
                    spell.Message1 = "The power of nature surrounds you.";
                    m_effect = new Spell(spell, 1);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_effect);
                }
                return m_effect;
            }
        }
        #endregion
        
        public override bool CheckQuestQualification(GamePlayer player)
        {
            // if the player is already doing the quest his level is no longer of relevance
            if (player.IsDoingQuest(typeof(PowerOfNature)) != null)
                return true;

            if (player.Level < minimumLevel || player.Level > maximumLevel)
                return false;

            return true;
        }

        private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
        {
            PowerOfNature quest = player.IsDoingQuest(typeof(PowerOfNature)) as PowerOfNature;

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

            if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(PowerOfNature)))
                return;

            if (e == GamePlayerEvent.AcceptQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x01);
            else if (e == GamePlayerEvent.DeclineQuest)
                CheckPlayerAcceptQuest(qargs.Player, 0x00);
        }

        private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
        {
            if (Theresa.CanGiveQuest(typeof(PowerOfNature), player) <= 0)
                return;

            if (player.IsDoingQuest(typeof(PowerOfNature)) != null)
                return;

            if (response == 0x00)
            {
                Theresa.SayTo(player,
                    $"Dont worry, thanks for listening to me, even that helped me a lot. Come back if you want to [help me].");
            }
            else
            {
                //Check if we can add the quest!
                if (!Theresa.GiveQuest(typeof(PowerOfNature), player, 1))
                    return;

                Theresa.SayTo(player,
                    $"Thank you very much, i don't know what to say. I will give you some [information] about him now.");
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
                        return "Continue speaking with Theresa and get more information about Karl.";
                    case 2:
                        return "Travel to the little lake in Lough Derg and search for Karl.";
                    case 3:
                        return "Speak with Karl and help him with the ceremony.";
                    case 4:
                        return "Return to Theresa with Karl's present.";
                }

                return base.Description;
            }
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

            if (player == null || player.IsDoingQuest(typeof(PowerOfNature)) == null)
                return;
        }

        public class PowerOfNatureTitle : EventPlayerTitle
        {
            /// <summary>
            /// The title description, shown in "Titles" window.
            /// </summary>
            /// <param name="player">The title owner.</param>
            /// <returns>The title description.</returns>
            public override string GetDescription(GamePlayer player)
            {
                return "Protected by Nature";
            }

            /// <summary>
            /// The title value, shown over player's head.
            /// </summary>
            /// <param name="source">The player looking.</param>
            /// <param name="player">The title owner.</param>
            /// <returns>The title value.</returns>
            public override string GetValue(GamePlayer source, GamePlayer player)
            {
                return "Protected by Nature";
            }

            /// <summary>
            /// The event to hook.
            /// </summary>
            public override DOLEvent Event
            {
                get { return GamePlayerEvent.GameEntered; }
            }

            /// <summary>
            /// Verify whether the player is suitable for this title.
            /// </summary>
            /// <param name="player">The player to check.</param>
            /// <returns>true if the player is suitable for this title.</returns>
            public override bool IsSuitable(GamePlayer player)
            {
                return player.HasFinishedQuest(typeof(PowerOfNature)) == 1;
            }

            /// <summary>
            /// The event callback.
            /// </summary>
            /// <param name="e">The event fired.</param>
            /// <param name="sender">The event sender.</param>
            /// <param name="arguments">The event arguments.</param>
            protected override void EventCallback(DOLEvent e, object sender, EventArgs arguments)
            {
                GamePlayer p = sender as GamePlayer;
                if (p != null && p.Titles.Contains(this))
                {
                    p.UpdateCurrentTitle();
                    return;
                }

                base.EventCallback(e, sender, arguments);
            }
        }

        public override void AbortQuest()
        {
            base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...
        }

        public override void FinishQuest()
        {
            m_questPlayer.GainExperience(eXPSource.Quest, 20, false);
            m_questPlayer.AddMoney(Money.GetMoney(0, 0, 1, 32, Util.Random(50)), "You receive {0} as a reward.");

            base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...
        }
    }
}