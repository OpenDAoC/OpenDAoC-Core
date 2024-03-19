using System;
using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
	public abstract class AbstractPacketLib
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The GameClient of this PacketLib
		/// </summary>
		protected readonly GameClient m_gameClient;

		/// <summary>
		/// Constructs a new PacketLib
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public AbstractPacketLib(GameClient client)
		{
			m_gameClient = client;
		}

		/// <summary>
		/// Retrieves the packet code depending on client version
		/// </summary>
		/// <param name="packetCode"></param>
		/// <returns></returns>
		public virtual byte GetPacketCode(eServerPackets packetCode)
		{
			return (byte)packetCode;
		}

		/// <summary>
		/// Sends a packet via TCP
		/// </summary>
		/// <param name="packet">The packet to be sent</param>
		public void SendTCP(GSTCPPacketOut packet)
		{
			m_gameClient.PacketProcessor.QueuePacket(packet);
		}

		/// <summary>
		/// Send the packet via UDP
		/// </summary>
		/// <param name="packet">Packet to be sent</param>
		public virtual void SendUDP(GSUDPPacketOut packet)
		{
			SendUDP(packet, false);
		}

		/// <summary>
		/// Send the packet via UDP
		/// </summary>
		/// <param name="packet">Packet to be sent</param>
		/// <param name="isForced">Force UDP packet if <code>true</code>, else packet can be sent over TCP</param>
		public virtual void SendUDP(GSUDPPacketOut packet, bool isForced)
		{
			m_gameClient.PacketProcessor.QueuePacket(packet, isForced);
		}

		/// <summary>
		/// Finds and creates packetlib for specified raw version.
		/// </summary>
		/// <param name="rawVersion">The version number sent by the client.</param>
		/// <param name="client">The client for which to create packet lib.</param>
		/// <param name="version">The client version of packetlib.</param>
		/// <returns>null if not found or new packetlib instance.</returns>
		public static IPacketLib CreatePacketLibForVersion(int rawVersion, GameClient client, out GameClient.eClientVersion version)
		{
			foreach (Type t in ScriptMgr.GetDerivedClasses(typeof (IPacketLib)))
			{
				foreach (PacketLibAttribute attr in t.GetCustomAttributes(typeof (PacketLibAttribute), false))
				{
					if (attr.RawVersion == rawVersion)
					{
						try
						{
							IPacketLib lib = (IPacketLib) Activator.CreateInstance(t, new object[] {client});
							version = attr.ClientVersion;
							return lib;
						}
						catch (Exception e)
						{
							if (log.IsErrorEnabled)
								log.Error("error creating packetlib (" + t.FullName + ") for raw version " + rawVersion, e);
						}
					}
				}
			}
			
			version = GameClient.eClientVersion.VersionUnknown;
			return null;
		}
		
		/// <summary>
		/// Return the msb or lsb byte used the server versionning
		/// eg: 199 -> 1.99; 1100 -> 1.100
		/// </summary>
		/// <param name="version"></param>
		/// <param name="IsMSB"></param>
		/// <returns></returns>
		public static byte ParseVersion(int version, bool IsMSB)
		{
			int cte_version = 100;
			if (version > 199) cte_version = 1000;

			if (IsMSB)
				return (byte)(version / cte_version);
			else
				return (byte)((version % cte_version)/10);
		}
	}
}
