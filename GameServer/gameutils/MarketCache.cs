using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.Logging;

namespace DOL.GS
{
    public static class MarketCache
    {
        private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static MarketSearchEngine _searchEngine = new();

        public static int ItemCount => _searchEngine.ItemCount;

        public static bool Initialize()
        {
            if (log.IsInfoEnabled)
                log.Info("Building Market Cache...");

            if (_searchEngine != null)
            {
                _searchEngine.Dispose();
                _searchEngine = new();
            }

            try
            {
                WhereClause filterBySlot = DB.Column("SlotPosition").IsGreaterOrEqualTo((int)eInventorySlot.Consignment_First).And(DB.Column("SlotPosition").IsLessOrEqualTo((int)eInventorySlot.Consignment_Last));
                IList<DbInventoryItem> list = DOLDB<DbInventoryItem>.SelectObjects(filterBySlot.And(DB.Column("OwnerLot").IsGreaterThan(0)));

                foreach (DbInventoryItem item in list)
                    _searchEngine.AddItem(GameInventoryItem.Create(item));

                if (log.IsInfoEnabled)
                    log.Info($"Market Cache initialized with {_searchEngine.ItemCount} items.");
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.Error("Failed to initialize Market Cache.", ex);

                return false;
            }

            return true;
        }

        public static bool AddItem(DbInventoryItem item)
        {
            if (item == null)
                return false;

            if (!_searchEngine.AddItem(item))
            {
                if (log.IsErrorEnabled)
                    log.Error($"Attempted to add duplicate item to Market Cache {item.ObjectId}");

                return false;
            }

            return true;
        }

        public static bool RemoveItem(DbInventoryItem item)
        {
            if (item == null)
                return false;

            return _searchEngine.RemoveItem(item);
        }

        public static IEnumerable<DbInventoryItem> SearchItems(in ItemQuery query)
        {
            return _searchEngine.Search(query);
        }
    }
}
