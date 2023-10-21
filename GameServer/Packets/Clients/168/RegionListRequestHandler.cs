using System.Linq;
using Core.GS.Enums;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.RegionListRequest, "Handles sending the region overview", EClientStatus.None)]
public class RegionListRequestHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		var slot = packet.ReadByte();
		if (slot >= 0x14)
			slot += 300 - 0x14;
		else if (slot >= 0x0A)
			slot += 200 - 0x0A;
		else
			slot += 100;
		var character = client.Account.Characters.FirstOrDefault(c => c.AccountSlot == slot);

		client.Out.SendRegions((ushort)character.Region);
	}
}