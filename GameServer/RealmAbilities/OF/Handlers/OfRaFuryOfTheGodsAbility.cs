using System.Collections.Generic;
using Core.Database;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities
{
	public class OfRaFuryOfTheGodsAbility : NfRaAngerOfTheGodsAbility
	{
		public OfRaFuryOfTheGodsAbility(DbAbility dba, int level) : base(dba, level) { }

        protected override string SpellName { get { return "Fury of the Gods"; } }
        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 14; }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add(string.Format("Value: {0} dps", GetDamageAddAmount()));
            list.Add("Target: Group");
            list.Add("Duration: 30 sec");
            list.Add("Casting time: instant");
        }

        protected override double GetDamageAddAmount() { return 30.0; }
	}
}