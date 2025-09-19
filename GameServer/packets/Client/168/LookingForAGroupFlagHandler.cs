namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.LookingForGroupFlag, "handle Change LFG flag", eClientStatus.PlayerInGame)]
	public class LookingForAGroupFlagHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GSPacketIn packet)
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
					Group group = client.Player.Group;
					if (group != null)
					{
						group.Status = code;
					}
					break;
			}
		}
	}
}
