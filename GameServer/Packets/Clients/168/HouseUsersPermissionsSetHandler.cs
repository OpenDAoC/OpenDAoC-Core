using Core.GS.Housing;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.HouseUserPermissionSet, "Handles housing Users permissions requests", EClientStatus.PlayerInGame)]
	public class HouseUsersPermissionsSetHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			int permissionSlot = packet.ReadByte();
			int newPermissionLevel = packet.ReadByte();
			ushort houseNumber = packet.ReadShort();

			// house is null, return
			var house = HouseMgr.GetHouse(houseNumber);
			if (house == null)
				return;

			// player is null, return
			if (client.Player == null)
				return;

			// can't set permissions unless you're the owner.
			if (!house.HasOwnerPermissions(client.Player) && client.Account.PrivLevel <= 1)
				return;

			// check if we're setting or removing permissions
			if (newPermissionLevel == 100)
			{
				house.RemovePermission(permissionSlot);
			}
			else
			{
				house.AdjustPermissionSlot(permissionSlot, newPermissionLevel);
			}
		}
	}
}