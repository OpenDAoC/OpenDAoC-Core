using System;
using Core.GS;

namespace Core.Events;

/// <summary>
/// Holds the arguments for the GiveMoney event of GamePlayer
/// </summary>
public class GiveMoneyEventArgs : EventArgs
{

	private GamePlayer m_source;
	private GameObject m_target;
	private long m_money;

	/// <summary>
	/// Constructs a new GiveMoneyEventArgs
	/// </summary>
	/// <param name="source">the source that is saying something</param>
	/// <param name="target">the target that listened to the say</param>
	/// <param name="money">amount of money being given</param>
	public GiveMoneyEventArgs(GamePlayer source, GameObject target, long money)
	{
		m_source = source;
		m_target = target;
		m_money = money;
	}

	/// <summary>
	/// Gets the GamePlayer source
	/// </summary>
	public GamePlayer Source
	{
		get { return m_source; }
	}
	
	/// <summary>
	/// Gets the GameLiving target
	/// </summary>
	public GameObject Target
	{
		get { return m_target; }
	}

	/// <summary>
	/// Gets the amount of money being moved
	/// </summary>
	public long Money
	{
		get { return m_money; }
	}
}