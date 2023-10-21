using System;
using Core.GS;

namespace Core.Events;

/// <summary>
/// Holds the arguments for the EnemyKilled event of GameLivings
/// </summary>
public class EnemyKilledEventArgs : EventArgs
{

	/// <summary>
	/// the attacker of attack
	/// </summary>
	private readonly GameLiving m_target;

	/// <summary>
	/// Constructs a new EnemyKilledEventArgs
	/// </summary>
	public EnemyKilledEventArgs(GameLiving target)
	{
		this.m_target=target;
	}

	/// <summary>
	/// Gets the attacker of attack
	/// </summary>
	public GameLiving Target
	{
		get { return m_target; }
	}
}