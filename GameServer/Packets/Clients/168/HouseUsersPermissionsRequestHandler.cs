using Core.GS.Enums;
using Core.GS.Expansions.Foundations;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.HouseUserPermissionRequest, "Handles housing Users permissions requests from menu", EClientStatus.PlayerInGame)]
public class HouseUsersPermissionsRequestHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		int unk1 = packet.ReadByte();
		int unk2 = packet.ReadByte();
		ushort houseNumber = packet.ReadShort();

		// house is null, return
		var house = HouseMgr.GetHouse(houseNumber);
		if (house == null)
			return;

		// player is null, return
		if (client.Player == null)
			return;

		// player has no owner permissions and isn't a GM or admin, return
		if (!house.HasOwnerPermissions(client.Player) && client.Account.PrivLevel <= 1)
			return;

		// build the packet
		client.Out.SendHouseUsersPermissions(house);
	}
}