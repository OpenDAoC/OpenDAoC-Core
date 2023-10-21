using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS;

public class Behemoth : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public Behemoth()
        : base()
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
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == AbilityConstants.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperty.EPICS_DMG_MULTIPLIER;
    }
    public override int MaxHealth
    {
        get { return 600000; }
    }

    public override int AttackRange
    {
        get { return 450; }
        set { }
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 550;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.50;
    }
    public override void OnAttackEnemy(AttackData ad)
    {
        if (ad != null && ad.Target != null && ad.Target.IsAlive)
            ad.Target.Die(this);

        base.OnAttackEnemy(ad);
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60158340);
        LoadTemplate(npcTemplate);

        Strength = npcTemplate.Strength;
        Constitution = npcTemplate.Constitution;
        Dexterity = npcTemplate.Dexterity;
        Quickness = npcTemplate.Quickness;
        Empathy = npcTemplate.Empathy;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;

        BehemothBrain sBrain = new BehemothBrain();
        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        base.AddToWorld();
        return true;
    }
}