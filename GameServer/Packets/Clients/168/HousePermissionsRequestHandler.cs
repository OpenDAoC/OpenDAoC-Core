using DOL.GS.Housing;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.HousePermissionRequest, "Handles housing permissions requests from menu", EClientStatus.PlayerInGame)]
	public class HousePermissionsRequestHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			int pid = packet.ReadShort();
			ushort housenumber = packet.ReadShort();

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

			// send out the house permissions
			using (var pak = new GsTcpPacketOut(client.Out.GetPacketCode(EServerPackets.HousingPermissions)))
			{
				pak.WriteByte(HousingConstants.MaxPermissionLevel); // number of permissions ?
				pak.WriteByte(0x00); // unknown
				pak.WriteShort((ushort)house.HouseNumber);

				foreach (var entry in house.PermissionLevels)
				{
					var level = entry.Key;
					var permission = entry.Value;

					pak.WriteByte((byte)level);
					pak.WriteByte(permission.CanEnterHouse ? (byte)1 : (byte)0);
					pak.WriteByte(permission.Vault1);
					pak.WriteByte(permission.Vault2);
					pak.WriteByte(permission.Vault3);
					pak.WriteByte(permission.Vault4);
					pak.WriteByte(permission.CanChangeExternalAppearance ? (byte)1 : (byte)0);
					pak.WriteByte(permission.ChangeInterior);
					pak.WriteByte(permission.ChangeGarden);
					pak.WriteByte(permission.CanBanish ? (byte)1 : (byte)0);
					pak.WriteByte(permission.CanUseMerchants ? (byte)1 : (byte)0);
					pak.WriteByte(permission.CanUseTools ? (byte)1 : (byte)0);
					pak.WriteByte(permission.CanBindInHouse ? (byte)1 : (byte)0);
					pak.WriteByte(permission.ConsignmentMerchant);
					pak.WriteByte(permission.CanPayRent ? (byte)1 : (byte)0);
					pak.WriteByte(0x00); // ??
				}

				client.Out.SendTCP(pak);
			}
		}
	}
}