namespace Core.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// Handles the ping packet
    /// </summary>
    [PacketHandler(EPacketHandlerType.TCP, EClientPackets.PingRequest, "Sends the ping reply", EClientStatus.None)]
    public class PingRequestHandler : IPacketHandler
    {
        /// <summary>
        /// Called when the packet has been received
        /// </summary>
        /// <param name="client">Client that sent the packet</param>
        /// <param name="packet">Packet data</param>
        /// <returns>Non zero if function was successfull</returns>
        public void HandlePacket(GameClient client, GsPacketIn packet)
        {
            packet.Skip(4); //Skip the first 4 bytes
            client.PingTime = GameLoop.GetCurrentTime();
            ulong timestamp = packet.ReadInt();
            client.Out.SendPingReply(timestamp, packet.Sequence);
        }
    }
}
