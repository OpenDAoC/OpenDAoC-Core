using DOL.Database;
namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_Juggernaut : TimedRealmAbility, ISpellCastingAbilityHandler
    {
		public AtlasOF_Juggernaut(DBAbility dba, int level) : base(dba, level) { }

        // ISpellCastingAbilityHandler
        public Spell Spell { get { return m_spell; } }
        public SpellLine SpellLine { get { return m_spellline; } }
        public Ability Ability { get { return this; } }

        private DBSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;

        public override int MaxLevel { get { return 1; } }
		public override int CostForUpgrade(int level) { return 14; }
		public override int GetReUseDelay(int level) { return 1800; } // 30 mins
		
        public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			GamePlayer m_caster = living as GamePlayer;
			if (m_caster == null || m_caster.castingComponent == null)
				return;

            GameLiving m_target = m_caster.TargetObject as GameLiving;

            SpellLine RAspellLine = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
            Spell Juggernaut = SkillBase.GetSpellByID(90801);

            if (Juggernaut != null)
            {
	            m_caster.CastSpell(Juggernaut, RAspellLine);
            }
            
		}
	}
}
