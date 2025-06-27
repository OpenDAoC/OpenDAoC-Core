using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.Interfaces.Items;
using Microsoft.Extensions.Logging;

namespace DOL.GS.Infrastructure.Adapters
{
    /// <summary>
    /// Focused adapter for basic inventory operations following clean architecture
    /// Bridges legacy GamePlayerInventory with IGameInventory interface
    /// Follows SRP by handling only core inventory functionality
    /// </summary>
    public class CharacterInventoryAdapter : DOL.GS.Interfaces.Items.IGameInventory, DOL.GS.IGameInventory
    {
        private readonly GamePlayerInventory _legacyInventory;
        private readonly ILogger<CharacterInventoryAdapter> _logger;
        private readonly Lock _lock = new Lock();

        public CharacterInventoryAdapter(GamePlayerInventory legacyInventory, ILogger<CharacterInventoryAdapter> logger = null)
        {
            _legacyInventory = legacyInventory ?? throw new ArgumentNullException(nameof(legacyInventory));
            _logger = logger;
        }

        #region Core Inventory Operations

        public DbInventoryItem GetItem(eInventorySlot slot)
        {
            try
            {
                return _legacyInventory.GetItem(slot);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get item from slot {Slot}", slot);
                return null;
            }
        }

        public bool AddItem(DbInventoryItem item, eInventorySlot slot)
        {
            try
            {
                var result = _legacyInventory.AddItem(slot, item);
                if (result)
                {
                    _logger?.LogDebug("Added item {ItemName} to slot {Slot}", item?.Name, slot);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to add item {ItemName} to slot {Slot}", item?.Name, slot);
                return false;
            }
        }

        /// <summary>
        /// Legacy interface signature - slot first, item second
        /// </summary>
        public bool AddItem(eInventorySlot slot, DbInventoryItem item)
        {
            return AddItem(item, slot);
        }

        public bool RemoveItem(DbInventoryItem item)
        {
            try
            {
                var result = _legacyInventory.RemoveItem(item);
                if (result)
                {
                    _logger?.LogDebug("Removed item {ItemName}", item?.Name);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to remove item {ItemName}", item?.Name);
                return false;
            }
        }

        public bool MoveItem(eInventorySlot fromSlot, eInventorySlot toSlot, int itemCount)
        {
            try
            {
                var result = _legacyInventory.MoveItem(fromSlot, toSlot, itemCount);
                if (result)
                {
                    _logger?.LogDebug("Moved item from {FromSlot} to {ToSlot}, count: {Count}", 
                        fromSlot, toSlot, itemCount);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to move item from {FromSlot} to {ToSlot}", fromSlot, toSlot);
                return false;
            }
        }

        public bool CanAddItem(DbInventoryItem item, eInventorySlot slot)
        {
            try
            {
                var existingItem = GetItem(slot);
                return existingItem == null || (existingItem.Name == item?.Name && existingItem.IsStackable);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to check if item can be added");
                return false;
            }
        }

        #endregion

        #region Equipment Manager Operations

        public bool EquipItem(DbInventoryItem item, eInventorySlot slot) => AddItem(item, slot);
        public bool UnequipItem(eInventorySlot slot) => _legacyInventory.RemoveItem(GetItem(slot));
        public bool CanEquipItem(DbInventoryItem item, eInventorySlot slot) => CanAddItem(item, slot);
        public DbInventoryItem GetEquippedItem(eInventorySlot slot) => GetItem(slot);
        public ICollection<DbInventoryItem> GetEquippedItems() => 
            _legacyInventory.EquippedItems ?? new List<DbInventoryItem>();

        #endregion

        #region Query Operations

        public ICollection<DbInventoryItem> GetItemRange(eInventorySlot minSlot, eInventorySlot maxSlot)
        {
            try
            {
                return _legacyInventory.GetItemRange(minSlot, maxSlot)?.ToList() ?? new List<DbInventoryItem>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to get item range {MinSlot} to {MaxSlot}", minSlot, maxSlot);
                return new List<DbInventoryItem>();
            }
        }

        public DbInventoryItem FindFirstItemByName(string name, eInventorySlot minSlot, eInventorySlot maxSlot)
        {
            try
            {
                return _legacyInventory.GetFirstItemByName(name, minSlot, maxSlot);
            }
            catch
            {
                return null;
            }
        }

        public int GetFreeSlotCount(eInventorySlot minSlot, eInventorySlot maxSlot)
        {
            try
            {
                return _legacyInventory.CountSlots(false, minSlot, maxSlot);
            }
            catch
            {
                return 0;
            }
        }

        public eInventorySlot FindFirstEmptySlot(eInventorySlot minSlot, eInventorySlot maxSlot)
        {
            try
            {
                return _legacyInventory.FindFirstEmptySlot(minSlot, maxSlot);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to find empty slot in range {MinSlot} to {MaxSlot}", minSlot, maxSlot);
                return eInventorySlot.Invalid;
            }
        }

        public ICollection<DbInventoryItem> GetAllItems() => 
            _legacyInventory.AllItems ?? new List<DbInventoryItem>();

        #endregion

        #region Stacking Operations (Minimal Implementation)

        public bool AddCountToStack(DbInventoryItem item, int count) => 
            _legacyInventory.AddCountToStack(item, count);
        public bool RemoveCountFromStack(DbInventoryItem item, int count) => 
            _legacyInventory.RemoveCountFromStack(item, count);
        public bool AddTemplate(DbInventoryItem template, int count, eInventorySlot minSlot, eInventorySlot maxSlot) => 
            _legacyInventory.AddTemplate(template, count, minSlot, maxSlot);
        public bool RemoveTemplate(string templateID, int count, eInventorySlot minSlot, eInventorySlot maxSlot) => 
            _legacyInventory.RemoveTemplate(templateID, count, minSlot, maxSlot);
        public bool CanStack(DbInventoryItem item1, DbInventoryItem item2) => 
            item1?.Name == item2?.Name && item1?.IsStackable == true;

        #endregion

        #region Transaction Operations (Minimal Implementation)

        public void BeginChanges() => _legacyInventory.BeginChanges();
        public void CommitChanges() => _legacyInventory.CommitChanges();
        public bool UpdateInventoryWeight() => _legacyInventory.UpdateInventoryWeight();
        public void ClearInventory() => _legacyInventory.ClearInventory();
        public IList<eInventorySlot> GetChangedSlots() => new List<eInventorySlot>();

        #endregion

        #region Persistence Operations (Minimal Implementation)

        public bool LoadFromDatabase(string inventoryId) => _legacyInventory.LoadFromDatabase(inventoryId);
        public Task<IList> StartLoadFromDatabaseTask(string inventoryId) => 
            _legacyInventory.StartLoadFromDatabaseTask(inventoryId);
        public bool LoadInventory(string inventoryId, IList items) => 
            _legacyInventory.LoadInventory(inventoryId, items);
        public bool SaveIntoDatabase(string inventoryId) => _legacyInventory.SaveIntoDatabase(inventoryId);
        public void MarkForDeletion(DbInventoryItem item) { /* Legacy handles this */ }
        public ICollection<DbInventoryItem> GetItemsAwaitingDeletion() => new List<DbInventoryItem>();
        public void ProcessPendingOperations() { /* Legacy handles this */ }

        #endregion

        #region Validation Operations (Minimal Implementation)

        public ValidationResult ValidateAddItem(DbInventoryItem item, eInventorySlot slot) =>
            CanAddItem(item, slot) ? ValidationResult.Success() : 
            ValidationResult.Failure("Cannot add item to slot");
        public ValidationResult ValidateEquipment(DbInventoryItem item, eInventorySlot slot) => 
            ValidationResult.Success();
        public IList<eInventorySlot> GetConflictingSlots(DbInventoryItem item, eInventorySlot targetSlot) => 
            new List<eInventorySlot>();
        public ValidationResult ValidateExternalMove(DbInventoryItem item, eInventorySlot fromSlot, eInventorySlot toSlot) => 
            ValidationResult.Success();
        public bool IsValidSlot(eInventorySlot slot) => slot != eInventorySlot.Invalid;

        #endregion

        #region Equipment Effects Operations (Minimal Implementation)

        public void ApplyEquipmentBonuses(DbInventoryItem item, eInventorySlot slot) { /* Legacy handles this */ }
        public void RemoveEquipmentBonuses(DbInventoryItem item, eInventorySlot slot) { /* Legacy handles this */ }
        public void UpdateVisualAppearance(DbInventoryItem item, eInventorySlot slot, bool equipped) { /* Legacy handles this */ }
        public ICollection<DbInventoryItem> GetVisibleItems() => 
            _legacyInventory.VisibleItems ?? new List<DbInventoryItem>();
        public bool IsVisibleSlot(eInventorySlot slot) => slot >= eInventorySlot.MinEquipable && slot <= eInventorySlot.MaxEquipable;

        #endregion

        #region Legacy Implementation Requirements

        public bool AddItemWithoutDbAddition(eInventorySlot slot, DbInventoryItem item) => 
            _legacyInventory.AddItemWithoutDbAddition(slot, item);
        public bool RemoveItemWithoutDbDeletion(DbInventoryItem item) => 
            _legacyInventory.RemoveItemWithoutDbDeletion(item);
        public bool CheckItemsBeforeMovingFromOrToExternalInventory(DbInventoryItem fromItem, DbInventoryItem toItem, 
            eInventorySlot externalSlot, eInventorySlot playerInventorySlot, int itemCount) => 
            _legacyInventory.CheckItemsBeforeMovingFromOrToExternalInventory(fromItem, toItem, externalSlot, playerInventorySlot, itemCount);
        public void OnItemMove(DbInventoryItem fromItem, DbInventoryItem toItem, eInventorySlot fromSlot, eInventorySlot toSlot) => 
            _legacyInventory.OnItemMove(fromItem, toItem, fromSlot, toSlot);
        public int CountSlots(bool countUsed, eInventorySlot minSlot, eInventorySlot maxSlot) => 
            _legacyInventory.CountSlots(countUsed, minSlot, maxSlot);
        public int CountItemTemplate(string itemtemplateID, eInventorySlot minSlot, eInventorySlot maxSlot) => 
            _legacyInventory.CountItemTemplate(itemtemplateID, minSlot, maxSlot);
        public bool IsSlotsFree(int count, eInventorySlot minSlot, eInventorySlot maxSlot) => 
            _legacyInventory.IsSlotsFree(count, minSlot, maxSlot);
        public eInventorySlot FindLastEmptySlot(eInventorySlot first, eInventorySlot last) => 
            _legacyInventory.FindLastEmptySlot(first, last);
        public eInventorySlot FindFirstFullSlot(eInventorySlot first, eInventorySlot last) => 
            _legacyInventory.FindFirstFullSlot(first, last);
        public eInventorySlot FindLastFullSlot(eInventorySlot first, eInventorySlot last) => 
            _legacyInventory.FindLastFullSlot(first, last);
        public eInventorySlot FindFirstPartiallyFullSlot(eInventorySlot first, eInventorySlot last, DbInventoryItem item) => 
            _legacyInventory.FindFirstPartiallyFullSlot(first, last, item);
        public eInventorySlot FindLastPartiallyFullSlot(eInventorySlot first, eInventorySlot last, DbInventoryItem item) => 
            _legacyInventory.FindLastPartiallyFullSlot(first, last, item);
        public DbInventoryItem GetFirstItemByID(string uniqueID, eInventorySlot minSlot, eInventorySlot maxSlot) => 
            _legacyInventory.GetFirstItemByID(uniqueID, minSlot, maxSlot);
        public DbInventoryItem GetFirstItemByObjectType(int objectType, eInventorySlot minSlot, eInventorySlot maxSlot) => 
            _legacyInventory.GetFirstItemByObjectType(objectType, minSlot, maxSlot);
        public DbInventoryItem GetFirstItemByName(string name, eInventorySlot minSlot, eInventorySlot maxSlot) => 
            _legacyInventory.GetFirstItemByName(name, minSlot, maxSlot);

        #endregion

        #region Properties

        public ICollection<DbInventoryItem> VisibleItems => 
            _legacyInventory.VisibleItems ?? new List<DbInventoryItem>();
        public ICollection<DbInventoryItem> EquippedItems => 
            _legacyInventory.EquippedItems ?? new List<DbInventoryItem>();
        public ICollection<DbInventoryItem> AllItems => 
            _legacyInventory.AllItems ?? new List<DbInventoryItem>();
        public int InventoryWeight => _legacyInventory.InventoryWeight;
        public string InventoryId => "player_inventory";
        public object SyncRoot => _lock;
        
        /// <summary>
        /// Legacy interface requirement - returns proper Lock type
        /// </summary>
        public Lock Lock => _lock;

        #endregion

        #region Legacy Access

        /// <summary>
        /// Provides access to underlying legacy inventory for gradual migration
        /// </summary>
        public GamePlayerInventory LegacyInventory => _legacyInventory;

        #endregion
    }

    /// <summary>
    /// Extension methods for inventory adapter creation
    /// Following clean architecture patterns
    /// </summary>
    public static class InventoryAdapterExtensions
    {
        public static IGameInventory ToAdapter(this GamePlayerInventory inventory, ILogger<CharacterInventoryAdapter> logger = null)
        {
            return new CharacterInventoryAdapter(inventory, logger);
        }
    }
} 