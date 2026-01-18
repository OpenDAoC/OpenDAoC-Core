namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerSitRequest, "Handles Player Sit Request.", eClientStatus.PlayerInGame)]
    public class PlayerSitRequestHandler : PacketHandler
    {
        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            byte status = (byte) packet.ReadByte();
            client.Player.Sit(status != 0x00);
        }
    }
}
