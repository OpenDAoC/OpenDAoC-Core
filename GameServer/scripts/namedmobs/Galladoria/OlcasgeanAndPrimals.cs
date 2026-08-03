using System;
using System.Collections.Generic;
using System.Threading;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;

#region Olcasgean Initializator
/// <summary>
/// ///////////////////////////////////// Initializator Base ////////////////////////////////
/// </summary>

namespace DOL.GS
{
    public class OlcasgeanInitializator : GameNPC
    {
        public OlcasgeanInitializator() : base() { }

        // Set by Olcasgean when it's added to the world, which also resets the encounter.
        public Olcasgean Boss { get; set; }
        public bool EventStarted { get; set; }

        public override int MaxHealth
        {
            get { return 10000; }
        }
        public override bool CanDropLoot => false;
        public override void Die(GameObject killer)
        {
            base.Die(null); // null to not gain experience
        }
        public override bool AddToWorld()
        {
            Name = "Olcasgean Initializator";
            GuildName = "DO NOT REMOVE!";
            Model = 665;
            Realm = 0;
            Level = 50;
            Size = 50;
            CurrentRegionID = 191;//galladoria
            Flags = (GameNPC.eFlags)60;
            Faction = FactionMgr.GetFactionByID(96);
            X = 41116;
            Y = 64419;
            Z = 12746;
            OIBrain ubrain = new OIBrain();
            SetOwnBrain(ubrain);
            base.AddToWorld();
            return true;
        }
    }
}

/// <summary>
/// ///////////////////////////////////// Initializator Brain ////////////////////////////////
/// </summary>
namespace DOL.AI.Brain
{
    public class OIBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public OIBrain()
            : base()
        {
            ThinkInterval = 1000;
        }

        private static readonly Point3D _bridgePoint = new(39652, 60831, 11893);//loc of waterfall bridge to start event and pop elementars

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

        public override void Think()
        {
            if (Body.IsAlive && Body is OlcasgeanInitializator initializator && !initializator.EventStarted)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(7000))
                {
                    if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1 && player.IsWithinRadius(_bridgePoint, 350))
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Message1), 5000);//5s to start
                        initializator.EventStarted = true;
                        break;
                    }
                }
            }
            base.Think();
        }
        public int Message1(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("A voice that seems to come from all around you says: 'Intruders have entered inner sanctum.'"));
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Message2), 5000);
            return 0;
        }
        public int Message2(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("A deep booming voice responds; 'P...R...O...T...E...C...T..'"));
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Message3), 5000);
            return 0;
        }
        public int Message3(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("'I am tired, and yet, there is much left for me to take care of this day'"));
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Message4), 5000);
            return 0;
        }
        public int Message4(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("The first voice says: 'We shall protect.'"));
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnPrimals), 5000);
            return 0;
        }
        #region Spawn primals and other mobs
        private int SpawnPrimals(ECSGameTimer timer)//real timer to cast spell and reset check
        {
            SpawnAir();
            SpawnWater();
            SpawnFire();
            SpawnEarth();
            SpawnGuardianEarthmender();
            SpawnMagicalEarthmender();
            SpawnNaturalEarthmender();
            SpawnShadowyEarthmender();
            SpawnVortex();
            return 0;
        }
        public void SpawnAir()
        {
            AirPrimal Add = new AirPrimal();
            Add.X = 39713;
            Add.Y = 61264;
            Add.Z = 12372;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = Body.Heading;
            Add.Olcasgean = (Body as OlcasgeanInitializator)?.Boss;
            Add.AddToWorld();
        }
        public void SpawnWater()
        {
            WaterPrimal Add = new WaterPrimal();
            Add.X = 39547;
            Add.Y = 62071;
            Add.Z = 11688;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = 2052;
            Add.Olcasgean = (Body as OlcasgeanInitializator)?.Boss;
            Add.AddToWorld();
        }
        public void SpawnFire()
        {
            FirePrimal Add = new FirePrimal();
            Add.X = 39481;
            Add.Y = 63240;
            Add.Z = 11699;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = Body.Heading;
            Add.Olcasgean = (Body as OlcasgeanInitializator)?.Boss;
            Add.AddToWorld();
        }
        public void SpawnEarth()
        {
            EarthPrimal Add = new EarthPrimal();
            Add.X = 39727;
            Add.Y = 62620;
            Add.Z = 11684;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = 2052;
            Add.Olcasgean = (Body as OlcasgeanInitializator)?.Boss;
            Add.AddToWorld();
        }
        public void SpawnGuardianEarthmender()
        {
            GuardianEarthmender Add1 = new GuardianEarthmender();
            Add1.X = 40020;
            Add1.Y = 62401;
            Add1.Z = 11676;
            Add1.CurrentRegion = Body.CurrentRegion;
            Add1.Heading = 562;
            Add1.AddToWorld();
        }
        public void SpawnMagicalEarthmender()
        {
            MagicalEarthmender Add2 = new MagicalEarthmender();
            Add2.X = 39459;
            Add2.Y = 62412;
            Add2.Z = 11688;
            Add2.CurrentRegion = Body.CurrentRegion;
            Add2.Heading = 3623;
            Add2.AddToWorld();
        }
        public void SpawnNaturalEarthmender()
        {
            NaturalEarthmender Add3 = new NaturalEarthmender();
            Add3.X = 39552;
            Add3.Y = 62929;
            Add3.Z = 11690;
            Add3.CurrentRegion = Body.CurrentRegion;
            Add3.Heading = 2312;
            Add3.AddToWorld();
        }
        public void SpawnShadowyEarthmender()
        {
            ShadowyEarthmender Add4 = new ShadowyEarthmender();
            Add4.X = 39965;
            Add4.Y = 62921;
            Add4.Z = 11662;
            Add4.CurrentRegion = Body.CurrentRegion;
            Add4.Heading = 1769;
            Add4.AddToWorld();
        }
        public void SpawnVortex()
        {
            Vortex Add = new Vortex();
            Add.X = 40369;
            Add.Y = 60755;
            Add.Z = 10888;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = 3804;
            Add.AddToWorld();

            Vortex Add2 = new Vortex();
            Add2.X = 41278;
            Add2.Y = 61614;
            Add2.Z = 10888;
            Add2.CurrentRegion = Body.CurrentRegion;
            Add2.Heading = 1608;
            Add2.AddToWorld();

            Vortex Add3 = new Vortex();
            Add3.X = 41327;
            Add3.Y = 62330;
            Add3.Z = 10888;
            Add3.CurrentRegion = Body.CurrentRegion;
            Add3.Heading = 2006;
            Add3.AddToWorld();

            Vortex Add4 = new Vortex();
            Add4.X = 41258;
            Add4.Y = 63287;
            Add4.Z = 10888;
            Add4.CurrentRegion = Body.CurrentRegion;
            Add4.Heading = 3804;
            Add4.AddToWorld();

            Vortex Add5 = new Vortex();
            Add5.X = 40794;
            Add5.Y = 63876;
            Add5.Z = 10888;
            Add5.CurrentRegion = Body.CurrentRegion;
            Add5.Heading = 3804;
            Add5.AddToWorld();

            Vortex Add6 = new Vortex();
            Add6.X = 39584;
            Add6.Y = 64335;
            Add6.Z = 10888;
            Add6.CurrentRegion = Body.CurrentRegion;
            Add6.Heading = 3804;
            Add6.AddToWorld();

            Vortex Add7 = new Vortex();
            Add7.X = 38719;
            Add7.Y = 64004;
            Add7.Z = 10888;
            Add7.CurrentRegion = Body.CurrentRegion;
            Add7.Heading = 3804;
            Add7.AddToWorld();

            Vortex Add8 = new Vortex();
            Add8.X = 37965;
            Add8.Y = 63312;
            Add8.Z = 10888;
            Add8.CurrentRegion = Body.CurrentRegion;
            Add8.Heading = 3804;
            Add8.AddToWorld();

            Vortex Add9 = new Vortex();
            Add9.X = 37939;
            Add9.Y = 62113;
            Add9.Z = 10888;
            Add9.CurrentRegion = Body.CurrentRegion;
            Add9.Heading = 3804;
            Add9.AddToWorld();

            Vortex Add10 = new Vortex();
            Add10.X = 38390;
            Add10.Y = 61089;
            Add10.Z = 10888;
            Add10.CurrentRegion = Body.CurrentRegion;
            Add10.Heading = 3804;
            Add10.AddToWorld();

            Vortex Add11 = new Vortex();
            Add11.X = 39204;
            Add11.Y = 60731;
            Add11.Z = 10888;
            Add11.CurrentRegion = Body.CurrentRegion;
            Add11.Heading = 3804;
            Add11.AddToWorld();
        }
        #endregion
    }
}
#endregion Olcasgean Initializator

