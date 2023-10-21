using Core.GS.Enums;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.ShipHookPoint, "Handles Ship hookpoint interact", EClientStatus.PlayerInGame)]
	public class ShipHookpointInteractHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			ushort unk1 = packet.ReadShort();
			ushort objectOid = packet.ReadShort();
			ushort unk2 = packet.ReadShort();
			int slot = packet.ReadByte();
			int flag = packet.ReadByte();
			int currency = packet.ReadByte();
			int unk3 = packet.ReadByte();
			ushort unk4 = packet.ReadShort();
			int type = packet.ReadByte();
			int unk5 = packet.ReadByte();
			int unk6 = packet.ReadShort();

			if (client.Player.Steed == null || client.Player.Steed is GameBoat == false)
				return;

			switch (flag)
			{
				case 0:
					{
						//siegeweapon
						break;
					}
				case 3:
					{
						//move
						GameBoat boat = client.Player.Steed as GameBoat;
						if (boat.Riders[slot] == null)
						{
							client.Player.SwitchSeat(slot);
						}
						else
						{
							client.Player.Out.SendMessage("That seat isn't empty!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						}
						break;
					}
				default:
					{
						GameServer.KeepManager.Log.Error($"Unhandled ShipHookpointInteract client to server packet unk1 {unk1} objectOid {objectOid} unk2 {unk2} slot {slot} flag {flag} currency {currency} unk3 {unk3} unk4 {unk4} type {type} unk5 {unk5} unk6 {unk6}");
						break;
					}
			}
		}
	}
}