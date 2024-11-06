namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.InviteToGroup, "Handle Invite to Group Request.", eClientStatus.PlayerInGame)]
    public class InviteToGroupHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            GamePlayer player = client.Player;

            if (player.TargetObject == null || player.TargetObject == player)
            {
                ChatUtil.SendSystemMessage(player, "You have not selected a valid player as your target.");
                return;
            }

            if (player.TargetObject is not GamePlayer target)
            {
                ChatUtil.SendSystemMessage(player, "You have not selected a valid player as your target.");
                return;
            }

            if (player.Group != null && player.Group.Leader != player)
            {
                ChatUtil.SendSystemMessage(player, "You are not the leader of your group.");
                return;
            }

            if (player.Group != null && player.Group.MemberCount >= ServerProperties.Properties.GROUP_MAX_MEMBER)
            {
                ChatUtil.SendSystemMessage(player, "The group is full.");
                return;
            }

            if (!GameServer.ServerRules.IsAllowedToGroup(player, target, false))
                return;

            if (target.Group != null)
            {
                ChatUtil.SendSystemMessage(player, "The player is still in a group.");
                return;
            }

            ChatUtil.SendSystemMessage(player, $"You have invited {target.Name} to join your group.");
            target.Out.SendGroupInviteCommand(player, $"{player.Name} has invited you to join\n{player.GetPronoun(1, false)} group. Do you wish to join?");
            ChatUtil.SendSystemMessage(target, $"{player.Name} has invited you to join {player.GetPronoun(1, false)} group.");
        }
    }
}
