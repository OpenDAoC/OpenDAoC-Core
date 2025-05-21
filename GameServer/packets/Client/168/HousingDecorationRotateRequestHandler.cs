using DOL.GS.Housing;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.HouseDecorationRequest, "Handles housing decoration request", eClientStatus.PlayerInGame)]
	public class HousingDecorationRotateRequestHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			ushort housenumber = packet.ReadShort();
			var index = (byte) packet.ReadByte();
			var unk1 = (byte) packet.ReadByte();

			// house is null, return
			var house = HouseMgr.GetHouse(housenumber);
			if (house == null)
				return;

			// player is null, return
			if (client.Player == null)
				return;

			// rotation only works for inside items
			if (!client.Player.InHouse)
				return;

			// no permission to change the interior, return
			if (!house.CanChangeInterior(client.Player, DecorationPermissions.Add))
				return;

			using (var pak = GSTCPPacketOut.Rent(p => p.Init(AbstractPacketLib.GetPacketCode(eServerPackets.HouseDecorationRotate))))
			{
				pak.WriteShort(housenumber);
				pak.WriteByte(index);
				pak.WriteByte(0x01);
				client.Out.SendTCP(pak);
			}
		}
	}
}
