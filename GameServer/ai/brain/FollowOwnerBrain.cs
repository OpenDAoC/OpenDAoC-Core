using System;

using DOL.GS;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Dummy Controlled Brain to provide a Following Pet. 
	/// </summary>
	public class FollowOwnerBrain : ControlledMobBrain
	{
		/// <summary>
		/// Passive default
		/// </summary>
		/// <param name="owner"></param>
		public FollowOwnerBrain(GameLiving owner)
			: base(owner)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");
			
			m_aggressionState = eAggressionState.Passive;
		}

		/// <summary>
		/// Follow Even if not Main Pet
		/// </summary>
		public override void FollowOwner()
		{
			if (Body.IsAttacking)
				Disengage();

			Body.Follow(Owner, MIN_OWNER_FOLLOW_DIST, MAX_OWNER_FOLLOW_DIST);
		}
		
		/// <summary>
		/// Follow Owner on Think
		/// </summary>
		public override void Think()
		{
			base.Think();
			FollowOwner();
		}
	}
}
