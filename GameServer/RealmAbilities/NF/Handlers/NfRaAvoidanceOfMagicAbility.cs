using Core.Database.Tables;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class NfRaAvoidanceOfMagicAbility : RaPropertyEnhancer
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

	public NfRaAvoidanceOfMagicAbility(DbAbility dba, int level)
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