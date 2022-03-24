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
	public class AtlasOF_PreventFlight : RealmAbility
	{
		public AtlasOF_PreventFlight(DBAbility dba, int level) : base(dba, level) { }

		public override int MaxLevel { get { return 1; } }

		public override bool CheckRequirement(GamePlayer player) { return true; }

		public override int CostForUpgrade(int level) { return 14; }
	}
	
}