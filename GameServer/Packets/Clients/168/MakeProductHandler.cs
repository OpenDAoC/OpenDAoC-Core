using Core.GS.Enums;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.CraftRequest, "Handles the crafted product answer", EClientStatus.PlayerInGame)]
public class MakeProductHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		ushort ItemID = packet.ReadShort();
		client.Player.CraftItem(ItemID);
	}
}