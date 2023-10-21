using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using Core.GS;

namespace Core.GS;

#region Evern
public class Evern : GameEpicBoss
{
    public Evern() : base()
    {
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
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100;
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
        if (IsReturningToSpawnPoint && keyName == GS.Abilities.DamageImmunity)
            return true;
        return base.HasAbility(keyName);
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (IsOutOfTetherRange)
            {
                if (damageType == EDamageType.Body || damageType == EDamageType.Cold ||
                    damageType == EDamageType.Energy || damageType == EDamageType.Heat
                    || damageType == EDamageType.Matter || damageType == EDamageType.Spirit ||
                    damageType == EDamageType.Crush || damageType == EDamageType.Thrust
                    || damageType == EDamageType.Slash)
                {
                    GamePlayer truc;
                    if (source is GamePlayer)
                        truc = (source as GamePlayer);
                    else
                        truc = ((source as GameSummonedPet).Owner as GamePlayer);
                    if (truc != null)
                        truc.Out.SendMessage(this.Name + " is immune to any damage!", EChatType.CT_System,
                            EChatLoc.CL_ChatWindow);
                    base.TakeDamage(source, damageType, 0, 0);
                    return;
                }
            }
            else //take dmg
            {
                base.TakeDamage(source, damageType, damageAmount, criticalAmount);
            }
        }
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60160628);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        EvernBrain.spawnfairy = false;
        //Idle = false;
        MaxSpeedBase = 300;

        Faction = FactionMgr.GetFactionByID(81);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(81));
        EvernBrain sbrain = new EvernBrain();
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
        npcs = WorldMgr.GetNPCsByNameFromRegion("Evern", 200, (ERealm) 0);
        if (npcs.Length == 0)
        {
            log.Warn("Evern not found, creating it...");

            log.Warn("Initializing Evern...");
            Evern CO = new Evern();
            CO.Name = "Evern";
            CO.Model = 400;
            CO.Realm = 0;
            CO.Level = 75;
            CO.Size = 120;
            CO.CurrentRegionID = 200; //OF breifine

            CO.Strength = 5;
            CO.Intelligence = 150;
            CO.Piety = 150;
            CO.Dexterity = 200;
            CO.Constitution = 100;
            CO.Quickness = 125;
            CO.Empathy = 300;
            CO.BodyType = (ushort) EBodyType.Magical;
            CO.MeleeDamageType = EDamageType.Slash;

            CO.X = 429840;
            CO.Y = 380396;
            CO.Z = 2328;
            CO.MaxDistance = 3500;
            CO.TetherRange = 3800;
            CO.MaxSpeedBase = 250;
            CO.Heading = 4059;

            EvernBrain ubrain = new EvernBrain();
            ubrain.AggroLevel = 100;
            ubrain.AggroRange = 600;
            CO.SetOwnBrain(ubrain);
            CO.AddToWorld();
            CO.Brain.Start();
            CO.SaveIntoDatabase();
        }
        else
            log.Warn("Evern exist ingame, remove it and restart server if you want to add by script code.");
    }
    public override void Die(GameObject killer)
    {
        foreach (GameNpc npc in GetNPCsInRadius(8000))
        {
            if (npc == null) break;
            if (npc.Brain is EvernFairyBrain)
            {
                if (npc.RespawnInterval == -1)
                    npc.Die(npc); //we kill all fairys if boss die
            }
        }
        base.Die(killer);
    }
}
#endregion Evern

#region Evern Fairies
public class EvernFairy : GameNpc
{
    public EvernFairy() : base()
    {
    }
    public override long ExperienceValue => 0;
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100;
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 400;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.35;
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 25; // dmg reduction for melee dmg
            case EDamageType.Crush: return 25; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 25; // dmg reduction for melee dmg
            default: return 35; // dmg reduction for rest resists
        }
    }
    public override int MaxHealth
    {
        get { return 2000; }
    }
    public override void DropLoot(GameObject killer)
    {
    }
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 120; }
    public override bool AddToWorld()
    {
        Model = 603;
        Name = "Wraith Fairy";
        MeleeDamageType = EDamageType.Thrust;
        RespawnInterval = -1;
        Size = 50;
        Flags = ENpcFlags.FLYING;
        Faction = FactionMgr.GetFactionByID(81);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(81));
        Level = (byte) Util.Random(50, 55);
        Gender = EGender.Female;
        EvernFairyBrain adds = new EvernFairyBrain();
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Evern Fairies