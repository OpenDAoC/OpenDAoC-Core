using DOL.GS.Keeps;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Caster Guards Brain
	/// </summary>
	public class CasterBrain : KeepGuardBrain
	{
		/// <summary>
		/// Brain Think
		/// </summary>
		public override void Think()
		{
			CheckForNuking();
			base.Think();
		}
		private void CheckForNuking()
		{
			if (_keepGuardBody.CanUseRanged)
				_keepGuardBody.CheckForNuke();
		}
	}
}
