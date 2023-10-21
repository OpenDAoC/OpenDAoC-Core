using System.Collections.Generic;
using Core.Database;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities
{
	public class NfRaAngerOfTheGodsAbility : TimedRealmAbility
	{
        private DbSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;
        private double m_damage = 0;
        private GamePlayer m_player;

        public NfRaAngerOfTheGodsAbility(DbAbility dba, int level) : base(dba, level) {}
        public virtual void CreateSpell(double damage)
        {
            m_dbspell = new DbSpell();
            m_dbspell.Name = SpellName;
            m_dbspell.Icon = 7141;
            m_dbspell.ClientEffect = 7141;
            m_dbspell.Damage = damage;
            m_dbspell.DamageType = 0;
            m_dbspell.Target = "Group";
            m_dbspell.Radius = 0;
            m_dbspell.Type = ESpellType.DamageAdd.ToString();
            m_dbspell.Value = 0;
            m_dbspell.Duration = 30;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
			m_dbspell.EffectGroup = 99999; // stacks with other damage adds
            m_dbspell.Range = 1000;
            m_spell = new Spell(m_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
		{
			if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
			m_player = living as GamePlayer;
            m_damage = GetDamageAddAmount();

            CreateSpell(m_damage);
			CastSpell(m_player);
			DisableSkill(living);
		}

        protected void CastSpell(GameLiving target)
        {
			if (target.IsAlive && m_spell != null)
			{
				ISpellHandler dd = ScriptMgr.CreateSpellHandler(m_player, m_spell, m_spellline);
				dd.IgnoreDamageCap = true;
				dd.StartSpell(target);
			}
        }	

        public override int GetReUseDelay(int level)
        {
            return 600;
        }

		public override void AddEffectsInfo(IList<string> list)
		{
			if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				list.Add("Level 1: Adds 10 DPS");
				list.Add("Level 2: Adds 15 DPS");
				list.Add("Level 3: Adds 20 DPS");
				list.Add("Level 4: Adds 25 DPS");
				list.Add("Level 5: Adds 30 DPS");
				list.Add("");
				list.Add("Target: Group");
				list.Add("Duration: 30 sec");
				list.Add("Casting time: instant");				
			}
			else
			{
				list.Add("Level 1: Adds 10 DPS");
				list.Add("Level 2: Adds 20 DPS");
				list.Add("Level 3: Adds 30 DPS");
				list.Add("");
				list.Add("Target: Group");
				list.Add("Duration: 30 sec");
				list.Add("Casting time: instant");				
			}
		}

        protected virtual string SpellName
        {
            get { return "Anger of the Gods"; }
        }

        protected virtual double GetDamageAddAmount()
        {
            if (ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
            {
                switch (Level)
                {
                    case 1: return 10.0;
                    case 2: return 15.0;
                    case 3: return 20.0;
                    case 4: return 25.0;
                    case 5: return 30.0;
                }
            }
            else
            {
                switch (Level)
                {
                    case 1: return 10.0;
                    case 2: return 20.0;
                    case 3: return 30.0;
                }
            }
            return 0;
        }
	}
}
