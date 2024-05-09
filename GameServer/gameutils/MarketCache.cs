using System.Collections.Generic;
using DOL.Database;
using log4net;

namespace DOL.GS
{
	public class MarketCache
	{
		private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static Dictionary<string, DbInventoryItem> m_itemCache = null;

		private static object CacheLock = new object();

		/// <summary>
		/// Return a List of all items in the cache
		/// </summary>
		public static List<DbInventoryItem> Items
		{
			get { return new List<DbInventoryItem>(m_itemCache.Values); }
		}


		/// <summary>
		/// Load or reload all items into the market cache
		/// </summary>
		public static bool Initialize()
		{
			log.Info("Building Market Cache ....");
			try
			{
				m_itemCache = new Dictionary<string, DbInventoryItem>();

				var filterBySlot = DB.Column("SlotPosition").IsGreaterOrEqualTo((int)eInventorySlot.Consignment_First).And(DB.Column("SlotPosition").IsLessOrEqualTo((int)eInventorySlot.Consignment_Last));
				var list = DOLDB<DbInventoryItem>.SelectObjects(filterBySlot.And(DB.Column("OwnerLot").IsGreatherThan(0)));

				foreach (DbInventoryItem item in list)
				{
					GameInventoryItem playerItem = GameInventoryItem.Create(item);
					m_itemCache.Add(item.ObjectId, playerItem);
				}

				log.Info("Market Cache initialized with " + m_itemCache.Count + " items.");
			}
			catch
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Add an item to the market cache
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public static bool AddItem(DbInventoryItem item)
		{
			bool added = false;

			if (item != null && item.OwnerID != null)
			{
				lock (CacheLock)
				{
					if (m_itemCache.ContainsKey(item.ObjectId) == false)
					{
						m_itemCache.Add(item.ObjectId, item);
						added = true;
					}
					else
					{
						log.Error("Attempted to add duplicate item to Market Cache " + item.ObjectId);
					}
				}
			}

			return added;
		}

		/// <summary>
		/// Remove an item from the market cache
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public static bool RemoveItem(DbInventoryItem item)
		{
			if (item == null)
				return false;

			lock (CacheLock)
			{
				return m_itemCache.Remove(item.ObjectId);
			}
		}
	}
}
