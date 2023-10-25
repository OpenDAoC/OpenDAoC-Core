using System;
using Core.GS.Enums;

namespace Core.GS.Events;

/// <summary>
/// Holds the arguments for the EnemyHealed event of GameLivings
/// </summary>
public class EnemyHealedEventArgs : EventArgs
{
	private readonly GameLiving m_enemy;
	private readonly GameObject m_healSource;
	private readonly EHealthChangeType m_changeType;
	private readonly int m_healAmount;

	/// <summary>
	/// Constructs new EnemyHealedEventArgs
	/// </summary>
	/// <param name="enemy">The healed enemy</param>
	/// <param name="healSource">The heal source</param>
	/// <param name="changeType">The health change type</param>
	/// <param name="healAmount">The heal amount</param>
	public EnemyHealedEventArgs(GameLiving enemy, GameObject healSource, EHealthChangeType changeType, int healAmount)
	{
		m_enemy = enemy;
		m_healSource = healSource;
		m_changeType = changeType;
		m_healAmount = healAmount;
	}

	/// <summary>
	/// Gets the healed enemy
	/// </summary>
	public GameLiving Enemy
	{
		get { return m_enemy; }
	}

	/// <summary>
	/// Gets the heal source
	/// </summary>
	public GameObject HealSource
	{
		get { return m_healSource; }
	}

	/// <summary>
	/// Gets the health change type
	/// </summary>
	public EHealthChangeType ChangeType
	{
		get { return m_changeType; }
	}

	/// <summary>
	/// Gets the heal amount
	/// </summary>
	public int HealAmount
	{
		get { return m_healAmount; }
	}
}