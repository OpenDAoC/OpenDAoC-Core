using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_VolcanicPillar : TimedRealmAbility, ISpellCastingAbilityHandler
    {
        public AtlasOF_VolcanicPillar(DbAbility dba, int level) : base(dba, level) { }

        // ISpellCastingAbilityHandler
        public Spell Spell { get { return m_spell; } }
        public SpellLine SpellLine { get { return m_spellline; } }
        public Ability Ability { get { return this; } }

        private const int m_dmgValue = 900; // Takii - Temp value. Unclear what the real OF value was.
        private const int m_range = 1875; // bolt range
        private const int m_radius = 500; // post-1.62 nerf value (was 700)

        private DbSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 14; }
        public override int GetReUseDelay(int level) { return 900; } // 15 mins

        private void CreateSpell(GamePlayer caster)
        {
            m_dbspell = new DbSpell();
            m_dbspell.Name = "Volcanic Pillar";
            m_dbspell.Icon = 4253;
            m_dbspell.ClientEffect = 7025;
            m_dbspell.Damage = m_dmgValue;
            m_dbspell.DamageType = 13; // heat
            m_dbspell.Target = "Enemy";
            m_dbspell.Radius = m_radius;
            m_dbspell.Type = eSpellType.DirectDamageNoVariance.ToString();
            m_dbspell.Value = 0;
            m_dbspell.Duration = 0;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 0;
            m_dbspell.RecastDelay = GetReUseDelay(0); // Spell code is responsible for disabling this ability and will use this value.
            m_dbspell.Range = m_range;
            m_spell = new Spell(m_dbspell, caster.Level);
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
                return;

            if (living is not GamePlayer m_caster || m_caster.castingComponent == null)
                return;

            if (m_caster.TargetObject is not GameLiving)
                return;

            CreateSpell(m_caster);

            if (m_spell != null)
                m_caster.castingComponent.RequestStartCastSpell(m_spell, m_spellline, this);

            // We do not need to handle disabling the skill here. This ability casts a spell and is linked to that spell.
            // The spell casting code will disable this ability in SpellHandler's FinishSpellcast().
        }
    }
}
