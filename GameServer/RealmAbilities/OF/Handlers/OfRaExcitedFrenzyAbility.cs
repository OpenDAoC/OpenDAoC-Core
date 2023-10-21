using System.Collections.Generic;
using Core.Database;
using Core.GS.Spells;

namespace Core.GS.RealmAbilities
{
    public class OfRaExcitedFrenzyAbility : TimedRealmAbility
    {
        public OfRaExcitedFrenzyAbility(DbAbility dba, int level) : base(dba, level) { }

        public const int duration = 60000; // 60 seconds
        public override int MaxLevel { get { return 1; } }
        public override int GetReUseDelay(int level) { return 600; } // 10 mins
        public override int CostForUpgrade(int level)
        {
            return 6;
        }
        
        private DbSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;
        private double m_damage = 0;
        private GamePlayer m_player;

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Target: Pet");
            list.Add("Duration: 30 sec");
            list.Add("Range: 1500 units");
            list.Add("Casting time: instant");
        }
        
        public virtual void CreateSpell()
        {
            m_dbspell = new DbSpell();
            m_dbspell.Name = "Excited Frenzy";
            m_dbspell.Icon = 4246;
            m_dbspell.ClientEffect = 4246;
            m_dbspell.Damage = 0;
            m_dbspell.DamageType = 0;
            m_dbspell.Target = "Pet";
            m_dbspell.Radius = 0;
            m_dbspell.Type = ESpellType.CombatSpeedBuff.ToString();
            m_dbspell.Value = 50;
            m_dbspell.Duration = 30;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 0;
            m_dbspell.Range = 1500;
            m_spell = new Spell(m_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
            if (living is GamePlayer p)
                m_player = p;
            
            CreateSpell();
            if (m_spell == null) return;

            foreach (GameNpc npcs in m_player.GetNPCsInRadius((ushort)m_spell.Range))
            {
                if (npcs is GameSummonedPet pet)
                {
                    if (pet.Owner == m_player)
                    {
                        ISpellHandler dd = ScriptMgr.CreateSpellHandler(m_player, m_spell, m_spellline);
                        dd.StartSpell(pet);
                    }
                }
            }

            DisableSkill(living);
        }
    }

}
