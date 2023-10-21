using Core.Database;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities
{
    public class OfRaThornweedFieldAbility : NfRaThornweedFieldAbility
    {
        public OfRaThornweedFieldAbility(DbAbility dba, int level) : base(dba, level) { }

        public override int MaxLevel { get { return 1; } }
        public override int CostForUpgrade(int level) { return 14; }
        public override int GetReUseDelay(int level) { return 1800; } // 30 mins
    }
}