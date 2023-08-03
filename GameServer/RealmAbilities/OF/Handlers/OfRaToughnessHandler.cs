using Core.GS.RealmAbilities;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Toughness : Increases maximum hit points by 3% per level of this ability.
    /// </summary>
    public class OfRaToughnessHandler : RaPropertyEnhancer
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public OfRaToughnessHandler(DbAbilities dba, int level) : base(dba, level, EProperty.MaxHealth) { }

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