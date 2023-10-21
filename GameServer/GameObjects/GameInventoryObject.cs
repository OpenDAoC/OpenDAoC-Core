using System.Collections.Generic;
using Core.Database;

namespace Core.GS
{
	/// <summary>
	/// Interface for a GameInventoryObject
	/// This is an object or NPC that can interact with a players inventory, buy, or sell items
	/// </summary>		
	public interface IGameInventoryObject
	{
		object LockObject();

		int FirstClientSlot { get; }
		int LastClientSlot { get; }
		int FirstDBSlot { get; }
		int LastDBSlot { get; }
		string GetOwner(GamePlayer player);
		IList<DbInventoryItem> DBItems(GamePlayer player = null);
		Dictionary<int, DbInventoryItem> GetClientInventory(GamePlayer player);
		bool CanHandleMove(GamePlayer player, ushort fromClientSlot, ushort toClientSlot);
		bool MoveItem(GamePlayer player, ushort fromClientSlot, ushort toClientSlot);
		bool OnAddItem(GamePlayer player, DbInventoryItem item);
		bool OnRemoveItem(GamePlayer player, DbInventoryItem item);
		bool SetSellPrice(GamePlayer player, ushort clientSlot, uint sellPrice);
		bool SearchInventory(GamePlayer player, MarketSearch.SearchData searchData);
		void AddObserver(GamePlayer player);
		void RemoveObserver(GamePlayer player);
	}

	/// <summary>
	/// This is an extension class for GameInventoryObjects.  It's a way to get around the fact C# doesn't support multiple inheritance. 
	/// We want the ability for a GameInventoryObject to be a game static object, or an NPC, or anything else, and yet still contain common functionality 
	/// for an inventory object with code written in just one place
	/// </summary>
	public static class GameInventoryObjectExtensions
	{
		public const string ITEM_BEING_ADDED = "ItemBeingAddedToObject";
		public const string TEMP_SEARCH_KEY = "TempSearchKey";

