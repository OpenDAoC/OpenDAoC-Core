using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    public class OfRaRegenerationAbility : RaPropertyEnhancer
    {
        public OfRaRegenerationAbility(DbAbility dba, int level) : base(dba, level, EProperty.Undefined) { }

        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }

        public override int GetAmountForLevel(int level)
        {
            if (level < 1) { return 0; }
            
            // return values in milliseconds for tick
            switch (level)
            {
                case 1: return 500;
                case 2: return 1000;
                case 3: return 1500;
                case 4: return 2000;
                case 5: return 2500;
                default: return 0;
            }
        }

    }
}
