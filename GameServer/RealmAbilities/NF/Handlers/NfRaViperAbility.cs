using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class NfRaViperAbility : L3RaPropertyEnhancer
{
    public NfRaViperAbility(DbAbility dba, int level)
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