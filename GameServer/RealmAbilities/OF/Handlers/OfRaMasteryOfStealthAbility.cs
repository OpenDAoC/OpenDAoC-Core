using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	public class OfRaMasteryOfStealthAbility : NfRaMasteryOfStealthAbility
	{
        public OfRaMasteryOfStealthAbility(DbAbility dba, int level) : base(dba, level) { }
        protected override string ValueUnit { get { return "%"; } }
        public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugQuiLevel(player) >= 2; }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer5AmountForLevel(level); }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor3LevelsRA(level); }
    }
}