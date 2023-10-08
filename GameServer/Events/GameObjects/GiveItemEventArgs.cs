using System;
using DOL.Database;
using DOL.GS;

namespace DOL.Events;

/// <summary>
/// Holds the arguments for the GiveItem event of GamePlayer
/// </summary>
public class GiveItemEventArgs : EventArgs
{

	private GamePlayer source;
	private GameObject target;
	private DbInventoryItem item;

	/// <summary>
	/// Constructs a new SayReceiveEventArgs
	/// </summary>
	/// <param name="source">the source that is saying something</param>
	/// <param name="target">the target that listened to the say</param>
	/// <param name="item">the item being given</param>
	public GiveItemEventArgs(GamePlayer source, GameObject target, DbInventoryItem item)
	{
		this.source = source;
		this.target = target;
		this.item = item;
	}

	/// <summary>
	/// Gets the GamePlayer source who was saying something
	/// </summary>
	public GamePlayer Source
	{
		get { return source; }
	}
	
	/// <summary>
	/// Gets the GameLiving target who listened to the say
	/// </summary>
	public GameObject Target
	{
		get { return target; }
	}

	/// <summary>
	/// Gets the item being moved
	/// </summary>
	public DbInventoryItem Item
	{
		get { return item; }
	}
}