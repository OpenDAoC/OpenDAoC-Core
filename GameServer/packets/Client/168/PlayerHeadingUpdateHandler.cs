using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerHeadingUpdate, "Handles Player Heading Update (Short State)", eClientStatus.PlayerInGame)]
    public class PlayerHeadingUpdateHandler : IPacketHandler
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            if (client.Player == null || client.Player.ObjectState is not GameObject.eObjectState.Active)
                return;

            packet.Skip(2); // Session ID.

            if (client.Version >= GameClient.eClientVersion.Version1127)
                packet.Skip(2); // Target.

            client.Player.Heading = packet.ReadShort();
            packet.Skip(1); // Unknown.
            PlayerPositionUpdateHandler.ProcessActionFlags(client.Player, (PlayerPositionUpdateHandler.ActionFlags) packet.ReadByte());
            packet.Skip(1); // Steed slot (supposedly).
            PlayerPositionUpdateHandler.ProcessStateFlags(client.Player, (PlayerPositionUpdateHandler.StateFlags) (packet.ReadByte() << 2)); // 1.127. Same as position update's state flags, but shifted to the right and without strafing bits.

            if (client.Player.Steed != null && client.Player.Steed.ObjectState is GameObject.eObjectState.Active)
                client.Player.Heading = client.Player.Steed.Heading;

            client.Player.OnHeadingPacketReceived();
        }

        public static void BroadcastHeading(GameClient client)
        {
            GamePlayer player = client.Player;
            byte actionFlags = (byte) PlayerPositionUpdateHandler.GetActionFlagsOut(player);
            byte stateFlags = (byte) ((byte) player.StateFlags >> 2);
            byte healthByte = (byte) (player.HealthPercent + (player.attackComponent.AttackState ? 0x80 : 0));
            byte steedSeatPosition = (byte) (player.Steed?.RiderSlot(player) ?? 0);
            ushort heading;

            if (player.Steed != null && player.Steed.ObjectState is GameObject.eObjectState.Active)
                heading = (ushort) client.Player.Steed.ObjectID;
            else
                heading = player.RawHeading;

            GSUDPPacketOut outPak1127 = null;
            GSUDPPacketOut outPak1124 = null;
            GSUDPPacketOut outPak190 = null;

            foreach (GamePlayer otherPlayer in client.Player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                if (otherPlayer == player || !otherPlayer.CanDetect(player))
                    continue;

                if (otherPlayer.Client.Version >= GameClient.eClientVersion.Version1127)
                {
                    outPak1127 ??= CreateOutPak1127();
                    otherPlayer.Out.SendUDP(outPak1127);
                }
                else if (otherPlayer.Client.Version >= GameClient.eClientVersion.Version1124)
                {
                    outPak1124 ??= CreateOutPak1124();
                    otherPlayer.Out.SendUDP(outPak1124);
                }
                else
                {
                    outPak190 ??= CreateOutPak190();
                    otherPlayer.Out.SendUDP(outPak190);
                }
            }

            GSUDPPacketOut CreateOutPak1127()
            {
                GSUDPPacketOut outPak = new(client.Out.GetPacketCode(eServerPackets.PlayerHeading));
                outPak.WriteShort((ushort) client.SessionID);
                outPak.WriteShort(0); // Current target.
                outPak.WriteShort(heading);
                outPak.WriteByte(steedSeatPosition);
                outPak.WriteByte(actionFlags);
                outPak.WriteByte(0);
                outPak.WriteByte(stateFlags);
                outPak.WriteByte(healthByte);
                outPak.WriteByte(player.ManaPercent);
                outPak.WriteByte(player.EndurancePercent);
                outPak.WriteByte(0); // Unknown.
                return outPak;
            }

            GSUDPPacketOut CreateOutPak1124()
            {
                GSUDPPacketOut outPak = new(client.Out.GetPacketCode(eServerPackets.PlayerHeading));
                outPak.WriteShort((ushort) client.SessionID);
                outPak.WriteShort(heading);
                outPak.WriteByte(steedSeatPosition);
                outPak.WriteByte(actionFlags);
                outPak.WriteByte(0);
                outPak.WriteByte(stateFlags);
                outPak.WriteByte(healthByte);
                outPak.WriteByte(player.ManaPercent);
                outPak.WriteByte(player.EndurancePercent);
                outPak.WriteByte(0); // Unknown.
                return outPak;
            }

            GSUDPPacketOut CreateOutPak190()
            {
                GSUDPPacketOut outPak = new(client.Out.GetPacketCode(eServerPackets.PlayerHeading));
                outPak.WriteShort((ushort) client.SessionID);
                outPak.WriteShort(heading);
                outPak.WriteByte(0); // Unknown.
                outPak.WriteByte(actionFlags);
                outPak.WriteByte(steedSeatPosition);
                outPak.WriteByte(stateFlags);
                outPak.WriteByte(healthByte);
                outPak.WriteByte(0); // State?
                outPak.WriteByte(player.ManaPercent);
                outPak.WriteByte(player.EndurancePercent);
                return outPak;
            }
        }
    }
}