#region Olcasgean
namespace DOL.GS
{
    public class Olcasgean : GameEpicBoss
    {
        private int _deadPrimalsCount;

        public Olcasgean2 Copy { get; set; }
        public bool AllPrimalsDead => _deadPrimalsCount >= 4;

        public Olcasgean()
            : base()
        {
        }

        public void OnPrimalDied()
        {
            Interlocked.Increment(ref _deadPrimalsCount);
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }

        public override int MaxHealth
        {
            get { return 250000; }
        }
        public override int MeleeAttackRange => 1500;
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);

            // Both bodies share a single health pool.
            if (Copy != null && Copy.IsAlive)
                Copy.Health = Health;
        }
        #region Custom Methods
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
            }
        }
        protected void ReportNews(GameObject killer)
        {
            int numPlayers = AwardEpicEncounterKillPoint();
            String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
            NewsMgr.CreateNews(message, killer.Realm, eNewsType.PvE, true);

            if (Properties.GUILD_MERIT_ON_DRAGON_KILL > 0)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player.IsEligibleToGiveMeritPoints)
                    {
                        GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_DRAGON_KILL);
                    }
                }
            }
        }
        protected int AwardEpicEncounterKillPoint()
        {
            int count = 0;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.KillsEpicBoss++;
                count++;
            }
            return count;
        }
        public override void ProcessDeath(GameObject killer)
        {
            if (Copy != null && Copy.IsAlive)
                Copy.RemoveFromWorld();

            foreach (GameNPC npc in GetNPCsInRadius(10000))
            {
                if (npc != null && npc.IsAlive)
                {
                    if (npc.Brain is VortexBrain || npc.Brain is WaterfallAntipassBrain)
                        npc.RemoveFromWorld();
                }
            }

            bool canReportNews = true;
            // due to issues with attackers the following code will send a notify to all in area in order to force quest credit
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

                if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
                {
                    if (player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
                        canReportNews = false;
                }
            }
            if (canReportNews)
            {
                if (killer is not Olcasgean and not Olcasgean2)
                    ReportNews(killer);
            }
            base.ProcessDeath(killer);
        }
        #endregion
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164624);
            LoadTemplate(npcTemplate);

            X = 39237;
            Y = 62644;
            Z = 11685;
            Heading = 102;
            CurrentRegionID = 191;

            Flags = (GameNPC.eFlags)156;
            RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
            _deadPrimalsCount = 0;
            Copy = null;

            // Remove encounter leftovers, and register with the initializator so it can restart the event and hand us to the primals it spawns.
            foreach (GameNPC npc in GetNPCsInRadius(5500))
            {
                if (npc is OlcasgeanInitializator initializator)
                {
                    initializator.Boss = this;
                    initializator.EventStarted = false;
                    continue;
                }

                if (npc != null && npc.IsAlive)
                {
                    if (npc.Brain is WaterPrimalBrain || npc.Brain is AirPrimalBrain || npc.Brain is FirePrimalBrain || npc.Brain is EarthPrimalBrain
                        || npc.Brain is GuardianEarthmenderBrain || npc.Brain is MagicalEarthmenderBrain || npc.Brain is NaturalEarthmenderBrain || npc.Brain is ShadowyEarthmenderBrain || npc.Brain is OlcasgeanBrain2)
                    {
                        npc.RemoveFromWorld();
                    }
                }
            }
            Faction = FactionMgr.GetFactionByID(96);
            OlcasgeanBrain sBrain = new OlcasgeanBrain();
            SetOwnBrain(sBrain);
            return base.AddToWorld();
        }

        public override void OnAttackedByEnemy(AttackData ad)// on Boss being attacked
        {
            if (ad != null && ad.Damage > 0 && ad.Attacker != null && ad.Attacker.IsAlive && ad.Attacker is GamePlayer)
            {
                if (HealthPercent <= 50)
                {
                    if (Util.Chance(50))
                        CastSpell(OlcasgeanDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
                if (HealthPercent > 50)
                    if (Util.Chance(25))
                        CastSpell(OlcasgeanDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.OnAttackedByEnemy(ad);
        }
        public Spell m_OlcasgeanDD;
        public Spell OlcasgeanDD
        {
            get
            {
                if (m_OlcasgeanDD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 1;
                    spell.ClientEffect = 11027;
                    spell.Icon = 11027;
                    spell.TooltipId = 11027;
                    spell.Name = "Olcasgean's Root";
                    spell.Damage = 450;
                    spell.Range = 1800;
                    spell.SpellID = 11901;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.DamageType = (int)eDamageType.Matter;
                    m_OlcasgeanDD = new Spell(spell, 70);
                }
                return m_OlcasgeanDD;
            }
        }
    }
}
#endregion Olcasgean

#region Olcasgean Brain
namespace DOL.AI.Brain
{
    public class OlcasgeanBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public OlcasgeanBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 1000;
        }
        private GamePlayer _teleportTarget;
        private bool _teleportScheduled;
        private bool _antipassSpawned;
        private bool _wakeUpStarted;
        private bool _effectsSpawning;
        private bool _removedAdds;
        private readonly List<GamePlayer> player_in_range = new();
        private readonly List<GamePlayer> player_in_range2 = new();
        private readonly List<GamePlayer> player_to_port = new();
        private readonly List<GamePlayer> ported_player = new();

        public void SpawnAntiPass()
        {
            WaterfallAntipass Add = new WaterfallAntipass();
            Add.X = 39670;
            Add.Y = 60649;
            Add.Z = 12013;
            Add.CurrentRegion = Body.CurrentRegion;
            Add.Heading = Body.Heading;
            Add.AddToWorld();
        }
        public int SpawnEffects(ECSGameTimer timer)
        {
            if (HasAggro && Body.IsAlive)
            {
                Point3D spot = new Point3D(39526, 62755, 11690);
                for (int i = 0; i < Util.Random(8, 15); i++)
                {
                    OlcasgeanEffect Add = new OlcasgeanEffect();
                    Add.X = spot.X + Util.Random(-900, 900);
                    Add.Y = spot.Y + Util.Random(-900, 900);
                    Add.Z = spot.Z;
                    Add.CurrentRegion = Body.CurrentRegion;
                    Add.Heading = Body.Heading;
                    Add.AddToWorld();
                }
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetSpawnEffect), 2000);
            }
            return 0;
        }
        public int ResetSpawnEffect(ECSGameTimer timer)
        {
            _effectsSpawning = false;
            return 0;
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public int WakeUpBoss(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("A deep booming voice echoes: 'I am eternal. You and your kind will die.'"));
            Body.Flags = 0;
            return 0;
        }
        #region Think()
        public override void Think()
        {
            if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (!HasAggro)
            {
                _teleportScheduled = false;
                _antipassSpawned = false;
                _effectsSpawning = false;
                _teleportTarget = null;
                player_in_range.Clear();
                player_in_range2.Clear();
                player_to_port.Clear();
                ported_player.Clear();
                if (!_removedAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                    {
                        if (npc.Brain is WaterfallAntipassBrain)
                            npc.RemoveFromWorld();
                    }
                    _removedAdds = true;
                }
            }

            if (Body.IsAlive && Body is Olcasgean boss)
            {
                if (boss.Copy == null || !boss.Copy.IsAlive)
                    SpawnCopy(boss);

                if (boss.AllPrimalsDead && !_wakeUpStarted)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(WakeUpBoss), 25000);
                    _wakeUpStarted = true;
                }
                Point3D point1 = new Point3D();
                point1.X = 40170; point1.Y = 62600; point1.Z = 11681;//location where players need to stay to avoid Olcasgean spamming dd spell

                Point3D point2 = new Point3D();
                point2.X = 39237; point2.Y = 62644; point2.Z = 11685;

                if (HasAggro && Body.TargetObject != null)//Boss in combat
                {
                    _removedAdds = false;
                    if (!_effectsSpawning)
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnEffects), 2000);
                        _effectsSpawning = true;
                    }

                    if (!_antipassSpawned)//spawn anti pass near waterfall so players cant leave boss area until killed
                    {
                        SpawnAntiPass();
                        _antipassSpawned = true;
                    }
                    foreach (GamePlayer player in Body.GetPlayersInRadius(1500))//pick teleport player
                    {
                        if (player != null)
                        {
                            if (player.IsAlive && player.Client.Account.PrivLevel == 1 && player != Body.TargetObject)
                            {
                                if (!player_to_port.Contains(player))
                                    player_to_port.Add(player);
                            }
                        }
                    }
                    foreach (GamePlayer player in Body.GetPlayersInRadius(5000))//pick players to make list of 2 areas
                    {
                        if (player != null)
                        {
                            if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                            {
                                if (player.IsWithinRadius(point1, 200))//location of main boss
                                {
                                    if (!player_in_range.Contains(player))
                                        player_in_range.Add(player);
                                }
                                else
                                {
                                    if (player_in_range.Contains(player))
                                        player_in_range.Remove(player);//remove player if he leaves gloc radius
                                }
                                if (player.IsWithinRadius(point2, 200))//location of clone-boss
                                {
                                    if (!player_in_range2.Contains(player))
                                        player_in_range2.Add(player);
                                }
                                else
                                {
                                    if (player_in_range2.Contains(player))
                                        player_in_range2.Remove(player);//remove player if he leaves gloc radius
                                }
                            }
                        }
                    }
                    if (player_in_range.Count > 0 && player_in_range2.Count > 0)
                    {/* do nothing */}
                    else
                        Body.CastSpell(OlcasgeanDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));

                    if (player_to_port.Count > 0 && !_teleportScheduled && Body.HealthPercent <= 50)
                    {
                        _teleportTarget = player_to_port[Util.Random(0, player_to_port.Count - 1)];

                        if (_teleportTarget != null && _teleportTarget.IsAlive)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DoPort), Util.Random(12000,20000));//do teleport every 12-20s
                            _teleportScheduled = true;
                        }
                    }
                }
            }
            base.Think();
        }
        private void SpawnCopy(Olcasgean boss)
        {
            Olcasgean2 copy = new Olcasgean2();
            copy.X = 40170;
            copy.Y = 62600;
            copy.Z = 11681;
            copy.Heading = 2491;
            copy.CurrentRegion = Body.CurrentRegion;
            copy.Main = boss;
            copy.AddToWorld();
            boss.Copy = copy;
        }
        #endregion
        #region DOPort
        private static readonly (int X, int Y, int Z, ushort Heading)[] _portDestinations =
        [
            (38399, 60893, 12242, 3548),
            (38564, 64161, 12242, 2382),
            (41580, 62325, 12242, 890)
        ];

        public int DoPort(ECSGameTimer timer)
        {
            if (Body.HealthPercent <= 50 &&
                _teleportTarget != null &&
                _teleportTarget.IsAlive &&
                !ported_player.Contains(_teleportTarget))
            {
                (int x, int y, int z, ushort heading) = _portDestinations[Util.Random(0, _portDestinations.Length - 1)];
                _teleportTarget.MoveTo(Body.CurrentRegionID, x, y, z, heading);
                _teleportTarget.Client.Out.SendMessage(Body.Name + " throws you away...", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                ported_player.Add(_teleportTarget);
            }

            _teleportScheduled = false;
            return 0;
        }
        #endregion

        public Spell m_OlcasgeanDD;
        public Spell OlcasgeanDD
        {
            get
            {
                if (m_OlcasgeanDD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 4;
                    spell.RecastDelay = 1;
                    spell.ClientEffect = 11027;
                    spell.Icon = 11027;
                    spell.TooltipId = 11027;
                    spell.Name = "Olcasgean's Root";
                    spell.Damage = 500;
                    spell.Radius = 350;
                    spell.Range = 1800;
                    spell.SpellID = 11717;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.DamageType = (int)eDamageType.Matter;
                    m_OlcasgeanDD = new Spell(spell, 70);
                }
                return m_OlcasgeanDD;
            }
        }
    }
}
#endregion Olcasgean Brain

