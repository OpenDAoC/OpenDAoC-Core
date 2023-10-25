using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Server;

namespace Core.GS.RealmAbilities;

public class NfRaWildHealingAbility : RaPropertyEnhancer
{
	public NfRaWildHealingAbility(DbAbility dba, int level)
		: base(dba, level, EProperty.CriticalHealHitChance)
	{
	}

	protected override string ValueUnit { get { return "%"; } }

	public override int GetAmountForLevel(int level)
	{
		if (level < 1) return 0;
		if (ServerProperty.USE_NEW_PASSIVES_RAS_SCALING)
		{
			switch (level)
			{
					case 1: return 3;
					case 2: return 6;
					case 3: return 9;
					case 4: return 13;
					case 5: return 17;
					case 6: return 22;
					case 7: return 27;
					case 8: return 33;
					case 9: return 39;
					default: return 39;
			}
		}
		else
		{
			switch (level)
			{
					case 1: return 3;
					case 2: return 9;
					case 3: return 17;
					case 4: return 27;
					case 5: return 39;
					default: return 39;
			}
		}
	}
}