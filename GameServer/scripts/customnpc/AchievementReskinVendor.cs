using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOLDatabase.Tables;

namespace DOL.GS;

public class AchievementReskinVendor : GameNPC
{
    public string TempProperty = "ItemModel";
    public string DisplayedItem = "ItemDisplay";
    public string TempModelID = "TempModelID";
    public string TempModelPrice = "TempModelPrice";
    public string currencyName = "Orbs";
    private int Chance;
    private Random rnd = new Random();

    //placeholder prices
    //500 lowbie stuff
    //2k low toa
    //4k high toa
    //10k dragonsworn
    //20k champion
    private int freebie = 1;
    private int lowbie = 1000;
    private int festive = 2000;
    private int toageneric = 7500;
    private int armorpads = 2500;
    private int artifact = 15000;
    private int epic = 10000;
    private int dragonCost = 10000;
    private int champion = 35000;
    private int cloakcheap = 10000;
    private int cloakmedium = 18000;
    private int cloakexpensive = 35000;

    public override bool AddToWorld()
    {
        base.AddToWorld();
        return true;
    }

    public override bool Interact(GamePlayer player)
    {
        if (base.Interact(player))
        {
            TurnTo(player, 500);
            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);
            InventoryItem displayItem = player.TempProperties.getProperty<InventoryItem>(DisplayedItem);

            if (item == null)
            {
                SendReply(player, "Hello there! \n" +
                                  "I can offer a variety of aesthetics... for those willing to pay for it.\n" +
                                  "Hand me the item and then we can talk prices.");
            }
            else
            {
                ReceiveItem(player, item);
            }

            if (displayItem != null)
                DisplayReskinPreviewTo(player, (InventoryItem)displayItem.Clone());

            return true;
        }

