using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class NfRaLongWindAbility : RaPropertyEnhancer
{
	public NfRaLongWindAbility(DbAbility dba, int level) : base(dba, level, EProperty.Undefined) { }

    protected override string ValueUnit { get { return "%"; } }

	public override int GetAmountForLevel(int level)
	{
        //return level;
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