		/// <summary>
		/// Can this object handle the move request?
		/// </summary>
		public static bool CanHandleRequest(this IGameInventoryObject thisObject, GamePlayer player, ushort fromClientSlot, ushort toClientSlot)
		{
			// make sure from or to slots involve this object
			if ((fromClientSlot >= thisObject.FirstClientSlot && fromClientSlot <= thisObject.LastClientSlot) ||
				(toClientSlot >= thisObject.FirstClientSlot && toClientSlot <= thisObject.LastClientSlot))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Get the items of this object, mapped to the client inventory slots
		/// </summary>
		public static Dictionary<int, DbInventoryItem> GetClientItems(this IGameInventoryObject thisObject, GamePlayer player)
		{
			var inventory = new Dictionary<int, DbInventoryItem>();
			int slotOffset = thisObject.FirstClientSlot - thisObject.FirstDBSlot;
			foreach (DbInventoryItem item in thisObject.DBItems(player))
			{
				if (item != null)
				{
					if (!inventory.ContainsKey(item.SlotPosition + slotOffset))
					{
						inventory.Add(item.SlotPosition + slotOffset, item);
					}
				}
			}

			return inventory;
		}


		/// <summary>
		/// Move an item from the inventory object to a player's backpack (uses client slots)
		/// </summary>
		public static IDictionary<int, DbInventoryItem> MoveItemFromObject(this IGameInventoryObject thisObject, GamePlayer player, EInventorySlot fromClientSlot, EInventorySlot toClientSlot)
		{
			// We will only allow moving to the backpack.

			if (toClientSlot < EInventorySlot.FirstBackpack || toClientSlot > EInventorySlot.LastBackpack)
				return null;

			lock (thisObject.LockObject())
			{
				Dictionary<int, DbInventoryItem> inventory = thisObject.GetClientInventory(player);

				if (inventory.ContainsKey((int)fromClientSlot) == false)
				{
					ChatUtil.SendErrorMessage(player, "Item not found in slot " + (int)fromClientSlot);
					return null;
				}

				DbInventoryItem fromItem = inventory[(int)fromClientSlot];
				DbInventoryItem toItem = player.Inventory.GetItem(toClientSlot);

				// if there is an item in the players target inventory slot then move it to the object
				if (toItem != null)
				{
					player.Inventory.RemoveTradeItem(toItem);
					toItem.SlotPosition = fromItem.SlotPosition;
					toItem.OwnerID = thisObject.GetOwner(player);
					thisObject.OnAddItem(player, toItem);
					GameServer.Database.SaveObject(toItem);
				}

				thisObject.OnRemoveItem(player, fromItem);

				// Create the GameInventoryItem from this InventoryItem.  This simply wraps the InventoryItem, 
				// which is still updated when this item is moved around
				DbInventoryItem objectItem = GameInventoryItem.Create(fromItem);

				player.Inventory.AddTradeItem(toClientSlot, objectItem);

				var updateItems = new Dictionary<int, DbInventoryItem>(1);
				updateItems.Add((int)fromClientSlot, toItem);

				return updateItems;
			}
		}

		/// <summary>
		/// Move an item from a player's backpack to this inventory object (uses client slots)
		/// </summary>
		public static IDictionary<int, DbInventoryItem> MoveItemToObject(this IGameInventoryObject thisObject, GamePlayer player, EInventorySlot fromClientSlot, EInventorySlot toClientSlot)
		{
			// We will only allow moving from the backpack.

			if (fromClientSlot < EInventorySlot.FirstBackpack || fromClientSlot > EInventorySlot.LastBackpack)
				return null;

			DbInventoryItem fromItem = player.Inventory.GetItem(fromClientSlot);

			if (fromItem == null)
				return null;

			lock (thisObject.LockObject())
			{
				Dictionary<int, DbInventoryItem> inventory = thisObject.GetClientInventory(player);

				player.Inventory.RemoveTradeItem(fromItem);

				// if there is an item in the objects target slot then move it to the players inventory
				if (inventory.ContainsKey((int)toClientSlot))
				{
					DbInventoryItem toItem = inventory[(int)toClientSlot];
					thisObject.OnRemoveItem(player, toItem);
					player.Inventory.AddTradeItem(fromClientSlot, toItem);
				}

				fromItem.OwnerID = thisObject.GetOwner(player);
				fromItem.SlotPosition = (int)(toClientSlot) - (int)(thisObject.FirstClientSlot) + thisObject.FirstDBSlot;
				thisObject.OnAddItem(player, fromItem);
				GameServer.Database.SaveObject(fromItem);

				var updateItems = new Dictionary<int, DbInventoryItem>(1);
				updateItems.Add((int)toClientSlot, fromItem);

				// for objects that support doing something when added (setting a price, for example)
				player.TempProperties.SetProperty(ITEM_BEING_ADDED, fromItem);

				return updateItems;
			}
		}

		/// <summary>
		/// Move an item around inside this object (uses client slots)
		/// </summary>
		public static IDictionary<int, DbInventoryItem> MoveItemInsideObject(this IGameInventoryObject thisObject, GamePlayer player, EInventorySlot fromSlot, EInventorySlot toSlot)
		{
			lock (thisObject.LockObject())
			{
				IDictionary<int, DbInventoryItem> inventory = thisObject.GetClientInventory(player);

				if (!inventory.ContainsKey((int)fromSlot))
					return null;

				var updateItems = new Dictionary<int, DbInventoryItem>(2);
				DbInventoryItem fromItem = null, toItem = null;

				fromItem = inventory[(int)fromSlot];

				if (inventory.ContainsKey((int)toSlot))
				{
					toItem = inventory[(int)toSlot];
					toItem.SlotPosition = fromItem.SlotPosition;

					GameServer.Database.SaveObject(toItem);
				}

				fromItem.SlotPosition = (int)toSlot - (int)(thisObject.FirstClientSlot) + thisObject.FirstDBSlot;
				GameServer.Database.SaveObject(fromItem);

				updateItems.Add((int)fromSlot, toItem);
				updateItems.Add((int)toSlot, fromItem);

				return updateItems;
			}
		}
	}
}
