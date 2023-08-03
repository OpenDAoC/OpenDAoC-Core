using Core.GS.RealmAbilities;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Avoidance of Magic RA, reduces magical damage
	/// </summary>
	public class NfRaAvoidanceOfMagicHandler : RaPropertyEnhancer
	{
		/// <summary>
		/// The list of properties this RA affects
		/// </summary>
		public static EProperty[] properties = new EProperty[]
		{
			EProperty.Resist_Body,
			EProperty.Resist_Cold,
			EProperty.Resist_Energy,
			EProperty.Resist_Heat,
			EProperty.Resist_Matter,
			EProperty.Resist_Spirit,
		};

		public NfRaAvoidanceOfMagicHandler(DbAbilities dba, int level)
			: base(dba, level, properties)
		{
		}

		protected override string ValueUnit { get { return "%"; } }

		public override int CostForUpgrade(int level)
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

		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;

			switch (level)
			{
					case 1: return 3;
					case 2: return 6;
					case 3: return 9;
					case 4: return 12;
					case 5: return 15;
					default: return 15;
			}
		}
	}

	/// <summary>
	/// Physical Defence RA, reduces melee damage
	/// </summary>
	public class PhysicalDefenceAbility : RaPropertyEnhancer
	{
		/// <summary>
		/// The list of properties this RA affects
		/// </summary>
		public static EProperty[] properties = new EProperty[]
		{
			EProperty.Resist_Crush,
			EProperty.Resist_Slash,
			EProperty.Resist_Thrust,
		};

		public PhysicalDefenceAbility(DbAbilities dba, int level)
			: base(dba, level, properties)
		{
		}

		protected override string ValueUnit { get { return "%"; } }

		public override int CostForUpgrade(int level)
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

		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;

			switch (level)
			{
				case 1: return 3;
				case 2: return 6;
				case 3: return 9;
				case 4: return 12;
				case 5: return 15;
				default: return 15;
			}
		}
	}
}