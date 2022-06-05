using System;
using System.Collections;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS;

[NPCGuildScript("Recharger")]
public class Recharger : GameNPC
{
    private const string RECHARGE_ITEM_WEAK = "recharged item";
    private const double RECHARGE_ALL_TAX = 0.20;

    /// <summary>
    ///     Can accept any item
    /// </summary>
    public override bool CanTradeAnyItem => true;

    #region Examine/Interact Message

    public override IList GetExamineMessages(GamePlayer player)
    {
        IList list = new ArrayList();
        list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Recharger.GetExamineMessages",
            GetName(0, false, player.Client.Account.Language, this),
            GetPronoun(0, true, player.Client.Account.Language), GetAggroLevelString(player, false)));
        return list;
    }

    public override bool Interact(GamePlayer player)
    {
        if (!base.Interact(player))
            return false;

        TurnTo(player.X, player.Y);
        
        // Message: "I can recharge weapons or armor for you, Just hand me the item you want recharged and I'll see what I can do, for a small fee."
        SayTo(player, eChatLoc.CL_PopupWindow,
                LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Recharger.Interact"));
        SayTo(player, eChatLoc.CL_PopupWindow,
            $"If you're in a hurry, I can also [recharge all] your items for an additional {RECHARGE_ALL_TAX*100}% fee.");
        
        return true;
    }

    public override bool WhisperReceive(GameLiving source, string text)
    {
        if (!base.WhisperReceive(source, text) || !(source is GamePlayer player))
            return false;

        if (text.ToLower() != "recharge all") return false;
        AskRechargeAll(player);
        return true;
    }

    #endregion Examine/Interact Message

    #region Receive item

    public override bool ReceiveItem(GameLiving source, InventoryItem item)
    {
        if (source is not GamePlayer player || item == null)
            return false;

        if (DataQuestList.Count > 0)
            foreach (var quest in DataQuestList)
                quest.Notify(GameObjectEvent.ReceiveItem, this, new ReceiveItemEventArgs(source, this, item));

        if (item.Count != 1)
        {
            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "Scripts.Recharger.ReceiveItem.StackedObjects",
                    GetName(0, false, player.Client.Account.Language, this)), eChatType.CT_System,
                eChatLoc.CL_SystemWindow);
            return false;
        }

        if ((item.SpellID == 0 && item.SpellID1 == 0) ||
            item.Object_Type == (int) eObjectType.Poison ||
            (item.Object_Type == (int) eObjectType.Magical && (item.Item_Type == 40 || item.Item_Type == 41)))
        {
            SayTo(player,
                LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "Scripts.Recharger.ReceiveItem.CantThat"));
            return false;
        }

        if (item.Charges == item.MaxCharges && item.Charges1 == item.MaxCharges1)
        {
            SayTo(player,
                LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "Scripts.Recharger.ReceiveItem.FullyCharged"));
            return false;
        }

        long NeededMoney = 0;
        if (item.Charges < item.MaxCharges)
        {
            player.TempProperties.setProperty(RECHARGE_ITEM_WEAK, new WeakRef(item));
            NeededMoney += (item.MaxCharges - item.Charges) * Money.GetMoney(0, 0, 10, 0, 0);
        }

        if (item.Charges1 < item.MaxCharges1)
        {
            player.TempProperties.setProperty(RECHARGE_ITEM_WEAK, new WeakRef(item));
            NeededMoney += (item.MaxCharges1 - item.Charges1) * Money.GetMoney(0, 0, 10, 0, 0);
        }

        if (NeededMoney > 0)
        {
            player.Client.Out.SendCustomDialog(
                LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Recharger.ReceiveItem.Cost",
                    Money.GetString(NeededMoney)), RechargerDialogResponse);
            return true;
        }

        return false;
    }

    private void RechargerDialogResponse(GamePlayer player, byte response)
    {
        var itemWeak =
            (WeakReference) player.TempProperties.getProperty<object>(
                RECHARGE_ITEM_WEAK,
                new WeakRef(null)
            );
        player.TempProperties.removeProperty(RECHARGE_ITEM_WEAK);

        var item = (InventoryItem) itemWeak.Target;

        if (item == null || item.SlotPosition == (int) eInventorySlot.Ground
                         || item.OwnerID == null || item.OwnerID != player.InternalID)
        {
            player.Out.SendMessage(
                LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "Scripts.Recharger.RechargerDialogResponse.InvalidItem"), eChatType.CT_System,
                eChatLoc.CL_SystemWindow);
            return;
        }

        if (response != 0x01)
        {
            player.Out.SendMessage(
                LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "Scripts.Recharger.RechargerDialogResponse.Decline", item.Name), eChatType.CT_System,
                eChatLoc.CL_SystemWindow);
            return;
        }

        long cost = 0;
        if (item.Charges < item.MaxCharges) cost += (item.MaxCharges - item.Charges) * Money.GetMoney(0, 0, 10, 0, 0);

        if (item.Charges1 < item.MaxCharges1)
            cost += (item.MaxCharges1 - item.Charges1) * Money.GetMoney(0, 0, 10, 0, 0);

        if (!player.RemoveMoney(cost))
        {
            player.Out.SendMessage(
                LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "Scripts.Recharger.RechargerDialogResponse.NotMoney"), eChatType.CT_System,
                eChatLoc.CL_SystemWindow);
            return;
        }

        InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, cost);

        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,
                "Scripts.Recharger.RechargerDialogResponse.GiveMoney",
                GetName(0, false, player.Client.Account.Language, this), Money.GetString(cost)),
            eChatType.CT_System, eChatLoc.CL_SystemWindow);
        item.Charges = item.MaxCharges;
        item.Charges1 = item.MaxCharges1;

        player.Out.SendInventoryItemsUpdate(new[] {item});
        SayTo(player,
            LanguageMgr.GetTranslation(player.Client.Account.Language,
                "Scripts.Recharger.RechargerDialogResponse.FullyCharged"));
    }
    

    #endregion Receive item
    
    #region RechargeAll

    private void AskRechargeAll(GamePlayer player)
    {
        long TotalCost = 0;
        foreach (var inventoryItem in player.Inventory.AllItems)
        {
            if (!CanBeRecharged(inventoryItem)) continue;
            TotalCost += CalculateCost(inventoryItem);
        }
        
        if (TotalCost > 0)
            player.Client.Out.SendCustomDialog(
            LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Recharger.ReceiveItem.Cost",
                Money.GetString(TotalCost)), RechargeAll);
        else
            SayTo(player, eChatLoc.CL_PopupWindow,
                "All items are fully charged already.");
    }

    private void RechargeAll(GamePlayer player, byte response)
    {
        if (response != 0x01)
        {
            player.Out.SendMessage(
                LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "Scripts.Recharger.RechargerDialogResponse.Decline", "inventory"), eChatType.CT_System,
                eChatLoc.CL_SystemWindow);
            return;
        }

        long cost = 0;
        foreach (var inventoryItem in player.Inventory.AllItems)
        {
            if (!CanBeRecharged(inventoryItem)) continue;
            cost += CalculateCost(inventoryItem);
        }

        if (!player.RemoveMoney(cost))
        {
            SayTo(player,eChatLoc.CL_PopupWindow,
                LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "Scripts.Recharger.RechargerDialogResponse.NotMoney"));
            return;
        }

        InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, cost);

        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language,
                "Scripts.Recharger.RechargerDialogResponse.GiveMoney",
                GetName(0, false, player.Client.Account.Language, this), Money.GetString(cost)),
            eChatType.CT_System, eChatLoc.CL_SystemWindow);

        foreach (var inventoryItem in player.Inventory.AllItems)
        {
            Recharge(inventoryItem);
            player.Out.SendInventoryItemsUpdate(new[] {inventoryItem});
        }
        
        SayTo(player,eChatLoc.CL_PopupWindow,
            LanguageMgr.GetTranslation(player.Client.Account.Language,
                "Scripts.Recharger.RechargerDialogResponse.FullyCharged"));
    }

    private void Recharge(InventoryItem item)
    {
        
        if (item == null || item.SlotPosition == (int) eInventorySlot.Ground
                         || item.OwnerID == null) return;

        item.Charges = item.MaxCharges;
        item.Charges1 = item.MaxCharges1;
    }
    private long CalculateCost(InventoryItem item)
    {
        long NeededMoney = 0;
        if (item.Charges < item.MaxCharges)
        {
            NeededMoney += (item.MaxCharges - item.Charges) * Money.GetMoney(0, 0, 10, 0, 0);
        }
        if (item.Charges1 < item.MaxCharges1)
        {
            NeededMoney += (item.MaxCharges1 - item.Charges1) * Money.GetMoney(0, 0, 10, 0, 0);
        }

        var tax = NeededMoney * RECHARGE_ALL_TAX;
        
        NeededMoney += (long)tax;

        return NeededMoney;
    }

    private bool CanBeRecharged(InventoryItem item)
    {
        if (item.Count != 1)
        {
            return false;
        }

        if ((item.SpellID == 0 && item.SpellID1 == 0) ||
            item.Object_Type == (int) eObjectType.Poison ||
            (item.Object_Type == (int) eObjectType.Magical && (item.Item_Type == 40 || item.Item_Type == 41)))
        {
            return false;
        }

        if (item.Charges == item.MaxCharges && item.Charges1 == item.MaxCharges1)
        {
            return false;
        }

        return true;
    }
    #endregion
}