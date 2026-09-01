using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

namespace DOL.GS.Scripts
{
    public class Legion : GameEpicBoss
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static IArea legionArea = null;

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            const int radius = 650;
            Region region = WorldMgr.GetRegion(249);
            legionArea = region.AddArea(new Area.Circle("Legion's Lair", 45000, 51700, 15468, radius));
            log.Debug("Legion's Lair created with radius " + radius + " at 45000 51700 15468");
            //legionArea.RegisterPlayerEnter(new DOLEventHandler(PlayerEnterLegionArea));

            //GameEventMgr.AddHandler(GameLivingEvent.Dying, new DOLEventHandler(PlayerKilledByLegion));

            if (log.IsInfoEnabled)
                log.Info("Legion initialized..");
        }

        [ScriptUnloadedEvent]
        public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            //legionArea.UnRegisterPlayerEnter(new DOLEventHandler(PlayerEnterLegionArea));
            WorldMgr.GetRegion(249).RemoveArea(legionArea);

            //GameEventMgr.RemoveHandler(GameLivingEvent.Dying, new DOLEventHandler(PlayerKilledByLegion));
        }

        public Legion()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 20; // dmg reduction for melee dmg
                case eDamageType.Crush: return 20; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 20; // dmg reduction for melee dmg
                default: return 40; // dmg reduction for rest resists
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

            // demon
            BodyType = 2;
            RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
            Faction = FactionMgr.GetFactionByID(191);

            LegionBrain sBrain = new LegionBrain();
            SetOwnBrain(sBrain);
            SaveIntoDatabase();
            base.AddToWorld();
            return true;
        }


        public override int MeleeAttackRange => 450;
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override void ProcessDeath(GameObject killer)
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

            var throwPlayer = TempProperties.GetProperty<ECSGameTimer>("legion_throw");//cancel teleport
            if (throwPlayer != null)
            {
                throwPlayer.Stop();
                TempProperties.RemoveProperty("legion_throw");
            }

            var castaoe = TempProperties.GetProperty<ECSGameTimer>("legion_castaoe");//cancel cast aoe
            if (castaoe != null)
            {
                castaoe.Stop();
                TempProperties.RemoveProperty("legion_castaoe");
            }

            if (canReportNews)
            {
                ReportNews(killer);
            }

            base.ProcessDeath(killer);
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(3000))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public override void EnemyKilled(GameLiving enemy)
        {
            if (enemy != null && enemy is GamePlayer)
            {
                BroadcastMessage("Legion says, \"Your soul give me new strength.\"");
                Health += MaxHealth / 40; //heals if boss kill enemy player for 2.5% of his max health
            }
            base.EnemyKilled(enemy);
        }
      /*  private static void PlayerEnterLegionArea(DOLEvent e, object sender, EventArgs args)
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
               / else
                {
                    foreach (GamePlayer playerNearby in player.GetPlayersInRadius(350))
                    {
                        playerNearby.MoveTo(249, 48200, 49566, 20833, 1028);
                        playerNearby.BroadcastUpdate();
                    }

                    player.MoveTo(249, 48200, 49566, 20833, 1028);
                }

               // player.BroadcastUpdate();
            }
        }*/
       /* private static void PlayerKilledByLegion(DOLEvent e, object sender, EventArgs args)
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

            foreach (GamePlayer playerNearby in player.GetPlayersInRadius(350))
            {
                playerNearby.MoveTo(249, 48200, 49566, 20833, 1028);
                playerNearby.BroadcastUpdate();
            }
        }*/
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            //possible AttackRange
            int distance = 1400;
            
            if (source is GamePlayer || source is GameSummonedPet)
            {
                if (!source.IsWithinRadius(this, distance)) //take no damage from source that is not in radius 1000
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
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
                count++;
            }
            return count;
        }
        public override void DealDamage(AttackData ad)
        {
            if (ad != null && ad.DamageType == eDamageType.Body)
                Health += ad.Damage / 4;
            base.DealDamage(ad);
        }
    }
}

