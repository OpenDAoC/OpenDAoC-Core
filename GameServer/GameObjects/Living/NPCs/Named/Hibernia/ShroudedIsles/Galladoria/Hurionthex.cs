using System;
using Core.AI.Brain;
using Core.Database;
using Core.Events;

// Boss Mechanics
// Changes form every ~20 seconds
// Preceded by "Hurionthex casts a spell!"
// Has 3 different forms he randomly switches between. He remains in a form for 20 seconds.
// He then returns to his base form for 20 seconds, accompanied by system message:
// "Hurionthex returns to his natural form."
// Each state change is random, so he may change to the same form repeatedly.
// Form change accompanied by message, "A ring of magical energy emanates from Hurionthex."
// Spell animation same as ice wizard PBAOE.

namespace Core.GS;

public class Hurionthex : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public Hurionthex()
        : base()
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

    public override int MaxHealth
    {
        get { return 100000; }
    }

    public override int AttackRange
    {
        get { return 450; }
        set { }
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

    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }

    public override bool AddToWorld() //To make sure after it respawn these checks are correctly set
    {
        HurionthexBrain.IsBaseForm = false;
        HurionthexBrain.IsSaiyanForm = false;
        HurionthexBrain.IsTreantForm = false;
        HurionthexBrain.IsGranidonForm = false;

        HurionthexBrain.BaseFormCheck = false;
        HurionthexBrain.GranidonFormCheck = false;
        HurionthexBrain.TreantFormCheck = false;
        HurionthexBrain.SaiyanFormCheck = false;
        HurionthexBrain.SwitchForm = false;
        HurionthexBrain.reset_checks = false;

        HurionthexBrain.StartForms = false;
        HurionthexBrain.cast_DA = false;
        HurionthexBrain.cast_disease = false;
        HurionthexBrain.cast_DS = false;

        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162285);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Charisma = npcTemplate.Charisma;
        Empathy = npcTemplate.Empathy;
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

        HurionthexBrain sbrain = new HurionthexBrain();
        SetOwnBrain(sbrain);
        SaveIntoDatabase();
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;

        npcs = WorldMgr.GetNPCsByNameFromRegion("Hurionthex", 191, (ERealm) 0);
        if (npcs.Length == 0)
        {
            log.Warn("Hurionthex not found, creating it...");

            log.Warn("Initializing Hurionthex...");
            Hurionthex Hurion = new Hurionthex();
            Hurion.Name = "Hurionthex";
            Hurion.Model = 889;

            Hurion.Realm = 0;
            Hurion.Level = 81;
            Hurion.Size = 170;
            Hurion.CurrentRegionID = 191; // Galladoria
            Hurion.Strength = 550;
            Hurion.Intelligence = 220;
            Hurion.Piety = 220;
            Hurion.Dexterity = 200;
            Hurion.Constitution = 200;
            Hurion.Quickness = 125;
            Hurion.Empathy = 280;
            Hurion.BodyType = 5; // Giant
            Hurion.MeleeDamageType = EDamageType.Crush;
            Hurion.RoamingRange = 0;
            Hurion.Faction = FactionMgr.GetFactionByID(96);


            Hurion.X = 55672;
            Hurion.Y = 43536;
            Hurion.Z = 12417;
            Hurion.MaxDistance = 2000;
            Hurion.MaxSpeedBase = 300;
            Hurion.Heading = 1035;

            HurionthexBrain ubrain = new HurionthexBrain();
            ubrain.AggroLevel = 100;
            ubrain.AggroRange = 500;
            Hurion.SetOwnBrain(ubrain);
            //INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162285);
            //Hurion.LoadTemplate(npcTemplate);
            Hurion.AddToWorld();
            Hurion.Brain.Start();
            Hurion.SaveIntoDatabase();
        }
        else
            log.Warn(
                "Hurionthex already exists in-game! Remove it and restart the server if you want to add any scripts.");
    }
}