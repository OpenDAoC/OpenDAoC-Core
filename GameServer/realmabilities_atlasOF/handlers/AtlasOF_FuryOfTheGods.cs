using DOL.Database;
using System;
using System.Collections.Generic;

namespace DOL.GS.RealmAbilities
{
    public class AtlasOF_FuryOfTheGods : AngerOfTheGodsAbility
    {
        public AtlasOF_FuryOfTheGods(DbAbility dba, int level) : base(dba, level) { }

        protected override string SpellName => "Fury of the Gods";

        public override int MaxLevel => 1;

        public override int CostForUpgrade(int level)
        {
            return 14;
        }

        public override int GetReUseDelay(int level)
        {
            return 1800;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add($"Value: {Math.Abs(GetDamageAddAmount())}%");
            list.Add("Target: Group");
            list.Add("Duration: 30 sec");
            list.Add("Casting time: instant");
        }

        protected override double GetDamageAddAmount()
        {
            return -30.0;
        }
    }
}
