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
        public override int CostForUpgrade(int level) { return AtlasRAHelpers.GetCommonPassivesCostForUpgrade(level); }

        public override int GetAmountForLevel(int level)
		{
			if (level < 1) return 0;

			return level * 15;
		}
	}
}