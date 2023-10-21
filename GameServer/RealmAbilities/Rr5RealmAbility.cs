using Core.Database;
using Core.Database.Tables;

namespace Core.GS.RealmAbilities
{
	public class Rr5RealmAbility : TimedRealmAbility
	{
		public Rr5RealmAbility(DbAbility ability, int level) : base(ability, level) { }

		public override int MaxLevel
		{
			get
			{
				return 1;
			}
		}

		public override bool CheckRequirement(GamePlayer player)
		{
			return player.RealmLevel >= 40;
		}

		public override int CostForUpgrade(int level)
		{
			return 0;
		}

	}
}