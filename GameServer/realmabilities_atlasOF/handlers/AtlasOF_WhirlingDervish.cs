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
    public class AtlasOF_WhirlingDervish : TimedRealmAbility
    {
        public AtlasOF_WhirlingDervish(DbAbility dba, int level) : base(dba, level) { }

        public const int duration = 60000; // 60 seconds
        public override int MaxLevel { get { return 3; } }
        public override int GetReUseDelay(int level) { return 900; } // 15 mins
        public override bool CheckRequirement(GamePlayer player) { return AtlasRAHelpers.GetAugDexLevel(player) >= 3; }
        public override int CostForUpgrade(int currentLevel) { return AtlasRAHelpers.GetCommonUpgradeCostFor3LevelsRA(currentLevel); }
        
        private DbSpell m_dbspell;
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
        
        public virtual void CreateSpell()
        {
            new AtlasOF_WhirlingDervishECSEffect(new ECSGameEffectInitParams(m_player, duration, Level));
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;
            if (living is GamePlayer p)
                m_player = p;

            CreateSpell();
            DisableSkill(living);
        }
    }

}
