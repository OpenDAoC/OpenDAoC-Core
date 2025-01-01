using System;
using System.Collections.Generic;
using System.Threading;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class MarketExplorer : GameNPC, IGameInventoryObject
    {
        private static new readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string EXPLORER_ITEM_LIST = "MarketExplorerItems";

        private readonly Lock _lock = new();
        public Lock Lock => _lock;

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            if (player.ActiveInventoryObject != null)
            {
                player.ActiveInventoryObject.RemoveObserver(player);
                player.ActiveInventoryObject = null;
            }

            if (ServerProperties.Properties.MARKET_ENABLE)
            {
                player.ActiveInventoryObject = this;
                player.Out.SendMarketExplorerWindow();
            }
            else
                player.Out.SendMessage("Sorry, the market is not available at this time.", eChatType.CT_Staff, eChatLoc.CL_SystemWindow);

            return true;
        }

        public virtual string GetOwner(GamePlayer player)
        {
            return player.InternalID;
        }

        public virtual Dictionary<int, DbInventoryItem> GetClientInventory(GamePlayer player)
        {
            return null;
        }

        /// <summary>
        /// List of items in this objects inventory
        /// </summary>
        public virtual IList<DbInventoryItem> DBItems(GamePlayer player = null)
        {
            return MarketCache.Items;
        }

        /// <summary>
        /// First slot of the client window that shows this inventory
        /// </summary>
        public virtual eInventorySlot FirstClientSlot => eInventorySlot.MarketExplorerFirst;

        /// <summary>
        /// Last slot of the client window that shows this inventory
        /// </summary>
        public virtual eInventorySlot LastClientSlot => eInventorySlot.MarketExplorerFirst + 39;

        /// <summary>
        /// First slot in the DB.
        /// </summary>
        public virtual int FirstDbSlot => (int) eInventorySlot.Consignment_First;

        /// <summary>
        /// Last slot in the DB.
        /// </summary>
        public virtual int LastDbSlot => (int) eInventorySlot.Consignment_Last;

        public static eRealm GetRealmOfLot(ushort houseNumber)
        {
            if (houseNumber <= 1382)
                return eRealm.Albion;
            else if (houseNumber <= 2573)
                return eRealm.Midgard;
            else if (houseNumber <= 4398)
                return eRealm.Hibernia;

            return eRealm.None;
        }

        /// <summary>
        /// Search the MarketCache.
        /// </summary>
        public virtual bool SearchInventory(GamePlayer player, MarketSearch.SearchData searchData)
        {
            MarketSearch marketSearch = new(player);

            if (marketSearch.FindItemsInList(DBItems(), searchData) is List<DbInventoryItem> items)
            {
                int maxPerPage = 20;
                byte maxPages = (byte) (Math.Ceiling((double) items.Count / maxPerPage) - 1);
                int first = searchData.page * maxPerPage;
                int last = first + maxPerPage;
                List<DbInventoryItem> list = [];
                int index = 0;

                foreach (DbInventoryItem item in items)
                {
                    if (index >= first && index <= last)
                    {
                        if (GetRealmOfLot(item.OwnerLot) != player.Realm)
                        {
                            if (ServerProperties.Properties.MARKET_ENABLE_LOG)
                                log.Debug($"Not adding item '{item.Name}' to the return search since its from different realm.");
                        }
                        else
                            list.Add(item);
                    }

                    index++;
                }

                if (ServerProperties.Properties.MARKET_ENABLE_LOG)
                    log.Debug($"Current list find size is '{list.Count}'.");

                if (searchData.page == 0)
                    player.Out.SendMessage($"Items returned: {items.Count}", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

                if (items.Count == 0)
                    player.Out.SendMarketExplorerWindow(list, 0, 0);
                else if (searchData.page <= maxPages)
                {
                    player.Out.SendMessage($"Moving to page {searchData.page + 1}.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    player.Out.SendMarketExplorerWindow(list, searchData.page, maxPages);
                }

                // Save the last search list in case we buy an item from it.
                player.TempProperties.SetProperty(EXPLORER_ITEM_LIST, list);
            }
            else if (ServerProperties.Properties.MARKET_ENABLE_LOG)
                log.Debug("There is something wrong with the returned search...");

            return true;
        }

        /// <summary>
        /// Is this a move request for a market explorer.
        /// </summary>
        public virtual bool CanHandleMove(GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot)
        {
            if (player == null || player.ActiveInventoryObject != this)
                return false;

            return fromClientSlot >= FirstClientSlot && GameInventoryObjectExtensions.IsBackpackSlot(toClientSlot);
        }

        /// <summary>
        /// Move item from market explorer.
        /// </summary>
        public virtual bool MoveItem(GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot, ushort count)
        {
            // this move represents a buy item request
            if (fromClientSlot >= eInventorySlot.MarketExplorerFirst &&
                GameInventoryObjectExtensions.IsBackpackSlot(toClientSlot) &&
                player.ActiveInventoryObject == this)
            {
                List<DbInventoryItem> list = player.TempProperties.GetProperty<List<DbInventoryItem>>(EXPLORER_ITEM_LIST);

                if (list == null)
                    return false;

                int itemSlot = (int) fromClientSlot - (int) eInventorySlot.MarketExplorerFirst;
                DbInventoryItem item = list[itemSlot];
                BuyItem(item, player);
                return true;
            }

            return false;
        }

        public virtual bool OnAddItem(GamePlayer player, DbInventoryItem item)
        {
            return true;
        }

        public virtual bool SetSellPrice(GamePlayer player, eInventorySlot clientSlot, uint price)
        {
            return true;
        }

        public virtual bool OnRemoveItem(GamePlayer player, DbInventoryItem item)
        {
            return true;
        }

        public virtual void BuyItem(DbInventoryItem item, GamePlayer player)
        {
            GameConsignmentMerchant consignmentMerchant = HouseMgr.GetConsignmentByHouseNumber(item.OwnerLot);

            if (consignmentMerchant == null)
            {
                player.Out.SendMessage("I can't find the consignment merchant for this item!", eChatType.CT_Merchant, eChatLoc.CL_ChatWindow);
                log.Error($"ME: Error finding consignment merchant for lot {item.OwnerLot}; {player.Name}:{player.Client.Account.Name} trying to buy {item.Name}");
                return;
            }

            player.ActiveInventoryObject?.RemoveObserver(player);
            player.ActiveInventoryObject = consignmentMerchant;
            player.Out.SendInventoryItemsUpdate(consignmentMerchant.GetClientInventory(player), eInventoryWindowType.ConsignmentViewer);
            consignmentMerchant.AddObserver(player);
        }

        public virtual void AddObserver(GamePlayer player) { }

        public virtual void RemoveObserver(GamePlayer player) { }
    }
}
