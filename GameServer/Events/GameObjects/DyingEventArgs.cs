using System;
using System.Collections.Generic;

namespace Core.GS.Events;

/// <summary>
/// Holds the arguments for the Dying event of GameLivings
/// </summary>
public class DyingEventArgs : EventArgs
{

	/// <summary>
	/// The killer
	/// </summary>
	private GameObject m_killer;

	private List<GamePlayer> m_playerKillers = null;

	/// <summary>
	/// Constructs a new Dying event args
	/// </summary>
	public DyingEventArgs(GameObject killer)
	{
		m_killer=killer;
	}

	public DyingEventArgs(GameObject killer, List<GamePlayer> playerKillers)
	{
		m_killer = killer;
		m_playerKillers = playerKillers;
	}

	/// <summary>
	/// Gets the killer
	/// </summary>
	public GameObject Killer
	{
		get { return m_killer; }
	}

	public List<GamePlayer> PlayerKillers
	{
		get { return m_playerKillers; }
	}
}