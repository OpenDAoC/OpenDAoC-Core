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
	public class AtlasOF_BattleYell : TimedRealmAbility, ISpellCastingAbilityHandler
    {
		public AtlasOF_BattleYell(DBAbility dba, int level) : base(dba, level) { }

        // ISpellCastingAbilityHandler
        public Spell Spell { get { return m_spell; } }
        public SpellLine SpellLine { get { return m_spellline; } }
        public Ability Ability { get { return this; } }

        
		private const int m_range = 500; // pbaoe
        private const int m_radius = 0; //
        private const eDamageType m_damageType = eDamageType.Natural;

		private DBSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;
        private int m_tauntValue = 0;

        public override int MaxLevel { get { return 1; } }
		public override int CostForUpgrade(int level) { return 14; }
		public override int GetReUseDelay(int level) { return 900; } // 15 mins
		
		public override bool CheckRequirement(GamePlayer player) { return AtlasRAHelpers.HasAugConLevel(player, 3); }

        private void CreateSpell(GamePlayer caster)
        {
            m_dbspell = new DBSpell();
            m_dbspell.Name = "Battle Yell";
            m_dbspell.Icon = 333;
            m_dbspell.ClientEffect = 7276;
            m_dbspell.Damage = 0;
			m_dbspell.DamageType = (int)m_damageType;
            m_dbspell.Target = "Enemy";
            m_dbspell.Radius = m_radius;
			m_dbspell.Type = eSpellType.Taunt.ToString();
            m_dbspell.Value = m_tauntValue;
            m_dbspell.Duration = 0;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 0;
            m_dbspell.RecastDelay = GetReUseDelay(0); // Spell code is responsible for disabling this ability and will use this value.
            m_dbspell.Range = m_range;
            m_dbspell.Description = "Taunt all enemies in a " 
                                               + m_radius + " unit radius.";
			m_spell = new Spell(m_dbspell, caster.Level);
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			GamePlayer caster = living as GamePlayer;
			if (caster == null)
				return;
			m_tauntValue = GetTauntValue();

			CreateSpell(caster);

			foreach (GamePlayer pc in caster.GetPlayersInRadius(m_range))
			{
				CastSpellOn(pc, caster);
			}

            foreach (GameNPC npc in caster.GetNPCsInRadius(m_range))
            {
	            CastSpellOn(npc, caster);
            }

            // We do not need to handle disabling the skill here. This ability casts a spell and is linked to that spell.
            // The spell casting code will disable this ability in SpellHandler's FinishSpellcast().
		}

        private int GetTauntValue()
        {
	        switch (Level)
	        {
		        case 1: return 100;
		        case 2: return 200;
		        case 3: return 300;
		        default: return 100;
	        }
        }
        
        public void CastSpellOn(GameLiving target, GamePlayer caster)
        {
	        if (target.IsAlive && m_spell != null)
	        {
		        ISpellHandler dd = ScriptMgr.CreateSpellHandler(caster, m_spell, m_spellline);
		        dd.StartSpell(target);
	        }
        }
	}
}
