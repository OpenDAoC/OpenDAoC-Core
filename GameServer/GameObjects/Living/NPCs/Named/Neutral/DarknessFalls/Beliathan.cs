using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS;

#region Beliathan Inizializator
public class BeliathanInit : GameNpc
{
    public BeliathanInit() : base()
    {
    }

    public override bool AddToWorld()
    {
        BeliathanInitBrain hi = new BeliathanInitBrain();
        SetOwnBrain(hi);
        base.AddToWorld();
        return true;
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Beliathan Initializator", 249, (ERealm) 0);
        if (npcs.Length == 0)
        {
            log.Warn("Beliathan Initializator not found, creating it...");

            log.Warn("Initializing Beliathan Initializator...");
            BeliathanInit CO = new BeliathanInit();
            CO.Name = "Beliathan Initializator";
            CO.GuildName = "DO NOT REMOVE!";
            CO.RespawnInterval = 5000;
            CO.Model = 665;
            CO.Realm = 0;
            CO.Level = 50;
            CO.Size = 50;
            CO.CurrentRegionID = 249;
            CO.Flags ^= ENpcFlags.CANTTARGET;
            CO.Flags ^= ENpcFlags.FLYING;
            CO.Flags ^= ENpcFlags.DONTSHOWNAME;
            CO.Flags ^= ENpcFlags.PEACE;
            CO.Faction = FactionMgr.GetFactionByID(191);
            CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));
            CO.X = 22699;
            CO.Y = 18684;
            CO.Z = 15174;
            BeliathanInitBrain ubrain = new BeliathanInitBrain();
            CO.SetOwnBrain(ubrain);
            CO.AddToWorld();
            CO.SaveIntoDatabase();
            CO.Brain.Start();
        }
        else
            log.Warn(
                "Beliathan Initializator exists in game, remove it and restart server if you want to add by script code.");
    }
}
#endregion

#region Beliathan
public class Beliathan : GameEpicBoss
{
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameEventMgr.AddHandler(GameLivingEvent.Dying, new CoreEventHandler(PlayerKilledByBeliathan));
        if (log.IsInfoEnabled)
            log.Info("Beliathan initialized..");
    }

    [ScriptUnloadedEvent]
    public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
    {
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override int AttackSpeed(DbInventoryItem mainWeapon, DbInventoryItem leftWeapon = null)
    {
        return base.AttackSpeed(mainWeapon, leftWeapon) * 2;
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 40; // dmg reduction for melee dmg
            case EDamageType.Crush: return 40; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 40; // dmg reduction for melee dmg
            default: return 70; // dmg reduction for rest resists
        }
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 350;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override int MaxHealth
    {
        get { return 100000; }
    }
    public override short MaxSpeedBase => (short) (191 + Level * 2);
    public override int AttackRange
    {
        get => 180;
        set { }
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158351);
        LoadTemplate(npcTemplate);
        MaxDistance = 1500;
        TetherRange = 2000;
        RoamingRange = 400;
        Strength = npcTemplate.Strength;
        Constitution = npcTemplate.Constitution;
        Dexterity = npcTemplate.Dexterity;
        Quickness = npcTemplate.Quickness;
        Empathy = npcTemplate.Empathy;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        RespawnInterval = -1;
        BeliathanBrain sBrain = new BeliathanBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;

        // demon
        BodyType = 2;

        Faction = FactionMgr.GetFactionByID(191);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

        base.AddToWorld();
        return true;
    }

    public override void Die(GameObject killer)
    {
        base.Die(killer);

        foreach (GameNpc npc in GetNPCsInRadius(4000))
        {
            if (npc.Brain is BeliathanMinionBrain)
            {
                npc.RemoveFromWorld();
            }
        }
    }
    private static void PlayerKilledByBeliathan(CoreEvent e, object sender, EventArgs args)
    {
        GamePlayer player = sender as GamePlayer;

        if (player == null)
            return;

        DyingEventArgs eArgs = args as DyingEventArgs;

        if (eArgs?.Killer?.Name != "Beliathan")
            return;

        GameNpc beliathan = eArgs.Killer as GameNpc;

        if (beliathan == null)
            return;

        BeliathanMinion sMinion = new BeliathanMinion();
        sMinion.X = player.X;
        sMinion.Y = player.Y;
        sMinion.Z = player.Z;
        sMinion.CurrentRegion = player.CurrentRegion;
        sMinion.Heading = player.Heading;
        sMinion.AddToWorld();
        sMinion.StartAttack(beliathan.TargetObject);
    }
}
#endregion Beliathan

#region Beliathan minion
public class BeliathanMinion : GameNpc
{
    public override int MaxHealth
    {
        get { return 550; }
    }

    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158351);
        LoadTemplate(npcTemplate);
        Level = 50;
        Strength = 300;
        Size = 50;
        Name += "'s Minion";
        RoamingRange = 350;
        RespawnInterval = -1;
        MaxDistance = 1500;
        TetherRange = 2000;
        IsWorthReward = false; // worth no reward
        Realm = ERealm.None;
        BeliathanMinionBrain adds = new BeliathanMinionBrain();
        LoadedFromScript = true;
        SetOwnBrain(adds);

        // demon
        BodyType = 2;

        Faction = FactionMgr.GetFactionByID(191);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

        base.AddToWorld();
        return true;
    }
    public override void DropLoot(GameObject killer) //no loot
    {
    }
    public override long ExperienceValue => 0;
    public override void Die(GameObject killer)
    {
        base.Die(null); // null to not gain experience
    }
}
#endregion Beliathan minion