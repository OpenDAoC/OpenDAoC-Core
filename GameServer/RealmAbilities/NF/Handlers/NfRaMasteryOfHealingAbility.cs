using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Server;

namespace Core.GS.RealmAbilities;

public class NfRaMasteryOfHealingAbility : RaPropertyEnhancer
{
	public NfRaMasteryOfHealingAbility(DbAbility dba, int level)
		: base(dba, level, EProperty.HealingEffectiveness)
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
					case 1: return 2;
					case 2: return 4;
					case 3: return 6;
					case 4: return 9;
					case 5: return 12;
					case 6: return 16;
					case 7: return 20;
					case 8: return 25;
					case 9: return 30;
					default: return 30;
			}
		}
		else
		{
			switch (level)
			{
					case 1: return 2;
					case 2: return 5;
					case 3: return 12;
					case 4: return 19;
					case 5: return 28;
					default: return 28;
			}
		}
	}
}