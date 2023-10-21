using System;
using Core.GS;

namespace Core.Events;

/// <summary>
/// Holds the arguments for the AttackFinished event of GameLivings
/// </summary>
public class AttackFinishedEventArgs : EventArgs
{

	/// <summary>
	/// The attack data
	/// </summary>
	private AttackData m_attackData;

	/// <summary>
	/// Constructs a new AttackFinished
	/// </summary>
	/// <param name="attackData">The attack data</param>
	public AttackFinishedEventArgs(AttackData attackData)
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