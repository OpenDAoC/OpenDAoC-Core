using Core.Database;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities
{
    public class OfRaMasteryOfBlockingAbility : NfRaMasteryOfBlockingAbility
    {
        public OfRaMasteryOfBlockingAbility(DbAbility dba, int level) : base(dba, level) { }
        public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugDexLevel(player) >= 2; }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
    }
}