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
    public class AtlasOF_RainOfAnnihilation : TimedRealmAbility
    {
        public AtlasOF_RainOfAnnihilation(DBAbility dba, int level) : base(dba, level) { }

        public const int duration = 60000; // 60 seconds
        public override int MaxLevel { get { return 3; } }
        public override int GetReUseDelay(int level) { return 900; } // 15 mins
        public override int CostForUpgrade(int level) {
            switch (level)
            {
                case 1: return 3;
                case 2: return 6;
                case 3: return 10;
                default: return 3;
            }
        }
        
        private DBSpell m_dbspell;
        private Spell m_spell = null;
        private SpellLine m_spellline;
        private double m_damage = 0;
        private GamePlayer m_player;

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Target: Self");
            list.Add("Duration: 60 sec");
            list.Add("Casting time: instant");
        }
        
        public virtual void CreateSpell(double damage)
        {
            m_dbspell = new DBSpell();
            m_dbspell.Name = "Rain Of Annihilation";
            m_dbspell.Icon = 7127;
            m_dbspell.ClientEffect = 7127;
            m_dbspell.Damage = damage;
            m_dbspell.DamageType = 15;
            m_dbspell.Target = "Self";
            m_dbspell.Radius = 0;
            m_dbspell.Type = eSpellType.DamageAdd.ToString();
            m_dbspell.Value = 0;
            m_dbspell.Duration = 60;
            m_dbspell.Pulse = 0;
            m_dbspell.PulsePower = 0;
            m_dbspell.Power = 0;
            m_dbspell.CastTime = 0;
            m_dbspell.EffectGroup = 99999; // stacks with other damage adds
            m_dbspell.Range = 0;
            m_spell = new Spell(m_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
            m_spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
            if (living is GamePlayer p)
                m_player = p;
            
            m_damage = GetDamageAddAmount(m_player);

            CreateSpell(m_damage);
           
            ISpellHandler dd = ScriptMgr.CreateSpellHandler(living, m_spell, m_spellline);
            dd.IgnoreDamageCap = true;

            new AtlasOF_RainOfIceECSEffect(new ECSGameEffectInitParams(living, 60000, 1, dd));
            
            DisableSkill(living);
        }
        
        protected virtual double GetDamageAddAmount(GameLiving caster)
        {
            if (caster == null)
                return 0;
            
            // double damage = caster.AttackWeapon.DPS_AF * caster.AttackWeapon.SPD_ABS * .1 * .1;
            switch (Level)
            {
                case 1: return 1.10;
                case 2: return 1.20;
                case 3: return 1.30;
            }

            return 0;
        }
        
        protected void CastSpell(GameLiving target)
        {
            if (target.IsAlive && m_spell != null)
            {
                ISpellHandler dd = ScriptMgr.CreateSpellHandler(target, m_spell, m_spellline);
                dd.IgnoreDamageCap = true;
                dd.StartSpell(target);
            }
        }
    }

}
