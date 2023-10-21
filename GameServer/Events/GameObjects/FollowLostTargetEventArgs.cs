using System;

namespace Core.GS.Events;

/// <summary>
/// Holds the arguments for the FollowLostTarget event of GameNpc
/// </summary>
public class FollowLostTargetEventArgs : EventArgs
{
	private readonly GameObject m_lostTarget;

	/// <summary>
	/// Constructs new FollowLostTargetEventArgs
	/// </summary>
	/// <param name="lostTarget">The lost follow target</param>
	public FollowLostTargetEventArgs(GameObject lostTarget)
	{
		m_lostTarget = lostTarget;
	}

	/// <summary>
	/// Gets the lost follow target
	/// </summary>
	public GameObject LostTarget
	{
		get { return m_lostTarget; }
	}
}