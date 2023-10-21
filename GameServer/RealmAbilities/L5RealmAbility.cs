using Core.Database;

namespace Core.GS.RealmAbilities
{
	public class L5RealmAbility : RealmAbility
	{
		public L5RealmAbility(DbAbility ability, int level) : base(ability, level) { }

		public override int CostForUpgrade(int level)
		{
			if (ServerProperties.Properties.USE_NEW_PASSIVES_RAS_SCALING)
			{
				switch (level)
				{
						case 0: return 1;
						case 1: return 1;
						case 2: return 2;
						case 3: return 3;
						case 4: return 3;
						case 5: return 5;
						case 6: return 5;
						case 7: return 7;
						case 8: return 7;
						default: return 1000;
				}
			}
			else
			{
				switch (level)
				{
						case 0: return 1;
						case 1: return 3;
						case 2: return 6;
						case 3: return 10;
						case 4: return 14;
						default: return 1000;
				}
			}
		}

		public override bool CheckRequirement(GamePlayer player)
		{
			if (ServerProperties.Properties.USE_NEW_PASSIVES_RAS_SCALING)
			{
				return Level <= 9;
			}
			else
			{
				return Level <= 5;
			}
		}

		public override int MaxLevel
		{
			get
			{
				if (ServerProperties.Properties.USE_NEW_PASSIVES_RAS_SCALING)
				{
					return 9;
				}
				else
				{
					return 5;
				}
			}
		}
	}
}