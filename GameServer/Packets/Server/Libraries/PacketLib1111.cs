using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler
{
    [PacketLib(1111, GameClient.eClientVersion.Version1111)]
    public class PacketLib1111 : PacketLib1110
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs a new PacketLib for Client Version 1.111
        /// </summary>
        /// <param name="client">the gameclient this lib is associated with</param>
        public PacketLib1111(GameClient client)
            : base(client)
        {

        }

		public override void SendLoginGranted(byte color)
		{
			using (GsTcpPacketOut pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.LoginGranted)))
			{
				pak.WritePascalString(m_gameClient.Account.Name);
				pak.WritePascalString(GameServer.Instance.Configuration.ServerNameShort); //server name
				pak.WriteByte(0x0C); //Server ID
				pak.WriteByte(color);
				pak.WriteByte(0x00);
				pak.WriteByte(0x00);
				SendTCP(pak);
			}
		}
	}
}
