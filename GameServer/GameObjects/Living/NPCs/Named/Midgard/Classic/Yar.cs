using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;

namespace Core.GS;

public class Yar : GameEpicBoss
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public Yar()
        : base()
    {
    }
    public virtual int YarDifficulty
    {
        get { return ServerProperties.Properties.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS / 100; }
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
        return 350;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override int MaxHealth
    {
        get { return 40000; }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100;
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
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }      
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60168093);
        LoadTemplate(npcTemplate);
        
        Strength = npcTemplate.Strength;
        Constitution = npcTemplate.Constitution;
        Dexterity = npcTemplate.Dexterity;
        Quickness = npcTemplate.Quickness;
        Empathy = npcTemplate.Empathy;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        
        Faction = FactionMgr.GetFactionByID(154);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(154));
        RespawnInterval = ServerProperties.Properties.SET_EPIC_GAME_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

        YarBrain sBrain = new YarBrain();
        SetOwnBrain(sBrain);
        base.AddToWorld();
        return true;
    }
    public override void Die(GameObject killer)
    {
        foreach (GameNpc npc in GetNPCsInRadius(5000))
        {
            if (npc.Brain is YarAddBrain)
            {
                npc.RemoveFromWorld();
            }
        }
        base.Die(killer);
    }
}

#region Yar adds
public class YarAdd : GameNpc
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public YarAdd()
        : base()
    {
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100;
    }

    public override int MaxHealth
    {
        get
        {
            return 7000;
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
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.25;
    }
    public override bool AddToWorld()
    {
        Yar yar = new Yar();
        Model = 625;
        Name = "drakulv executioner";
        Size = 60;
        Level = 63;
        Realm = 0;
        CurrentRegionID = yar.CurrentRegionID;

        Strength = 180;
        Intelligence = 150;
        Piety = 150;
        Dexterity = 200;
        Constitution = 200;
        Quickness = 125;
        RespawnInterval = -1;

        Gender = EGender.Neutral;
        MeleeDamageType = EDamageType.Slash;

        Faction = FactionMgr.GetFactionByID(154);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(154));
        
        BodyType = 5;
        YarAddBrain sBrain = new YarAddBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        base.AddToWorld();
        return true;
    }
}
public class YarAdd2 : GameNpc
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public YarAdd2()
        : base()
    {
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100;
    }

    public override int MaxHealth
    {
        get
        {
            return 7000;
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
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.25;
    }
    public override bool AddToWorld()
    {
        Yar yar = new Yar();
        Model = 624;
        Name = "drakulv soultrapper";
        Size = 58;
        Level = 62;
        Realm = 0;
        CurrentRegionID = yar.CurrentRegionID;

        Strength = 180;
        Intelligence = 150;
        Piety = 150;
        Dexterity = 200;
        Constitution = 200;
        Quickness = 125;
        RespawnInterval = -1;

        Gender = EGender.Neutral;
        MeleeDamageType = EDamageType.Slash;
        
        Faction = FactionMgr.GetFactionByID(154);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(154));
        
        BodyType = 5;
        YarAddBrain sBrain = new YarAddBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        base.AddToWorld();
        return true;
    }
}
public class YarAdd3 : GameNpc
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public YarAdd3()
        : base()
    {
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100;
    }

    public override int MaxHealth
    {
        get
        {
            return 7000;
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
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 300;
    }

    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.25;
    }
    public override bool AddToWorld()
    {
        Yar yar = new Yar();
        Model = 625;
        Name = "drakulv disciple";
        Size = 58;
        Level = 62;
        Realm = 0;
        CurrentRegionID = yar.CurrentRegionID;

        Strength = 180;
        Intelligence = 150;
        Piety = 150;
        Dexterity = 200;
        Constitution = 200;
        Quickness = 125;
        RespawnInterval = -1;
        Faction = FactionMgr.GetFactionByID(154);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(154));
        
        Gender = EGender.Neutral;
        MeleeDamageType = EDamageType.Slash;

        BodyType = 5;
        YarAddBrain sBrain = new YarAddBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        base.AddToWorld();
        return true;
    }
}
#endregion Yar adds