using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.GS.Database;
using Core.GS.Enums;
using log4net;

namespace Core.GS.Players;

/// <summary>
/// This class represents a full merchant item list
/// and contains functions that can be used to
/// add and remove items
/// </summary>
public class MerchantTradeItems
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// The maximum number of items on one page
	/// </summary>
	public const byte MAX_ITEM_IN_TRADEWINDOWS = 30;

	/// <summary>
	/// The maximum number of pages supported by clients
	/// </summary>
	public const int MAX_PAGES_IN_TRADEWINDOWS = 5;

	#region Constructor/Declaration

	// for client one page is 30 items, just need to use scrollbar to see them all
	// item30 will be on page 0
	// item31 will be on page 1

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="itemsListId"></param>
	public MerchantTradeItems(string itemsListId)
	{
		m_itemsListID = itemsListId;
	}

	/// <summary>
	/// Item list id
	/// </summary>
	protected string m_itemsListID;

	/// <summary>
	/// Item list id
	/// </summary>
	public string ItemsListID
	{
		get { return m_itemsListID; }
	}

	/// <summary>
	/// Holds item template instances defined with script
	/// </summary>
	protected HybridDictionary m_usedItemsTemplates = new HybridDictionary();

	#endregion

	#region Add Trade Item

	/// <summary>
	/// Adds an item to the merchant item list
	/// </summary>
	/// <param name="page">Zero-based page number</param>
	/// <param name="slot">Zero-based slot number</param>
	/// <param name="item">The item template to add</param>
	public virtual bool AddTradeItem(int page, EMerchantWindowSlot slot, DbItemTemplate item)
	{
		lock (m_usedItemsTemplates.SyncRoot)
		{
			if (item == null)
			{
				return false;
			}

			EMerchantWindowSlot pageSlot = GetValidSlot(page, slot);

			if (pageSlot == EMerchantWindowSlot.Invalid)
			{
				log.ErrorFormat("Invalid slot {0} specified for page {1} of TradeItemList {2}", slot, page, ItemsListID);
				return false;
			}

			m_usedItemsTemplates[(page*MAX_ITEM_IN_TRADEWINDOWS)+(int)pageSlot] = item;
		}

		return true;
	}

	/// <summary>
	/// Removes an item from trade window
	/// </summary>
	/// <param name="page">Zero-based page number</param>
	/// <param name="slot">Zero-based slot number</param>
	/// <returns>true if removed</returns>
	public virtual bool RemoveTradeItem(int page, EMerchantWindowSlot slot)
	{
		lock (m_usedItemsTemplates.SyncRoot)
		{
			slot = GetValidSlot(page, slot);
			if (slot == EMerchantWindowSlot.Invalid) return false;
			if (!m_usedItemsTemplates.Contains((page*MAX_ITEM_IN_TRADEWINDOWS)+(int)slot)) return false;
			m_usedItemsTemplates.Remove((page*MAX_ITEM_IN_TRADEWINDOWS)+(int)slot);
			return true;
		}
	}

	#endregion

	#region Get Inventory Informations

	/// <summary>
	/// Get the list of all items in the specified page
	/// </summary>
	public virtual IDictionary GetItemsInPage(int page)
	{
		try
		{
			HybridDictionary itemsInPage = new HybridDictionary(MAX_ITEM_IN_TRADEWINDOWS);
			if (m_itemsListID != null && m_itemsListID.Length > 0)
			{
				var itemList = CoreDb<DbMerchantItem>.SelectObjects(DB.Column("ItemListID").IsEqualTo(m_itemsListID).And(DB.Column("PageNumber").IsEqualTo(page)));
				foreach (DbMerchantItem merchantitem in itemList)
				{
					DbItemTemplate item = GameServer.Database.FindObjectByKey<DbItemTemplate>(merchantitem.ItemTemplateID);
                    if (item != null)
                    {
                        DbItemTemplate slotItem = (DbItemTemplate)itemsInPage[merchantitem.SlotPosition];
                        if (slotItem == null)
                        {
                            itemsInPage.Add(merchantitem.SlotPosition, item);
                        }
                        else
                        {
                            log.ErrorFormat("two merchant items on same page/slot: listID={0} page={1} slot={2}", m_itemsListID, page, merchantitem.SlotPosition);
                        }
                    }
                    else
                    {
                        log.ErrorFormat("Item template with ID = '{0}' not found for merchant item list '{1}'", 
                            merchantitem.ItemTemplateID, ItemsListID);
                    }
				}
			}
			lock (m_usedItemsTemplates.SyncRoot)
			{
				foreach (DictionaryEntry de in m_usedItemsTemplates)
				{
					if ((int)de.Key >= (MAX_ITEM_IN_TRADEWINDOWS*page) && (int)de.Key < (MAX_ITEM_IN_TRADEWINDOWS*page+MAX_ITEM_IN_TRADEWINDOWS))
						itemsInPage[(int)de.Key%MAX_ITEM_IN_TRADEWINDOWS] = (DbItemTemplate)de.Value;
				}
			}
			return itemsInPage;
		}
		catch (Exception e)
		{
			if (log.IsErrorEnabled)
				log.Error("Loading merchant items list (" + m_itemsListID + ") page (" + page + "): ", e);
			return new HybridDictionary();
		}
	}

	/// <summary>
	/// Get the item in the specified page and slot
	/// </summary>
	/// <param name="page">The item page</param>
	/// <param name="slot">The item slot</param>
	/// <returns>Item template or null</returns>
	public virtual DbItemTemplate GetItem(int page, EMerchantWindowSlot slot)
	{
		try
		{
			slot = GetValidSlot(page, slot);
			if (slot == EMerchantWindowSlot.Invalid) return null;

			DbItemTemplate item;
			lock (m_usedItemsTemplates.SyncRoot)
			{
				item = m_usedItemsTemplates[(int)slot+(page*MAX_ITEM_IN_TRADEWINDOWS)] as DbItemTemplate;
				if (item != null) return item;
			}

			if (m_itemsListID != null && m_itemsListID.Length > 0)
			{
				var itemToFind = CoreDb<DbMerchantItem>.SelectObject(DB.Column("ItemListID").IsEqualTo(m_itemsListID).And(DB.Column("PageNumber").IsEqualTo(page)).And(DB.Column("SlotPosition").IsEqualTo((int)slot)));
				if (itemToFind != null)
				{
					item = GameServer.Database.FindObjectByKey<DbItemTemplate>(itemToFind.ItemTemplateID);
				}
			}
			return item;
		}
		catch (Exception e)
		{
			if (log.IsErrorEnabled)
				log.Error("Loading merchant items list (" + m_itemsListID + ") page (" + page + ") slot (" + slot + "): ", e);
			return null;
		}
	}

	/// <summary>
	/// Gets a copy of all intems in trade window
	/// </summary>
	/// <returns>A list where key is the slot position and value is the ItemTemplate</returns>
	public virtual IDictionary GetAllItems()
	{
		try
		{
			Hashtable allItems = new Hashtable();
			if (m_itemsListID != null && m_itemsListID.Length > 0)
			{
				var itemList = CoreDb<DbMerchantItem>.SelectObjects(DB.Column("ItemListID").IsEqualTo(m_itemsListID));
				foreach (DbMerchantItem merchantitem in itemList)
				{
					DbItemTemplate item = GameServer.Database.FindObjectByKey<DbItemTemplate>(merchantitem.ItemTemplateID);
					if (item != null)
					{
						DbItemTemplate slotItem = (DbItemTemplate)allItems[merchantitem.SlotPosition];
						if (slotItem == null)
						{
							allItems.Add(merchantitem.SlotPosition, item);
						}
						else
						{
							log.ErrorFormat("two merchant items on same page/slot: listID={0} page={1} slot={2}", m_itemsListID, merchantitem.PageNumber, merchantitem.SlotPosition);
						}
					}
				}
			}

			lock (m_usedItemsTemplates.SyncRoot)
			{
				foreach (DictionaryEntry de in m_usedItemsTemplates)
				{
					allItems[(int)de.Key] = (DbItemTemplate)de.Value;
				}
			}
			return allItems;
		}
		catch (Exception e)
		{
			if (log.IsErrorEnabled)
				log.Error("Loading merchant items list (" + m_itemsListID + "):", e);
			return new HybridDictionary();
		}
	}

	/// <summary>
	/// Check if the slot is valid
	/// </summary>
	/// <param name="page">Zero-based page number</param>
	/// <param name="slot">SlotPosition to check</param>
	/// <returns>the slot if it's valid or eMerchantWindowSlot.Invalid if not</returns>
	public virtual EMerchantWindowSlot GetValidSlot(int page, EMerchantWindowSlot slot)
	{
		if (page < 0 || page >= MAX_PAGES_IN_TRADEWINDOWS) return EMerchantWindowSlot.Invalid;

		if (slot == EMerchantWindowSlot.FirstEmptyInPage)
		{
			IDictionary itemsInPage = GetItemsInPage(page);
			for (int i = (int)EMerchantWindowSlot.FirstInPage; i < (int)EMerchantWindowSlot.LastInPage; i++)
			{
				if (!itemsInPage.Contains(i))
					return ((EMerchantWindowSlot)i);
			}
			return EMerchantWindowSlot.Invalid;
		}

		if (slot < EMerchantWindowSlot.FirstInPage || slot > EMerchantWindowSlot.LastInPage)
			return EMerchantWindowSlot.Invalid;

		return slot;
	}

	#endregion
}