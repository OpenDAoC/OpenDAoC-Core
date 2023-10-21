using Core.Database;

namespace Core.GS.RealmAbilities
{
    public class OfRaToughnessAbility : RaPropertyEnhancer
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OfRaToughnessAbility(DbAbility dba, int level) : base(dba, level, EProperty.MaxHealth) { }

        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }

        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }

        public override void Activate(GameLiving living, bool sendUpdates)
        {
            log.Warn("INSIDE Activate");
            base.Activate(living, sendUpdates);
        }

        public override void Deactivate(GameLiving living, bool sendUpdates)
        {
            log.Warn("INSIDE Deactivate");
            base.Deactivate(living, sendUpdates);
        }

        public override void OnLevelChange(int oldLevel, int newLevel = 0)
        {
            log.Warn("INSIDE onLevelChange");
            SendUpdates(this.m_activeLiving);
            base.OnLevelChange(oldLevel, newLevel);
        }
    }
}
