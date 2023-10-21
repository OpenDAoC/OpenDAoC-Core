using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities
{
	public class NfRaDualThreatAbility : L3RaPropertyEnhancer
	{
		public NfRaDualThreatAbility(DbAbility dba, int level)
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
}