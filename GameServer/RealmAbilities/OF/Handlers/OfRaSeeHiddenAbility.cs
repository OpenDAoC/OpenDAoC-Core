using Core.Database;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities
{
	public class OfRaSeeHiddenAbility : RealmAbility
	{
		public OfRaSeeHiddenAbility(DbAbility dba, int level) : base(dba, level) { }

		public override int MaxLevel { get { return 1; } }

		public override bool CheckRequirement(GamePlayer player) { return true; }

		public override int CostForUpgrade(int level) { return 8; }
	}
	
}