namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.GameOpenRequest, "Checks if UDP is working for the client", eClientStatus.None)]
    public class GameOpenRequestHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            int flag = packet.ReadByte(); // Always 0? (1.127)
            client.UdpPingTime = GameLoop.GameLoopTime;
            client.UdpConfirm = flag == 1;
            client.Out.SendGameOpenReply();
            GameLoopService.Instance.Post(client => client.Out.SendStatusUpdate(), client); // Dirty hack. Doesn't seem to work if sent immediately.
            client.Out.SendUpdatePoints();
            client.Player?.UpdateDisabledSkills();
        }
    }
}
