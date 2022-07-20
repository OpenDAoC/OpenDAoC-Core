using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
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
        public static GameNPC Olcasgean_Initializator = new GameNPC();
        public override int MaxHealth
        {
            get { return 10000; }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
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
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public OIBrain()
            : base()
        {
            ThinkInterval = 1500;
        }

        public static bool startevent = false;
        public static int DeadPrimalsCount = 0;

        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }

        public override void Think()
        {
            Point3D point1 = new Point3D();
            point1.X = 39652; point1.Y = 60831; point1.Z = 11893;//loc of waterfall bridge to start event and pop elementars
            if (Body.IsAlive)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(7000))
                {
                    if (player != null && player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (player.IsWithinRadius(point1, 200) && !startevent && DeadPrimalsCount == 0 && !startevent)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(Message1), 5000);//5s to start
                            startevent = true;
                        }
                    }
                }
            }
            base.Think();
        }
        public int Message1(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("A voice that seems to come from all around you says: 'Intruders have eneteres inner sanctum.'"));
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public bool Master = true;
        public GameNPC Master_NPC;
        public List<GameNPC> CopyNPC;
        public Olcasgean()
            : base()
        {
        }
        public Olcasgean(bool master)
        {
            Master = master;
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
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }
        public override int MaxHealth
        {
            get { return 250000; }
        }
        public override int AttackRange
        {
            get { return 1500; }
            set { }
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
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {

            if (!Master && Master_NPC != null)
                Master_NPC.TakeDamage(source, damageType, damageAmount, criticalAmount);
            else
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                int damageDealt = damageAmount + criticalAmount;

                if (CopyNPC != null && CopyNPC.Count > 0)
                {
                    lock (CopyNPC)
                    {
                        foreach (GameNPC npc in CopyNPC)
                        {
                            if (npc == null) break;
                            npc.Health = Health;//they share same healthpool
                        }
                    }
                }
            }
        }
        #region Custom Methods
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_ChatWindow);
            }
        }
        protected void ReportNews(GameObject killer)
        {
            if (PackageID == "Olcasgean1")
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
        }
        protected int AwardEpicEncounterKillPoint()
        {
            int count = 0;
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.KillsEpicBoss++;
                player.Achieve(AchievementUtils.AchievementNames.Epic_Boss_Kills);
                count++;
            }
            return count;
        }
        public override void Die(GameObject killer)
        {
            if (!(killer is Olcasgean) && !Master && Master_NPC != null)
            {
                Master_NPC.Die(killer);
                OlcasgeanBrain.spawn3 = false;
                //OIBrain.DeadPrimalsCount = 0;
            }
            else
            {
                if (CopyNPC != null && CopyNPC.Count > 0)
                {
                    lock (CopyNPC)
                    {
                        foreach (GameNPC npc in CopyNPC)
                        {
                            if (npc.IsAlive)
                                npc.Die(this);//if one die all others aswell
                            OlcasgeanBrain.spawn3 = false;
                            --OlcasgeanBrain.OlcasgeanCount;
                            //OIBrain.DeadPrimalsCount = 0;
                        }
                    }
                }
                foreach (GameNPC npc in GetNPCsInRadius(10000))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive)
                        {
                            if (npc.Brain is VortexBrain)
                                npc.RemoveFromWorld();

                            if (npc.Brain is WaterfallAntipassBrain)
                                npc.RemoveFromWorld();
                        }
                    }
                }
                CopyNPC = new List<GameNPC>();
                OlcasgeanBrain.spawn3 = false;
                OIBrain.DeadPrimalsCount = 0;

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
                    ReportNews(killer);
                }
                base.Die(killer);
            }
        }
        public override void Follow(GameObject target, int minDistance, int maxDistance)
        {
            if (TargetObject != null)
                return;
            base.Follow(target, minDistance, maxDistance);
        }
        #endregion
        public override bool AddToWorld()
        {
            if (PackageID == "Olcasgean1")
            {
                Flags = (GameNPC.eFlags)156;
                RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
                OIBrain.startevent = false;
                OIBrain.DeadPrimalsCount = 0;
                OlcasgeanBrain.setbossflags = false;
                OlcasgeanBrain.wake_up_boss = false;
                foreach (GameNPC npc in GetNPCsInRadius(3500))
                {
                    if (npc != null && npc.IsAlive)
                    {
                        if (npc.Brain is WaterPrimalBrain || npc.Brain is AirPrimalBrain || npc.Brain is FirePrimalBrain || npc.Brain is EarthPrimalBrain
                            || npc.Brain is GuardianEarthmenderBrain || npc.Brain is MagicalEarthmenderBrain || npc.Brain is NaturalEarthmenderBrain || npc.Brain is ShadowyEarthmenderBrain)
                        {
                            npc.RemoveFromWorld();
                        }
                    }
                }
            }
            else
            {
                RespawnInterval = -1;
                Flags = (GameNPC.eFlags)156;
            }
            base.AddToWorld();
            return true;
        }
        #region Olcasgean Stats
        public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
        public override short Constitution { get => base.Constitution; set => base.Constitution = 100; }
        public override short Empathy { get => base.Empathy; set => base.Empathy = 400; }
        public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
        public override short Piety { get => base.Piety; set => base.Piety = 200; }
        public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
        public override short Quickness { get => base.Quickness; set => base.Quickness = 20; }
        public override short Strength { get => base.Strength; set => base.Strength = 350; }
        #endregion
        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            OlcasgeanBrain ob = new OlcasgeanBrain();
            npcs = WorldMgr.GetNPCsByNameFromRegion("Olcasgean", 191, (eRealm)0);
            if (npcs.Length == 0)
            {
                log.Warn("Olcasgean not found, creating it...");

                log.Warn("Initializing Olcasgean nr1...");
                Olcasgean OLC = new Olcasgean();
                OLC.Name = "Olcasgean";
                OLC.PackageID = "Olcasgean1";
                OLC.Model = 862;
                OLC.Realm = 0;
                OLC.Level = 87;
                OLC.Size = 50;
                OLC.CurrentRegionID = 191;//galladoria

                OLC.X = 40170;
                OLC.Y = 62600;
                OLC.Z = 11681;
                OLC.Heading = 2491;

                OLC.RespawnInterval = Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
                OLC.MeleeDamageType = eDamageType.Slash;
                OLC.Faction = FactionMgr.GetFactionByID(96);
                OLC.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

                OLC.MaxDistance = 2000;
                OLC.TetherRange = 2500;
                OLC.MaxSpeedBase = 0;
                OLC.Heading = 2491;
                OLC.AttackRange = 1500;

                OlcasgeanBrain ubrain = new OlcasgeanBrain();
                ubrain.AggroLevel = 100;
                ubrain.AggroRange = 1500;
                OLC.SetOwnBrain(ubrain);
                OLC.AddToWorld();
                OLC.Brain.Start();
                OLC.SaveIntoDatabase();
            }
            else
                log.Warn("Olcasgean exist ingame, remove it and restart server if you want to add by script code.");
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
                    if (Util.Chance(15))
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
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 2;
                    spell.ClientEffect = 11027;
                    spell.Icon = 11027;
                    spell.TooltipId = 11027;
                    spell.Name = "Olcasgean's Root";
                    spell.Damage = 450;
                    spell.Range = 1800;
                    spell.SpellID = 11901;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.DamageType = (int)eDamageType.Matter;
                    m_OlcasgeanDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OlcasgeanDD);
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public OlcasgeanBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 3000;
        }
        public static int OlcasgeanCount = 0;
        public static bool spawn3 = false;
        public static bool cast1 = true;
        public void SpawnOlcasgean2()
        {
            foreach (GameNPC npc in Body.GetNPCsInRadius(2500))
            {
                if (npc.Brain is OlcasgeanBrain && npc.PackageID == "Olcasgean2")
                    return;
            }
            GameLiving ptarget = CalculateNextAttackTarget();
            Olcasgean Add = new Olcasgean();
            Add.Name = "Olcasgean";
            Add.Model = 862;
            Add.Level = 87;
            Add.Size = 50;
            Add.Faction = FactionMgr.GetFactionByID(96);
            Add.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

            Add.Strength = 5;
            Add.Intelligence = 200;
            Add.Piety = 200;
            Add.Dexterity = 200;
            Add.Constitution = 100;
            Add.Quickness = 80;
            Add.Empathy = 300;
            Add.MaxSpeedBase = 0;
            Add.AttackRange = 1500;
            Add.RespawnInterval = -1;

            Add.X = 39237;
            Add.Y = 62644;
            Add.Z = 11685;
            Add.PackageID = "Olcasgean2";
            Add.CurrentRegionID = 191;
            Add.Heading = 102;
            OlcasgeanBrain smb = new OlcasgeanBrain();
            smb.AggroLevel = 100;
            smb.AggroRange = 1500;
            Add.AddBrain(smb);
            Add.AddToWorld();

            OlcasgeanBrain brain = (OlcasgeanBrain)Add.Brain;
            brain.AddToAggroList(ptarget, 1);
            Add.StartAttack(ptarget);
            Add.Master_NPC = Body;
            Add.Master = false;
            ++OlcasgeanCount;
            if (Body is Olcasgean)
            {
                Olcasgean sg = Body as Olcasgean;
                sg.CopyNPC.Add(Add);
            }
        }
        public override void AddToAggroList(GameLiving living, int aggroamount, bool NaturalAggro)
        {
            base.AddToAggroList(living, aggroamount, NaturalAggro);

            if (!(Body as Olcasgean).Master && (Body as Olcasgean).Master_NPC != null && !((Body as Olcasgean).Master_NPC.Brain as OlcasgeanBrain).HasAggro)
            {
                ((Body as Olcasgean).Master_NPC.Brain as OlcasgeanBrain).AddToAggroList(living, aggroamount, NaturalAggro);
            }

            if ((Body as Olcasgean).Master && (Body as Olcasgean).CopyNPC != null && (Body as Olcasgean).CopyNPC.Count > 0)
            {
                foreach (GameNPC npc in (Body as Olcasgean).CopyNPC)
                {
                    if (npc.IsAlive && !(npc.Brain as OlcasgeanBrain).HasAggro)
                        (npc.Brain as OlcasgeanBrain).AddToAggroList(living, aggroamount, NaturalAggro);
                }
            }
        }
        private int PopBoss(ECSGameTimer timer)
        {
            SpawnOlcasgean2();
            return 0;
        }
        private GamePlayer teleporttarget = null;
        private GamePlayer TeleportTarget//teleport target 
        {
            get { return teleporttarget; }
            set { teleporttarget = value; }
        }

        public static bool setbossflags = false;
        public static bool teleport_player = false;
        public static bool spawn_antipass = false;
        public static bool wake_up_boss = false;
        public static bool spawn_effect = false;
        private bool RemoveAdds = false;
        List<GamePlayer> player_in_range;
        List<GamePlayer> player_in_range2;
        List<GamePlayer> player_to_port;
        List<GamePlayer> ported_player;

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
            spawn_effect = false;
            return 0;
        }
        public void BroadcastMessage(String message)
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                player.Out.SendMessage(message, eChatType.CT_Broadcast, eChatLoc.CL_SystemWindow);
            }
        }
        public int WakeUpBoss(ECSGameTimer timer)
        {
            BroadcastMessage(String.Format("A deep booming voice echoes: 'I am eternal. You and your kind will die.'"));
            foreach (GameNPC boss in Body.GetNPCsInRadius(5000))
            {
                if (boss.Brain is OlcasgeanBrain)
                {
                    if (Body.PackageID == "Olcasgean1" && boss.PackageID == "Olcasgean2")
                    {
                        if (!setbossflags)
                        {
                            Body.Flags = 0;
                            boss.Flags = 0;
                            setbossflags = true;
                        }
                    }
                }
            }
            return 0;
        }
        #region Think()
        public override void Think()
        {
            if (Body.InCombatInLast(60 * 1000) == false && this.Body.InCombatInLast(65 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (!HasAggressionTable())
            {
                //set state to RETURN TO SPAWN
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                teleport_player = false;
                cast1 = true;
                spawn_antipass = false;
                spawn_effect = false;
                TeleportTarget = null;
                if (!RemoveAdds)
                {
                    foreach (GameNPC npc in Body.GetNPCsInRadius(4000))
                    {
                        if (npc.Brain is WaterfallAntipassBrain)
                            npc.RemoveFromWorld();
                    }
                    RemoveAdds = true;
                }
            }
            if (!(Body is Olcasgean))
            {
                base.Think();
                return;
            }
            Olcasgean sg = Body as Olcasgean;

            if (sg.CopyNPC == null || sg.CopyNPC.Count == 0)
                sg.CopyNPC = new List<GameNPC>();

            if (Body.IsAlive)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius(10000))
                {
                    if (player != null && player.IsAlive && !spawn3)
                    {
                        foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                        {
                            if (npc.Name == "Olcasgean" && npc.Brain is OlcasgeanBrain && npc.PackageID != "Olcasgean2")
                            {
                                if (OlcasgeanCount < 1 && !spawn3 && Body.PackageID == "Olcasgean1")
                                {
                                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PopBoss), 1000);//create copy of himself
                                    spawn3 = true;
                                }
                            }
                        }
                    }
                }
                if (OIBrain.DeadPrimalsCount >= 4 && wake_up_boss == false && Body.PackageID == "Olcasgean1")
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(WakeUpBoss), 25000);
                    wake_up_boss = true;
                }
                Point3D point1 = new Point3D();
                point1.X = 40170; point1.Y = 62600; point1.Z = 11681;//location where players need to stay to avoid Olcasgean spamming dd spell

                Point3D point2 = new Point3D();
                point2.X = 39237; point2.Y = 62644; point2.Z = 11685;

                if (HasAggro && Body.TargetObject != null)//Boss in combat
                {
                    RemoveAdds = false;
                    if (spawn_effect == false)
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnEffects), 2000);
                        spawn_effect = true;
                    }
                    if (player_in_range == null)
                        player_in_range = new List<GamePlayer>();

                    if (player_in_range2 == null)
                        player_in_range2 = new List<GamePlayer>();

                    if (player_to_port == null)
                        player_to_port = new List<GamePlayer>();

                    if (ported_player == null)
                        ported_player = new List<GamePlayer>();

                    if (spawn_antipass == false)//spawn anti pass near waterfall so players cant leave boss area until killed 
                    {
                        SpawnAntiPass();
                        spawn_antipass = true;
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

                    if (ported_player.Count > 0)
                    {
                        GamePlayer port = ((GamePlayer)(player_to_port[Util.Random(0, player_to_port.Count) - 1]));
                        TeleportTarget = port;

                        if (teleport_player == false && Body.PackageID == "Olcasgean1" && Body.HealthPercent <= 50 && TeleportTarget.IsAlive && TeleportTarget != null)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(DoPort), Util.Random(12000,20000));//do teleport every 12-20s
                            teleport_player = true;
                        }
                    }
                }
            }
            base.Think();
        }
        #endregion
        #region DOPort
        public int DoPort(ECSGameTimer timer)
        {
            if (player_to_port.Count > 0)
            {
                int random = Util.Random(1, 3);
                if (Body.HealthPercent <= 50 && Body.PackageID == "Olcasgean1")
                {
                    switch (random)
                    {
                        case 1:
                            {
                                if (TeleportTarget.IsAlive && TeleportTarget != null && !ported_player.Contains(TeleportTarget))
                                {
                                    TeleportTarget.MoveTo(Body.CurrentRegionID, 38399, 60893, 12242, 3548);
                                    TeleportTarget.Client.Out.SendMessage(Body.Name + " throws you away...", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    if (!ported_player.Contains(TeleportTarget))
                                        ported_player.Add(TeleportTarget);
                                }
                            }
                            break;
                        case 2:
                            {
                                if (TeleportTarget.IsAlive && TeleportTarget != null && !ported_player.Contains(TeleportTarget))
                                {
                                    TeleportTarget.MoveTo(Body.CurrentRegionID, 38564, 64161, 12242, 2382);
                                    TeleportTarget.Client.Out.SendMessage(Body.Name + " throws you away...", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    if (!ported_player.Contains(TeleportTarget))
                                        ported_player.Add(TeleportTarget);
                                }

                            }
                            break;
                        case 3:
                            {
                                if (TeleportTarget.IsAlive && TeleportTarget != null && !ported_player.Contains(TeleportTarget))
                                {
                                    TeleportTarget.MoveTo(Body.CurrentRegionID, 41580, 62325, 12242, 890);
                                    TeleportTarget.Client.Out.SendMessage(Body.Name + " throws you away...", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                                    if (!ported_player.Contains(TeleportTarget))
                                        ported_player.Add(TeleportTarget);
                                }
                            }
                            break;
                    }
                }
            }
            teleport_player = false;
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
                    DBSpell spell = new DBSpell();
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
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.DamageType = (int)eDamageType.Matter;
                    m_OlcasgeanDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_OlcasgeanDD);
                }
                return m_OlcasgeanDD;
            }
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            if (source is GamePet || source is TurretPet)
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
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
        public override void WalkToSpawn(short speed)
        {
            if (IsAlive)
                return;
            base.WalkToSpawn(speed);
        }
        public override void Follow(GameObject target, int minDistance, int maxDistance)
        {
        }
        public override void StopFollowing()
        {
        }      
        public override void Die(GameObject killer)
        {
            ++OIBrain.DeadPrimalsCount;
            base.Die(killer);
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159435);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            RespawnInterval = -1;//will not respawn
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            Flags = eFlags.FLYING;

            AirPrimalBrain sBrain = new AirPrimalBrain();
            SetOwnBrain(sBrain);
            Brain.Start();
            base.AddToWorld();
            return true;
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public AirPrimalBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 0;
            ThinkInterval = 2000;
        }

        private GameLiving randomtarget = null;
        private GameLiving RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        List<GamePlayer> inRangeLiving;
        public void PickRandomTarget()
        {
            IList enemies = new ArrayList(m_aggroTable.Keys);

            foreach (GamePlayer player in Body.GetPlayersInRadius(1100))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (player.GetDistanceTo(Body) < 1100 && player.IsVisibleTo(Body))
                        {
                            if (!m_aggroTable.ContainsKey(player))
                                m_aggroTable.Add(player, 1);
                        }
                        else
                        {
                            if (m_aggroTable.ContainsKey(player))
                                m_aggroTable.Remove(player);
                        }
                    }
                }
            }

            if (enemies.Count == 0)
                return;
            else
            {
                List<GameLiving> damage_enemies = new List<GameLiving>();
                for (int i = 0; i < enemies.Count; i++)
                {
                    if (enemies[i] == null)
                        continue;
                    if (!(enemies[i] is GameLiving))
                        continue;
                    if (!(enemies[i] as GameLiving).IsAlive)
                        continue;
                    GameLiving living = null;
                    living = enemies[i] as GameLiving;
                    if (living.IsVisibleTo(Body) && Body.TargetInView)
                    {
                        damage_enemies.Add(enemies[i] as GameLiving);
                    }
                }

                if (damage_enemies.Count > 0)
                {
                    RandomTarget = damage_enemies[Util.Random(0, damage_enemies.Count - 1)];
                    if (RandomTarget.IsVisibleTo(Body) && Body.TargetInView)
                    {
                        PrepareToDD();
                        if (Util.Chance(15) && RandomTarget != null)
                        {
                            Body.TargetObject = RandomTarget;
                            if (!RandomTarget.effectListComponent.ContainsEffectForEffectType(eEffect.Mez))
                            {
                                Body.CastSpell(Mezz, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                                AggroTable.Remove(RandomTarget);
                                m_aggroTable.Remove(RandomTarget);
                            }
                        }
                    }
                    else
                    {
                        AggroTable.Remove(RandomTarget);
                        m_aggroTable.Remove(RandomTarget);
                    }
                }
            }
        }

        private int CastDD(ECSGameTimer timer)
        {
            GameObject oldTarget = Body.TargetObject;

            Body.TargetObject = RandomTarget;
            if (Body.TargetObject != null)
            {
                Body.CastSpell(AirDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                AggroTable.Remove(RandomTarget);
                m_aggroTable.Remove(RandomTarget);
            }
            RandomTarget = null;
            if (oldTarget != null) Body.TargetObject = oldTarget;
            return 0;
        }

        private void PrepareToDD()
        {
            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastDD), 1200);
        }
        public static bool path1 = false;
        public static bool path2 = false;
        public static bool path3 = false;
        public static bool path4 = false;
        public static bool path5 = false;
        public static bool path6 = false;
        public static bool path7 = false;
        public static bool path8 = false;
        public static bool path9 = false;
        public static bool path10 = false;
        public static bool path11 = false;

        public override void Think()
        {
            foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
            {
                if (player != null)
                {
                    if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                    {
                        if (!AggroTable.ContainsKey(player))
                            AggroTable.Add(player, 100);
                    }
                }
            }
            Point3D point1 = new Point3D();
            point1.X = 39120; point1.Y = 61387; point1.Z = 12372;
            Point3D point2 = new Point3D();
            point2.X = 38531; point2.Y = 61871; point2.Z = 12372;
            Point3D point3 = new Point3D();
            point3.X = 38361; point3.Y = 62497; point3.Z = 12372;
            Point3D point4 = new Point3D();
            point4.X = 38525; point4.Y = 63092; point4.Z = 12372;
            Point3D point5 = new Point3D();
            point5.X = 38936; point5.Y = 63471; point5.Z = 12372;
            Point3D point6 = new Point3D();
            point6.X = 39462; point6.Y = 63707; point6.Z = 12372;
            Point3D point7 = new Point3D();
            point7.X = 40028; point7.Y = 63647; point7.Z = 12372;
            Point3D point8 = new Point3D();
            point8.X = 40633; point8.Y = 63236; point8.Z = 12372;
            Point3D point9 = new Point3D();
            point9.X = 40817; point9.Y = 62737; point9.Z = 12372;
            Point3D point10 = new Point3D();
            point10.X = 40760; point10.Y = 62068; point10.Z = 12372;
            Point3D point11 = new Point3D();
            point11.X = 40355; point11.Y = 61543; point11.Z = 12372;

            if (inRangeLiving == null)
                inRangeLiving = new List<GamePlayer>();
            if (Body.InCombatInLast(20 * 1000) == false && this.Body.InCombatInLast(25 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (Body.IsAlive)
            {
                if (!Body.IsWithinRadius(point1, 20) && path1 == false)
                {
                    Body.WalkTo(point1, 250);
                }
                else
                {
                    path1 = true;
                    path11 = false;
                    if (!Body.IsWithinRadius(point2, 20) && path1 == true && path2 == false)
                    {
                        Body.WalkTo(point2, 250);
                    }
                    else
                    {
                        path2 = true;
                        if (!Body.IsWithinRadius(point3, 20) && path1 == true && path2 == true && path3 == false)
                        {
                            Body.WalkTo(point3, 250);
                        }
                        else
                        {
                            path3 = true;
                            if (!Body.IsWithinRadius(point4, 20) && path1 == true && path2 == true && path3 == true && path4 == false)
                            {
                                Body.WalkTo(point4, 250);
                            }
                            else
                            {
                                path4 = true;
                                if (!Body.IsWithinRadius(point5, 20) && path1 == true && path2 == true && path3 == true && path4 == true && path5 == false)
                                {
                                    Body.WalkTo(point5, 250);
                                }
                                else
                                {
                                    path5 = true;
                                    if (!Body.IsWithinRadius(point6, 20) && path1 == true && path2 == true && path3 == true && path4 == true && path5 == true
                                        && path6 == false)
                                    {
                                        Body.WalkTo(point6, 250);
                                    }
                                    else
                                    {
                                        path6 = true;
                                        if (!Body.IsWithinRadius(point7, 20) && path1 == true && path2 == true && path3 == true && path4 == true && path5 == true
                                            && path6 == true && path7 == false)
                                        {
                                            Body.WalkTo(point7, 250);
                                        }
                                        else
                                        {
                                            path7 = true;
                                            if (!Body.IsWithinRadius(point8, 20) && path1 == true && path2 == true && path3 == true && path4 == true && path5 == true
                                                 && path6 == true && path7 == true && path8 == false)
                                            {
                                                Body.WalkTo(point8, 250);
                                            }
                                            else
                                            {
                                                path8 = true;
                                                if (!Body.IsWithinRadius(point9, 20) && path1 == true && path2 == true && path3 == true && path4 == true && path5 == true
                                                    && path6 == true && path7 == true && path8 == true && path9 == false)
                                                {
                                                    Body.WalkTo(point9, 250);
                                                }
                                                else
                                                {
                                                    path9 = true;
                                                    if (!Body.IsWithinRadius(point10, 20) && path1 == true && path2 == true && path3 == true && path4 == true && path5 == true
                                                        && path6 == true && path7 == true && path8 == true && path9 == true && path10 == false)
                                                    {
                                                        Body.WalkTo(point10, 250);
                                                    }
                                                    else
                                                    {
                                                        path10 = true;
                                                        if (!Body.IsWithinRadius(point11, 20) && path1 == true && path2 == true && path3 == true && path4 == true && path5 == true
                                                            && path6 == true && path7 == true && path8 == true && path9 == true && path10 == true && path11 == false)
                                                        {
                                                            Body.WalkTo(point11, 250);
                                                        }
                                                        else
                                                        {
                                                            path11 = true;
                                                            path1 = false;
                                                            path2 = false;
                                                            path3 = false;
                                                            path4 = false;
                                                            path5 = false;
                                                            path6 = false;
                                                            path7 = false;
                                                            path8 = false;
                                                            path9 = false;
                                                            path10 = false;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                PickRandomTarget();
            }
            base.Think();
        }

        public Spell m_AirDD;
        public Spell AirDD
        {
            get
            {
                if (m_AirDD == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 2;
                    spell.ClientEffect = 479;
                    spell.Icon = 479;
                    spell.TooltipId = 479;
                    spell.Damage = 600;
                    spell.Range = 1200;
                    spell.SpellID = 11718;
                    spell.Target = "Enemy";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Spirit;
                    m_AirDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_AirDD);
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
                    DBSpell spell = new DBSpell();
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
                    spell.Target = "Enemy";
                    spell.Type = "Mesmerize";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Spirit; //Spirit DMG Type
                    m_mezSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_mezSpell);
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
        public override void Die(GameObject killer)
        {
            ++OIBrain.DeadPrimalsCount;
            base.Die(killer);
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
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
                return 200000;
            }
        }

        public override int AttackRange
        {
            get
            {
                return 350;
            }
            set
            {
            }
        }
        public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
        {
            if (source is GamePlayer || source is GamePet)
            {
                if (WaterPrimalBrain.dontattack)//dont take any dmg 
                {
                    if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                        || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                        || damageType == eDamageType.Slash)
                    {
                        GamePlayer truc;
                        if (source is GamePlayer)
                            truc = (source as GamePlayer);
                        else
                            truc = ((source as GamePet).Owner as GamePlayer);
                        if (truc != null)
                            truc.Out.SendMessage(this.Name + " is under waterfall effect!", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        base.TakeDamage(source, damageType, 0, 0);
                        return;
                    }
                }
                else//take dmg
                {
                    base.TakeDamage(source, damageType, damageAmount, criticalAmount);
                }
            }
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159438);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;

            WaterPrimalBrain.message = false;
            WaterPrimalBrain.lowhealth1 = false;
            WaterPrimalBrain.dontattack = false;
            WaterPrimalBrain.TeleportTarget = null;
            WaterPrimalBrain.IsTargetTeleported = false;

            CurrentRegionID = 191;//galladoria
            Flags ^= eFlags.GHOST;//ghost

            RespawnInterval = -1;//will not respawn
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            WaterPrimalBrain sBrain = new WaterPrimalBrain();
            SetOwnBrain(sBrain);
            Brain.Start();
            base.AddToWorld();
            return true;
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public WaterPrimalBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 5000;
        }
        public static bool dontattack = false;
        public static bool lowhealth1 = false;
        public static bool message = false;
        public override void Notify(DOLEvent e, object sender, EventArgs args)
        {
            if (e == GameNPCEvent.AddToWorld)
            {
                Point3D point1 = new Point3D();
                point1.X = 39652; point1.Y = 60831; point1.Z = 11893;
                Body.WalkTo(point1, 300);
            }
            base.Notify(e, sender, args);
        }
        public override void AttackMostWanted()
        {
            if (dontattack == true)
                return;
            else
            {
                if (ECS.Debug.Diagnostics.AggroDebugEnabled)
                {
                    PrintAggroTable();
                }

                Body.TargetObject = CalculateNextAttackTarget();

                if (Body.TargetObject != null)
                {
                    if (!CheckSpells(eCheckSpellType.Offensive))
                    {
                        Body.StartAttack(Body.TargetObject);
                    }
                }
            }
            base.AttackMostWanted();
        }
        public int CanAttack(ECSGameTimer timer)
        {
            dontattack = false;
            AggroRange = 1500;
            return 0;
        }
        public void LowOnHealth()
        {
            Point3D point1 = new Point3D();
            point1.X = 39652; point1.Y = 60831; point1.Z = 11893;

            if (Body.HealthPercent < 30 && lowhealth1 == false)
            {
                if (Body.IsWithinRadius(point1, 80))
                {
                    Body.CastSpell(WaterEffect, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
                    Body.Health += Body.MaxHealth / 6;
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CanAttack), 5000);
                    lowhealth1 = true;
                }
                else
                {
                    if (message == false)
                    {
                        ClearAggroList();
                        message = true;
                    }
                    Body.WalkTo(point1, 300);
                    dontattack = true;
                }
            }
        }
        public override void Think()
        {
            if (HasAggro && Body.TargetObject != null)
            {
                if (Util.Chance(10))
                {
                    if (IsTargetTeleported == false)
                    {
                        new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(PickTeleportPlayer), Util.Random(25000, 45000));
                        IsTargetTeleported = true;
                    }
                }
            }
            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
                dontattack = false;
                lowhealth1 = false;
                message = false;
                IsTargetTeleported = false;
                TeleportTarget = null;
                AggroRange = 600;
            }
            LowOnHealth();
            base.Think();
        }
        #region Pick player to port
        public static bool IsTargetTeleported = false;
        public static GamePlayer teleporttarget = null;
        public static GamePlayer TeleportTarget
        {
            get { return teleporttarget; }
            set { teleporttarget = value; }
        }
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
                    TeleportTarget = null;//reset random target to null
                    IsTargetTeleported = false;
                }
                else
                {
                    if (Port_Enemys.Count > 0)
                    {
                        GamePlayer Target = Port_Enemys[Util.Random(0, Port_Enemys.Count - 1)];
                        TeleportTarget = Target;
                        if (TeleportTarget.IsAlive && TeleportTarget != null)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(TeleportPlayer), 3000);
                        }
                    }
                }
            }
            return 0;
        }
        public int TeleportPlayer(ECSGameTimer timer)
        {
            if (TeleportTarget.IsAlive && TeleportTarget != null && HasAggro)
            {
                switch (Util.Random(1, 2))
                {
                    case 1: TeleportTarget.MoveTo(Body.CurrentRegionID, 38626, 60891, 11771, 2881); break;
                    case 2: TeleportTarget.MoveTo(Body.CurrentRegionID, 40606, 60868, 11721, 1095); break;
                }
                Port_Enemys.Remove(TeleportTarget);
                TeleportTarget = null;//reset random target to null
                IsTargetTeleported = false;
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
                    DBSpell spell = new DBSpell();
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
                    spell.Target = "Self";
                    spell.Type = eSpellType.PowerRegenBuff.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    m_WaterEffect = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_WaterEffect);
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
        public override void WalkToSpawn()
        {
            if (IsAlive)
                return;
            base.WalkToSpawn();
        }
        public override void Die(GameObject killer)
        {
            ++OIBrain.DeadPrimalsCount;
            base.Die(killer);
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
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
                return 200000;
            }
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }

        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159437);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            FirePrimalBrain.CanSpawnFire = false;

            Flags ^= eFlags.FLYING;//flying
            RespawnInterval = -1;//will not respawn
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

            FirePrimalBrain sBrain = new FirePrimalBrain();
            SetOwnBrain(sBrain);
            Brain.Start();
            base.AddToWorld();
            return true;
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public FirePrimalBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 1500;
            ThinkInterval = 2500;
        }

        public static bool path1 = false;
        public static bool path2 = false;
        public static bool path3 = false;
        public static bool path4 = false;
        public override void Think()
        {
            Point3D point1 = new Point3D();
            point1.X = 40142; point1.Y = 63014; point1.Z = 11670;
            Point3D point2 = new Point3D();
            point2.X = 40368; point2.Y = 62034; point2.Z = 11676;
            Point3D point3 = new Point3D();
            point3.X = 39134; point3.Y = 61783; point3.Z = 11688;
            Point3D point4 = new Point3D();
            point4.X = 38989; point4.Y = 62939; point4.Z = 11694;

            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (Body.IsAlive)
            {
                Body.CastSpell(FireDS, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));//cast dmg shield every 60s
                foreach (GamePlayer player in Body.GetPlayersInRadius(2500))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            if (!AggroTable.ContainsKey(player))
                                AggroTable.Add(player, 100);
                        }
                    }
                }
                if (CanSpawnFire == false)
                {
                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(SpawnFire), 1000);
                    CanSpawnFire = true;
                }
                if (!Body.IsWithinRadius(point1, 20) && path1 == false)
                {
                    Body.WalkTo(point1, 200);
                }
                else
                {
                    path1 = true;
                    path4 = false;
                    if (!Body.IsWithinRadius(point2, 20) && path1 == true && path2 == false)
                    {
                        Body.WalkTo(point2, 200);
                    }
                    else
                    {
                        path2 = true;
                        if (!Body.IsWithinRadius(point3, 20) && path1 == true && path2 == true && path3 == false)
                        {
                            Body.WalkTo(point3, 200);
                        }
                        else
                        {
                            path3 = true;
                            if (!Body.IsWithinRadius(point4, 20) && path1 == true && path2 == true && path3 == true && path4 == false)
                            {
                                Body.WalkTo(point4, 200);
                            }
                            else
                            {
                                path4 = true;
                                path1 = false;
                                path2 = false;
                                path3 = false;
                            }
                        }
                    }
                }
            }
            base.Think();
        }
        public static bool CanSpawnFire = false;
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
            CanSpawnFire = false;
            return 0;
        }
        private Spell m_FireDS;
        private Spell FireDS
        {
            get
            {
                if (m_FireDS == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 60;
                    spell.ClientEffect = 57;
                    spell.Icon = 57;
                    spell.Damage = 80;
                    spell.Duration = 60;
                    spell.Name = "Fire Primal Damage Shield";
                    spell.TooltipId = 57;
                    spell.SpellID = 11721;
                    spell.Target = "Self";
                    spell.Type = "DamageShield";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Heat;
                    m_FireDS = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FireDS);
                }
                return m_FireDS;
            }
        }
    }
}
#region trail of fire
namespace DOL.GS
{
    public class TrailOfFire : GameNPC
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
                {
                    if (player != null)
                        player.Out.SendSpellEffectAnimation(this, this, 5906, 0, false, 0x01);
                }
                SetGroundTarget(X, Y, Z);
                if (!IsCasting)
                    CastSpell(FireGroundDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells), false);

                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(DoCast), 1000);
            }
            return 0;
        }
        private int DoCast(ECSGameTimer timer)
        {
            if (IsAlive)
                new ECSGameTimer(this, new ECSGameTimer.ECSTimerCallback(Show_Effect), 1000);
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
            Model = 665;
            Name = "trail of fire";
            Flags ^= eFlags.DONTSHOWNAME;
            Flags ^= eFlags.CANTTARGET;
            //Flags ^= eFlags.STATUE;
            MaxSpeedBase = 0;
            Level = 80;
            Size = 10;

            RespawnInterval = -1;//will not respawn
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

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
                    DBSpell spell = new DBSpell();
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
                    spell.Target = "Area";
                    spell.Type = eSpellType.DirectDamageNoVariance.ToString();
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Heat;
                    m_FireGroundDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_FireGroundDD);
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
        public override void Die(GameObject killer)
        {
            ++OIBrain.DeadPrimalsCount;
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
            base.Die(killer);
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
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
            get { return 200000; }
        }
        public override int AttackRange
        {
            get { return 350; }
            set { }
        }
        public override bool AddToWorld()
        {
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159436);
            LoadTemplate(npcTemplate);
            Strength = npcTemplate.Strength;
            Dexterity = npcTemplate.Dexterity;
            Constitution = npcTemplate.Constitution;
            Quickness = npcTemplate.Quickness;
            Piety = npcTemplate.Piety;
            Intelligence = npcTemplate.Intelligence;
            Empathy = npcTemplate.Empathy;
            TetherRange = 890;

            RespawnInterval = -1;//will not respawn
            Faction = FactionMgr.GetFactionByID(96);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
                    if (!target.IsWithinRadius(spawn, 900) && AggroTable.ContainsKey(target) && target != null && target.IsAlive)
                    {
                        AggroTable.Remove(target);
                        CalculateNextAttackTarget();
                        CanSwitchTarget = false;
                    }
                }
            }
            return 0;
        }
        public static bool CanSwitchTarget = false;
        public override void Think()
        {
            if (!HasAggressionTable())
            {
                Body.Health = Body.MaxHealth;
                CanSwitchTarget = false;
                INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159436);
                Body.MaxSpeedBase = npcTemplate.MaxSpeed;
            }
            if (Body.InCombat && HasAggro)
            {
                if (Util.Chance(15))
                    Body.CastSpell(EarthRoot, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
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
                        if (CanSwitchTarget == false)
                        {
                            new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(TargetIsOut), 5000);
                            CanSwitchTarget = true;
                        }
                    }
                    else
                        Body.MaxSpeedBase = npcTemplate.MaxSpeed;
                }
            }
            if (Body.IsOutOfTetherRange && !HasAggro)
            {
                FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            }
            base.Think();
        }
        private Spell m_EarthRoot;
        private Spell EarthRoot
        {
            get
            {
                if (m_EarthRoot == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = Util.Random(15, 25);
                    spell.ClientEffect = 277;
                    spell.Icon = 277;
                    spell.TooltipId = 277;
                    spell.Name = "Roots from Earth";
                    spell.Value = 99;
                    spell.Duration = 60;
                    spell.Range = 1200;
                    spell.Range = 1500;
                    spell.SpellID = 11726;
                    spell.Target = "Enemy";
                    spell.Type = "SpeedDecrease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Cold;
                    m_EarthRoot = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_EarthRoot);
                }
                return m_EarthRoot;
            }
        }
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            if (source is GamePet)
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
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            MeleeDamageType = eDamageType.Slash;
            BodyType = 5;

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
    public class GuardianEarthmenderBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public GuardianEarthmenderBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        private GameLiving randomtarget;
        private GameLiving RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public int CastHeal(ECSGameTimer timer)
        {
            GameObject oldTarget = Body.TargetObject;
            Body.TargetObject = RandomTarget;
            if (Body.TargetObject != null)
            {
                if (!Body.IsCasting)
                    Body.CastSpell(EarthmenderHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            RandomTarget = null;
            if (oldTarget != null) Body.TargetObject = oldTarget;
            return 0;
        }
        List<GameNPC> inRangeLiving;
        public override void Think()
        {
            if (inRangeLiving == null)
                inRangeLiving = new List<GameNPC>();

            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (Body.IsAlive)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        if (npc.Brain is NaturalEarthmenderBrain || npc.Brain is MagicalEarthmenderBrain || npc.Brain is ShadowyEarthmenderBrain || npc.Brain is EarthPrimalBrain)
                        {
                            if (npc.IsAlive && npc.HealthPercent < 100)
                            {
                                if (!inRangeLiving.Contains(npc))
                                {
                                    inRangeLiving.Add(npc);
                                }
                                if (inRangeLiving.Count > 0)
                                {
                                    GameNPC ptarget = ((GameNPC)(inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
                                    RandomTarget = ptarget;
                                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastHeal), 2000);
                                }
                            }
                        }
                    }
                }
            }
            base.Think();
        }
        private Spell m_EarthmenderHeal;
        private Spell EarthmenderHeal
        {
            get
            {
                if (m_EarthmenderHeal == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 3;
                    spell.ClientEffect = 4858;
                    spell.Icon = 4858;
                    spell.TooltipId = 4858;
                    spell.Value = 200;
                    spell.Range = 1500;
                    spell.SpellID = 11722;
                    spell.Target = "Realm";
                    spell.Type = "Heal";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    m_EarthmenderHeal = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_EarthmenderHeal);
                }
                return m_EarthmenderHeal;
            }
        }
    }
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            if (source is GamePet)
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
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            MeleeDamageType = eDamageType.Slash;
            BodyType = 5;

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
    public class MagicalEarthmenderBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public MagicalEarthmenderBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        private GameLiving randomtarget;
        private GameLiving RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public int CastHeal(ECSGameTimer timer)
        {
            GameObject oldTarget = Body.TargetObject;
            Body.TargetObject = RandomTarget;
            if (Body.TargetObject != null)
            {
                if (!Body.IsCasting)
                    Body.CastSpell(EarthmenderHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            RandomTarget = null;
            if (oldTarget != null) Body.TargetObject = oldTarget;
            return 0;
        }
        List<GameNPC> inRangeLiving;
        public override void Think()
        {
            if (inRangeLiving == null)
                inRangeLiving = new List<GameNPC>();

            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (Body.IsAlive)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        if (npc.Brain is GuardianEarthmenderBrain || npc.Brain is NaturalEarthmenderBrain || npc.Brain is ShadowyEarthmenderBrain || npc.Brain is EarthPrimalBrain)
                        {
                            if (npc.IsAlive && npc.HealthPercent < 100)
                            {
                                if (!inRangeLiving.Contains(npc))
                                {
                                    inRangeLiving.Add(npc);
                                }
                                if (inRangeLiving.Count > 0)
                                {
                                    GameNPC ptarget = ((GameNPC)(inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
                                    RandomTarget = ptarget;
                                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastHeal), 2000);
                                }
                            }
                        }
                    }
                }
            }
            base.Think();
        }
        private Spell m_EarthmenderHeal;
        private Spell EarthmenderHeal
        {
            get
            {
                if (m_EarthmenderHeal == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 3;
                    spell.ClientEffect = 4858;
                    spell.Icon = 4858;
                    spell.TooltipId = 4858;
                    spell.Value = 200;
                    spell.Range = 1500;
                    spell.SpellID = 11723;
                    spell.Target = "Realm";
                    spell.Type = "Heal";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    m_EarthmenderHeal = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_EarthmenderHeal);
                }
                return m_EarthmenderHeal;
            }
        }
    }
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            if (source is GamePet)
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
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            MeleeDamageType = eDamageType.Slash;
            BodyType = 5;

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
    public class NaturalEarthmenderBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public NaturalEarthmenderBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        private GameLiving randomtarget;
        private GameLiving RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public int CastHeal(ECSGameTimer timer)
        {
            GameObject oldTarget = Body.TargetObject;
            Body.TargetObject = RandomTarget;
            if (Body.TargetObject != null)
            {
                if (!Body.IsCasting)
                    Body.CastSpell(EarthmenderHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            RandomTarget = null;
            if (oldTarget != null) Body.TargetObject = oldTarget;
            return 0;
        }
        List<GameNPC> inRangeLiving;
        public override void Think()
        {
            if (inRangeLiving == null)
                inRangeLiving = new List<GameNPC>();

            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (Body.IsAlive)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        if (npc.Brain is GuardianEarthmenderBrain || npc.Brain is MagicalEarthmenderBrain || npc.Brain is ShadowyEarthmenderBrain || npc.Brain is EarthPrimalBrain)
                        {
                            if (npc.IsAlive && npc.HealthPercent < 100)
                            {
                                if (!inRangeLiving.Contains(npc))
                                {
                                    inRangeLiving.Add(npc);
                                }
                                if (inRangeLiving.Count > 0)
                                {
                                    GameNPC ptarget = ((GameNPC)(inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
                                    RandomTarget = ptarget;
                                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastHeal), 2000);
                                }
                            }
                        }
                    }
                }
            }
            base.Think();
        }
        private Spell m_EarthmenderHeal;
        private Spell EarthmenderHeal
        {
            get
            {
                if (m_EarthmenderHeal == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 3;
                    spell.ClientEffect = 4858;
                    spell.Icon = 4858;
                    spell.TooltipId = 4858;
                    spell.Value = 200;
                    spell.Range = 1500;
                    spell.SpellID = 11724;
                    spell.Target = "Realm";
                    spell.Type = "Heal";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    m_EarthmenderHeal = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_EarthmenderHeal);
                }
                return m_EarthmenderHeal;
            }
        }
    }
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            if (source is GamePet)
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
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
            MeleeDamageType = eDamageType.Slash;
            BodyType = 5;

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
    public class ShadowyEarthmenderBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ShadowyEarthmenderBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 500;
        }
        private GameLiving randomtarget;
        private GameLiving RandomTarget
        {
            get { return randomtarget; }
            set { randomtarget = value; }
        }
        public int CastHeal(ECSGameTimer timer)
        {
            GameObject oldTarget = Body.TargetObject;
            Body.TargetObject = RandomTarget;
            if (Body.TargetObject != null)
            {
                if (!Body.IsCasting)
                    Body.CastSpell(EarthmenderHeal, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
            }
            RandomTarget = null;
            if (oldTarget != null) Body.TargetObject = oldTarget;
            return 0;
        }
        List<GameNPC> inRangeLiving;
        public override void Think()
        {
            if (inRangeLiving == null)
                inRangeLiving = new List<GameNPC>();

            if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            if (Body.IsAlive)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(5000))
                {
                    if (npc != null)
                    {
                        if (npc.Brain is GuardianEarthmenderBrain || npc.Brain is MagicalEarthmenderBrain || npc.Brain is NaturalEarthmenderBrain || npc.Brain is EarthPrimalBrain)
                        {
                            if (npc.IsAlive && npc.HealthPercent < 100)
                            {
                                if (!inRangeLiving.Contains(npc))
                                {
                                    inRangeLiving.Add(npc);
                                }
                                if (inRangeLiving.Count > 0)
                                {
                                    GameNPC ptarget = ((GameNPC)(inRangeLiving[Util.Random(1, inRangeLiving.Count) - 1]));
                                    RandomTarget = ptarget;
                                    new ECSGameTimer(Body, new ECSGameTimer.ECSTimerCallback(CastHeal), 2000);
                                }
                            }
                        }
                    }
                }
            }
            base.Think();
        }
        private Spell m_EarthmenderHeal;
        private Spell EarthmenderHeal
        {
            get
            {
                if (m_EarthmenderHeal == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.RecastDelay = 3;
                    spell.ClientEffect = 4858;
                    spell.Icon = 4858;
                    spell.TooltipId = 4858;
                    spell.Value = 200;
                    spell.Range = 1500;
                    spell.SpellID = 11725;
                    spell.Target = "Realm";
                    spell.Type = "Heal";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    m_EarthmenderHeal = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_EarthmenderHeal);
                }
                return m_EarthmenderHeal;
            }
        }
    }
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
            if (source is GamePlayer || source is GamePet)
            {
                if (damageType == eDamageType.Body || damageType == eDamageType.Cold || damageType == eDamageType.Energy || damageType == eDamageType.Heat
                    || damageType == eDamageType.Matter || damageType == eDamageType.Spirit || damageType == eDamageType.Crush || damageType == eDamageType.Thrust
                    || damageType == eDamageType.Slash)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GamePet).Owner as GamePlayer);
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
        public override int AttackRange
        {
            get { return 200; }
            set { }
        }
        public override void DropLoot(GameObject killer)//no loot
        {
        }
        public override void Die(GameObject killer)
        {
            base.Die(null); // null to not gain experience
        }
        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 250;
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
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
                    DBSpell spell = new DBSpell();
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
                    spell.Target = "Enemy";
                    spell.Type = "DamageSpeedDecrease";
                    spell.Uninterruptible = true;
                    spell.MoveCast = true;
                    spell.DamageType = (int)eDamageType.Spirit;
                    m_VortexDD = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_VortexDD);
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
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
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
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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