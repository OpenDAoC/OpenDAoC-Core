using System;

namespace Core.Events;

public class TurnToEventArgs : EventArgs
{
	private int x;
	private int y;

	/// <summary>
	/// Constructs a new TurnToEventArgs
	/// </summary>
	/// <param name="x">the target x</param>
	/// <param name="y">the target y</param>
	public TurnToEventArgs(int x, int y)
	{
		this.x=x;
		this.y=y;
	}

	/// <summary>
	/// Gets the target X
	/// </summary>
	public int X
	{
		get { return x; }
	}		
	
	/// <summary>
	/// Gets the target Y
	/// </summary>
	public int Y
	{
		get { return y; }
	}
}