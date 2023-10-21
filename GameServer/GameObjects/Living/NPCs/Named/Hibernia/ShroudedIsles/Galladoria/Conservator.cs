using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;

namespace DOL.GS;

public class Conservator : GameEpicBoss
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public Conservator()
        : base()
    {
    }
    
    public virtual int COifficulty
    {
        get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
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
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            Point3D spawn = new Point3D(SpawnPoint.X, SpawnPoint.Y, SpawnPoint.Z);
            if (!source.IsWithinRadius(spawn,800))//dont take any dmg 
            {
                if (damageType == EDamageType.Body || damageType == EDamageType.Cold || damageType == EDamageType.Energy || damageType == EDamageType.Heat
                    || damageType == EDamageType.Matter || damageType == EDamageType.Spirit || damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                    || damageType == EDamageType.Slash)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System, EChatLoc.CL_ChatWindow);
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
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }    
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159351);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Charisma = npcTemplate.Charisma;
        Empathy = npcTemplate.Empathy;
        ConservatorBrain.spampoison = false;
        ConservatorBrain.spamaoe = false;
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

        ConservatorBrain sBrain = new ConservatorBrain();
        SetOwnBrain(sBrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
    public override int MaxHealth
    {
        get
        {
            return 100000;
        }
    }
    public override int AttackRange
    {
        get
        {
            return 450;
        }
        set
        {
        }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == DOL.GS.Abilities.CCImmunity)
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
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;
        npcs = WorldMgr.GetNPCsByNameFromRegion("Conservator", 191, (ERealm)0);
        if (npcs.Length == 0)
        {
            log.Warn("Conservator not found, creating it...");

            log.Warn("Initializing Conservator...");
            Conservator CO = new Conservator();
            CO.Name = "Conservator";
            CO.Model = 817;
            CO.Realm = 0;
            CO.Level = 77;
            CO.Size = 250;
            CO.CurrentRegionID = 191;//galladoria

            CO.Strength = 500;
            CO.Intelligence = 220;
            CO.Piety = 220;
            CO.Dexterity = 200;
            CO.Constitution = 200;
            CO.Quickness = 125;
            CO.BodyType = 5;
            CO.MeleeDamageType = EDamageType.Slash;
            CO.Faction = FactionMgr.GetFactionByID(96);
            CO.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

            CO.X = 31297;
            CO.Y = 41040;
            CO.Z = 13473;
            CO.MaxDistance = 2000;
            CO.TetherRange = 2500;
            CO.MaxSpeedBase = 300;
            CO.Heading = 409;

            ConservatorBrain ubrain = new ConservatorBrain();
            CO.SetOwnBrain(ubrain);
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159351);
            CO.LoadTemplate(npcTemplate);
            CO.AddToWorld();
            CO.Brain.Start();
            CO.SaveIntoDatabase();
        }
        else
            log.Warn("Conservator exist ingame, remove it and restart server if you want to add by script code.");
    }
}