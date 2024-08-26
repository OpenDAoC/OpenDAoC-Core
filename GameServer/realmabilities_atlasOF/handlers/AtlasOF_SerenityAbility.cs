using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Adds to the amount of power regenerated over time.
    /// Pre-requisites: Augmented Acuity 2
    /// </summary>
    public class AtlasOF_SerenityAbility : RAPropertyEnhancer
    {
        public AtlasOF_SerenityAbility(DbAbility dba, int level) : base(dba, level, eProperty.PowerRegenerationAmount) { }

        public override bool CheckRequirement(GamePlayer player)
        {
            return AtlasRAHelpers.GetAugAcuityLevel(player) >= 2;
        }

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
