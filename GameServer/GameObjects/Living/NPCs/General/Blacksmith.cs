using System;
using System.Collections;
using Core.Base;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.Enums;
using Core.GS.Events;
using Core.GS.GameUtils;
using Core.GS.Languages;

namespace Core.GS;

[NpcGuildScript("Smith")]
public class Blacksmith : GameNpc
{
    private const string REPAIR_ITEM_WEAK = "repair item";
    private const double REPAIR_ALL_TAX = 0.20;

    /// <summary>
    ///     Can accept any item
    /// </summary>
    public override bool CanTradeAnyItem => true;


    #region Examine Messages

    /// <summary>
    ///     Adds messages to array, which are all sent when the NPC is examined (i.e., clicked on) by a GamePlayer.
    /// </summary>
    /// <param name="player">The GamePlayer examining the NPC.</param>
    /// <returns>Returns the messages in the array and sends them all to the player.</returns>
    public override IList GetExamineMessages(GamePlayer player)
    {
        IList list = new ArrayList();
        // Message: You target [{0}].
        list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Blacksmith.YouTarget",
            GetName(0, false, player.Client.Account.Language, this)));
        // Message: You examine {0}. {1} is {2} and is a smith.
        list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Blacksmith.YouExamine",
            GetName(0, false, player.Client.Account.Language, this),
            GetPronoun(0, true, player.Client.Account.Language),
            GetAggroLevelString(player, false)));
        // Message: [Give {0} an item to be repaired]
        list.Add(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameNPC.Blacksmith.GiveObject",
            GetPronoun(0, false, player.Client.Account.Language)));
        return list;
    }

    #endregion Examine Messages

    #region Interact Message

    /// <summary>
    ///     Behaviors that occur when a GamePlayer interacts with the NPC (e.g., facing player, send interact message, etc.).
    /// </summary>
    /// <param name="player">The GamePlayer interacting with the NPC.</param>
    /// <returns>If the interaction is prevented by the base class on GamePlayer.cs, then return 'false'.</returns>
    public override bool Interact(GamePlayer player)
    {
        // If the interaction is prevented by the base class on GamePlayer.cs, then return 'false'
        if (!base.Interact(player))
            return false;

        // NPC faces the player that just interacted with them for #### milliseconds
        TurnTo(player, 5000);

        // Check for ambient trigger messages for the NPC in the 'MobXAmbientBehaviour' table
        // var triggers = GameServer.Instance.NpcManager.AmbientBehaviour[base.Name];
        // // If the NPC has no ambient trigger message assigned, then return this message
        // if (triggers == null || triggers.Length == 0)

        // Message: {0} says, "I can repair weapons or armor for you. Just hand me the item you want repaired and I'll see what I can do, for a small fee."
        SayTo(player, EChatLoc.CL_PopupWindow,
            "I can repair weapons or armor for you. Just hand me the item you want repaired and I'll see what I can do, for a small fee.");
        SayTo(player, EChatLoc.CL_PopupWindow,
            $"If you're in a hurry, I can also [repair all] your items for an additional {REPAIR_ALL_TAX * 100}% fee.");

        return true;
    }

    public override bool WhisperReceive(GameLiving source, string text)
    {
        if (!base.WhisperReceive(source, text) || !(source is GamePlayer player))
            return false;

        if (text.ToLower() != "repair all") return false;
        AskRepairAll(player);
        return true;
    }

    #endregion Interact Messages

    #region Receive Responses

    /// <summary>
    ///     Behaviors that occur when an NPC is given an item (primarily to repair, but also for XP item turn-in).
    /// </summary>
    /// <param name="source">The entity giving the item to the NPC (e.g., 'GamePlayer player').</param>
    /// ///
    /// <param name="item">The specific item being given to the NPC.</param>
    public override bool ReceiveItem(GameLiving source, DbInventoryItem item)
    {
        var player = source as GamePlayer;
        if (player == null || item == null)
            return false;

        if (DataQuestList.Count > 0)
            foreach (var quest in DataQuestList)
                quest.Notify(GameObjectEvent.ReceiveItem, this, new ReceiveItemEventArgs(source, this, item));

        if (item.Count != 1)
        {
            // Message: {0} doesn't want stacked items.
            ChatUtil.SendErrorMessage(player, "GameNPC.Blacksmith.StackedObjets", GetName(0, true));

            return false;
        }

        switch (item.Object_Type)
        {
            case (int) EObjectType.GenericItem:
            case (int) EObjectType.Magical:
            case (int) EObjectType.Instrument:
            case (int) EObjectType.Poison:
                // Message: {0} says, "I can't repair that."
                ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.CantRepairThat", GetName(0, true));

                return false;
        }

        if (item.Condition < item.MaxCondition)
        {
            if (item.Durability <= 0)
            {
                // Message: {0} says, "I can't repair that."
                ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.CantRepairThat", GetName(0, true));

                return false;
            }

            player.TempProperties.SetProperty(REPAIR_ITEM_WEAK, new WeakRef(item));
            // Pop-up dialog prompting repair with ACCEPT/DECLINE options
            // Message: It will cost {0} to repair {1}. Do you accept?
            player.Client.Out.SendCustomDialog(LanguageMgr.GetTranslation(player.Client.Account.Language,
                "GameNPC.Blacksmith.RepairCostAccept",
                MoneyMgr.GetString(item.RepairCost), item.GetName(0, false)), BlacksmithDialogResponse);
        }
        else
        {
            // Message: Your {0} is already in perfect condition.
            ChatUtil.SendErrorMessage(player, "GameNPC.Blacksmith.NoNeedRepair", item.Name);
        }


        return false;
    }

    #endregion Receive Responses

    #region Repair Responses

    /// <summary>
    ///     Responses that occur when an NPC attempts to repair an item given to them.
    /// </summary>
    /// <param name="player">The entity that originally gave the item to the NPC.</param>
    /// ///
    /// <param name="response">The player's response when prompted to initiate the repair (ACCEPT/DECLINE).</param>
    protected void BlacksmithDialogResponse(GamePlayer player, byte response)
    {
        var itemWeak = player.TempProperties.GetProperty<WeakReference>(REPAIR_ITEM_WEAK, new WeakRef(null));
        player.TempProperties.RemoveProperty(REPAIR_ITEM_WEAK);
        var item = (DbInventoryItem) itemWeak.Target;

        if (response != 0x01)
        {
            // Message: You decline to have your {0} repaired.
            ChatUtil.SendSystemMessage(player, "GameNPC.Blacksmith.AbortRepair", item.Name);

            return;
        }


        if (item == null || item.SlotPosition == (int) EInventorySlot.Ground
                         || item.OwnerID == null || item.OwnerID != player.InternalID)
        {
            // Message: {0} says, "I can't repair that."
            ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.CantRepairThat", GetName(0, true));

            return;
        }

        var ToRecoverCond = item.MaxCondition - item.Condition;

        if (!player.RemoveMoney(item.RepairCost))
        {
            InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, item.RepairCost);
            // Message: {0} says, "It costs {1} to repair {2}. You don't have that much."
            ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.NotEnoughMoney",
                GetName(0, true),
                MoneyMgr.GetString(item.RepairCost),
                item.GetName(0, false));

            return;
        }

        // Message: You pay {0} {1}.
        ChatUtil.SendSystemMessage(player, "GameNPC.Blacksmith.YouPay", GetName(0, false),
            MoneyMgr.GetString(item.RepairCost));

        // Items with IsNotLosingDur are not....losing DUR.
        if (ToRecoverCond + 1 >= item.Durability)
        {
            item.Condition = item.Condition + item.Durability;
            item.Durability = 0;
            // Message: {0} says, "Uhh, {1} is rather old. I won't be able to repair it again, so be careful!"
            ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.ObjectRatherOld", GetName(0, true),
                item.GetName(0, false));
        }
        else
        {
            item.Condition = item.MaxCondition;
            if (!item.IsNotLosingDur) item.Durability -= ToRecoverCond + 1;
        }


        player.Out.SendInventoryItemsUpdate(new[] {item});
        // Message: {0} says, "There, {1} is ready for combat."
        ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.ItsDone", GetName(0, true), item.GetName(0, false));
    }

    #endregion Repair Responses

    #region repair all

    private void AskRepairAll(GamePlayer player)
    {
        long TotalCost = 0;
        foreach (var inventoryItem in player.Inventory.AllItems)
        {
            if (!CanBeRepaired(inventoryItem)) continue;
            TotalCost += CalculateCost(inventoryItem);
        }

        if (TotalCost > 0)
            player.Client.Out.SendCustomDialog(
                $"It will cost {MoneyMgr.GetString(TotalCost)} to repair everything. Do you accept?", RepairAll);
        else
            SayTo(player, EChatLoc.CL_PopupWindow,
                "All items are fully repaired already.");
    }

    private bool CanBeRepaired(DbInventoryItem item)
    {
        if (item == null || item.SlotPosition == (int) EInventorySlot.Ground
                         || item.OwnerID == null) return false;
        if (item.Condition == item.MaxCondition) return false;
        if (item.RepairCost == 0) return false; // skipping items with no template price - hopefully we'll get tickets and we'll adjust the prices

        return true;
    }

    private long CalculateCost(DbInventoryItem item)
    {
        long NeededMoney = 0;
        NeededMoney = ((item.Template.MaxCondition - item.Condition) * item.Template.Price) /
                      item.Template.MaxCondition;

        var tax = NeededMoney * REPAIR_ALL_TAX;

        return NeededMoney + (long) tax;
    }

    private void RepairAll(GamePlayer player, byte response)
    {
        if (response != 0x01)
        {
            player.Out.SendMessage(
                LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "GameNPC.Blacksmith.AbortRepair", "inventory"), EChatType.CT_System,
                EChatLoc.CL_SystemWindow);
            return;
        }

        long cost = 0;
        foreach (var inventoryItem in player.Inventory.AllItems)
        {
            if (!CanBeRepaired(inventoryItem)) continue;
            cost += CalculateCost(inventoryItem);
        }

        if (!player.RemoveMoney(cost))
        {
            SayTo(player, EChatLoc.CL_PopupWindow,
                LanguageMgr.GetTranslation(player.Client.Account.Language,
                    "GameNPC.Blacksmith.NotEnoughMoney", MoneyMgr.GetString(cost), "everything"));
            return;
        }


        InventoryLogging.LogInventoryAction(player, this, EInventoryActionType.Merchant, cost);

        ChatUtil.SendSystemMessage(player, "GameNPC.Blacksmith.YouPay", GetName(0, false),
            MoneyMgr.GetString(cost));


        foreach (var inventoryItem in player.Inventory.AllItems)
        {
            Repair(inventoryItem, player);
            player.Out.SendInventoryItemsUpdate(new[] {inventoryItem});
        }

        SayTo(player, EChatLoc.CL_PopupWindow,
            LanguageMgr.GetTranslation(player.Client.Account.Language,
                "Scripts.Recharger.RechargerDialogResponse.FullyCharged"));
    }

    private void Repair(DbInventoryItem item, GamePlayer player)
    {
        var ToRecoverCond = item.MaxCondition - item.Condition;

        // Items with IsNotLosingDur are not....losing DUR.
        if (ToRecoverCond + 1 >= item.Durability)
        {
            item.Condition = +item.Durability;
            item.Durability = 0;
            // Message: {0} says, "Uhh, {1} is rather old. I won't be able to repair it again, so be careful!"
            ChatUtil.SendSayMessage(player, "GameNPC.Blacksmith.ObjectRatherOld", GetName(0, true),
                item.GetName(0, false));
        }
        else
        {
            item.Condition = item.MaxCondition;
            if (!item.IsNotLosingDur) item.Durability -= ToRecoverCond + 1;
        }
    }

    #endregion
}
