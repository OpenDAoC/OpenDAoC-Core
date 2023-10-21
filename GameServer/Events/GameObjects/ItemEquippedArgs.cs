using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS;
using Core.GS.Enums;

namespace Core.Events;

public class ItemEquippedArgs : EventArgs
{
	private DbInventoryItem m_item;
	private EInventorySlot m_previousSlotPosition;

	/// <summary>
	/// Constructs a new ItemEquippedArgs
	/// </summary>
	/// <param name="item">The equipped item</param>
	/// <param name="previousSlotPosition">The slot position item had before it was equipped</param>
	public ItemEquippedArgs(DbInventoryItem item, EInventorySlot previousSlotPosition)
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
		m_previousSlotPosition = (EInventorySlot)previousSlotPosition;
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
	public EInventorySlot PreviousSlotPosition
	{
		get { return m_previousSlotPosition; }
	}
}