#region Olcasgean2
namespace DOL.GS
{
    public class Olcasgean2 : GameEpicBoss
    {
        public Olcasgean Main { get; set; }

        public Olcasgean2()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }

        public override int MaxHealth
        {
            get { return 250000; }
        }
        public override int MeleeAttackRange => 1500;
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            base.TakeDamage(source, damageType, damageAmount, criticalAmount);

            // Both bodies share a single health pool.
            if (Main != null && Main.IsAlive)
                Main.Health = Health;
        }
        #region Custom Methods
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
            }
        }
        protected void ReportNews(GameObject killer)
        {
            int numPlayers = AwardEpicEncounterKillPoint();
            String message = String.Format("{0} has been slain by a force of {1} warriors!", Name, numPlayers);
            NewsMgr.CreateNews(message, killer.Realm, eNewsType.PvE, true);

            if (Properties.GUILD_MERIT_ON_DRAGON_KILL > 0)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player.IsEligibleToGiveMeritPoints)
                    {
                        GuildEventHandler.MeritForNPCKilled(player, this, Properties.GUILD_MERIT_ON_DRAGON_KILL);
                    }
                }
            }
        }
        protected int AwardEpicEncounterKillPoint()
        {
            int count = 0;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.KillsEpicBoss++;
                count++;
            }
            return count;
        }
        public override void ProcessDeath(GameObject killer)
        {
            if (Main != null && Main.IsAlive)
                Main.Die(Main);

            foreach (GameNPC npc in GetNPCsInRadius(10000))
            {
                if (npc != null && npc.IsAlive)
                {
                    if (npc.Brain is VortexBrain || npc.Brain is WaterfallAntipassBrain)
                        npc.RemoveFromWorld();
                }
            }

            bool canReportNews = true;
            // due to issues with attackers the following code will send a notify to all in area in order to force quest credit
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Notify(GameLivingEvent.EnemyKilled, killer, new EnemyKilledEventArgs(this));

                if (canReportNews && GameServer.ServerRules.CanGenerateNews(player) == false)
                {
                    if (player.Client.Account.PrivLevel == (int)ePrivLevel.Player)
                        canReportNews = false;
                }
            }
            if (canReportNews)
            {
                if(killer is not Olcasgean or Olcasgean2)
                    ReportNews(killer);
            }
            base.ProcessDeath(killer);
        }
        #endregion
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164624);
            LoadTemplate(npcTemplate);

            Flags = (GameNPC.eFlags)156;
            LoadedFromScript = true;
            RespawnInterval = -1;

            Faction = FactionMgr.GetFactionByID(96);
            OlcasgeanBrain2 sBrain = new OlcasgeanBrain2();
            SetOwnBrain(sBrain);
            return base.AddToWorld();
        }

        public override void OnAttackedByEnemy(AttackData ad)// on Boss being attacked
        {
            if (ad != null && ad.Damage > 0 && ad.Attacker != null && ad.Attacker.IsAlive && ad.Attacker is GamePlayer)
            {
                if (HealthPercent <= 50)
                {
                    if (Util.Chance(50))
                        CastSpell(OlcasgeanDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                }
                if (HealthPercent > 50)
                    if (Util.Chance(25))
                        CastSpell(OlcasgeanDD2, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.OnAttackedByEnemy(ad);
        }
        private Spell m_OlcasgeanDD2;
        private Spell OlcasgeanDD2
        {
            get
            {
                if (m_OlcasgeanDD2 == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 1;
                    spell.ClientEffect = 11027;
                    spell.Icon = 11027;
                    spell.TooltipId = 11027;
                    spell.Name = "Olcasgean's Root";
                    spell.Damage = 450;
                    spell.Range = 1800;
                    spell.SpellID = 12011;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.DamageType = (int)eDamageType.Matter;
                    m_OlcasgeanDD2 = new Spell(spell, 70);
                }
                return m_OlcasgeanDD2;
            }
        }
    }
}
#endregion Olcasgean

#region Olcasgean2 Brain
namespace DOL.AI.Brain
{
    public class OlcasgeanBrain2 : StandardMobBrain
    {
        private bool _wakeUpStarted;

        public OlcasgeanBrain2()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 3000;
        }

        public override void Think()
        {
            if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }

            if (Body.IsAlive)
            {
                if (Body is Olcasgean2 { Main.AllPrimalsDead: true } && !_wakeUpStarted)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(WakeUpBoss), 25000);
                    _wakeUpStarted = true;
                }
            }
            base.Think();
        }
        private int WakeUpBoss(ECSGameTimer timer)
        {
            Body.Flags = 0;
            return 0;
        } 
    }
}
#endregion Olcasgean Brain

