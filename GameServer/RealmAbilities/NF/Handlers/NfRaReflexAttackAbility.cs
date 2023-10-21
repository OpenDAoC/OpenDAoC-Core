using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class NfRaReflexAttackAbility : L3RaPropertyEnhancer
{
	public NfRaReflexAttackAbility(DbAbility dba, int level)
		: base(dba, level, EProperty.Undefined)
	{
	}

	protected override string ValueUnit { get { return "%"; } }

	public override int GetAmountForLevel(int level)
	{
		if(ServerProperties.Properties.USE_NEW_ACTIVES_RAS_SCALING)
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