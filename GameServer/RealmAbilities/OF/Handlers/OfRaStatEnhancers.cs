using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class OfRaStrengthEnhancerAbility : NfRaStrengthEnhancer
	{
		public OfRaStrengthEnhancerAbility(DbAbility dba, int level) : base(dba, level) { }
		public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetStatEnhancerAmountForLevel(level); }
    }

	public class OfRaConstitutionEnhancerAbility : NfRaConstitutionEnhancer
	{
		public OfRaConstitutionEnhancerAbility(DbAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetStatEnhancerAmountForLevel(level); }
    }

	public class OfRaQuicknessEnhancerAbility : NfRaQuicknessEnhancer
	{
		public OfRaQuicknessEnhancerAbility(DbAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetStatEnhancerAmountForLevel(level); }
    }

	public class OfRaDexterityEnhancerAbility : NfRaDexterityEnhancer
	{
		public OfRaDexterityEnhancerAbility(DbAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetStatEnhancerAmountForLevel(level); }
    }

	public class OfRaAcuityEnhancerAbility : NfRaAcuityEnhancer
	{
		public OfRaAcuityEnhancerAbility(DbAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetStatEnhancerAmountForLevel(level); }
    }

	public class OfRaMaxPowerEnhancerAbility : NfRaMaxPowerEnhancer
	{
		public OfRaMaxPowerEnhancerAbility(DbAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }
    }

	public class OfRaMaxHealthEnhancerAbility : NfRaMaxHealthEnhancer
	{
		public OfRaMaxHealthEnhancerAbility(DbAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }
        public override bool CheckRequirement(GamePlayer player) { return true; } // Override NF level 40 requirement.
    }
	
	public class OfRAEndRegenEnhancerAbility : NfRaEndRegenEnhancer
	{
		public OfRAEndRegenEnhancerAbility(DbAbility dba, int level) : base(dba, level) { }
		
		public override int MaxLevel
		{
			get
			{
				return 1;
			}
		}
		public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
		public override int GetAmountForLevel(int level) { return 1; }
		public override bool CheckRequirement(GamePlayer player) { return true; } // Override NF level 40 requirement.

		public override IList<string> DelveInfo { 			
			get
			{
				var delveInfoList = new List<string>();
				delveInfoList.Add(m_description);

				return delveInfoList;
			}
			
		}
	}
}