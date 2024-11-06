namespace DOL.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// Handles the disband group packet
    /// </summary>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.DisbandFromGroup, "Disband From Group Request Handler", eClientStatus.PlayerInGame)]
    public class DisbandFromGroupHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            GamePlayer player = client.Player;

            if (player.Group == null)
                return;

            GameLiving disbandMember = player;

            if (player.TargetObject != null &&
                player.TargetObject is GameLiving livingTarget &&
                livingTarget.Group != null &&
                livingTarget.Group == player.Group)
            {
                disbandMember = livingTarget;
            }

            if (disbandMember != player && player != player.Group.Leader)
                return;

            player.Group.RemoveMember(disbandMember);
        }
    }
}
