using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class OfRaMasteryOfArcheryAbility : RaPropertyEnhancer
    {
        public OfRaMasteryOfArcheryAbility(DbAbility dba, int level) : base(dba, level, EProperty.ArcherySpeed) { }
        protected override string ValueUnit { get { return "%"; } }
        public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugDexLevel(player) >= 3; }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
    }
}