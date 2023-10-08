using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// The use type applyed to the item:
	/// click on icon in quickbar, /use or /use2
	/// </summary>
	public enum EUseType
	{
		clic = 0,
		use1 = 1,
		use2 = 2,
	}
	
	/// <summary>
	/// Interface for GameInventory
	/// </summary>		
	public interface IGameInventory
	{
		bool            LoadFromDatabase(string inventoryID);
		bool            SaveIntoDatabase(string inventoryID);

		bool			AddItem(EInventorySlot slot, DbInventoryItem item);
						/// <summary>
						/// Add an item to Inventory and save.  This assumes item is already in the database and is being transferred.
						/// </summary>
						/// <param name="slot"></param>
						/// <param name="item"></param>
						/// <returns></returns>
		bool			AddTradeItem(EInventorySlot slot, DbInventoryItem item);
		bool			AddCountToStack(DbInventoryItem item, int count);
		bool			AddTemplate(DbInventoryItem template, int count, EInventorySlot minSlot, EInventorySlot maxSlot);
		bool            RemoveItem(DbInventoryItem item);
						/// <summary>
						/// Remove an item from Inventory and update owner and position but do not remove from the database.
						/// This is use for transferring items.
						/// </summary>
						/// <param name="item"></param>
						/// <returns></returns>
		bool            RemoveTradeItem(DbInventoryItem item);
		bool			RemoveCountFromStack(DbInventoryItem item, int count);
		bool			RemoveTemplate(string templateID, int count, EInventorySlot minSlot, EInventorySlot maxSlot);
		bool            MoveItem(EInventorySlot fromSlot, EInventorySlot toSlot, int itemCount);
		DbInventoryItem   GetItem(EInventorySlot slot);
		ICollection<DbInventoryItem> GetItemRange(EInventorySlot minSlot, EInventorySlot maxSlot);

		void            BeginChanges();
		void            CommitChanges();
		void			ClearInventory();

		int				CountSlots(bool countUsed, EInventorySlot minSlot, EInventorySlot maxSlot);
		int				CountItemTemplate(string itemtemplateID, EInventorySlot minSlot, EInventorySlot maxSlot);
		bool			IsSlotsFree(int count, EInventorySlot minSlot, EInventorySlot maxSlot);
		
		EInventorySlot	FindFirstEmptySlot(EInventorySlot first, EInventorySlot last);
		EInventorySlot	FindLastEmptySlot(EInventorySlot first, EInventorySlot last);
		EInventorySlot	FindFirstFullSlot(EInventorySlot first, EInventorySlot last);
		EInventorySlot	FindLastFullSlot(EInventorySlot first, EInventorySlot last);

		DbInventoryItem	GetFirstItemByID(string uniqueID, EInventorySlot minSlot, EInventorySlot maxSlot);
		DbInventoryItem	GetFirstItemByObjectType(int objectType, EInventorySlot minSlot, EInventorySlot maxSlot);
		DbInventoryItem   GetFirstItemByName(string name ,EInventorySlot minSlot, EInventorySlot maxSlot);

		ICollection<DbInventoryItem> VisibleItems		{ get; }
		ICollection<DbInventoryItem> EquippedItems	{ get; }
		ICollection<DbInventoryItem> AllItems			{ get; }

		int InventoryWeight { get; }
	}
}
