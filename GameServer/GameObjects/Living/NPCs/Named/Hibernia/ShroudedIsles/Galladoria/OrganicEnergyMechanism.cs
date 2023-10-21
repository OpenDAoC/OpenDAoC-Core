using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;

namespace Core.GS;

#region Organic Energy Mechanism
public class OrganicEnergyMechanism : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public OrganicEnergyMechanism()
        : base()
    {
    }
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("Organic-Energy Mechanism Initializing...");
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
    public override void StartAttack(GameObject target)//dont attack
    {
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
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.20;
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 350;
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }

    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60164704);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Charisma = npcTemplate.Charisma;
        Empathy = npcTemplate.Empathy;

        OrganicEnergyMechanismBrain sBrain = new OrganicEnergyMechanismBrain();
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000; //1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        SetOwnBrain(sBrain);

        OrganicEnergyMechanismBrain.StartCastDOT = false;
        OrganicEnergyMechanismBrain.CanCast = false;
        OrganicEnergyMechanismBrain.RandomTarget = null;

        bool success = base.AddToWorld();
        if (success)
        {
            new EcsGameTimer(this, new EcsGameTimer.EcsTimerCallback(Show_Effect), 500);
        }
        SaveIntoDatabase();
        LoadedFromScript = false;
        return success;
    }

    protected int Show_Effect(EcsGameTimer timer)
    {
        if (IsAlive)
        {
            foreach (GamePlayer player in GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(this, this, 509, 0, false, 0x01);

            return 3000;
        }

        return 0;
    }
}
#endregion Organic Energy Mechanism

#region Organic Energy Mechanism adds
public class EnergyMechanismAdd : GameNpc
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public EnergyMechanismAdd()
        : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 35; // dmg reduction for melee dmg
            case EDamageType.Crush: return 35; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 35; // dmg reduction for melee dmg
            default: return 35; // dmg reduction for rest resists
        }
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.15;
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 200;
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override int MaxHealth
    {
        get { return 5000; }
    }
    public override int AttackRange
    {
        get { return 350; }
        set { }
    }
    public override void DropLoot(GameObject killer) //no loot
    {
    }
    public override void Die(GameObject killer)
    {
        base.Die(null); //null to not gain experience
    }
    public override short Strength { get => base.Strength; set => base.Strength = 150; }
    public override bool AddToWorld()
    {
        Model = 905;
        Name = "Summoned Bottom Feeder";
        Size = 32;
        Level = (byte) Util.Random(51, 55);
        Realm = 0;
        CurrentRegionID = 191; //galladoria

        Strength = 150;
        Intelligence = 150;
        Piety = 150;
        Dexterity = 200;
        Constitution = 200;
        Quickness = 125;
        RespawnInterval = -1;
        Gender = EGender.Neutral;
        MeleeDamageType = EDamageType.Slash;
        Faction = FactionMgr.GetFactionByID(96);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(96));
        IsWorthReward = false; //worth no reward

        BodyType = 1;
        MaxSpeedBase = 245;
        EnergyMechanismAddBrain sBrain = new EnergyMechanismAddBrain();
        SetOwnBrain(sBrain);
        base.AddToWorld();
        return true;
    }
}
#endregion Organic Energy Mechanism adds