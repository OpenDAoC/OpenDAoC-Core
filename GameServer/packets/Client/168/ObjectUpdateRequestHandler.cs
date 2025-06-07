namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.ObjectUpdateRequest, "Update all GameObjects in Playerrange", eClientStatus.PlayerInGame)]
    public class ObjectUpdateRequestHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            // Ignore the request.
        }
    }
}
