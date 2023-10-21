using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities
{
	public class NfRaSerenityAbility : RaPropertyEnhancer
	{
		public NfRaSerenityAbility(DbAbility dba, int level) : base(dba, level, EProperty.PowerRegenerationRate) { }

		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;
			switch (level)
			{
				case 1: return 1;
				case 2: return 2;
				case 3: return 3;
				case 4: return 5;
				case 5: return 7;
				default: return 7;
			}
		}
	}
}