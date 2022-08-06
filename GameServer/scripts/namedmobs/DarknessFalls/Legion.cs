using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using log4net;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS.Scripts
{
    public class Legion : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static IArea legionArea = null;

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            const int radius = 650;
            Region region = WorldMgr.GetRegion(249);
            legionArea = region.AddArea(new Area.Circle("Legion's Lair", 45000, 51700, 15468, radius));
            log.Debug("Legion's Lair created with radius " + radius + " at 45000 51700 15468");
            legionArea.RegisterPlayerEnter(new DOLEventHandler(PlayerEnterLegionArea));

            GameEventMgr.AddHandler(GameLivingEvent.Dying, new DOLEventHandler(PlayerKilledByLegion));

            if (log.IsInfoEnabled)
                log.Info("Legion initialized..");
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            legionArea.UnRegisterPlayerEnter(new DOLEventHandler(PlayerEnterLegionArea));
            WorldMgr.GetRegion(249).RemoveArea(legionArea);

            GameEventMgr.RemoveHandler(GameLivingEvent.Dying, new DOLEventHandler(PlayerKilledByLegion));
        }

        public Legion()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40; // dmg reduction for melee dmg
                case eDamageType.Crush: return 40; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 70; // dmg reduction for rest resists
            }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 350;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get { return 300000; }
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(13333);
            LoadTemplate(npcTemplate);

            Size = 120;
            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            LegionBrain.CanThrow = false;
            LegionBrain.RemoveAdds = false;
            LegionBrain.IsCreatingSouls = false;

            // demon
            BodyType = 2;
            Race = 2001;
            RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(191);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

            LegionBrain sBrain = new LegionBrain();
            SetOwnBrain(sBrain);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int AttackRange
        {
            get { return 450; }
            set { }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override void Die(GameObject killer)
        {
            foreach (GameNPC npc in GetNPCsInRadius(5000))
            {
                if (npc.Brain is LegionAddBrain)
                {
                    npc.RemoveFromWorld();
                }
            }

            bool canReportNews = true;

            // due to issues with attackers the following code will send a notify to all in area in order to force quest credit
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

                if (!canReportNews || GameServer.ServerRules.CanGenerateNews(player) != false) continue;
                if (player.Client.Account.PrivLevel == (int) ePrivLevel.Player)
                    canReportNews = false;
            }

            var throwPlayer = TempProperties.getProperty<ECSGameTimer>("legion_throw");//cancel teleport
            if (throwPlayer != null)
            {
                throwPlayer.Stop();
                TempProperties.removeProperty("legion_throw");
            }

            var castaoe = TempProperties.getProperty<ECSGameTimer>("legion_castaoe");//cancel cast aoe
            if (castaoe != null)
            {
                castaoe.Stop();
                TempProperties.removeProperty("legion_castaoe");
            }

            base.Die(killer);

            if (canReportNews)
            {
                ReportNews(killer);
            }
        }
        public override void EnemyKilled(GameLiving enemy)
        {
            Health += MaxHealth / 20; //heals if boss kill enemy 5% of health
            base.EnemyKilled(enemy);
        }
        private static void PlayerEnterLegionArea(DOLEvent e, object sender, EventArgs args)
        {
            AreaEventArgs aargs = args as AreaEventArgs;
            GamePlayer player = aargs?.GameObject as GamePlayer;

            if (player == null)
                return;

            var mobsInArea = player.GetNPCsInRadius(2500);

            if (mobsInArea == null)
                return;

            foreach (GameNPC mob in mobsInArea)
            {
                if (mob is not Legion || !mob.InCombat) continue;

                if (Util.Chance(33))
                {
                    foreach (GamePlayer nearbyPlayer in mob.GetPlayersInRadius(2500))
                    {
                        nearbyPlayer.Out.SendMessage("Legion doesn't like enemies in his lair", eChatType.CT_Broadcast,
                            eChatLoc.CL_ChatWindow);
                        nearbyPlayer.Out.SendSpellEffectAnimation(mob, player, 5933, 0, false, 1);
                    }

                    //player.Die(mob);
                }
               /* else
                {
                    foreach (GamePlayer playerNearby in player.GetPlayersInRadius(350))
                    {
                        playerNearby.MoveTo(249, 48200, 49566, 20833, 1028);
                        playerNearby.BroadcastUpdate();
                    }

                    player.MoveTo(249, 48200, 49566, 20833, 1028);
                }*/

               // player.BroadcastUpdate();
            }
        }
        private static void PlayerKilledByLegion(DOLEvent e, object sender, EventArgs args)
        {
            GamePlayer player = sender as GamePlayer;

            if (player == null)
                return;

            DyingEventArgs eArgs = args as DyingEventArgs;

            if (eArgs?.Killer?.Name != "Legion")
                return;

            foreach (GameNPC mob in player.GetNPCsInRadius(2000))
            {
                if (mob is not Legion) continue;
                mob.Health += player.MaxHealth;
                mob.UpdateHealthManaEndu();
            }

           /* foreach (GamePlayer playerNearby in player.GetPlayersInRadius(350))
            {
                playerNearby.MoveTo(249, 48200, 49566, 20833, 1028);
                playerNearby.BroadcastUpdate();
            }*/
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            //possible AttackRange
            int distance = 400;
            
            if (source is GamePlayer || source is GamePet)
            {
                if (!source.IsWithinRadius(this, distance)) //take no damage from source that is not in radius 400
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GamePet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is not attackable from this range and is immune to your damage!", eChatType.CT_System,
                            eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                }
                else //take dmg
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        private void ReportNews(GameObject killer)
        {
            int numPlayers = AwardLegionKillPoint();
            String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
            NewsMgr.CreateNews(message, killer.Realm, eNewsType.PvE, true);

            if (Properties.GUILD_MERIT_ON_LEGION_KILL <= 0) return;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (player.IsEligibleToGiveMeritPoints)
                {
                    GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_LEGION_KILL);
                }
            }
        }
        private int AwardLegionKillPoint()
        {
            int count = 0;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.KillsLegion++;
                player.Achieve(AchievementUtils.AchievementNames.Legion_Kills);
                player.RaiseRealmLoyaltyFloor(1);
                count++;
            }
            return count;
        }
        public override void DealDamage(AttackData ad)
        {
            if (ad != null && ad.DamageType == eDamageType.Body)
                Health += ad.Damage / 2;
            base.DealDamage(ad);
        }
    }
}

