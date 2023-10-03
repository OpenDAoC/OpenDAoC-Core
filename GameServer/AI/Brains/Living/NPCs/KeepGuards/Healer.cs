using DOL.Events;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Healer Guard Brain
	/// </summary>
	public class HealerBrain : KeepGuardBrain
	{
		/// <summary>
		/// Brain Think Method
		/// </summary>
		public override void Think()
		{
			CheckAreaForHealing();
			base.Think();
		}

		/// <summary>
		/// Checks the areas for friendlies to heal if we can cast spells
		/// </summary>
		private void CheckAreaForHealing()
		{
			if (_keepGuardBody.CanUseRanged)
				_keepGuardBody.CheckAreaForHeals();
		}

		/// <summary>
		/// To be honest, I don't really know what this does, 
		/// I think it stops telling all mobs in area that this guard has healed
		/// I had some problems before with guards wanting to attack themselves and eachother
		/// </summary>
		/// <param name="e">The event</param>
		/// <param name="sender">The sender</param>
		/// <param name="args">The arguments</param>
		public override void Notify(DOLEvent e, object sender, System.EventArgs args)
		{
			if (e == GameLivingEvent.EnemyHealed)
				return;
			base.Notify(e, sender, args);
		}
	}
}
