using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS;

#region Council Otrygg
public class CouncilOtrygg : GameEpicBoss
{
    public CouncilOtrygg() : base()
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
    public override int AttackRange
    {
        get { return 350; }
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
    public override int MaxHealth
    {
        get { return 100000; }
    }
    public override void Die(GameObject killer)
    {
        foreach (GameNpc npc in GetNPCsInRadius(8000))
        {
            if (npc == null) continue;
            if (!npc.IsAlive) continue;
            if (npc.Brain is OtryggAddBrain)
            {
                npc.Die(this);
            }
        }
        base.Die(killer);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159451);
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
        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds

        OtryggAdd.PetsCount = 0;
        CouncilOtryggBrain sbrain = new CouncilOtryggBrain();
        SetOwnBrain(sbrain);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
}
#endregion Council Otrygg

#region Otrygg adds
public class OtryggAdd : GameNpc
{
    public OtryggAdd() : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 20;// dmg reduction for melee dmg
            case EDamageType.Crush: return 20;// dmg reduction for melee dmg
            case EDamageType.Thrust: return 20;// dmg reduction for melee dmg
            default: return 20;// dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 50 * ServerProperty.EPICS_DMG_MULTIPLIER;
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
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
        get { return 10000; }
    }
    public override void Die(GameObject killer)
    {
        PetsCount--;
        base.Die(killer);
    }
    public override void DropLoot(GameObject killer) //no loot
    {
    }
    public static int PetsCount = 0;
    #region Stats
    public override short Charisma { get => base.Charisma; set => base.Charisma = 200; }
    public override short Piety { get => base.Piety; set => base.Piety = 200; }
    public override short Intelligence { get => base.Intelligence; set => base.Intelligence = 200; }
    public override short Empathy { get => base.Empathy; set => base.Empathy = 200; }
    public override short Dexterity { get => base.Dexterity; set => base.Dexterity = 200; }
    public override short Quickness { get => base.Quickness; set => base.Quickness = 100; }
    public override short Strength { get => base.Strength; set => base.Strength = 50; }
    #endregion
    public override bool AddToWorld()
    {
        Model = (byte)Util.Random(241,244);
        MeleeDamageType = EDamageType.Crush;
        Name = "summoned pet";
        RespawnInterval = -1;

        RoamingRange = 120;
        Size = 50;
        Level = 62;
        MaxSpeedBase = 250;

        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));
        BodyType = 6;
        Realm = ERealm.None;

        OtryggAddBrain adds = new OtryggAddBrain();
        SetOwnBrain(adds);
        LoadedFromScript = false;
        base.AddToWorld();
        return true;
    }
}
#endregion Otrygg adds