using System;
using System.Collections;
using System.Reflection;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.Events;
using DOL.Database;
using DOL.GS.Keeps;

namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_SiegeBolt : TimedRealmAbility, ISpellCastingAbilityHandler
    {
		public AtlasOF_SiegeBolt(DBAbility dba, int level) : base(dba, level) { }

        // ISpellCastingAbilityHandler
        public Spell Spell { get { return m_spell; } }
        public SpellLine SpellLine { get { return m_spellline; } }
        public Ability Ability { get { return this; } }

        private const int m_dmgValue = 65000; //from thread: you can ONE SHOT miles gates, keep doors at level 1 ( just take keep by another realm for exemple ) to level 2 doors, OS ram and other siege equipement ...
											//on relic raid, with doors level 10, you need 5 theurg to rekt this door ( something like 20% dmg on doors level 10 )
											//level 1 door has 10k hp, level 10 door has 100k HP so this must deal about 20k damage after resists. 
		private const int m_range = 1875; // bolt range
        private const int m_radius = 0; //

		private DBSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;

        public override int MaxLevel { get { return 1; } }
		public override int CostForUpgrade(int level) { return 14; }
		public override int GetReUseDelay(int level) { return 300; } //5 mins

        private void CreateSpell(GamePlayer caster)
        {
            m_dbspell = new DBSpell();
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
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			GamePlayer m_caster = living as GamePlayer;
			if (m_caster == null || m_caster.castingComponent == null)
				return;
			GameObject m_target = m_caster.TargetObject as GameObject;
            if (m_target == null)
                return;
            if (m_target is not GameSiegeWeapon && m_target is not GameKeepDoor)
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
            {
	            m_caster.castingComponent.StartCastSpell(m_spell, m_spellline, this);
            }
            DisableSkill(m_caster);

            // We do not need to handle disabling the skill here. This ability casts a spell and is linked to that spell.
            // The spell casting code will disable this ability in SpellHandler's FinishSpellcast().
		}
	}
}
