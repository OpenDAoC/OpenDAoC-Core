using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS;
using Core.GS.AI.Brains;

namespace Core.GS;

#region Giant Sporite Cluster
public class GiantSporiteCluster : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public GiantSporiteCluster()
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
        get { return 250; }
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
        return 300;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        int damageDealt = damageAmount + criticalAmount;
        foreach (GameNpc copy in GetNPCsInRadius(10000))
        {
            if (copy != null)
            {
                if (copy is SporiteClusterAdds && copy.IsAlive)
                {
                    copy.Health = Health;
                }
            }
        }
    }
    public override void Die(GameObject killer)
    {
        foreach (GameNpc copy in GetNPCsInRadius(10000))
        {
            if (copy != null)
            {
                if (copy.IsAlive && copy is SporiteClusterAdds)
                {
                    copy.RemoveFromWorld();
                }
            }
        }
        base.Die(killer);
    }
    public void Spawn()
    {
        foreach (GameNpc npc in GetNPCsInRadius(4000))
        {
            if (npc.Brain is SporiteClusterAddsBrain)
            {
                return;
            }
        }
        for (int i = 0; i < 7; i++)
        {
            SporiteClusterAdds Add = new SporiteClusterAdds();
            Add.X = X + Util.Random(-50, 80);
            Add.Y = Y + Util.Random(-50, 80);
            Add.Z = Z;
            Add.CurrentRegion = CurrentRegion;
            Add.Heading = Heading;
            Add.AddToWorld();
        }
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60161336);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Charisma = npcTemplate.Charisma;
        Empathy = npcTemplate.Empathy;
        Spawn();

        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        GiantSporiteClusterBrain sBrain = new GiantSporiteClusterBrain();
        SetOwnBrain(sBrain);
        base.AddToWorld();
        return true;
    }
}
#endregion Giant Sporite Cluster

#region Giant Sporite Cluster adds
public class SporiteClusterAdds : GameEpicNPC
{
    public SporiteClusterAdds() : base()
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
        get { return 250; }
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
        return 300;
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
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        int damageDealt = damageAmount + criticalAmount;
        foreach (GameNpc copy in GetNPCsInRadius(10000))
        {
            if (copy != null)
            {
                if ((copy is SporiteClusterAdds || copy is GiantSporiteCluster) && this != copy && copy.IsAlive)
                {
                    copy.Health = Health;
                }
            }
        }
    }
    public override void Die(GameObject killer)
    {
        foreach(GameNpc copy in GetNPCsInRadius(10000))
        {
            if (copy != null)
            {
                if (this != copy && copy is SporiteClusterAdds && copy.IsAlive)
                {
                    copy.RemoveFromWorld();
                }
            }
        }
        foreach (GameNpc boss in GetNPCsInRadius(10000))
        {
            if (boss != null)
            {
                if (this != boss && boss is GiantSporiteCluster && boss.IsAlive)
                {
                    boss.Die(boss);
                }
            }
        }
        base.Die(killer);
    }
    public override short Strength { get => base.Strength; set => base.Strength = 50; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 100; }
    public override bool AddToWorld()
    {
        Model = 906;
        Name = "Giant Sporite Cluster";
        RespawnInterval = -1;
        MaxDistance = 0;
        TetherRange = 0;
        Size = (byte)Util.Random(45, 55);
        Level = 79;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        BodyType = 8;
        Realm = ERealm.None;
        SporiteClusterAddsBrain adds = new SporiteClusterAddsBrain();
        LoadedFromScript = true;
        SetOwnBrain(adds);
        base.AddToWorld();
        return true;
    }
}
#endregion Giant Sporite Cluster adds