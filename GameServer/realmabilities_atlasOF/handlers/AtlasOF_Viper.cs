using DOL.Database;
using System.Collections.Generic;
using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{

    public class AtlasOF_Viper : TimedRealmAbility
    {
        public AtlasOF_Viper(DBAbility dba, int level) : base(dba, level) { }
        
        int m_duration = 30000; // 30s

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 14; }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins

        public override void Execute(GameLiving living)
        {
            GamePlayer player = living as GamePlayer;
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED)) return;

            DisableSkill(living);

            new AtlasOF_ViperECSEffect(new ECSGameEffectInitParams(player, m_duration, 1, CreateSpell(living)));
        }
        
        private SpellHandler CreateSpell(GameLiving owner)
        {
            DBSpell tmpSpell = new DBSpell();
            tmpSpell.Name = "Viper";
            tmpSpell.Icon = 7013;
            tmpSpell.ClientEffect = 7013;
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
            tmpSpell.Description = "Your damage-over-time poisons will deal double damage.";
            SpellLine spellLine = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
            SpellHandler tmpHandler = new SpellHandler(owner, new Spell(tmpSpell, 0) , spellLine); // make spell level 0 so it bypasses the spec level adjustment code
            return tmpHandler;
        }
        
        public override IList<string> DelveInfo
        {
            get
            {
                var delveInfoList = new List<string>();
                delveInfoList.Add("Atlas Viper Effect");

                return delveInfoList;
            }
        }
        
        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Damage-Over-Time effects will deal double damage for 30 seconds");
            list.Add("Target: ");
            list.Add("Casting time: instant");
        }
    }
}