using System;
using System.Numerics;
using DOL.GS.ServerProperties;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerGroundTarget, "Handles Player Ground Target Settings", eClientStatus.PlayerInGame)]
    public class PlayerGroundTargetHandler : PacketHandler
    {
        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            int groundX = (int) packet.ReadInt();
            int groundY = (int) packet.ReadInt();
            int groundZ = (int) packet.ReadInt();
            ushort flag = packet.ReadShort();
            // byte unk1 = (byte) packet.ReadByte(); // 0 when /groundset is used.
            // byte unk2 = (byte) packet.ReadByte(); // 0 when /groundset is used.

            GamePlayer player = client.Player;

            if (ValidateGroundTarget(player, ref groundX, ref groundY, ref groundZ))
            {
                // We can't adjust the client-side position without it printing confusing messages, since the packet is meant for /groundassist.
                // So we accept the current and subsequent LoS state sent by the client, even if the position is being adjusted.
                // This should be fine as long as snapMaxDistance isn't too high.
                player.GroundTargetInView = (flag & 0x100) != 0;
                player.SetGroundTarget(groundX, groundY, groundZ);
            }
            else
                player.GroundTarget.Unset();

            if (!player.GroundTargetInView)
                player.Out.SendMessage("Your ground target is not visible!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

            if (player.SiegeWeapon != null && player.SiegeWeapon.Owner == player)
            {
                player.SiegeWeapon.Move();
                return;
            }

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

        private static bool ValidateGroundTarget(GamePlayer player, ref int groundX, ref int groundY, ref int groundZ)
        {
            float snapMaxDistance = Properties.GROUND_TARGET_SNAP_MAX_DISTANCE;

            if (snapMaxDistance <= 0)
                return true;

            Zone zone = player.CurrentRegion.GetZone(groundX, groundY);

            if (zone == null)
                return false;

            // Not sure how ground targets are supposed to behave under water.
            if (zone.IsUnderwater(groundX, groundY, groundZ))
                return true;

            Vector3 position = new(groundX, groundY, groundZ);

            if (!PathfindingProvider.Instance.TrySnapToMesh(zone, ref position, snapMaxDistance))
                return false;

            groundX = (int) Math.Round(position.X);
            groundY = (int) Math.Round(position.Y);
            groundZ = (int) Math.Round(position.Z);
            return true;
        }
    }
}
