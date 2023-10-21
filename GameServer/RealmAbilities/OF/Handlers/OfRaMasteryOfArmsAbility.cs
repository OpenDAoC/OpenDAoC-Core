using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class OfRaMasteryOfArmsAbility : RaPropertyEnhancer
{
    public OfRaMasteryOfArmsAbility(DbAbility dba, int level) : base(dba, level, EProperty.MeleeSpeed) { }
    protected override string ValueUnit { get { return "%"; } }

    public override bool CheckRequirement(GamePlayer player)
    { 
        // Atlas custom change - Friar pre-req is AugDex3 instead of a 100% useless AugStr3.
        if (player.PlayerClass.ID == (byte)EPlayerClass.Friar)
        {
            return OfRaHelpers.GetAugDexLevel(player) >= 3;
        }
        
        return OfRaHelpers.GetAugStrLevel(player) >= 3;
    }

    public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }
    public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
}