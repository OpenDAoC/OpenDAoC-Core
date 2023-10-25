using Core.GS.Enums;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.WarmapBonusRequest, "Show warmap bonuses",
	EClientStatus.PlayerInGame)]
public class WarmapBonusesRequestHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		client.Out.SendWarmapBonuses();
	}
}