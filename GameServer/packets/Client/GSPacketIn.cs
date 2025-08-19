using System;
using System.Reflection;
using DOL.Network;

namespace DOL.GS.PacketHandler
{
    /// <summary>
    /// Game server specific packet
    /// </summary>
    public class GSPacketIn : PacketIn, IPooledObject<GSPacketIn>
    {
        /// <summary>
        /// Header size including checksum at the end of the packet
        /// </summary>
        public const ushort HDR_SIZE = 12;

        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Packet ID
        /// </summary>
        private ushort m_code;

        /// <summary>
        /// Packet parameter
        /// </summary>
        private ushort m_parameter;

        /// <summary>
        /// Packet size
        /// </summary>
        private ushort m_psize;

        /// <summary>
        /// Packet sequence (ordering)
        /// </summary>
        private ushort m_sequence;

        /// <summary>
        /// Session ID
        /// </summary>
        private ushort m_sessionID;

        public GSPacketIn() { }

        public GSPacketIn(int size) : base(size) { }

        /// <summary>
        /// Gets the session id
        /// </summary>
        public ushort SessionID { get { return m_sessionID; } }

        /// <summary>
        /// Gets the packet size
        /// </summary>
        public ushort PacketSize { get { return (ushort)(m_psize + HDR_SIZE); } }

        /// <summary>
        /// Gets the size of the data portion of the packet
        /// </summary>
        public ushort DataSize { get { return m_psize; } }

        /// <summary>
        /// Gets the sequence of the packet
        /// </summary>
        public ushort Sequence { get { return m_sequence; } }

        /// <summary>
        /// Gets the packet ID
        /// </summary>
        public ushort Code { get { return m_code; } }

        /// <summary>
        /// Gets the packet parameter
        /// </summary>
        public ushort Parameter { get { return m_parameter; } }

        /// <summary>
        /// Dumps the packet data into the log
        /// </summary>
        public void LogDump()
        {
            if (log.IsDebugEnabled)
                log.Debug(Marshal.ToHexDump(ToString(), ToArray()));
        }

        public override void Init()
        {
            SetLength(0);
            base.Init();
        }

        /// <summary>
        /// Loads the specified count of bytes from another buffer
        /// </summary>
        /// <param name="buf">The buffer to load the data from</param>
        /// <param name="count">The count of packet bytes</param>
        public void Load(byte[] buf, int offset, int count)
        {
            m_psize = (ushort)((buf[offset] << 8) | buf[offset + 1]);
            m_sequence = (ushort)((buf[offset + 2] << 8) | buf[offset + 3]);
            m_sessionID = (ushort)((buf[offset + 4] << 8) | buf[offset + 5]);
            m_parameter = (ushort)((buf[offset + 6] << 8) | buf[offset + 7]);
            // m_code = (ushort)((buf[offset + 8] << 8) | buf[offset + 9]);
            m_code = buf[offset + 9];

            Position = 0;
            Write(buf, offset + 10, count - HDR_SIZE);
            SetLength(count - HDR_SIZE);
            Position = 0;
        }

        /// <summary>
        /// Info about the packet
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format($"{nameof(GSPacketIn)}: Size={m_psize} Sequence=0x{m_sequence:X4} Session={m_sessionID} Parameter={m_parameter} ID=0x{m_code:X2}");
        }

        public static PooledObjectKey PooledObjectKey => PooledObjectKey.InPacket;

        public long IssuedTimestamp { get; set;}

        public static GSPacketIn GetForTick(Action<GSPacketIn> initializer)
        {
            return GameLoop.GetForTick(PooledObjectKey, initializer);
        }

        public static void Release(GSPacketIn packet)
        {
            packet.IssuedTimestamp = 0;
        }
    }
}
