using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;

namespace Core.GS;

#region Jailer Vifil
public class JailerVifil : GameEpicBoss
{
    public JailerVifil() : base()
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
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
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
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162583);
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
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

        JailerVifilBrain sbrain = new JailerVifilBrain();
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
        npcs = WorldMgr.GetNPCsByNameFromRegion("Jailer Vifil", 160, (ERealm) 0);
        if (npcs.Length == 0)
        {
            log.Warn("Jailer Vifil not found, creating it...");

            log.Warn("Initializing Jailer Vifil...");
            JailerVifil TG = new JailerVifil();
            TG.Name = "Jailer Vifil";
            TG.Model = 918;
            TG.Realm = 0;
            TG.Level = 82;
            TG.Size = 70;
            TG.CurrentRegionID = 160; //tuscaran glacier
            TG.MeleeDamageType = EDamageType.Crush;
            TG.RespawnInterval =
                ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL *
                60000; //1min is 60000 miliseconds
            TG.Faction = FactionMgr.GetFactionByID(140);
            TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

            TG.X = 27086;
            TG.Y = 61284;
            TG.Z = 10349;
            TG.Heading = 990;
            JailerVifilBrain ubrain = new JailerVifilBrain();
            TG.SetOwnBrain(ubrain);
            TG.AddToWorld();
            TG.SaveIntoDatabase();
            TG.Brain.Start();
        }
        else
            log.Warn("Jailer Vifil exist ingame, remove it and restart server if you want to add by script code.");
    }
}
#endregion Jailer Vifil

#region Jailer adds
public class JailerAdd : GameNpc
{
    public JailerAdd() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 45; // dmg reduction for melee dmg
            case EDamageType.Crush: return 45; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 45; // dmg reduction for melee dmg
            default: return 30; // dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }

    public override int AttackRange
    {
        get { return 350; }
        set { }
    }

    public override double GetArmorAF(EArmorSlot slot)
    {
        return 200;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.15;
    }

    public override int MaxHealth
    {
        get { return 20000; }
    }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 250; }
    public override bool AddToWorld()
    {
        Model = 918;
        MeleeDamageType = EDamageType.Crush;
        Name = "hrimathursa tormentor";
        RespawnInterval = -1;

        MaxDistance = 5500;
        TetherRange = 5800;
        Size = 50;
        Level = 78;
        MaxSpeedBase = 270;

        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        BodyType = 1;
        Realm = ERealm.None;

        JailerAddBrain adds = new JailerAddBrain();
        SetOwnBrain(adds);
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }
}
#endregion Jailer adds