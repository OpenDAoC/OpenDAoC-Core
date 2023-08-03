using System;
using DOL.Database;
using DOL.GS;

namespace DOL.Events
{
	/// <summary>
	/// Holds the arguments for the ItemUnEquipped event of PlayerInventory
	/// </summary>
	public class ItemUnequippedArgs : EventArgs
	{
		private InventoryItem m_item;
		private eInventorySlot m_previousSlotPos;

		/// <summary>
		/// Constructs a new ItemEquippedArgs
		/// </summary>
		/// <param name="item">The unequipped item</param>
		/// <param name="previousSlotPos">The slot position item had before it was equipped</param>
		public ItemUnequippedArgs(InventoryItem item, eInventorySlot previousSlotPos)
		{
			m_item = item;
			m_previousSlotPos = previousSlotPos;
		}

		/// <summary>
		/// Constructs a new ItemEquippedArgs
		/// </summary>
		/// <param name="item">The unequipped item</param>
		/// <param name="previousSlotPos">The slot position item had before it was equipped</param>
		public ItemUnequippedArgs(InventoryItem item, int previousSlotPos)
		{
			m_item = item;
			m_previousSlotPos = (eInventorySlot)previousSlotPos;
		}

		/// <summary>
		/// Gets the unequipped item
		/// </summary>
		public InventoryItem Item
		{
			get { return m_item; }
		}

		/// <summary>
		/// Gets the previous slot position
		/// </summary>
		public eInventorySlot PreviousSlotPosition
		{
			get { return m_previousSlotPos; }
		}
	}
}