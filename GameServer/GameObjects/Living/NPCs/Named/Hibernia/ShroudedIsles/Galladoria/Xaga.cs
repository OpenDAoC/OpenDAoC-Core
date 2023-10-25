using System;
using Core.Database.Tables;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS;

#region Xaga
public class Xaga : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public Xaga()
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
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (source is GamePlayer || source is GameSummonedPet)
        {
            if (this.IsOutOfTetherRange)//dont take any dmg if is too far away from spawn point
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
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperty.EPICS_DMG_MULTIPLIER;
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
    public void SpawnTineBeatha()
    {
        if (Tine.TineCount == 0)
        {
            Tine tine = new Tine();
            tine.X = 27211;
            tine.Y = 54902;
            tine.Z = 13213;
            tine.CurrentRegion = CurrentRegion;
            tine.Heading = 2157;
            tine.RespawnInterval = -1;
            tine.AddToWorld();
        }
        if (Beatha.BeathaCount == 0)
        {
            Beatha beatha = new Beatha();
            beatha.X = 27614;
            beatha.Y = 54866;
            beatha.Z = 13213;
            beatha.CurrentRegion = CurrentRegion;
            beatha.Heading = 2038;
            beatha.RespawnInterval = -1;
            beatha.AddToWorld();
        }
    }
    public static bool spawn_lights = false;
    public override void Die(GameObject killer)
    {
        foreach(GameNpc lights in WorldMgr.GetNPCsFromRegion(CurrentRegionID))
        {
            if(lights != null)
            {
                if(lights.IsAlive && (lights.Brain is TineBrain || lights.Brain is BeathaBrain))
                    lights.Die(lights);
            }
        }
        base.Die(killer);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168075);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Charisma = npcTemplate.Charisma;
        Empathy = npcTemplate.Empathy;

        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        XagaBrain sBrain = new XagaBrain();
        SetOwnBrain(sBrain);
        SaveIntoDatabase();
        LoadedFromScript = false;
        spawn_lights = false;
        bool success = base.AddToWorld();
        if (success)
        {
            if (spawn_lights == false)
            {
                SpawnTineBeatha();
                spawn_lights = true;
            }
        }
        return success;
    }
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;

        npcs = WorldMgr.GetNPCsByNameFromRegion("Xaga", 191, (ERealm) 0);
        if (npcs.Length == 0)
        {
            log.Warn("Xaga not found, creating it...");

            log.Warn("Initializing Xaga...");
            Xaga SB = new Xaga();
            SB.Name = "Xaga";
            SB.Model = 917;
            SB.Realm = 0;
            SB.Level = 81;
            SB.Size = 250;
            SB.CurrentRegionID = 191; //galladoria

            SB.Strength = 260;
            SB.Intelligence = 220;
            SB.Piety = 220;
            SB.Dexterity = 200;
            SB.Constitution = 200;
            SB.Quickness = 125;
            SB.BodyType = 5;
            SB.MeleeDamageType = EDamageType.Slash;
            SB.Faction = FactionMgr.GetFactionByID(96);
            SB.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

            SB.X = 27397;
            SB.Y = 54975;
            SB.Z = 12949;
            SB.MaxDistance = 2000;
            SB.TetherRange = 2500;
            SB.MaxSpeedBase = 300;
            SB.Heading = 2013;

            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168075);
            SB.LoadTemplate(npcTemplate);

            XagaBrain ubrain = new XagaBrain();
            ubrain.AggroLevel = 100;
            ubrain.AggroRange = 500;
            SB.SetOwnBrain(ubrain);

            SB.AddToWorld();
            SB.Brain.Start();
            SB.SaveIntoDatabase();
        }
        else
            log.Warn("Xaga exist ingame, remove it and restart server if you want to add by script code.");
    }
}
#endregion Xaga

#region Beatha
public class Beatha : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public Beatha()
        : base()
    {
    }
    public override void StartAttack(GameObject target)
    {
    }
    public override void ReturnToSpawnPoint(short speed)
    {
        return;
    }
    public override void DealDamage(AttackData ad)
    {
        if (ad != null)
        {
            foreach (GameNpc xaga in GetNPCsInRadius(8000))
            {
                if (xaga != null)
                {
                    if (xaga.IsAlive && xaga.Brain is XagaBrain)
                        xaga.Health += ad.Damage*2;//dmg heals xaga
                }
            }
        }
        base.DealDamage(ad);
    }
    public override int MaxHealth
    {
        get { return 50000; }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
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
    public static int BeathaCount = 0;
    public override void Die(GameObject killer)
    {
        --BeathaCount;
        base.Die(killer);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158330);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Charisma = npcTemplate.Charisma;
        Empathy = npcTemplate.Empathy;
        Flags = ENpcFlags.FLYING;
        BeathaBrain.path4 = false;
        BeathaBrain.path1 = false;
        BeathaBrain.path2 = false;
        BeathaBrain.path3 = false;

        AbilityBonus[(int)EProperty.Resist_Body] = 60;
        AbilityBonus[(int)EProperty.Resist_Heat] = -20;//weak to heat
        AbilityBonus[(int)EProperty.Resist_Cold] = 99;//resi to cold
        AbilityBonus[(int)EProperty.Resist_Matter] = 60;
        AbilityBonus[(int)EProperty.Resist_Energy] = 60;
        AbilityBonus[(int)EProperty.Resist_Spirit] = 60;
        AbilityBonus[(int)EProperty.Resist_Slash] = 40;
        AbilityBonus[(int)EProperty.Resist_Crush] = 40;
        AbilityBonus[(int)EProperty.Resist_Thrust] = 40;

        ++BeathaCount;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BeathaBrain sBrain = new BeathaBrain();
        SetOwnBrain(sBrain);
        base.AddToWorld();
        return true;
    }
}
#endregion Beatha

#region Tine
public class Tine : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public Tine()
        : base()
    {
    }
    public override void StartAttack(GameObject target)
    {
    }
    public override void ReturnToSpawnPoint(short speed)
    {
        return;
    }
    public override int MaxHealth
    {
        get { return 50000; }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
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
    public static int TineCount = 0;
    public override void Die(GameObject killer)
    {
        --TineCount;
        base.Die(killer);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60167084);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Charisma = npcTemplate.Charisma;
        Empathy = npcTemplate.Empathy;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        Flags = ENpcFlags.FLYING;
        TineBrain.path4_2 = false;
        TineBrain.path1_2 = false;
        TineBrain.path2_2 = false;
        TineBrain.path3_2 = false;

        AbilityBonus[(int)EProperty.Resist_Body] = 60;
        AbilityBonus[(int)EProperty.Resist_Heat] = 99;//resi to heat
        AbilityBonus[(int)EProperty.Resist_Cold] = -20;//weak to cold
        AbilityBonus[(int)EProperty.Resist_Matter] = 60;
        AbilityBonus[(int)EProperty.Resist_Energy] = 60;
        AbilityBonus[(int)EProperty.Resist_Spirit] = 60;
        AbilityBonus[(int)EProperty.Resist_Slash] = 40;
        AbilityBonus[(int)EProperty.Resist_Crush] = 40;
        AbilityBonus[(int)EProperty.Resist_Thrust] = 40;

        ++TineCount;
        TineBrain sBrain = new TineBrain();
        SetOwnBrain(sBrain);
        base.AddToWorld();
        return true;
    }
}
#endregion Tine