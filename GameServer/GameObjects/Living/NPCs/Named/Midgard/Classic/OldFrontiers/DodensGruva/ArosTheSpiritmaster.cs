using System;
using Core.AI.Brain;
using Core.Database;
using Core.Events;

namespace Core.GS.Scripts;

public class ArosTheSpiritmaster : GameEpicAros
{
    /// <summary>
    /// Set Aros the Spiritmaster Stats, Peace Flag and Equiptemplate
    /// </summary>
    public override bool AddToWorld()
    {
        INpcTemplate npcTemplate = NpcTemplateMgr.GetTemplate(9916);
        LoadTemplate(npcTemplate);
        Strength = npcTemplate.Strength;
        Dexterity = npcTemplate.Dexterity;
        Constitution = npcTemplate.Constitution;
        Quickness = npcTemplate.Quickness;
        Piety = npcTemplate.Piety;
        Intelligence = npcTemplate.Intelligence;
        Empathy = npcTemplate.Empathy;

        ScalingFactor = 40;
        Faction = FactionMgr.GetFactionByID(779);
        LoadedFromScript = false; //load from database
        SaveIntoDatabase();
        base.AddToWorld();
        BroadcastLivingEquipmentUpdate();
        base.SetOwnBrain(new ArosTheSpiritmasterBrain());
        return true;
    }
    [ScriptLoadedEvent]
    public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
    {
        if (log.IsInfoEnabled)
            log.Info("Aros the Spiritmaster NPC Initializing...");
    }
    #region Debuff
    private Spell m_Debuff;
    /// <summary>
    /// The Debuff spell.
    /// </summary>
    protected override Spell Debuff
    {
        get
        {
            if (m_Debuff == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 0;
                spell.Uninterruptible = true;
                spell.ClientEffect = 4575;
                spell.Icon = 4575;
                spell.Description = "Spirit Resist Debuff";
                spell.Name = "Negate Spirit";
                spell.Range = 1500;
                spell.Radius = 1500;
                spell.Value = 45 * ArosDifficulty / 100;
                spell.Duration = 45;
                spell.Damage = 0;
                spell.DamageType = (int) EDamageType.Spirit;
                spell.SpellID = 4575;
                spell.Target = "Enemy";
                spell.MoveCast = true;
                spell.Type = ESpellType.SpiritResistDebuff.ToString();
                spell.Message1 = "You feel more vulnerable to spirit magic!";
                spell.Message2 = "{0} seems vulnerable to spirit magic!";
                m_Debuff = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Debuff);
            }
            return m_Debuff;
        }
    }
    #endregion

    #region Summon
    private Spell m_Summon;
    /// <summary>
    /// The Debuff spell.
    /// </summary>
    protected override Spell Summon
    {
        get
        {
            if (m_Summon == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 1;
                spell.Uninterruptible = true;
                spell.ClientEffect = 2802;
                spell.Description = "Summon a Pet";
                spell.Name = "Summoning Aros Pet";
                spell.Range = 1000;
                spell.Radius = 1000;
                spell.Damage = 0;
                spell.DamageType = (int) EDamageType.Spirit;
                spell.Target = "Enemy";
                spell.MoveCast = false;
                spell.Type = ESpellType.SpiritResistDebuff.ToString();
                m_Summon = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_Summon);
            }
            return m_Summon;
        }
    }
    #endregion

    #region Bomb
    /// <summary>
    /// The Bomb spell.
    /// </summary>
    protected override Spell Bomb
    {
        get
        {
            if (m_BombSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 3;
                spell.ClientEffect = 2797;
                spell.Damage = 1100 * ArosDifficulty / 100;
                spell.Name = "Soul Annihilation";
                spell.Range = 1000;
                spell.Radius = 750;
                spell.SpellID = 2797;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = false;
                spell.DamageType = (int) EDamageType.Spirit; //Spirit DMG Type
                m_BombSpell = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BombSpell);
            }
            return m_BombSpell;
        }
    }
    #endregion Bomb

    #region BigBomb
    /// <summary>
    /// The Bomb spell.
    /// </summary>
    protected override Spell BigBomb
    {
        get
        {
            if (m_BigBombSpell == null)
            {
                DbSpell spell = new DbSpell();
                spell.AllowAdd = false;
                spell.CastTime = 6;
                spell.ClientEffect = 2797;
                spell.Damage = 1350 * ArosDifficulty / 100;
                spell.Name = "Soul Annihilation";
                spell.Range = 1000;
                spell.Radius = 1500;
                spell.SpellID = 2797;
                spell.Target = "Enemy";
                spell.Type = ESpellType.DirectDamageNoVariance.ToString();
                spell.Uninterruptible = true;
                spell.MoveCast = false;
                spell.DamageType = (int) EDamageType.Spirit; //Spirit DMG Type
                m_BigBombSpell = new Spell(spell, 70);
                SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_BigBombSpell);
            }
            return m_BigBombSpell;
        }
    }
    #endregion Bomb
}