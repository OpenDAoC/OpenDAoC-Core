namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.ShipHookPoint, "Handles Ship hookpoint interact", eClientStatus.PlayerInGame)]
	public class ShipHookpointInteractHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
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
							client.Player.Out.SendMessage("That seat isn't empty!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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