#region Air Elementar
/// <summary>
/// /////////////////////////////////////////      Air Elementar Base
/// </summary>
namespace DOL.GS
{
    public class AirPrimal : GameEpicBoss
    {
        public const short PATROL_SPEED = 250;

        // Flying circle above the room.
        private static readonly (int X, int Y, int Z)[] _patrolPoints =
        [
            (39120, 61387, 12372),
            (38531, 61871, 12372),
            (38361, 62497, 12372),
            (38525, 63092, 12372),
            (38936, 63471, 12372),
            (39462, 63707, 12372),
            (40028, 63647, 12372),
            (40633, 63236, 12372),
            (40817, 62737, 12372),
            (40760, 62068, 12372),
            (40355, 61543, 12372)
        ];

        public Olcasgean Olcasgean { get; set; }

        public AirPrimal()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GameSummonedPet || source is TurretPet)
            {
                base.TakeDamage(source, damageType, 5, 5);//pets deal less dmg to this primal to avoid being killed to fast
            }
            else//take dmg
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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
            get
            {
                return 900;//low health, as source says 1 volcanic pillar 5 could one shot it
            }
        }
        public override int MeleeAttackRange => 350;
        public override void Follow(GameObject target, long minDistance, long maxDistance)
        {
        }
        public override void StopFollowing()
        {
        }
        public override void ProcessDeath(GameObject killer)
        {
            Olcasgean?.OnPrimalDied();
            base.ProcessDeath(killer);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159435);
            LoadTemplate(npcTemplate);
            RespawnInterval = -1;//will not respawn
            Faction = FactionMgr.GetFactionByID(96);
            Flags = eFlags.FLYING;
            CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, PATROL_SPEED, _patrolPoints);

            AirPrimalBrain sBrain = new AirPrimalBrain();
            SetOwnBrain(sBrain);
            return base.AddToWorld();
        }
    }
}
/// <summary>
/// /////////////////////////////////////////      Air Elementar Brain
/// </summary>
namespace DOL.AI.Brain
{
    public class AirPrimalBrain : StandardMobBrain
    {
        private class CastDdTimer : ECSGameTimerWrapperBase
        {
            private AirPrimalBrain _brain;

            public GamePlayer Target { get; set; }

            public CastDdTimer(AirPrimalBrain brain, GameObject owner) : base(owner)
            {
                _brain = brain;
                Start(1500);
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                _brain.CastDD(Target);
                return 0;
            }
        }

        private CastDdTimer _castDdTimer;

        public AirPrimalBrain(): base()
        {
            AggroLevel = 100;
            AggroRange = 0;
            ThinkInterval = 2000;
            _castDdTimer = new(this, Body);
        }

        public void CastDD(GamePlayer target)
        {
            GameObject previousTarget = Body.TargetObject;
            Body.TargetObject = target;
            Body.CastSpell(AirDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            Body.TargetObject = previousTarget;
        }

        public void CastMez(GamePlayer target)
        {
            GameObject previousTarget = Body.TargetObject;
            Body.TargetObject = target;
            Body.CastSpell(Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            Body.TargetObject = previousTarget;
        }

        public void PickRandomTarget()
        {
            if (_castDdTimer.IsAlive)
                return;

            List<GamePlayer> enemies = GameLoop.GetListForTick<GamePlayer>();

            foreach (GamePlayer player in Body.GetPlayersInRadius(1100))
            {
                if (GameServer.ServerRules.IsAllowedToAttack(Body, player, true))
                    enemies.Add(player);
            }

            if (enemies.Count == 0)
                return;

            GamePlayer randomTarget = enemies[Util.Random(0, enemies.Count - 1)];

            if (Util.Chance(15))
            {
                if (!randomTarget.effectListComponent.ContainsEffectForEffectType(eEffect.Mez))
                    CastMez(randomTarget);
            }

            _castDdTimer.Target = randomTarget;
            _castDdTimer.Start();
        }

        public override void Think()
        {
            // The primal never attacks directly and endlessly circles the room, even while in combat.
            // The aggro state stops path movement, so restart it whenever it's interrupted.
            if (Body.IsAlive && !Body.IsMovingOnPath)
                Body.MoveOnPath(AirPrimal.PATROL_SPEED);

            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        AddToAggroList(player);
                }
            }

            if (Body.InCombatInLast(20 * 1000) == false && this.Body.InCombatInLast(25 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }

            if (Body.IsAlive)
                PickRandomTarget();

            base.Think();
        }

        public Spell m_AirDD;
        public Spell AirDD
        {
            get
            {
                if (m_AirDD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 2;
                    spell.ClientEffect = 479;
                    spell.Icon = 479;
                    spell.TooltipId = 479;
                    spell.Damage = 600;
                    spell.Range = 1200;
                    spell.SpellID = 11718;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Spirit;
                    m_AirDD = new Spell(spell, 70);
                }
                return m_AirDD;
            }
        }
        protected Spell m_mezSpell;
        protected Spell Mezz
        {
            get
            {
                if (m_mezSpell == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 10;
                    spell.ClientEffect = 466;
                    spell.Icon = 466;
                    spell.TooltipId = 466;
                    spell.Name = "Mesmerized";
                    spell.Range = 1500;
                    spell.Radius = 350;
                    spell.SpellID = 11719;
                    spell.Duration = 60;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = "Mesmerize";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Spirit; //Spirit DMG Type
                    m_mezSpell = new Spell(spell, 70);
                }
                return m_mezSpell;
            }
        }
    }
}
#endregion Air elementar

#region Water Elementar
/// <summary>
/// /////////////////////////////////////////      Water Elementar Base
/// </summary>
namespace DOL.GS
{
    public class WaterPrimal : GameEpicBoss
    {
        public Olcasgean Olcasgean { get; set; }

