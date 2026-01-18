namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.GameOpenRequest, "Checks if UDP is working for the client", eClientStatus.None)]
    public class GameOpenRequestHandler : PacketHandler
    {
        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            if (client.ClientState is not GameClient.eClientState.WorldEnter)
                return;

            client.Player.PlayerObjectCache.Clear();
            client.ClientState = GameClient.eClientState.Playing;
            int flag = packet.ReadByte(); // Always 0? (1.127)
            client.UdpPingTime = GameLoop.GameLoopTime;
            client.UdpConfirm = flag == 1;
            client.Out.SendGameOpenReply();
            GameLoopThreadPool.Context.Post(static state => (state as GameClient).Out.SendStatusUpdate(), client); // Dirty hack. Doesn't seem to work if sent immediately.
            client.Out.SendUpdatePoints();
            client.Player?.UpdateDisabledSkills();
        }
    }
}
