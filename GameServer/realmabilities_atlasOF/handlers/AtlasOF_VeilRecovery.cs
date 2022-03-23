using System;
using DOL.Database;
using DOL.GS.PropertyCalc;

namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_VeilRecovery : RAPropertyEnhancer
	{
		public AtlasOF_VeilRecovery(DBAbility dba, int level)
			: base(dba, level, eProperty.ResIllnessReduction)
		{
		}

		protected override string ValueUnit { get { return "%"; } }

		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;
			switch (level)
            {
                case 1: return 10;
                case 2: return 20;
                case 3: return 30;
                case 4: return 40;
                case 5: return 50;
                default: return 0;
            }
		}
		
		public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }
	}
}