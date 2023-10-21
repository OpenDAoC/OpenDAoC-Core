using Core.Database;

namespace Core.GS.RealmAbilities
{
	public class NfRaDeterminationAbility : RaPropertyEnhancer
	{
		public static EProperty[] properties = new EProperty[]
		{
			EProperty.MesmerizeDurationReduction,
			EProperty.StunDurationReduction,
			EProperty.SpeedDecreaseDurationReduction,
		};
		public NfRaDeterminationAbility(DbAbility dba, int level) : base(dba, level, properties) { }

		public override int MaxLevel
			{
				get { return 5; }
			}

		protected override string ValueUnit { get { return "%"; } }

		public override int CostForUpgrade(int level)
		{
			switch (level)
			{
				case 0: return 0;
				case 1: return 1;
				case 2: return 3;
				case 3: return 6;
				case 4: return 10;
				case 5: return 14;
				default: return 1000;
			}
		}


		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;

			int amount = 0;
			if (level == 1)
				amount += 15;
			if (level >= 2)
				amount += 15;
			if (level >= 3)
				amount += 15;
			if (level >= 4)
				amount += 15;
			if (level >= 5)
				amount += 15;
			return amount;
		}
	}
}