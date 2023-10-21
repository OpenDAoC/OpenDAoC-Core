using Core.Database.Tables;
using Core.GS.Crafting;
using Core.GS.Enums;
using Core.GS.Languages;

namespace Core.GS.Commands;

[Command("&setsalvage",
     EPrivLevel.GM,
     "/setsalvage - the item to modify have to be in lastbagpack slot (item saved and updated automatically)")]
public class SetSalvageCommand : ACommandHandler, ICommandHandler
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public void OnCommand(GameClient client, string[] args)
    {
     
        int slot = (int)EInventorySlot.LastBackpack;

        DbInventoryItem item = client.Player.Inventory.GetItem((EInventorySlot)slot);

        if (item == null)
        {
            client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Item.Count.NoItemInSlot", slot), EChatType.CT_System, EChatLoc.CL_SystemWindow);
            return;
        }
        
        string idnb = item.Id_nb;


        if (idnb == string.Empty && (item.AllowAdd == false || item.Id_nb == DbInventoryItem.BLANK_ITEM))
        {
            DisplayMessage(client, "This item can' be configured.");
            return;
        }
        if (idnb == string.Empty)
        {
            DisplayMessage(client, "This item can' be configured.");
            return;
        }

        (item.Template as DbItemTemplate).AllowUpdate = true;
        (item.Template as DbItemTemplate).Dirty = true;

        DbItemTemplate temp = GameServer.Database.FindObjectByKey<DbItemTemplate>(idnb);
        
        if (temp == null)
        {
            DisplayMessage(client, "This item can' be configured.");
            return;
        }
        var sCalc = new SalvagingCalculator();
        var ReturnSalvage = sCalc.GetSalvage(client.Player, item);

        var oldprice = item.Price;
        
        item.Condition = 50000;
        item.MaxCondition = 50000;
        item.Charges = item.MaxCharges;
        item.Charges1 = item.MaxCharges1;
        item.Price = ReturnSalvage.MSRP;
        
        GameServer.Database.SaveObject(item.Template);
        GameServer.Database.UpdateInCache<DbItemTemplate>(item.Template.Id_nb);
        client.Out.SendInventoryItemsUpdate(new DbInventoryItem[] { item });

        DisplayMessage(client, $"{item.Name} price changed from {oldprice} to {item.Price}!");
    }
}