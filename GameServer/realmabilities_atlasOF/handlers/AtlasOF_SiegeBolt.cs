using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_SiegeBolt : TimedRealmAbility, ISpellCastingAbilityHandler
    {
        public AtlasOF_SiegeBolt(DbAbility dba, int level) : base(dba, level) { }

        public Spell Spell { get { return m_spell; } }
        public SpellLine SpellLine { get { return m_spellline; } }
        public Ability Ability { get { return this; } }

        // You can one shot miles gates, keep doors at level 1 and 2, rams and other siege equipment.
        // On reliquary doors, 5 bolts are needed.
        // This puts the damage at at least 20k, after resits / toughness.
        // This should be changed based on server settings. And ideally made so that it's adjusted automatically.
        private const int m_dmgValue = 25000;
        private const int m_range = 1875;
        private const int m_radius = 0;

        private DbSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 14; }
        public override int GetReUseDelay(int level) { return 300; } //5 mins

        private void CreateSpell(GamePlayer caster)
        {
            m_dbspell = new DbSpell();
            m_dbspell.Name = "Siege Bolt";
            m_dbspell.Icon = 0;
            m_dbspell.ClientEffect = 2100;
            m_dbspell.Damage = m_dmgValue;
            m_dbspell.DamageType = (int)eDamageType.Natural;
            m_dbspell.Target = "Enemy";
            m_dbspell.Radius = m_radius;
            m_dbspell.Type = eSpellType.SiegeDirectDamage.ToString();
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
            m_spellline = GlobalSpellsLines.RealmSpellsSpellLine;
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
                return;

            if (living is not GamePlayer m_caster || m_caster.castingComponent == null)
                return;

            GameObject m_target = m_caster.TargetObject;

            if (m_target == null)
                return;

            if (m_target is not GameSiegeWeapon and not GameKeepDoor)
            {
                m_caster.Out.SendMessage("You can only cast this spell on keep doors and siege weapons.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }

            if (m_caster.GetDistance(m_target) > m_range)
            {
                m_caster.Out.SendMessage("Target out of range.", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
                return;
            }

            CreateSpell(m_caster);

            if (m_spell != null)
                m_caster.CastSpell(m_spell, m_spellline, this);

            DisableSkill(m_caster);

            // We do not need to handle disabling the skill here. This ability casts a spell and is linked to that spell.
            // The spell casting code will disable this ability in SpellHandler's FinishSpellcast().
        }
    }
}
