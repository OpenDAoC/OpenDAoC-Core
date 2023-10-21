using Core.Database.Tables;

namespace Core.GS.RealmAbilities;

public class OfRaWildHealingAbility : WildHealingAbility
{
    public OfRaWildHealingAbility(DbAbility dba, int level) : base(dba, level) { }
    public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugAcuityLevel(player) >= 2; }
    public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer5AmountForLevel(level); }
    public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
}