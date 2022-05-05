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
        }

        return base.ReceiveItem(source, item);
    }

    private void DisplayHelmOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        //add all basic options
        sb.Append($"Free\n" +
                  $"[Crafted 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 3] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted 5] ({lowbie} {currencyName})\n" +
                      $"[Crafted 6] ({lowbie} {currencyName})\n");
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
                  $"[Crafted 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 3] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted 5] ({lowbie} {currencyName})\n" +
                      $"[Crafted 6] ({lowbie} {currencyName})\n");
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
                      $"[Dragonslayer Breastplate] (" + dragonCost * 1.5 + " " + currencyName + ") | Catacombs Models Only\n");
        }

        if (player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills) >= 10)
        {
            sb.Append("10 Epic Boss Kills\n" +
                      "[Possessed Realm Breastplate](" + festive + " " + currencyName + ")\n | Catacombs Models Only\n");
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

        sb.Append("I can also offer you some [armor pad] (" + armorpads + " " + currencyName + ") options.");
        
        SendReply(player, sb.ToString());

    }
    
    public void DisplayArmsOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        //add all basic options
        sb.Append($"Free\n" +
                  $"[Crafted 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 3] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted 5] ({lowbie} {currencyName})\n" +
                      $"[Crafted 6] ({lowbie} {currencyName})\n");
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
                      $"[Dragonslayer Sleeves] (" + dragonCost * 1.5 + " " + currencyName + ") | Catacombs Models Only\n");
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

        sb.Append("I can also offer you some [armor pad] (" + armorpads + " " + currencyName + ") options.");
        
        SendReply(player, sb.ToString());

    }
    
    public void DisplayPantsOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        //add all basic options
        sb.Append($"Free\n" +
                  $"[Crafted 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 3] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted 5] ({lowbie} {currencyName})\n" +
                      $"[Crafted 6] ({lowbie} {currencyName})\n");
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
                      $"[Dragonslayer Pants] (" + dragonCost * 1.5 + " " + currencyName + ") | Catacombs Models Only\n");
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

        sb.Append("I can also offer you some [armor pad] (" + armorpads + " " + currencyName + ") options.");
        
        SendReply(player, sb.ToString());

    }
    
    public void DisplayGloveOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        //add all basic options
        sb.Append($"Free\n" +
                  $"[Crafted 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 3] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted 5] ({lowbie} {currencyName})\n" +
                      $"[Crafted 6] ({lowbie} {currencyName})\n");
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
                      $"[Dragonslayer Gloves] (" + dragonCost * 1.5 + " " + currencyName + ") | Catacombs Models Only\n");
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

        sb.Append("I can also offer you some [armor pad] (" + armorpads + " " + currencyName + ") options.");
        
        SendReply(player, sb.ToString());

    }
    
    public void DisplayBootsOption(GamePlayer player, InventoryItem item)
    {
        StringBuilder sb = new StringBuilder();
        int RR = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);

        //add all basic options
        sb.Append($"Free\n" +
                  $"[Crafted 1] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 2] ({freebie} {currencyName})\n");
        sb.Append($"[Crafted 3] ({freebie} {currencyName})\n");

        if (RR > 1)
        {
            sb.Append($"Realm Rank 2+\n" +
                      $"[Crafted 4] ({lowbie} {currencyName})\n" +
                      $"[Crafted 5] ({lowbie} {currencyName})\n" +
                      $"[Crafted 6] ({lowbie} {currencyName})\n");
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
                      $"[Dragonslayer Boots] (" + dragonCost * 1.5 + " " + currencyName + ") | Catacombs Models Only\n");
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

        sb.Append("I can also offer you some [armor pad] (" + armorpads + " " + currencyName + ") options.");
        
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
}