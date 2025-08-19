using System;
using DOL.Network;

namespace DOL.GS.PacketHandler
{
    /// <summary>
    /// An outgoing TCP packet
    /// </summary>
    public class GSTCPPacketOut : PacketOut, IPooledObject<GSTCPPacketOut>
    {
        public GSTCPPacketOut() { }

        public override void Init(byte code)
        {
            SetLength(0);
            base.Init(code);
            WriteShort(0x00); // Reserved for size.
            base.WriteByte(code);
        }

        public override void WritePacketLength()
        {
            OnStartWritePacketLength();
            WriteShort((ushort) (Length - 3));
        }

        public override string ToString()
        {
            return $"{base.ToString()}: Size={Length - 5} ID=0x{Code:X2}";
        }

        public static PooledObjectKey PooledObjectKey => PooledObjectKey.TcpOutPacket;

        public long IssuedTimestamp { get; set; }

        public static GSTCPPacketOut GetForTick(Action<GSTCPPacketOut> initializer)
        {
            return GameLoop.GetForTick(PooledObjectKey, initializer);
        }

        public static void Release(GSTCPPacketOut packet)
        {
            packet.IssuedTimestamp = 0;
        }
    }
}
