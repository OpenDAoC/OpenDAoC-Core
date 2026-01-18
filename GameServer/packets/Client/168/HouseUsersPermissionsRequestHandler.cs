using DOL.GS.Housing;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.HouseUserPermissionRequest, "Handles housing Users permissions requests from menu", eClientStatus.PlayerInGame)]
	public class HouseUsersPermissionsRequestHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
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
}