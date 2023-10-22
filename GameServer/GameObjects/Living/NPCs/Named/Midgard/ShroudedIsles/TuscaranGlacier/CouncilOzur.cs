using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS;

public class CouncilOzur : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public CouncilOzur()
        : base()
    {
    }

    public virtual int OzurDifficulty
    {
        get { return ServerProperty.SET_DIFFICULTY_ON_EPIC_ENCOUNTERS; }
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperty.EPICS_DMG_MULTIPLIER;
    }

    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60159452);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;
        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

        Name = "Council Ozur";
        Model = 918;
        Size = 70;
        Level = 77;
        // Giant
        BodyType = 5;
        ScalingFactor = 45;

        CouncilOzurBrain sBrain = new CouncilOzurBrain();
        SetOwnBrain(sBrain);
        LoadedFromScript = false;//load from database
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
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