using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Server;

namespace Core.GS.RealmAbilities;

public class NfRaMasteryOfMageryAbility : RaPropertyEnhancer
{
	public NfRaMasteryOfMageryAbility(DbAbility dba, int level)
		: base(dba, level, EProperty.SpellDamage)
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
					case 2: return 3;
					case 3: return 4;
					case 4: return 6;
					case 5: return 8;
					case 6: return 10;
					case 7: return 12;
					case 8: return 14;
					case 9: return 16;
					default: return 16;
			}
		}
		else
		{
			switch (level)
			{
					case 1: return 2;
					case 2: return 4;
					case 3: return 7;
					case 4: return 11;
					case 5: return 15;
					default: return 15;
			}
		}
	}
}