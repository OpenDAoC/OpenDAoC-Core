using System.Collections;
using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.LookingForGroup, "handle Looking for a group", EClientStatus.PlayerInGame)]
	public class LookingForAGroupHandler : IPacketHandler
	{
		//rewritten by Corillian so if it doesn't work you know who to yell at ;)
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			byte grouped = (byte)packet.ReadByte();
			ArrayList list = new ArrayList();
			if (grouped != 0x00)
			{
				var groups = GroupMgr.ListGroupByStatus(0x00);
				if (groups != null)
				{
					foreach (GroupUtil group in groups)
						if (GameServer.ServerRules.IsAllowedToGroup(group.Leader, client.Player, true))
						{
							list.Add(group.Leader);
						}
				}
			}

			var Lfg = GroupMgr.LookingForGroupPlayers();

			if (Lfg != null)
			{
				foreach (GamePlayer player in Lfg)
				{
					if (player != client.Player && GameServer.ServerRules.IsAllowedToGroup(client.Player, player, true))
					{
						list.Add(player);
					}
				}
			}

			client.Out.SendFindGroupWindowUpdate((GamePlayer[])list.ToArray(typeof(GamePlayer)));
		}
	}
}