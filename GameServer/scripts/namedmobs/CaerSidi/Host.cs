using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.Events;
using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Styles;
using DOL.GS.Effects;
using Timer = System.Timers.Timer;
using System.Timers;

//Mob with packageid="HostBaf" in same region as bosss will come if he is pulled/aggroed
//Make sure to add that packageid to Host rooms, unless it will not bring a friends!
//DO NOT REMOVE Host Initializator from ingame or encounter will  not work!
namespace DOL.GS
{
    public class HostInitializator : GameNPC
    {
        public HostInitializator() : base()
        {
        }

        #region Initializator Timer Cycle + SpawnHostCopy

        public void StartTimer()
        {
            Timer myTimer = new Timer();
            myTimer.Elapsed += new ElapsedEventHandler(DisplayTimeEvent);
            myTimer.Interval = 1000; // 1000 ms is one second
            myTimer.Start();
        }

        public void DisplayTimeEvent(object source, ElapsedEventArgs e)
        {
            DoStuff();
        }

        public static bool Spawnhost = false;
        public static bool DoRespawnTimer = false;

        public void DoStuff()
        {
            if (this.IsAlive)
            {
                if (Host.HostCount == 0)
                {
                    pickhostcheck = false;
                    set_realhost = false;
                    if (ChooseHost.Count > 0)
                    {
                        ChooseHost.Clear();
                    }
                }

                if (Spawnhost == false && Host.HostCount == 0)
                {
                    SpawnHostCopy();
                    Spawnhost = true;
                }

                if (pickhostcheck == false && Host.HostCount > 0)
                {
                    PickHost();
                    pickhostcheck = true;
                }

                if (Host.HostCount == 0 && DoRespawnTimer == false)
                {
                    RespawnChecker();
                    DoRespawnTimer = true;
                }
            }
        }

        public void SpawnHostCopy()
        {
            DoRespawnTimer = false;
            for (Host.HostCount = 0; Host.HostCount < 10; Host.HostCount++)
            {
                Host Add = new Host();
                Add.X = this.X;
                Add.Y = this.Y;
                Add.Z = this.Z;
                Add.CurrentRegion = this.CurrentRegion;
                Add.Heading = this.Heading;
                Add.AddToWorld();
                Add.PackageID = "HostCopy" + Host.HostCount;
            }
        }

        #endregion

        #region Pick real Host

        List<GameNPC> ChooseHost = new List<GameNPC>();
        public static bool set_realhost = false;
        public static bool pickhostcheck = false;

        public void PickHost()
        {
            foreach (GameNPC host in GetNPCsInRadius(8000))
            {
                if (host != null)
                {
                    if (host.Brain is HostBrain)
                    {
                        if (!ChooseHost.Contains(host))
                        {
                            ChooseHost.Add(host);
                        }
                    }
                }
            }

            if (ChooseHost.Count > 0)
            {
                if (set_realhost == false)
                {
                    GameNPC RealHost = ChooseHost[Util.Random(0, ChooseHost.Count - 1)];
                    RealHost.PackageID = "HostReal";
                    set_realhost = true;
                }
            }
        }

        #endregion

        #region Respawn Timer and method

        public int RespawnTime(RegionTimer timer)
        {
            Spawnhost = false;
            return 0;
        }

        public void RespawnChecker()
        {
            int time = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL *
                       60000; //1min is 60000miliseconds        
            new RegionTimer(this, new RegionTimerCallback(RespawnTime), time);
        }

        #endregion

        #region AddToWorld + ScriptLoaded

        public override bool AddToWorld()
        {
            StartTimer();
            HIBrain hi = new HIBrain();
            SetOwnBrain(hi);
            base.AddToWorld();
            return true;
        }

        [ScriptLoadedEvent]
        public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            GameNPC[] npcs;
            npcs = WorldMgr.GetNPCsByNameFromRegion("Host Initializator", 60, (eRealm) 0);
            if (npcs.Length == 0)
            {
                log.Warn("Host Initializator not found, creating it...");

                log.Warn("Initializing Host Initializator...");
                HostInitializator CO = new HostInitializator();
                CO.Name = "Host Initializator";
                CO.GuildName = "DO NOT REMOVE!";
                CO.RespawnInterval = 5000;
                CO.Model = 665;
                CO.Realm = 0;
                CO.Level = 50;
                CO.Size = 50;
                CO.CurrentRegionID = 60; //caer sidi
                CO.Flags ^= eFlags.CANTTARGET;
                CO.Flags ^= eFlags.FLYING;
                CO.Flags ^= eFlags.DONTSHOWNAME;
                CO.Flags ^= eFlags.PEACE;
                CO.Faction = FactionMgr.GetFactionByID(64);
                CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
                CO.X = 26995;
                CO.Y = 29733;
                CO.Z = 17871;
                HIBrain ubrain = new HIBrain();
                CO.SetOwnBrain(ubrain);
                CO.AddToWorld();
                CO.SaveIntoDatabase();
                CO.Brain.Start();
            }
            else
                log.Warn(
                    "Host Initializator exist ingame, remove it and restart server if you want to add by script code.");
        }

        #endregion
    }
}

namespace DOL.AI.Brain
{
    public class HIBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public HIBrain()
            : base()
        {
            ThinkInterval = 2000;
        }

        public override void Think()
        {
            base.Think();
        }
    }
}

///////////////////////////////////////////////////Host Copy////////////////////////////////////
namespace DOL.GS
{
    public class Host : GameEpicBoss
    {
        public Host() : base()
        {
        }

        public override double AttackDamage(InventoryItem weapon)
        {
            return base.AttackDamage(weapon) * Strength / 100;
        }

        public override int AttackRange
        {
            get { return 350; }
            set { }
        }

        public override int GetResist(eDamageType damageType)
        {
            switch (damageType)
            {
                case eDamageType.Slash: return 45; // dmg reduction for melee dmg
                case eDamageType.Crush: return 45; // dmg reduction for melee dmg
                case eDamageType.Thrust: return 45; // dmg reduction for melee dmg
                default: return 25; // dmg reduction for rest resists
            }
        }
        public override int MaxHealth
        {
            get { return 15000; }
        }
        public override double GetArmorAF(eArmorSlot slot)
        {
            return 700;
        }
        public override double GetArmorAbsorb(eArmorSlot slot)
        {
            // 85% ABS is cap.
            return 0.55;
        }

        public override void Die(GameObject killer)
        {
            if (PackageID == "HostReal")
            {
                foreach (GameNPC boss in WorldMgr.GetNPCsByNameFromRegion("Host", this.CurrentRegionID, 0))
                {
                    if (boss != null)
                    {
                        if (boss.IsAlive)
                        {
                            if (boss.Brain is HostBrain)
                            {
                                if (boss.PackageID != "HostReal")
                                {
                                    boss.Die(killer); //kill rest of copies if real one dies
                                }
                            }
                        }
                    }
                }

                HostCount = 0; //reset host count to 0
                base.Die(killer);
            }
            else
            {
                --HostCount;
                base.Die(killer);
            }
        }

        public override void DropLoot(GameObject killer)
        {
            if (PackageID == "HostReal") //give only loot from real host
            {
                base.DropLoot(killer);
            }
        }
        public override void WalkToSpawn() //dont walk to spawn
        {
            if (IsAlive)
                return;
            base.WalkToSpawn();
        }
        public static int HostCount = 0;

        public override bool AddToWorld()
        {
            Model = 26;
            MeleeDamageType = eDamageType.Crush;
            Name = "Host";
            PackageID = "HostCopy";
            RespawnInterval = -1;

            MaxDistance = 6500;
            TetherRange = 6600;
            Size = 60;
            Level = 79;
            MaxSpeedBase = 300;
            Flags = eFlags.GHOST;

            Faction = FactionMgr.GetFactionByID(64);
            Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            BodyType = 6;
            Realm = eRealm.None;

            Strength = 5;
            Dexterity = 200;
            Constitution = 100;
            Quickness = 125;
            Piety = 220;
            Intelligence = 220;
            Empathy = 100;

            HostBrain.walkback = false;
            HostBrain.path1 = false;
            HostBrain.path11 = false;
            HostBrain.path21 = false;
            HostBrain.path31 = false;
            HostBrain.path41 = false;
            HostBrain.path51 = false;
            HostBrain.path2 = false;
            HostBrain.path12 = false;
            HostBrain.path22 = false;
            HostBrain.path32 = false;
            HostBrain.path42 = false;
            HostBrain.path3 = false;
            HostBrain.path13 = false;
            HostBrain.path23 = false;
            HostBrain.path33 = false;
            HostBrain.path43 = false;
            HostBrain.path4 = false;
            HostBrain.path14 = false;
            HostBrain.path24 = false;
            HostBrain.path34 = false;
            HostBrain.path44 = false;
            HostBrain.path5 = false;
            HostBrain.path15 = false;
            HostBrain.path25 = false;
            HostBrain.path35 = false;
            HostBrain.path45 = false;
            HostBrain.path6 = false;
            HostBrain.path16 = false;
            HostBrain.path26 = false;
            HostBrain.path36 = false;
            HostBrain.path46 = false;
            HostBrain.path7 = false;
            HostBrain.path17 = false;
            HostBrain.path27 = false;
            HostBrain.path37 = false;
            HostBrain.path47 = false;
            HostBrain.path8 = false;
            HostBrain.path18 = false;
            HostBrain.path28 = false;
            HostBrain.path38 = false;
            HostBrain.path48 = false;
            HostBrain.path9 = false;
            HostBrain.path19 = false;
            HostBrain.path29 = false;
            HostBrain.path39 = false;
            HostBrain.path49 = false;
            HostBrain.path10 = false;
            HostBrain.path20 = false;
            HostBrain.path30 = false;
            HostBrain.path40 = false;
            HostBrain.path50 = false;

            HostBrain adds = new HostBrain();
            SetOwnBrain(adds);
            base.AddToWorld();
            return true;
        }
    }
}

