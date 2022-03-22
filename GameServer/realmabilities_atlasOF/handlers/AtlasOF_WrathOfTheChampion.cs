using System;
using System.Collections;
using System.Reflection;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.Events;
using DOL.Database;
namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_WrathOfTheChampion : TimedRealmAbility, ISpellCastingAbilityHandler
    {
		public AtlasOF_WrathOfTheChampion(DBAbility dba, int level) : base(dba, level) { }

        // ISpellCastingAbilityHandler
        public Spell Spell { get { return m_spell; } }
        public SpellLine SpellLine { get { return m_spellline; } }
        public Ability Ability { get { return this; } }

        private const int m_dmgValue = 750; //Unclear what the real OF value was. This forum says ~750 damage: https://forums.freddyshouse.com/threads/wrath-of-champions-ra.39511/
		private const int m_range = 0; // pbaoe
        private const int m_radius = 150; //
        private const eDamageType m_damageType = eDamageType.Spirit;

		private DBSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;

        public override int MaxLevel { get { return 1; } }
		public override int CostForUpgrade(int level) { return 10; }
		public override int GetReUseDelay(int level) { return 900; } // 15 mins

        private void CreateSpell(GamePlayer caster)
        {
            m_dbspell = new DBSpell();
            m_dbspell.Name = "Wrath Of The Champion";
            m_dbspell.Icon = 333;
            m_dbspell.ClientEffect = 2797;
            m_dbspell.Damage = m_dmgValue;
			m_dbspell.DamageType = (int)m_damageType;
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
            m_dbspell.Description = m_dmgValue + " damage blast erupts from the caster, damaging enemies in a " 
                                               + m_radius + " radius.";
			m_spell = new Spell(m_dbspell, caster.Level);
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			GamePlayer m_caster = living as GamePlayer;
			if (m_caster == null || m_caster.castingComponent == null)
				return;

            GameLiving m_target = m_caster.TargetObject as GameLiving;
            if (m_target == null)
                m_target = m_caster;

            CreateSpell(m_caster);

            if (m_spell != null)
            {
	            m_spell.Damage = m_spell.Damage * m_caster.Level / 50;
	            m_caster.castingComponent.StartCastSpell(m_spell, m_spellline, this);
            }

            // We do not need to handle disabling the skill here. This ability casts a spell and is linked to that spell.
            // The spell casting code will disable this ability in SpellHandler's FinishSpellcast().
		}
	}
}
