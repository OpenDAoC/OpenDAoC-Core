using System.Collections.Generic;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Logging;

namespace DOL.GS
{
    public class VaultItemCache : ECSGameTimerWrapperBase
    {
        private static readonly Logger log = LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const int EXPIRES_AFTER = 30000;

        private readonly Dictionary<string, GamePlayer> _observers = new();
        private readonly VaultItemCacheManager.VaultItemCacheKey _key;
        private readonly Lock _lock = new();
        private Dictionary<int, DbInventoryItem> _items; // Key = client slots.
        private bool _isDisposed;

        public VaultItemCache(VaultItemCacheManager.VaultItemCacheKey key) : base(null)
        {
            _key = key;
        }

        public void AddObserver(GamePlayer player)
        {
            lock (_lock)
            {
                _observers.TryAdd(player.Name, player);
            }
        }

        public void RemoveObserver(GamePlayer player)
        {
            lock (_lock)
            {
                _observers.Remove(player.Name);
            }
        }

        public bool MoveItem(GamePlayer player, GameVault vault, eInventorySlot fromSlot, eInventorySlot toSlot, ushort count)
        {
            VaultItemCache newCache = null;

            lock (_lock)
            {
                if (!_isDisposed)
                {
                    ValidateCacheInternal(vault);

                    // We are protected by the cache lock, now enter the player's inventory lock
                    // Preserves old lock order: InventoryObject -> PlayerInventory.
                    player.Inventory.Lock.Enter();

                    try
                    {
                        bool fromHousing = GameInventoryObjectExtensions.IsHousingInventorySlot(fromSlot);
                        bool toHousingInitial = GameInventoryObjectExtensions.IsHousingInventorySlot(toSlot);

                        if (!fromHousing && !toHousingInitial && toSlot is not eInventorySlot.GeneralHousing)
                            return false;

                        if (player.ActiveInventoryObject != vault)
                        {
                            player.Out.SendMessage("You are not actively viewing a vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            player.Out.SendInventoryItemsUpdate(null);
                            return false;
                        }

                        DbInventoryItem itemInFromSlot = fromHousing ?
                            _items.GetValueOrDefault((int)fromSlot) :
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
                            else
                            {
                                toSlot = itemInFromSlot.IsStackable ?
                                    GetFirstPartiallyFullClientSlot(vault, itemInFromSlot) :
                                    GetFirstEmptyClientSlot(vault);
                            }
                        }

                        bool toHousing = GameInventoryObjectExtensions.IsHousingInventorySlot(toSlot);

                        DbInventoryItem itemInToSlot = toHousing ?
                            _items.GetValueOrDefault((int)toSlot) :
                            player.Inventory.GetItem(toSlot);

                        if (toHousing)
                        {
                            if (!vault.CanAddItems(player))
                            {
                                player.Out.SendMessage("You don't have permission to add items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return false;
                            }

                            if (itemInToSlot != null && !vault.CanRemoveItems(player))
                            {
                                player.Out.SendMessage("You don't have permission to remove items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return false;
                            }

                            if (player.Client.Account.PrivLevel == 1 && vault is not AccountVault && !itemInFromSlot.IsTradable)
                            {
                                player.Out.SendMessage("You can not put this untradable item into a house vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                player.Out.SendInventoryItemsUpdate(null);
                                return false;
                            }
                        }
                        else if (fromHousing)
                        {
                            if (!vault.CanRemoveItems(player))
                            {
                                player.Out.SendMessage("You don't have permission to remove items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return false;
                            }

                            if (itemInToSlot != null)
                            {
                                if (!vault.CanAddItems(player))
                                {
                                    player.Out.SendMessage("You don't have permission to add items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return false;
                                }

                                if (player.Client.Account.PrivLevel == 1 && vault is not AccountVault && !itemInToSlot.IsTradable)
                                {
                                    player.Out.SendMessage("You cannot swap with an untradable item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    player.Out.SendInventoryItemsUpdate(null);
                                    return false;
                                }
                            }
                        }

                        // Calling extension method. Inside this, it might trigger OnAddItem/OnRemoveItem/OnMoveItem.
                        var updatedItems = GameInventoryObjectExtensions.MoveItemInternal(vault, player, fromSlot, toSlot, count);

                        if (updatedItems.Count > 0)
                            GameInventoryObjectExtensions.NotifyObservers(player, _observers, updatedItems);

                        return true;
                    }
                    finally
                    {
                        if (player.Inventory.Lock.IsHeldByCurrentThread)
                            player.Inventory.Lock.Exit();
                    }
                }

                newCache = VaultItemCacheManager.GetCache(_key);
            }

            return newCache != null && newCache.MoveItem(player, vault, fromSlot, toSlot, count);
        }

        public bool OnAddItem(GamePlayer player, GameVault vault, DbInventoryItem item)
        {
            VaultItemCache newCache = null;

            lock (_lock)
            {
                if (!_isDisposed)
                {
                    ValidateCacheInternal(vault);
                    int newSlot = GetClientSlotPosition(vault, item.SlotPosition);

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

                newCache = VaultItemCacheManager.GetCache(_key);
            }

            return newCache != null && newCache.OnAddItem(player, vault, item);
        }

        public bool OnRemoveItem(GamePlayer player, GameVault vault, DbInventoryItem item, int previousSlot)
        {
            VaultItemCache newCache = null;

            lock (_lock)
            {
                if (!_isDisposed)
                {
                    ValidateCacheInternal(vault);
                    previousSlot = GetClientSlotPosition(vault, previousSlot);

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

                newCache = VaultItemCacheManager.GetCache(_key);
            }

            return newCache != null && newCache.OnRemoveItem(player, vault, item, previousSlot);
        }

        public bool OnMoveItem(GamePlayer player, GameVault vault, DbInventoryItem firstItem, int previousFirstSlot, DbInventoryItem secondItem, int previousSecondSlot)
        {
            VaultItemCache newCache = null;

            lock (_lock)
            {
                if (!_isDisposed)
                {
                    ValidateCacheInternal(vault);

                    if (previousFirstSlot == previousSecondSlot)
                        return true;

                    DbInventoryItem existingFirstItem = null;
                    DbInventoryItem existingSecondItem = null;

                    previousFirstSlot = GetClientSlotPosition(vault, previousFirstSlot);
                    previousSecondSlot = GetClientSlotPosition(vault, previousSecondSlot);

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

                    return true;
                }

                newCache = VaultItemCacheManager.GetCache(_key);
            }

            return newCache != null && newCache.OnMoveItem(player, vault, firstItem, previousFirstSlot, secondItem, previousSecondSlot);
        }

        public Dictionary<int, DbInventoryItem> GetItems(GameVault vault)
        {
            VaultItemCache newCache = null;

            lock (_lock)
            {
                if (!_isDisposed)
                {
                    ValidateCacheInternal(vault);
                    return new(_items);
                }

                newCache = VaultItemCacheManager.GetCache(_key);
            }

            return newCache?.GetItems(vault);
        }

        public DbInventoryItem GetItem(GameVault vault, int slot)
        {
            VaultItemCache newCache = null;

            lock (_lock)
            {
                if (!_isDisposed)
                {
                    ValidateCacheInternal(vault);
                    return _items.TryGetValue(slot, out DbInventoryItem item) ? item : null;
                }

                newCache = VaultItemCacheManager.GetCache(_key);
            }

            return newCache?.GetItem(vault, slot);
        }

        public void ForceValidateCache()
        {
            // Simply force disposal and removal.
            OnTick(this);
        }

        protected override int OnTick(ECSGameTimer timer)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return 0;

                // Clean up observers that didn't remove themselves (quit without changing target).
                foreach (var pair in _observers)
                {
                    if (pair.Value.ObjectState is not GameObject.eObjectState.Active)
                        _observers.Remove(pair.Key);
                }

                if (_observers.Count > 0)
                    return EXPIRES_AFTER;

                _items = null;
                _isDisposed = true;
                VaultItemCacheManager.RemoveCache(_key, this);
            }

            return 0;
        }

        private eInventorySlot GetFirstEmptyClientSlot(GameVault vault)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return eInventorySlot.Invalid;

                eInventorySlot result = vault.FirstClientSlot;

                while (result <= vault.LastClientSlot)
                {
                    if (!_items.ContainsKey((int)result))
                        return result;

                    result++;
                }

                return eInventorySlot.Invalid;
            }
        }

        private eInventorySlot GetFirstPartiallyFullClientSlot(GameVault vault, DbInventoryItem item)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return eInventorySlot.Invalid;

                eInventorySlot result = vault.FirstClientSlot;

                while (result <= vault.LastClientSlot)
                {
                    if (_items.TryGetValue((int)result, out DbInventoryItem otherItem) &&
                        otherItem.Count < otherItem.MaxCount &&
                        otherItem.Name.Equals(item.Name))
                    {
                        return result;
                    }

                    result++;
                }

                return GetFirstEmptyClientSlot(vault);
            }
        }

        private static int GetClientSlotPosition(GameVault vault, int slot)
        {
            int offset = -vault.FirstDbSlot + (int) eInventorySlot.HousingInventory_First;
            return offset + slot;
        }

        private void ValidateCacheInternal(GameVault vault)
        {
            // Always refresh or start the timer.
            Start(EXPIRES_AFTER);

            if (_items != null)
                return;

            _items = new();

            foreach (DbInventoryItem item in vault.GetDbItems())
            {
                int slotPosition = GetClientSlotPosition(vault, item.SlotPosition);

                if (!_items.TryAdd(slotPosition, item))
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Error during cache validation. Slot already taken. (Added: {_items[slotPosition]}) (Rejected: {item})");
                }
            }
        }
    }
}
