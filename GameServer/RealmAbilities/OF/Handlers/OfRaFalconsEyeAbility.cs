using Core.Database;
using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities
{
    public class OfRaFalconsEyeAbility : RaPropertyEnhancer // We don't want to piggyback on the NF FalconsEye because it increases spell crit chance and not archery for some reason...
    {
        public OfRaFalconsEyeAbility(DbAbility dba, int level) : base(dba, level, EProperty.CriticalArcheryHitChance) { }
        public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugDexLevel(player) >= 2; }
        public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer5AmountForLevel(level); }
        public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
    }
}
