using System;
using DOL.Database;
using DOL.GS;

namespace DOL.Events;

public class ItemEquippedArgs : EventArgs
{
	private DbInventoryItem m_item;
	private eInventorySlot m_previousSlotPosition;

	/// <summary>
	/// Constructs a new ItemEquippedArgs
	/// </summary>
	/// <param name="item">The equipped item</param>
	/// <param name="previousSlotPosition">The slot position item had before it was equipped</param>
	public ItemEquippedArgs(DbInventoryItem item, eInventorySlot previousSlotPosition)
	{
		m_item = item;
		m_previousSlotPosition = previousSlotPosition;
	}

	/// <summary>
	/// Constructs a new ItemEquippedArgs
	/// </summary>
	/// <param name="item">The equipped item</param>
	/// <param name="previousSlotPosition">The slot position item had before it was equipped</param>
	public ItemEquippedArgs(DbInventoryItem item, int previousSlotPosition)
	{
		m_item = item;
		m_previousSlotPosition = (eInventorySlot)previousSlotPosition;
	}

	/// <summary>
	/// Gets the equipped item
	/// </summary>
	public DbInventoryItem Item
	{
		get { return m_item; }
	}

	/// <summary>
	/// Gets the previous slot position
	/// </summary>
	public eInventorySlot PreviousSlotPosition
	{
		get { return m_previousSlotPosition; }
	}
}