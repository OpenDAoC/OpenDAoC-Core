using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS;

#region Icelord Hakr
public class IcelordHakr : GameEpicBoss
{
    public IcelordHakr() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 40;// dmg reduction for melee dmg
            case EDamageType.Crush: return 40;// dmg reduction for melee dmg
            case EDamageType.Thrust: return 40;// dmg reduction for melee dmg
            default: return 70;// dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperty.EPICS_DMG_MULTIPLIER;
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
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
    public override void Die(GameObject killer) //on kill generate orbs
    {
        Spawn_Snakes = false;
        IcelordHakrBrain.spam_message1 = false;
        base.Die(killer);
    }
    public static bool Spawn_Snakes = false;
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162347);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        Spawn_Snakes = false;
        IcelordHakrBrain.spam_message1 = false;
        if (Spawn_Snakes == false)
        {
            SpawnSnakes();
            Spawn_Snakes = true;
        }
        IcelordHakrBrain sbrain = new IcelordHakrBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Icelord Hakr", 160, (ERealm) 0);
        if (npcs.Length == 0)
        {
            log.Warn("Icelord Hakr not found, creating it...");

            log.Warn("Initializing Icelord Hakr ...");
            IcelordHakr TG = new IcelordHakr();
            TG.Name = "Icelord Hakr";
            TG.Model = 918;
            TG.Realm = 0;
            TG.Level = 82;
            TG.Size = 70;
            TG.CurrentRegionID = 160; //tuscaran glacier
            TG.MeleeDamageType = EDamageType.Crush;
            TG.RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
            TG.Faction = FactionMgr.GetFactionByID(140);
            TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

            TG.X = 25405;
            TG.Y = 57241;
            TG.Z = 11359;
            TG.Heading = 1939;
            IcelordHakrBrain ubrain = new IcelordHakrBrain();
            TG.SetOwnBrain(ubrain);
            TG.AddToWorld();
            TG.SaveIntoDatabase();
            TG.Brain.Start();
        }
        else
            log.Warn("Icelord Hakr exist ingame, remove it and restart server if you want to add by script code.");
    }
    public void SpawnSnakes()
    {
        for (int i = 0; i < 2; i++)
        {
            HakrAdd Add1 = new HakrAdd();
            Add1.X = X + Util.Random(-100, 100);
            Add1.Y = Y + Util.Random(-100, 100);
            Add1.Z = Z;
            Add1.CurrentRegion = CurrentRegion;
            Add1.Heading = Heading;
            Add1.PackageID = "HakrBaf";
            Add1.AddToWorld();
            ++HakrAdd.IceweaverCount;
        }
        for (int i = 0; i < 2; i++)
        {
            HakrAdd Add2 = new HakrAdd();
            Add2.X = 30008 + Util.Random(-100, 100);
            Add2.Y = 56329 + Util.Random(-100, 100);
            Add2.Z = 11894;
            Add2.CurrentRegion = CurrentRegion;
            Add2.Heading = Heading;
            Add2.AddToWorld();
            ++HakrAdd.IceweaverCount;
        }
    }
}
#endregion Icelord Hakr

#region Hakr snake adds
public class HakrAdd : GameNpc
{
    public HakrAdd() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 35; // dmg reduction for melee dmg
            case EDamageType.Crush: return 35; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 35; // dmg reduction for melee dmg
            default: return 35; // dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperty.EPICS_DMG_MULTIPLIER;
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override int MaxHealth
    {
        get { return 20000; }
    }
    public static int IceweaverCount = 0;
    public override void Die(GameObject killer)
    {
        --IceweaverCount;
        base.Die(killer);
    }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 250; }
    public override bool AddToWorld()
    {
        Model = 766;
        MeleeDamageType = EDamageType.Thrust;
        Name = "Royal Iceweaver";
        RespawnInterval = -1;

        MaxDistance = 3500;
        TetherRange = 3800;
        Size = 60;
        Level = 78;
        MaxSpeedBase = 270;

        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        BodyType = 1;
        Realm = ERealm.None;

        HakrAddBrain adds = new HakrAddBrain();
        SetOwnBrain(adds);
        LoadedFromScript = true;
        base.AddToWorld();
        return true;
    }
}
#endregion Hakr snake adds