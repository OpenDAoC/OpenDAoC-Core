namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.LookingForGroupFlag, "handle Change LFG flag", EClientStatus.PlayerInGame)]
	public class LookingForAGroupFlagHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{  
			byte code =(byte) packet.ReadByte();
			switch(code)
			{
				case 0x01:
					GroupMgr.SetPlayerLooking(client.Player);
					break;
				case 0x00:
					GroupMgr.RemovePlayerLooking(client.Player);
					break;
				default:
					GroupUtil group = client.Player.Group;
					if (group != null)
					{
						group.Status = code;
					}
					break;
			}
		}
	}
}