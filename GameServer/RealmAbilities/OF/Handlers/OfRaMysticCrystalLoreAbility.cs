using Core.Database.Tables;

namespace Core.GS.RealmAbilities;

public class OfRaMysticCrystalLoreAbility : NfRaMysticCrystalLoreAbility
{
	public OfRaMysticCrystalLoreAbility(DbAbility dba, int level) : base(dba, level) { }

    public override int CostForUpgrade(int currentLevel) { return OfRaHelpers.GetCommonUpgradeCostFor3LevelsRA(currentLevel); }
    
    public override int GetReUseDelay(int level)
    {
        return 300;
    }
}