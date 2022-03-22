using System;
using System.Collections;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.Spells;
using System.Collections.Generic;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Fury of the Gods realm ability
	/// </summary>
	public class AtlasOF_FuryOfTheGods : AngerOfTheGodsAbility
	{
		public AtlasOF_FuryOfTheGods(DBAbility dba, int level) : base(dba, level) { }

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

        protected override double GetDamageAddAmount() { return 7.0; }
	}
}