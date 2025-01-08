using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_Vanish : TimedRealmAbility
    {
        public AtlasOF_Vanish(DbAbility dba, int level) : base(dba, level) { }
        
        int m_duration = 30000; // 30s

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 10; }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins

        public override void Execute(GameLiving living)
        {
            GamePlayer player = living as GamePlayer;
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

            DisableSkill(living);

            new AtlasOF_VanishECSEffect(new ECSGameEffectInitParams(player, m_duration, 1, CreateSpell(living)));
        }
        
        private SpellHandler CreateSpell(GameLiving owner)
        {
            DbSpell tmpSpell = new DbSpell();
            tmpSpell.Name = "Vanish";
            tmpSpell.Icon = 4280;
            tmpSpell.ClientEffect = 3019;
            tmpSpell.Damage = 0;
            tmpSpell.DamageType = 0;
            tmpSpell.Target = "Self";
            tmpSpell.Radius = 0;
            tmpSpell.Type = eSpellType.OffensiveProc.ToString();
            tmpSpell.Value = 0;
            tmpSpell.Duration = 30;
            tmpSpell.Pulse = 0;
            tmpSpell.PulsePower = 0;
            tmpSpell.Power = 0;
            tmpSpell.CastTime = 0;
            tmpSpell.EffectGroup = 0; // stacks with other damage adds
            tmpSpell.Range = 0;
            tmpSpell.Description = "You will immediately hide, regardless of action state or in-combat timer.";
            SpellLine spellLine = GlobalSpellsLines.RealmSpellsSpellLine;
            return ScriptMgr.CreateSpellHandler(owner, new Spell(tmpSpell, 0) , spellLine) as SpellHandler;
        }

        public override IList<string> DelveInfo
        {
            get
            {
                var delveInfoList = new List<string>();
                delveInfoList.Add("You will immediately hide, regardless of action state or in-combat timer.");
                delveInfoList.Add("Casting time: instant");

                return delveInfoList;
            }
        }
        
        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("You will immediately hide, regardless of action state or in-combat timer.");
            list.Add("Casting time: instant");
        }
    }
}