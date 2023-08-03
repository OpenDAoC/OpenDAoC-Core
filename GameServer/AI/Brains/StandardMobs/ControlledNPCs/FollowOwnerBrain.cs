﻿using System;
using DOL.GS;

namespace DOL.AI.Brain
{
	/// <summary>
	/// Dummy Controlled Brain to provide a Following Pet. 
	/// </summary>
	public class FollowOwnerBrain : ControlledNpcBrain
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
			
			m_aggressionState = EAggressionState.Passive;
		}

		/// <summary>
		/// Follow Even if not Main Pet
		/// </summary>
		public override void FollowOwner()
		{
			Body.StopAttack();
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