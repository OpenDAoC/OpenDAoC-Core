using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;

namespace Core.GS.Scripts;

public class CouncilHord : GameEpicBoss
{
    private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public CouncilHord()
        : base()
    {
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }      
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159449);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

        Faction = FactionMgr.GetFactionByID(140);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(140));

        Name = "Council Hord";
        Model = 918;
        Size = 70;
        Level = 77;
        // Giant
        BodyType = 5;
        ScalingFactor = 45;
        
        CouncilHordBrain sBrain = new CouncilHordBrain();
        SetOwnBrain(sBrain);
        LoadedFromScript = false;//load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }

    public override int MaxHealth
    {
        get{return 100000;}
    }
    public override int AttackRange
    {
        get{ return 450;}
        set{}
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
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
    #region Damage & Heal Events

    /// <summary>
    /// Take some amount of damage inflicted by another GameObject.
    /// </summary>
    /// <param name="source">The object inflicting the damage.</param>
    /// <param name="damageType">The type of damage.</param>
    /// <param name="damageAmount">The amount of damage inflicted.</param>
    /// <param name="criticalAmount">The critical amount of damage inflicted</param>
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
        Brain.Notify(GameObjectEvent.TakeDamage, this,
            new TakeDamageEventArgs(source, damageType, damageAmount, criticalAmount));
    }
    #endregion
    
}