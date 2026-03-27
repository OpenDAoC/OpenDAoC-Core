using System.Collections.Generic;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    /// <summary>
    /// A vault.
    /// </summary>
    public abstract class GameVault : GameStaticItem, IGameInventoryObject
    {
        private const int VAULT_SIZE = 100;

        public int Index { get; protected set; }
        public virtual int VaultSize => VAULT_SIZE;
        public virtual eInventorySlot FirstClientSlot => eInventorySlot.HousingInventory_First;
        public virtual eInventorySlot LastClientSlot => eInventorySlot.HousingInventory_Last;
        public virtual int FirstDbSlot => (int) eInventorySlot.HouseVault_First + VaultSize * Index;
        public virtual int LastDbSlot => FirstDbSlot + (VaultSize - 1);

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
                return false;

            player.ActiveInventoryObject?.RemoveObserver(player);
            AddObserver(player);
            player.ActiveInventoryObject = this;
            player.Out.SendInventoryItemsUpdate(GetClientInventory(), eInventoryWindowType.HouseVault);
            return true;
        }

        public abstract string GetOwner();

        public virtual IEnumerable<DbInventoryItem> GetDbItems()
        {
            WhereClause filterBySlot = DB.Column("SlotPosition").IsGreaterOrEqualTo(FirstDbSlot).And(DB.Column("SlotPosition").IsLessOrEqualTo(LastDbSlot));
            return DOLDB<DbInventoryItem>.SelectObjects(DB.Column("OwnerID").IsEqualTo(GetOwner()).And(filterBySlot));
        }

        public virtual Dictionary<int, DbInventoryItem> GetClientInventory()
        {
            return GetCache().GetItems(this);
        }

        public virtual bool TryGetItem(int slot, out DbInventoryItem item)
        {
            item = GetCache().GetItem(this, slot);
            return item != null;
        }

        public virtual bool CanHandleMove(GamePlayer player, eInventorySlot fromSlot, eInventorySlot toSlot)
        {
            if (player == null || player.ActiveInventoryObject != this)
                return false;

            // House vaults and consignment merchants deliver the same slot numbers.
            return GameInventoryObjectExtensions.IsHousingInventorySlot(fromSlot) || GameInventoryObjectExtensions.IsHousingInventorySlot(toSlot);
        }

        public virtual bool MoveItem(GamePlayer player, eInventorySlot fromSlot, eInventorySlot toSlot, ushort count)
        {
            if (fromSlot == toSlot)
                return false;

            return GetCache()?.MoveItem(player, this, fromSlot, toSlot, count) ?? false;
        }

        public virtual bool OnAddItem(GamePlayer player, DbInventoryItem item, int previousSlot)
        {
            return GetCache()?.OnAddItem(player, this, item) ?? false;
        }

        public virtual bool OnRemoveItem(GamePlayer player, DbInventoryItem item, int previousSlot)
        {
            return GetCache()?.OnRemoveItem(player, this, item, previousSlot) ?? false;
        }

        public virtual bool OnMoveItem(GamePlayer player, DbInventoryItem firstItem, int previousFirstSlot, DbInventoryItem secondItem, int previousSecondSlot)
        {
            return GetCache()?.OnMoveItem(player, this, firstItem, previousFirstSlot, secondItem, previousSecondSlot) ?? false;
        }

        public virtual void OnItemManipulationError(GamePlayer player)
        {
            GetCache()?.ForceValidateCache();
        }

        public virtual bool SetSellPrice(GamePlayer player, eInventorySlot clientSlot, uint price)
        {
            return true;
        }

        public bool SearchInventory(GamePlayer player, MarketSearch.SearchData searchData)
        {
            return false;
        }

        public virtual void AddObserver(GamePlayer player)
        {
            GetCache()?.AddObserver(player);
        }

        public virtual void RemoveObserver(GamePlayer player)
        {
            GetCache()?.RemoveObserver(player);
        }

        private VaultItemCache GetCache()
        {
            return VaultItemCacheManager.GetCache(this);
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
    }
}
