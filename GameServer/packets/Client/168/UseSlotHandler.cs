namespace DOL.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// Handles spell cast requests from client
    /// </summary>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.UseSlot, "Handle Player Use Slot Request.", eClientStatus.PlayerInGame)]
    public class UseSlotHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            if (client.Version >= GameClient.eClientVersion.Version1124)
            {
                client.Player.X = (int) packet.ReadFloatLowEndian();
                client.Player.Y = (int) packet.ReadFloatLowEndian();
                client.Player.Z = (int) packet.ReadFloatLowEndian();
                client.Player.CurrentSpeed = (short) packet.ReadFloatLowEndian();
                client.Player.Heading = packet.ReadShort();
            }

            int flagSpeedData = packet.ReadShort();
            int slot = packet.ReadByte();
            int type = packet.ReadByte();

            // Commenting out. 'flagSpeedData' doesn't vary with movement speed, and this stops the player for a fraction of a second.
            //if ((m_flagSpeedData & 0x200) != 0)
            //	player.CurrentSpeed = (short)(-(m_flagSpeedData & 0x1ff)); // backward movement
            //else
            //	player.CurrentSpeed = (short)(m_flagSpeedData & 0x1ff); // forwardmovement

            client.Player.IsStrafing = (flagSpeedData & 0x4000) != 0;
            client.Player.TargetInView = (flagSpeedData & 0xa000) != 0; // why 2 bits? that has to be figured out
            client.Player.GroundTargetInView = (flagSpeedData & 0x1000) != 0;
            client.Player.UseSlot(slot, type);
        }
    }
}
