using DOL.GS.Housing;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.WithDrawMerchantMoney, "Withdraw GameConsignmentMerchant Merchant Money", eClientStatus.PlayerInGame)]
    public class PlayerWithdrawMerchantMoney : PacketHandler
    {
        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            if (client.Player == null)
                return;

            if (client.Player.ActiveInventoryObject is not GameConsignmentMerchant conMerchant)
                return;

            House house = conMerchant.CurrentHouse;

            if (house == null)
                return;

            if (!house.CanUseConsignmentMerchant(client.Player, ConsignmentPermissions.Withdraw))
            {
                client.Player.Out.SendMessage("You don't have permission to withdraw money from this merchant!", eChatType.CT_Important, eChatLoc.CL_ChatWindow);
                return;
            }

            conMerchant.WithdrawMoney(client.Player);
        }
    }
}
