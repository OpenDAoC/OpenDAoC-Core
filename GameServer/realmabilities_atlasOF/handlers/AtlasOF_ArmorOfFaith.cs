using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_ArmorOfFaith : TimedRealmAbility
    {
        public AtlasOF_ArmorOfFaith(DbAbility dba, int level) : base(dba, level) { }

        public const int duration = 60000; // 60 seconds
        public override int MaxLevel { get { return 3; } }
        public override int GetReUseDelay(int level) { return 900; } // 15 mins
        public override bool CheckRequirement(GamePlayer player) { return AtlasRAHelpers.GetAugConLevel(player) >= 3; }
        public override int CostForUpgrade(int currentLevel) { return AtlasRAHelpers.GetCommonUpgradeCostFor3LevelsRA(currentLevel); } 
        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Target: Self");
            list.Add("Duration: 60 sec");
            list.Add("Casting time: instant");
        }
        
        private DbSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;
        private double m_value = 0;
        private GamePlayer m_player;

        public virtual void CreateSpell()
        {
            m_dbspell = new DbSpell();
            m_dbspell.Name = "Armor Of Faith";
            m_dbspell.Icon = 7118;
            m_dbspell.ClientEffect = 7118;
            m_dbspell.Damage = 0;
            m_dbspell.DamageType = 11;
            m_dbspell.Target = "Self";
            m_dbspell.Radius = 0;
            m_dbspell.Type = eSpellType.SpecArmorFactorBuff.ToString();
            m_dbspell.Value = m_value;
            m_dbspell.Duration = 60;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 0; // stacks with other damage adds
            m_dbspell.Range = 0;
            m_spell = new Spell(m_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
            m_spellline = GlobalSpellsLines.RealmSpellsSpellLine;
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
            if (living is GamePlayer p)
            {
                m_player = p;
            }

            m_value = Level * 50;
            CreateSpell();
            CastSpell(living);
            DisableSkill(living);
        }

        protected void CastSpell(GameLiving target)
        {
            if (target.IsAlive && m_spell != null)
            {
                ISpellHandler dd = ScriptMgr.CreateSpellHandler(target, m_spell, m_spellline);
                dd.StartSpell(target);
            }
        }
    }

}
