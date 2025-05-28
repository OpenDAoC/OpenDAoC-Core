using System.Reflection;

namespace DOL.GS.PacketHandler
{
	[PacketLib(188, GameClient.eClientVersion.Version188)]
	public class PacketLib188 : PacketLib187
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.88 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib188(GameClient client)
			: base(client)
		{
		}

		public override void SendXFireInfo(byte flag)
		{
			if (m_gameClient == null || m_gameClient.Player == null)
				return;
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init((byte) eServerPackets.XFire)))
			{
				pak.WriteShort((ushort)m_gameClient.Player.ObjectID);
				pak.WriteByte(flag);
				pak.WriteByte(0x00);
				SendTCP(pak);
			}
		}
	}
}
