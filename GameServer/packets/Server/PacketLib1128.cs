namespace DOL.GS.PacketHandler
{
    [PacketLib(1128, GameClient.eClientVersion.Version1128)]
    public class PacketLib1128 : PacketLib1127
    {
        public PacketLib1128(GameClient client) : base(client) { }

        public override void SendVersionAndCryptKey()
        {
            //Construct the new packet
            using (var pak = new GSTCPPacketOut(GetPacketCode(eServerPackets.CryptKey)))
            {
                pak.WriteIntLowEndian(0); // Disable encryption (1110+ always encrypts).
                pak.WriteString($"{(int) m_gameClient.Version / 1000}.{(int) m_gameClient.Version - 1000}", 5); // Version.
                pak.WriteByte(0x00); // Revision.
                pak.WriteByte(0x00); // Always 0?
                pak.WriteByte(0x00); // Build number.
                pak.WriteByte(0x00); // Build number.
                SendTCP(pak);
                m_gameClient.PacketProcessor.SendPendingPackets();
            }
        }
    }
}
