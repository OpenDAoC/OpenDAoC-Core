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
	public class AtlasOF_Trip : TimedRealmAbility, ISpellCastingAbilityHandler
    {
		public AtlasOF_Trip(DBAbility dba, int level) : base(dba, level) { }

        // ISpellCastingAbilityHandler
        public Spell Spell { get { return m_spell; } }
        public SpellLine SpellLine { get { return m_spellline; } }
        public Ability Ability { get { return this; } }

        private const int m_tauntValue = 0;
		private const int m_range = 0; // pbaoe
        private const int m_radius = 500; //
        private const eDamageType m_damageType = eDamageType.Natural;

		private DBSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;

        public override int MaxLevel { get { return 1; } }
		public override int CostForUpgrade(int level) { return 10; }
		public override int GetReUseDelay(int level) { return 900; } // 15 mins
		
		public override bool CheckRequirement(GamePlayer player) { return AtlasRAHelpers.HasAugConLevel(player, 3); }

        private void CreateSpell(GamePlayer caster)
        {
            m_dbspell = new DBSpell();
            m_dbspell.Name = "Trip";
            m_dbspell.Icon = 333;
            m_dbspell.ClientEffect = 7046;
            m_dbspell.Damage = 0;
			m_dbspell.DamageType = (int)m_damageType;
            m_dbspell.Target = "Enemy";
            m_dbspell.Radius = m_radius;
			m_dbspell.Type = eSpellType.SpeedDecrease.ToString();
            m_dbspell.Value = 30;
            m_dbspell.Duration = 12;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 0;
            m_dbspell.RecastDelay = GetReUseDelay(0); // Spell code is responsible for disabling this ability and will use this value.
            m_dbspell.Range = m_range;
            m_dbspell.Description = "Reduce the movement speed of all enemies in a " 
                                               + m_radius + " unit radius by 35%.";
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

			foreach (GamePlayer pl in caster.GetPlayersInRadius(m_radius))
			{
				if(pl.Realm != caster.Realm)
					CastSpellOn(pl);
			}

			foreach (GameNPC npc in caster.GetNPCsInRadius(m_radius))
			{
				CastSpellOn(npc);
			}

			// We do not need to handle disabling the skill here. This ability casts a spell and is linked to that spell.
            // The spell casting code will disable this ability in SpellHandler's FinishSpellcast().
		}

        public void CastSpellOn(GameLiving target)
        {
	        if (target.IsAlive && m_spell != null)
	        {
		        ISpellHandler dd = ScriptMgr.CreateSpellHandler(target, m_spell, m_spellline);
		        dd.StartSpell(target);
	        }
        }

	}
}