        public WaterPrimal()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }
        public override void ProcessDeath(GameObject killer)
        {
            Olcasgean?.OnPrimalDied();
            base.ProcessDeath(killer);
        }

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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
            get
            {
                return 125000;
            }
        }

        public override int MeleeAttackRange => 350;
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            // Only players and their pets can damage the primal.
            if (source is not GamePlayer and not GameSummonedPet)
                return;

            // Take no damage while retreating to the waterfall.
            if (Brain is WaterPrimalBrain { DontAttack: true })
            {
                GamePlayer player = source as GamePlayer ?? (source as GameSummonedPet).Owner as GamePlayer;
                player?.Out.SendMessage($"{Name} is under waterfall effect!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                base.TakeDamage(source, damageType, 0, 0);
                return;
            }

            base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159438);
            LoadTemplate(npcTemplate);

            CurrentRegionID = 191;//galladoria
            Flags ^= eFlags.GHOST;//ghost

            RespawnInterval = -1;//will not respawn
            Faction = FactionMgr.GetFactionByID(96);
            WaterPrimalBrain sBrain = new WaterPrimalBrain();
            SetOwnBrain(sBrain);
            return base.AddToWorld();
        }
    }
}

/// <summary>
/// /////////////////////////////////////////     Water Elementar Brain
/// </summary>
namespace DOL.AI.Brain
{
    public class WaterPrimalBrain : StandardMobBrain
    {
        private static readonly Point3D _waterfallPoint = new(39652, 60831, 11893);

        private bool _lowHealthTriggered;
        private bool _retreatStarted;

        public bool DontAttack { get; private set; }

        public WaterPrimalBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 5000;
        }

        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            if (e == GameObjectEvent.AddToWorld)
                Body.PathTo(_waterfallPoint, 300);

            base.Notify(e, sender, args);
        }
        public override void AttackMostWanted()
        {
            if (DontAttack)
                return;

            base.AttackMostWanted();
        }
        public int CanAttack(ECSGameTimer timer)
        {
            DontAttack = false;
            AggroRange = 1500;
            return 0;
        }
        public void LowOnHealth()
        {
            if (Body.HealthPercent < 30 && !_lowHealthTriggered)
            {
                if (Body.IsWithinRadius(_waterfallPoint, 80))
                {
                    Body.CastSpell(WaterEffect, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    Body.Health += Body.MaxHealth / 6;
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CanAttack), 5000);
                    _lowHealthTriggered = true;
                }
                else
                {
                    if (!_retreatStarted)
                    {
                        ClearAggroList();
                        _retreatStarted = true;
                    }
                    Body.PathTo(_waterfallPoint, 300);
                    DontAttack = true;
                }
            }
        }
        public override void Think()
        {
            if (HasAggro && Body.TargetObject != null)
            {
                if (Util.Chance(10))
                {
                    if (!_isTargetTeleported)
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickTeleportPlayer), Util.Random(25000, 45000));
                        _isTargetTeleported = true;
                    }
                }
            }
            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
                DontAttack = false;
                _lowHealthTriggered = false;
                _retreatStarted = false;
                _isTargetTeleported = false;
                _teleportTarget = null;
                AggroRange = 600;
            }
            LowOnHealth();
            base.Think();
        }
        #region Pick player to port
        private bool _isTargetTeleported;
        private GamePlayer _teleportTarget;
        List<GamePlayer> Port_Enemys = new List<GamePlayer>();
        public int PickTeleportPlayer(ECSGameTimer timer)
        {
            if (Body.IsAlive && HasAggro)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!Port_Enemys.Contains(player))
                            {
                                if (player != Body.TargetObject)
                                {
                                    Port_Enemys.Add(player);
                                }
                            }
                        }
                    }
                }
                if (Port_Enemys.Count == 0)
                {
                    _teleportTarget = null;//reset random target to null
                    _isTargetTeleported = false;
                }
                else
                {
                    _teleportTarget = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];

                    if (_teleportTarget != null && _teleportTarget.IsAlive)
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(TeleportPlayer), 3000);
                    }
                }
            }
            return 0;
        }
        public int TeleportPlayer(ECSGameTimer timer)
        {
            if (_teleportTarget != null && _teleportTarget.IsAlive && HasAggro)
            {
                switch (Util.Random(1, 2))
                {
                    case 1: _teleportTarget.MoveTo(Body.CurrentRegionID, 38626, 60891, 11771, 2881); break;
                    case 2: _teleportTarget.MoveTo(Body.CurrentRegionID, 40606, 60868, 11721, 1095); break;
                }
                Port_Enemys.Remove(_teleportTarget);
                _teleportTarget = null;//reset random target to null
                _isTargetTeleported = false;
            }
            return 0;
        }
        #endregion
        private Spell m_WaterEffect;
        private Spell WaterEffect
        {
            get
            {
                if (m_WaterEffect == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 5;
                    spell.Duration = 5;
                    spell.ClientEffect = 4323;
                    spell.Icon = 4323;
                    spell.Value = 1;
                    spell.Name = "Machanism Effect";
                    spell.TooltipId = 4323;
                    spell.SpellID = 11865;
                    spell.Target = eSpellTarget.SELF.ToString();
                    spell.Type = eSpellType.PowerRegenBuff.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    m_WaterEffect = new Spell(spell, 70);
                }
                return m_WaterEffect;
            }
        }
    }
}
#endregion Water Elementar

#region Fire Elementar
/// <summary>
/// /////////////////////////////////////////      Fire Elementar Base
/// </summary>
namespace DOL.GS
{
    public class FirePrimal : GameEpicBoss
    {
        public const short PATROL_SPEED = 200;

        private static readonly (int X, int Y, int Z)[] _patrolPoints =
        [
            (40142, 63014, 11670),
            (40368, 62034, 11676),
            (39134, 61783, 11688),
            (38989, 62939, 11694)
        ];

        public Olcasgean Olcasgean { get; set; }

        public FirePrimal()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override void ProcessDeath(GameObject killer)
        {
            Olcasgean?.OnPrimalDied();
            base.ProcessDeath(killer);
        }

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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
            get
            {
                return 125000;
            }
        }
        public override int MeleeAttackRange => 350;

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159437);
            LoadTemplate(npcTemplate);

            Flags ^= eFlags.FLYING;//flying
            RespawnInterval = -1;//will not respawn
            Faction = FactionMgr.GetFactionByID(96);
            CurrentPathPoint = MovementMgr.CreatePath(EPathType.Loop, PATROL_SPEED, _patrolPoints);
            Spells = [FirePrimalBrain.DamageShield];

            FirePrimalBrain sBrain = new FirePrimalBrain();
            SetOwnBrain(sBrain);
            return base.AddToWorld();
        }
    }
}

