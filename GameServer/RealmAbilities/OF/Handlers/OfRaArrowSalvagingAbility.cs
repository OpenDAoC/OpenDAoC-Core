using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities
{
	public class OfRaArrowSalvagingAbility : RaPropertyEnhancer
    {
        public OfRaArrowSalvagingAbility(DbAbility dba, int level) : base(dba, level, EProperty.ArrowRecovery) { }
        protected override string ValueUnit { get { return "%"; } }
        public override bool CheckRequirement(GamePlayer player) { return true; }
        public override int GetAmountForLevel(int level) { return level * 10; }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
    }
}