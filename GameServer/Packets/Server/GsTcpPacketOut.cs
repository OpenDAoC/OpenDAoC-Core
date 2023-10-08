using DOL.Network;

namespace DOL.GS.PacketHandler
{
	/// <summary>
	/// An outgoing TCP packet
	/// </summary>
	public class GsTcpPacketOut : PacketOut
	{
		private byte m_packetCode;
		
		/// <summary>
		/// This Packet Byte Handling Code
		/// </summary>
		public byte PacketCode {
			get { return m_packetCode; }
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="packetCode">ID of the packet</param>
		public GsTcpPacketOut(byte packetCode)
		{
			m_packetCode = packetCode;
			base.WriteShort(0x00); //reserved for size
			base.WriteByte(packetCode);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="packetCode">ID of the packet</param>
		public GsTcpPacketOut(byte packetCode, int startingSize) : base(startingSize + 3)
		{
			m_packetCode = packetCode;
			base.WriteShort(0x00); //reserved for size
			base.WriteByte(packetCode);
		}

		public override string ToString()
		{
			return base.ToString() + $": Size={Length - 3} ID=0x{m_packetCode:X2}";
		}

		public override void Close()
		{
			// Called by Dispose and normally invalidates the stream.
			// But this is both pointless (`MemoryStream` doesn't have any unmanaged resource) and undesirable (we always want the buffer to remain accessible)
		}
	}
}
