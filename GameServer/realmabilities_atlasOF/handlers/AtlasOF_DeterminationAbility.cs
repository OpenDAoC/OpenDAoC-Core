using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.GS.SkillHandler;

namespace DOL.GS.RealmAbilities
{
	/// <summary>
	/// Determination
	/// </summary>
	public class AtlasOF_DeterminationAbility : DeterminationAbility
	{
		public AtlasOF_DeterminationAbility(DBAbility dba, int level) : base(dba, level) { }

		public override int MaxLevel { get { return 5; } }
		public override int CostForUpgrade(int level)
		{
			// OF Det circa 2003 has lower cost than the usual 1/3/6/10/14.
			switch (level)
			{
				case 0: return 1;
				case 1: return 2;
				case 2: return 3;
				case 3: return 6;
				case 4: return 10;
				default: return 1000;
			}
		}
		public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;

			return level * 15;
		}
	}
	public class AtlasOF_DeterminationHybridAbility : AtlasOF_DeterminationAbility
	{
		public AtlasOF_DeterminationHybridAbility(DBAbility dba, int level) : base(dba, level) { }
		public override int MaxLevel { get { return 3; } }
	}
}