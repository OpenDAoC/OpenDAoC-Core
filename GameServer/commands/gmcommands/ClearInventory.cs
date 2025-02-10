using DOL.Database;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    [Cmd(
        "&clearinventory",
        ePrivLevel.GM,
        "/clearinventory - clears your entire inventory")]
    public class ClearInventoryCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            foreach (DbInventoryItem item in client.Player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
                    client.Player.Inventory.RemoveItem(item);

            client.Out.SendMessage("Inventory cleared", eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
