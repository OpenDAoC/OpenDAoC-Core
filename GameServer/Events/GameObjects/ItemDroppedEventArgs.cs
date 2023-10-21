using System;
using Core.Database.Tables;

namespace Core.GS.Events;

public class ItemDroppedEventArgs : EventArgs
{
	private DbInventoryItem m_sourceItem;
	private WorldInventoryItem m_groundItem;

	public ItemDroppedEventArgs(DbInventoryItem sourceItem, WorldInventoryItem groundItem)
	{
		m_sourceItem = sourceItem;
		m_groundItem = groundItem;
	}

	/// <summary>
	/// Gets the source item
	/// </summary>
	public DbInventoryItem SourceItem
	{
		get { return m_sourceItem; }
	}

	/// <summary>
	/// Gets the ground item
	/// </summary>
	public WorldInventoryItem GroundItem
	{
		get { return m_groundItem; }
	}
}