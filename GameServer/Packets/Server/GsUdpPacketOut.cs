using Core.Base;

namespace Core.GS.PacketHandler;

/// <summary>
/// Outgoing game server UDP packet
/// </summary>
public class GsUdpPacketOut : PacketOut
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
	public GsUdpPacketOut(byte packetCode) : base()
	{
		m_packetCode = packetCode;
		base.WriteShort(0x00); //reserved for size
		base.WriteShort(0x00); //reserved for UDP counter
		base.WriteByte(packetCode);
	}

	/// <summary>
	/// Calculates the packet size and prepends it
	/// </summary>
	/// <returns>The packet size</returns>
	public override ushort WritePacketLength()
	{
		Position = 0;
		WriteShort((ushort)(Length-5));

		//IMPORTANT!!!
		//Set the capacity of the internal buffer or
		//the byte-array of GetBuffer will be TOO big!
		Capacity = (int)Length;

		return (ushort)(Length-5);
	}

	public override string ToString()
	{
		return base.ToString() + $": Size={Length - 5} ID=0x{m_packetCode:X2}";
	}

	public override void Close()
	{
		// Called by Dispose and normally invalidates the stream.
		// But this is both pointless (`MemoryStream` doesn't have any unmanaged resource) and undesirable (we always want the buffer to remain accessible)
	}
}