using Core.Database;

namespace Core.GS.RealmAbilities
{
	public class OfRaPreventFlightAbility : RealmAbility
	{
		public OfRaPreventFlightAbility(DbAbility dba, int level) : base(dba, level) { }

		public override int MaxLevel { get { return 1; } }

		public override bool CheckRequirement(GamePlayer player) { return true; }

		public override int CostForUpgrade(int level) { return 14; }
	}
	
}