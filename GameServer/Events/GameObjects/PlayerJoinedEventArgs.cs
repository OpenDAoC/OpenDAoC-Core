using System;
using DOL.GS;

namespace DOL.Events;

/// <summary>
/// Holds the arguments for the PlayerJoined event of PlayerGroup.
/// </summary>
public class MemberJoinedEventArgs : EventArgs
{
	private GameLiving m_member;

	/// <summary>
	/// Constructs new MemberJoinedEventArgs
	/// </summary>
	/// <param name="living">The joined living</param>
	public MemberJoinedEventArgs(GameLiving living)
	{
		m_member = living;
	}

	/// <summary>
	/// The joined member
	/// </summary>
	public GameLiving Member
	{
		get { return m_member; }
	}
}