/// <summary>
/// /////////////////////////////////////////      Fire Elementar Brain
/// </summary>
namespace DOL.AI.Brain
{
    public class FirePrimalBrain : StandardMobBrain
    {
        private bool _canSpawnFire;

        public FirePrimalBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 2500;
        }

        public override void Think()
        {
            // The primal never attacks directly and endlessly circles the room, even while in combat.
            // The aggro state stops path movement, so restart it whenever it's interrupted.
            if (Body.IsAlive && !Body.IsMovingOnPath)
                Body.MoveOnPath(FirePrimal.PATROL_SPEED);

            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (Body.IsAlive)
            {
                CheckSpells(eCheckSpellType.Defensive);//keeps the damage shield up
                foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                            AddToAggroList(player);
                    }
                }
                if (!_canSpawnFire)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnFire), 1000);
                    _canSpawnFire = true;
                }
            }
            base.Think();
        }
        public int SpawnFire(ECSGameTimer timer)
        {
            if (Body.IsAlive)
            {
                TrailOfFire npc = new TrailOfFire();
                npc.X = Body.X;
                npc.Y = Body.Y;
                npc.Z = Body.Z;
                npc.RespawnInterval = -1;
                npc.Heading = Body.Heading;
                npc.CurrentRegion = Body.CurrentRegion;
                npc.AddToWorld();
                new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(ResetSpawnFire), 1000);
            }
            return 0;
        }
        public int ResetSpawnFire(ECSGameTimer timer)
        {
            _canSpawnFire = false;
            return 0;
        }
        internal static Spell DamageShield => ScriptSpells.GetOrCreate("FirePrimalDS", 70, static spell =>
        {
            spell.CastTime = 0;
            spell.RecastDelay = 60;
            spell.ClientEffect = 57;
            spell.Icon = 57;
            spell.Damage = 80;
            spell.Duration = 60;
            spell.Name = "Fire Primal Damage Shield";
            spell.TooltipId = 57;
            spell.SpellID = 11721;
            spell.Target = eSpellTarget.SELF.ToString();
            spell.Type = "DamageShield";
            spell.Uninterruptible = true;
            spell.MoveCast = true;
            spell.DamageType = (int)eDamageType.Heat;
        });
    }
}
#region trail of fire
namespace DOL.GS
{
    public class TrailOfFire : GameNPC
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TrailOfFire()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 99; // dmg reduction for melee dmg
                case eDamageType.Crush: return 99; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 99; // dmg reduction for melee dmg
                default: return 99; // dmg reduction for rest resists
            }
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 800;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }
        public override int MaxHealth
        {
            get
            {
                return 10000;
            }
        }

        private int Show_Effect(ECSGameTimer timer)
        {
            if (IsAlive)
            {
                foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendSpellEffectAnimation(this, this, 5906, 0, false, 0x01);

                SetGroundTarget(X, Y, Z);

                if (!IsCasting)
                    CastSpell(FireGroundDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);

                return 2000;
            }

            return 0;
        }

        private int RemoveFire(ECSGameTimer timer)
        {
            if (IsAlive)
                RemoveFromWorld();
            return 0;
        }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
        public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
        public override bool AddToWorld()
        {
            Model = 2000;
            Name = "trail of fire";
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.CANTTARGET;
            //Flags ^= eFlags.STATUE;
            MaxSpeedBase = 0;
            Level = 80;
            Size = 10;

            RespawnInterval = -1;//will not respawn
            Faction = FactionMgr.GetFactionByID(96);

            TrailOfFireBrain sBrain = new TrailOfFireBrain();
            SetOwnBrain(sBrain);
            //Brain.Start();
            bool success = base.AddToWorld();
            if (success)
            {
                SetGroundTarget(X, Y, Z);
                if (!IsCasting)
                    CastSpell(FireGroundDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(RemoveFire), 6000);
            }
            return success;
        }
        private Spell m_FireGroundDD;
        private Spell FireGroundDD
        {
            get
            {
                if (m_FireGroundDD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 2;
                    spell.ClientEffect = 368;
                    spell.Icon = 368;
                    spell.TooltipId = 368;
                    spell.Damage = 220;
                    spell.Range = 1200;
                    spell.Radius = 450;
                    spell.SpellID = 11866;
                    spell.Target = eSpellTarget.AREA.ToString();
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Heat;
                    m_FireGroundDD = new Spell(spell, 70);
                }
                return m_FireGroundDD;
            }
        }
    }
}
namespace DOL.AI.Brain
{
    public class TrailOfFireBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public TrailOfFireBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
        }
        public override void Think()
        {
            base.Think();
        }
    }
}
#endregion
#endregion Fire Elementar

#region Earth Elementar
/// <summary>
/// /////////////////////////////////////////      Earth Elementar Base
/// </summary>
namespace DOL.GS
{
    public class EarthPrimal : GameEpicBoss
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Olcasgean Olcasgean { get; set; }

        public EarthPrimal()
            : base()
        {
        }
        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 40;// dmg reduction for melee dmg
                case eDamageType.Crush: return 40;// dmg reduction for melee dmg
                case eDamageType.Thrust: return 40;// dmg reduction for melee dmg
                default: return 70;// dmg reduction for rest resists
            }
        }
        public override void ProcessDeath(GameObject killer)
        {
            Olcasgean?.OnPrimalDied();
            foreach (GameNPC npc in GetNPCsInRadius(8000))
            {
                if (npc != null)
                {
                    if (npc.IsAlive)
                    {
                        if (npc.Brain is GuardianEarthmenderBrain || npc.Brain is MagicalEarthmenderBrain || npc.Brain is NaturalEarthmenderBrain || npc.Brain is ShadowyEarthmenderBrain)
                            npc.Die(null);
                    }
                }
            }
            base.ProcessDeath(killer);
        }

        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
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
            get { return 125000; }
        }
        public override int MeleeAttackRange => 350;
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159436);
            LoadTemplate(npcTemplate);
            TetherRange = 890;

            RespawnInterval = -1;//will not respawn
            Faction = FactionMgr.GetFactionByID(96);
            Spells = [EarthPrimalBrain.Root];

            EarthPrimalBrain sBrain = new EarthPrimalBrain();
            SetOwnBrain(sBrain);
            Brain.Start();
            base.AddToWorld();
            return true;
        }
    }
}
/// <summary>
/// /////////////////////////////////////////  Earth Elementar Brain ////////////////////////////
/// </summary>
namespace DOL.AI.Brain
{
    public class EarthPrimalBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public EarthPrimalBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
            ThinkInterval = 1000;
        }
        public int TargetIsOut(ECSGameTimer timer)
        {
            if (Body.IsAlive)
            {
                if (HasAggro && Body.TargetObject != null)
                {
                    Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!target.IsWithinRadius(spawn, 900) && target != null && target.IsAlive)
                    {
                        if (RemoveFromAggroList(target))
                        {
                            CalculateNextAttackTarget();
                            _canSwitchTarget = false;
                        }
                    }
                }
            }
            return 0;
        }
        private bool _canSwitchTarget;
        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                Body.Health = Body.MaxHealth;
                _canSwitchTarget = false;
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159436);
                Body.MaxSpeedBase = npcTemplate.MaxSpeed;
            }
            if (Body.IsOutOfTetherRange && HasAggro && Body.TargetObject != null)
            {
                Body.StopFollowing();
                Point3D spawn = new Point3D(Body.SpawnPoint.X, Body.SpawnPoint.Y, Body.SpawnPoint.Z);
                GameLiving target = Body.TargetObject as GameLiving;
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159436);
                if (target != null)
                {
                    if (!target.IsWithinRadius(spawn, 900))
                    {
                        Body.MaxSpeedBase = 0;
                        if (!_canSwitchTarget)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(TargetIsOut), 5000);
                            _canSwitchTarget = true;
                        }
                    }
                    else
                        Body.MaxSpeedBase = npcTemplate.MaxSpeed;
                }
            }
            base.Think();
        }
        internal static Spell Root => ScriptSpells.GetOrCreate("EarthPrimalRoot", 70, static spell =>
        {
            spell.CastTime = 0;
            spell.RecastDelay = Util.Random(15, 25);
            spell.ClientEffect = 277;
            spell.Icon = 277;
            spell.TooltipId = 277;
            spell.Name = "Roots from Earth";
            spell.Value = 99;
            spell.Duration = 60;
            spell.Range = 1500;
            spell.SpellID = 11726;
            spell.Target = eSpellTarget.ENEMY.ToString();
            spell.Type = "SpeedDecrease";
            spell.Uninterruptible = true;
            spell.MoveCast = true;
            spell.DamageType = (int)eDamageType.Cold;
        });
    }
}

