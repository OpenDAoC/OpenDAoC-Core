using DOL.Database;
using System.Collections.Generic;
using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{

    public class AtlasOF_MajesticWill : TimedRealmAbility
    {
        public AtlasOF_MajesticWill(DBAbility dba, int level) : base(dba, level) { }
        
        int m_duration = 60000; // 30s

        public override int MaxLevel { get { return 3; } }
        public override int CostForUpgrade(int level) {
            switch (level)
            {
                case 1: return 6;
                case 2: return 10;
                case 3: return 14;
                default: return 6;
            }
        }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins

        public override void Execute(GameLiving living)
        {
            GamePlayer player = living as GamePlayer;
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

            DisableSkill(living);

            new AtlasOF_MajesticWillECSEffect(new ECSGameEffectInitParams(player, m_duration, Level, CreateSpell(living)));
        }
        
        private SpellHandler CreateSpell(GameLiving owner)
        {
            DBSpell tmpSpell = new DBSpell();
            tmpSpell.Name = "Majestic Will";
            tmpSpell.Icon = 7065;
            tmpSpell.ClientEffect = 7065;
            tmpSpell.Damage = 0;
            tmpSpell.DamageType = 0;
            tmpSpell.Target = "Self";
            tmpSpell.Radius = 0;
            tmpSpell.Type = eSpellType.OffensiveProc.ToString();
            tmpSpell.Value = 0;
            tmpSpell.Duration = 60;
            tmpSpell.Pulse = 0;
            tmpSpell.PulsePower = 0;
            tmpSpell.Power = 0;
            tmpSpell.CastTime = 0;
            tmpSpell.EffectGroup = 0; // stacks with other damage adds
            tmpSpell.Range = 0;
            tmpSpell.Description = "Your targets chance of resisting your spells is reduced by 5% per level per level of this ability for 60 seconds.";
            SpellLine spellLine = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
            SpellHandler tmpHandler = new SpellHandler(owner, new Spell(tmpSpell, 0) , spellLine); // make spell level 0 so it bypasses the spec level adjustment code
            return tmpHandler;
        }
        
        public override IList<string> DelveInfo
        {
            get
            {
                var delveInfoList = new List<string>();
                delveInfoList.Add("Your targets chance of resisting your spells is reduced by 5% per level per level of this ability for 60 seconds.");
                delveInfoList.Add("Casting time: instant");

                return delveInfoList;
            }
        }
        
        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Your targets chance of resisting your spells is reduced by 5% per level per level of this ability for 60 seconds.");
            list.Add("Casting time: instant");
        }
    }
}