using Core.Database;

namespace Core.GS.RealmAbilities
{
	public class OfRaMasteryOfWaterAbility : RaPropertyEnhancer
    {
        public OfRaMasteryOfWaterAbility(DbAbility dba, int level) : base(dba, level, EProperty.WaterSpeed) { }
        protected override string ValueUnit { get { return "%"; } }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
    }
}