//////////////////////////////////////////////// Earthmenders ////////////////////////////////

/// <summary>
/// ////////////////////////////////////////////Guardian Earthmender Base
/// </summary>
#region Guardian Earthmender
namespace DOL.GS
{
    public class GuardianEarthmender : GameNPC
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public GuardianEarthmender()
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
                default: return 60; // dmg reduction for rest resists
            }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer)
            {
                GamePlayer truc = source as GamePlayer;

                if (truc.CharacterClass.ID == 43 || truc.CharacterClass.ID == 44 || truc.CharacterClass.ID == 45 || truc.CharacterClass.ID == 56 || truc.CharacterClass.ID == 55)// bm,hero,champ,vw,ani
                {
                    if (source is GamePlayer)
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                else
                {
                    truc.Out.SendMessage(Name + " is immune to your damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
            }
            if (source is GameSummonedPet)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 300;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get
            {
                return 60000;
            }
        }
        public override bool AddToWorld()
        {
            Model = 951;
            Name = "Guardian Earthmender";
            Size = 150;
            Level = 73;
            Realm = 0;
            CurrentRegionID = 191;//galladoria
            MaxSpeedBase = 0;

            RespawnInterval = -1;//will not respawn
            Gender = eGender.Neutral;
            Faction = FactionMgr.GetFactionByID(96);
            MeleeDamageType = eDamageType.Slash;
            BodyType = 5;

            Spells = [EarthmenderBrain.Heal];
            GuardianEarthmenderBrain sBrain = new GuardianEarthmenderBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            Brain.Start();
            base.AddToWorld();
            return true;
        }
    }
}
/// <summary>
/// /////////////////////////////////////////      Guardian Earthmender Brain
/// </summary>
namespace DOL.AI.Brain
{
    public abstract class EarthmenderBrain : StandardMobBrain
    {
        public EarthmenderBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }

        public override void AttackMostWanted()
        {
        }

        public override void Think()
        {
            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }

            // Defensive spells are only checked by out of combat states, so a fighting earthmender has to check them itself.
            // Stopping the attack mirrors what `AttackMostWanted` does for offensive spells, and lets the cast actually start.
            if (CheckSpells(eCheckSpellType.Defensive))
                Body.StopAttack();

            base.Think();
        }

        protected override GameLiving FindTargetForDefensiveSpell(Spell spell)
        {
            if (spell.SpellType is not eSpellType.Heal)
                return base.FindTargetForDefensiveSpell(spell);

            foreach (GameNPC npc in Body.GetNPCsInRadius(1000))
            {
                if (npc != Body && npc.IsAlive && npc.HealthPercent < 100 && npc.Brain is EarthmenderBrain or EarthPrimalBrain)
                    return npc;
            }

            return null;
        }

        internal static Spell Heal => ScriptSpells.GetOrCreate("EarthmenderHeal", 70, static spell =>
        {
            spell.CastTime = 3;
            spell.RecastDelay = 0;
            spell.ClientEffect = 4858;
            spell.Icon = 4858;
            spell.TooltipId = 4858;
            spell.Value = 2000;
            spell.Range = 1500;
            spell.SpellID = 11722;
            spell.Target = eSpellTarget.REALM.ToString();
            spell.Type = eSpellType.Heal.ToString();
            spell.Uninterruptible = true;
            spell.MoveCast = true;
        });
    }

    public class GuardianEarthmenderBrain : EarthmenderBrain { }
}
#endregion
/// <summary>
/// ////////////////////////////////////////////Magical Earthmender Base
/// </summary>
#region Magical Earthmender
namespace DOL.GS
{
    public class MagicalEarthmender : GameNPC
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MagicalEarthmender()
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
                default: return 60; // dmg reduction for rest resists
            }
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer)
            {
                GamePlayer truc = source as GamePlayer;

                if (truc.CharacterClass.ID == 40 || truc.CharacterClass.ID == 41 || truc.CharacterClass.ID == 42 || truc.CharacterClass.ID == 56 || truc.CharacterClass.ID == 55)// eld,ench,menta,vw,ani
                {
                    if (source is GamePlayer)
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                else
                {
                    truc.Out.SendMessage(Name + " is immune to your damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
            }
            if (source is GameSummonedPet)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 300;
        }

        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get
            {
                return 60000;
            }
        }
        public override bool AddToWorld()
        {
            Model = 951;
            Name = "Magical Earthmender";
            Size = 150;
            Level = 73;
            Realm = 0;
            CurrentRegionID = 191;//galladoria
            MaxSpeedBase = 0;


            RespawnInterval = -1;//will not respawn
            Gender = eGender.Neutral;
            Faction = FactionMgr.GetFactionByID(96);
            MeleeDamageType = eDamageType.Slash;
            BodyType = 5;

            Spells = [EarthmenderBrain.Heal];
            MagicalEarthmenderBrain sBrain = new MagicalEarthmenderBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            Brain.Start();
            base.AddToWorld();
            return true;
        }
    }
}

