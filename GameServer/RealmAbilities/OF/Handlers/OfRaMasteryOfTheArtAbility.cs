using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class OfRaMasteryOfTheArtAbility : RaPropertyEnhancer
{
    public OfRaMasteryOfTheArtAbility(DbAbility dba, int level) : base(dba, level, EProperty.CastingSpeed) { }
    protected override string ValueUnit { get { return "%"; } }
    public override bool CheckRequirement(GamePlayer player) { return OfRaHelpers.GetAugAcuityLevel(player) >= 3; }
    public override int GetAmountForLevel(int level) { return OfRaHelpers.GetPropertyEnhancer3AmountForLevel(level); }
    public override int CostForUpgrade(int level) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(level); }
}