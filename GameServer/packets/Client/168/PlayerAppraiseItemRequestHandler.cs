using DOL.Database;
using DOL.GS.Housing;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerAppraiseItemRequest, "Player Appraise Item Request handler.", eClientStatus.PlayerInGame)]
    public class PlayerAppraiseItemRequestHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            uint X = packet.ReadInt();
            uint Y = packet.ReadInt();
            ushort id = packet.ReadShort();
            eInventorySlot slot = (eInventorySlot) packet.ReadShort();
            GamePlayer player = client.Player;

            if (player.TargetObject == null)
                return;

            DbInventoryItem item = player.Inventory.GetItem(slot);

            if (player.TargetObject is GameMerchant merchant)
                merchant.OnPlayerAppraise(player, item, false);
            else if (player.TargetObject is GameLotMarker lot)
                lot.OnPlayerAppraise(player, item, false);

            return;
        }
    }
}
