using System;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using Core.GS.Enums;

namespace Core.GS;

public class PrincessNahemah : GameEpicBoss
{
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("Princess Nahemah initialized..");
    }

    [ScriptUnloadedEvent]
    public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
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
    
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
    }
    public override int MaxHealth
    {
        get { return 100000; }
    }
    public override short MaxSpeedBase => (short) (191 + Level * 2);
    public override int AttackRange
    {
        get => 180;
        set { }
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60165038);
        LoadTemplate(npcTemplate);
        MaxDistance = 1500;
        TetherRange = 2000;
        RoamingRange = 400;
        PrincessNahemahBrain sBrain = new PrincessNahemahBrain();
        SetOwnBrain(sBrain);
        sBrain.AggroLevel = 100;
        sBrain.AggroRange = 500;
        PrincessNahemahBrain.spawnMinions = true;

        // demon
        BodyType = 2;
        RespawnInterval = ServerProperties.Properties.SET_SI_EPIC_ENCOUNTER_RESPAWNINTERVAL * 60000;//1min is 60000 miliseconds
        Faction = FactionMgr.GetFactionByID(191);
        Faction.AddFriendFaction(FactionMgr.GetFactionByID(191));
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
    public override void Die(GameObject killer)
    {
        base.Die(killer);

        foreach (GameNpc npc in GetNPCsInRadius(4000))
        {
            if (npc.Brain is NahemahMinionBrain)
            {
                npc.RemoveFromWorld();
            }
        }
    }
}

public class NahemahMinion : GameNpc
{
    public override int MaxHealth
    {
        get { return 550; }
    }
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162189);
        LoadTemplate(npcTemplate);
        RoamingRange = 350;
        RespawnInterval = -1;
        MaxDistance = 1500;
        TetherRange = 2000;
        IsWorthReward = false; // worth no reward
        Realm = ERealm.None;
        NahemahMinionBrain adds = new NahemahMinionBrain();
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
    public override void Die(GameObject killer)
    {
        base.Die(null); // null to not gain experience
    }
    public override void OnAttackEnemy(AttackData ad)
    {
        if (Util.Chance(10))
        {
            CastSpell(FireDD, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.OnAttackEnemy(ad);
    }
    #region fire aoe dd

    /// <summary>
    /// The Bomb spell.
    /// and assign the spell to m_breathSpell.
    /// </summary>
    ///
    /// 
    protected Spell m_fireDDSpell;

    /// <summary>
    /// The Bomb spell.
    /// </summary>
    protected Spell FireDD
    {
        get
        {
            if (m_fireDDSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.ClientEffect = 310;
                spell.Icon = 310;
                spell.Damage = 170;
                spell.Name = "Maelstrom";
                spell.Range = 1500;
                spell.Radius = 350;
                spell.SpellID = 99998;
                spell.Target = "Enemy";
                spell.Type = "DirectDamage";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Heat;
                m_fireDDSpell = new Spell(spell, 50);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_fireDDSpell);
            }

            return m_fireDDSpell;
        }
    }
    #endregion
}