using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Lifter : 20% additional maximum carrying capacity per level.
    /// </summary>
    public class AtlasOF_LifterAbility : RAPropertyEnhancer
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected override string ValueUnit => "%";

        public AtlasOF_LifterAbility(DbAbility dba, int level) : base(dba, level, eProperty.Undefined) { }

        public override int CostForUpgrade(int level)
        {
            return AtlasRAHelpers.GetCommonUpgradeCostFor5LevelsRA(level);
        }

        public override int GetAmountForLevel(int level)
        {
            return level < 1 ? 0 : level * 20;
        }

        public override void Activate(GameLiving living, bool sendUpdates)
        {
            base.Activate(living, sendUpdates);

            if (living is GamePlayer player)
                player.Out.SendEncumbrance();
        }

        public override void Deactivate(GameLiving living, bool sendUpdates)
        {
            base.Deactivate(living, sendUpdates);

            if (living is GamePlayer player)
                player.Out.SendEncumbrance();
        }

        public override void OnLevelChange(int oldLevel, int newLevel = 0) 
        {
            base.OnLevelChange(oldLevel, newLevel);

            if (m_activeLiving is GamePlayer player)
                player.Out.SendEncumbrance();
        }
    }
}