namespace DOL.AI.Brain
{
    public class LegionBrain : EpicBossBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        
        public LegionBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 850;
        }
        private bool RemoveAdds = false;
        private bool CanThrow = false;
        private bool CanPbaoe = false;
        private readonly bool[] addsSpawned = new bool[19];

        public override void Think()
        {
            if(!CheckProximityAggro())
            {
                CanThrow = false;
                Array.Clear(addsSpawned, 0, addsSpawned.Length);

                if (randomlyPickedPlayers.Count > 0)//clear randomly picked players
                    randomlyPickedPlayers.Clear();

                var throwPlayer = Body.TempProperties.GetProperty<ECSGameTimer>("legion_throw");//cancel teleport
                if (throwPlayer != null)
                {
                    throwPlayer.Stop();
                    Body.TempProperties.RemoveProperty("legion_throw");
                }
                var castaoe = Body.TempProperties.GetProperty<ECSGameTimer>("legion_castaoe");//cancel cast aoe
                if (castaoe != null)
                {
                    castaoe.Stop();
                    Body.TempProperties.RemoveProperty("legion_castaoe");
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
                    Body.TempProperties.SetProperty("legion_castaoe", castAoe);
                    CanPbaoe = true;
                }
                #region Legion health checks
                int healthPercent = Body.HealthPercent;
                for (int i = 0; i < addsSpawned.Length; i++)
                {
                    int upperBound = 95 - i * 5;
                    if (healthPercent <= upperBound && healthPercent > upperBound - 5 && !addsSpawned[i])
                    {
                        SpawnAdds();
                        addsSpawned[i] = true;
                    }
                }
                #endregion
                if (!CanThrow)
                {
                    ECSGameTimer throwPlayer = new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ThrowPlayer), Util.Random(40000, 65000));//throw players
                    Body.TempProperties.SetProperty("legion_throw", throwPlayer);
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
                        ECSGameEffect effect = EffectListService.GetEffectOnTarget(target, eEffect.Bladeturn);

                        if (effect != null)
                        {
                            effect.End();//remove bladeturn effect here
                            bladeturnConsumed++;

                            if (target is GamePlayer player)
                            {
                                if (player.IsAlive)
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
        private void SpawnAdds()
        {
            int playerCountInLegionLair = 0;
            int spawnAmount = 0;
            if (Body.InCombat && Body.IsAlive && HasAggro)
            {
                foreach (GamePlayer playerNearby in Body.GetPlayersInRadius(2000))
                {
                    if (playerNearby != null && playerNearby.Client.Account.PrivLevel == 1)
                    {
                        playerCountInLegionLair++;
                    }
                    if (playerCountInLegionLair < 4)
                        spawnAmount = 1;
                    if (playerCountInLegionLair > 4)
                        spawnAmount = playerCountInLegionLair / 4;
                }
            }
            if (playerCountInLegionLair > 0 && spawnAmount > 0)
            {
                //log.Warn("PlayerCountInLegionLair = " + PlayerCountInLegionLair + " and spawnAmount = "+ spawnAmount);
                for (int i = 0; i < spawnAmount; i++)
                {
                    var level = Util.Random(52, 58);

                    LegionAdd add = new LegionAdd();
                    add.X = Body.X + Util.Random(-150, 150);
                    add.Y = Body.Y + Util.Random(-150, 150);
                    add.Z = Body.Z;
                    add.CurrentRegionID = 249;
                    add.Level = (byte)level;
                    add.AddToWorld();
                }
            }
        }
        #region Legion Port
        List<GamePlayer> randomlyPickedPlayers = new List<GamePlayer>();
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
            }
        }

        private int ThrowPlayer(ECSGameTimer timer)
        {
            if (Body.IsAlive && HasAggro)
            {
                // From an ordered aggro list, ignore the first 5 entities. Then take 8~16 random players
                var randomlyPickedPlayers = GetOrderedAggroList(5).OfType<GamePlayer>().Where(x =>
                {
                    return x.Client.Account.PrivLevel == 1 && x.IsWithinRadius(Body, 2500);
                }).OrderBy(static x => Util.Random(int.MaxValue - 1)).Take(Util.Random(8, 16));

                foreach (GamePlayer player in randomlyPickedPlayers)
                    player.MoveTo(249, 48200, 49566, 20833, 1028);

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
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Power = 0;
                    spell.RecastDelay = 5;
                    spell.ClientEffect = 9191;
                    spell.Icon = 9191;
                    spell.Damage = 1000;
                    spell.DamageType = (int)eDamageType.Body;
                    spell.Name = "Lifetap";
                    spell.Range = 0;
                    spell.Radius = 1000;
                    spell.SpellID = 12013;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    m_LegionLifetapAoe = new Spell(spell, 60);
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
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LegionAdd()
            : base()
        {
        }
        public override int MaxHealth
        {
            get { return 1200; }
        }

        public override int MeleeAttackRange => 450;
        public override bool CanDropLoot => false;
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
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        public override int MaxHealth
        {
            get { return 600000; }
        }

        public override int MeleeAttackRange => 450;
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
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public BehemothBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
            }
            base.Think();
        }
    }
}
#endregion