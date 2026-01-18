namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandler(PacketHandlerType.TCP, 0x6A ^ 168, "Checks for bad character names")]
	public class BadNameCheckRequestHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			string name = packet.ReadString(30);

			//TODO do bad name checks here from some database with bad names, this is just a temp test thing here
			var bad = GameServer.Instance.PlayerManager.InvalidNames[name];

			client.Out.SendBadNameCheckReply(name, bad);
		}
	}
}