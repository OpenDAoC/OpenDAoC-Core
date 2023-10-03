using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Mastery of Stealth RA
	/// </summary>
	public class MasteryOfStealthAbility : RAPropertyEnhancer
	{
		public MasteryOfStealthAbility(DbAbility dba, int level) : base(dba, level, eProperty.Undefined) { }

		public override int GetAmountForLevel(int level)
		{
			return level switch
			{
				1 => 5,
				2 => 10,
				3 => 15,
				_ => 0
			};
		}

		public override int MaxLevel => 3;

		public override int CostForUpgrade(int level)
		{
			return level switch
			{
				1 => 6,
				2 => 10,
				_ => 3
			};
		}

		public override IList<string> DelveInfo
		{
			get
			{
				var list = new List<string>();
				list.Add(m_description);
				list.Add("");
				for (int i = 1; i <= MaxLevel; i++)
				{
					list.Add("Level " + i + ": Amount: " + GetAmountForLevel(i) + "%");
				}
				return list;
			}
		}
	}
}