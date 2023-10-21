namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, 0x6A ^ 168, "Checks for bad character names")]
	public class BadNameCheckRequestHandler : IPacketHandler
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			string name = packet.ReadString(30);

			//TODO do bad name checks here from some database with bad names, this is just a temp test thing here
			var bad = GameServer.Instance.PlayerManager.InvalidNames[name];

			client.Out.SendBadNameCheckReply(name, bad);
		}
	}
}