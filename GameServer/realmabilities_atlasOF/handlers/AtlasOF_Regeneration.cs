using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Your hit points regenerate faster than normal.
    /// </summary>
    public class AtlasOF_Regeneration : RAPropertyEnhancer
    {
        public AtlasOF_Regeneration(DbAbility dba, int level) : base(dba, level, eProperty.HealthRegenerationAmount) { }

        public override int CostForUpgrade(int level)
        {
            return AtlasRAHelpers.GetCommonUpgradeCostFor5LevelsRA(level);
        }

        public override int GetAmountForLevel(int level)
        {
            return level switch
            {
                1 => 1,
                2 => 2,
                3 => 3,
                4 => 4,
                5 => 5,
                _ => 0,
            };
        }
    }
}
