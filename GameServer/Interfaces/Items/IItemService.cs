using DOL.GS.Interfaces.Core;
using DOL.GS.Interfaces.Character;
using DOL.GS.Interfaces.Combat;
using System.Collections.Generic;

namespace DOL.GS.Interfaces.Items
{
    /// <summary>
    /// Service for managing items
    /// </summary>
    public interface IItemService : IGameService
    {
        IItem GetItem(string itemId);
        void UpdateItem(IItem item);
        void RepairItem(IItem item, int repairAmount);
        void DegradeItem(IItem item, int degradeAmount);
    }

    /// <summary>
    /// Factory for creating items
    /// </summary>
    public interface IItemFactory
    {
        IItem CreateFromTemplate(string templateId);
        IItem CreateRandom(int level, ItemType type);
        IItem CreateUnique(string uniqueId);
    }

    /// <summary>
    /// Calculator for item bonuses and effectiveness
    /// </summary>
    public interface IItemBonusCalculator
    {
        int GetBonusCap(int level, Property property);
        int GetEffectiveBonus(IItem item, Property property);
        int GetQualityModifier(int quality);
        int GetConditionModifier(int condition);
        int GetDurabilityModifier(int durability, int maxDurability);
    }

    /// <summary>
    /// Service for equipment management
    /// </summary>
    public interface IEquipmentService : IGameService
    {
        bool CanEquip(ICharacter character, IItem item, int slot);
        void EquipItem(ICharacter character, IItem item, int slot);
        void UnequipItem(ICharacter character, int slot);
        IItem GetEquippedItem(ICharacter character, int slot);
        Dictionary<int, IItem> GetAllEquippedItems(ICharacter character);
    }
} 