using Core.Database.Tables;

namespace Core.GS.RealmAbilities;

public class OfRaMasteryOfPainAbility : NfRaMasteryOfPainAbility
{
	public OfRaMasteryOfPainAbility(DbAbility dba, int level) : base(dba, level) { }
    public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugDexLevel(player) >= 2; }

    // MoP is 5% per level unlike most other Mastery RAs.
    public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer5AmountForLevel(level); } 
    public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
}