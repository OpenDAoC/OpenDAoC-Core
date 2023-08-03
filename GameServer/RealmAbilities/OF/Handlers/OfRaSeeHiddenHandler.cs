using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Determination
	/// </summary>
	public class OfRaSeeHiddenHandler : RealmAbility
	{
		public OfRaSeeHiddenHandler(DbAbilities dba, int level) : base(dba, level) { }

		public override int MaxLevel { get { return 1; } }

		public override bool CheckRequirement(GamePlayer player) { return true; }

		public override int CostForUpgrade(int level) { return 8; }
	}
	
}