using System.Collections.Generic;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    /// <summary>
    /// A vault.
    /// </summary>
    public class GameVault : GameStaticItem, IGameInventoryObject
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Number of items a single vault can hold.
        /// </summary>
        private const int VAULT_SIZE = 100;

        /// <summary>
        /// This list holds all the players that are currently viewing
        /// the vault; it is needed to update the contents of the vault
        /// for any one observer if there is a change.
        /// </summary>
        protected Dictionary<string, GamePlayer> _observers = [];

        private ItemCache _itemCache;

        public int Index { get; protected set; }

        /// <summary>
        /// Gets the number of items that can be held in the vault.
        /// </summary>
        public virtual int VaultSize => VAULT_SIZE;

        /// <summary>
        /// What is the first client slot this inventory object uses? This is client window dependent, and for housing vaults we use the housing vault window.
        /// </summary>
        public virtual eInventorySlot FirstClientSlot => eInventorySlot.HousingInventory_First;

        /// <summary>
        /// Last slot of the client window that shows this inventory.
        /// </summary>
        public virtual eInventorySlot LastClientSlot => eInventorySlot.HousingInventory_Last;

        /// <summary>
        /// First slot in the DB.
        /// </summary>
        public virtual int FirstDbSlot => (int) eInventorySlot.HouseVault_First + VaultSize * Index;

        /// <summary>
        /// Last slot in the DB.
        /// </summary>
        public virtual int LastDbSlot => (int) eInventorySlot.HouseVault_First + VaultSize * (Index + 1) - 1;

        private readonly Lock _lock = new();
        public Lock Lock => _lock;

        public GameVault() : base()
        {
            _itemCache = new(this);
        }

        public virtual string GetOwner(GamePlayer player = null)
        {
            if (player == null)
            {
                if (log.IsErrorEnabled)
                    log.Error("GameVault GetOwner(): player cannot be null!");

                return string.Empty;
            }

            return player.InternalID;
        }

        /// <summary>
        /// Do we handle a search?
        /// </summary>
        public bool SearchInventory(GamePlayer player, MarketSearch.SearchData searchData)
        {
            return false;
        }

        /// <summary>
        /// Inventory for this vault.
        /// </summary>
        public virtual Dictionary<int, DbInventoryItem> GetClientInventory(GamePlayer player)
        {
            return _itemCache.GetItems(player);
        }

        public virtual eInventorySlot GetFirstEmptyClientSlot(GamePlayer player)
        {
            eInventorySlot result = FirstClientSlot;
            Dictionary<int, DbInventoryItem> clientInventory = GetClientInventory(player);

            while (result <= LastClientSlot)
            {
                if (!clientInventory.ContainsKey((int) result))
                    return result;

                result++;
            }

            return eInventorySlot.Invalid;
        }

        public virtual eInventorySlot GetFirstPartiallyFullClientSlot(GamePlayer player, DbInventoryItem item)
        {
            eInventorySlot result = FirstClientSlot;
            Dictionary<int, DbInventoryItem> clientInventory = GetClientInventory(player);

            while (result <= LastClientSlot)
            {
                if (clientInventory.TryGetValue((int) result, out DbInventoryItem otherItem) && otherItem.Count < otherItem.MaxCount && otherItem.Name.Equals(item.Name))
                    return result;

                result++;
            }

            // Return the first empty slot if we couldn't find any partially full one.
            return GetFirstEmptyClientSlot(player);
        }

        /// <summary>
        /// Player interacting with this vault.
        /// </summary>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            player.ActiveInventoryObject?.RemoveObserver(player);

            lock (Lock)
            {
                _observers.TryAdd(player.Name, player);
            }

            player.ActiveInventoryObject = this;
            player.Out.SendInventoryItemsUpdate(GetClientInventory(player), eInventoryWindowType.HouseVault);
            return true;
        }

        /// <summary>
        /// List of items in the vault.
        /// </summary>
        public virtual IList<DbInventoryItem> GetDbItems(GamePlayer player)
        {
            WhereClause filterBySlot = DB.Column("SlotPosition").IsGreaterOrEqualTo(FirstDbSlot).And(DB.Column("SlotPosition").IsLessOrEqualTo(LastDbSlot));
            return DOLDB<DbInventoryItem>.SelectObjects(DB.Column("OwnerID").IsEqualTo(GetOwner(player)).And(filterBySlot));
        }

        /// <summary>
        /// Is this a move request for a housing vault?
        /// </summary>
        public virtual bool CanHandleMove(GamePlayer player, eInventorySlot fromSlot, eInventorySlot toSlot)
        {
            if (player == null || player.ActiveInventoryObject != this)
                return false;

            // House Vaults and consignment merchants deliver the same slot numbers
            return GameInventoryObjectExtensions.IsHousingInventorySlot(fromSlot) || GameInventoryObjectExtensions.IsHousingInventorySlot(toSlot);
        }

        /// <summary>
        /// Move an item from, to or inside a house vault. From IGameInventoryObject.
        /// </summary>
        public virtual bool MoveItem(GamePlayer player, eInventorySlot fromSlot, eInventorySlot toSlot, ushort count)
        {
            if (fromSlot == toSlot)
                return false;

            lock (Lock)
            {
                try
                {
                    player.Inventory.Lock.Enter();

                    bool fromHousing = GameInventoryObjectExtensions.IsHousingInventorySlot(fromSlot);
                    bool toHousingInitial = GameInventoryObjectExtensions.IsHousingInventorySlot(toSlot);

                    if (!fromHousing && !toHousingInitial && toSlot is not eInventorySlot.GeneralHousing)
                        return false;

                    if (player.ActiveInventoryObject is not GameVault gameVault)
                    {
                        player.Out.SendMessage("You are not actively viewing a vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        player.Out.SendInventoryItemsUpdate(null);
                        return false;
                    }

                    // Get the item being moved from its source.
                    DbInventoryItem itemInFromSlot = fromHousing ?
                        GetClientInventory(player).GetValueOrDefault((int) fromSlot) :
                        player.Inventory.GetItem(fromSlot);

                    if (itemInFromSlot == null)
                        return false;

                    // Determine destination slot for shift right click moves.
                    if (toSlot is eInventorySlot.GeneralHousing)
                    {
                        if (fromHousing)
                        {
                            toSlot = itemInFromSlot.IsStackable ?
                                player.Inventory.FindFirstPartiallyFullSlot(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack, itemInFromSlot) :
                                player.Inventory.FindFirstEmptySlot(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
                        }
                        else // Moving from Player Inventory -> Vault
                        {
                            toSlot = itemInFromSlot.IsStackable ?
                                GetFirstPartiallyFullClientSlot(player, itemInFromSlot) :
                                GetFirstEmptyClientSlot(player);
                        }
                    }

                    bool toHousing = GameInventoryObjectExtensions.IsHousingInventorySlot(toSlot);

                    // Get the item at the destination, if any.
                    DbInventoryItem itemInToSlot = toHousing ?
                        GetClientInventory(player).GetValueOrDefault((int) toSlot) :
                        player.Inventory.GetItem(toSlot);

                    if (toHousing)
                    {
                        if (!gameVault.CanAddItems(player))
                        {
                            player.Out.SendMessage("You don't have permission to add items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return false;
                        }

                        if (itemInToSlot != null && !gameVault.CanRemoveItems(player))
                        {
                            player.Out.SendMessage("You don't have permission to remove items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return false;
                        }

                        // Non-tradable item check.
                        if (player.Client.Account.PrivLevel == 1 && this is not AccountVault && !itemInFromSlot.IsTradable)
                        {
                            player.Out.SendMessage("You can not put this untradable item into a house vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            player.Out.SendInventoryItemsUpdate(null);
                            return false;
                        }
                    }
                    else if (fromHousing)
                    {
                        if (!gameVault.CanRemoveItems(player))
                        {
                            player.Out.SendMessage("You don't have permission to remove items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return false;
                        }

                        if (itemInToSlot != null)
                        {
                            if (!gameVault.CanAddItems(player))
                            {
                                player.Out.SendMessage("You don't have permission to add items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return false;
                            }

                            // Non-tradable item check for the item being swapped in.
                            if (player.Client.Account.PrivLevel == 1 && this is not AccountVault && !itemInToSlot.IsTradable)
                            {
                                player.Out.SendMessage("You cannot swap with an untradable item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                player.Out.SendInventoryItemsUpdate(null);
                                return false;
                            }
                        }
                    }

                    var updatedItems = GameInventoryObjectExtensions.MoveItem(this, player, fromSlot, toSlot, count);

                    if (updatedItems != null && updatedItems.Count > 0)
                        GameInventoryObjectExtensions.NotifyObservers(this, player, _observers, updatedItems);
                }
                finally
                {
                    if (player.Inventory.Lock.IsHeldByCurrentThread)
                        player.Inventory.Lock.Exit();
                }
            }

            return true;
        }

        public virtual bool OnAddItem(GamePlayer player, DbInventoryItem item, int previousSlot)
        {
            return _itemCache.AddItem(player, item);
        }

        public virtual bool OnRemoveItem(GamePlayer player, DbInventoryItem item, int previousSlot)
        {
            return _itemCache.RemoveItem(player, item, previousSlot);
        }

        public virtual bool OnMoveItem(GamePlayer player, DbInventoryItem firstItem, int previousFirstSlot, DbInventoryItem secondItem, int previousSecondSlot)
        {
            return _itemCache.MoveItems(player, firstItem, previousFirstSlot, secondItem, previousSecondSlot);
        }

        public virtual void OnItemManipulationError(GamePlayer player)
        {
            _itemCache.ForceValidateCache(player);
        }

        public virtual bool SetSellPrice(GamePlayer player, eInventorySlot clientSlot, uint price)
        {
            return true;
        }

        public virtual bool CanView(GamePlayer player)
        {
            return true;
        }

        public virtual bool CanAddItems(GamePlayer player)
        {
            return true;
        }

        public virtual bool CanRemoveItems(GamePlayer player)
        {
            return true;
        }

        public virtual void AddObserver(GamePlayer player)
        {
            _observers.TryAdd(player.Name, player);
        }

        public virtual void RemoveObserver(GamePlayer player)
        {
            _observers.Remove(player.Name);
        }

        protected class ItemCache : ECSGameTimerWrapperBase
        {
            private const int EXPIRES_AFTER = 30000;

            private readonly GameVault _vault;
            private Dictionary<int, DbInventoryItem> _items; // Uses client slots, not DB slots.

            public ItemCache(GameVault vault) : base(vault)
            {
                _vault = vault;
            }

            public bool AddItem(GamePlayer player, DbInventoryItem item)
            {
                lock (_vault.Lock)
                {
                    ValidateCacheInternal(player);
                    int newSlot = GetClientSlotPosition(item.SlotPosition);

                    if (!GameInventoryObjectExtensions.IsHousingInventorySlot((eInventorySlot) newSlot) ||
                        !_items.TryAdd(newSlot, item))
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Error when adding item to cache.");

                        _items = null;
                        return false;
                    }

                    return true;
                }
            }

            public bool RemoveItem(GamePlayer player, DbInventoryItem item, int previousSlot)
            {
                lock (_vault.Lock)
                {
                    ValidateCacheInternal(player);
                    previousSlot = GetClientSlotPosition(previousSlot);

                    if (!GameInventoryObjectExtensions.IsHousingInventorySlot((eInventorySlot) previousSlot) ||
                        !_items.Remove(previousSlot, out DbInventoryItem existingItem) ||
                        existingItem != item)
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Error when removing item from cache.");

                        _items = null;
                        return false;
                    }

                    return true;
                }
            }

            public bool MoveItems(GamePlayer player, DbInventoryItem firstItem, int previousFirstSlot, DbInventoryItem secondItem, int previousSecondSlot)
            {
                lock (_vault.Lock)
                {
                    ValidateCacheInternal(player);

                    if (previousFirstSlot == previousSecondSlot)
                        return true;

                    DbInventoryItem existingFirstItem = null;
                    DbInventoryItem existingSecondItem = null;

                    previousFirstSlot = GetClientSlotPosition(previousFirstSlot);
                    previousSecondSlot = GetClientSlotPosition(previousSecondSlot);

                    if (GameInventoryObjectExtensions.IsHousingInventorySlot((eInventorySlot) previousFirstSlot))
                        _items.TryGetValue(previousFirstSlot, out existingFirstItem);

                    if (GameInventoryObjectExtensions.IsHousingInventorySlot((eInventorySlot) previousSecondSlot))
                        _items.TryGetValue(previousSecondSlot, out existingSecondItem);

                    if ((existingFirstItem != null && existingFirstItem != firstItem) || (existingSecondItem != null && existingSecondItem != secondItem))
                    {
                        if (log.IsErrorEnabled)
                            log.Error("Error when moving items inside cache.");

                        _items = null;
                        return false;
                    }

                    if (firstItem != null && secondItem != null)
                    {
                        _items[previousFirstSlot] = secondItem;
                        _items[previousSecondSlot] = firstItem;
                    }
                    else if (firstItem != null && secondItem == null)
                    {
                        _items.Remove(previousFirstSlot);
                        _items[previousSecondSlot] = firstItem;
                    }
                    else if (firstItem == null && secondItem != null)
                    {
                        _items.Remove(previousSecondSlot);
                        _items[previousFirstSlot] = secondItem;
                    }
                }

                return true;
            }

            public Dictionary<int, DbInventoryItem> GetItems(GamePlayer player)
            {
                lock (_vault.Lock)
                {
                    ValidateCacheInternal(player);
                }

                return _items;
            }

            public void ForceValidateCache(GamePlayer player)
            {
                OnTick(this);
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                lock (_vault.Lock)
                {
                    // Simply discard the list in case `GetItems` is called from the timer service.
                    _items = null;
                }

                return 0;
            }

            private void ValidateCacheInternal(GamePlayer player)
            {
                // Always refresh or start the timer.
                Start(EXPIRES_AFTER);

                if (_items != null)
                    return;

                _items = new();

                foreach (DbInventoryItem item in _vault.GetDbItems(player))
                {
                    int slotPosition = GetClientSlotPosition(item.SlotPosition);

                    if (!_items.TryAdd(slotPosition, item))
                    {
                        if (log.IsErrorEnabled)
                            log.Error($"Error during cache validation. Slot already taken. (Added: {_items[slotPosition]}) (Rejected: {item}) (Player {player})");
                    }
                }
            }

            private int GetClientSlotPosition(int slot)
            {
                int offset = -_vault.FirstDbSlot + (int) eInventorySlot.HousingInventory_First;
                return offset + slot;
            }
        }
    }
}
