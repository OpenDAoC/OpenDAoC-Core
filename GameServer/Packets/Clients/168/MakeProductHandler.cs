using Core.GS.Enums;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.CraftRequest, "Handles the crafted product answer", EClientStatus.PlayerInGame)]
	public class MakeProductHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			ushort ItemID = packet.ReadShort();
			client.Player.CraftItem(ItemID);
		}
	}
}
