using System;

namespace DOL.GS.PacketHandler
{
	/// <summary>
	/// Type of packet handler
	/// </summary>
	public enum EPacketHandlerType
	{
		TCP = 0x01,
		UDP = 0x02
	}

	/// <summary>
	/// Denotes a class as a packet handler
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class PacketHandlerAttribute : Attribute 
	{
		/// <summary>
		/// Type of packet handler
		/// </summary>
		protected EPacketHandlerType m_type;
		/// <summary>
		/// Packet ID to handle
		/// </summary>
		protected int m_code;
		/// <summary>
		/// Description of the packet handler
		/// </summary>
		protected string m_desc;

		/// <summary>
		/// Holds the ID of the preprocessor to use for this packet.
		/// </summary>
		protected int m_preprocessorId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type of packet to handle</param>
		/// <param name="code">ID of the packet to handle</param>
		/// <param name="desc">Description of the packet handler</param>
		public PacketHandlerAttribute(EPacketHandlerType type, int code, string desc)
		{
			m_type = type;
			m_code = code;
			m_desc = desc;
			m_preprocessorId = (int)eClientStatus.None;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type of packet to handle</param>
		/// <param name="code">ID of the packet to handle</param>
		/// <param name="desc">Description of the packet handler</param>
		/// <param name="preprocessorId">ID of the preprocessor to use for this packet</param>
		public PacketHandlerAttribute(EPacketHandlerType type, int code, string desc, int preprocessorId)
		{
			m_type = type;
			m_code = code;
			m_desc = desc;
			m_preprocessorId = preprocessorId;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type of packet to handle</param>
		/// <param name="code">ID of the packet to handle</param>
		/// <param name="desc">Description of the packet handler</param>
		/// <param name="preprocessorId">ID of the preprocessor to use for this packet</param>
		public PacketHandlerAttribute(EPacketHandlerType type, int code, string desc, eClientStatus preprocessorId)
		{
			m_type = type;
			m_code = code;
			m_desc = desc;
			m_preprocessorId = (int) preprocessorId;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type of packet to handle</param>
		/// <param name="code">ID of the packet to handle</param>
		/// <param name="desc">Description of the packet handler</param>
		/// <param name="preprocessorId">ID of the preprocessor to use for this packet</param>
		public PacketHandlerAttribute(EPacketHandlerType type, EClientPackets code, eClientStatus preprocessorId)
		{
			m_type = type;
			m_code = (int)code;
			m_desc = "";
			m_preprocessorId = (int)preprocessorId;
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Type of packet to handle</param>
		/// <param name="code">ID of the packet to handle</param>
		/// <param name="desc">Description of the packet handler</param>
		/// <param name="preprocessorId">ID of the preprocessor to use for this packet</param>
		public PacketHandlerAttribute(EPacketHandlerType type, EClientPackets code, string desc, eClientStatus preprocessorId)
		{
			m_type = type;
			m_code = (int)code;
			m_desc = desc;
			m_preprocessorId = (int)preprocessorId;
		}

		/// <summary>
		/// Gets the packet type
		/// </summary>
		public EPacketHandlerType Type
		{
			get
			{
				return m_type;
			}
		}

		/// <summary>
		/// Gets the packet ID that is handled
		/// </summary>
		public int Code 
		{
			get
			{
				return m_code;
			}
		}

		/// <summary>
		/// Gets the description of the packet handler
		/// </summary>
		public string Description
		{
			get
			{
				return m_desc;
			}
		}

		/// <summary>
		/// Gets the preprocessor ID associated with this packet.
		/// </summary>
		public int PreprocessorID
		{
			get
			{
				return m_preprocessorId;
			}
		}
	}
}