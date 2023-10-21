using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class NfRaLifterAbility : RaPropertyEnhancer
	{
		public NfRaLifterAbility(DbAbility dba, int level)
			: base(dba, level, EProperty.Undefined)
		{
		}

		protected override string ValueUnit { get { return "%"; } }
		public override int CostForUpgrade(int level)
		{
			switch (level)
			{
				case 0: return 1;
				case 1: return 3;
				case 2: return 6;
				case 3: return 10;
				case 4: return 14;
				default: return 1000;
			}
		}
		public override int GetAmountForLevel(int level)
		{
            switch (level)
            {
                case 1: return 20;
                case 2: return 40;
                case 3: return 60;
                case 4: return 80;
                case 5: return 100;
                default: return 0;
            }
		}
	}
}