/// <summary>
/// /////////////////////////////////////////      Magical Earthmender Brain
/// </summary>
namespace DOL.AI.Brain
{
    public class MagicalEarthmenderBrain : EarthmenderBrain { }
}
#endregion
/// <summary>
/// ////////////////////////////////////////////Natural Earthmender Base
/// </summary>
#region Natural Earthmender
namespace DOL.GS
{
    public class NaturalEarthmender : GameNPC
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public NaturalEarthmender()
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
                default: return 60; // dmg reduction for rest resists
            }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer)
            {
                GamePlayer truc = source as GamePlayer;

                if (truc.CharacterClass.ID == 48 || truc.CharacterClass.ID == 47 || truc.CharacterClass.ID == 46 || truc.CharacterClass.ID == 56 || truc.CharacterClass.ID == 55)// bard,druid,warden,ani,vw
                {
                    if (source is GamePlayer)
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                else
                {
                    truc.Out.SendMessage(Name + " is immune to your damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
            }
            if (source is GameSummonedPet)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 300;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get
            {
                return 60000;
            }
        }
        public override bool AddToWorld()
        {
            Model = 951;
            Name = "Natural Earthmender";
            Size = 150;
            Level = 73;
            Realm = 0;
            CurrentRegionID = 191;//galladoria
            MaxSpeedBase = 0;

            RespawnInterval = -1;//will not respawn
            Gender = eGender.Neutral;
            Faction = FactionMgr.GetFactionByID(96);
            MeleeDamageType = eDamageType.Slash;
            BodyType = 5;

            Spells = [EarthmenderBrain.Heal];
            NaturalEarthmenderBrain sBrain = new NaturalEarthmenderBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            Brain.Start();
            base.AddToWorld();
            return true;
        }
    }
}
/// <summary>
/// /////////////////////////////////////////      Natural Earthmender Brain
/// </summary>
namespace DOL.AI.Brain
{
    public class NaturalEarthmenderBrain : EarthmenderBrain { }
}
#endregion
/// <summary>
/// ////////////////////////////////////////////Shadowy Earthmender Base
/// </summary>
#region Shadowy Earthmender
namespace DOL.GS
{
    public class ShadowyEarthmender : GameNPC
    {
        private static new readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ShadowyEarthmender()
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
                default: return 60; // dmg reduction for rest resists
            }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer)
            {
                GamePlayer truc = source as GamePlayer;

                if (truc.CharacterClass.ID == 49 || truc.CharacterClass.ID == 50 || truc.CharacterClass.ID == 56 || truc.CharacterClass.ID == 55)// ns,ranger,vw,ani
                {
                    if (source is GamePlayer)
                    {
                        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                    }
                }
                else
                {
                    truc.Out.SendMessage(Name + " is immune to your damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
            }
            if (source is GameSummonedPet)
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
        public override void StartAttack(GameObject target)
        {
        }
        public override bool HasAbility(string keyName)
        {
            if (IsAlive && keyName == GS.Abilities.CCImmunity)
                return true;

            return base.HasAbility(keyName);
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 300;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.20;
        }
        public override int MaxHealth
        {
            get
            {
                return 60000;
            }
        }
        public override bool AddToWorld()
        {
            Model = 951;
            Name = "Shadowy Earthmender";
            Size = 150;
            Level = 73;
            Realm = 0;
            CurrentRegionID = 191;//galladoria
            MaxSpeedBase = 0;

            RespawnInterval = -1;//will not respawn
            Gender = eGender.Neutral;
            Faction = FactionMgr.GetFactionByID(96);
            MeleeDamageType = eDamageType.Slash;
            BodyType = 5;

            Spells = [EarthmenderBrain.Heal];
            ShadowyEarthmenderBrain sBrain = new ShadowyEarthmenderBrain();
            SetOwnBrain(sBrain);
            sBrain.AggroLevel = 100;
            sBrain.AggroRange = 500;
            Brain.Start();
            base.AddToWorld();
            return true;
        }
    }
}
/// <summary>
/// /////////////////////////////////////////      Shadowy Earthmender Brain
/// </summary>
namespace DOL.AI.Brain
{
    public class ShadowyEarthmenderBrain : EarthmenderBrain { }
}
#endregion
#endregion Earth Elementar

#region Vortex
namespace DOL.GS
{
    public class Vortex : GameNPC
    {
        public Vortex() : base() { }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GameSummonedPet)
            {
                if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                    || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                    || damageType == eDamageType.Slash)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(Name + " is immune to any damage!", eChatType.CT_System, eChatLoc.CL_ChatWindow);

                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
                else
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }
        public override int MaxHealth
        {
            get { return 5000; }
        }
        public override int MeleeAttackRange => 200;
        public override bool CanDropLoot => false;
        public override void Die(GameObject killer)
        {
            base.Die(null); // null to not gain experience
        }

        public override bool AddToWorld()
        {
            Model = 1269;
            Name = "Watery Vortex";
            RespawnInterval = 360000;
            Size = 50;
            Level = 87;
            MaxSpeedBase = 0;
            Strength = 15;
            Intelligence = 200;
            Piety = 200;
            Flags ^= eFlags.FLYING;

            Faction = FactionMgr.GetFactionByID(96);
            BodyType = 8;
            Realm = eRealm.None;
            VortexBrain adds = new VortexBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class VortexBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public VortexBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 450;
            ThinkInterval = 3000;
        }
        public override void Think()
        {
            if (Body.InCombat || HasAggro)
            {
                if (!Body.IsCasting)
                    Body.CastSpell(VortexDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            base.Think();
        }
        public Spell m_VortexDD;

        public Spell VortexDD
        {
            get
            {
                if (m_VortexDD == null)
                {
                    DbSpell spell = new DbSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 3;
                    spell.ClientEffect = 11027;
                    spell.Name = "Vortex's Root";
                    spell.Icon = 11027;
                    spell.TooltipId = 11027;
                    spell.Damage = 150;
                    spell.Value = 50;
                    spell.Duration = 36;
                    spell.Range = 500;
                    spell.SpellID = 11727;
                    spell.Target = eSpellTarget.ENEMY.ToString();
                    spell.Type = "DamageSpeedDecrease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Spirit;
                    m_VortexDD = new Spell(spell, 70);
                }
                return m_VortexDD;
            }
        }
    }
}
#endregion Vortex

#region Waterfall Anti-Pass
namespace DOL.GS
{
    public class WaterfallAntipass : GameNPC
    {
        public WaterfallAntipass() : base() { }
        public override bool AddToWorld()
        {
            Model = 665;
            Name = "Waterfall Antipass";
            Size = 50;
            Level = 50;
            MaxSpeedBase = 0;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Flags ^= eFlags.CANTTARGET;

            Faction = FactionMgr.GetFactionByID(96);
            BodyType = 8;
            Realm = eRealm.None;
            WaterfallAntipassBrain adds = new WaterfallAntipassBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}
namespace DOL.AI.Brain
{
    public class WaterfallAntipassBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public WaterfallAntipassBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 250;
            ThinkInterval = 1000;
        }
        public override void Think()
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius((ushort)AggroRange))
            {
                if (player != null)
                {
                    if (player.IsAlive)
                    {
                        if (player.Client.Account.PrivLevel == 1)
                            player.MoveTo(Body.CurrentRegionID, 39664, 60792, 11542, 4078);
                    }
                }
            }
            base.Think();
        }
    }
}
#endregion

#region Visual Effects
namespace DOL.GS
{
    public class OlcasgeanEffect : GameNPC
    {
        public OlcasgeanEffect() : base() { }
        public override bool AddToWorld()
        {
            Model = 665;
            Name = "Root Effect";
            Size = 70;
            Level = 50;
            MaxSpeedBase = 0;
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.PEACE;
            Flags ^= eFlags.CANTTARGET;

            Faction = FactionMgr.GetFactionByID(96);
            BodyType = 8;
            Realm = eRealm.None;
            OlcasgeanEffectBrain adds = new OlcasgeanEffectBrain();
            LoadedFromScript = true;
            SetOwnBrain(adds);
            bool success = base.AddToWorld();
            if (success)
            {
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 500);
            }
            return success;
        }
        protected int Show_Effect(ECSGameTimer timer)
        {
            if (IsAlive)
            {
                foreach (GamePlayer player in this.GetPlayersInRadius(8000))
                {
                    if (player != null)
                        player.Out.SendSpellEffectAnimation(this, this, 11027, 0, false, 0x01);
                }
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(RemoveMob), 3000);
            }
            return 0;
        }
        public int RemoveMob(ECSGameTimer timer)
        {
            if (IsAlive)
                RemoveFromWorld();
            return 0;
        }
    }
}
namespace DOL.AI.Brain
{
    public class OlcasgeanEffectBrain : StandardMobBrain
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public OlcasgeanEffectBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 250;
            ThinkInterval = 1000;
        }
        public override void Think()
        {
            base.Think();
        }
    }
}
#endregion