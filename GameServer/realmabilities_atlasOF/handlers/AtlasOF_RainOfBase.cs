using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{
    public abstract class AtlasOF_RainOfBase : TimedRealmAbility
    {
        public const int duration = 60000; // 60 seconds

        protected int m_clientEffect;
        protected int m_damageType;
        private Spell m_spell;
        private SpellLine m_spellline;

        public AtlasOF_RainOfBase(DbAbility ability, int level) : base(ability, level) { }

        public override int MaxLevel => 3;

        public override int GetReUseDelay(int level) { return 900; } // 15 mins

        public override int CostForUpgrade(int currentLevel) { return AtlasRAHelpers.GetCommonUpgradeCostFor3LevelsRA(currentLevel); }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Target: Self");
            list.Add("Duration: 60 sec");
            list.Add("Casting time: instant");
        }
        
        protected virtual void Execute(string name, int icon, int clientEffect, int damageType, GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
                return;

            if (living is GamePlayer)
                CreateSpell(name, icon, clientEffect, damageType, GetDamageAddAmount(living));

            ISpellHandler spellHandler = ScriptMgr.CreateSpellHandler(living, m_spell, m_spellline);
            new AtlasOF_RainOfBaseECSEffect(new ECSGameEffectInitParams(living, duration, 1, spellHandler));
            DisableSkill(living);
        }

        private void CreateSpell(string name, int icon, int clientEffect, int damageType, double damage)
        {
            DbSpell m_dbspell = new DbSpell
            {
                Name = name,
                Icon = icon,
                ClientEffect = clientEffect,
                Damage = damage,
                DamageType = damageType,
                Target = "Self",
                Radius = 0,
                Type = eSpellType.DamageAdd.ToString(),
                Value = 0,
                Duration = duration / 1000,
                Pulse = 0,
                PulsePower = 0,
                Power = 0,
                CastTime = 0,
                EffectGroup = 99999, // stacks with other damage adds
                Range = 0
            };
            m_spell = new Spell(m_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
            m_spellline = GlobalSpellsLines.RealmSpellsSpellLine;
        }
        
        private double GetDamageAddAmount(GameLiving caster)
        {
            if (caster == null)
                return 0;

            // Previously was "caster.AttackWeapon.DPS_AF * caster.AttackWeapon.SPD_ABS * .1 * .1 * Level * .1"
            return Level switch
            {
                1 => 10,
                2 => 20,
                3 => 30,
                _ => 0,
            };
        }
    }
}
