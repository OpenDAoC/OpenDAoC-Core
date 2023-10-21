using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Server;

namespace Core.GS;

#region High Lord Baln
public class HighLordBaln : GameEpicBoss
{
    private static new readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("High Lord Baln initialized..");
    }

    public HighLordBaln()
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
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162130);
        LoadTemplate(npcTemplate);

        Strength = npcTemplate.Strength;
        Constitution = npcTemplate.Constitution;
        Dexterity = npcTemplate.Dexterity;
        Quickness = npcTemplate.Quickness;
        Empathy = npcTemplate.Empathy;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        RespawnInterval = ServerProperty.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds

        // demon
        BodyType = 2;
        Faction = FactionMgr.GetFactionByID(191);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

        HighLordBalnBrain sBrain = new HighLordBalnBrain();
        SetOwnBrain(sBrain);
        LoadedFromScript = false;
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100  * ServerProperty.EPICS_DMG_MULTIPLIER;
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
    public override void OnAttackedByEnemy(AttackData ad)
    {          
        if (!InCombat)
        {
            var mobs = GetNPCsInRadius(3000);
            foreach (GameNpc mob in mobs)
            {
                if (!mob.InCombat)
                {
                    mob.StartAttack(ad.Attacker);
                }
            }
        }
        base.OnAttackedByEnemy(ad);
    }
}
#endregion High Lord Baln

#region Baln minion
public class BalnMinion : GameNpc
{
    public override int MaxHealth
    {
        get { return 800; }
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162130);
        LoadTemplate(npcTemplate);
        Level = 58;
        Strength = 300;
        Size = 50;
        Name += "'s Minion";
        RoamingRange = 350;
        RespawnInterval = -1;
        MaxDistance = 1500;
        TetherRange = 2000;
        IsWorthReward = false; // worth no reward
        Realm = ERealm.None;
        BalnMinionBrain adds = new BalnMinionBrain();
        LoadedFromScript = true;
        SetOwnBrain(adds);

        // demon
        BodyType = 2;

        Faction = FactionMgr.GetFactionByID(191);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));

        base.AddToWorld();
        return true;
    }
    public override void DropLoot(GameObject killer) //no loot
    {
    }
    public override long ExperienceValue => 0;
    public override void Die(GameObject killer)
    {
        base.Die(null); // null to not gain experience
    }
}
#endregion Baln minion