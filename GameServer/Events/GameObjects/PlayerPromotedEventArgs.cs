using System;
using DOL.GS;

namespace DOL.Events;

public class PlayerPromotedEventArgs : EventArgs
{
	private GamePlayer player;
	private ICharacterClass oldClass;

	/// <summary>
	/// Constructs a new PlayerPromoted event argument class
	/// </summary>
	/// <param name="player">the player that was promoted</param>
	/// <param name="oldClass">the player old class</param>
	public PlayerPromotedEventArgs(GamePlayer player, ICharacterClass oldClass)
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
	public ICharacterClass OldClass
	{
		get { return oldClass; }
	}
}