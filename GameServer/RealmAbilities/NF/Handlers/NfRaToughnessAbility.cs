using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Server;

namespace Core.GS.RealmAbilities;

public class NfRaToughnessAbility : RaPropertyEnhancer
{
	public NfRaToughnessAbility(DbAbility dba, int level) : base(dba, level, EProperty.MaxHealth) { }

	public override int GetAmountForLevel(int level)
	{
		if (level < 1) return 0;
        if (ServerProperty.USE_NEW_PASSIVES_RAS_SCALING)
        {     
            switch (level)
            {
                case 1: return 25;
                case 2: return 50;
                case 3: return 75;
                case 4: return 100;
                case 5: return 150;
                case 6: return 200;
                case 7: return 250;
                case 8: return 325;
                case 9: return 400;
                default: return 400;
            }
        }
        else
        {
            switch (level)
            {
                case 1: return 25;
                case 2: return 75;
                case 3: return 150;
                case 4: return 250;
                case 5: return 400;
                default: return 0;
            }
        }
	}

	public override bool CheckRequirement(GamePlayer player)
	{
		return player.Level >= 40;
	}
}