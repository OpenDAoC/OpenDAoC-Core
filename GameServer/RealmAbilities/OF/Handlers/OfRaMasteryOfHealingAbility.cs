using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    public class OfRaMasteryOfHealingAbility : MasteryOfHealingAbility
    {
        public OfRaMasteryOfHealingAbility(DbAbility dba, int level) : base(dba, level) { }
        public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugAcuityLevel(player) >= 2; }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
    }
}