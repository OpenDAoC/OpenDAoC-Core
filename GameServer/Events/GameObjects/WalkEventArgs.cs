using System;

namespace DOL.Events;

public class WalkEventArgs : EventArgs
{
	private int speed;

	/// <summary>
	/// Constructs a new WalkEventArgs
	/// </summary>
	/// <param name="speed">the walk speed</param>
	public WalkEventArgs(int speed)
	{
		this.speed=speed;
	}
	
	/// <summary>
	/// Gets the walk speed
	/// </summary>
	public int Speed
	{
		get { return speed; }
	}
}