namespace DOL.AI.Brain
{
    public class HostBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public HostBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 400;
            ThinkInterval = 2500;
        }

        public static bool BafMobs = false;

        #region path points checks

        public static bool path1 = false;
        public static bool path11 = false;
        public static bool path21 = false;
        public static bool path31 = false;
        public static bool path41 = false;
        public static bool path2 = false;
        public static bool path12 = false;
        public static bool path22 = false;
        public static bool path39 = false;
        public static bool path42 = false;
        public static bool path3 = false;
        public static bool path13 = false;
        public static bool path23 = false;
        public static bool path32 = false;
        public static bool path43 = false;
        public static bool path4 = false;
        public static bool path14 = false;
        public static bool path24 = false;
        public static bool path33 = false;
        public static bool path44 = false;
        public static bool path5 = false;
        public static bool path15 = false;
        public static bool path25 = false;
        public static bool path34 = false;
        public static bool path45 = false;
        public static bool path6 = false;
        public static bool path16 = false;
        public static bool path26 = false;
        public static bool path35 = false;
        public static bool path46 = false;
        public static bool path7 = false;
        public static bool path17 = false;
        public static bool path27 = false;
        public static bool path36 = false;
        public static bool path47 = false;
        public static bool path8 = false;
        public static bool path18 = false;
        public static bool path28 = false;
        public static bool path37 = false;
        public static bool path48 = false;
        public static bool path9 = false;
        public static bool path19 = false;
        public static bool path29 = false;
        public static bool path38 = false;
        public static bool path49 = false;
        public static bool path10 = false;
        public static bool path20 = false;
        public static bool path30 = false;
        public static bool path40 = false;
        public static bool path50 = false;
        public static bool path51 = false;
        public static bool walkback = false;

        #endregion

        public void HostPath()
        {
            #region path glocs

            Point3D point1 = new Point3D();
            point1.X = 26749;
            point1.Y = 29730;
            point1.Z = 17871; //3th floor
            Point3D point2 = new Point3D();
            point2.X = 26180;
            point2.Y = 30241;
            point2.Z = 17871;
            Point3D point3 = new Point3D();
            point3.X = 25743;
            point3.Y = 30447;
            point3.Z = 17861;
            Point3D point4 = new Point3D();
            point4.X = 25154;
            point4.Y = 30151;
            point4.Z = 17861;
            Point3D point5 = new Point3D();
            point5.X = 24901;
            point5.Y = 29673;
            point5.Z = 17861;
            Point3D point6 = new Point3D();
            point6.X = 25376;
            point6.Y = 29310;
            point6.Z = 17861; // stairs start
            Point3D point7 = new Point3D();
            point7.X = 25360;
            point7.Y = 29635;
            point7.Z = 17866;
            Point3D point8 = new Point3D();
            point8.X = 25608;
            point8.Y = 29967;
            point8.Z = 17702;
            Point3D point9 = new Point3D();
            point9.X = 25984;
            point9.Y = 29902;
            point9.Z = 17534;
            Point3D point10 = new Point3D();
            point10.X = 26121;
            point10.Y = 29617;
            point10.Z = 17405;
            Point3D point11 = new Point3D();
            point11.X = 25889;
            point11.Y = 29309;
            point11.Z = 17251;
            Point3D point12 = new Point3D();
            point12.X = 25453;
            point12.Y = 29390;
            point12.Z = 17051;
            Point3D point13 = new Point3D();
            point13.X = 25372;
            point13.Y = 29775;
            point13.Z = 16897;
            Point3D point14 = new Point3D();
            point14.X = 25946;
            point14.Y = 29958;
            point14.Z = 16638;
            Point3D point15 = new Point3D();
            point15.X = 26116;
            point15.Y = 29523;
            point15.Z = 16495;
            Point3D point16 = new Point3D();
            point16.X = 26106;
            point16.Y = 29305;
            point16.Z = 16495; //start 2nd floor
            Point3D point17 = new Point3D();
            point17.X = 25061;
            point17.Y = 29335;
            point17.Z = 16495;
            Point3D point18 = new Point3D();
            point18.X = 25046;
            point18.Y = 30229;
            point18.Z = 16495;
            Point3D point19 = new Point3D();
            point19.X = 25686;
            point19.Y = 30428;
            point19.Z = 16495;
            Point3D point20 = new Point3D();
            point20.X = 26832;
            point20.Y = 29793;
            point20.Z = 16495;
            Point3D point21 = new Point3D();
            point21.X = 25718;
            point21.Y = 29012;
            point21.Z = 16495;
            Point3D point22 = new Point3D();
            point22.X = 25358;
            point22.Y = 29563;
            point22.Z = 16495; //ebd of 2nd floor/starting stairs
            Point3D point23 = new Point3D();
            point23.X = 25426;
            point23.Y = 29842;
            point23.Z = 16406;
            Point3D point24 = new Point3D();
            point24.X = 25842;
            point24.Y = 29983;
            point24.Z = 16223;
            Point3D point25 = new Point3D();
            point25.X = 26129;
            point25.Y = 29643;
            point25.Z = 16039;
            Point3D point26 = new Point3D();
            point26.X = 25714;
            point26.Y = 29267;
            point26.Z = 15796;
            Point3D point27 = new Point3D();
            point27.X = 25345;
            point27.Y = 29587;
            point27.Z = 15588;
            Point3D point28 = new Point3D();
            point28.X = 25711;
            point28.Y = 29995;
            point28.Z = 15357;
            Point3D point29 = new Point3D();
            point29.X = 26123;
            point29.Y = 29645;
            point29.Z = 15122; //start 1st floor/ end of stairs
            Point3D point30 = new Point3D();
            point30.X = 25796;
            point30.Y = 28979;
            point30.Z = 15120;
            Point3D point31 = new Point3D();
            point31.X = 24729;
            point31.Y = 29725;
            point31.Z = 15119;
            Point3D point32 = new Point3D();
            point32.X = 25695;
            point32.Y = 30592;
            point32.Z = 15119;
            Point3D point33 = new Point3D();
            point33.X = 26792;
            point33.Y = 29721;
            point33.Z = 15119;
            Point3D point34 = new Point3D();
            point34.X = 26102;
            point34.Y = 29302;
            point34.Z = 15120; //end of floor 1// going all way up now
            Point3D point35 = new Point3D();
            point35.X = 26085;
            point35.Y = 29802;
            point35.Z = 15192;
            Point3D point36 = new Point3D();
            point36.X = 25487;
            point36.Y = 29903;
            point36.Z = 15457;
            Point3D point37 = new Point3D();
            point37.X = 25370;
            point37.Y = 29483;
            point37.Z = 15625;
            Point3D point38 = new Point3D();
            point38.X = 25873;
            point38.Y = 29309;
            point38.Z = 15872;
            Point3D point39 = new Point3D();
            point39.X = 26103;
            point39.Y = 29695;
            point39.Z = 16058;
            Point3D point40 = new Point3D();
            point40.X = 25693;
            point40.Y = 29975;
            point40.Z = 16284;
            Point3D point41 = new Point3D();
            point41.X = 25352;
            point41.Y = 29538;
            point41.Z = 16495; //stairs entering 2nd floor
            Point3D point42 = new Point3D();
            point42.X = 25775;
            point42.Y = 29107;
            point42.Z = 16495;
            Point3D point43 = new Point3D();
            point43.X = 26114;
            point43.Y = 29597;
            point43.Z = 16495;
            Point3D point44 = new Point3D();
            point44.X = 25730;
            point44.Y = 29985;
            point44.Z = 16722;
            Point3D point45 = new Point3D();
            point45.X = 25368;
            point45.Y = 29610;
            point45.Z = 16957;
            Point3D point46 = new Point3D();
            point46.X = 25705;
            point46.Y = 29283;
            point46.Z = 17169;
            Point3D point47 = new Point3D();
            point47.X = 26109;
            point47.Y = 29587;
            point47.Z = 17393;
            Point3D point48 = new Point3D();
            point48.X = 25759;
            point48.Y = 30023;
            point48.Z = 17632;
            Point3D point49 = new Point3D();
            point49.X = 25359;
            point49.Y = 29578;
            point49.Z = 17871; //starting 3th floor
            Point3D point50 = new Point3D();
            point50.X = 25809;
            point50.Y = 29142;
            point50.Z = 17871;
            Point3D point51 = new Point3D();
            point51.X = 26344;
            point51.Y = 29391;
            point51.Z = 17871;
            Point3D spawn = new Point3D();
            spawn.X = 26995;
            spawn.Y = 29733;
            spawn.Z = 17871;

            #endregion path glocs

            if (!Body.InCombat && !HasAggro)
            {
                #region AllPathChecksHere

                if (!Body.IsWithinRadius(point1, 30) && path1 == false)
                {
                    Body.WalkTo(point1, 100);
                }
                else
                {
                    path1 = true;
                    walkback = false;
                    if (!Body.IsWithinRadius(point2, 30) && path1 == true && path2 == false)
                    {
                        Body.WalkTo(point2, 100);
                    }
                    else
                    {
                        path2 = true;
                        if (!Body.IsWithinRadius(point3, 30) && path1 == true && path2 == true && path3 == false)
                        {
                            Body.WalkTo(point3, 100);
                        }
                        else
                        {
                            path3 = true;
                            if (!Body.IsWithinRadius(point4, 30) && path1 == true && path2 == true && path3 == true &&
                                path4 == false)
                            {
                                Body.WalkTo(point4, 100);
                            }
                            else
                            {
                                path4 = true;
                                if (!Body.IsWithinRadius(point5, 30) && path1 == true && path2 == true &&
                                    path3 == true && path4 == true && path5 == false)
                                {
                                    Body.WalkTo(point5, 100);
                                }
                                else
                                {
                                    path5 = true;
                                    if (!Body.IsWithinRadius(point6, 30) && path1 == true && path2 == true &&
                                        path3 == true && path4 == true && path5 == true && path6 == false)
                                    {
                                        Body.WalkTo(point6, 100);
                                    }
                                    else
                                    {
                                        path6 = true;
                                        if (!Body.IsWithinRadius(point7, 30) && path1 == true && path2 == true &&
                                            path3 == true && path4 == true && path5 == true && path6 == true &&
                                            path7 == false)
                                        {
                                            Body.WalkTo(point7, 100);
                                        }
                                        else
                                        {
                                            path7 = true;
                                            if (!Body.IsWithinRadius(point8, 30) && path1 == true && path2 == true &&
                                                path3 == true && path4 == true && path5 == true && path6 == true &&
                                                path7 == true && path8 == false)
                                            {
                                                Body.WalkTo(point8, 100);
                                            }
                                            else
                                            {
                                                path8 = true;
                                                if (!Body.IsWithinRadius(point9, 30) && path1 == true &&
                                                    path2 == true && path3 == true && path4 == true && path5 == true &&
                                                    path6 == true && path7 == true && path8 == true && path9 == false)
                                                {
                                                    Body.WalkTo(point9, 100);
                                                }
                                                else
                                                {
                                                    path9 = true;
                                                    if (!Body.IsWithinRadius(point10, 30) && path1 == true &&
                                                        path2 == true && path3 == true && path4 == true &&
                                                        path5 == true && path6 == true && path7 == true &&
                                                        path8 == true && path9 == true && path10 == false)
                                                    {
                                                        Body.WalkTo(point10, 100);
                                                    }
                                                    else
                                                    {
                                                        path10 = true;
                                                        if (!Body.IsWithinRadius(point11, 30) && path1 == true &&
                                                            path2 == true && path3 == true && path4 == true &&
                                                            path5 == true && path6 == true && path7 == true &&
                                                            path8 == true && path9 == true && path10 == true
                                                            && path11 == false)
                                                        {
                                                            Body.WalkTo(point11, 100);
                                                        }
                                                        else
                                                        {
                                                            path11 = true;
                                                            if (!Body.IsWithinRadius(point12, 30) && path1 == true &&
                                                                path2 == true && path3 == true && path4 == true &&
                                                                path5 == true && path6 == true && path7 == true &&
                                                                path8 == true && path9 == true && path10 == true
                                                                && path11 == true && path12 == false)
                                                            {
                                                                Body.WalkTo(point12, 100);
                                                            }
                                                            else
                                                            {
                                                                path12 = true;
                                                                if (!Body.IsWithinRadius(point13, 30) &&
                                                                    path1 == true && path2 == true && path3 == true &&
                                                                    path4 == true && path5 == true && path6 == true &&
                                                                    path7 == true && path8 == true && path9 == true &&
                                                                    path10 == true
                                                                    && path11 == true && path12 == true &&
                                                                    path13 == false)
                                                                {
                                                                    Body.WalkTo(point13, 100);
                                                                }
                                                                else
                                                                {
                                                                    path13 = true;
                                                                    if (!Body.IsWithinRadius(point14, 30) &&
                                                                        path1 == true && path2 == true &&
                                                                        path3 == true && path4 == true &&
                                                                        path5 == true && path6 == true &&
                                                                        path7 == true && path8 == true &&
                                                                        path9 == true && path10 == true
                                                                        && path11 == true && path12 == true &&
                                                                        path13 == true && path14 == false)
                                                                    {
                                                                        Body.WalkTo(point14, 100);
                                                                    }
                                                                    else
                                                                    {
                                                                        path14 = true;
                                                                        if (!Body.IsWithinRadius(point15, 30) &&
                                                                            path1 == true && path2 == true &&
                                                                            path3 == true && path4 == true &&
                                                                            path5 == true && path6 == true &&
                                                                            path7 == true && path8 == true &&
                                                                            path9 == true && path10 == true
                                                                            && path11 == true && path12 == true &&
                                                                            path13 == true && path14 == true &&
                                                                            path15 == false)
                                                                        {
                                                                            Body.WalkTo(point15, 100);
                                                                        }
                                                                        else
                                                                        {
                                                                            path15 = true;
                                                                            if (!Body.IsWithinRadius(point16, 30) &&
                                                                                path1 == true && path2 == true &&
                                                                                path3 == true && path4 == true &&
                                                                                path5 == true && path6 == true &&
                                                                                path7 == true && path8 == true &&
                                                                                path9 == true && path10 == true
                                                                                && path11 == true && path12 == true &&
                                                                                path13 == true && path14 == true &&
                                                                                path15 == true && path16 == false)
                                                                            {
                                                                                Body.WalkTo(point16, 100);
                                                                            }
                                                                            else
                                                                            {
                                                                                path16 = true;
                                                                                if (!Body.IsWithinRadius(point17, 30) &&
                                                                                 path1 == true && path2 == true &&
                                                                                 path3 == true && path4 == true &&
                                                                                 path5 == true && path6 == true &&
                                                                                 path7 == true && path8 == true &&
                                                                                 path9 == true && path10 == true
                                                                                 && path11 == true &&
                                                                                 path12 == true && path13 == true &&
                                                                                 path14 == true && path15 == true &&
                                                                                 path16 == true && path17 == false)
                                                                                {
                                                                                    Body.WalkTo(point17, 100);
                                                                                }
                                                                                else
                                                                                {
                                                                                    path17 = true;
                                                                                    if (!Body.IsWithinRadius(point18,
                                                                                         30) && path1 == true &&
                                                                                     path2 == true &&
                                                                                     path3 == true &&
                                                                                     path4 == true &&
                                                                                     path5 == true &&
                                                                                     path6 == true &&
                                                                                     path7 == true &&
                                                                                     path8 == true &&
                                                                                     path9 == true && path10 == true
                                                                                     && path11 == true &&
                                                                                     path12 == true &&
                                                                                     path13 == true &&
                                                                                     path14 == true &&
                                                                                     path15 == true &&
                                                                                     path16 == true &&
                                                                                     path17 == true &&
                                                                                     path18 == false)
                                                                                    {
                                                                                        Body.WalkTo(point18, 100);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        path18 = true;
                                                                                        if (!Body.IsWithinRadius(
                                                                                             point19, 30) &&
                                                                                         path1 == true &&
                                                                                         path2 == true &&
                                                                                         path3 == true &&
                                                                                         path4 == true &&
                                                                                         path5 == true &&
                                                                                         path6 == true &&
                                                                                         path7 == true &&
                                                                                         path8 == true &&
                                                                                         path9 == true &&
                                                                                         path10 == true
                                                                                         && path11 == true &&
                                                                                         path12 == true &&
                                                                                         path13 == true &&
                                                                                         path14 == true &&
                                                                                         path15 == true &&
                                                                                         path16 == true &&
                                                                                         path17 == true &&
                                                                                         path18 == true &&
                                                                                         path19 == false)
                                                                                        {
                                                                                            Body.WalkTo(point19, 100);
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            path19 = true;
                                                                                            if (!Body.IsWithinRadius(
                                                                                                 point20, 30) &&
                                                                                             path1 == true &&
                                                                                             path2 == true &&
                                                                                             path3 == true &&
                                                                                             path4 == true &&
                                                                                             path5 == true &&
                                                                                             path6 == true &&
                                                                                             path7 == true &&
                                                                                             path8 == true &&
                                                                                             path9 == true &&
                                                                                             path10 == true
                                                                                             && path11 == true &&
                                                                                             path12 == true &&
                                                                                             path13 == true &&
                                                                                             path14 == true &&
                                                                                             path15 == true &&
                                                                                             path16 == true &&
                                                                                             path17 == true &&
                                                                                             path18 == true &&
                                                                                             path19 == true &&
                                                                                             path20 == false)
                                                                                            {
                                                                                                Body.WalkTo(point20,
                                                                                                    100);
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                path20 = true;
                                                                                                if (!Body
                                                                                                     .IsWithinRadius(
                                                                                                         point21,
                                                                                                         30) &&
                                                                                                 path1 == true &&
                                                                                                 path2 == true &&
                                                                                                 path3 == true &&
                                                                                                 path4 == true &&
                                                                                                 path5 == true &&
                                                                                                 path6 == true &&
                                                                                                 path7 == true &&
                                                                                                 path8 == true &&
                                                                                                 path9 == true &&
                                                                                                 path10 == true
                                                                                                 && path11 ==
                                                                                                 true &&
                                                                                                 path12 == true &&
                                                                                                 path13 == true &&
                                                                                                 path14 == true &&
                                                                                                 path15 == true &&
                                                                                                 path16 == true &&
                                                                                                 path17 == true &&
                                                                                                 path18 == true &&
                                                                                                 path19 == true &&
                                                                                                 path20 == true
                                                                                                 && path21 == false)
                                                                                                {
                                                                                                    Body.WalkTo(point21,
                                                                                                        100);
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    path21 = true;
                                                                                                    if (!Body
                                                                                                         .IsWithinRadius(
                                                                                                             point22,
                                                                                                             30) &&
                                                                                                     path1 ==
                                                                                                     true &&
                                                                                                     path2 ==
                                                                                                     true &&
                                                                                                     path3 ==
                                                                                                     true &&
                                                                                                     path4 ==
                                                                                                     true &&
                                                                                                     path5 ==
                                                                                                     true &&
                                                                                                     path6 ==
                                                                                                     true &&
                                                                                                     path7 ==
                                                                                                     true &&
                                                                                                     path8 ==
                                                                                                     true &&
                                                                                                     path9 ==
                                                                                                     true &&
                                                                                                     path10 == true
                                                                                                     && path11 ==
                                                                                                     true &&
                                                                                                     path12 ==
                                                                                                     true &&
                                                                                                     path13 ==
                                                                                                     true &&
                                                                                                     path14 ==
                                                                                                     true &&
                                                                                                     path15 ==
                                                                                                     true &&
                                                                                                     path16 ==
                                                                                                     true &&
                                                                                                     path17 ==
                                                                                                     true &&
                                                                                                     path18 ==
                                                                                                     true &&
                                                                                                     path19 ==
                                                                                                     true &&
                                                                                                     path20 == true
                                                                                                     && path21 ==
                                                                                                     true &&
                                                                                                     path22 ==
                                                                                                     false)
                                                                                                    {
                                                                                                        Body.WalkTo(
                                                                                                            point22,
                                                                                                            100);
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        path22 = true;
                                                                                                        if (!Body
                                                                                                             .IsWithinRadius(
                                                                                                                 point23,
                                                                                                                 30) &&
                                                                                                         path1 ==
                                                                                                         true &&
                                                                                                         path2 ==
                                                                                                         true &&
                                                                                                         path3 ==
                                                                                                         true &&
                                                                                                         path4 ==
                                                                                                         true &&
                                                                                                         path5 ==
                                                                                                         true &&
                                                                                                         path6 ==
                                                                                                         true &&
                                                                                                         path7 ==
                                                                                                         true &&
                                                                                                         path8 ==
                                                                                                         true &&
                                                                                                         path9 ==
                                                                                                         true &&
                                                                                                         path10 ==
                                                                                                         true
                                                                                                         &&
                                                                                                         path11 ==
                                                                                                         true &&
                                                                                                         path12 ==
                                                                                                         true &&
                                                                                                         path13 ==
                                                                                                         true &&
                                                                                                         path14 ==
                                                                                                         true &&
                                                                                                         path15 ==
                                                                                                         true &&
                                                                                                         path16 ==
                                                                                                         true &&
                                                                                                         path17 ==
                                                                                                         true &&
                                                                                                         path18 ==
                                                                                                         true &&
                                                                                                         path19 ==
                                                                                                         true &&
                                                                                                         path20 ==
                                                                                                         true
                                                                                                         &&
                                                                                                         path21 ==
                                                                                                         true &&
                                                                                                         path22 ==
                                                                                                         true &&
                                                                                                         path23 ==
                                                                                                         false)
                                                                                                        {
                                                                                                            Body.WalkTo(
                                                                                                                point23,
                                                                                                                100);
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            path23 =
                                                                                                                true;
                                                                                                            if (!Body
                                                                                                                 .IsWithinRadius(
                                                                                                                     point24,
                                                                                                                     30) &&
                                                                                                             path1 ==
                                                                                                             true &&
                                                                                                             path2 ==
                                                                                                             true &&
                                                                                                             path3 ==
                                                                                                             true &&
                                                                                                             path4 ==
                                                                                                             true &&
                                                                                                             path5 ==
                                                                                                             true &&
                                                                                                             path6 ==
                                                                                                             true &&
                                                                                                             path7 ==
                                                                                                             true &&
                                                                                                             path8 ==
                                                                                                             true &&
                                                                                                             path9 ==
                                                                                                             true &&
                                                                                                             path10 ==
                                                                                                             true
                                                                                                             &&
                                                                                                             path11 ==
                                                                                                             true &&
                                                                                                             path12 ==
                                                                                                             true &&
                                                                                                             path13 ==
                                                                                                             true &&
                                                                                                             path14 ==
                                                                                                             true &&
                                                                                                             path15 ==
                                                                                                             true &&
                                                                                                             path16 ==
                                                                                                             true &&
                                                                                                             path17 ==
                                                                                                             true &&
                                                                                                             path18 ==
                                                                                                             true &&
                                                                                                             path19 ==
                                                                                                             true &&
                                                                                                             path20 ==
                                                                                                             true
                                                                                                             &&
                                                                                                             path21 ==
                                                                                                             true &&
                                                                                                             path22 ==
                                                                                                             true &&
                                                                                                             path23 ==
                                                                                                             true &&
                                                                                                             path24 ==
                                                                                                             false)
                                                                                                            {
                                                                                                                Body
                                                                                                                    .WalkTo(
                                                                                                                        point24,
                                                                                                                        100);
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                path24 =
                                                                                                                    true;
                                                                                                                if
                                                                                                                    (!
                                                                                                                         Body
                                                                                                                             .IsWithinRadius(
                                                                                                                                 point25,
                                                                                                                                 30) &&
                                                                                                                     path1 ==
                                                                                                                     true &&
                                                                                                                     path2 ==
                                                                                                                     true &&
                                                                                                                     path3 ==
                                                                                                                     true &&
                                                                                                                     path4 ==
                                                                                                                     true &&
                                                                                                                     path5 ==
                                                                                                                     true &&
                                                                                                                     path6 ==
                                                                                                                     true &&
                                                                                                                     path7 ==
                                                                                                                     true &&
                                                                                                                     path8 ==
                                                                                                                     true &&
                                                                                                                     path9 ==
                                                                                                                     true &&
                                                                                                                     path10 ==
                                                                                                                     true
                                                                                                                     &&
                                                                                                                     path11 ==
                                                                                                                     true &&
                                                                                                                     path12 ==
                                                                                                                     true &&
                                                                                                                     path13 ==
                                                                                                                     true &&
                                                                                                                     path14 ==
                                                                                                                     true &&
                                                                                                                     path15 ==
                                                                                                                     true &&
                                                                                                                     path16 ==
                                                                                                                     true &&
                                                                                                                     path17 ==
                                                                                                                     true &&
                                                                                                                     path18 ==
                                                                                                                     true &&
                                                                                                                     path19 ==
                                                                                                                     true &&
                                                                                                                     path20 ==
                                                                                                                     true
                                                                                                                     &&
                                                                                                                     path21 ==
                                                                                                                     true &&
                                                                                                                     path22 ==
                                                                                                                     true &&
                                                                                                                     path23 ==
                                                                                                                     true &&
                                                                                                                     path24 ==
                                                                                                                     true &&
                                                                                                                     path25 ==
                                                                                                                     false)
                                                                                                                {
                                                                                                                    Body
                                                                                                                        .WalkTo(
                                                                                                                            point25,
                                                                                                                            100);
                                                                                                                }
                                                                                                                else
                                                                                                                {
                                                                                                                    path25 =
                                                                                                                        true;
                                                                                                                    if
                                                                                                                        (!
                                                                                                                             Body
                                                                                                                                 .IsWithinRadius(
                                                                                                                                     point26,
                                                                                                                                     30) &&
                                                                                                                         path1 ==
                                                                                                                         true &&
                                                                                                                         path2 ==
                                                                                                                         true &&
                                                                                                                         path3 ==
                                                                                                                         true &&
                                                                                                                         path4 ==
                                                                                                                         true &&
                                                                                                                         path5 ==
                                                                                                                         true &&
                                                                                                                         path6 ==
                                                                                                                         true &&
                                                                                                                         path7 ==
                                                                                                                         true &&
                                                                                                                         path8 ==
                                                                                                                         true &&
                                                                                                                         path9 ==
                                                                                                                         true &&
                                                                                                                         path10 ==
                                                                                                                         true
                                                                                                                         &&
                                                                                                                         path11 ==
                                                                                                                         true &&
                                                                                                                         path12 ==
                                                                                                                         true &&
                                                                                                                         path13 ==
                                                                                                                         true &&
                                                                                                                         path14 ==
                                                                                                                         true &&
                                                                                                                         path15 ==
                                                                                                                         true &&
                                                                                                                         path16 ==
                                                                                                                         true &&
                                                                                                                         path17 ==
                                                                                                                         true &&
                                                                                                                         path18 ==
                                                                                                                         true &&
                                                                                                                         path19 ==
                                                                                                                         true &&
                                                                                                                         path20 ==
                                                                                                                         true
                                                                                                                         &&
                                                                                                                         path21 ==
                                                                                                                         true &&
                                                                                                                         path22 ==
                                                                                                                         true &&
                                                                                                                         path23 ==
                                                                                                                         true &&
                                                                                                                         path24 ==
                                                                                                                         true &&
                                                                                                                         path25 ==
                                                                                                                         true &&
                                                                                                                         path26 ==
                                                                                                                         false)
                                                                                                                    {
                                                                                                                        Body
                                                                                                                            .WalkTo(
                                                                                                                                point26,
                                                                                                                                100);
                                                                                                                    }
                                                                                                                    else
                                                                                                                    {
                                                                                                                        path26 =
                                                                                                                            true;
                                                                                                                        if
                                                                                                                            (!
                                                                                                                                 Body
                                                                                                                                     .IsWithinRadius(
                                                                                                                                         point27,
                                                                                                                                         30) &&
                                                                                                                             path1 ==
                                                                                                                             true &&
                                                                                                                             path2 ==
                                                                                                                             true &&
                                                                                                                             path3 ==
                                                                                                                             true &&
                                                                                                                             path4 ==
                                                                                                                             true &&
                                                                                                                             path5 ==
                                                                                                                             true &&
                                                                                                                             path6 ==
                                                                                                                             true &&
                                                                                                                             path7 ==
                                                                                                                             true &&
                                                                                                                             path8 ==
                                                                                                                             true &&
                                                                                                                             path9 ==
                                                                                                                             true &&
                                                                                                                             path10 ==
                                                                                                                             true
                                                                                                                             &&
                                                                                                                             path11 ==
                                                                                                                             true &&
                                                                                                                             path12 ==
                                                                                                                             true &&
                                                                                                                             path13 ==
                                                                                                                             true &&
                                                                                                                             path14 ==
                                                                                                                             true &&
                                                                                                                             path15 ==
                                                                                                                             true &&
                                                                                                                             path16 ==
                                                                                                                             true &&
                                                                                                                             path17 ==
                                                                                                                             true &&
                                                                                                                             path18 ==
                                                                                                                             true &&
                                                                                                                             path19 ==
                                                                                                                             true &&
                                                                                                                             path20 ==
                                                                                                                             true
                                                                                                                             &&
                                                                                                                             path21 ==
                                                                                                                             true &&
                                                                                                                             path22 ==
                                                                                                                             true &&
                                                                                                                             path23 ==
                                                                                                                             true &&
                                                                                                                             path24 ==
                                                                                                                             true &&
                                                                                                                             path25 ==
                                                                                                                             true &&
                                                                                                                             path26 ==
                                                                                                                             true &&
                                                                                                                             path27 ==
                                                                                                                             false)
                                                                                                                        {
                                                                                                                            Body
                                                                                                                                .WalkTo(
                                                                                                                                    point27,
                                                                                                                                    100);
                                                                                                                        }
                                                                                                                        else
                                                                                                                        {
                                                                                                                            path27 =
                                                                                                                                true;
                                                                                                                            if
                                                                                                                                (!
                                                                                                                                     Body
                                                                                                                                         .IsWithinRadius(
                                                                                                                                             point28,
                                                                                                                                             30) &&
                                                                                                                                 path1 ==
                                                                                                                                 true &&
                                                                                                                                 path2 ==
                                                                                                                                 true &&
                                                                                                                                 path3 ==
                                                                                                                                 true &&
                                                                                                                                 path4 ==
                                                                                                                                 true &&
                                                                                                                                 path5 ==
                                                                                                                                 true &&
                                                                                                                                 path6 ==
                                                                                                                                 true &&
                                                                                                                                 path7 ==
                                                                                                                                 true &&
                                                                                                                                 path8 ==
                                                                                                                                 true &&
                                                                                                                                 path9 ==
                                                                                                                                 true &&
                                                                                                                                 path10 ==
                                                                                                                                 true
                                                                                                                                 &&
                                                                                                                                 path11 ==
                                                                                                                                 true &&
                                                                                                                                 path12 ==
                                                                                                                                 true &&
                                                                                                                                 path13 ==
                                                                                                                                 true &&
                                                                                                                                 path14 ==
                                                                                                                                 true &&
                                                                                                                                 path15 ==
                                                                                                                                 true &&
                                                                                                                                 path16 ==
                                                                                                                                 true &&
                                                                                                                                 path17 ==
                                                                                                                                 true &&
                                                                                                                                 path18 ==
                                                                                                                                 true &&
                                                                                                                                 path19 ==
                                                                                                                                 true &&
                                                                                                                                 path20 ==
                                                                                                                                 true
                                                                                                                                 &&
                                                                                                                                 path21 ==
                                                                                                                                 true &&
                                                                                                                                 path22 ==
                                                                                                                                 true &&
                                                                                                                                 path23 ==
                                                                                                                                 true &&
                                                                                                                                 path24 ==
                                                                                                                                 true &&
                                                                                                                                 path25 ==
                                                                                                                                 true &&
                                                                                                                                 path26 ==
                                                                                                                                 true &&
                                                                                                                                 path27 ==
                                                                                                                                 true &&
                                                                                                                                 path28 ==
                                                                                                                                 false)
                                                                                                                            {
                                                                                                                                Body
                                                                                                                                    .WalkTo(
                                                                                                                                        point28,
                                                                                                                                        100);
                                                                                                                            }
                                                                                                                            else
                                                                                                                            {
                                                                                                                                path28 =
                                                                                                                                    true;
                                                                                                                                if
                                                                                                                                    (!
                                                                                                                                         Body
                                                                                                                                             .IsWithinRadius(
                                                                                                                                                 point29,
                                                                                                                                                 30) &&
                                                                                                                                     path1 ==
                                                                                                                                     true &&
                                                                                                                                     path2 ==
                                                                                                                                     true &&
                                                                                                                                     path3 ==
                                                                                                                                     true &&
                                                                                                                                     path4 ==
                                                                                                                                     true &&
                                                                                                                                     path5 ==
                                                                                                                                     true &&
                                                                                                                                     path6 ==
                                                                                                                                     true &&
                                                                                                                                     path7 ==
                                                                                                                                     true &&
                                                                                                                                     path8 ==
                                                                                                                                     true &&
                                                                                                                                     path9 ==
                                                                                                                                     true &&
                                                                                                                                     path10 ==
                                                                                                                                     true
                                                                                                                                     &&
                                                                                                                                     path11 ==
                                                                                                                                     true &&
                                                                                                                                     path12 ==
                                                                                                                                     true &&
                                                                                                                                     path13 ==
                                                                                                                                     true &&
                                                                                                                                     path14 ==
                                                                                                                                     true &&
                                                                                                                                     path15 ==
                                                                                                                                     true &&
                                                                                                                                     path16 ==
                                                                                                                                     true &&
                                                                                                                                     path17 ==
                                                                                                                                     true &&
                                                                                                                                     path18 ==
                                                                                                                                     true &&
                                                                                                                                     path19 ==
                                                                                                                                     true &&
                                                                                                                                     path20 ==
                                                                                                                                     true
                                                                                                                                     &&
                                                                                                                                     path21 ==
                                                                                                                                     true &&
                                                                                                                                     path22 ==
                                                                                                                                     true &&
                                                                                                                                     path23 ==
                                                                                                                                     true &&
                                                                                                                                     path24 ==
                                                                                                                                     true &&
                                                                                                                                     path25 ==
                                                                                                                                     true &&
                                                                                                                                     path26 ==
                                                                                                                                     true &&
                                                                                                                                     path27 ==
                                                                                                                                     true &&
                                                                                                                                     path28 ==
                                                                                                                                     true &&
                                                                                                                                     path29 ==
                                                                                                                                     false)
                                                                                                                                {
                                                                                                                                    Body
                                                                                                                                        .WalkTo(
                                                                                                                                            point29,
                                                                                                                                            100);
                                                                                                                                }
                                                                                                                                else
                                                                                                                                {
                                                                                                                                    path29 =
                                                                                                                                        true;
                                                                                                                                    if
                                                                                                                                        (!
                                                                                                                                             Body
                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                     point30,
                                                                                                                                                     30) &&
                                                                                                                                         path1 ==
                                                                                                                                         true &&
                                                                                                                                         path2 ==
                                                                                                                                         true &&
                                                                                                                                         path3 ==
                                                                                                                                         true &&
                                                                                                                                         path4 ==
                                                                                                                                         true &&
                                                                                                                                         path5 ==
                                                                                                                                         true &&
                                                                                                                                         path6 ==
                                                                                                                                         true &&
                                                                                                                                         path7 ==
                                                                                                                                         true &&
                                                                                                                                         path8 ==
                                                                                                                                         true &&
                                                                                                                                         path9 ==
                                                                                                                                         true &&
                                                                                                                                         path10 ==
                                                                                                                                         true
                                                                                                                                         &&
                                                                                                                                         path11 ==
                                                                                                                                         true &&
                                                                                                                                         path12 ==
                                                                                                                                         true &&
                                                                                                                                         path13 ==
                                                                                                                                         true &&
                                                                                                                                         path14 ==
                                                                                                                                         true &&
                                                                                                                                         path15 ==
                                                                                                                                         true &&
                                                                                                                                         path16 ==
                                                                                                                                         true &&
                                                                                                                                         path17 ==
                                                                                                                                         true &&
                                                                                                                                         path18 ==
                                                                                                                                         true &&
                                                                                                                                         path19 ==
                                                                                                                                         true &&
                                                                                                                                         path20 ==
                                                                                                                                         true
                                                                                                                                         &&
                                                                                                                                         path21 ==
                                                                                                                                         true &&
                                                                                                                                         path22 ==
                                                                                                                                         true &&
                                                                                                                                         path23 ==
                                                                                                                                         true &&
                                                                                                                                         path24 ==
                                                                                                                                         true &&
                                                                                                                                         path25 ==
                                                                                                                                         true &&
                                                                                                                                         path26 ==
                                                                                                                                         true &&
                                                                                                                                         path27 ==
                                                                                                                                         true &&
                                                                                                                                         path28 ==
                                                                                                                                         true &&
                                                                                                                                         path29 ==
                                                                                                                                         true &&
                                                                                                                                         path30 ==
                                                                                                                                         false)
                                                                                                                                    {
                                                                                                                                        Body
                                                                                                                                            .WalkTo(
                                                                                                                                                point30,
                                                                                                                                                100);
                                                                                                                                    }
                                                                                                                                    else
                                                                                                                                    {
                                                                                                                                        path30 =
                                                                                                                                            true;
                                                                                                                                        if
                                                                                                                                            (!
                                                                                                                                                 Body
                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                         point31,
                                                                                                                                                         30) &&
                                                                                                                                             path1 ==
                                                                                                                                             true &&
                                                                                                                                             path2 ==
                                                                                                                                             true &&
                                                                                                                                             path3 ==
                                                                                                                                             true &&
                                                                                                                                             path4 ==
                                                                                                                                             true &&
                                                                                                                                             path5 ==
                                                                                                                                             true &&
                                                                                                                                             path6 ==
                                                                                                                                             true &&
                                                                                                                                             path7 ==
                                                                                                                                             true &&
                                                                                                                                             path8 ==
                                                                                                                                             true &&
                                                                                                                                             path9 ==
                                                                                                                                             true &&
                                                                                                                                             path10 ==
                                                                                                                                             true
                                                                                                                                             &&
                                                                                                                                             path11 ==
                                                                                                                                             true &&
                                                                                                                                             path12 ==
                                                                                                                                             true &&
                                                                                                                                             path13 ==
                                                                                                                                             true &&
                                                                                                                                             path14 ==
                                                                                                                                             true &&
                                                                                                                                             path15 ==
                                                                                                                                             true &&
                                                                                                                                             path16 ==
                                                                                                                                             true &&
                                                                                                                                             path17 ==
                                                                                                                                             true &&
                                                                                                                                             path18 ==
                                                                                                                                             true &&
                                                                                                                                             path19 ==
                                                                                                                                             true &&
                                                                                                                                             path20 ==
                                                                                                                                             true
                                                                                                                                             &&
                                                                                                                                             path21 ==
                                                                                                                                             true &&
                                                                                                                                             path22 ==
                                                                                                                                             true &&
                                                                                                                                             path23 ==
                                                                                                                                             true &&
                                                                                                                                             path24 ==
                                                                                                                                             true &&
                                                                                                                                             path25 ==
                                                                                                                                             true &&
                                                                                                                                             path26 ==
                                                                                                                                             true &&
                                                                                                                                             path27 ==
                                                                                                                                             true &&
                                                                                                                                             path28 ==
                                                                                                                                             true &&
                                                                                                                                             path29 ==
                                                                                                                                             true &&
                                                                                                                                             path30 ==
                                                                                                                                             true
                                                                                                                                             &&
                                                                                                                                             path31 ==
                                                                                                                                             false)
                                                                                                                                        {
                                                                                                                                            Body
                                                                                                                                                .WalkTo(
                                                                                                                                                    point31,
                                                                                                                                                    100);
                                                                                                                                        }
                                                                                                                                        else
                                                                                                                                        {
                                                                                                                                            path31 =
                                                                                                                                                true;
                                                                                                                                            if
                                                                                                                                                (!
                                                                                                                                                     Body
                                                                                                                                                         .IsWithinRadius(
                                                                                                                                                             point32,
                                                                                                                                                             30) &&
                                                                                                                                                 path1 ==
                                                                                                                                                 true &&
                                                                                                                                                 path2 ==
                                                                                                                                                 true &&
                                                                                                                                                 path3 ==
                                                                                                                                                 true &&
                                                                                                                                                 path4 ==
                                                                                                                                                 true &&
                                                                                                                                                 path5 ==
                                                                                                                                                 true &&
                                                                                                                                                 path6 ==
                                                                                                                                                 true &&
                                                                                                                                                 path7 ==
                                                                                                                                                 true &&
                                                                                                                                                 path8 ==
                                                                                                                                                 true &&
                                                                                                                                                 path9 ==
                                                                                                                                                 true &&
                                                                                                                                                 path10 ==
                                                                                                                                                 true
                                                                                                                                                 &&
                                                                                                                                                 path11 ==
                                                                                                                                                 true &&
                                                                                                                                                 path12 ==
                                                                                                                                                 true &&
                                                                                                                                                 path13 ==
                                                                                                                                                 true &&
                                                                                                                                                 path14 ==
                                                                                                                                                 true &&
                                                                                                                                                 path15 ==
                                                                                                                                                 true &&
                                                                                                                                                 path16 ==
                                                                                                                                                 true &&
                                                                                                                                                 path17 ==
                                                                                                                                                 true &&
                                                                                                                                                 path18 ==
                                                                                                                                                 true &&
                                                                                                                                                 path19 ==
                                                                                                                                                 true &&
                                                                                                                                                 path20 ==
                                                                                                                                                 true
                                                                                                                                                 &&
                                                                                                                                                 path21 ==
                                                                                                                                                 true &&
                                                                                                                                                 path22 ==
                                                                                                                                                 true &&
                                                                                                                                                 path23 ==
                                                                                                                                                 true &&
                                                                                                                                                 path24 ==
                                                                                                                                                 true &&
                                                                                                                                                 path25 ==
                                                                                                                                                 true &&
                                                                                                                                                 path26 ==
                                                                                                                                                 true &&
                                                                                                                                                 path27 ==
                                                                                                                                                 true &&
                                                                                                                                                 path28 ==
                                                                                                                                                 true &&
                                                                                                                                                 path29 ==
                                                                                                                                                 true &&
                                                                                                                                                 path30 ==
                                                                                                                                                 true
                                                                                                                                                 &&
                                                                                                                                                 path31 ==
                                                                                                                                                 true &&
                                                                                                                                                 path32 ==
                                                                                                                                                 false)
                                                                                                                                            {
                                                                                                                                                Body
                                                                                                                                                    .WalkTo(
                                                                                                                                                        point32,
                                                                                                                                                        100);
                                                                                                                                            }
                                                                                                                                            else
                                                                                                                                            {
                                                                                                                                                path32 =
                                                                                                                                                    true;
                                                                                                                                                if
                                                                                                                                                    (!
                                                                                                                                                         Body
                                                                                                                                                             .IsWithinRadius(
                                                                                                                                                                 point33,
                                                                                                                                                                 30) &&
                                                                                                                                                     path1 ==
                                                                                                                                                     true &&
                                                                                                                                                     path2 ==
                                                                                                                                                     true &&
                                                                                                                                                     path3 ==
                                                                                                                                                     true &&
                                                                                                                                                     path4 ==
                                                                                                                                                     true &&
                                                                                                                                                     path5 ==
                                                                                                                                                     true &&
                                                                                                                                                     path6 ==
                                                                                                                                                     true &&
                                                                                                                                                     path7 ==
                                                                                                                                                     true &&
                                                                                                                                                     path8 ==
                                                                                                                                                     true &&
                                                                                                                                                     path9 ==
                                                                                                                                                     true &&
                                                                                                                                                     path10 ==
                                                                                                                                                     true
                                                                                                                                                     &&
                                                                                                                                                     path11 ==
                                                                                                                                                     true &&
                                                                                                                                                     path12 ==
                                                                                                                                                     true &&
                                                                                                                                                     path13 ==
                                                                                                                                                     true &&
                                                                                                                                                     path14 ==
                                                                                                                                                     true &&
                                                                                                                                                     path15 ==
                                                                                                                                                     true &&
                                                                                                                                                     path16 ==
                                                                                                                                                     true &&
                                                                                                                                                     path17 ==
                                                                                                                                                     true &&
                                                                                                                                                     path18 ==
                                                                                                                                                     true &&
                                                                                                                                                     path19 ==
                                                                                                                                                     true &&
                                                                                                                                                     path20 ==
                                                                                                                                                     true
                                                                                                                                                     &&
                                                                                                                                                     path21 ==
                                                                                                                                                     true &&
                                                                                                                                                     path22 ==
                                                                                                                                                     true &&
                                                                                                                                                     path23 ==
                                                                                                                                                     true &&
                                                                                                                                                     path24 ==
                                                                                                                                                     true &&
                                                                                                                                                     path25 ==
                                                                                                                                                     true &&
                                                                                                                                                     path26 ==
                                                                                                                                                     true &&
                                                                                                                                                     path27 ==
                                                                                                                                                     true &&
                                                                                                                                                     path28 ==
                                                                                                                                                     true &&
                                                                                                                                                     path29 ==
                                                                                                                                                     true &&
                                                                                                                                                     path30 ==
                                                                                                                                                     true
                                                                                                                                                     &&
                                                                                                                                                     path31 ==
                                                                                                                                                     true &&
                                                                                                                                                     path32 ==
                                                                                                                                                     true &&
                                                                                                                                                     path33 ==
                                                                                                                                                     false)
                                                                                                                                                {
                                                                                                                                                    Body
                                                                                                                                                        .WalkTo(
                                                                                                                                                            point33,
                                                                                                                                                            100);
                                                                                                                                                }
                                                                                                                                                else
                                                                                                                                                {
                                                                                                                                                    path33 =
                                                                                                                                                        true;
                                                                                                                                                    if
                                                                                                                                                        (!
                                                                                                                                                             Body
                                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                                     point34,
                                                                                                                                                                     30) &&
                                                                                                                                                         path1 ==
                                                                                                                                                         true &&
                                                                                                                                                         path2 ==
                                                                                                                                                         true &&
                                                                                                                                                         path3 ==
                                                                                                                                                         true &&
                                                                                                                                                         path4 ==
                                                                                                                                                         true &&
                                                                                                                                                         path5 ==
                                                                                                                                                         true &&
                                                                                                                                                         path6 ==
                                                                                                                                                         true &&
                                                                                                                                                         path7 ==
                                                                                                                                                         true &&
                                                                                                                                                         path8 ==
                                                                                                                                                         true &&
                                                                                                                                                         path9 ==
                                                                                                                                                         true &&
                                                                                                                                                         path10 ==
                                                                                                                                                         true
                                                                                                                                                         &&
                                                                                                                                                         path11 ==
                                                                                                                                                         true &&
                                                                                                                                                         path12 ==
                                                                                                                                                         true &&
                                                                                                                                                         path13 ==
                                                                                                                                                         true &&
                                                                                                                                                         path14 ==
                                                                                                                                                         true &&
                                                                                                                                                         path15 ==
                                                                                                                                                         true &&
                                                                                                                                                         path16 ==
                                                                                                                                                         true &&
                                                                                                                                                         path17 ==
                                                                                                                                                         true &&
                                                                                                                                                         path18 ==
                                                                                                                                                         true &&
                                                                                                                                                         path19 ==
                                                                                                                                                         true &&
                                                                                                                                                         path20 ==
                                                                                                                                                         true
                                                                                                                                                         &&
                                                                                                                                                         path21 ==
                                                                                                                                                         true &&
                                                                                                                                                         path22 ==
                                                                                                                                                         true &&
                                                                                                                                                         path23 ==
                                                                                                                                                         true &&
                                                                                                                                                         path24 ==
                                                                                                                                                         true &&
                                                                                                                                                         path25 ==
                                                                                                                                                         true &&
                                                                                                                                                         path26 ==
                                                                                                                                                         true &&
                                                                                                                                                         path27 ==
                                                                                                                                                         true &&
                                                                                                                                                         path28 ==
                                                                                                                                                         true &&
                                                                                                                                                         path29 ==
                                                                                                                                                         true &&
                                                                                                                                                         path30 ==
                                                                                                                                                         true
                                                                                                                                                         &&
                                                                                                                                                         path31 ==
                                                                                                                                                         true &&
                                                                                                                                                         path32 ==
                                                                                                                                                         true &&
                                                                                                                                                         path33 ==
                                                                                                                                                         true &&
                                                                                                                                                         path34 ==
                                                                                                                                                         false)
                                                                                                                                                    {
                                                                                                                                                        Body
                                                                                                                                                            .WalkTo(
                                                                                                                                                                point34,
                                                                                                                                                                100);
                                                                                                                                                    }
                                                                                                                                                    else
                                                                                                                                                    {
                                                                                                                                                        path34 =
                                                                                                                                                            true;
                                                                                                                                                        if
                                                                                                                                                            (!
                                                                                                                                                                 Body
                                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                                         point35,
                                                                                                                                                                         30) &&
                                                                                                                                                             path1 ==
                                                                                                                                                             true &&
                                                                                                                                                             path2 ==
                                                                                                                                                             true &&
                                                                                                                                                             path3 ==
                                                                                                                                                             true &&
                                                                                                                                                             path4 ==
                                                                                                                                                             true &&
                                                                                                                                                             path5 ==
                                                                                                                                                             true &&
                                                                                                                                                             path6 ==
                                                                                                                                                             true &&
                                                                                                                                                             path7 ==
                                                                                                                                                             true &&
                                                                                                                                                             path8 ==
                                                                                                                                                             true &&
                                                                                                                                                             path9 ==
                                                                                                                                                             true &&
                                                                                                                                                             path10 ==
                                                                                                                                                             true
                                                                                                                                                             &&
                                                                                                                                                             path11 ==
                                                                                                                                                             true &&
                                                                                                                                                             path12 ==
                                                                                                                                                             true &&
                                                                                                                                                             path13 ==
                                                                                                                                                             true &&
                                                                                                                                                             path14 ==
                                                                                                                                                             true &&
                                                                                                                                                             path15 ==
                                                                                                                                                             true &&
                                                                                                                                                             path16 ==
                                                                                                                                                             true &&
                                                                                                                                                             path17 ==
                                                                                                                                                             true &&
                                                                                                                                                             path18 ==
                                                                                                                                                             true &&
                                                                                                                                                             path19 ==
                                                                                                                                                             true &&
                                                                                                                                                             path20 ==
                                                                                                                                                             true
                                                                                                                                                             &&
                                                                                                                                                             path21 ==
                                                                                                                                                             true &&
                                                                                                                                                             path22 ==
                                                                                                                                                             true &&
                                                                                                                                                             path23 ==
                                                                                                                                                             true &&
                                                                                                                                                             path24 ==
                                                                                                                                                             true &&
                                                                                                                                                             path25 ==
                                                                                                                                                             true &&
                                                                                                                                                             path26 ==
                                                                                                                                                             true &&
                                                                                                                                                             path27 ==
                                                                                                                                                             true &&
                                                                                                                                                             path28 ==
                                                                                                                                                             true &&
                                                                                                                                                             path29 ==
                                                                                                                                                             true &&
                                                                                                                                                             path30 ==
                                                                                                                                                             true
                                                                                                                                                             &&
                                                                                                                                                             path31 ==
                                                                                                                                                             true &&
                                                                                                                                                             path32 ==
                                                                                                                                                             true &&
                                                                                                                                                             path33 ==
                                                                                                                                                             true &&
                                                                                                                                                             path34 ==
                                                                                                                                                             true &&
                                                                                                                                                             path35 ==
                                                                                                                                                             false)
                                                                                                                                                        {
                                                                                                                                                            Body
                                                                                                                                                                .WalkTo(
                                                                                                                                                                    point35,
                                                                                                                                                                    100);
                                                                                                                                                        }
                                                                                                                                                        else
                                                                                                                                                        {
                                                                                                                                                            path35 =
                                                                                                                                                                true;
                                                                                                                                                            if
                                                                                                                                                                (!
                                                                                                                                                                     Body
                                                                                                                                                                         .IsWithinRadius(
                                                                                                                                                                             point36,
                                                                                                                                                                             30) &&
                                                                                                                                                                 path1 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path2 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path3 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path4 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path5 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path6 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path7 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path8 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path9 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path10 ==
                                                                                                                                                                 true
                                                                                                                                                                 &&
                                                                                                                                                                 path11 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path12 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path13 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path14 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path15 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path16 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path17 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path18 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path19 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path20 ==
                                                                                                                                                                 true
                                                                                                                                                                 &&
                                                                                                                                                                 path21 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path22 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path23 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path24 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path25 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path26 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path27 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path28 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path29 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path30 ==
                                                                                                                                                                 true
                                                                                                                                                                 &&
                                                                                                                                                                 path31 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path32 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path33 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path34 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path35 ==
                                                                                                                                                                 true &&
                                                                                                                                                                 path36 ==
                                                                                                                                                                 false)
                                                                                                                                                            {
                                                                                                                                                                Body
                                                                                                                                                                    .WalkTo(
                                                                                                                                                                        point36,
                                                                                                                                                                        100);
                                                                                                                                                            }
                                                                                                                                                            else
                                                                                                                                                            {
                                                                                                                                                                path36 =
                                                                                                                                                                    true;
                                                                                                                                                                if
                                                                                                                                                                    (!
                                                                                                                                                                         Body
                                                                                                                                                                             .IsWithinRadius(
                                                                                                                                                                                 point37,
                                                                                                                                                                                 30) &&
                                                                                                                                                                     path1 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path2 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path3 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path4 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path5 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path6 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path7 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path8 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path9 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path10 ==
                                                                                                                                                                     true
                                                                                                                                                                     &&
                                                                                                                                                                     path11 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path12 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path13 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path14 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path15 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path16 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path17 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path18 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path19 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path20 ==
                                                                                                                                                                     true
                                                                                                                                                                     &&
                                                                                                                                                                     path21 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path22 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path23 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path24 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path25 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path26 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path27 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path28 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path29 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path30 ==
                                                                                                                                                                     true
                                                                                                                                                                     &&
                                                                                                                                                                     path31 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path32 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path33 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path34 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path35 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path36 ==
                                                                                                                                                                     true &&
                                                                                                                                                                     path37 ==
                                                                                                                                                                     false)
                                                                                                                                                                {
                                                                                                                                                                    Body
                                                                                                                                                                        .WalkTo(
                                                                                                                                                                            point37,
                                                                                                                                                                            100);
                                                                                                                                                                }
                                                                                                                                                                else
                                                                                                                                                                {
                                                                                                                                                                    path37 =
                                                                                                                                                                        true;
                                                                                                                                                                    if
                                                                                                                                                                        (!
                                                                                                                                                                             Body
                                                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                                                     point38,
                                                                                                                                                                                     30) &&
                                                                                                                                                                         path1 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path2 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path3 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path4 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path5 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path6 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path7 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path8 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path9 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path10 ==
                                                                                                                                                                         true
                                                                                                                                                                         &&
                                                                                                                                                                         path11 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path12 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path13 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path14 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path15 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path16 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path17 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path18 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path19 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path20 ==
                                                                                                                                                                         true
                                                                                                                                                                         &&
                                                                                                                                                                         path21 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path22 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path23 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path24 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path25 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path26 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path27 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path28 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path29 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path30 ==
                                                                                                                                                                         true
                                                                                                                                                                         &&
                                                                                                                                                                         path31 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path32 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path33 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path34 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path35 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path36 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path37 ==
                                                                                                                                                                         true &&
                                                                                                                                                                         path38 ==
                                                                                                                                                                         false)
                                                                                                                                                                    {
                                                                                                                                                                        Body
                                                                                                                                                                            .WalkTo(
                                                                                                                                                                                point38,
                                                                                                                                                                                100);
                                                                                                                                                                    }
                                                                                                                                                                    else
                                                                                                                                                                    {
                                                                                                                                                                        path38 =
                                                                                                                                                                            true;
                                                                                                                                                                        if
                                                                                                                                                                            (!
                                                                                                                                                                                 Body
                                                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                                                         point39,
                                                                                                                                                                                         30) &&
                                                                                                                                                                             path1 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path2 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path3 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path4 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path5 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path6 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path7 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path8 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path9 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path10 ==
                                                                                                                                                                             true
                                                                                                                                                                             &&
                                                                                                                                                                             path11 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path12 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path13 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path14 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path15 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path16 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path17 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path18 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path19 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path20 ==
                                                                                                                                                                             true
                                                                                                                                                                             &&
                                                                                                                                                                             path21 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path22 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path23 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path24 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path25 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path26 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path27 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path28 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path29 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path30 ==
                                                                                                                                                                             true
                                                                                                                                                                             &&
                                                                                                                                                                             path31 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path32 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path33 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path34 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path35 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path36 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path37 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path38 ==
                                                                                                                                                                             true &&
                                                                                                                                                                             path39 ==
                                                                                                                                                                             false)
                                                                                                                                                                        {
                                                                                                                                                                            Body
                                                                                                                                                                                .WalkTo(
                                                                                                                                                                                    point39,
                                                                                                                                                                                    100);
                                                                                                                                                                        }
                                                                                                                                                                        else
                                                                                                                                                                        {
                                                                                                                                                                            path39 =
                                                                                                                                                                                true;
                                                                                                                                                                            if
                                                                                                                                                                                (!
                                                                                                                                                                                     Body
                                                                                                                                                                                         .IsWithinRadius(
                                                                                                                                                                                             point40,
                                                                                                                                                                                             30) &&
                                                                                                                                                                                 path1 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path2 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path3 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path4 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path5 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path6 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path7 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path8 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path9 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path10 ==
                                                                                                                                                                                 true
                                                                                                                                                                                 &&
                                                                                                                                                                                 path11 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path12 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path13 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path14 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path15 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path16 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path17 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path18 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path19 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path20 ==
                                                                                                                                                                                 true
                                                                                                                                                                                 &&
                                                                                                                                                                                 path21 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path22 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path23 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path24 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path25 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path26 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path27 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path28 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path29 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path30 ==
                                                                                                                                                                                 true
                                                                                                                                                                                 &&
                                                                                                                                                                                 path31 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path32 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path33 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path34 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path35 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path36 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path37 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path38 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path39 ==
                                                                                                                                                                                 true &&
                                                                                                                                                                                 path40 ==
                                                                                                                                                                                 false)
                                                                                                                                                                            {
                                                                                                                                                                                Body
                                                                                                                                                                                    .WalkTo(
                                                                                                                                                                                        point40,
                                                                                                                                                                                        100);
                                                                                                                                                                            }
                                                                                                                                                                            else
                                                                                                                                                                            {
                                                                                                                                                                                path40 =
                                                                                                                                                                                    true;
                                                                                                                                                                                if
                                                                                                                                                                                    (!
                                                                                                                                                                                         Body
                                                                                                                                                                                             .IsWithinRadius(
                                                                                                                                                                                                 point41,
                                                                                                                                                                                                 30) &&
                                                                                                                                                                                     path1 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path2 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path3 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path4 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path5 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path6 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path7 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path8 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path9 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path10 ==
                                                                                                                                                                                     true
                                                                                                                                                                                     &&
                                                                                                                                                                                     path11 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path12 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path13 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path14 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path15 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path16 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path17 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path18 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path19 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path20 ==
                                                                                                                                                                                     true
                                                                                                                                                                                     &&
                                                                                                                                                                                     path21 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path22 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path23 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path24 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path25 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path26 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path27 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path28 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path29 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path30 ==
                                                                                                                                                                                     true
                                                                                                                                                                                     &&
                                                                                                                                                                                     path31 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path32 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path33 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path34 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path35 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path36 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path37 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path38 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path39 ==
                                                                                                                                                                                     true &&
                                                                                                                                                                                     path40 ==
                                                                                                                                                                                     true
                                                                                                                                                                                     &&
                                                                                                                                                                                     path41 ==
                                                                                                                                                                                     false)
                                                                                                                                                                                {
                                                                                                                                                                                    Body
                                                                                                                                                                                        .WalkTo(
                                                                                                                                                                                            point41,
                                                                                                                                                                                            100);
                                                                                                                                                                                }
                                                                                                                                                                                else
                                                                                                                                                                                {
                                                                                                                                                                                    path41 =
                                                                                                                                                                                        true;
                                                                                                                                                                                    if
                                                                                                                                                                                        (!
                                                                                                                                                                                             Body
                                                                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                                                                     point42,
                                                                                                                                                                                                     30) &&
                                                                                                                                                                                         path1 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path2 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path3 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path4 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path5 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path6 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path7 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path8 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path9 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path10 ==
                                                                                                                                                                                         true
                                                                                                                                                                                         &&
                                                                                                                                                                                         path11 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path12 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path13 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path14 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path15 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path16 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path17 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path18 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path19 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path20 ==
                                                                                                                                                                                         true
                                                                                                                                                                                         &&
                                                                                                                                                                                         path21 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path22 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path23 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path24 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path25 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path26 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path27 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path28 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path29 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path30 ==
                                                                                                                                                                                         true
                                                                                                                                                                                         &&
                                                                                                                                                                                         path31 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path32 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path33 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path34 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path35 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path36 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path37 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path38 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path39 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path40 ==
                                                                                                                                                                                         true
                                                                                                                                                                                         &&
                                                                                                                                                                                         path41 ==
                                                                                                                                                                                         true &&
                                                                                                                                                                                         path42 ==
                                                                                                                                                                                         false)
                                                                                                                                                                                    {
                                                                                                                                                                                        Body
                                                                                                                                                                                            .WalkTo(
                                                                                                                                                                                                point42,
                                                                                                                                                                                                100);
                                                                                                                                                                                    }
                                                                                                                                                                                    else
                                                                                                                                                                                    {
                                                                                                                                                                                        path42 =
                                                                                                                                                                                            true;
                                                                                                                                                                                        if
                                                                                                                                                                                            (!
                                                                                                                                                                                                 Body
                                                                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                                                                         point43,
                                                                                                                                                                                                         30) &&
                                                                                                                                                                                             path1 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path2 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path3 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path4 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path5 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path6 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path7 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path8 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path9 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path10 ==
                                                                                                                                                                                             true
                                                                                                                                                                                             &&
                                                                                                                                                                                             path11 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path12 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path13 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path14 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path15 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path16 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path17 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path18 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path19 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path20 ==
                                                                                                                                                                                             true
                                                                                                                                                                                             &&
                                                                                                                                                                                             path21 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path22 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path23 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path24 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path25 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path26 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path27 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path28 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path29 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path30 ==
                                                                                                                                                                                             true
                                                                                                                                                                                             &&
                                                                                                                                                                                             path31 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path32 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path33 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path34 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path35 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path36 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path37 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path38 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path39 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path40 ==
                                                                                                                                                                                             true
                                                                                                                                                                                             &&
                                                                                                                                                                                             path41 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path42 ==
                                                                                                                                                                                             true &&
                                                                                                                                                                                             path43 ==
                                                                                                                                                                                             false)
                                                                                                                                                                                        {
                                                                                                                                                                                            Body
                                                                                                                                                                                                .WalkTo(
                                                                                                                                                                                                    point43,
                                                                                                                                                                                                    100);
                                                                                                                                                                                        }
                                                                                                                                                                                        else
                                                                                                                                                                                        {
                                                                                                                                                                                            path43 =
                                                                                                                                                                                                true;
                                                                                                                                                                                            if
                                                                                                                                                                                                (!
                                                                                                                                                                                                     Body
                                                                                                                                                                                                         .IsWithinRadius(
                                                                                                                                                                                                             point44,
                                                                                                                                                                                                             30) &&
                                                                                                                                                                                                 path1 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path2 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path3 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path4 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path5 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path6 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path7 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path8 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path9 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path10 ==
                                                                                                                                                                                                 true
                                                                                                                                                                                                 &&
                                                                                                                                                                                                 path11 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path12 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path13 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path14 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path15 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path16 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path17 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path18 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path19 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path20 ==
                                                                                                                                                                                                 true
                                                                                                                                                                                                 &&
                                                                                                                                                                                                 path21 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path22 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path23 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path24 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path25 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path26 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path27 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path28 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path29 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path30 ==
                                                                                                                                                                                                 true
                                                                                                                                                                                                 &&
                                                                                                                                                                                                 path31 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path32 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path33 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path34 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path35 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path36 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path37 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path38 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path39 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path40 ==
                                                                                                                                                                                                 true
                                                                                                                                                                                                 &&
                                                                                                                                                                                                 path41 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path42 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path43 ==
                                                                                                                                                                                                 true &&
                                                                                                                                                                                                 path44 ==
                                                                                                                                                                                                 false)
                                                                                                                                                                                            {
                                                                                                                                                                                                Body
                                                                                                                                                                                                    .WalkTo(
                                                                                                                                                                                                        point44,
                                                                                                                                                                                                        100);
                                                                                                                                                                                            }
                                                                                                                                                                                            else
                                                                                                                                                                                            {
                                                                                                                                                                                                path44 =
                                                                                                                                                                                                    true;
                                                                                                                                                                                                if
                                                                                                                                                                                                    (!
                                                                                                                                                                                                         Body
                                                                                                                                                                                                             .IsWithinRadius(
                                                                                                                                                                                                                 point45,
                                                                                                                                                                                                                 30) &&
                                                                                                                                                                                                     path1 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path2 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path3 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path4 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path5 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path6 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path7 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path8 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path9 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path10 ==
                                                                                                                                                                                                     true
                                                                                                                                                                                                     &&
                                                                                                                                                                                                     path11 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path12 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path13 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path14 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path15 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path16 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path17 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path18 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path19 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path20 ==
                                                                                                                                                                                                     true
                                                                                                                                                                                                     &&
                                                                                                                                                                                                     path21 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path22 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path23 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path24 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path25 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path26 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path27 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path28 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path29 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path30 ==
                                                                                                                                                                                                     true
                                                                                                                                                                                                     &&
                                                                                                                                                                                                     path31 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path32 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path33 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path34 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path35 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path36 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path37 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path38 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path39 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path40 ==
                                                                                                                                                                                                     true
                                                                                                                                                                                                     &&
                                                                                                                                                                                                     path41 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path42 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path43 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path44 ==
                                                                                                                                                                                                     true &&
                                                                                                                                                                                                     path45 ==
                                                                                                                                                                                                     false)
                                                                                                                                                                                                {
                                                                                                                                                                                                    Body
                                                                                                                                                                                                        .WalkTo(
                                                                                                                                                                                                            point45,
                                                                                                                                                                                                            100);
                                                                                                                                                                                                }
                                                                                                                                                                                                else
                                                                                                                                                                                                {
                                                                                                                                                                                                    path45 =
                                                                                                                                                                                                        true;
                                                                                                                                                                                                    if
                                                                                                                                                                                                        (!
                                                                                                                                                                                                             Body
                                                                                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                                                                                     point46,
                                                                                                                                                                                                                     30) &&
                                                                                                                                                                                                         path1 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path2 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path3 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path4 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path5 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path6 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path7 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path8 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path9 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path10 ==
                                                                                                                                                                                                         true
                                                                                                                                                                                                         &&
                                                                                                                                                                                                         path11 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path12 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path13 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path14 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path15 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path16 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path17 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path18 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path19 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path20 ==
                                                                                                                                                                                                         true
                                                                                                                                                                                                         &&
                                                                                                                                                                                                         path21 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path22 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path23 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path24 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path25 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path26 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path27 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path28 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path29 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path30 ==
                                                                                                                                                                                                         true
                                                                                                                                                                                                         &&
                                                                                                                                                                                                         path31 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path32 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path33 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path34 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path35 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path36 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path37 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path38 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path39 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path40 ==
                                                                                                                                                                                                         true
                                                                                                                                                                                                         &&
                                                                                                                                                                                                         path41 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path42 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path43 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path44 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path45 ==
                                                                                                                                                                                                         true &&
                                                                                                                                                                                                         path46 ==
                                                                                                                                                                                                         false)
                                                                                                                                                                                                    {
                                                                                                                                                                                                        Body
                                                                                                                                                                                                            .WalkTo(
                                                                                                                                                                                                                point46,
                                                                                                                                                                                                                100);
                                                                                                                                                                                                    }
                                                                                                                                                                                                    else
                                                                                                                                                                                                    {
                                                                                                                                                                                                        path46 =
                                                                                                                                                                                                            true;
                                                                                                                                                                                                        if
                                                                                                                                                                                                            (!
                                                                                                                                                                                                                 Body
                                                                                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                                                                                         point47,
                                                                                                                                                                                                                         30) &&
                                                                                                                                                                                                             path1 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path2 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path3 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path4 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path5 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path6 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path7 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path8 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path9 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path10 ==
                                                                                                                                                                                                             true
                                                                                                                                                                                                             &&
                                                                                                                                                                                                             path11 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path12 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path13 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path14 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path15 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path16 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path17 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path18 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path19 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path20 ==
                                                                                                                                                                                                             true
                                                                                                                                                                                                             &&
                                                                                                                                                                                                             path21 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path22 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path23 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path24 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path25 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path26 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path27 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path28 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path29 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path30 ==
                                                                                                                                                                                                             true
                                                                                                                                                                                                             &&
                                                                                                                                                                                                             path31 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path32 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path33 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path34 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path35 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path36 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path37 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path38 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path39 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path40 ==
                                                                                                                                                                                                             true
                                                                                                                                                                                                             &&
                                                                                                                                                                                                             path41 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path42 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path43 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path44 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path45 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path46 ==
                                                                                                                                                                                                             true &&
                                                                                                                                                                                                             path47 ==
                                                                                                                                                                                                             false)
                                                                                                                                                                                                        {
                                                                                                                                                                                                            Body
                                                                                                                                                                                                                .WalkTo(
                                                                                                                                                                                                                    point47,
                                                                                                                                                                                                                    100);
                                                                                                                                                                                                        }
                                                                                                                                                                                                        else
                                                                                                                                                                                                        {
                                                                                                                                                                                                            path47 =
                                                                                                                                                                                                                true;
                                                                                                                                                                                                            if
                                                                                                                                                                                                                (!
                                                                                                                                                                                                                     Body
                                                                                                                                                                                                                         .IsWithinRadius(
                                                                                                                                                                                                                             point48,
                                                                                                                                                                                                                             30) &&
                                                                                                                                                                                                                 path1 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path2 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path3 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path4 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path5 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path6 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path7 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path8 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path9 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path10 ==
                                                                                                                                                                                                                 true
                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                 path11 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path12 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path13 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path14 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path15 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path16 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path17 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path18 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path19 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path20 ==
                                                                                                                                                                                                                 true
                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                 path21 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path22 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path23 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path24 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path25 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path26 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path27 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path28 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path29 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path30 ==
                                                                                                                                                                                                                 true
                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                 path31 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path32 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path33 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path34 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path35 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path36 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path37 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path38 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path39 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path40 ==
                                                                                                                                                                                                                 true
                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                 path41 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path42 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path43 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path44 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path45 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path46 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path47 ==
                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                 path48 ==
                                                                                                                                                                                                                 false)
                                                                                                                                                                                                            {
                                                                                                                                                                                                                Body
                                                                                                                                                                                                                    .WalkTo(
                                                                                                                                                                                                                        point48,
                                                                                                                                                                                                                        100);
                                                                                                                                                                                                            }
                                                                                                                                                                                                            else
                                                                                                                                                                                                            {
                                                                                                                                                                                                                path48 =
                                                                                                                                                                                                                    true;
                                                                                                                                                                                                                if
                                                                                                                                                                                                                    (!
                                                                                                                                                                                                                         Body
                                                                                                                                                                                                                             .IsWithinRadius(
                                                                                                                                                                                                                                 point49,
                                                                                                                                                                                                                                 30) &&
                                                                                                                                                                                                                     path1 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path2 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path3 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path4 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path5 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path6 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path7 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path8 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path9 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path10 ==
                                                                                                                                                                                                                     true
                                                                                                                                                                                                                     &&
                                                                                                                                                                                                                     path11 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path12 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path13 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path14 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path15 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path16 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path17 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path18 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path19 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path20 ==
                                                                                                                                                                                                                     true
                                                                                                                                                                                                                     &&
                                                                                                                                                                                                                     path21 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path22 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path23 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path24 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path25 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path26 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path27 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path28 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path29 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path30 ==
                                                                                                                                                                                                                     true
                                                                                                                                                                                                                     &&
                                                                                                                                                                                                                     path31 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path32 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path33 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path34 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path35 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path36 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path37 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path38 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path39 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path40 ==
                                                                                                                                                                                                                     true
                                                                                                                                                                                                                     &&
                                                                                                                                                                                                                     path41 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path42 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path43 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path44 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path45 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path46 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path47 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path48 ==
                                                                                                                                                                                                                     true &&
                                                                                                                                                                                                                     path49 ==
                                                                                                                                                                                                                     false)
                                                                                                                                                                                                                {
                                                                                                                                                                                                                    Body
                                                                                                                                                                                                                        .WalkTo(
                                                                                                                                                                                                                            point49,
                                                                                                                                                                                                                            100);
                                                                                                                                                                                                                }
                                                                                                                                                                                                                else
                                                                                                                                                                                                                {
                                                                                                                                                                                                                    path49 =
                                                                                                                                                                                                                        true;
                                                                                                                                                                                                                    if
                                                                                                                                                                                                                        (!
                                                                                                                                                                                                                             Body
                                                                                                                                                                                                                                 .IsWithinRadius(
                                                                                                                                                                                                                                     point50,
                                                                                                                                                                                                                                     30) &&
                                                                                                                                                                                                                         path1 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path2 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path3 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path4 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path5 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path6 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path7 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path8 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path9 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path10 ==
                                                                                                                                                                                                                         true
                                                                                                                                                                                                                         &&
                                                                                                                                                                                                                         path11 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path12 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path13 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path14 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path15 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path16 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path17 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path18 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path19 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path20 ==
                                                                                                                                                                                                                         true
                                                                                                                                                                                                                         &&
                                                                                                                                                                                                                         path21 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path22 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path23 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path24 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path25 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path26 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path27 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path28 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path29 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path30 ==
                                                                                                                                                                                                                         true
                                                                                                                                                                                                                         &&
                                                                                                                                                                                                                         path31 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path32 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path33 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path34 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path35 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path36 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path37 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path38 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path39 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path40 ==
                                                                                                                                                                                                                         true
                                                                                                                                                                                                                         &&
                                                                                                                                                                                                                         path41 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path42 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path43 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path44 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path45 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path46 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path47 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path48 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path49 ==
                                                                                                                                                                                                                         true &&
                                                                                                                                                                                                                         path50 ==
                                                                                                                                                                                                                         false)
                                                                                                                                                                                                                    {
                                                                                                                                                                                                                        Body
                                                                                                                                                                                                                            .WalkTo(
                                                                                                                                                                                                                                point50,
                                                                                                                                                                                                                                100);
                                                                                                                                                                                                                    }
                                                                                                                                                                                                                    else
                                                                                                                                                                                                                    {
                                                                                                                                                                                                                        path50 =
                                                                                                                                                                                                                            true;
                                                                                                                                                                                                                        if
                                                                                                                                                                                                                            (!
                                                                                                                                                                                                                                 Body
                                                                                                                                                                                                                                     .IsWithinRadius(
                                                                                                                                                                                                                                         point51,
                                                                                                                                                                                                                                         30) &&
                                                                                                                                                                                                                             path1 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path2 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path3 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path4 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path5 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path6 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path7 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path8 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path9 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path10 ==
                                                                                                                                                                                                                             true
                                                                                                                                                                                                                             &&
                                                                                                                                                                                                                             path11 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path12 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path13 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path14 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path15 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path16 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path17 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path18 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path19 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path20 ==
                                                                                                                                                                                                                             true
                                                                                                                                                                                                                             &&
                                                                                                                                                                                                                             path21 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path22 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path23 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path24 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path25 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path26 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path27 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path28 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path29 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path30 ==
                                                                                                                                                                                                                             true
                                                                                                                                                                                                                             &&
                                                                                                                                                                                                                             path31 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path32 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path33 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path34 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path35 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path36 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path37 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path38 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path39 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path40 ==
                                                                                                                                                                                                                             true
                                                                                                                                                                                                                             &&
                                                                                                                                                                                                                             path41 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path42 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path43 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path44 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path45 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path46 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path47 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path48 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path49 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path50 ==
                                                                                                                                                                                                                             true &&
                                                                                                                                                                                                                             path51 ==
                                                                                                                                                                                                                             false)
                                                                                                                                                                                                                        {
                                                                                                                                                                                                                            Body
                                                                                                                                                                                                                                .WalkTo(
                                                                                                                                                                                                                                    point51,
                                                                                                                                                                                                                                    100);
                                                                                                                                                                                                                        }
                                                                                                                                                                                                                        else
                                                                                                                                                                                                                        {
                                                                                                                                                                                                                            path51 =
                                                                                                                                                                                                                                true;
                                                                                                                                                                                                                            if
                                                                                                                                                                                                                                (!
                                                                                                                                                                                                                                     Body
                                                                                                                                                                                                                                         .IsWithinRadius(
                                                                                                                                                                                                                                             spawn,
                                                                                                                                                                                                                                             30) &&
                                                                                                                                                                                                                                 path1 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path2 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path3 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path4 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path5 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path6 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path7 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path8 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path9 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path10 ==
                                                                                                                                                                                                                                 true
                                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                                 path11 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path12 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path13 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path14 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path15 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path16 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path17 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path18 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path19 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path20 ==
                                                                                                                                                                                                                                 true
                                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                                 path21 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path22 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path23 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path24 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path25 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path26 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path27 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path28 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path29 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path30 ==
                                                                                                                                                                                                                                 true
                                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                                 path31 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path32 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path33 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path34 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path35 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path36 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path37 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path38 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path39 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path40 ==
                                                                                                                                                                                                                                 true
                                                                                                                                                                                                                                 &&
                                                                                                                                                                                                                                 path41 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path42 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path43 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path44 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path45 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path46 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path47 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path48 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path49 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path50 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 path51 ==
                                                                                                                                                                                                                                 true &&
                                                                                                                                                                                                                                 walkback ==
                                                                                                                                                                                                                                 false)
                                                                                                                                                                                                                            {
                                                                                                                                                                                                                                Body
                                                                                                                                                                                                                                    .WalkTo(
                                                                                                                                                                                                                                        spawn,
                                                                                                                                                                                                                                        100);
                                                                                                                                                                                                                            }
                                                                                                                                                                                                                            else
                                                                                                                                                                                                                            {
                                                                                                                                                                                                                                walkback =
                                                                                                                                                                                                                                    true;
                                                                                                                                                                                                                                path1 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path11 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path21 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path31 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path41 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path51 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path2 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path12 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path22 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path32 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path42 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path3 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path13 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path23 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path33 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path43 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path4 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path14 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path24 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path34 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path44 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path5 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path15 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path25 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path35 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path45 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path6 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path16 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path26 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path36 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path46 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path7 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path17 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path27 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path37 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path47 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path8 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path18 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path28 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path38 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path48 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path9 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path19 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path29 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path39 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path49 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path10 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path20 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path30 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path40 =
                                                                                                                                                                                                                                    false;
                                                                                                                                                                                                                                path50 =
                                                                                                                                                                                                                                    false;
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
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                #endregion
            }
        }

        #region Set Baf Mob stats

        public void SetMobstats()
        {
            if (Body.TargetObject != null && (Body.InCombat || HasAggro || Body.AttackState == true)) //if in combat
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "HostBaf" && npc.NPCTemplate != null)
                        {
                            if (BafMobs == true && npc.TargetObject == Body.TargetObject)
                            {
                                npc.MaxDistance = 10000; //set mob distance to make it reach target
                                npc.TetherRange = 10000; //set tether to not return to home
                                if (!npc.IsWithinRadius(Body.TargetObject, 100))
                                {
                                    npc.MaxSpeedBase = 300; //speed is is not near to reach target faster
                                }
                                else
                                    npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed; //return speed to normal
                            }
                        }
                    }
                }
            }
            else //if not in combat
            {
                foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                {
                    if (npc != null)
                    {
                        if (npc.IsAlive && npc.PackageID == "HostBaf" && npc.NPCTemplate != null)
                        {
                            if (BafMobs == false)
                            {
                                npc.MaxDistance = npc.NPCTemplate.MaxDistance; //return distance to normal
                                npc.TetherRange = npc.NPCTemplate.TetherRange; //return tether to normal
                                npc.MaxSpeedBase = npc.NPCTemplate.MaxSpeed; //return speed to normal
                            }
                        }
                    }
                }
            }
        }

        #endregion

        public override void Think()
        {
            if (!HasAggressionTable())
            {
                Body.Health = Body.MaxHealth;
                BafMobs = false;
            }
            if (Body.IsMoving)
            {
                foreach (GamePlayer player in Body.GetPlayersInRadius((ushort) AggroRange))
                {
                    if (player != null)
                    {
                        if (player.IsAlive && player.Client.Account.PrivLevel == 1)
                        {
                            AddToAggroList(player, 10);
                        }
                    }
                    if (player == null || !player.IsAlive || player.Client.Account.PrivLevel != 1)
                    {
                        if (AggroTable.Count > 0)
                        {
                            ClearAggroList();//clear list if it contain any aggroed players
                        }
                    }
                }
            }

            if (Body.InCombat && HasAggro)
            {
                if (BafMobs == false)
                {
                    foreach (GameNPC npc in WorldMgr.GetNPCsFromRegion(Body.CurrentRegionID))
                    {
                        if (npc != null)
                        {
                            if (npc.IsAlive && (npc?.Brain is HostBrain || npc?.PackageID == "HostBaf"))
                            {
                                AddAggroListTo(npc?.Brain as HostBrain);
                                BafMobs = true;
                            }
                        }
                    }
                }
            }
            else
            {
                HostPath();
            }
            base.Think();
        }
    }
}