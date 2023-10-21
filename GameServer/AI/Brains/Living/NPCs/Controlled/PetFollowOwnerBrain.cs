using System;
using Core.AI.Brain;
using Core.GS.Enums;

namespace Core.GS.AI.Brains;

/// <summary>
/// Dummy Controlled Brain to provide a Following Pet. 
/// </summary>
public class PetFollowOwnerBrain : ControlledNpcBrain
{
	/// <summary>
	/// Passive default
	/// </summary>
	/// <param name="owner"></param>
	public PetFollowOwnerBrain(GameLiving owner)
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