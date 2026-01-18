using DOL.Database;
using DOL.GS.Housing;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.HousePermissionSet, "Handles housing permissions changes", eClientStatus.PlayerInGame)]
	public class HousePermissionsSetHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			int level = packet.ReadByte();
			int unk1 = packet.ReadByte();
			ushort housenumber = packet.ReadShort();

			// make sure permission level is within bounds
			if (level < HousingConstants.MinPermissionLevel || level > HousingConstants.MaxPermissionLevel)
				return;

			// house is null, return
			var house = HouseMgr.GetHouse(housenumber);
			if (house == null)
				return;

			// player is null, return
			if (client.Player == null)
				return;

			// player has no owner permissions and isn't a GM or admin, return
			if (!house.HasOwnerPermissions(client.Player) && client.Account.PrivLevel <= 1)
				return;

			// read in the permission values
			DbHousePermissions permission = house.PermissionLevels[level];

			permission.CanEnterHouse = (packet.ReadByte() != 0);
			permission.Vault1 = (byte) packet.ReadByte();
			permission.Vault2 = (byte) packet.ReadByte();
			permission.Vault3 = (byte) packet.ReadByte();
			permission.Vault4 = (byte) packet.ReadByte();
			permission.CanChangeExternalAppearance = (packet.ReadByte() != 0);
			permission.ChangeInterior = (byte) packet.ReadByte();
			permission.ChangeGarden = (byte) packet.ReadByte();
			permission.CanBanish = (packet.ReadByte() != 0);
			permission.CanUseMerchants = (packet.ReadByte() != 0);
			permission.CanUseTools = (packet.ReadByte() != 0);
			permission.CanBindInHouse = (packet.ReadByte() != 0);
			permission.ConsignmentMerchant = (byte) packet.ReadByte();
			permission.CanPayRent = (packet.ReadByte() != 0);
			int unk2 = (byte) packet.ReadByte();

			// save the updated permission
			GameServer.Database.SaveObject(permission);
		}
	}
}