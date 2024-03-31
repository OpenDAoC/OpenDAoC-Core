using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS
{
    public enum eInventorySlot : int
    {
        LastEmptyBagHorse   = -8,
        FirstEmptyBagHorse  = -7,
        LastEmptyQuiver     = -6,
        FirstEmptyQuiver    = -5,
        LastEmptyVault      = -4,
        FirstEmptyVault     = -3,
        LastEmptyBackpack   = -2,
        FirstEmptyBackpack  = -1,

        Invalid             = 0,
        Ground              = 1,

        Min_Inv             = 7,

        HorseArmor          = 7, // Equipment, horse armor
        HorseBarding        = 8, // Equipment, horse barding
        Horse               = 9, // Equipment, horse

        MinEquipable        = 10,
        RightHandWeapon     = 10, // Equipment, visible
        LeftHandWeapon      = 11, // Equipment, visible
        TwoHandWeapon       = 12, // Equipment, visible
        DistanceWeapon      = 13, // Equipment, visible
        FirstQuiver         = 14,
        SecondQuiver        = 15,
        ThirdQuiver         = 16,
        FourthQuiver        = 17,
        HeadArmor           = 21, // Equipment, visible
        HandsArmor          = 22, // Equipment, visible
        FeetArmor           = 23, // Equipment, visible
        Jewelry             = 24, // Equipment
        TorsoArmor          = 25, // Equipment, visible
        Cloak               = 26, // Equipment, visible
        LegsArmor           = 27, // Equipment, visible
        ArmsArmor           = 28, // Equipment, visible
        Neck                = 29, // Equipment
        Waist               = 32, // Equipment
        LeftBracer          = 33, // Equipment
        RightBracer         = 34, // Equipment
        LeftRing            = 35, // Equipment
        RightRing           = 36, // Equipment
        Mythical            = 37, // Equipment
        MaxEquipable        = 37,

        FirstBackpack       = 40,
        LastBackpack        = 79,

        FirstBagHorse       = 80,
        LastBagHorse        = 95,

        LeftFrontSaddleBag  = 96,
        RightFrontSaddleBag = 97,
        LeftRearSaddleBag   = 98,
        RightRearSaddleBag  = 99,

        PlayerPaperDoll     = 100,

        Mithril             = 101,
        Platinum            = 102,
        Gold                = 103,
        Silver              = 104,
        Copper              = 105,

        FirstVault          = 110,
        LastVault           = 149,

        HousingInventory_First = 150,
        HousingInventory_Last  = 249,

        HouseVault_First    = 1000,
        HouseVault_Last     = 1399,

        Consignment_First   = 1500,
        Consignment_Last    = 1599,

        MarketExplorerFirst = 1000,

        //FirstFixLoot      = 256, //You can define drops that will ALWAYS occur (eg quest drops etc.)
        //LastFixLoot       = 356, //100 drops should be enough ... if not, just raise this var, we have thousands free
        //LootPagesStart    = 500, //Let's say each loot page is 100 slots in size, lots of space for random drops

        // Money slots changed since 178.
        Mithril178          = 500,
        Platinum178         = 501,
        Gold178             = 502,
        Silver178           = 503,
        Copper178           = 504,

        // Destination slots sent when the client is shift right clicking an item or dropping something on the paper doll.
        NewPlayerPaperDoll  = 600,
        GeneralVault        = 601,
        GeneralHousing      = 602,

        Max_Inv = 249
    }

    /// <summary>
    /// The use type applyed to the item:
    /// clic on icon in quickbar, /use or /use2
    /// </summary>
    public enum eUseType
    {
        Click = 0,
        Use1 = 1,
        Use2 = 2,
    }

    /// <summary>
    /// Interface for GameInventory
    /// </summary>
    public interface IGameInventory
    {
        bool LoadFromDatabase(string inventoryID);
        bool SaveIntoDatabase(string inventoryID);

        bool AddItem(eInventorySlot slot, DbInventoryItem item);
        bool AddItemWithoutDbAddition(eInventorySlot slot, DbInventoryItem item);
        bool AddCountToStack(DbInventoryItem item, int count);
        bool AddTemplate(DbInventoryItem template, int count, eInventorySlot minSlot, eInventorySlot maxSlot);
        bool RemoveItem(DbInventoryItem item);
        bool RemoveItemWithoutDbDeletion(DbInventoryItem item);
        bool RemoveCountFromStack(DbInventoryItem item, int count);
        bool RemoveTemplate(string templateID, int count, eInventorySlot minSlot, eInventorySlot maxSlot);
        bool MoveItem(eInventorySlot fromSlot, eInventorySlot toSlot, int itemCount);
        bool CheckItemsBeforeMovingFromOrToExternalInventory(DbInventoryItem fromItem, DbInventoryItem toItem, eInventorySlot externalSlot, eInventorySlot playerInventorySlot, int itemCount);
        void OnItemMove(DbInventoryItem fromItem, DbInventoryItem toItem, eInventorySlot fromSlot, eInventorySlot toSlot);
        DbInventoryItem GetItem(eInventorySlot slot);
        ICollection<DbInventoryItem> GetItemRange(eInventorySlot minSlot, eInventorySlot maxSlot);

        void BeginChanges();
        void CommitChanges();
        void ClearInventory();

        int CountSlots(bool countUsed, eInventorySlot minSlot, eInventorySlot maxSlot);
        int CountItemTemplate(string itemtemplateID, eInventorySlot minSlot, eInventorySlot maxSlot);
        bool IsSlotsFree(int count, eInventorySlot minSlot, eInventorySlot maxSlot);

        eInventorySlot FindFirstEmptySlot(eInventorySlot first, eInventorySlot last);
        eInventorySlot FindLastEmptySlot(eInventorySlot first, eInventorySlot last);
        eInventorySlot FindFirstFullSlot(eInventorySlot first, eInventorySlot last);
        eInventorySlot FindLastFullSlot(eInventorySlot first, eInventorySlot last);
        eInventorySlot FindFirstPartiallyFullSlot(eInventorySlot first, eInventorySlot last, DbInventoryItem item);
        eInventorySlot FindLastPartiallyFullSlot(eInventorySlot first, eInventorySlot last, DbInventoryItem item);

        DbInventoryItem GetFirstItemByID(string uniqueID, eInventorySlot minSlot, eInventorySlot maxSlot);
        DbInventoryItem GetFirstItemByObjectType(int objectType, eInventorySlot minSlot, eInventorySlot maxSlot);
        DbInventoryItem GetFirstItemByName(string name ,eInventorySlot minSlot, eInventorySlot maxSlot);

        ICollection<DbInventoryItem> VisibleItems { get; }
        ICollection<DbInventoryItem> EquippedItems { get; }
        ICollection<DbInventoryItem> AllItems { get; }
        int InventoryWeight { get; }
        object LockObject { get; }
    }
}
