using System;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;
using Core.GS.World;

namespace Core.GS;

#region Spindler Broodmother
public class SpindlerBroodmother : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SpindlerBroodmother()
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

    public override void Die(GameObject killer)
    {
        SpawnAfterDead();
        base.Die(killer);
    }

    public override bool AddToWorld()
    {
        foreach (GameNpc npc in GetNPCsInRadius(4000))
        {
            if (npc.RespawnInterval == -1 && npc.Brain is SBDeadAddsBrain)
            {
                npc.RemoveFromWorld();
            }
        }

        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166449);
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
        SpindlerBroodmotherBrain sBrain = new SpindlerBroodmotherBrain();
        SetOwnBrain(sBrain);
        SaveIntoDatabase();
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }

    public void SpawnAfterDead()
    {
        for (int i = 0; i < Util.Random(20, 25); i++) // Spawn 20-25 adds
        {
            SBDeadAdds Add = new SBDeadAdds();
            Add.X = X + Util.Random(-50, 80);
            Add.Y = Y + Util.Random(-50, 80);
            Add.Z = Z;
            Add.CurrentRegion = CurrentRegion;
            Add.Heading = Heading;
            Add.AddToWorld();
        }
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        GameNpc[] npcs;

        npcs = WorldMgr.GetNPCsByNameFromRegion("Spindler Broodmother", 191, (ERealm) 0);
        if (npcs.Length == 0)
        {
            log.Warn("Spindler Broodmother not found, creating it...");

            log.Warn("Initializing Spindler Broodmother...");
            SpindlerBroodmother SB = new SpindlerBroodmother();
            SB.Name = "Spindler Broodmother";
            SB.Model = 904;
            SB.Realm = 0;
            SB.Level = 81;
            SB.Size = 125;
            SB.CurrentRegionID = 191; //galladoria

            SB.Strength = 500;
            SB.Intelligence = 220;
            SB.Piety = 220;
            SB.Dexterity = 200;
            SB.Constitution = 200;
            SB.Quickness = 125;
            SB.BodyType = 5;
            SB.MeleeDamageType = EDamageType.Slash;
            SB.Faction = FactionMgr.GetFactionByID(96);
            SB.Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));

            SB.X = 21283;
            SB.Y = 51707;
            SB.Z = 10876;
            SB.MaxDistance = 2000;
            SB.TetherRange = 2500;
            SB.MaxSpeedBase = 300;
            SB.Heading = 0;

            SpindlerBroodmotherBrain ubrain = new SpindlerBroodmotherBrain();
            ubrain.AggroLevel = 100;
            ubrain.AggroRange = 500;
            SB.SetOwnBrain(ubrain);
            INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60166449);
            SB.LoadTemplate(npcTemplate);
            SB.AddToWorld();
            SB.Brain.Start();
            SB.SaveIntoDatabase();
        }
        else
            log.Warn(
                "Spindler Broodmother exist ingame, remove it and restart server if you want to add by script code.");
    }
}
#endregion Spindler Broodmother

#region Spindler Broodmother adds
public class SBAdds : GameNpc
{
    public SBAdds() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 30; // dmg reduction for melee dmg
            case EDamageType.Crush: return 30; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 30; // dmg reduction for melee dmg
            default: return 30; // dmg reduction for rest resists
        }
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 200;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.10;
    }
    public override int MaxHealth
    {
        get { return 8000; }
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100;
    }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 80; }
    public override short Strength { get => base.Strength; set => base.Strength = 250; }
    public override bool AddToWorld()
    {
        Model = 904;
        Name = "Newly-born spindler";
        MeleeDamageType = EDamageType.Slash;
        RespawnInterval = -1;
        Size = (byte) Util.Random(50, 60);
        Level = (byte) Util.Random(56, 59);
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        Realm = 0;
        SBAddsBrain adds = new SBAddsBrain();
        LoadedFromScript = true;
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }

    public override void DropLoot(GameObject killer) //no loot
    {
    }
    public override void Die(GameObject killer)
    {
        base.Die(killer); //null to not gain experience
    }
}
#endregion Spindler Broodmother adds

#region Spindler Broodmother post-death adds
public class SBDeadAdds : GameNpc
{
    public SBDeadAdds() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20; // dmg reduction for melee dmg
            case EDamageType.Crush: return 20; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 20; // dmg reduction for melee dmg
            default: return 30; // dmg reduction for rest resists
        }
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.15;
    }
    public override int MaxHealth
    {
        get { return 800; }
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100;
    }
    public override bool AddToWorld()
    {
        Model = 904;
        Name = "underdeveloped spindler";
        MeleeDamageType = EDamageType.Slash;
        RespawnInterval = -1;
        Strength = 100;
        IsWorthReward = false; //worth no reward
        Size = (byte) Util.Random(30, 40);
        Level = 50;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        Realm = 0;
        SBDeadAddsBrain adds = new SBDeadAddsBrain();
        LoadedFromScript = true;
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Spindler Broodmother post-death adds