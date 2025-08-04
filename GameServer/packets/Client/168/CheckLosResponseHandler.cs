namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.CheckLosRequest, "Handles a LoS Check Response", eClientStatus.PlayerInGame)]
    public class CheckLosResponseHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            ushort checkerObjectId = packet.ReadShort();
            ushort targetObjectId = packet.ReadShort();
            LosCheckResponse response = (packet.ReadShort() & 0x100) == 0x100 ? LosCheckResponse.True : LosCheckResponse.False;
            // packet.ReadShort(); ?

            client.Player.LosCheckHandler.SetResponse(checkerObjectId, targetObjectId, response);
        }
    }
}
