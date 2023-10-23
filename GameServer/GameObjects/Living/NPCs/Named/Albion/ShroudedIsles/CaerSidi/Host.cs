using System;
using System.Collections.Generic;
using System.Timers;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.World;

//Mob with packageid="HostBaf" in same region as bosss will come if he is pulled/aggroed
//Make sure to add that packageid to Host rooms, unless it will not bring a friends!
//DO NOT REMOVE Host Initializator from ingame or encounter will  not work!

namespace Core.GS;

#region Host Initializer
public class HostInitializer : GameNpc
{
    public HostInitializer() : base()
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
        for (Host.HostCount = 0; Host.HostCount < 8; Host.HostCount++)
        {
            Host Add = new Host();
            Add.X = this.X;
            Add.Y = this.Y;
            Add.Z = this.Z;
            Add.CurrentRegion = this.CurrentRegion;
            Add.Heading = this.Heading;
            Add.AddToWorld();
            Add.OrbsReward = 10;
            Add.PackageID = "HostCopy" + Host.HostCount;
        }
    }

    #endregion

    #region Pick real Host

    List<GameNpc> ChooseHost = new List<GameNpc>();
    public static bool set_realhost = false;
    public static bool pickhostcheck = false;

    public void PickHost()
    {
        foreach (GameNpc host in GetNPCsInRadius(8000))
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
                GameNpc RealHost = ChooseHost[Util.Random(0, ChooseHost.Count - 1)];
                RealHost.PackageID = "HostReal";
                RealHost.OrbsReward = ServerProperty.EPICBOSS_ORBS;
                set_realhost = true;
            }
        }
    }

    #endregion

    #region Respawn Timer and method

    public int RespawnTime(EcsGameTimer timer)
    {
        Spawnhost = false;
        return 0;
    }

    public void RespawnChecker()
    {
        int time = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000miliseconds        
        new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(RespawnTime), time);
    }

    #endregion

    #region AddToWorld + ScriptLoaded

    public override bool AddToWorld()
    {
        StartTimer();
        HostInitializerBrain hi = new HostInitializerBrain();
        SetOwnBrain(hi);
        base.AddToWorld();
        return true;
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Host Initializator", 60, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Host Initializator not found, creating it...");

            log.Warn("Initializing Host Initializator...");
            HostInitializer CO = new HostInitializer();
            CO.Name = "Host Initializator";
            CO.GuildName = "DO NOT REMOVE!";
            CO.RespawnInterval = 5000;
            CO.Model = 665;
            CO.Realm = 0;
            CO.Level = 50;
            CO.Size = 50;
            CO.CurrentRegionID = 60; //caer sidi
            CO.Flags ^= ENpcFlags.CANTTARGET;
            CO.Flags ^= ENpcFlags.FLYING;
            CO.Flags ^= ENpcFlags.DONTSHOWNAME;
            CO.Flags ^= ENpcFlags.PEACE;
            CO.Faction = FactionMgr.GetFactionByID(64);
            CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
            CO.X = 26995;
            CO.Y = 29733;
            CO.Z = 17871;
            HostInitializerBrain ubrain = new HostInitializerBrain();
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
#endregion Host Initializer

#region Host
public class Host : GameEpicBoss
{
    public Host() : base()
    {
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 150  * ServerProperty.EPICS_DMG_MULTIPLIER;
    }

    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 40;// dmg reduction for melee dmg
            case EDamageType.Crush: return 40;// dmg reduction for melee dmg
            case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
            default: return 50;// dmg reduction for rest resists
        }
    }
    public override int MaxHealth
    {
        get { return 30000; }
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 250;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }

    public override void Die(GameObject killer)
    {
        if (PackageID == "HostReal")
        {
            foreach (GameNpc boss in WorldMgr.GetNPCsByNameFromRegion("Host", this.CurrentRegionID, 0))
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

    public override void ReturnToSpawnPoint(short speed)
    {
        return;
    }
    public static int HostCount = 0;

    public override bool AddToWorld()
    {
        Model = 26;
        MeleeDamageType = EDamageType.Crush;
        Name = "Host";
        PackageID = "HostCopy";
        RespawnInterval = -1;
        MaxDistance = 0;
        TetherRange = 0;
        Size = 60;
        Level = 79;
        MaxSpeedBase = 300;
        Flags = ENpcFlags.GHOST;

        Faction = FactionMgr.GetFactionByID(64);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
        BodyType = 6;
        Realm = ERealm.None;

        Strength = 5;
        Dexterity = 200;
        Constitution = 100;
        Quickness = 125;
        Piety = 220;
        Intelligence = 220;
        Empathy = 200;

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
#endregion Host