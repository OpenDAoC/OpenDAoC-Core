using DOL.GS.Keeps;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Caster Guards Brain
	/// </summary>
	public class PveSafetyGuard : KeepGuardBrain
	{
		public override long NPC_AGGRO_DELAY => 1000;
	}
}
