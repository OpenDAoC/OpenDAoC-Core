using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Database;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_HailOfBlows : TimedRealmAbility
    {
        public AtlasOF_HailOfBlows(DBAbility dba, int level) : base(dba, level) { }

        public const int duration = 60000; // 60 seconds
        public override int MaxLevel { get { return 3; } }
        public override int GetReUseDelay(int level) { return 900; } // 15 mins
        public override bool CheckRequirement(GamePlayer player) { return AtlasRAHelpers.HasAugDexLevel(player, 3); }
        
        public override int CostForUpgrade(int level)
        {
            return level switch
            {
                1 => 6,
                2 => 10,
                _ => 3
            };
        }
        
        private DBSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;
        private double m_hasteValue = 0;
        
        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Target: Self");
            list.Add("Duration: 60 sec");
            list.Add("Casting time: instant");
        }

        public virtual void CreateSpell(double damage)
        {
            m_dbspell = new DBSpell();
            m_dbspell.Name = "Hail Of Blows";
            m_dbspell.Icon = 4240;
            m_dbspell.ClientEffect = 1692;
            m_dbspell.Damage = 0;
            m_dbspell.DamageType = 0;
            m_dbspell.Target = "Self";
            m_dbspell.Radius = 0;
            m_dbspell.Type = eSpellType.CelerityBuff.ToString();
            m_dbspell.Value = m_hasteValue;
            m_dbspell.Duration = 60;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 100;
            m_dbspell.Range = 0;
            m_spell = new Spell(m_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
            if (living is GamePlayer p)
                m_hasteValue = GetHasteValue();

            Console.WriteLine($"haste buff of {m_hasteValue} applied");
            CreateSpell(m_hasteValue);
            CastSpell(living);
            DisableSkill(living);
        }
        
        protected virtual double GetHasteValue()
        {
            return 5 * Level;
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
