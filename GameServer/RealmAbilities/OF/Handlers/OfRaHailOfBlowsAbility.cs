using System.Collections.Generic;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities;

public class OfRaHailOfBlowsAbility : TimedRealmAbility
{
    public OfRaHailOfBlowsAbility(DbAbility dba, int level) : base(dba, level) { }

    public const int duration = 60000; // 60 seconds
    public override int MaxLevel { get { return 3; } }
    public override int GetReUseDelay(int level) { return 900; } // 15 mins
    public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugDexLevel(player) >= 3; }
    public override int CostForUpgrade(int currentLevel) { return OfRaHelpers.GetCommonUpgradeCostFor3LevelsRA(currentLevel); }
    
    private DbSpell m_dbspell;
    private Spell m_spell = null;
    private SpellLine m_spellline;
    private double m_hasteValue = 0;
    
    public override void AddEffectsInfo(IList<string> list)
    {
        list.Add("Target: Self");
        list.Add("Duration: 60 sec");
        list.Add("Casting time: instant");
    }

    public virtual void CreateSpell(double damage)
    {
        m_dbspell = new DbSpell();
        m_dbspell.Name = "Hail Of Blows";
        m_dbspell.Icon = 7130;
        m_dbspell.ClientEffect = 7130;
        m_dbspell.Damage = 0;
        m_dbspell.DamageType = 0;
        m_dbspell.Target = "Self";
        m_dbspell.Radius = 0;
        m_dbspell.Type = ESpellType.CombatSpeedBuff.ToString();
        m_dbspell.Value = m_hasteValue;
        m_dbspell.Duration = 60;
        m_dbspell.Pulse = 0;
        m_dbspell.PulsePower = 0;
        m_dbspell.Power = 0;
        m_dbspell.CastTime = 0;
        m_dbspell.EffectGroup = 100;
        m_dbspell.Range = 0;
        m_spell = new Spell(m_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
        m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
    }

    public override void Execute(GameLiving living)
    {
        if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
        if (living is GamePlayer p)
            m_hasteValue = GetHasteValue();

        // Console.WriteLine($"haste buff of {m_hasteValue} applied");
        CreateSpell(m_hasteValue);
        CastSpell(living);
        DisableSkill(living);
    }
    
    protected virtual double GetHasteValue()
    {
        return 5 * Level;
    }
    
    protected void CastSpell(GameLiving target)
    {
        if (target.IsAlive && m_spell != null)
        {
            ISpellHandler dd = ScriptMgr.CreateSpellHandler(target, m_spell, m_spellline);
            dd.StartSpell(target);
        }
    }
}