using System.Reflection;

namespace DOL.GS.PacketHandler
{
	[PacketLib(193, GameClient.eClientVersion.Version193)]
	public class PacketLib193 : PacketLib192
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.93 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib193(GameClient client)
			: base(client)
		{
		}

		public override void SendBlinkPanel(byte flag)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.Rent(p => p.Init(GetPacketCode(eServerPackets.VisualEffect))))
			{
				GamePlayer player = m_gameClient.Player;

				pak.WriteShort((ushort)player.ObjectID);
				pak.WriteByte(8);
				pak.WriteByte(flag);
				pak.WriteByte(0);

				SendTCP(pak);
			}
		}
	}
}
