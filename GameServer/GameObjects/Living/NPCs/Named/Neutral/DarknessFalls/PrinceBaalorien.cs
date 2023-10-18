using System;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;

namespace DOL.GS;

public class PrinceBaalorien : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("Prince Ba'alorien initialized..");
    }
    [ScriptUnloadedEvent]
    public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
    {
    }
    public PrinceBaalorien()
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
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165031);
        LoadTemplate(npcTemplate);

        Strength = npcTemplate.Strength;
        Constitution = npcTemplate.Constitution;
        Dexterity = npcTemplate.Dexterity;
        Quickness = npcTemplate.Quickness;
        Empathy = npcTemplate.Empathy;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;

        // demon
        BodyType = 2;
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(191);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

        PrinceBaalorienBrain sBrain = new PrinceBaalorienBrain();
        SetOwnBrain(sBrain);
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }

    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override int AttackRange
    {
        get { return 450; }
        set { }
    }
    public override bool HasAbility(string keyName)
    {
        if (IsAlive && keyName == GS.Abilities.CCImmunity)
            return true;

        return base.HasAbility(keyName);
    }
}