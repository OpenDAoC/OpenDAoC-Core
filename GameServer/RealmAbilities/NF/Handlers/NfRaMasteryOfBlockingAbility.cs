using Core.Database;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities
{
    public class NfRaMasteryOfBlockingAbility : RaPropertyEnhancer
    {
        public NfRaMasteryOfBlockingAbility(DbAbility dba, int level)
            : base(dba, level, EProperty.BlockChance)
        {
        }

        protected override string ValueUnit { get { return "%"; } }

        public override int GetAmountForLevel(int level)
        {
            if (level < 1) return 0;
            if (ServerProperties.Properties.USE_NEW_PASSIVES_RAS_SCALING)
            {
                switch (level)
                {
                    case 1: return 2;
                    case 2: return 4;
                    case 3: return 6;
                    case 4: return 9;
                    case 5: return 12;
                    case 6: return 15;
                    case 7: return 18;
                    case 8: return 21;
                    case 9: return 25;
                    default: return 25;
                }
            }
            else
            {
                switch (level)
                {
                    case 1: return 2;
                    case 2: return 5;
                    case 3: return 10;
                    case 4: return 16;
                    case 5: return 23;
                    default: return 23;
                }
            }
        }
    }
}