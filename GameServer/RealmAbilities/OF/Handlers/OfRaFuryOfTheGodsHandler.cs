using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Fury of the Gods realm ability
	/// </summary>
	public class OfRaFuryOfTheGodsHandler : NfRaAngerOfTheGodsHandler
	{
		public OfRaFuryOfTheGodsHandler(DbAbilities dba, int level) : base(dba, level) { }

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