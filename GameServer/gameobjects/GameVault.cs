using System.Collections.Generic;
using System.Linq;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    /// <summary>
    /// A vault.
    /// </summary>
    public class GameVault : GameStaticItem, IGameInventoryObject
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        /// <summary>
        /// This is used to synchronize actions on the vault.
        /// </summary>
        protected object _vaultSync = new();

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

        public object LockObject()
        {
            return _vaultSync;
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
            Dictionary<int, DbInventoryItem> inventory = [];
            int slotOffset = -FirstDbSlot + (int) eInventorySlot.HousingInventory_First;

            foreach (DbInventoryItem item in DBItems(player))
            {
                if (item != null)
                {
                    int slot = item.SlotPosition + slotOffset;

                    if (!inventory.TryAdd(slot, item))
                        log.Error($"GAMEVAULT: Duplicate item {item.Name}, owner {item.OwnerID}, position {item.SlotPosition + slotOffset}");
                }
            }

            return inventory;
        }

        /// <summary>
        /// Player interacting with this vault.
        /// </summary>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            player.ActiveInventoryObject?.RemoveObserver(player);

            lock (LockObject())
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
        public virtual IList<DbInventoryItem> DBItems(GamePlayer player = null)
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

            bool fromHousing = GameInventoryObjectExtensions.IsHousingInventorySlot(fromSlot);
            bool toHousing = GameInventoryObjectExtensions.IsHousingInventorySlot(toSlot);

            if (!fromHousing && !toHousing)
                return false;

            if (player.ActiveInventoryObject is not GameVault gameVault)
            {
                player.Out.SendMessage("You are not actively viewing a vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                player.Out.SendInventoryItemsUpdate(null);
                return false;
            }

            if (toHousing && !gameVault.CanAddItems(player))
            {
                player.Out.SendMessage("You don't have permission to add items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            if (fromHousing && !gameVault.CanRemoveItems(player))
            {
                player.Out.SendMessage("You don't have permission to remove items!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return false;
            }

            DbInventoryItem itemInFromSlot = player.Inventory.GetItem(fromSlot);
            DbInventoryItem itemInToSlot = player.Inventory.GetItem(toSlot);

            // Check for a swap to get around not allowing non-tradeable items in a housing vault.
            if (fromHousing && itemInToSlot != null && !itemInToSlot.IsTradable && this is not AccountVault)
            {
                player.Out.SendMessage("You cannot swap with an untradable item!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                log.Debug($"GameVault: {player.Name} attempted to swap untradable item {itemInFromSlot.Name} with {itemInToSlot.Name}");
                player.Out.SendInventoryItemsUpdate(null);
                return false;
            }

            // Allow people to get untradable items out of their house vaults (old bug) but block placing untradable items into housing vaults from any source.
            if (toHousing && itemInFromSlot != null && !itemInFromSlot.IsTradable && this is not AccountVault)
            {
                player.Out.SendMessage("You can not put this item into a House Vault!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                player.Out.SendInventoryItemsUpdate(null);
                return false;
            }

            lock (_vaultSync)
            {
                GameInventoryObjectExtensions.NotifyObservers(this, player, _observers, GameInventoryObjectExtensions.MoveItem(this, player, fromSlot, toSlot, count));
            }

            return true;
        }

        public virtual bool OnAddItem(GamePlayer player, DbInventoryItem item)
        {
            return true;
        }

        public virtual bool OnRemoveItem(GamePlayer player, DbInventoryItem item)
        {
            return true;
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
    }
}
