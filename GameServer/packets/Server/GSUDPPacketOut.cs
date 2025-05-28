using System;
using DOL.Network;

namespace DOL.GS.PacketHandler
{
    /// <summary>
    /// Outgoing game server UDP packet
    /// </summary>
    public class GSUDPPacketOut : PacketOut, IPooledObject<GSUDPPacketOut>
    {
        public GSUDPPacketOut() { }

        public override void Init(byte code)
        {
            SetLength(0);
            base.Init(code);
            WriteShort(0x00); // Reserved for size.
            WriteShort(0x00); // Reserved for UDP counter.
            base.WriteByte(code);
        }

        public override void WritePacketLength()
        {
            OnStartWritePacketLength();
            WriteShort((ushort) (Length - 5));
        }

        public override string ToString()
        {
            return $"{base.ToString()}: Size={Length - 5} ID=0x{Code:X2}";
        }

        public static PooledObjectKey PooledObjectKey => PooledObjectKey.UdpOutPacket;

        public static GSUDPPacketOut GetForTick(Action<GSUDPPacketOut> initializer)
        {
            return GameLoop.GetForTick(PooledObjectKey, initializer);
        }
    }
}
