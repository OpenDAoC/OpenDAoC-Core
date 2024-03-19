using DOL.Network;

namespace DOL.GS.PacketHandler
{
    /// <summary>
    /// An outgoing TCP packet
    /// </summary>
    public class GSTCPPacketOut : PacketOut
    {
        public GSTCPPacketOut(byte packetCode)
        {
            PacketCode = packetCode;
            WriteShort(0x00); // Reserved for size.
            base.WriteByte(packetCode);
        }

        public GSTCPPacketOut(byte packetCode, int startingSize) : base(startingSize + 3)
        {
            PacketCode = packetCode;
            WriteShort(0x00); // Reserved for size.
            base.WriteByte(packetCode);
        }

        public override void WritePacketLength()
        {
            OnStartWritePacketLength();
            WriteShort((ushort) (Length - 3));
        }

        public override string ToString()
        {
            return $"{base.ToString()}: Size={Length - 5} ID=0x{PacketCode:X2}";
        }
    }
}
