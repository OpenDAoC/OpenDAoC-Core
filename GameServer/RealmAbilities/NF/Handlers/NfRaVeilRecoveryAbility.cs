using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Server;

namespace Core.GS.RealmAbilities;

public class NfRaVeilRecoveryAbility : RaPropertyEnhancer
{
	public NfRaVeilRecoveryAbility(DbAbility dba, int level)
		: base(dba, level, EProperty.ResIllnessReduction)
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
                case 1: return 10;
                case 2: return 15;
                case 3: return 20;
                case 4: return 30;
                case 5: return 40;
                case 6: return 50;
                case 7: return 60;
                case 8: return 70;
                case 9: return 80;
                default: return 80;
            }
        }
        else
        {
            switch (level)
            {
                case 1: return 10;
                case 2: return 20;
                case 3: return 40;
                case 4: return 60;
                case 5: return 80;
                default: return 0;
            }
        }
	}
}