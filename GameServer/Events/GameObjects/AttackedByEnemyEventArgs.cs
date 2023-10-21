using System;
using Core.GS;

namespace Core.Events;

/// <summary>
/// Holds the arguments for the AttackedByEnemy event of GameLivings
/// </summary>
public class AttackedByEnemyEventArgs : EventArgs
{

	/// <summary>
	/// The attack data
	/// </summary>
	private AttackData m_attackData;

	/// <summary>
	/// Constructs a new AttackedByEnemy
	/// </summary>
	public AttackedByEnemyEventArgs(AttackData attackData)
	{
		this.m_attackData=attackData;
	}

	/// <summary>
	/// Gets the attack data
	/// </summary>
	public AttackData AttackData
	{
		get { return m_attackData; }
	}
}