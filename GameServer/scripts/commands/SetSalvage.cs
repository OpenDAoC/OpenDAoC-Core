using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.SalvageCalc;

namespace DOL.GS.Commands
{
    [Cmd("&setsalvage",
         ePrivLevel.GM,
         "/setsalvage - the item to modify have to be in lastbagpack slot (item saved and updated automatically)")]
    public class SetSalvageCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void OnCommand(GameClient client, string[] args)
        {
         
            int slot = (int)eInventorySlot.LastBackpack;

            InventoryItem item = client.Player.Inventory.GetItem((eInventorySlot)slot);

            if (item == null)
            {
                client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Item.Count.NoItemInSlot", slot), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            
            string idnb = item.Id_nb;


            if (idnb == string.Empty && (item.AllowAdd == false || item.Id_nb == InventoryItem.BLANK_ITEM))
            {
                DisplayMessage(client, "This item can' be configured.");
                return;
            }
            if (idnb == string.Empty)
            {
                DisplayMessage(client, "This item can' be configured.");
                return;
            }

            (item.Template as ItemTemplate).AllowUpdate = true;
            (item.Template as ItemTemplate).Dirty = true;

            ItemTemplate temp = GameServer.Database.FindObjectByKey<ItemTemplate>(idnb);
            
            if (temp == null)
            {
                DisplayMessage(client, "This item can' be configured.");
                return;
            }
            var sCalc = new SalvageCalculator();
            var ReturnSalvage = sCalc.GetSalvage(client.Player, item);

            var oldprice = item.Price;
            
            item.Condition = 50000;
            item.MaxCondition = 50000;
            item.Charges = item.MaxCharges;
            item.Charges1 = item.MaxCharges1;
            item.Price = ReturnSalvage.MSRP;
            
            GameServer.Database.SaveObject(item.Template);
            GameServer.Database.UpdateInCache<ItemTemplate>(item.Template.Id_nb);
            client.Out.SendInventoryItemsUpdate(new InventoryItem[] { item });

            DisplayMessage(client, $"{item.Name} price changed from {oldprice} to {item.Price}!");
        }
    }
}