using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.GS.SkillHandler;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Ethereal Bond
	/// </summary>
	public class XEtherealBondAbility : RAPropertyEnhancer
	{
		public XEtherealBondAbility(DBAbility dba, int level) : base(dba, level, eProperty.MaxMana) { }

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
					case 1: return 50;
					case 2: return 100;
					case 3: return 150;
					case 4: return 200;
					case 5: return 250;
					default: return 0;
			}
		}
	}
}