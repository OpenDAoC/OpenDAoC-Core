using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{

    public class AtlasOF_BatteryOfLife : TimedRealmAbility
    {
        public AtlasOF_BatteryOfLife(DbAbility dba, int level) : base(dba, level) { }
        
        int m_duration = 0; // 30s

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 10; }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
                return;

            GamePlayer player = living as GamePlayer;

            SendCasterSpellEffectAndCastMessage(player, 7009, true);
            DisableSkill(player);

            new AtlasOF_BatteryOfLifeECSEffect(new ECSGameEffectInitParams(player, m_duration, 1, CreateSpell(player)));
        }
        
        private SpellHandler CreateSpell(GameLiving owner)
        {
            DbSpell tmpSpell = new DbSpell();
            tmpSpell.Name = "Battery Of Life";
            tmpSpell.Icon = 4274;
            tmpSpell.ClientEffect = 7009;
            tmpSpell.Damage = 0;
            tmpSpell.Concentration = 1;
            tmpSpell.DamageType = 0;
            tmpSpell.Target = "Self";
            tmpSpell.Radius = 0;
            tmpSpell.Type = eSpellType.DefensiveProc.ToString();
            tmpSpell.Value = 0;
            tmpSpell.Duration = 0;
            tmpSpell.Pulse = 0;
            tmpSpell.PulsePower = 0;
            tmpSpell.Power = 0;
            tmpSpell.CastTime = 0;
            tmpSpell.EffectGroup = 0;
            tmpSpell.Range = 0;
            tmpSpell.Frequency = 500;
            tmpSpell.Description = "Creates a 1000HP buffer that is distributed to groupmembers within 1500 units as healing. Healing priority matches spreadheal.";
            SpellLine spellLine = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
            SpellHandler tmpHandler = new SpellHandler(owner, new Spell(tmpSpell, 0) , spellLine); // make spell level 0 so it bypasses the spec level adjustment code
            return tmpHandler;
        }
        
        public override IList<string> DelveInfo
        {
            get
            {
                var delveInfoList = new List<string>();
                delveInfoList.Add("Creates a 1000HP buffer that is distributed to groupmembers within 1500 units as healing. Healing priority matches spreadheal.");
                delveInfoList.Add("Casting time: instant");

                return delveInfoList;
            }
        }
        
        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Creates a 1000HP buffer that is distributed to groupmembers within 1500 units as healing. Healing priority matches spreadheal.");
            list.Add("Casting time: instant");
        }
    }
}