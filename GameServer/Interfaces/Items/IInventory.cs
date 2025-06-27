using System;
using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS.Interfaces.Items
{
    /// <summary>
    /// Core inventory operations interface
    /// DAoC Rule: Basic inventory management with slot-based storage
    /// Following ISP - Maximum 5 methods per interface
    /// </summary>
    public interface IInventory
    {
        /// <summary>
        /// Get item in specific slot
        /// </summary>
        DbInventoryItem GetItem(eInventorySlot slot);
        
        /// <summary>
        /// Add item to inventory slot
        /// DAoC Rule: Validates slot availability and item compatibility
        /// </summary>
        bool AddItem(DbInventoryItem item, eInventorySlot slot);
        
        /// <summary>
        /// Remove item from inventory
        /// </summary>
        bool RemoveItem(DbInventoryItem item);
        
        /// <summary>
        /// Move item between slots
        /// DAoC Rule: Supports partial stacking for compatible items
        /// </summary>
        bool MoveItem(eInventorySlot fromSlot, eInventorySlot toSlot, int itemCount);
        
        /// <summary>
        /// Check if slot is available for item
        /// </summary>
        bool CanAddItem(DbInventoryItem item, eInventorySlot slot);
    }

    /// <summary>
    /// Equipment management operations
    /// DAoC Rule: Equipment slots provide stat bonuses and visual appearance
    /// </summary>
    public interface IEquipmentManager
    {
        /// <summary>
        /// Equip item to specific equipment slot
        /// DAoC Rule: Validates level, class, and skill requirements
        /// </summary>
        bool EquipItem(DbInventoryItem item, eInventorySlot slot);
        
        /// <summary>
        /// Unequip item from slot to backpack
        /// </summary>
        bool UnequipItem(eInventorySlot slot);
        
        /// <summary>
        /// Check if item can be equipped in slot
        /// DAoC Rule: Validates all equipment restrictions
        /// </summary>
        bool CanEquipItem(DbInventoryItem item, eInventorySlot slot);
        
        /// <summary>
        /// Get equipped item in specific slot
        /// </summary>
        DbInventoryItem GetEquippedItem(eInventorySlot slot);
        
        /// <summary>
        /// Get all equipped items
        /// </summary>
        ICollection<DbInventoryItem> GetEquippedItems();
    }

    /// <summary>
    /// Inventory query and search operations
    /// DAoC Rule: Efficient item lookup and filtering
    /// </summary>
    public interface IInventoryQuery
    {
        /// <summary>
        /// Get items in slot range
        /// </summary>
        ICollection<DbInventoryItem> GetItemRange(eInventorySlot minSlot, eInventorySlot maxSlot);
        
        /// <summary>
        /// Find first item by name in range
        /// </summary>
        DbInventoryItem FindFirstItemByName(string name, eInventorySlot minSlot, eInventorySlot maxSlot);
        
        /// <summary>
        /// Get count of free slots in range
        /// </summary>
        int GetFreeSlotCount(eInventorySlot minSlot, eInventorySlot maxSlot);
        
        /// <summary>
        /// Find first empty slot in range
        /// </summary>
        eInventorySlot FindFirstEmptySlot(eInventorySlot minSlot, eInventorySlot maxSlot);
        
        /// <summary>
        /// Get all items in inventory
        /// </summary>
        ICollection<DbInventoryItem> GetAllItems();
    }

    /// <summary>
    /// Inventory stacking and template operations
    /// DAoC Rule: Stackable items combine automatically up to MaxCount
    /// </summary>
    public interface IInventoryStacking
    {
        /// <summary>
        /// Add count to existing stack
        /// DAoC Rule: Respects item MaxCount limits
        /// </summary>
        bool AddCountToStack(DbInventoryItem item, int count);
        
        /// <summary>
        /// Remove count from stack
        /// </summary>
        bool RemoveCountFromStack(DbInventoryItem item, int count);
        
        /// <summary>
        /// Add items from template with automatic stacking
        /// </summary>
        bool AddTemplate(DbInventoryItem template, int count, eInventorySlot minSlot, eInventorySlot maxSlot);
        
        /// <summary>
        /// Remove items by template ID
        /// </summary>
        bool RemoveTemplate(string templateID, int count, eInventorySlot minSlot, eInventorySlot maxSlot);
        
        /// <summary>
        /// Check if items can stack together
        /// </summary>
        bool CanStack(DbInventoryItem item1, DbInventoryItem item2);
    }

    /// <summary>
    /// Inventory transaction management
    /// DAoC Rule: Batch operations for consistency and performance
    /// </summary>
    public interface IInventoryTransaction
    {
        /// <summary>
        /// Begin batch of inventory changes
        /// </summary>
        void BeginChanges();
        
        /// <summary>
        /// Commit all pending changes
        /// </summary>
        void CommitChanges();
        
        /// <summary>
        /// Update inventory weight calculation
        /// DAoC Rule: Encumbrance affects movement speed
        /// </summary>
        bool UpdateInventoryWeight();
        
        /// <summary>
        /// Clear entire inventory
        /// </summary>
        void ClearInventory();
        
        /// <summary>
        /// Get list of slots changed since last commit
        /// </summary>
        IList<eInventorySlot> GetChangedSlots();
    }

    /// <summary>
    /// Inventory persistence operations
    /// DAoC Rule: Items persist between sessions with database storage
    /// </summary>
    public interface IInventoryPersistence
    {
        /// <summary>
        /// Load inventory from database
        /// </summary>
        bool LoadFromDatabase(string inventoryId);
        
        /// <summary>
        /// Save inventory to database
        /// </summary>
        bool SaveIntoDatabase(string inventoryId);
        
        /// <summary>
        /// Mark item for deletion from database
        /// </summary>
        void MarkForDeletion(DbInventoryItem item);
        
        /// <summary>
        /// Get items awaiting database deletion
        /// </summary>
        ICollection<DbInventoryItem> GetItemsAwaitingDeletion();
        
        /// <summary>
        /// Process all pending database operations
        /// </summary>
        void ProcessPendingOperations();
    }

    /// <summary>
    /// Inventory validation and rules enforcement
    /// DAoC Rule: Complex validation rules for equipment and items
    /// </summary>
    public interface IInventoryValidator
    {
        /// <summary>
        /// Validate item can be added to slot
        /// DAoC Rule: Checks conflicts, requirements, and restrictions
        /// </summary>
        ValidationResult ValidateAddItem(DbInventoryItem item, eInventorySlot slot);
        
        /// <summary>
        /// Validate equipment restrictions
        /// DAoC Rule: Level, class, realm, and skill requirements
        /// </summary>
        ValidationResult ValidateEquipment(DbInventoryItem item, eInventorySlot slot);
        
        /// <summary>
        /// Check for equipment conflicts
        /// DAoC Rule: Two-handed weapons conflict with shields
        /// </summary>
        IList<eInventorySlot> GetConflictingSlots(DbInventoryItem item, eInventorySlot targetSlot);
        
        /// <summary>
        /// Validate item movement between inventories
        /// </summary>
        ValidationResult ValidateExternalMove(DbInventoryItem item, eInventorySlot fromSlot, eInventorySlot toSlot);
        
        /// <summary>
        /// Check if slot is valid for inventory type
        /// </summary>
        bool IsValidSlot(eInventorySlot slot);
    }

    /// <summary>
    /// Equipment visual and bonus management
    /// DAoC Rule: Equipment affects appearance and provides stat bonuses
    /// </summary>
    public interface IEquipmentEffects
    {
        /// <summary>
        /// Apply equipment bonuses to character
        /// DAoC Rule: Item bonuses add to character stats with caps
        /// </summary>
        void ApplyEquipmentBonuses(DbInventoryItem item, eInventorySlot slot);
        
        /// <summary>
        /// Remove equipment bonuses from character
        /// </summary>
        void RemoveEquipmentBonuses(DbInventoryItem item, eInventorySlot slot);
        
        /// <summary>
        /// Update visual appearance for equipped item
        /// </summary>
        void UpdateVisualAppearance(DbInventoryItem item, eInventorySlot slot, bool equipped);
        
        /// <summary>
        /// Get all visible equipment items
        /// </summary>
        ICollection<DbInventoryItem> GetVisibleItems();
        
        /// <summary>
        /// Check if slot affects visual appearance
        /// </summary>
        bool IsVisibleSlot(eInventorySlot slot);
    }

    /// <summary>
    /// Validation result for inventory operations
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public ValidationErrorType ErrorType { get; set; }
        public IList<eInventorySlot> ConflictingSlots { get; set; } = new List<eInventorySlot>();

        public static ValidationResult Success() => new() { IsValid = true };
        public static ValidationResult Failure(string message, ValidationErrorType type = ValidationErrorType.General)
            => new() { IsValid = false, ErrorMessage = message, ErrorType = type };
    }

    /// <summary>
    /// Types of validation errors
    /// </summary>
    public enum ValidationErrorType
    {
        General,
        LevelRequirement,
        ClassRequirement,
        RealmRequirement,
        SkillRequirement,
        SlotConflict,
        ItemConflict,
        CapacityLimit,
        WeightLimit
    }

    /// <summary>
    /// Comprehensive inventory interface combining all operations
    /// Used for dependency injection registration
    /// </summary>
    public interface IGameInventory : IInventory, IEquipmentManager, IInventoryQuery, 
        IInventoryStacking, IInventoryTransaction, IInventoryPersistence, 
        IInventoryValidator, IEquipmentEffects
    {
        /// <summary>
        /// Inventory owner identifier
        /// </summary>
        string InventoryId { get; }
        
        /// <summary>
        /// Lock for thread-safe operations
        /// </summary>
        object SyncRoot { get; }
    }
} 