namespace DOL.AI.Brain
{
    public class LegionBrain : StandardMobBrain
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public LegionBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 850;
        }
        public static bool RemoveAdds = false;
        public static bool IsCreatingSouls = false;
        public static bool CanThrow = false;
        public static bool CanPbaoe = false;
        #region Health check bools
        public static bool adds1 = false;
        public static bool adds2 = false;
        public static bool adds3 = false;
        public static bool adds4 = false;
        public static bool adds5 = false;
        public static bool adds6 = false;
        public static bool adds7 = false;
        public static bool adds8 = false;
        public static bool adds9 = false;
        public static bool adds10 = false;
        public static bool adds11 = false;
        public static bool adds12 = false;
        public static bool adds13 = false;
        public static bool adds14 = false;
        public static bool adds15 = false;
        public static bool adds16 = false;
        public static bool adds17 = false;
        public static bool adds18 = false;
        public static bool adds19 = false;
        #endregion

        public override void Think()
        {
            if(!HasAggressionTable())
            {
                IsCreatingSouls = false;
                CanThrow = false;
                #region Health check bools
                adds1 = false;
                adds2 = false;
                adds3 = false;
                adds4 = false;
                adds5 = false;
                adds6 = false;
                adds7 = false;
                adds8 = false;
                adds9 = false;
                adds10 = false;
                adds11 = false;
                adds12 = false;
                adds13 = false;
                adds14 = false;
                adds15 = false;
                adds16 = false;
                adds17 = false;
                adds18 = false;
                adds19 = false;
                #endregion

                if (Port_Enemys.Count > 0)//clear port players
                    Port_Enemys.Clear();
                if (randomlyPickedPlayers.Count > 0)//clear randomly picked players
                    randomlyPickedPlayers.Clear();

                var throwPlayer = Body.TempProperties.getProperty<ECSGameTimer>("legion_throw");//cancel teleport
                if (throwPlayer != null)
                {
                    throwPlayer.Stop();
                    Body.TempProperties.removeProperty("legion_throw");
                }
                var castaoe = Body.TempProperties.getProperty<ECSGameTimer>("legion_castaoe");//cancel cast aoe
                if (castaoe != null)
                {
                    castaoe.Stop();
                    Body.TempProperties.removeProperty("legion_castaoe");
                }
            }
            if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
            {
                Body.Health = Body.MaxHealth;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                    {
                        if (npc.Brain is LegionAddBrain)
                            npc.RemoveFromWorld();
                    }
                    RemoveAdds = true;
                }
            }
            if (HasAggro && Body.TargetObject != null)
            {
                RemoveAdds = false;
                DestroyDamnBubble();
                if(bladeturnConsumed >= 5 && !CanPbaoe)
                {
                    ReleaseAoeLifetap();
                    ECSGameTimer castAoe = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetAoe), 10000);
                    Body.TempProperties.setProperty("legion_castaoe", castAoe);
                    CanPbaoe = true;
                }
                #region Legion health checks
                if (Body.HealthPercent <= 95 && Body.HealthPercent > 90 && !adds1)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds1 = true;
                }
                if (Body.HealthPercent <= 90 && Body.HealthPercent > 85 && !adds2)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds2 = true;
                }
                if (Body.HealthPercent <= 85 && Body.HealthPercent > 80 && !adds3)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds3 = true;
                }
                if (Body.HealthPercent <= 80 && Body.HealthPercent > 75 && !adds4)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds4 = true;
                }
                if (Body.HealthPercent <= 75 && Body.HealthPercent > 70 && !adds5)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds5 = true;
                }
                if (Body.HealthPercent <= 70 && Body.HealthPercent > 65 && !adds6)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds6 = true;
                }
                if (Body.HealthPercent <= 65 && Body.HealthPercent > 60 && !adds7)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds7 = true;
                }
                if (Body.HealthPercent <= 60 && Body.HealthPercent > 55 && !adds8)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds8 = true;
                }
                if (Body.HealthPercent <= 55 && Body.HealthPercent > 50 && !adds9)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds9 = true;
                }
                if (Body.HealthPercent <= 50 && Body.HealthPercent > 45 && !adds10)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds10 = true;
                }
                if (Body.HealthPercent <= 45 && Body.HealthPercent > 40 && !adds11)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds11 = true;
                }
                if (Body.HealthPercent <= 40 && Body.HealthPercent > 35 && !adds12)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds12 = true;
                }
                if (Body.HealthPercent <= 35 && Body.HealthPercent > 30 && !adds13)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds13 = true;
                }
                if (Body.HealthPercent <= 30 && Body.HealthPercent > 25 && !adds14)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds14 = true;
                }
                if (Body.HealthPercent <= 25 && Body.HealthPercent > 20 && !adds15)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds15 = true;
                }
                if (Body.HealthPercent <= 20 && Body.HealthPercent > 15 && !adds16)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds16 = true;
                }
                if (Body.HealthPercent <= 15 && Body.HealthPercent > 10 && !adds17)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds17 = true;
                }
                if (Body.HealthPercent <= 10 && Body.HealthPercent > 5 && !adds18)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds18 = true;
                }
                if (Body.HealthPercent <= 5 && Body.HealthPercent > 0 && !adds19)
                {
                    SpawnAdds();
                    spawnAmount = 0;
                    PlayerCountInLegionLair = 0;
                    adds19 = true;
                }
                #endregion
                if (!CanThrow)
                {
                    ECSGameTimer throwPlayer = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ThrowPlayer), Util.Random(20000, 35000));//throw players
                    Body.TempProperties.setProperty("legion_throw", throwPlayer);
                    CanThrow = true;
                }
            }

            base.Think();
        }
        private int bladeturnConsumed = 0;
        private void DestroyDamnBubble()
        {
            if (Body.TargetObject != null && HasAggro)
            {
                GameLiving target = Body.TargetObject as GameLiving;
                if (Util.Chance(100))
                {
                    if (target.effectListComponent.ContainsEffectForEffectType(eEffect.Bladeturn) && target != null && target.IsAlive)
                    {
                        var effect = EffectListService.GetEffectOnTarget(target, eEffect.Bladeturn);
                        if (effect != null)
                        {
                            EffectService.RequestImmediateCancelEffect(effect);//remove bladeturn effect here
                            bladeturnConsumed++;
                            if(target is GamePlayer player)
                            {
                                if (player != null && player.IsAlive)
                                    player.Out.SendMessage("Legion consume your bladeturn effect!", eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                            }
                        }
                    }
                }
            }
        }
        public void ReleaseAoeLifetap()
        {
            if (Body.TargetObject != null)
            {
                if (!Body.IsCasting)
                {
                    BroadcastMessage("Legion unleashing massive soul consumption blast.");
                    Body.CastSpell(LegionLifetapAoe, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
                }
            }
            bladeturnConsumed = 0;
        }
        private int ResetAoe(ECSGameTimer timer)
        {
            CanPbaoe = false;
            return 0;
        }
        public static int PlayerCountInLegionLair = 0;
        public static int spawnAmount = 0;
        private void SpawnAdds()
        {
            if (Body.InCombat && Body.IsAlive && HasAggro)
            {
                foreach (GamePlayer playerNearby in Body.GetPlayersInRadius(2000))
                {
                    if (playerNearby != null && playerNearby.Client.Account.PrivLevel == 1)
                    {
                        PlayerCountInLegionLair++;
                    }
                    if (PlayerCountInLegionLair < 4)
                        spawnAmount = 1;
                    if (PlayerCountInLegionLair > 4)
                        spawnAmount = PlayerCountInLegionLair / 4;
                }
            }
            if (PlayerCountInLegionLair > 0 && spawnAmount > 0)
            {
                //log.Warn("PlayerCountInLegionLair = " + PlayerCountInLegionLair + " and spawnAmount = "+ spawnAmount);
                for (int i = 0; i < spawnAmount; i++)
                {
                    //var distanceDelta = Util.Random(0, 300);
                    var level = Util.Random(52, 58);

                    LegionAdd add = new LegionAdd();
                    /*
                     add.X = target.X + distanceDelta;
                    add.Y = target.Y + distanceDelta;
                    add.Z = target.Z;
                    add.CurrentRegionID = target.CurrentRegionID;
                    */
                    add.X = Body.X + Util.Random(-150, 150);
                    add.Y = Body.Y + Util.Random(-150, 150);
                    add.Z = Body.Z;
                    add.CurrentRegionID = 249;
                    add.IsWorthReward = false;
                    add.Level = (byte)level;
                    add.AddToWorld();
                    //add.StartAttack(target);
                }
            }
        }
        #region Legion Port
        List<GamePlayer> Port_Enemys = new List<GamePlayer>();
        List<GamePlayer> randomlyPickedPlayers = new List<GamePlayer>();
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
            }
        }
        public static List<t> GetRandomElements<t>(IEnumerable<t> list, int elementsCount)//pick X elements from list
        {
            return list.OrderBy(x => Guid.NewGuid()).Take(elementsCount).ToList();
        }
        private int ThrowPlayer(ECSGameTimer timer)
        {
            if (Body.IsAlive && HasAggro)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(3000))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!Port_Enemys.Contains(player))
                            {
                                if (player != Body.TargetObject)//dont throw main target
                                    Port_Enemys.Add(player);
                            }
                        }
                    }
                }
                if (Port_Enemys.Count > 0)
                {
                    randomlyPickedPlayers = GetRandomElements(Port_Enemys, Util.Random(8, 16));//pick 5-8players from list to new list

                    if (randomlyPickedPlayers.Count > 0)
                    {
                        foreach (GamePlayer player in randomlyPickedPlayers)
                        {
                            if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && HasAggro && player.IsWithinRadius(Body, 2500))
                            {
                                player.MoveTo(249, 48200, 49566, 20833, 1028);
                                //player.BroadcastUpdate();
                            }
                        }
                        randomlyPickedPlayers.Clear();//clear list after port
                    }
                }
                CanThrow = false;// set to false, so can throw again
            }
            return 0;
        }
        #endregion
        #region Spells
        private Spell m_LegionLifetapAoe;
        public Spell LegionLifetapAoe
        {
            get
            {
                if (m_LegionLifetapAoe == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Power = 0;
                    spell.RecastDelay = 5;
                    spell.ClientEffect = 9191;
                    spell.Icon = 9191;
                    spell.Damage = 1200;
                    spell.DamageType = (int)eDamageType.Body;
                    spell.Name = "Lifetap";
                    spell.Range = 0;
                    spell.Radius = 1500;
                    spell.SpellID = 12013;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    m_LegionLifetapAoe = new Spell(spell, 60);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_LegionLifetapAoe);
                }
                return m_LegionLifetapAoe;
            }
        }
        #endregion
    }
}
#region Legion adds
namespace DOL.GS
{
    public class LegionAdd : GameNPC
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LegionAdd()
            : base()
        {
        }
        public override int MaxHealth
        {
            get { return 1200; }
        }

