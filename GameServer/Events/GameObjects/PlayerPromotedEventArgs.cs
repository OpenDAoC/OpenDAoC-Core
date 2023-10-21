using System;

namespace Core.GS.Events;

public class PlayerPromotedEventArgs : EventArgs
{
	private GamePlayer player;
	private IPlayerClass oldClass;

	/// <summary>
	/// Constructs a new PlayerPromoted event argument class
	/// </summary>
	/// <param name="player">the player that was promoted</param>
	/// <param name="oldClass">the player old class</param>
	public PlayerPromotedEventArgs(GamePlayer player, IPlayerClass oldClass)
	{
		this.player = player;
		this.oldClass = oldClass;
	}

	/// <summary>
	/// Gets the player that was promoted
	/// </summary>
	public GamePlayer Player
	{
		get { return player; }
	}

	/// <summary>
	/// Gets the class player was using before promotion
	/// </summary>
	public IPlayerClass OldClass
	{
		get { return oldClass; }
	}
}