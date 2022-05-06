using System;
using System.Text;
using DOL.Database;
using DOL.GS.PacketHandler;

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
    private int freebie = 0;
    private int lowbie = 1000;
    private int festive = 2000;
    private int toageneric = 5000;
    private int armorpads = 2500;
    private int artifact = 10000;
    private int epic = 10000;
    private int dragonCost = 10000;
    private int champion = 20000;
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
                DisplayReskinPreviewTo(player, (InventoryItem) displayItem.Clone());

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

        DisplayReskinPreviewTo(t, item);

        switch (item.Item_Type)
        {
            case Slot.HELM:
                DisplayHelmOption(t, item);
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
        }

        SendReply(t, "When you are finished browsing, let me know and I will [confirm model]."
        );
        var tmp = (InventoryItem) item.Clone();
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

        if (item.Object_Type == (int) eObjectType.Cloth)
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
                      "[Class Epic Gloves](" + epic + " " + currencyName + ")\n");
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
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] {newInventoryItem});
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
            if (item.Item_Type != (int) eEquipmentItems.TORSO && item.Item_Type != (int) eEquipmentItems.HAND &&
                item.Item_Type != (int) eEquipmentItems.FEET)
            {
                SendReply(player, "I'm sorry, but I can only modify the pads on Torso, Hand, and Feet armors.");
                return;
            }


            player.Inventory.RemoveItem(item);
            ItemUnique unique = new ItemUnique(item.Template);
            unique.Extension = number;
            GameServer.Database.AddObject(unique);
            InventoryItem newInventoryItem = GameInventoryItem.Create(unique as ItemTemplate);
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new InventoryItem[] {newInventoryItem});
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
        ushort tmpModel = (ushort) (player.Model << 5);
        tmpModel = (ushort) (tmpModel >> 5);

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
        tempAd.AttackType = AttackData.eAttackType.MeleeOneHand;
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
                        SetExtension(player, (byte) cachedModelID, cachedModelPrice);
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                if (item.Item_Type != Slot.HELM || item.Object_Type != (int) eObjectType.Cloth)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eCharacterClass) player.CharacterClass.ID)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eCharacterClass) player.CharacterClass.ID)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eObjectType) item.Object_Type)
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
                switch ((eRealm) player.Realm)
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
                switch ((eRealm) player.Realm)
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

                price = cloakcheap;
                modelIDToAssign = 669;
                break;

            #endregion

            #region weapons

            #region 1h wep

            case "traitor's dagger 1h":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int) eDamageType.Thrust)
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
                    item.Type_Damage != (int) eDamageType.Slash)
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
                    item.Type_Damage != (int) eDamageType.Thrust)
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
                    item.Type_Damage != (int) eDamageType.Slash)
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
                    item.Type_Damage != (int) eDamageType.Thrust)
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
                    item.Type_Damage != (int) eDamageType.Slash)
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
                    item.Type_Damage != (int) eDamageType.Crush)
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
                    item.Type_Damage != (int) eDamageType.Crush)
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
                    item.Type_Damage != (int) eDamageType.Crush)
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
                    item.Type_Damage != (int) eDamageType.Slash)
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
                    item.Type_Damage != (int) eDamageType.Crush)
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
                    item.Type_Damage == (int) eDamageType.Crush)
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
                    item.Type_Damage != (int) eDamageType.Crush)
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
            case "wakazashi":
                if (item.Item_Type != Slot.RIGHTHAND &&
                    item.Item_Type != Slot.LEFTHAND &&
                    item.Type_Damage != (int) eDamageType.Thrust &&
                    item.Type_Damage != (int) eDamageType.Slash)
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
                    item.Type_Damage != (int) eDamageType.Crush)
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
                    item.Type_Damage != (int) eDamageType.Slash)
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
            case "khopesh":
                if ((item.Item_Type != Slot.RIGHTHAND &&
                     item.Item_Type != Slot.LEFTHAND) &&
                    item.Type_Damage != (int) eDamageType.Slash)
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
                    item.Type_Damage != (int) eDamageType.Crush)
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
                    item.Type_Damage != (int) eDamageType.Crush)
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

            //hand to hand
            case "snakecharmer's fist":
                if (item.Object_Type != (int) eObjectType.HandToHand)
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
                if (item.Object_Type != (int) eObjectType.HandToHand)
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
                switch ((eDamageType) item.Type_Damage)
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
                if (item.Object_Type != (int) eObjectType.HandToHand)
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
                switch ((eDamageType) item.Type_Damage)
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
                if (item.Object_Type != (int) eObjectType.Flexible)
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
                if (item.Object_Type != (int) eObjectType.Flexible)
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
                switch ((eDamageType) item.Type_Damage)
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
                if (item.Object_Type != (int) eObjectType.Flexible)
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
                switch ((eDamageType) item.Type_Damage)
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

            #region 2h wep

            case "pickaxe":
                if (item.Item_Type != Slot.TWOHAND ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon)
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
                    item.Type_Damage != (int) eDamageType.Slash ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon)
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
                    item.Type_Damage != (int) eDamageType.Slash ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon)
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
                    item.Type_Damage != (int) eDamageType.Slash ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon)
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
                if (item.Object_Type != (int) eObjectType.Spear &&
                    item.Object_Type != (int) eObjectType.CelticSpear)
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
                if (item.Object_Type != (int) eObjectType.Spear &&
                    item.Object_Type != (int) eObjectType.CelticSpear)
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
                if (item.Object_Type != (int) eObjectType.Spear &&
                    item.Object_Type != (int) eObjectType.CelticSpear)
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
                if (item.Object_Type != (int) eObjectType.Spear &&
                    item.Object_Type != (int) eObjectType.CelticSpear)
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
                    item.Type_Damage != (int) eDamageType.Crush ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe)
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
                    item.Type_Damage != (int) eDamageType.Crush ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe)
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
                    item.Type_Damage != (int) eDamageType.Crush ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe)
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
                    item.Type_Damage != (int) eDamageType.Crush ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe)
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
                    item.Type_Damage != (int) eDamageType.Crush ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe)
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
                    item.Type_Damage != (int) eDamageType.Slash ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe)
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
                    item.Type_Damage != (int) eDamageType.Slash ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe)
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
                    item.Type_Damage != (int) eDamageType.Slash ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe)
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
                    item.Type_Damage != (int) eDamageType.Slash ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe)
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
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe ||
                    item.Type_Damage == (int) eDamageType.Crush)
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
                    item.Type_Damage != (int) eDamageType.Thrust ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe)
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
                    item.Type_Damage != (int) eDamageType.Thrust ||
                    item.Object_Type == (int) eObjectType.PolearmWeapon ||
                    item.Object_Type == (int) eObjectType.Spear ||
                    item.Object_Type == (int) eObjectType.CelticSpear ||
                    item.Object_Type == (int) eObjectType.Scythe)
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
                if (item.Object_Type != (int) eObjectType.Staff)
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
                if (item.Object_Type != (int) eObjectType.Staff)
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
                if (item.Object_Type != (int) eObjectType.Staff)
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
                if (item.Object_Type != (int) eObjectType.Staff)
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
                if (item.Object_Type != (int) eObjectType.Staff)
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
                if (item.Object_Type != (int) eObjectType.Scythe)
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
                if (item.Object_Type != (int) eObjectType.Scythe)
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
                if (item.Object_Type != (int) eObjectType.Scythe)
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
                if (item.Object_Type != (int) eObjectType.Scythe)
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
                if (item.Object_Type != (int) eObjectType.Scythe)
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
                if (item.Object_Type != (int) eObjectType.PolearmWeapon)
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
                switch ((eDamageType) item.Type_Damage)
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
                if (item.Object_Type != (int) eObjectType.PolearmWeapon)
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
                if (item.Object_Type != (int) eObjectType.PolearmWeapon)
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
                switch ((eDamageType) item.Type_Damage)
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
                if (item.Object_Type != (int) eObjectType.PolearmWeapon)
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
                switch ((eCharacterClass) player.CharacterClass.ID)
                {
                    //alb
                    case eCharacterClass.Armsman:
                        if (item.Object_Type == (int) eObjectType.Shield)
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

                        switch ((eDamageType) item.Type_Damage)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                        if (item.Object_Type != (int) eObjectType.ThrustWeapon ||
                            item.Object_Type != (int) eObjectType.SlashingWeapon)
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

                        switch ((eDamageType) item.Type_Damage)
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
                        if (item.Object_Type != (int) eObjectType.ThrustWeapon ||
                            item.Object_Type != (int) eObjectType.SlashingWeapon ||
                            item.Object_Type != (int) eObjectType.CrushingWeapon)
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

                        switch ((eDamageType) item.Type_Damage)
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
                        if (item.Object_Type != (int) eObjectType.ThrustWeapon ||
                            item.Object_Type != (int) eObjectType.SlashingWeapon)
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

                        switch ((eDamageType) item.Type_Damage)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                        if (item.Object_Type != (int) eObjectType.ThrustWeapon ||
                            item.Object_Type != (int) eObjectType.SlashingWeapon ||
                            item.Object_Type != (int) eObjectType.CrushingWeapon)
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

                        switch ((eDamageType) item.Type_Damage)
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
                        if ((eObjectType) item.Object_Type == eObjectType.Flexible)
                        {
                            modelIDToAssign = 3292;
                        }
                        else
                        {
                            if (item.Object_Type != (int) eObjectType.ThrustWeapon ||
                                item.Object_Type != (int) eObjectType.SlashingWeapon ||
                                item.Object_Type != (int) eObjectType.CrushingWeapon)
                            {
                                SendNotValidMessage(player);
                                price = 0;
                                break;
                            }

                            switch ((eDamageType) item.Type_Damage)
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
                        if (item.Object_Type != (int) eObjectType.ThrustWeapon ||
                            item.Object_Type != (int) eObjectType.SlashingWeapon)
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

                        switch ((eDamageType) item.Type_Damage)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                            item.Object_Type != (int) eObjectType.Hammer)
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
                        
                        switch ((eObjectType) item.Object_Type)
                        {
                            case eObjectType.Spear:
                                if ((eDamageType) item.Type_Damage == eDamageType.Thrust)
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
                        switch ((eObjectType) item.Object_Type)
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
                        switch ((eObjectType) item.Object_Type)
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
                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                        switch ((eObjectType) item.Object_Type)
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
                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                        if (item.Object_Type != (int) eObjectType.Blades &&
                            item.Object_Type != (int) eObjectType.Blunt)
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

                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Blades &&
                            item.Object_Type != (int) eObjectType.Blunt &&
                            item.Object_Type != (int) eObjectType.Piercing)
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

                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Blades &&
                            item.Object_Type != (int) eObjectType.Blunt &&
                            item.Object_Type != (int) eObjectType.Piercing)
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

                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Blades &&
                            item.Object_Type != (int) eObjectType.Blunt)
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

                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                        if (item.Object_Type != (int) eObjectType.Blades &&
                            item.Object_Type != (int) eObjectType.Blunt &&
                            item.Object_Type != (int) eObjectType.Piercing)
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
                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
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
                        if (item.Object_Type != (int) eObjectType.Blades &&
                            item.Object_Type != (int) eObjectType.Piercing)
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

                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Blades &&
                            item.Object_Type != (int) eObjectType.Piercing)
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

                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Scythe)
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
                        if (item.Object_Type != (int) eObjectType.Blades &&
                            item.Object_Type != (int) eObjectType.Blunt)
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

                        switch ((eObjectType) item.Object_Type)
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
                switch ((eCharacterClass) player.CharacterClass.ID)
                {
                    //alb
                    case eCharacterClass.Armsman:
                        if ((eObjectType) item.Object_Type == eObjectType.PolearmWeapon)
                        {
                            switch ((eDamageType) item.Type_Damage)
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
                            switch ((eDamageType) item.Type_Damage)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3264;
                        break;
                    case eCharacterClass.Cleric:
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3282;
                        break;
                    case eCharacterClass.Friar:
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3271;
                        break;
                    case eCharacterClass.Necromancer:
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3268;
                        break;
                    case eCharacterClass.Paladin:
                        switch ((eDamageType) item.Type_Damage)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3265;
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        break;
                    case eCharacterClass.Theurgist:
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3266;
                        break;
                    case eCharacterClass.Wizard:
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3267;
                        break;

                    //mid
                    case eCharacterClass.Berserker:
                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3311;
                        break;
                    case eCharacterClass.Healer:
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3335;
                        break;
                    case eCharacterClass.Hunter:
                        switch ((eObjectType) item.Object_Type)
                        {
                            case eObjectType.Spear:
                                if ((eDamageType) item.Type_Damage == eDamageType.Thrust)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3309;
                        break;
                    case eCharacterClass.Savage:
                        switch ((eObjectType) item.Object_Type)
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
                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Hammer)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3338;
                        break;
                    case eCharacterClass.Skald:
                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3310;
                        break;
                    case eCharacterClass.Thane:
                        switch ((eObjectType) item.Object_Type)
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
                        switch ((eObjectType) item.Object_Type)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3229;
                        break;
                    case eCharacterClass.Champion:
                        switch ((eDamageType) item.Type_Damage)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3226;
                        break;
                    case eCharacterClass.Enchanter:
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3227;
                        break;
                    case eCharacterClass.Hero:
                        switch ((eObjectType) item.Object_Type)
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
                                switch ((eDamageType) item.Type_Damage)
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
                        if (item.Object_Type != (int) eObjectType.Staff)
                        {
                            SendNotValidMessage(player);
                            price = 0;
                            break;
                        }

                        modelIDToAssign = 3228;
                        break;
                    case eCharacterClass.Valewalker:
                        if (item.Object_Type != (int) eObjectType.Scythe)
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

            case "aten's shield":
                if (item.Object_Type != (int) eObjectType.Shield)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1663;
                break;
            case "cyclop's eye":
                if (item.Object_Type != (int) eObjectType.Shield)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1664;
                break;
            case "shield of khaos":
                if (item.Object_Type != (int) eObjectType.Shield)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1665;
                break;
            case "oceanus shield":
                if (item.Object_Type != (int) eObjectType.Shield)
                {
                    SendNotValidMessage(player);
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
                if (item.Object_Type != (int) eObjectType.Shield)
                {
                    SendNotValidMessage(player);
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
                if (item.Object_Type != (int) eObjectType.Shield)
                {
                    SendNotValidMessage(player);
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
                if (item.Object_Type != (int) eObjectType.Shield)
                {
                    SendNotValidMessage(player);
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
                if (item.Object_Type != (int) eObjectType.Instrument)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = epic;
                if ((eCharacterClass) player.CharacterClass.ID == eCharacterClass.Bard)
                {
                    modelIDToAssign = 3239;
                }
                else if ((eCharacterClass) player.CharacterClass.ID == eCharacterClass.Minstrel)
                {
                    modelIDToAssign = 3280;
                }
                else
                {
                    price = 0;
                }

                break;
            case "labyrinth harp":
                if (item.Object_Type != (int) eObjectType.Instrument)
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3688;
                break;
            case "class epic bow":
                if (item.Object_Type != (int) eObjectType.CompositeBow &&
                    item.Object_Type != (int) eObjectType.Longbow &&
                    item.Object_Type != (int) eObjectType.RecurvedBow
                   )
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = champion;
                if ((eCharacterClass) player.CharacterClass.ID == eCharacterClass.Scout)
                {
                    modelIDToAssign = 3275;
                }
                else if ((eCharacterClass) player.CharacterClass.ID == eCharacterClass.Hunter)
                {
                    modelIDToAssign = 3365;
                }
                else if ((eCharacterClass) player.CharacterClass.ID == eCharacterClass.Ranger)
                {
                    modelIDToAssign = 3243;
                }
                else
                {
                    price = 0;
                }

                break;
            case "fool's bow":
                if (item.Object_Type != (int) eObjectType.CompositeBow &&
                    item.Object_Type != (int) eObjectType.Longbow &&
                    item.Object_Type != (int) eObjectType.RecurvedBow &&
                    item.Object_Type != (int) eObjectType.Fired
                   )
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1666;
                break;
            case "braggart's bow":
                if (item.Object_Type != (int) eObjectType.CompositeBow &&
                    item.Object_Type != (int) eObjectType.Longbow &&
                    item.Object_Type != (int) eObjectType.RecurvedBow &&
                    item.Object_Type != (int) eObjectType.Fired
                   )
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = artifact;
                modelIDToAssign = 1667;
                break;
            case "labyrinth bow":
                if (item.Object_Type != (int) eObjectType.CompositeBow &&
                    item.Object_Type != (int) eObjectType.Longbow &&
                    item.Object_Type != (int) eObjectType.RecurvedBow &&
                    item.Object_Type != (int) eObjectType.Fired
                   )
                {
                    SendNotValidMessage(player);
                    break;
                }

                price = toageneric;
                modelIDToAssign = 3706;
                break;

            #endregion

            #region Armor Pads

            case "armor pad":
                SendReply(player, "I can offer the following pad types: \n\n" +
                                  "[Type 1] \n" +
                                  "[Type 2] \n" +
                                  "[Type 3] \n" +
                                  "[Type 4] \n" +
                                  "[Type 5]"
                );
                return true;

            case "type 1":
                price = armorpads;
                modelIDToAssign = 1;
                break;
            case "type 2":
                price = armorpads;
                modelIDToAssign = 2;
                break;
            case "type 3":
                price = armorpads;
                modelIDToAssign = 3;
                break;
            case "type 4":
                price = armorpads;
                modelIDToAssign = 4;
                break;
            case "type 5":
                price = armorpads;
                modelIDToAssign = 5;
                break;

            #endregion
        }

        //Console.WriteLine($"price {price} model {modelIDToAssign}");
        if (price == armorpads)
        {
            InventoryItem tmpItem = (InventoryItem) displayItem.Clone();
            byte tmp = tmpItem.Extension;
            tmpItem.Extension = (byte) modelIDToAssign;
            DisplayReskinPreviewTo(player, tmpItem);
            tmpItem.Extension = tmp;
        }
        else
        {
            InventoryItem tmpItem = (InventoryItem) displayItem.Clone();
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