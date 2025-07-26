using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    /// <summary>
    /// Interface for a GameInventoryObject
    /// </summary>
    public interface IGameInventoryObject
    {
        eInventorySlot FirstClientSlot { get; }
        eInventorySlot LastClientSlot { get; }
        int FirstDbSlot { get; }
        int LastDbSlot { get; }
        Lock Lock { get; }
        string GetOwner(GamePlayer player);
        IList<DbInventoryItem> GetDbItems(GamePlayer player);
        Dictionary<int, DbInventoryItem> GetClientInventory(GamePlayer player);
        bool CanHandleMove(GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot);
        bool MoveItem(GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot, ushort itemCount);
        bool OnAddItem(GamePlayer player, DbInventoryItem item, int previousSlot);
        bool OnRemoveItem(GamePlayer player, DbInventoryItem item, int previousSlot);
        bool OnMoveItem(GamePlayer player, DbInventoryItem firstItem, int previousFirstSlot, DbInventoryItem secondItem, int previousSecondSlot);
        bool SetSellPrice(GamePlayer player, eInventorySlot clientSlot, uint sellPrice);
        bool SearchInventory(GamePlayer player, MarketSearch.SearchData searchData);
        void AddObserver(GamePlayer player);
        void RemoveObserver(GamePlayer player);
    }

    /// <summary>
    /// Extension class for GameInventoryObject.
    /// </summary>
    public static class GameInventoryObjectExtensions
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public static bool CanHandleRequest(this IGameInventoryObject thisObject, eInventorySlot fromClientSlot, eInventorySlot toClientSlot)
        {
            return (fromClientSlot >= thisObject.FirstClientSlot && fromClientSlot <= thisObject.LastClientSlot) || (toClientSlot >= thisObject.FirstClientSlot && toClientSlot <= thisObject.LastClientSlot);
        }

        public static IDictionary<int, DbInventoryItem> MoveItem(this IGameInventoryObject thisObject, GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot, ushort count)
        {
            lock (thisObject.Lock)
            {
                if (!GetItemInSlot(fromClientSlot, out DbInventoryItem fromItem))
                {
                    SendUnsupportedActionMessage(player);
                    return null;
                }

                GetItemInSlot(toClientSlot, out DbInventoryItem toItem);
                IDictionary<int, DbInventoryItem> updatedItems = MoveItemInner(fromItem, toItem);
                return updatedItems;
            }

            bool GetItemInSlot(eInventorySlot slot, out DbInventoryItem item)
            {
                item = null;

                if (IsHousingInventorySlot(slot))
                    thisObject.GetClientInventory(player).TryGetValue((int) slot, out item);
                else
                    item = player.Inventory.GetItem(slot);

                return item != null;
            }

            IDictionary<int, DbInventoryItem> MoveItemInner(DbInventoryItem fromItem, DbInventoryItem toItem)
            {
                Dictionary<int, DbInventoryItem> updatedItems = new(2);

                if (toItem == null)
                    MoveItemToEmptySlot(thisObject, player, fromClientSlot, toClientSlot, fromItem, count, updatedItems);
                else if (toItem.IsStackable && fromItem.Count < toItem.MaxCount && toItem.Count < toItem.MaxCount && toItem.Name.Equals(fromItem.Name))
                {
                    // `count` is inconsistent here.
                    // With account vaults, it seems to always be 0, so we can treat it as an error if it isn't.
                    // With consignment merchants, it takes the stack's size, but stacking / splitting is disallowed anyway.
                    // Others... ?
                    if (count != 0)
                    {
                        SendUnsupportedActionMessage(player);
                        return updatedItems;
                    }

                    StackItems(thisObject, player, fromClientSlot, toClientSlot, fromItem, toItem, updatedItems);
                }
                else
                    SwapItems(thisObject, player, fromClientSlot, toClientSlot, fromItem, toItem, updatedItems);

                return updatedItems;
            }
        }

        public static void NotifyObservers(GameObject thisOwner, GamePlayer player, Dictionary<string, GamePlayer> observers, IDictionary<int, DbInventoryItem> updatedItems)
        {
            if (updatedItems == null)
                return;

            List<string> inactiveList = [];
            Dictionary<int, DbInventoryItem> updatedItemsForObserver = updatedItems.Where(x => IsHousingInventorySlot((eInventorySlot) x.Key)).ToDictionary();
            bool playerFound = false;

            foreach (GamePlayer observer in observers.Values)
            {
                if (observer == player)
                {
                    observer.Client.Out.SendInventoryItemsUpdate(updatedItems, eInventoryWindowType.Update);
                    playerFound = true;
                }
                else if (observer.ActiveInventoryObject == thisOwner && observer.IsWithinRadius(thisOwner, WorldMgr.INTERACT_DISTANCE))
                    observer.Client.Out.SendInventoryItemsUpdate(updatedItemsForObserver, eInventoryWindowType.Update);
                else
                    inactiveList.Add(observer.Name);
            }

            if (!playerFound)
                player.Client.Out.SendInventoryItemsUpdate(updatedItems, eInventoryWindowType.Update);

            foreach (string observerName in inactiveList)
                observers.Remove(observerName);
        }

        private static void MoveItemToEmptySlot(this IGameInventoryObject thisObject, GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot, DbInventoryItem fromItem, ushort count, Dictionary<int, DbInventoryItem> updatedItems)
        {
            if (count == 0)
            {
                MoveWholeStack();
                return;
            }

            int fromItemCount = Math.Max(0, fromItem.Count - count);

            if (fromItemCount == 0)
            {
                MoveWholeStack();
                return;
            }

            SplitStack();

            void MoveWholeStack()
            {
                if (IsCharacterInventorySlot(fromClientSlot))
                {
                    if (IsHousingInventorySlot(toClientSlot))
                    {
                        if (!MoveWholeStackFromCharacterInventoryToHousingInventory())
                            return;
                    }
                    else
                    {
                        SendUnsupportedActionMessage(player);
                        return;
                    }
                }
                else if (IsHousingInventorySlot(fromClientSlot))
                {
                    if (IsHousingInventorySlot(toClientSlot))
                    {
                       if (!MoveWholeStackFromHousingInventoryToHousingInventory())
                            return;
                    }
                    else if (IsCharacterInventorySlot(toClientSlot))
                    {
                        if (!MoveWholeStackFromHousingInventoryToCharacterInventory())
                            return;
                    }
                    else
                    {
                        SendUnsupportedActionMessage(player);
                        return;
                    }
                }
                else
                {
                    SendUnsupportedActionMessage(player);
                    return;
                }

                updatedItems.Add((int) fromClientSlot, null);
                updatedItems.Add((int) toClientSlot, fromItem);

                bool MoveWholeStackFromCharacterInventoryToHousingInventory()
                {
                    if (!player.Inventory.CheckItemsBeforeMovingFromOrToExternalInventory(fromItem, null, toClientSlot, fromClientSlot, count))
                        return false;

                    if (!player.Inventory.RemoveItemWithoutDbDeletion(fromItem))
                    {
                        SendErrorMessage(player, nameof(MoveWholeStackFromCharacterInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, null, count);
                        return false;
                    }

                    player.Inventory.OnItemMove(fromItem, null, fromClientSlot, toClientSlot);
                    fromItem.SlotPosition = toClientSlot - thisObject.FirstClientSlot + thisObject.FirstDbSlot;
                    fromItem.OwnerID = thisObject.GetOwner(player);
                    thisObject.OnAddItem(player, fromItem, (int) fromClientSlot);

                    if (!SaveItem(fromItem))
                    {
                        SendErrorMessage(player, nameof(MoveWholeStackFromCharacterInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, null, count);
                        return false;
                    }

                    player.Inventory.SaveIntoDatabase(player.InternalID);
                    return true;
                }

                bool MoveWholeStackFromHousingInventoryToHousingInventory()
                {
                    int previousSlot = fromItem.SlotPosition;
                    fromItem.SlotPosition = toClientSlot - thisObject.FirstClientSlot + thisObject.FirstDbSlot;
                    fromItem.OwnerID = thisObject.GetOwner(player);
                    thisObject.OnMoveItem(player, fromItem, previousSlot, null, fromItem.SlotPosition);

                    if (!SaveItem(fromItem))
                    {
                        SendErrorMessage(player, nameof(MoveWholeStackFromCharacterInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, null, count);
                        return false;
                    }

                    return true;
                }

                bool MoveWholeStackFromHousingInventoryToCharacterInventory()
                {
                    if (!player.Inventory.CheckItemsBeforeMovingFromOrToExternalInventory(fromItem, null, fromClientSlot, toClientSlot, count))
                        return false;

                    int previousSlot = fromItem.SlotPosition;

                    if (!player.Inventory.AddItemWithoutDbAddition(toClientSlot, fromItem))
                    {
                        SendErrorMessage(player, nameof(MoveWholeStackFromHousingInventoryToCharacterInventory), fromClientSlot, toClientSlot, fromItem, null, count);
                        return false;
                    }

                    thisObject.OnRemoveItem(player, fromItem, previousSlot);
                    player.Inventory.OnItemMove(fromItem, null, fromClientSlot, toClientSlot);
                    player.Inventory.SaveIntoDatabase(player.InternalID);
                    return true;
                }
            }

            void SplitStack()
            {
                DbInventoryItem toItem = (DbInventoryItem) fromItem.Clone();
                toItem.Count = count;
                toItem.AllowAdd = fromItem.Template.AllowAdd;

                if (IsBackpackSlot(fromClientSlot))
                {
                    if (IsHousingInventorySlot(toClientSlot))
                    {
                        if (!SplitStackFromCharacterInventoryToHousingInventory())
                            return;
                    }
                    else
                    {
                        SendUnsupportedActionMessage(player);
                        return;
                    }
                }
                else if (IsHousingInventorySlot(fromClientSlot))
                {
                    if (IsHousingInventorySlot(toClientSlot))
                    {
                        if (!SplitStackFromHousingInventoryToHousingInventory())
                            return;
                    }
                    else if (IsBackpackSlot(toClientSlot))
                    {
                        if (!SplitStackFromHousingInventoryToCharacterInventory())
                            return;
                    }
                    else
                    {
                        SendUnsupportedActionMessage(player);
                        return;
                    }
                }
                else
                {
                    SendUnsupportedActionMessage(player);
                    return;
                }

                updatedItems.Add((int) fromClientSlot, fromItem);
                updatedItems.Add((int) toClientSlot, toItem);

                bool SplitStackFromCharacterInventoryToHousingInventory()
                {
                    if (!player.Inventory.RemoveCountFromStack(fromItem, count))
                    {
                        SendErrorMessage(player, nameof(SplitStackFromCharacterInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, toItem, count);
                        return false;
                    }

                    toItem.SlotPosition = toClientSlot - thisObject.FirstClientSlot + thisObject.FirstDbSlot;
                    toItem.OwnerID = thisObject.GetOwner(player);
                    thisObject.OnAddItem(player, toItem, fromItem.SlotPosition);

                    if (!SaveItem(toItem))
                    {
                        SendErrorMessage(player, nameof(SplitStackFromCharacterInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, toItem, count);
                        return false;
                    }

                    player.Inventory.SaveIntoDatabase(player.InternalID);
                    return true;
                }

                bool SplitStackFromHousingInventoryToHousingInventory()
                {
                    fromItem.Count -= count;

                    if (!SaveItem(fromItem))
                    {
                        SendErrorMessage(player, nameof(SplitStackFromHousingInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, toItem, count);
                        return false;
                    }

                    toItem.SlotPosition = toClientSlot - thisObject.FirstClientSlot + thisObject.FirstDbSlot;
                    toItem.OwnerID = thisObject.GetOwner(player);
                    thisObject.OnAddItem(player, toItem, fromItem.SlotPosition);

                    if (!SaveItem(toItem))
                    {
                        SendErrorMessage(player, nameof(SplitStackFromHousingInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, toItem, count);
                        return false;
                    }

                    return true;
                }

                bool SplitStackFromHousingInventoryToCharacterInventory()
                {
                    fromItem.Count -= count;

                    if (!SaveItem(fromItem) || !player.Inventory.AddItem(toClientSlot, toItem))
                    {
                        SendErrorMessage(player, nameof(SplitStackFromHousingInventoryToCharacterInventory), fromClientSlot, toClientSlot, fromItem, toItem, count);
                        return false;
                    }

                    player.Inventory.SaveIntoDatabase(player.InternalID);
                    return true;
                }
            }
        }

        private static void StackItems(this IGameInventoryObject thisObject, GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot, DbInventoryItem fromItem, DbInventoryItem toItem, Dictionary<int, DbInventoryItem> updatedItems)
        {
            // Assumes that neither stacks are full. If that's the case, `SwapItems` should have been called instead.
            int count = fromItem.Count + toItem.Count > fromItem.MaxCount ? toItem.MaxCount - toItem.Count : fromItem.Count;

            if (IsBackpackSlot(fromClientSlot))
            {
                if (IsHousingInventorySlot(toClientSlot))
                {
                    if (!StackItemsFromCharacterInventoryToHousingInventory())
                        return;
                }
                else
                {
                    SendUnsupportedActionMessage(player);
                    return;
                }
            }
            else if (IsHousingInventorySlot(fromClientSlot))
            {
                if (IsHousingInventorySlot(toClientSlot))
                {
                    if (!StackItemsFromHousingInventoryToHousingInventory())
                        return;
                }
                else if (IsBackpackSlot(toClientSlot))
                {
                    if (!StackItemsFromHousingInventoryToCharacterInventory())
                        return;
                }
                else
                {
                    SendUnsupportedActionMessage(player);
                    return;
                }
            }
            else
            {
                SendUnsupportedActionMessage(player);
                return;
            }

            updatedItems.Add((int) fromClientSlot, fromItem);
            updatedItems.Add((int) toClientSlot, toItem);

            bool StackItemsFromCharacterInventoryToHousingInventory()
            {
                if (fromItem.Count - count <= 0)
                {
                    if (!player.Inventory.RemoveItem(fromItem))
                    {
                        SendErrorMessage(player, nameof(StackItemsFromCharacterInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                        return false;
                    }

                    fromItem = null;
                }
                else if (!player.Inventory.RemoveCountFromStack(fromItem, count))
                {
                    SendErrorMessage(player, nameof(StackItemsFromCharacterInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                    return false;
                }

                toItem.Count += count;

                if (!SaveItem(toItem))
                {
                    SendErrorMessage(player, nameof(StackItemsFromCharacterInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                    return false;
                }

                player.Inventory.SaveIntoDatabase(player.InternalID);
                return true;
            }

            bool StackItemsFromHousingInventoryToHousingInventory()
            {
                if (fromItem.Count - count <= 0)
                {
                    fromItem.Count = 0;
                    thisObject.OnRemoveItem(player, fromItem, fromItem.SlotPosition);

                    if (!SaveItem(fromItem))
                    {
                        SendErrorMessage(player, nameof(StackItemsFromCharacterInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                        return false;
                    }

                    fromItem = null;
                }
                else
                {
                    fromItem.Count -= count;

                    if (!SaveItem(fromItem))
                    {
                        SendErrorMessage(player, nameof(StackItemsFromCharacterInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                        return false;
                    }
                }

                toItem.Count += count;

                if (!SaveItem(toItem))
                {
                    SendErrorMessage(player, nameof(StackItemsFromCharacterInventoryToHousingInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                    return false;
                }

                return true;
            }

            bool StackItemsFromHousingInventoryToCharacterInventory()
            {
                if (fromItem.Count - count <= 0)
                {
                    fromItem.Count = 0;
                    thisObject.OnRemoveItem(player, fromItem, fromItem.SlotPosition);

                    if (!SaveItem(fromItem))
                    {
                        SendErrorMessage(player, nameof(StackItemsFromHousingInventoryToCharacterInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                        return false;
                    }

                    fromItem = null;
                }
                else
                {
                    fromItem.Count -= count;

                    if (!SaveItem(fromItem))
                    {
                        SendErrorMessage(player, nameof(StackItemsFromHousingInventoryToCharacterInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                        return false;
                    }
                }

                if (!player.Inventory.AddCountToStack(toItem, count))
                {
                    SendErrorMessage(player, nameof(StackItemsFromHousingInventoryToCharacterInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                    return false;
                }

                player.Inventory.SaveIntoDatabase(player.InternalID);
                return true;
            }
        }

        private static void SwapItems(this IGameInventoryObject thisObject, GamePlayer player, eInventorySlot fromClientSlot, eInventorySlot toClientSlot, DbInventoryItem fromItem, DbInventoryItem toItem, Dictionary<int, DbInventoryItem> updatedItems)
        {
            if (IsCharacterInventorySlot(fromClientSlot))
            {
                if (IsHousingInventorySlot(toClientSlot))
                {
                    // From backpack to housing inventory.
                    if (!SwapItemsFromOrToCharacterInventory(toClientSlot, fromClientSlot, toItem, fromItem))
                        return;
                }
                else
                {
                    SendUnsupportedActionMessage(player);
                    return;
                }
            }
            else if (IsHousingInventorySlot(fromClientSlot))
            {
                if (IsCharacterInventorySlot(toClientSlot))
                {
                    // From housing inventory to backpack.
                    if (!SwapItemsFromOrToCharacterInventory(fromClientSlot, toClientSlot, fromItem, toItem))
                        return;
                }
                else if (IsHousingInventorySlot(toClientSlot))
                {
                    if (!SwapItemsFromAndToHousingInventory())
                        return;
                }
                else
                {
                    SendUnsupportedActionMessage(player);
                    return;
                }
            }
            else
            {
                SendUnsupportedActionMessage(player);
                return;
            }

            updatedItems.Add((int) toClientSlot, fromItem);
            updatedItems.Add((int) fromClientSlot, toItem);

            bool SwapItemsFromOrToCharacterInventory(eInventorySlot vaultSlot, eInventorySlot characterInventorySlot, DbInventoryItem vaultItem, DbInventoryItem characterInventoryItem)
            {
                if (!player.Inventory.CheckItemsBeforeMovingFromOrToExternalInventory(vaultItem, characterInventoryItem, vaultSlot, characterInventorySlot, 0))
                    return false;

                if (!player.Inventory.RemoveItemWithoutDbDeletion(characterInventoryItem))
                {
                    SendErrorMessage(player, nameof(SwapItemsFromOrToCharacterInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                    return false;
                }

                int characterInventoryItemPreviousSlotPosition = characterInventoryItem.SlotPosition;
                characterInventoryItem.SlotPosition = vaultItem.SlotPosition;
                characterInventoryItem.OwnerID = thisObject.GetOwner(player);
                thisObject.OnMoveItem(player, characterInventoryItem, characterInventoryItemPreviousSlotPosition, vaultItem, vaultItem.SlotPosition);

                if (!SaveItem(characterInventoryItem))
                {
                    SendErrorMessage(player, nameof(SwapItemsFromOrToCharacterInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                    return false;
                }

                if (!player.Inventory.AddItemWithoutDbAddition(characterInventorySlot, vaultItem) || !SaveItem(vaultItem))
                {
                    SendErrorMessage(player, nameof(SwapItemsFromOrToCharacterInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                    return false;
                }

                player.Inventory.OnItemMove(fromItem, toItem, fromClientSlot, toClientSlot);
                player.Inventory.SaveIntoDatabase(player.InternalID);
                return true;
            }

            bool SwapItemsFromAndToHousingInventory()
            {
                (toItem.SlotPosition, fromItem.SlotPosition) = (fromItem.SlotPosition, toItem.SlotPosition);
                thisObject.OnMoveItem(player, fromItem, toItem.SlotPosition, toItem, fromItem.SlotPosition);

                if (!SaveItem(fromItem) || !SaveItem(toItem))
                {
                    SendErrorMessage(player, nameof(SwapItemsFromAndToHousingInventory), fromClientSlot, toClientSlot, fromItem, toItem, 0);
                    return false;
                }

                return true;
            }
        }

        public static bool SaveItem(DbInventoryItem item)
        {
            // Failsafe in case count wasn't properly tracked.
            // We might also be able to ignore `IsStackable`, but I'm not sure non stackable items always have a non 0 count.
            if (item.IsStackable && item.Count == 0)
                return DeleteItem(item);

            bool result = item.IsPersisted ? GameServer.Database.SaveObject(item) : GameServer.Database.AddObject(item);

            if (!result)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Couldn't save or add an item with {nameof(DataObject.IsPersisted)}={item.IsPersisted}. Attempting to call the other method instead.");

                result = item.IsPersisted ? GameServer.Database.AddObject(item) : GameServer.Database.SaveObject(item);
            }

            return result;
        }

        public static bool DeleteItem(DbInventoryItem item)
        {
            return GameServer.Database.DeleteObject(item);
        }

        public static bool IsHousingInventorySlot(eInventorySlot slot)
        {
            return slot is >= eInventorySlot.HousingInventory_First and <= eInventorySlot.HousingInventory_Last;
        }

        public static bool IsBackpackSlot(eInventorySlot slot)
        {
            return slot is >= eInventorySlot.FirstBackpack and <= eInventorySlot.LastBackpack;
        }

        public static bool IsEquipmentSlot(eInventorySlot slot)
        {
            return slot is >= eInventorySlot.MinEquipable and <= eInventorySlot.MaxEquipable;
        }

        public static bool IsCharacterVaultSlot(eInventorySlot slot)
        {
            return slot is >= eInventorySlot.FirstVault and <= eInventorySlot.LastVault;
        }

        public static bool IsCharacterInventorySlot(eInventorySlot slot)
        {
            return IsBackpackSlot(slot) || IsEquipmentSlot(slot);
        }

        private static void SendErrorMessage(GamePlayer player, string method, eInventorySlot fromClientSlot, eInventorySlot toClientSlot, DbInventoryItem fromItem, DbInventoryItem toItem, ushort count)
        {
            player.Out.SendMessage($"Error while moving an item in '{method}':", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            player.Out.SendMessage($"- [{fromItem?.Name}] [{fromClientSlot}] ({count})", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            player.Out.SendMessage($"- [{toItem?.Name}] [{toClientSlot}]", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
            player.Out.SendMessage($"The item may be lost or temporarily invisible.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }

        private static void SendUnsupportedActionMessage(GamePlayer player)
        {
            player.Out.SendMessage("This action isn't currently supported. Try a different source or destination slot.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
        }
    }
}
