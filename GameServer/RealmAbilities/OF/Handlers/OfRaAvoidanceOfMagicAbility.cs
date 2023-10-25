using Core.Database.Tables;

namespace Core.GS.RealmAbilities;

public class OfRaAvoidanceOfMagicAbility : NfRaAvoidanceOfMagicAbility
{
	public OfRaAvoidanceOfMagicAbility(DbAbility dba, int level) : base(dba, level) { }
	public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }
	public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
}