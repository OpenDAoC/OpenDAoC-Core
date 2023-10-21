using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;

namespace Core.GS;

#region High Lord Oro
public class HighLordOro : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("High Lord Oro initialized..");
    }

    [ScriptUnloadedEvent]
    public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
    {
    }
    public HighLordOro()
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
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162132);
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

        HighLordOroBrain sBrain = new HighLordOroBrain();
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
    public override void Die(GameObject killer)
    {
        foreach (GameNpc npc in GetNPCsInRadius(5000))
        {
            if (npc.Brain is OroCloneBrain)
            {
                npc.Die(killer);
            }
        }
        base.Die(killer);
    }
}
#endregion High Lord Oro

#region High Lord Oro Clone
public class HighLordOroClone : GameNpc
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public HighLordOroClone()
        : base()
    {
    }
    public override int GetResist(EDamageType damageType)
    {
        switch (damageType)
        {
            case EDamageType.Slash: return 45; // dmg reduction for melee dmg
            case EDamageType.Crush: return 45; // dmg reduction for melee dmg
            case EDamageType.Thrust: return 45; // dmg reduction for melee dmg
            default: return 35; // dmg reduction for rest resists
        }
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100 * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override int MaxHealth
    {
        get { return 30000; }
    }
    public override int AttackRange
    {
        get { return 450; }
        set { }
    }
    public override double GetArmorAF(EArmorSlot slot)
    {
        return 250;
    }
    public override double GetArmorAbsorb(EArmorSlot slot)
    {
        // 85% ABS is cap.
        return 0.50;
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(90162132);
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

        Faction = FactionMgr.GetFactionByID(191);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

        OroCloneBrain sBrain = new OroCloneBrain();
        SetOwnBrain(sBrain);

        IsWorthReward = false;

        base.AddToWorld();
        return true;
    }
}
#endregion High Lord Oro Clone