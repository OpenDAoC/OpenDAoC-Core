using System;
using System.Reflection;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.AI.Brains;
using log4net;

namespace Core.GS;

public class HighLordBaelerdoth : GameEpicBoss
{
    private static new readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("High Lord Baelerdoth initialized..");
    }
    public HighLordBaelerdoth() : base()
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
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(60162129);
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

        HighLordBaelerdothBrain sBrain = new HighLordBaelerdothBrain();
        SetOwnBrain(sBrain);
        SaveIntoDatabase();
        base.AddToWorld();
        return true;
    }
    public override double AttackDamage(DbInventoryItem weapon)
    {
        return base.AttackDamage(weapon) * Strength / 100  * ServerProperties.Properties.EPICS_DMG_MULTIPLIER;
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
        base.Die(killer);
    }
    public override void TakeDamage(GameObject source, EDamageType damageType, int damageAmount, int criticalAmount)
    {
        if (HealthPercent < 25)
        {
            CastSpell(AbsDebuff, SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells));
        }
        base.TakeDamage(source, damageType, damageAmount, criticalAmount);
    }
    #region pbaoe abs debuff

    /// <summary>
    /// The Bomb spell.
    /// and assign the spell to m_breathSpell.
    /// </summary>
    ///
    /// 
    protected Spell m_absDebuffSpell;

    /// <summary>
    /// The Bomb spell.
    /// </summary>
    protected Spell AbsDebuff
    {
        get
        {
            if (m_absDebuffSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.Duration = 20;
                spell.ClientEffect = 9606;
                spell.Icon = 9606;
                spell.Damage = 0;
                spell.Value = 15;
                spell.Name = "Aura of Baelerdoth";
                spell.Range = 1500;
                spell.Radius = 350;
                spell.SpellID = 99998;
                spell.Target = "Enemy";
                spell.Type = "ArmorAbsorptionDebuff";
                spell.Uninterruptible = true;
                spell.MoveCast = true;
                spell.DamageType = (int) EDamageType.Spirit;
                m_absDebuffSpell = new Spell(spell, 50);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_absDebuffSpell);
            }

            return m_absDebuffSpell;
        }
    }

    #endregion
}