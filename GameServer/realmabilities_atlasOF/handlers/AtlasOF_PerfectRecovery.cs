using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_PerfectRecovery : TimedRealmAbility, ISpellCastingAbilityHandler
    {
        public AtlasOF_PerfectRecovery(DbAbility dba, int level) : base(dba, level) { }

        // ISpellCastingAbilityHandler
        public Spell Spell { get { return m_spell; } }
        public SpellLine SpellLine { get { return m_spellline; } }
        public Ability Ability { get { return this; } }

        private DbSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;

        private bool LastTargetLetRequestExpire = false;
        private const string RESURRECT_CASTER_PROPERTY = "RESURRECT_CASTER";

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 14; }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins

        private void CreateRezSpell(GamePlayer caster)
        {
            m_dbspell = new DbSpell();
            m_dbspell.Name = "Perfect Recovery Rez";
            m_dbspell.Icon = 0;
            m_dbspell.ClientEffect = 7019;
            m_dbspell.Damage = 0;
            m_dbspell.Target = "corpse"; // Rez spells are of that type so that they only work on dead realm members.
            m_dbspell.Radius = 0;
            m_dbspell.Type = eSpellType.Resurrect.ToString();
            m_dbspell.Value = 0;
            m_dbspell.Duration = 0;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 0;
            m_dbspell.Range = 1500;
            m_dbspell.RecastDelay = GetReUseDelay(0); // Spell code is responsible for disabling this ability and will use this value.
            m_dbspell.ResurrectHealth = 100;
            m_dbspell.ResurrectMana = 100;
            m_spell = new Spell(m_dbspell, caster.Level);
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", false);
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
                return;

            if (living is not GamePlayer m_caster || m_caster.castingComponent == null)
                return;

            if (m_caster.TargetObject is not GameLiving)
                return;

            CreateRezSpell(m_caster);

            if (m_spell != null)
                m_caster.castingComponent.RequestStartCastSpell(m_spell, m_spellline, this);

            // Cleanup
            m_spell = null;
            m_dbspell = null;
            m_spellline = null;

            // We do not need to handle disabling the skill here. This ability casts a spell and is linked to that spell.
            // The spell casting code will disable this ability in SpellHandler's FinishSpellcast().
        }

        public void OnRezDeclined(GamePlayer rezzer)
        {
            rezzer.DisableSkill(this, 0); // Re-enable PR;
        }
    }
}
