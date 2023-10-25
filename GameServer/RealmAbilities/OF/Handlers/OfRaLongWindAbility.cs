using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class OfRaLongWindAbility : RaPropertyEnhancer
{

    public OfRaLongWindAbility(DbAbility dba, int level) : base(dba, level, EProperty.Undefined) { }

    protected override string ValueUnit { get { return "%"; } }

    public override int CostForUpgrade(int currentLevel) { return OfRaHelpers.GetCommonUpgradeCostFor5LevelsRA(currentLevel); }

    public override int GetAmountForLevel(int level)
    {
        if (level < 1) { return 0; }
        switch (level)
        {
            case 1: return 20;
            case 2: return 40;
            case 3: return 60;
            case 4: return 80;
            case 5: return 100;
            default: return 0;
        }
    }

}