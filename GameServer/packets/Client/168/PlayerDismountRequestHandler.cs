namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerDismountRequest, "Handles Player Dismount Request.", eClientStatus.PlayerInGame)]
    public class PlayerDismountRequestHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            GamePlayer player = client.Player;

            if (!player.IsRiding)
            {
                ChatUtil.SendSystemMessage(player, "You are not riding any steed!");
                return;
            }

            player.DismountSteed(false);
        }
    }
}
