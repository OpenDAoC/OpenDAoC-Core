using System;

namespace Core.Events;

public class MoveToEventArgs : EventArgs
{
	private ushort regionID;
	private int x;
private int y;
	private int z;
	private ushort heading;

	/// <summary>
	/// Constructs new MoveToEventArgs
	/// </summary>
	/// <param name="regionId">the target regionid</param>
	/// <param name="x">the target x</param>
	/// <param name="y">the target y</param>
	/// <param name="z">the target z</param>
	/// <param name="heading">the target heading</param>
	public MoveToEventArgs(ushort regionId, int x, int y, int z, ushort heading)
	{
		this.regionID = regionId;
		this.x = x;
		this.y = y;
		this.z = z;
		this.heading = heading;
	}

	/// <summary>
	/// Gets the target RegionID
	/// </summary>
	public ushort RegionId
	{
		get { return regionID; }
	}

	/// <summary>
	/// Gets the target x
	/// </summary>
	public int X
	{
		get { return x; }
	}

	/// <summary>
	/// Gets the target y
	/// </summary>
	public int Y
	{
		get { return y; }
	}

	/// <summary>
	/// Gets the target z
	/// </summary>
	public int Z
	{
		get { return z; }
	}

	/// <summary>
	/// Gets the target heading
	/// </summary>
	public ushort Heading
	{
		get { return heading; }
	}
}