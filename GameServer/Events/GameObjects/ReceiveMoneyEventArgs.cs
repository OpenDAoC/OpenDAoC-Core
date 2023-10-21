using Core.Database.Tables;

namespace Core.GS.Events;

public class ReceiveItemEventArgs : SourceEventArgs
{
	private GameObject target;
	private DbInventoryItem item;

	/// <summary>
	/// Constructs new ReceiveItemEventArgs
	/// </summary>
	/// <param name="source">the source of the item</param>
	/// <param name="target">the target of the item</param>
	/// <param name="item">the item to transfer</param>
	public ReceiveItemEventArgs(GameLiving source, GameObject target, DbInventoryItem item)
		: base(source)
	{
		this.target = target;
		this.item = item;
	}

	/// <summary>
	/// Gets the GameObject who receives the item
	/// </summary>
	public GameObject Target
	{
		get { return target; }
	}

	/// <summary>
	/// Gets the item to transfer
	/// </summary>
	public DbInventoryItem Item
	{
		get { return item; }
	}
}