        public override int AttackRange
        {
            get { return 450; }
            set { }
        }
        public override void DropLoot(GameObject killer)
        {
        }
        public override long ExperienceValue => 0;
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 150;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.10;
        }

        public override bool AddToWorld()
        {
            Model = 660;
            Name = "graspering soul";
            Size = 50;
            Realm = 0;

            Strength = 60;
            Intelligence = 60;
            Piety = 60;
            Dexterity = 60;
            Constitution = 60;
            Quickness = 60;
            RespawnInterval = -1;

            Gender = eGender.Neutral;
            MeleeDamageType = eDamageType.Slash;

            BodyType = 2;
            LegionAddBrain sBrain = new LegionAddBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 800;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class LegionAddBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LegionAddBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
        }

        public override void Think()
        {
            if (Body.InCombatInLast(60 * 1000) == false && Body.InCombatInLast(65 * 1000))
            {
                Body.RemoveFromWorld();
            }
            base.Think();
        }
    }
}
#endregion

#region Behemoth
namespace DOL.GS
{
    public class Behemoth : GameEpicBoss
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Behemoth()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40; // dmg reduction for melee dmg
                case eDamageType.Crush: return 40; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 40; // dmg reduction for melee dmg
                default: return 70; // dmg reduction for rest resists
            }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int MaxHealth
        {
            get { return 600000; }
        }

        public override int AttackRange
        {
            get { return 450; }
            set { }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 550;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.50;
        }
        public override void OnAttackEnemy(AttackData ad)
        {
            if (ad != null && ad.Target != null && ad.Target.IsAlive)
                ad.Target.Die(this);

            base.OnAttackEnemy(ad);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158340);
            LoadTemplate(npcTemplate);

            Strength = npcTemplate.Strength;
            Constitution = npcTemplate.Constitution;
            Dexterity = npcTemplate.Dexterity;
            Quickness = npcTemplate.Quickness;
            Empathy = npcTemplate.Empathy;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;

            BehemothBrain sBrain = new BehemothBrain();
            RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class BehemothBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BehemothBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }
            base.Think();
        }
    }
}
#endregion