using Core.GS.Enums;
using Core.GS.Expansions.Foundations;
using Core.GS.GameUtils;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.HouseDecorationRotate, "Handles housing decoration rotation", EClientStatus.PlayerInGame)]
	public class HousingDecorationRotateHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			int unk1 = packet.ReadByte();
			int position = packet.ReadByte();
			ushort housenumber = packet.ReadShort();
			ushort angle = packet.ReadShort();
			ushort unk2 = packet.ReadShort();

			// rotation only works for inside items
			if (!client.Player.InHouse)
				return;

			// house is null, return
			var house = HouseMgr.GetHouse(housenumber);
			if (house == null)
				return;

			// player is null, return
			if (client.Player == null)
				return;

			// no permission to change the interior, return
			if (!house.CanChangeInterior(client.Player, EDecorationPermissions.Add))
				return;

			if (house.IndoorItems.ContainsKey(position) == false)
				return;

			// grab the item in question
			IndoorItem iitem = house.IndoorItems[position];
			if (iitem == null)
			{
				client.Player.Out.SendMessage("error: id was null", EChatType.CT_Help, EChatLoc.CL_SystemWindow);
				return;
			} //should this ever happen?

			// adjust the item's roation
			int old = iitem.Rotation;
			iitem.Rotation = (iitem.Rotation + angle)%360;

			if (iitem.Rotation < 0)
			{
				iitem.Rotation = 360 + iitem.Rotation;
			}

			iitem.DatabaseItem.Rotation = iitem.Rotation;

			// save item
			GameServer.Database.SaveObject(iitem.DatabaseItem);

			ChatUtil.SendSystemMessage(client,
			                           string.Format("Interior decoration rotated from {0} degrees to {1}", old, iitem.Rotation));

			// update all players in the house.
			foreach (GamePlayer plr in house.GetAllPlayersInHouse())
			{
				plr.Client.Out.SendFurniture(house, position);
			}
		}
	}
}