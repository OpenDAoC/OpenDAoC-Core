namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerAttackRequest, "Handles Player Attack Request", eClientStatus.PlayerInGame)]
    public class PlayerAttackRequestHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            bool start = packet.ReadByte() != 0;
            bool userAction = packet.ReadByte() == 0; // Set to 0 if user pressed the button, set to 1 if client decided to stop attack.
            GamePlayer player = client.Player;

            if (player.ActiveWeaponSlot == eActiveWeaponSlot.Distance)
            {
                if (userAction)
                    player.Out.SendMessage("You can't enter melee combat mode with a ranged weapon!", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);

                return;
            }

            if (start && userAction)
                player.attackComponent.RequestStartAttack(player.TargetObject);
            else
                player.attackComponent.StopAttack();
        }
    }
}
