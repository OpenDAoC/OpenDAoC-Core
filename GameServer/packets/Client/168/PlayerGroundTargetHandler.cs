namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerGroundTarget, "Handles Player Ground Target Settings", eClientStatus.PlayerInGame)]
    public class PlayerGroundTargetHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            int groundX = (int) packet.ReadInt();
            int groundY = (int) packet.ReadInt();
            int groundZ = (int) packet.ReadInt();
            ushort flag = packet.ReadShort();
            // ushort unk2 = packet.ReadShort();

            GamePlayer player = client.Player;
            player.GroundTargetInView = (flag & 0x100) != 0;
            player.SetGroundTarget(groundX, groundY, (ushort) groundZ);

            if (!player.GroundTargetInView)
                player.Out.SendMessage("Your ground target is not visible!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            // ???
            // if (player.SiegeWeapon != null && player.SiegeWeapon.Owner == player)
            // {
            //     player.SiegeWeapon.Move();
            //     return 0;
            // }

            if (player.Steed != null && player.Steed.MAX_PASSENGERS >= 1 && player.Steed.OwnerID == player.InternalID)
            {
                if (player.Steed is GameTaxiBoat)
                    return;

                if (player.Steed is GameBoat)
                {
                    if (player.Steed.OwnerID == player.InternalID)
                    {
                        player.Out.SendMessage("You usher your boat forward.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        player.Steed.WalkTo(player.GroundTarget, player.Steed.MaxSpeed);
                        return;
                    }
                }

                if (player.Steed.MAX_PASSENGERS > 8 && player.Steed.CurrentRiders.Length < player.Steed.REQUIRED_PASSENGERS)
                {
                    player.Out.SendMessage($"The {player.Steed.Name} does not yet have enough passengers to move!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }

                player.Steed.WalkTo(player.GroundTarget, player.Steed.MaxSpeed);
            }
        }
    }
}
