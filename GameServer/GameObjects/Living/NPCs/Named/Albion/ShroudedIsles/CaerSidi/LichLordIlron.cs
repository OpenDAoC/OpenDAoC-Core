using System;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;

namespace Core.GS;

#region Lich Lord Ilron
public class LichLordIlron : GameEpicBoss
{
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
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 350;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }

    public override short MaxSpeedBase => (short) (191 + Level * 2);

    public override int MaxHealth => 100000;
    
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * ServerProperty.EPICS_DMG_MULTIPLIER;
    }

    public override int AttackRange
    {
        get => 180;
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == "CCImmunity")
            return true;

        return base.HasAbility(keyName);
    }
    public override bool AddToWorld()
    {
        Level = 79;
        Gender = EGender.Neutral;
        BodyType = 11; // undead
        MaxDistance = 1500;
        TetherRange = 2000;
        RoamingRange = 400;
        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(64);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));

        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60163266);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;

        LichLordIlronBrain sBrain = new LichLordIlronBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;

        LichLordIlronBrain.spawnimages = true;
        LoadedFromScript = false;//load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }

    public override void Die(GameObject killer)
    {
        base.Die(killer);

        foreach (GameNpc npc in GetNPCsInRadius(4000))
        {
            if (npc.Brain is IlronImagesBrain)
                npc.RemoveFromWorld();
        }
    }

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("Lich Lord Ilron NPC Initializing...");
    }
}
#endregion Lich Lord Ilron

#region Ilron Images
public class IlronImages : GameNpc
{
    public override int MaxHealth
    {
        get { return 10000; }
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 25; // dmg reduction for melee dmg
            case EDamageType.Crush: return 25; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 25; // dmg reduction for melee dmg
            default: return 25; // dmg reduction for rest resists
        }
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
    public override bool AddToWorld()
    {
        Model = 441;
        Name = "Lich Lord Ilron";
        Size = 130;
        Level = 70;
        RoamingRange = 350;
        RespawnInterval = -1;
        MaxDistance = 1500;
        TetherRange = 2000;
        Faction = FactionMgr.GetFactionByID(64);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(64));
        IsWorthReward = false; // worth no reward
        Flags ^= ENpcFlags.GHOST;
        Realm = ERealm.None;
        IlronImagesBrain adds = new IlronImagesBrain();
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
        base.Die(null); // null to not gain experience
    }
}
#endregion Ilron Images