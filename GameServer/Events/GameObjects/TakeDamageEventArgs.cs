using System;
using Core.GS.Enums;

namespace Core.GS.Events;

/// <summary>
/// Holds the arguments for the TakeDamage event of GameObjects
/// </summary>
public class TakeDamageEventArgs : EventArgs
{
	private GameObject m_damageSource;
	private EDamageType m_damageType;
	private int m_damageAmount;
	private int m_criticalAmount;

	/// <summary>
	/// Constructs new TakeDamageEventArgs
	/// </summary>
	/// <param name="damageSource">The damage source</param>
	/// <param name="damageType">The damage type</param>
	/// <param name="damageAmount">The damage amount</param>
	/// <param name="criticalAmount">The critical damage amount</param>
	public TakeDamageEventArgs(GameObject damageSource, EDamageType damageType, int damageAmount, int criticalAmount)
	{
		m_damageSource = damageSource;
		m_damageType = damageType;
		m_damageAmount = damageAmount;
		m_criticalAmount = criticalAmount;
	}

	/// <summary>
	/// Gets the damage source
	/// </summary>
	public GameObject DamageSource
	{
		get { return m_damageSource; }
	}

	/// <summary>
	/// Gets the damage type
	/// </summary>
	public EDamageType DamageType
	{
		get { return m_damageType; }
	}

	/// <summary>
	/// Gets the damage amount
	/// </summary>
	public int DamageAmount
	{
		get { return m_damageAmount; }
	}

	/// <summary>
	/// Gets the critical damage amount
	/// </summary>
	public int CriticalAmount
	{
		get { return m_criticalAmount; }
	}
}