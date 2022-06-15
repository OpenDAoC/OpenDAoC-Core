using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.Language;


namespace DOL.GS.RealmAbilities
{
	public class AtlasOF_RAStrengthEnhancer : RAStrengthEnhancer
	{
		public AtlasOF_RAStrengthEnhancer(DBAbility dba, int level) : base(dba, level) { }
		public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }
        public override int GetAmountForLevel(int level) { return AtlasRAHelpers.GetStatEnhancerAmountForLevel(level); }
    }

	public class AtlasOF_RAConstitutionEnhancer : RAConstitutionEnhancer
	{
		public AtlasOF_RAConstitutionEnhancer(DBAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }
        public override int GetAmountForLevel(int level) { return AtlasRAHelpers.GetStatEnhancerAmountForLevel(level); }
    }

	public class AtlasOF_RAQuicknessEnhancer : RAQuicknessEnhancer
	{
		public AtlasOF_RAQuicknessEnhancer(DBAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }
        public override int GetAmountForLevel(int level) { return AtlasRAHelpers.GetStatEnhancerAmountForLevel(level); }
    }

	public class AtlasOF_RADexterityEnhancer : RADexterityEnhancer
	{
		public AtlasOF_RADexterityEnhancer(DBAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }
        public override int GetAmountForLevel(int level) { return AtlasRAHelpers.GetStatEnhancerAmountForLevel(level); }
    }

	public class AtlasOF_RAAcuityEnhancer : RAAcuityEnhancer
	{
		public AtlasOF_RAAcuityEnhancer(DBAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }
        public override int GetAmountForLevel(int level) { return AtlasRAHelpers.GetStatEnhancerAmountForLevel(level); }
    }

	public class AtlasOF_RAMaxManaEnhancer : RAMaxManaEnhancer
	{
		public AtlasOF_RAMaxManaEnhancer(DBAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }
        public override int GetAmountForLevel(int level) { return AtlasRAHelpers.GetPropertyEnhancer3AmountForLevel(level); }
    }

	public class AtlasOF_RAMaxHealthEnhancer : RAMaxHealthEnhancer
	{
		public AtlasOF_RAMaxHealthEnhancer(DBAbility dba, int level) : base(dba, level) { }
        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }
        public override int GetAmountForLevel(int level) { return AtlasRAHelpers.GetPropertyEnhancer3AmountForLevel(level); }
        public override bool CheckRequirement(GamePlayer player) { return true; } // Override NF level 40 requirement.
    }
	
	public class AtlasOF_RAEndRegenEnhancer : RAEndRegenEnhancer
	{
		public AtlasOF_RAEndRegenEnhancer(DBAbility dba, int level) : base(dba, level) { }
		
		public override int MaxLevel
		{
			get
			{
				return 1;
			}
		}
		public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }
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