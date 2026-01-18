namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.WarmapBonusRequest, "Show warmap bonuses", eClientStatus.PlayerInGame)]
	public class WarmapBonusesRequestHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			client.Out.SendWarmapBonuses();
		}
	}
}