using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities;

public class OfRaTripAbility : TimedRealmAbility, ISpellCastingAbilityHandler
{
	public OfRaTripAbility(DbAbility dba, int level) : base(dba, level) { }

    // ISpellCastingAbilityHandler
    public Spell Spell { get { return m_spell; } }
    public SpellLine SpellLine { get { return m_spellline; } }
    public Ability Ability { get { return this; } }

    private const int m_tauntValue = 0;
	private const int m_range = 350;
	private const EDamageType m_damageType = EDamageType.Natural;

	private DbSpell m_dbspell;
    private Spell m_spell = null;
    private SpellLine m_spellline;

    public override int MaxLevel { get { return 1; } }
	public override int CostForUpgrade(int level) { return 10; }
	public override int GetReUseDelay(int level) { return 900; } // 15 mins

	public override bool CheckRequirement(GamePlayer player) { return true; }

    private void CreateSpell(GamePlayer caster)
    {
        m_dbspell = new DbSpell();
        m_dbspell.Name = "Trip";
        m_dbspell.Icon = 4225;
        m_dbspell.ClientEffect = 2758;
        m_dbspell.Damage = 0;
		m_dbspell.DamageType = (int)m_damageType;
        m_dbspell.Target = "Enemy";
        m_dbspell.Radius = 0;
		m_dbspell.Type = ESpellType.SpeedDecrease.ToString();
        m_dbspell.Value = 30;
        m_dbspell.Duration = 15;
        m_dbspell.Pulse = 0;
        m_dbspell.PulsePower = 0;
        m_dbspell.Power = 0;
        m_dbspell.CastTime = 0;
        m_dbspell.EffectGroup = 0;
        m_dbspell.RecastDelay = GetReUseDelay(0); // Spell code is responsible for disabling this ability and will use this value.
        m_dbspell.Range = m_range;
        m_dbspell.Message1 = "You are tripped and cannot move as quickly.";
        m_dbspell.Message2 = "{0}'s is tripped and cannot move as quickly!";
        m_dbspell.Description = "Reduce the movement speed of all enemies in a " 
                                           + m_range + " unit radius by 35%.";
		m_spell = new Spell(m_dbspell, caster.Level);
        m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
    }

    public override void Execute(GameLiving living)
	{
		if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
		GamePlayer caster = living as GamePlayer;
		if (caster == null)
			return;

		CreateSpell(caster);

		foreach (GamePlayer pl in caster.GetPlayersInRadius(m_range))
		{
			if(pl.Realm != caster.Realm)
				CastSpellOn(pl, caster);
		}

		foreach (GameNpc npc in caster.GetNPCsInRadius(m_range))
		{
			CastSpellOn(npc, caster);
		}

		DisableSkill(caster);
	}

    public void CastSpellOn(GameLiving target, GameLiving caster)
    {
        if (target.IsAlive && m_spell != null)
        {
	        ISpellHandler dd = ScriptMgr.CreateSpellHandler(caster, m_spell, m_spellline);
	        dd.StartSpell(target);
        }
    }

}