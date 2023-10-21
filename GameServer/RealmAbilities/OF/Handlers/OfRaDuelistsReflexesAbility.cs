using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities
{
    public class OfRaDuelistsReflexesAbility : RaPropertyEnhancer
    {
        public OfRaDuelistsReflexesAbility(DbAbility dba, int level) : base(dba, level, EProperty.OffhandDamageAndChance) { }
        protected override string ValueUnit { get { return "%"; } }
        public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugDexLevel(player) >= 2; }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
    }
}