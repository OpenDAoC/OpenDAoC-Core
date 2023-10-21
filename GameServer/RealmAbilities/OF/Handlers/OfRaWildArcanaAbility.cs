using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities
{
    public class OfRaWildArcanaAbility : RaPropertyEnhancer
    {
        public OfRaWildArcanaAbility(DbAbility dba, int level) : base(dba, level, EProperty.CriticalDotHitChance) { }
        public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugAcuityLevel(player) >= 2; }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer5AmountForLevel(level); }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
    }
}