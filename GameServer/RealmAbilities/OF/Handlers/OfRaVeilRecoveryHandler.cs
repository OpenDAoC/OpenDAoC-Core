using Core.GS.RealmAbilities;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class OfRaVeilRecoveryHandler : RaPropertyEnhancer
	{
		public OfRaVeilRecoveryHandler(DbAbilities dba, int level)
			: base(dba, level, EProperty.ResIllnessReduction)
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
		
		public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
	}
}