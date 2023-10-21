namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.WarmapBonusRequest, "Show warmap bonuses", EClientStatus.PlayerInGame)]
	public class WarmapBonusesRequestHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			client.Out.SendWarmapBonuses();
		}
	}
}