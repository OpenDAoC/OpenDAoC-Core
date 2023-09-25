using System;
using System.Collections;
using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Falcon's Eye RA
	/// </summary>
	public class FalconsEyeAbility : RAPropertyEnhancer
	{
		public FalconsEyeAbility(DbAbility dba, int level)
			: base(dba, level, eProperty.CriticalSpellHitChance)
		{
		}
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
					case 1: return 5;
					case 2: return 10;
					case 3: return 15;
					case 4: return 20;
					case 5: return 25;
					default: return 25;
			}
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
					list.Add("Level " + i + ": Amount: " + Level * 5 + "% / " + GetAmountForLevel(i));
				}
				return list;
			}
		}
	}
}