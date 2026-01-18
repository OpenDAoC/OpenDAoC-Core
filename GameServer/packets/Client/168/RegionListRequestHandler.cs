using System.Linq;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandler(PacketHandlerType.TCP, eClientPackets.RegionListRequest, "Handles sending the region overview", eClientStatus.None)]
	public class RegionListRequestHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
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
}
