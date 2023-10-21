using Core.GS.Enums;
using Core.GS.Expansions.Foundations;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.HouseDecorationRequest, "Handles housing decoration request", EClientStatus.PlayerInGame)]
	public class HousingDecorationRotateRequestHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
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
			if (!house.CanChangeInterior(client.Player, EDecorationPermissions.Add))
				return;

			var pak = new GsTcpPacketOut(client.Out.GetPacketCode(EServerPackets.HouseDecorationRotate));
			pak.WriteShort(housenumber);
			pak.WriteByte(index);
			pak.WriteByte(0x01);
			client.Out.SendTCP(pak);
		}
	}
}