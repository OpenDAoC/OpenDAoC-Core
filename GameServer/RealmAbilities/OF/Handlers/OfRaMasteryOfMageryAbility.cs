using Core.Database;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities
{
	public class OfRaMasteryOfMageryAbility : RaPropertyEnhancer
    {
        public OfRaMasteryOfMageryAbility(DbAbility dba, int level) : base(dba, level, EProperty.SpellDamage) { }
        protected override string ValueUnit { get { return "%"; } }
        public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugAcuityLevel(player) >= 2; }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
    }
}