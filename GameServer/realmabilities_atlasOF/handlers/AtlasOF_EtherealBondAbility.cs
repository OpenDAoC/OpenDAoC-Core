using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Ethereal Bond : Increases maximum power by 3% per level of this ability.
    /// Pre-Requisits : Serenity lvl 2
    /// </summary>
    public class AtlasOF_EtherealBondAbility : RAPropertyEnhancer
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public AtlasOF_EtherealBondAbility(DbAbility dba, int level) : base(dba, level, eProperty.PowerPool) { }

        protected override string ValueUnit => "%";

        public override bool CheckRequirement(GamePlayer player)
        {
            return AtlasRAHelpers.GetSerenityLevel(player) >= 2;
        }

        public override int GetAmountForLevel(int level)
        {
            return AtlasRAHelpers.GetPropertyEnhancer3AmountForLevel(level);
        }

        public override int CostForUpgrade(int level)
        {
            return AtlasRAHelpers.GetCommonUpgradeCostFor5LevelsRA(level);
        }
    }
}
