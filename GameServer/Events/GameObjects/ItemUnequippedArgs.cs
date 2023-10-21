using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS;

namespace Core.Events;

public class ItemUnequippedArgs : EventArgs
{
	private DbInventoryItem m_item;
	private EInventorySlot m_previousSlotPos;

	/// <summary>
	/// Constructs a new ItemEquippedArgs
	/// </summary>
	/// <param name="item">The unequipped item</param>
	/// <param name="previousSlotPos">The slot position item had before it was equipped</param>
	public ItemUnequippedArgs(DbInventoryItem item, EInventorySlot previousSlotPos)
	{
		m_item = item;
		m_previousSlotPos = previousSlotPos;
	}

	/// <summary>
	/// Constructs a new ItemEquippedArgs
	/// </summary>
	/// <param name="item">The unequipped item</param>
	/// <param name="previousSlotPos">The slot position item had before it was equipped</param>
	public ItemUnequippedArgs(DbInventoryItem item, int previousSlotPos)
	{
		m_item = item;
		m_previousSlotPos = (EInventorySlot)previousSlotPos;
	}

	/// <summary>
	/// Gets the unequipped item
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
		get { return m_previousSlotPos; }
	}
}