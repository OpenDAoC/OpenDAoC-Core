using Core.Database.Tables;

namespace Core.GS.RealmAbilities;

public class OfRaDeterminationAbility : NfRaDeterminationAbility
{
	public OfRaDeterminationAbility(DbAbility dba, int level) : base(dba, level) { }

	public override int MaxLevel { get { return 5; } }
	public override int CostForUpgrade(int level)
	{
		// OF Det circa 2003 has lower cost than the usual 1/3/6/10/14.
		switch (level)
		{
			case 0: return 1;
			case 1: return 2;
			case 2: return 3;
			case 3: return 6;
			case 4: return 10;
			default: return 1000;
		}
	}
	public override int GetAmountForLevel(int level)
	{
		if (level < 1) return 0;

		return level * 15;
	}
}
public class OfRaDeterminationHybridAbility : OfRaDeterminationAbility
{
	public OfRaDeterminationHybridAbility(DbAbility dba, int level) : base(dba, level) { }
	public override int MaxLevel { get { return 3; } }
}