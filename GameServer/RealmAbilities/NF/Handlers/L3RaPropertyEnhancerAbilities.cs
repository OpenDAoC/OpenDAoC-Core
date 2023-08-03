using Core.GS.RealmAbilities;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class NfRaDualThreatAbility : L3RaPropertyEnhancer
	{
		public NfRaDualThreatAbility(DbAbilities dba, int level)
			: base(dba, level, new EProperty[] { EProperty.CriticalMeleeHitChance, EProperty.CriticalSpellHitChance, EProperty.CriticalHealHitChance})
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
				case 1: return 3;
				case 2: return 6;
				case 3: return 9;
				case 4: return 12;
				case 5: return 15;
				default: return 0;
			}
		}
	}

	public class XNfRaReflexAttackAbility : L3RaPropertyEnhancer
	{
		public XNfRaReflexAttackAbility(DbAbilities dba, int level)
			: base(dba, level, EProperty.Undefined)
		{
		}

		protected override string ValueUnit { get { return "%"; } }

		public override int GetAmountForLevel(int level)
		{
			if(ServerProperties.ServerProperties.USE_NEW_ACTIVES_RAS_SCALING)
			{
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
			else
			{
				switch (level)
				{
					case 1: return 5;
					case 2: return 15;
					case 3: return 30;
					default: return 0;
				}
			}
		}
	}

	public class NfRaViperAbility : L3RaPropertyEnhancer
	{
		public NfRaViperAbility(DbAbilities dba, int level)
			: base(dba, level, EProperty.Undefined)
		{
		}

		protected override string ValueUnit { get { return "%"; } }

		public override int GetAmountForLevel(int level)
		{
			if(ServerProperties.ServerProperties.USE_NEW_ACTIVES_RAS_SCALING)
			{
				switch (level)
				{
					case 1: return 25;
					case 2: return 35;
					case 3: return 50;
					case 4: return 75;
					case 5: return 100;
					default: return 0;
				}
			}
			else
			{
				switch (level)
				{
					case 1: return 25;
					case 2: return 50;
					case 3: return 100;
					default: return 0;
				}
			}
		}
	}
}