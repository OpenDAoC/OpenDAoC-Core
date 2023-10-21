using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;

namespace Core.GS;

public class IcelordSkuf : GameEpicBoss
{
    public IcelordSkuf() : base()
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
    public override void Die(GameObject killer) //on kill generate orbs
    {
        base.Die(killer);
    }
    public static bool Spawn_Snakes = false;
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162349);
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
        RespawnInterval =ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        BodyType = (ushort)EBodyType.Giant;

        IcelordSkufBrain sbrain = new IcelordSkufBrain();
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
        npcs = WorldMgr.GetNPCsByNameFromRegion("Icelord Skuf", 160, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Icelord Skuf not found, creating it...");

            log.Warn("Initializing Icelord Skuf ...");
            IcelordSkuf TG = new IcelordSkuf();
            TG.Name = "Icelord Skuf";
            TG.Model = 918;
            TG.Realm = 0;
            TG.Level = 80;
            TG.Size = 70;
            TG.CurrentRegionID = 160; //tuscaran glacier
            TG.MeleeDamageType = EDamageType.Crush;
            TG.RespawnInterval =
                ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL *
                60000; //1min is 60000 miliseconds
            TG.Faction = FactionMgr.GetFactionByID(140);
            TG.Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
            TG.BodyType = (ushort)EBodyType.Giant;

            TG.X = 25405;
            TG.Y = 57241;
            TG.Z = 11359;
            TG.Heading = 1939;
            IcelordSkufBrain ubrain = new IcelordSkufBrain();
            TG.SetOwnBrain(ubrain);
            TG.AddToWorld();
            TG.SaveIntoDatabase();
            TG.Brain.Start();
        }
        else
            log.Warn("Icelord Skuf exist ingame, remove it and restart server if you want to add by script code.");
    }
}