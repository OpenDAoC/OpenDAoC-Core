using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Toughness : Increases maximum hit points by 3% per level of this ability.
    /// </summary>
    public class AtlasOF_ToughnessAbility : RAPropertyEnhancer
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AtlasOF_ToughnessAbility(DbAbility dba, int level) : base(dba, level, eProperty.Of_Toughness) { }

        public override int CostForUpgrade(int level)
        {
            return AtlasRAHelpers.GetCommonUpgradeCostFor5LevelsRA(level);
        }

        public override int GetAmountForLevel(int level)
        {
            return AtlasRAHelpers.GetPropertyEnhancer3AmountForLevel(level);
        }

        public override void OnLevelChange(int oldLevel, int newLevel = 0)
        {
            SendUpdates(m_activeLiving);
        }
    }
}
