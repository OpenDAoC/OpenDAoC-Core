using DOL.Network;

namespace DOL.GS.PacketHandler
{
    /// <summary>
    /// Outgoing game server UDP packet
    /// </summary>
    public class GSUDPPacketOut : PacketOut
    {
        public GSUDPPacketOut(byte packetCode) : base()
        {
            PacketCode = packetCode;
            WriteShort(0x00); // Reserved for size.
            WriteShort(0x00); // Reserved for UDP counter.
            base.WriteByte(packetCode);
        }

        public override void WritePacketLength()
        {
            OnStartWritePacketLength();
            WriteShort((ushort) (Length - 5));
        }

        public override string ToString()
        {
            return $"{base.ToString()} Size={Length - 5} ID=0x{PacketCode:X2}";
        }
    }
}
