using System;

namespace Core.GS.Events;

/// <summary>
/// Holds the arguments for the ReceiveMoney event of GameLivings
/// </summary>
public class ReceiveMoneyEventArgs : EventArgs
{
	private GameLiving source;
	private GameLiving target;
	private long copperValue;

	/// <summary>
	/// Constructs new ReceiveMoneyEventArgs
	/// </summary>
	/// <param name="source">the source of the money</param>
	/// <param name="target">the target of the money</param>
	/// <param name="copperValue">the money value</param>
	public ReceiveMoneyEventArgs(GameLiving source, GameLiving target, long copperValue)
	{
		this.source = source;
		this.target = target;
		this.copperValue = copperValue;
	}

	/// <summary>
	/// Gets the GameLiving who spent the money
	/// </summary>
	public GameLiving Source
	{
		get { return source; }
	}

	/// <summary>
	/// Gets the GameLivng who receives the money
	/// </summary>
	public GameLiving Target
	{
		get { return target; }
	}

	/// <summary>
	/// Gets the value of the money
	/// </summary>
	public long CopperValue
	{
		get { return copperValue; }
	}
}