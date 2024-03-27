namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.SetMarketPrice, "Set Market/Consignment Merchant Price.", eClientStatus.PlayerInGame)]
    public class PlayerSetMarketPriceHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            if (client == null || client.Player == null || client.Player.TargetObject is not IGameInventoryObject inventoryObject)
                return;

            int slot = packet.ReadByte();
            _ = packet.ReadByte();
            _ = packet.ReadShort();
            uint price = packet.ReadInt();
            inventoryObject.SetSellPrice(client.Player, (eInventorySlot) slot, price);
        }
    }
}