        return false;
    }

    public override bool ReceiveItem(GameLiving source, InventoryItem item)
    {
        GamePlayer t = source as GamePlayer;
        if (t == null || item == null || item.Template.Name.Equals("token_many")) return false;
        if (GetDistanceTo(t) > WorldMgr.INTERACT_DISTANCE)
        {
            t.Out.SendMessage("You are too far away to give anything to " + GetName(0, false) + ".",
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }
        StringBuilder sb = new StringBuilder();
        List<SkinVendorItem> VendorItemList = new List<SkinVendorItem>();

        // Two Handed Weapons //
        //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //alb
        VendorItemList.Add(new SkinVendorItem("Battle Axe 2h", 9, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("War Mattock 2h", 16, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Albion War Axe 2h", 73, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Albion Great Hammer 2h", 17, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, freebie));

        //rr2
        VendorItemList.Add(new SkinVendorItem("Briton Arch Mace 2h", 640, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Briton Scimitar 2h", 645, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Briton War Pick 2h", 646, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Thrust, lowbie));

        //rr4
        VendorItemList.Add(new SkinVendorItem("Zweihander 2h", 841, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Claymore 2h", 843, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Great Mace 2h", 842, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Dire Hammer 2h", 844, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Dire Axe 2h", 845, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Great Mattock 2h", 846, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Great Scimitar 2h", 847, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, toageneric));

        //mid
        VendorItemList.Add(new SkinVendorItem("Norse Sword 2h", 314, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Norse Great Axe 2h", 317, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Norse Large Axe 2h", 318, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Norse Hammer 2h", 574, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, freebie));

        //rr2
        VendorItemList.Add(new SkinVendorItem("Norse Greatsword 2h", 572, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Norse Battleaxe 2h", 577, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Norse Greathammer 2h", 576, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Norse Warhammer 2h", 575, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, lowbie));

        //rr3
        VendorItemList.Add(new SkinVendorItem("Dwarven Sword 2h", 658, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Dwarven Greataxe 2h", 1027, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Dwarven Great Hammer 2h", 1028, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("War Cleaver 2h", 660, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Spiked Hammer 2h", 659, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Troll Greatsword 2h", 957, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Kobold Greataxe 2h", 1030, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Kobold Great Club 2h", 1031, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Kobold Great Sword 2h", 1032, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));

        //hib
        VendorItemList.Add(new SkinVendorItem("Celtic Greatsword 2h", 448, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Celtic Sword 2h", 459, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Celtic Great Hammer 2h", 462, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Celtic Spiked Mace 2h", 463, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Firbolg Scythe", 927, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Harvest Scythe", 929, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("War Scythe", 932, Slot.TWOHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, freebie));

        //rr2
        VendorItemList.Add(new SkinVendorItem("Celtic Falcata 2h", 639, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Celtic Sledgehammer 2h", 640, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Firbolg Great Scythe", 926, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Great War Scythe", 928, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Martial Scythe", 930, Slot.TWOHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, lowbie));

        //rr4
        VendorItemList.Add(new SkinVendorItem("Celtic Hammer 2h", 904, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Celtic Great Mace 2h", 905, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Celtic Dire Club 2h", 906, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Elven Greatsword 2h", 907, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Firbolg Hammer 2h", 908, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Firbolg Mace 2h", 909, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Firbolg Trollsplitter 2h", 910, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leaf Point 2h", 911, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Shod Shillelagh 2h", 912, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Magma Scythe", 2213, Slot.TWOHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, toageneric));


        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // OneHanded Weapons
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // Generic
        VendorItemList.Add(new SkinVendorItem("Wakazashi 1h", 2209, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wakazashi 1h", 2209, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wakazashi 1h", 2209, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, toageneric));

        VendorItemList.Add(new SkinVendorItem("Magma Hammer 1h", 2214, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Magma Hammer 1h", 2214, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Magma Hammer 1h", 2214, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, toageneric));

        VendorItemList.Add(new SkinVendorItem("Aerus Hammer 1h", 2205, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Hammer 1h", 2205, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Hammer 1h", 2205, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, toageneric));

        VendorItemList.Add(new SkinVendorItem("Khopesh 1h", 2195, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Khopesh 1h", 2195, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Khopesh 1h", 2195, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, toageneric));

        VendorItemList.Add(new SkinVendorItem("Aerus Sword 1h", 2203, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Sword 1h", 2203, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Sword 1h", 2203, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, toageneric));

        VendorItemList.Add(new SkinVendorItem("Magma Axe 1h", 2216, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Magma Axe 1h", 2216, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Magma Axe 1h", 2216, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, toageneric));

        VendorItemList.Add(new SkinVendorItem("Traitor's Dagger 1h", 1668, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, artifact));
        VendorItemList.Add(new SkinVendorItem("Traitor's Dagger 1h", 1668, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, artifact));

        VendorItemList.Add(new SkinVendorItem("Croc Tooth Dagger 1h", 1669, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, artifact));
        VendorItemList.Add(new SkinVendorItem("Croc Tooth Dagger 1h", 1669, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, artifact));

        VendorItemList.Add(new SkinVendorItem("Golden Spear 1h", 1807, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, artifact));
        VendorItemList.Add(new SkinVendorItem("Golden Spear 1h", 1807, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, artifact));

        VendorItemList.Add(new SkinVendorItem("Battler Hammer 1h", 3453, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, artifact));
        VendorItemList.Add(new SkinVendorItem("Battler Hammer 1h", 3453, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, artifact));
        VendorItemList.Add(new SkinVendorItem("Battler Hammer 1h", 3453, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, artifact));


        VendorItemList.Add(new SkinVendorItem("Malice Hammer 1h", 3447, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, artifact));
        VendorItemList.Add(new SkinVendorItem("Malice Hammer 1h", 3447, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, artifact));
        VendorItemList.Add(new SkinVendorItem("Malice Hammer 1h", 3447, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, artifact));

        VendorItemList.Add(new SkinVendorItem("Bruiser Hammer 1h", 1671, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, artifact));
        VendorItemList.Add(new SkinVendorItem("Bruiser Hammer 1h", 1671, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, artifact));
        VendorItemList.Add(new SkinVendorItem("Bruiser Hammer 1h", 1671, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, artifact));

        VendorItemList.Add(new SkinVendorItem("Scepter of the Meritorious", 1672, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, artifact));
        VendorItemList.Add(new SkinVendorItem("Scepter of the Meritorious", 1672, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, artifact));
        VendorItemList.Add(new SkinVendorItem("Scepter of the Meritorious", 1672, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, artifact));

        VendorItemList.Add(new SkinVendorItem("Croc Tooth Axe 1h", 3451, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, artifact));
        VendorItemList.Add(new SkinVendorItem("Croc Tooth Axe 1h", 3451, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, artifact));
        VendorItemList.Add(new SkinVendorItem("Croc Tooth Axe 1h", 3451, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, artifact));

        VendorItemList.Add(new SkinVendorItem("Traitor's Axe 1h", 3452, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, artifact));
        VendorItemList.Add(new SkinVendorItem("Traitor's Axe 1h", 3452, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, artifact));
        VendorItemList.Add(new SkinVendorItem("Traitor's Axe 1h", 3452, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, artifact));

        VendorItemList.Add(new SkinVendorItem("Malice Axe 1h", 2109, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, artifact));
        VendorItemList.Add(new SkinVendorItem("Malice Axe 1h", 2109, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, artifact));
        VendorItemList.Add(new SkinVendorItem("Malice Axe 1h", 2109, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, artifact));

        VendorItemList.Add(new SkinVendorItem("Battler Sword 1h", 2112, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, artifact));
        VendorItemList.Add(new SkinVendorItem("Battler Sword 1h", 2112, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, artifact));
        VendorItemList.Add(new SkinVendorItem("Battler Sword 1h", 2112, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, artifact));

        // alb

        //flex unsure of dmgtype
        VendorItemList.Add(new SkinVendorItem("Flex Chain", 857, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Flex Whip", 859, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Flex Flail", 861, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Flex Dagger Flail", 860, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Flex Morning Star", 862, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Flex Pick Flail", 863, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Slash, freebie));

        VendorItemList.Add(new SkinVendorItem("Dirk 1h", 21, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Rapier 1h", 22, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Main Gauche 1h", 25, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Club 1h", 11, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Hammer 1h", 12, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Mace 1h", 13, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Short Sword 1h", 3, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Longsword 1h", 4, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Handaxe 1h", 2, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Scimitar 1h", 8, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, freebie));


        //rr2 
        //flex unsure of dmgtype
        VendorItemList.Add(new SkinVendorItem("Flex Spiked Flail", 864, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Flex Spiked Whip", 865, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Flex War Chain", 866, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Flex Whip Dagger", 868, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Flex Whip Mace", 869, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Toothpick 1h", 876, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Highlander Dirk 1h", 889, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Foil 1h", 29, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Guarded Rapier 1h", 653, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Spiked Mace 1h", 20, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("War Hammer 1h", 15, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Flanged Mace 1h", 14, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Jambiya 1h", 651, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Cinquedea 1h", 877, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Falchion 1h", 879, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Coffin Axe 1h", 876, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, lowbie));

        //rr4
        VendorItemList.Add(new SkinVendorItem("Duelists Dagger 1h", 885, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Duelists Rapier 1h", 886, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Parrying Dagger 1h", 887, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Parrying Rapier 1h", 888, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Bishops Mace 1h", 854, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Coffin Hammer Mace 1h", 855, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Coffin Mace 1h", 856, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, toageneric));

        //rr6
        VendorItemList.Add(new SkinVendorItem("Snakecharmer's Whip", 2119, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Slash, artifact));
      

        // hib
        VendorItemList.Add(new SkinVendorItem("Celtic Dirk 1h", 454, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Celtic Rapier 1h", 455, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Celtic Stiletto 1h", 456, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Celtic Club 1h", 449, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Celtic Mace 1h", 450, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Celtic Short Sword 1h", 445, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Celtic Longsword 1h", 456, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Celtic Broadsword 1h", 447, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, freebie));

        //rr2 
        VendorItemList.Add(new SkinVendorItem("Curved Dagger 1h", 457, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Celtic Guarded Rapier 1h", 643, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Lurikeen Dagger 1h", 943, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Spiked Club 1h", 452, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Celtic Spiked Mace 1h", 451, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Celtic Hammer 1h", 461, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Celtic Sickle 1h", 453, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Celtic Hooked Sword 1h", 460, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Falcata 1h", 444, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, lowbie));

        //rr4
        VendorItemList.Add(new SkinVendorItem("Elven Dagger 1h", 895, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Firbolg Dagger 1h", 898, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leaf Dagger 1h", 902, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Adze 1h", 940, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Barbed Adze 1h", 941, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Firbolg Adze 1h", 942, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("War Adze 1h", 947, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Celt Hammer 1h", 913, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Dire Mace 1h", 915, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Firbolg Hammer 1h", 916, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Elven Short Sword 1h", 897, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Elven Longsword 1h", 896, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Firbolg Short Sword 1h", 900, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Firbolg Longsword 1h", 899, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leaf Short Sword 1h", 903, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leaf Longsword 1h", 901, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));

        // mid
        //handtohand unsure of dmgtype
        VendorItemList.Add(new SkinVendorItem("Bladed Claw Greave", 959, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Bladed Fang Greave", 960, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Bladed Moon Claw", 961, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Bladed Moon Fang", 962, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Claw Greave", 963, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Fang Greave", 964, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Thrust, freebie));

        VendorItemList.Add(new SkinVendorItem("Small Hammer 1h", 320, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Norse Hammer 1h", 321, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Norse Short Sword 1h", 311, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Norse Broadsword 1h", 310, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Norse Spiked Axe 1h", 315, Slot.RIGHTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, freebie));
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        //rr2 
        VendorItemList.Add(new SkinVendorItem("Great Bladed Claw Greave", 965, Slot.RIGHTHAND, 0,2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Great Bladed Fang Greave", 966, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Great Bladed Moon Claw", 967, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Great Bladed Moon Fang", 968, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Great Claw Greave", 969, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Great Fang Greave", 970, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Great Hammer 1h", 324, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Pick Hammer 1h", 323, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Spiked Hammer 1h", 656, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Dwarven Short Sword 1h", 655, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Norse Cleaver 1h", 654, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Double Axe 1h", 573, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Norse Bearded Axe 1h", 316, Slot.RIGHTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, lowbie));

        //rr4
        VendorItemList.Add(new SkinVendorItem("Troll Hammer 1h", 950, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Troll War Hammer 1h", 954, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Kobold Sap 1h", 1016, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Kobold War Club 1h", 1019, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Troll Dagger 1h", 949, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Troll Short Sword 1h", 952, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Troll Long Sword 1h", 948, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Kobold Dagger 1h", 1013, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Kobold Short Sword 1h", 1017, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Kobold Long Sword 1h", 1015, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Troll Hand Axe 1h", 1023, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Troll War Axe 1h", 1025, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Kobold Hand Axe 1h", 1014, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Kobold War Axe 1h", 1018, Slot.RIGHTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));


        //rr6
        VendorItemList.Add(new SkinVendorItem("Snakecharmer's Fist", 2469, Slot.RIGHTHAND, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, artifact));

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // Shields
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // Generic
        VendorItemList.Add(new SkinVendorItem("Oceanus Small Shield", 2192, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Small Shield", 2210, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Magma Small Shield", 2218, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Small Shield", 2200, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Medium Shield", 2193, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Medium Shield", 2211, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Magma Medium Shield", 2219, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Medium Shield", 2201, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aten's Shield", 1663, Slot.LEFTHAND, 0, 6, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, artifact));
        VendorItemList.Add(new SkinVendorItem("Cyclop's Eye", 1664, Slot.LEFTHAND, 0, 6, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, artifact));
        VendorItemList.Add(new SkinVendorItem("Shield Of Khaos", 1665, Slot.LEFTHAND, 0, 6, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, artifact));
        VendorItemList.Add(new SkinVendorItem("Oceanus Large Shield", 2194, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Large Shield", 2212, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Magma Large Shield", 2220, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Large Shield", 2202, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));


        // alb
        VendorItemList.Add(new SkinVendorItem("Leather Buckler", 140, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Buckler", 141, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Buckler", 142, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Albion Dragonslayer Buckler", 3965, Slot.LEFTHAND, 0, 0, 25, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Heater", 1049, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Heater", 1050, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Heater", 1051, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Tower", 1085, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Tower", 1086, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Tower", 1087, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Albion Dragonslayer Medium", 3966, Slot.LEFTHAND, 0, 0, 25, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Leather Large Heater", 1067, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Heater", 1068, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Heater", 1069, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Leather Large Tower", 1058, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Tower", 1059, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Tower", 1060, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Leather Large Round", 1076, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Round", 1077, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Round", 1078, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Albion Dragonslayer Large", 3967, Slot.LEFTHAND, 0, 0, 25, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, dragonCost * 2));
        // rr2
        VendorItemList.Add(new SkinVendorItem("Leather Tri-Tip Buckler", 1103, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Metal Tri-Tip Buckler", 1104, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Wood Tri-Tip Buckler", 1105, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Horned", 1112, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Horned", 1113, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Horned", 1114, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Leather Large Horned", 1106, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Horned", 1107, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Horned", 1108, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));

        // rr4
        VendorItemList.Add(new SkinVendorItem("Leather Kite Buckler", 1118, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Kite Buckler", 1119, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Kite Buckler", 1120, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Kite", 1115, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Kite", 1116, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Kite", 1117, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leather Large Kite", 1109, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Large Kite", 1110, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Large Kite", 1111, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leather Studded Tower", 1121, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Studded Tower", 1122, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Studded Tower", 1123, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));

        // hib
        VendorItemList.Add(new SkinVendorItem("Leather Buckler", 146, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Buckler", 147, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Buckle", 148, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Hibernia Dragonslayer Buckler", 3888, Slot.LEFTHAND, 0, 0, 25, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Heater", 1055, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Heater", 1056, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Heater", 1057, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Tower", 1088, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Tower", 1089, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Tower", 1090, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Hibernia Dragonslayer Medium", 3889, Slot.LEFTHAND, 0, 0, 25, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Leather Large Heater", 1073, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Heater", 1074, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Heater", 1075, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Leather Large Tower", 1064, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Tower", 1065, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Tower", 1066, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Leather Large Round", 1082, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Round", 1083, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Round", 1084, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Hibernia Dragonslayer Large", 3890, Slot.LEFTHAND, 0, 0, 25, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, dragonCost * 2));

        // rr2
        VendorItemList.Add(new SkinVendorItem("Leather Celtic Buckler", 1148, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Metal Celtic Buckler", 1149, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Wood Celtic Buckler", 1150, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Celtic", 1145, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Celtic", 1146, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Celtic", 1147, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Leather Large Celtic", 1154, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Celtic", 1155, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Celtic", 1156, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));

        // rr4
        VendorItemList.Add(new SkinVendorItem("Leather Leaf Buckler", 1163, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Leaf Buckler", 1164, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Leaf Buckler", 1165, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Leaf", 1160, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Leaf", 1161, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Leaf", 1162, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leather Large Leaf", 1157, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Large Leaf", 1158, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Large Leaf", 1159, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leather Celtic Tower", 1151, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Celtic Tower", 1152, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Celtic Tower", 1153, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));

        // mid
        VendorItemList.Add(new SkinVendorItem("Leather Buckler", 143, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Buckler", 144, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Buckler", 145, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
        VendorItemList.Add(new SkinVendorItem("Midgard Dragonslayer Buckler", 3929, Slot.LEFTHAND, 0, 0, 25, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Heater", 1052, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Heater", 1053, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Heater", 1054, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Tower", 1091, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Tower", 1092, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Tower", 1093, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
        VendorItemList.Add(new SkinVendorItem("Midgard Dragonslayer Medium", 3930, Slot.LEFTHAND, 0, 0, 25, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Leather Large Heater", 1070, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Heater", 1071, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Heater", 1072, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Leather Large Tower", 1061, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Tower", 1062, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Tower", 1063, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Leather Large Round", 1079, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Round", 1080, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Round", 1081, Slot.LEFTHAND, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
        VendorItemList.Add(new SkinVendorItem("Midgard Dragonslayer Large", 3931, Slot.LEFTHAND, 0, 0, 25, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, dragonCost * 2));
        // rr2
        VendorItemList.Add(new SkinVendorItem("Leather Norse Buckler", 1139, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Metal Norse Buckler", 1140, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Wood Norse Buckler", 1141, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Crescent", 1124, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Crescent", 1125, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Crescent", 1126, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
        VendorItemList.Add(new SkinVendorItem("Leather Large Crescent", 1133, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Metal Large Crescent", 1134, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
        VendorItemList.Add(new SkinVendorItem("Wood Large Crescent", 1135, Slot.LEFTHAND, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));

        // rr3
        VendorItemList.Add(new SkinVendorItem("Leather Grave Buckler", 1130, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Grave Buckler", 1131, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Grave Buckler", 1132, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leather Medium Grave", 1132, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Medium Grave", 1128, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Medium Grave", 1129, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leather Large Grave", 1136, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Large Grave", 1137, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Large Grave", 1138, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Leather Norse Tower", 1142, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Metal Norse Tower", 1143, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
        VendorItemList.Add(new SkinVendorItem("Wood Norse Tower", 1144, Slot.LEFTHAND, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));

        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // Helm
        //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        // Generic

        // alb
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 822, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 62, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 824, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 824, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 63, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 64, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 823, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 1231, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 1233, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 1233, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 2812, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 93, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 1229, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 2800, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 1234, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 1234, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 1236, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 1238, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wizard Hat", 1278, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));


        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3864, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3862, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3866, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3861, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4056, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4054, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4055, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4055, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4057, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4053, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, dragonCost * 2));

        //rr 2
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1230, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1231, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1234, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1234, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1236, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1239, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1230, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1232, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1235, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1235, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1236, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1240, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, lowbie));
        //rr 4
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2253, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2256, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2262, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2262, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2265, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2268, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2307, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2310, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2316, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2316, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2313, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2322, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2361, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2364, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2370, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2370, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2373, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2376, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2415, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2418, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2424, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2424, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2421, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2430, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, toageneric));
        //rr 6
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, artifact));

        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, artifact));

        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, artifact));

        // hib
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 826, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 438, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 835, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 835, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 838, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 1197, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 439, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 836, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 836, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 1202, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 1197, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 440, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 837, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 837, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 840, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Robin Hood Hat", 1282, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Robin Hood Hat", 1282, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Robin Hood Hat", 1282, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Robin Hood Hat", 1282, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Robin Hood Hat", 1282, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Leaf Hat", 1285, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Leaf Hat", 1285, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Leaf Hat", 1285, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Leaf Hat", 1285, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Leaf Hat", 1285, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Stag Helm", 1288, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Stag Helm", 1288, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Stag Helm", 1288, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Stag Helm", 1288, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Stag Helm", 1288, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wizard Hat", 1279, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3864, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3862, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3867, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4063, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4061, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4062, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4062, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4066, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, dragonCost * 2));
        //rr2
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1197, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1198, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1199, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1199, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1200, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1197, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1198, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1199, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1199, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1200, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, lowbie));
        //rr4
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2289, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2292, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2298, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2298, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2301, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2343, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2346, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2352, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2352, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2355, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2397, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2400, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2406, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2406, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2409, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2451, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2454, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2460, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2460, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2463, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType.Heat, toageneric));
        //rr6
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, artifact));
        // mid
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 825, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 335, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 829, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 829, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 1", 832, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 1213, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 336, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 830, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 830, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 2", 833, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 1213, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 337, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 831, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 831, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 3", 834, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, freebie));
        VendorItemList.Add(new SkinVendorItem("Fur Cap", 1283, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Fur Cap", 1283, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Fur Cap", 1283, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Fur Cap", 1283, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Fur Cap", 1283, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wing Hat", 1286, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wing Hat", 1286, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wing Hat", 1286, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wing Hat", 1286, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wing Hat", 1286, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wolf Helm", 1289, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wolf Helm", 1289, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wolf Helm", 1289, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wolf Helm", 1289, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wolf Helm", 1289, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Wizard Hat", 1280, Slot.HELM, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, festive));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3864, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3862, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonsworn Helm", 3866, Slot.HELM, 0, 0, 10, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4070, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4068, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4069, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4069, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, dragonCost * 2));
        VendorItemList.Add(new SkinVendorItem("Dragonslayer Helm", 4072, Slot.HELM, 0, 0, 25, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, dragonCost * 2));

        //rr2
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1213, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1214, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1215, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1215, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 4", 1216, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1213, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1214, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1215, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1215, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, lowbie));
        VendorItemList.Add(new SkinVendorItem("Crafted Helm 5", 1216, Slot.HELM, 0, 2, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, lowbie));
        //rr4
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2271, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2274, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2280, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2280, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Oceanus Helm", 2277, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2325, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2328, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2334, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2334, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Stygia Helm", 2331, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2379, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2382, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2388, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2388, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Volcanus Helm", 2385, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2433, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2436, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2442, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2442, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, toageneric));
        VendorItemList.Add(new SkinVendorItem("Aerus Helm", 2439, Slot.HELM, 0, 4, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, toageneric));
        //rr6
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType.Heat, artifact));
        VendorItemList.Add(new SkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType.Heat, artifact));

        // var added = GameServer.Database.AddObject(VendorItemList);
        // var itemCount = GameServer.Database.GetObjectCount<SkinVendorItem>();

        DisplayReskinPreviewTo(t, item);

        int playerRealm = (int)t.Realm;
        int noneRealm = (int)eRealm.None;
        int damageType = (int)(eDamageType)item.Type_Damage;
        int characterClassUnknown = (int)eCharacterClass.Unknown;
        int playerClass = (int)(eCharacterClass)t.CharacterClass.ID;
        int playerRealmRank = t.RealmLevel;
        int accountRealmRank = t.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);
        int playerDragonKills = t.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);
        int playerOrbs = t.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned);
        int itemSlot = item.Item_Type;

        switch (item.Item_Type)
        {
            case Slot.HELM:
                //DisplayHelmOption(t, item);
              List<SkinVendorItem> foundItems = VendorItemList.FindAll(x => (x.ItemType == item.Item_Type)
              && (x.Realm == playerRealm || x.Realm == noneRealm)
              && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
              && x.PlayerRealmRank <= playerRealmRank
              && x.AccountRealmRank <= accountRealmRank
              && x.Orbs <= playerOrbs
              && x.Drake <= playerDragonKills
              && x.DamageType == damageType
              && x.ObjectType == item.Object_Type);

                foreach (var sItem in foundItems)
                {
                    sb.Append($"[{sItem.Name}] ({sItem.Price} {currencyName})\n");
                }


                Console.WriteLine("item type is " + item.Item_Type + " realm is " + playerRealm + "realmrank is " + playerRealmRank + " orbs is " + playerOrbs + " drake is " + playerDragonKills + " damageType is" + damageType + " objectType is " + item.Object_Type);

                SendReply(t, sb.ToString());

                break;
            case Slot.TORSO:
                DisplayTorsoOption(t, item);
                break;
            case Slot.ARMS:
                DisplayArmsOption(t, item);
                break;
            case Slot.LEGS:
                DisplayPantsOption(t, item);
                break;
            case Slot.HANDS:
                DisplayGloveOption(t, item);
                break;
            case Slot.FEET:
                DisplayBootsOption(t, item);
                break;
            case Slot.CLOAK:
                DisplayCloakOption(t, item);
                break;
            case Slot.RIGHTHAND:
            case Slot.LEFTHAND:
               List<SkinVendorItem> foundItems3 = VendorItemList.FindAll(x => (x.ItemType == item.Item_Type || x.ItemType == Slot.LEFTHAND) 
               && (x.Realm == playerRealm || x.Realm == noneRealm)
               && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
               && x.PlayerRealmRank <= playerRealmRank
               && x.AccountRealmRank <= accountRealmRank
               && x.Orbs <= playerOrbs
               && x.Drake <= playerDragonKills
               && x.DamageType == damageType
               && x.ObjectType == item.Object_Type );

                foreach (var sItem in foundItems3)
                {
                    sb.Append($"[{sItem.Name}] ({sItem.Price} {currencyName})\n");
                }


                Console.WriteLine("item type is " + item.Item_Type + " realm is " + playerRealm + "realmrank is " + playerRealmRank + " orbs is " + playerOrbs + " drake is " + playerDragonKills + " damageType is" + damageType + " objectType is " + item.Object_Type);

                SendReply(t, sb.ToString());
                break;


               // if (item.Object_Type != (int)eObjectType.Shield)
                //    DisplayOneHandWeaponOption(t, item);
               // else
               //     DisplayShieldOption(t, item);
               // break;
            case Slot.TWOHAND:

                List<SkinVendorItem> foundItems1 = VendorItemList.FindAll(x => x.ItemType == item.Item_Type
                && (x.Realm == playerRealm || x.Realm == noneRealm)
                && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
                && x.PlayerRealmRank <= playerRealmRank
                && x.AccountRealmRank <= accountRealmRank
                && x.Orbs <= playerOrbs
                && x.Drake <= playerDragonKills
                && x.DamageType == damageType
                && x.ObjectType == item.Object_Type);

                foreach (var sItem in foundItems1)
                {
                    sb.Append($"[{sItem.Name}] ({sItem.Price} {currencyName})\n");
                }


                Console.WriteLine("item type is " + item.Item_Type + " realm is " + playerRealm + "realmrank is " + playerRealmRank + " orbs is " + playerOrbs + " drake is " + playerDragonKills + " damageType is" + damageType + " objectType is " + item.Object_Type);

                SendReply(t, sb.ToString());
                break;
        }

        SendReply(t, "When you are finished browsing, let me know and I will [confirm model]."
        );
        var tmp = (InventoryItem)item.Clone();
        t.TempProperties.setProperty(TempProperty, item);
        t.TempProperties.setProperty(DisplayedItem, tmp);

        return false;
    }

    private void DisplayHelmOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        //add all basic options
        sb.Append($"Free\n" +
                  $"[Crafted Helm 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Helm 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Helm 3] ({freebie} {currencyName})\n");


        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted Helm 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted Helm 5] ({lowbie} {currencyName})\n");
        }

        if (RR > 3)
        {
            sb.Append("Realm Rank 4+\n" +
                      "[Oceanus Helm] (" + toageneric + " " + currencyName + ")\n" +
                      "[Stygia Helm] (" + toageneric + " " + currencyName + ")\n" +
                      "[Volcanus Helm] (" + toageneric + " " + currencyName + ")\n" +
                      "[Aerus Helm] (" + toageneric + " " + currencyName + ")\n");
        }

        if (RR > 5)
        {
            sb.Append("Realm Rank 6+\n" +
                      "[Crown of Zahur] (" + artifact + " " + currencyName + ")\n" +
                      "[Crown of Zahur variant] (" + artifact + " " + currencyName + ")\n" +
                      "[Winged Helm] (" + artifact + " " + currencyName + ")\n");
        }

        int dragon = player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);

        if (dragon > 9)
        {
            sb.Append($"10 Dragon Kills\n" +
                      $"[Dragonsworn Helm] (" + dragonCost + " " + currencyName + ") | Catacombs Models Only\n");
        }

        if (dragon > 24)
        {
            sb.Append($"25 Dragon Kills\n" +
                      $"[Dragonslayer Helm] (" + dragonCost * 2 + " " + currencyName + ") | Catacombs Models Only\n");
        }

        sb.Append("\nAdditionally, I have some realm specific headgear available: \n");
        switch (player.Realm)
        {
            case eRealm.Albion:
                sb.Append("[Robin Hood Hat] (" + festive + " " + currencyName + ")\n" +
                          "[Tarboosh] (" + festive + " " + currencyName + ")\n" +
                          "[Jester Hat] (" + festive + " " + currencyName + ")\n" +
                          "");
                break;
            case eRealm.Hibernia:
                sb.Append("[Robin Hood Hat] (" + festive + " " + currencyName + ")\n" +
                          "[Leaf Hat] (" + festive + " " + currencyName + ")\n" +
                          "[Stag Helm] (" + festive + " " + currencyName + ")\n" +
                          "");
                break;
            case eRealm.Midgard:
                sb.Append("[Fur Cap] (" + festive + " " + currencyName + ")\n" +
                          "[Wing Hat] (" + festive + " " + currencyName + ")\n" +
                          "[Wolf Helm] (" + festive + " " + currencyName + ")\n" +
                          "");
                break;
        }

        if (item.Object_Type == (int)eObjectType.Cloth)
            sb.Append("[Wizard Hat] (" + epic + " " + currencyName + ")\n");

        SendReply(player, sb.ToString());
    }

    public void DisplayTorsoOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        //add all basic options
        sb.Append($"Free\n" +
                  $"[Crafted Torso 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Torso 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Torso 3] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted Torso 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted Torso 5] ({lowbie} {currencyName})\n");
        }

        if (RR > 3)
        {
            sb.Append("Realm Rank 4+\n" +
                      "[Oceanus Breastplate] (" + toageneric + " " + currencyName + ")\n" +
                      "[Stygia Breastplate] (" + toageneric + " " + currencyName + ")\n" +
                      "[Volcanus Breastplate] (" + toageneric + " " + currencyName + ")\n" +
                      "[Aerus Breastplate] (" + toageneric + " " + currencyName + ")\n");
        }

        if (RR > 4)
        {
            sb.Append("Realm Rank 5+\n" +
                      "[Class Epic Chestpiece](" + epic + " " + currencyName + ")\n");
        }

        if (RR > 5)
        {
            sb.Append("Realm Rank 6+\n" +
                      "[Eirene's Chest](" + artifact + " " + currencyName + ")\n" +
                      "[Naliah's Robe](" + artifact + " " + currencyName + ")\n" +
                      "[Guard of Valor](" + artifact + " " + currencyName + ")\n" +
                      "[Golden Scarab Vest](" + artifact + " " + currencyName + ")\n");
        }

        int dragon = player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);

        if (dragon > 9)
        {
            sb.Append($"10 Dragon Kills\n" +
                      $"[Dragonsworn Breastplate] (" + dragonCost + " " + currencyName + ") | Catacombs Models Only\n");
        }

        if (dragon > 24)
        {
            sb.Append($"25 Dragon Kills\n" +
                      $"[Dragonslayer Breastplate] (" + dragonCost * 1.5 + " " + currencyName +
                      ") | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills) >= 10)
        {
            sb.Append("10 Epic Boss Kills\n" +
                      "[Possessed Realm Breastplate](" + festive + " " + currencyName +
                      ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts) >= 3)
        {
            sb.Append("3 Crafts Above 1000\n" +
                      "[Good Realm Breastplate](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) > 100000)
        {
            sb.Append("100k Orbs Earned\n" +
                      "[Good Shar Breastplate](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) > 250000)
        {
            sb.Append("250k Orbs Earned\n" +
                      "[Good Inconnu Breastplate](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        sb.Append("\nI can also offer you some [armor pad] (" + armorpads + " " + currencyName + ") options.");

        SendReply(player, sb.ToString());
    }

    public void DisplayArmsOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        //add all basic options
        sb.Append($"Free\n" +
                  $"[Crafted Sleeves 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Sleeves 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Sleeves 3] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted Sleeves 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted Sleeves 5] ({lowbie} {currencyName})\n");
        }

        if (RR > 3)
        {
            sb.Append("Realm Rank 4+\n" +
                      "[Oceanus Sleeves] (" + toageneric + " " + currencyName + ")\n" +
                      "[Stygia Sleeves] (" + toageneric + " " + currencyName + ")\n" +
                      "[Volcanus Sleeves] (" + toageneric + " " + currencyName + ")\n" +
                      "[Aerus Sleeves] (" + toageneric + " " + currencyName + ")\n");
        }

        if (RR > 4)
        {
            sb.Append("Realm Rank 5+\n" +
                      "[Class Epic Sleeves](" + epic + " " + currencyName + ")\n");
        }

        if (RR > 5)
        {
            sb.Append("Realm Rank 6+\n" +
                      "[Foppish Sleeves] (" + artifact + " " + currencyName + ")\n" +
                      "[Arms of the Wind] (" + artifact + " " + currencyName + ")\n");
        }

        int dragon = player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);

        if (dragon > 4)
        {
            sb.Append($"5 Dragon Kills\n" +
                      $"[Dragonsworn Sleeves] (" + dragonCost + " " + currencyName + ") | Catacombs Models Only\n");
        }

        if (dragon > 24)
        {
            sb.Append($"25 Dragon Kills\n" +
                      $"[Dragonslayer Sleeves] (" + dragonCost * 1.5 + " " + currencyName +
                      ") | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills) >= 5)
        {
            sb.Append("5 Epic Boss Kills\n" +
                      "[Possessed Realm Sleeves](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts) >= 2)
        {
            sb.Append("2 Crafts Above 1000\n" +
                      "[Good Realm Sleeves](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) > 100000)
        {
            sb.Append("100k Orbs Earned\n" +
                      "[Good Shar Sleeves](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) > 250000)
        {
            sb.Append("250k Orbs Earned\n" +
                      "[Good Inconnu Sleeves](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        SendReply(player, sb.ToString());
    }

    public void DisplayPantsOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        //add all basic options
        sb.Append($"Free\n" +
                  $"[Crafted Pants 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Pants 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Pants 3] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted Pants 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted Pants 5] ({lowbie} {currencyName})\n");
        }

        if (RR > 3)
        {
            sb.Append("Realm Rank 4+\n" +
                      "[Oceanus Pants] (" + toageneric + " " + currencyName + ")\n" +
                      "[Stygia Pants] (" + toageneric + " " + currencyName + ")\n" +
                      "[Volcanus Pants] (" + toageneric + " " + currencyName + ")\n" +
                      "[Aerus Pants] (" + toageneric + " " + currencyName + ")\n");
        }

        if (RR > 4)
        {
            sb.Append("Realm Rank 5+\n" +
                      "[Class Epic Pants](" + epic + " " + currencyName + ")\n");
        }

        if (RR > 5)
        {
            sb.Append("Realm Rank 6+\n" +
                      "[Wings Dive] (" + artifact + " " + currencyName + ")\n" +
                      "[Alvarus' Leggings] (" + artifact + " " + currencyName + ")\n");
        }

        int dragon = player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);

        if (dragon > 4)
        {
            sb.Append($"5 Dragon Kills\n" +
                      $"[Dragonsworn Pants] (" + dragonCost + " " + currencyName + ") | Catacombs Models Only\n");
        }

        if (dragon > 24)
        {
            sb.Append($"25 Dragon Kills\n" +
                      $"[Dragonslayer Pants] (" + dragonCost * 1.5 + " " + currencyName +
                      ") | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills) >= 5)
        {
            sb.Append("5 Epic Boss Kills\n" +
                      "[Possessed Realm Pants](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts) >= 2)
        {
            sb.Append("2 Crafts Above 1000\n" +
                      "[Good Realm Pants](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) > 100000)
        {
            sb.Append("100k Orbs Earned\n" +
                      "[Good Shar Pants](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) > 250000)
        {
            sb.Append("250k Orbs Earned\n" +
                      "[Good Inconnu Pants](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        SendReply(player, sb.ToString());
    }

    public void DisplayGloveOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        //add all basic options
        sb.Append($"Free\n" +
                  $"[Crafted Gloves 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Gloves 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Gloves 3] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted Gloves 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted Gloves 5] ({lowbie} {currencyName})\n");
        }

        if (RR > 3)
        {
            sb.Append("Realm Rank 4+\n" +
                      "[Oceanus Gloves] (" + toageneric + " " + currencyName + ")\n" +
                      "[Stygia Gloves] (" + toageneric + " " + currencyName + ")\n" +
                      "[Volcanus Gloves] (" + toageneric + " " + currencyName + ")\n" +
                      "[Aerus Gloves] (" + toageneric + " " + currencyName + ")\n");
        }

        if (RR > 4)
        {
            sb.Append("Realm Rank 5+\n" +
                      "[Class Epic Gloves](" + epic + " " + currencyName + ")\n");
        }

        if (RR > 5)
        {
            sb.Append("Realm Rank 6+\n" +
                      "[Maddening Scalars] (" + artifact + " " + currencyName + ")\n" +
                      "[Sharkskin Gloves] (" + artifact + " " + currencyName + ")\n");
        }

        int dragon = player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);

        if (dragon > 0)
        {
            sb.Append($"1 Dragon Kill\n" +
                      $"[Dragonsworn Gloves] (" + dragonCost + " " + currencyName + ") | Catacombs Models Only\n");
        }

        if (dragon > 24)
        {
            sb.Append($"25 Dragon Kills\n" +
                      $"[Dragonslayer Gloves] (" + dragonCost * 1.5 + " " + currencyName +
                      ") | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills) >= 1)
        {
            sb.Append("1 Epic Boss Kill\n" +
                      "[Possessed Realm Gloves](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts) >= 1)
        {
            sb.Append("1 Craft Above 1000\n" +
                      "[Good Realm Gloves](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) > 100000)
        {
            sb.Append("100k Orbs Earned\n" +
                      "[Good Shar Gloves](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) > 250000)
        {
            sb.Append("250k Orbs Earned\n" +
                      "[Good Inconnu Gloves](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        sb.Append("\nI can also offer you some [armor pad] (" + armorpads + " " + currencyName + ") options.");

        SendReply(player, sb.ToString());
    }

    public void DisplayBootsOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        //add all basic options
        sb.Append($"Free\n" +
                  $"[Crafted Boots 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Boots 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted Boots 3] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted Boots 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted Boots 5] ({lowbie} {currencyName})\n");
        }

        if (RR > 3)
        {
            sb.Append("Realm Rank 4+\n" +
                      "[Oceanus Boots] (" + toageneric + " " + currencyName + ")\n" +
                      "[Stygia Boots] (" + toageneric + " " + currencyName + ")\n" +
                      "[Volcanus Boots] (" + toageneric + " " + currencyName + ")\n" +
                      "[Aerus Boots] (" + toageneric + " " + currencyName + ")\n");
        }

        if (RR > 4)
        {
            sb.Append("Realm Rank 5+\n" +
                      "[Class Epic Boots](" + epic + " " + currencyName + ")\n");
        }

        if (RR > 5)
        {
            sb.Append("Realm Rank 6+\n" +
                      "[Enyalio's Boots] (" + artifact + " " + currencyName + ")\n" +
                      "[Flamedancer's Boots] (" + artifact + " " + currencyName + ")\n");
        }

        int dragon = player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);

        if (dragon > 0)
        {
            sb.Append($"1 Dragon Kill\n" +
                      $"[Dragonsworn Boots] (" + dragonCost + " " + currencyName + ") | Catacombs Models Only\n");
        }

        if (dragon > 24)
        {
            sb.Append($"25 Dragon Kills\n" +
                      $"[Dragonslayer Boots] (" + dragonCost * 1.5 + " " + currencyName +
                      ") | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills) >= 1)
        {
            sb.Append("1 Epic Boss Kill\n" +
                      "[Possessed Realm Boots](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts) >= 1)
        {
            sb.Append("1 Craft Above 1000\n" +
                      "[Good Realm Boots](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) > 100000)
        {
            sb.Append("100k Orbs Earned\n" +
                      "[Good Shar Boots](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) > 250000)
        {
            sb.Append("250k Orbs Earned\n" +
                      "[Good Inconnu Boots](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
        }

        sb.Append("\nI can also offer you some [armor pad] (" + armorpads + " " + currencyName + ") options.");

        SendReply(player, sb.ToString());
    }

    public void DisplayCloakOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        sb.Append($"Free\n" +
                  $"[Basic Cloak] ({freebie} {currencyName})\n");
        sb.Append($"[Hooded Cloak] ({freebie} {currencyName})\n");
        sb.Append($"[Bordered Cloak] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2\n" +
                      $"[Mage Cloak 2] ({lowbie} {currencyName})\n");
            sb.Append($"[Mage Cloak 3] ({lowbie} {currencyName})\n");
            sb.Append($"[Embossed Cloak] ({lowbie} {currencyName})\n");
            sb.Append($"[Collared Cloak] ({lowbie} {currencyName})\n");
        }

        if (RR > 3)
        {
            sb.Append("Realm Rank 4\n" +
                      "[Oceanus Cloak] (" + cloakmedium + " " + currencyName + ")\n" +
                      "[Magma Cloak] (" + cloakmedium + " " + currencyName + ")\n" +
                      "[Stygian Cloak] (" + cloakmedium + " " + currencyName + ")\n");
        }

        if (RR > 4)
        {
            sb.Append($"Realm Rank 5\n" +
                      "[Realm Cloak] (" + cloakexpensive + " " + currencyName + ")\n");
        }

        if (RR > 5)
        {
            sb.Append("Realm Rank 6\n" +
                      "[Cloudsong] (" + cloakmedium + " " + currencyName + ")\n" +
                      "[Shades of Mist] (" + cloakmedium + " " + currencyName + ")\n" +
                      "[Harpy Feather Cloak] (" + cloakmedium + " " + currencyName + ")\n" +
                      "[Feathered Cloak] (" + cloakmedium + " " + currencyName + ")\n" +
                      "[Healer's Embrace] (" + cloakmedium + " " + currencyName + ")\n");
        }

        int dragon = player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);

        if (dragon > 9)
        {
            sb.Append($"10 Dragon Kills\n" +
                      "[Dragonsworn Cloak] (" + cloakmedium + " " + currencyName + ")\n");
        }

        if (dragon > 24)
        {
            sb.Append($"25 Dragon Kills\n" +
                      "[Dragonslayer Cloak] (" + cloakexpensive + " " + currencyName + ")\n");
        }

        SendReply(player, sb.ToString());
    }

    public void DisplayOneHandWeaponOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        sb.Append($"Free\n");
        if ((eObjectType)item.Object_Type == eObjectType.HandToHand)
        {
            sb.Append("[Bladed Claw Greave](" + freebie + " " + currencyName + ")\n" +
                      "[Bladed Fang Greave](" + freebie + " " + currencyName + ")\n" +
                      "[Bladed Moon Claw](" + freebie + " " + currencyName + ")\n" +
                      "[Bladed Moon Fang](" + freebie + " " + currencyName + ")\n" +
                      "[Claw Greave](" + freebie + " " + currencyName + ")\n" +
                      "[Fang Greave](" + freebie + " " + currencyName + ")\n" +
                      "");
        }
        else if ((eObjectType)item.Object_Type == eObjectType.Flexible)
        {
            sb.Append("[Flex Chain](" + freebie + " " + currencyName + ")\n" +
                      "[Flex Whip](" + freebie + " " + currencyName + ")\n" +
                      "[Flex Flail](" + freebie + " " + currencyName + ")\n" +
                      "[Flex Dagger Flail](" + freebie + " " + currencyName + ")\n" +
                      "[Flex Morning Star](" + freebie + " " + currencyName + ")\n" +
                      "[Flex Pick Flail](" + freebie + " " + currencyName + ")\n" +
                      "");
        }
        else if ((eObjectType)item.Object_Type == eObjectType.Scythe)
        {
            sb.Append("[Firbolg Scythe](" + freebie + " " + currencyName + ")\n" +
                      "[Harvest Scythe](" + freebie + " " + currencyName + ")\n" +
                      "[War Scythe](" + freebie + " " + currencyName + ")\n" +
                      "");
        }
        else
        {
            switch ((eDamageType)item.Type_Damage)
            {
                case eDamageType.Thrust:
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            sb.Append("[Dirk 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Rapier 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Main Gauche 1h](" + lowbie + " " + currencyName + ")\n");
                            break;
                        case eRealm.Hibernia:
                            sb.Append("[Celtic Dirk 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Celtic Rapier 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Celtic Stiletto 1h](" + lowbie + " " + currencyName + ")\n");
                            break;
                        case eRealm.Midgard:
                            break;
                    }

                    break;

                case eDamageType.Crush:
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            sb.Append("[Club 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Hammer 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Mace 1h](" + lowbie + " " + currencyName + ")\n");
                            break;
                        case eRealm.Hibernia:
                            sb.Append("[Celtic Club 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Celtic Mace 1h](" + lowbie + " " + currencyName + ")\n");
                            break;
                        case eRealm.Midgard:
                            sb.Append("[Small Hammer 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Pick Hammer 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Norse Hammer 1h](" + lowbie + " " + currencyName + ")\n");
                            break;
                    }

                    break;

                case eDamageType.Slash:
                    switch (player.Realm)
                    {
                        case eRealm.Albion:
                            sb.Append("[Short Sword 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Longsword 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Handaxe 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Scimitar 1h](" + lowbie + " " + currencyName + ")\n");
                            break;
                        case eRealm.Hibernia:
                            sb.Append("[Celtic Short Sword 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Celtic Longsword 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Celtic Broadsword 1h](" + lowbie + " " + currencyName + ")\n");
                            break;
                        case eRealm.Midgard:
                            sb.Append("[Norse Short Sword 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Norse Longsword 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Norse Broadsword 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "[Norse Spiked Axe 1h](" + lowbie + " " + currencyName + ")\n" +
                                      "");
                            break;
                    }

                    break;
            }
        }

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2\n");
            if ((eObjectType)item.Object_Type == eObjectType.HandToHand)
            {
                sb.Append("[Great Bladed Claw Greave](" + lowbie + " " + currencyName + ")\n" +
                          "[Great Bladed Fang Greave](" + lowbie + " " + currencyName + ")\n" +
                          "[Great Bladed Moon Claw](" + lowbie + " " + currencyName + ")\n" +
                          "[Great Bladed Moon Fang](" + lowbie + " " + currencyName + ")\n" +
                          "[Great Claw Greave](" + lowbie + " " + currencyName + ")\n" +
                          "[Great Fang Greave](" + lowbie + " " + currencyName + ")\n" +
                          "");
            }
            else if ((eObjectType)item.Object_Type == eObjectType.Flexible)
            {
                sb.Append("[Flex Spiked Flail](" + freebie + " " + currencyName + ")\n" +
                          "[Flex Spiked Whip](" + freebie + " " + currencyName + ")\n" +
                          "[Flex War Chain](" + freebie + " " + currencyName + ")\n" +
                          "[Flex Whip Dagger](" + freebie + " " + currencyName + ")\n" +
                          "[Flex Whip Mace](" + freebie + " " + currencyName + ")\n" +
                          "");
            }
            else if ((eObjectType)item.Object_Type == eObjectType.Scythe)
            {
                sb.Append("[Firbolg Great Scythe](" + freebie + " " + currencyName + ")\n" +
                          "[Great War Scythe](" + freebie + " " + currencyName + ")\n" +
                          "[Martial Scythe](" + freebie + " " + currencyName + ")\n" +
                          "");
            }
            else
            {
                switch ((eDamageType)item.Type_Damage)
                {
                    case eDamageType.Thrust:
                        switch (player.Realm)
                        {
                            case eRealm.Albion:
                                sb.Append("[Toothpick 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Highlander Dirk 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Foil 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Guarded Rapier 1h](" + lowbie + " " + currencyName + ")\n");
                                break;
                            case eRealm.Hibernia:
                                sb.Append("[Curved Dagger 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Celtic Guarded Rapier 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Lurikeen Dagger 1h](" + lowbie + " " + currencyName + ")\n");
                                break;
                            case eRealm.Midgard:
                                break;
                        }

                        break;

                    case eDamageType.Crush:
                        switch (player.Realm)
                        {
                            case eRealm.Albion:
                                sb.Append("[Spiked Mace 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[War Hammer 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Flanged Mace 1h](" + lowbie + " " + currencyName + ")\n");
                                break;
                            case eRealm.Hibernia:
                                sb.Append("[Spiked Club 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Celtic Spiked Mace 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Celtic Hammer 1h](" + lowbie + " " + currencyName + ")\n");
                                break;
                            case eRealm.Midgard:
                                sb.Append("[Great Hammer 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Pick Hammer 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Spiked Hammer 1h](" + lowbie + " " + currencyName + ")\n");
                                break;
                        }

                        break;

                    case eDamageType.Slash:
                        switch (player.Realm)
                        {
                            case eRealm.Albion:
                                sb.Append("[Jambiya 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Cinquedea 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Falchion 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Coffin Axe 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "");
                                break;
                            case eRealm.Hibernia:
                                sb.Append("[Celtic Sickle 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Celtic Hooked Sword 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Falcata 1h](" + lowbie + " " + currencyName + ")\n");
                                break;
                            case eRealm.Midgard:
                                sb.Append("[Dwarven Short Sword 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Norse Cleaver 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Double Axe 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "[Norse Bearded Axe 1h](" + lowbie + " " + currencyName + ")\n" +
                                          "");
                                break;
                        }

                        break;
                }
            }
        }

        if (RR > 3)
        {
            sb.Append("Realm Rank 4\n");
            if ((eObjectType)item.Object_Type == eObjectType.Scythe)
            {
                sb.Append("[Magma Scythe](" + toageneric + " " + currencyName + ")\n" +
                          "");
            }
            else if ((eObjectType)item.Object_Type != eObjectType.HandToHand &&
                     (eObjectType)item.Object_Type != eObjectType.Flexible)
            {
                switch ((eDamageType)item.Type_Damage)
                {
                    case eDamageType.Thrust:
                        switch (player.Realm)
                        {
                            case eRealm.Albion:
                                sb.Append("[Duelists Dagger 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Duelists Rapier 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Parrying Dagger 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Parrying Rapier 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "");
                                break;
                            case eRealm.Hibernia:
                                sb.Append("[Elven Dagger 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Firbolg Dagger 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Leaf Dagger 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Adze 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Barbed Adze 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Firbolg Adze 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[War Adze 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "");
                                break;
                            case eRealm.Midgard:
                                break;
                        }

                        sb.Append($"[Wakazashi 1h]({toageneric} {currencyName})");

                        break;

                    case eDamageType.Crush:
                        switch (player.Realm)
                        {
                            case eRealm.Albion:
                                sb.Append("[Bishops Mace 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Coffin Hammer Mace 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Coffin Mace 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "");
                                break;
                            case eRealm.Hibernia:
                                sb.Append("[Celt Hammer 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Celtic Mace 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Dire Mace 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Firbolg Hammer 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "");
                                break;
                            case eRealm.Midgard:
                                sb.Append("[Troll Hammer 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Troll War Hammer 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Kobold Sap 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Kobold War Club 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "");
                                break;
                        }
                        sb.Append("[Magma Hammer 1h](" + toageneric + " " + currencyName + ")\n" +
                                  "[Aerus Hammer 1h](" + toageneric + " " + currencyName + ")\n");

                        break;

                    case eDamageType.Slash:
                        switch (player.Realm)
                        {
                            case eRealm.Albion:
                                sb.Append("[Coffin Axe 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "");
                                sb.Append("[Khopesh 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Aerus Sword 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Magma Axe 1h](" + toageneric + " " + currencyName + ")\n");
                                break;
                            case eRealm.Hibernia:
                                sb.Append("[Elven Short Sword 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Elven Longsword 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Firbolg Short Sword 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Firbolg Longsword 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Leaf Short Sword 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Leaf Longsword 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "");
                                sb.Append("[Khopesh 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Aerus Sword 1h](" + toageneric + " " + currencyName + ")\n" +
                                          "[Magma Axe 1h](" + toageneric + " " + currencyName + ")\n");
                                break;
                            case eRealm.Midgard:
                                if (item.Object_Type == (int)eObjectType.Sword)
                                {
                                    sb.Append("[Troll Dagger 1h](" + toageneric + " " + currencyName + ")\n" +
                                              "[Troll Short Sword 1h](" + toageneric + " " + currencyName + ")\n" +
                                              "[Troll Long Sword 1h](" + toageneric + " " + currencyName + ")\n" +
                                              "[Kobold Dagger 1h](" + toageneric + " " + currencyName + ")\n" +
                                              "[Kobold Short Sword 1h](" + toageneric + " " + currencyName + ")\n" +
                                              "[Kobold Long Sword 1h](" + toageneric + " " + currencyName + ")\n");
                                    sb.Append("[Khopesh 1h](" + toageneric + " " + currencyName + ")\n" +
                                              "[Aerus Sword 1h](" + toageneric + " " + currencyName + ")\n");
                                }

                                if (item.Object_Type == (int)eObjectType.Axe ||
                                    item.Object_Type == (int)eObjectType.LeftAxe)
                                {
                                    sb.Append("[Troll Hand Axe 1h](" + toageneric + " " + currencyName + ")\n" +
                                              "[Troll War Axe 1h](" + toageneric + " " + currencyName + ")\n" +
                                              "[Kobold Hand Axe 1h](" + toageneric + " " + currencyName + ")\n" +
                                              "[Kobold War Axe 1h](" + toageneric + " " + currencyName + ")\n" +
                                              "");
                                    sb.Append("[Magma Axe 1h](" + toageneric + " " + currencyName + ")\n");
                                }

                                break;
                        }

                        sb.Append("[Khopesh 1h](" + toageneric + " " + currencyName + ")\n" +
                                  "[Aerus Sword 1h](" + toageneric + " " + currencyName + ")\n" +
                                  "[Magma Axe 1h](" + toageneric + " " + currencyName + ")\n");

                        break;
                }
            }
        }

        if (RR > 5)
        {
            sb.Append("Realm Rank 6\n");
            if ((eObjectType)item.Object_Type == eObjectType.HandToHand)
            {
                sb.Append("[Snakecharmer's Fist](" + artifact + " " + currencyName + ")\n");
            }
            else if ((eObjectType)item.Object_Type == eObjectType.Flexible)
            {
                sb.Append("[Snakecharmer's Whip](" + artifact + " " + currencyName + ")\n");
            }
            else
            {
                switch ((eDamageType)item.Type_Damage)
                {
                    case eDamageType.Thrust:
                        sb.Append("[Traitor's Dagger 1h](" + artifact + " " + currencyName + ")\n" +
                                  "[Croc Tooth Dagger 1h](" + artifact + " " + currencyName + ")\n" +
                                  "[Golden Spear 1h](" + artifact + " " + currencyName + ")\n");
                        break;

                    case eDamageType.Crush:
                        sb.Append("[Battler Hammer 1h](" + artifact + " " + currencyName + ")\n" +
                                  "[Malice Hammer 1h](" + artifact + " " + currencyName + ")\n" +
                                  "[Bruiser Hammer 1h](" + artifact + " " + currencyName + ")\n" +
                                  "[Scepter of the Meritorious](" + artifact + " " + currencyName + ")\n");
                        break;

                    case eDamageType.Slash:
                        sb.Append("[Croc Tooth Axe 1h](" + artifact + " " + currencyName + ")\n" +
                                  "[Traitor's Axe 1h](" + artifact + " " + currencyName + ")\n" +
                                  "[Malice Axe 1h](" + artifact + " " + currencyName + ")\n" +
                                  "[Battler Sword 1h](" + artifact + " " + currencyName + ")\n");
                        break;
                }
            }
        }

        int dragon = player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);

        if (dragon > 9)
        {
            sb.Append($"10 Dragon Kills\n");
        }

        if (dragon > 24)
        {
            sb.Append($"25 Dragon Kills\n");
        }

        SendReply(player, sb.ToString());
    }


    public void DisplayStaffOptions(GamePlayer player, InventoryItem item)
    {
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);
        int dragon = player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);
        int orbs = player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned);


        StringBuilder sb = GetStaffOptions(player, item, RR, dragon, orbs);


    }

    public StringBuilder GetStaffOptions(GamePlayer player, InventoryItem item, int RR, int dragon, int orbs)
    {
        StringBuilder sb = new StringBuilder();

        switch (player.Realm)
        {
            case eRealm.Albion:
                sb.Append($"[Briton Mage Staff] ({lowbie} {currencyName})\n");
                break;
            case eRealm.Midgard:
                break;
            case eRealm.Hibernia:
                break;
            default:
                break;
        }

        return sb;

    }

    public void DisplayTwoHandWeaponOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);
        int dragon = player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);

        switch (player.Realm)
        {
            case eRealm.Albion:
                sb.Append($"Free\n" +
                          $"[Battle Axe 2h] ({freebie} {currencyName})\n");
                sb.Append($"[War Mattock 2h] ({freebie} {currencyName})\n");
                sb.Append($"[Albion War Axe 2h] ({freebie} {currencyName})\n");
                sb.Append($"[Albion Great Hammer 2h] ({freebie} {currencyName})\n");

                if (RR > 1)
                {
                    sb.Append($"Realm Rank 2+\n" +
                              $"[Briton Arch Mace 2h] ({lowbie} {currencyName})\n");
                    sb.Append($"[Briton Scimitar 2h] ({lowbie} {currencyName})\n");
                    sb.Append($"[Briton War Pick 2h] ({lowbie} {currencyName})\n");
                }

                if (RR > 3)
                {
                    sb.Append($"Realm Rank 4+\n" +
                              $"[Zweihander 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Claymore 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Great Mace 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Dire Hammer 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Dire Axe 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Great Mattock 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Great Scimitar 2h] ({toageneric} {currencyName})\n");
                }
                break;
            case eRealm.Midgard:
                sb.Append($"Free\n" +
                          $"[Norse Sword 2h] ({freebie} {currencyName})\n");
                sb.Append($"[Norse Great Axe 2h] ({freebie} {currencyName})\n");
                sb.Append($"[Norse Large Axe 2h] ({freebie} {currencyName})\n");
                sb.Append($"[Norse Hammer 2h] ({freebie} {currencyName})\n");

                if (RR > 1)
                {
                    sb.Append($"Realm Rank 2+\n" +
                              $"[Norse Greatsword 2h] ({lowbie} {currencyName})\n");
                    sb.Append($"[Norse Warhammer 2h] ({lowbie} {currencyName})\n");
                    sb.Append($"[Norse Greathammer 2h] ({lowbie} {currencyName})\n");
                    sb.Append($"[Norse Battleaxe 2h] ({lowbie} {currencyName})\n");
                }

                if (RR > 3)
                {
                    sb.Append($"Realm Rank 4+\n" +
                              $"[Dwarven Sword 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Dwarven Greataxe 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Dwarven Great Hammer 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[War Cleaver 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Spiked Hammer 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Troll Greatsword 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Kobold Greataxe 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Kobold Great Club 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Kobold Great Sword 2h] ({toageneric} {currencyName})\n");
                }
                break;
            case eRealm.Hibernia:
                sb.Append($"Free\n" +
                          $"[Celtic Greatsword 2h] ({freebie} {currencyName})\n");
                sb.Append($"[Celtic Sword 2h] ({freebie} {currencyName})\n");
                sb.Append($"[Celtic Great Hammer 2h] ({freebie} {currencyName})\n");
                sb.Append($"[Celtic Spiked Mace 2h] ({freebie} {currencyName})\n");
                sb.Append($"[Celtic Shillelagh 2h] ({freebie} {currencyName})\n");

                if (RR > 1)
                {
                    sb.Append($"Realm Rank 2+\n" +
                              $"[Celtic Falcata 2h] ({lowbie} {currencyName})\n");
                    sb.Append($"[Celtic Sledgehammer 2h] ({lowbie} {currencyName})\n");
                }

                if (RR > 3)
                {
                    sb.Append($"Realm Rank 4+\n" +
                              $"[Celtic Hammer 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Celtic Great Mace 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Celtic Dire Club 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Elven Greatsword 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Firbolg Hammer 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Firbolg Mace 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Firbolg Trollsplitter 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Leaf Point 2h] ({toageneric} {currencyName})\n");
                    sb.Append($"[Shod Shillelagh 2h] ({toageneric} {currencyName})\n");
                }
                break;
        }



        SendReply(player, sb.ToString());
    }

    public void DisplayShieldOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        switch (item.Type_Damage)
        {
            //small shields
            case 1:
                //add all basic options
                sb.Append($"Free\n" +
                          $"[Leather Buckler] ({freebie} {currencyName})\n");
                sb.Append($"[Metal Buckler] ({freebie} {currencyName})\n");
                sb.Append($"[Wood Buckler] ({freebie} {currencyName})\n");

                switch (player.Realm)
                {
                    case eRealm.Albion:
                        if (RR > 1)
                        {
                            sb.Append($"Realm Rank 2+\n" +
                                      $"[Leather Tri-Tip Buckler] ({lowbie} {currencyName})\n" +
                                      $"[Metal Tri-Tip Buckler] ({lowbie} {currencyName})\n" +
                                      $"[Wood Tri-Tip Buckler] ({lowbie} {currencyName})\n");
                        }

                        if (RR > 3)
                        {
                            sb.Append($"Realm Rank 4+\n" +
                                      $"[Leather Kite Buckler] ({toageneric} {currencyName})\n" +
                                      $"[Metal Kite Buckler] ({toageneric} {currencyName})\n" +
                                      $"[Wood Kite Buckler] ({toageneric} {currencyName})\n");
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) > 24)
                        {

                            sb.Append($"25 Dragon Kills\n" +
                                      $"[Albion Dragonslayer Buckler] (" + dragonCost * 1.5 + " " + currencyName + ")\n");

                        }
                        break;
                    case eRealm.Midgard:
                        if (RR > 1)
                        {
                            sb.Append($"Realm Rank 2+\n" +
                                      $"[Leather Norse Buckler] ({lowbie} {currencyName})\n" +
                                      $"[Metal Norse Buckler] ({lowbie} {currencyName})\n" +
                                      $"[Wood Norse Buckler] ({lowbie} {currencyName})\n");
                        }

                        if (RR > 3)
                        {
                            sb.Append($"Realm Rank 4+\n" +
                                      $"[Leather Grave Buckler] ({toageneric} {currencyName})\n" +
                                      $"[Metal Grave Buckler] ({toageneric} {currencyName})\n" +
                                      $"[Wood Grave Buckler] ({toageneric} {currencyName})\n");
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) > 24)
                        {

                            sb.Append($"25 Dragon Kills\n" +
                                      $"[Midgard Dragonslayer Buckler] (" + dragonCost * 1.5 + " " + currencyName + ")\n");

                        }
                        break;
                    case eRealm.Hibernia:
                        if (RR > 1)
                        {
                            sb.Append($"Realm Rank 2+\n" +
                                      $"[Leather Celtic Buckler] ({lowbie} {currencyName})\n" +
                                      $"[Metal Celtic Buckler] ({lowbie} {currencyName})\n" +
                                      $"[Wood Celtic Buckler] ({lowbie} {currencyName})\n");
                        }

                        if (RR > 3)
                        {
                            sb.Append($"Realm Rank 4+\n" +
                                      $"[Leather Leaf Buckler] ({toageneric} {currencyName})\n" +
                                      $"[Metal Leaf Buckler] ({toageneric} {currencyName})\n" +
                                      $"[Wood Leaf Buckler] ({toageneric} {currencyName})\n");
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) > 24)
                        {

                            sb.Append($"25 Dragon Kills\n" +
                                      $"[Hibernia Dragonslayer Buckler] (" + dragonCost * 1.5 + " " + currencyName + ")\n");

                        }
                        break;
                }

                if (RR > 3)
                {
                    sb.Append($"[Oceanus Small Shield] ({toageneric} {currencyName})\n");
                    sb.Append($"[Aerus Small Shield] ({toageneric} {currencyName})\n");
                    sb.Append($"[Magma Small Shield] ({toageneric} {currencyName})\n");
                    sb.Append($"[Stygia Small Shield] ({toageneric} {currencyName})\n");
                }
                break;
            case 2:
                //add all basic options
                sb.Append($"Free\n" +
                          $"[Leather Medium Heater] ({freebie} {currencyName})\n");
                sb.Append($"[Metal Medium Heater] ({freebie} {currencyName})\n");
                sb.Append($"[Wood Medium Heater] ({freebie} {currencyName})\n");

                sb.Append($"[Leather Medium Tower] ({freebie} {currencyName})\n");
                sb.Append($"[Metal Medium Tower] ({freebie} {currencyName})\n");
                sb.Append($"[Wood Medium Tower] ({freebie} {currencyName})\n");

                sb.Append($"[Leather Medium Round] ({freebie} {currencyName})\n");
                sb.Append($"[Metal Medium Round] ({freebie} {currencyName})\n");
                sb.Append($"[Wood Medium Round] ({freebie} {currencyName})\n");

                switch (player.Realm)
                {
                    case eRealm.Albion:
                        if (RR > 1)
                        {
                            sb.Append($"Realm Rank 2+\n" +
                                      $"[Leather Medium Horned] ({lowbie} {currencyName})\n" +
                                      $"[Metal Medium Horned] ({lowbie} {currencyName})\n" +
                                      $"[Wood Medium Horned] ({lowbie} {currencyName})\n");
                        }

                        if (RR > 3)
                        {
                            sb.Append($"Realm Rank 4+\n" +
                                      $"[Leather Medium Kite] ({toageneric} {currencyName})\n" +
                                      $"[Metal Medium Kite] ({toageneric} {currencyName})\n" +
                                      $"[Wood Medium Kite] ({toageneric} {currencyName})\n");
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) > 24)
                        {

                            sb.Append($"25 Dragon Kills\n" +
                                      $"[Albion Dragonslayer Medium] (" + dragonCost * 1.5 + " " + currencyName + ")\n");

                        }
                        break;
                    case eRealm.Midgard:
                        if (RR > 1)
                        {
                            sb.Append($"Realm Rank 2+\n" +
                                      $"[Leather Medium Crescent] ({lowbie} {currencyName})\n" +
                                      $"[Metal Medium Crescent] ({lowbie} {currencyName})\n" +
                                      $"[Wood Medium Crescent] ({lowbie} {currencyName})\n");
                        }

                        if (RR > 3)
                        {
                            sb.Append($"Realm Rank 4+\n" +
                                      $"[Leather Medium Grave] ({toageneric} {currencyName})\n" +
                                      $"[Metal Medium Grave] ({toageneric} {currencyName})\n" +
                                      $"[Wood Medium Grave] ({toageneric} {currencyName})\n");
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) > 24)
                        {

                            sb.Append($"25 Dragon Kills\n" +
                                      $"[Midgard Dragonslayer Medium] (" + dragonCost * 1.5 + " " + currencyName + ")\n");

                        }
                        break;
                    case eRealm.Hibernia:
                        if (RR > 1)
                        {
                            sb.Append($"Realm Rank 2+\n" +
                                      $"[Leather Medium Celtic] ({lowbie} {currencyName})\n" +
                                      $"[Metal Medium Celtic] ({lowbie} {currencyName})\n" +
                                      $"[Wood Medium Celtic] ({lowbie} {currencyName})\n");
                        }

                        if (RR > 3)
                        {
                            sb.Append($"Realm Rank 4+\n" +
                                      $"[Leather Medium Leaf] ({toageneric} {currencyName})\n" +
                                      $"[Metal Medium Leaf] ({toageneric} {currencyName})\n" +
                                      $"[Wood Medium Leaf] ({toageneric} {currencyName})\n");
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) > 24)
                        {

                            sb.Append($"25 Dragon Kills\n" +
                                      $"[Hibernia Dragonslayer Medium] (" + dragonCost * 1.5 + " " + currencyName + ")\n");

                        }
                        break;
                }

                if (RR > 3)
                {
                    sb.Append($"[Oceanus Medium Shield] ({toageneric} {currencyName})\n");
                    sb.Append($"[Aerus Medium Shield] ({toageneric} {currencyName})\n");
                    sb.Append($"[Magma Medium Shield] ({toageneric} {currencyName})\n");
                    sb.Append($"[Stygia Medium Shield] ({toageneric} {currencyName})\n");
                }

                break;
            case 3:
                //add all basic options
                sb.Append($"Free\n" +
                          $"[Leather Large Heater] ({freebie} {currencyName})\n");
                sb.Append($"[Metal Large Heater] ({freebie} {currencyName})\n");
                sb.Append($"[Wood Large Heater] ({freebie} {currencyName})\n");

                sb.Append($"[Leather Large Tower] ({freebie} {currencyName})\n");
                sb.Append($"[Metal Large Tower] ({freebie} {currencyName})\n");
                sb.Append($"[Wood Large Tower] ({freebie} {currencyName})\n");

                sb.Append($"[Leather Large Round] ({freebie} {currencyName})\n");
                sb.Append($"[Metal Large Round] ({freebie} {currencyName})\n");
                sb.Append($"[Wood Large Round] ({freebie} {currencyName})\n");

                switch (player.Realm)
                {
                    case eRealm.Albion:
                        if (RR > 1)
                        {
                            sb.Append($"Realm Rank 2+\n" +
                                      $"[Leather Large Horned] ({lowbie} {currencyName})\n" +
                                      $"[Metal Large Horned] ({lowbie} {currencyName})\n" +
                                      $"[Wood Large Horned] ({lowbie} {currencyName})\n");
                        }

                        if (RR > 3)
                        {
                            sb.Append($"Realm Rank 4+\n" +
                                      $"[Leather Large Kite] ({toageneric} {currencyName})\n" +
                                      $"[Metal Large Kite] ({toageneric} {currencyName})\n" +
                                      $"[Wood Large Kite] ({toageneric} {currencyName})\n");
                            sb.Append($"[Leather Studded Tower] ({toageneric} {currencyName})\n");
                            sb.Append($"[Metal Studded Tower] ({toageneric} {currencyName})\n");
                            sb.Append($"[Wood Studded Tower] ({toageneric} {currencyName})\n");
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) > 24)
                        {

                            sb.Append($"25 Dragon Kills\n" +
                                      $"[Albion Dragonslayer Large] (" + dragonCost * 1.5 + " " + currencyName + ")\n");

                        }
                        break;
                    case eRealm.Midgard:
                        if (RR > 1)
                        {
                            sb.Append($"Realm Rank 2+\n" +
                                      $"[Leather Large Crescent] ({lowbie} {currencyName})\n" +
                                      $"[Metal Large Crescent] ({lowbie} {currencyName})\n" +
                                      $"[Wood Large Crescent] ({lowbie} {currencyName})\n");
                        }

                        if (RR > 3)
                        {
                            sb.Append($"Realm Rank 4+\n" +
                                      $"[Leather Large Grave] ({toageneric} {currencyName})\n" +
                                      $"[Metal Large Grave] ({toageneric} {currencyName})\n" +
                                      $"[Wood Large Grave] ({toageneric} {currencyName})\n");
                            sb.Append($"[Leather Norse Tower] ({toageneric} {currencyName})\n");
                            sb.Append($"[Metal Norse Tower] ({toageneric} {currencyName})\n");
                            sb.Append($"[Wood Norse Tower] ({toageneric} {currencyName})\n");
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) > 24)
                        {

                            sb.Append($"25 Dragon Kills\n" +
                                      $"[Midgard Dragonslayer Large] (" + dragonCost * 1.5 + " " + currencyName + ")\n");

                        }
                        break;
                    case eRealm.Hibernia:
                        if (RR > 1)
                        {
                            sb.Append($"Realm Rank 2+\n" +
                                      $"[Leather Large Celtic] ({lowbie} {currencyName})\n" +
                                      $"[Metal Large Celtic] ({lowbie} {currencyName})\n" +
                                      $"[Wood Large Celtic] ({lowbie} {currencyName})\n");
                        }

                        if (RR > 3)
                        {
                            sb.Append($"Realm Rank 4+\n" +
                                      $"[Leather Large Leaf] ({toageneric} {currencyName})\n" +
                                      $"[Metal Large Leaf] ({toageneric} {currencyName})\n" +
                                      $"[Wood Large Leaf] ({toageneric} {currencyName})\n");
                            sb.Append($"[Leather Celtic Tower] ({toageneric} {currencyName})\n");
                            sb.Append($"[Metal Celtic Tower] ({toageneric} {currencyName})\n");
                            sb.Append($"[Wood Celtic Tower] ({toageneric} {currencyName})\n");
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) > 24)
                        {

                            sb.Append($"25 Dragon Kills\n" +
                                      $"[Hibernia Dragonslayer Large] (" + dragonCost * 1.5 + " " + currencyName + ")\n");

                        }
                        break;
                }

                if (RR > 3)
                {
                    sb.Append($"Realm Rank 4+\n" +
                              $"[Oceanus Large Shield] ({toageneric} {currencyName})\n");
                    sb.Append($"[Aerus Large Shield] ({toageneric} {currencyName})\n");
                    sb.Append($"[Magma Large Shield] ({toageneric} {currencyName})\n");
                    sb.Append($"[Stygia Large Shield] ({toageneric} {currencyName})\n");
                }

                break;

        }

        if (RR > 5)
        {
            sb.Append($"Realm Rank 6+\n" +
                      $"[Aten's Shield] ({toageneric} {currencyName})\n");
            sb.Append($"[Cyclop's Eye] ({toageneric} {currencyName})\n");
            sb.Append($"[Shield Of Khaos] ({toageneric} {currencyName})\n");
        }


        SendReply(player, sb.ToString());
    }


    public bool SetModel(GamePlayer player, int number, int price)
    {
        if (price > 0)
        {
            int playerOrbs = player.Inventory.CountItemTemplate("token_many", eInventorySlot.FirstBackpack,
                eInventorySlot.LastBackpack);
            log.Info("Player Orbs:" + playerOrbs);

            if (playerOrbs < price)
            {
                SendReply(player, "I'm sorry, but you cannot afford my services currently.");
                return false;
            }

            SendReply(player, "Thanks for your donation. " +
                              "I have changed your item's model, you can now use it. \n\n" +
                              "I look forward to doing business with you in the future.");

            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);
            InventoryItem displayItem = player.TempProperties.getProperty<InventoryItem>(DisplayedItem);

            if (item == null || item.OwnerID != player.InternalID || item.OwnerID == null)
                return false;

            player.TempProperties.removeProperty(TempProperty);
            player.TempProperties.removeProperty(DisplayedItem);
            player.TempProperties.removeProperty(TempModelID);

            //Console.WriteLine($"item model: {item.Model} assignment {number}");
            player.Inventory.RemoveItem(item);
            ItemUnique unique = new ItemUnique(item.Template);
            unique.Model = number;
            item.IsTradable = false;
            item.IsDropable = false;
            GameServer.Database.AddObject(unique);
            //Console.WriteLine($"unique model: {unique.Model} assignment {number}");
            InventoryItem newInventoryItem = GameInventoryItem.Create(unique as ItemTemplate);
            if (item.IsCrafted)
                newInventoryItem.IsCrafted = true;
            if (item.Creator != "")
                newInventoryItem.Creator = item.Creator;
            newInventoryItem.Count = 1;
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { newInventoryItem });
            // player.RemoveBountyPoints(300);
            //player.RealmPoints -= price;
            //player.RespecRealm();
            //SetRealmLevel(player, (int)player.RealmPoints);
            player.Inventory.RemoveTemplate("token_many", price, eInventorySlot.FirstBackpack,
                eInventorySlot.LastBackpack);

            player.SaveIntoDatabase();
            return true;
        }

        SendReply(player, "I'm sorry, I seem to have gotten confused. Please start over. \n" +
                          "If you repeatedly get this message, please file a bug ticket on how you recreate it.");
        return false;
    }

    public void SetExtension(GamePlayer player, byte number, int price)
    {
        if (price > 0)
        {
            int playerOrbs = player.Inventory.CountItemTemplate("token_many", eInventorySlot.FirstBackpack,
                eInventorySlot.LastBackpack);
            //log.Info("Player Orbs:" + playerOrbs);

            if (playerOrbs < price)
            {
                SendReply(player, "I'm sorry, but you cannot afford my services currently.");
                return;
            }

            InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);
            InventoryItem displayItem = player.TempProperties.getProperty<InventoryItem>(DisplayedItem);

            if (item == null || item.OwnerID != player.InternalID || item.OwnerID == null)
                return;

            player.TempProperties.removeProperty(TempProperty);
            player.TempProperties.removeProperty(DisplayedItem);

            //only allow pads on valid slots: torso/hand/feet
            if (item.Item_Type != (int)eEquipmentItems.TORSO && item.Item_Type != (int)eEquipmentItems.HAND &&
                item.Item_Type != (int)eEquipmentItems.FEET)
            {
                SendReply(player, "I'm sorry, but I can only modify the pads on Torso, Hand, and Feet armors.");
                return;
            }


            player.Inventory.RemoveItem(item);
            ItemUnique unique = new ItemUnique(item.Template);
            unique.Extension = number;
            GameServer.Database.AddObject(unique);
            InventoryItem newInventoryItem = GameInventoryItem.Create(unique as ItemTemplate);
            if (item.IsCrafted)
                newInventoryItem.IsCrafted = true;
            if (item.Creator != "")
                newInventoryItem.Creator = item.Creator;
            newInventoryItem.Count = 1;
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] { newInventoryItem });
            // player.RemoveBountyPoints(300);
            //player.RealmPoints -= price;
            //player.RespecRealm();
            //SetRealmLevel(player, (int)player.RealmPoints);
            player.Inventory.RemoveTemplate("token_many", price, eInventorySlot.FirstBackpack,
                eInventorySlot.LastBackpack);

            player.SaveIntoDatabase();

            SendReply(player, "Thanks for your donation. " +
                              "I have changed your item's extension, you can now use it. \n\n" +
                              "I look forward to doing business with you in the future.");

            return;
        }

        SendReply(player, "I'm sorry, I seem to have gotten confused. Please start over. \n" +
                          "If you repeatedly get this message, please file a bug ticket on how you recreate it.");
    }

    public void SendReply(GamePlayer player, string msg)
    {
        player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_PopupWindow);
    }

    private GameNPC CreateDisplayNPC(GamePlayer player, InventoryItem item)
    {
        var mob = new DisplayModel(player, item);

        //player model contains 5 bits of extra data that causes issues if used
        //for an NPC model. we do this to drop the first 5 bits and fill w/ 0s
        ushort tmpModel = (ushort)(player.Model << 5);
        tmpModel = (ushort)(tmpModel >> 5);

        //Fill the object variables
        mob.X = this.X + 50;
        mob.Y = this.Y;
        mob.Z = this.Z;
        mob.CurrentRegion = this.CurrentRegion;

        return mob;

        /*
        mob.Inventory = new GameNPCInventory(GameNpcInventoryTemplate.EmptyTemplate);
        //Console.WriteLine($"item: {item} slot: {item.Item_Type}");
        //mob.Inventory.AddItem((eInventorySlot) item.Item_Type, item);
        //Console.WriteLine($"mob inventory: {mob.Inventory.ToString()}");
        player.Out.SendNPCCreate(mob);
        //mob.AddToWorld();*/
    }

    private void DisplayReskinPreviewTo(GamePlayer player, InventoryItem item)
    {
        GameNPC display = CreateDisplayNPC(player, item);
        display.AddToWorld();

        var tempAd = new AttackData();
        tempAd.Attacker = display;
        tempAd.Target = display;
        if (item.Hand == 1)
        {
            tempAd.AttackType = AttackData.eAttackType.MeleeTwoHand;
            display.SwitchWeapon(eActiveWeaponSlot.TwoHanded);
        }
        else
        {
            tempAd.AttackType = AttackData.eAttackType.MeleeOneHand;
            display.SwitchWeapon(eActiveWeaponSlot.Standard);
        }

        tempAd.AttackResult = eAttackResult.HitUnstyled;
        display.AttackState = true;
        display.TargetObject = display;
        display.ObjectState = eObjectState.Active;
        display.attackComponent.AttackState = true;
        display.BroadcastLivingEquipmentUpdate();
        player.Out.SendObjectUpdate(display);

        //Uncomment this if you want animations
        // var animationThread = new Thread(() => LoopAnimation(player,item, display,tempAd));
        // animationThread.IsBackground = true;
        // animationThread.Start();
    }

    private void LoopAnimation(GamePlayer player, InventoryItem item, GameNPC display, AttackData ad)
    {
        var _lastAnimation = 0l;
        while (GameLoop.GameLoopTime < display.SpawnTick)
        {
            if (GameLoop.GameLoopTime - _lastAnimation > 2000)
            {
                _lastAnimation = GameLoop.GameLoopTime;
            }
        }
    }


    private void SendNotValidMessage(GamePlayer player)
    {
        SendReply(player, "This skin is not valid for this item type. Please try a different combo.");
    }

    private void SendNotQualifiedMessage(GamePlayer player)
    {
        SendReply(player, "You don't have the achievement required for that item. Please try a different combo.");
    }
    public override bool WhisperReceive(GameLiving source, string str)
    {
        /*
        if (!base.WhisperReceive(source, str)) return false;
        if (!(source is GamePlayer)) return false;
        GamePlayer player = (GamePlayer)source;
        TurnTo(player.X, player.Y);

        InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);

        if (item == null)
        {
            SendReply(player, "I need an item to work on!");
            return false;
        }
        int model = item.Model;
        int tmpmodel = int.Parse(str);
        if (tmpmodel != 0) model = tmpmodel;
        SetModel(player, model);
        SendReply(player, "I have changed your item's model, you can now use it.");
        */

        if (!base.WhisperReceive(source, str)) return false;
        if (!(source is GamePlayer)) return false;

        int modelIDToAssign = 0;
        int price = 0;

        GamePlayer player = source as GamePlayer;
        TurnTo(player.X, player.Y);
        InventoryItem item = player.TempProperties.getProperty<InventoryItem>(TempProperty);
        InventoryItem displayItem = player.TempProperties.getProperty<InventoryItem>(DisplayedItem);
        int cachedModelID = player.TempProperties.getProperty<int>(TempModelID);
        int cachedModelPrice = player.TempProperties.getProperty<int>(TempModelPrice);

        if (item == null)
        {
            SendReply(player, "I need an item to work on!");
            return false;
        }

        switch (str.ToLower())
        {
            case "confirm model":
                //Console.WriteLine($"Cached: {cachedModelID}");
                if (cachedModelID > 0 && cachedModelPrice > 0)
                {
                    if (cachedModelPrice == armorpads)
                        SetExtension(player, (byte)cachedModelID, cachedModelPrice);
                    else
                        SetModel(player, cachedModelID, cachedModelPrice);

                    return true;
                }
                else
                {
                    SendReply(player,
                        "I'm sorry, I seem to have lost track of the model you wanted. Please start over.");
                }

                break;

            #region crafted

            case "crafted helm 1":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 823;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 825;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 826;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 62;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 335;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 438;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 824;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 829;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 835;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 63;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 832;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 838;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 64;
                        break;
                }

                break;
            case "crafted helm 2":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1230;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1213;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1197;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 62;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 336;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 439;
                        break;
                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 824;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 830;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 836;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 63;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 833;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1202;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 93;
                        break;
                }

                break;
            case "crafted helm 3":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1230;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1213;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1197;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1231;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 337;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 440;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1234;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 831;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 837;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1236;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 834;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 840;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1238;
                        break;
                }

                break;
            case "crafted helm 4":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1230;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1213;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1197;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1231;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1214;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1198;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1234;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1215;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1199;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1236;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1216;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1200;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1239;
                        break;
                }

                break;
            case "crafted helm 5":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1230;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1213;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1197;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1232;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1214;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1198;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1235;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1215;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1199;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1236;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1216;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1200;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1240;
                        break;
                }

                break;

            case "crafted torso 1":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 139;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 245;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 378;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 31;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 240;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 373;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 51;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 230;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 363;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 41;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 235;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 388;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 46;
                        break;
                }

                break;
            case "crafted torso 2":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1230;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1213;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1197;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 36;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 260;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 393;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 81;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 250;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 383;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 186;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 275;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 408;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 86;
                        break;
                }

                break;
            case "crafted torso 3":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1230;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1213;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1197;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 74;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 280;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 413;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 156;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 270;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 403;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 196;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 295;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 428;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 201;
                        break;
                }

                break;
            case "crafted torso 4":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1230;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1213;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1197;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 134;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 300;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 433;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 216;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1215;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 423;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1246;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 999;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 988;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 206;
                        break;
                }

                break;
            case "crafted torso 5":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 1230;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1213;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1197;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 176;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 300;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 353;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 221;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1215;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1256;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1251;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1262;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1272;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 211;
                        break;
                }

                break;

            case "crafted sleeves 1":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 141;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 247;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 360;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 33;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 242;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 375;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 51;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 230;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 363;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 43;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 237;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 370;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 48;
                        break;
                }

                break;
            case "crafted sleeves 2":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 141;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 267;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 380;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 38;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 262;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 395;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 81;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 250;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 383;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 183;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 257;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 390;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 88;
                        break;
                }

                break;
            case "crafted sleeves 3":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 141;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 287;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 400;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 76;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 282;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 415;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 156;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 270;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 403;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 188;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 277;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 410;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 203;
                        break;
                }

                break;
            case "crafted sleeves 4":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 141;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 307;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 420;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 136;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 302;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 435;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 216;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1215;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 423;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1248;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1002;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 430;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 208;
                        break;
                }

                break;
            case "crafted sleeves 5":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 141;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 307;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 420;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 136;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 302;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 435;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 221;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1215;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1256;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1253;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1265;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1274;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 213;
                        break;
                }

                break;

            case "crafted pants 1":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 141;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 247;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 360;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 33;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 242;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 375;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 51;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 230;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 363;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 41;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 235;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 388;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 46;
                        break;
                }

                break;
            case "crafted pants 2":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 141;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 267;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 380;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 38;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 262;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 395;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 81;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 250;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 383;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 186;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 275;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 408;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 86;
                        break;
                }

                break;
            case "crafted pants 3":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 141;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 287;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 400;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 76;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 282;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 415;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 156;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 270;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 403;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 196;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 295;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 428;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 201;
                        break;
                }

                break;
            case "crafted pants 4":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 141;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 307;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 420;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 136;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 302;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 435;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 216;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1215;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 423;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1246;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 999;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 988;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 206;
                        break;
                }

                break;
            case "crafted pants 5":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 141;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 307;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 420;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 136;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 302;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 435;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 221;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 1215;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1256;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1251;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1262;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1272;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 211;
                        break;
                }

                break;

            case "crafted boots 1":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 143;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 249;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 362;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 40;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 150;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 377;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 54;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 234;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 367;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 45;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 239;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 372;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 50;
                        break;
                }

                break;
            case "crafted boots 2":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 143;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 269;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 382;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 78;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 244;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 397;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 84;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 254;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 387;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 185;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 259;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 392;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 90;
                        break;
                }

                break;
            case "crafted boots 3":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 143;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 289;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 422;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 133;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 264;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 417;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 160;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 274;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 407;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 190;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 279;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 412;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 205;
                        break;
                }

                break;
            case "crafted boots 4":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 143;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 309;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 342;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 138;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 284;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 437;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 220;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 294;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 427;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 195;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1001;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 432;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 210;
                        break;
                }

                break;
            case "crafted boots 5":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 143;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 987;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 342;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 136;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 302;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 437;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 225;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 225;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1260;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1250;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1264;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 352;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 215;
                        break;
                }

                break;

            case "crafted gloves 1":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 142;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 248;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 361;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 34;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 243;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 376;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 80;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 233;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 366;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 44;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 238;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 371;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 49;
                        break;
                }

                break;
            case "crafted gloves 2":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 142;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 268;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 381;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 39;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 263;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 396;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 85;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 253;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 386;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 184;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 258;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 391;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 89;
                        break;
                }

                break;
            case "crafted gloves 3":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 143;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 288;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 401;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 77;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 283;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 416;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 159;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 273;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 406;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 189;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 278;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 411;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 204;
                        break;
                }

                break;
            case "crafted gloves 4":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 142;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 308;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 341;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 138;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 303;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 436;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 219;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 293;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 426;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 194;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1000;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 431;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 209;
                        break;
                }

                break;
            case "crafted gloves 5":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = freebie;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 142;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 986;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 341;
                        break;

                    case eObjectType.Leather:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 179;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 179;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 436;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        if (player.Realm == eRealm.Albion) modelIDToAssign = 224;
                        if (player.Realm == eRealm.Midgard) modelIDToAssign = 224;
                        if (player.Realm == eRealm.Hibernia) modelIDToAssign = 1259;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1249;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1263;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 351;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 214;
                        break;
                }

                break;

            #endregion

            #region helms

            case "dragonslayer helm":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4056;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4070;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4063;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4054;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4068;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4061;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4055;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4069;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4062;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4057;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4072;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 4066;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 4053;
                        break;
                }

                break;
            case "dragonsworn helm":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3864;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3862;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3863;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 3866;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 3865;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 3867;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3861;
                        break;
                }

                break;
            case "crown of zahur":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1839;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1840;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1841;
                        break;
                    default:
                        modelIDToAssign = 0;
                        break;
                }

                break;
            case "crown of zahur variant":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1842;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1843;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1844;
                        break;
                    default:
                        modelIDToAssign = 0;
                        break;
                }

                break;
            case "winged helm":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 2223;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 2225;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 2224;
                        break;
                    default:
                        modelIDToAssign = 0;
                        break;
                }

                break;
            case "oceanus helm":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2253;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2289;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2271;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2256;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2292;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2274;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2262;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2280;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Reinforced:
                        modelIDToAssign = 2298;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2265;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2277;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 2301;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2268;
                        break;
                }

                break;
            case "stygia helm":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2307;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2343;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2325;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2310;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2346;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2328;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2316;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2334;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Reinforced:
                        modelIDToAssign = 2352;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2313;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2331;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 2355;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2322;
                        break;
                }

                break;
            case "volcanus helm":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2361;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2397;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2379;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2364;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2400;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2382;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2370;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2388;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Reinforced:
                        modelIDToAssign = 2406;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2373;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2385;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 2409;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2376;
                        break;
                }

                break;
            case "aerus helm":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2415;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2451;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2433;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2418;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2454;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2436;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2424;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2442;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Reinforced:
                        modelIDToAssign = 2460;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2421;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2439;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 2463;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2430;
                        break;
                }

                break;
            case "wizard hat":
                if (item.Item_Type != Slot.HELM || item.Object_Type != (int)eObjectType.Cloth)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = epic;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1278;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1279;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1280;
                        break;
                    default:
                        modelIDToAssign = 0;
                        break;
                }

                break;
            case "robin hood hat":
                if (item.Item_Type != Slot.HELM)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1281;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1282;
                        break;
                    default:
                        modelIDToAssign = 0;
                        break;
                }

                break;
            case "fur cap":
                if (item.Item_Type != Slot.HELM || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                modelIDToAssign = 1283;
                break;
            case "tarboosh":
                if (item.Item_Type != Slot.HELM || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                modelIDToAssign = 1284;
                break;
            case "leaf hat":
                if (item.Item_Type != Slot.HELM || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                modelIDToAssign = 1285;
                break;
            case "wing hat":
                if (item.Item_Type != Slot.HELM || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                modelIDToAssign = 1286;
                break;
            case "jester hat":
                if (item.Item_Type != Slot.HELM || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                modelIDToAssign = 1287;
                break;
            case "stag helm":
                if (item.Item_Type != Slot.HELM || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                modelIDToAssign = 1288;
                break;
            case "wolf helm":
                if (item.Item_Type != Slot.HELM || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                modelIDToAssign = 1289;
                break;
            /* case "candle hat":
                 if (item.Item_Type != Slot.HELM)
                 {
                     SendNotValidMessage(player);
                     break;
                 }
                 price = festive;
                 modelIDToAssign = 4169;
                 break;*/

            #endregion

            #region torsos

            case "dragonslayer breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4015;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4046;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4099;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 3990;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4021;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4074;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4010;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4041;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4094;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 3995;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4026;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 4089;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 4000;
                        break;
                }

                break;
            case "dragonsworn breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3783;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3758;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3778;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3778;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3773;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3768;
                        break;
                }

                break;
            case "good shar breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) < 100000)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3018;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2988;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3012;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 2994;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3000;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3006;
                        break;
                }

                break;
            case "possessed shar breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3081;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3086;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3091;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3106;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3096;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3101;
                        break;
                }

                break;
            case "good inconnu breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) < 250000)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3059;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3064;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3116;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3043;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3048;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3053;
                        break;
                }

                break;
            case "possessed inconnu breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3069;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3075;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3111;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3028;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3033;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3038;
                        break;
                }

                break;
            case "good realm breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts) < 3)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2790;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2797;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 2803;
                        break;

                    case eObjectType.Scale:
                    case eObjectType.Chain:
                        modelIDToAssign = 2809;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 2815;
                        break;
                }

                break;
            case "possessed realm breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2728;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2735;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 2741;
                        break;

                    case eObjectType.Scale:
                    case eObjectType.Chain:
                        modelIDToAssign = 2747;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 2753;
                        break;
                }

                break;
            case "mino breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3631;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3606;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3611;
                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 3621;
                        break;
                    case eObjectType.Chain:
                        modelIDToAssign = 3611;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 3616;
                        break;
                }

                break;
            case "eirene's chest":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 2226;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 2228;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 2227;
                        break;
                    default:
                        modelIDToAssign = 0;
                        break;
                }

                break;
            case "naliah's robe":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 2516;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 2517;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 2518;
                        break;
                    default:
                        modelIDToAssign = 0;
                        break;
                }

                break;
            case "guard of valor":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 2475;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 2476;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 2477;
                        break;
                    default:
                        modelIDToAssign = 0;
                        break;
                }

                break;
            case "golden scarab vest":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 2187;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 2189;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 2188;
                        break;
                    default:
                        modelIDToAssign = 0;
                        break;
                }

                break;
            case "oceanus breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1619;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 1621;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1623;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1640;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 1641;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1642;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1848;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1849;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Reinforced:
                        modelIDToAssign = 1850;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2101;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2102;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1773;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2092;
                        break;
                }

                break;
            case "stygia breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2515;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2515;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2515;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2135; //lol leopard print
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2136;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2137;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1757;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1758;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Reinforced:
                        modelIDToAssign = 1759;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1809;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1810;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1791;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2124;
                        break;
                }

                break;
            case "volcanus breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2169;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2169;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2169;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2176;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2178;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2177;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1780;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1781;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Reinforced:
                        modelIDToAssign = 1782;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1694;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1695;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1714;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1703;
                        break;
                }

                break;
            case "aerus breastplate":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2245;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2246;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2247;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 2144;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 2146;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 2145;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1798;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1799;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Reinforced:
                        modelIDToAssign = 1800;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 1736;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 1737;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1738;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1685;
                        break;
                }

                break;
            case "class epic chestpiece":
                if (item.Item_Type != Slot.TORSO)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                switch ((eCharacterClass)player.CharacterClass.ID)
                {
                    //alb
                    case eCharacterClass.Armsman:
                        modelIDToAssign = 688;
                        break;
                    case eCharacterClass.Cabalist:
                        modelIDToAssign = 682;
                        break;
                    case eCharacterClass.Cleric:
                        modelIDToAssign = 713;
                        break;
                    case eCharacterClass.Friar:
                        modelIDToAssign = 797;
                        break;
                    case eCharacterClass.Infiltrator:
                        modelIDToAssign = 792;
                        break;
                    case eCharacterClass.Mercenary:
                        modelIDToAssign = 718;
                        break;
                    case eCharacterClass.Minstrel:
                        modelIDToAssign = 3380;
                        break;
                    case eCharacterClass.Necromancer:
                        modelIDToAssign = 1266;
                        break;
                    case eCharacterClass.Paladin:
                        modelIDToAssign = 693;
                        break;
                    case eCharacterClass.Reaver:
                        modelIDToAssign = 1267;
                        break;
                    case eCharacterClass.Scout:
                        modelIDToAssign = 728;
                        break;
                    case eCharacterClass.Sorcerer:
                        modelIDToAssign = 804;
                        break;
                    case eCharacterClass.Theurgist:
                        modelIDToAssign = 733;
                        break;
                    case eCharacterClass.Wizard:
                        modelIDToAssign = 798;
                        break;

                    //mid
                    case eCharacterClass.Berserker:
                        modelIDToAssign = 751;
                        break;
                    case eCharacterClass.Bonedancer:
                        modelIDToAssign = 1187;
                        break;
                    case eCharacterClass.Healer:
                        modelIDToAssign = 698;
                        break;
                    case eCharacterClass.Hunter:
                        modelIDToAssign = 756;
                        break;
                    case eCharacterClass.Runemaster:
                        modelIDToAssign = 703;
                        break;
                    case eCharacterClass.Savage:
                        modelIDToAssign = 1192;
                        break;
                    case eCharacterClass.Shadowblade:
                        modelIDToAssign = 761;
                        break;
                    case eCharacterClass.Shaman:
                        modelIDToAssign = 766;
                        break;
                    case eCharacterClass.Skald:
                        modelIDToAssign = 771;
                        break;
                    case eCharacterClass.Spiritmaster:
                        modelIDToAssign = 799;
                        break;
                    case eCharacterClass.Thane:
                        modelIDToAssign = 3370;
                        break;
                    case eCharacterClass.Warrior:
                        modelIDToAssign = 776;
                        break;

                    //hib
                    case eCharacterClass.Animist:
                        modelIDToAssign = 1186;
                        break;
                    case eCharacterClass.Bard:
                        modelIDToAssign = 734;
                        break;
                    case eCharacterClass.Blademaster:
                        modelIDToAssign = 782;
                        break;
                    case eCharacterClass.Champion:
                        modelIDToAssign = 810;
                        break;
                    case eCharacterClass.Druid:
                        modelIDToAssign = 739;
                        break;
                    case eCharacterClass.Eldritch:
                        modelIDToAssign = 744;
                        break;
                    case eCharacterClass.Enchanter:
                        modelIDToAssign = 781;
                        break;
                    case eCharacterClass.Hero:
                        modelIDToAssign = 708;
                        break;
                    case eCharacterClass.Mentalist:
                        modelIDToAssign = 745;
                        break;
                    case eCharacterClass.Nightshade:
                        modelIDToAssign = 746;
                        break;
                    case eCharacterClass.Ranger:
                        modelIDToAssign = 815;
                        break;
                    case eCharacterClass.Valewalker:
                        modelIDToAssign = 1003;
                        break;
                    case eCharacterClass.Warden:
                        modelIDToAssign = 805;
                        break;
                }

                break;

            #endregion

            #region sleeves

            case "dragonslayer sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4017;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4048;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4101;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 3992;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4023;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4076;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4012;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4043;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4096;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 3997;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4028;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 4091;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 4002;
                        break;
                }

                break;
            case "good shar sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) < 100000)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3020;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2990;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3014;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 2996;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3002;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3008;
                        break;
                }

                break;
            case "possessed shar sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3083;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3088;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3093;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3108;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3098;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3103;
                        break;
                }

                break;
            case "good inconnu sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) < 250000)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3061;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3066;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3118;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3045;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3050;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3055;
                        break;
                }

                break;
            case "possessed inconnu sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3123;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3077;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3113;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3030;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3035;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3040;
                        break;
                }

                break;
            case "good realm sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2793;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2799;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 2805;
                        break;

                    case eObjectType.Scale:
                    case eObjectType.Chain:
                        modelIDToAssign = 2811;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 2817;
                        break;
                }

                break;
            case "possessed realm sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills) < 5)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2731;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2737;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 2743;
                        break;

                    case eObjectType.Scale:
                    case eObjectType.Chain:
                        modelIDToAssign = 2749;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 2755;
                        break;
                }

                break;
            case "mino sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3644;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3608;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3628;
                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 3623;
                        break;
                    case eObjectType.Chain:
                        modelIDToAssign = 3613;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 3618;
                        break;
                }

                break;
            case "class epic sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                switch ((eCharacterClass)player.CharacterClass.ID)
                {
                    //alb
                    case eCharacterClass.Armsman:
                        modelIDToAssign = 690;
                        break;
                    case eCharacterClass.Cabalist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Cleric:
                        modelIDToAssign = 715;
                        break;
                    case eCharacterClass.Friar:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Infiltrator:
                        modelIDToAssign = 794;
                        break;
                    case eCharacterClass.Mercenary:
                        modelIDToAssign = 720;
                        break;
                    case eCharacterClass.Minstrel:
                        modelIDToAssign = 725;
                        break;
                    case eCharacterClass.Necromancer:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Paladin:
                        modelIDToAssign = 695;
                        break;
                    case eCharacterClass.Reaver:
                        modelIDToAssign = 1269;
                        break;
                    case eCharacterClass.Scout:
                        modelIDToAssign = 730;
                        break;
                    case eCharacterClass.Sorcerer:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Theurgist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Wizard:
                        modelIDToAssign = 0;
                        break;

                    //mid
                    case eCharacterClass.Berserker:
                        modelIDToAssign = 753;
                        break;
                    case eCharacterClass.Bonedancer:
                        modelIDToAssign = 1189;
                        break;
                    case eCharacterClass.Healer:
                        modelIDToAssign = 700;
                        break;
                    case eCharacterClass.Hunter:
                        modelIDToAssign = 758;
                        break;
                    case eCharacterClass.Runemaster:
                        modelIDToAssign = 705;
                        break;
                    case eCharacterClass.Savage:
                        modelIDToAssign = 1194;
                        break;
                    case eCharacterClass.Shadowblade:
                        modelIDToAssign = 763;
                        break;
                    case eCharacterClass.Shaman:
                        modelIDToAssign = 768;
                        break;
                    case eCharacterClass.Skald:
                        modelIDToAssign = 773;
                        break;
                    case eCharacterClass.Spiritmaster:
                        modelIDToAssign = 801;
                        break;
                    case eCharacterClass.Thane:
                        modelIDToAssign = 3372;
                        break;
                    case eCharacterClass.Warrior:
                        modelIDToAssign = 778;
                        break;

                    //hib
                    case eCharacterClass.Animist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Bard:
                        modelIDToAssign = 736;
                        break;
                    case eCharacterClass.Blademaster:
                        modelIDToAssign = 784;
                        break;
                    case eCharacterClass.Champion:
                        modelIDToAssign = 812;
                        break;
                    case eCharacterClass.Druid:
                        modelIDToAssign = 741;
                        break;
                    case eCharacterClass.Eldritch:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Enchanter:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Hero:
                        modelIDToAssign = 710;
                        break;
                    case eCharacterClass.Mentalist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Nightshade:
                        modelIDToAssign = 748;
                        break;
                    case eCharacterClass.Ranger:
                        modelIDToAssign = 817;
                        break;
                    case eCharacterClass.Valewalker:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Warden:
                        modelIDToAssign = 807;
                        break;
                }

                break;
            case "foppish sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1732;
                break;
            case "arms of the wind":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1733;
                break;
            case "oceanus sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 1625;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 1639;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1847;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 2100;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1770;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2091;
                        break;
                }

                break;
            case "stygia sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2152;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2134;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1756;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1808;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1788;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2123;
                        break;
                }

                break;
            case "volcanus sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2161;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2175;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1779;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1693;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1711;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1702;
                        break;
                }

                break;
            case "aerus sleeves":
                if (item.Item_Type != Slot.ARMS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2237;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2143;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1797;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1735;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1747;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1684;
                        break;
                }

                break;

            #endregion

            #region pants

            case "dragonslayer pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4016;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4047;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4100;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 3991;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4022;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4075;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4011;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4042;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4095;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 3996;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4027;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 4090;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 4001;
                        break;
                }

                break;
            case "good shar pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) < 100000)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3019;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2989;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3013;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 2995;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3001;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3007;
                        break;
                }

                break;
            case "possessed shar pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3082;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3087;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3092;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3107;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3097;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3102;
                        break;
                }

                break;
            case "good inconnu pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) < 250000)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3060;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3065;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3117;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3044;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3049;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3054;
                        break;
                }

                break;
            case "possessed inconnu pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3071;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3076;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3112;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3029;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3034;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3039;
                        break;
                }

                break;
            case "good realm pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2792;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2798;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 2804;
                        break;

                    case eObjectType.Scale:
                    case eObjectType.Chain:
                        modelIDToAssign = 2810;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 2816;
                        break;
                }

                break;
            case "possessed realm pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills) < 5)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2730;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2736;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 2742;
                        break;

                    case eObjectType.Scale:
                    case eObjectType.Chain:
                        modelIDToAssign = 2748;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 2754;
                        break;
                }

                break;
            case "mino pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3643;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3607;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3627;
                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 3622;
                        break;
                    case eObjectType.Chain:
                        modelIDToAssign = 3612;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 3617;
                        break;
                }

                break;
            case "wing's dive":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1767;
                break;
            case "alvarus' leggings":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1744;
                break;
            case "oceanus pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 1631;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 1646;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1854;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 2107;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1778;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2098;
                        break;
                }

                break;
            case "stygia pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2158;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2141;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1763;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1815;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1796;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2130;
                        break;
                }

                break;
            case "volcanus pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2167;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2182;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1786;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1700;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1718;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1709;
                        break;
                }

                break;
            case "aerus pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2243;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2150;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1804;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1742;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1754;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1691;
                        break;
                }

                break;
            case "class epic pants":
                if (item.Item_Type != Slot.LEGS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                switch ((eCharacterClass)player.CharacterClass.ID)
                {
                    //alb
                    case eCharacterClass.Armsman:
                        modelIDToAssign = 689;
                        break;
                    case eCharacterClass.Cabalist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Cleric:
                        modelIDToAssign = 714;
                        break;
                    case eCharacterClass.Friar:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Infiltrator:
                        modelIDToAssign = 793;
                        break;
                    case eCharacterClass.Mercenary:
                        modelIDToAssign = 719;
                        break;
                    case eCharacterClass.Minstrel:
                        modelIDToAssign = 724;
                        break;
                    case eCharacterClass.Necromancer:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Paladin:
                        modelIDToAssign = 694;
                        break;
                    case eCharacterClass.Reaver:
                        modelIDToAssign = 1268;
                        break;
                    case eCharacterClass.Scout:
                        modelIDToAssign = 729;
                        break;
                    case eCharacterClass.Sorcerer:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Theurgist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Wizard:
                        modelIDToAssign = 0;
                        break;

                    //mid
                    case eCharacterClass.Berserker:
                        modelIDToAssign = 752;
                        break;
                    case eCharacterClass.Bonedancer:
                        modelIDToAssign = 1188;
                        break;
                    case eCharacterClass.Healer:
                        modelIDToAssign = 699;
                        break;
                    case eCharacterClass.Hunter:
                        modelIDToAssign = 757;
                        break;
                    case eCharacterClass.Runemaster:
                        modelIDToAssign = 704;
                        break;
                    case eCharacterClass.Savage:
                        modelIDToAssign = 1193;
                        break;
                    case eCharacterClass.Shadowblade:
                        modelIDToAssign = 762;
                        break;
                    case eCharacterClass.Shaman:
                        modelIDToAssign = 767;
                        break;
                    case eCharacterClass.Skald:
                        modelIDToAssign = 772;
                        break;
                    case eCharacterClass.Spiritmaster:
                        modelIDToAssign = 800;
                        break;
                    case eCharacterClass.Thane:
                        modelIDToAssign = 3371;
                        break;
                    case eCharacterClass.Warrior:
                        modelIDToAssign = 777;
                        break;

                    //hib
                    case eCharacterClass.Animist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Bard:
                        modelIDToAssign = 735;
                        break;
                    case eCharacterClass.Blademaster:
                        modelIDToAssign = 783;
                        break;
                    case eCharacterClass.Champion:
                        modelIDToAssign = 811;
                        break;
                    case eCharacterClass.Druid:
                        modelIDToAssign = 740;
                        break;
                    case eCharacterClass.Eldritch:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Enchanter:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Hero:
                        modelIDToAssign = 709;
                        break;
                    case eCharacterClass.Mentalist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Nightshade:
                        modelIDToAssign = 747;
                        break;
                    case eCharacterClass.Ranger:
                        modelIDToAssign = 816;
                        break;
                    case eCharacterClass.Valewalker:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Warden:
                        modelIDToAssign = 807;
                        break;
                }

                break;

            #endregion

            #region boots

            case "dragonslayer boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4019;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4050;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4103;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 3994;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4025;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4078;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4013;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4044;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4097;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 3999;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4030;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 4093;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 4004;
                        break;
                }

                break;
            case "good shar boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) < 100000)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3021;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2992;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3015;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 2998;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3004;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3010;
                        break;
                }

                break;
            case "possessed shar boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3084;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3089;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3094;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3109;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3099;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3104;
                        break;
                }

                break;
            case "good inconnu boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) < 250000)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3062;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3067;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3119;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3046;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3051;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3056;
                        break;
                }

                break;
            case "possessed inconnu boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3073;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3078;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3114;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3031;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3036;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3041;
                        break;
                }

                break;
            case "good realm boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts) < 1)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2795;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2801;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 2807;
                        break;

                    case eObjectType.Scale:
                    case eObjectType.Chain:
                        modelIDToAssign = 2813;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 2819;
                        break;
                }

                break;
            case "possessed realm boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills) < 1)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2733;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2739;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 2745;
                        break;

                    case eObjectType.Scale:
                    case eObjectType.Chain:
                        modelIDToAssign = 2751;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 2757;
                        break;
                }

                break;
            case "mino boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3646;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3610;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3629;
                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 3625;
                        break;
                    case eObjectType.Chain:
                        modelIDToAssign = 3615;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 3620;
                        break;
                }

                break;
            case "enyalio's boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 2488;
                break;
            case "flamedancer's boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1731;
                break;
            case "oceanus boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 1629;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 1643;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1851;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 2104;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1775;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2095;
                        break;
                }

                break;
            case "stygia boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2157;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2139;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1761;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1813;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1793;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2127;
                        break;
                }

                break;
            case "volcanus boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2166;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2180;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1784;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1698;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1716;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1706;
                        break;
                }

                break;
            case "aerus boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2242;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2148;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1802;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1740;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1752;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1688;
                        break;
                }

                break;
            case "class epic boots":
                if (item.Item_Type != Slot.FEET)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                switch ((eCharacterClass)player.CharacterClass.ID)
                {
                    //alb
                    case eCharacterClass.Armsman:
                        modelIDToAssign = 692;
                        break;
                    case eCharacterClass.Cabalist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Cleric:
                        modelIDToAssign = 717;
                        break;
                    case eCharacterClass.Friar:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Infiltrator:
                        modelIDToAssign = 796;
                        break;
                    case eCharacterClass.Mercenary:
                        modelIDToAssign = 722;
                        break;
                    case eCharacterClass.Minstrel:
                        modelIDToAssign = 727;
                        break;
                    case eCharacterClass.Necromancer:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Paladin:
                        modelIDToAssign = 697;
                        break;
                    case eCharacterClass.Reaver:
                        modelIDToAssign = 1270;
                        break;
                    case eCharacterClass.Scout:
                        modelIDToAssign = 731;
                        break;
                    case eCharacterClass.Sorcerer:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Theurgist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Wizard:
                        modelIDToAssign = 0;
                        break;

                    //mid
                    case eCharacterClass.Berserker:
                        modelIDToAssign = 755;
                        break;
                    case eCharacterClass.Bonedancer:
                        modelIDToAssign = 1190;
                        break;
                    case eCharacterClass.Healer:
                        modelIDToAssign = 702;
                        break;
                    case eCharacterClass.Hunter:
                        modelIDToAssign = 760;
                        break;
                    case eCharacterClass.Runemaster:
                        modelIDToAssign = 707;
                        break;
                    case eCharacterClass.Savage:
                        modelIDToAssign = 1196;
                        break;
                    case eCharacterClass.Shadowblade:
                        modelIDToAssign = 765;
                        break;
                    case eCharacterClass.Shaman:
                        modelIDToAssign = 770;
                        break;
                    case eCharacterClass.Skald:
                        modelIDToAssign = 775;
                        break;
                    case eCharacterClass.Spiritmaster:
                        modelIDToAssign = 803;
                        break;
                    case eCharacterClass.Thane:
                        modelIDToAssign = 3374;
                        break;
                    case eCharacterClass.Warrior:
                        modelIDToAssign = 780;
                        break;

                    //hib
                    case eCharacterClass.Animist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Bard:
                        modelIDToAssign = 738;
                        break;
                    case eCharacterClass.Blademaster:
                        modelIDToAssign = 786;
                        break;
                    case eCharacterClass.Champion:
                        modelIDToAssign = 814;
                        break;
                    case eCharacterClass.Druid:
                        modelIDToAssign = 743;
                        break;
                    case eCharacterClass.Eldritch:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Enchanter:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Hero:
                        modelIDToAssign = 712;
                        break;
                    case eCharacterClass.Mentalist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Nightshade:
                        modelIDToAssign = 750;
                        break;
                    case eCharacterClass.Ranger:
                        modelIDToAssign = 819;
                        break;
                    case eCharacterClass.Valewalker:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Warden:
                        modelIDToAssign = 809;
                        break;
                }

                break;

            #endregion

            #region gloves

            case "dragonslayer gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4018;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4049;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4102;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Leather:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 3993;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4024;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4077;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 4014;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4045;
                                break;
                            case eRealm.Hibernia:
                                modelIDToAssign = 4098;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                                modelIDToAssign = 3998;
                                break;
                            case eRealm.Midgard:
                                modelIDToAssign = 4029;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 4092;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 4003;
                        break;
                }

                break;
            case "good shar gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) < 100000)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3022;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2993;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3016;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 2999;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3005;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3011;
                        break;
                }

                break;
            case "possessed shar gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3085;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3090;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3095;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3110;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3100;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3105;
                        break;
                }

                break;
            case "good inconnu gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned) < 250000)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3063;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3068;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3120;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3047;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3052;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3057;
                        break;
                }

                break;
            case "possessed inconnu gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3074;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3079;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3115;
                        break;

                    case eObjectType.Chain:
                        switch (source.Realm)
                        {
                            case eRealm.Albion:
                            case eRealm.Midgard:
                                modelIDToAssign = 3032;
                                break;
                            default:
                                modelIDToAssign = 0;
                                break;
                        }

                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 3037;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 3042;
                        break;
                }

                break;
            case "good realm gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts) < 1)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2796;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2802;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 2808;
                        break;

                    case eObjectType.Scale:
                    case eObjectType.Chain:
                        modelIDToAssign = 2814;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 2820;
                        break;
                }

                break;
            case "possessed realm gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills) < 1)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2734;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2740;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 2746;
                        break;

                    case eObjectType.Scale:
                    case eObjectType.Chain:
                        modelIDToAssign = 2752;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 2758;
                        break;
                }

                break;
            case "mino gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = festive;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 3645;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 3609;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 3630;
                        break;

                    case eObjectType.Scale:
                        modelIDToAssign = 3624;
                        break;
                    case eObjectType.Chain:
                        modelIDToAssign = 3614;
                        break;

                    case eObjectType.Plate:
                        modelIDToAssign = 3619;
                        break;
                }

                break;
            case "maddening scalars":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1746;
                break;
            case "sharkskin gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1734;
                break;
            case "oceanus gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 1620;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 1645;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1853;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 2106;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1776;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2097;
                        break;
                }

                break;
            case "stygia gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2248;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2140;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1762;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1814;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1794;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 2129;
                        break;
                }

                break;
            case "volcanus gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2249;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2181;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1785;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1699;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1717;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1708;
                        break;
                }

                break;
            case "aerus gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eObjectType)item.Object_Type)
                {
                    case eObjectType.Cloth:
                        modelIDToAssign = 2250;
                        break;

                    case eObjectType.Leather:
                        modelIDToAssign = 2149;
                        break;

                    case eObjectType.Studded:
                    case eObjectType.Reinforced:
                        modelIDToAssign = 1803;
                        break;

                    case eObjectType.Chain:
                        modelIDToAssign = 1741;
                        break;
                    case eObjectType.Scale:
                        modelIDToAssign = 1753;
                        break;
                    case eObjectType.Plate:
                        modelIDToAssign = 1690;
                        break;
                }

                break;
            case "class epic gloves":
                if (item.Item_Type != Slot.HANDS)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                switch ((eCharacterClass)player.CharacterClass.ID)
                {
                    //alb
                    case eCharacterClass.Armsman:
                        modelIDToAssign = 691;
                        break;
                    case eCharacterClass.Cabalist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Cleric:
                        modelIDToAssign = 716;
                        break;
                    case eCharacterClass.Friar:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Infiltrator:
                        modelIDToAssign = 795;
                        break;
                    case eCharacterClass.Mercenary:
                        modelIDToAssign = 721;
                        break;
                    case eCharacterClass.Minstrel:
                        modelIDToAssign = 726;
                        break;
                    case eCharacterClass.Necromancer:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Paladin:
                        modelIDToAssign = 696;
                        break;
                    case eCharacterClass.Reaver:
                        modelIDToAssign = 1271;
                        break;
                    case eCharacterClass.Scout:
                        modelIDToAssign = 732;
                        break;
                    case eCharacterClass.Sorcerer:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Theurgist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Wizard:
                        modelIDToAssign = 0;
                        break;

                    //mid
                    case eCharacterClass.Berserker:
                        modelIDToAssign = 754;
                        break;
                    case eCharacterClass.Bonedancer:
                        modelIDToAssign = 1191;
                        break;
                    case eCharacterClass.Healer:
                        modelIDToAssign = 701;
                        break;
                    case eCharacterClass.Hunter:
                        modelIDToAssign = 759;
                        break;
                    case eCharacterClass.Runemaster:
                        modelIDToAssign = 706;
                        break;
                    case eCharacterClass.Savage:
                        modelIDToAssign = 1195;
                        break;
                    case eCharacterClass.Shadowblade:
                        modelIDToAssign = 764;
                        break;
                    case eCharacterClass.Shaman:
                        modelIDToAssign = 768;
                        break;
                    case eCharacterClass.Skald:
                        modelIDToAssign = 774;
                        break;
                    case eCharacterClass.Spiritmaster:
                        modelIDToAssign = 802;
                        break;
                    case eCharacterClass.Thane:
                        modelIDToAssign = 3373;
                        break;
                    case eCharacterClass.Warrior:
                        modelIDToAssign = 779;
                        break;

                    //hib
                    case eCharacterClass.Animist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Bard:
                        modelIDToAssign = 737;
                        break;
                    case eCharacterClass.Blademaster:
                        modelIDToAssign = 785;
                        break;
                    case eCharacterClass.Champion:
                        modelIDToAssign = 813;
                        break;
                    case eCharacterClass.Druid:
                        modelIDToAssign = 742;
                        break;
                    case eCharacterClass.Eldritch:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Enchanter:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Hero:
                        modelIDToAssign = 711;
                        break;
                    case eCharacterClass.Mentalist:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Nightshade:
                        modelIDToAssign = 749;
                        break;
                    case eCharacterClass.Ranger:
                        modelIDToAssign = 818;
                        break;
                    case eCharacterClass.Valewalker:
                        modelIDToAssign = 0;
                        break;
                    case eCharacterClass.Warden:
                        modelIDToAssign = 808;
                        break;
                }

                break;

            #endregion

            #region cloaks

            case "realm cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = cloakexpensive;
                switch ((eRealm)player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 3800;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 3802;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 3801;
                        break;
                }

                break;
            case "dragonslayer cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = cloakexpensive;
                switch ((eRealm)player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 4105;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 4109;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 4107;
                        break;
                }

                break;
            case "dragonsworn cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 3790;
                break;
            case "valentines cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 3752;
                break;
            case "winter cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 4115;
                break;
            case "clean leather cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 3637;
                break;
            case "corrupt leather cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 3634;
                break;
            case "cloudsong":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 1727;
                break;
            case "shades of mist":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 1726;
                break;
            case "magma cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 1725;
                break;
            case "stygian cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 1724;
                break;
            case "feathered cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 1720;
                break;
            case "oceanus cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 1722;
                break;
            case "harpy feather cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 1721;
                break;
            case "healer's embrace":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = cloakmedium;
                modelIDToAssign = 1723;
                break;
            // $"[Mage Cloak 2] ({lowbie} {currencyName})\n");
            // sb.Append($"[Mage Cloak 3] ({lowbie} {currencyName})\n");
            // sb.Append($"[Embossed Cloak] ({lowbie} {currencyName})\n");
            // sb.Append($"[Collared Cloak] ({lowbie} {currencyName})\n");
            case "collared cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }
                price = lowbie;
                modelIDToAssign = 669;
                break;
            case "mage cloak 2":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }
                price = lowbie;
                modelIDToAssign = 557;
                break;
            case "mage cloak 3":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }
                price = lowbie;
                modelIDToAssign = 144;
                break;
            case "embossed cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }
                price = lowbie;
                modelIDToAssign = 560;
                break;
            case "basic cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 57;
                break;

            case "hooded cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 96;
                break;

            case "bordered cloak":
                if (item.Item_Type != Slot.CLOAK)
                {
                    SendNotValidMessage(player);
                    break;
                }
                price = freebie;
                modelIDToAssign = 92;
                break;

            #endregion

            #region weapons

            #region 1h wep

            #region crafted skins

            case "bladed claw greave":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 959;
                break;

            case "bladed fang greave":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 960;
                break;
            case "bladed moon claw":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 961;
                break;
            case "bladed moon fang":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 962;
                break;
            case "claw greave":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 963;
                break;
            case "fang greave":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 964;
                break;
            case "flex chain":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 857;
                break;
            case "flex whip":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 859;
                break;
            case "flex flail":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 861;
                break;
            case "flex dagger flail":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 860;
                break;
            case "flex morning star":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 862;
                break;
            case "flex pick flail":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 863;
                break;
            case "firbolg scythe":
                if (item.Object_Type != (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 927;
                break;
            case "harvest scythe":
                if (item.Object_Type != (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 929;
                break;
            case "war scythe":
                if (item.Object_Type != (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 932;
                break;
            case "dirk 1h":
                if (item.Object_Type != (int)eObjectType.ThrustWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 21;
                break;
            case "rapier 1h":
                if (item.Object_Type != (int)eObjectType.ThrustWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 22;
                break;
            case "main gauche 1h":
                if (item.Object_Type != (int)eObjectType.ThrustWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 25;
                break;
            case "celtic dirk 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 454;
                break;
            case "celtic rapier 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 455;
                break;
            case "celtic stiletto 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 456;
                break;
            case "club 1h":
                if (item.Object_Type != (int)eObjectType.CrushingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 11;
                break;
            case "hammer 1h":
                if (item.Object_Type != (int)eObjectType.CrushingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 12;
                break;
            case "mace 1h":
                if (item.Object_Type != (int)eObjectType.CrushingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 13;
                break;
            case "celtic club 1h":
                if (item.Object_Type != (int)eObjectType.Blunt)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 449;
                break;
            case "celtic mace 1h":
                if (item.Object_Type != (int)eObjectType.Blunt)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 450;
                break;
            case "small hammer 1h":
                if (item.Object_Type != (int)eObjectType.Hammer)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 320;
                break;
            case "pick hammer 1h":
                if (item.Object_Type != (int)eObjectType.Hammer)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 323;
                break;
            case "norse hammer 1h":
                if (item.Object_Type != (int)eObjectType.Hammer)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 321;
                break;
            case "short sword 1h":
                if (item.Object_Type != (int)eObjectType.SlashingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 3;
                break;
            case "longsword 1h":
                if (item.Object_Type != (int)eObjectType.SlashingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 4;
                break;
            case "handaxe 1h":
                if (item.Object_Type != (int)eObjectType.SlashingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 2;
                break;
            case "scimitar 1h":
                if (item.Object_Type != (int)eObjectType.SlashingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 8;
                break;
            case "celtic short sword 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 445;
                break;
            case "celtic longsword 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 446;
                break;
            case "celtic broadsword 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 447;
                break;
            case "norse short sword 1h":
                if (item.Object_Type != (int)eObjectType.Sword)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 311;
                break;
            case "norse longsword 1h":
                if (item.Object_Type != (int)eObjectType.Sword)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 310;
                break;
            case "norse broadsword 1h":
                if (item.Object_Type != (int)eObjectType.Sword)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 312;
                break;
            case "norse spiked axe 1h":
                if (item.Object_Type != (int)eObjectType.Sword)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 315;
                break;
            case "great bladed claw greave":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 965;
                break;
            case "great bladed fang greave":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 966;
                break;
            case "great bladed moon claw":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 967;
                break;
            case "great bladed moon fang":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 968;
                break;
            case "great claw greave":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 969;
                break;
            case "great fang greave":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 970;
                break;
            case "flex spiked flail":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 864;
                break;
            case "flex spiked whip":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 865;
                break;
            case "flex war chain":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 866;
                break;
            case "flex whip dagger":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 868;
                break;
            case "flex whip mace":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 869;
                break;
            case "firbolg great scythe":
                if (item.Object_Type != (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 926;
                break;
            case "great war scythe":
                if (item.Object_Type != (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 928;
                break;
            case "martial scythe":
                if (item.Object_Type != (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 930;
                break;
            case "toothpick 1h":
                if (item.Object_Type != (int)eObjectType.ThrustWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 876;
                break;
            case "highlander dirk 1h":
                if (item.Object_Type != (int)eObjectType.ThrustWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 889;
                break;
            case "foil 1h":
                if (item.Object_Type != (int)eObjectType.ThrustWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 29;
                break;
            case "guarded rapier 1h":
                if (item.Object_Type != (int)eObjectType.ThrustWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 653;
                break;
            case "curved dagger 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 457;
                break;
            case "celtic guarded rapier 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 643;
                break;
            case "lurikeen dagger 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 943;
                break;
            case "spiked mace 1h":
                if (item.Object_Type != (int)eObjectType.CrushingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 20;
                break;
            case "war hammer 1h":
                if (item.Object_Type != (int)eObjectType.CrushingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 15;
                break;
            case "flanged mace 1h":
                if (item.Object_Type != (int)eObjectType.CrushingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 14;
                break;
            case "spiked club 1h":
                if (item.Object_Type != (int)eObjectType.Blunt)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 452;
                break;
            case "celtic spiked mace 1h":
                if (item.Object_Type != (int)eObjectType.Blunt)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 451;
                break;
            case "celtic hammer 1h":
                if (item.Object_Type != (int)eObjectType.Blunt)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 461;
                break;
            case "great hammer 1h":
                if (item.Object_Type != (int)eObjectType.Hammer)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 324;
                break;
            case "spiked hammer 1h":
                if (item.Object_Type != (int)eObjectType.Hammer)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 656;
                break;
            case "jambiya 1h":
                if (item.Object_Type != (int)eObjectType.SlashingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 651;
                break;
            case "cinquedea 1h":
                if (item.Object_Type != (int)eObjectType.SlashingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 877;
                break;
            case "falchion 1h":
                if (item.Object_Type != (int)eObjectType.SlashingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 879;
                break;
            case "coffin axe 1h":
                if (item.Object_Type != (int)eObjectType.SlashingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 878;
                break;
            case "celtic sickle 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 453;
                break;
            case "celtic hooked sword 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 460;
                break;
            case "falcata 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 444;
                break;
            case "dwarven short sword 1h":
                if (item.Object_Type != (int)eObjectType.Sword)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 655;
                break;
            case "norse cleaver 1h":
                if (item.Object_Type != (int)eObjectType.Axe &&
                    item.Object_Type != (int)eObjectType.LeftAxe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 654;
                break;
            case "double axe 1h":
                if (item.Object_Type != (int)eObjectType.Axe &&
                    item.Object_Type != (int)eObjectType.LeftAxe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 573;
                break;
            case "norse bearded axe 1h":
                if (item.Object_Type != (int)eObjectType.Axe &&
                    item.Object_Type != (int)eObjectType.LeftAxe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 316;
                break;
            case "duelists dagger 1h":
                if (item.Object_Type != (int)eObjectType.ThrustWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 885;
                break;
            case "duelists rapier 1h":
                if (item.Object_Type != (int)eObjectType.ThrustWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 886;
                break;
            case "parrying dagger 1h":
                if (item.Object_Type != (int)eObjectType.ThrustWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 887;
                break;
            case "parrying rapier 1h":
                if (item.Object_Type != (int)eObjectType.ThrustWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 888;
                break;
            case "elven dagger 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 895;
                break;
            case "firbolg dagger 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 898;
                break;
            case "leaf dagger 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 902;
                break;
            case "adze 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 940;
                break;
            case "barbed adze 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 941;
                break;
            case "firbolg adze 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 942;
                break;
            case "war adze 1h":
                if (item.Object_Type != (int)eObjectType.Piercing)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 947;
                break;
            case "bishops mace 1h":
                if (item.Object_Type != (int)eObjectType.CrushingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 854;
                break;
            case "coffin hammer mace 1h":
                if (item.Object_Type != (int)eObjectType.CrushingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 855;
                break;
            case "coffin mace 1h":
                if (item.Object_Type != (int)eObjectType.CrushingWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 856;
                break;
            case "celt hammer 1h":
                if (item.Object_Type != (int)eObjectType.Blunt)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 913;
                break;
            case "dire mace 1h":
                if (item.Object_Type != (int)eObjectType.Blunt)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 915;
                break;
            case "firbolg hammer 1h":
                if (item.Object_Type != (int)eObjectType.Blunt)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 916;
                break;
            case "troll hammer 1h":
                if (item.Object_Type != (int)eObjectType.Hammer)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 950;
                break;
            case "troll war hammer 1h":
                if (item.Object_Type != (int)eObjectType.Hammer)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 954;
                break;
            case "kobold sap 1h":
                if (item.Object_Type != (int)eObjectType.Hammer)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1016;
                break;
            case "kobold war club 1h":
                if (item.Object_Type != (int)eObjectType.Hammer)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1019;
                break;
            case "magma hammer 1h":
                if (item.Type_Damage != (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2214;
                break;
            case "aerus hammer 1h":
                if (item.Type_Damage != (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2205;
                break;
            case "aerus sword 1h":
                if (item.Type_Damage != (int)eDamageType.Slash)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2203;
                break;
            case "magma axe 1h":
                if (item.Type_Damage != (int)eDamageType.Slash)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2216;
                break;

            case "elven short sword 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 897;
                break;
            case "elven longsword 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 896;
                break;
            case "firbolg short sword 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 900;
                break;
            case "firbolg longsword 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 899;
                break;
            case "leaf short sword 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 903;
                break;
            case "leaf longsword 1h":
                if (item.Object_Type != (int)eObjectType.Blades)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 901;
                break;
            case "troll dagger 1h":
                if (item.Object_Type != (int)eObjectType.Sword)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 949;
                break;
            case "troll short sword 1h":
                if (item.Object_Type != (int)eObjectType.Sword)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 952;
                break;
            case "troll long sword 1h":
                if (item.Object_Type != (int)eObjectType.Sword)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 948;
                break;
            case "kobold dagger 1h":
                if (item.Object_Type != (int)eObjectType.Sword)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1013;
                break;
            case "kobold short sword 1h":
                if (item.Object_Type != (int)eObjectType.Sword)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1017;
                break;
            case "kobold long sword 1h":
                if (item.Object_Type != (int)eObjectType.Sword)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1015;
                break;
            case "troll hand axe 1h":
                if (item.Object_Type != (int)eObjectType.Axe &&
                    item.Object_Type != (int)eObjectType.LeftAxe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1023;
                break;
            case "troll war axe 1h":
                if (item.Object_Type != (int)eObjectType.Axe &&
                    item.Object_Type != (int)eObjectType.LeftAxe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1025;
                break;
            case "kobold hand axe 1h":
                if (item.Object_Type != (int)eObjectType.Axe &&
                    item.Object_Type != (int)eObjectType.LeftAxe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1014;
                break;
            case "kobold war axe 1h":
                if (item.Object_Type != (int)eObjectType.Axe &&
                    item.Object_Type != (int)eObjectType.LeftAxe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1018;
                break;

            #endregion

            case "traitor's dagger 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Thrust)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1668;
                break;
            case "traitor's axe 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Slash)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 3452;
                break;
            case "croc tooth dagger 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Thrust)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1669;
                break;
            case "croc tooth axe 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Slash)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 3451;
                break;
            case "golden spear 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Thrust)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1807;
                break;
            case "malice axe 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Slash)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 2109;
                break;
            case "malice hammer 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 3447;
                break;
            case "bruiser hammer 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1671;
                break;
            case "battler hammer 1h":
                if ((item.Item_Type != Slot.RIGHTHAND ||
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 3453;
                break;
            case "battler sword 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Slash)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 2112;
                break;
            case "scepter of the meritorious":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1672;
                break;
            case "hilt 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) ||
                    item.Type_Damage == (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 8)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 673;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 670;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 674;
                        break;
                }

                break;
            case "rolling pin":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                modelIDToAssign = 3458;
                break;
            case "wakazashi 1h":
                if (item.Item_Type != Slot.RIGHTHAND &&
                    item.Item_Type != Slot.LEFTHAND &&
                    item.Type_Damage != (int)eDamageType.Thrust &&
                    item.Type_Damage != (int)eDamageType.Slash)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                modelIDToAssign = 2209;
                break;
            case "turkey leg":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 8)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = champion;
                modelIDToAssign = 3454;
                break;
            case "cleaver":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Slash)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                modelIDToAssign = 654;
                break;
            case "khopesh 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Slash)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                modelIDToAssign = 2195;
                break;
            case "stein":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = champion;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 3440;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 3443;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 3438;
                        break;
                }

                break;
            case "hot metal rod":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 8)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = champion;
                modelIDToAssign = 2984;
                break;

            case "hibernia dragonslayer sword 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND)
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3895;
                break;
            case "hibernia dragonslayer hammer 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND)
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3897;
                break;
            case "hibernia dragonslayer dagger 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND)
                    || item.Type_Damage != (int)eDamageType.Thrust
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3899;
                break;

            case "midgard dragonslayer sword 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND)
                    || item.Object_Type != (int)eObjectType.Sword
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3936;
                break;
            case "midgard dragonslayer hammer 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND)
                    || item.Object_Type != (int)eObjectType.Hammer
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3938;
                break;
            case "midgard dragonslayer axe 1h":

            case "albion dragonslayer sword 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND)
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3972;
                break;
            case "albion dragonslayer axe 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND)
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3978;
                break;
            case "albion dragonslayer hammer 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND)
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3974;
                break;
            case "albion dragonslayer dagger 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND)
                    || item.Type_Damage != (int)eDamageType.Thrust
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3976;
                break;

            //hand to hand
            case "snakecharmer's fist":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 2469;
                break;
            case "scorched fist":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eDamageType)item.Type_Damage)
                {
                    case eDamageType.Slash:
                        modelIDToAssign = 3726;
                        break;
                    case eDamageType.Crush:
                        modelIDToAssign = 3728;
                        break;
                    case eDamageType.Thrust:
                        modelIDToAssign = 3730;
                        break;
                }

                break;
            case "dragonsworn fist":
                if (item.Object_Type != (int)eObjectType.HandToHand)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost;
                switch ((eDamageType)item.Type_Damage)
                {
                    case eDamageType.Slash:
                        modelIDToAssign = 3843;
                        break;
                    case eDamageType.Crush:
                        modelIDToAssign = 3845;
                        break;
                    case eDamageType.Thrust:
                        modelIDToAssign = 3847;
                        break;
                }

                break;

            //flex
            case "snakecharmer's whip":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 2119;
                break;
            case "scorched whip":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eDamageType)item.Type_Damage)
                {
                    case eDamageType.Slash:
                        modelIDToAssign = 3697;
                        break;
                    case eDamageType.Crush:
                        modelIDToAssign = 3696;
                        break;
                    case eDamageType.Thrust:
                        modelIDToAssign = 3698;
                        break;
                }

                break;
            case "dragonsworn whip":
                if (item.Object_Type != (int)eObjectType.Flexible)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost;
                switch ((eDamageType)item.Type_Damage)
                {
                    case eDamageType.Slash:
                        modelIDToAssign = 3814;
                        break;
                    case eDamageType.Crush:
                        modelIDToAssign = 3815;
                        break;
                    case eDamageType.Thrust:
                        modelIDToAssign = 3813;
                        break;
                }

                break;

            #endregion

            #region 11 wep

            #region Crafted Skins
            case "battle axe 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 9;
                break;
            case "war mattock 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 16;
                break;
            case "albion great hammer 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 17;
                break;
            case "albion greataxe 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 72;
                break;
            case "albion war axe 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 73;
                break;
            case "norse sword 2h":
                if (item.Object_Type != (int)eObjectType.Sword
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 314;
                break;
            case "norse great axe 2h":
                if (item.Object_Type != (int)eObjectType.Axe
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 317;
                break;
            case "norse large axe 2h":
                if (item.Object_Type != (int)eObjectType.Axe
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 318;
                break;
            case "celtic greatsword 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 448;
                break;
            case "celtic sword 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 459;
                break;
            case "celtic great hammer 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 462;
                break;
            case "celtic spiked mace 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 463;
                break;
            case "celtic shillelagh 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 474;
                break;
            case "norse greatsword 2h":
                if (item.Object_Type != (int)eObjectType.Sword
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 572;
                break;
            case "norse hammer 2h":
                if (item.Object_Type != (int)eObjectType.Hammer
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                modelIDToAssign = 574;
                break;
            case "norse warhammer 2h":
                if (item.Object_Type != (int)eObjectType.Hammer
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 575;
                break;
            case "norse greathammer 2h":
                if (item.Object_Type != (int)eObjectType.Hammer
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 576;
                break;
            case "norse battleaxe 2h":
                if (item.Object_Type != (int)eObjectType.Axe
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 577;
                break;
            case "celtic great falcata 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 639;
                break;
            case "celtic falcata 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 639;
                break;

            case "celtic sledgehammer 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 640;
                break;
            case "briton arch mace 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 640;
                break;
            case "briton scimitar 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 645;
                break;
            case "briton war pick 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Thrust
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 646;
                break;

            case "briton mage staff":
                if (item.Object_Type != (int)eObjectType.Staff
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 3266;
                break;
            case "dwarven sword 2h":
                if (item.Object_Type != (int)eObjectType.Sword
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 658;
                break;
            case "war cleaver 2h":
                if (item.Object_Type != (int)eObjectType.Axe
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 660;
                break;
            case "spiked hammer 2h":
                if (item.Object_Type != (int)eObjectType.Hammer
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 659;
                break;
            case "zweihander 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 841;
                break;
            case "claymore 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 843;
                break;
            case "great mace 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 842;
                break;
            case "dire hammer 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 844;
                break;
            case "dire axe 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 845;
                break;
            case "great mattock 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Thrust
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 846;
                break;
            case "great scimitar 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 847;
                break;
            case "celtic hammer 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 904;
                break;
            case "celtic great mace 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 905;
                break;
            case "celtic dire club 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 906;
                break;
            case "elven greatsword 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 907;
                break;
            case "firbolg hammer 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 908;
                break;
            case "firbolg mace 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 909;
                break;
            case "firbolg trollsplitter 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 910;
                break;
            case "leaf point 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 911;
                break;
            case "shod shillelagh 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 912;
                break;
            case "troll greatsword 2h":
                if (item.Object_Type != (int)eObjectType.Sword
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 957;
                break;
            case "dwarven greataxe 2h":
                if (item.Object_Type != (int)eObjectType.Axe
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1027;
                break;
            case "dwarven great hammer 2h":
                if (item.Object_Type != (int)eObjectType.Hammer
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1028;
                break;
            case "kobold greataxe 2h":
                if (item.Object_Type != (int)eObjectType.Axe
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1030;
                break;
            case "kobold great club 2h":
                if (item.Object_Type != (int)eObjectType.Hammer
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1031;
                break;
            case "kobold great sword 2h":
                if (item.Object_Type != (int)eObjectType.Sword
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1032;
                break;
            case "midgard dragonslayer sword 2h":
                if (item.Object_Type != (int)eObjectType.Sword
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3919;
                break;
            case "midgard dragonslayer hammer 2h":
                if (item.Object_Type != (int)eObjectType.Hammer
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3922;
                break;
            case "midgard dragonslayer axe 2h":
                if (item.Object_Type != (int)eObjectType.Axe
                    || item.Hand != 1
                    || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3923;
                break;
            case "albion dragonslayer thrust 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Thrust
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3954;
                break;
            case "albion dragonslayer slash 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3955;
                break;
            case "albion dragonslayer crush 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3958;
                break;
            case "albion dragonslayer axe 2h":
                if (item.Object_Type != (int)eObjectType.TwoHandedWeapon
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3959;
                break;
            case "hibernia dragonslayer slash 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Slash
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3878;
                break;
            case "hibernia dragonslayer crush 2h":
                if (item.Object_Type != (int)eObjectType.LargeWeapons
                    || item.Hand != 1
                    || item.Type_Damage != (int)eDamageType.Crush
                    || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3881;
                break;

            #endregion

            case "pickaxe":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                modelIDToAssign = 2983;
                break;

            //axe
            case "malice axe 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Slash ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 2110;
                break;
            case "scorched axe 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Slash ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3705;
                break;
            case "magma axe 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Slash ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2217;
                break;

            //spears
            case "golden spear 2h":
                if (item.Object_Type != (int)eObjectType.Spear &&
                    item.Object_Type != (int)eObjectType.CelticSpear)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1662;
                break;
            case "dragon spear 2h":
                if (item.Object_Type != (int)eObjectType.Spear &&
                    item.Object_Type != (int)eObjectType.CelticSpear)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost;
                modelIDToAssign = 3819;
                break;
            case "scorched spear 2h":
                if (item.Object_Type != (int)eObjectType.Spear &&
                    item.Object_Type != (int)eObjectType.CelticSpear)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3714;
                break;
            case "trident spear 2h":
                if (item.Object_Type != (int)eObjectType.Spear &&
                    item.Object_Type != (int)eObjectType.CelticSpear)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2191;
                break;

            //hammers
            case "bruiser hammer 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Crush ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 2113;
                break;
            case "battler hammer 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Crush ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 3448;
                break;
            case "malice hammer 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Crush ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 3449;
                break;
            case "scorched hammer 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Crush ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3704;
                break;
            case "magma hammer 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Crush ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2215;
                break;

            //swords
            case "battler sword 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Slash ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1670;
                break;
            case "scorched sword 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Slash ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3701;
                break;
            case "katana 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Slash ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                modelIDToAssign = 2208;
                break;
            case "khopesh 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Slash ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                modelIDToAssign = 2196;
                break;
            case "hilt 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe ||
                    item.Type_Damage == (int)eDamageType.Crush)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 672;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 671;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 675;
                        break;
                }

                break;

            //thrust
            case "scorched thrust 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Thrust ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3700;
                break;
            case "dragon thrust 2h":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Type_Damage != (int)eDamageType.Thrust ||
                    item.Object_Type == (int)eObjectType.PolearmWeapon ||
                    item.Object_Type == (int)eObjectType.Spear ||
                    item.Object_Type == (int)eObjectType.CelticSpear ||
                    item.Object_Type == (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost;
                modelIDToAssign = 3817;
                break;

            //staffs
            case "traldor's oracle":
                if (item.Object_Type != (int)eObjectType.Staff)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1659;
                break;
            case "trident of the gods":
                if (item.Object_Type != (int)eObjectType.Staff)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1660;
                break;
            case "tartaros gift":
                if (item.Object_Type != (int)eObjectType.Staff)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1658;
                break;
            case "dragonsworn staff":
                if (item.Object_Type != (int)eObjectType.Staff)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost;
                modelIDToAssign = 3827;
                break;
            case "scorched staff":
                if (item.Object_Type != (int)eObjectType.Staff)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3710;
                break;

            //scythes
            case "dragonsworn scythe":
                if (item.Object_Type != (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 3825;
                break;
            case "magma scythe":
                if (item.Object_Type != (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2213;
                break;
            case "scorched scythe":
                if (item.Object_Type != (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3708;
                break;
            case "scythe of kings":
                if (item.Object_Type != (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 3450;
                break;
            case "snakechamer's scythe":
                if (item.Object_Type != (int)eObjectType.Scythe)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 2111;
                break;

            //polearms
            case "dragonsworn pole":
                if (item.Object_Type != (int)eObjectType.PolearmWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost;
                switch ((eDamageType)item.Type_Damage)
                {
                    case eDamageType.Slash:
                        modelIDToAssign = 3832;
                        break;
                    case eDamageType.Crush:
                        modelIDToAssign = 3833;
                        break;
                    case eDamageType.Thrust:
                        modelIDToAssign = 3831;
                        break;
                }

                break;
            case "pole of kings":
                if (item.Object_Type != (int)eObjectType.PolearmWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1661;
                break;
            case "scorched pole":
                if (item.Object_Type != (int)eObjectType.PolearmWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                switch ((eDamageType)item.Type_Damage)
                {
                    case eDamageType.Slash:
                        modelIDToAssign = 3715;
                        break;
                    case eDamageType.Crush:
                        modelIDToAssign = 3716;
                        break;
                    case eDamageType.Thrust:
                        modelIDToAssign = 3714;
                        break;
                }

                break;
            case "golden pole":
                if (item.Object_Type != (int)eObjectType.PolearmWeapon)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1662;
                break;

            #endregion

            #region class weapons

            case "class epic 1h":
                price = champion;
                switch ((eCharacterClass)player.CharacterClass.ID)
                {
                    //alb
                    case eCharacterClass.Armsman:
                        if (item.Object_Type == (int)eObjectType.Shield)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eDamageType)item.Type_Damage)
                        {
                            case eDamageType.Thrust:
                                modelIDToAssign = 3296;
                                break;
                            case eDamageType.Slash:
                                modelIDToAssign = 3295;
                                break;
                            case eDamageType.Crush:
                                modelIDToAssign = 3294;
                                break;
                        }

                        break;
                    case eCharacterClass.Cabalist:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3264;
                        break;
                    case eCharacterClass.Cleric:
                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3282;
                        break;
                    case eCharacterClass.Friar:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3272;
                        break;
                    case eCharacterClass.Infiltrator:
                        if (item.Object_Type != (int)eObjectType.ThrustWeapon ||
                            item.Object_Type != (int)eObjectType.SlashingWeapon)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eDamageType)item.Type_Damage)
                        {
                            case eDamageType.Thrust:
                                modelIDToAssign = 3270;
                                break;
                            case eDamageType.Slash:
                                modelIDToAssign = 3269;
                                break;
                        }

                        break;
                    case eCharacterClass.Mercenary:
                        if (item.Object_Type != (int)eObjectType.ThrustWeapon ||
                            item.Object_Type != (int)eObjectType.SlashingWeapon ||
                            item.Object_Type != (int)eObjectType.CrushingWeapon)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eDamageType)item.Type_Damage)
                        {
                            case eDamageType.Thrust:
                                modelIDToAssign = 3285;
                                break;
                            case eDamageType.Slash:
                                modelIDToAssign = 3284;
                                break;
                            case eDamageType.Crush:
                                modelIDToAssign = 3283;
                                break;
                        }

                        break;
                    case eCharacterClass.Minstrel:
                        if (item.Object_Type != (int)eObjectType.ThrustWeapon ||
                            item.Object_Type != (int)eObjectType.SlashingWeapon)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eDamageType)item.Type_Damage)
                        {
                            case eDamageType.Thrust:
                                modelIDToAssign = 3277;
                                break;
                            case eDamageType.Slash:
                                modelIDToAssign = 3276;
                                break;
                        }

                        break;
                    case eCharacterClass.Necromancer:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3268;
                        break;
                    case eCharacterClass.Paladin:
                        if (item.Object_Type != (int)eObjectType.ThrustWeapon ||
                            item.Object_Type != (int)eObjectType.SlashingWeapon ||
                            item.Object_Type != (int)eObjectType.CrushingWeapon)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eDamageType)item.Type_Damage)
                        {
                            case eDamageType.Thrust:
                                modelIDToAssign = 3305;
                                break;
                            case eDamageType.Slash:
                                modelIDToAssign = 3304;
                                break;
                            case eDamageType.Crush:
                                modelIDToAssign = 3303;
                                break;
                        }

                        break;
                    case eCharacterClass.Reaver:
                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        if ((eObjectType)item.Object_Type == eObjectType.Flexible)
                        {
                            modelIDToAssign = 3292;
                        }
                        else
                        {
                            if (item.Object_Type != (int)eObjectType.ThrustWeapon ||
                                item.Object_Type != (int)eObjectType.SlashingWeapon ||
                                item.Object_Type != (int)eObjectType.CrushingWeapon)
                            {
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                            }

                            switch ((eDamageType)item.Type_Damage)
                            {
                                case eDamageType.Thrust:
                                    modelIDToAssign = 3291;
                                    break;
                                case eDamageType.Slash:
                                    modelIDToAssign = 3290;
                                    break;
                                case eDamageType.Crush:
                                    modelIDToAssign = 3289;
                                    break;
                            }
                        }

                        break;
                    case eCharacterClass.Scout:
                        if (item.Object_Type != (int)eObjectType.ThrustWeapon ||
                            item.Object_Type != (int)eObjectType.SlashingWeapon)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eDamageType)item.Type_Damage)
                        {
                            case eDamageType.Thrust:
                                modelIDToAssign = 3274;
                                break;
                            case eDamageType.Slash:
                                modelIDToAssign = 3273;
                                break;
                        }

                        break;
                    case eCharacterClass.Sorcerer:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3265;
                        break;
                    case eCharacterClass.Theurgist:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3266;
                        break;
                    case eCharacterClass.Wizard:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3267;
                        break;

                    //mid
                    case eCharacterClass.Berserker:
                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Hammer:
                                modelIDToAssign = 3323;
                                break;
                            case eObjectType.Axe:
                                modelIDToAssign = 3321;
                                break;
                            case eObjectType.Sword:
                                modelIDToAssign = 3325;
                                break;
                            case eObjectType.LeftAxe:
                                modelIDToAssign = 3321;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Bonedancer:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3311;
                        break;
                    case eCharacterClass.Healer:
                        if (item.Item_Type != Slot.RIGHTHAND || item.Item_Type != Slot.TWOHAND ||
                            item.Object_Type != (int)eObjectType.Hammer)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        if (item.Item_Type == Slot.RIGHTHAND)
                            modelIDToAssign = 3335;
                        else
                            modelIDToAssign = 3336;
                        break;
                    case eCharacterClass.Hunter:
                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Spear:
                                if ((eDamageType)item.Type_Damage == eDamageType.Thrust)
                                {
                                    modelIDToAssign = 3319;
                                }
                                else
                                {
                                    modelIDToAssign = 3320;
                                }

                                break;
                            case eObjectType.Sword:
                                modelIDToAssign = 3317;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Runemaster:
                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3309;
                        break;
                    case eCharacterClass.Savage:
                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Hammer:
                                modelIDToAssign = 3329;
                                break;
                            case eObjectType.Axe:
                                modelIDToAssign = 3327;
                                break;
                            case eObjectType.Sword:
                                modelIDToAssign = 3331;
                                break;
                            case eObjectType.HandToHand:
                                modelIDToAssign = 3333;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Shadowblade:
                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Axe:
                                if (item.Item_Type == Slot.TWOHAND)
                                    modelIDToAssign = 3316;
                                else
                                    modelIDToAssign = 3315;
                                break;
                            case eObjectType.Sword:
                                if (item.Item_Type == Slot.TWOHAND)
                                    modelIDToAssign = 3314;
                                else
                                    modelIDToAssign = 3313;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Shaman:
                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        if (item.Item_Type == Slot.TWOHAND)
                            modelIDToAssign = 3338;
                        else
                            modelIDToAssign = 3337;
                        break;
                    case eCharacterClass.Skald:
                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Hammer:
                                if (item.Item_Type == Slot.TWOHAND)
                                    modelIDToAssign = 3342;
                                else
                                    modelIDToAssign = 3341;
                                break;
                            case eObjectType.Axe:
                                if (item.Item_Type == Slot.TWOHAND)
                                    modelIDToAssign = 3340;
                                else
                                    modelIDToAssign = 3339;
                                break;
                            case eObjectType.Sword:
                                if (item.Item_Type == Slot.TWOHAND)
                                    modelIDToAssign = 3344;
                                else
                                    modelIDToAssign = 3343;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Spiritmaster:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3310;
                        break;
                    case eCharacterClass.Thane:
                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Hammer:
                                if (item.Item_Type == Slot.TWOHAND)
                                    modelIDToAssign = 3348;
                                else
                                    modelIDToAssign = 3347;
                                break;
                            case eObjectType.Axe:
                                if (item.Item_Type == Slot.TWOHAND)
                                    modelIDToAssign = 3346;
                                else
                                    modelIDToAssign = 3345;
                                break;
                            case eObjectType.Sword:
                                if (item.Item_Type == Slot.TWOHAND)
                                    modelIDToAssign = 3350;
                                else
                                    modelIDToAssign = 3349;
                                break;
                        }

                        break;
                    case eCharacterClass.Warrior:
                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Hammer:
                                if (item.Item_Type == Slot.TWOHAND)
                                    modelIDToAssign = 3354;
                                else
                                    modelIDToAssign = 3353;
                                break;
                            case eObjectType.Axe:
                                if (item.Item_Type == Slot.TWOHAND)
                                    modelIDToAssign = 3352;
                                else
                                    modelIDToAssign = 3351;
                                break;
                            case eObjectType.Sword:
                                if (item.Item_Type == Slot.TWOHAND)
                                    modelIDToAssign = 3356;
                                else
                                    modelIDToAssign = 3355;
                                break;
                        }

                        break;

                    //hib
                    case eCharacterClass.Animist:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3229;
                        break;
                    case eCharacterClass.Bard:
                        if (item.Object_Type != (int)eObjectType.Blades &&
                            item.Object_Type != (int)eObjectType.Blunt)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Blades:
                                modelIDToAssign = 3235;
                                break;
                            case eObjectType.Blunt:
                                modelIDToAssign = 3236;
                                break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        break;
                    case eCharacterClass.Blademaster:
                        if (item.Object_Type != (int)eObjectType.Blades &&
                            item.Object_Type != (int)eObjectType.Blunt &&
                            item.Object_Type != (int)eObjectType.Piercing)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Blades:
                                modelIDToAssign = 3244;
                                break;
                            case eObjectType.Blunt:
                                modelIDToAssign = 3246;
                                break;
                            case eObjectType.Piercing:
                                modelIDToAssign = 3245;
                                break;
                        }

                        break;
                    case eCharacterClass.Champion:
                        if (item.Object_Type != (int)eObjectType.Blades &&
                            item.Object_Type != (int)eObjectType.Blunt &&
                            item.Object_Type != (int)eObjectType.Piercing)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Blades:
                                modelIDToAssign = 3251;
                                break;
                            case eObjectType.Blunt:
                                modelIDToAssign = 3253;
                                break;
                            case eObjectType.Piercing:
                                modelIDToAssign = 3252;
                                break;
                        }

                        break;
                    case eCharacterClass.Druid:
                        if (item.Object_Type != (int)eObjectType.Blades &&
                            item.Object_Type != (int)eObjectType.Blunt)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Blades:
                                modelIDToAssign = 3247;
                                break;
                            case eObjectType.Blunt:
                                modelIDToAssign = 3248;
                                break;
                        }

                        break;
                    case eCharacterClass.Eldritch:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3226;
                        break;
                    case eCharacterClass.Enchanter:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3227;
                        break;
                    case eCharacterClass.Hero:
                        if (item.Object_Type != (int)eObjectType.Blades &&
                            item.Object_Type != (int)eObjectType.Blunt &&
                            item.Object_Type != (int)eObjectType.Piercing)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Blades:
                                modelIDToAssign = 3256;
                                break;
                            case eObjectType.Blunt:
                                modelIDToAssign = 3258;
                                break;
                            case eObjectType.Piercing:
                                modelIDToAssign = 3257;
                                break;
                        }

                        break;
                    case eCharacterClass.Mentalist:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3228;
                        break;
                    case eCharacterClass.Nightshade:
                        if (item.Object_Type != (int)eObjectType.Blades &&
                            item.Object_Type != (int)eObjectType.Piercing)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Blades:
                                modelIDToAssign = 3233;
                                break;
                            case eObjectType.Piercing:
                                modelIDToAssign = 3234;
                                break;
                        }

                        break;
                    case eCharacterClass.Ranger:
                        if (item.Object_Type != (int)eObjectType.Blades &&
                            item.Object_Type != (int)eObjectType.Piercing)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Blades:
                                modelIDToAssign = 3242;
                                break;
                            case eObjectType.Piercing:
                                modelIDToAssign = 3241;
                                break;
                        }

                        break;
                    case eCharacterClass.Valewalker:
                        if (item.Object_Type != (int)eObjectType.Scythe)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3231;
                        break;
                    case eCharacterClass.Warden:
                        if (item.Object_Type != (int)eObjectType.Blades &&
                            item.Object_Type != (int)eObjectType.Blunt)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                        {
                            SendNotQualifiedMessage(player);
                            price = 0;
                            break;
                        }

                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Blades:
                                modelIDToAssign = 3249;
                                break;
                            case eObjectType.Blunt:
                                modelIDToAssign = 3250;
                                break;
                        }

                        break;
                    default:
                        price = 0;
                        break;
                }

                break;

            case "class epic 2h":
                price = champion;
                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                {
                    SendNotQualifiedMessage(player);
                    price = 0;
                    break;
                }

                switch ((eCharacterClass)player.CharacterClass.ID)
                {
                    //alb
                    case eCharacterClass.Armsman:
                        if ((eObjectType)item.Object_Type == eObjectType.PolearmWeapon)
                        {
                            switch ((eDamageType)item.Type_Damage)
                            {
                                case eDamageType.Thrust:
                                    modelIDToAssign = 3297;
                                    break;
                                case eDamageType.Slash:
                                    modelIDToAssign = 3297;
                                    break;
                                case eDamageType.Crush:
                                    modelIDToAssign = 3298;
                                    break;
                                default:
                                    SendNotValidMessage(player);
                                    price = 0;
                                    break;
                            }
                        }
                        else
                        {
                            switch ((eDamageType)item.Type_Damage)
                            {
                                case eDamageType.Thrust:
                                    modelIDToAssign = 3301;
                                    break;
                                case eDamageType.Slash:
                                    modelIDToAssign = 3300;
                                    break;
                                case eDamageType.Crush:
                                    modelIDToAssign = 3302;
                                    break;
                                default:
                                    SendNotValidMessage(player);
                                    price = 0;
                                    break;
                            }
                        }

                        break;
                    case eCharacterClass.Cabalist:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3264;
                        break;
                    case eCharacterClass.Cleric:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3282;
                        break;
                    case eCharacterClass.Friar:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3271;
                        break;
                    case eCharacterClass.Necromancer:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3268;
                        break;
                    case eCharacterClass.Paladin:
                        switch ((eDamageType)item.Type_Damage)
                        {
                            case eDamageType.Thrust:
                                modelIDToAssign = 3307;
                                break;
                            case eDamageType.Slash:
                                modelIDToAssign = 3306;
                                break;
                            case eDamageType.Crush:
                                modelIDToAssign = 3308;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Sorcerer:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3265;
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        break;
                    case eCharacterClass.Theurgist:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3266;
                        break;
                    case eCharacterClass.Wizard:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3267;
                        break;

                    //mid
                    case eCharacterClass.Berserker:
                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Hammer:
                                modelIDToAssign = 3324;
                                break;
                            case eObjectType.Axe:
                                modelIDToAssign = 3322;
                                break;
                            case eObjectType.Sword:
                                modelIDToAssign = 3326;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Bonedancer:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3311;
                        break;
                    case eCharacterClass.Healer:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3335;
                        break;
                    case eCharacterClass.Hunter:
                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Spear:
                                if ((eDamageType)item.Type_Damage == eDamageType.Thrust)
                                {
                                    modelIDToAssign = 3319;
                                }
                                else
                                {
                                    modelIDToAssign = 3320;
                                }

                                break;
                            case eObjectType.Sword:
                                modelIDToAssign = 3318;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Runemaster:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3309;
                        break;
                    case eCharacterClass.Savage:
                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Hammer:
                                modelIDToAssign = 3330;
                                break;
                            case eObjectType.Axe:
                                modelIDToAssign = 3328;
                                break;
                            case eObjectType.Sword:
                                modelIDToAssign = 3332;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Shadowblade:
                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Axe:
                                modelIDToAssign = 3316;
                                break;
                            case eObjectType.Sword:
                                modelIDToAssign = 3314;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Shaman:
                        if (item.Object_Type != (int)eObjectType.Hammer)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3338;
                        break;
                    case eCharacterClass.Skald:
                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Hammer:
                                modelIDToAssign = 3342;
                                break;
                            case eObjectType.Axe:
                                modelIDToAssign = 3340;
                                break;
                            case eObjectType.Sword:
                                modelIDToAssign = 3344;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Spiritmaster:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3310;
                        break;
                    case eCharacterClass.Thane:
                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Hammer:
                                modelIDToAssign = 3348;
                                break;
                            case eObjectType.Axe:
                                modelIDToAssign = 3346;
                                break;
                            case eObjectType.Sword:
                                modelIDToAssign = 3350;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Warrior:
                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Hammer:
                                modelIDToAssign = 3354;
                                break;
                            case eObjectType.Axe:
                                modelIDToAssign = 3352;
                                break;
                            case eObjectType.Sword:
                                modelIDToAssign = 3356;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;

                    //hib
                    case eCharacterClass.Animist:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3229;
                        break;
                    case eCharacterClass.Champion:
                        switch ((eDamageType)item.Type_Damage)
                        {
                            case eDamageType.Slash:
                                modelIDToAssign = 3254;
                                break;
                            case eDamageType.Thrust:
                                modelIDToAssign = 3254;
                                break;
                            case eDamageType.Crush:
                                modelIDToAssign = 3253;
                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Eldritch:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3226;
                        break;
                    case eCharacterClass.Enchanter:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3227;
                        break;
                    case eCharacterClass.Hero:
                        switch ((eObjectType)item.Object_Type)
                        {
                            case eObjectType.Blades:
                                modelIDToAssign = 3259;
                                break;
                            case eObjectType.Blunt:
                                modelIDToAssign = 3260;
                                break;
                            case eObjectType.Piercing:
                                modelIDToAssign = 3261;
                                break;
                            case eObjectType.CelticSpear:
                                modelIDToAssign = 3263;
                                break;
                            case eObjectType.LargeWeapons:
                                switch ((eDamageType)item.Type_Damage)
                                {
                                    case eDamageType.Slash:
                                        modelIDToAssign = 3259;
                                        break;
                                    case eDamageType.Thrust:
                                        modelIDToAssign = 3261;
                                        break;
                                    case eDamageType.Crush:
                                        modelIDToAssign = 3260;
                                        break;
                                }

                                break;
                            default:
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                        }

                        break;
                    case eCharacterClass.Mentalist:
                        if (item.Object_Type != (int)eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3228;
                        break;
                    case eCharacterClass.Valewalker:
                        if (item.Object_Type != (int)eObjectType.Scythe)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3231;
                        break;
                    default:
                        price = 0;
                        break;
                }

                break;

            #endregion

            #endregion

            #region shields

            #region small shields
            case "leather buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1040;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1043;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1046;
                        break;
                }
                break;
            case "metal buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1041;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1044;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1047;
                        break;
                }
                break;
            case "wood buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1042;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1045;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1048;
                        break;
                }
                break;

            case "dragonsworn buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost;
                modelIDToAssign = 3828;
                break;

            //albion specific
            case "leather tri-tip buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }


                price = lowbie;
                modelIDToAssign = 1103;
                break;
            case "metal tri-tip buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1104;
                break;
            case "wood tri-tip buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1105;
                break;

            case "leather kite buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1118;
                break;
            case "metal kite buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1119;
                break;
            case "wood kite buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1120;
                break;

            case "albion dragonslayer buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3965;
                break;

            //midgard specific        
            case "leather grave buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1130;
                break;
            case "metal grave buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }


                price = lowbie;
                modelIDToAssign = 1131;
                break;
            case "wood grave buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }


                price = lowbie;
                modelIDToAssign = 1132;
                break;

            case "leather norse buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1139;
                break;
            case "metal norse buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1140;
                break;
            case "wood norse buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1141;
                break;

            case "midgard dragonslayer buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3929;
                break;

            //hibernia specific
            case "leather celtic buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }


                price = lowbie;
                modelIDToAssign = 1148;
                break;
            case "metal celtic buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }


                price = lowbie;
                modelIDToAssign = 1149;
                break;
            case "wood celtic buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }


                price = lowbie;
                modelIDToAssign = 1150;
                break;

            case "leather leaf buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1163;
                break;
            case "metal leaf buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1164;
                break;
            case "wood leaf buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1165;
                break;

            case "hibernia dragonslayer buckler":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3888;
                break;

            case "oceanus small shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2192;
                break;
            case "aerus small shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2210;
                break;
            case "magma small shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2218;
                break;
            case "stygia small shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2200;
                break;

            #endregion

            #region medium shields

            case "leather medium heater":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1049;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1052;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1055;
                        break;
                }
                break;
            case "metal medium heater":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1050;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1053;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1056;
                        break;
                }
                break;
            case "wood medium heater":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1051;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1054;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1057;
                        break;
                }
                break;

            case "leather medium tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1085;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1088;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1091;
                        break;
                }
                break;
            case "metal medium tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1086;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1089;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1092;
                        break;
                }
                break;
            case "wood medium tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1087;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1090;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1093;
                        break;
                }
                break;

            case "leather medium round":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1094;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1097;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1100;
                        break;
                }
                break;
            case "metal medium round":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1095;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1098;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1101;
                        break;
                }
                break;
            case "wood medium round":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1096;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1099;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1102;
                        break;
                }
                break;

            case "dragonsworn medium":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost;
                modelIDToAssign = 3829;
                break;

            case "oceanus medium shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2193;
                break;
            case "aerus medium shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2211;
                break;
            case "magma medium shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2219;
                break;
            case "stygia medium shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2201;
                break;

            //albion specific        
            case "leather medium horned":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1112;
                break;
            case "metal medium horned":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1113;
                break;
            case "wood medium horned":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1114;
                break;

            case "leather medium kite":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1115;
                break;
            case "metal medium kite":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1116;
                break;
            case "wood medium kite":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1117;
                break;

            case "albion dragonslayer medium":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3966;
                break;

            //midgard specific
            case "leather medium crescent":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1124;
                break;
            case "metal medium crescent":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1125;
                break;
            case "wood medium crescent":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1126;
                break;

            case "leather medium grave":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1127;
                break;
            case "metal medium grave":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1128;
                break;
            case "wood medium grave":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1129;
                break;

            case "midgard dragonslayer medium":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3930;
                break;

            //hibernia specific
            case "leather medium celtic":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1145;
                break;
            case "metal medium celtic":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1146;
                break;
            case "wood medium celtic":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1147;
                break;

            case "leather medium leaf":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1160;
                break;
            case "metal medium leaf":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1161;
                break;
            case "wood medium leaf":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1162;
                break;

            case "hibernia dragonslayer medium":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 2 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3889;
                break;

            #endregion

            #region large shields
            case "leather large tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1058;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1061;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1064;
                        break;
                }
                break;
            case "metal large tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1059;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1062;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1065;
                        break;
                }
                break;
            case "wood large tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1060;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1063;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1066;
                        break;
                }
                break;

            case "leather large heater":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1067;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1070;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1073;
                        break;
                }
                break;
            case "metal large heater":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1068;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1071;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1074;
                        break;
                }
                break;
            case "wood large heater":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1069;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1072;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1075;
                        break;
                }
                break;

            case "leather large round":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1076;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1079;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1082;
                        break;
                }
                break;
            case "metal large round":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1077;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1080;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1083;
                        break;
                }
                break;
            case "wood large round":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = freebie;
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        modelIDToAssign = 1078;
                        break;
                    case eRealm.Midgard:
                        modelIDToAssign = 1081;
                        break;
                    case eRealm.Hibernia:
                        modelIDToAssign = 1084;
                        break;
                }
                break;

            case "dragonsworn large":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost;
                modelIDToAssign = 3830;
                break;

            case "oceanus large shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2194;
                break;
            case "aerus large shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2212;
                break;
            case "magma large shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2220;
                break;
            case "stygia large shield":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 2202;
                break;

            //albion specific        
            case "leather large horned":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1106;
                break;
            case "metal large horned":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1107;
                break;
            case "wood large horned":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1108;
                break;

            case "leather large kite":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1109;
                break;
            case "metal large kite":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1110;
                break;
            case "wood large kite":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 1 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 1111;
                break;

            case "leather studded tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1121;
                break;
            case "metal studded tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1122;
                break;
            case "wood studded tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1123;
                break;

            case "albion dragonslayer large":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Albion)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3967;
                break;

            //midgard specific
            case "leather large crescent":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1133;
                break;
            case "metal large crescent":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1134;
                break;
            case "wood large crescent":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1135;
                break;

            case "leather large grave":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1136;
                break;
            case "metal large grave":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1137;
                break;
            case "wood large grave":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1138;
                break;

            case "leather norse tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1142;
                break;
            case "metal norse tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1143;
                break;
            case "wood norse tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1144;
                break;

            case "midgard dragonslayer large":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Midgard)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3931;
                break;

            //hibernia specific
            case "leather celtic tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1151;
                break;
            case "metal celtic tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1152;
                break;
            case "wood celtic tower":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1153;
                break;

            case "leather large celtic":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1154;
                break;
            case "metal large celtic":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1155;
                break;
            case "wood large celtic":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1156;
                break;

            case "leather large leaf":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1157;
                break;
            case "metal large leaf":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1158;
                break;
            case "wood large leaf":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = lowbie;
                modelIDToAssign = 1159;
                break;

            case "hibernia dragonslayer large":
                if (item.Object_Type != (int)eObjectType.Shield || item.Type_Damage != 3 || player.Realm != eRealm.Hibernia)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills) < 25)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = dragonCost * 2;
                modelIDToAssign = 3890;
                break;

            #endregion


            case "aten's shield":
                if (item.Object_Type != (int)eObjectType.Shield)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1663;
                break;
            case "cyclop's eye":
                if (item.Object_Type != (int)eObjectType.Shield)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1664;
                break;
            case "shield of khaos":
                if (item.Object_Type != (int)eObjectType.Shield)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1665;
                break;
            case "oceanus shield":
                if (item.Object_Type != (int)eObjectType.Shield)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                if (item.Type_Damage == 1) //small shield
                {
                    modelIDToAssign = 2192;
                }
                else if (item.Type_Damage == 2)
                {
                    modelIDToAssign = 2193;
                }
                else if (item.Type_Damage == 3)
                {
                    modelIDToAssign = 2194;
                }

                break;
            case "aerus shield":
                if (item.Object_Type != (int)eObjectType.Shield)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                if (item.Type_Damage == 1) //small shield
                {
                    modelIDToAssign = 2210;
                }
                else if (item.Type_Damage == 2)
                {
                    modelIDToAssign = 2211;
                }
                else if (item.Type_Damage == 3)
                {
                    modelIDToAssign = 2212;
                }

                break;
            case "magma shield":
                if (item.Object_Type != (int)eObjectType.Shield)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                if (item.Type_Damage == 1) //small shield
                {
                    modelIDToAssign = 2218;
                }
                else if (item.Type_Damage == 2)
                {
                    modelIDToAssign = 2219;
                }
                else if (item.Type_Damage == 3)
                {
                    modelIDToAssign = 2220;
                }

                break;
            case "minotaur shield":
                if (item.Object_Type != (int)eObjectType.Shield)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 4)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3554;
                break;

            #endregion

            #region ranged weapons/instruments

            //case "dragonslayer harp": probably doesn't work
            //     break;
            case "class epic harp":
                if (item.Object_Type != (int)eObjectType.Instrument)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = epic;
                if ((eCharacterClass)player.CharacterClass.ID == eCharacterClass.Bard)
                {
                    modelIDToAssign = 3239;
                }
                else if ((eCharacterClass)player.CharacterClass.ID == eCharacterClass.Minstrel)
                {
                    modelIDToAssign = 3280;
                }
                else
                {
                    price = 0;
                }

                break;
            case "labyrinth harp":
                if (item.Object_Type != (int)eObjectType.Instrument)
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3688;
                break;
            case "class epic bow":
                if (item.Object_Type != (int)eObjectType.CompositeBow &&
                    item.Object_Type != (int)eObjectType.Longbow &&
                    item.Object_Type != (int)eObjectType.RecurvedBow
                   )
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 5)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = champion;
                if ((eCharacterClass)player.CharacterClass.ID == eCharacterClass.Scout)
                {
                    modelIDToAssign = 3275;
                }
                else if ((eCharacterClass)player.CharacterClass.ID == eCharacterClass.Hunter)
                {
                    modelIDToAssign = 3365;
                }
                else if ((eCharacterClass)player.CharacterClass.ID == eCharacterClass.Ranger)
                {
                    modelIDToAssign = 3243;
                }
                else
                {
                    price = 0;
                }

                break;
            case "fool's bow":
                if (item.Object_Type != (int)eObjectType.CompositeBow &&
                    item.Object_Type != (int)eObjectType.Longbow &&
                    item.Object_Type != (int)eObjectType.RecurvedBow &&
                    item.Object_Type != (int)eObjectType.Fired
                   )
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1666;
                break;
            case "braggart's bow":
                if (item.Object_Type != (int)eObjectType.CompositeBow &&
                    item.Object_Type != (int)eObjectType.Longbow &&
                    item.Object_Type != (int)eObjectType.RecurvedBow &&
                    item.Object_Type != (int)eObjectType.Fired
                   )
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 6)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1667;
                break;
            case "labyrinth bow":
                if (item.Object_Type != (int)eObjectType.CompositeBow &&
                    item.Object_Type != (int)eObjectType.Longbow &&
                    item.Object_Type != (int)eObjectType.RecurvedBow &&
                    item.Object_Type != (int)eObjectType.Fired
                   )
                {
                    SendNotValidMessage(player);
                    break;
                }

                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 10)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3706;
                break;

            #endregion

            #region Armor Pads

            case "armor pad":
                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                SendReply(player, "I can offer the following pad types: \n\n" +
                                  "[Type 1] \n" +
                                  "[Type 2] \n" +
                                  "[Type 3] \n" +
                                  "[Type 4] \n" +
                                  "[Type 5]"
                );
                return true;

            case "type 1":
                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = armorpads;
                modelIDToAssign = 1;
                break;
            case "type 2":
                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = armorpads;
                modelIDToAssign = 2;
                break;
            case "type 3":
                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = armorpads;
                modelIDToAssign = 3;
                break;
            case "type 4":
                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = armorpads;
                modelIDToAssign = 4;
                break;
            case "type 5":
                if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank) < 2)
                {
                    SendNotQualifiedMessage(player);
                    break;
                }

                price = armorpads;
                modelIDToAssign = 5;
                break;

                #endregion
        }

        //Console.WriteLine($"price {price} model {modelIDToAssign}");
        if (price == armorpads)
        {
            InventoryItem tmpItem = (InventoryItem)displayItem.Clone();
            byte tmp = tmpItem.Extension;
            tmpItem.Extension = (byte)modelIDToAssign;
            DisplayReskinPreviewTo(player, tmpItem);
            tmpItem.Extension = tmp;
        }
        else
        {
            InventoryItem tmpItem = (InventoryItem)displayItem.Clone();
            int tmp = tmpItem.Model;
            tmpItem.Model = modelIDToAssign;
            DisplayReskinPreviewTo(player, tmpItem);
            tmpItem.Model = tmp;
        }

        player.TempProperties.setProperty(TempModelID, modelIDToAssign);
        player.TempProperties.setProperty(TempModelPrice, price);

        return true;
    }

}