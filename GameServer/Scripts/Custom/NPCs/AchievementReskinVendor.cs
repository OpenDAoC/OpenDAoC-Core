using System;
using System.Collections.Generic;
using System.Linq;
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
    
    private string _currencyID = ServerProperties.Properties.ALT_CURRENCY_ID;

    private int Chance;
    private Random rnd = new Random();
    private List<DbSkinVendorItem> VendorItemList = new List<DbSkinVendorItem>();

    public override bool AddToWorld()
    {
        VendorItemService service = new VendorItemService();

        List<DbSkinVendorItem> itemList = service.createVendorItems();

        this.VendorItemList = itemList;
        base.AddToWorld();
        return true;
    }

    public override bool Interact(GamePlayer player)
    {
        if (base.Interact(player))
        {
            TurnTo(player, 500);
            DbInventoryItem item = player.TempProperties.GetProperty<DbInventoryItem>(TempProperty);
            DbInventoryItem displayItem = player.TempProperties.GetProperty<DbInventoryItem>(DisplayedItem);

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
                DisplayReskinPreviewTo(player, (DbInventoryItem)displayItem.Clone());

            return true;
        }

        return false;
    }

    public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
    {
        GamePlayer t = source as GamePlayer;
        if (t == null || item == null || item.Template.Name.Equals(_currencyID)) return false;
        if (GetDistanceTo(t) > WorldMgr.INTERACT_DISTANCE)
        {
            t.Out.SendMessage("You are too far away to give anything to " + GetName(0, false) + ".",
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
            return false;
        }
        StringBuilder sb = new StringBuilder();

        DisplayReskinPreviewTo(t, item);

        int playerRealm = (int)t.Realm;
        int noneRealm = (int)eRealm.None;
        int damageType = (int)(eDamageType)item.Type_Damage;
        int characterClassUnknown = (int)eCharacterClass.Unknown;
        int playerClass = (int)(eCharacterClass)t.CharacterClass.ID;
        int playerRealmRank = (int)Math.Floor((double)(t.RealmLevel + 10.0) / 10.0);
        int accountRealmRank = t.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);
        int playerDragonKills = t.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);
        int playerOrbs = t.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned);
        int epicBossPlayerKills = t.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills);
        int masteredCrafts = t.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts);
        int itemSlot = item.Item_Type;
        bool isGM = t.Client.Account.PrivLevel != 1;

        //Console.Write("Item Type is" + item.Item_Type + "damagetype is" + damageType + "objectType is " + item.Object_Type);

        List<DbSkinVendorItem> foundItems = null;

        if (item.Item_Type == Slot.RIGHTHAND || item.Item_Type == Slot.LEFTHAND)
        {
            foundItems = FindAllAvailableOneHandedOptions(item, playerRealm, noneRealm, damageType, characterClassUnknown, playerClass, playerRealmRank, accountRealmRank, playerDragonKills, playerOrbs, epicBossPlayerKills, masteredCrafts, isGM);
        }
        else if(item.Item_Type == Slot.RANGED || item.Object_Type == (int)eObjectType.Instrument)
        {
            foundItems = FindAllAvailableOptionsInstruments(item, playerRealm, noneRealm, damageType, characterClassUnknown, playerClass, playerRealmRank, accountRealmRank, playerDragonKills, playerOrbs, epicBossPlayerKills, masteredCrafts, isGM);
        }else
        {
            foundItems = FindAllAvailableOtherItemOptions(item, playerRealm, noneRealm, damageType, characterClassUnknown, playerClass, playerRealmRank, accountRealmRank, playerDragonKills, playerOrbs, epicBossPlayerKills, masteredCrafts, isGM);
        }

        if (foundItems.Count == 0)
        {
            SendReply(t, "There are no skinoptions available for this item at the moment, please check back in a few days !");
            return true;
        }


        foreach (var sItem in foundItems)
        {
            sb.Append($"[{sItem.Name}] ({sItem.Price} {currencyName})\n");
        }


        if (item.Item_Type == Slot.FEET || item.Item_Type == Slot.TORSO || item.Item_Type == Slot.HANDS)
        {
            SendReply(t, sb.ToString() + "\n\n" + "I can offer the following pad types: \n\n" +
                               "[Type 1] (2500 Orbs)  \n" +
                               "[Type 2] (2500 Orbs) \n" +
                               "[Type 3] (2500 Orbs) \n" +
                               "[Type 4] (2500 Orbs) \n" +
                               "[Type 5] (2500 Orbs)");
        }
        else
        {
            SendReply(t, sb.ToString());
        }

        SendReply(t, "When you are finished browsing, let me know and I will [confirm model]."
        );
        var tmp = (DbInventoryItem)item.Clone();
        t.TempProperties.SetProperty(TempProperty, item);
        t.TempProperties.SetProperty(DisplayedItem, tmp);

        return false;
    }



    private List<DbSkinVendorItem> FindAllAvailableOptionsInstruments(DbInventoryItem item, int playerRealm, int noneRealm, int damageType, int characterClassUnknown, int playerClass, int playerRealmRank, int accountRealmRank, int playerDragonKills, int playerOrbs, int epicBossPlayerKills, int masteredCrafts, bool isGm)
    {
        if (isGm)
        {
            return VendorItemList.FindAll(x => (x.ItemType == item.Item_Type)
             && (x.Realm == playerRealm || x.Realm == noneRealm)
             && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
             && x.ObjectType == item.Object_Type
             && x.Price != 2500).OrderBy(o => o.Price).ToList();
        }
        else
        {
            return VendorItemList.FindAll(x => (x.ItemType == item.Item_Type)
                         && (x.Realm == playerRealm || x.Realm == noneRealm)
                         && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
                         && x.PlayerRealmRank <= playerRealmRank
                         && x.AccountRealmRank <= accountRealmRank
                         && x.Orbs <= playerOrbs
                         && x.Drake <= playerDragonKills
                         && x.EpicBossKills <= epicBossPlayerKills
                         && x.MasteredCrafts <= masteredCrafts
                         && x.ObjectType == item.Object_Type
                         && x.Price != 2500).OrderBy(o => o.Price).ToList();
        }
    }

    private List<DbSkinVendorItem> FindAllAvailableOtherItemOptions(DbInventoryItem item, int playerRealm, int noneRealm, int damageType, int characterClassUnknown, int playerClass, int playerRealmRank, int accountRealmRank, int playerDragonKills, int playerOrbs, int epicBossPlayerKills, int masteredCrafts, bool isGm)
    {
        if (isGm)
        {
            return VendorItemList.FindAll(x => (x.ItemType == item.Item_Type)
             && (x.Realm == playerRealm || x.Realm == noneRealm)
             && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
             && x.DamageType == damageType
             && x.ObjectType == item.Object_Type
             && x.Price != 2500).OrderBy(o => o.Price).ToList();
        }
        else
        {
            return VendorItemList.FindAll(x => (x.ItemType == item.Item_Type)
                         && (x.Realm == playerRealm || x.Realm == noneRealm)
                         && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
                         && x.PlayerRealmRank <= playerRealmRank
                         && x.AccountRealmRank <= accountRealmRank
                         && x.Orbs <= playerOrbs
                         && x.Drake <= playerDragonKills
                         && x.EpicBossKills <= epicBossPlayerKills
                         && x.MasteredCrafts <= masteredCrafts
                         && x.DamageType == damageType
                         && x.ObjectType == item.Object_Type
                         && x.Price != 2500).OrderBy(o => o.Price).ToList();
        }
    }

    private List<DbSkinVendorItem> FindAllAvailableOneHandedOptions(DbInventoryItem item, int playerRealm, int noneRealm, int damageType, int characterClassUnknown, int playerClass, int playerRealmRank, int accountRealmRank, int playerDragonKills, int playerOrbs, int epicBossPlayerKills, int masteredCrafts,bool isGm)
    {
        if (isGm)
        {
            return VendorItemList.FindAll(x => (x.ItemType == item.Item_Type || x.ItemType == Slot.RIGHTHAND)
                         && (x.Realm == playerRealm || x.Realm == noneRealm)
                         && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
                         && x.DamageType == damageType
                         && x.ObjectType == item.Object_Type
                         && x.Price != 2500).OrderBy(o => o.Price).ToList();
        }
        else
        {
            return VendorItemList.FindAll(x => (x.ItemType == item.Item_Type || x.ItemType == Slot.RIGHTHAND)
                         && (x.Realm == playerRealm || x.Realm == noneRealm)
                         && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
                         && x.PlayerRealmRank <= playerRealmRank
                         && x.AccountRealmRank <= accountRealmRank
                         && x.Orbs <= playerOrbs
                         && x.Drake <= playerDragonKills
                         && x.EpicBossKills <= epicBossPlayerKills
                         && x.MasteredCrafts <= masteredCrafts
                         && x.DamageType == damageType
                         && x.ObjectType == item.Object_Type
                         && x.Price != 2500).OrderBy(o => o.Price).ToList();
        }
    }
    
    private List<DbSkinVendorItem> GetCharacterLockedSkinIDs()
    {
        return VendorItemList.FindAll(x => x.PlayerRealmRank is >= 10 or 5);
    }

    private List<DbSkinVendorItem> GetAccountLockedSkinIDs()
    {
        return VendorItemList.FindAll(x => x.AccountRealmRank is >= 10 || x.Drake >= 25 || x.EpicBossKills >= 25);
    }
    
    public bool SetModel(GamePlayer player, int number, int price)
    {
        if (price > 0)
        {
            int playerOrbs = player.Inventory.CountItemTemplate(_currencyID, eInventorySlot.FirstBackpack,
                eInventorySlot.LastBackpack);
            log.Info("Player Orbs:" + playerOrbs);

            if (playerOrbs < price)
            {
                SendReply(player, "I'm sorry, but you cannot afford my services currently.");
                return false;
            }

            eInventorySlot slot = player.Inventory.FindFirstEmptySlot(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

            if (slot == eInventorySlot.Invalid)
            {
                SendReply(player, "I'm sorry, but you currently have no available inventory space for this Item.");
                return false;
            }

            DbInventoryItem item = player.TempProperties.GetProperty<DbInventoryItem>(TempProperty);
            DbInventoryItem displayItem = player.TempProperties.GetProperty<DbInventoryItem>(DisplayedItem);

            DbInventoryItem foundItem = player.Inventory.GetFirstItemByID(item.Id_nb, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

            if (foundItem == null)
            {
                SendReply(player, "I'm sorry, the Item you want to Skin is not in your Inventory.");
                return false;
            }

            if (item == null || item.OwnerID != player.InternalID || item.OwnerID == null)
                return false;

            SendReply(player, "Thanks for your donation. " +
                          "I have changed your item's model, you can now use it. \n\n" +
                          "I look forward to doing business with you in the future.");

            player.TempProperties.RemoveProperty(TempProperty);
            player.TempProperties.RemoveProperty(DisplayedItem);
            player.TempProperties.RemoveProperty(TempModelID);

            //Console.WriteLine($"item model: {item.Model} assignment {number}");
            player.Inventory.RemoveItem(item);
            DbItemUnique unique = new DbItemUnique(item.Template);
            unique.Model = number;
            //item.IsTradable = false;
            //item.IsDropable = false;
            
            /*
            if (GetCharacterLockedSkinIDs().FirstOrDefault(x => x.ModelID == unique.Model) != null)
            {
                unique.IsTradable = false; //no trading/selling
                unique.IsDropable = false; //no account vault
            }*/
            if (GetAccountLockedSkinIDs().FirstOrDefault(x => x.ModelID == unique.Model) != null || GetCharacterLockedSkinIDs().FirstOrDefault(x => x.ModelID == unique.Model) != null)
            {
                unique.IsTradable = false; //no trading/selling
            }

            GameServer.Database.AddObject(unique);
            //Console.WriteLine($"unique model: {unique.Model} assignment {number}");
            DbInventoryItem newInventoryItem = GameInventoryItem.Create(unique as DbItemTemplate);
            if (item.IsCrafted)
                newInventoryItem.IsCrafted = true;
            if (item.Creator != "")
                newInventoryItem.Creator = item.Creator;
            newInventoryItem.Count = 1;

            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new DbInventoryItem[] { newInventoryItem });
            // player.RemoveBountyPoints(300);
            //player.RealmPoints -= price;
            //player.RespecRealm();
            //SetRealmLevel(player, (int)player.RealmPoints);
            player.Inventory.RemoveTemplate(_currencyID, price, eInventorySlot.FirstBackpack,
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
            int playerOrbs = player.Inventory.CountItemTemplate(_currencyID, eInventorySlot.FirstBackpack,
                eInventorySlot.LastBackpack);
            //log.Info("Player Orbs:" + playerOrbs);

            if (playerOrbs < price)
            {
                SendReply(player, "I'm sorry, but you cannot afford my services currently.");
                return;
            }

            eInventorySlot slot = player.Inventory.FindFirstEmptySlot(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

            if (slot == eInventorySlot.Invalid)
            {
                SendReply(player, "I'm sorry, but you currently have no available inventoryspace for this Item.");
                return;
            }
            DbInventoryItem item = player.TempProperties.GetProperty<DbInventoryItem>(TempProperty);
            DbInventoryItem displayItem = player.TempProperties.GetProperty<DbInventoryItem>(DisplayedItem);

            DbInventoryItem foundItem = player.Inventory.GetFirstItemByID(item.Id_nb, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

            if (foundItem == null)
            {
                SendReply(player, "I'm sorry, the Item you want to Skin is not in your Inventory.");
                return;
            }


            if (item == null || item.OwnerID != player.InternalID || item.OwnerID == null)
                return;

            player.TempProperties.RemoveProperty(TempProperty);
            player.TempProperties.RemoveProperty(DisplayedItem);

            //only allow pads on valid slots: torso/hand/feet
            if (item.Item_Type != (int)eEquipmentItems.TORSO && item.Item_Type != (int)eEquipmentItems.HAND &&
                item.Item_Type != (int)eEquipmentItems.FEET)
            {
                SendReply(player, "I'm sorry, but I can only modify the pads on Torso, Hand, and Feet armors.");
                return;
            }


            player.Inventory.RemoveItem(item);
            DbItemUnique unique = new DbItemUnique(item.Template);
            unique.Extension = number;
            GameServer.Database.AddObject(unique);
            DbInventoryItem newInventoryItem = GameInventoryItem.Create(unique as DbItemTemplate);
            if (item.IsCrafted)
                newInventoryItem.IsCrafted = true;
            if (item.Creator != "")
                newInventoryItem.Creator = item.Creator;
            newInventoryItem.Count = 1;
            player.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, newInventoryItem);
            player.Out.SendInventoryItemsUpdate(new DbInventoryItem[] { newInventoryItem });
            player.Out.SendInventoryItemsUpdate(new DbInventoryItem[] { newInventoryItem });
            // player.RemoveBountyPoints(300);
            //player.RealmPoints -= price;
            //player.RespecRealm();
            //SetRealmLevel(player, (int)player.RealmPoints);
            player.Inventory.RemoveTemplate(_currencyID, price, eInventorySlot.FirstBackpack,
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

    private GameNPC CreateDisplayNPC(GamePlayer player, DbInventoryItem item)
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

    private void DisplayReskinPreviewTo(GamePlayer player, DbInventoryItem item)
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
        ClientService.UpdateObjectForPlayer(player, display);

        //Uncomment this if you want animations
        // var animationThread = new Thread(() => LoopAnimation(player,item, display,tempAd));
        // animationThread.IsBackground = true;
        // animationThread.Start();
    }

    private void LoopAnimation(GamePlayer player, DbInventoryItem item, GameNPC display, AttackData ad)
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
        DbInventoryItem item = player.TempProperties.GetProperty<DbInventoryItem>(TempProperty);
        DbInventoryItem displayItem = player.TempProperties.GetProperty<DbInventoryItem>(DisplayedItem);
        int cachedModelID = player.TempProperties.GetProperty<int>(TempModelID);
        int cachedModelPrice = player.TempProperties.GetProperty<int>(TempModelPrice);

        int playerRealm = (int)player.Realm;
        int noneRealm = (int)eRealm.None;
        int damageType = (int)(eDamageType)item.Type_Damage;
        int characterClassUnknown = (int)eCharacterClass.Unknown;
        int playerClass = (int)(eCharacterClass)player.CharacterClass.ID;
        int playerRealmRank = (int)Math.Floor((double)(player.RealmLevel + 10.0) / 10.0);
        int accountRealmRank = player.GetAchievementProgress(AchievementUtils.AchievementNames.Realm_Rank);
        int playerDragonKills = player.GetAchievementProgress(AchievementUtils.AchievementNames.Dragon_Kills);
        int playerOrbs = player.GetAchievementProgress(AchievementUtils.AchievementNames.Orbs_Earned);
        int epicBossPlayerKills = player.GetAchievementProgress(AchievementUtils.AchievementNames.Epic_Boss_Kills);
        int masteredCrafts = player.GetAchievementProgress(AchievementUtils.AchievementNames.Mastered_Crafts);
        bool isGM = player.Client.Account.PrivLevel != 1;


        int itemSlot = item.Item_Type;

        if (item == null)
        {
            SendReply(player, "I need an item to work on!");
            return false;
        }

        DbSkinVendorItem foundItem = null;

        if (item.Item_Type == Slot.RIGHTHAND || item.Item_Type == Slot.LEFTHAND)
        {
            foundItem = FindChoosenOptionOneHanded(str, item, playerRealm, noneRealm, damageType, characterClassUnknown, playerClass, playerRealmRank, accountRealmRank, playerDragonKills, playerOrbs, epicBossPlayerKills, masteredCrafts, isGM);
        }
        else if(item.Item_Type == Slot.RANGED && item.Object_Type == (int) eObjectType.Instrument)
        {
            foundItem = FindChoosenOptionInstrument(str, item, playerRealm, noneRealm, damageType, characterClassUnknown, playerClass, playerRealmRank, accountRealmRank, playerDragonKills, playerOrbs, epicBossPlayerKills, masteredCrafts, isGM);
        }
        else
        {
            foundItem = FindChoosenOptionOtherItems(str, item, playerRealm, noneRealm, damageType, characterClassUnknown, playerClass, playerRealmRank, accountRealmRank, playerDragonKills, playerOrbs, epicBossPlayerKills, masteredCrafts, isGM);
        }

        Console.Write("Item Type is" + item.Item_Type + "name is" + str + "damagetype is" + damageType + "objectType is " + item.Object_Type);

        switch (str.ToLower())
        {
            case "confirm model":


                if (item.Item_Type == Slot.RIGHTHAND || item.Item_Type == Slot.LEFTHAND)
                {
                    foundItem = FindConfirmedOneHanded(item, cachedModelID, playerRealm, noneRealm, damageType, characterClassUnknown, playerClass, playerRealmRank, accountRealmRank, playerDragonKills, playerOrbs, epicBossPlayerKills, masteredCrafts, isGM);
                }
                else if(item.Item_Type == Slot.RANGED && item.Object_Type == (int)eObjectType.Instrument)
                {
                    foundItem = FindConfirmedInstruments(item, cachedModelID, playerRealm, noneRealm, damageType, characterClassUnknown, playerClass, playerRealmRank, accountRealmRank, playerDragonKills, playerOrbs, epicBossPlayerKills, masteredCrafts, isGM);
                }else
                {
                    foundItem = FindConfirmedOtherItems(item, cachedModelID, playerRealm, noneRealm, damageType, characterClassUnknown, playerClass, playerRealmRank, accountRealmRank, playerDragonKills, playerOrbs, epicBossPlayerKills, masteredCrafts, isGM);
                }


                //Console.WriteLine($"Cached: {cachedModelID}");
                if (cachedModelID > 0 && cachedModelPrice > 0 && foundItem != null)
                {
                    if (cachedModelPrice == 2500)
                        SetExtension(player, (byte)cachedModelID, cachedModelPrice);
                    else
                    {
                        //if restricted, use a pop-up
                        if (GetCharacterLockedSkinIDs().FirstOrDefault(x => x.ModelID == cachedModelID) != null || GetAccountLockedSkinIDs().FirstOrDefault(x => x.ModelID == cachedModelID) != null)
                        {
                            //come back to me
                            player.Out.SendCustomDialog(
                                "THIS WILL MAKE THE ITEM CHARACTER-BOUND. CONTINUE?",
                                new CustomDialogResponse(CharacterBindResponseHandler));
                        }
                        else
                            SetModel(player, cachedModelID, cachedModelPrice);
                        /*
                        else if (GetAccountLockedSkinIDs().FirstOrDefault(x => x.ModelID == cachedModelID) != null)
                        {
                            player.Out.SendCustomDialog(
                                "THIS WILL MAKE THE ITEM ACCOUNT-BOUND. CONTINUE?",
                                new CustomDialogResponse(CharacterBindResponseHandler));
                        }*/
                       
                    }
                        

                    return true;
                }
                else
                {
                    SendReply(player,
                        "I'm sorry, I seem to have lost track of the model you wanted. Please start over.");
                }
                break;
            default:
                if (foundItem != null)
                {
                    modelIDToAssign = foundItem.ModelID;
                    price = foundItem.Price;

                    if (price == 2500)
                    {
                        DbInventoryItem tmpItem = (DbInventoryItem)displayItem.Clone();
                        byte tmp = tmpItem.Extension;
                        tmpItem.Extension = (byte)modelIDToAssign;
                        DisplayReskinPreviewTo(player, tmpItem);
                        //tmpItem.Extension = tmp;
                    }
                    else
                    {
                        DbInventoryItem tmpItem = (DbInventoryItem)displayItem.Clone();
                        int tmp = tmpItem.Model;
                        tmpItem.Model = modelIDToAssign;
                        DisplayReskinPreviewTo(player, tmpItem);
                        tmpItem.Model = tmp;
                    }

                    player.TempProperties.SetProperty(TempModelID, modelIDToAssign);
                    player.TempProperties.SetProperty(TempModelPrice, price);

                }
                else
                {
                    SendNotValidMessage(player);
                }
                break;
        }
        return true;
    }
    
    protected virtual void CharacterBindResponseHandler(GamePlayer player, byte response)
    {
        if (response == 1)
        {
            int cachedModelID = player.TempProperties.GetProperty<int>(TempModelID);
            int cachedModelPrice = player.TempProperties.GetProperty<int>(TempModelPrice);
            SetModel(player, cachedModelID, cachedModelPrice);
        }
    }

    private DbSkinVendorItem FindChoosenOptionInstrument(string str, DbInventoryItem item, int playerRealm, int noneRealm, int damageType, int characterClassUnknown, int playerClass, int playerRealmRank, int accountRealmRank, int playerDragonKills, int playerOrbs, int epicBossPlayerKills, int masteredCrafts, bool isGm)
    {

        if (isGm)
        {
            return VendorItemList.Find(x => (x.ItemType == item.Item_Type)
                  && x.Name == str
                  && (x.Realm == playerRealm || x.Realm == noneRealm)
                  && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
                  && x.ObjectType == item.Object_Type);
        }
        else
        {
            return VendorItemList.Find(x => (x.ItemType == item.Item_Type)
                   && x.Name == str
                   && (x.Realm == playerRealm || x.Realm == noneRealm)
                   && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
                   && x.PlayerRealmRank <= playerRealmRank
                   && x.AccountRealmRank <= accountRealmRank
                   && x.Orbs <= playerOrbs
                   && x.Drake <= playerDragonKills
                   && x.EpicBossKills <= epicBossPlayerKills
                   && x.MasteredCrafts <= masteredCrafts
                   && x.ObjectType == item.Object_Type);
        }

    }

    private DbSkinVendorItem FindChoosenOptionOtherItems(string str, DbInventoryItem item, int playerRealm, int noneRealm, int damageType, int characterClassUnknown, int playerClass, int playerRealmRank, int accountRealmRank, int playerDragonKills, int playerOrbs, int epicBossPlayerKills, int masteredCrafts,bool isGm)
    {

        if (isGm)
        {
            return VendorItemList.Find(x => (x.ItemType == item.Item_Type)
                  && x.Name == str
                  && (x.Realm == playerRealm || x.Realm == noneRealm)
                  && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
                  && x.DamageType == damageType
                  && x.ObjectType == item.Object_Type);
        }
        else
        {
            return VendorItemList.Find(x => (x.ItemType == item.Item_Type)
                   && x.Name == str
                   && (x.Realm == playerRealm || x.Realm == noneRealm)
                   && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
                   && x.PlayerRealmRank <= playerRealmRank
                   && x.AccountRealmRank <= accountRealmRank
                   && x.Orbs <= playerOrbs
                   && x.Drake <= playerDragonKills
                   && x.EpicBossKills <= epicBossPlayerKills
                   && x.MasteredCrafts <= masteredCrafts
                   && x.DamageType == damageType
                   && x.ObjectType == item.Object_Type);
        }

    }

    private DbSkinVendorItem FindConfirmedOneHanded(DbInventoryItem item, int cachedModelID, int playerRealm, int noneRealm, int damageType, int characterClassUnknown, int playerClass, int playerRealmRank, int accountRealmRank, int playerDragonKills, int playerOrbs, int epicBossPlayerKills, int masteredCrafts,bool isGm)
    {

        if (isGm)
        {
            return VendorItemList.Find(x => x.ModelID == cachedModelID
       && (x.ItemType == item.Item_Type || x.ItemType == Slot.RIGHTHAND)
       && x.ObjectType == item.Object_Type
       && x.DamageType == damageType
       && (x.Realm == playerRealm || x.Realm == noneRealm)
       && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown));
        }
        else
        {
            return VendorItemList.Find(x => x.ModelID == cachedModelID
       && (x.ItemType == item.Item_Type || x.ItemType == Slot.RIGHTHAND)
       && x.PlayerRealmRank <= playerRealmRank
       && x.AccountRealmRank <= accountRealmRank
       && x.Orbs <= playerOrbs
       && x.Drake <= playerDragonKills
       && x.EpicBossKills <= epicBossPlayerKills
       && x.MasteredCrafts <= masteredCrafts
       && x.ObjectType == item.Object_Type
       && x.DamageType == damageType
       && (x.Realm == playerRealm || x.Realm == noneRealm)
       && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown));
        }

    }

    private DbSkinVendorItem FindConfirmedInstruments(DbInventoryItem item, int cachedModelID, int playerRealm, int noneRealm, int damageType, int characterClassUnknown, int playerClass, int playerRealmRank, int accountRealmRank, int playerDragonKills, int playerOrbs, int epicBossPlayerKills, int masteredCrafts, bool isGm)
    {

        if (isGm)
        {
            return VendorItemList.Find(x => x.ModelID == cachedModelID
       && x.ItemType == item.Item_Type
       && x.ObjectType == item.Object_Type
       && (x.Realm == playerRealm || x.Realm == noneRealm)
       && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown));
        }
        else
        {
            return VendorItemList.Find(x => x.ModelID == cachedModelID
       && x.ItemType == item.Item_Type
       && x.PlayerRealmRank <= playerRealmRank
       && x.AccountRealmRank <= accountRealmRank
       && x.Orbs <= playerOrbs
       && x.Drake <= playerDragonKills
       && x.EpicBossKills <= epicBossPlayerKills
       && x.MasteredCrafts <= masteredCrafts
       && x.ObjectType == item.Object_Type
       && (x.Realm == playerRealm || x.Realm == noneRealm)
       && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown));
        }

    }

    private DbSkinVendorItem FindConfirmedOtherItems(DbInventoryItem item, int cachedModelID, int playerRealm, int noneRealm, int damageType, int characterClassUnknown, int playerClass, int playerRealmRank, int accountRealmRank, int playerDragonKills, int playerOrbs, int epicBossPlayerKills, int masteredCrafts, bool isGm)
    {

        if (isGm)
        {
            return VendorItemList.Find(x => x.ModelID == cachedModelID
       && x.ItemType == item.Item_Type
       && x.ObjectType == item.Object_Type
       && x.DamageType == damageType
       && (x.Realm == playerRealm || x.Realm == noneRealm)
       && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown));
        }
        else
        {
            return VendorItemList.Find(x => x.ModelID == cachedModelID
       && x.ItemType == item.Item_Type
       && x.PlayerRealmRank <= playerRealmRank
       && x.AccountRealmRank <= accountRealmRank
       && x.Orbs <= playerOrbs
       && x.Drake <= playerDragonKills
       && x.EpicBossKills <= epicBossPlayerKills
       && x.MasteredCrafts <= masteredCrafts
       && x.ObjectType == item.Object_Type
       && x.DamageType == damageType
       && (x.Realm == playerRealm || x.Realm == noneRealm)
       && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown));
        }

    }

    private DbSkinVendorItem FindChoosenOptionOneHanded(string str, DbInventoryItem item, int playerRealm, int noneRealm, int damageType, int characterClassUnknown, int playerClass, int playerRealmRank, int accountRealmRank, int playerDragonKills, int playerOrbs, int epicBossPlayerKills, int masteredCrafts, bool isGm)
    {

        if (isGm)
        {
            return VendorItemList.Find(x => (x.ItemType == item.Item_Type || x.ItemType == Slot.RIGHTHAND)
            && x.Name == str
            && (x.Realm == playerRealm || x.Realm == noneRealm)
            && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
            && x.DamageType == damageType
            && x.ObjectType == item.Object_Type);
        }
        else
        {
            return VendorItemList.Find(x => (x.ItemType == item.Item_Type || x.ItemType == Slot.RIGHTHAND)
            && x.Name == str
            && (x.Realm == playerRealm || x.Realm == noneRealm)
            && (x.CharacterClass == playerClass || x.CharacterClass == characterClassUnknown)
            && x.PlayerRealmRank <= playerRealmRank
            && x.AccountRealmRank <= accountRealmRank
            && x.Orbs <= playerOrbs
            && x.Drake <= playerDragonKills
            && x.EpicBossKills <= epicBossPlayerKills
            && x.MasteredCrafts <= masteredCrafts
            && x.DamageType == damageType
            && x.ObjectType == item.Object_Type);
        }

    }

    public class VendorItemService
    {
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
        //placeholder prices
        //500 lowbie stuff
        //2k low toa
        //4k high toa
        //10k dragonsworn
        //20k champion

        public List<DbSkinVendorItem> createVendorItems()
        {

            List<DbSkinVendorItem> VendorItemList = new List<DbSkinVendorItem>();

            // Two Handed Weapons //
            //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            //Generic
            VendorItemList.Add(new DbSkinVendorItem("Battler Hammer 2h", 3448, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Battler Hammer 2h", 3448, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Battler Hammer 2h", 3448, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Battler Sword 2h", 1670, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Battler Sword 2h", 1670, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Battler Sword 2h", 1670, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Malice Hammer 2h", 3449, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Malice Hammer 2h", 3449, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Malice Hammer 2h", 3449, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Malice Axe 2h", 2110, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Malice Axe 2h", 2110, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Malice Axe 2h", 2110, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Staff Of The Oracle", 1658, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Tartaros Gift", 1659, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Trident Of The Gods", 1660, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Trident Of The Gods", 2191, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Trident Of The Gods", 2191, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Trident Of The Gods", 2191, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Trident Of The Gods", 2191, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Trident Of The Gods", 2191, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Trident Of The Gods", 2191, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Claw Hand Staff", 828, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Bow", 2207, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Bow", 2207, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Bow", 2207, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Trident", 458, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Trident", 458, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Trident", 458, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Spear Of Kings", 1661, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Spear Of Kings", 1661, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Spear Of Kings", 1661, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Spear Of Kings", 1661, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Spear Of Kings", 1661, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Spear Of Kings", 1661, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Spear", 1662, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Spear", 1662, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Spear", 1662, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Spear", 1662, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Spear", 1662, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Spear", 1662, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Fools Bow", 1666, Slot.RANGED, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Fools Bow", 1666, Slot.RANGED, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Fools Bow", 1666, Slot.RANGED, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Braggarts Bow", 1667, Slot.RANGED, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Braggarts Bow", 1667, Slot.RANGED, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Braggarts Bow", 1667, Slot.RANGED, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Spear of Kings", 3450, Slot.TWOHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Lute", 227, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Drum", 228, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Flute", 325, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("TOA Flute", 2115, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("TOA Lute", 2117, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("TOA Drum", 2114, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("TOA Harp", 2116, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Base Lute", 2970, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Base Drum", 2971, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Base Flute", 2972, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Old Lute", 2973, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Old Drum", 2974, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Old Flute", 2975, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Slash, lowbie));



            //Drake
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Mage Staff", 3827, Slot.TWOHAND, 0, 0, 10, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Quarter Staff", 3826, Slot.TWOHAND, 0, 0, 10, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Quarter Staff", 3963, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Quarter Staff", 3927, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Quarter Staff", 3886, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Mage Staff", 3964, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Mage Staff", 3928, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Mage Staff", 3887, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Polearm", 3831, Slot.TWOHAND, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Polearm", 3832, Slot.TWOHAND, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Polearm", 3833, Slot.TWOHAND, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Spear", 3819, Slot.TWOHAND, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Spear", 3820, Slot.TWOHAND, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Spear", 3819, Slot.TWOHAND, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Spear", 3820, Slot.TWOHAND, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Polearm", 3968, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Polearm", 3969, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Polearm", 3970, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Spear", 3879, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Spear", 3880, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Spear", 3920, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Spear", 3921, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Longbow", 3823, Slot.RANGED, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Shortbow", 3824, Slot.RANGED, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Crossbow", 3816, Slot.RANGED, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Crossbow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Crossbow", 3876, Slot.RANGED, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Crossbow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Longbow", 3823, Slot.RANGED, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Shortbow", 3824, Slot.RANGED, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Longbow", 3823, Slot.RANGED, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Shortbow", 3824, Slot.RANGED, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Longbow", 3960, Slot.RANGED, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Shortbow", 3961, Slot.RANGED, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Longbow", 3924, Slot.RANGED, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Shortbow", 3925, Slot.RANGED, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Longbow", 3883, Slot.RANGED, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Shortbow", 3884, Slot.RANGED, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Scythe", 3825, Slot.TWOHAND, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Scythe", 3885, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Mandolin", 3848, Slot.TWOHAND, 0, 0, 10, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Harp", 3985, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Harp", 3908, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Harp", 3949, Slot.TWOHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType._FirstResist, dragonCost * 2));



            //alb
            VendorItemList.Add(new DbSkinVendorItem("Battle Axe 2h", 9, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("War Mattock 2h", 16, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Albion War Axe 2h", 73, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Albion Great Hammer 2h", 17, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Mage Staff", 19, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Shod Quarterstaff", 567, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Focus Staff", 568, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Polearm", 649, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Polearm", 2663, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Polearm", 2664, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton bill", 26, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Halberd", 67, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Halberd", 67, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Lochaber Axe", 68, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Pike", 69, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Lucerne Hammer", 70, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Longbow", 132, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Shortbow", 569, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Greatbow", 570, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Assault Crossbow", 890, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Crossbow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Brawlers Crossbow", 891, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Crossbow, (int)eDamageType.Thrust, freebie));



            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Briton Arch Mace 2h", 640, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Scimitar 2h", 645, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Briton War Pick 2h", 646, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Albion Mage Staff 1", 1166, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Albion Mage Staff 2", 1167, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Albion Mage Staff 3", 1168, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Albion Mage Staff 4", 1169, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Albion Mage Staff 5", 1170, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Necro Staff 1", 1171, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Necro Staff 2", 1172, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Partizan", 661, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Bardiche", 648, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Briton Spiked Polehammer", 650, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Bishops Reach", 870, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Foodmans Pick", 871, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Military Fork", 872, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Sabre Axe", 873, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Saracen Glaive", 874, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Tall Hammer", 875, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Brawlers Bow", 848, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Saracen Long Bow", 849, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Saracen Short Bow", 850, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Skinners Bow", 851, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Thorn Bow", 852, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Expert Crossbow", 892, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Crossbow, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Skinners Crossbow", 893, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Crossbow, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Steel Crossbow", 894, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Crossbow, (int)eDamageType.Thrust, lowbie));


            //rr4
            VendorItemList.Add(new DbSkinVendorItem("Zweihander 2h", 841, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Claymore 2h", 843, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Great Mace 2h", 842, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dire Hammer 2h", 844, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dire Axe 2h", 845, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Great Mattock 2h", 846, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Great Scimitar 2h", 847, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.TwoHandedWeapon, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Albion Mage Staff Fire", 1967, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Albion Mage Staff Water", 1968, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Albion Mage Staff Earth", 1966, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Alb Spiked Staff Mace Air", 1949, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Alb Spiked Staff Mace Earth", 1950, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Alb Spiked Staff Mace Fire", 1951, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Alb Spiked Staff Mace Water", 1952, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Military Fork Fire", 1931, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Military Fork Water", 1932, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Military Fork Air", 1933, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Military Fork Earth", 1930, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Saracen Glaive Air", 1933, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Saracen Glaive Earth", 1934, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Saracen Glaive Fire", 1935, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Saracen Glaive Water", 1936, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Tall Hammer Air", 1937, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Tall Hammer Earth", 1938, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Tall Hammer Fire", 1939, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Tall Hammer Water", 1940, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.PolearmWeapon, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Saracen Short Bow Air", 1905, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Saracen Short Bow Earth", 1906, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Saracen Short Bow Fire", 1907, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Saracen Short Bow Water", 1908, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Thorn Bow Air", 1909, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Thorn Bow Earth", 1910, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Thorn Bow Fire", 1911, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Thorn Bow Water", 1912, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Longbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Expert Crossbow Air", 1961, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Crossbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Expert Crossbow Earth", 1962, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Crossbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Expert Crossbow Fire", 1963, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Crossbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Expert Crossbow Water", 1964, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Crossbow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Albion Lute", 2976, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Albion Drum", 2977, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Albion Flute", 2975, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Slash, toageneric));


            //mid
            VendorItemList.Add(new DbSkinVendorItem("Norse Sword 2h", 314, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Great Axe 2h", 317, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Large Axe 2h", 318, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Hammer 2h", 574, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Staff", 327, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Shod Staff", 565, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Imported Hibernian Staff 1", 1175, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Imported Hibernian Staff 2", 1176, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Simple Spear", 328, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Simple Spear", 328, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Long Spear", 329, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Long Spear", 329, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Bill Spear", 331, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Bill Spear", 331, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Composite Bow", 564, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Bow", 1037, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Viking Bow", 1039, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, freebie));

            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Norse Greatsword 2h", 572, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Battleaxe 2h", 577, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Greathammer 2h", 576, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Warhammer 2h", 575, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Focus Staff", 566, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Imported Hibernian Staff 3", 1179, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Imported Hibernian Staff 4", 1180, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Big Spear", 332, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Big Spear", 332, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Battle Spear", 657, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Battle Spear", 657, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Troll Spear", 1036, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Troll Spear", 1036, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Spear", 1029, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Spear", 1029, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Spear, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Kobold Fang Bow", 1038, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Skinners Bow", 851, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Thorn Bow", 852, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, lowbie));

            //rr3
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Sword 2h", 658, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Greataxe 2h", 1027, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Great Hammer 2h", 1028, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("War Cleaver 2h", 660, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Spiked Hammer 2h", 659, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Troll Greatsword 2h", 957, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold Greataxe 2h", 1030, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold Great Club 2h", 1031, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold Great Sword 2h", 1032, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Mid Mage Staff Fire", 2067, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Mid Mage Staff Water", 2068, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Mid Mage Staff Air", 2065, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Mid Mage Staff Earth", 2066, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Spear Fire", 2047, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Spear Fire", 2047, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Spear Water", 2048, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Spear Water", 2048, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Spear Air", 2045, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Spear Air", 2045, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Spear Earth", 2046, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Spear Earth", 2046, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Viking Bow Air", 2061, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Viking Bow Earth", 2062, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Viking Bow Fire", 2063, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Viking Bow Water", 2064, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.CompositeBow, (int)eDamageType.Thrust, toageneric));

            //hib
            VendorItemList.Add(new DbSkinVendorItem("Celtic Greatsword 2h", 448, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Sword 2h", 459, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Great Hammer 2h", 462, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Spiked Mace 2h", 463, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Scythe", 927, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Harvest Scythe", 929, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff 1", 1173, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff 2", 1174, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff 3", 1175, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff 4", 1176, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Spear", 469, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Spear", 469, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Short Spear", 470, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Short Spear", 470, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Barbed Spear", 475, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Barbed Spear", 475, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Long Spear", 476, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Long Spear", 476, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Recurve Bow", 471, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Brawlers Bow", 918, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtric Great Bow", 919, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Cutters Bow", 920, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Elven Long Bow", 921, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Elven Short Bow", 922, Slot.RANGED, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Great Scythe", 926, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Greatwar Scythe", 928, Slot.TWOHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, freebie));

            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Celtic Falcata 2h", 639, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Sledgehammer 2h", 640, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Great Scythe", 926, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Great War Scythe", 928, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Martial Scythe", 930, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff 5", 1177, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff 6", 1178, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff 7", 1179, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff 8", 1180, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff 9", 1181, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic War Spear", 477, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic War Spear", 477, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Hooked Spear", 642, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Hooked Spear", 642, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Elven Spear", 935, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Elven Spear", 935, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Spear", 936, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Spear", 936, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Lurikeen Thorn Bow", 923, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Recurve Long Bow", 925, Slot.RANGED, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("War Scythe", 932, Slot.TWOHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Martial Scythe", 930, Slot.TWOHAND, 2, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Small Scythe", 931, Slot.TWOHAND, 2, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, lowbie));

            //rr4
            VendorItemList.Add(new DbSkinVendorItem("Celtic Hammer 2h", 904, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Great Mace 2h", 905, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Dire Club 2h", 906, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Elven Greatsword 2h", 907, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Hammer 2h", 908, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Mace 2h", 909, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Trollsplitter 2h", 910, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leaf Point 2h", 911, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Shod Shillelagh 2h", 912, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.LargeWeapons, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Magma Scythe", 2213, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff Air", 2017, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff Earth", 2018, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff Fire", 2019, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Hibernian Staff Water", 2020, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Staff, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Spear Air", 2005, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Spear Air", 2005, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Spear Earth", 2006, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Spear Earth", 2006, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Spear Fire", 2007, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Spear Fire", 2007, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Spear Water", 2008, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Spear Water", 2008, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.CelticSpear, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Elven Long Bow Air", 1993, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Elven Long Bow Earth", 1994, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Elven Long Bow Fire", 1995, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Elven Long Bow Water", 1996, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Elven Short Bow Air", 1997, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Elven Short Bow Earth", 1998, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Elven Short Bow Fire", 1999, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Elven Short Bow Water", 2000, Slot.RANGED, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.RecurvedBow, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Scythe Air", 2001, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Scythe Earth", 2002, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Scythe Fire", 2003, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Scythe Water", 2004, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Serpent Scythe", 2111, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Magma Scythe", 2213, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scythe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Hibernia Lute", 2979, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Hibernia Drum", 2980, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Hibernia Flute", 2981, Slot.TWOHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Instrument, (int)eDamageType.Slash, toageneric));


            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // OneHanded Weapons
            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Generic
            VendorItemList.Add(new DbSkinVendorItem("Wakazashi 1h", 2209, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wakazashi 1h", 2209, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wakazashi 1h", 2209, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, toageneric));

            VendorItemList.Add(new DbSkinVendorItem("Magma Hammer 1h", 2214, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Magma Hammer 1h", 2214, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Magma Hammer 1h", 2214, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, toageneric));

            VendorItemList.Add(new DbSkinVendorItem("Aerus Hammer 1h", 2205, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Hammer 1h", 2205, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Hammer 1h", 2205, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, toageneric));

            VendorItemList.Add(new DbSkinVendorItem("Khopesh 1h", 2195, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Khopesh 1h", 2195, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Khopesh 1h", 2195, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, toageneric));

            VendorItemList.Add(new DbSkinVendorItem("Aerus Sword 1h", 2203, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Sword 1h", 2203, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Sword 1h", 2203, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, toageneric));

            VendorItemList.Add(new DbSkinVendorItem("Magma Axe 1h", 2216, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Magma Axe 1h", 2216, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Magma Axe 1h", 2216, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Magma Axe 1h", 2216, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, toageneric));

            VendorItemList.Add(new DbSkinVendorItem("Traitor's Dagger 1h", 1668, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Traitor's Dagger 1h", 1668, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, artifact));

            VendorItemList.Add(new DbSkinVendorItem("Croc Tooth Dagger 1h", 1669, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Croc Tooth Dagger 1h", 1669, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, artifact));

            VendorItemList.Add(new DbSkinVendorItem("Golden Spear 1h", 1807, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Spear 1h", 1807, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, artifact));

            VendorItemList.Add(new DbSkinVendorItem("Battler Hammer 1h", 3453, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Battler Hammer 1h", 3453, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Battler Hammer 1h", 3453, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, artifact));


            VendorItemList.Add(new DbSkinVendorItem("Malice Hammer 1h", 3447, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Malice Hammer 1h", 3447, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Malice Hammer 1h", 3447, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, artifact));

            VendorItemList.Add(new DbSkinVendorItem("Bruiser Hammer 1h", 1671, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Bruiser Hammer 1h", 1671, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Bruiser Hammer 1h", 1671, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, artifact));

            VendorItemList.Add(new DbSkinVendorItem("Scepter of the Meritorious", 1672, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Scepter of the Meritorious", 1672, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Scepter of the Meritorious", 1672, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, artifact));

            VendorItemList.Add(new DbSkinVendorItem("Croc Tooth Axe 1h", 3451, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Croc Tooth Axe 1h", 3451, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Croc Tooth Axe 1h", 3451, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Croc Tooth Axe 1h", 3451, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, artifact));

            VendorItemList.Add(new DbSkinVendorItem("Traitor's Axe 1h", 3452, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Traitor's Axe 1h", 3452, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Traitor's Axe 1h", 3452, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Traitor's Axe 1h", 3452, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, artifact));

            VendorItemList.Add(new DbSkinVendorItem("Malice Axe 1h", 2109, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Malice Axe 1h", 2109, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Malice Axe 1h", 2109, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Malice Axe 1h", 2109, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, artifact));

            VendorItemList.Add(new DbSkinVendorItem("Battler Sword 1h", 2112, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Battler Sword 1h", 2112, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Battler Sword 1h", 2112, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, artifact));

            // alb

            //flex unsure of dmgtype
            VendorItemList.Add(new DbSkinVendorItem("Flex Chain", 857, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Flex Whip", 859, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Flex Flail", 861, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Flex Dagger Flail", 860, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Flex Morning Star", 862, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Flex Pick Flail", 863, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Slash, freebie));

            VendorItemList.Add(new DbSkinVendorItem("Dirk 1h", 21, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Rapier 1h", 22, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Main Gauche 1h", 25, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Club 1h", 11, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Hammer 1h", 12, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Mace 1h", 13, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Short Sword 1h", 3, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Longsword 1h", 4, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Handaxe 1h", 2, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Scimitar 1h", 8, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, freebie));


            //rr2 
            //flex unsure of dmgtype
            VendorItemList.Add(new DbSkinVendorItem("Flex Spiked Flail", 864, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Flex Spiked Whip", 865, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Flex War Chain", 866, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Flex Whip Dagger", 868, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Flex Whip Mace", 869, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Toothpick 1h", 876, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Highlander Dirk 1h", 889, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Foil 1h", 29, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Guarded Rapier 1h", 653, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Spiked Mace 1h", 20, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("War Hammer 1h", 15, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Flanged Mace 1h", 14, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Jambiya 1h", 651, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Cinquedea 1h", 877, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Falchion 1h", 879, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Coffin Axe 1h", 876, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.SlashingWeapon, (int)eDamageType.Slash, lowbie));

            //rr4
            VendorItemList.Add(new DbSkinVendorItem("Duelists Dagger 1h", 885, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Duelists Rapier 1h", 886, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Parrying Dagger 1h", 887, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Parrying Rapier 1h", 888, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.ThrustWeapon, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Bishops Mace 1h", 854, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Coffin Hammer Mace 1h", 855, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Coffin Mace 1h", 856, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.CrushingWeapon, (int)eDamageType.Crush, toageneric));

            //rr6
            VendorItemList.Add(new DbSkinVendorItem("Snakecharmer's Whip", 2119, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Flexible, (int)eDamageType.Slash, artifact));


            // hib
            VendorItemList.Add(new DbSkinVendorItem("Celtic Dirk 1h", 454, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Rapier 1h", 455, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Stiletto 1h", 456, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Club 1h", 449, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Mace 1h", 450, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Short Sword 1h", 445, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Longsword 1h", 446, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Broadsword 1h", 447, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, freebie));

            //rr2 
            VendorItemList.Add(new DbSkinVendorItem("Curved Dagger 1h", 457, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Guarded Rapier 1h", 643, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Lurikeen Dagger 1h", 943, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Spiked Club 1h", 452, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Spiked Mace 1h", 451, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Hammer 1h", 461, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Sickle 1h", 453, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Celtic Hooked Sword 1h", 460, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Falcata 1h", 444, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, lowbie));

            //rr4
            VendorItemList.Add(new DbSkinVendorItem("Elven Dagger 1h", 895, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Dagger 1h", 898, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leaf Dagger 1h", 902, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Adze 1h", 940, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Barbed Adze 1h", 941, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Adze 1h", 942, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("War Adze 1h", 947, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Celt Hammer 1h", 913, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dire Mace 1h", 915, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Hammer 1h", 916, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blunt, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Elven Short Sword 1h", 897, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Elven Longsword 1h", 896, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Short Sword 1h", 900, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Longsword 1h", 899, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leaf Short Sword 1h", 903, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leaf Longsword 1h", 901, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Blades, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Adze Air 1h", 2009, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Adze Earth 1h", 2010, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Adze Fire 1h", 2011, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Firbolg Adze Water 1h", 2012, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("War Adze Air 1h", 2013, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("War Adze Earth 1h", 2014, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("War Adze Fire 1h", 2015, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("War Adze Water 1h", 2016, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Piercing, (int)eDamageType.Thrust, toageneric));

            // mid
            //handtohand unsure of dmgtype
            VendorItemList.Add(new DbSkinVendorItem("Bladed Claw Greave", 959, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Bladed Fang Greave", 960, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Bladed Moon Claw", 961, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Bladed Moon Fang", 962, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Claw Greave", 963, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Fang Greave", 964, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Thrust, freebie));

            VendorItemList.Add(new DbSkinVendorItem("Small Hammer 1h", 320, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Hammer 1h", 321, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Short Sword 1h", 311, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Broadsword 1h", 310, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Spiked Axe 1h", 315, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Spiked Axe 1h", 315, Slot.RIGHTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, freebie));
            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            //rr2 
            VendorItemList.Add(new DbSkinVendorItem("Great Bladed Claw Greave", 965, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Great Bladed Fang Greave", 966, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Great Bladed Moon Claw", 967, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Great Bladed Moon Fang", 968, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Great Claw Greave", 969, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Great Fang Greave", 970, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Great Hammer 1h", 324, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Pick Hammer 1h", 323, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Spiked Hammer 1h", 656, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven Short Sword 1h", 655, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Cleaver 1h", 654, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Double Axe 1h", 573, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Bearded Axe 1h", 316, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Cleaver 1h", 654, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Double Axe 1h", 573, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Norse Bearded Axe 1h", 316, Slot.RIGHTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, lowbie));

            //rr4
            VendorItemList.Add(new DbSkinVendorItem("Troll Hammer 1h", 950, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Troll War Hammer 1h", 954, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold Sap 1h", 1016, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold War Club 1h", 1019, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Hammer, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Troll Dagger 1h", 949, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Troll Short Sword 1h", 952, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Troll Long Sword 1h", 948, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold Dagger 1h", 1013, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold Short Sword 1h", 1017, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold Long Sword 1h", 1015, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Sword, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Troll Hand Axe 1h", 1023, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Troll War Axe 1h", 1025, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold Hand Axe 1h", 1014, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold War Axe 1h", 1018, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Troll Hand Axe 1h", 1023, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Troll War Axe 1h", 1025, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold Hand Axe 1h", 1014, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Kobold War Axe 1h", 1018, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, toageneric));

            VendorItemList.Add(new DbSkinVendorItem("Dwarven War Axe 1h Air", 2029, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven War Axe 1h Air", 2029, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven War Axe 1h Earth", 2030, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven War Axe 1h Earth", 2030, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven War Axe 1h Fire", 2031, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven War Axe 1h Fire", 2031, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven War Axe 1h Water", 2032, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Axe, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Dwarven War Axe 1h Water", 2032, Slot.RIGHTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.LeftAxe, (int)eDamageType.Slash, toageneric));


            //rr6
            VendorItemList.Add(new DbSkinVendorItem("Snakecharmer's Fist", 2469, Slot.RIGHTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.HandToHand, (int)eDamageType.Slash, artifact));

            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Shields
            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Generic
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Small Shield", 2192, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Small Shield", 2210, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Magma Small Shield", 2218, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Small Shield", 2200, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Medium Shield", 2193, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Medium Shield", 2211, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Magma Medium Shield", 2219, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Medium Shield", 2201, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aten's Shield", 1663, Slot.LEFTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Cyclop's Eye", 1664, Slot.LEFTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Cyclop's Eye", 1664, Slot.LEFTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Cyclop's Eye", 1664, Slot.LEFTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Shield Of Khaos", 1665, Slot.LEFTHAND, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Large Shield", 2194, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Large Shield", 2212, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Magma Large Shield", 2220, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Large Shield", 2202, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));


            // alb
            VendorItemList.Add(new DbSkinVendorItem("Leather Buckler", 1040, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Buckler", 1041, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Buckler", 1042, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Albion Dragonslayer Buckler", 3965, Slot.LEFTHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Heater", 1049, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Heater", 1050, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Heater", 1051, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Tower", 1085, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Tower", 1086, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Tower", 1087, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Albion Dragonslayer Medium", 3966, Slot.LEFTHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Heater", 1067, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Heater", 1068, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Heater", 1069, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Tower", 1058, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Tower", 1059, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Tower", 1060, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Round", 1076, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Round", 1077, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Round", 1078, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Albion Dragonslayer Large", 3967, Slot.LEFTHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, dragonCost * 2));
            // rr2
            VendorItemList.Add(new DbSkinVendorItem("Leather Tri-Tip Buckler", 1103, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Tri-Tip Buckler", 1104, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Tri-Tip Buckler", 1105, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Horned", 1112, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Horned", 1113, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Horned", 1114, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Horned", 1106, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Horned", 1107, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Horned", 1108, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));

            // rr4
            VendorItemList.Add(new DbSkinVendorItem("Leather Kite Buckler", 1118, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Kite Buckler", 1119, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Kite Buckler", 1120, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Kite", 1115, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Kite", 1116, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Kite", 1117, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Kite", 1109, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Kite", 1110, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Kite", 1111, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leather Studded Tower", 1121, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Studded Tower", 1122, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Studded Tower", 1123, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));

            // hib
            VendorItemList.Add(new DbSkinVendorItem("Leather Buckler", 1046, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Buckler", 1047, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Buckle", 1048, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernia Dragonslayer Buckler", 3888, Slot.LEFTHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Heater", 1055, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Heater", 1056, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Heater", 1057, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Tower", 1088, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Tower", 1089, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Tower", 1090, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernia Dragonslayer Medium", 3889, Slot.LEFTHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Heater", 1073, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Heater", 1074, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Heater", 1075, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Tower", 1064, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Tower", 1065, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Tower", 1066, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Round", 1082, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Round", 1083, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Round", 1084, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Hibernia Dragonslayer Large", 3890, Slot.LEFTHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, dragonCost * 2));

            // rr2
            VendorItemList.Add(new DbSkinVendorItem("Leather Celtic Buckler", 1148, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Celtic Buckler", 1149, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Celtic Buckler", 1150, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Celtic", 1145, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Celtic", 1146, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Celtic", 1147, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Celtic", 1154, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Celtic", 1155, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Celtic", 1156, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));

            // rr4
            VendorItemList.Add(new DbSkinVendorItem("Leather Leaf Buckler", 1163, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Leaf Buckler", 1164, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Leaf Buckler", 1165, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Leaf", 1160, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Leaf", 1161, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Leaf", 1162, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Leaf", 1157, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Leaf", 1158, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Leaf", 1159, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leather Celtic Tower", 1151, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Celtic Tower", 1152, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Celtic Tower", 1153, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));

            // mid
            VendorItemList.Add(new DbSkinVendorItem("Leather Buckler", 1043, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Buckler", 1044, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Buckler", 1045, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Midgard Dragonslayer Buckler", 3929, Slot.LEFTHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Heater", 1052, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Heater", 1053, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Heater", 1054, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Tower", 1091, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Tower", 1092, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Tower", 1093, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Midgard Dragonslayer Medium", 3930, Slot.LEFTHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Heater", 1070, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Heater", 1071, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Heater", 1072, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Tower", 1061, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Tower", 1062, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Tower", 1063, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Round", 1079, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Round", 1080, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Round", 1081, Slot.LEFTHAND, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Midgard Dragonslayer Large", 3931, Slot.LEFTHAND, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, dragonCost * 2));
            // rr2
            VendorItemList.Add(new DbSkinVendorItem("Leather Norse Buckler", 1139, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Norse Buckler", 1140, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Norse Buckler", 1141, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Crescent", 1124, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Crescent", 1125, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Crescent", 1126, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Crescent", 1133, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Crescent", 1134, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Crescent", 1135, Slot.LEFTHAND, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, lowbie));

            // rr3
            VendorItemList.Add(new DbSkinVendorItem("Leather Grave Buckler", 1130, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Grave Buckler", 1131, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Grave Buckler", 1132, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Crush, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leather Medium Grave", 1132, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Medium Grave", 1128, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Medium Grave", 1129, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Slash, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leather Large Grave", 1136, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Large Grave", 1137, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Large Grave", 1138, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Leather Norse Tower", 1142, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Metal Norse Tower", 1143, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Wood Norse Tower", 1144, Slot.LEFTHAND, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Shield, (int)eDamageType.Thrust, toageneric));

            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Helm
            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Generic

            // alb
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 1229, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 62, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 824, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 824, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 63, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 64, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 1230, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 1231, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 1233, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 1233, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 2812, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 1238, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 1229, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 2800, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 1234, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 1234, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 1236, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 1238, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Robin Hood Hat", 1281, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Tarboosh", 1284, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Jester Hat", 1287, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wizard Hat", 1278, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));


            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3864, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3862, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3866, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3861, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4056, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4054, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4055, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4055, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4057, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4053, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));

            //rr 2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1230, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1231, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1234, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1234, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1236, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1239, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1230, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1232, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1235, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1235, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1236, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1240, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            //rr 4
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2253, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2256, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2262, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2262, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2265, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2268, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2307, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2310, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2316, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2316, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2313, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2322, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2361, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2364, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2370, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2370, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2373, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2376, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2415, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2418, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2424, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2424, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2421, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2430, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            //rr 6
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1839, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));

            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1842, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));

            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2223, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));


            // hib
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 826, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 438, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 835, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 835, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 838, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 1197, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 439, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 836, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 836, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 1202, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 1197, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 440, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 837, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 837, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 840, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Robin Hood Hat", 1282, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Robin Hood Hat", 1282, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Robin Hood Hat", 1282, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Robin Hood Hat", 1282, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Robin Hood Hat", 1282, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Leaf Hat", 1285, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Leaf Hat", 1285, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Leaf Hat", 1285, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Leaf Hat", 1285, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Leaf Hat", 1285, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Stag Helm", 1288, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Stag Helm", 1288, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Stag Helm", 1288, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Stag Helm", 1288, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Stag Helm", 1288, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wizard Hat", 1279, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3864, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3862, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3867, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4063, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4061, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4062, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4062, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4066, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1197, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1198, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1199, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1199, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1200, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1197, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1198, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1199, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1199, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1200, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            //rr4
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2289, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2292, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2298, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2298, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2301, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2343, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2346, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2352, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2352, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2355, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2397, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2400, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2406, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2406, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2409, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2451, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2454, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2460, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2460, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2463, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            //rr6
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1840, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1843, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2225, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            // mid
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 825, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 335, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 829, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 829, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 1", 832, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 1213, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 336, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 830, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 830, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 2", 833, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 1213, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 337, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 831, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 831, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 3", 834, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Fur Cap", 1283, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Fur Cap", 1283, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Fur Cap", 1283, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Fur Cap", 1283, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Fur Cap", 1283, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wing Hat", 1286, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wing Hat", 1286, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wing Hat", 1286, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wing Hat", 1286, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wing Hat", 1286, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wolf Helm", 1289, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wolf Helm", 1289, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wolf Helm", 1289, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wolf Helm", 1289, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wolf Helm", 1289, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Wizard Hat", 1280, Slot.HELM, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3864, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3862, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3863, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Helm", 3866, Slot.HELM, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4070, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4068, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4069, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4069, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Helm", 4072, Slot.HELM, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));

            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1213, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 337, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1215, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1215, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 4", 1216, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1213, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 337, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1215, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1215, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Helm 5", 1216, Slot.HELM, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            //rr4
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2271, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2274, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2280, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2280, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Helm", 2277, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2325, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2328, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2334, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2334, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Helm", 2331, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2379, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2382, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2388, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2388, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Helm", 2385, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2433, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2436, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2442, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2442, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Helm", 2439, Slot.HELM, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            //rr6
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur", 1841, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Crown of Zahur variant", 1844, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Winged Helm", 2224, Slot.HELM, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));

            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Torso
            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            // Generic


            // alb
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 139, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 31, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 51, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 51, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 41, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 46, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 139, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 36, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 81, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 81, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 186, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 86, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 139, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 74, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 156, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 156, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 196, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 201, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3783, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3758, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3778, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3778, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3778, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3768, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4015, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 3990, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4010, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4010, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 3995, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4000, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2728, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2735, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2741, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2741, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2747, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2753, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2790, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2797, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2803, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2803, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2809, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2815, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 3018, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 2988, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 3012, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 3012, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 2994, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 3006, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3059, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3064, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3116, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3116, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3043, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3053, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            //rr 2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 139, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 134, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 216, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 216, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 1246, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 206, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 139, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 176, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 221, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 221, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 1251, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 211, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            //rr 4
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1619, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1640, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1848, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1848, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 2101, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 2092, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 2515, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 2135, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 1757, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 1757, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 1809, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 2124, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 2169, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 2176, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 1780, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 1780, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 1694, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 1703, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 2245, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 2144, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 1798, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 1798, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 1736, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 1685, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            //rr 5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 693, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Paladin, (int)eObjectType.Plate, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 688, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Armsman, (int)eObjectType.Plate, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 718, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Mercenary, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 1267, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Reaver, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 713, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Cleric, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 3380, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Minstrel, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 728, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Scout, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 728, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Scout, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 792, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Infiltrator, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 797, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Friar, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 798, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Wizard, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 733, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Theurgist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 682, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Cabalist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 804, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Sorcerer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 1266, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Necromancer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            //rr 6
            VendorItemList.Add(new DbSkinVendorItem("Eirene's Chest", 2511, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Eirene's Chest", 2226, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Naliah's Robe", 2516, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Naliah's Vest", 1626, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2120, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2470, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2473, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2473, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2476, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2479, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Scarab Vest", 2187, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Scarab Vest", 2496, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Scarab Vest", 2496, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));


            // hib
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 378, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 373, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 363, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 363, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 388, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 398, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 393, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 383, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 383, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 408, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 418, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 413, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 403, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 403, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 428, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3783, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3758, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3778, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3778, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3773, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4099, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4074, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4094, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4094, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4089, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2759, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2766, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2772, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2772, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2778, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2790, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2797, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2803, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2803, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2809, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 3018, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 2988, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 3012, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 3012, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 3000, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3059, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3064, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3116, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3116, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3048, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            //rr 2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 418, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 433, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 423, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 423, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 988, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 418, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 353, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 1256, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 1256, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 1272, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            //rr 4
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1621, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1641, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1850, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1850, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1773, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 2515, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 2136, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 1759, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 1759, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 1791, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 2169, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 2178, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 1782, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 1782, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 1714, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 2246, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 2146, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 1800, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 1800, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 1738, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            //rr 5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 708, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Hero, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 810, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Champion, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 739, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Druid, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 805, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Warden, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 734, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Bard, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 734, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Bard, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 782, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Blademaster, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 782, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Blademaster, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 815, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Ranger, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 815, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Ranger, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 746, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Nightshade, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 1003, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Valewalker, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 745, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Mentalist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 781, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Enchanter, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 744, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Eldritch, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 1186, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Animist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            //rr 6
            VendorItemList.Add(new DbSkinVendorItem("Eirene's Chest", 2228, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Naliah's Robe", 2517, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Naliah's Vest", 1628, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2122, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2472, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2475, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2475, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2480, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Scarab Vest", 2189, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Scarab Vest", 2498, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Scarab Vest", 2498, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));


            // mid
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 245, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 240, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 230, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 230, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 1", 235, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 265, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 260, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 250, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 250, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 2", 275, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 285, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 280, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 270, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 270, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 3", 295, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3783, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3758, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3778, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3778, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Breastplate", 3778, Slot.TORSO, 0, 0, 10, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4046, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4021, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4041, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4041, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Breastplate", 4026, Slot.TORSO, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2694, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2701, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2707, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2707, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Breastplate", 2713, Slot.TORSO, 0, 0, 0, 0, 10, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2790, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2797, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2803, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2803, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Breastplate", 2809, Slot.TORSO, 0, 0, 0, 0, 0, 3, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 3018, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 2988, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 3012, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 3012, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Breastplate", 2994, Slot.TORSO, 0, 0, 0, 100000, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3059, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3064, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3116, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3116, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Breastplate", 3043, Slot.TORSO, 0, 0, 0, 250000, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            //rr 2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 305, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 300, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 270, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 270, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 4", 999, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 305, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 300, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 343, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 343, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Torso 5", 1262, Slot.TORSO, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            //rr 4
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1623, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1642, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1849, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 1849, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Breastplate", 2102, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 2515, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 2137, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 1758, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 1758, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Breastplate", 1810, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 2169, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 2177, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 1781, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 1781, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Breastplate", 1695, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 2247, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 2145, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 1799, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 1799, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Breastplate", 1737, Slot.TORSO, 0, 4, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            //rr 5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 776, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Warrior, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 698, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Healer, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 766, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Shaman, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 771, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Skald, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 3370, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Thane, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 751, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Berserker, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 751, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Berserker, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 1192, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Savage, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 1192, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Savage, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 756, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Hunter, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 756, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Hunter, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 761, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Shadowblade, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 799, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Spiritmaster, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 703, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Runemaster, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Chestpiece", 1187, Slot.TORSO, 5, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Bonedancer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            //rr 6
            VendorItemList.Add(new DbSkinVendorItem("Eirene's Chest", 2227, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Naliah's Robe", 2518, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Naliah's Vest", 1627, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2121, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2471, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2474, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2474, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Guard of Valor", 2477, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Scarab Vest", 2188, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Scarab Vest", 2497, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Golden Scarab Vest", 2497, Slot.TORSO, 0, 6, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            // var added = GameServer.Database.AddObject(VendorItemList);
            // var itemCount = GameServer.Database.GetObjectCount<SkinVendorItem>();

            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Amor Pads
            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.TORSO, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));

            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));

            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 1", 1, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 2", 2, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 3", 3, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 4", 4, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, armorpads));
            VendorItemList.Add(new DbSkinVendorItem("Type 5", 5, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, armorpads));

            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Amor Cloaks
            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            //Generic
            VendorItemList.Add(new DbSkinVendorItem("Basic Cloak", 57, Slot.CLOAK, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Hooded Cloak", 96, Slot.CLOAK, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Bordered Cloak", 92, Slot.CLOAK, 0, 0, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Cloak", 3790, Slot.CLOAK, 0, 0, 10, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, dragonCost * 2));

            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Mage Cloak 2", 557, Slot.CLOAK, 0, 2, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Mage Cloak 3", 144, Slot.CLOAK, 0, 2, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Embossed Cloak", 560, Slot.CLOAK, 0, 2, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Collared Cloak", 669, Slot.CLOAK, 0, 2, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, lowbie));
            //rr4
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Cloak", 1722, Slot.CLOAK, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Magma Cloak", 1725, Slot.CLOAK, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygian Cloak", 1724, Slot.CLOAK, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, toageneric));

            //rr6
            VendorItemList.Add(new DbSkinVendorItem("Cloudsong", 1727, Slot.CLOAK, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Shades of Mist", 1726, Slot.CLOAK, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Harpy Feather Cloak", 1721, Slot.CLOAK, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Feathered Cloak", 1720, Slot.CLOAK, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Healer's Embrace", 1723, Slot.CLOAK, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, artifact));
            //alb
            VendorItemList.Add(new DbSkinVendorItem("Realm Cloak", 3800, Slot.CLOAK, 0, 5, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Cloak", 4105, Slot.CLOAK, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, dragonCost * 2));
            //hib
            VendorItemList.Add(new DbSkinVendorItem("Realm Cloak", 3802, Slot.CLOAK, 0, 5, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Cloak", 4109, Slot.CLOAK, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, dragonCost * 2));
            //mid
            VendorItemList.Add(new DbSkinVendorItem("Realm Cloak", 3801, Slot.CLOAK, 0, 5, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Cloak", 4107, Slot.CLOAK, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Magical, (int)eDamageType._FirstResist, dragonCost * 2));

            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Amor Sleeves
            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            //Generic
            //rr 4
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Sleeves", 1625, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Sleeves", 1639, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Sleeves", 1847, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Sleeves", 1847, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Sleeves", 2100, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Sleeves", 1770, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Sleeves", 2091, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Sleeves", 2152, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Sleeves", 2134, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Sleeves", 1756, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Sleeves", 1756, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Sleeves", 1808, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Sleeves", 1788, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Sleeves", 2123, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Sleeves", 2161, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Sleeves", 2175, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Sleeves", 1779, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Sleeves", 1779, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Sleeves", 1693, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Sleeves", 1711, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Sleeves", 1702, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));

            VendorItemList.Add(new DbSkinVendorItem("Aerus Sleeves", 2237, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Sleeves", 2143, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Sleeves", 1797, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Sleeves", 1797, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Sleeves", 1735, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Sleeves", 1747, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Sleeves", 1684, Slot.ARMS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            //rr 6
            VendorItemList.Add(new DbSkinVendorItem("Foppish Sleeves", 1732, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Foppish Sleeves", 1732, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Foppish Sleeves", 1732, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Foppish Sleeves", 1732, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Foppish Sleeves", 1732, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Foppish Sleeves", 1732, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Foppish Sleeves", 1732, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Arms of the Wind", 1733, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Arms of the Wind", 1733, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Arms of the Wind", 1733, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Arms of the Wind", 1733, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Arms of the Wind", 1733, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Arms of the Wind", 1733, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Arms of the Wind", 1733, Slot.ARMS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            //drake
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Sleeves", 3785, Slot.ARMS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Sleeves", 3760, Slot.ARMS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Sleeves", 3780, Slot.ARMS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Sleeves", 3780, Slot.ARMS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Sleeves", 3765, Slot.ARMS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Sleeves", 3775, Slot.ARMS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Sleeves", 3770, Slot.ARMS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));
            //epic boss kill
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Sleeves", 2731, Slot.ARMS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Sleeves", 2737, Slot.ARMS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Sleeves", 2743, Slot.ARMS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Sleeves", 2743, Slot.ARMS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Sleeves", 2749, Slot.ARMS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Sleeves", 2749, Slot.ARMS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Sleeves", 2755, Slot.ARMS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            // craft
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Sleeves", 2793, Slot.ARMS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Sleeves", 2799, Slot.ARMS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Sleeves", 2805, Slot.ARMS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Sleeves", 2805, Slot.ARMS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Sleeves", 2811, Slot.ARMS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Sleeves", 2811, Slot.ARMS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Sleeves", 2817, Slot.ARMS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            //orbs
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Sleeves", 3020, Slot.ARMS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Sleeves", 2990, Slot.ARMS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Sleeves", 3014, Slot.ARMS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Sleeves", 3014, Slot.ARMS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Sleeves", 2996, Slot.ARMS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Sleeves", 3002, Slot.ARMS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Sleeves", 3008, Slot.ARMS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Sleeves", 3061, Slot.ARMS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Sleeves", 3066, Slot.ARMS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Sleeves", 3118, Slot.ARMS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Sleeves", 3118, Slot.ARMS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Sleeves", 3045, Slot.ARMS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Sleeves", 3050, Slot.ARMS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Sleeves", 3055, Slot.ARMS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));

            //alb
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 141, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 33, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 53, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 53, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 43, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 48, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 141, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 38, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 83, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 83, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 183, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 88, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 141, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 76, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 158, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 158, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 188, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 203, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4017, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 3992, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4012, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4012, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 3997, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4002, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));
            //rr 2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 141, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 136, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 218, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 218, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 1248, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 208, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 141, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 136, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 223, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 223, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 1253, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 213, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            //rr 5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 690, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Armsman, (int)eObjectType.Plate, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 0, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Cabalist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 690, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Cleric, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 0, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Friar, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 794, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Infiltrator, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 720, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Mercenary, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 725, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Minstrel, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 0, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Necromancer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 695, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Paladin, (int)eObjectType.Plate, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 1269, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Reaver, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 730, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Scout, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 0, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Sorcerer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 0, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Theurgist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 0, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Wizard, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));


            //hib
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 360, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 375, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 365, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 365, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 370, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 380, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 395, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 385, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 385, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 390, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 400, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 415, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 405, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 405, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 410, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4101, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4076, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4096, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4096, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4091, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            //rr 2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 420, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 435, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 425, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 425, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 430, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 420, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 435, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 1258, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 1258, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 1274, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            //rr 5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 0, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Animist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 736, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Bard, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 784, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Blademaster, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 812, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Champion, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 741, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Druid, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 0, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Eldritch, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 0, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Enchanter, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 710, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Hero, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 0, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Mentalist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 748, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Nightshade, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 817, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Ranger, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 0, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Valewalker, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 807, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Warden, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            //mid
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 247, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 242, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 232, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 232, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 1", 237, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 267, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 262, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 252, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 252, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 2", 257, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 287, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 282, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 272, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 272, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 3", 277, Slot.ARMS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4048, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4023, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4043, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4043, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Sleeves", 4028, Slot.ARMS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            //rr 2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 307, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 302, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 272, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 272, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 4", 1002, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Sleeves 5", 1265, Slot.ARMS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            //rr 5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 753, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Berserker, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 1189, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Bonedancer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 700, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Healer, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 758, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Hunter, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 705, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Runemaster, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 1194, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Savage, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 763, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Shadowblade, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 768, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Shaman, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 773, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Skald, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 801, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Spiritmaster, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 3372, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Thane, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Sleeves", 778, Slot.ARMS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Warrior, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));

            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Amor Pants
            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            //Generic

            //drake
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Pants", 3784, Slot.LEGS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Pants", 3759, Slot.LEGS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Pants", 3779, Slot.LEGS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Pants", 3779, Slot.LEGS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Pants", 3764, Slot.LEGS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Pants", 3774, Slot.LEGS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Pants", 3769, Slot.LEGS, 0, 0, 5, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));

            //epic boss kill
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Pants", 2730, Slot.LEGS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Pants", 2736, Slot.LEGS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Pants", 2742, Slot.LEGS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Pants", 2742, Slot.LEGS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Pants", 2748, Slot.LEGS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Pants", 2748, Slot.LEGS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Pants", 2754, Slot.LEGS, 0, 0, 0, 0, 5, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            // craft
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Pants", 2792, Slot.LEGS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Pants", 2798, Slot.LEGS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Pants", 2804, Slot.LEGS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Pants", 2804, Slot.LEGS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Pants", 2810, Slot.LEGS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Pants", 2810, Slot.LEGS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Pants", 2816, Slot.LEGS, 0, 0, 0, 0, 0, 2, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            //orbs
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Pants", 3019, Slot.LEGS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Pants", 2989, Slot.LEGS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Pants", 3013, Slot.LEGS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Pants", 3013, Slot.LEGS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Pants", 2995, Slot.LEGS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Pants", 3001, Slot.LEGS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Pants", 3007, Slot.LEGS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Pants", 3060, Slot.LEGS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Pants", 3065, Slot.LEGS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Pants", 3117, Slot.LEGS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Pants", 3117, Slot.LEGS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Pants", 3044, Slot.LEGS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Pants", 3049, Slot.LEGS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Pants", 3054, Slot.LEGS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));

            //rr4
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Pants", 1631, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Pants", 1646, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Pants", 1854, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Pants", 1854, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Pants", 2107, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Pants", 2098, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Pants", 1778, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Pants", 2158, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Pants", 2141, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Pants", 1763, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Pants", 1763, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Pants", 1815, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Pants", 1796, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Pants", 2130, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Pants", 2167, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Pants", 2182, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Pants", 1786, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Pants", 1786, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Pants", 1700, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Pants", 1718, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Pants", 1709, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Pants", 2243, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Pants", 2150, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Pants", 1804, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Pants", 1804, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Pants", 1742, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Pants", 1754, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Pants", 1691, Slot.LEGS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            //rr6
            VendorItemList.Add(new DbSkinVendorItem("Wings Dive", 1767, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Wings Dive", 1767, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Wings Dive", 1767, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Wings Dive", 1767, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Wings Dive", 1767, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Wings Dive", 1767, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Wings Dive", 1767, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Alvarus' Leggings", 1744, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Alvarus' Leggings", 1744, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Alvarus' Leggings", 1744, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Alvarus' Leggings", 1744, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Alvarus' Leggings", 1744, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Alvarus' Leggings", 1744, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Alvarus' Leggings", 1744, Slot.LEGS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));

            //alb
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 140, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 37, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 52, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 52, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 42, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 47, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 140, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 75, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 82, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 82, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 187, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 87, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 140, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 135, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 157, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 157, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 197, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 1273, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4016, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 3991, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4011, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4011, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 3996, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4001, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));
            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 140, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 135, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 157, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 157, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 197, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 1273, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 140, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 135, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 222, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 222, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 1252, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 1273, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            //rr5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 689, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Armsman, (int)eObjectType.Plate, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 0, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Cabalist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 714, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Cleric, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 0, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Friar, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 793, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Infiltrator, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 719, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Mercenary, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 724, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Minstrel, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 0, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Necromancer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 694, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Paladin, (int)eObjectType.Plate, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 1268, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Reaver, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 729, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Scout, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 729, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Scout, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 0, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Sorcerer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 0, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Theurgist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 0, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Wizard, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));

            //hib
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 359, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 374, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 364, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 364, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 389, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 359, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 394, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 384, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 384, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 409, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 399, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 414, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 404, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 404, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 429, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4100, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4075, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4095, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4095, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4090, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 419, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 434, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 424, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 424, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 989, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 419, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 434, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 1257, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 1257, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 1273, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            //rr5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 0, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Animist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 735, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Bard, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 783, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Blademaster, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 811, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Champion, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 740, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Druid, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 0, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Eldritch, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 0, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Enchanter, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 709, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Hero, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 0, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Mentalist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 747, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Nightshade, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 816, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Ranger, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 0, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Valewalker, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 807, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Warden, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            //mid
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 246, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 241, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 231, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 231, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 1", 236, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 246, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 261, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 251, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 251, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 2", 276, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 286, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 281, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 271, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 271, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 3", 296, Slot.LEGS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4047, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4022, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4042, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4042, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Pants", 4027, Slot.LEGS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));

            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 306, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 301, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 291, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 291, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 4", 998, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 306, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 301, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 291, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 291, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Pants 5", 1261, Slot.LEGS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            //rr5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 752, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Berserker, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 1188, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Bonedancer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 699, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Healer, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 757, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Hunter, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 704, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Runemaster, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 1193, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Savage, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 762, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Shadowblade, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 767, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Shaman, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 772, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Skald, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 800, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Spiritmaster, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 3371, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Thane, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Pants", 777, Slot.LEGS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Warrior, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));


            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Amor Gloves
            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            //Generic

            //drake
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Gloves", 3786, Slot.HANDS, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Gloves", 3761, Slot.HANDS, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Gloves", 3782, Slot.HANDS, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Gloves", 3782, Slot.HANDS, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Gloves", 3766, Slot.HANDS, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Gloves", 3776, Slot.HANDS, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Gloves", 3771, Slot.HANDS, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));

            //epic boss kill
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Gloves", 2734, Slot.HANDS, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Gloves", 2740, Slot.HANDS, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Gloves", 2746, Slot.HANDS, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Gloves", 2746, Slot.HANDS, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Gloves", 2752, Slot.HANDS, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Gloves", 2752, Slot.HANDS, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Gloves", 2758, Slot.HANDS, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            // craft
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Gloves", 2796, Slot.HANDS, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Gloves", 2802, Slot.HANDS, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Gloves", 2808, Slot.HANDS, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Gloves", 2808, Slot.HANDS, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Gloves", 2814, Slot.HANDS, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Gloves", 2814, Slot.HANDS, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Gloves", 2820, Slot.HANDS, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            //orbs
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Gloves", 3022, Slot.HANDS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Gloves", 2993, Slot.HANDS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Gloves", 3016, Slot.HANDS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Gloves", 3016, Slot.HANDS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Gloves", 2999, Slot.HANDS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Gloves", 3005, Slot.HANDS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Gloves", 3011, Slot.HANDS, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Gloves", 3063, Slot.HANDS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Gloves", 3068, Slot.HANDS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Gloves", 3120, Slot.HANDS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Gloves", 3120, Slot.HANDS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Gloves", 3047, Slot.HANDS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Gloves", 3052, Slot.HANDS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Gloves", 3057, Slot.HANDS, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));

            //rr4
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Gloves", 1620, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Gloves", 1645, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Gloves", 1853, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Gloves", 1853, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Gloves", 2106, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Gloves", 1776, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Gloves", 2097, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Gloves", 2248, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Gloves", 2140, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Gloves", 1762, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Gloves", 1762, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Gloves", 1814, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Gloves", 1794, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Gloves", 2129, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Gloves", 2249, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Gloves", 2181, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Gloves", 1785, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Gloves", 1785, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Gloves", 1699, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Gloves", 1717, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Gloves", 1708, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Gloves", 2250, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Gloves", 2149, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Gloves", 1803, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Gloves", 1803, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Gloves", 1741, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Gloves", 1753, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Gloves", 1690, Slot.HANDS, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            //rr6
            VendorItemList.Add(new DbSkinVendorItem("Maddening Scalars", 1746, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Maddening Scalars", 1746, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Maddening Scalars", 1746, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Maddening Scalars", 1746, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Maddening Scalars", 1746, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Maddening Scalars", 1746, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Maddening Scalars", 1746, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Sharkskin Gloves", 1734, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Sharkskin Gloves", 1734, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Sharkskin Gloves", 1734, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Sharkskin Gloves", 1734, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Sharkskin Gloves", 1734, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Sharkskin Gloves", 1734, Slot.HANDS, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, artifact));

            //alb
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 142, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 34, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 80, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 80, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 44, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 49, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 142, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 39, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 85, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 85, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 184, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 89, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 142, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 77, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 159, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 159, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 189, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 204, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4018, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 3993, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4014, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4014, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 3998, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4003, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));
            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 142, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 149, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 219, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 219, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 194, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 209, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 142, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 179, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 224, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 224, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 1249, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 214, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            //rr5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 691, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Armsman, (int)eObjectType.Plate, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 0, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Cabalist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 716, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Cleric, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 0, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Friar, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 795, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Infiltrator, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 721, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Mercenary, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 726, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Minstrel, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 0, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Necromancer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 696, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Paladin, (int)eObjectType.Plate, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 1271, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Reaver, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 732, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Scout, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 732, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Scout, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 0, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Sorcerer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 0, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Theurgist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 0, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Wizard, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));

            //hib
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 361, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 376, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 366, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 366, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 371, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 381, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 396, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 386, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 386, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 391, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 401, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 416, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 406, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 406, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 411, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4102, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4077, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4098, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4098, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4092, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 341, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 436, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 426, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 426, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 431, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 341, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 436, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 1259, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 1259, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 351, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            //rr5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 0, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Animist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 737, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Bard, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 785, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Blademaster, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 813, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Champion, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 742, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Druid, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 0, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Eldritch, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 0, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Enchanter, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 711, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Hero, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 0, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Mentalist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 749, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Nightshade, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 818, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Ranger, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 0, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Valewalker, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 808, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Warden, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            //mid
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 248, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 243, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 233, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 233, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 1", 238, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 268, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 263, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 253, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 253, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 2", 258, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 288, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 283, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 273, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 273, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 3", 278, Slot.HANDS, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4049, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4024, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4045, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4045, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Gloves", 4029, Slot.HANDS, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));

            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 308, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 303, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 293, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 293, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 4", 1000, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 986, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 179, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 224, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 224, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Gloves 5", 1263, Slot.HANDS, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            //rr5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 754, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Berserker, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 1191, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Bonedancer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 701, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Healer, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 759, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Hunter, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 706, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Runemaster, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 1195, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Savage, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 764, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Shadowblade, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 768, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Shaman, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 774, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Skald, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 802, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Spiritmaster, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 3373, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Thane, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Gloves", 779, Slot.HANDS, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Warrior, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));


            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            // Amor Boots
            //---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            //Generic

            //drake
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Boots", 3787, Slot.FEET, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Boots", 3762, Slot.FEET, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Boots", 3781, Slot.FEET, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Boots", 3781, Slot.FEET, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Boots", 3767, Slot.FEET, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Boots", 3777, Slot.FEET, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonsworn Boots", 3772, Slot.FEET, 0, 0, 1, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));

            //epic boss kill
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Boots", 2733, Slot.FEET, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Boots", 2739, Slot.FEET, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Boots", 2745, Slot.FEET, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Boots", 2745, Slot.FEET, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Boots", 2751, Slot.FEET, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Boots", 2751, Slot.FEET, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Possessed Realm Boots", 2757, Slot.FEET, 0, 0, 0, 0, 1, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            // craft
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Boots", 2795, Slot.FEET, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Boots", 2801, Slot.FEET, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Boots", 2807, Slot.FEET, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Boots", 2807, Slot.FEET, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Boots", 2813, Slot.FEET, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Boots", 2813, Slot.FEET, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Realm Boots", 2819, Slot.FEET, 0, 0, 0, 0, 0, 1, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            //orbs
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Boots", 3021, Slot.FEET, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Boots", 2992, Slot.FEET, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Boots", 3015, Slot.FEET, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Boots", 3015, Slot.FEET, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Boots", 2998, Slot.FEET, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Boots", 3004, Slot.FEET, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Shar Boots", 3010, Slot.FEET, 0, 0, 0, 100000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Boots", 3062, Slot.FEET, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Boots", 3067, Slot.FEET, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Boots", 3119, Slot.FEET, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Boots", 3119, Slot.FEET, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Boots", 3046, Slot.FEET, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Boots", 3051, Slot.FEET, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, festive));
            VendorItemList.Add(new DbSkinVendorItem("Good Inconnu Boots", 3056, Slot.FEET, 0, 0, 0, 250000, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, festive));

            //rr4
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Boots", 1629, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Boots", 1643, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Boots", 1851, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Boots", 1851, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Boots", 2104, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Boots", 1775, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Oceanus Boots", 2095, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Boots", 2157, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Boots", 2139, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Boots", 1761, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Boots", 1761, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Boots", 1813, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Boots", 1793, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Stygia Boots", 2127, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Boots", 2166, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Boots", 2180, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Boots", 1784, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Boots", 1784, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Boots", 1698, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Boots", 1716, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Volcanus Boots", 1706, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Boots", 2242, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Boots", 2148, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Boots", 1802, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Boots", 1802, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Boots", 1740, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Boots", 1752, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, toageneric));
            VendorItemList.Add(new DbSkinVendorItem("Aerus Boots", 1688, Slot.FEET, 0, 4, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, toageneric));
            //rr6
            VendorItemList.Add(new DbSkinVendorItem("Enyalio's Boots", 2488, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Enyalio's Boots", 2488, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Enyalio's Boots", 2488, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Enyalio's Boots", 2488, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Enyalio's Boots", 2488, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Enyalio's Boots", 2488, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Enyalio's Boots", 2488, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Flamedancer's Boots", 1731, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Flamedancer's Boots", 1731, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Flamedancer's Boots", 1731, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Flamedancer's Boots", 1731, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Flamedancer's Boots", 1731, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Flamedancer's Boots", 1731, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, artifact));
            VendorItemList.Add(new DbSkinVendorItem("Flamedancer's Boots", 1731, Slot.FEET, 0, 6, 0, 0, 0, 0, (int)eRealm.None, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, artifact));

            //alb
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 143, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 40, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 54, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 54, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 45, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 50, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 143, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 78, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 84, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 84, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 185, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 90, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 143, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 133, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 160, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 160, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 190, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 205, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4019, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 3994, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4013, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4013, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 3999, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4004, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, dragonCost * 2));
            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 143, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 138, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 220, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 220, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 195, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 210, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 143, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 138, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 225, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 225, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 1250, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 215, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Unknown, (int)eObjectType.Plate, (int)eDamageType._FirstResist, lowbie));
            //rr5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 692, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Armsman, (int)eObjectType.Plate, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 0, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Cabalist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 717, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Cleric, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 0, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Friar, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 796, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Infiltrator, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 722, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Mercenary, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 727, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Minstrel, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 0, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Necromancer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 697, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Paladin, (int)eObjectType.Plate, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 1270, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Reaver, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 731, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Scout, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 731, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Scout, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 0, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Sorcerer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 0, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Theurgist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 0, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Albion, (int)eCharacterClass.Wizard, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));

            //hib
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 362, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 377, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 367, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 367, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 372, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 382, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 397, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 387, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 387, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 392, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 422, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 417, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 407, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 407, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 412, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4103, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4078, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4097, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4097, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4093, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, dragonCost * 2));
            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 342, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 437, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 427, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 427, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 432, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 342, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 437, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 1260, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 1260, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 352, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Unknown, (int)eObjectType.Scale, (int)eDamageType._FirstResist, lowbie));
            //rr5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 0, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Animist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 738, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Bard, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 786, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Blademaster, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 814, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Champion, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 743, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Druid, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 0, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Eldritch, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 0, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Enchanter, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 712, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Hero, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 0, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Mentalist, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 750, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Nightshade, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 819, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Ranger, (int)eObjectType.Studded, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 0, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Valewalker, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 809, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Hibernia, (int)eCharacterClass.Warden, (int)eObjectType.Scale, (int)eDamageType._FirstResist, epic));
            //mid
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 249, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 150, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 234, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 234, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 1", 239, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 269, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 244, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 254, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 254, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 2", 259, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 289, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 264, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 274, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 274, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 3", 279, Slot.FEET, 0, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, freebie));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4050, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4025, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4044, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4044, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, dragonCost * 2));
            VendorItemList.Add(new DbSkinVendorItem("Dragonslayer Boots", 4030, Slot.FEET, 0, 0, 25, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, dragonCost * 2));

            //rr2
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 309, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 284, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 294, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 294, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 4", 1001, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 987, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 304, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Leather, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 225, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Studded, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 225, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, lowbie));
            VendorItemList.Add(new DbSkinVendorItem("Crafted Boots 5", 1264, Slot.FEET, 0, 2, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Unknown, (int)eObjectType.Chain, (int)eDamageType._FirstResist, lowbie));
            //rr5
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 755, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Berserker, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 1190, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Bonedancer, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 702, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Healer, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 760, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Hunter, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 707, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Runemaster, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 1196, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Savage, (int)eObjectType.Reinforced, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 765, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Shadowblade, (int)eObjectType.Leather, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 770, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Shaman, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 775, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Skald, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 803, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Spiritmaster, (int)eObjectType.Cloth, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 3374, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Thane, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));
            VendorItemList.Add(new DbSkinVendorItem("Class Epic Boots", 780, Slot.FEET, 5, 0, 0, 0, 0, 0, (int)eRealm.Midgard, (int)eCharacterClass.Warrior, (int)eObjectType.Chain, (int)eDamageType._FirstResist, epic));

            return VendorItemList;

            // var added = GameServer.Database.AddObject(VendorItemList);
            // var itemCount = GameServer.Database.GetObjectCount<SkinVendorItem>();

        }
    }
}
